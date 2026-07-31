namespace ryzen_smu_cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        using CancellationTokenSource cancellationSource = new();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            return CliApplication.RunWithCancellation(
                args,
                () => CreateController(Console.Error),
                new WindowsPrivilegeChecker(),
                Console.Out,
                Console.Error,
                cancellationSource.Token);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static IRyzenController CreateController(TextWriter diagnostics)
    {
        TextWriter originalOutput = Console.Out;
        try
        {
            // ZenStates-Core still writes a few initialization diagnostics to
            // Console.Out. Keep machine-readable command stdout uncontaminated.
            Console.SetOut(diagnostics);
            return new ZenStatesRyzenController();
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }
}
