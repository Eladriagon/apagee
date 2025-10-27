namespace Apagee.Controllers;

[Route("")]
public class SystemController(UserService userService, SettingsService settingsService, ArticleService articleService, GlobalConfiguration config) 
    : BaseController
{
    public UserService UserService { get; } = userService;
    public SettingsService SettingsService { get; } = settingsService;
    public ArticleService ArticleService { get; } = articleService;
    public GlobalConfiguration Config { get; } = config;

    [Route("")]
    public async Task<IActionResult> Index()
    {
        if (Request.Headers.Accept.ToString().ToLower().Contains(Globals.JSON_ACT_CONTENT_TYPE)
            || Request.Headers.Accept.ToString().ToLower().Contains(Globals.JSON_LD_CONTENT_TYPE_TRIM))
        {
            return Redirect($"/api/users/{GlobalConfiguration.Current!.FediverseUsername}");
        }
        if (Request.Headers.Accept.ToString().ToLower().Contains(Globals.JSON_RD_CONTENT_TYPE))
        {
            return Redirect($"/.well-known/webfinger?resource=acct:{GlobalConfiguration.Current!.FediverseUsername}@{GlobalConfiguration.Current!.PublicHostname}");
        }

        var articles = await ArticleService.GetOlderThan(count: 10);

        return View("Public/ListView", new ArticleListViewModel
        {
            Articles = articles.OrderByDescending(a => a.PublishedOn).Select(a => new ArticleViewModel { Article = a }),
            AuthorUsername = Config.FediverseUsername,
            AuthorDisplayName = Config.AuthorDisplayName,
            AuthorBio = Config.FediverseBio,
            SiteSettings = SettingsService.Current,
            ThemeCss = SettingsService.Current?.ThemeCss
        });
    }

    [Route("@{user}")]
    public IActionResult ViewAuthor([FromRoute] string user)
    {
        if (user != Config.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        if (Request.Headers.Accept.ToString().ToLower().Contains(Globals.JSON_LD_CONTENT_TYPE)
            || Request.Headers.Accept.ToString().ToLower().Contains(Globals.JSON_ACT_CONTENT_TYPE))
        {
            return Redirect($"/api/users/{user}");
        }

        return View("Public/AuthorView", new ArticleListViewModel
        {
            Articles = [],
            AuthorUsername = Config.FediverseUsername,
            AuthorBio = Config.FediverseBio,
            AuthorDisplayName = Config.AuthorDisplayName
        });
    }
    
    [Route("404")]
    public IActionResult PageNotFound() => Content("Apagee -- 404 Not Found");

    [Route("403")]
    public IActionResult AccessDenied() => Content("Apagee -- 403 Access Denied");

    [HttpGet]
    [Route("/admin/login")]
    public IActionResult LoginView()
    {
        if (User.Identity is {IsAuthenticated: true })
        {
            return RedirectToAction("Index", "Admin");
        }

        if (TempData["LoginFail"] is true)
        {
            ViewBag.LoginFailed = true;
        }
        if (TempData["LoginErr"] is true)
        {
            ViewBag.LoginError = true;
        }
        return View("Login");
    }

    [HttpPost]
    [Route("/admin/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
    {
        try
        {
            if (await UserService.CheckUserLogin(username, password))
            {
                var user = await UserService.GetUser() ?? throw new ApageeException("Unable to find user in database (check connection).");

                await SignIn(user.Username);

                user.LastLogin = DateTime.UtcNow;
                await UserService.UpsertUser(user);

                return RedirectToAction("Index", "Admin");
            }

            TempData["LoginFail"] = true;
            return RedirectToAction(nameof(LoginView));
        }
        catch (Exception ex)
        {
            LogUiError(ex);
            TempData["LoginErr"] = true;
            return RedirectToAction(nameof(LoginView));
        }
    }
    
    // Image handling

    [HttpGet]
    [Route("/favicon.ico")]
    public async Task<IActionResult> GetFavicon()
    {
        var settings = SettingsService.Current;

        if (settings is null)
        {
            return NotFound();
        }

        var b64 = settings.Favicon;
        if (string.IsNullOrWhiteSpace(b64))
            return NotFound();

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(b64);
        }
        catch
        {
            return NotFound(); // invalid base64
        }

        // ICO header check:
        // ICO files start with: 00 00 01 00 (little-endian reserved/type fields)
        if (Utils.IsIco(b64))
        {
            // Serve it as an actual favicon
            return File(bytes, "image/x-icon");
        }
        else if (Utils.IsPng(b64))
        {
            return File(bytes, "image/png");
        }
        else
        {
            return NotFound(); // not a valid ICO/PNG file
        }

    }

    [HttpGet]
    [Route("/avatar.png")]
    public IActionResult GetAvatar()
    {
        var settings = SettingsService.Current;

        if (settings is null)
        {
            return NotFound();
        }

        var b64 = settings.AuthorAvatar;
        if (string.IsNullOrWhiteSpace(b64))
            return NotFound();

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(b64);
        }
        catch
        {
            return NotFound(); // invalid Base64
        }

        // --- PNG check ---
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            Response.Headers.CacheControl = "public, max-age=86400";
            return File(bytes, "image/png");
        }

        // --- JPEG check ---
        if (bytes.Length >= 4 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 &&
            bytes[^2] == 0xFF && bytes[^1] == 0xD9)
        {
            Response.Headers.CacheControl = "public, max-age=86400";
            return File(bytes, "image/jpeg");
        }

        return NotFound(); // unsupported or invalid image
    }

    [HttpGet]
    [Route("/banner.png")]
    public IActionResult GetBanner()
    {
        var settings = SettingsService.Current;

        if (settings is null)
        {
            return NotFound();
        }

        var b64 = settings.AuthorAvatar;
        if (string.IsNullOrWhiteSpace(b64))
            return NotFound();

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(b64);
        }
        catch
        {
            return NotFound(); // invalid Base64
        }

        // --- PNG check ---
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            Response.Headers.CacheControl = "public, max-age=86400";
            return File(bytes, "image/png");
        }

        // --- JPEG check ---
        if (bytes.Length >= 4 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 &&
            bytes[^2] == 0xFF && bytes[^1] == 0xD9)
        {
            Response.Headers.CacheControl = "public, max-age=86400";
            return File(bytes, "image/jpeg");
        }

        return NotFound(); // unsupported or invalid image
    }

    private async Task SignIn(string username)
    {
        var id = new ClaimsIdentity([
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "admin"),
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        var authSettings = new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = true
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                      new ClaimsPrincipal(id),
                                      authSettings);
    }
}