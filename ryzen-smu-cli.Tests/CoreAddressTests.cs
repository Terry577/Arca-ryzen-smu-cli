namespace ryzen_smu_cli.Tests;

public sealed class CoreAddressTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(3, 0, 3, 0, 3)]
    [InlineData(4, 0, 4, 1, 0)]
    [InlineData(7, 0, 7, 1, 3)]
    [InlineData(8, 1, 0, 0, 0)]
    [InlineData(16, 2, 0, 0, 0)]
    [InlineData(31, 3, 7, 1, 3)]
    [InlineData(127, 15, 7, 1, 3)]
    public void PhysicalCoreProducesCcdAndCcxCoordinates(
        int physicalCore,
        int expectedCcd,
        int expectedCore,
        int expectedCcx,
        int expectedCoreWithinCcx)
    {
        CoreAddress address =
            CoreAddress.FromPhysicalCoreIndex(physicalCore);

        Assert.Equal(expectedCcd, address.CcdIndex);
        Assert.Equal(expectedCore, address.CoreIndex);
        Assert.Equal(expectedCcx, address.CcxIndex);
        Assert.Equal(expectedCoreWithinCcx, address.CoreIndexWithinCcx);
    }

    [Fact]
    public void MoreThanSixteenCcdsIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CoreAddress.FromPhysicalCoreIndex(128));
    }

    [Fact]
    public void DowncoreBitmapSupportsArbitraryCcdIndex()
    {
        HashSet<int> disabled = [16, 18, 23];

        byte bitmap =
            AmdAcpiDowncoreController.BuildDisableMask(2, disabled);

        Assert.Equal(0b1000_0101, bitmap);
    }
}
