using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Apagee.Models;

[Table(nameof(Interaction))]
public class Interaction
{
    public required string ID { get; set; }

    public required string ArticleUID { get; set; }

    public InteractionType Type { get; set; }

    public static Interaction? FromJson(JsonObject obj, Article related)
    {
        if (obj["id"] is not JsonValue valId || valId.GetValue<string>() is not { Length: > 0 } id
        || obj["type"] is not JsonValue valType || valType.GetValue<string>() is not { Length: > 0 } type)
        {
            return null;
        }

        try
        {
            return new Interaction
            {
                ID = id,
                ArticleUID = related.Uid,
                Type = type switch
                {
                    APubConstants.TYPE_ACT_LIKE => InteractionType.Like,
                    APubConstants.TYPE_ACT_ANNOUNCE => InteractionType.Announce,
                    _ => throw new ApageeException("Invalid type: " + type)
                }
            };
        }
        catch (ApageeException)
        {
            return null;
        }
    }
}
