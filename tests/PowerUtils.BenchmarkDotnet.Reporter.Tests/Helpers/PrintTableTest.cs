using System;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers;

public sealed class TableBuilderTest
{
    private readonly TableBuilder _builder = TableBuilder.Create();


    [Fact]
    public void When_Doesnt_Have_Any_Rows_Should_Return_EmptyArray()
    {
        // Arrange & Act
        var act = _builder.Build();


        // Assert
        act.ShouldBeEmpty();
    }

    [Fact]
    public void When_Calling_AddHeader_Twice_Should_Throw_InvalidOperationException()
    {
        // Arrange
        _builder.AddHeader("Header1", "Header2");


        // Act
        TableBuilder act() => _builder.AddHeader("Header1", "Header2");


        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Header has already been added");
    }

    [Fact]
    public void When_Calling_AddHeader_After_Adding_Rows_Should_Throw_InvalidOperationException()
    {
        // Arrange
        _builder.AddRow("Row1Col1", "Row1Col2");


        // Act
        TableBuilder act() => _builder.AddHeader("Header1", "Header2");


        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Rows have already been added, cannot add header now");
    }

    [Fact]
    public void When_Try_Add_Row_With_Diferent_Number_Of_Columns_Than_Header_Should_Throw_InvalidOperationException()
    {
        // Arrange
        _builder.AddHeader("Header1", "Header2");


        // Act
        TableBuilder act() => _builder.AddRow("Row1Col1", "Row1Col2", "Row1Col3");


        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot add row with a different number of columns than already defined before");
    }

    [Fact]
    public void When_Add_Header_With_Zero_Columns_Shouldnt_Add_Any_Rows()
    {
        // Arrange & Act
        _builder.AddHeader();
        var act = _builder.Build();


        // Assert
        act.ShouldBeEmpty();
    }

    [Fact]
    public void When_Add_Row_With_Zero_Columns_Shouldnt_Add_Any_Rows()
    {
        // Arrange & Act
        _builder.AddRow();
        var act = _builder.Build();


        // Assert
        act.ShouldBeEmpty();
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
        act[0][0].ShouldBe("Header-1     ");
        act[1][0].ShouldBe("─────────────");
        act[2][0].ShouldBe("Row11        ");
        act[3][0].ShouldBe("Row21        ");

        act[0][1].ShouldBe("Header--2     ");
        act[1][1].ShouldBe("──────────────");
        act[2][1].ShouldBe("Row12         ");
        act[3][1].ShouldBe("Row22         ");

        act[0][2].ShouldBe("Header---3");
        act[1][2].ShouldBe("──────────");
        act[2][2].ShouldBe("Row13     ");
        act[3][2].ShouldBe("Row23     ");
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
        act[0][0].ShouldBe("Header-1           ");
        act[1][0].ShouldBe("───────────────────");
        act[2][0].ShouldBe("Row*********11     ");
        act[3][0].ShouldBe("Row21              ");

        act[0][1].ShouldBe("Header--2            ");
        act[1][1].ShouldBe("─────────────────────");
        act[2][1].ShouldBe("Row***********12     ");
        act[3][1].ShouldBe("Row22                ");

        act[0][2].ShouldBe("Header---3       ");
        act[1][2].ShouldBe("─────────────────");
        act[2][2].ShouldBe("Row************13");
        act[3][2].ShouldBe("Row23            ");
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
        act[0][0].ShouldBe("Row11     ");
        act[1][0].ShouldBe("Row21     ");

        act[0][1].ShouldBe("Row12     ");
        act[1][1].ShouldBe("Row22     ");

        act[0][2].ShouldBe("Row13");
        act[1][2].ShouldBe("Row23");
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
        act[0][0].ShouldBe("Row11     ");
        act[1][0].ShouldBe("Row21     ");

        act[0][1].ShouldBe("          ");
        act[1][1].ShouldBe("Row22     ");

        act[0][2].ShouldBe("Row13");
        act[1][2].ShouldBe("     ");
    }
}
