namespace Apagee.Models.APub.Attributes;

/// <summary>
/// Adds Content-Type: <custom value>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ExplicitContentTypeAttribute(string Name) : Attribute
{
    public string Name { get; } = Name;
}