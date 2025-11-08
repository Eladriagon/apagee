namespace Apagee.Models;

[Table(nameof(Inbox))]
public class Inbox
{
    [ExplicitKey]
    public required string ID { get; set; }

    public required string UID { get; set; }

    public string? Type { get; set; }

    public string? ContentType { get; set; }

    public string? RemoteServer { get; set; }

    public required DateTime ReceivedOn { get; set; }

    public required int BodySize { get; set; }

    public byte[]? Body { get; set; }

    [Write(false)]
    public string? BodyData
    {
        get => Body == null ? null : Encoding.UTF8.GetString(Body);
        set => Body = value == null ? null : Encoding.UTF8.GetBytes(value);
    }
}
