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

    protected IActionResult BadRequest400(object? content = null, string? message = null)
    {
        return StatusCode(StatusCodes.Status400BadRequest, content ?? new { error = message });
    }
    
    protected IActionResult NotFound404(object? content = null, string? message = null)
    {
        return StatusCode(StatusCodes.Status404NotFound, content ?? new { error = message });
    }
    
    protected IActionResult ServerError500(object? content = null, string? message = null)
    {
        return StatusCode(StatusCodes.Status500InternalServerError, content ?? new { error = message });
    }
}