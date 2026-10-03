using System.Collections.Generic;
using System.Linq;
using MarkdownLog;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class MarkdownLogExtensionsTests
{
    [Fact]
    public void When_Cells_Are_Converted_ToTableRow_Should_Preserve_Their_Text_And_Order()
    {
        // Arrange
        List<string?> cells = ["first", null, "third"];


        // Act
        var result = cells.ToTableRow();


        // Assert
        result.Cells.Should().AllBeOfType<TableCell>();
        result.Cells.Cast<TableCell>().Select(cell => cell.Text).Should().Equal("first", string.Empty, "third");
    }
}
