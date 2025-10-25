namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubActivity : APubObject
{
    public override string Type => APubConstants.TYPE_OBJ_ACTIVITY;

    public APubPolyBase? Actor { get; set; }
    public APubPolyBase? Object { get; set; }
    public APubPolyBase? Target { get; set; }
    public APubPolyBase? Result { get; set; }
    public APubPolyBase? Origin { get; set; }
    public APubPolyBase? Instrument { get; set; }

    public static TAct Wrap<TAct>(APubObject innerObject, string? actorUri = null, APubPolyBase? target = null, APubObject? result = null, APubObject? origin = null, DateTime? published = null)
        where TAct : APubActivity, new()
    {
        if (innerObject is APubActivity)
        {
            throw new ApageeException("APub: Can't wrap an activity inside another activity.");
        }

        return new TAct
        {
            Actor = actorUri,
            Object = innerObject,
            Target = target,
            Result = result,
            Origin = origin
        };
    }
}