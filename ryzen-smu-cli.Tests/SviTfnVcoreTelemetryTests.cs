namespace ryzen_smu_cli.Tests;

public sealed class SviTfnVcoreTelemetryTests
{
    [Theory]
    [InlineData(0x19u, 0x74u, 0x0006F038u, 8)]
    [InlineData(0x19u, 0x75u, 0x0006F038u, 8)]
    [InlineData(0x19u, 0x78u, 0x0006F038u, 8)]
    [InlineData(0x19u, 0x7Cu, 0x0006F038u, 8)]
    [InlineData(0x1Au, 0x20u, 0x0005A00Cu, 8)]
    [InlineData(0x1Au, 0x24u, 0x0005A00Cu, 8)]
    [InlineData(0x1Au, 0x60u, 0x0005A00Cu, 8)]
    [InlineData(0x1Au, 0x68u, 0x0005A00Cu, 8)]
    [InlineData(0x1Au, 0x70u, 0x00073010u, 8)]
    public void Zen4AndZen5ApuModelsResolveTheirPlatformSviPlane(
        uint family,
        uint model,
        uint expectedRegister,
        int expectedVidShift)
    {
        bool resolved = SviTfnVcoreTelemetry.TryResolve(
            family,
            model,
            out SviTfnVcoreTelemetry? layout);

        Assert.True(resolved);
        Assert.NotNull(layout);
        Assert.Equal(expectedRegister, layout!.CorePlaneRegister);
        Assert.Equal(expectedVidShift, layout.VidShift);
    }

    [Theory]
    [InlineData(0x19u, 0x61u)]
    [InlineData(0x1Au, 0x44u)]
    [InlineData(0x19u, 0x21u)]
    [InlineData(0x17u, 0x71u)]
    public void DesktopDieAndOlderModelsDoNotUseApuSviLayouts(
        uint family,
        uint model)
    {
        Assert.False(SviTfnVcoreTelemetry.TryResolve(
            family,
            model,
            out _));
    }

    [Theory]
    [InlineData(0x01u, 1.54375)]
    [InlineData(0x20u, 1.35)]
    [InlineData(0x2Bu, 1.28125)]
    [InlineData(0x58u, 1.00)]
    public void Zen4AndZen5DecodeUsesVidBitsFifteenThroughEight(
        uint vid,
        double expectedVoltage)
    {
        SviTfnVcoreTelemetry.TryResolve(
            0x19,
            0x74,
            out SviTfnVcoreTelemetry? layout);
        uint telemetry = vid << 8;

        OperationResult<double> result = layout!.Decode(telemetry);

        Assert.True(result.Success);
        Assert.Equal(expectedVoltage, result.Value, precision: 6);
    }

    [Fact]
    public void StrixLegacyBlockHonorsItsOwnPlaneUnavailableBit()
    {
        SviTfnVcoreTelemetry.TryResolve(
            0x1A,
            0x20,
            out SviTfnVcoreTelemetry? layout);

        OperationResult result = layout!.ValidateStatus(0x1);

        Assert.False(result.Success);
        Assert.Contains("unavailable", result.Error);
    }

    [Fact]
    public void ExtendedApuStatusIsNotGatedByTheLegacyBlock()
    {
        SviTfnVcoreTelemetry.TryResolve(
            0x19,
            0x74,
            out SviTfnVcoreTelemetry? layout);

        Assert.Null(layout!.StatusRegister);
        Assert.True(layout.ValidateStatus(uint.MaxValue).Success);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(uint.MaxValue)]
    public void DecodeRejectsSentinelRegisterValues(uint telemetry)
    {
        SviTfnVcoreTelemetry.TryResolve(
            0x19,
            0x74,
            out SviTfnVcoreTelemetry? layout);

        OperationResult<double> result = layout!.Decode(telemetry);

        Assert.False(result.Success);
        Assert.Contains("sentinel", result.Error);
    }

    [Fact]
    public void DecodeRejectsNonzeroRegisterWithEmptySelectedVidField()
    {
        SviTfnVcoreTelemetry.TryResolve(
            0x19,
            0x74,
            out SviTfnVcoreTelemetry? layout);

        OperationResult<double> result = layout!.Decode(0x00000001);

        Assert.False(result.Success);
        Assert.Contains("empty", result.Error);
    }

    [Fact]
    public void DiagnosticDecoderCanExposeBothPackingCandidates()
    {
        const uint telemetry = 0x002B5801;

        OperationResult<double> bitsTwentyThreeToSixteen =
            SviTfnVcoreTelemetry.DecodeCandidate(telemetry, 16);
        OperationResult<double> bitsFifteenToEight =
            SviTfnVcoreTelemetry.DecodeCandidate(telemetry, 8);

        Assert.Equal(1.28125, bitsTwentyThreeToSixteen.Value, precision: 6);
        Assert.Equal(1.00, bitsFifteenToEight.Value, precision: 6);
    }
}
