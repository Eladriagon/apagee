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
    [Route("api/users/{username}/statuses/{id}/shares")]
    [Route("api/users/{username}/articles/{id}/likes")]
    [Route("api/users/{username}/articles/{id}/shares")]
    public async Task<IActionResult> GetLikesSharesPlacehoolder([FromRoute] string username, [FromRoute] string id)
    {
        return Ok(new APubOrderedCollection
        {
            Id = CurrentPathAndQuery,
            TotalItems = 0
        });
    }
}