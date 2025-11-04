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

    private readonly JsonSerializerOptions SerializerOptions;

    public ContextResponseWrapperFilter(IOptions<JsonOptions> jsonOptions)
    {
        SerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    // Executed on controller result processing (e.g. return Ok(...);).
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // Note: Only works on responses using "Ok(...)".

        if (context.Result is not OkObjectResult { Value: not null } ok)
        {
            await next(); return;
        }

        // Add content caching headers - no API requests should be cached (for now)
        if (!context.HttpContext.Response.HasStarted)
        {
            context.HttpContext.Response.Headers.CacheControl = new StringValues(["no-store", "no-cache", "must-revalidate"]);
            context.HttpContext.Response.Headers.Pragma = "no-cache";
            context.HttpContext.Response.Headers.Expires = "0";
        }

        // Most specific/rare ones first.
        ok.ContentTypes.Clear();
        if (GetCurrentCustomAttributeWithClass<ExplicitContentTypeAttribute>(context.ActionDescriptor) is ExplicitContentTypeAttribute ect)
        {
            ok.ContentTypes.Add(ect.Name);
        }
        else if (GetCurrentCustomAttributeWithClass<JrdContentTypeAttribute>(context.ActionDescriptor) is not null)
        {
            ok.ContentTypes.Add(Globals.JSON_RD_CONTENT_TYPE);
        }
        else if (GetCurrentCustomAttributeWithClass<NodeInfoContentTypeAttribute>(context.ActionDescriptor) is not null)
        {
            ok.ContentTypes.Add(Globals.JSON_NODEINFO_CONTENT_TYPE);
        }
        else if (GetCurrentCustomAttributeWithClass<ActivityContentTypeAttribute>(context.ActionDescriptor) is not null)
        {
            ok.ContentTypes.Add(WithUtf8(Globals.JSON_ACT_CONTENT_TYPE));
        }
        else
        {
            ok.ContentTypes.Add(WithUtf8("application/json"));
        }

        // Not some sort of Json* type but still an object?
        // Serialize it now so we can manipulate it.
        var declared = ok.Value.GetType();
        var node = JsonSerializer.SerializeToNode(ok.Value, declared, SerializerOptions);

        if (node is not JsonObject obj)
        {
            // Non-object roots: do nothing
            await next(); return;
        }

        JsonObject root;
        if (GetCurrentCustomAttributeWithClass<NoContextAttribute>(context.ActionDescriptor) is not null)
        {
            root = obj;
        }
        else
        {
            if (node["@context"] is not null)
            {
                await next(); return;
            }

            // Build new root with "context" + original properties (cloned!)
            root = new JsonObject
            {
                ["@context"] = JsonSerializer.SerializeToNode(APubGlobalContext)
            };

            foreach (var kvp in obj)
            {
                // Avoid re-parenting: clone the node before adding
                root[kvp.Key] = kvp.Value?.DeepClone();
            }
        }

        // This bypasses any other dumb output formatting that happens, like adding "; charset=utf-8" everywhere or checking the `Accept` header.
        context.Result = new ContentResult
        {
            Content = JsonSerializer.Serialize(root, SerializerOptions),
            ContentType = ok.ContentTypes.First(),
            StatusCode = ok.StatusCode
        };

        await next();
    }

    private string WithUtf8(string contentType) => $"{contentType}; charset=utf-8";

    private static bool IsJson(ObjectResult r)
        => r.ContentTypes.Count == 0 || r.ContentTypes.Contains("application/json");

    private static JsonSerializerOptions GetJsonOptions(FilterContext ctx)
        => ctx.HttpContext.RequestServices
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;

    private IEnumerable<Attribute>? GetCurrentCustomAttributes(ActionDescriptor ad) =>
        ad is ControllerActionDescriptor cad ? cad.MethodInfo.GetCustomAttributes(true).OfType<Attribute>() : null;

    private IEnumerable<(Attribute attr, int priority)>? GetCurrentCustomAttributesWithClass(ActionDescriptor ad) =>
        ad is ControllerActionDescriptor cad
            ? cad.MethodInfo.GetCustomAttributes(true).OfType<Attribute>().Select(a => (a, 2))
                .Union(cad.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<Attribute>().Select(a => (a, 1)) ?? [])
            : null;

    private T? GetCurrentCustomAttribute<T>(ActionDescriptor ad) where T : Attribute =>
        (T?)GetCurrentCustomAttributes(ad)?.FirstOrDefault(a => a is T);

    private T? GetCurrentCustomAttributeWithClass<T>(ActionDescriptor ad) where T : Attribute =>
        (T?)GetCurrentCustomAttributesWithClass(ad)?.OrderByDescending(a => a.priority).FirstOrDefault(a => a.attr is T).attr;
}