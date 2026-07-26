using System.Globalization;

namespace ryzen_smu_cli;

internal sealed record CoreSelection(IReadOnlySet<int> PhysicalCoreIndices)
{
    public static bool TryParse(
        string value,
        out CoreSelection? selection,
        out string? error)
    {
        selection = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "The disabled-core list cannot be empty.";
            return false;
        }

        string[] tokens = value.Split(',', StringSplitOptions.TrimEntries);
        HashSet<int> indices = [];

        foreach (string token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token) ||
                !int.TryParse(
                    token,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int index) ||
                index < 0)
            {
                error = $"Invalid physical core index '{token}'.";
                return false;
            }

            if (!indices.Add(index))
            {
                error = $"Physical core {index} was specified more than once.";
                return false;
            }
        }

        selection = new CoreSelection(indices);
        return true;
    }
}
