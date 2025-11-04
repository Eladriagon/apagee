using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Apagee.Controllers;

public partial class ApiController : BaseController
{
    [HttpGet]
    [Route("api/users/{username}/statuses/{id}")]
    public async Task<IActionResult> GetStatusAsync([FromRoute] string username, [FromRoute] string id)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound404(message: "User not found.");
        }

        var article = await ArticleService.GetByUid(id);

        if (article is not { Status: ArticleStatus.Published })
        {
            return NotFound404(message: "Status not found.");
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
            return NotFound404(message: "User not found.");
        }

        var article = await ArticleService.GetByUid(id);

        if (article is not { Status: ArticleStatus.Published })
        {
            return NotFound404(message: "Activity not found.");
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
            return NotFound404(message: "User not found.");
        }

        var article = await ArticleService.GetByUid(id);

        if (article is not { Status: ArticleStatus.Published })
        {
            return NotFound404(message: "Article not found.");
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
            return NotFound404(message: "User not found.");
        }

        var article = await ArticleService.GetByUid(id);

        if (article is not { Status: ArticleStatus.Published })
        {
            return NotFound404(message: "Activity not found.");
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
    [Route("api/users/{username}/statuses/{id}/likes")]
    [Route("api/users/{username}/articles/{id}/likes")]
    public async Task<IActionResult> GetLikesCollection([FromRoute] string username, [FromRoute] string id)
    {
        return Ok(new APubCollection
        {
            Id = CurrentPath,
            TotalItems = await InteractionService.GetInteractionCount(id, InteractionType.Like)
        });
    }
    
    [HttpGet]
    [Route("api/users/{username}/statuses/{id}/shares")]
    [Route("api/users/{username}/articles/{id}/shares")]
    public async Task<IActionResult> GetSharesCollection([FromRoute] string username, [FromRoute] string id)
    {
        return Ok(new APubCollection
        {
            Id = CurrentPath,
            TotalItems = await InteractionService.GetInteractionCount(id, InteractionType.Announce)
        });
    }
    
    [HttpGet]
    [Route("api/users/{username}/statuses/{id}/replies")]
    [Route("api/users/{username}/articles/{id}/replies")]
    public async Task<IActionResult> GetReplies([FromRoute] string username, [FromRoute] string id, [FromQuery] string? olderThan = null)
    {
        if (olderThan is not { Length: > 0 })
        {
            return Ok(new APubCollection
            {
                Id = CurrentPath,
                First = new APubCollectionPage
                {
                    PartOf = new APubLink(CurrentPath),
                    Next = new APubLink($"{CurrentPath}?olderThan={Ulid.MaxValue}"),
                    Items = []
                }
            });
        }
        else
        {
            // "Page 1"
            return Ok(new APubCollectionPage
            {
                Id = CurrentAtomId,
                PartOf = new APubLink($"{CurrentPath}"),
                Items = []
            });
        }
    }
}