using System.Management;

namespace ryzen_smu_cli;

internal static class AmdAcpiDowncoreController
{
    private const string WmiScope = @"root\wmi";
    private const string WmiClass = "AMD_ACPI";
    private const string DowncoreCommandName = "Software Downcore Config";

    public static DowncoreOperationResult Apply(
        int ccdCount,
        IReadOnlySet<int> physicalCoreIndices)
    {
        if (ccdCount <= 0)
        {
            return DowncoreOperationResult.Fail(
                "The CPU topology did not report any CCDs.");
        }

        try
        {
            using ManagementObjectSearcher searcher = new(
                WmiScope,
                $"SELECT * FROM {WmiClass}");
            using ManagementObjectCollection instances = searcher.Get();
            using ManagementObjectCollection.ManagementObjectEnumerator enumerator =
                instances.GetEnumerator();

            if (!enumerator.MoveNext() || enumerator.Current is not ManagementObject instance)
            {
                return DowncoreOperationResult.Fail(
                    $"The motherboard did not expose {WmiClass} in {WmiScope}.");
            }

            using (instance)
            {
                OperationResult<uint> commandIdResult = FindDowncoreCommandId(instance);
                if (!commandIdResult.Success)
                {
                    return DowncoreOperationResult.Fail(commandIdResult.Error!);
                }

                List<byte> masks = new(ccdCount);
                for (int ccdIndex = 0; ccdIndex < ccdCount; ccdIndex++)
                {
                    byte disableMask = BuildDisableMask(ccdIndex, physicalCoreIndices);
                    uint commandArgument =
                        0x8000u |
                        ((uint)ccdIndex << 8) |
                        disableMask;

                    OperationResult runResult = RunCommand(
                        instance,
                        commandIdResult.Value,
                        commandArgument);
                    if (!runResult.Success)
                    {
                        return DowncoreOperationResult.Fail(
                            $"Failed to configure CCD{ccdIndex}: {runResult.Error}");
                    }

                    masks.Add(disableMask);
                }

                return DowncoreOperationResult.Ok(masks);
            }
        }
        catch (ManagementException ex)
        {
            return DowncoreOperationResult.Fail(
                $"AMD ACPI WMI operation failed: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return DowncoreOperationResult.Fail(
                $"AMD ACPI WMI access was denied: {ex.Message}");
        }
        catch (Exception ex)
        {
            return DowncoreOperationResult.Fail(
                $"AMD ACPI WMI initialization failed: {ex.Message}");
        }
    }

    internal static byte BuildDisableMask(
        int ccdIndex,
        IReadOnlySet<int> physicalCoreIndices)
    {
        byte mask = 0;
        int firstPhysicalCore = checked(ccdIndex * 8);

        for (int coreIndex = 0; coreIndex < 8; coreIndex++)
        {
            if (physicalCoreIndices.Contains(firstPhysicalCore + coreIndex))
            {
                mask |= (byte)(1 << coreIndex);
            }
        }

        return mask;
    }

    private static OperationResult<uint> FindDowncoreCommandId(
        ManagementObject instance)
    {
        foreach (string methodName in new[] { "GetObjectID", "GetObjectID2" })
        {
            using ManagementBaseObject? output = instance.InvokeMethod(
                methodName,
                null,
                null);
            if (output?["pack"] is not ManagementBaseObject pack)
            {
                continue;
            }

            using (pack)
            {
                uint[] ids = (uint[]?)pack["ID"] ?? [];
                string[] names = (string[]?)pack["IDString"] ?? [];
                int length = Math.Min(
                    Convert.ToInt32(pack["Length"]),
                    Math.Min(ids.Length, names.Length));

                for (int index = 0; index < length; index++)
                {
                    if (names[index].Contains(
                            DowncoreCommandName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return OperationResult<uint>.Ok(ids[index]);
                    }
                }
            }
        }

        return OperationResult<uint>.Fail(
            $"The motherboard's {WmiClass} interface does not expose " +
            $"'{DowncoreCommandName}'.");
    }

    private static OperationResult RunCommand(
        ManagementObject instance,
        uint commandId,
        uint commandArgument)
    {
        using ManagementBaseObject input = instance.GetMethodParameters("RunCommand");
        byte[] buffer = new byte[8];
        Buffer.BlockCopy(BitConverter.GetBytes(commandId), 0, buffer, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(commandArgument), 0, buffer, 4, 4);
        input["Inbuf"] = buffer;

        using ManagementBaseObject? output = instance.InvokeMethod(
            "RunCommand",
            input,
            null);
        if (output?["Outbuf"] is not ManagementBaseObject pack)
        {
            return OperationResult.Fail(
                "RunCommand returned no output buffer.");
        }

        using (pack)
        {
            bool hasResult = pack["Result"] is byte[];
            return hasResult
                ? OperationResult.Ok()
                : OperationResult.Fail("RunCommand returned no result.");
        }
    }
}
