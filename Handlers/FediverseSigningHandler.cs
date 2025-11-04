using System.Net.Cache;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Apagee.Handlers;

public class FediverseSigningHandler(KeypairHelper keypairHelper, InboxService inboxService) : DelegatingHandler
{
    public InboxService InboxService { get; } = inboxService;

    // Handler for outgoing HTTP client requests made with FedClient
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // No caching
        request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true, MaxAge = TimeSpan.Zero };

        request.Headers.Accept.Clear();
        request.Content?.Headers.Remove("Content-Type");
        if (request.Headers.TryGetValues("X-Use-Jrd", out _))
        {
            // Include JRD JSON
            request.Headers.TryAddWithoutValidation("Accept", $"{Globals.JSON_RD_CONTENT_TYPE}");
            if (request.Content is not null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(Globals.JSON_RD_CONTENT_TYPE);
            }
            request.Headers.Remove("X-Use-Jrd");
        }
        else
        {
            // Include Activity JSON
            request.Headers.TryAddWithoutValidation("Accept", $"{Globals.JSON_ACT_CONTENT_TYPE}");
            if (request.Content is not null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(Globals.JSON_ACT_CONTENT_TYPE);
            }
        }

        // Good internet citizenry
        request.Headers.UserAgent.Clear();
        request.Headers.TryAddWithoutValidation("User-Agent", $"Apagee/{Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? "1.0"} (+https://github.com/eladriagon/apagee)");

        // And sign it
        var privKey = keypairHelper.ActorRsaPrivateKey
            ?? throw new ApageeException("Cannot create FediverseSigningHandler: Actor private key is missing.");

        await SignRequest(request, privKey, keypairHelper.KeyId);

        var resp = await base.SendAsync(request, cancellationToken);

        try
        {
            Console.WriteLine($"""
                [DBG-RESP]
                HTTP {(int)resp.StatusCode} {resp.StatusCode}
                {string.Join("\r\n", resp.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}

                """);
            if ((await resp.Content.ReadAsStringAsync(cancellationToken)) is { Length: > 0 } body)
            {
                Console.WriteLine(body);
            }
            else
            {
                Console.WriteLine("[[[ NO RESP BODY ]]]");
            }
            Console.WriteLine("\r\n[/DBG-RESP]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DBG-ERR] Error in debug block: {ex.GetType().FullName}: {ex.Message}\r\n{ex.StackTrace}\r\n");
        }

        return resp;
    }

    private async Task SignRequest(HttpRequestMessage request, RSA privateKey, string keyId)
    {
        var method = request.Method.Method.ToLowerInvariant();
        var pathAndQuery = request.RequestUri!.PathAndQuery;
        var host = request.RequestUri.Host;

        // Required headers
        request.Headers.Host = host;
        request.Headers.Date = DateTimeOffset.UtcNow;

        // Digest (body)
        var digest = "";
        if (request.Content != null)
        {
            var bodyBytes = await request.Content.ReadAsByteArrayAsync();
            var bodyHash = Convert.ToBase64String(SHA256.HashData(bodyBytes));
            digest = $"SHA-256={bodyHash}";

            request.Headers.TryAddWithoutValidation("Digest", digest);
        }

        // Build signing string
        var signingString =
            $"(request-target): {method} {pathAndQuery}\n" +
            $"host: {host}\n" +
            $"date: {request.Headers.Date?.ToString("r")}\n";

        if (!string.IsNullOrEmpty(digest))
            signingString += $"digest: {digest}\n";

        var bytesToSign = Encoding.ASCII.GetBytes(signingString.TrimEnd());
        var sigBytes = privateKey.SignData(bytesToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signature = Convert.ToBase64String(sigBytes);

        var headers = string.IsNullOrEmpty(digest)
            ? "(request-target) host date"
            : "(request-target) host date digest";

        var signatureHeader =
            $"keyId=\"{keyId}\",algorithm=\"rsa-sha256\",headers=\"{headers}\",signature=\"{signature}\"";

        request.Headers.TryAddWithoutValidation("Signature", signatureHeader);

        // Optional: Collection-Synchronization
        if (request.Headers.Contains("Collection-Synchronization"))
        {
            request.Headers.Remove("Collection-Synchronization");

            var followersCollectionId = new Uri($"https://{GlobalConfiguration.Current!.PublicHostname}/api/users/{GlobalConfiguration.Current!.FediverseUsername}/followers");

            var followers = (IReadOnlyCollection<string>)await InboxService.GetFollowerList(count: 1000, domain: host);

            if (followers.Count is 0)
            {
                return;
            }

            var followerSyncId = new Uri($"https://{GlobalConfiguration.Current!.PublicHostname}/api/users/{GlobalConfiguration.Current!.FediverseUsername}/followers?domain={new Uri(followers.First()).Host}");

            // digest = XOR of SHA-256(identifier UTF-8) for each identifier, hex-encoded
            var xor = XorSha256Digests(followers ?? []);
            var xorHex = ToHex(xor);

            var collectionSyncHeader =
                $"collectionId=\"{Escape(followersCollectionId.ToString())}\"," +
                $"url=\"{Escape(followerSyncId.ToString())}\"," +
                $"digest=\"{xorHex}\"";

            request.Headers.TryAddWithoutValidation("Collection-Synchronization", collectionSyncHeader);
        }

        // Temporary
        Console.WriteLine("");
        Console.WriteLine($"[[[ DBG ]]]");
        Console.WriteLine(" ");
        Console.WriteLine($"{request.Method.Method} {request.RequestUri} HTTP/1.1");
        foreach (var h in request.Headers)
        {
            Console.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");
        }
        Console.WriteLine(" ");
        Console.WriteLine($"{(request.Content is not null ? await request.Content.ReadAsStringAsync() : "### Body has no content. ###")}");
        Console.WriteLine(" ");
        Console.WriteLine($"[[[ /DBG ]]]");
        Console.WriteLine("");
    }

    private static string ToHex(ReadOnlySpan<byte> bytes)
    {
        var c = new char[bytes.Length * 2];
        int i = 0;
        foreach (var b in bytes)
        {
            c[i++] = GetHexNibble(b >> 4);
            c[i++] = GetHexNibble(b & 0xF);
        }
        return new string(c);
    }

    private static char GetHexNibble(int value) => (char)(value < 10 ? ('0' + value) : ('a' + (value - 10)));

    private static byte[] XorSha256Digests(IEnumerable<string> identifiers)
    {
        var acc = new byte[32]; // SHA-256 length
        foreach (var id in identifiers ?? Enumerable.Empty<string>())
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(id));
            for (int i = 0; i < acc.Length; i++)
            {
                acc[i] ^= hash[i];
            }
        }
        return acc;
    }

    private static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}