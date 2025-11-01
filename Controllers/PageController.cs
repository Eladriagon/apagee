namespace Apagee.Controllers;

public class PageController(ArticleService articleService, InboxService inboxService, GlobalConfiguration config, SettingsService settingsService)
    : BaseController
{
    public ArticleService ArticleService { get; } = articleService;
    public InboxService InboxService { get; } = inboxService;
    public GlobalConfiguration Config { get; } = config;
    public SettingsService SettingsService { get; } = settingsService;

    public async Task<IActionResult> Get([FromRoute] string slug)
    {
        try
        {
            var article = await ArticleService.GetBySlug(slug);

            if (article is null || (article.Status == ArticleStatus.Draft && (!User.Identity?.IsAuthenticated ?? false)))
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
        
            return View("Public/ArticleView", new ArticleListViewModel
            {
                Articles = [new ArticleViewModel { Article = article }],
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
}