namespace ryzen_smu_cli;

internal sealed class PawnIoNotInstalledException : Exception
{
    public PawnIoNotInstalledException()
        : base("PawnIO is not installed.")
    {
    }
}
