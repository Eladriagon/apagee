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

    public bool Discoverable { get; set; }

    public bool Indexable { get; set; }

    public bool Memorial { get; set; }

    public PublicKey PublicKey { get; set; } = default!;

    public APubImage? Icon { get; set; }

    public static APubActor Create<TActor>(string keyId, string keyPem) where TActor : APubActor, new()
    {
        var icon = SettingsService.Current?.AuthorAvatar;
        var config = ConfigManager.GetGlobalConfig();

        var baseUrl = $"https://{config.PublicHostname}/api/users/{config.FediverseUsername}";

        return new TActor
        {
            Id = $"{baseUrl}",
            Name = config.AuthorDisplayName ?? config.FediverseUsername,
            PreferredUsername = config.FediverseUsername,
            Icon = icon is null ? null : new()
            {
                Url = $"{baseUrl}/avatar.png",
                MediaType = Utils.IsPng(icon) ? "image/png" : "image/jpeg"
            },
            Inbox = $"{baseUrl}/inbox",
            Outbox = $"{baseUrl}/outbox",
            Followers = $"{baseUrl}/followers",
            Following = $"{baseUrl}/following",
            Summary = config.FediverseBio,
            Url = [new APubLink(baseUrl)],
            Discoverable = true,
            Indexable = true,
            Memorial = false,
            PublicKey = new PublicKey
            {
                Id = keyId,
                Owner = baseUrl,
                PublicKeyPem = keyPem
            }
        };
    }
}