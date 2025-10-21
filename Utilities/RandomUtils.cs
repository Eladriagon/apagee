namespace Apagee.Utilities;

public static class RandomUtils
{
    /// <summary>
    /// Gets a random string of <paramref name="length" /> characters containing A-Z a-z 0-9.
    /// </summary>
    public static string GetRandomAlphanumeric(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new char[length];
        var buffer = new byte[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[buffer[i] % chars.Length];
        }

        return new string(result);
    }
}
