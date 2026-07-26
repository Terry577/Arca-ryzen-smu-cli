namespace ryzen_smu_cli;

internal interface IRyzenController : IDisposable
{
    int CcdCount { get; }

    int PhysicalCoreSlots { get; }

    int EnabledCoreCount { get; }

    IReadOnlyList<byte> FactoryCoreDisableMasks { get; }

    bool CanReadPboOffsets { get; }

    bool CanWritePboOffsets { get; }

    OperationResult<int> GetPboOffset(CoreAddress core);

    OperationResult SetPboOffset(CoreAddress core, int offset);

    OperationResult<float> GetPboScalar();

    OperationResult SetPboScalar(int scalar);

    DowncoreOperationResult SetDisabledCores(IReadOnlySet<int> physicalCoreIndices);
}
