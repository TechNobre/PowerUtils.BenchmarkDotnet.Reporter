using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class ThresholdResolverTests
{
    private static (decimal Value, bool IsPercentage) _identityParse(string value)
        => (decimal.Parse(value), false);

    [Fact]
    public void Resolve_Should_Parse_Every_Rule_And_Sort_By_Specificity_Descending()
    {
        // Arrange
        var rules = new List<KeyValuePair<string, string>>
        {
            new("*", "1"),
            new("DemoApi.Controllers.CreateController.Create", "3"),
            new("DemoApi.Controllers.*", "2")
        };

        // Act
        var resolved = ThresholdResolver.Resolve(rules, _identityParse);

        // Assert
        resolved.Should().HaveCount(3);
        resolved[0].Pattern.Should().Be("DemoApi.Controllers.CreateController.Create");
        resolved[1].Pattern.Should().Be("DemoApi.Controllers.*");
        resolved[2].Pattern.Should().Be("*");
    }

    [Fact]
    public void FindMatch_Should_Return_The_Most_Specific_Matching_Rule()
    {
        // Arrange
        var rules = new List<KeyValuePair<string, string>>
        {
            new("*", "1"),
            new("DemoApi.Controllers.*", "2"),
            new("DemoApi.Controllers.CreateController.Create", "3")
        };
        var resolved = ThresholdResolver.Resolve(rules, _identityParse);

        // Act
        var match = ThresholdResolver.FindMatch(resolved, "DemoApi.Controllers.CreateController.Create");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Value.Should().Be(3);
        match.Value.Pattern.Should().Be("DemoApi.Controllers.CreateController.Create");
    }

    [Fact]
    public void FindMatch_Should_Fall_Back_To_A_Less_Specific_Rule_When_The_Exact_One_Doesnt_Match()
    {
        // Arrange
        var rules = new List<KeyValuePair<string, string>>
        {
            new("*", "1"),
            new("DemoApi.Controllers.*", "2"),
            new("DemoApi.Controllers.CreateController.Create", "3")
        };
        var resolved = ThresholdResolver.Resolve(rules, _identityParse);

        // Act
        var match = ThresholdResolver.FindMatch(resolved, "DemoApi.Controllers.OtherController.Get");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Value.Should().Be(2);
        match.Value.Pattern.Should().Be("DemoApi.Controllers.*");
    }

    [Fact]
    public void FindMatch_Should_Return_Null_When_No_Rule_Matches()
    {
        // Arrange
        var rules = new List<KeyValuePair<string, string>>
        {
            new("DemoApi.Controllers.*", "2")
        };
        var resolved = ThresholdResolver.Resolve(rules, _identityParse);

        // Act
        var match = ThresholdResolver.FindMatch(resolved, "OtherNamespace.OtherClass.Method");

        // Assert
        match.Should().BeNull();
    }

    [Fact]
    public void FindMatch_Should_Return_Null_When_FullName_Is_Null()
    {
        // Arrange
        var rules = new List<KeyValuePair<string, string>>
        {
            new("*", "1")
        };
        var resolved = ThresholdResolver.Resolve(rules, _identityParse);

        // Act
        var match = ThresholdResolver.FindMatch(resolved, null);

        // Assert
        match.Should().BeNull();
    }
}
