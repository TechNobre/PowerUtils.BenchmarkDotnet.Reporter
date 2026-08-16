using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Compare;

public sealed class CompareNewRemovedTests
{
    [Fact]
    public async Task When_Target_Removes_A_Method_And_Adds_A_New_One_Should_Report_New_And_Removed_Statuses()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-03/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("compare", "-b", baseline, "-t", target);


        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("StringConcat");
        result.StandardOutput.ShouldContain("[REMOVED]");
        result.StandardOutput.ShouldContain("MethodTest");
        result.StandardOutput.ShouldContain("[NEW]");
        result.StandardOutput.ShouldContain("StringJoin");
        result.StandardOutput.ShouldContain("-4.98%");
    }
}
