using System;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.Models;

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
        threshold.Value.Should().Be(expectedValue);
        threshold.IsPercentage.Should().Be(expectedIsPercentage);
    }

    [Fact]
    public void Memory_Conversion()
    {
        // Arrange
        var threshold = MemoryThreshold.Parse("124KB");


        // Act
        decimal act = threshold;


        // Assert
        act.Should().Be(124000);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("B")]
    [InlineData("%")]
    [InlineData("0B")]
    [InlineData("-1B")]
    [InlineData("1kg")]
    [InlineData("1tb")]
    [InlineData("123")]
    public void Invalid_Text_Should_Not_Parse(string? value)
    {
        // Act
        var result = MemoryThreshold.TryParse(value, out var threshold);


        // Assert
        result.Should().BeFalse();
        threshold.Should().Be(default(MemoryThreshold));
    }

    [Fact]
    public void Parse_With_Invalid_Value_Should_Throw_FormatException()
    {
        // Arrange
        var value = "invalid";


        // Act
        var act = () => { MemoryThreshold.Parse(value); };


        // Assert
        var exception = act.Should().Throw<FormatException>();
        exception.Which.Message.Should().Contain(value);
    }
}
