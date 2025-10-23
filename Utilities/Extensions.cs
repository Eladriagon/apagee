namespace Apagee.Utilities;

public static class ApageeExtensions
{
    /// <summary>
    /// Case-insensitive auto-trimmed string compare.
    /// Mostly because "StringComparison.InvariantCultureIgnoreCase" is way too long.
    /// </summary>
    public static bool IEquals(this string str, string other, bool noTrim = false) =>
        (noTrim ? str : str.Trim()).Equals((noTrim ? other : other.Trim()), StringComparison.InvariantCultureIgnoreCase);

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
}