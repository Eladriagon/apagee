namespace Apagee.Models;

[Table("Settings")]
public sealed class Settings
{
    [ExplicitKey]
    public int Id { get; set; } = 1;

    public string? ThemeCss { get; set; }

    public bool EnableFontAwesome { get; set; }

    public string? Favicon { get; set; }

    public string? AuthorAvatar { get; set; }

    public string? ODataTitle { get; set; }

    public string? ODataDescription { get; set; }

    public string? ClaimProfileUrl { get; set; }
}