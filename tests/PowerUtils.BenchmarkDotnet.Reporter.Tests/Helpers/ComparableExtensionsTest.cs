using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers;

public sealed class ComparableExtensionsTest
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "test", false)]
    [InlineData("test", null, false)]
    [InlineData("Test", "test", true)]
    [InlineData("Test1", "Test2", false)]
    public void String_Validate_Result_Equivalente_Operation(string? left, string? right, bool expected)
    {
        // Arrange & Act
        var act = left.Equivalente(right);


        // Assert
        act.ShouldBe(expected);
    }
}
