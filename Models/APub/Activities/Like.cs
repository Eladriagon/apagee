namespace Apagee.Models.APub;

[AutoDerivedType]
public class Like : APubActivity
{
    public override string Type => APubConstants.TYPE_ACT_LIKE;
}