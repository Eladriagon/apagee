namespace Apagee.ViewModels;

public class ArticleListViewModel
{
    public required IEnumerable<ArticleViewModel> Articles { get; set; }

    public Settings? SiteSettings { get; set; }

    public required string AuthorUsername { get; set; }

    public string? AuthorDisplayName { get; set; }

    public string? AuthorAvatarB64 { get; set; }

    public string? AuthorBio { get; set; }

    public string? ThemeCss { get; set; }
}