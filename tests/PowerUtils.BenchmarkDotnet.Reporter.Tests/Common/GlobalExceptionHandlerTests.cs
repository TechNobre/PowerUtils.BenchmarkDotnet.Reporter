using System;
using System.CommandLine;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class GlobalExceptionHandlerTests
{
    private static ParseResult _newParseResult() => new Command("test").Parse(Array.Empty<string>());


    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    public void Wrap_WhenActionSucceeds_ShouldReturn_SameExitCode(int exitCode)
    {
        // Arrange
        Func<ParseResult, int> action = _ => exitCode;


        // Act
        var result = GlobalExceptionHandler.Wrap(action)(_newParseResult());


        // Assert
        result.Should().Be(exitCode);
    }

    [Fact]
    public void Wrap_WhenDomainExceptionIsThrown_ShouldReturn_ErrorExitCode()
    {
        // Arrange
        Func<ParseResult, int> action = _ => throw new DomainException("something went wrong");

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
        const string message = "bad config file";
        Func<ParseResult, int> action = _ => throw new DomainException(message);

        var parseResult = _newParseResult();
        using var stderrWriter = new StringWriter();
        parseResult.InvocationConfiguration.Error = stderrWriter;
        parseResult.InvocationConfiguration.Output = TextWriter.Null;


        // Act
        GlobalExceptionHandler.Wrap(action)(parseResult);


        // Assert
        stderrWriter.ToString().Should().Contain($"Error: {message}");
    }

    [Fact]
    public void Wrap_WhenDomainExceptionIsThrown_ShouldWrite_HelpText_ToOutput()
    {
        // Arrange
        Func<ParseResult, int> action = _ => throw new DomainException("some error");

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
        Func<ParseResult, int> action = _ => throw new InvalidOperationException("internal bug");


        // Act
        Action act = () => GlobalExceptionHandler.Wrap(action)(_newParseResult());


        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("internal bug");
    }
}
