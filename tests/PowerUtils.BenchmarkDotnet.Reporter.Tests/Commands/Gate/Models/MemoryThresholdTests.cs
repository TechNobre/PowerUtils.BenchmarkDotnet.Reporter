using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.Models;

public sealed class MemoryThresholdTests
{
    [Theory]
    [InlineData("1MB", 1_000_000)]
    public void From_Text_To_MemoryThreshold(string value, decimal expectedValue)
    {
        // Arrange & Act
        var threshold = MemoryThreshold.Parse(value);


        // Assert
        threshold.Value.Should().Be(expectedValue);
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
    [InlineData("15%")]
    [InlineData("100%")]
    [InlineData("%")]
    [InlineData("0B")]
    [InlineData("-1B")]
    [InlineData("1kg")]
    [InlineData("1tb")]
    [InlineData("79228162514264337593543950335gb")]
    [InlineData("79228162514264337593543950335mb")]
    public void Invalid_Text_Should_Not_Parse(string? value)
    {
        // Act
        var result = MemoryThreshold.TryParse(value, out var threshold);


        // Assert
        result.Should().BeFalse();
        threshold.Should().Be(default(MemoryThreshold));
    }

    [Fact]
    public void Parse_With_Invalid_Value_Should_Throw_DomainException()
    {
        // Arrange
        var value = "invalid";


        // Act
        var act = () => MemoryThreshold.Parse(value);


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
        var act = () => MemoryThreshold.Parse(value);


        // Assert
        var exception = act.Should().Throw<DomainException>();
        exception.Which.Message.Should().Contain(value);
    }
}
