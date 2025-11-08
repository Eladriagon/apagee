namespace Apagee.Models;

[Table("Article")]
public class Article
{
    [ExplicitKey]
    public required string Uid { get; set; }
    public required string Slug { get; set; }
    public required DateTime CreatedOn { get; set; }
    public DateTime? PublishedOn { get; set; }
    public ArticleStatus Status { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public BodyMode BodyMode { get; set; }

    [Write(false)]
    public IList<string>? Tags { get; set; }

    [Write(false)]
    public string? ArticleLocalSummary =>
        Utils.MarkdownToHtml($"{(Body is not null ? "\n\n" + Body.Replace("\r", "").Split("\n\n")[0].Truncate(400) : "")}").Trim();

    [Write(false)]
    public string? ArticleSummary =>
        ArticleLocalSummary + Utils.MarkdownToHtml($"[Read more at {GlobalConfiguration.Current!.PublicHostname}...](https://{GlobalConfiguration.Current!.PublicHostname}/{Slug})");
}
