using System.Globalization;

namespace ryzen_smu_cli;

internal readonly record struct FMaxFrequency(uint Megahertz)
{
    public const uint StepMegahertz = 25;
    public const uint MinimumMegahertz = StepMegahertz;
    public const uint MaximumMegahertz = 0xFFFFF;

    public static bool TryParse(
        string value,
        out FMaxFrequency frequency,
        out string? error)
    {
        frequency = default;
        error = null;

        if (!uint.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint megahertz) ||
            megahertz is < MinimumMegahertz or > MaximumMegahertz ||
            megahertz % StepMegahertz != 0)
        {
            error =
                $"FMax must be a whole number of MHz from {MinimumMegahertz} through " +
                $"{MaximumMegahertz}, in {StepMegahertz} MHz steps.";
            return false;
        }

        frequency = new FMaxFrequency(megahertz);
        return true;
    }
}
