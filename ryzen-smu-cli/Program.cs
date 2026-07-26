namespace ryzen_smu_cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        return CliApplication.Run(
            args,
            () => new ZenStatesRyzenController(),
            new WindowsPrivilegeChecker(),
            Console.Out,
            Console.Error);
    }
}
