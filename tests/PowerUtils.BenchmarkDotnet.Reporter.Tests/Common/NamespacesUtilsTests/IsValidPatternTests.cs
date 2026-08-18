using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.NamespacesUtilsTests;

public sealed class IsValidPatternTests
{
    [Theory]
    [InlineData("Demo.Benchmarks.ArrayProcessorBenchmarks.Method")]
    [InlineData("Demo.*")]
    [InlineData("Demo.Benchmarks.*")]
    [InlineData("Demo.Benchmarks.ArrayProcessorBenchmarks.*")]
    [InlineData("*")]
    public void IsValidPattern_ShouldReturn_True(string pattern)
    {
        // Act
        var result = NamespacesUtils.IsValidPattern(pattern);


        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Demo.*.ArrayProcessorBenchmarks")]
    [InlineData("*Demo.Benchmarks")]
    [InlineData("Demo.**")]
    public void IsValidPattern_ShouldReturn_False(string? pattern)
    {
        // Act
        var result = NamespacesUtils.IsValidPattern(pattern);


        // Assert
        result.Should().BeFalse();
    }
}
