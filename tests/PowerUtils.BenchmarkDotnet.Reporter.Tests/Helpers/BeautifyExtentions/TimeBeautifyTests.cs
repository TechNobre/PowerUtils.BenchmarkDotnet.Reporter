using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.BeautifyExtentions;

public static class TimeBeautifyTests
{
    [Fact]
    public static void When_Time_Is_Null_Should_Return_Empty()
    {
        // Arrange
        decimal? time = null;


        // Act
        var act = time.BeautifyTime();


        // Assert
        act.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, "1 ns")]
    [InlineData(814, "814 ns")]
    [InlineData(999, "999 ns")]
    [InlineData(1000, "1 μs")]
    [InlineData(3457, "3.457 μs")]
    [InlineData(999999, "999.999 μs")]
    [InlineData(1000000, "1 ms")]
    [InlineData(5243454, "5.243 ms")]
    [InlineData(999998888, "999.999 ms")]
    [InlineData(1000000000, "1 s")]
    [InlineData(6234242345, "6.234 s")]
    [InlineData(60000000000, "1m")]
    [InlineData(70345345345, "1m 10s")]
    public static void From_Nanoseconds_To(decimal time, string expected)
    {
        // Arrange & Act
        var act = time.BeautifyTime();


        // Assert
        act.ShouldBe(expected);
    }
}
