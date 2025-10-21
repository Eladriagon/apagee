

namespace Apagee.Services;

public sealed class SecurityHelper
{
    IDataProtector Protector { get; }

    private string KeyEncryptor { get; }

    public SecurityHelper(IDataProtectionProvider dp)
    {
        Protector = dp.CreateProtector(Globals.KEYRING_NAME);

        var encKeyFile = Path.Combine(Globals.KeyringDir, Globals.ENC_KEY_ID);
        if (!File.Exists(encKeyFile))
        {
            KeyEncryptor = RandomUtils.GetRandomAlphanumeric(64);
            File.WriteAllText(encKeyFile, Protector.Protect(KeyEncryptor));
            Output.WriteLine($"{Output.Ansi.Blue} $ Wrote new key encryptor to: {Globals.KeyringDir}");
        }
        else
        {
            KeyEncryptor = Protector.Unprotect(File.ReadAllText(encKeyFile));
        }
    }

    public string Encrypt(string text) =>
        Convert.ToBase64String(((Func<byte[]>)(() =>
        {
            using var a = Aes.Create();
            a.Mode = CipherMode.ECB;
            a.Padding = PaddingMode.PKCS7;
            a.Key = SHA256.HashData(Encoding.UTF8.GetBytes(KeyEncryptor));
            using var c = a.CreateEncryptor();
            var p = Encoding.UTF8.GetBytes(text);
            return c.TransformFinalBlock(p, 0, p.Length);
        }))());

    public string Decrypt(string data) =>
        Encoding.UTF8.GetString(((Func<byte[]>)(() =>
        {
            using var a = Aes.Create();
            a.Mode = CipherMode.ECB;
            a.Padding = PaddingMode.PKCS7;
            a.Key = SHA256.HashData(Encoding.UTF8.GetBytes(KeyEncryptor));
            using var c = a.CreateDecryptor();
            var ct = Convert.FromBase64String(data);
            return c.TransformFinalBlock(ct, 0, ct.Length); 
        }))());
    
    public string Hash(string data)
    {
        return Convert.ToHexStringLower(SHA3_256.HashData(Encoding.UTF8.GetBytes(data ?? throw new ApageeException("SecurityHelper.Hash: value provided was null."))));
    }

    public string Hash(string data, string salt)
    {
        return Convert.ToHexStringLower(SHA3_256.HashData(Encoding.UTF8.GetBytes($"{data ?? throw new ApageeException("SecurityHelper.Hash: value provided was null.")}\x1E{salt}")));
    }

    public bool HashMatch(string hash, string dataToTry, string? salt = null)
    {
        return hash == Convert.ToHexStringLower(SHA3_256.HashData(Encoding.UTF8.GetBytes($"{dataToTry}{(salt is { Length: > 0 } ? $"\x1E{salt}" : "")}")));
    }
}