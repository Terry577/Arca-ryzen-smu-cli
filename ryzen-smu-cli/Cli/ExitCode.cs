namespace ryzen_smu_cli;

internal enum ExitCode
{
    Success = 0,
    NotAdministrator = 1,
    InitializationFailed = 2,
    UnsupportedOperation = 3,
    InvalidInput = 4,
    CoreOutOfRange = 5,
    OperationFailed = 6,
    DowncoreUnavailable = 7,
    CoreMappingFailed = 8,
    UnsupportedPlatform = 9,
}
