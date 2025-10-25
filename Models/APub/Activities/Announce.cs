namespace Apagee.Models.APub;

[AutoDerivedType]
public class Announce : APubActivity
{
    public override string Type => APubConstants.TYPE_ACT_ANNOUNCE;
}