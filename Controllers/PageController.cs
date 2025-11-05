namespace Apagee.Controllers;

public class PageController(ArticleService articleService, InteractionService interactionService, InboxService inboxService, GlobalConfiguration config, SettingsService settingsService)
    : BaseController
{
    public ArticleService ArticleService { get; } = articleService;
    public InteractionService InteractionService { get; } = interactionService;
    public InboxService InboxService { get; } = inboxService;
    public GlobalConfiguration Config { get; } = config;
    public SettingsService SettingsService { get; } = settingsService;

    public async Task<IActionResult> Get([FromRoute] string slug)
    {
        try
        {
            var article = await ArticleService.GetBySlug(slug);

            if (article is null || article.Status == ArticleStatus.Draft)
            {
                return NotFound("Sorry, can't find that!");
            }

            if (Request.Headers.Accept.ToString().ToLower().Contains(Globals.JSON_LD_CONTENT_TYPE_TRIM)
                || Request.Headers.Accept.ToString().ToLower().Contains(Globals.JSON_ACT_CONTENT_TYPE))
            {
                return Redirect($"/api/users/{GlobalConfiguration.Current!.FediverseUsername}/statuses/{article.Uid}");
            }

            uint? fcount = null;
            if (SettingsService.Current?.DisplayFollowerCount is true)
            {
                fcount = await InboxService.GetFollowerCount();
            }

            var interactions = await InteractionService.GetInteractionCounts(article);

            return View("Public/ArticleView", new ArticleListViewModel
            {
                Articles = [new ArticleViewModel
                {
                    Article = article,
                    Likes = interactions[InteractionType.Like],
                    Shares = interactions[InteractionType.Announce]
                }],
                AuthorUsername = Config.FediverseUsername,
                AuthorDisplayName = Config.AuthorDisplayName,
                AuthorBio = Config.FediverseBio,
                FollowerCount = fcount,
                SiteSettings = SettingsService.Current,
                ThemeCss = SettingsService.Current?.ThemeCss
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "Oops... something went wrong on our end.");
        }
    }

    [HttpGet]
    [Authorize]
    [Route("/preview/{id}")]
    public async Task<IActionResult> Preview([FromRoute] string id)
    {
        try
        {
            var article = await ArticleService.GetByUid(id);

            if (article is null)
            {
                return NotFound("Sorry, can't find that!");
            }

            if (article.Status is ArticleStatus.Published)
            {
                return Redirect($"/{article.Slug}");
            }

            uint? fcount = null;
            if (SettingsService.Current?.DisplayFollowerCount is true)
            {
                fcount = await InboxService.GetFollowerCount();
            }

            ViewBag.PreviewMode = true;

            return View("Public/ArticleView", new ArticleListViewModel
            {
                Articles = [new ArticleViewModel
                {
                    Article = article,
                    Likes = 0,
                    Shares = 0
                }],
                AuthorUsername = Config.FediverseUsername,
                AuthorDisplayName = Config.AuthorDisplayName,
                AuthorBio = Config.FediverseBio,
                FollowerCount = fcount,
                SiteSettings = SettingsService.Current,
                ThemeCss = SettingsService.Current?.ThemeCss
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Oops... something went wrong on our end. ({ex.GetType().FullName}: {ex.Message})");
        }
    }

}