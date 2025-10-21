namespace Apagee.Models.APub;

public class PublicKey
{
    public string Id { get; set; } = default!;

    public string Owner { get; set; } = default!;

    public string PublicKeyPem { get; set; } = default!;
}