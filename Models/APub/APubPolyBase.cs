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

    public static implicit operator APubPolyBase(string[] other) => other.Select(o => new APubLink(o)).ToArray();

    [JsonIgnore]
    public bool HasOneOnlyLink => Count == 1 && this.First() is APubLink { IsOnlyLink: true };

    public override string ToString()
    {
        return this switch
        {
            APubPolyBase when Count == 0 => $"[PolyBase] Count = 0",
            APubPolyBase when Count == 1 && this.First() is APubLink { IsOnlyLink: true } ol => $"[PolyBase] [Single] <Only>{this.First().Type}: {ol.Href}",
            APubPolyBase when Count == 1 && this.First() is APubLink l => $"[PolyBase] [Single] {this.First().Type}: {l.Name} -- {l.Href}",
            APubPolyBase when Count == 1 && this.First() is APubObject o => $"[PolyBase] [Single] {this.First().Type}: {o.Id}",
            APubPolyBase when Count == 1 => $"[PolyBase] [Single] {this.First().Type}: {this.First()}",
            APubPolyBase when Count == this.GroupBy(a => a.Type).OrderByDescending(g => g.Count()).Count() => $"[PolyBase] [SameArray={Count}] {this.First().Type}",
            _ => $"[PolyBase] [Other?]",
        };
    }
}