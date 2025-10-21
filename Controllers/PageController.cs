namespace Apagee.Controllers;

public class PageController : BaseController
{
    public IActionResult Get([FromRoute] string slug) => Content("Placeholder for a page.");
}