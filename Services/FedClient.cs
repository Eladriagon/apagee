namespace Apagee.Services;

public class FedClient(IHttpClientFactory httpClientFactory, JsonSerializerOptions opts, IMemoryCache cache)
{
    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    public JsonSerializerOptions Opts { get; } = opts;
    public IMemoryCache Cache { get; } = cache;

    private HttpClient Client => HttpClientFactory.CreateClient(Globals.HTTP_CLI_NAME_FED);

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
        var resp = await Client.PostAsJsonAsync(inboxUri, item, Opts);

        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> PostInboxFromActor<TObj>(string actorUri, TObj item) where TObj : APubObject
    {
        var actorInbox = await GetActorInboxAsync(actorUri);

        if (actorInbox is null) return false;

        var resp = await Client.PostAsJsonAsync(actorInbox, item, Opts);

        return resp.IsSuccessStatusCode;
    }
}