namespace Apagee.Models.APub.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MultiplePropertyAttribute(params string[] names) : Attribute
{
    public string[] Names { get; } = names;
}