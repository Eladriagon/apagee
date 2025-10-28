namespace Apagee.Services;

public sealed class KeypairHelper
{
    public GlobalConfiguration Config { get; }
    private Lazy<string?> _publicKeyPem = default!;
    private Lazy<string?> _privateKeyPem = default!;
    private Lazy<RSA?> _publicRsa = default!;
    private Lazy<RSA?> _privateRsa = default!;
    public string? ActorPublicKeyPem => _publicKeyPem.Value;
    public string? ActorPrivateKeyPem => _privateKeyPem.Value;
    public RSA? ActorRsaPublicKey => _publicRsa.Value;
    public RSA? ActorRsaPrivateKey => _privateRsa.Value;
    public string KeyFragment => "key-" + Convert.ToHexStringLower(SHA3_256.Create().ComputeHash(new MemoryStream(Encoding.UTF8.GetBytes(ActorPublicKeyPem ?? throw new ApageeException("Cannot get KeyId: Actor public key PEM is null.")))))[..12];
    public string KeyId => $"https://{Config.PublicHostname}/api/users/{Config.FediverseUsername}#{KeyFragment}";
    private Lazy<string?> _sitePublicKeyPem = default!;
    private Lazy<string?> _sitePrivateKeyPem = default!;
    private Lazy<RSA?> _sitePublicRsa = default!;
    private Lazy<RSA?> _sitePrivateRsa = default!;
    public string? SiteActorPublicKeyPem => _sitePublicKeyPem.Value;
    public string? SiteActorPrivateKeyPem => _sitePrivateKeyPem.Value;
    public RSA? SiteActorRsaPublicKey => _sitePublicRsa.Value;
    public RSA? SiteActorRsaPrivateKey => _sitePrivateRsa.Value;
    public string SiteKeyFragment => "key-" + Convert.ToHexStringLower(SHA3_256.Create().ComputeHash(new MemoryStream(Encoding.UTF8.GetBytes(SiteActorPublicKeyPem ?? throw new ApageeException("Cannot get KeyId: Site Actor public key PEM is null.")))))[..12];
    public string SiteKeyId => $"https://{Config.PublicHostname}/actor#{SiteKeyFragment}";
    public string ActorPubPath(string user) => Path.Combine(Globals.KeyringDir, $"{user}.pem");
    public string ActorPrivPath(string user) => Path.Combine(Globals.KeyringDir, $"{user}.key");
    public string SiteActorPubPath() => Path.Combine(Globals.KeyringDir, $"site.pem");
    public string SiteActorPrivPath() => Path.Combine(Globals.KeyringDir, $"site.key");

    public KeypairHelper(GlobalConfiguration config)
    {
        Config = config;

        ReloadKeypairProperties();
    }
    
    public async Task TryCreateUserKeypair()
    {
        try
        {
            if (ActorPublicKeyPem is null && ActorPrivateKeyPem is not null
                || ActorPublicKeyPem is not null && ActorPrivateKeyPem is null)
            {
                throw new ApageeException("Cannot generate new actor keypair: Only one of the two pem/key pair files exists.");
            }
            else if (ActorPublicKeyPem is not null && ActorPrivateKeyPem is not null)
            {
                return;
            }

            using var rsa = RSA.Create(Globals.RSA_KEY_STRENGTH);

            var privateKey = rsa.ExportPkcs8PrivateKeyPem();
            var publicKey = rsa.ExportSubjectPublicKeyInfoPem();

            await File.WriteAllTextAsync(ActorPrivPath(Config.FediverseUsername), privateKey);
            await File.WriteAllTextAsync(ActorPubPath(Config.FediverseUsername), publicKey);

            ReloadKeypairProperties();

            Output.WriteLine($"{Output.Ansi.Blue} % Generated new HttpSig keypair for actor (saved to {Globals.KeyringDir}).");
        }
        catch (Exception ex)
        {
            Output.WriteLine($"{Output.Ansi.Red} % Error generating signing keypair: {ex.Message}");
        }
    }
    
    public async Task TryCreateSiteActorKeypair()
    {
        try
        {
            if (SiteActorPublicKeyPem is null && SiteActorPrivateKeyPem is not null
                || SiteActorPublicKeyPem is not null && SiteActorPrivateKeyPem is null)
            {
                throw new ApageeException("Cannot generate new site actor keypair: Only one of the two pem/key pair files exists.");
            }
            else if (SiteActorPublicKeyPem is not null && SiteActorPrivateKeyPem is not null)
            {
                return;
            }

            using var rsa = RSA.Create(Globals.RSA_KEY_STRENGTH);

            var privateKey = rsa.ExportPkcs8PrivateKeyPem();
            var publicKey = rsa.ExportSubjectPublicKeyInfoPem();

            await File.WriteAllTextAsync(SiteActorPrivPath(), privateKey);
            await File.WriteAllTextAsync(SiteActorPubPath(), publicKey);

            ReloadKeypairProperties();

            Output.WriteLine($"{Output.Ansi.Blue} % Generated new HttpSig keypair for site actor (saved to {Globals.KeyringDir}).");
        }
        catch (Exception ex)
        {
            Output.WriteLine($"{Output.Ansi.Red} % Error generating signing keypair for site actor: {ex.Message}");
        }
    }

    private void ReloadKeypairProperties()
    {
        _publicKeyPem = new Lazy<string?>(() =>
        {
            var path = ActorPubPath(Config.FediverseUsername);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }, true);

        _privateKeyPem = new Lazy<string?>(() =>
        {
            var path = ActorPrivPath(Config.FediverseUsername);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }, true);

        _publicRsa = new Lazy<RSA?>(() =>
        {
            var pem = ActorPublicKeyPem;
            if (pem is null) return null;
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }, true);

        _privateRsa = new Lazy<RSA?>(() =>
        {
            var pem = ActorPrivateKeyPem;
            if (pem is null) return null;
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }, true);
        
        _sitePublicKeyPem = new Lazy<string?>(() =>
        {
            var path = SiteActorPubPath();
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }, true);

        _sitePrivateKeyPem = new Lazy<string?>(() =>
        {
            var path = SiteActorPrivPath();
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }, true);

        _sitePublicRsa = new Lazy<RSA?>(() =>
        {
            var pem = SiteActorPublicKeyPem;
            if (pem is null) return null;
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }, true);

        _sitePrivateRsa = new Lazy<RSA?>(() =>
        {
            var pem = SiteActorPrivateKeyPem;
            if (pem is null) return null;
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }, true);
    }
}
