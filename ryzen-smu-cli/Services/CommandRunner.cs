using System.Globalization;

namespace ryzen_smu_cli;

internal sealed class CommandRunner
{
    private readonly Func<IRyzenController> _controllerFactory;
    private readonly IPrivilegeChecker _privilegeChecker;
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    public CommandRunner(
        Func<IRyzenController> controllerFactory,
        IPrivilegeChecker privilegeChecker,
        TextWriter output,
        TextWriter error)
    {
        _controllerFactory = controllerFactory;
        _privilegeChecker = privilegeChecker;
        _output = output;
        _error = error;
    }

    public int Execute(CliRequest request)
    {
        if (!_privilegeChecker.IsWindows)
        {
            _error.WriteLine("This application can only access the SMU on Windows.");
            return (int)ExitCode.UnsupportedPlatform;
        }

        if (!_privilegeChecker.IsAdministrator)
        {
            _error.WriteLine("This application must be run as an administrator.");
            return (int)ExitCode.NotAdministrator;
        }

        IRyzenController controller;
        try
        {
            controller = _controllerFactory();
        }
        catch (PawnIoNotInstalledException)
        {
            _error.WriteLine(
                "PawnIO is required for Ryzen SMU access but is not installed.");
            _error.WriteLine(
                "Install the signed PawnIO driver from https://pawnio.eu/.");
            _error.WriteLine(
                "The self-contained-with-pawnio release package includes the official " +
                "installer. Run it interactively as an administrator, then retry this command.");
            return (int)ExitCode.InitializationFailed;
        }
        catch (Exception ex)
        {
            _error.WriteLine($"Failed to initialize Ryzen hardware access: {ex.Message}");
            return (int)ExitCode.InitializationFailed;
        }

        using (controller)
        {
            try
            {
                return Execute(request, controller);
            }
            catch (Exception ex)
            {
                _error.WriteLine($"Hardware operation failed: {ex.Message}");
                return (int)ExitCode.OperationFailed;
            }
        }
    }

    private int Execute(CliRequest request, IRyzenController controller)
    {
        IReadOnlyDictionary<int, CoreAddress>? coreMap = null;

        if (NeedsCoreMap(request))
        {
            OperationResult<IReadOnlyDictionary<int, CoreAddress>> mapResult =
                CoreMapper.Map(controller);
            if (!mapResult.Success)
            {
                _error.WriteLine(mapResult.Error);
                return (int)ExitCode.CoreMappingFailed;
            }

            coreMap = mapResult.Value!;
        }

        int validationResult = ValidateRequest(request, controller, coreMap);
        if (validationResult != (int)ExitCode.Success)
        {
            return validationResult;
        }

        bool rebootRequired = false;

        if (request.ShowInfo)
        {
            WriteCpuInformation(controller.Information);
        }

        if (request.OffsetSpecification is not null)
        {
            foreach (OffsetAssignment assignment in
                     request.OffsetSpecification.Assignments)
            {
                CoreAddress core = coreMap![assignment.EnabledCoreIndex];
                OperationResult result =
                    controller.SetPboOffset(core, assignment.Offset);
                if (!result.Success)
                {
                    _error.WriteLine(result.Error);
                    return (int)ExitCode.OperationFailed;
                }

                _output.WriteLine(
                    $"Set enabled core {assignment.EnabledCoreIndex}, physical core " +
                    $"{core.PhysicalCoreIndex} offset to {assignment.Offset}.");
            }
        }

        if (request.DisabledCores is not null || request.EnableAllCores)
        {
            IReadOnlySet<int> disabledCores =
                request.DisabledCores?.PhysicalCoreIndices ?? new HashSet<int>();
            DowncoreOperationResult result =
                controller.SetDisabledCores(disabledCores);
            if (!result.Success)
            {
                _error.WriteLine(result.Error);
                return (int)ExitCode.DowncoreUnavailable;
            }

            if (result.DisableMasks.Count != controller.CcdCount)
            {
                _error.WriteLine(
                    "AMD ACPI returned an incomplete core-disable result.");
                return (int)ExitCode.DowncoreUnavailable;
            }

            for (int ccdIndex = 0; ccdIndex < result.DisableMasks.Count; ccdIndex++)
            {
                string bitmap = Convert
                    .ToString(result.DisableMasks[ccdIndex], 2)
                    .PadLeft(8, '0');
                _output.WriteLine(
                    $"New core disable bitmap for CCD{ccdIndex}: {bitmap}");
            }

            rebootRequired = true;
        }

        if (request.GetOffsetsTerse)
        {
            List<string> offsets = new(coreMap!.Count);
            foreach (CoreAddress core in coreMap.Values)
            {
                OperationResult<int> result = controller.GetPboOffset(core);
                if (!result.Success)
                {
                    _error.WriteLine(result.Error);
                    return (int)ExitCode.OperationFailed;
                }

                offsets.Add(result.Value.ToString()!);
            }

            _output.WriteLine(string.Join(",", offsets));
        }

        if (request.GetPhysicalCores)
        {
            _output.WriteLine("Factory-fused status of physical core slots:");
            for (int physicalCoreIndex = 0;
                 physicalCoreIndex < controller.PhysicalCoreSlots;
                 physicalCoreIndex++)
            {
                CoreAddress core =
                    CoreAddress.FromPhysicalCoreIndex(physicalCoreIndex);
                bool disabled =
                    (controller.FactoryCoreDisableMasks[core.CcdIndex] &
                     (1 << core.CoreIndex)) != 0;
                _output.WriteLine(
                    $"Physical core {physicalCoreIndex}: " +
                    (disabled ? "Disabled" : "Enabled"));
            }
        }

        if (request.GetEnabledCores)
        {
            Dictionary<int, int> enabledCoreByPhysicalIndex = coreMap!
                .ToDictionary(pair => pair.Value.PhysicalCoreIndex, pair => pair.Key);

            for (int physicalCoreIndex = 0;
                 physicalCoreIndex < controller.PhysicalCoreSlots;
                 physicalCoreIndex++)
            {
                if (enabledCoreByPhysicalIndex.TryGetValue(
                        physicalCoreIndex,
                        out int enabledCoreIndex))
                {
                    _output.WriteLine(
                        $"Physical core {physicalCoreIndex}: Enabled " +
                        $"(enabled core {enabledCoreIndex})");
                }
                else
                {
                    _output.WriteLine(
                        $"Physical core {physicalCoreIndex}: Disabled");
                }
            }
        }

        if (request.PboScalar is int scalar)
        {
            OperationResult result = controller.SetPboScalar(scalar);
            if (!result.Success)
            {
                _error.WriteLine(result.Error);
                return (int)ExitCode.OperationFailed;
            }

            _output.WriteLine($"Set PBO scalar to {scalar}.");
        }

        if (request.GetPboScalar)
        {
            OperationResult<float> result = controller.GetPboScalar();
            if (!result.Success)
            {
                _error.WriteLine(result.Error);
                return (int)ExitCode.OperationFailed;
            }

            _output.WriteLine(
                $"Current PBO scalar: " +
                result.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (request.FMax is FMaxFrequency fMax)
        {
            OperationResult result = controller.SetFMax(fMax.Megahertz);
            if (!result.Success)
            {
                _error.WriteLine(result.Error);
                return (int)ExitCode.OperationFailed;
            }

            _output.WriteLine($"Set FMax to {fMax.Megahertz} MHz.");
        }

        if (request.GetFMax)
        {
            OperationResult<uint> result = controller.GetFMax();
            if (!result.Success)
            {
                _error.WriteLine(result.Error);
                return (int)ExitCode.OperationFailed;
            }

            _output.WriteLine($"Current FMax: {result.Value} MHz.");
        }

        if (rebootRequired)
        {
            _output.WriteLine("A reboot is required for changes to take effect.");
        }

        return (int)ExitCode.Success;
    }

    private void WriteCpuInformation(CpuInformation information)
    {
        WriteInformationLine("CPU", information.CpuName);
        WriteInformationLine(
            "CPUID",
            $"{information.CpuId} ({information.CodeName})");
        WriteInformationLine("Model", information.Model);
        WriteInformationLine("Package", information.Package);
        WriteInformationLine(
            "Config",
            $"{information.CcdCount} CCD / {information.CcxCount} CCX / " +
            $"{information.PhysicalCoreCount} physical cores");
        WriteInformationLine("MB Vendor", information.MotherboardVendor);
        WriteInformationLine("MB Model", information.MotherboardModel);
        WriteInformationLine("BIOS", information.BiosVersion);
        WriteInformationLine("Firmware", information.FirmwareVersion);
        WriteInformationLine("SMU", information.SmuVersion);
    }

    private void WriteInformationLine(string label, string value)
    {
        _output.WriteLine($"{label,-11}{value}");
    }

    private int ValidateRequest(
        CliRequest request,
        IRyzenController controller,
        IReadOnlyDictionary<int, CoreAddress>? coreMap)
    {
        if (request.OffsetSpecification is not null)
        {
            if (!controller.CanWritePboOffsets)
            {
                _error.WriteLine(
                    "This CPU does not expose the SMU command required to set per-core offsets.");
                return (int)ExitCode.UnsupportedOperation;
            }

            foreach (OffsetAssignment assignment in
                     request.OffsetSpecification.Assignments)
            {
                if (!coreMap!.ContainsKey(assignment.EnabledCoreIndex))
                {
                    _error.WriteLine(
                        $"Enabled core {assignment.EnabledCoreIndex} is out of range. " +
                        $"This system has {coreMap.Count} enabled cores, indexed from 0.");
                    return (int)ExitCode.CoreOutOfRange;
                }
            }
        }

        if (request.DisabledCores is not null)
        {
            if (request.DisabledCores.PhysicalCoreIndices.Count >=
                controller.PhysicalCoreSlots)
            {
                _error.WriteLine(
                    "Refusing to disable every physical core slot.");
                return (int)ExitCode.InvalidInput;
            }

            foreach (int physicalCoreIndex in request.DisabledCores.PhysicalCoreIndices)
            {
                if (physicalCoreIndex >= controller.PhysicalCoreSlots)
                {
                    _error.WriteLine(
                        $"Physical core {physicalCoreIndex} is out of range. " +
                        $"This topology has {controller.PhysicalCoreSlots} physical core " +
                        "slots, indexed from 0.");
                    return (int)ExitCode.CoreOutOfRange;
                }
            }
        }

        if (request.GetPhysicalCores &&
            controller.FactoryCoreDisableMasks.Count < controller.CcdCount)
        {
            _error.WriteLine(
                "ZenStates-Core returned an incomplete factory core-disable map.");
            return (int)ExitCode.InitializationFailed;
        }

        if (request.FMax is not null && !controller.CanWriteFMax)
        {
            _error.WriteLine(
                "This CPU does not expose the SMU command required to set FMax.");
            return (int)ExitCode.UnsupportedOperation;
        }

        if (request.GetFMax && !controller.CanReadFMax)
        {
            _error.WriteLine(
                "This CPU does not expose the SMU command required to read FMax.");
            return (int)ExitCode.UnsupportedOperation;
        }

        return (int)ExitCode.Success;
    }

    private static bool NeedsCoreMap(CliRequest request)
    {
        return request.OffsetSpecification is not null ||
               request.GetOffsetsTerse ||
               request.GetEnabledCores;
    }
}
