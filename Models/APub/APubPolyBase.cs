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
}