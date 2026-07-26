namespace ryzen_smu_cli.Tests;

public sealed class CoreMapperTests
{
    [Fact]
    public void DisabledPhysicalSlotsAreSkippedInEnabledCoreMap()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: [0, 1, 3, 4, 6, 7]);

        OperationResult<IReadOnlyDictionary<int, CoreAddress>> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal(6, result.Value!.Count);
        Assert.Equal(0, result.Value[0].PhysicalCoreIndex);
        Assert.Equal(3, result.Value[2].PhysicalCoreIndex);
        Assert.Equal(7, result.Value[5].PhysicalCoreIndex);
    }

    [Fact]
    public void MappingFailureReturnsDiagnosticAfterRetries()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: [0, 1])
        {
            EnabledCoreCountOverride = 3,
        };

        OperationResult<IReadOnlyDictionary<int, CoreAddress>> result =
            CoreMapper.Map(controller);

        Assert.False(result.Success);
        Assert.Contains("expected 3, found 2", result.Error);
        Assert.Equal(24, controller.OffsetReadCount);
    }

    [Fact]
    public void MappingRequiresReadableOffsetCommand()
    {
        FakeRyzenController controller = new(8, [0, 1])
        {
            CanReadPboOffsets = false,
        };

        OperationResult<IReadOnlyDictionary<int, CoreAddress>> result =
            CoreMapper.Map(controller);

        Assert.False(result.Success);
        Assert.Equal(0, controller.OffsetReadCount);
    }
}
