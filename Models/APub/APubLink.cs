namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubLink : APubObject
{
    public APubLink() { }

    public APubLink(string uri)
    {
        Href = new Uri(uri);
    }

    public APubLink(Uri uri)
    {
        Href = uri;
    }

    public override string Type => APubConstants.TYPE_LINK;

    [JsonIgnore]
    public Uri? Href { get; set; }

    [JsonPropertyName("href")]
    public string? HrefStr
    {
        get => Href?.ToString();
        set => Href = value is null ? null : new Uri(value);
    }
    public List<string?>? Rel { get; set; }
    public string? MediaType { get; set; }
    public string? HrefLang { get; set; }
    public uint? Height { get; set; }
    public uint? Width { get; set; }
    public APubPolyBase? Preview { get; set; }

    /// <summary>
    /// Used during deserialization for anything that doesn't fit into a Uri().
    /// </summary>
    [JsonIgnore]
    public string? BadHref { get; set; }

    [JsonIgnore]
    public bool IsEmpty => Href is null && Name is null && MediaType is null;

    [JsonIgnore]
    public bool IsOnlyLink => Href is not null && Name is null && MediaType is null;
}
