using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.Configuration;

public sealed class YamlDocumentParserTests
{
    [Fact]
    public void Parse_WithEmptyText_ShouldReturn_EmptyMapping()
    {
        // Act
        var result = YamlDocumentParser.Parse(string.Empty);


        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithOnlyCommentsAndBlankLines_ShouldReturn_EmptyMapping()
    {
        // Arrange
        var text = """
        # a comment

           # another comment
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithFlatScalarMapping_ShouldReturn_Values()
    {
        // Arrange
        var text = """
        compare:
          thresholdMean: 5%
          thresholdAllocation: 5kb
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        result.Should().ContainKey("compare");
        var compare = result["compare"].Should().BeOfType<Dictionary<string, object?>>().Subject;
        compare["thresholdMean"].Should().Be("5%");
        compare["thresholdAllocation"].Should().Be("5kb");
    }

    [Theory]
    [InlineData("key: \"quoted value\"", "quoted value")]
    [InlineData("key: 'quoted value'", "quoted value")]
    [InlineData("key: unquoted value", "unquoted value")]
    public void Parse_ScalarValues_ShouldBe_Unquoted(string line, string expected)
    {
        // Act
        var result = YamlDocumentParser.Parse(line);


        // Assert
        result["key"].Should().Be(expected);
    }

    [Fact]
    public void Parse_KeyLookup_ShouldBe_CaseInsensitive()
    {
        // Arrange
        var text = """
        Compare:
          ThresholdMean: 5%
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        result.Should().ContainKey("compare");
    }

    [Fact]
    public void Parse_WithSequenceOfMappings_ShouldReturn_ListOfDictionaries()
    {
        // Arrange
        var text = """
        compare:
          thresholds:
            - pattern: "Demo.*"
              thresholdMean: 10ms
            - pattern: "Demo.Benchmarks.ArrayProcessorBenchmarks.Method"
              thresholdAllocation: 5kb
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var compare = (Dictionary<string, object?>)result["compare"]!;
        var thresholds = compare["thresholds"].Should().BeOfType<List<object?>>().Subject;
        thresholds.Should().HaveCount(2);

        var first = (Dictionary<string, object?>)thresholds[0]!;
        first["pattern"].Should().Be("Demo.*");
        first["thresholdMean"].Should().Be("10ms");
        first.Should().NotContainKey("thresholdAllocation");

        var second = (Dictionary<string, object?>)thresholds[1]!;
        second["pattern"].Should().Be("Demo.Benchmarks.ArrayProcessorBenchmarks.Method");
        second["thresholdAllocation"].Should().Be("5kb");
    }

    [Fact]
    public void Parse_WithFullReadmeExample_ShouldMatch_ExpectedShape()
    {
        // Arrange
        var text = """
        compare:
          thresholds:
            - thresholdMean: 5%
            - pattern: "DemoApi.*"
              thresholdMean: 10ms
            - thresholdAllocation: 10kb
            - pattern: "DemoApi.Controllers.CreateController.Create"
              thresholdAllocation: 5kb
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var compare = (Dictionary<string, object?>)result["compare"]!;
        var thresholds = (List<object?>)compare["thresholds"]!;
        thresholds.Should().HaveCount(4);
        var globalMean = (Dictionary<string, object?>)thresholds[0]!;
        globalMean["thresholdMean"].Should().Be("5%");
        globalMean.Should().NotContainKey("pattern");
    }

    [Fact]
    public void Parse_WithBareScalarSequence_ShouldReturn_ListOfStrings()
    {
        // Arrange
        var text = """
        formats:
          - json
          - markdown
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var formats = result["formats"].Should().BeOfType<List<object?>>().Subject;
        formats.Should().Equal("json", "markdown");
    }

    [Fact]
    public void Parse_WithSequenceItemOnItsOwnLine_ShouldReturn_NestedMapping()
    {
        // Arrange
        var text = """
        thresholds:
          -
            pattern: Demo.*
            thresholdMean: 10ms
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var thresholds = (List<object?>)result["thresholds"]!;
        var entry = (Dictionary<string, object?>)thresholds[0]!;
        entry["pattern"].Should().Be("Demo.*");
        entry["thresholdMean"].Should().Be("10ms");
    }

    [Fact]
    public void Parse_WithNestedMapping_ShouldReturn_DeeplyNestedValues()
    {
        // Arrange
        var text = """
        root:
          child:
            grandchild: value
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var root = (Dictionary<string, object?>)result["root"]!;
        var child = (Dictionary<string, object?>)root["child"]!;
        child["grandchild"].Should().Be("value");
    }

    [Fact]
    public void Parse_WithLineMissingColon_ShouldThrow_DomainException()
    {
        // Arrange
        var text = """
        compare:
          not a valid line
        """;


        // Act
        var act = () => YamlDocumentParser.Parse(text);


        // Assert
        // Line 2 (1-based) is the offending line; the message must name it and quote the content.
        act.Should().Throw<DomainException>().WithMessage("*line 2*'not a valid line'*");
    }

    [Fact]
    public void Parse_WithTabIndentation_ShouldThrow_DomainException()
    {
        // Arrange
        var text = "compare:\n\tthresholdMean: 5%\n";


        // Act
        var act = () => YamlDocumentParser.Parse(text);


        // Assert
        // Line 2 (1-based) is the offending line.
        act.Should().Throw<DomainException>().WithMessage("*Tabs are not allowed*line 2*");
    }

    [Fact]
    public void Parse_WithSequenceAtRoot_ShouldThrow_DomainException()
    {
        // Arrange
        var text = """
        - one
        - two
        """;


        // Act
        var act = () => YamlDocumentParser.Parse(text);


        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Parse_WithKeyHavingNoValueAndNoNestedBlock_ShouldSet_NullValue()
    {
        // Arrange
        var text = """
        compare:
          thresholdMean:
          thresholdAllocation: 5kb
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var compare = (Dictionary<string, object?>)result["compare"]!;
        compare["thresholdMean"].Should().BeNull();
        compare["thresholdAllocation"].Should().Be("5kb");
    }

    [Fact]
    public void Parse_WithCrLfLineEndings_ShouldSplit_Lines()
    {
        // Arrange
        var text = "compare:\r\n  thresholdMean: 5%\r\n  thresholdAllocation: 5kb\r\n";


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var compare = (Dictionary<string, object?>)result["compare"]!;
        compare["thresholdMean"].Should().Be("5%");
        compare["thresholdAllocation"].Should().Be("5kb");
    }

    [Fact]
    public void Parse_WithBareDashAsLastLine_ShouldAdd_NullItem_WithoutThrowing()
    {
        // Arrange
        var text = """
        thresholds:
          -
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var thresholds = result["thresholds"].Should().BeOfType<List<object?>>().Subject;
        thresholds.Should().Equal((object?)null);
    }

    [Fact]
    public void Parse_WithInlineKeyMissingValue_FollowedByNestedBlock_ShouldReturn_NestedMapping()
    {
        // Arrange
        var text = """
        thresholds:
          - nested:
              pattern: Demo.*
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var thresholds = (List<object?>)result["thresholds"]!;
        var item = (Dictionary<string, object?>)thresholds[0]!;
        var nested = (Dictionary<string, object?>)item["nested"]!;
        nested["pattern"].Should().Be("Demo.*");
    }

    [Fact]
    public void Parse_WithInlineKeyMissingValue_AsLastLine_ShouldSet_NullValue()
    {
        // Arrange
        var text = """
        thresholds:
          - nested:
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var thresholds = (List<object?>)result["thresholds"]!;
        var item = (Dictionary<string, object?>)thresholds[0]!;
        item["nested"].Should().BeNull();
    }

    [Fact]
    public void Parse_WithInlineKeyMissingValue_FollowedByLineAtItemIndent_ShouldTreatAsSiblingKey_NotNestedBlock()
    {
        // Arrange
        // "sibling" is at the same indent as the list item's own keys (itemIndent = 4), not deeper,
        // so it must be parsed as another key of the list item's mapping, not as a child of "nested".
        var text = """
        thresholds:
          - nested:
            sibling: value
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var thresholds = (List<object?>)result["thresholds"]!;
        var item = (Dictionary<string, object?>)thresholds[0]!;
        item["nested"].Should().BeNull();
        item["sibling"].Should().Be("value");
    }

    [Fact]
    public void Parse_WithSingleInlineMappingItem_AsEntireDocument_ShouldNotThrow()
    {
        // Arrange
        var text = "- key: value";


        // Act
        var result = YamlDocumentParser.Parse("root:\n  " + text);


        // Assert
        var list = (List<object?>)result["root"]!;
        var item = (Dictionary<string, object?>)list[0]!;
        item["key"].Should().Be("value");
    }

    [Theory]
    [InlineData("key: \"unterminated")]
    [InlineData("key: unterminated\"")]
    [InlineData("key: 'unterminated")]
    [InlineData("key: unterminated'")]
    public void Parse_WithMismatchedQuotes_ShouldNotBeUnquoted(string line)
    {
        // Act
        var result = YamlDocumentParser.Parse(line);


        // Assert
        var rawValue = line["key: ".Length..];
        result["key"].Should().Be(rawValue);
    }

    [Fact]
    public void Parse_WithEmptyQuotedString_ShouldReturn_EmptyString()
    {
        // Act
        var result = YamlDocumentParser.Parse("key: \"\"");


        // Assert
        result["key"].Should().Be(string.Empty);
    }

    [Fact]
    public void Parse_WithFlowSequence_ShouldReturn_ListOfStrings()
    {
        // Arrange
        var text = "formats: [json, markdown, console]";


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var formats = result["formats"].Should().BeOfType<List<object?>>().Subject;
        formats.Should().Equal("json", "markdown", "console");
    }

    [Fact]
    public void Parse_WithFlowSequenceWithExtraWhitespace_ShouldReturn_TrimmedItems()
    {
        // Arrange
        var text = "formats: [ json , markdown ]";


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var formats = result["formats"].Should().BeOfType<List<object?>>().Subject;
        formats.Should().Equal("json", "markdown");
    }

    [Fact]
    public void Parse_WithFlowSequenceWithQuotedItems_ShouldReturn_UnquotedItems()
    {
        // Arrange
        var text = "formats: [\"json\", 'markdown']";


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var formats = result["formats"].Should().BeOfType<List<object?>>().Subject;
        formats.Should().Equal("json", "markdown");
    }

    [Fact]
    public void Parse_WithEmptyFlowSequence_ShouldReturn_EmptyList()
    {
        // Arrange
        var text = "formats: []";


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var formats = result["formats"].Should().BeOfType<List<object?>>().Subject;
        formats.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithFlowSequenceInsideNestedMapping_ShouldReturn_ListOfStrings()
    {
        // Arrange
        var text = """
        compare:
          formats: [json, markdown]
        """;


        // Act
        var result = YamlDocumentParser.Parse(text);


        // Assert
        var compare = (Dictionary<string, object?>)result["compare"]!;
        var formats = compare["formats"].Should().BeOfType<List<object?>>().Subject;
        formats.Should().Equal("json", "markdown");
    }
}
