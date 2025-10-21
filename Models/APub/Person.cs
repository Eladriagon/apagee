namespace Apagee.Models.APub;

public class Person
{
    [JsonPropertyName("@context")]
    public object[] Context { get; set; } =
    [
        "https://www.w3.org/ns/activitystreams",
        new Dictionary<string, string>
        {
            { "toot", "http://joinmastodon.org/ns#" }
        },
        "https://w3id.org/security/v1"
    ];

    public string Type { get; set; } = "Person";

    public string Id { get; set; } = default!;

    public string PreferredUsername { get; set; } = default!;

    public string Inbox { get; set; } = default!;

    public string Outbox { get; set; } = default!;

    public string Followers { get; set; } = default!;

    public string Following { get; set; } = default!;

    public string Summary { get; set; } = default!;

    public string Url { get; set; } = default!;

    public PublicKey PublicKey { get; set; } = default!;

    public static Person Create(string keyId, string keyPem)
    {
        var config = ConfigManager.GetGlobalConfig();

        var baseUrl = $"https://{config.PublicHostname}/api/users/{config.FediverseUsername}";

        return new Person
        {
            Id = $"{baseUrl}",
            PreferredUsername = config.FediverseUsername,
            Inbox = $"{baseUrl}/api/inbox/{config.FediverseUsername}",
            Outbox = $"{baseUrl}/api/outbox/{config.FediverseUsername}",
            Followers = $"{baseUrl}/api/followers/{config.FediverseUsername}",
            Following = $"{baseUrl}/api/following/{config.FediverseUsername}",
            Summary = config.FediverseBio,
            Url = baseUrl,
            PublicKey = new PublicKey
            {
                Id = keyId,
                Owner = baseUrl,
                PublicKeyPem = keyPem
            }
        };
    }
}