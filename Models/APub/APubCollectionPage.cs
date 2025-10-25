namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubCollectionPage : APubCollection
{
    public override string Type => APubConstants.TYPE_COLLECTION_PAGE;

    /// <summary>
    /// Can be one of: <see cref="APubCollection"/>, <see cref="APubLink"/>
    /// </summary>
    public APubBase? PartOf { get; set; }

    /// <summary>
    /// Can be one of: <see cref="APubCollectionPage"/>, <see cref="APubLink"/>
    /// </summary>
    public APubBase? Next { get; set; }

    /// <summary>
    /// Can be one of: <see cref="APubCollectionPage"/>, <see cref="APubLink"/>
    /// </summary>
    public APubBase? Prev { get; set; }

    // Helpers

    [JsonIgnore]
    public APubLink? PartOfLink => PartOf as APubLink;

    [JsonIgnore]
    public APubLink? NextLink => Next as APubLink;
    
    [JsonIgnore]
    public APubLink? PrevLink => Prev as APubLink;
}