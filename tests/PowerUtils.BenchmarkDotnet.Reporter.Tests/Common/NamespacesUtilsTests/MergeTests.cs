using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.NamespacesUtilsTests;

public sealed class MergeTests
{
    [Fact]
    public void Merge_WithNoLayers_ShouldReturn_Empty()
    {
        // Act
        var result = NamespacesUtils.Merge();


        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Merge_WithSingleLayer_ShouldReturn_SameRules()
    {
        // Arrange
        var layer = new List<KeyValuePair<string, string>>
        {
            new("*", "5%"),
            new("Demo.*", "10ms")
        };


        // Act
        var result = NamespacesUtils.Merge(layer);


        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(rule => rule.Key == "*" && rule.Value == "5%");
        result.Should().Contain(rule => rule.Key == "Demo.*" && rule.Value == "10ms");
    }

    [Fact]
    public void Merge_WhenLaterLayerHasSamePattern_ShouldOverride_EarlierLayer()
    {
        // Arrange
        var config = new List<KeyValuePair<string, string>> { new("*", "50%") };
        var cli = new List<KeyValuePair<string, string>> { new("*", "5%") };


        // Act
        var result = NamespacesUtils.Merge(config, cli);


        // Assert
        result.Should().ContainSingle(rule => rule.Key == "*" && rule.Value == "5%");
    }

    [Fact]
    public void Merge_WhenLayersHaveDifferentPatterns_ShouldKeep_AllRules()
    {
        // Arrange
        var config = new List<KeyValuePair<string, string>> { new("Demo.Benchmarks.*", "50%") };
        var cli = new List<KeyValuePair<string, string>> { new("*", "5%") };


        // Act
        var result = NamespacesUtils.Merge(config, cli);


        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(rule => rule.Key == "Demo.Benchmarks.*" && rule.Value == "50%");
        result.Should().Contain(rule => rule.Key == "*" && rule.Value == "5%");
    }

    [Fact]
    public void Merge_PatternComparison_ShouldBe_CaseInsensitive()
    {
        // Arrange
        var config = new List<KeyValuePair<string, string>> { new("Demo.Benchmarks.*", "50%") };
        var cli = new List<KeyValuePair<string, string>> { new("DEMO.BENCHMARKS.*", "5%") };


        // Act
        var result = NamespacesUtils.Merge(config, cli);


        // Assert
        result.Should().ContainSingle(rule => rule.Key == "DEMO.BENCHMARKS.*" && rule.Value == "5%");
    }

    [Fact]
    public void Merge_WithThreeLayers_ShouldApply_HighestPrecedenceLast()
    {
        // Arrange
        // Simulates file < env vars < CLI precedence for the same pattern.
        var file = new List<KeyValuePair<string, string>> { new("*", "50%") };
        var envVars = new List<KeyValuePair<string, string>> { new("*", "20%") };
        var cli = new List<KeyValuePair<string, string>> { new("*", "5%") };


        // Act
        var result = NamespacesUtils.Merge(file, envVars, cli);


        // Assert
        result.Should().ContainSingle(rule => rule.Key == "*" && rule.Value == "5%");
    }
}
