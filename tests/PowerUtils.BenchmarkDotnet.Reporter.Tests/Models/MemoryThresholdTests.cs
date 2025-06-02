using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Models;

public sealed class MemoryThresholdTests
{
    [Theory]
    [InlineData("1B", 1, false)]
    [InlineData("101B", 101, false)]
    [InlineData("1KB", 1_000, false)]
    [InlineData("1123KB", 1_123_000, false)]
    [InlineData("1MB", 1_000_000, false)]
    [InlineData("1234MB", 1234000000, false)]
    [InlineData("1GB", 1000000000, false)]
    [InlineData("1234GB", 1234000000000, false)]
    [InlineData("15%", 15, true)]
    [InlineData("100%", 100, true)]
    public void From_Text_To_MemoryThreshold(string value, decimal expectedValue, bool expectedIsPercentage)
    {
        // Arrange & Act
        var threshold = MemoryThreshold.Parse(value);


        // Assert
        threshold.Value.ShouldBe(expectedValue);
        threshold.IsPercentage.ShouldBe(expectedIsPercentage);
    }

    [Fact]
    public void Memory_Conversion()
    {
        // Arrange
        var threshold = MemoryThreshold.Parse("124KB");


        // Act
        decimal act = threshold;


        // Assert
        act.ShouldBe(124000);
    }
}
