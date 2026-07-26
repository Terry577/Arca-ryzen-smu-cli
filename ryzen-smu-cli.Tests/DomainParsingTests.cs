namespace ryzen_smu_cli.Tests;

public sealed class DomainParsingTests
{
    [Fact]
    public void PositionalOffsetsMapToSequentialEnabledCores()
    {
        bool success = OffsetSpecification.TryParse(
            "-10, 5, -50",
            out OffsetSpecification? specification,
            out string? error);

        Assert.True(success, error);
        Assert.Equal(
            [
                new OffsetAssignment(0, -10),
                new OffsetAssignment(1, 5),
                new OffsetAssignment(2, -50),
            ],
            specification!.Assignments);
    }

    [Fact]
    public void KeyedOffsetsRetainExplicitEnabledCoreIndices()
    {
        bool success = OffsetSpecification.TryParse(
            "7:-20,0:10,16:-1",
            out OffsetSpecification? specification,
            out string? error);

        Assert.True(success, error);
        Assert.Equal(
            [
                new OffsetAssignment(7, -20),
                new OffsetAssignment(0, 10),
                new OffsetAssignment(16, -1),
            ],
            specification!.Assignments);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0:-10,-20")]
    [InlineData("0:-10,0:5")]
    [InlineData("0:-51")]
    [InlineData("51")]
    [InlineData("-1:-10")]
    [InlineData("core:-10")]
    [InlineData("1:")]
    [InlineData("1::2")]
    [InlineData("1,,2")]
    public void InvalidOffsetsAreRejected(string value)
    {
        Assert.False(
            OffsetSpecification.TryParse(value, out _, out string? error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void CoreSelectionParsesPhysicalIndices()
    {
        bool success = CoreSelection.TryParse(
            "0, 7, 16",
            out CoreSelection? selection,
            out string? error);

        Assert.True(success, error);
        Assert.Equal([0, 7, 16], selection!.PhysicalCoreIndices.Order());
    }

    [Theory]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("1,1")]
    [InlineData("1,,2")]
    [InlineData("core")]
    public void InvalidCoreSelectionsAreRejected(string value)
    {
        Assert.False(CoreSelection.TryParse(value, out _, out string? error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("5225", 5225)]
    [InlineData("5250", 5250)]
    public void FMaxParsesTwentyFiveMegahertzSteps(
        string value,
        uint expectedMegahertz)
    {
        bool success = FMaxFrequency.TryParse(
            value,
            out FMaxFrequency frequency,
            out string? error);

        Assert.True(success, error);
        Assert.Equal(expectedMegahertz, frequency.Megahertz);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-25")]
    [InlineData("5224")]
    [InlineData("5226")]
    [InlineData("1048600")]
    [InlineData("5.25GHz")]
    public void InvalidFMaxValuesAreRejected(string value)
    {
        Assert.False(
            FMaxFrequency.TryParse(value, out _, out string? error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
