namespace ryzen_smu_cli;

internal sealed record CpuInformation(
    string CpuName,
    string CpuId,
    string CodeName,
    string Model,
    string Package,
    int CcdCount,
    int CcxCount,
    int PhysicalCoreCount,
    string MotherboardVendor,
    string MotherboardModel,
    string BiosVersion,
    string FirmwareVersion,
    string SmuVersion);
