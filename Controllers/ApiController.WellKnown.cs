namespace Apagee.Controllers;

public partial class ApiController : BaseController
{
    [HttpGet]
    [NoContext]
    [JrdContentType]
    [Route(Globals.WEBFINGER_PATH)]
    public IActionResult Webfinger([FromQuery] string resource)
    {
        try
        {
            if (GlobalConfiguration.Current is not { FediverseUsername.Length: > 0, PublicHostname.Length: > 0 })
            {
                return ServerError500(message: "Server has not been set up yet.");
            }

            // Single-user only, otherwise we'd have to parse the acct: subject.
            var acct = WebfingerAccount.CreateUser();
            var app = WebfingerAccount.CreateApp();

            if (resource is null)
            {
                return BadRequest400("Missing resource parameter.");
            }

            if (resource == acct.Subject)
            {
                return Ok(acct);
            }
            else if (resource == app.Subject)
            {
                return Ok(app);
            }

            return NotFound404(message: "Resource not found.");
        }
        catch
        {
            return ServerError500(message: "Webfinger endpoint error.");
        }
    }

    [HttpGet]
    [NoContext]
    [ExplicitContentType("application/json")]
    [Route(Globals.NODEINFO_PATH)]
    public async Task<IActionResult> NodeDiscovery()
    {
        return Ok(new NodeDiscovery
        {
            Rel = Globals.NODEINFO_REL,
            Href = $"https://{GlobalConfiguration.Current!.PublicHostname}/nodeinfo/2.0"
        });
    }
    
    [HttpGet]
    [NoContext]
    [NodeInfoContentType]
    [Route(Globals.NODEINFO_DOC_PATH)]
    public async Task<IActionResult> NodeInfo()
    {
        var articleCount = await ArticleService.GetCount(true);

        return Ok(new NodeInfoResult
        {
            Version = "2.0",
            Software = new()
            {
                Name = "Apagee",
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.1",
            },
            Protocols = ["activitypub"],
            Services = new(),
            Usage = new()
            {
                Users = new()
                {
                    Total = 1,
                    ActiveHalfyear = 1,
                    ActiveMonth = 1
                },
                LocalPosts = articleCount
            },
            OpenRegistrations = false,
            Metadata = new()
            {
                NodeName = SettingsService.Current?.ODataTitle ?? GlobalConfiguration.Current!.PublicHostname,
                NodeDescription = SettingsService.Current?.ODataDescription ?? GlobalConfiguration.Current?.FediverseBio ?? GlobalConfiguration.Current!.FediverseUsername,
            }
        });
    }
}