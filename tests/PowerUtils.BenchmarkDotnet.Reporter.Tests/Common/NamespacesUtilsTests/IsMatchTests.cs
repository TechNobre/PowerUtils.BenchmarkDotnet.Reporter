using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.NamespacesUtilsTests;

public sealed class IsMatchTests
{
    [Theory]
    [InlineData("Demo.Benchmarks.ArrayProcessorBenchmarks.Method", "Demo.Benchmarks.ArrayProcessorBenchmarks.Method")]
    [InlineData("demo.benchmarks.arrayprocessorbenchmarks.method", "Demo.Benchmarks.ArrayProcessorBenchmarks.Method")]
    [InlineData("Demo.*", "Demo.Benchmarks.ArrayProcessorBenchmarks.Method")]
    [InlineData("DEMO.*", "Demo.Benchmarks.ArrayProcessorBenchmarks.Method")]
    [InlineData("Demo.Benchmarks.*", "Demo.Benchmarks.ArrayProcessorBenchmarks.Method")]
    [InlineData("Demo.Benchmarks.ArrayProcessorBenchmarks.*", "Demo.Benchmarks.ArrayProcessorBenchmarks.Method")]
    public void IsMatch_ShouldReturn_True(string pattern, string fullName)
    {
        // Act
        var result = NamespacesUtils.IsMatch(pattern, fullName);


        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Demo.*", "DemoApiFoo.Bar.Baz")]
    [InlineData("Demo.*", null)]
    [InlineData("Demo.*", "")]
    [InlineData("Demo.*", " ")]
    [InlineData("Demo.Benchmarks.ArrayProcessorBenchmarks.Method", "Demo.Benchmarks.ArrayProcessorBenchmarks.OtherMethod")]
    [InlineData("Demo.Benchmarks.ArrayProcessorBenchmarks.*", "Demo.Benchmarks.StringProcessorBenchmarks.Method")]
    public void IsMatch_ShouldReturn_False(string pattern, string? fullName)
    {
        // Act
        var result = NamespacesUtils.IsMatch(pattern, fullName);


        // Assert
        result.Should().BeFalse();
    }
}
