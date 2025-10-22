using Microsoft.Win32;

namespace TrackTime;

internal static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Only adds it to the autostart if its not already in there
    /// </summary>
    public static void AddToStartup()
    {
        if (IsInStartup())
        { 
            return; 
        }

        if (OperatingSystem.IsWindows())
        {
            string appName = AppDomain.CurrentDomain.FriendlyName;
            string exePath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine executable path.");

            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                ?? throw new InvalidOperationException("Unable to open registry key.");

            key.SetValue(appName, $"\"{exePath}\"");
        }
    }

    public static void RemoveFromStartup()
    {
        if (OperatingSystem.IsWindows())
        {
            string appName = AppDomain.CurrentDomain.FriendlyName;

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                ?? throw new InvalidOperationException("Unable to open registry key.");

            if (key?.GetValue(appName) is not null)
                key.DeleteValue(appName);  
        }
    }

    public static bool IsInStartup()
    {
        if (OperatingSystem.IsWindows())
        {
            string appName = AppDomain.CurrentDomain.FriendlyName;

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            if (key?.GetValue(appName) is not string exePath)
            {
                return false;
            }

            if (exePath == Environment.ProcessPath)
            {
                return true;
            }

            RemoveFromStartup(); //Path is obsolete
            return false;
        }

        return false;
    }
}
