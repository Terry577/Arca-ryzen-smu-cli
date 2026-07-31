namespace ryzen_smu_cli;

internal interface IRyzenController : IDisposable
{
    CpuInformation Information { get; }

    int CcdCount { get; }

    int PhysicalCoreSlots { get; }

    int EnabledCoreCount { get; }

    IReadOnlyList<byte> FactoryCoreDisableMasks { get; }

    bool CanReadPboOffsets { get; }

    bool CanWritePboOffsets { get; }

    bool CanReadFMax { get; }

    bool CanWriteFMax { get; }

    bool CanReadVcore { get; }

    string? VcoreReadUnavailableReason { get; }

    OperationResult<int> GetPboOffset(CoreAddress core);

    OperationResult SetPboOffset(CoreAddress core, int offset);

    OperationResult<float> GetPboScalar();

    OperationResult SetPboScalar(int scalar);

    OperationResult<uint> GetFMax();

    OperationResult SetFMax(uint megahertz);

    OperationResult<double> GetVcore();

    DowncoreOperationResult SetDisabledCores(IReadOnlySet<int> physicalCoreIndices);
}
