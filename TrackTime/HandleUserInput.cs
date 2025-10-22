using System.Diagnostics;
using TrackTime.Sqlite;

namespace TrackTime;

internal static class HandleUserInput
{
    internal static void ShowStats()
    {
        SqliteDatabase sqliteDatabase = new(null);
        TrackedProcess[] trackedProcesses = [.. sqliteDatabase.GetAll<TrackedProcess>()];

        if (trackedProcesses.Length == 0)
        {
            Console.WriteLine("No processes are being tracked :(\n");
            return;
        }

        foreach (TrackedProcess trackedProcess in trackedProcesses)
        {
            Console.WriteLine($"PROCESS: {trackedProcess.ProcessName}");
            Console.WriteLine($"Started tracking on {trackedProcess.TrackingStart}");
            Console.WriteLine($"Time spent with the app open in the foreground: {trackedProcess.ForegroundTrackedTime}");
            Console.WriteLine($"Time spent with the app open {trackedProcess.OverallTrackedTime}\n");
        }
    }

    internal static void AddProcess()
    {
        Console.Write("Enter the name of the process you wanna track: ");
        string? processName = Console.ReadLine();

        if (processName is null || Process.GetProcessesByName(processName).Length == 0)
        {
            Console.WriteLine("Invalid process name! Make sure the process is open\n");
            return;
        }

        SqliteDatabase sqliteDatabase = new(null);
        TrackedProcess trackedProcess = new()
        {
            ProcessName = processName,
            ForegroundTrackedTime = TimeSpan.Zero,
            OverallTrackedTime = TimeSpan.Zero,
            TrackingStart = DateTime.Now,
        };

        sqliteDatabase.Insert(trackedProcess);
        Tracker.AddProcess(processName);

        Console.WriteLine($"\nSuccessfully added {processName} to the processes to track!\n");
    }

    internal static void RemoveProcess()
    {
        SqliteDatabase sqliteDatabase = new(null);
        TrackedProcess[] trackedProcesses = [.. sqliteDatabase.GetAll<TrackedProcess>()];
        
        for (int i = 0; i < trackedProcesses.Length; i++)
        {
            Console.WriteLine($"{i +1}. {trackedProcesses[i].ProcessName}");
        }

        Console.Write("\nSelect the process you wanna delete: ");
        ConsoleKey pressedKey = Console.ReadKey(intercept: true).Key;
        int selectedOption = pressedKey - ConsoleKey.D1;

        if (selectedOption < 0 || selectedOption >= trackedProcesses.Length)
        {
            return;
        }

        string? processName = trackedProcesses[selectedOption].ProcessName;
        bool deletionSuccesful = Tracker.RemoveProcess(processName);
        string msg = "Something went wrong :(";

        if (!deletionSuccesful)
        {
            Console.WriteLine(msg);
            return;
        }

        deletionSuccesful = sqliteDatabase.Remove(new TrackedProcess { ProcessName = processName});

        msg = deletionSuccesful
            ? $"Successfully deleted {processName}"
            : msg;

        Console.WriteLine($"{msg}\n");
    }
}
