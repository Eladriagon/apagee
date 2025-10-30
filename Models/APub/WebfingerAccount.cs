namespace Apagee.Models.APub;

public class WebfingerAccount
{
    public string Subject { get; set; } = default!;

    public List<string> Aliases { get; set; } = [];

    public List<WebfingerLink> Links { get; set; } = [];

    public static WebfingerAccount CreateUser()
    {
        var hostname = GlobalConfiguration.Current!.PublicHostname;
        var user = GlobalConfiguration.Current!.FediverseUsername;
        var acct = new WebfingerAccount
        {
            Subject = $"acct:{user}@{hostname}",
            Aliases =
            [
                $"https://{hostname}/@{user}",
                $"https://{hostname}/api/users/{user}"
            ],
            Links =
            [
                new()
                {
                    Rel = "self",
                    Type = Globals.JSON_ACT_CONTENT_TYPE,
                    Href = $"https://{hostname}/api/users/{user}"
                },
                new()
                {
                    Rel = "http://webfinger.net/rel/profile-page",
                    Type = "text/html",
                    Href = $"https://{hostname}/@{user}"
                },
                new()
                {
                    Rel = Globals.OSTATUS_SUBSCRIBE_REL,
                    Template = $"https://{hostname}/follow_authorize?uri={{uri}}"
                }
            ]
        };

        if (SettingsService.Current!.AuthorAvatar is not null)
        {
            acct.Links.Add(new()
            {
                Rel = Globals.WEBFINGER_AVATAR_REL,
                Type = Utils.IsPng(SettingsService.Current.AuthorAvatar) ? "image/png" : "image/jpeg",
                Href = $"https://{hostname}/avatar.png"
            });
        }

        return acct;
    }

    public static WebfingerAccount CreateApp()
    {
        var hostname = GlobalConfiguration.Current!.PublicHostname;
        return new WebfingerAccount
        {
            Subject = $"acct:{hostname}@{hostname}",
            Aliases =
            [
                $"https://{hostname}/actor"
            ],
            Links =
            [
                new()
                {
                    Rel = "self",
                    Type = Globals.JSON_ACT_CONTENT_TYPE,
                    Href = $"https://{hostname}/actor"
                },
                new()
                {
                    Rel = "about",
                    Type = "text/html",
                    Href = $"https://github.com/eladriagon/apagee"
                },
                new()
                {
                    Rel = Globals.OSTATUS_SUBSCRIBE_REL,
                    Template = $"https://{hostname}/follow_authorize?uri={{uri}}"
                }
            ]
        };
    }
}

public class WebfingerLink
{
    public string Rel { get; set; } = default!;

    public string Type { get; set; } = default!;

    public string Href { get; set; } = default!;

    public string Template { get; set; } = default!;
}