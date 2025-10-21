namespace Apagee;

/// <summary>
/// Wrapper for console output that disables or reroutes it if necessary.
/// Also supports ANSI colors 🌈 (ooooh, aaaah).
/// </summary>
public static class Output
{
    private static bool IsOutputEnabled { get; } = true;

    public static void WriteLine(string data)
    {
        if (IsOutputEnabled)
        {
            Console.WriteLine(data + Ansi.Reset);
        }
    }

    public static void Write(string data)
    {
        if (IsOutputEnabled)
        {
            Console.Write(data);
        }
    }

    /// <summary>
    /// ANSI color code reference strings.
    /// </summary>
    public static class Ansi
    {
        public static bool IsEnabled =>
            Environment.GetEnvironmentVariable("NO_COLOR") is null &&
            Environment.GetEnvironmentVariable("ANSI_OFF") is null &&
            !Console.IsOutputRedirected;

        public static string Reset => !IsEnabled ? "" : "\u001b[0m";

        // Text styles
        public static string Bold => !IsEnabled ? "" : "\u001b[1m";
        public static string Dim => !IsEnabled ? "" : "\u001b[2m";
        public static string Italic => !IsEnabled ? "" : "\u001b[3m";
        public static string Underline => !IsEnabled ? "" : "\u001b[4m";
        public static string Blink => !IsEnabled ? "" : "\u001b[5m";
        public static string Inverse => !IsEnabled ? "" : "\u001b[7m";
        public static string Hidden => !IsEnabled ? "" : "\u001b[8m";
        public static string Strikethrough => !IsEnabled ? "" : "\u001b[9m";

        // Foreground colors
        public static string Black => !IsEnabled ? "" : "\u001b[30m";
        public static string Red => !IsEnabled ? "" : "\u001b[31m";
        public static string Green => !IsEnabled ? "" : "\u001b[32m";
        public static string Yellow => !IsEnabled ? "" : "\u001b[33m";
        public static string Blue => !IsEnabled ? "" : "\u001b[34m";
        public static string Magenta => !IsEnabled ? "" : "\u001b[35m";
        public static string Cyan => !IsEnabled ? "" : "\u001b[36m";
        public static string White => !IsEnabled ? "" : "\u001b[37m";
        public static string Default => !IsEnabled ? "" : "\u001b[39m";

        // Background colors
        public static string BgBlack => !IsEnabled ? "" : "\u001b[40m";
        public static string BgRed => !IsEnabled ? "" : "\u001b[41m";
        public static string BgGreen => !IsEnabled ? "" : "\u001b[42m";
        public static string BgYellow => !IsEnabled ? "" : "\u001b[43m";
        public static string BgBlue => !IsEnabled ? "" : "\u001b[44m";
        public static string BgMagenta => !IsEnabled ? "" : "\u001b[45m";
        public static string BgCyan => !IsEnabled ? "" : "\u001b[46m";
        public static string BgWhite => !IsEnabled ? "" : "\u001b[47m";
        public static string BgDefault => !IsEnabled ? "" : "\u001b[49m";

        // Cursor control
        public static string CursorUp(int n = 1) => !IsEnabled ? "" : $"\u001b[{n}A";
        public static string CursorDown(int n = 1) => !IsEnabled ? "" : $"\u001b[{n}B";
        public static string CursorForward(int n = 1) => !IsEnabled ? "" : $"\u001b[{n}C";
        public static string CursorBack(int n = 1) => !IsEnabled ? "" : $"\u001b[{n}D";
        public static string CursorTo(int row, int col) => !IsEnabled ? "" : $"\u001b[{row};{col}H";
        public static string ClearScreen => !IsEnabled ? "" : "\u001b[2J";
        public static string ClearLine => !IsEnabled ? "" : "\u001b[2K";

        // 256-color and RGB (because you’re fancy)
        public static string Fg256(int color) => !IsEnabled ? "" : $"\u001b[38;5;{color}m";
        public static string Bg256(int color) => !IsEnabled ? "" : $"\u001b[48;5;{color}m";
        public static string FgRgb(int r, int g, int b) => !IsEnabled ? "" : $"\u001b[38;2;{r};{g};{b}m";
        public static string BgRgb(int r, int g, int b) => !IsEnabled ? "" : $"\u001b[48;2;{r};{g};{b}m";

        /// <summary>
        /// Strips ANSI escape codes from a string without regex.
        /// </summary>
        public static string StripAnsi(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            Span<char> buffer = stackalloc char[input.Length];
            int o = 0;
            bool inEscape = false;

            foreach (char c in input)
            {
                if (inEscape)
                {
                    if ((c >= '@' && c <= '~')) // ANSI sequence terminator
                        inEscape = false;
                    continue;
                }

                if (c == '\u001b')
                {
                    inEscape = true;
                    continue;
                }

                buffer[o++] = c;
            }

            return new string(buffer[..o]);
        }
    }
}