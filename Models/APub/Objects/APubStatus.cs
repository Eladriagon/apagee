namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubStatus : APubObject
{
    public override string Type => APubConstants.TYPE_OBJ_NOTE;

    public static APubStatus FromArticle(Article article)
    {
        var status = FromArticle<APubStatus>(article);

        status.Content = article.ArticleSummary;
        status.ContentMap = new()
        {
            ["en"] = article.ArticleSummary
        };
        status.Attachment = new APubLink($"https://{GlobalConfiguration.Current?.PublicHostname}/{article.Slug}")
        {
            Name = "Read more...",
            MediaType = "text/html"
        };

        return status;
    }
}