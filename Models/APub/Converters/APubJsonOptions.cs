using Markdig.Extensions.Tables;

public static class APubJsonOptions
{
    public static Assembly ThisAssembly { get; } = Assembly.GetAssembly(typeof(APubJsonOptions)) ?? Assembly.GetExecutingAssembly();

    public static IEnumerable<Type> CachedTypes { get; } = ThisAssembly.GetTypes();

    public static JsonSerializerOptions GetOptions
    {
        get
        {
            var opts = new JsonSerializerOptions();
            OptionModifier(opts);
            return opts;
        }
    }

    public static Action<JsonSerializerOptions> OptionModifier => new(opt =>
    {
        opt.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opt.PropertyNameCaseInsensitive = true;
        opt.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.WriteIndented = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is not "Production";

        opt.Converters.Add(new APubDateConverter());
        opt.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        opt.Converters.Add(new APubConverterFactory());

        opt.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();

        opt.TypeInfoResolver.WithAddedModifier(ti =>
        {
            if (ti.Kind != JsonTypeInfoKind.Object) return;

            if (ti.Type == typeof(APubObject))
            {
                ti.PolymorphismOptions ??= new JsonPolymorphismOptions();
                ti.PolymorphismOptions.TypeDiscriminatorPropertyName = "type";

                foreach (var type in CachedTypes.Where(t => t.GetCustomAttribute<AutoDerivedTypeAttribute>() is not null))
                {
                    var inst = Activator.CreateInstance(type) as APubBase ?? throw new ApageeException("Found [AutoDerivedType] on something other than APubBase!");
                    ti.PolymorphismOptions.DerivedTypes.Add(new(type, inst.Type));
                }
            }

            foreach (var prop in ti.Properties.ToList())
            {
                if (prop.PropertyType == typeof(APubLink))
                {
                    prop.ShouldSerialize = (parent, self) => self is APubLink link && (!link.IsEmpty || link.BadHref is { Length: > 0 });
                }

                var member = prop.AttributeProvider as MemberInfo;
                var multi = member?
                    .GetCustomAttributes(typeof(MultiplePropertyAttribute), false)
                    .OfType<MultiplePropertyAttribute>()
                    .FirstOrDefault();

                if (multi is null || multi.Names.Length == 0) continue;

                var canonical = prop.Name;

                foreach (var alias in multi.Names.Distinct(StringComparer.Ordinal))
                {
                    if (string.Equals(alias, canonical, StringComparison.Ordinal)) continue;
                    if (ti.Properties.Any(p => string.Equals(p.Name, alias, StringComparison.Ordinal))) continue;

                    var aliasInfo = ti.CreateJsonPropertyInfo(prop.PropertyType, alias);

                    aliasInfo.Set = prop.Set;
                    aliasInfo.IsRequired = prop.IsRequired;
                    aliasInfo.NumberHandling = prop.NumberHandling;
                    aliasInfo.ObjectCreationHandling = prop.ObjectCreationHandling;
                    aliasInfo.AttributeProvider = prop.AttributeProvider;

                    aliasInfo.ShouldSerialize = static (_, _) => false;

                    ti.Properties.Add(aliasInfo);
                }
            }
        });
    });
   
    internal static void SerializeObject(Utf8JsonWriter writer, object? item, JsonSerializerOptions opts)
    {
        if (item is null) return;

        writer.WriteStartObject();
        foreach (var pi in item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var val = pi.GetValue(item);

            if (item == default || val == default)
            {
                continue;
            }

            if (pi.GetCustomAttribute<JsonIgnoreAttribute>() is not null) continue;

            var name = pi.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            writer.WritePropertyName(name ?? JsonNamingPolicy.CamelCase.ConvertName(pi.Name));

            var requiresArray = pi.GetCustomAttribute<AlwaysArrayAttribute>(true) is not null && val is APubPolyBase { Count: 1 } a;

            if (requiresArray)
            {
                writer.WriteStartArray();
            }

            if (val is APubPolyBase poly)
            {
                var polyConv = (JsonConverter<APubPolyBase>)opts.GetConverter(pi.PropertyType);
                polyConv.Write(writer, poly, opts);
            }
            else if (val is APubBase baseVal)
            {
                var baseConv = (JsonConverter<APubBase>)opts.GetConverter(pi.PropertyType);
                baseConv.Write(writer, baseVal, opts);
            }
            else
            {
                JsonSerializer.Serialize(writer, val, pi.PropertyType, opts);
            }

            if (requiresArray)
            {
                writer.WriteEndArray();
            }
        }
        writer.WriteEndObject();
    }
}