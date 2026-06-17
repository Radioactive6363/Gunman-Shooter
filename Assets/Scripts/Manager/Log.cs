public static class Log
{
    public static void Info(string msg)    => GameLogger.Instance?.Add(msg, LogType.Info);
    public static void Warning(string msg) => GameLogger.Instance?.Add(msg, LogType.Warning);
    public static void Error(string msg)   => GameLogger.Instance?.Add(msg, LogType.Error);
}