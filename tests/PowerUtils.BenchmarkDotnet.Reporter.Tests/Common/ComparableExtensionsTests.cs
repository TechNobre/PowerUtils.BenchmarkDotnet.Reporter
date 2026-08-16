using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class ComparableExtensionsTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "test", false)]
    [InlineData("test", null, false)]
    [InlineData("Test", "test", true)]
    [InlineData("Test1", "Test2", false)]
    public void String_Validate_Result_EquivalentTo_Operation(string? left, string? right, bool expected)
    {
        // Arrange & Act
        var act = left.EquivalentTo(right);


        // Assert
        act.ShouldBe(expected);
    }
}
