namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubObject : APubBase
{
    public override string Type => APubConstants.TYPE_OBJECT;

    [JsonPropertyName("name")]
    public string? NameSingle { get; set; }
    public Dictionary<string, string?>? NameMap { get; set; }

    [JsonIgnore]
    public string? Name
    {
        get
        {
            return NameSingle ?? NameMap?[GlobalConfiguration.Current?.LanguageCode ?? "en"]; /* TODO: Language selection? */
        }
        set
        {
            NameSingle = value;
            NameMap ??= [];
            NameMap[GlobalConfiguration.Current?.LanguageCode ?? "en"] = value;
        }
    }

    [JsonPropertyName("content")]
    public string? ContentSingle { get; set; }
    public Dictionary<string, string?>? ContentMap { get; set; }

    [JsonIgnore]
    public string? Content
    {
        get
        {
            return ContentSingle ?? ContentMap?[GlobalConfiguration.Current?.LanguageCode ?? "en"]; /* TODO: Language selection? */
        }
        set
        {
            ContentSingle = value;
            ContentMap ??= [];
            ContentMap[GlobalConfiguration.Current?.LanguageCode ?? "en"] = value;
        }
    }

    [JsonPropertyName("summary")]
    public string? SummarySingle { get; set; }
    public Dictionary<string, string?>? SummaryMap { get; set; }

    [JsonIgnore]
    public string? Summary
    {
        get
        {
            return SummarySingle ?? SummaryMap?[GlobalConfiguration.Current?.LanguageCode ?? "en"]; /* TODO: Language selection? */
        }
        set
        {
            SummarySingle = value;
            SummaryMap ??= [];
            SummaryMap[GlobalConfiguration.Current?.LanguageCode ?? "en"] = value;
        }
    }

    public APubPolyBase? Url { get; set; }
    public APubPolyBase? Tag { get; set; }
    public APubPolyBase? Context { get; set; }
    
    public APubPolyBase? AttributedTo { get; set; }
    public Uri? AtomUri { get; set; }
    public bool? Sensitive { get; set; }

    public DateTime? Published { get; set; }
    public DateTime? Updated { get; set; }

    [AlwaysArray]
    public APubPolyBase? To { get; set; }

    [AlwaysArray]
    public APubPolyBase? Bto { get; set; }

    [AlwaysArray]
    public APubPolyBase? Cc { get; set; }

    [AlwaysArray]
    public APubPolyBase? Bcc { get; set; }
    
    public APubPolyBase? Audience { get; set; }
}