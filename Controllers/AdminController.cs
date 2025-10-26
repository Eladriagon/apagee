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
            PostCount = (int)articleCount,
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
            var settings = SettingsService.Current;

            // Copy some non-persistent settings over
            newSettings.Favicon = settings?.Favicon;
            newSettings.AuthorAvatar = settings?.AuthorAvatar;

            if (newSettings.FaviconInput is not null)
            {
                var b64favicon = await newSettings.FaviconInput.ReadToBase64();
                if (b64favicon is { Length: > 0 } && Utils.IsIco(b64favicon) || Utils.IsPng(b64favicon))
                {
                    newSettings.Favicon = b64favicon;
                }
            }
            if (newSettings.AuthorAvatarInput is not null)
            {
                var b64avatar = await newSettings.AuthorAvatarInput.ReadToBase64();
                if (b64avatar is { Length: > 0 } && Utils.IsJpeg(b64avatar) || Utils.IsPng(b64avatar))
                {
                    newSettings.AuthorAvatar = b64avatar;
                }
            }

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
        ViewBag.NewSuccess = TempData["NewSuccess"];
        ViewBag.EditSuccess = TempData["EditSuccess"];
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
        return View("ArticleEditor");
    }

    [HttpGet]
    [Route("articles/edit/{id}")]
    public async Task<IActionResult> EditArticle(string id, [FromQuery] bool isDraft = false)
    {
        try
        {
            var article = await ArticleService.GetByUid(id);

            if (article is null)
            {
                return NotFound();
            }

            return View("ArticleEditor", new ArticleEditViewModel
            {
                Uid = article.Uid,
                Title = article.Title,
                Body = article.Body,
                WasEverPublished = article.PublishedOn is not null
            });
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Error fetching article \"{id}\" to preview delete - {ex.Message}", ex);
        }
    }

    [HttpPost]
    [Route("articles/save/{id?}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveArticle([FromForm] ArticleEditViewModel formArticle, [FromRoute] string? id = null, [FromQuery] bool isDraft = false)
    {
        Article? article;

        if (formArticle.Uid is not null)
        {
            article = await ArticleService.GetByUid(formArticle.Uid) ?? throw new ApageeException($"Edit article not found: {formArticle.Uid}");
            article.Title = formArticle.Title;
            article.Body = formArticle.Body;
        }
        else
        {
            article = (Article?)new()
            {
                Uid = Ulid.NewUlid().ToString(),
                Title = formArticle.Title,
                Slug = "",
                Body = formArticle.Body,
                CreatedOn = DateTime.UtcNow
            };
        }

        // If it's null here, it's because it wasn't found
        // Also check the IDs match
        if (article is null || (id is not null && formArticle.Uid != id))
        {
            return BadRequest("Invalid article ID for editing.");
        }

        if (string.IsNullOrWhiteSpace(article.Title))
        {
            TempData["ArticleError"] = "Forgetting something?";
            TempData["ArticleSubError"] = "Title is missing.";
            return View("ArticleEditor");
        }

        if (string.IsNullOrWhiteSpace(article.Body))
        {
            TempData["ArticleError"] = "Forgetting something?";
            TempData["ArticleSubError"] = "Article body is missing.";
            return View("ArticleEditor");
        }

        // Compute extra properties
        article.Slug = article.Title.ToUrlSlug();
        article.BodyMode = BodyMode.Markdown;
        article.Status = isDraft ? ArticleStatus.Draft : ArticleStatus.Published;
        article.PublishedOn = isDraft ? null : DateTime.UtcNow;

        if (id is null)
        {
            await ArticleService.Create(article);
            TempData["NewSuccess"] = true;
        }
        else
        {
            await ArticleService.Update(article);
            TempData["EditSuccess"] = true;
        }
        
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