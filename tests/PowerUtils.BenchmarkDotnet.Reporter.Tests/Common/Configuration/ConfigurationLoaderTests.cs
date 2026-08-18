using System;
using System.Collections.Generic;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.Configuration;

public sealed class ConfigurationLoaderTests
{
    [Fact]
    public void Load_ShouldReturn_Configuration()
    {
        // Act
        var configuration = ConfigurationLoader.Load();


        // Assert
        configuration.Should().NotBeNull();
    }

    [Fact]
    public void Load_ShouldReflect_RealProcessEnvironmentVariables()
    {
        // Arrange
        const string variableName = "PBREPORTER_COMPARE__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(variableName, "7%");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load();


            // Assert
            configuration.Compare!.ThresholdMean.Should().Be("7%");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }


    [Fact]
    public void ParseEnvironmentVariables_WithEmptyDictionary_ShouldReturn_NullCompareSection()
    {
        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(new Dictionary<string, string?>());


        // Assert
        configuration.Compare.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithUnrelatedVariables_ShouldBeIgnored()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PATH"] = "/usr/bin",
            ["OTHER_TOOL_THRESHOLD_MEAN"] = "5%"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithGlobalMeanThreshold_ShouldSet_ThresholdMean()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__THRESHOLD_MEAN"] = "5%"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare.Should().NotBeNull();
        configuration.Compare!.ThresholdMean.Should().Be("5%");
        configuration.Compare.ThresholdAllocation.Should().BeNull();
        configuration.Compare.Thresholds.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithGlobalAllocationThreshold_ShouldSet_ThresholdAllocation()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__THRESHOLD_ALLOCATION"] = "5kb"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.ThresholdAllocation.Should().Be("5kb");
    }

    [Fact]
    public void ParseEnvironmentVariables_WithBaseline_ShouldSet_Baseline()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__BASELINE"] = "baseline-full.json"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.Baseline.Should().Be("baseline-full.json");
        configuration.Compare.Target.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithTarget_ShouldSet_Target()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__TARGET"] = "target-full.json"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.Target.Should().Be("target-full.json");
        configuration.Compare.Baseline.Should().BeNull();
    }

    [Theory]
    [InlineData("PBREPORTER_COMPARE__BASELINE")]
    [InlineData("pbreporter_compare__baseline")]
    [InlineData("Pbreporter_Compare__Baseline")]
    public void ParseEnvironmentVariables_BaselineKeyMatching_ShouldBe_CaseInsensitive(string key)
    {
        // Arrange
        var variables = new Dictionary<string, string?> { [key] = "baseline-full.json" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.Baseline.Should().Be("baseline-full.json");
    }

    [Theory]
    [InlineData("PBREPORTER_COMPARE__THRESHOLD_MEAN")]
    [InlineData("pbreporter_compare__threshold_mean")]
    [InlineData("Pbreporter_Compare__Threshold_Mean")]
    public void ParseEnvironmentVariables_KeyMatching_ShouldBe_CaseInsensitive(string key)
    {
        // Arrange
        var variables = new Dictionary<string, string?> { [key] = "5%" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.ThresholdMean.Should().Be("5%");
    }

    [Fact]
    public void ParseEnvironmentVariables_WithScopedRule_ShouldBuild_ThresholdsEntry()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN"] = "Demo.Benchmarks.*",
            ["PBREPORTER_COMPARE__THRESHOLDS__0__THRESHOLD_MEAN"] = "10ms",
            ["PBREPORTER_COMPARE__THRESHOLDS__0__THRESHOLD_ALLOCATION"] = "5kb"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.Thresholds.Should().ContainSingle();
        var entry = configuration.Compare.Thresholds![0];
        entry.Pattern.Should().Be("Demo.Benchmarks.*");
        entry.ThresholdMean.Should().Be("10ms");
        entry.ThresholdAllocation.Should().Be("5kb");
    }

    [Fact]
    public void ParseEnvironmentVariables_WithMultipleScopedRuleIndices_ShouldBuild_OrderedEntries()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__THRESHOLDS__1__PATTERN"] = "Demo.Benchmarks.StringProcessorBenchmarks.*",
            ["PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN"] = "Demo.Benchmarks.ArrayProcessorBenchmarks.*"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.Thresholds.Should().HaveCount(2);
        configuration.Compare.Thresholds![0].Pattern.Should().Be("Demo.Benchmarks.ArrayProcessorBenchmarks.*");
        configuration.Compare.Thresholds[1].Pattern.Should().Be("Demo.Benchmarks.StringProcessorBenchmarks.*");
    }

    [Fact]
    public void ParseEnvironmentVariables_WithGlobalAndScopedRulesTogether_ShouldKeep_BothOnTheSameSection()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__THRESHOLD_MEAN"] = "5%",
            ["PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN"] = "Demo.*"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        // The global value must survive even though the Thresholds list is assigned after the loop.
        configuration.Compare!.ThresholdMean.Should().Be("5%");
        configuration.Compare.Thresholds.Should().ContainSingle(rule => rule.Pattern == "Demo.*");
    }

    [Theory]
    [InlineData("PBREPORTER_COMPARE")]
    [InlineData("PBREPORTER_FUTURE_COMMAND__SOME_KEY")]
    public void ParseEnvironmentVariables_WithKeyOutsideCompareSection_ShouldLeave_CompareNull(string key)
    {
        // Arrange
        var variables = new Dictionary<string, string?> { [key] = "5%" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare.Should().BeNull();
    }

    [Theory]
    [InlineData("PBREPORTER_COMPARE__UNKNOWN_KEY")]
    [InlineData("PBREPORTER_COMPARE__THRESHOLDS__PATTERN")]
    [InlineData("PBREPORTER_COMPARE__THRESHOLDS__abc__PATTERN")]
    public void ParseEnvironmentVariables_WithUnrecognizedCompareKeyShape_ShouldNotSet_AnyThresholdField(string key)
    {
        // Arrange
        var variables = new Dictionary<string, string?> { [key] = "5%" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.ThresholdMean.Should().BeNull();
        configuration.Compare.ThresholdAllocation.Should().BeNull();
        configuration.Compare.Thresholds.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithThresholdsKeyMissingFieldSegment_ShouldNotThrow()
    {
        // Arrange
        // 3 segments (COMPARE, THRESHOLDS, "0") with a numeric-looking last segment: must not be treated
        // as a complete "index + field" key, and must not index past the end of the segments array.
        var variables = new Dictionary<string, string?> { ["PBREPORTER_COMPARE__THRESHOLDS__0"] = "5%" };

        // Act
        var act = () => ConfigurationLoader.ParseEnvironmentVariables(variables);

        // Assert
        act.Should().NotThrow();
        var configuration = act();
        configuration.Compare!.Thresholds.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithScopedRuleUnknownField_ShouldCreate_EmptyEntry()
    {
        // Arrange
        var variables = new Dictionary<string, string?> { ["PBREPORTER_COMPARE__THRESHOLDS__0__UNKNOWN_FIELD"] = "5%" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.Thresholds.Should().ContainSingle();
        var entry = configuration.Compare.Thresholds![0];
        entry.Pattern.Should().BeNull();
        entry.ThresholdMean.Should().BeNull();
        entry.ThresholdAllocation.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseEnvironmentVariables_WithBlankValue_ShouldBeIgnored(string? value)
    {
        // Arrange
        var variables = new Dictionary<string, string?> { ["PBREPORTER_COMPARE__THRESHOLD_MEAN"] = value };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare.Should().BeNull();
    }


    [Fact]
    public void ParseYamlDocument_WithNoCompareKey_ShouldReturn_NullCompareSection()
    {
        // Arrange
        var document = new Dictionary<string, object?>();


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithCompareKeyNotAMapping_ShouldReturn_NullCompareSection()
    {
        // Arrange
        var document = new Dictionary<string, object?> { ["compare"] = "not-a-mapping" };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithUnknownCompareKey_ShouldThrow_DomainException()
    {
        // Arrange
        // 'format' (singular) was the old key name; it is now 'formats'. This test ensures the
        // typo is caught rather than silently ignored.
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?> { ["format"] = "json" }
        };


        // Act
        var act = () => ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Unknown key 'format'*'compare' configuration section*");
    }

    [Fact]
    public void ParseYamlDocument_WithUnknownCompareKey_ErrorMessage_ShouldList_SupportedKeys()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?> { ["typo"] = "value" }
        };


        // Act
        var act = () => ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Supported keys: baseline, formats, target, thresholds*");
    }

    [Fact]
    public void ParseYamlDocument_WithUnknownThresholdEntryKey_ShouldThrow_DomainException()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["pattern"] = "Demo.*", ["thresholdFoo"] = "5%" }
                }
            }
        };


        // Act
        var act = () => ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Unknown key 'thresholdFoo'*compare.thresholds*");
    }

    [Fact]
    public void ParseYamlDocument_WithAllKnownCompareKeys_ShouldNotThrow()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["baseline"] = "baseline.json",
                ["target"] = "target.json",
                ["formats"] = "json",
                ["thresholds"] = new List<object?>()
            }
        };


        // Act
        var act = () => ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ParseYamlDocument_WithBaselineAndTarget_ShouldSet_Values()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["baseline"] = "baseline-full.json",
                ["target"] = "target-full.json"
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Baseline.Should().Be("baseline-full.json");
        configuration.Compare.Target.Should().Be("target-full.json");
    }

    [Fact]
    public void ParseYamlDocument_WithoutBaselineOrTarget_ShouldLeave_ThemNull()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>()
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Baseline.Should().BeNull();
        configuration.Compare.Target.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithGlobalThresholds_ShouldSet_Values()
    {
        // Arrange
        // A thresholds entry with no "pattern" key is the global rule for whichever metric it sets.
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["thresholdMean"] = "5%" },
                    new Dictionary<string, object?> { ["thresholdAllocation"] = "5kb" }
                }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.ThresholdMean.Should().Be("5%");
        configuration.Compare.ThresholdAllocation.Should().Be("5kb");
        // Both entries were pattern-less (global), so nothing lands in the scoped list.
        configuration.Compare.Thresholds.Should().BeEmpty();
    }

    [Fact]
    public void ParseYamlDocument_WithMultipleGlobalEntriesForSameMetric_ShouldKeep_LastOne()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["thresholdMean"] = "50%" },
                    new Dictionary<string, object?> { ["thresholdMean"] = "5%" }
                }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.ThresholdMean.Should().Be("5%");
    }

    [Fact]
    public void ParseYamlDocument_WithGlobalEntrySettingOnlyOneMetric_ShouldNotAffect_OtherMetric()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["thresholdMean"] = "5%" }
                }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.ThresholdMean.Should().Be("5%");
        configuration.Compare.ThresholdAllocation.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithMixOfGlobalAndScopedEntries_ShouldResolve_BothCorrectly()
    {
        // Arrange
        // Mirrors the documented pbreporter.yml example: two pattern-less (global) entries, one
        // per metric, interleaved with two scoped entries.
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["thresholdMean"] = "5%" },
                    new Dictionary<string, object?>
                    {
                        ["pattern"] = "DemoApi.*",
                        ["thresholdMean"] = "10ms"
                    },
                    new Dictionary<string, object?> { ["thresholdAllocation"] = "10kb" },
                    new Dictionary<string, object?>
                    {
                        ["pattern"] = "DemoApi.Controllers.CreateController.Create",
                        ["thresholdAllocation"] = "5kb"
                    }
                }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.ThresholdMean.Should().Be("5%");
        configuration.Compare.ThresholdAllocation.Should().Be("10kb");
        configuration.Compare.Thresholds.Should().HaveCount(2);
        configuration.Compare.Thresholds.Should().Contain(rule =>
            rule.Pattern == "DemoApi.*" && rule.ThresholdMean == "10ms");
        configuration.Compare.Thresholds.Should().Contain(rule =>
            rule.Pattern == "DemoApi.Controllers.CreateController.Create" && rule.ThresholdAllocation == "5kb");
    }

    [Fact]
    public void ParseYamlDocument_WithScopedThresholds_ShouldBuild_Entries()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?>
                    {
                        ["pattern"] = "Demo.*",
                        ["thresholdMean"] = "10ms",
                        ["thresholdAllocation"] = "5kb"
                    }
                }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Thresholds.Should().ContainSingle();
        var entry = configuration.Compare.Thresholds![0];
        entry.Pattern.Should().Be("Demo.*");
        entry.ThresholdMean.Should().Be("10ms");
        entry.ThresholdAllocation.Should().Be("5kb");
    }

    [Fact]
    public void ParseYamlDocument_WithThresholdsNotAList_ShouldLeave_ThresholdsNull()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?> { ["thresholds"] = "not-a-list" }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Thresholds.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithNonMappingThresholdEntry_ShouldBeSkipped()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?> { "not-a-mapping" }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Thresholds.Should().BeEmpty();
    }


    [Fact]
    public void Load_WithExplicitConfigFilePath_ShouldParse_YamlFile()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - thresholdMean: 5%
            """);

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.ThresholdMean.Should().Be("5%");
            // The one entry was global (no pattern); Load()'s file/env merge collapses an empty
            // scoped list with no env-side entries back to null (functionally equivalent downstream).
            configuration.Compare.Thresholds.Should().BeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WithExplicitConfigFilePath_WhenFileMissing_ShouldThrow_DomainException()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), $"pbreporter-missing-{Guid.NewGuid():N}.yml");


        // Act
        var act = () => ConfigurationLoader.Load(path);


        // Assert
        act.Should().Throw<DomainException>().WithMessage($"The configuration file '{path}' was not found.");
    }

    [Fact]
    public void Load_WithoutExplicitConfigFilePath_WhenNoDefaultFileExists_ShouldNotThrow()
    {
        // Arrange
        // Uses the (explicitConfigFilePath, workingDirectory) overload so this never touches the
        // real process working directory, which is shared, global state across parallel tests.
        var scratchDirectory = Directory.CreateTempSubdirectory("pbreporter-loader-test-");

        try
        {
            // Act
            var act = () => ConfigurationLoader.Load(null, scratchDirectory.FullName);


            // Assert
            act.Should().NotThrow();
            act().Compare.Should().BeNull();
        }
        finally
        {
            scratchDirectory.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("pbreporter.yml")]
    [InlineData("pbreporter.yaml")]
    public void Load_WithoutExplicitConfigFilePath_ShouldFallBackTo_DefaultFileNameInGivenDirectory(string fileName)
    {
        // Arrange
        var scratchDirectory = Directory.CreateTempSubdirectory("pbreporter-loader-test-");

        try
        {
            File.WriteAllText(
                Path.Combine(scratchDirectory.FullName, fileName),
                """
                compare:
                  thresholds:
                    - thresholdMean: 5%
                """);


            // Act
            var configuration = ConfigurationLoader.Load(null, scratchDirectory.FullName);


            // Assert
            configuration.Compare!.ThresholdMean.Should().Be("5%");
        }
        finally
        {
            scratchDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_WithMalformedYamlFile_ShouldThrow_DomainException()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              not a valid line
            """);

        try
        {
            // Act
            var act = () => ConfigurationLoader.Load(path);


            // Assert
            act.Should().Throw<DomainException>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WithFileGlobalAndEnvironmentScopedOverride_ShouldMerge_AtFieldLevel()
    {
        // Arrange
        // File sets both metrics for the same pattern; env only overrides allocation for that pattern.
        // The merged entry must keep the file's mean and use the env's allocation.
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - pattern: "Demo.*"
                  thresholdMean: 10ms
                  thresholdAllocation: 10kb
            """);

        const string patternEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN";
        const string allocationEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__THRESHOLD_ALLOCATION";
        Environment.SetEnvironmentVariable(patternEnvVar, "Demo.*");
        Environment.SetEnvironmentVariable(allocationEnvVar, "5kb");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.Thresholds.Should().ContainSingle();
            var entry = configuration.Compare.Thresholds![0];
            entry.Pattern.Should().Be("Demo.*");
            entry.ThresholdMean.Should().Be("10ms");
            entry.ThresholdAllocation.Should().Be("5kb");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(patternEnvVar, null);
            Environment.SetEnvironmentVariable(allocationEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithFileOnlyPattern_AndUnrelatedEnvironmentPattern_ShouldKeep_BothRules()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - pattern: "Demo.Benchmarks.ArrayProcessorBenchmarks.*"
                  thresholdMean: 10ms
            """);

        const string patternEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN";
        const string meanEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(patternEnvVar, "Demo.Benchmarks.StringProcessorBenchmarks.*");
        Environment.SetEnvironmentVariable(meanEnvVar, "20ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.Thresholds.Should().HaveCount(2);
            configuration.Compare.Thresholds.Should().Contain(rule =>
                rule.Pattern == "Demo.Benchmarks.ArrayProcessorBenchmarks.*" && rule.ThresholdMean == "10ms");
            configuration.Compare.Thresholds.Should().Contain(rule =>
                rule.Pattern == "Demo.Benchmarks.StringProcessorBenchmarks.*" && rule.ThresholdMean == "20ms");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(patternEnvVar, null);
            Environment.SetEnvironmentVariable(meanEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithFileBaseline_AndEnvironmentBaseline_ShouldPrefer_EnvironmentValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              baseline: from-file.json
            """);

        const string baselineEnvVar = "PBREPORTER_COMPARE__BASELINE";
        Environment.SetEnvironmentVariable(baselineEnvVar, "from-env.json");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.Baseline.Should().Be("from-env.json");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(baselineEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithFileTargetOnly_AndNoEnvironmentOverride_ShouldUse_FileValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              target: from-file.json
            """);

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            // Env sets nothing, so this only resolves correctly if the merge falls back to the file's value.
            configuration.Compare!.Target.Should().Be("from-file.json");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WithFileGlobalThreshold_AndEnvironmentGlobalThreshold_ShouldPrefer_EnvironmentValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - thresholdMean: 50%
            """);

        const string meanEnvVar = "PBREPORTER_COMPARE__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(meanEnvVar, "5%");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.ThresholdMean.Should().Be("5%");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(meanEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithFileGlobalAllocation_AndEnvironmentGlobalAllocation_ShouldPrefer_EnvironmentValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - thresholdAllocation: 50kb
            """);

        const string allocationEnvVar = "PBREPORTER_COMPARE__THRESHOLD_ALLOCATION";
        Environment.SetEnvironmentVariable(allocationEnvVar, "5kb");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.ThresholdAllocation.Should().Be("5kb");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(allocationEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithNoFile_AndEnvironmentScopedRule_ShouldUse_EnvironmentRule()
    {
        // Arrange
        // No file present: the merge's "lower" (file) side is a null Thresholds list, exercising
        // that _mergeThresholds handles a null lower list without throwing.
        var scratchDirectory = Directory.CreateTempSubdirectory("pbreporter-loader-test-");

        const string patternEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN";
        const string meanEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(patternEnvVar, "Demo.*");
        Environment.SetEnvironmentVariable(meanEnvVar, "10ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(null, scratchDirectory.FullName);


            // Assert
            configuration.Compare!.Thresholds.Should().ContainSingle(rule =>
                rule.Pattern == "Demo.*" && rule.ThresholdMean == "10ms");
        }
        finally
        {
            scratchDirectory.Delete(recursive: true);
            Environment.SetEnvironmentVariable(patternEnvVar, null);
            Environment.SetEnvironmentVariable(meanEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithFileAllocation_AndEnvironmentMeanOnlyForSamePattern_ShouldKeep_FileAllocation()
    {
        // Arrange
        // File sets allocation for the pattern; env only overrides mean for that same pattern.
        // The merged entry must keep the file's allocation, not lose it.
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - pattern: "Demo.*"
                  thresholdAllocation: 10kb
            """);

        const string patternEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN";
        const string meanEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(patternEnvVar, "Demo.*");
        Environment.SetEnvironmentVariable(meanEnvVar, "10ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.Thresholds.Should().ContainSingle();
            var entry = configuration.Compare.Thresholds![0];
            entry.ThresholdMean.Should().Be("10ms");
            entry.ThresholdAllocation.Should().Be("10kb");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(patternEnvVar, null);
            Environment.SetEnvironmentVariable(meanEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithEnvironmentScopedRuleMissingPattern_ShouldBeIgnored_ByMerge()
    {
        // Arrange
        // The env-only key (no PATTERN segment) produces a Thresholds entry with a null Pattern;
        // the merge must skip it rather than adding a bogus rule or crashing.
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - pattern: "Demo.*"
                  thresholdMean: 10ms
            """);

        const string unknownFieldEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__UNKNOWN_FIELD";
        Environment.SetEnvironmentVariable(unknownFieldEnvVar, "5%");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.Thresholds.Should().ContainSingle(rule =>
                rule.Pattern == "Demo.*" && rule.ThresholdMean == "10ms");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(unknownFieldEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithFileAndEnvironmentBothSettingMeanForSamePattern_ShouldPrefer_EnvironmentValue()
    {
        // Arrange
        // Both layers set ThresholdMean (to different values) for the same pattern: env must win.
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - pattern: "Demo.*"
                  thresholdMean: 50ms
            """);

        const string patternEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN";
        const string meanEnvVar = "PBREPORTER_COMPARE__THRESHOLDS__0__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(patternEnvVar, "Demo.*");
        Environment.SetEnvironmentVariable(meanEnvVar, "5ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.Thresholds.Should().ContainSingle(rule =>
                rule.Pattern == "Demo.*" && rule.ThresholdMean == "5ms");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(patternEnvVar, null);
            Environment.SetEnvironmentVariable(meanEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithExplicitConfigFilePath_ShouldTakePrecedence_OverDefaultFileInSameDirectory()
    {
        // Arrange
        // A default pbreporter.yml also happens to exist in the working directory; the explicitly
        // passed --config path must still win over it, not the other way around.
        var scratchDirectory = Directory.CreateTempSubdirectory("pbreporter-loader-test-");
        File.WriteAllText(
            Path.Combine(scratchDirectory.FullName, "pbreporter.yml"),
            """
            compare:
              thresholds:
                - thresholdMean: 50%
            """);

        var explicitPath = Path.GetTempFileName();
        File.WriteAllText(
            explicitPath,
            """
            compare:
              thresholds:
                - thresholdMean: 5%
            """);

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(explicitPath, scratchDirectory.FullName);


            // Assert
            configuration.Compare!.ThresholdMean.Should().Be("5%");
        }
        finally
        {
            File.Delete(explicitPath);
            scratchDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_WithFileAllocationOnly_AndNoEnvironmentOverride_ShouldUse_FileValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - thresholdAllocation: 5kb
            """);

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            // Env sets nothing, so this only resolves correctly if the merge falls back to the file's value.
            configuration.Compare!.ThresholdAllocation.Should().Be("5kb");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ParseEnvironmentVariables_WithFormats_ShouldSet_Formats()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__FORMATS"] = "json"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.Formats.Should().Equal("json");
        configuration.Compare.Baseline.Should().BeNull();
        configuration.Compare.Target.Should().BeNull();
    }

    [Theory]
    [InlineData("PBREPORTER_COMPARE__FORMATS")]
    [InlineData("pbreporter_compare__formats")]
    [InlineData("Pbreporter_Compare__Formats")]
    public void ParseEnvironmentVariables_FormatsKeyMatching_ShouldBe_CaseInsensitive(string key)
    {
        // Arrange
        var variables = new Dictionary<string, string?> { [key] = "markdown" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.Formats.Should().Equal("markdown");
    }

    [Fact]
    public void ParseYamlDocument_WithFormats_AsScalar_ShouldSet_SingleFormat()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["formats"] = "markdown"
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Formats.Should().Equal("markdown");
    }

    [Fact]
    public void ParseYamlDocument_WithFormats_AsFlowSequence_ShouldSet_MultipleFormats()
    {
        // Arrange
        // Simulates `formats: [json, markdown, console]` after the YAML parser resolves the flow sequence.
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["formats"] = new List<object?> { "json", "markdown", "console" }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Formats.Should().Equal("json", "markdown", "console");
    }

    [Fact]
    public void ParseYamlDocument_WithFormats_AsBlockSequence_ShouldSet_MultipleFormats()
    {
        // Arrange
        // Simulates the block-style `formats:\n  - json\n  - markdown` after YAML parsing.
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>
            {
                ["formats"] = new List<object?> { "json", "markdown" }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Formats.Should().Equal("json", "markdown");
    }

    [Fact]
    public void ParseYamlDocument_WithoutFormats_ShouldLeave_FormatsNull()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?>()
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Formats.Should().BeNull();
    }

    [Fact]
    public void Load_WithFileFormats_AndEnvironmentFormats_ShouldPrefer_EnvironmentValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              formats: markdown
            """);

        const string formatsEnvVar = "PBREPORTER_COMPARE__FORMATS";
        Environment.SetEnvironmentVariable(formatsEnvVar, "json");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.Formats.Should().Equal("json");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(formatsEnvVar, null);
        }
    }

    [Fact]
    public void Load_WithFileScopedRule_AndNoEnvironmentThresholds_ShouldKeep_FileRule()
    {
        // Arrange
        // Env sets no PBREPORTER_COMPARE__THRESHOLDS__* vars at all, so the merge's "higher" side
        // has a null Thresholds list - exercises _mergeThresholds' second loop with higher == null.
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            compare:
              thresholds:
                - pattern: "Demo.*"
                  thresholdMean: 10ms
            """);

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Compare!.Thresholds.Should().ContainSingle(rule =>
                rule.Pattern == "Demo.*" && rule.ThresholdMean == "10ms");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
