namespace ryzen_smu_cli.Tests;

public sealed class VcoreTelemetryLayoutTests
{
    [Theory]
    [InlineData(0x00540004u, 47, "VDDCR")]
    [InlineData(0x00540104u, 18, "Vcore Peak")]
    [InlineData(0x00620105u, 18, "Vcore Peak")]
    [InlineData(0x00620205u, 18, "Vcore Peak")]
    public void KnownPmTablesResolveToExplicitTelemetryEntries(
        uint version,
        int expectedIndex,
        string expectedSource)
    {
        bool resolved = VcoreTelemetryLayout.TryResolve(
            version,
            out VcoreTelemetryLayout? layout);

        Assert.True(resolved);
        Assert.NotNull(layout);
        Assert.Equal(expectedIndex, layout!.ValueIndex);
        Assert.Equal(expectedSource, layout.SourceName);
    }

    [Theory]
    [InlineData(0x00540208u)]
    [InlineData(0x00621102u)]
    [InlineData(0x00621202u)]
    [InlineData(0u)]
    public void UnknownPmTablesAreNotGuessed(uint version)
    {
        Assert.False(VcoreTelemetryLayout.TryResolve(version, out _));
    }

    [Fact]
    public void ReadReturnsMappedValueWithoutVidConversion()
    {
        VcoreTelemetryLayout.TryResolve(
            0x00620105,
            out VcoreTelemetryLayout? layout);
        float[] table = new float[32];
        table[18] = 1.21875f;

        OperationResult<double> result = layout!.Read(table);

        Assert.True(result.Success);
        Assert.Equal(1.21875, result.Value, precision: 6);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(2.1f)]
    public void ReadRejectsInvalidTelemetry(float value)
    {
        VcoreTelemetryLayout.TryResolve(
            0x00620105,
            out VcoreTelemetryLayout? layout);
        float[] table = new float[32];
        table[18] = value;

        OperationResult<double> result = layout!.Read(table);

        Assert.False(result.Success);
        Assert.Contains("0x00620105", result.Error);
        Assert.Contains("Vcore Peak", result.Error);
    }

    [Fact]
    public void ReadRejectsTruncatedTable()
    {
        VcoreTelemetryLayout.TryResolve(
            0x00620105,
            out VcoreTelemetryLayout? layout);

        OperationResult<double> result = layout!.Read(new float[18]);

        Assert.False(result.Success);
        Assert.Contains("entry 18", result.Error);
    }
}
