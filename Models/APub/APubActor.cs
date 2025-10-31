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

    public string Featured { get; set; } = default!;

    public string FeaturedTags { get; set; } = default!;

    public bool Discoverable { get; set; }

    public bool Indexable { get; set; }

    public bool Memorial { get; set; }

    public bool Suspended { get; set; }

    public bool ManuallyApprovesFollowers { get; set; }

    public PublicKey PublicKey { get; set; } = default!;

    public APubImage? Icon { get; set; }
    
    public Dictionary<string,string>? Endpoints { get; set; }

    public static APubActor Create<TActor>(string keyId, string keyPem) where TActor : APubActor, new()
    {
        var icon = SettingsService.Current?.AuthorAvatar;
        var image = SettingsService.Current?.BannerImage;
        var config = ConfigManager.GetGlobalConfig();

        var rootUrl = $"https://{config.PublicHostname}";
        var baseUrl = $"{rootUrl}/api/users/{config.FediverseUsername}";

        return new TActor
        {
            Id = $"{baseUrl}",
            Name = config.AuthorDisplayName ?? config.FediverseUsername,
            PreferredUsername = config.FediverseUsername,
            Summary = config.FediverseBio,
            Icon = icon is null ? null : new()
            {
                Url = $"{rootUrl}/avatar.png",
                MediaType = Utils.IsPng(icon) ? "image/png" : "image/jpeg"
            },
            Image = image is null ? null : new()
            {
                Url = $"{rootUrl}/banner.png",
                MediaType = Utils.IsPng(icon) ? "image/png" : "image/jpeg"
            },
            Inbox = $"{baseUrl}/inbox",
            Outbox = $"{baseUrl}/outbox",
            Followers = $"{baseUrl}/followers",
            Following = $"{baseUrl}/following",
            Featured = $"{baseUrl}/collections/featured",
            FeaturedTags = $"{baseUrl}/collections/tags",
            Published = null,
            Attachment = [],
            Url = [new APubLink($"{rootUrl}/@{config.FediverseUsername}")],
            Endpoints = new()
            {
                ["sharedInbox"] = $"{rootUrl}/api/inbox"
            },
            Discoverable = true,
            Indexable = true,
            Memorial = false,
            ManuallyApprovesFollowers = false,
            PublicKey = new PublicKey
            {
                Id = keyId,
                Owner = baseUrl,
                PublicKeyPem = keyPem
            }
        };
    }

    public static APubActor CreateApplication<TActor>(string keyId, string keyPem) where TActor : APubActor, new()
    {
        var config = ConfigManager.GetGlobalConfig();

        var rootUrl = $"https://{config.PublicHostname}";
        var baseUrl = $"{rootUrl}/actor";

        return new TActor
        {
            Id = $"{baseUrl}",
            Name = "apagee",
            PreferredUsername = "Apagee Application",
            Inbox = $"{baseUrl}/api/inbox",
            Outbox = $"{baseUrl}/api/outbox",
            Followers = $"{baseUrl}/api/followers",
            Following = $"{baseUrl}/api/following",
            Summary = "Apagee is a lightweight fediverse-first FOSS blog app.",
            Url = [new APubLink(baseUrl)],
            Endpoints = new()
            {
                ["sharedInbox"] = $"{rootUrl}/api/inbox"
            },
            ManuallyApprovesFollowers = false,
            Discoverable = true,
            Indexable = true,
            Memorial = false,
            Suspended = false,
            PublicKey = new PublicKey
            {
                Id = keyId,
                Owner = baseUrl,
                PublicKeyPem = keyPem
            }
        };
    }
}