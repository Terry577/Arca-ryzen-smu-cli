namespace ryzen_smu_cli;

internal readonly record struct CompactCoreEntry(
    int CompactIndex,
    CoreAddress Address,
    int Offset);

internal sealed record CompactCoreMap(IReadOnlyList<CompactCoreEntry> Entries)
{
    public int Count => Entries.Count;

    public bool ContainsKey(int compactIndex) =>
        compactIndex >= 0 && compactIndex < Entries.Count;

    public CoreAddress GetAddress(int compactIndex) =>
        Entries[compactIndex].Address;

    public CompactCoreMap WithOffset(int compactIndex, int offset)
    {
        CompactCoreEntry[] updated = Entries.ToArray();
        updated[compactIndex] = updated[compactIndex] with
        {
            Offset = offset,
        };
        return new CompactCoreMap(updated);
    }
}

internal static class CoreMapper
{
    private const int MaximumProbePasses = 3;

    public static OperationResult<CompactCoreMap> Map(
        IRyzenController controller)
    {
        if (!controller.CanReadPboOffsets)
        {
            return OperationResult<CompactCoreMap>.Fail(
                "This CPU does not expose the SMU command required to read per-core offsets.");
        }

        CoreAddress[] primaryCandidates = OrderCandidates(
            controller.PboOffsetCandidates);
        if (primaryCandidates.Length == 0)
        {
            return OperationResult<CompactCoreMap>.Fail(
                "No SMU per-core offset selectors are available to probe.");
        }

        CoreAddress[] fallbackCandidates = OrderCandidates(
            controller.PboOffsetFallbackCandidates
                .Except(primaryCandidates));
        OperationResult<CompactCoreMap> primaryResult = ProbeCandidates(
            controller,
            primaryCandidates,
            MaximumProbePasses,
            failFastWhenFirstPassHasNoSuccess: fallbackCandidates.Length > 0);
        if (primaryResult.Success || fallbackCandidates.Length == 0)
        {
            return primaryResult;
        }

        OperationResult<CompactCoreMap> fallbackResult = ProbeCandidates(
            controller,
            fallbackCandidates,
            MaximumProbePasses,
            failFastWhenFirstPassHasNoSuccess: false);
        if (fallbackResult.Success)
        {
            return fallbackResult;
        }

        // If both the primary bitmap and its alternate selector failed, give
        // the original range its two remaining transient-recovery passes
        // before returning failure.
        return ProbeCandidates(
            controller,
            primaryCandidates,
            MaximumProbePasses - 1,
            failFastWhenFirstPassHasNoSuccess: false);
    }

    private static OperationResult<CompactCoreMap> ProbeCandidates(
        IRyzenController controller,
        CoreAddress[] candidates,
        int maximumPasses,
        bool failFastWhenFirstPassHasNoSuccess)
    {
        Dictionary<CoreAddress, int> successfulOffsets = [];
        CoreAddress[] remainingCandidates = candidates;

        for (int pass = 1; pass <= maximumPasses; pass++)
        {
            List<CoreAddress> failedCandidates = [];

            foreach (CoreAddress address in remainingCandidates)
            {
                OperationResult<int> offsetResult =
                    controller.GetPboOffset(address);
                if (offsetResult.Success &&
                    offsetResult.Value is >= OffsetSpecification.MinimumOffset and
                        <= OffsetSpecification.MaximumOffset)
                {
                    successfulOffsets[address] = offsetResult.Value;
                }
                else
                {
                    failedCandidates.Add(address);
                }
            }

            // Only a fully successful candidate pass can finish early. The
            // topology-derived enabled-core count is diagnostic metadata, not
            // proof that a failed selector is fused off: a stale count can
            // otherwise make a transient first-pass failure permanently
            // shorten and shift the compact map.
            if (successfulOffsets.Count > 0 &&
                failedCandidates.Count == 0)
            {
                return OperationResult<CompactCoreMap>.Ok(BuildMap(
                    candidates,
                    successfulOffsets));
            }

            if (pass == 1 &&
                successfulOffsets.Count == 0 &&
                failFastWhenFirstPassHasNoSuccess)
            {
                return OperationResult<CompactCoreMap>.Fail(
                    "No primary per-core offset selector answered successfully.");
            }

            remainingCandidates = failedCandidates.ToArray();
        }

        if (successfulOffsets.Count > 0)
        {
            return OperationResult<CompactCoreMap>.Ok(BuildMap(
                candidates,
                successfulOffsets));
        }

        return OperationResult<CompactCoreMap>.Fail(
            "No per-core offset selector answered successfully after all retry attempts.");
    }

    private static CoreAddress[] OrderCandidates(
        IEnumerable<CoreAddress> candidates) => candidates
            .Distinct()
            .OrderBy(address => address.PhysicalCoreIndex)
            .ThenBy(address => address.CcdIndex)
            .ThenBy(address => address.CoreIndex)
            .ToArray();

    private static CompactCoreMap BuildMap(
        IReadOnlyList<CoreAddress> orderedCandidates,
        IReadOnlyDictionary<CoreAddress, int> successfulOffsets)
    {
        List<CompactCoreEntry> entries = [];
        foreach (CoreAddress address in orderedCandidates)
        {
            if (successfulOffsets.TryGetValue(address, out int offset))
            {
                entries.Add(new CompactCoreEntry(
                    entries.Count,
                    address,
                    offset));
            }
        }

        return new CompactCoreMap(entries);
    }
}
