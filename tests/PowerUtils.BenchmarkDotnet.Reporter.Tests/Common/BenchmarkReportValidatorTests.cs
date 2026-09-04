using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class BenchmarkReportValidatorTests
{
    [Theory]
    [InlineData("RELEASE")]
    [InlineData("release")]
    [InlineData("Release")]
    public void When_Configuration_Is_Release_Should_Not_Add_Message(string configuration)
    {
        // Arrange
        var messages = new List<string>();
        var report = _createBenchmarkReport(configuration);


        // Act
        messages.AddIfNotRelease(report);


        // Assert
        messages.Should().BeEmpty();
    }

    [Theory]
    [InlineData("DEBUG")]
    [InlineData("Debug")]
    [InlineData("ddd")]
    [InlineData("")]
    public void When_Configuration_Is_Not_Release_Should_Add_Message(string configuration)
    {
        // Arrange
        var messages = new List<string>();
        var report = _createBenchmarkReport(configuration);


        // Act
        messages.AddIfNotRelease(report);


        // Assert
        messages.Should().ContainSingle()
            .Which.Should().Be($"[Fake.Namespace.FakeBenchmark.FakeMethod] The report wasn't executed in RELEASE mode: '{configuration}'");
    }

    [Fact]
    public void When_Configuration_Is_Null_Should_Add_Message()
    {
        // Arrange
        var messages = new List<string>();
        var report = _createBenchmarkReport(null);


        // Act
        messages.AddIfNotRelease(report);


        // Assert
        messages.Should().ContainSingle()
            .Which.Should().Be("[Fake.Namespace.FakeBenchmark.FakeMethod] The report wasn't executed in RELEASE mode: ''");
    }

    [Fact]
    public void When_HostEnvironmentInfo_Is_Null_Should_Add_Message()
    {
        // Arrange
        var messages = new List<string>();
        var report = new BenchmarkReport
        {
            FullName = "Fake.Namespace.FakeBenchmark.FakeMethod",
            Header = new()
        };


        // Act
        messages.AddIfNotRelease(report);


        // Assert
        messages.Should().ContainSingle()
            .Which.Should().Be("[Fake.Namespace.FakeBenchmark.FakeMethod] The report wasn't executed in RELEASE mode: ''");
    }

    [Fact]
    public void When_Header_Is_Null_Should_Add_Message()
    {
        // Arrange
        var messages = new List<string>();
        var report = new BenchmarkReport { FullName = "Fake.Namespace.FakeBenchmark.FakeMethod" };


        // Act
        messages.AddIfNotRelease(report);


        // Assert
        messages.Should().ContainSingle()
            .Which.Should().Be("[Fake.Namespace.FakeBenchmark.FakeMethod] The report wasn't executed in RELEASE mode: ''");
    }

    [Fact]
    public void When_Report_Is_Null_Should_Not_Add_Message()
    {
        // Arrange
        var messages = new List<string>();
        BenchmarkReport? report = null;


        // Act
        messages.AddIfNotRelease(report);


        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public void When_List_Already_Has_Messages_Should_Append_Message()
    {
        // Arrange
        var messages = new List<string> { "Existing message" };
        var report = _createBenchmarkReport("DEBUG");


        // Act
        messages.AddIfNotRelease(report);


        // Assert
        messages.Should().HaveCount(2);
        messages[0].Should().Be("Existing message");
        messages[1].Should().Be("[Fake.Namespace.FakeBenchmark.FakeMethod] The report wasn't executed in RELEASE mode: 'DEBUG'");
    }

    private static BenchmarkReport _createBenchmarkReport(string? configuration) => new()
    {
        FullName = "Fake.Namespace.FakeBenchmark.FakeMethod",
        Header = new()
        {
            HostEnvironmentInfo = new() { Configuration = configuration }
        }
    };
}
