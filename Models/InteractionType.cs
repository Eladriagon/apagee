namespace Apagee.Models;

/// <summary>
/// Specifies the ActivityPub interaction type.
/// </summary>
public enum InteractionType
{
    /// <summary>
    /// The item was announced / boosted / shared / etc.
    /// </summary>
    Announce,

    /// <summary>
    /// The item was liked / favorited / hearted / etc.
    /// </summary>
    Like,

    /// <summary>
    /// The item was replied to.
    /// </summary>
    Reply
}