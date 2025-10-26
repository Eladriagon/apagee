namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubImage : APubObject
{
    public override string Type => APubConstants.TYPE_OBJ_IMAGE;

    public required string? MediaType { get; set; }
}