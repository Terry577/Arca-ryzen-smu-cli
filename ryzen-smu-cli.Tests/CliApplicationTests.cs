namespace ryzen_smu_cli.Tests;

public sealed class CliApplicationTests
{
    [Theory]
    [InlineData("--help")]
    [InlineData("--version")]
    public void InformationalOptionsDoNotInitializeHardware(string option)
    {
        int factoryCalls = 0;
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = CliApplication.Run(
            [option],
            () =>
            {
                factoryCalls++;
                throw new InvalidOperationException("Must not be called");
            },
            FakePrivilegeChecker.Administrator(),
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Theory]
    [InlineData("--set-pbo-scalar", "0")]
    [InlineData("--set-pbo-scalar", "11")]
    [InlineData("--set-pbo-scalar", "not-a-number")]
    [InlineData("--set-fmax", "0")]
    [InlineData("--set-fmax", "5226")]
    [InlineData("--set-fmax", "not-a-number")]
    [InlineData("--offset", "0:-10,-20")]
    [InlineData("--disable-cores", "1,1")]
    public void InvalidArgumentsDoNotInitializeHardware(params string[] args)
    {
        int factoryCalls = 0;

        int exitCode = CliApplication.Run(
            args,
            () =>
            {
                factoryCalls++;
                return new FakeRyzenController(8, Enumerable.Range(0, 8));
            },
            FakePrivilegeChecker.Administrator(),
            new StringWriter(),
            new StringWriter());

        Assert.NotEqual(0, exitCode);
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    public void ValidFMaxIsPassedToTheController()
    {
        FakeRyzenController controller = new(8, Enumerable.Range(0, 8));
        StringWriter output = new();

        int exitCode = CliApplication.Run(
            ["--set-fmax", "5225", "--get-fmax"],
            () => controller,
            FakePrivilegeChecker.Administrator(),
            output,
            new StringWriter());

        Assert.Equal((int)ExitCode.Success, exitCode);
        Assert.Equal([5225u], controller.FMaxWrites);
        Assert.Equal(1, controller.FMaxReadCount);
        Assert.Contains("Set FMax to 5225 MHz.", output.ToString());
        Assert.Contains("Current FMax: 5250 MHz.", output.ToString());
    }

    [Fact]
    public void ConflictingDowncoreOptionsAreRejectedBeforeHardwareAccess()
    {
        int factoryCalls = 0;

        int exitCode = CliApplication.Run(
            ["--disable-cores", "1", "--enable-all-cores"],
            () =>
            {
                factoryCalls++;
                return new FakeRyzenController(8, Enumerable.Range(0, 8));
            },
            FakePrivilegeChecker.Administrator(),
            new StringWriter(),
            new StringWriter());

        Assert.NotEqual(0, exitCode);
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    public void HardwareCommandChecksPrivilegeBeforeCreatingController()
    {
        int factoryCalls = 0;
        StringWriter error = new();

        int exitCode = CliApplication.Run(
            ["--get-pbo-scalar"],
            () =>
            {
                factoryCalls++;
                return new FakeRyzenController(8, Enumerable.Range(0, 8));
            },
            new FakePrivilegeChecker(true, false),
            new StringWriter(),
            error);

        Assert.Equal((int)ExitCode.NotAdministrator, exitCode);
        Assert.Equal(0, factoryCalls);
        Assert.Contains("administrator", error.ToString());
    }
}
