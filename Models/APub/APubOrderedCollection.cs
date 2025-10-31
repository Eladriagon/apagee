namespace Apagee.Models.APub;

[AutoDerivedType]
public class APubOrderedCollection : APubCollection
{
    public override string Type => APubConstants.TYPE_COLLECTION_ORDERED;

    /// <summary>
    /// Can contain: <see cref="APubObject"/>, <see cref="APubLink"/>
    /// </summary>
    [AlwaysArray]
    public APubPolyBase? OrderedItems { get; set; }
}