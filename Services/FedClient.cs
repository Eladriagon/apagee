using System.Net;

namespace Apagee.Services;

public class FedClient(IHttpClientFactory httpClientFactory, JsonSerializerOptions opts, IMemoryCache cache)
{
    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    public JsonSerializerOptions Opts { get; } = opts;
    public IMemoryCache Cache { get; } = cache;

    private HttpClient Client => HttpClientFactory.CreateClient(Globals.HTTP_CLI_NAME_FED);

    public async Task<JsonObject?> GetWebfinger(string domain, string? account = null)
    {
        domain = domain.ToLower().Trim().TrimEnd('/');
        if (domain.StartsWith("https://"))
        {
            domain = domain.Replace("https://", "");
        }
        if (account?.Trim() is { Length: 0 })
        {
            account = domain;
        }

        var client = Client;

        var target = $"https://{domain}/{Globals.WEBFINGER_PATH}?resource=acct:{account}@{domain}";
        var request = new HttpRequestMessage(HttpMethod.Get, target);
        request.Headers.TryAddWithoutValidation("X-Use-Jrd", "1");

        var resp = await client.SendAsync(request);

        if (!resp.IsSuccessStatusCode)
        {
            throw new ApageeException($"Webfinger request failed: {target} > HTTP {(int)resp.StatusCode} {resp.StatusCode} - {resp.ReasonPhrase}");
        }

        return await JsonNode.ParseAsync(await resp.Content.ReadAsStreamAsync()) is JsonObject j ? j : null;
    }

    public async Task<string?> GetSubscribeRedirect(string domain, string? account = null)
    {
        JsonObject wf;
        try
        {
            wf = await GetWebfinger(domain, account) ?? throw new("Null response from GetWebfinger()");
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Unable to get subscribe redirect URL from webfinger for {account}@{domain}.", ex);
        }

        if (wf["links"] is JsonArray { Count: > 0 } links)
        {
            var subLink = links.FirstOrDefault(s => s?["rel"] is JsonValue v && v.GetValue<string>() == Globals.OSTATUS_SUBSCRIBE_REL);
            return subLink?["template"] is JsonValue tpl && Uri.TryCreate(tpl.GetValue<string>(), UriKind.Absolute, out _) ? tpl.GetValue<string>() : null;
        }
        return null;
    }

    public async Task<string?> GetActorInboxAsync(string actorUri)
    {
        if (Cache.Get<string>($"ACT:INB:{actorUri}") is string { Length: > 0 } s) return s;

        var actor = await Client.GetStringAsync(actorUri);

        if (actor is null) return null;

        var json = await JsonNode.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(actor)));

        if (json?["inbox"] is JsonValue v && v.GetValue<string>() is string { Length: > 0 } inb)
        {
            Cache.Set($"ACT:INB:{actorUri}", inb, DateTimeOffset.UtcNow.AddDays(3));
            return inb;
        }

        return null;
    }

    public async Task<bool> PostInbox<TObj>(string inboxUri, TObj item) where TObj : APubObject
    {
        var client = Client;

        // Triggers a signature compute in the FediverseSigningHandler.
        // TODO: This header is expensive to compute. Cache follower lists somewhere.
        var request = new HttpRequestMessage(HttpMethod.Post, inboxUri);
        request.Headers.TryAddWithoutValidation("Collection-Synchronization", "pending");
        request.Content = JsonContent.Create(item, options: Opts);

        var resp = await client.SendAsync(request);

        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> PostInboxFromActor<TObj>(string actorUri, TObj item) where TObj : APubObject
    {
        var actorInbox = await GetActorInboxAsync(actorUri);

        if (actorInbox is null) return false;

        return await PostInbox(actorInbox, item);
    }
}