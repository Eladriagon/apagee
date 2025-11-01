namespace Apagee.Models.APub.Attributes;

/// <summary>
/// Removes @context from response.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class NoContextAttribute : Attribute { }