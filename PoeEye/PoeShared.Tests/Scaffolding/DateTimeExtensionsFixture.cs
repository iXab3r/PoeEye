using System;
using System.Collections.Generic;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

public class DateTimeExtensionsFixture : FixtureBase
{
    [Test]
    [TestCaseSource(nameof(ShouldCalculateIntersectionCases))]
    public void ShouldCalculateIntersection(DateTimeOffset date, TimeSpan duration, DateTimeOffset intervalStart, TimeSpan intervalDuration, TimeSpan expected)
    {
        //Given
        //When
        var result = date.IntersectionDuration(duration, intervalStart, intervalDuration);

        //Then
        result.ShouldBe(expected);
    }

    
    public static IEnumerable<NamedTestCaseData> ShouldCalculateIntersectionCases()
    {
        yield return new NamedTestCaseData(default, default, default, default, default) { TestName = "empty" };
        yield return new NamedTestCaseData(Dt(2024, 7, 28), TimeSpan.FromDays(1), Dt(2024, 7, 28), TimeSpan.FromDays(1), TimeSpan.FromDays(1)) { TestName = "simple" };
        yield return new NamedTestCaseData(Dt(2024, 7, 28), TimeSpan.FromHours(5), Dt(2024, 7, 28, 3), TimeSpan.FromHours(5), TimeSpan.FromHours(2)) { TestName = "partial overlap start" };
        yield return new NamedTestCaseData(Dt(2024, 7, 28, 3), TimeSpan.FromHours(5), Dt(2024, 7, 28), TimeSpan.FromHours(5), TimeSpan.FromHours(2)) { TestName = "partial overlap end" };
        yield return new NamedTestCaseData(Dt(2024, 7, 28), TimeSpan.FromHours(3), Dt(2024, 7, 28, 1), TimeSpan.FromHours(2), TimeSpan.FromHours(2)) { TestName = "partial overlap" };
        yield return new NamedTestCaseData(Dt(2024, 7, 28, 1), TimeSpan.FromHours(2), Dt(2024, 7, 27), TimeSpan.FromHours(3), TimeSpan.Zero) { TestName = "no overlap before" };
        yield return new NamedTestCaseData(Dt(2024, 7, 28, 1), TimeSpan.FromHours(2), Dt(2024, 7, 28, 4), TimeSpan.FromHours(3), TimeSpan.Zero) { TestName = "no overlap after" };
        yield return new NamedTestCaseData(Dt(2024, 7, 28), TimeSpan.FromHours(6), Dt(2024, 7, 28, 2), TimeSpan.FromHours(2), TimeSpan.FromHours(2)) { TestName = "contained within" };
        yield return new NamedTestCaseData(Dt(2024, 7, 28, 2), TimeSpan.FromHours(2), Dt(2024, 7, 28), TimeSpan.FromHours(6), TimeSpan.FromHours(2)) { TestName = "contains interval" };
        yield return new NamedTestCaseData(Dt(2024, 7, 28), TimeSpan.FromDays(1), Dt(2024, 7, 29), TimeSpan.FromDays(1), TimeSpan.Zero) { TestName = "adjacent intervals" };
    }

    private static DateTimeOffset Dt(int year, int month, int day, int hour = 0)
    {
        return new DateTimeOffset(year, month, day, hour, 0, 0, TimeSpan.Zero);
    }
}