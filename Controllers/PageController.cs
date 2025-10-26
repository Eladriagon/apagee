namespace Apagee.Controllers;

public class PageController(ArticleService articleService, GlobalConfiguration config, SettingsService settingsService)
    : BaseController
{
    public ArticleService ArticleService { get; } = articleService;
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

            return View("Public/ArticleView", new ArticleViewModel
            {
                Article = article,
                AuthorUsername = Config.FediverseUsername,
                AuthorDisplayName = Config.AuthorDisplayName,
                AuthorBio = Config.ShowBioOnArticles ? Config.FediverseBio : null,
                SiteSettings = SettingsService.Current,
                BodyHtml = article.BodyMode switch
                {
                    BodyMode.Markdown => Markdig.Markdown.ToHtml(article.Body ?? ""),
                    BodyMode.HTML => article.Body ?? "",
                    BodyMode.PlainText => article.Body?.Replace("\r", "").Replace("\n", "<br />") ?? "",
                    _ => "oops, i think a stray neutron caused a bit flip..."
                },
                ThemeCss = SettingsService.Current?.ThemeCss
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "Oops... something went wrong on our end.");
        }
    }
}