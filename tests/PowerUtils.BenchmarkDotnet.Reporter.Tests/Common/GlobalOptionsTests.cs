using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common;

public sealed class GlobalOptionsTests
{
    [Fact]
    public void ConfigOption_ShouldHave_ExpectedShape()
    {
        // Assert
        GlobalOptions.ConfigOption.Name.Should().Be("--config");
        GlobalOptions.ConfigOption.Aliases.Should().Contain("-c");
        GlobalOptions.ConfigOption.Recursive.Should().BeTrue();
        GlobalOptions.ConfigOption.Description.Should().Be(
            "Path to a YAML configuration file. Defaults to 'pbreporter.yml' or 'pbreporter.yaml' " +
            "in the current directory when present.");
    }
}
