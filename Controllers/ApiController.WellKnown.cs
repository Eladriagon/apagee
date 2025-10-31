using System.Threading.Tasks;

namespace Apagee.Controllers;

public partial class ApiController : BaseController
{
    [HttpGet]
    [Route(Globals.WEBFINGER_PATH)]
    [Produces(Globals.JSON_RD_CONTENT_TYPE)]
    public IActionResult Webfinger([FromQuery] string resource)
    {
        try
        {
            if (GlobalConfiguration.Current is not { FediverseUsername.Length: > 0, PublicHostname.Length: > 0 })
            {
                return StatusCode(500, "Server has not been set up yet.");
            }

            // Single-user only, otherwise we'd have to parse the acct: subject.
            var acct = WebfingerAccount.CreateUser();
            var app = WebfingerAccount.CreateApp();

            if (resource is null)
            {
                return BadRequest("Missing resource parameter.");
            }

            if (resource == acct.Subject)
            {
                return Json(acct, JsonSerializerOptions.Web);
            }
            else if (resource == app.Subject)
            {
                return Json(app, JsonSerializerOptions.Web);
            }

            return NotFound("Resource not found.");
        }
        catch
        {
            return StatusCode(500, "Webfinger endpoint error.");
        }
    }

    [HttpGet]
    [Route(Globals.NODEINFO_PATH)]
    public async Task<IActionResult> NodeDiscovery()
    {
        return Json(new NodeDiscovery
        {
            Rel = Globals.NODEINFO_REL,
            Href = $"https://{GlobalConfiguration.Current!.PublicHostname}/nodeinfo/2.0"
        }, JsonSerializerOptions.Web);
    }
    
    [HttpGet]
    [Route(Globals.NODEINFO_DOC_PATH)]
    public async Task<IActionResult> NodeInfo()
    {
        var articleCount = await ArticleService.GetCount(true);

        return Json(new NodeInfo
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
        }, JsonSerializerOptions.Web);
    }
}