namespace ryzen_smu_cli.Tests;

public sealed class VcoreDiagnosticsTests
{
    [Theory]
    [InlineData(0x19u, 0x74u, 0u, 0x0006F038u, 0x00073010u)]
    [InlineData(0x19u, 0x78u, 0u, 0x0006F038u, 0x00073010u)]
    [InlineData(0x1Au, 0x20u, 0u, 0x0005A00Cu, 0x00073010u)]
    [InlineData(0x1Au, 0x60u, 0u, 0x0005A00Cu, 0x00073010u)]
    [InlineData(0x1Au, 0x70u, 0u, 0x00073010u, 0x0005A00Cu)]
    [InlineData(0x1Au, 0x44u, 1u, 0x00073010u, 0x0005A00Cu)]
    public void PlatformDiagnosticsUseOnlyTheirFixedCandidateGroups(
        uint family,
        uint model,
        uint package,
        uint expectedAddress,
        uint excludedAddress)
    {
        IReadOnlyList<VcoreDiagnosticRegisterDescriptor> descriptors =
            VcoreDiagnostics.ResolveRegisters(family, model, package);

        Assert.Contains(descriptors, item => item.Address == expectedAddress);
        Assert.DoesNotContain(descriptors, item => item.Address == excludedAddress);
    }

    [Theory]
    [InlineData(0x19u, 0x61u, 0u)]
    [InlineData(0x1Au, 0x44u, 0u)]
    [InlineData(0x19u, 0x21u, 0u)]
    [InlineData(0x17u, 0x71u, 2u)]
    public void DesktopPmAndOlderCpusDoNotProbeUnrelatedSviCandidates(
        uint family,
        uint model,
        uint package)
    {
        Assert.Empty(VcoreDiagnostics.ResolveRegisters(family, model, package));
    }

    [Fact]
    public void OnlyCorePlaneDescriptorsRequestVidDecoding()
    {
        IReadOnlyList<VcoreDiagnosticRegisterDescriptor> descriptors =
            VcoreDiagnostics.ResolveRegisters(0x1A, 0x70, 0);

        Assert.All(
            descriptors.Where(item => item.DecodeCoreVidCandidates),
            item => Assert.Equal("core-plane", item.Role));
        Assert.All(
            descriptors.Where(item => item.Role != "core-plane"),
            item => Assert.False(item.DecodeCoreVidCandidates));
    }
}
