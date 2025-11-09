
using System.Collections;
using Markdig.Extensions.Diagrams;

namespace Apagee.Utilities;

public static class Utils
{
    private static MarkdownPipeline MarkdownConfig =>
        new MarkdownPipelineBuilder()
            .UseEmphasisExtras()
            .UseGenericAttributes()
            .UseAutoIdentifiers()
            .UsePipeTables()
            .UseFootnotes()
            .UseAutoLinks()
            .UseMediaLinks()
            .UseAlertBlocks()
            .Build();

    public static string MarkdownToHtml(string md)
    {
        var doc = Markdig.Markdown.Parse(md, MarkdownConfig);

        foreach (var para in doc.Descendants<ParagraphBlock>())
        {
            if (para.Inline is null) continue;

            bool isSingleImageParagraph = IsSingleImageParagraph(para);

            if (!isSingleImageParagraph)
            {
                foreach (var img in InlineDescendants<LinkInline>(para.Inline).Where(li => li.IsImage))
                {
                    var attrs = img.GetAttributes();
                    attrs.AddClass("img-inline");
                }
            }
        }
        return Markdig.Markdown.ToHtml(doc, MarkdownConfig);
    }
    
    /// <summary>
    /// Extracts a list of tags from a string, e.g. "this is a #string with some #hash-tags" returns <c>["string", "hash"]</c>.
    /// </summary>
    public static IEnumerable<string> ExtractTags(string input)
        => input is not { Length: > 0 } ? [] : Globals.RgxTagExtractor()
                                                      .Matches(input)
                                                      .Select(m => m.Groups["tag"].Value);

    public static bool IsPng(string? b64)
    {
        if (string.IsNullOrWhiteSpace(b64))
            return false;

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(b64);
        }
        catch
        {
            return false;
        }

        // PNG magic number: 89 50 4E 47 0D 0A 1A 0A
        return bytes.Length >= 8 &&
               bytes[0] == 0x89 && bytes[1] == 0x50 &&
               bytes[2] == 0x4E && bytes[3] == 0x47 &&
               bytes[4] == 0x0D && bytes[5] == 0x0A &&
               bytes[6] == 0x1A && bytes[7] == 0x0A;
    }

    public static bool IsJpeg(string? b64)
    {
        if (string.IsNullOrWhiteSpace(b64))
            return false;

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(b64);
        }
        catch
        {
            return false;
        }

        // JPEG magic numbers: FF D8 (SOI) ... FF D9 (EOI)
        return bytes.Length >= 4 &&
               bytes[0] == 0xFF && bytes[1] == 0xD8 &&
               bytes[^2] == 0xFF && bytes[^1] == 0xD9;
    }

    public static bool IsIco(string? b64)
    {
        if (string.IsNullOrWhiteSpace(b64))
            return false;

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(b64);
        }
        catch
        {
            return false;
        }

        // ICO header: 00 00 01 00
        return bytes.Length >= 4 &&
               bytes[0] == 0x00 && bytes[1] == 0x00 &&
               bytes[2] == 0x01 && bytes[3] == 0x00;
    }

    // Helpers for Markdig extension

        private static bool IsSingleImageParagraph(ParagraphBlock para)
    {
        var root = para.Inline;
        if (root is null) return false;

        int imageCount = 0;
        int nonWhitespaceNonBreakCount = 0;

        for (Inline? cur = root.FirstChild; cur is not null; cur = cur.NextSibling)
        {
            switch (cur)
            {
                case LineBreakInline:
                    // ignore soft/hard breaks
                    break;

                case LiteralInline lit:
                    // ignore pure whitespace
                    if (!string.IsNullOrWhiteSpace(lit.Content.ToString()))
                        nonWhitespaceNonBreakCount++;
                    break;

                case LinkInline li when li.IsImage:
                    imageCount++;
                    break;

                default:
                    // any other inline (emphasis, code, links, etc.) means it's not "on its own line"
                    nonWhitespaceNonBreakCount++;
                    break;
            }
        }

        return imageCount == 1 && nonWhitespaceNonBreakCount == 0;
    }

    private static IEnumerable<T> InlineDescendants<T>(ContainerInline root) where T : Inline
    {
        for (Inline? cur = root.FirstChild; cur is not null; cur = cur.NextSibling)
        {
            if (cur is T hit) yield return hit;

            if (cur is ContainerInline nested)
            {
                foreach (var child in InlineDescendants<T>(nested))
                    yield return child;
            }
        }
    }
}