using System;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class DomainExceptionTests
{
    [Fact]
    public void When_Pass_Message_ShouldSetSameMessage()
    {
        // Arrange
        var message = "Error message";


        // Act
        var act = new DomainException(message);


        // Assert
        act.Message.Should().Be(message);
    }

    [Fact]
    public void When_Pass_Message_And_InnerException_ShouldSetSameMessage()
    {
        // Arrange
        var message = "outer error";
        var inner = new Exception("inner error");


        // Act
        var act = new DomainException(message, inner);


        // Assert
        act.Message.Should().Be(message);
    }

    [Fact]
    public void When_Pass_Message_And_InnerException_ShouldSetSameInnerException()
    {
        // Arrange
        var message = "outer error";
        var inner = new Exception("inner error");


        // Act
        var act = new DomainException(message, inner);


        // Assert
        act.InnerException.Should().Be(inner);
    }
}
