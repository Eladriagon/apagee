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
}
