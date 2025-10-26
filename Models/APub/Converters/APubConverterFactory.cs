namespace Apagee.Models.APub.Converters;

public sealed class APubConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeof(APubPolyBase).IsAssignableFrom(typeToConvert)
        || typeof(APubBase).IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeof(APubPolyBase).IsAssignableFrom(typeToConvert))
        {
            return new PolyConverter();
        }
        else if (typeof(APubBase).IsAssignableFrom(typeToConvert))
        {
            return new BaseConverter();   
        }

        throw new NotSupportedException($"Unsupported target type {typeToConvert}");
    }
}