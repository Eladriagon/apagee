namespace Apagee.Models.APub;

[AutoDerivedType]
public class Update : APubActivity
{
    public override string Type => APubConstants.TYPE_ACT_UPDATE;
}