using System;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.IOHelpersTests;

public sealed class WriteFileTests : IDisposable
{
    private readonly string _tempDirectory;


    public WriteFileTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if(Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }


    [Fact]
    public void When_Pass_Path_And_Content_Should_Create_File()
    {
        // Arrange
        var path = Path.Combine(_tempDirectory, "test.txt");
        var content = "Test content";


        // Act
        IOHelpers.WriteFile(path, content);


        // Assert
        File.Exists(path).Should().BeTrue();
        File.ReadAllText(path).Should().Be(content);
    }

    [Fact]
    public void When_Pass_Root_Path_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var path = Path.GetPathRoot(Path.GetFullPath("."))!;


        // Act
        Action act = () => IOHelpers.WriteFile(path, "Test content");


        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
