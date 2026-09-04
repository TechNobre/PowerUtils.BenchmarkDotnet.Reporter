using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.Models;

public sealed class TimeThresholdTests
{
    [Theory]
    [InlineData("1ms", 1_000_000)]
    public void From_Text_To_TimeThreshold(string value, decimal expectedValue)
    {
        // Arrange & Act
        var threshold = TimeThreshold.Parse(value);


        // Assert
        threshold.Value.Should().Be(expectedValue);
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
    [InlineData("15%")]
    [InlineData("100%")]
    [InlineData("%")]
    [InlineData("0ns")]
    [InlineData("-1ns")]
    [InlineData("1xx")]
    [InlineData("1kg")]
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

    [Fact]
    public void Parse_With_Percentage_Value_Should_Throw_DomainException()
    {
        // Arrange
        var value = "5%";


        // Act
        var act = () => { TimeThreshold.Parse(value); };


        // Assert
        var exception = act.Should().Throw<DomainException>();
        exception.Which.Message.Should().Contain(value);
    }
}
