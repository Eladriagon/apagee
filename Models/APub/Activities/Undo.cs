namespace Apagee.Models.APub;

[AutoDerivedType]
public class Undo : APubActivity
{
    public override string Type => APubConstants.TYPE_ACT_UNDO;
}