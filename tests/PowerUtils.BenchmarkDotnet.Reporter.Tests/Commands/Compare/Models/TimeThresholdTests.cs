using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.Models;

public sealed class TimeThresholdTests
{
    [Theory]
    [InlineData("1ns", 1, false)]
    [InlineData("101ns", 101, false)]
    [InlineData("1μs", 1000, false)]
    [InlineData("1µs", 1000, false)]
    [InlineData("1us", 1000, false)]
    [InlineData("1123μs", 1123000, false)]
    [InlineData("1123us", 1123000, false)]
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
    [InlineData("123")]
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
