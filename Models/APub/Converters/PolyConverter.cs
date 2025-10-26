namespace Apagee.Models.APub.Converters;

public sealed class PolyConverter : JsonConverter<APubPolyBase>
{
    public override bool CanConvert(Type typeToConvert) => true;

    public override APubPolyBase? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions opts)
    {
        return JsonSerializer.Deserialize<APubPolyBase?>(reader.ValueSpan, opts);
    }

    public override void Write(Utf8JsonWriter writer, APubPolyBase value, JsonSerializerOptions opts)
    {
        if (value.Count == 1)
        {
            if (value[0] is APubLink { IsEmpty: true })
            {
                return;
            }
            else if (value[0] is APubLink { IsOnlyLink: true } ol)
            {
                JsonSerializer.Serialize(writer, ol.Href, ol.Href!.GetType(), opts);
            }
            else
            {
                if (value[0] is not null)
                {
                    APubJsonOptions.SerializeObject(writer, value[0], opts);
                }
            }
            return;
        }

        writer.WriteStartArray();

        foreach (var item in value)
        {
            APubJsonOptions.SerializeObject(writer, item, opts);
        }

        writer.WriteEndArray();
    }
    }
