namespace ryzen_smu_cli;

internal static class VcoreStreaming
{
    public const int DefaultIntervalMilliseconds = 150;
    public const int MinimumIntervalMilliseconds = 50;
    public const int MaximumIntervalMilliseconds = 60_000;

    public static bool IsValidInterval(int milliseconds) =>
        milliseconds is >= MinimumIntervalMilliseconds and
            <= MaximumIntervalMilliseconds;
}
