using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Models;

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
        act.ShouldBeNull();
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
        act.ShouldNotBeNull();
        act.Status.ShouldBe(ComparisonStatus.New);
        act.Target.ShouldBe(target);
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
        act.ShouldNotBeNull();
        act.Status.ShouldBe(ComparisonStatus.Removed);
        act.Baseline.ShouldBe(baseline);
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
        act.ShouldNotBeNull();
        act.Status.ShouldBe(ComparisonStatus.Equal);
        act.Baseline.ShouldBe(baseline);
        act.Target.ShouldBe(target);
        act.Diff.ShouldBe(0);
        act.DiffPercentage.ShouldBe(0);
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
        act.ShouldNotBeNull();
        act.Status.ShouldBe(ComparisonStatus.Worse);
        act.Baseline.ShouldBe(baseline);
        act.Target.ShouldBe(target);
        act.Diff.ShouldBe(3);
        act.DiffPercentage.ShouldBe(25);
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
        act.ShouldNotBeNull();
        act.Status.ShouldBe(ComparisonStatus.Better);
        act.Baseline.ShouldBe(baseline);
        act.Target.ShouldBe(target);
        act.Diff.ShouldBe(-3);
        act.DiffPercentage.ShouldBe(-20);
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
        act.ShouldNotBeNull();
        act.Status.ShouldBe(ComparisonStatus.Equal);
        act.Baseline.ShouldBe(baseline);
        act.Target.ShouldBe(target);
        act.Diff.ShouldBe(0);
        act.DiffPercentage.ShouldBeNull();
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
        result.ShouldNotBeNull();
        result.Unit.ShouldBe("ns");
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
        result.ShouldNotBeNull();
        result.Unit.ShouldBe("B");
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
        result.ShouldNotBeNull();
        result.Unit.ShouldBeNull();
        result.Status.ShouldBe(ComparisonStatus.Worse);
        result.Baseline.ShouldBe(100_000);
        result.Target.ShouldBe(1_000_000);
        result.Diff.ShouldBe(900_000);
        result.DiffPercentage.ShouldBe(900);
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
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ComparisonStatus.New);
        result.Baseline.ShouldBeNull();
        result.Target.ShouldBe(500);
        result.Diff.ShouldBeNull();
        result.DiffPercentage.ShouldBeNull();
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
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ComparisonStatus.New);
        result.Baseline.ShouldBeNull();
        result.Target.ShouldBe(82_700);
        result.Diff.ShouldBeNull();
        result.DiffPercentage.ShouldBeNull();
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
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ComparisonStatus.New);
        result.Baseline.ShouldBeNull();
        result.Target.ShouldBe(82_700);
        result.Diff.ShouldBeNull();
        result.DiffPercentage.ShouldBeNull();
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
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ComparisonStatus.Removed);
        result.Baseline.ShouldBe(100_000);
        result.Target.ShouldBeNull();
        result.Diff.ShouldBeNull();
        result.DiffPercentage.ShouldBeNull();
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
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ComparisonStatus.Removed);
        result.Baseline.ShouldBe(9_000);
        result.Target.ShouldBeNull();
        result.Diff.ShouldBeNull();
        result.DiffPercentage.ShouldBeNull();
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
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ComparisonStatus.Removed);
        result.Baseline.ShouldBe(9_000);
        result.Target.ShouldBeNull();
        result.Diff.ShouldBeNull();
        result.DiffPercentage.ShouldBeNull();
    }
}
