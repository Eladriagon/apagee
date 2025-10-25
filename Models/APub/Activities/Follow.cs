namespace Apagee.Models.APub;

[AutoDerivedType]
public class Follow : APubActivity
{
    public override string Type => APubConstants.TYPE_ACT_FOLLOW;
}