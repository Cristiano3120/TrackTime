using System;
using System.Collections.Generic;
using System.Text;

namespace TrackTime;

internal class Application
{
    public const string ApplicationName = "TimeTracker";
    public const string Version = "1.0.0";

    public static string ApplicationFolderPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
}
