using System.Diagnostics;
using System.Runtime.InteropServices;
using TrackTime.Sqlite;

namespace TrackTime;

internal static class Tracker
{
    private static List<string> _processesToTrack = [];

    internal static void StartTracking(List<string> processNames)
    {
        _processesToTrack = processNames;
        _ = Task.Run(UpdateTrackedTimeAsync);
    }

    internal static void AddProcess(string processName)
    {
        _processesToTrack.Add(processName.Trim());
    }

    internal static bool RemoveProcess(string processName)
        => _processesToTrack.Remove(processName);


    private static async Task UpdateTrackedTimeAsync()
    {
        TimeSpan updateInterval = TimeSpan.FromSeconds(3);
        TimeSpan dbUpdateInterval = TimeSpan.FromSeconds(30);
        DateTime lastDbUpdate = DateTime.UtcNow;

        Dictionary<string, (TimeSpan overall, TimeSpan foreground)> trackedDeltas = [];

        while (true)
        {
            try
            {
                await Task.Delay(updateInterval);

                IntPtr hWnd = GetForegroundWindow();
                _ = GetWindowThreadProcessId(hWnd, out uint processId);

                string? foregroundWindowName = null;
                if (hWnd != IntPtr.Zero)
                {
                    try
                    {
                        foregroundWindowName = Process.GetProcessById((int)processId).ProcessName;
                    }
                    catch { /*Process might be closed mid process*/ } 
                }

                foreach (string processName in _processesToTrack)
                {
                    if (Process.GetProcessesByName(processName).Length == 0)
                        continue;

                    bool isForeground = processName.Equals(foregroundWindowName, StringComparison.OrdinalIgnoreCase);

                    if (!trackedDeltas.ContainsKey(processName))
                        trackedDeltas[processName] = (TimeSpan.Zero, TimeSpan.Zero);

                    (TimeSpan overall, TimeSpan foreground) delta = trackedDeltas[processName];
                    delta.overall += updateInterval;

                    if (isForeground)
                        delta.foreground += updateInterval;

                    trackedDeltas[processName] = delta;
                }

                if (DateTime.UtcNow - lastDbUpdate >= dbUpdateInterval)
                {
                    SqliteDatabase sqliteDatabase = new(null);
                    TrackedProcess[] trackedProcesses = [.. sqliteDatabase.GetAll<TrackedProcess>()];

                    foreach ((string? processName, (TimeSpan overall, TimeSpan foreground) delta) in trackedDeltas)
                    {
                        TrackedProcess? trackedProcess = trackedProcesses.FirstOrDefault(x => x.ProcessName == processName);
                        if (trackedProcess is null)
                            continue;

                        trackedProcess = trackedProcess with
                        {
                            OverallTrackedTime = trackedProcess.OverallTrackedTime + delta.overall,
                            ForegroundTrackedTime = trackedProcess.ForegroundTrackedTime + delta.foreground
                        };

                        sqliteDatabase.Update(trackedProcess);
                    }

                    trackedDeltas.Clear();
                    lastDbUpdate = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TrackingError] {ex}");
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
