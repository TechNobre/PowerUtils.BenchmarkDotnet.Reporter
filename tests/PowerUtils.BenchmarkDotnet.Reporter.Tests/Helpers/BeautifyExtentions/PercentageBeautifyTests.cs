using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.BeautifyExtentions;

public static class PercentageBeautifyTests
{
    [Fact]
    public static void When_Percentage_Is_Null_Should_Return_Empty()
    {
        // Arrange
        decimal? memory = null;


        // Act
        var act = memory.BeautifyPercentage();


        // Assert
        act.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, "1%")]
    [InlineData(10.24, "10.24%")]
    [InlineData(10.2439, "10.24%")]
    [InlineData(10.249, "10.25%")]
    public static void From_Percentage(decimal percentage, string expected)
    {
        // Arrange & Act
        var act = percentage.BeautifyPercentage();


        // Assert
        act.ShouldBe(expected);
    }
}
