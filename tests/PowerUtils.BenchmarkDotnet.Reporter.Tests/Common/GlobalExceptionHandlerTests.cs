using System;
using System.CommandLine;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class GlobalExceptionHandlerTests
{
    private static ParseResult _newParseResult() => new Command("test").Parse([]);


    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    public void Wrap_WhenActionSucceeds_ShouldReturn_SameExitCode(int exitCode)
    {
        // Arrange
        int action(ParseResult _) => exitCode;


        // Act
        var result = GlobalExceptionHandler.Wrap(action)(_newParseResult());


        // Assert
        result.Should().Be(exitCode);
    }

    [Fact]
    public void Wrap_WhenDomainExceptionIsThrown_ShouldReturn_ErrorExitCode()
    {
        // Arrange
        static int action(ParseResult _) => throw new DomainException("something went wrong");

        var parseResult = _newParseResult();
        parseResult.InvocationConfiguration.Error = TextWriter.Null;
        parseResult.InvocationConfiguration.Output = TextWriter.Null;


        // Act
        var result = GlobalExceptionHandler.Wrap(action)(parseResult);


        // Assert
        result.Should().Be(Constants.ExitCodes.ERROR);
    }

    [Fact]
    public void Wrap_WhenDomainExceptionIsThrown_ShouldWrite_ErrorMessage_ToStderr()
    {
        // Arrange
        const string MESSAGE = "bad config file";
        static int action(ParseResult _) => throw new DomainException(MESSAGE);

        var parseResult = _newParseResult();
        using var stderrWriter = new StringWriter();
        parseResult.InvocationConfiguration.Error = stderrWriter;
        parseResult.InvocationConfiguration.Output = TextWriter.Null;


        // Act
        GlobalExceptionHandler.Wrap(action)(parseResult);


        // Assert
        stderrWriter.ToString().Should().Contain($"Error: {MESSAGE}");
    }

    [Fact]
    public void Wrap_WhenDomainExceptionIsThrown_ShouldWrite_HelpText_ToOutput()
    {
        // Arrange
        static int action(ParseResult _) => throw new DomainException("some error");

        var parseResult = _newParseResult();
        parseResult.InvocationConfiguration.Error = TextWriter.Null;
        using var stdoutWriter = new StringWriter();
        parseResult.InvocationConfiguration.Output = stdoutWriter;


        // Act
        GlobalExceptionHandler.Wrap(action)(parseResult);


        // Assert
        stdoutWriter.ToString().Should().Contain("Usage:");
    }

    [Fact]
    public void Wrap_WhenNonDomainExceptionIsThrown_ShouldRethrow()
    {
        // Arrange
        static int action(ParseResult _) => throw new InvalidOperationException("internal bug");


        // Act
        Action act = () => GlobalExceptionHandler.Wrap(action)(_newParseResult());


        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("internal bug");
    }
}
