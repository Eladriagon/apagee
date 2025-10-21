namespace Apagee.Controllers;

[Authorize]
[Route("admin")]
public class AdminController(UserService userService)
    : BaseController
{
    public UserService UserService { get; } = userService;

    [HttpGet]
    [Route("")]
    public IActionResult Index() => View();

    [HttpGet]
    [Route("config")]
    public IActionResult Config() => View();

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