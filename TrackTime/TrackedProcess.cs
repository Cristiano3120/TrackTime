using TrackTime.Sqlite.Attributes;

namespace TrackTime;

internal sealed record TrackedProcess
{
    [PrimaryKey]
    public string ProcessName { get; init; }
    public DateTime TrackingStart { get; init; }
    public TimeSpan OverallTrackedTime { get; init; }
    public TimeSpan ForegroundTrackedTime { get; init; }
}
