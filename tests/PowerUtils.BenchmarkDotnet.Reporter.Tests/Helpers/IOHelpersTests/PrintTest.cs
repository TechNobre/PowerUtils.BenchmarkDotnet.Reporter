using System;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.IOHelpersTests;

public sealed class PrintTest
{
    [Fact]
    public void When_Pass_Message_Should_Print_It()
    {
        // Arrange
        var originalOutput = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var expectedMessage = "Test message";


        // Act
        IOHelpers.Print(expectedMessage);


        // Assert
        var output = stringWriter.ToString();
        output.ShouldBe(expectedMessage);

        Console.SetOut(originalOutput);
    }
}
