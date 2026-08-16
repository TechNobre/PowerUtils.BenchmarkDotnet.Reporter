using System;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class TableBuilderTests
{
    private readonly TableBuilder _builder = TableBuilder.Create();


    [Fact]
    public void When_Doesnt_Have_Any_Rows_Should_Return_EmptyArray()
    {
        // Arrange & Act
        var act = _builder.Build();


        // Assert
        act.Should().BeEmpty();
    }

    [Fact]
    public void When_Calling_AddHeader_Twice_Should_Throw_InvalidOperationException()
    {
        // Arrange
        _builder.AddHeader("Header1", "Header2");


        // Act
        Func<TableBuilder> act = () => _builder.AddHeader("Header1", "Header2");


        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("Header has already been added");
    }

    [Fact]
    public void When_Calling_AddHeader_After_Adding_Rows_Should_Throw_InvalidOperationException()
    {
        // Arrange
        _builder.AddRow("Row1Col1", "Row1Col2");


        // Act
        Func<TableBuilder> act = () => _builder.AddHeader("Header1", "Header2");


        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("Rows have already been added, cannot add header now");
    }

    [Fact]
    public void When_Try_Add_Row_With_Diferent_Number_Of_Columns_Than_Header_Should_Throw_InvalidOperationException()
    {
        // Arrange
        _builder.AddHeader("Header1", "Header2");


        // Act
        Func<TableBuilder> act = () => _builder.AddRow("Row1Col1", "Row1Col2", "Row1Col3");


        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("Cannot add row with a different number of columns than already defined before");
    }

    [Fact]
    public void When_Add_Header_With_Zero_Columns_Shouldnt_Add_Any_Rows()
    {
        // Arrange & Act
        _builder.AddHeader();
        var act = _builder.Build();


        // Assert
        act.Should().BeEmpty();
    }

    [Fact]
    public void When_Add_Row_With_Zero_Columns_Shouldnt_Add_Any_Rows()
    {
        // Arrange & Act
        _builder.AddRow();
        var act = _builder.Build();


        // Assert
        act.Should().BeEmpty();
    }

    [Fact]
    public void When_Headers_Columns_Is_Largest_Than_Column_Rows_Each_Column_Should_Have_With_Based_On_Header()
    {
        // Arrange
        _builder.AddHeader("Header-1", "Header--2", "Header---3");
        _builder.AddRow("Row11", "Row12", "Row13");
        _builder.AddRow("Row21", "Row22", "Row23");


        // Act
        var act = _builder.Build();


        // Assert
        act[0][0].Should().Be("Header-1     ");
        act[1][0].Should().Be("─────────────");
        act[2][0].Should().Be("Row11        ");
        act[3][0].Should().Be("Row21        ");

        act[0][1].Should().Be("Header--2     ");
        act[1][1].Should().Be("──────────────");
        act[2][1].Should().Be("Row12         ");
        act[3][1].Should().Be("Row22         ");

        act[0][2].Should().Be("Header---3");
        act[1][2].Should().Be("──────────");
        act[2][2].Should().Be("Row13     ");
        act[3][2].Should().Be("Row23     ");
    }

    [Fact]
    public void When_Rows_Columns_Is_Largest_Than_Column_Header_Each_Column_Should_Have_With_Based_On_Rows()
    {
        // Arrange
        _builder.AddHeader("Header-1", "Header--2", "Header---3");
        _builder.AddRow("Row*********11", "Row***********12", "Row************13");
        _builder.AddRow("Row21", "Row22", "Row23");


        // Act
        var act = _builder.Build();


        // Assert
        act[0][0].Should().Be("Header-1           ");
        act[1][0].Should().Be("───────────────────");
        act[2][0].Should().Be("Row*********11     ");
        act[3][0].Should().Be("Row21              ");

        act[0][1].Should().Be("Header--2            ");
        act[1][1].Should().Be("─────────────────────");
        act[2][1].Should().Be("Row***********12     ");
        act[3][1].Should().Be("Row22                ");

        act[0][2].Should().Be("Header---3       ");
        act[1][2].Should().Be("─────────────────");
        act[2][2].Should().Be("Row************13");
        act[3][2].Should().Be("Row23            ");
    }

    [Fact]
    public void When_Doesnt_Add_Header_And_Rows_Shouldnt_Return_Rows_For_Header()
    {
        // Arrange
        _builder.AddRow("Row11", "Row12", "Row13");
        _builder.AddRow("Row21", "Row22", "Row23");


        // Act
        var act = _builder.Build();


        // Assert
        act[0][0].Should().Be("Row11     ");
        act[1][0].Should().Be("Row21     ");

        act[0][1].Should().Be("Row12     ");
        act[1][1].Should().Be("Row22     ");

        act[0][2].Should().Be("Row13");
        act[1][2].Should().Be("Row23");
    }

    [Fact]
    public void When_Has_Null_Columns_Should_Continue_Cells_With_Same_Width_Per_Column()
    {
        // Arrange
        _builder.AddRow("Row11", null, "Row13");
        _builder.AddRow("Row21", "Row22", null);


        // Act
        var act = _builder.Build();


        // Assert
        act[0][0].Should().Be("Row11     ");
        act[1][0].Should().Be("Row21     ");

        act[0][1].Should().Be("          ");
        act[1][1].Should().Be("Row22     ");

        act[0][2].Should().Be("Row13");
        act[1][2].Should().Be("     ");
    }

    [Fact]
    public void When_Doesnt_Add_Rows_Should_Print_Header_And_Separator_If_Exists_Header()
    {
        // Arrange
        _builder.AddHeader("Header-1", "Header--2", "Header---3");


        // Act
        var act = _builder.Build();


        // Assert
        act[0][0].Should().Be("Header-1     ");
        act[1][0].Should().Be("─────────────");

        act[0][1].Should().Be("Header--2     ");
        act[1][1].Should().Be("──────────────");

        act[0][2].Should().Be("Header---3");
        act[1][2].Should().Be("──────────");
    }
}
