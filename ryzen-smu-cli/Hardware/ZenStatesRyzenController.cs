using ZenStates.Core;

namespace ryzen_smu_cli;

internal sealed class ZenStatesRyzenController : IRyzenController
{
    private readonly Cpu _cpu;
    private bool _disposed;

    public ZenStatesRyzenController()
    {
        _cpu = new Cpu();
        try
        {
            ValidateTopology();
            FactoryCoreDisableMasks = _cpu.info.topology.coreDisableMap
                .Select(mask => (byte)(mask & 0xff))
                .ToArray();
        }
        catch
        {
            _cpu.Dispose();
            throw;
        }
    }

    public int CcdCount => checked((int)_cpu.info.topology.ccds);

    public int PhysicalCoreSlots => checked((int)_cpu.info.topology.physicalCores);

    public int EnabledCoreCount => checked((int)_cpu.info.topology.cores);

    public IReadOnlyList<byte> FactoryCoreDisableMasks { get; }

    public bool CanReadPboOffsets =>
        _cpu.smu.Rsmu.SMU_MSG_GetDldoPsmMargin > 0;

    public bool CanWritePboOffsets =>
        _cpu.smu.Rsmu.SMU_MSG_SetDldoPsmMargin > 0 ||
        _cpu.smu.Mp1Smu.SMU_MSG_SetDldoPsmMargin > 0;

    private void ValidateTopology()
    {
        if (CcdCount is < 1 or > 16)
        {
            throw new InvalidOperationException(
                $"Unsupported CCD count reported by ZenStates-Core: {CcdCount}.");
        }

        if (PhysicalCoreSlots != checked(CcdCount * 8))
        {
            throw new InvalidOperationException(
                "ZenStates-Core returned an inconsistent physical-core topology.");
        }

        if (EnabledCoreCount is < 1 || EnabledCoreCount > PhysicalCoreSlots)
        {
            throw new InvalidOperationException(
                "ZenStates-Core returned an invalid enabled-core count.");
        }
    }

    public OperationResult<int> GetPboOffset(CoreAddress core)
    {
        uint? value = _cpu.GetPsmMarginSingleCore(
            (uint)core.CoreIndex,
            (uint)core.CcdIndex,
            (uint)core.CcxIndex);
        return value.HasValue
            ? OperationResult<int>.Ok(unchecked((int)value.Value))
            : OperationResult<int>.Fail(
                $"The SMU did not return an offset for physical core {core.PhysicalCoreIndex}.");
    }

    public OperationResult SetPboOffset(CoreAddress core, int offset)
    {
        bool success = _cpu.SetPsmMarginSingleCore(
            (uint)core.CoreIndex,
            (uint)core.CcdIndex,
            (uint)core.CcxIndex,
            offset);
        return success
            ? OperationResult.Ok()
            : OperationResult.Fail(
                $"The SMU rejected offset {offset} for physical core " +
                $"{core.PhysicalCoreIndex}.");
    }

    public OperationResult<float> GetPboScalar()
    {
        if (_cpu.smu.Rsmu.SMU_MSG_GetPBOScalar == 0)
        {
            return OperationResult<float>.Fail(
                "This CPU does not expose the SMU command required to read the PBO scalar.");
        }

        uint[] arguments = new uint[_cpu.smu.Rsmu.MAX_ARGS];
        SMU.Status status = _cpu.smu.SendRsmuCommand(
            _cpu.smu.Rsmu.SMU_MSG_GetPBOScalar,
            ref arguments);
        if (status != SMU.Status.OK)
        {
            return OperationResult<float>.Fail(
                $"The SMU failed to read the PBO scalar ({status}).");
        }

        float scalar = BitConverter.UInt32BitsToSingle(arguments[0]);
        return scalar is >= 0 and <= 10
            ? OperationResult<float>.Ok(scalar)
            : OperationResult<float>.Fail(
                $"The SMU returned an invalid PBO scalar value ({scalar}).");
    }

    public OperationResult SetPboScalar(int scalar)
    {
        if (_cpu.smu.Rsmu.SMU_MSG_SetPBOScalar == 0)
        {
            return OperationResult.Fail(
                "This CPU does not expose the SMU command required to set the PBO scalar.");
        }

        uint[] arguments = new uint[_cpu.smu.Rsmu.MAX_ARGS];
        arguments[0] = checked((uint)scalar * 100);
        SMU.Status status = _cpu.smu.SendRsmuCommand(
            _cpu.smu.Rsmu.SMU_MSG_SetPBOScalar,
            ref arguments);
        return status == SMU.Status.OK
            ? OperationResult.Ok()
            : OperationResult.Fail(
                $"The SMU rejected PBO scalar {scalar} ({status}).");
    }

    public DowncoreOperationResult SetDisabledCores(
        IReadOnlySet<int> physicalCoreIndices)
    {
        return AmdAcpiDowncoreController.Apply(CcdCount, physicalCoreIndices);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cpu.Dispose();
        _disposed = true;
    }
}
