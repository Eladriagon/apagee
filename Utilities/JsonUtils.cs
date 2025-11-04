using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Apagee.Utilities;

public static class JsonUtils
{
    /// <summary>
    /// Injects @context into the item by serializing it to a <see cref="JsonObject" /> first. Returns as-is if criteria is not met.
    /// </summary>
    public static JsonNode? ConvertWithContext(object? item, bool noContext = false)
    {
        if (item is null) return null;

        var node = JsonSerializer.SerializeToNode(item, item.GetType(), APubJsonOptions.GetOptions);

        if (node is not JsonObject obj || noContext || node["@context"] is not null)
        {
            // Non-object roots: do nothing
            return node;
        }

        // Build new root with "context" + original properties (cloned!)
        var root = new JsonObject
        {
            ["@context"] = JsonSerializer.SerializeToNode(ContextResponseWrapperFilter.APubGlobalContext)
        };

        foreach (var kvp in obj)
        {
            // Avoid re-parenting: clone the node before adding
            root[kvp.Key] = kvp.Value?.DeepClone();
        }
    
        return root;
    }
}