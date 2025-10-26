namespace Apagee.Models;

public class Inbox
{
    public required string ID { get; set; }

    public required string UID { get; set; }

    public string? Type { get; set; }

    public string? ContentType { get; set; }

    public string? RemoteServer { get; set; }

    public required DateTime ReceivedOn { get; set; }

    public required int BodySize { get; set; }

    public byte[]? Blob { get; set; }

    [Write(false)]
    public string? BlobData
    {
        get => Blob == null ? null : Encoding.UTF8.GetString(Blob);
        set => Blob = value == null ? null : Encoding.UTF8.GetBytes(value);
    }
}
