using System.Globalization;

namespace ryzen_smu_cli;

internal readonly record struct OffsetAssignment(int EnabledCoreIndex, int Offset);

internal sealed record OffsetSpecification(IReadOnlyList<OffsetAssignment> Assignments)
{
    // AMD extended the supported negative range from -30 to -50 on newer CPUs.
    // Firmware remains the final authority and every SMU result is checked.
    public const int MinimumOffset = -50;
    public const int MaximumOffset = 50;

    public static bool TryParse(
        string value,
        out OffsetSpecification? specification,
        out string? error)
    {
        specification = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "The offset list cannot be empty.";
            return false;
        }

        string[] tokens = value.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Any(string.IsNullOrWhiteSpace))
        {
            error = "The offset list contains an empty item.";
            return false;
        }

        bool keyed = tokens[0].Contains(':', StringComparison.Ordinal);
        if (tokens.Any(token => token.Contains(':', StringComparison.Ordinal) != keyed))
        {
            error = "Do not mix positional offsets with core:offset assignments.";
            return false;
        }

        List<OffsetAssignment> assignments = new(tokens.Length);
        HashSet<int> seenCoreIndices = [];

        for (int index = 0; index < tokens.Length; index++)
        {
            int enabledCoreIndex = index;
            string offsetText = tokens[index];

            if (keyed)
            {
                string[] parts = tokens[index].Split(':', StringSplitOptions.TrimEntries);
                if (parts.Length != 2 ||
                    !int.TryParse(
                        parts[0],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out enabledCoreIndex) ||
                    enabledCoreIndex < 0)
                {
                    error = $"Invalid enabled-core index in '{tokens[index]}'.";
                    return false;
                }

                offsetText = parts[1];
            }

            if (!int.TryParse(
                    offsetText,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out int offset) ||
                offset is < MinimumOffset or > MaximumOffset)
            {
                error =
                    $"Offset '{offsetText}' must be a whole number from " +
                    $"{MinimumOffset} through {MaximumOffset}.";
                return false;
            }

            if (!seenCoreIndices.Add(enabledCoreIndex))
            {
                error = $"Enabled core {enabledCoreIndex} was specified more than once.";
                return false;
            }

            assignments.Add(new OffsetAssignment(enabledCoreIndex, offset));
        }

        specification = new OffsetSpecification(assignments);
        return true;
    }
}
