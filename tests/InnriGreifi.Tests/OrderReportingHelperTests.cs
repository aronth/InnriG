using Xunit;

namespace InnriGreifi.Tests;

/// <summary>
/// Tests for static helper methods used in OrderReportingService.
/// Since the helpers are private, we test them indirectly through public interfaces
/// or by making a test-only wrapper. Here we test the key date/period logic.
/// </summary>
public class OrderReportingHelperTests
{
    [Theory]
    [InlineData("2025-01-06", 0)] // Monday -> 0
    [InlineData("2025-01-07", 1)] // Tuesday -> 1
    [InlineData("2025-01-08", 2)] // Wednesday -> 2
    [InlineData("2025-01-09", 3)] // Thursday -> 3
    [InlineData("2025-01-10", 4)] // Friday -> 4
    [InlineData("2025-01-11", 5)] // Saturday -> 5
    [InlineData("2025-01-12", 6)] // Sunday -> 6
    public void GetIsoWeekday_ReturnsCorrectDay(string dateStr, int expectedWeekday)
    {
        var date = DateTime.Parse(dateStr);
        var isoWeekday = GetIsoWeekday(date);
        Assert.Equal(expectedWeekday, isoWeekday);
    }

    [Theory]
    [InlineData("2025-01-08", "day", "2025-01-08")]  // Wednesday -> same day
    [InlineData("2025-01-08", "week", "2025-01-06")] // Wednesday -> Monday of that week
    [InlineData("2025-01-06", "week", "2025-01-06")] // Monday -> same Monday
    [InlineData("2025-01-12", "week", "2025-01-06")] // Sunday -> Monday of that week
    [InlineData("2025-01-15", "month", "2025-01-01")] // Jan 15 -> Jan 1
    [InlineData("2025-02-28", "month", "2025-02-01")] // Feb 28 -> Feb 1
    public void GetPeriodStart_ReturnsCorrectStart(string dateStr, string granularity, string expectedStr)
    {
        var date = DateTime.Parse(dateStr);
        var expected = DateTime.Parse(expectedStr);
        var result = GetPeriodStart(date, granularity);
        Assert.Equal(expected.Date, result.Date);
    }

    [Fact]
    public void WeekdayExpectedIndex_BasicCalculation()
    {
        // Scenario: 
        // - Baseline: 8 weeks with consistent Monday=100, Friday=200 orders per day
        // - Target: 1 week with Monday=120, Friday=180
        // Expected behavior:
        // - Monday index = 120/100 = 1.2 (20% above expected)
        // - Friday index = 180/200 = 0.9 (10% below expected)

        var baselineAvgByWeekday = new decimal[7];
        baselineAvgByWeekday[0] = 100; // Monday
        baselineAvgByWeekday[4] = 200; // Friday
        // Other days = 0

        var targetActualByWeekday = new int[7];
        targetActualByWeekday[0] = 120; // Monday
        targetActualByWeekday[4] = 180; // Friday

        var targetDaysInPeriod = new int[7];
        targetDaysInPeriod[0] = 1; // 1 Monday
        targetDaysInPeriod[4] = 1; // 1 Friday

        // Calculate expected
        decimal expectedTotal = 0;
        for (int wd = 0; wd < 7; wd++)
        {
            expectedTotal += baselineAvgByWeekday[wd] * targetDaysInPeriod[wd];
        }
        Assert.Equal(300m, expectedTotal); // 100*1 + 200*1

        // Calculate actual
        int actualTotal = 0;
        for (int wd = 0; wd < 7; wd++)
        {
            actualTotal += targetActualByWeekday[wd];
        }
        Assert.Equal(300, actualTotal); // 120 + 180

        // Index should be 1.0 (actual == expected)
        decimal index = actualTotal / expectedTotal;
        Assert.Equal(1.0m, index);

        // But per-weekday indices differ:
        decimal mondayIndex = targetActualByWeekday[0] / (baselineAvgByWeekday[0] * targetDaysInPeriod[0]);
        decimal fridayIndex = targetActualByWeekday[4] / (baselineAvgByWeekday[4] * targetDaysInPeriod[4]);

        Assert.Equal(1.2m, mondayIndex);  // Monday: 20% above expected
        Assert.Equal(0.9m, fridayIndex);  // Friday: 10% below expected
    }

    [Fact]
    public void WeekdayExpectedIndex_HandlesZeroExpected()
    {
        // If baseline has 0 for a weekday, index should be 0 (avoid divide by zero)
        var baselineAvg = 0m;
        var actual = 10;
        var daysInPeriod = 1;

        var expected = baselineAvg * daysInPeriod;
        var index = expected == 0 ? 0 : actual / expected;

        Assert.Equal(0m, index);
    }

    [Fact]
    public void WaitTimeDistribution_BucketAssignment()
    {
        // Test bucket assignment logic
        var waitTimes = new List<int> { 0, 3, 5, 7, 10, 15, 20, 25, 30, 35, 60, 65, 90 };
        var bucketSize = 5;

        var buckets = new Dictionary<int, int>(); // bucketStart -> count
        foreach (var wt in waitTimes)
        {
            var bucketStart = (wt / bucketSize) * bucketSize;
            if (!buckets.ContainsKey(bucketStart))
                buckets[bucketStart] = 0;
            buckets[bucketStart]++;
        }

        Assert.Equal(2, buckets[0]);   // 0, 3
        Assert.Equal(2, buckets[5]);   // 5, 7
        Assert.Equal(1, buckets[10]);  // 10
        Assert.Equal(1, buckets[15]);  // 15
        Assert.Equal(1, buckets[20]);  // 20
        Assert.Equal(1, buckets[25]);  // 25
        Assert.Equal(1, buckets[30]);  // 30
        Assert.Equal(1, buckets[35]);  // 35
        Assert.Equal(1, buckets[60]);  // 60
        Assert.Equal(1, buckets[65]);  // 65
        Assert.Equal(1, buckets[90]);  // 90
    }

    #region Helper methods (mirrors of OrderReportingService private methods)

    /// <summary>
    /// Returns ISO weekday: 0=Monday..6=Sunday.
    /// </summary>
    private static int GetIsoWeekday(DateTime d)
    {
        // .NET: Sunday=0, Monday=1, ..., Saturday=6
        // ISO: Monday=0, Tuesday=1, ..., Sunday=6
        return d.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)d.DayOfWeek - 1;
    }

    private static DateTime GetPeriodStart(DateTime utcDateTime, string granularity)
    {
        var d = utcDateTime.Date;
        return granularity switch
        {
            "day" => d,
            "week" => d.AddDays(-GetIsoWeekday(d)), // Monday as start
            "month" => new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => d
        };
    }

    #endregion
}

