using System.Threading.Tasks;

namespace Apagee.Controllers;

[ApiController]
public class ApiController(ArticleService articleService, KeypairHelper keypairHelper, IMemoryCache cache, InboxService inboxService, IHttpClientFactory httpClientFactory, JsonSerializerOptions opts)
    : BaseController
{
    public ArticleService ArticleService { get; } = articleService;
    public KeypairHelper KeypairHelper { get; } = keypairHelper;
    public IMemoryCache Cache { get; } = cache;
    public InboxService InboxService { get; } = inboxService;
    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    public JsonSerializerOptions Opts { get; } = opts;

    private string ActorId => $"https://{GlobalConfiguration.Current?.PublicHostname}/api/users/{GlobalConfiguration.Current!.FediverseUsername}";
    private string CurrentPath => $"https://{GlobalConfiguration.Current?.PublicHostname}{Request.Path}";
    private string AtomId => CurrentPath + Request.QueryString;
    private APubActor CurrentActor => APubActor.Create<Person>(KeypairHelper.KeyId, KeypairHelper.ActorPublicKeyPem ?? throw new ApageeException("Cannot create user: Actor public key PEM is null."));

    [HttpGet]
    [Route(".well-known/webfinger")]
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
                return Ok(acct);
            }
            else if (resource == app.Subject)
            {
                return Ok(app);
            }

            return NotFound("Resource not found.");
        }
        catch
        {
            return StatusCode(500, "Webfinger endpoint error.");
        }
    }

    [HttpGet]
    [Route("actor")]
    public async Task<IActionResult> SiteActor()
    {
        return Redirect(ActorId);
    }

    [HttpGet]
    [Route("api/users/{username}")]
    public IActionResult GetUser([FromRoute] string username)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var user = CurrentActor;

        if (TryRedirectHtml(user, out var link))
        {
            return Redirect(link);
        }

        return Ok(user);
    }

    [HttpGet]
    [Route("api/users/{username}/statuses/{id}")]
    public async Task<IActionResult> GetStatusAsync([FromRoute] string username, [FromRoute] string id)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var article = await ArticleService.GetByUid(id);

        if (article is not { Status: ArticleStatus.Published })
        {
            return NotFound();
        }

        var aPubArticle = APubStatus.FromArticle(article);

        if (TryRedirectHtml(aPubArticle, out var link))
        {
            return Redirect(link);
        }

        return Ok(aPubArticle);
    }

    [HttpGet]
    [Route("api/users/{username}/statuses/{id}/activity")]
    public async Task<IActionResult> GetStatusActivityAsync([FromRoute] string username, [FromRoute] string id)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var article = await ArticleService.GetByUid(id);

        if (article is not { Status: ArticleStatus.Published })
        {
            return NotFound();
        }

        var aPubArticle = APubArticle.FromArticle(article);

        var act = APubActivity.Wrap<Create>((APubObject)aPubArticle, CurrentActor.Id, published: article.PublishedOn);
        act.Published = article.PublishedOn;

        if (TryRedirectHtml(aPubArticle, out var link))
        {
            return Redirect(link);
        }

        return Ok(act);
    }

    [HttpGet]
    [Route("api/users/{username}/articles/{id}")]
    public async Task<IActionResult> GetArticleAsync([FromRoute] string username, [FromRoute] string id)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var article = await ArticleService.GetByUid(id);

        if (article is not { Status: ArticleStatus.Published })
        {
            return NotFound();
        }

        var aPubArticle = APubArticle.FromArticle(article);

        if (TryRedirectHtml(aPubArticle, out var link))
        {
            return Redirect(link);
        }

        return Ok(aPubArticle);
    }

    [HttpGet]
    [Route("api/users/{username}/articles/{id}/activity")]
    public async Task<IActionResult> GetWrappedArticleAsync([FromRoute] string username, [FromRoute] string id)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var article = await ArticleService.GetByUid(id);

        if (article is not { Status: ArticleStatus.Published })
        {
            return NotFound();
        }

        var aPubArticle = APubArticle.FromArticle(article);

        var act = APubActivity.Wrap<Create>((APubObject)aPubArticle, CurrentActor.Id, published: article.PublishedOn);
        act.Published = article.PublishedOn;

        if (TryRedirectHtml(aPubArticle, out var link))
        {
            return Redirect(link);
        }

        return Ok(act);
    }

    [HttpGet]
    [Route("api/users/{username}/followers")]
    public async Task<IActionResult> GetFollowers([FromRoute] string username, [FromQuery] string? after = null)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var total = await InboxService.GetFollowerCount();

        if (after is null)
        {
            var oc = new APubOrderedCollection
            {
                Id = $"{ActorId}/followers",
                TotalItems = total,
                First = $"{ActorId}/followers?after={Ulid.MaxValue}"
            };
            return Ok(oc);
        }
        else
        {
            var followerList = await InboxService.GetFollowerList(after);
            var followerIds = new APubPolyBase();
            followerIds.AddRange(followerList.Select(f => new APubLink(f)));
            var ocp = new APubOrderedCollectionPage
            {
                Id = $"{ActorId}/followers?after={after}",
                TotalItems = 0,
                PartOf = new APubLink($"{ActorId}/followers"),
                OrderedItems = followerIds
            };
            return Ok(ocp);
        }
    }

    [HttpGet]
    [Route("api/users/{username}/following")]
    public IActionResult GetFollowing([FromRoute] string username)
    {
        return Ok(new APubOrderedCollection
        {
            Id = AtomId,
            TotalItems = 0
        });
    }

    [HttpGet]
    [Route("api/users/{username}/outbox")]
    public async Task<IActionResult> GetOutbox([FromRoute] string username, [FromQuery] bool page = false, [FromQuery] string? max = null, [FromQuery] string? min = null)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var basePathPaged = $"{CurrentPath}?page=true";

        var oc = new APubOrderedCollection
        {
            Id = AtomId,
            TotalItems = await ArticleService.GetCount(true)
        };

        var ocp = new APubOrderedCollectionPage
        {
            Id = AtomId,
            PartOf = new APubLink(CurrentActor.Outbox),
            Items = new()
        };


        if (!page)
        {
            oc.First = basePathPaged;
            oc.Last = $"{basePathPaged}&min={Ulid.MinValue}";

            return Ok(oc);
        }
        else
        {
            IEnumerable<Article> articles;
            if (min is null && max is null)
            {
                articles = await ArticleService.GetOlderThan();
                if (articles.Any())
                {
                    ocp.Next = new APubLink($"{basePathPaged}&max={articles.Last().Uid}");
                    ocp.Prev = new APubLink($"{basePathPaged}&max={articles.First().Uid}");
                }
            }
            else if (min is null)
            {
                articles = await ArticleService.GetOlderThan(max);
                if (articles.Any())
                {
                    ocp.Next = new APubLink($"{basePathPaged}&max={articles.Last().Uid}");
                    ocp.Prev = new APubLink($"{basePathPaged}&max={articles.First().Uid}");
                }
            }
            else if (max is null)
            {
                articles = await ArticleService.GetNewerThan(min);
                if (articles.Any())
                {
                    ocp.Next = new APubLink($"{basePathPaged}&min={articles.Last().Uid}");
                    ocp.Prev = new APubLink($"{basePathPaged}&min={articles.First().Uid}");
                }
            }
            else
            {
                return StatusCode(400, "Cannot specify both min and max.");
            }

            ocp.Items.AddRange(articles.SelectMany(a => new[] {
                APubActivity.Wrap<Create>(APubStatus.FromArticle(a), CurrentActor.Id, published: a.PublishedOn),
                APubActivity.Wrap<Create>(APubArticle.FromArticle(a), CurrentActor.Id, published: a.PublishedOn)
            }));

            return Ok(ocp);
        }
    }

    [HttpGet]
    [Route("api/users/{username}/inbox")]
    public async Task<IActionResult> GetInbox([FromRoute] string username)
    {
        return NotFound();
    }

    [HttpPost]
    [Route("api/inbox")]
    public async Task<IActionResult> PostToSharedInbox()
    {
        return await PostToInbox(GlobalConfiguration.Current!.FediverseUsername);
    }

    [HttpPost]
    [Route("api/users/{username}/inbox")]
    public async Task<IActionResult> PostToInbox([FromRoute] string username)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        using var sr = new StreamReader(Request.Body);
        var body = await sr.ReadToEndAsync();

        var item = new Inbox
        {
            ID = "",
            UID = Ulid.NewUlid().ToString(),
            BodySize = body.Length,
            BodyData = body,
            Type = "",
            ReceivedOn = DateTime.UtcNow,
            ContentType = Request.ContentType?.ToString() ?? "no/type",
            RemoteServer = Request.Headers["X-Forwarded-For"].ToString() ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };


        JsonNode? json = default;
        try
        {
            json = await JsonNode.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(body)));
            if (json?.GetValueKind() == JsonValueKind.Object)
            {
                item.ID = json["id"]?.GetValue<string>() ?? "unknown-" + item.UID;
                item.Type = json["type"]?.GetValue<string>() ?? "unknown-" + item.UID;
            }
            else
            {
                item.ID = "not-an-obj-" + item.UID;
                item.Type = "not-an-obj-" + item.UID;
            }
        }
        catch
        {
            item.ID = "err-" + item.UID;
            item.Type = "err-" + item.UID;
        }

        if (json is not null)
        {
            if (Request.HasJsonContentType()
                || Request.Headers.ContentType.ToString().ToLower().Contains(Globals.JSON_LD_CONTENT_TYPE)
                || Request.Headers.ContentType.ToString().ToLower().Contains(Globals.JSON_ACT_CONTENT_TYPE))
            {
                switch (item.Type)
                {
                    case APubConstants.TYPE_ACT_FOLLOW when json["object"] is JsonValue v:
                        if (v.GetValue<string>().ToUpper() == ActorId.ToUpper())
                        {
                            await InboxService.CreateFollower(APubFollower.FromJson(json));
                        }
                        break;
                    case APubConstants.TYPE_ACT_FOLLOW when json["object"] is JsonArray arr && arr[0] is JsonValue v:
                        if (v.GetValue<string>().ToUpper() == ActorId.ToUpper())
                        {
                            await InboxService.CreateFollower(APubFollower.FromJson(json));
                        }
                        break;
                    case APubConstants.TYPE_ACT_UNDO when json["object"] is JsonObject obj 
                        && obj["id"] is JsonValue origId
                        && obj["object"] is JsonValue origTarget
                        && origId.GetValue<string>() is { Length: > 0 }:
                        if (origTarget.GetValue<string>().ToUpper() == ActorId.ToUpper())
                        {
                            await InboxService.DeleteFollower(origId.GetValue<string>());
                        }
                        break;
                }
            }
        }

        await InboxService.Create(item);

        return Created();
    }

    private bool TryRedirectHtml(object obj, out string redirect)
    {
        redirect = "";
        if (Request.Headers.Accept.ToString().ToLower().Contains("text/html"))
        {
            var polyUrl = obj is APubObject o
                ? o.Url
                : obj is APubLink l
                    ? l.Url
                    : obj is APubPolyBase { Count: 1 } p
                        ? (p[0].IsLink ? ((APubLink)p[0]).Url : p[0].IsObject ? ((APubObject)p[0]).Url : null)
                        : null;

            var target = polyUrl is { Count: 1 } && polyUrl[0].IsLink && ((APubLink)polyUrl[0]).Href is not null ? ((APubLink)polyUrl[0]).Href : null;
            if (target is not null)
            {
                redirect = target.PathAndQuery;
                return true;
            }
        }
        return false;
    }
}