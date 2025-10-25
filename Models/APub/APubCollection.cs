namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubCollection : APubObject
{
    public override string Type => APubConstants.TYPE_COLLECTION;
            
    public uint? TotalItems { get; set; }

    /// <summary>
    /// Can be a collection of one of: <see cref="APubObject"/>, <see cref="APubLink"/>
    /// </summary>
    public APubPolyBase? Items { get; set; }

    /// <sumary>
    /// Can be one of: <see cref="APubCollectionPage"/>, <see cref="APubLink"/>
    /// </summary>
    public APubPolyBase? Current { get; set; }

    /// <summary>
    /// Can be one of: <see cref="APubCollectionPage"/>, <see cref="APubLink"/>
    /// </summary>
    public APubPolyBase? First { get; set; }

    /// <summary>
    /// Can be one of: <see cref="APubCollectionPage"/>, <see cref="APubLink"/>
    /// </summary>
    public APubPolyBase? Last { get; set; }


    // Helpers

    [JsonIgnore]
    public APubLink? CurrentLink => Current?.SingleOrDefault() as APubLink;

    [JsonIgnore]
    public APubLink? FirstLink => First?.SingleOrDefault() as APubLink;
    
    [JsonIgnore]
    public APubLink? LastLink => Last?.SingleOrDefault() as APubLink;
}