using Markdig.Extensions.Tables;

public static class APubJsonOptions
{
    public static Assembly ThisAssembly { get; } = Assembly.GetAssembly(typeof(APubJsonOptions)) ?? Assembly.GetExecutingAssembly();

    public static IEnumerable<Type> CachedTypes { get; } = ThisAssembly.GetTypes();

    public static Action<JsonSerializerOptions> OptionModifier => new(opt =>
    {
        opt.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opt.PropertyNameCaseInsensitive = true;
        opt.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.WriteIndented = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is not "Production";
        opt.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        // Runs only once per distinct type
        opt.TypeInfoResolver = opt.TypeInfoResolver.WithAddedModifier(ti =>
        {
            if (ti.Kind != JsonTypeInfoKind.Object) return;

            // 1) If this is your polymorphic base, move the discriminator off "type"
            if (ti.Type == typeof(APubObject))
            {
                ti.PolymorphismOptions ??= new JsonPolymorphismOptions();
                ti.PolymorphismOptions.TypeDiscriminatorPropertyName = "type";

                foreach (var type in CachedTypes.Where(t => t.GetCustomAttribute<AutoDerivedTypeAttribute>() is not null))
                {
                    // TODO: We can move this to the attribute to avoid a new-up
                    var inst = Activator.CreateInstance(type) as APubBase ?? throw new ApageeException("Found [AutoDerivedType] on something other than APubBase!");
                    ti.PolymorphismOptions.DerivedTypes.Add(new(type, inst.Type));
                }
            }

            // 2) Add read-only aliases for MultiplePropertyAttribute
            foreach (var prop in ti.Properties.ToList())
            {
                // "Should Serialize" logic
                // 1. APubLink
                if (prop.PropertyType == typeof(APubLink))
                {
                    prop.ShouldSerialize = (parent, self) => self is APubLink link && (link.Href is not null || link.BadHref is { Length: > 0 });
                }

                var member = prop.AttributeProvider as MemberInfo;
                var multi = member?
                    .GetCustomAttributes(typeof(MultiplePropertyAttribute), false)
                    .OfType<MultiplePropertyAttribute>()
                    .FirstOrDefault();

                if (multi is null || multi.Names.Length == 0) continue;

                var canonical = prop.Name; // JSON name after naming policy

                foreach (var alias in multi.Names.Distinct(StringComparer.Ordinal))
                {
                    if (string.Equals(alias, canonical, StringComparison.Ordinal)) continue;
                    if (ti.Properties.Any(p => string.Equals(p.Name, alias, StringComparison.Ordinal))) continue;

                    var aliasInfo = ti.CreateJsonPropertyInfo(prop.PropertyType, alias);

                    // read-only alias: used only for deserialization
                    aliasInfo.Set = prop.Set;
                    aliasInfo.IsRequired = prop.IsRequired;
                    aliasInfo.NumberHandling = prop.NumberHandling;
                    aliasInfo.ObjectCreationHandling = prop.ObjectCreationHandling;
                    aliasInfo.AttributeProvider = prop.AttributeProvider;

                    aliasInfo.ShouldSerialize = static (_, _) => false;   // never emit alias

                    ti.Properties.Add(aliasInfo);
                }
            }
        });

        opt.Converters.Add(new APubDateConverter());
        opt.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        opt.Converters.Add(new APubSingleArrayConverter<string>());
        opt.Converters.Add(new PolyConverterFactory());
    });
}