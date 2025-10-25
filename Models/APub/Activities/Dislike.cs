namespace Apagee.Models.APub;

[AutoDerivedType]
public class Dislike : APubActivity
{
    public override string Type => APubConstants.TYPE_ACT_DISLIKE;
}