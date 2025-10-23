namespace Apagee.Models;

public enum ArticleStatus
{
    /// <summary>
    /// Article has not been published yet.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Article has been published.
    /// </summary>
    Published = 1,

    /// <summary>
    /// Article has been archived. Different from draft because at one point it was publicly visible.
    /// (Not currently used.)
    /// </summary>
    Archived = 2
}