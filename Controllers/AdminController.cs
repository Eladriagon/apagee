using System.Buffers.Text;
using System.Net;

namespace Apagee.Controllers;

[Authorize]
[Route("admin")]
public class AdminController(UserService userService,
                             ArticleService articleService,
                             InteractionService interactionService,
                             SettingsService settingsService,
                             InboxService inboxService,
                             IFileService fileService,
                             FedClient client,
                             IHttpClientFactory httpClientFactory)
    : BaseController
{
    public UserService UserService { get; } = userService;
    public ArticleService ArticleService { get; } = articleService;
    public InteractionService InteractionService { get; } = interactionService;
    public SettingsService SettingsService { get; } = settingsService;
    public InboxService InboxService { get; } = inboxService;
    public IFileService FileService { get; } = fileService;
    public FedClient Client { get; } = client;
    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

    public Settings? SiteSettings => SettingsService.Current;
    public GlobalConfiguration? SiteConfig => GlobalConfiguration.Current;

    [HttpGet]
    [Route("")]
    public async Task<IActionResult> Index()
    {
        var articleCount = await ArticleService.GetCount();
        var recentArticles = await ArticleService.GetRecent(10);
        var followerCount = await InboxService.GetFollowerCount();
        var articles = await ArticleService.GetRecent(10);
        var interactions = await InteractionService.GetAllInteractions();

        return View(new DashboardViewModel
        {
            PostCount = (int)articleCount,
            FollowerCount = followerCount,
            BoostCount = interactions.Count(i => i.Type == InteractionType.Announce),
            FavoriteCount = interactions.Count(i => i.Type == InteractionType.Like),
            RecentArticles = new ArticleListViewModel
            {
                AuthorUsername = GlobalConfiguration.Current!.FediverseUsername,
                AuthorDisplayName = GlobalConfiguration.Current!.AuthorDisplayName,
                FollowerCount = followerCount,
                Articles = articles
                    .GroupJoin(interactions, k1 => k1.Uid, k2 => k2.ArticleUID, (art, inter) => new ArticleViewModel
                    {
                        Article = art,
                        Likes = (uint)inter.Count(i => i.Type == InteractionType.Like),
                        Shares = (uint)inter.Count(i => i.Type == InteractionType.Announce),
                    })
            },
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
            newSettings.BannerImage = settings?.BannerImage;

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
            if (newSettings.BannerImageInput is not null)
            {
                var b64banner = await newSettings.BannerImageInput.ReadToBase64();
                if (b64banner is { Length: > 0 } && Utils.IsIco(b64banner) || Utils.IsPng(b64banner))
                {
                    newSettings.BannerImage = b64banner;
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
        var interactions = await InteractionService.GetAllInteractions();

        return View(new ArticleListViewModel
        {
            AuthorUsername = GlobalConfiguration.Current!.FediverseUsername,
            AuthorDisplayName = GlobalConfiguration.Current!.AuthorDisplayName,
            Articles = articles
                .GroupJoin(interactions, k1 => k1.Uid, k2 => k2.ArticleUID, (art, inter) => new ArticleViewModel
                {
                    Article = art,
                    Likes = (uint)inter.Count(i => i.Type == InteractionType.Like),
                    Shares = (uint)inter.Count(i => i.Type == InteractionType.Announce),
                })
        });
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
    public async Task<IActionResult> EditArticle(string id)
    {
        try
        {
            var article = await ArticleService.GetByUid(id);

            if (article is null)
            {
                return NotFound();
            }

            if (TempData["PreviewArticle"] is true)
            {
                ViewBag.PreviewArticle = true;
            }

            return View("ArticleEditor", new ArticleEditViewModel
            {
                Uid = article.Uid,
                Title = article.Title,
                Body = article.Body,
                Tags = string.Join(" ", article.Tags?.Select(t => "#" + t) ?? []),
                WasEverPublished = article.PublishedOn is not null
            });
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Error fetching article \"{id}\" to preview delete - {ex.Message}", ex);
        }
    }

    [HttpPost]
    [Route("articles/edit/{id}/parseTags")]
    public async Task<IActionResult> ParseTags([FromRoute] string id, [FromForm] string tags)
    {
        try
        {
            if (tags is not { Length: > 0 }) return Ok(Array.Empty<string>());
            return Ok(Utils.ExtractTags(tags));
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to parse tags from article {id} via input '{tags}': {ex.GetType().FullName}: {ex.Message}", ex);
        }
    }

    [HttpPost]
    [Route("articles/edit/{id}/addImage")]
    public async Task<IActionResult> UploadMedia([FromRoute] string id, [FromForm] IFormFile file, [FromForm] string mimeType)
    {
        var fileData = await file.ReadToBase64();
        var filePath = await FileService.SaveFile(fileData.AsBase64Bytes(), mimeType, id);

        return Ok(new
        {
            md = $"![alt text]({filePath})"
        });
    }

    [HttpPost]
    [Route("articles/media/proxyGet")]
    public async Task<IActionResult> ProxyGetMedia([FromForm] string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.Scheme.ToUpper().StartsWith("HTTP"))
            {
                throw new("Not an absolute URL or wrong protocol.");
            }

            var client = HttpClientFactory.CreateClient();

            // Hello, fellow humans, how do you do?
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36 Edg/142.0.0.0");

            var data = await client.GetByteArrayAsync(uri);

            if (data is not { Length: > 0 })
            {
                throw new("No content received.");
            }
            return Ok(new
            {
                file = data.AsBase64()
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to proxy media: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
            return Ok(new
            {
                error = true
            });
        }
    }

    [HttpPost]
    [Route("articles/save/{id?}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveArticle([FromForm] ArticleEditViewModel formArticle, [FromRoute] string? id = null, [FromQuery] bool? isDraft = false, [FromQuery] bool? preview = false)
    {
        try
        {
            Article? article;

            var hasAnyChanges = true;

            if (formArticle.Uid is not null)
            {
                article = await ArticleService.GetByUid(formArticle.Uid) ?? throw new ApageeException($"Edit article not found: {formArticle.Uid}");

                hasAnyChanges = article.Title != formArticle.Title || article.Body != formArticle.Body;

                article.Title = formArticle.Title;
                article.Body = formArticle.Body;
            }
            else
            {
                article = new()
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
                return StatusCode(StatusCodes.Status400BadRequest, new { error = "Invalid article ID for editing." });
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

            var sendToFollowers = article.PublishedOn is null && isDraft is not true;

            var sendUpdate = hasAnyChanges
                          && article.PublishedOn is not null
                          && isDraft is not true
                          && article.PublishedOn < DateTime.UtcNow;

            // Compute extra properties
            article.Slug = article.Title.ToUrlSlug();
            article.BodyMode = BodyMode.Markdown;
            article.Status = isDraft is true ? ArticleStatus.Draft : ArticleStatus.Published;
            article.PublishedOn = isDraft is true ? null : article.PublishedOn ?? DateTime.UtcNow;

            // Set tags parsed from input field
            article.Tags = formArticle.Tags?.Trim() is { Length: > 0 } t
                ? Utils.ExtractTags(t).ToList()
                : null;

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

            if (sendToFollowers)
            {
                await PublishToFediverse(article);
            }
            else if (sendUpdate)
            {
                await PublishUpdateToFediverse(article);
            }

            if (article.PublishedOn is null && isDraft is true && preview is true)
            {
                TempData["PreviewArticle"] = true;
            }

            // Published? Go back to the list.
            if (sendToFollowers || sendUpdate)
            {
                return RedirectToAction("Articles");
            }

            // Otherwise, keep showing the editor.
            return RedirectToAction("EditArticle", new { id = article.Uid });
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Error while saving article '{id}' - {ex.GetType().FullName}: {ex.Message}", ex);
        }
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

    [HttpGet]
    [Route("redeliver")]
    public async Task<IActionResult> RedeliverArticle(string id)
    {
        try
        {
            var article = await ArticleService.GetByUid(id);

            if (article is { PublishedOn: not null, Status: ArticleStatus.Published })
            {
                await PublishToFediverse(article);
            }

        return RedirectToAction("Articles");
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Error redelivering article \"{id}\" - {ex.Message}", ex);
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

    [HttpGet]
    [Route("SignOut")]
    public async Task<IActionResult> SignMeOut()
    {
        try
        {
            await HttpContext.SignOutAsync();
            HttpContext.Session.Clear();
        }
        catch
        {

        }

        // Should bounce to the login screen as a sanity check / confirmation of logout.
        return Redirect("/admin");
    }

    private async Task PublishToFediverse(Article article)
    {
        // TODO: Extremely very super not performant.
        // Involves adding a background worker/thread
        // so this is here as a test/proof of concept
        // only.
        var followers = await InboxService.GetFollowerList();
        var actArticle = APubStatus.FromArticle(article);
        foreach (var f in followers)
        {
            await Client.PostInboxFromActor(f, new Create()
            {
                Id = $"https://{SiteConfig?.PublicHostname}/api/users/{SiteConfig?.FediverseUsername}/statuses/{article.Uid}/activity",
                Actor = $"https://{SiteConfig?.PublicHostname}/api/users/{SiteConfig?.FediverseUsername}",
                Published = article.PublishedOn,
                To = actArticle.To,
                Cc = actArticle.Cc,
                Object = actArticle
            });
        }
    }

    private async Task PublishUpdateToFediverse(Article article)
    {
        // TODO: Extremely very super not performant.
        // Involves adding a background worker/thread
        // so this is here as a test/proof of concept
        // only.
        var updateTime = DateTime.UtcNow;
        var updateTs = updateTime.ToUnixTimestamp();
        var followers = await InboxService.GetFollowerList();
        var actArticle = APubStatus.FromArticle(article);

        foreach (var f in followers)
        {
            await Client.PostInboxFromActor(f, new Update()
            {
                Id = $"{actArticle.Id}#update/{updateTs}",
                Actor = $"https://{SiteConfig?.PublicHostname}/api/users/{SiteConfig?.FediverseUsername}",
                Updated = updateTime,
                To = actArticle.To,
                Cc = actArticle.Cc,
                Object = actArticle
            });
        }
    }
}