namespace Apagee.Models.APub;

public abstract class APubBase
{
    [MultipleProperty("objectType", "verb", "@type", "type")]
    public abstract string Type { get; }

    [MultipleProperty("@Id", "id")]
    public string? Id { get; set; }

    /// <summary>
    /// Stores any unmapped properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Attributes { get; set; }

    /// <summary>
    /// Gets if this item is an object. (Object and Link are distinct.)
    /// </summary>
    [JsonIgnore]
    public bool IsObject => this is APubObject;

    /// <summary>
    /// Gets if this item is a link. (Object and Link are distinct.)
    /// </summary>
    [JsonIgnore]
    public bool IsLink => this is APubLink;
}