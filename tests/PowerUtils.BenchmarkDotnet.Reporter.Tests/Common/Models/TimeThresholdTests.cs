using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.Models;

public sealed class TimeThresholdTests
{
    [Theory]
    [InlineData("1ms", 1_000_000, false)]
    [InlineData("15%", 15, true)]
    [InlineData("5.5%", 5.5, true)]
    public void From_Text_To_TimeThreshold(string value, decimal expectedValue, bool expectedIsPercentage)
    {
        // Arrange & Act
        var threshold = TimeThreshold.Parse(value);


        // Assert
        threshold.Value.Should().Be(expectedValue);
        threshold.IsPercentage.Should().Be(expectedIsPercentage);
    }

    [Fact]
    public void Time_Conversion()
    {
        // Arrange
        var threshold = TimeThreshold.Parse("124μs");


        // Act
        decimal act = threshold;


        // Assert
        act.Should().Be(124000);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ns")]
    [InlineData("%")]
    [InlineData("0ns")]
    [InlineData("-1ns")]
    [InlineData("1xx")]
    [InlineData("1kg")]
    [InlineData("79228162514264337593543950335s")]
    [InlineData("79228162514264337593543950335ms")]
    public void Invalid_Text_Should_Not_Parse(string? value)
    {
        // Act
        var result = TimeThreshold.TryParse(value, out var threshold);


        // Assert
        result.Should().BeFalse();
        threshold.Should().Be(default(TimeThreshold));
    }

    [Fact]
    public void Parse_With_Invalid_Value_Should_Throw_DomainException()
    {
        // Arrange
        var value = "invalid";


        // Act
        var act = () => { TimeThreshold.Parse(value); };


        // Assert
        var exception = act.Should().Throw<DomainException>();
        exception.Which.Message.Should().Contain(value);
    }
}
