namespace Apagee.Utilities;

public static class DebugHelper
{
    private static bool IS_DEBUG_CONSOLE_ENABLED => false;

    public static async Task LogResponse(HttpResponseMessage resp)
    {
        if (!IS_DEBUG_CONSOLE_ENABLED) return;

        try
        {
            Console.WriteLine($"""
                [DBG-RESP]
                HTTP {(int)resp.StatusCode} {resp.StatusCode}
                {string.Join("\r\n", resp.Headers.Select(static h => $"{h.Key}: {string.Join(", ", h.Value)}"))}

                """);
            if ((await resp.Content.ReadAsStringAsync()) is { Length: > 0 } body)
            {
                Console.WriteLine(body);
            }
            else
            {
                Console.WriteLine("[[[ NO RESP BODY ]]]");
            }
            Console.WriteLine("\r\n[/DBG-RESP]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DBG-ERR] Error in debug block: {ex.GetType().FullName}: {ex.Message}\r\n{ex.StackTrace}\r\n");
        }
    }

    public static async Task LogRequest(HttpRequestMessage request)
    {
        if (!IS_DEBUG_CONSOLE_ENABLED) return;

        try
        {
            Console.WriteLine("");
            Console.WriteLine($"[[[ DBG ]]]");
            Console.WriteLine(" ");
            Console.WriteLine($"{request.Method.Method} {request.RequestUri} HTTP/1.1");
            foreach (var h in request.Headers)
            {
                Console.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");
            }
            Console.WriteLine(" ");
            Console.WriteLine($"{(request.Content is not null ? await request.Content.ReadAsStringAsync() : "### Body has no content. ###")}");
            Console.WriteLine(" ");
            Console.WriteLine($"[[[ /DBG ]]]");
            Console.WriteLine("");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DBG-ERR] Error in debug block: {ex.GetType().FullName}: {ex.Message}\r\n{ex.StackTrace}\r\n");
        }
    }

    public static void LogInboxActivity(Inbox? item)
    {
        if (!IS_DEBUG_CONSOLE_ENABLED) return;

        Console.WriteLine($"[⁂] ----\r\n{JsonSerializer.Serialize(item)}\r\n[⁂] ----\r\n");
    }
}