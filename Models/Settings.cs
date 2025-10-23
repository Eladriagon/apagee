namespace Apagee.Models;

[Table("Settings")]
public sealed class Settings
{
    [ExplicitKey]
    public int Id { get; set; } = 1;

    public string? ThemeCss { get; set; }

    public bool EnableFontAwesome { get; set; }

    public string? Favicon { get; set; }

    /// <summary>
    /// Stored here and not in the config because we offer a file upload for it.
    /// </summary>
    public string? AuthorAvatar { get; set; }
}