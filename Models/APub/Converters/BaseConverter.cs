namespace Apagee.Models.APub.Converters;

public sealed class BaseConverter : JsonConverter<APubBase>
{
    public override bool CanConvert(Type typeToConvert) => typeof(APubPolyBase).IsAssignableFrom(typeToConvert);

    public override APubBase? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions opts)
    {
        return JsonSerializer.Deserialize<APubBase?>(reader.ValueSpan, opts);
    }

    public override void Write(Utf8JsonWriter writer, APubBase value, JsonSerializerOptions opts)
    {
        if (value is APubLink { IsEmpty: true })
        {
            return;
        }
        else if (value is APubLink { IsOnlyLink: true } ol)
        {
            JsonSerializer.Serialize(writer, ol.Href, ol.Href!.GetType(), opts);
        }
        else
        {
            APubJsonOptions.SerializeObject(writer, value, opts);
        }
        return;
    }
}
