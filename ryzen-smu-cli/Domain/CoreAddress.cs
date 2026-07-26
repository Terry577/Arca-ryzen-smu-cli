namespace ryzen_smu_cli;

internal readonly record struct CoreAddress(int PhysicalCoreIndex, int CcdIndex, int CoreIndex)
{
    private const int CoresPerCcd = 8;
    private const int CoresPerCcx = 4;
    private const int MaximumCcdCount = 16;

    public int CcxIndex => CoreIndex / CoresPerCcx;

    public int CoreIndexWithinCcx => CoreIndex % CoresPerCcx;

    public static CoreAddress FromPhysicalCoreIndex(int physicalCoreIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(physicalCoreIndex);

        int ccdIndex = physicalCoreIndex / CoresPerCcd;
        if (ccdIndex >= MaximumCcdCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(physicalCoreIndex),
                physicalCoreIndex,
                $"The SMU core mask supports at most {MaximumCcdCount} CCDs.");
        }

        return new CoreAddress(
            physicalCoreIndex,
            ccdIndex,
            physicalCoreIndex % CoresPerCcd);
    }
}
