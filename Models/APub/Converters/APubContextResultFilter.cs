namespace Apagee.Models.APub.Converters;

public sealed class ContextResponseWrapperFilter : IAsyncResultFilter
{
    internal static object[] APubGlobalContext = [
        "https://www.w3.org/ns/activitystreams",
        "https://w3id.org/security/v1",
        new Dictionary<string, object> {
            ["manuallyApprovesFollowers"] = "as:manuallyApprovesFollowers",
            ["toot"] = "http://joinmastodon.org/ns#",
            ["Hashtag"] = "https://www.w3.org/ns/activitystreams#Hashtag",
            ["alsoKnownAs"] = new Dictionary<string, object> {
                ["@id"] = "as:alsoKnownAs",
                ["@type"] = "@id"
            },
            ["attributionDomains"] = new Dictionary<string, object> {
                ["@id"] = "as:attributionDomains",
                ["@type"] = "@id"
            },
            ["featured"] = new Dictionary<string, object> {
                ["@id"] = "toot:featured",
                ["@type"] = "@id"
            },
            ["featuredTags"] = new Dictionary<string, object> {
                ["@id"] = "toot:featuredTags",
                ["@type"] = "@id"
            },
            ["Emoji"] = "toot:Emoji",
            ["schema"] = "http://schema.org#",
            ["PropertyValue"] = "schema:PropertyValue",
            ["value"] = "schema:value",
            ["discoverable"] = "toot:discoverable",
            ["suspended"] = "toot:suspended",
            ["memorial"] = "toot:memorial",
            ["indexable"] = "toot:indexable",
        }
    ];

    private static bool IsJson(ObjectResult r)
        => r.ContentTypes.Count == 0 || r.ContentTypes.Contains("application/json");

    private readonly JsonSerializerOptions _json;

    public ContextResponseWrapperFilter(IOptions<JsonOptions> jsonOptions)
        => _json = jsonOptions.Value.JsonSerializerOptions;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is not ObjectResult r || !IsJson(r))
        {
            await next(); return;
        }

        if (context.HttpContext.Request.Path.Value?.Contains("webfinger") ?? false)
        {
            await next(); return;
        }

        if (r.Value is null)
        {
            await next(); return;
        }

        var declared = r.DeclaredType ?? r.Value.GetType();
        var node = JsonSerializer.SerializeToNode(r.Value, declared, _json);

        if (node is not JsonObject obj)
        {
            // Non-object roots: do nothing
            await next(); return;
        }

        if (node["@context"] is not null)
        {
            await next(); return;
        }

        // Build new root with "context" + original properties (cloned!)
        var root = new JsonObject
        {
            ["@context"] = JsonSerializer.SerializeToNode(APubGlobalContext)
        };

        foreach (var kvp in obj)
        {
            // Avoid re-parenting: clone the node before adding
            root[kvp.Key] = kvp.Value?.DeepClone();
        }

        r.Value = root;
        r.DeclaredType = typeof(JsonObject); // helps STJ avoid reflecting the old type
        r.ContentTypes.Clear();
        r.ContentTypes.Add("application/json");

        await next();
    }

    private static JsonSerializerOptions GetJsonOptions(FilterContext ctx)
        => ctx.HttpContext.RequestServices
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
}