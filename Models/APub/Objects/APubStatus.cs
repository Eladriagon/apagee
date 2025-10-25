namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubStatus : APubObject
{
    public override string Type => APubConstants.TYPE_OBJ_NOTE;

    public static APubStatus Create(Article article)
    {
        var author = $"https://{GlobalConfiguration.Current?.PublicHostname}/api/users/{GlobalConfiguration.Current?.FediverseUsername}";
        var id = $"{author}/statuses/{article.Uid}";
        var content = Markdig.Markdown.ToHtml(article.Body ?? "");

        return new APubStatus
        {
            Id = id,
            Published = article.PublishedOn,
            Updated = article.PublishedOn,
            Url = $"https://{GlobalConfiguration.Current?.PublicHostname}/{article.Slug}",
            AttributedTo = author,
            To = APubConstants.TARGET_PUBLIC,
            AtomUri = new Uri(id),
            ContentSingle = article.ArticleSummary,
            ContentMap = new()
            {
                ["en"] = article.ArticleSummary,
            }
        };
    }
}