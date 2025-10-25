namespace Apagee.Models.APub;

[AutoDerivedType]
public class Person : APubActor
{
    public override string Type => APubConstants.TYPE_ID_PERSON;
}