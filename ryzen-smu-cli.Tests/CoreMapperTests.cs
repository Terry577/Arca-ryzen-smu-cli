namespace ryzen_smu_cli.Tests;

public sealed class CoreMapperTests
{
    [Fact]
    public void EnabledCcdBitmapPreservesSurvivingSecondCcdSelector()
    {
        IReadOnlyList<int> selectors =
            ZenStatesRyzenController.ResolvePboCcdSelectors(
                enabledCcdMap: 0b10,
                cpuidCoreCount: 8,
                reportedCcdCount: 1,
                physicalCoreSlots: 8,
                family: 0x19,
                model: 0x61);

        Assert.Equal([1], selectors);
    }

    [Fact]
    public void EnabledCcdBitmapKeepsHealthySingleCcdProbeMinimal()
    {
        IReadOnlyList<int> selectors =
            ZenStatesRyzenController.ResolvePboCcdSelectors(
                enabledCcdMap: 0b01,
                cpuidCoreCount: 8,
                reportedCcdCount: 1,
                physicalCoreSlots: 8,
                family: 0x1A,
                model: 0x44);

        Assert.Equal([0], selectors);
    }

    [Fact]
    public void HealthyPrimaryCcdDoesNotProbeFallbackSelector()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: Enumerable.Range(0, 8))
        {
            PboOffsetFallbackCandidates = Enumerable
                .Range(8, 8)
                .Select(CoreAddress.FromPhysicalCoreIndex)
                .ToArray(),
        };

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, result.Value!.Count);
        Assert.Equal(0, result.Value.Entries[0].Address.PhysicalCoreIndex);
        Assert.Equal(8, controller.OffsetReadCount);
    }

    [Fact]
    public void StaleEnabledCcdBitmapFallsBackToOppositeSelector()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: Enumerable.Range(8, 8))
        {
            PboOffsetCandidates = Enumerable
                .Range(0, 8)
                .Select(CoreAddress.FromPhysicalCoreIndex)
                .ToArray(),
            PboOffsetFallbackCandidates = Enumerable
                .Range(8, 8)
                .Select(CoreAddress.FromPhysicalCoreIndex)
                .ToArray(),
        };

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, result.Value!.Count);
        Assert.Equal(8, result.Value.Entries[0].Address.PhysicalCoreIndex);
        Assert.Equal(15, result.Value.Entries[7].Address.PhysicalCoreIndex);
        Assert.Equal(16, controller.OffsetReadCount);
    }

    [Theory]
    [InlineData(0x19u, 0x61u)]
    [InlineData(0x1Au, 0x44u)]
    public void NonzeroSingleCcdBitmapProvidesOppositeFallbackSelector(
        uint family,
        uint model)
    {
        Assert.Equal(
            [1],
            ZenStatesRyzenController.ResolvePboFallbackCcdSelectors(
                enabledCcdMap: 0b01,
                primaryCcdSelectors: [0],
                family,
                model));

        Assert.Equal(
            [0],
            ZenStatesRyzenController.ResolvePboFallbackCcdSelectors(
                enabledCcdMap: 0b10,
                primaryCcdSelectors: [1],
                family,
                model));
    }

    [Fact]
    public void MissingCcdBitmapDoesNotCreateRedundantFallbackSelectors()
    {
        Assert.Empty(
            ZenStatesRyzenController.ResolvePboFallbackCcdSelectors(
                enabledCcdMap: 0,
                primaryCcdSelectors: [0, 1],
                family: 0x1A,
                model: 0x44));
    }

    [Theory]
    [InlineData(0x19u, 0x61u)]
    [InlineData(0x1Au, 0x44u)]
    public void DesktopDieWithoutCcdBitmapProbesBothPossibleSelectors(
        uint family,
        uint model)
    {
        IReadOnlyList<int> selectors =
            ZenStatesRyzenController.ResolvePboCcdSelectors(
                enabledCcdMap: 0,
                cpuidCoreCount: 8,
                reportedCcdCount: 1,
                physicalCoreSlots: 8,
                family,
                model);

        Assert.Equal([0, 1], selectors);
    }

    [Fact]
    public void CpuidCoreCountRepairsIncompleteEnabledCcdBitmap()
    {
        IReadOnlyList<int> selectors =
            ZenStatesRyzenController.ResolvePboCcdSelectors(
                enabledCcdMap: 0b01,
                cpuidCoreCount: 16,
                reportedCcdCount: 1,
                physicalCoreSlots: 8,
                family: 0x1A,
                model: 0x44);

        Assert.Equal([0, 1], selectors);
    }

    [Fact]
    public void DisabledPhysicalSlotsAreSkippedInEnabledCoreMap()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: [0, 1, 3, 4, 6, 7]);

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal(6, result.Value!.Count);
        Assert.Equal(0, result.Value.Entries[0].Address.PhysicalCoreIndex);
        Assert.Equal(3, result.Value.Entries[2].Address.PhysicalCoreIndex);
        Assert.Equal(7, result.Value.Entries[5].Address.PhysicalCoreIndex);
        Assert.Equal(12, controller.OffsetReadCount);
    }

    [Fact]
    public void StaleEnabledCoreCountDoesNotRejectOperableCompactMap()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: [0, 1])
        {
            EnabledCoreCountOverride = 3,
        };

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(20, controller.OffsetReadCount);
    }

    [Fact]
    public void SoftCountHintRetriesFailedSelectorThroughThirdPass()
    {
        int targetSelectorAttempts = 0;
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: Enumerable.Range(0, 8))
        {
            // The stale topology hint deliberately matches the seven
            // first-pass successes. It must not suppress focused retries.
            EnabledCoreCountOverride = 7,
            GetPboOffsetHandler = (core, _) =>
            {
                if (core.PhysicalCoreIndex == 3 &&
                    ++targetSelectorAttempts <= 2)
                {
                    return OperationResult<int>.Fail("SMU busy");
                }

                return OperationResult<int>.Ok(-core.PhysicalCoreIndex);
            },
        };

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, result.Value!.Count);
        Assert.Equal(10, controller.OffsetReadCount);
        Assert.Equal(-3, result.Value.Entries[3].Offset);
    }

    [Fact]
    public void PersistentFailureAfterFocusedRetryDoesNotRestoreTopologyGate()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: Enumerable.Range(0, 8))
        {
            GetPboOffsetHandler = (core, _) =>
                core.PhysicalCoreIndex == 3
                    ? OperationResult<int>.Fail("persistent firmware rejection")
                    : OperationResult<int>.Ok(-core.PhysicalCoreIndex),
        };

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal(7, result.Value!.Count);
        Assert.Equal(10, controller.OffsetReadCount);
        Assert.DoesNotContain(result.Value.Entries,
            entry => entry.Address.PhysicalCoreIndex == 3);
    }

    [Fact]
    public void OutOfRangeHardwareReadIsNotAdmittedAsAnOperableCore()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 2,
            enabledPhysicalCores: [0, 1])
        {
            GetPboOffsetHandler = (core, _) =>
                OperationResult<int>.Ok(
                    core.PhysicalCoreIndex == 0 ? -10 : 500),
        };

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        CompactCoreEntry entry = Assert.Single(result.Value!.Entries);
        Assert.Equal(0, entry.Address.PhysicalCoreIndex);
        Assert.Equal(-10, entry.Offset);
        Assert.Equal(4, controller.OffsetReadCount);
    }

    [Fact]
    public void MappingRequiresReadableOffsetCommand()
    {
        FakeRyzenController controller = new(8, [0, 1])
        {
            CanReadPboOffsets = false,
        };

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.False(result.Success);
        Assert.Equal(0, controller.OffsetReadCount);
    }

    [Fact]
    public void CompactMapDoesNotRequireQualifiedPhysicalTopology()
    {
        FakeRyzenController controller = new(16, Enumerable.Range(0, 12))
        {
            HasUsableCoreTopology = false,
            CoreTopologyUnavailableReason =
                "Per-core selectors are not qualified for this topology.",
        };

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal(12, result.Value!.Count);
        Assert.Equal(24, controller.OffsetReadCount);
    }

    [Fact]
    public void WholeCcdDisableMapKeepsCompactKeysAndRealSelectors()
    {
        CoreAddress[] survivingCcd = Enumerable
            .Range(8, 8)
            .Select(CoreAddress.FromPhysicalCoreIndex)
            .Reverse()
            .ToArray();
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: Enumerable.Range(8, 8))
        {
            PboOffsetCandidates = survivingCcd,
            HasUsableCoreTopology = false,
            CoreTopologyUnavailableReason = "The complete fuse map is unavailable.",
        };

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, result.Value!.Count);
        Assert.Equal(8, result.Value.Entries[0].Address.PhysicalCoreIndex);
        Assert.Equal(15, result.Value.Entries[7].Address.PhysicalCoreIndex);
    }

    [Fact]
    public void FailedSelectorReadsAreNeverAdmittedAsOperableCores()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 8,
            enabledPhysicalCores: [1, 3, 7]);

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.True(result.Success, result.Error);
        Assert.Equal([1, 3, 7], result.Value!.Entries
            .Select(entry => entry.Address.PhysicalCoreIndex));
    }

    [Fact]
    public void NoSuccessfulSelectorRetriesBeforeFailing()
    {
        FakeRyzenController controller = new(
            physicalCoreSlots: 2,
            enabledPhysicalCores: []);

        OperationResult<CompactCoreMap> result =
            CoreMapper.Map(controller);

        Assert.False(result.Success);
        Assert.Contains("No per-core offset selector", result.Error);
        Assert.Equal(6, controller.OffsetReadCount);
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
    public void UnqualifiedMobileFamiliesRemainUnqualifiedForPhysicalTopology(
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
