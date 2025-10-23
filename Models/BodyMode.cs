namespace Apagee.Models;

public enum BodyMode
{
    /// <summary>
    /// Body is in plain text format.
    /// </summary>
    PlainText = 0,

    /// <summary>
    /// Body is in Markdown format.
    /// </summary>
    Markdown = 1,

    /// <summary>
    /// Body is in HTML format.
    /// </summary>
    HTML = 2
}