namespace Apagee.Models.APub.Attributes;

/// <summary>
/// Adds Content-Type: <see cref="Globals.JSON_RD_CONTENT_TYPE" />
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class JrdContentTypeAttribute : Attribute { }