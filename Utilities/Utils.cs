
namespace Apagee.Utilities;

public static class Utils
{
    private static MarkdownPipeline MarkdownConfig =>
        new MarkdownPipelineBuilder()
            .UseAlertBlocks()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseGenericAttributes()
            .UseAutoIdentifiers()
            .UseFootnotes()
            .UseAutoLinks()
            .UseMediaLinks()
            .UseDiagrams()
            .UseColorCode()
            .Build();

    public static string MarkdownToHtml(string md)
    {
        return Markdig.Markdown.ToHtml(md, MarkdownConfig);
    }

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
}