using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Apagee.Utilities;

[HtmlTargetElement("a", Attributes = "asp-action")]
public class ActiveRouteTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    [HtmlAttributeName("asp-action")]
    public string? AspAction { get; set; }

    [HtmlAttributeName("asp-controller")]
    public string? AspController { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var currentAction = ViewContext.RouteData.Values["action"]?.ToString();
        var currentController = ViewContext.RouteData.Values["controller"]?.ToString();

        if (AspAction is null || currentAction is null || currentController is null)
        {
            return;
        }

        if (AspAction.IEquals(currentAction) && (AspController == null || AspController.IEquals(currentController)))
        {
            output.Attributes.SetAttribute("class", $"{output.Attributes["class"]?.Value?.ToString()} is-active".Trim());
        }
    }
}