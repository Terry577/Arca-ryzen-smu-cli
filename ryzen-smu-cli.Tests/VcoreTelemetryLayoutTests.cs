namespace ryzen_smu_cli.Tests;

public sealed class VcoreTelemetryLayoutTests
{
    [Theory]
    [InlineData(0x00540000u, 0x0828u, 47)]
    [InlineData(0x00540001u, 0x082Cu, 47)]
    [InlineData(0x00540002u, 0x087Cu, 47)]
    [InlineData(0x00540003u, 0x089Cu, 47)]
    [InlineData(0x00540004u, 0x08BCu, 47)]
    [InlineData(0x00540005u, 0x08C8u, 47)]
    [InlineData(0x00540100u, 0x0618u, 47)]
    [InlineData(0x00540101u, 0x061Cu, 47)]
    [InlineData(0x00540102u, 0x066Cu, 47)]
    [InlineData(0x00540103u, 0x068Cu, 47)]
    [InlineData(0x00540104u, 0x06A8u, 47)]
    [InlineData(0x00540105u, 0x06B4u, 47)]
    [InlineData(0x00540108u, 0x06BCu, 47)]
    [InlineData(0x00540208u, 0x08D0u, 48)]
    [InlineData(0x00620105u, 0x0724u, 49)]
    [InlineData(0x00620205u, 0x0994u, 49)]
    [InlineData(0x00621102u, 0x0724u, 49)]
    [InlineData(0x00621202u, 0x0994u, 49)]
    public void KnownDesktopDieTablesResolveToExactTelemetryEntries(
        uint version,
        uint size,
        int expectedIndex)
    {
        bool resolved = TryResolve(
            version,
            size,
            out VcoreTelemetryLayout? layout);

        Assert.True(resolved);
        Assert.NotNull(layout);
        Assert.Equal(size, layout!.PmTableSize);
        Assert.Equal(expectedIndex, layout.ValueIndex);
        Assert.Equal("VDDCR CPU Telemetry", layout.SourceName);
    }

    [Theory]
    [InlineData(0x00540004u, 0x08C0u)]
    [InlineData(0x00540104u, 0x08BCu)]
    [InlineData(0x00540208u, 0x0994u)]
    [InlineData(0x00620105u, 0x0994u)]
    [InlineData(0x00620205u, 0x0724u)]
    public void KnownVersionsWithUnexpectedSizesAreRejected(
        uint version,
        uint wrongSize)
    {
        Assert.False(TryResolve(
            version,
            wrongSize,
            out _));
    }

    [Theory]
    [InlineData(0x004C0009u, 0x0B00u)]
    [InlineData(0x005D0008u, 0x0D54u)]
    [InlineData(0x0064020Cu, 0x0E50u)]
    [InlineData(0x00650005u, 0x0B78u)]
    [InlineData(0u, 0u)]
    public void ApuAndUnknownPmTablesAreNotGuessed(
        uint version,
        uint size)
    {
        Assert.False(TryResolve(version, size, out _));
    }

    [Fact]
    public void GraniteRidgeReadUsesCpuTelemetryInsteadOfPeakOrSetVoltage()
    {
        TryResolve(
            0x00620105,
            0x0724,
            out VcoreTelemetryLayout? layout);
        float[] table = new float[272];
        table[18] = 1.37f;
        table[19] = 1.31f;
        table[48] = 1.31f;
        table[49] = 1.28f;
        table[271] = 1.26f;

        OperationResult<double> result = layout!.Read(table);

        Assert.True(result.Success);
        Assert.Equal(1.28, result.Value, precision: 6);
    }

    [Fact]
    public void RaphaelReadUsesLiveCpuTelemetryInsteadOfPeakValue()
    {
        TryResolve(
            0x00540104,
            0x06A8,
            out VcoreTelemetryLayout? layout);
        float[] table = new float[48];
        table[18] = 1.47f;
        table[19] = 1.31f;
        table[47] = 1.28f;

        OperationResult<double> result = layout!.Read(table);

        Assert.True(result.Success);
        Assert.Equal(1.28, result.Value, precision: 6);
    }

    [Fact]
    public void DragonRangeReadUsesShiftedLiveCpuTelemetry()
    {
        TryResolve(
            0x00540208,
            0x08D0,
            out VcoreTelemetryLayout? layout);
        float[] table = new float[49];
        table[47] = 1.040419f;
        table[48] = 1.032785f;

        OperationResult<double> result = layout!.Read(table);

        Assert.True(result.Success);
        Assert.Equal(1.032785, result.Value, precision: 6);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(2.1f)]
    public void ReadRejectsInvalidTelemetry(float value)
    {
        TryResolve(
            0x00620105,
            0x0724,
            out VcoreTelemetryLayout? layout);
        float[] table = new float[50];
        table[49] = value;

        OperationResult<double> result = layout!.Read(table);

        Assert.False(result.Success);
        Assert.Contains("0x00620105", result.Error);
        Assert.Contains("VDDCR CPU Telemetry", result.Error);
    }

    [Fact]
    public void ReadRejectsTruncatedTable()
    {
        TryResolve(
            0x00620105,
            0x0724,
            out VcoreTelemetryLayout? layout);

        OperationResult<double> result = layout!.Read(new float[49]);

        Assert.False(result.Success);
        Assert.Contains("entry 49", result.Error);
    }

    [Theory]
    [InlineData(0x1Au, 0x44u, 0x00540104u, 0x06A8u)]
    [InlineData(0x19u, 0x61u, 0x00620105u, 0x0724u)]
    [InlineData(0x19u, 0x74u, 0x00540104u, 0x06A8u)]
    public void CorrectTableMetadataOnTheWrongCpuIsRejected(
        uint family,
        uint model,
        uint version,
        uint size)
    {
        Assert.False(VcoreTelemetryLayout.TryResolve(
            family,
            model,
            0,
            version,
            size,
            out _));
    }

    [Theory]
    [InlineData(0x00540004u, 0x08BCu, 1u)]
    [InlineData(0x00540208u, 0x08D0u, 0u)]
    [InlineData(0x00620105u, 0x0724u, 1u)]
    public void DesktopAndMobilePmLayoutsCannotCrossPackageBoundaries(
        uint version,
        uint size,
        uint wrongPackage)
    {
        (uint family, uint model) = (version >> 16) switch
        {
            0x54 => (0x19u, 0x61u),
            0x62 => (0x1Au, 0x44u),
            _ => (0u, 0u),
        };

        Assert.False(VcoreTelemetryLayout.TryResolve(
            family,
            model,
            wrongPackage,
            version,
            size,
            out _));
    }

    [Theory]
    [InlineData(0x00621102u, 0x0724u)]
    [InlineData(0x00621202u, 0x0994u)]
    public void FireRangeUsesOnlyItsStructuralMobileLayouts(
        uint version,
        uint size)
    {
        Assert.True(VcoreTelemetryLayout.TryResolve(
            0x1A,
            0x44,
            1,
            version,
            size,
            out VcoreTelemetryLayout? layout));
        Assert.Equal(VcorePackageClass.Mobile, layout!.PackageClass);
        Assert.Equal(VcoreMappingConfidence.Structural, layout.Confidence);
    }

    private static bool TryResolve(
        uint version,
        uint size,
        out VcoreTelemetryLayout? layout)
    {
        (uint family, uint model, uint package) = (version >> 16) switch
        {
            0x54 when version == 0x00540208 => (0x19u, 0x61u, 1u),
            0x54 => (0x19u, 0x61u, 0u),
            0x62 => (0x1Au, 0x44u, 0u),
            _ => (0u, 0u, 0u),
        };

        return VcoreTelemetryLayout.TryResolve(
            family,
            model,
            package,
            version,
            size,
            out layout);
    }
}
