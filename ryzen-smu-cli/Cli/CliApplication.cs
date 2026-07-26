using System.CommandLine;
using System.Globalization;

namespace ryzen_smu_cli;

internal static class CliApplication
{
    public static int Run(
        string[] args,
        Func<IRyzenController> controllerFactory,
        IPrivilegeChecker privilegeChecker,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(controllerFactory);
        ArgumentNullException.ThrowIfNull(privilegeChecker);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        RootCommand rootCommand = CreateRootCommand(controllerFactory, privilegeChecker, output, error);
        string[] effectiveArgs = args.Length == 0 ? ["--help"] : args;

        InvocationConfiguration invocationConfiguration = new()
        {
            Output = output,
            Error = error,
        };

        return rootCommand.Parse(effectiveArgs).Invoke(invocationConfiguration);
    }

    internal static RootCommand CreateRootCommand(
        Func<IRyzenController> controllerFactory,
        IPrivilegeChecker privilegeChecker,
        TextWriter output,
        TextWriter error)
    {
        Option<string?> offsetOption = new("--offset")
        {
            Description =
                "Set Curve Optimizer offsets. Use a comma-separated positional list such as " +
                "-10,5,-20 or enabled-core assignments such as 0:-10,1:5,2:-20. " +
                $"Offsets must be between {OffsetSpecification.MinimumOffset} and " +
                $"{OffsetSpecification.MaximumOffset}.",
        };
        offsetOption.Validators.Add(result =>
        {
            string? value = result.GetValue(offsetOption);
            if (value is not null &&
                !OffsetSpecification.TryParse(value, out _, out string? validationError))
            {
                result.AddError(validationError!);
            }
        });

        Option<string?> disableCoresOption = new("--disable-cores")
        {
            Description =
                "Set the complete list of physical core slots to disable, for example 0,1,4,7. " +
                "Unspecified slots are enabled. The change requires a reboot.",
        };
        disableCoresOption.Validators.Add(result =>
        {
            string? value = result.GetValue(disableCoresOption);
            if (value is not null &&
                !CoreSelection.TryParse(value, out _, out string? validationError))
            {
                result.AddError(validationError!);
            }
        });

        Option<bool> enableAllCoresOption = new("--enable-all-cores")
        {
            Description = "Enable every physical core slot. The change requires a reboot.",
        };
        Option<bool> getOffsetsTerseOption = new("--get-offsets-terse")
        {
            Description =
                "Print Curve Optimizer offsets for enabled cores as one comma-separated line.",
        };
        Option<bool> getPhysicalCoresOption = new("--get-physical-cores")
        {
            Description = "Print the factory-fused status of every physical core slot.",
        };
        Option<bool> getEnabledCoresOption = new("--get-enabled-cores")
        {
            Description =
                "Print enabled/disabled status and the enabled-core to physical-slot mapping.",
        };
        Option<string?> setPboScalarOption = new("--set-pbo-scalar")
        {
            Description = "Set the PBO scalar to a whole number from 1 through 10.",
        };
        setPboScalarOption.Validators.Add(result =>
        {
            string? rawValue = result.GetValue(setPboScalarOption);
            if (rawValue is null)
            {
                return;
            }

            if (!int.TryParse(
                    rawValue,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int value) ||
                value is < 1 or > 10)
            {
                result.AddError("PBO scalar must be a whole number from 1 through 10.");
            }
        });

        Option<bool> getPboScalarOption = new("--get-pbo-scalar")
        {
            Description = "Print the current PBO scalar.",
        };
        Option<string?> setFMaxOption = new("--set-fmax")
        {
            Description =
                $"Set the maximum boost-frequency limit in MHz. The value must use " +
                $"{FMaxFrequency.StepMegahertz} MHz steps, for example 5225 or 5250.",
        };
        setFMaxOption.Validators.Add(result =>
        {
            string? value = result.GetValue(setFMaxOption);
            if (value is not null &&
                !FMaxFrequency.TryParse(value, out _, out string? validationError))
            {
                result.AddError(validationError!);
            }
        });
        Option<bool> getFMaxOption = new("--get-fmax")
        {
            Description = "Print the current maximum boost-frequency limit in MHz.",
        };

        RootCommand rootCommand = new("A CLI for the Ryzen System Management Unit (SMU).")
        {
            Options =
            {
                offsetOption,
                disableCoresOption,
                enableAllCoresOption,
                getOffsetsTerseOption,
                getPhysicalCoresOption,
                getEnabledCoresOption,
                setPboScalarOption,
                getPboScalarOption,
                setFMaxOption,
                getFMaxOption,
            },
        };

        rootCommand.Validators.Add(result =>
        {
            if (result.GetValue(disableCoresOption) is not null &&
                result.GetValue(enableAllCoresOption))
            {
                result.AddError(
                    "--disable-cores and --enable-all-cores cannot be used together.");
            }
        });

        rootCommand.SetAction(parseResult =>
        {
            OffsetSpecification? offsetSpecification = null;
            string? rawOffsets = parseResult.GetValue(offsetOption);
            if (rawOffsets is not null)
            {
                OffsetSpecification.TryParse(rawOffsets, out offsetSpecification, out _);
            }

            CoreSelection? disabledCores = null;
            string? rawDisabledCores = parseResult.GetValue(disableCoresOption);
            if (rawDisabledCores is not null)
            {
                CoreSelection.TryParse(rawDisabledCores, out disabledCores, out _);
            }

            FMaxFrequency? fMax = null;
            string? rawFMax = parseResult.GetValue(setFMaxOption);
            if (rawFMax is not null)
            {
                FMaxFrequency.TryParse(rawFMax, out FMaxFrequency parsedFMax, out _);
                fMax = parsedFMax;
            }

            CliRequest request = new(
                offsetSpecification,
                disabledCores,
                parseResult.GetValue(enableAllCoresOption),
                parseResult.GetValue(getOffsetsTerseOption),
                parseResult.GetValue(getPhysicalCoresOption),
                parseResult.GetValue(getEnabledCoresOption),
                int.TryParse(
                    parseResult.GetValue(setPboScalarOption),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int scalar)
                    ? scalar
                    : null,
                parseResult.GetValue(getPboScalarOption),
                fMax,
                parseResult.GetValue(getFMaxOption));

            if (!request.HasOperation)
            {
                error.WriteLine("No operation was specified. Use --help to list the options.");
                return (int)ExitCode.InvalidInput;
            }

            CommandRunner runner = new(controllerFactory, privilegeChecker, output, error);
            return runner.Execute(request);
        });

        return rootCommand;
    }
}
