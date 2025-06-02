using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Validations;
namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Validations;

public sealed class ValidationExceptionTest
{
    [Fact]
    public void When_Pass_Message_ShouldSetSameMessage()
    {
        // Arrange
        var message = "Error message";


        // Act
        var act = new ValidationException(message);


        // Assert
        act.Message.ShouldBe(message);
    }

    [Fact]
    public void Constructor_WithMessagesList_ShouldJoinMessagesWithPipe()
    {
        // Arrange
        var messages = new List<string>
        {
            "First error",
            "Second error",
            "Third error"
        };


        // Act
        var act = new ValidationException(messages);


        // Assert
        act.Message.ShouldBe("First error | Second error | Third error");
    }

    [Fact]
    public void When_Pass_MessageList_ShouldJoinMessagesWithPipe()
    {
        // Arrange
        var messages = new List<string>();


        // Act
        var act = new ValidationException(messages);


        // Assert
        act.Message.ShouldBeEmpty();
    }
}
