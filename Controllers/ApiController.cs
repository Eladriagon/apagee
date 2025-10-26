using System.Xml;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Hosting.Internal;

namespace Apagee.Controllers;

[ApiController]
public class ApiController(ArticleService articleService, KeypairHelper keypairHelper, IMemoryCache cache)
    : BaseController
{
    public ArticleService ArticleService { get; } = articleService;
    public KeypairHelper KeypairHelper { get; } = keypairHelper;
    public IMemoryCache Cache { get; } = cache;

    private string CurrentPath => $"https://{GlobalConfiguration.Current?.PublicHostname}{Request.Path}";
    private string AtomId => CurrentPath + Request.QueryString;
    private APubActor CurrentActor => APubActor.Create<Person>(KeypairHelper.KeyId, KeypairHelper.ActorPublicKeyPem ?? throw new ApageeException("Cannot create user: Actor public key PEM is null."));

    [HttpGet]
    [Route(".well-known/webfinger")]
    public IActionResult Webfinger([FromQuery] string resource)
    {
        if (GlobalConfiguration.Current is not { FediverseUsername.Length: > 0, PublicHostname.Length: > 0 })
        {
            return StatusCode(500, "Server has not been set up yet.");
        }

        var cfg = GlobalConfiguration.Current!;

        // Single-user only, otherwise we'd have to parse the acct: subject.
        var acct = WebfingerAccount.Create(cfg.PublicHostname, cfg.FediverseUsername);

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
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        var user = CurrentActor;

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

        return Ok(act);
    }

    [HttpGet]
    [Route("api/users/{username}/followers")]
    public IActionResult GetFollowers([FromRoute] string username, [FromQuery] int? after = null)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        if (after is null)
        {
            var oc = new APubOrderedCollection
            {
                Id = Request.GetDisplayUrl() + Request.QueryString,
                TotalItems = 0,
                First = Request.GetDisplayUrl() + "?after=0"
            };
            return Ok(oc);
        }
        else
        {
            var ocp = new APubOrderedCollectionPage
            {
                Id = Request.GetDisplayUrl() + Request.QueryString,
                TotalItems = 0,
                PartOf = new APubLink(Request.GetDisplayUrl()),
                OrderedItems = new APubOrderedCollection()
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
            BlobData = body,
            Type = "",
            ReceivedOn = DateTime.UtcNow,
            ContentType = Request.ContentType?.ToString() ?? "no/type",
            RemoteServer = Request.Headers["X-Forwarded-For"].ToString() ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        try
        {
            var json = await JsonNode.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(body)));

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
        
        if (Request.HasJsonContentType() || Request.ContentType?.ToLower() == Globals.JSON_LD_CONTENT_TYPE)
        {
            // TBD
        }
        return Ok();
    }
}