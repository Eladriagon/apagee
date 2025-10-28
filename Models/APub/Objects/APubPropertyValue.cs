namespace Apagee.Models.APub;

public class APubPropertyValue : APubObject
{    
    public override string Type => APubConstants.TYPE_OBJ_PROP_VAL;
    public string? Value { get; set; }

    public APubPropertyValue() { }

    public APubPropertyValue(string name, string value)
    {
        Name = name;
        Value = value;
    }
}