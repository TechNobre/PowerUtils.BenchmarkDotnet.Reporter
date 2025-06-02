using System;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.IOHelpersTests;

public sealed class WriteFileTest : IDisposable
{
    private readonly string _tempDirectory;


    public WriteFileTest()
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
        File.Exists(path).ShouldBeTrue();
        File.ReadAllText(path).ShouldBe(content);
    }
}
