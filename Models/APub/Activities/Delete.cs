namespace Apagee.Models.APub;

[AutoDerivedType]
public class Delete : APubActivity
{
    public override string Type => APubConstants.TYPE_ACT_DELETE;
}