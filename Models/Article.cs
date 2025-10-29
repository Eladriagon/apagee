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

    // If we end up having a <!-- more --> or something similar, add support for that here.
    [Write(false)]
    public string? ArticleSummary => Utils.MarkdownToHtml($"{Title}{(Body is not null ? "\n\n" + Body.Replace("\r", "").Split("\n\n")[0].Truncate(400) : null)}").Trim();
}
