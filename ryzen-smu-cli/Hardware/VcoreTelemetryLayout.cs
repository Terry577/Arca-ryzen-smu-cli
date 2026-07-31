namespace ryzen_smu_cli;

internal sealed record VcoreTelemetryLayout(
    uint CpuFamily,
    uint CpuModel,
    VcorePackageClass PackageClass,
    uint PmTableVersion,
    uint PmTableSize,
    int ValueIndex,
    string SourceName,
    VcoreMappingConfidence Confidence)
{
    private const double MinimumPlausibleVoltage = 0.1;
    private const double MaximumPlausibleVoltage = 2.0;

    private static readonly IReadOnlyDictionary<(uint Family, uint Model, VcorePackageClass PackageClass, uint Version), VcoreTelemetryLayout> KnownLayouts =
        new Dictionary<(uint Family, uint Model, VcorePackageClass PackageClass, uint Version), VcoreTelemetryLayout>
        {
            // Entry 47 is directly verified for 0x540004 and 0x540104. The
            // other known Raphael revisions retain the same early rail block,
            // but are marked as structural mappings until synchronized raw
            // telemetry is collected from those firmware revisions.
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540000)] = Raphael(0x00540000, 0x0828),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540001)] = Raphael(0x00540001, 0x082C),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540002)] = Raphael(0x00540002, 0x087C),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540003)] = Raphael(0x00540003, 0x089C),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540004)] = Raphael(
                0x00540004,
                0x08BC,
                VcoreMappingConfidence.Verified),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540005)] = Raphael(0x00540005, 0x08C8),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540100)] = Raphael(0x00540100, 0x0618),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540101)] = Raphael(0x00540101, 0x061C),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540102)] = Raphael(0x00540102, 0x066C),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540103)] = Raphael(0x00540103, 0x068C),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540104)] = Raphael(
                0x00540104,
                0x06A8,
                VcoreMappingConfidence.Verified),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540105)] = Raphael(0x00540105, 0x06B4),
            [(0x19, 0x61, VcorePackageClass.Desktop, 0x00540108)] = Raphael(0x00540108, 0x06BC),

            // Dragon Range inserts one float before the shared Raphael rail
            // telemetry block. The following clocks and voltage rails are
            // likewise shifted by one entry in its exact 0x540208 layout.
            [(0x19, 0x61, VcorePackageClass.Mobile, 0x00540208)] = new(
                0x19,
                0x61,
                VcorePackageClass.Mobile,
                0x00540208,
                0x08D0,
                48,
                "VDDCR CPU Telemetry",
                VcoreMappingConfidence.Structural),

            // Synchronized HWiNFO and raw-SMU captures on Granite Ridge identify
            // entry 49 as the live CPU-rail telemetry value. Entry 18 is a peak
            // or limit metric and systematically overstates the voltage needed
            // by CurveAlign's sampling windows.
            [(0x1A, 0x44, VcorePackageClass.Desktop, 0x00620105)] = GraniteRidge(
                0x00620105,
                0x0724,
                VcoreMappingConfidence.Verified),

            // The live CPU-rail telemetry block is shared by the one- and
            // two-CCD Granite Ridge layouts; their known differences are in
            // later per-CCD/per-core arrays.
            [(0x1A, 0x44, VcorePackageClass.Desktop, 0x00620205)] = GraniteRidge(0x00620205, 0x0994),

            // These earlier Granite Ridge / Fire Range table revisions have
            // the same early rail-telemetry block and differ in later arrays.
            [(0x1A, 0x44, VcorePackageClass.Desktop, 0x00621102)] = GraniteRidge(0x00621102, 0x0724),
            [(0x1A, 0x44, VcorePackageClass.Desktop, 0x00621202)] = GraniteRidge(0x00621202, 0x0994),

            // Fire Range shares Family 1Ah model 44h with Granite Ridge, but
            // package type 1 identifies the mobile FL1 derivative. Only the
            // observed 0x6211/0x6212 table shapes are admitted here.
            [(0x1A, 0x44, VcorePackageClass.Mobile, 0x00621102)] = FireRange(0x00621102, 0x0724),
            [(0x1A, 0x44, VcorePackageClass.Mobile, 0x00621202)] = FireRange(0x00621202, 0x0994),
        };

    private static VcoreTelemetryLayout Raphael(
        uint version,
        uint size,
        VcoreMappingConfidence confidence = VcoreMappingConfidence.Structural) =>
        new(
            0x19,
            0x61,
            VcorePackageClass.Desktop,
            version,
            size,
            47,
            "VDDCR CPU Telemetry",
            confidence);

    private static VcoreTelemetryLayout GraniteRidge(
        uint version,
        uint size,
        VcoreMappingConfidence confidence = VcoreMappingConfidence.Structural) =>
        new(
            0x1A,
            0x44,
            VcorePackageClass.Desktop,
            version,
            size,
            49,
            "VDDCR CPU Telemetry",
            confidence);

    private static VcoreTelemetryLayout FireRange(
        uint version,
        uint size) => new(
            0x1A,
            0x44,
            VcorePackageClass.Mobile,
            version,
            size,
            49,
            "VDDCR CPU Telemetry",
            VcoreMappingConfidence.Structural);

    public static bool TryResolve(
        uint cpuFamily,
        uint cpuModel,
        uint cpuPackage,
        uint pmTableVersion,
        uint pmTableSize,
        out VcoreTelemetryLayout? layout)
    {
        VcorePackageClass packageClass = cpuPackage == 1
            ? VcorePackageClass.Mobile
            : VcorePackageClass.Desktop;
        return KnownLayouts.TryGetValue(
                   (cpuFamily, cpuModel, packageClass, pmTableVersion),
                   out layout) &&
        layout.PmTableSize == pmTableSize;
    }

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

internal enum VcoreMappingConfidence
{
    Verified,
    Structural,
}

internal enum VcorePackageClass
{
    Desktop,
    Mobile,
}
