using TrackTime.Sqlite;

namespace TrackTime;

internal static class Program
{
    private readonly static (string optionDescription, Action callback)[] _options =
    [
        ("Show stats", HandleUserInput.ShowStats),
        ("Add a process to track", HandleUserInput.AddProcess),
        ("Remove a process", HandleUserInput.RemoveProcess)
    ];

    static void Main()
    {
        SqliteDatabase sqliteDatabase = new(null);
        sqliteDatabase.CreateTable<TrackedProcess>();

        List<string> processNames = [.. sqliteDatabase.GetAll<TrackedProcess>().Select(x => x.ProcessName)];
        Tracker.StartTracking(processNames);
        Autostart.AddToStartup();

        while (true)
        {
            ShowOptions();
        }
    }

    static void ShowOptions()
    {
        for (int i = 0; i < _options.Length; i++)
        {
            Console.WriteLine($"{i +1}.{_options[i].optionDescription}");
        }

        Console.WriteLine("\nChoose an option:\n");

        ReadUserInput();
    }

    internal static void ReadUserInput()
    {
        ConsoleKey pressedKey = Console.ReadKey(intercept: true).Key;
        
        if (pressedKey == ConsoleKey.Escape)
        {
            Environment.Exit(0);
        }

        // Do this to convert the pressed key enum to an array index
        //We use D1 cause its the first possible option
        //Example: user presses '2'. '2' == D2. D2 - D1 == 1. 1 is the second index in the array
        int selectedOption = pressedKey - ConsoleKey.D1;

        if (selectedOption < 0 || selectedOption >= _options.Length)
        {
            return;
        }

        _options[selectedOption].callback();
    }
}
