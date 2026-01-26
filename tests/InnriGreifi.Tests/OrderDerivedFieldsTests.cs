using InnriGreifi.API.Services;
using Xunit;

namespace InnriGreifi.Tests;

public class OrderDerivedFieldsTests
{
    [Fact]
    public void ComputeDeliveryMethod_ReturnsLausaSala_WhenOrderNumberIsZero()
    {
        Assert.Equal("Lausa sala", OrderDerivedFields.ComputeDeliveryMethod(null, null, "0"));
        Assert.Equal("Lausa sala", OrderDerivedFields.ComputeDeliveryMethod("Sótt", null, "0"));
        Assert.Equal("Lausa sala", OrderDerivedFields.ComputeDeliveryMethod(null, "Heimsent", "0"));
    }

    [Fact]
    public void ComputeDeliveryMethod_UsesOrderTypeFirst()
    {
        Assert.Equal("Sótt", OrderDerivedFields.ComputeDeliveryMethod("Sótt", null));
        Assert.Equal("Sent", OrderDerivedFields.ComputeDeliveryMethod("Heimsent", null));
        Assert.Equal("Salur", OrderDerivedFields.ComputeDeliveryMethod("Í sal", null));
    }

    [Fact]
    public void ComputeDeliveryMethod_FallsBackToInvoiceText3()
    {
        Assert.Equal("Salur", OrderDerivedFields.ComputeDeliveryMethod(null, "Salur"));
        Assert.Equal("Sótt", OrderDerivedFields.ComputeDeliveryMethod(null, "Take away"));
        Assert.Equal("Sent", OrderDerivedFields.ComputeDeliveryMethod(null, "Heimsent"));
        Assert.Equal("Unknown", OrderDerivedFields.ComputeDeliveryMethod(null, "11.12.2025 18:03:13 skannad\n11.12.2025 18:17:59 skráð út"));
    }

    [Fact]
    public void ComputeOrderSource_UsesOrderNumberThreshold()
    {
        Assert.Equal("Counter", OrderDerivedFields.ComputeOrderSource("45"));
        Assert.Equal("Counter", OrderDerivedFields.ComputeOrderSource("999"));
        Assert.Equal("Web", OrderDerivedFields.ComputeOrderSource("1000"));
        Assert.Equal("Web", OrderDerivedFields.ComputeOrderSource("12345"));
        Assert.Equal("Unknown", OrderDerivedFields.ComputeOrderSource(null));
        Assert.Equal("Unknown", OrderDerivedFields.ComputeOrderSource("ABC"));
    }

    [Fact]
    public void ComputeTimes_ComputesWaitTimeRoundedToFive()
    {
        var created = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc);
        var ready14 = new DateTime(2025, 12, 1, 10, 14, 0, DateTimeKind.Utc);
        var ready18 = new DateTime(2025, 12, 1, 10, 18, 0, DateTimeKind.Utc);

        var (_, _, wait14) = OrderDerivedFields.ComputeTimes(created, ready14);
        var (_, _, wait18) = OrderDerivedFields.ComputeTimes(created, ready18);

        Assert.Equal(15, wait14); // 14 min -> rounds to 15
        Assert.Equal(20, wait18); // 18 min -> rounds to 20
    }
}


