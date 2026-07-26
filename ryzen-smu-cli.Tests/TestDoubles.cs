namespace ryzen_smu_cli.Tests;

internal sealed class FakePrivilegeChecker(
    bool isWindows,
    bool isAdministrator) : IPrivilegeChecker
{
    public bool IsWindows { get; } = isWindows;

    public bool IsAdministrator { get; } = isAdministrator;

    public static FakePrivilegeChecker Administrator() => new(true, true);
}

internal sealed class FakeRyzenController : IRyzenController
{
    private readonly HashSet<int> _enabledPhysicalCores;

    public FakeRyzenController(
        int physicalCoreSlots,
        IEnumerable<int> enabledPhysicalCores)
    {
        PhysicalCoreSlots = physicalCoreSlots;
        _enabledPhysicalCores = enabledPhysicalCores.ToHashSet();
        CcdCount = (physicalCoreSlots + 7) / 8;
        FactoryCoreDisableMasks = Enumerable
            .Repeat((byte)0, CcdCount)
            .ToArray();

        foreach (int physicalCore in _enabledPhysicalCores)
        {
            Offsets[physicalCore] = 0;
        }
    }

    public int CcdCount { get; }

    public int PhysicalCoreSlots { get; }

    public int EnabledCoreCount =>
        EnabledCoreCountOverride ?? _enabledPhysicalCores.Count;

    public int? EnabledCoreCountOverride { get; init; }

    public IReadOnlyList<byte> FactoryCoreDisableMasks { get; init; }

    public bool CanReadPboOffsets { get; init; } = true;

    public bool CanWritePboOffsets { get; init; } = true;

    public Dictionary<int, int> Offsets { get; } = [];

    public List<(CoreAddress Core, int Offset)> OffsetWrites { get; } = [];

    public int OffsetReadCount { get; private set; }

    public OperationResult SetOffsetResult { get; init; } = OperationResult.Ok();

    public OperationResult<float> GetScalarResult { get; init; } =
        OperationResult<float>.Ok(1.0f);

    public OperationResult SetScalarResult { get; init; } = OperationResult.Ok();

    public DowncoreOperationResult? DowncoreResult { get; init; }

    public int DowncoreWriteCount { get; private set; }

    public IReadOnlySet<int>? LastDisabledCores { get; private set; }

    public bool Disposed { get; private set; }

    public OperationResult<int> GetPboOffset(CoreAddress core)
    {
        OffsetReadCount++;
        return Offsets.TryGetValue(core.PhysicalCoreIndex, out int value)
            ? OperationResult<int>.Ok(value)
            : OperationResult<int>.Fail("disabled");
    }

    public OperationResult SetPboOffset(CoreAddress core, int offset)
    {
        OffsetWrites.Add((core, offset));
        if (SetOffsetResult.Success)
        {
            Offsets[core.PhysicalCoreIndex] = offset;
        }

        return SetOffsetResult;
    }

    public OperationResult<float> GetPboScalar() => GetScalarResult;

    public OperationResult SetPboScalar(int scalar) => SetScalarResult;

    public DowncoreOperationResult SetDisabledCores(
        IReadOnlySet<int> physicalCoreIndices)
    {
        DowncoreWriteCount++;
        LastDisabledCores = physicalCoreIndices.ToHashSet();
        return DowncoreResult ??
               DowncoreOperationResult.Ok(
                   Enumerable.Repeat((byte)0, CcdCount).ToArray());
    }

    public void Dispose()
    {
        Disposed = true;
    }
}
