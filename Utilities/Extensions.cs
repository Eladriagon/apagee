namespace Apagee.Utilities;

public static class ApageeExtensions
{
    /// <summary>
    /// Case-insensitive auto-trimmed string compare.
    /// Mostly because "StringComparison.InvariantCultureIgnoreCase" is way too long.
    /// </summary>
    public static bool IEquals(this string str, string other, bool noTrim = false) =>
        (noTrim ? str : str.Trim()).Equals((noTrim ? other : other.Trim()), StringComparison.InvariantCultureIgnoreCase);
}