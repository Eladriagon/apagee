namespace Apagee.Models.APub;

public class WebfingerAccount
{
    public string Subject { get; set; } = default!;

    public List<string> Aliases { get; set; } = [];

    public List<WebfingerLink> Links { get; set; } = [];

    public static WebfingerAccount Create(string hostname, string user)
    {
        return new WebfingerAccount
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
                    Type = "application/activity+json",
                    Href = $"https://{hostname}/api/users/{user}"
                },
                new()
                {
                    Rel = "http://webfinger.net/rel/profile-page",
                    Type = "text/html",
                    Href = $"https://{hostname}/@{user}"
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
}