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

    public CpuInformation Information { get; init; } = new(
        "AMD Ryzen 7 9800X3D 8-Core Processor",
        "B40F40",
        "GraniteRidge",
        "44",
        "FPX",
        1,
        1,
        8,
        "Micro-Star International Co., Ltd.",
        "MPG X870I EDGE TI EVO WIFI (MS-7E50)",
        "1.A32",
        "0B404035",
        "98.83.0");

    public int CcdCount { get; }

    public int PhysicalCoreSlots { get; }

    public int EnabledCoreCount =>
        EnabledCoreCountOverride ?? _enabledPhysicalCores.Count;

    public int? EnabledCoreCountOverride { get; init; }

    public IReadOnlyList<byte> FactoryCoreDisableMasks { get; init; }

    public bool CanReadPboOffsets { get; init; } = true;

    public bool CanWritePboOffsets { get; init; } = true;

    public bool CanReadFMax { get; init; } = true;

    public bool CanWriteFMax { get; init; } = true;

    public Dictionary<int, int> Offsets { get; } = [];

    public List<(CoreAddress Core, int Offset)> OffsetWrites { get; } = [];

    public int OffsetReadCount { get; private set; }

    public OperationResult SetOffsetResult { get; init; } = OperationResult.Ok();

    public OperationResult<float> GetScalarResult { get; init; } =
        OperationResult<float>.Ok(1.0f);

    public OperationResult SetScalarResult { get; init; } = OperationResult.Ok();

    public OperationResult<uint> GetFMaxResult { get; init; } =
        OperationResult<uint>.Ok(5250);

    public OperationResult SetFMaxResult { get; init; } = OperationResult.Ok();

    public int FMaxReadCount { get; private set; }

    public List<uint> FMaxWrites { get; } = [];

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

    public OperationResult<uint> GetFMax()
    {
        FMaxReadCount++;
        return GetFMaxResult;
    }

    public OperationResult SetFMax(uint megahertz)
    {
        FMaxWrites.Add(megahertz);
        return SetFMaxResult;
    }

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
