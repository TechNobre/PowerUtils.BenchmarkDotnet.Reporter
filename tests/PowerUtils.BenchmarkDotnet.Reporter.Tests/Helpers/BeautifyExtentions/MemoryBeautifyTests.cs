using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.BeautifyExtentions;

public static class MemoryBeautifyTests
{
    [Fact]
    public static void When_Memory_Is_Null_Should_Return_Empty()
    {
        // Arrange
        decimal? memory = null;


        // Act
        var act = memory.BeautifyMemory();


        // Assert
        act.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, "1 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(3421, "3.341 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(3434455, "3.275 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776, "1 TB")]
    public static void From_Bytes_To(decimal memory, string expected)
    {
        // Arrange & Act
        var act = memory.BeautifyMemory();


        // Assert
        act.ShouldBe(expected);
    }
}
