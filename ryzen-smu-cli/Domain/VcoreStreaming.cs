namespace ryzen_smu_cli;

internal static class VcoreStreaming
{
    public const int DefaultIntervalMilliseconds = 150;
    public const int MinimumIntervalMilliseconds = 50;
    public const int MaximumIntervalMilliseconds = 60_000;

    public static bool IsValidInterval(int milliseconds) =>
        milliseconds is >= MinimumIntervalMilliseconds and
            <= MaximumIntervalMilliseconds;

    public static long GetNextSampleDueMilliseconds(
        long previousDueMilliseconds,
        long elapsedMilliseconds,
        int intervalMilliseconds)
    {
        long nextDueMilliseconds = checked(
            previousDueMilliseconds + intervalMilliseconds);
        if (nextDueMilliseconds > elapsedMilliseconds)
        {
            return nextDueMilliseconds;
        }

        long missedIntervals = checked(
            ((elapsedMilliseconds - nextDueMilliseconds) /
                intervalMilliseconds) + 1);
        return checked(
            nextDueMilliseconds + (missedIntervals * intervalMilliseconds));
    }
}
