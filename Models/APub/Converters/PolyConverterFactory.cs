namespace Apagee.Models.APub.Converters;

public sealed class PolyConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => IsLinkish(typeToConvert) || IsLinkishCollection(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (IsLinkish(typeToConvert))
        {
            var convType = typeof(SingleConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(convType)!;
        }

        // Forward APubPolyBase -> List<APubBase>
        if (typeToConvert == typeof(APubPolyBase))
        {
            return (JsonConverter)Activator.CreateInstance(typeof(PolyToListConverter))!;
        }

        // Collections
        if (typeToConvert.IsArray)
        {
            var elem = typeToConvert.GetElementType()!;
            var convType = typeof(ArrayConverter<>).MakeGenericType(elem);
            return (JsonConverter)Activator.CreateInstance(convType)!;
        }

        if (IsGenericList(typeToConvert, out var elemType))
        {
            var convType = typeof(ListConverter<>).MakeGenericType(elemType!);
            return (JsonConverter)Activator.CreateInstance(convType)!;
        }

        throw new NotSupportedException($"Unsupported target type {typeToConvert}");
    }

    // --- Helpers -------------------------------------------------------------

    static bool IsLinkish(Type t)
        => t == typeof(APubLink) || t == typeof(APubBase);

    static bool IsLinkishCollection(Type t)
        => t == typeof(APubPolyBase)
           || (t.IsArray && IsLinkish(t.GetElementType()!))
           || (IsGenericList(t, out var elem) && IsLinkish(elem!));

    static bool IsGenericList(Type t, out Type? elem)
    {
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
        {
            elem = t.GetGenericArguments()[0];
            return true;
        }
        elem = null;
        return false;
    }

    static JsonSerializerOptions StripSelf(JsonSerializerOptions options)
    {
        var clone = new JsonSerializerOptions(options)
        {
            // make sure we keep the exact ignore + resolver behavior
            DefaultIgnoreCondition = options.DefaultIgnoreCondition,
            TypeInfoResolver = options.TypeInfoResolver,
            PropertyNamingPolicy = options.PropertyNamingPolicy,
            Encoder = options.Encoder,
            WriteIndented = options.WriteIndented,
            PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive
        };
        var self = clone.Converters.FirstOrDefault(c => c is PolyConverterFactory);
        if (self is not null) clone.Converters.Remove(self);
        return clone;
    }

    // --- Core logic reused by all flavors -----------------------------------

    static APubBase ReadOne(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var iri = reader.GetString();

            return Uri.TryCreate(iri, UriKind.Absolute, out var uri)
                ? new APubLink { Href = uri }
                : new APubLink { BadHref = iri };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Let STJ populate APubLink/APubBase normally (by "type" etc.)
            using var doc = JsonDocument.ParseValue(ref reader);
            var raw = doc.RootElement.GetRawText();

            // Try APubLink first; if caller expects APubBase it still fits.
            var clean = StripSelf(options);
            var link = JsonSerializer.Deserialize<APubLink>(raw, clean);
            if (link is not null) return link;

            // Fallback: APubBase (in case you have custom subtypes registered elsewhere)
            var baseObj = JsonSerializer.Deserialize<APubBase>(raw, clean);
            if (baseObj is not null) return baseObj;
        }

        throw new JsonException("Expected Uri or object for APub value.");
    }

    // --- Single value converters --------------------------------------------

    private sealed class SingleConverter<T> : JsonConverter<T> where T : APubBase
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = ReadOne(ref reader, options);
            return (T)val; // APubLink : APubBase makes this safe for both T=APubLink and T=APubBase
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var clean = StripSelf(options);

            // Special cases
            if (value is APubLink link && link.Href is not null)
            {
                JsonSerializer.Serialize(writer, link.Href, link.Href.GetType(), clean);
            }
            else
            {
                JsonSerializer.Serialize(writer, value, value.GetType(), clean);
            }

        }
    }

    // --- Forward APubPolyBase to List<APubBase> --------------------------------------------

    private sealed class PolyToListConverter : JsonConverter<APubPolyBase>
    {
        public override APubPolyBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = ReadOne(ref reader, options);
            return (APubPolyBase?)val; // APubLink : APubBase makes this safe for both T=APubLink and T=APubBase
        }

        public override void Write(Utf8JsonWriter writer, APubPolyBase value, JsonSerializerOptions options)
        {
            if (value.Count == 1 && value.Single() is APubLink link && link.Href is not null)
            {
                Console.WriteLine("===> Path 1 (One link > string)");
                JsonSerializer.Serialize(writer, link.Href, link.Href.GetType(), options);
            }
            else if (value.Count > 0 && value.All(v => v is APubLink l && l.Href is not null))
            {
                Console.WriteLine("===> Path 2 (Many links > string[])");
                JsonSerializer.Serialize(writer, value.OfType<APubLink>().Select(l => l.Href), typeof(string[]), options);
            }
            else if(value.Count == 1)
            {
                Console.WriteLine("===> Path 3 (One object > object)");
                JsonSerializer.Serialize(writer, value.First(), value.First().GetType(), options);
            }
            else
            {
                Console.WriteLine("===> Path 4 (all)");
                writer.WriteStartArray();
                
                foreach (var val in value)
                {
                    JsonSerializer.Serialize(writer, val, val.GetType(), options);
                }

                writer.WriteEndArray();
            }
        }
    }

    // --- List<TElement> converters ------------------------------------------

    private sealed class ListConverter<TElement> : JsonConverter<List<TElement>> where TElement : APubBase
    {
        public override List<TElement>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new List<TElement>();

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray) break;
                    var item = (TElement)ReadOne(ref reader, options);
                    result.Add(item);
                }
                return result;
            }

            // Single → wrap into list
            var single = (TElement)ReadOne(ref reader, options);
            result.Add(single);
            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<TElement> value, JsonSerializerOptions options)
        {
            var clean = StripSelf(options);

            if (value.Count == 1 && value[0] is APubLink link)
            {
                if (link.Href is not null || link.BadHref is not null)
                {
                    writer.WriteStringValue(link.Href?.ToString() ?? link.BadHref);
                }
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in value)
                {
                    JsonSerializer.Serialize(writer, item, item?.GetType() ?? typeof(TElement), clean);
                }
                writer.WriteEndArray();
            }
        }
    }

    // --- TElement[] converters ----------------------------------------------

    private sealed class ArrayConverter<TElement> : JsonConverter<TElement[]> where TElement : APubBase
    {
        public override TElement[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var list = new List<TElement>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray) break;
                    list.Add((TElement)ReadOne(ref reader, options));
                }
                return list.ToArray();
            }

            // Single → 1-element array
            return new[] { (TElement)ReadOne(ref reader, options) };
        }

        public override void Write(Utf8JsonWriter writer, TElement[] value, JsonSerializerOptions options)
        {
            var clean = StripSelf(options);

            // 1-element link array > string
            if (value.Length == 1 && value[0] is APubLink link)
            {
                writer.WriteStringValue(link.Href?.ToString() ?? link.BadHref);
            }
            else
            {
                writer.WriteStartArray();

                foreach (var item in value)
                {
                    JsonSerializer.Serialize(writer, (object)item, item?.GetType() ?? typeof(TElement), clean);
                }

                writer.WriteEndArray();
            }
        }
    }
}