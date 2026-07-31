namespace ryzen_smu_cli;

internal sealed record SviTfnVcoreTelemetry(
    uint CpuFamily,
    uint CpuModel,
    uint CorePlaneRegister,
    int VidShift,
    uint? StatusRegister,
    uint UnavailableMask,
    string PlatformName,
    VcoreMappingConfidence Confidence)
{
    private const double VidBaseVoltage = 1.55;
    private const double VidStepVoltage = 0.00625;
    private const double MinimumPlausibleVoltage = 0.1;
    private const double MaximumPlausibleVoltage = 2.0;

    // Zen 4/5 changed the telemetry packing used by the public Zen monitor
    // implementations: the voltage VID is in bits 15:8. Addresses are chosen
    // per silicon family rather than inherited from an older generic path.
    private static readonly IReadOnlyDictionary<(uint Family, uint Model), SviTfnVcoreTelemetry> KnownLayouts =
        new Dictionary<(uint Family, uint Model), SviTfnVcoreTelemetry>
        {
            // Phoenix, Phoenix 2 and Hawk Point. ZenStates-Core, ZenTimings and
            // nwinfo independently select the extended APU SVI plane.
            [(0x19, 0x74)] = ExtendedApu(0x19, 0x74, "Phoenix / Hawk Point"),
            [(0x19, 0x75)] = ExtendedApu(0x19, 0x75, "Phoenix / Hawk Point"),
            [(0x19, 0x78)] = ExtendedApu(0x19, 0x78, "Phoenix 2"),
            [(0x19, 0x7C)] = ExtendedApu(0x19, 0x7C, "Hawk Point"),

            // Family 1Ah uses SVI3-era layouts. These are structural mappings
            // from current public monitor implementations and remain labelled
            // as such until synchronized hardware captures are collected.
            [(0x1A, 0x20)] = LegacyBlock(0x1A, 0x20, "Strix Point"),
            [(0x1A, 0x24)] = LegacyBlock(0x1A, 0x24, "Strix Point"),
            [(0x1A, 0x60)] = LegacyBlock(0x1A, 0x60, "Krackan Point"),
            [(0x1A, 0x68)] = LegacyBlock(0x1A, 0x68, "Krackan Point refresh"),
            [(0x1A, 0x70)] = Family1AHalo(0x1A, 0x70, "Strix Halo"),
        };

    private static SviTfnVcoreTelemetry ExtendedApu(
        uint family,
        uint model,
        string platformName) => new(
            family,
            model,
            0x0006F038,
            8,
            null,
            0,
            platformName,
            VcoreMappingConfidence.Structural);

    private static SviTfnVcoreTelemetry LegacyBlock(
        uint family,
        uint model,
        string platformName) => new(
            family,
            model,
            0x0005A00C,
            8,
            0x0005A008,
            0x1,
            platformName,
            VcoreMappingConfidence.Structural);

    private static SviTfnVcoreTelemetry Family1AHalo(
        uint family,
        uint model,
        string platformName) => new(
            family,
            model,
            0x00073010,
            8,
            null,
            0,
            platformName,
            VcoreMappingConfidence.Structural);

    public static bool TryResolve(
        uint family,
        uint model,
        out SviTfnVcoreTelemetry? layout) =>
        KnownLayouts.TryGetValue((family, model), out layout);

    public OperationResult ValidateStatus(uint status)
    {
        if (StatusRegister.HasValue && (status & UnavailableMask) != 0)
        {
            return OperationResult.Fail(
                $"SMU SVI reports that the {PlatformName} CPU-core rail " +
                $"telemetry plane is unavailable (status 0x{status:X8}).");
        }

        return OperationResult.Ok();
    }

    public OperationResult<double> Decode(uint corePlaneTelemetry)
    {
        if (corePlaneTelemetry is 0 or uint.MaxValue)
        {
            return OperationResult<double>.Fail(
                $"SMU SVI returned a sentinel CPU-core rail value " +
                $"(telemetry 0x{corePlaneTelemetry:X8}).");
        }

        uint vid = (corePlaneTelemetry >> VidShift) & 0xFF;
        if (vid == 0)
        {
            return OperationResult<double>.Fail(
                $"SMU SVI returned an empty CPU-core rail VID field " +
                $"(telemetry 0x{corePlaneTelemetry:X8}, bits " +
                $"{VidShift + 7}:{VidShift}).");
        }

        double voltage = VidBaseVoltage - (VidStepVoltage * vid);
        if (!double.IsFinite(voltage) ||
            voltage < MinimumPlausibleVoltage ||
            voltage > MaximumPlausibleVoltage)
        {
            return OperationResult<double>.Fail(
                $"SMU SVI returned an invalid CPU-core rail value " +
                $"(telemetry 0x{corePlaneTelemetry:X8}, VID 0x{vid:X2}, " +
                $"bits {VidShift + 7}:{VidShift}).");
        }

        return OperationResult<double>.Ok(voltage);
    }

    internal static OperationResult<double> DecodeCandidate(
        uint telemetry,
        int vidShift)
    {
        SviTfnVcoreTelemetry candidate = new(
            0,
            0,
            0,
            vidShift,
            null,
            0,
            "diagnostic candidate",
            VcoreMappingConfidence.Structural);
        return candidate.Decode(telemetry);
    }
}
