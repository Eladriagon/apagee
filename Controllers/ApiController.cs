namespace Apagee.Controllers;

[ApiController]
public class ApiController(GlobalConfiguration config, KeypairHelper keypairHelper) 
    : BaseController
{
    public GlobalConfiguration Config { get; } = config;
    public KeypairHelper KeypairHelper { get; } = keypairHelper;

    [HttpGet]
    [Route(".well-known/webfinger")]
    public IActionResult Webfinger([FromQuery] string resource)
    {
        // Single-user only, otherwise we'd have to parse the acct: subject.
        var acct = WebfingerAccount.Create(Config.PublicHostname, Config.FediverseUsername);

        if (resource is null)
        {
            return BadRequest("Missing resource parameter.");
        }

        if (resource != acct.Subject)
        {
            return NotFound("Resource not found.");
        }

        return Ok(acct);
    }

    [HttpGet]
    [Route("api/users/{username}")]
    public IActionResult GetUser([FromRoute] string username)
    {
        // Single-user only
        if (username != Config.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var user = Person.Create(KeypairHelper.KeyId, KeypairHelper.ActorPublicKeyPem ?? throw new ApageeException("Cannot create user: Actor public key PEM is null."));

        return Ok(user);
    }
}