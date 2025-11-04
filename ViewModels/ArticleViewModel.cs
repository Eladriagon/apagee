namespace Apagee.ViewModels;

public class ArticleViewModel
{
    public required Article Article { get; set; }

    public uint? Likes { get; set; }
    public uint? Shares { get; set; }
    public uint? Replies { get; set; }

    public string BodyHtml => Article.BodyMode switch
    {
        BodyMode.Markdown => Utils.MarkdownToHtml(Article.Body ?? ""),
        BodyMode.HTML => Article.Body ?? "",
        BodyMode.PlainText => Article.Body?.Replace("\r", "").Replace("\n", "<br />") ?? "",
        _ => "oops, i think a stray neutron caused a bit flip..."
    };
}