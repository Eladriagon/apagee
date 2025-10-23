using System.Threading.Tasks;

namespace Apagee.Controllers;

[Authorize]
[Route("admin")]
public class AdminController(UserService userService, ArticleService articleService, SettingsService settingsService)
    : BaseController
{
    public UserService UserService { get; } = userService;
    public ArticleService ArticleService { get; } = articleService;
    public SettingsService SettingsService { get; } = settingsService;

    [HttpGet]
    [Route("")]
    public async Task<IActionResult> Index()
    {
        var articleCount = await ArticleService.GetCount();
        var recentArticles = await ArticleService.GetRecent(10);

        return View(new DashboardViewModel
        {
            PostCount = articleCount,
            RecentArticles = recentArticles
        });
    }

    [HttpGet]
    [Route("config")]
    public IActionResult Config() => View();

    [HttpGet]
    [Route("settings")]
    public async Task<IActionResult> Settings()
    {
        await SettingsService.RefreshSettings();

        ViewBag.SettingsSuccess = TempData["SettingsSuccess"];

        return View(SettingsService.Current);
    }

    [HttpPost]
    [Route("settings")]
    public async Task<IActionResult> Settings([FromForm] Settings newSettings)
    {
        try
        {
            await SettingsService.UpsertSettings(newSettings);

            TempData["SettingsSuccess"] = true;

            return RedirectToAction("Settings");
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Error updating settings - {ex.Message}", ex);
        }
    }

    [HttpGet]
    [Route("articles")]
    public async Task<IActionResult> Articles()
    {
        ViewBag.ArticleSuccess = TempData["ArticleSuccess"];
        ViewBag.DelSuccess = TempData["DelSuccess"];
        var articles = await ArticleService.GetAll();
        return View(articles);
    }

    [HttpGet]
    [Route("articles/new")]
    public IActionResult NewArticle()
    {
        ViewBag.ArticleError = TempData["ArticleError"];
        ViewBag.ArticleSubError = TempData["ArticleSubError"];
        return View();
    }

    [HttpPost]
    [Route("articles/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewArticle([FromForm] string title, [FromForm] string article, [FromQuery] bool isDraft = false)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ArticleError"] = "Forgetting something?";
            TempData["ArticleSubError"] = "Title is missing.";
            return RedirectToAction("NewArticle");
        }

        if (string.IsNullOrWhiteSpace(article))
        {
            TempData["ArticleError"] = "Forgetting something?";
            TempData["ArticleSubError"] = "Article body is missing.";
            return RedirectToAction("NewArticle");
        }

        var slug = title.ToUrlSlug();

        await ArticleService.Create(new Article
        {
            Uid = Ulid.NewUlid().ToString(),
            Title = title.Trim(),
            Slug = slug,
            Body = article.Trim(),
            BodyMode = BodyMode.Markdown,
            Status = isDraft ? ArticleStatus.Draft : ArticleStatus.Published,
            CreatedOn = DateTime.UtcNow
        });

        TempData["ArticleSuccess"] = true;

        return RedirectToAction("Articles");
    }

    [HttpGet]
    [Route("delete")]
    public async Task<IActionResult> DeleteArticle(string id)
    {
        try
        {
            var article = await ArticleService.GetByUid(id);

            return View(article);
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Error fetching article \"{id}\" to preview delete - {ex.Message}", ex);
        }
    }

    [HttpPost]
    [Route("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var article = await ArticleService.GetByUid(id);
            if (article is not null)
            {
                await ArticleService.Delete(article);
            }
            TempData["DelSuccess"] = true;

            return RedirectToAction("Articles");
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Error fetching article \"{id}\" to preview delete - {ex.Message}", ex);
        }
    }

    [HttpGet]
    [Route("updatePass")]
    public IActionResult UpdatePass()
    {
        ViewBag.FormError = TempData["PassError"];
        ViewBag.Success = TempData["PassSuccess"];
        return View();
    }

    [HttpPost]
    [Route("updatePass")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePass([FromForm] string oldPass, [FromForm] string newPass, [FromForm] string newPassAgain)
    {
        void SetError(string text) => TempData["PassError"] ??= text;

        if (oldPass is null or { Length: 0 })
        {
            SetError("Old password must be provided.");
            return RedirectToAction();
        }

        if (newPass is null or { Length: 0 } || newPassAgain is null or { Length: 0 } || newPass != newPassAgain)
        {
            SetError("New passwords don't match or were omitted.");
            return RedirectToAction();
        }

        if (newPass is not { Length: >= 8 })
        {
            SetError("New password must be at least 8 characters.");
            return RedirectToAction();
        }

        if (!await UserService.CheckUserLogin("admin", oldPass))
        {
            SetError("Current password was incorrect, try again.");
            return RedirectToAction();
        }

        var user = await UserService.GetUser();

        if (user is null)
        {
            SetError("Could not fetch user from database.");
            return RedirectToAction();
        }

        await UserService.UpsertUser(user, newPass);

        TempData["PassSuccess"] = true;
        return RedirectToAction();
    }
}