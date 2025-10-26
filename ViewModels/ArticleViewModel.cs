namespace Apagee.ViewModels;

public class ArticleViewModel
{
    public required Article Article { get; set; }

    public string BodyHtml => Article.BodyMode switch
    {
        BodyMode.Markdown => Markdown.ToHtml(Article.Body ?? ""),
        BodyMode.HTML => Article.Body ?? "",
        BodyMode.PlainText => Article.Body?.Replace("\r", "").Replace("\n", "<br />") ?? "",
        _ => "oops, i think a stray neutron caused a bit flip..."
    };
}