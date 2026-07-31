namespace ryzen_smu_cli;

internal static class VcoreDiagnostics
{
    public const int DefaultSampleCount = 40;
    public const int MinimumSampleCount = 1;
    public const int MaximumSampleCount = 1000;

    public static bool IsValidSampleCount(int value) =>
        value is >= MinimumSampleCount and <= MaximumSampleCount;

    private static readonly IReadOnlyList<VcoreDiagnosticRegisterDescriptor> LegacyRegisters =
    [
        new(0x0005A008, "legacy-status", "status", false, true, false),
        new(0x0005A00C, "legacy-core-plane", "core-plane", true, false, false),
        new(0x0005A010, "legacy-soc-plane", "other-plane", false, false, false),
        new(0x0005A014, "legacy-third-plane", "other-plane", false, false, false),
    ];

    private static readonly IReadOnlyList<VcoreDiagnosticRegisterDescriptor> ExtendedRegisters =
    [
        new(0x0006F034, "extended-prefix", "unknown", false, false, false),
        new(0x0006F038, "extended-core-plane", "core-plane", true, false, false),
        new(0x0006F03C, "extended-soc-plane", "other-plane", false, false, false),
    ];

    private static readonly IReadOnlyList<VcoreDiagnosticRegisterDescriptor> Family1ARegisters =
    [
        new(0x0007300C, "family1a-prefix", "unknown", false, false, false),
        new(0x00073010, "family1a-core-plane", "core-plane", true, false, false),
        new(0x00073014, "family1a-secondary-plane", "other-plane", false, false, true),
    ];

    public static IReadOnlyList<VcoreDiagnosticRegisterDescriptor> ResolveRegisters(
        uint family,
        uint model,
        uint package)
    {
        if (family == 0x19 && model is 0x74 or 0x75 or 0x78 or 0x7C)
        {
            return [.. LegacyRegisters, .. ExtendedRegisters];
        }

        if (family == 0x1A && model is 0x20 or 0x24 or 0x60 or 0x68)
        {
            return [.. LegacyRegisters, .. ExtendedRegisters];
        }

        if (family == 0x1A &&
            (model == 0x70 || (model == 0x44 && package == 1)))
        {
            return [.. ExtendedRegisters, .. Family1ARegisters];
        }

        return [];
    }
}

internal sealed record VcoreDiagnosticRegisterDescriptor(
    uint Address,
    string Name,
    string Role,
    bool DecodeCoreVidCandidates,
    bool DecodeLegacyStatus,
    bool DecodeFamily1AHardwareVid);

internal sealed record VcoreDiagnosticReport(
    int SchemaVersion,
    string ToolVersion,
    DateTimeOffset CapturedAtUtc,
    VcoreDiagnosticCpu Cpu,
    string SelectedSource,
    VcoreDiagnosticSource Source,
    string SelectionState,
    string? SelectionReason,
    int SelectedSuccessCount,
    int SelectedFailureCount,
    int RegisterSuccessCount,
    int RegisterFailureCount,
    int RequestedSamples,
    int CapturedSamples,
    bool Cancelled,
    int IntervalMilliseconds,
    IReadOnlyList<VcoreDiagnosticSample> Samples);

internal sealed record VcoreDiagnosticSource(
    string Kind,
    string Confidence,
    string? Platform,
    string? Register,
    int? VidShift,
    string? StatusRegister,
    string? PmTableVersion,
    string? PmTableSize,
    int? PmTableEntry);

internal sealed record VcoreDiagnosticCpu(
    string Name,
    string CpuId,
    string Family,
    string Model,
    string Package,
    string CodeName,
    int ReportedCcdCount,
    int ReportedPhysicalCoreCount,
    int ReportedLogicalProcessorCount,
    int ThreadsPerCore,
    bool SmtEnabled,
    int ReportedPhysicalCoreSlots,
    int ReportedEnabledCoreCount,
    bool CoreTopologyQualified,
    string? CoreTopologyReason,
    string MotherboardVendor,
    string MotherboardModel,
    string BiosVersion,
    string FirmwareVersion,
    string SmuVersion,
    string PmTableVersion,
    string PmTableSize);

internal sealed record VcoreDiagnosticSample(
    int Sequence,
    long StartedMilliseconds,
    long CompletedMilliseconds,
    VcoreDiagnosticSelectedReading Selected,
    IReadOnlyList<VcoreDiagnosticRegisterReading> Registers);

internal sealed record VcoreDiagnosticSelectedReading(
    bool Success,
    double? Volts,
    string? Error);

internal sealed record VcoreDiagnosticRegisterReading(
    string Address,
    string Name,
    string Role,
    bool Success,
    string? Raw,
    IReadOnlyList<byte>? BytesLittleEndian,
    uint? VidBits23To16,
    double? VoltsBits23To16,
    uint? VidBits15To8,
    double? VoltsBits15To8,
    bool? LegacyPlane0Unavailable,
    bool? LegacyPlane1Unavailable,
    uint? Family1AHardwareVidBits14To6,
    string? Error);
