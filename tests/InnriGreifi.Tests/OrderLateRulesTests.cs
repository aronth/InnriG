using InnriGreifi.API.Models;
using InnriGreifi.API.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace InnriGreifi.Tests;

public class OrderLateRulesTests
{
    private OrderLateRules CreateOrderLateRules(int sentThreshold = 15, int otherThreshold = 7)
    {
        var options = Options.Create(new OrderLateRulesOptions
        {
            SentThresholdMinutes = sentThreshold,
            OtherThresholdMinutes = otherThreshold
        });
        return new OrderLateRules(options);
    }

    [Fact]
    public void Sent_IsLate_WhenMinutesLeftIsLessThan15()
    {
        var rules = CreateOrderLateRules();
        var ready = new DateTime(2025, 12, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.True(rules.IsLate("Sent", ready, checkedOutAtUtc: ready.AddMinutes(-10), scannedAtUtc: null));
        Assert.False(rules.IsLate("Sent", ready, checkedOutAtUtc: ready.AddMinutes(-20), scannedAtUtc: null));
    }

    [Fact]
    public void Other_IsLate_WhenMinutesLeftIsLessThan7()
    {
        var rules = CreateOrderLateRules();
        var ready = new DateTime(2025, 12, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.True(rules.IsLate("Sótt", ready, checkedOutAtUtc: null, scannedAtUtc: ready.AddMinutes(-5)));
        Assert.False(rules.IsLate("Salur", ready, checkedOutAtUtc: null, scannedAtUtc: ready.AddMinutes(-9)));
    }

    [Fact]
    public void Uses_Configured_Thresholds()
    {
        var rules = CreateOrderLateRules(sentThreshold: 20, otherThreshold: 10);
        var ready = new DateTime(2025, 12, 1, 12, 0, 0, DateTimeKind.Utc);

        // With 20 minute threshold for Sent, 15 minutes should not be late
        Assert.False(rules.IsLate("Sent", ready, checkedOutAtUtc: ready.AddMinutes(-15), scannedAtUtc: null));
        // But 10 minutes should be late
        Assert.True(rules.IsLate("Sent", ready, checkedOutAtUtc: ready.AddMinutes(-10), scannedAtUtc: null));

        // With 10 minute threshold for Other, 8 minutes should not be late
        Assert.False(rules.IsLate("Sótt", ready, checkedOutAtUtc: null, scannedAtUtc: ready.AddMinutes(-8)));
        // But 5 minutes should be late
        Assert.True(rules.IsLate("Sótt", ready, checkedOutAtUtc: null, scannedAtUtc: ready.AddMinutes(-5)));
    }
}


