namespace Apagee.Models.APub;

[AutoDerivedType]
public class Reject : APubActivity
{
    public override string Type => APubConstants.TYPE_ACT_REJECT;
}