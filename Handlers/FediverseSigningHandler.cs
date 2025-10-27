namespace Apagee.Handlers;

public class FediverseSigningHandler(KeypairHelper keypairHelper) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Include JSON-LD
        request.Headers.Accept.Clear();
        request.Headers.TryAddWithoutValidation("Accept", $"{Globals.JSON_LD_CONTENT_TYPE}, {Globals.JSON_ACT_CONTENT_TYPE}");

        if (request.Content is not null)
        {
            request.Content.Headers.Remove("Content-Type");
            request.Content.Headers.TryAddWithoutValidation("Content-Type", Globals.JSON_LD_CONTENT_TYPE);
        }

        // Good internet citizenry
        request.Headers.UserAgent.Clear();
        request.Headers.TryAddWithoutValidation("User-Agent", $"Apagee/{Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? "1.0"} (+https://github.com/eladriagon/apagee)");

        // And sign it
        var privKey = keypairHelper.ActorRsaPrivateKey
            ?? throw new ApageeException("Cannot create FediverseSigningHandler: Actor private key is missing.");

        SignRequest(request, privKey, keypairHelper.KeyId);

        return await base.SendAsync(request, cancellationToken);
    }

    private static void SignRequest(HttpRequestMessage request, RSA privateKey, string keyId)
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
            var bodyBytes = request.Content.ReadAsByteArrayAsync().Result;
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
    }
}