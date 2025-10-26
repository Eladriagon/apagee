namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubArticle : APubObject
{
    public override string Type => APubConstants.TYPE_OBJ_ARTICLE;

    public static APubArticle FromArticle(Article article)
    {
        var art = FromArticle<APubArticle>(article);

        art.Name = article.Title;

        return art;
    }
}