using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class NumericUnitParserTests
{
    [Theory]
    [InlineData("500ms", 500, "ms")]
    [InlineData("10kb", 10, "kb")]
    [InlineData("1.5s", 1.5, "s")]
    [InlineData("15%", 15, "%")]
    [InlineData("101ns", 101, "ns")]
    public void When_Value_Is_Valid_Should_Extract_Numeric_Value_And_Unit(string value, decimal expectedValue, string expectedUnit)
    {
        // Act
        var result = NumericUnitParser.TryExtract(value, out var numericValue, out var unit);


        // Assert
        result.Should().BeTrue();
        numericValue.Should().Be(expectedValue);
        unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void When_Value_Is_Null_Or_Whitespace_Should_Return_False(string? value)
    {
        // Act
        var result = NumericUnitParser.TryExtract(value, out _, out _);


        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("ms")]
    [InlineData("%")]
    [InlineData("0ns")]
    [InlineData("-1ns")]
    public void When_Numeric_Part_Is_Missing_Zero_Or_Negative_Should_Return_False(string value)
    {
        // Act
        var result = NumericUnitParser.TryExtract(value, out _, out _);


        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void When_Value_Has_No_Unit_Should_Return_Empty_Unit()
    {
        // Act
        var result = NumericUnitParser.TryExtract("123", out var numericValue, out var unit);


        // Assert
        result.Should().BeTrue();
        numericValue.Should().Be(123);
        unit.Should().BeEmpty();
    }

    [Theory]
    [InlineData("5.5.5%", 5.5, ".5%")]
    [InlineData("5,5%", 5, ",5%")]
    public void When_Value_Has_Extra_Punctuation_After_The_Numeric_Part_Should_Fold_It_Into_The_Unit(string value, decimal expectedValue, string expectedUnit)
    {
        // Only the first '.' is treated as a decimal separator; anything after it (including a
        // second '.' or a ',') is not part of the numeric scan and becomes part of the unit suffix.
        // Act
        var result = NumericUnitParser.TryExtract(value, out var numericValue, out var unit);


        // Assert
        result.Should().BeTrue();
        numericValue.Should().Be(expectedValue);
        unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData(1, "ns", 1)]
    [InlineData(1, "μs", 1_000)]
    [InlineData(1, "µs", 1_000)]
    [InlineData(1, "us", 1_000)]
    [InlineData(1.5, "ms", 1_500_000)]
    [InlineData(1.5, "s", 1_500_000_000)]
    [InlineData(1, "MS", 1_000_000)]
    public void When_Time_Unit_Is_Supported_Should_Convert_To_Nanoseconds(decimal value, string unit, decimal expectedValue)
    {
        // Act
        var result = NumericUnitParser.TryConvertTimeToNanoseconds(value, unit, out var nanoseconds);


        // Assert
        result.Should().BeTrue();
        nanoseconds.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(1, "B", 1)]
    [InlineData(1, "KB", 1_000)]
    [InlineData(1.5, "MB", 1_500_000)]
    [InlineData(1.5, "GB", 1_500_000_000)]
    [InlineData(1, "kb", 1_000)]
    public void When_Memory_Unit_Is_Supported_Should_Convert_To_Bytes(decimal value, string unit, decimal expectedValue)
    {
        // Act
        var result = NumericUnitParser.TryConvertMemoryToBytes(value, unit, out var bytes);


        // Assert
        result.Should().BeTrue();
        bytes.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("us", 1_000)]
    [InlineData("ms", 1_000_000)]
    [InlineData("s", 1_000_000_000)]
    public void When_Time_Conversion_Overflows_Should_Return_False(string unit, int factor)
    {
        // Arrange
        var overflowing = (decimal.MaxValue / factor) + 1;


        // Act
        var result = NumericUnitParser.TryConvertTimeToNanoseconds(overflowing, unit, out _);


        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("kb", 1_000)]
    [InlineData("mb", 1_000_000)]
    [InlineData("gb", 1_000_000_000)]
    public void When_Memory_Conversion_Overflows_Should_Return_False(string unit, int factor)
    {
        // Arrange
        var overflowing = (decimal.MaxValue / factor) + 1;


        // Act
        var result = NumericUnitParser.TryConvertMemoryToBytes(overflowing, unit, out _);


        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("us", 1_000)]
    [InlineData("ms", 1_000_000)]
    [InlineData("s", 1_000_000_000)]
    public void When_Time_Conversion_Fits_Exactly_Should_Convert(string unit, int factor)
    {
        // Arrange
        var largest = decimal.MaxValue / factor;


        // Act
        var result = NumericUnitParser.TryConvertTimeToNanoseconds(largest, unit, out var nanoseconds);


        // Assert
        result.Should().BeTrue();
        nanoseconds.Should().Be(largest * factor);
    }

    [Theory]
    [InlineData("kb", 1_000)]
    [InlineData("mb", 1_000_000)]
    [InlineData("gb", 1_000_000_000)]
    public void When_Memory_Conversion_Fits_Exactly_Should_Convert(string unit, int factor)
    {
        // Arrange
        var largest = decimal.MaxValue / factor;


        // Act
        var result = NumericUnitParser.TryConvertMemoryToBytes(largest, unit, out var bytes);


        // Assert
        result.Should().BeTrue();
        bytes.Should().Be(largest * factor);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("kb")]
    [InlineData("")]
    public void When_Time_Unit_Is_Not_Supported_Should_Return_False(string unit)
    {
        // Act
        var result = NumericUnitParser.TryConvertTimeToNanoseconds(10, unit, out var nanoseconds);


        // Assert
        result.Should().BeFalse();
        nanoseconds.Should().Be(10);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("ms")]
    [InlineData("")]
    public void When_Memory_Unit_Is_Not_Supported_Should_Return_False(string unit)
    {
        // Act
        var result = NumericUnitParser.TryConvertMemoryToBytes(10, unit, out var bytes);


        // Assert
        result.Should().BeFalse();
        bytes.Should().Be(10);
    }
}
