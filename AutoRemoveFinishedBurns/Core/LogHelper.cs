using Brutal.Logging;

namespace AutoRemoveFinishedBurns.Core;

static class LogHelper
{
    private static readonly HashSet<string> _loggedWarnings = new();

    public static void WarnOnce(string key, string message)
    {
        if (_loggedWarnings.Add(key))
            DefaultCategory.Log.Warning(message);
    }

    public static void ErrorOnce(string key, string message)
    {
        if (_loggedWarnings.Add(key))
            DefaultCategory.Log.Error(message);
    }

    public static void Reset() => _loggedWarnings.Clear();
}
