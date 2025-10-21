using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Apagee.Controllers;

[Route("")]
public class SystemController(UserService userService, GlobalConfiguration config) 
    : BaseController
{
    public UserService UserService { get; } = userService;
    public GlobalConfiguration Config { get; } = config;

    [Route("")]
    public IActionResult Index() => Content("Hello Apagee!");

    [Route("@{user}")]
    public IActionResult ViewAuthor([FromRoute] string user)
    {
        if (user != Config.FediverseUsername)
        {
            return NotFound("User not found.");
        }

        return View("Author");
    }

    [Route("404")]
    public IActionResult PageNotFound() => Content("Apagee -- 404 Not Found");

    [Route("403")]
    public IActionResult AccessDenied() => Content("Apagee -- 403 Access Denied");

    [Route("/admin/login")]
    public IActionResult LoginView()
    {
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