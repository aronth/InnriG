using InnriGreifi.API.Services;
using Xunit;

namespace InnriGreifi.Tests;

public class OrderTextParserTests
{
    [Fact]
    public void ParseReikningstexti3_WhenNullOrEmpty_ReturnsNulls()
    {
        var (s1, c1) = OrderTextParser.ParseReikningstexti3(null);
        Assert.Null(s1);
        Assert.Null(c1);

        var (s2, c2) = OrderTextParser.ParseReikningstexti3("   ");
        Assert.Null(s2);
        Assert.Null(c2);
    }

    [Fact]
    public void ParseReikningstexti3_WhenSkannadOnly_ParsesScanTime()
    {
        var raw = "Skannað kl. 01.12.2025 17:51:41";
        var (scan, checkout) = OrderTextParser.ParseReikningstexti3(raw);

        Assert.NotNull(scan);
        Assert.Null(checkout);
        Assert.Equal(2025, scan!.Value.Year);
        Assert.Equal(12, scan.Value.Month);
        Assert.Equal(1, scan.Value.Day);
        Assert.Equal(17, scan.Value.Hour);
        Assert.Equal(51, scan.Value.Minute);
        Assert.Equal(41, scan.Value.Second);
    }

    [Fact]
    public void ParseReikningstexti3_WhenSkannadAndSkradUtOnOneLine_ParsesBoth()
    {
        var raw = "Skannað kl. 01.12.2025 17:51:41 Skráð út kl. 01.12.2025 18:17:59";
        var (scan, checkout) = OrderTextParser.ParseReikningstexti3(raw);

        Assert.NotNull(scan);
        Assert.NotNull(checkout);
        Assert.True(scan < checkout);
        Assert.Equal(new DateTime(2025, 12, 1, 17, 51, 41, DateTimeKind.Utc), scan!.Value);
        Assert.Equal(new DateTime(2025, 12, 1, 18, 17, 59, DateTimeKind.Utc), checkout!.Value);
    }

    [Fact]
    public void ParseReikningstexti3_WhenExportUsesSkannadSuffix_ParsesScanTime()
    {
        var raw = "1.12.2025 17:51:41 skannad";
        var (scan, checkout) = OrderTextParser.ParseReikningstexti3(raw);

        Assert.NotNull(scan);
        Assert.Null(checkout);
        Assert.Equal(new DateTime(2025, 12, 1, 17, 51, 41, DateTimeKind.Utc), scan!.Value);
    }

    [Fact]
    public void ParseReikningstexti3_WhenExportHasTwoLines_ParsesScanAndCheckout()
    {
        var raw = "11.12.2025 18:03:13 skannad\n11.12.2025 18:17:59 skráð út";
        var (scan, checkout) = OrderTextParser.ParseReikningstexti3(raw);

        Assert.NotNull(scan);
        Assert.NotNull(checkout);
        Assert.Equal(new DateTime(2025, 12, 11, 18, 03, 13, DateTimeKind.Utc), scan!.Value);
        Assert.Equal(new DateTime(2025, 12, 11, 18, 17, 59, DateTimeKind.Utc), checkout!.Value);
    }
}


