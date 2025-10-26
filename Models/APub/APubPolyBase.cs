namespace Apagee.Models.APub;

public class APubPolyBase : List<APubBase>
{
    public static implicit operator APubPolyBase?(string? uri) => uri is null ? null : [new APubLink() {
        Href = Uri.TryCreate(uri, UriKind.Absolute, out var link) ? link : null,
        BadHref = link is null ? uri : null
    }];

    public static implicit operator APubPolyBase(Uri uri) => [new APubLink(uri)];

    public static implicit operator APubPolyBase?(APubBase? other) => other is null ? null : [other];

    public static implicit operator APubPolyBase(APubBase[] other) => [.. other];

    [JsonIgnore]
    public bool HasOneOnlyLink => Count == 1 && this.First() is APubLink { IsOnlyLink: true };

    public override string ToString()
    {
        switch (this)
        {
            case APubPolyBase when Count == 0:
                return $"[PolyBase] Count = 0";
            case APubPolyBase when Count == 1 && this.First() is APubLink { IsOnlyLink: true } ol:
                return $"[PolyBase] [Single] <Only>{this.First().Type}: {ol.Href}";
            case APubPolyBase when Count == 1 && this.First() is APubLink l:
                return $"[PolyBase] [Single] {this.First().Type}: {l.Name} -- {l.Href}";
            case APubPolyBase when Count == 1 && this.First() is APubObject o:
                return $"[PolyBase] [Single] {this.First().Type}: {o.Id}";
            case APubPolyBase when Count == 1:
                return $"[PolyBase] [Single] {this.First().Type}: {this.First()}";
            case APubPolyBase when Count == this.GroupBy(a => a.Type).OrderByDescending(g => g.Count()).Count():
                return $"[PolyBase] [SameArray={Count}] {this.First().Type}";
            default:
                return $"[PolyBase] [Other?]";
        }
    }
}