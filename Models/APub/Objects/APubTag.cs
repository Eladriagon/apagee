namespace Apagee.Models.APub;

public class APubTag : APubObject
{    
    public override string Type => APubConstants.TYPE_OBJ_HASHTAG;
    public string? Href { get; set; }

    public APubTag() { }

    public APubTag(string name, string href)
    {
        Name = name;
        Href = href;
    }
}