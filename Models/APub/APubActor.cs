namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubActor : APubObject
{
    public override string Type =>  APubConstants.TYPE_OBJ_ACTOR;

    public string PreferredUsername { get; set; } = default!;

    public string Inbox { get; set; } = default!;

    public string Outbox { get; set; } = default!;

    public string Followers { get; set; } = default!;

    public string Following { get; set; } = default!;

    public PublicKey PublicKey { get; set; } = default!;

    public static APubActor Create<TActor>(string keyId, string keyPem) where TActor : APubActor, new()
    {
        var config = ConfigManager.GetGlobalConfig();

        var baseUrl = $"https://{config.PublicHostname}/api/users/{config.FediverseUsername}";

        return new TActor
        {
            Id = $"{baseUrl}",
            PreferredUsername = config.FediverseUsername,
            Inbox = $"{baseUrl}/inbox",
            Outbox = $"{baseUrl}/outbox",
            Followers = $"{baseUrl}/followers",
            Following = $"{baseUrl}/following",
            Summary = config.FediverseBio,
            Url = [new APubLink(baseUrl)],
            PublicKey = new PublicKey
            {
                Id = keyId,
                Owner = baseUrl,
                PublicKeyPem = keyPem
            }
        };
    }
}