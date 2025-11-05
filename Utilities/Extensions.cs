namespace Apagee.Utilities;

public static class ApageeExtensions
{
    /// <summary>
    /// Case-insensitive auto-trimmed string compare.
    /// Mostly because "StringComparison.InvariantCultureIgnoreCase" is way too long.
    /// </summary>
    public static bool IEquals(this string str, string other, bool noTrim = false) =>
        (noTrim ? str : str.Trim()).Equals(noTrim ? other : other.Trim(), StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Truncates a string and optionally adds a suffix, such as "..." to the end if it was truncated.
    /// </summary>
    public static string? Truncate(this string? str, int length, string suffix = "...") =>
        str is null || str.Length <= length ? str : string.Concat(str.AsSpan(0, length), suffix);

    public static byte[] AsBase64Bytes(this string? str) =>
        str is null ? [] : Convert.FromBase64String(str);

    public static string AsBase64(this byte[] data) =>
        Convert.ToBase64String(data);

    /// <summary>
    /// Converts a long string into a short status note.
    /// </summary>
    public static string? ToStatusNote(this string? str)
    {
        if (str is null) return null;

        str = str.Trim().Replace("\r", "");

        if (str.Contains("\n\n"))
        {
            return str.Split("\n\n")[0].Truncate(400);
        }
        else
        {
            return str.Truncate(400);
        }
    }

    /// <summary>
    /// Takes only alphanumeric characters, replaces everything else with spaces, condenses multiple spaces to single hyphens,
    /// trims leading/trailing hyphens, and lowercases the result.
    /// </summary>
    /// <param name="str">The original string.</param>
    /// <returns>A URL slug-safe string.</returns>
    public static string ToUrlSlug(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        var sb = new StringBuilder();
        bool wasHyphen = false;

        foreach (char c in str)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
                wasHyphen = false;
            }
            else
            {
                if (!wasHyphen)
                {
                    sb.Append('-');
                    wasHyphen = true;
                }
            }
        }

        // Trim leading/trailing hyphens
        return sb.ToString().Trim('-');
    }

    public static async Task<string?> ReadToBase64(this IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Converts the base64 string into an inline data URI for use in an <img> tag.
    /// </summary>
    public static string? ToDataUri(this string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return null;

        if (Utils.IsPng(base64))
        {
            return $"data:image/png;base64,{base64}";
        }

        if (Utils.IsJpeg(base64))
        {
            return $"data:image/jpeg;base64,{base64}";
        }

        return null;
    }
}