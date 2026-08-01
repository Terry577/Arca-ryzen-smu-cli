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

    [Fact]
    public void UnqualifiedHybridTopologyFailsBeforeAnyCoreProbe()
    {
        FakeRyzenController controller = new(16, Enumerable.Range(0, 12))
        {
            HasUsableCoreTopology = false,
            CoreTopologyUnavailableReason =
                "Per-core selectors are not qualified for this topology.",
        };

        OperationResult<IReadOnlyDictionary<int, CoreAddress>> result =
            CoreMapper.Map(controller);

        Assert.False(result.Success);
        Assert.Contains("not qualified", result.Error);
        Assert.Equal(0, controller.OffsetReadCount);
    }

    [Theory]
    [InlineData(0x19u, 0x74u, 0u)]
    [InlineData(0x19u, 0x75u, 1u)]
    [InlineData(0x19u, 0x78u, 1u)]
    [InlineData(0x19u, 0x7Cu, 0u)]
    [InlineData(0x1Au, 0x20u, 1u)]
    [InlineData(0x1Au, 0x24u, 1u)]
    [InlineData(0x1Au, 0x60u, 1u)]
    [InlineData(0x1Au, 0x68u, 1u)]
    [InlineData(0x1Au, 0x70u, 1u)]
    public void UnqualifiedMobileFamiliesFailClosedForPerCoreOperations(
        uint family,
        uint model,
        uint package)
    {
        string? reason =
            ZenStatesRyzenController.GetUnqualifiedCoreTopologyReason(
                family,
                model,
                package);

        Assert.NotNull(reason);
        Assert.Contains("Vcore telemetry remain available", reason);
    }

    [Theory]
    [InlineData(0x19u, 0x61u, 0u)]
    [InlineData(0x19u, 0x61u, 1u)]
    [InlineData(0x1Au, 0x44u, 0u)]
    [InlineData(0x1Au, 0x44u, 1u)]
    public void DesktopDiePlatformsAreNotRejectedByPlatformGate(
        uint family,
        uint model,
        uint package)
    {
        Assert.Null(
            ZenStatesRyzenController.GetUnqualifiedCoreTopologyReason(
                family,
                model,
                package));
    }

    [Fact]
    public void UnqualifiedTopologyUsesCpuidCoreCountForInformation()
    {
        Assert.Equal(
            16,
            ZenStatesRyzenController.ResolveInformationPhysicalCoreCount(
                enabledCoreCount: 8,
                cpuidPhysicalCoreCount: 16,
                coreTopologyQualified: false));
    }

    [Theory]
    [InlineData(true, 8u)]
    [InlineData(false, 0u)]
    [InlineData(false, 257u)]
    public void InformationCoreCountFallsBackToEnabledCount(
        bool coreTopologyQualified,
        uint cpuidPhysicalCoreCount)
    {
        Assert.Equal(
            8,
            ZenStatesRyzenController.ResolveInformationPhysicalCoreCount(
                enabledCoreCount: 8,
                cpuidPhysicalCoreCount,
                coreTopologyQualified));
    }
}
