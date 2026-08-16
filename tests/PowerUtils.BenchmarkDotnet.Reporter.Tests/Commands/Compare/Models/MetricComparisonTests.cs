using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.Models;

public sealed class MetricComparisonTests
{
    [Fact]
    public void When_Baseline_And_Target_Is_Null_Should_Return_Null()
    {
        // Arrange
        decimal? baseline = null;
        decimal? target = null;


        // Act
        var act = MetricComparison.CalculateExecutionTime(baseline, target);


        // Assert
        act.Should().BeNull();
    }

    [Fact]
    public void When_Baseline_Is_Null_And_Target_Is_Not_Null_Should_Return_New()
    {
        // Arrange
        decimal? baseline = null;
        decimal? target = 12;


        // Act
        var act = MetricComparison.CalculateExecutionTime(baseline, target);


        // Assert
        act.Should().NotBeNull();
        act.Status.Should().Be(ComparisonStatus.New);
        act.Target.Should().Be(target);
    }

    [Fact]
    public void When_Baseline_Is_Not_Null_And_Target_Is_Null_Should_Return_Removed()
    {
        // Arrange
        decimal? baseline = 12;
        decimal? target = null;


        // Act
        var act = MetricComparison.CalculateExecutionTime(baseline, target);


        // Assert
        act.Should().NotBeNull();
        act.Status.Should().Be(ComparisonStatus.Removed);
        act.Baseline.Should().Be(baseline);
    }

    [Fact]
    public void When_Baseline_Is_Equal_Target_Should_Return_Equal()
    {
        // Arrange
        decimal? baseline = 12;
        decimal? target = 12;


        // Act
        var act = MetricComparison.CalculateExecutionTime(baseline, target);


        // Assert
        act.Should().NotBeNull();
        act.Status.Should().Be(ComparisonStatus.Equal);
        act.Baseline.Should().Be(baseline);
        act.Target.Should().Be(target);
        act.Diff.Should().Be(0);
        act.DiffPercentage.Should().Be(0);
    }

    [Fact]
    public void When_Baseline_Is_Less_Target_Should_Return_Less()
    {
        // Arrange
        decimal? baseline = 12;
        decimal? target = 15;


        // Act
        var act = MetricComparison.CalculateExecutionTime(baseline, target);


        // Assert
        act.Should().NotBeNull();
        act.Status.Should().Be(ComparisonStatus.Worse);
        act.Baseline.Should().Be(baseline);
        act.Target.Should().Be(target);
        act.Diff.Should().Be(3);
        act.DiffPercentage.Should().Be(25);
    }

    [Fact]
    public void When_Baseline_Is_Greater_Target_Should_Return_Greater()
    {
        // Arrange
        decimal? baseline = 15;
        decimal? target = 12;


        // Act
        var act = MetricComparison.CalculateExecutionTime(baseline, target);


        // Assert
        act.Should().NotBeNull();
        act.Status.Should().Be(ComparisonStatus.Better);
        act.Baseline.Should().Be(baseline);
        act.Target.Should().Be(target);
        act.Diff.Should().Be(-3);
        act.DiffPercentage.Should().Be(-20);
    }

    [Fact]
    public void When_Baseline_Is_Zero_And_Target_Is_Zero_Should_Return_Equal()
    {
        // Arrange
        decimal? baseline = 0;
        decimal? target = 0;


        // Act
        var act = MetricComparison.CalculateExecutionTime(baseline, target);


        // Assert
        act.Should().NotBeNull();
        act.Status.Should().Be(ComparisonStatus.Equal);
        act.Baseline.Should().Be(baseline);
        act.Target.Should().Be(target);
        act.Diff.Should().Be(0);
        act.DiffPercentage.Should().BeNull();
    }

    [Fact]
    public void When_Calculate_Using_CalculateExecutionTime_Should_Return_Unit_NS()
    {
        // Arrange
        decimal? baseline = 100;
        decimal? target = 120;

        // Act
        var result = MetricComparison.CalculateExecutionTime(baseline, target);

        // Assert
        result.Should().NotBeNull();
        result.Unit.Should().Be("ns");
    }

    [Fact]
    public void When_Calculate_Using_CalculateMemoryUsage_Should_Return_Unit_B()
    {
        // Arrange
        decimal? baseline = 1000;
        decimal? target = 800;

        // Act
        var result = MetricComparison.CalculateMemoryUsage(baseline, target);

        // Assert
        result.Should().NotBeNull();
        result.Unit.Should().Be("B");
    }


    [Fact]
    public void When_Calculate_GCOperations_With_Valid_Values_Should_Return_Baseline_And_Target()
    {
        // Arrange
        decimal? baselineIteration = 1000;
        decimal? baselineOperations = 10;
        decimal? targetIteration = 2000;
        decimal? targetOperations = 2;


        // Act
        var result = MetricComparison.CalculateGarbageCollectionOperations(
            baselineIteration,
            baselineOperations,
            targetIteration,
            targetOperations);


        // Assert
        result.Should().NotBeNull();
        result.Unit.Should().BeNull();
        result.Status.Should().Be(ComparisonStatus.Worse);
        result.Baseline.Should().Be(100_000);
        result.Target.Should().Be(1_000_000);
        result.Diff.Should().Be(900_000);
        result.DiffPercentage.Should().Be(900);
    }

    [Fact]
    public void When_Calculate_GCOperations_With_BaselineIteration_Null_Should_Return_Baseline_Null()
    {
        // Arrange
        decimal? baselineIteration = null;
        decimal? baselineOperations = 12;
        decimal? targetIteration = 1000;
        decimal? targetOperations = 2000;


        // Act
        var result = MetricComparison.CalculateGarbageCollectionOperations(
            baselineIteration,
            baselineOperations,
            targetIteration,
            targetOperations);


        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ComparisonStatus.New);
        result.Baseline.Should().BeNull();
        result.Target.Should().Be(500);
        result.Diff.Should().BeNull();
        result.DiffPercentage.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(null)]
    public void When_Calculate_GCOperations_With_BaselineOperations_Invalid_Should_Return_Baseline_Null(int? baselineOperations)
    {
        // Arrange
        decimal? baselineIteration = 12;
        decimal? targetIteration = 1654;
        decimal? targetOperations = 20;


        // Act
        var result = MetricComparison.CalculateGarbageCollectionOperations(
            baselineIteration,
            baselineOperations,
            targetIteration,
            targetOperations);


        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ComparisonStatus.New);
        result.Baseline.Should().BeNull();
        result.Target.Should().Be(82_700);
        result.Diff.Should().BeNull();
        result.DiffPercentage.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(null)]
    public void When_Calculate_GCOperations_With_BaselineIteration_Invalid_Should_Return_Baseline_Null(int? baselineIteration)
    {
        // Arrange
        decimal? baselineOperations = 12;
        decimal? targetIteration = 1654;
        decimal? targetOperations = 20;


        // Act
        var result = MetricComparison.CalculateGarbageCollectionOperations(
            baselineIteration,
            baselineOperations,
            targetIteration,
            targetOperations);


        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ComparisonStatus.New);
        result.Baseline.Should().BeNull();
        result.Target.Should().Be(82_700);
        result.Diff.Should().BeNull();
        result.DiffPercentage.Should().BeNull();
    }

    [Fact]
    public void When_Calculate_GCOperations_With_TargetIteration_Null_Should_Return_Target_Null()
    {
        // Arrange
        decimal? baselineIteration = 1000;
        decimal? baselineOperations = 10;
        decimal? targetIteration = null;
        decimal? targetOperations = 2;


        // Act
        var result = MetricComparison.CalculateGarbageCollectionOperations(
            baselineIteration,
            baselineOperations,
            targetIteration,
            targetOperations);


        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ComparisonStatus.Removed);
        result.Baseline.Should().Be(100_000);
        result.Target.Should().BeNull();
        result.Diff.Should().BeNull();
        result.DiffPercentage.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(null)]
    public void When_Calculate_GCOperations_With_TargetOperations_Invalid_Should_Return_Target_Null(int? targetOperations)
    {
        // Arrange
        decimal? baselineIteration = 27;
        decimal? baselineOperations = 3;
        decimal? targetIteration = 800;


        // Act
        var result = MetricComparison.CalculateGarbageCollectionOperations(
            baselineIteration,
            baselineOperations,
            targetIteration,
            targetOperations);


        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ComparisonStatus.Removed);
        result.Baseline.Should().Be(9_000);
        result.Target.Should().BeNull();
        result.Diff.Should().BeNull();
        result.DiffPercentage.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(null)]
    public void When_Calculate_GCOperations_With_TargetIteration_Invalid_Should_Return_Target_Null(int? targetIteration)
    {
        // Arrange
        decimal? baselineIteration = 27;
        decimal? baselineOperations = 3;
        decimal? targetOperations = 800;


        // Act
        var result = MetricComparison.CalculateGarbageCollectionOperations(
            baselineIteration,
            baselineOperations,
            targetIteration,
            targetOperations);


        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ComparisonStatus.Removed);
        result.Baseline.Should().Be(9_000);
        result.Target.Should().BeNull();
        result.Diff.Should().BeNull();
        result.DiffPercentage.Should().BeNull();
    }
}
