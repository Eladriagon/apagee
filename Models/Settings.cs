using Markdig.Helpers;

namespace Apagee.Models;

[Table("Settings")]
public sealed class Settings
{
    [ExplicitKey]
    public int Id { get; set; } = 1;

    public string? ThemeCss { get; set; }

    public bool EnableFontAwesome { get; set; }

    public string? Favicon { get; set; }
    
    [Write(false)]
    public IFormFile? FaviconInput { get; set; }

    public string? AuthorAvatar { get; set; }
    
    [Write(false)]
    public IFormFile? AuthorAvatarInput { get; set; }

    public string? BannerImage { get; set; }
    
    [Write(false)]
    public IFormFile? BannerImageInput { get; set; }

    public string? ODataTitle { get; set; }

    public string? ODataDescription { get; set; }

    public bool ShowBioOnSite { get; set; }

    public bool AutoReciprocateFollows { get; set; }

    public string? ClaimProfileUrl { get; set; }

    public string? Property1Key { get; set; }

    public string? Property1Value { get; set; }

    [Write(false)]
    public string Property1Formatted =>
        Property1Value is { Length: > 0 } p1v && p1v.Trim().ToUpper().StartsWith("HTTPS://")
            ? @$"<a href=""{p1v}"" rel=""nofollow noopener"" translate=""no"" target=""_blank""><span class=""hidden"" aria-hidden=""true"">https://</span><span class=""url"">{p1v[8..]}</span>{(p1v.EndsWith('/') ? @"<span class=""hidden"" aria-hidden=""true"">/</span>" : "")}</a>"
            : HtmlEncoder.Default.Encode(Property1Value ?? "");

    public string? Property2Key { get; set; }

    public string? Property2Value { get; set; }

    [Write(false)]
    public string? Property2Formatted =>
        Property2Value is { Length: > 0 } p2v && p2v.Trim().ToUpper().StartsWith("HTTPS://")
            ? @$"<a href=""{p2v}"" rel=""nofollow noopener"" translate=""no"" target=""_blank""><span class=""hidden"" aria-hidden=""true"">https://</span><span class=""url"">{p2v[8..]}</span>{(p2v.EndsWith('/') ? @"<span class=""hidden"" aria-hidden=""true"">/</span>" : "")}</a>"
            : HtmlEncoder.Default.Encode(Property2Value ?? "");

    public string? Property3Key { get; set; }

    public string? Property3Value { get; set; }

    [Write(false)]
    public string? Property3Formatted =>
        Property3Value is { Length: > 0 } p3v && p3v.Trim().ToUpper().StartsWith("HTTPS://")
            ? @$"<a href=""{p3v}"" rel=""nofollow noopener"" translate=""no"" target=""_blank""><span class=""hidden"" aria-hidden=""true"">https://</span><span class=""url"">{p3v[8..]}</span>{(p3v.EndsWith('/') ? @"<span class=""hidden"" aria-hidden=""true"">/</span>" : "")}</a>"
            : HtmlEncoder.Default.Encode(Property3Value ?? "");
}