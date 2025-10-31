namespace Apagee.Controllers;

[ApiController]
public partial class ApiController(ArticleService articleService, KeypairHelper keypairHelper, IMemoryCache cache, InboxService inboxService, FedClient client, KeyValueService kvService, JsonSerializerOptions opts)
    : BaseController
{
    public ArticleService ArticleService { get; } = articleService;
    public KeypairHelper KeypairHelper { get; } = keypairHelper;
    public IMemoryCache Cache { get; } = cache;
    public InboxService InboxService { get; } = inboxService;
    public FedClient Client { get; } = client;
    public KeyValueService KvService { get; } = kvService;
    public JsonSerializerOptions Opts { get; } = opts;

    private string NewActivityId => $"{RootUrl}/{Ulid.NewUlid()}";
    private string RootUrl => $"https://{GlobalConfiguration.Current?.PublicHostname}";
    private string ActorId => $"{RootUrl}/api/users/{GlobalConfiguration.Current!.FediverseUsername}";
    private string CurrentPath => $"{RootUrl}{Request.Path}";
    private string AtomId => CurrentPath + Request.QueryString;
    private APubActor CurrentActor => APubActor.Create<Person>(KeypairHelper.KeyId, KeypairHelper.ActorPublicKeyPem ?? throw new ApageeException("Cannot create user: Actor public key PEM is null."));

    [HttpGet]
    [Route("actor")]
    public async Task<IActionResult> SiteActor()
    {
        var appActor = APubActor.CreateApplication<Application>(KeypairHelper.SiteKeyId, KeypairHelper.SiteActorPublicKeyPem ?? throw new ApageeException("Site actor has no public key."));
        appActor.Published = await KvService.Get(Globals.APP_START_DATE_KEY) is string s ? DateTime.Parse(s) : null;
        return Ok(appActor);
    }

    [HttpGet]
    [Route("api/users/{username}")]
    public async Task<IActionResult> GetUser([FromRoute] string username)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var user = CurrentActor;

        user.Published = await KvService.Get(Globals.APP_START_DATE_KEY) is string s ? DateTime.Parse(s) : null;

        if (SettingsService.Current?.Property1Key is { Length: > 0 } k1
            && SettingsService.Current?.Property1Value is { Length: > 0 } v1)
        {
            user.Attachment!.Add(new APubPropertyValue(k1, v1));
        }

        if (SettingsService.Current?.Property2Key is { Length: > 0 } k2
            && SettingsService.Current?.Property2Value is { Length: > 0 } v2)
        {
            user.Attachment!.Add(new APubPropertyValue(k2, v2));
        }

        if (SettingsService.Current?.Property3Key is { Length: > 0 } k3
            && SettingsService.Current?.Property3Value is { Length: > 0 } v3)
        {
            user.Attachment!.Add(new APubPropertyValue(k3, v3));
        }

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

        var act = APubActivity.Wrap<Create>(aPubArticle, CurrentActor.Id, published: article.PublishedOn);
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

        var act = APubActivity.Wrap<Create>(aPubArticle, CurrentActor.Id, published: article.PublishedOn);
        act.Published = article.PublishedOn;

        if (TryRedirectHtml(aPubArticle, out var link))
        {
            return Redirect(link);
        }

        return Ok(act);
    }

    [HttpGet]
    [Route("api/users/{username}/followers")]
    public async Task<IActionResult> GetFollowers([FromRoute] string username, [FromQuery] string? domain = null, [FromQuery] string? after = null)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var total = await InboxService.GetFollowerCount(domain);

        if (domain is not null)
        {
            var domainFollowers = await InboxService.GetFollowerList(after, domain, 1000);
            var oc = new APubOrderedCollection
            {
                Id = $"{ActorId}/followers?domain={domain}",
                TotalItems = total,
                OrderedItems = domainFollowers.ToArray()
            };
            return Ok(oc);
        }
        else
        {
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
                var ocp = new APubOrderedCollectionPage
                {
                    Id = $"{ActorId}/followers?after={after}",
                    TotalItems = total,
                    PartOf = new APubLink($"{ActorId}/followers"),
                    OrderedItems = followerList.ToArray()
                };
                return Ok(ocp);
            }
        }
    }

    [HttpGet]
    [Route("api/users/{username}/following")]
    public async Task<IActionResult> GetFollowingAsync([FromRoute] string username, [FromQuery] string? after = null)
    {
        // Currently, we don't track actors we're following, but we can assume
        // that if we reciprocate followers, that our follower and followed
        // lists would be identical.

        if (SettingsService.Current!.AutoReciprocateFollows is not true)
        {
            return Ok(new APubOrderedCollection
            {
                Id = AtomId,
                TotalItems = 0
            });
        }
        else
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
                    Id = $"{ActorId}/following",
                    TotalItems = total,
                    First = $"{ActorId}/following?after={Ulid.MaxValue}"
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
                    Id = $"{ActorId}/following?after={after}",
                    TotalItems = total,
                    PartOf = new APubLink($"{ActorId}/following"),
                    OrderedItems = followerIds
                };
                return Ok(ocp);
            }
        }
    }

    [HttpGet]
    [Route("api/users/{username}/collections/featured")]
    public IActionResult GetFeatured([FromRoute] string username)
    {
        return Ok(new APubOrderedCollection
        {
            Id = AtomId,
            TotalItems = 0
        });
    }

    [HttpGet]
    [Route("api/users/{username}/collections/tags")]
    public IActionResult GetTags([FromRoute] string username)
    {
        return Ok(new APubOrderedCollection
        {
            Id = AtomId,
            TotalItems = 0
        });
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