namespace ryzen_smu_cli;

internal interface IPrivilegeChecker
{
    bool IsWindows { get; }

    bool IsAdministrator { get; }
}
