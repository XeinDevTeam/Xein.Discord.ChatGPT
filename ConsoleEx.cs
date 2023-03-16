namespace Xein.Discord.ChatGPT;

public enum ConsoleType
{
    Normal,
    Debug,
    Warn,
    Error,
}

public class ConsoleItem
{
    public ConsoleItem(string msg, ConsoleType type)
    {
        switch (type)
        {
            case ConsoleType.Normal:
                System.Console.ForegroundColor = ConsoleColor.White;
                break;
            case ConsoleType.Debug:
                System.Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case ConsoleType.Warn:
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case ConsoleType.Error:
                System.Console.ForegroundColor = ConsoleColor.Red;
                break;
        }

        System.Console.WriteLine(msg);
        
        System.Console.ForegroundColor = ConsoleColor.White;
    }
}

public static class Console
{
    private static string GetTime() => DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss:fff tt");

    public static void Log(string msg) => new ConsoleItem($"[{GetTime()}] {msg}", ConsoleType.Normal);
    public static void Debug(string msg) => new ConsoleItem($"[{GetTime()}] [DEBUG] {msg}", ConsoleType.Debug);
    public static void Warn(string msg) => new ConsoleItem($"[{GetTime()}] [WARN] {msg}", ConsoleType.Warn);
    public static void Error(string msg) => new ConsoleItem($"[{GetTime()}] [ERROR] {msg}", ConsoleType.Error);
}
