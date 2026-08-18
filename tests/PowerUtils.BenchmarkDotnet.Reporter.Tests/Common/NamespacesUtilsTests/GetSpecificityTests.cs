using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.NamespacesUtilsTests;

public sealed class GetSpecificityTests
{
    [Fact]
    public void GetSpecificity_ExactPattern_ShouldReturn_MaxValue()
    {
        // Act
        var result = NamespacesUtils.GetSpecificity("Demo.Benchmarks.ArrayProcessorBenchmarks.Method");


        // Assert
        result.Should().Be(int.MaxValue);
    }

    [Fact]
    public void GetSpecificity_WildcardPattern_ShouldReturn_LiteralPrefixLength()
    {
        // Act
        var shortPattern = NamespacesUtils.GetSpecificity("Demo.*");
        var longPattern = NamespacesUtils.GetSpecificity("Demo.Benchmarks.ArrayProcessorBenchmarks.*");


        // Assert
        shortPattern.Should().Be("Demo.".Length);
        longPattern.Should().Be("Demo.Benchmarks.ArrayProcessorBenchmarks.".Length);
        longPattern.Should().BeGreaterThan(shortPattern);
    }
}
