namespace ryzen_smu_cli;

internal sealed record VcoreTelemetryLayout(
    uint PmTableVersion,
    int ValueIndex,
    string SourceName)
{
    private const double MinimumPlausibleVoltage = 0.1;
    private const double MaximumPlausibleVoltage = 2.0;

    private static readonly IReadOnlyDictionary<uint, VcoreTelemetryLayout> KnownLayouts =
        new Dictionary<uint, VcoreTelemetryLayout>
        {
            // LibreHardwareMonitor and the embedded RyzenSmu mapping identify
            // entry 47 as the VDDCR voltage for this exact Zen 4 table.
            [0x00540004] = new(0x00540004, 47, "VDDCR"),

            // Exact 0x540104 table captures and a CPUID-gated Zen 4 hardware
            // monitor identify entry 18 as the CPU-rail peak voltage. This is
            // a distinct layout from 0x540004 and must not inherit entry 47.
            [0x00540104] = new(0x00540104, 18, "Vcore Peak"),

            // Empirical Granite Ridge reverse engineering identifies entry 18
            // as peak CPU-rail Vcore telemetry. CurveAlign compares short
            // 150 ms windows, so this is deliberately preferred over entry 19
            // (sleep-state-weighted average) and entry 48 (requested/set value).
            [0x00620105] = new(0x00620105, 18, "Vcore Peak"),

            // The leading telemetry block is shared by the one- and two-CCD
            // Granite Ridge layouts; their known differences are in later
            // per-CCD/per-core arrays.
            [0x00620205] = new(0x00620205, 18, "Vcore Peak"),
        };

    public static bool TryResolve(
        uint pmTableVersion,
        out VcoreTelemetryLayout? layout) =>
        KnownLayouts.TryGetValue(pmTableVersion, out layout);

    public OperationResult<double> Read(float[]? table)
    {
        if (table is null || table.Length <= ValueIndex)
        {
            return OperationResult<double>.Fail(
                $"PM table 0x{PmTableVersion:X8} did not contain " +
                $"{SourceName} entry {ValueIndex}.");
        }

        float value = table[ValueIndex];
        if (!float.IsFinite(value) ||
            value < MinimumPlausibleVoltage ||
            value > MaximumPlausibleVoltage)
        {
            return OperationResult<double>.Fail(
                $"PM table 0x{PmTableVersion:X8} returned an invalid " +
                $"{SourceName} value ({value}).");
        }

        return OperationResult<double>.Ok(value);
    }
}
