namespace ryzen_smu_cli;

internal static class CoreMapper
{
    private const int MaximumAttempts = 3;

    public static OperationResult<IReadOnlyDictionary<int, CoreAddress>> Map(
        IRyzenController controller)
    {
        if (!controller.CanReadPboOffsets)
        {
            return OperationResult<IReadOnlyDictionary<int, CoreAddress>>.Fail(
                "This CPU does not expose the SMU command required to read per-core offsets.");
        }

        int bestCount = 0;

        for (int attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            Dictionary<int, CoreAddress> mapping = [];

            for (int physicalCoreIndex = 0;
                 physicalCoreIndex < controller.PhysicalCoreSlots;
                 physicalCoreIndex++)
            {
                CoreAddress address =
                    CoreAddress.FromPhysicalCoreIndex(physicalCoreIndex);
                OperationResult<int> offsetResult = controller.GetPboOffset(address);
                if (offsetResult.Success)
                {
                    mapping.Add(mapping.Count, address);
                }
            }

            bestCount = Math.Max(bestCount, mapping.Count);
            if (mapping.Count == controller.EnabledCoreCount)
            {
                return OperationResult<IReadOnlyDictionary<int, CoreAddress>>.Ok(mapping);
            }
        }

        return OperationResult<IReadOnlyDictionary<int, CoreAddress>>.Fail(
            $"Could not map all enabled cores after {MaximumAttempts} attempts: " +
            $"expected {controller.EnabledCoreCount}, found {bestCount}.");
    }
}
