using System.Text.Json;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.JsonUtilsTests;

public sealed class ToJsonTests
{
    [Fact]
    public void When_Serializing_Object_Should_Return_Valid_Json_With_Expected_Values()
    {
        // Arrange
        var content = new
        {
            Name = "Benchmark",
            Count = 2
        };


        // Act
        var result = content.ToJson();


        // Assert
        using var document = JsonDocument.Parse(result);
        var root = document.RootElement;

        root.GetProperty("Name").GetString().Should().Be("Benchmark");
        root.GetProperty("Count").GetInt32().Should().Be(2);
    }

    [Fact]
    public void When_Serializing_Object_Should_Write_Indented_Json()
    {
        // Arrange
        var content = new
        {
            Name = "Benchmark",
            Tags = new[] { "baseline", "target" }
        };


        // Act
        var result = content.ToJson();


        // Assert
        var normalizedResult = result.Replace("\r\n", "\n");

        normalizedResult.Should().Contain("\n  \"Name\": \"Benchmark\",");
        normalizedResult.Should().Contain("\n  \"Tags\": [");
        normalizedResult.Should().Contain("\n    \"baseline\",");
    }

    [Fact]
    public void When_Serializing_Text_With_Html_Sensitive_Characters_Should_Use_Relaxed_Json_Escaping()
    {
        // Arrange
        var content = new
        {
            Text = "<script>if (a > b && c < d) { return a & b; }</script>"
        };


        // Act
        var result = content.ToJson();


        // Assert
        result.Should().Contain("<script>");
        result.Should().Contain("> b");
        result.Should().Contain("& b");
        result.Should().NotContain("\\u003C");
        result.Should().NotContain("\\u003E");
        result.Should().NotContain("\\u0026");
    }

    [Fact]
    public void When_Serializing_Null_Should_Return_Null_Json_Literal()
    {
        // Arrange
        object? content = null;


        // Act
        var result = content.ToJson();


        // Assert
        result.Should().Be("null");
    }
}
