namespace TrackTime;

internal static class Program
{
    private readonly static (string optionDescription, Action callback)[] _options =
    [
        ("Show stats", HandleUserInput.ShowStats),
        ("Add a process to track", HandleUserInput.AddProcess)
    ];
    //Foreground time and time overall tracken
    //Maybe update funktion
    static void Main()
    {
        ShowOptions();
        ReadUserInput();
    }

    static void ShowOptions()
    {
        for (int i = 0; i < _options.Length; i++)
        {
            Console.WriteLine($"{i}.{_options[i].optionDescription}");
        }

        Console.WriteLine("\n Choose an option\n");
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
