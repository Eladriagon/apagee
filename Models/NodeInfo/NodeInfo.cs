namespace Apagee.Models.NodeInfo;

public class NodeInfoResult
{
    public required string Version { get; set; }
    public required NodeInfoSoftware Software { get; set; }
    public required List<string> Protocols { get; set; }
    public required NodeInfoUsage Usage { get; set; }
    public required NodeInfoServices Services { get; set; }
    public bool OpenRegistrations { get; set; }
    public required NodeMetadata Metadata { get; set; }
}

public class NodeInfoSoftware
{
    public required string Name { get; set; }
    public required string Version { get; set; }
}

public class NodeInfoServices
{
    public object[] Outbound { get; set; } = [];
    public object[] Inbound { get; set; } = [];
}

public class NodeInfoUsage
{
    public required NodeInfoUsageUsers Users { get; set; }
    public required uint LocalPosts { get; set; }
}

public class NodeInfoUsageUsers
{
    public uint? Total { get; set; }
    public uint? ActiveMonth { get; set; }
    public uint? ActiveHalfyear { get; set; }
}

public class NodeMetadata
{
    public required string NodeName { get; set; }
    public required string NodeDescription { get; set; }
}