using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Models;

public sealed class TimeThresholdTests
{
    [Theory]
    [InlineData("1ns", 1, false)]
    [InlineData("101ns", 101, false)]
    [InlineData("1μs", 1000, false)]
    [InlineData("1123μs", 1123000, false)]
    [InlineData("1ms", 1000000, false)]
    [InlineData("1234ms", 1234000000, false)]
    [InlineData("1s", 1000000000, false)]
    [InlineData("1234s", 1234000000000, false)]
    [InlineData("15%", 15, true)]
    [InlineData("100%", 100, true)]
    public void From_Text_To_TimeThreshold(string value, decimal expectedValue, bool expectedIsPercentage)
    {
        // Arrange & Act
        var threshold = TimeThreshold.Parse(value);


        // Assert
        threshold.Value.ShouldBe(expectedValue);
        threshold.IsPercentage.ShouldBe(expectedIsPercentage);
    }

    [Fact]
    public void Time_Conversion()
    {
        // Arrange
        var threshold = TimeThreshold.Parse("124μs");


        // Act
        decimal act = threshold;


        // Assert
        act.ShouldBe(124000);
    }
}
