namespace ryzen_smu_cli;

internal sealed record CliRequest(
    OffsetSpecification? OffsetSpecification,
    CoreSelection? DisabledCores,
    bool EnableAllCores,
    bool GetOffsetsTerse,
    bool GetPhysicalCores,
    bool GetEnabledCores,
    int? PboScalar,
    bool GetPboScalar,
    FMaxFrequency? FMax,
    bool GetFMax,
    bool ShowInfo)
{
    public bool HasOperation =>
        OffsetSpecification is not null ||
        DisabledCores is not null ||
        EnableAllCores ||
        GetOffsetsTerse ||
        GetPhysicalCores ||
        GetEnabledCores ||
        PboScalar is not null ||
        GetPboScalar ||
        FMax is not null ||
        GetFMax ||
        ShowInfo;
}
