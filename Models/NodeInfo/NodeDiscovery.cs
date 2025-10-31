namespace Apagee.Models.NodeInfo;

public class NodeDiscoveryCollection
{
    public required List<NodeDiscovery> Links { get; set; }
}

public class NodeDiscovery
{
    public required string Rel { get; set; }
    public required string Href { get; set; }
}