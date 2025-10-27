namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubOrderedCollectionPage : APubCollectionPage
{
    public override string Type => APubConstants.TYPE_COLLECTION_PAGE_ORDERED;

    public uint? StartIndex { get; set; }

    /// <summary>
    /// Can contain: <see cref="APubObject"/>, <see cref="APubLink"/>
    /// </summary>
    [AlwaysArray]
    public APubPolyBase? OrderedItems { get; set; }
}