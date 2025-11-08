namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubObject : APubBase
{
    public override string Type => APubConstants.TYPE_OBJECT;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenReading)]
    [JsonPropertyName("@context")]
    public object[]? RootContext { get; set; }

    [JsonPropertyName("name")]
    public string? NameSingle { get; set; }

    [JsonIgnore] // Not needed?
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
    
    [JsonIgnore] // Not needed?
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

    public APubCollection? Likes { get; set; }
    public APubCollection? Shares { get; set; }
    public APubCollection? Replies { get; set; }

    public APubPolyBase? Url { get; set; }
    public APubPolyBase? Tag { get; set; }
    public APubPolyBase? Context { get; set; }
    public APubImage? Image { get; set; }
    
    [AlwaysArray]
    public APubPolyBase? Attachment { get; set; }
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

    public static TObj FromArticle<TObj>(Article article) where TObj : APubObject, new()
    {
        var author = $"https://{GlobalConfiguration.Current?.PublicHostname}/api/users/{GlobalConfiguration.Current?.FediverseUsername}";
        var id = $"{author}/statuses/{article.Uid}";
        var content = Utils.MarkdownToHtml(article.Body ?? "");

        return new TObj
        {
            Id = id,
            Published = article.PublishedOn,
            Url = $"https://{GlobalConfiguration.Current?.PublicHostname}/{article.Slug}",
            AttributedTo = author,
            To = APubConstants.TARGET_PUBLIC,
            Cc = $"{author}/followers",
            AtomUri = new Uri(id),
            ContentSingle = content,
            ContentMap = new()
            {
                ["en"] = content,
            },
            Attachment = [],
            Tag = article.Tags?.Select(t => new APubTag(t, $"https://{GlobalConfiguration.Current!.PublicHostname}/api/tags/{t}")).ToArray() ?? [],
            Likes = new()
            {
                Id = $"{id}/likes",
                TotalItems = 0
            },
            Shares = new()
            {
                Id = $"{id}/shares",
                TotalItems = 0
            },
            Replies = new()
            {
                Id = $"{id}/replies",
                First = new APubCollectionPage
                {
                    PartOf = new APubLink($"{id}/replies"),
                    Next = new APubLink($"{id}/replies?olderThan={Ulid.MaxValue}"),
                    Items = []
                }

            }
        };
    }

}