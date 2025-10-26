namespace Apagee.ViewModels;

public class ArticleViewModel
{
    public required Article Article { get; set; }

    public Settings? SiteSettings { get; set; }

    public required string BodyHtml { get; set; }

    public required string AuthorUsername { get; set; }

    public string? AuthorDisplayName { get; set; }

    public string? AuthorBio { get; set; }

    public string? ThemeCss { get; set; }
}