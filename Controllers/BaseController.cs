namespace Apagee.Controllers;

public class BaseController : Controller
{
    protected void LogUiError(Exception ex)
    {
        Output.WriteLine($"{Output.Ansi.Red} # UI error on {HttpContext.Request.Path}: {ex.GetType().FullName}: {ex.Message}");
        if (Globals.IsVerboseDevelopment)
        {
            Output.WriteLine($"{Output.Ansi.Dim}{Output.Ansi.Red}{ex.StackTrace}");
        }
    }
}