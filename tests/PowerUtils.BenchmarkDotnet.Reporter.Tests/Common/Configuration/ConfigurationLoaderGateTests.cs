using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Common.Configuration;

public sealed class ConfigurationLoaderGateTests
{
    [Fact]
    public void ParseEnvironmentVariables_WithEmptyDictionary_ShouldReturn_NullGateSection()
    {
        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(new Dictionary<string, string?>());


        // Assert
        configuration.Gate.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithInput_ShouldSet_Input()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__INPUT"] = "report.json"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.Input.Should().Be("report.json");
        configuration.Gate.Formats.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithFormats_ShouldSet_Formats()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__FORMATS"] = "json"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.Formats.Should().Equal("json");
        configuration.Gate.Input.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithGlobalMeanThreshold_ShouldSet_ThresholdMean()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__THRESHOLD_MEAN"] = "500ms"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.ThresholdMean.Should().Be("500ms");
        configuration.Gate.ThresholdAllocation.Should().BeNull();
        configuration.Gate.Thresholds.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithGlobalAllocationThreshold_ShouldSet_ThresholdAllocation()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__THRESHOLD_ALLOCATION"] = "5kb"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.ThresholdAllocation.Should().Be("5kb");
    }

    [Fact]
    public void ParseEnvironmentVariables_WithScopedRule_ShouldBuild_ThresholdsEntry()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__THRESHOLDS__0__PATTERN"] = "Demo.Benchmarks.*",
            ["PBREPORTER_GATE__THRESHOLDS__0__THRESHOLD_MEAN"] = "10ms",
            ["PBREPORTER_GATE__THRESHOLDS__0__THRESHOLD_ALLOCATION"] = "5kb"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.Thresholds.Should().ContainSingle();
        var entry = configuration.Gate.Thresholds![0];
        entry.Pattern.Should().Be("Demo.Benchmarks.*");
        entry.ThresholdMean.Should().Be("10ms");
        entry.ThresholdAllocation.Should().Be("5kb");
    }

    [Fact]
    public void ParseEnvironmentVariables_WithUnrecognizedFieldSegment_ShouldCreate_EmptyEntry()
    {
        // Arrange
        var variables = new Dictionary<string, string?> { ["PBREPORTER_GATE__THRESHOLDS__0__UNKNOWN_FIELD"] = "5" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.Thresholds.Should().ContainSingle();
        var entry = configuration.Gate.Thresholds![0];
        entry.Pattern.Should().BeNull();
        entry.ThresholdMean.Should().BeNull();
        entry.ThresholdAllocation.Should().BeNull();
    }

    [Theory]
    [InlineData("PBREPORTER_GATE")]
    [InlineData("PBREPORTER_COMPARE__INPUT")]
    public void ParseEnvironmentVariables_WithKeyOutsideGateSection_ShouldLeave_GateNull(string key)
    {
        // Arrange
        var variables = new Dictionary<string, string?> { [key] = "value" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate.Should().BeNull();
    }

    [Theory]
    [InlineData("PBREPORTER_GATE__UNKNOWN_KEY")]
    [InlineData("PBREPORTER_GATE__THRESHOLDS__PATTERN")]
    [InlineData("PBREPORTER_GATE__THRESHOLDS__abc__PATTERN")]
    public void ParseEnvironmentVariables_WithUnrecognizedGateKeyShape_ShouldNotSet_AnyThresholdField(string key)
    {
        // Arrange
        var variables = new Dictionary<string, string?> { [key] = "value" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.Input.Should().BeNull();
        configuration.Gate.ThresholdMean.Should().BeNull();
        configuration.Gate.ThresholdAllocation.Should().BeNull();
        configuration.Gate.Thresholds.Should().BeNull();
    }

    [Fact]
    public void ParseEnvironmentVariables_WithGateAndCompareTogether_ShouldPopulate_BothSectionsIndependently()
    {
        // Arrange
        // Both commands' scoped-entry dictionaries must stay independent - a Compare index shouldn't
        // leak into Gate's Thresholds list or vice versa.
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN"] = "Compare.*",
            ["PBREPORTER_GATE__THRESHOLDS__0__PATTERN"] = "Gate.*"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Compare!.Thresholds.Should().ContainSingle(rule => rule.Pattern == "Compare.*");
        configuration.Gate!.Thresholds.Should().ContainSingle(rule => rule.Pattern == "Gate.*");
    }


    [Fact]
    public void ParseYamlDocument_WithNoGateKey_ShouldReturn_NullGateSection()
    {
        // Arrange
        var document = new Dictionary<string, object?>();


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithGateKeyNotAMapping_ShouldReturn_NullGateSection()
    {
        // Arrange
        var document = new Dictionary<string, object?> { ["gate"] = "not-a-mapping" };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithUnknownGateKey_ShouldThrow_DomainException()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?> { ["report"] = "report.json" }
        };


        // Act
        var act = () => ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Unknown key 'report'*'gate' configuration section*");
    }

    [Fact]
    public void ParseYamlDocument_WithUnknownThresholdEntryKey_ShouldThrow_DomainException()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["pattern"] = "Demo.*", ["thresholdFoo"] = "5ms" }
                }
            }
        };


        // Act
        var act = () => ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Unknown key 'thresholdFoo'*gate.thresholds*");
    }

    [Fact]
    public void ParseYamlDocument_WithAllKnownGateKeys_ShouldNotThrow()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["input"] = "report.json",
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
    public void ParseYamlDocument_WithInput_ShouldSet_Value()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?> { ["input"] = "report-full.json" }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.Input.Should().Be("report-full.json");
    }

    [Fact]
    public void ParseYamlDocument_WithoutInput_ShouldLeave_ItNull()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>()
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.Input.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithGlobalThresholds_ShouldSet_Values()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["thresholdMean"] = "500ms" },
                    new Dictionary<string, object?> { ["thresholdAllocation"] = "5kb" }
                }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.ThresholdMean.Should().Be("500ms");
        configuration.Gate.ThresholdAllocation.Should().Be("5kb");
        configuration.Gate.Thresholds.Should().BeEmpty();
    }

    [Fact]
    public void ParseYamlDocument_WithMultipleGlobalEntriesForSameMetric_ShouldKeep_LastOne()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["thresholdMean"] = "500ms" },
                    new Dictionary<string, object?> { ["thresholdMean"] = "50ms" }
                }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.ThresholdMean.Should().Be("50ms");
    }

    [Fact]
    public void ParseYamlDocument_WithGlobalEntrySettingOnlyOneMetric_ShouldNotAffect_OtherMetric()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["thresholdMean"] = "500ms" }
                }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.ThresholdMean.Should().Be("500ms");
        configuration.Gate.ThresholdAllocation.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithMixOfGlobalAndScopedEntries_ShouldResolve_BothCorrectly()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["thresholdMean"] = "500ms" },
                    new Dictionary<string, object?>
                    {
                        ["pattern"] = "DemoApi.*",
                        ["thresholdMean"] = "100ms"
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
        configuration.Gate!.ThresholdMean.Should().Be("500ms");
        configuration.Gate.ThresholdAllocation.Should().Be("10kb");
        configuration.Gate.Thresholds.Should().HaveCount(2);
        configuration.Gate.Thresholds.Should().Contain(rule =>
            rule.Pattern == "DemoApi.*" && rule.ThresholdMean == "100ms");
        configuration.Gate.Thresholds.Should().Contain(rule =>
            rule.Pattern == "DemoApi.Controllers.CreateController.Create" && rule.ThresholdAllocation == "5kb");
    }

    [Fact]
    public void ParseYamlDocument_WithScopedThresholds_ShouldBuild_Entries()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
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
        configuration.Gate!.Thresholds.Should().ContainSingle();
        var entry = configuration.Gate.Thresholds![0];
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
            ["gate"] = new Dictionary<string, object?> { ["thresholds"] = "not-a-list" }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.Thresholds.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithNonMappingThresholdEntry_ShouldBeSkipped()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?> { "not-a-mapping" }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.Thresholds.Should().BeEmpty();
    }

    [Fact]
    public void ParseYamlDocument_WithGateAndCompareTogether_ShouldPopulate_BothSectionsIndependently()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["compare"] = new Dictionary<string, object?> { ["baseline"] = "baseline.json" },
            ["gate"] = new Dictionary<string, object?> { ["input"] = "report.json" }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Compare!.Baseline.Should().Be("baseline.json");
        configuration.Gate!.Input.Should().Be("report.json");
    }


    [Fact]
    public void Load_WithFileGateInput_AndEnvironmentGateInput_ShouldPrefer_EnvironmentValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              input: from-file.json
            """);

        const string INPUT_ENV_VAR = "PBREPORTER_GATE__INPUT";
        Environment.SetEnvironmentVariable(INPUT_ENV_VAR, "from-env.json");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.Input.Should().Be("from-env.json");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(INPUT_ENV_VAR, null);
        }
    }

    [Fact]
    public void Load_WithFileGateFormats_AndNoEnvironmentOverride_ShouldUse_FileValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              formats: markdown
            """);

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.Formats.Should().Equal("markdown");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WithFileGateGlobalThreshold_AndEnvironmentGlobalThreshold_ShouldPrefer_EnvironmentValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              thresholds:
                - thresholdMean: 500ms
            """);

        const string MEAN_ENV_VAR = "PBREPORTER_GATE__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(MEAN_ENV_VAR, "5ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.ThresholdMean.Should().Be("5ms");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(MEAN_ENV_VAR, null);
        }
    }

    [Fact]
    public void Load_WithFileGateGlobalAllocation_AndNoEnvironmentOverride_ShouldUse_FileValue()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              thresholds:
                - thresholdAllocation: 5kb
            """);

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.ThresholdAllocation.Should().Be("5kb");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WithNoFile_AndEnvironmentGateScopedRule_ShouldUse_EnvironmentRule()
    {
        // Arrange
        // No file present: the merge's "lower" (file) side is a null Gate section, exercising
        // _mergeGateSections/_mergeGateThresholds handling a null lower side without throwing.
        var scratchDirectory = Directory.CreateTempSubdirectory("pbreporter-loader-test-");

        const string PATTERN_ENV_VAR = "PBREPORTER_GATE__THRESHOLDS__0__PATTERN";
        const string MEAN_ENV_VAR = "PBREPORTER_GATE__THRESHOLDS__0__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(PATTERN_ENV_VAR, "Demo.*");
        Environment.SetEnvironmentVariable(MEAN_ENV_VAR, "10ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(null, scratchDirectory.FullName);


            // Assert
            configuration.Gate!.Thresholds.Should().ContainSingle(rule =>
                rule.Pattern == "Demo.*" && rule.ThresholdMean == "10ms");
        }
        finally
        {
            scratchDirectory.Delete(recursive: true);
            Environment.SetEnvironmentVariable(PATTERN_ENV_VAR, null);
            Environment.SetEnvironmentVariable(MEAN_ENV_VAR, null);
        }
    }

    [Fact]
    public void Load_WithFileGateScopedRule_AndNoEnvironmentThresholds_ShouldKeep_FileRule()
    {
        // Arrange
        // Env sets no PBREPORTER_GATE__THRESHOLDS__* vars at all, so the merge's "higher" side has a
        // null Thresholds list - exercises _mergeGateThresholds' second loop with higher == null.
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              thresholds:
                - pattern: "Demo.*"
                  thresholdMean: 10ms
            """);

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.Thresholds.Should().ContainSingle(rule =>
                rule.Pattern == "Demo.*" && rule.ThresholdMean == "10ms");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WithFileGateAllocation_AndEnvironmentMeanOnlyForSamePattern_ShouldKeep_FileAllocation()
    {
        // Arrange
        // File sets allocation for the pattern; env only overrides mean for that same pattern.
        // The merged entry must keep the file's allocation, not lose it.
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              thresholds:
                - pattern: "Demo.*"
                  thresholdAllocation: 10kb
            """);

        const string PATTERN_ENV_VAR = "PBREPORTER_GATE__THRESHOLDS__0__PATTERN";
        const string MEAN_ENV_VAR = "PBREPORTER_GATE__THRESHOLDS__0__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(PATTERN_ENV_VAR, "Demo.*");
        Environment.SetEnvironmentVariable(MEAN_ENV_VAR, "10ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.Thresholds.Should().ContainSingle();
            var entry = configuration.Gate.Thresholds![0];
            entry.ThresholdMean.Should().Be("10ms");
            entry.ThresholdAllocation.Should().Be("10kb");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(PATTERN_ENV_VAR, null);
            Environment.SetEnvironmentVariable(MEAN_ENV_VAR, null);
        }
    }

    [Fact]
    public void Load_WithFileOnlyGatePattern_AndUnrelatedEnvironmentGatePattern_ShouldKeep_BothRules()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              thresholds:
                - pattern: "Demo.Benchmarks.ArrayProcessorBenchmarks.*"
                  thresholdMean: 10ms
            """);

        const string PATTERN_ENV_VAR = "PBREPORTER_GATE__THRESHOLDS__0__PATTERN";
        const string MEAN_ENV_VAR = "PBREPORTER_GATE__THRESHOLDS__0__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(PATTERN_ENV_VAR, "Demo.Benchmarks.StringProcessorBenchmarks.*");
        Environment.SetEnvironmentVariable(MEAN_ENV_VAR, "20ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.Thresholds.Should().HaveCount(2);
            configuration.Gate.Thresholds.Should().Contain(rule =>
                rule.Pattern == "Demo.Benchmarks.ArrayProcessorBenchmarks.*" && rule.ThresholdMean == "10ms");
            configuration.Gate.Thresholds.Should().Contain(rule =>
                rule.Pattern == "Demo.Benchmarks.StringProcessorBenchmarks.*" && rule.ThresholdMean == "20ms");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(PATTERN_ENV_VAR, null);
            Environment.SetEnvironmentVariable(MEAN_ENV_VAR, null);
        }
    }

    [Fact]
    public void Load_WithEnvironmentGateScopedRuleMissingPattern_ShouldBeIgnored_ByMerge()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              thresholds:
                - pattern: "Demo.*"
                  thresholdMean: 10ms
            """);

        const string UNKNOWN_FIELD_ENV_VAR = "PBREPORTER_GATE__THRESHOLDS__0__UNKNOWN_FIELD";
        Environment.SetEnvironmentVariable(UNKNOWN_FIELD_ENV_VAR, "5ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.Thresholds.Should().ContainSingle(rule =>
                rule.Pattern == "Demo.*" && rule.ThresholdMean == "10ms");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(UNKNOWN_FIELD_ENV_VAR, null);
        }
    }

    [Fact]
    public void Load_WithNoGateSection_AndNoGateEnvironmentVariables_ShouldLeave_GateNull()
    {
        // Arrange
        var scratchDirectory = Directory.CreateTempSubdirectory("pbreporter-loader-test-");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(null, scratchDirectory.FullName);


            // Assert
            configuration.Gate.Should().BeNull();
        }
        finally
        {
            scratchDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseEnvironmentVariables_WithMultipleScopedRuleIndices_ShouldBuild_OrderedEntries()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__THRESHOLDS__10__PATTERN"] = "Third.*",
            ["PBREPORTER_GATE__THRESHOLDS__2__PATTERN"] = "Second.*",
            ["PBREPORTER_GATE__THRESHOLDS__1__PATTERN"] = "First.*"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.Thresholds!.Select(rule => rule.Pattern)
            .Should().Equal("First.*", "Second.*", "Third.*");
    }

    [Theory]
    [InlineData("pbreporter_gate__input")]
    [InlineData("PBREPORTER_Gate__Input")]
    public void ParseEnvironmentVariables_GateKeyMatching_ShouldBe_CaseInsensitive(string key)
    {
        // Arrange
        var variables = new Dictionary<string, string?> { [key] = "report.json" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.Input.Should().Be("report.json");
    }

    [Fact]
    public void ParseEnvironmentVariables_GateFieldMatching_ShouldBe_CaseInsensitive()
    {
        // Arrange
        var variables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__formats"] = "json",
            ["PBREPORTER_GATE__Threshold_Mean"] = "10ms",
            ["PBREPORTER_GATE__threshold_allocation"] = "5kb",
            ["PBREPORTER_GATE__thresholds__0__pattern"] = "Demo.*",
            ["PBREPORTER_GATE__thresholds__0__threshold_mean"] = "1ms",
            ["PBREPORTER_GATE__thresholds__0__Threshold_Allocation"] = "2kb"
        };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        var gate = configuration.Gate!;
        gate.Formats.Should().Equal("json");
        gate.ThresholdMean.Should().Be("10ms");
        gate.ThresholdAllocation.Should().Be("5kb");
        var rule = gate.Thresholds.Should().ContainSingle().Subject;
        rule.Pattern.Should().Be("Demo.*");
        rule.ThresholdMean.Should().Be("1ms");
        rule.ThresholdAllocation.Should().Be("2kb");
    }

    [Fact]
    public void ParseEnvironmentVariables_WithThresholdsKeyMissingFieldSegment_ShouldNotThrow()
    {
        // Arrange
        var variables = new Dictionary<string, string?> { ["PBREPORTER_GATE__THRESHOLDS__0"] = "value" };


        // Act
        var configuration = ConfigurationLoader.ParseEnvironmentVariables(variables);


        // Assert
        configuration.Gate!.Thresholds.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithUnknownGateKey_ErrorMessage_ShouldList_SupportedKeys()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?> { ["report"] = "report.json" }
        };


        // Act
        var act = () => ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Supported keys: formats, input, thresholds.*");
    }

    [Fact]
    public void ParseYamlDocument_WithUnknownThresholdEntryKey_ErrorMessage_ShouldList_SupportedKeys()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["thresholdFoo"] = "5ms" }
                }
            }
        };


        // Act
        var act = () => ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Supported keys: pattern, thresholdAllocation, thresholdMean.*");
    }

    [Fact]
    public void ParseYamlDocument_WithFormats_AsScalar_ShouldSet_SingleFormat()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?> { ["formats"] = "json" }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.Formats.Should().Equal("json");
    }

    [Fact]
    public void ParseYamlDocument_WithFormats_AsList_ShouldSet_MultipleFormats_IgnoringNonStrings()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["formats"] = new List<object?> { "json", 5, "markdown" }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.Formats.Should().Equal("json", "markdown");
    }

    [Fact]
    public void ParseYamlDocument_WithoutFormats_ShouldLeave_FormatsNull()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?> { ["input"] = "report.json" }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.Formats.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithFormats_AsEmptyList_ShouldLeave_FormatsNull()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?> { ["formats"] = new List<object?>() }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.Formats.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithFormats_AsMapping_ShouldLeave_FormatsNull()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["formats"] = new Dictionary<string, object?> { ["a"] = "b" }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.Formats.Should().BeNull();
    }

    [Fact]
    public void ParseYamlDocument_WithScopedAndGlobalEntries_ShouldKeep_ScopedOrder()
    {
        // Arrange
        var document = new Dictionary<string, object?>
        {
            ["gate"] = new Dictionary<string, object?>
            {
                ["thresholds"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["pattern"] = "B.*", ["thresholdMean"] = "1ms" },
                    new Dictionary<string, object?> { ["thresholdMean"] = "9ms" },
                    new Dictionary<string, object?> { ["pattern"] = "A.*", ["thresholdAllocation"] = "1kb" }
                }
            }
        };


        // Act
        var configuration = ConfigurationLoader.ParseYamlDocument(document);


        // Assert
        configuration.Gate!.ThresholdMean.Should().Be("9ms");
        configuration.Gate.Thresholds!.Select(rule => rule.Pattern).Should().Equal("B.*", "A.*");
    }

    [Fact]
    public void Load_WithFileAndEnvironmentPatternsDifferingOnlyByCase_ShouldMerge_IntoOneRule()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              thresholds:
                - pattern: "Demo.*"
                  thresholdMean: 10ms
                  thresholdAllocation: 5kb
            """);

        const string PATTERN_ENV_VAR = "PBREPORTER_GATE__THRESHOLDS__0__PATTERN";
        const string MEAN_ENV_VAR = "PBREPORTER_GATE__THRESHOLDS__0__THRESHOLD_MEAN";
        Environment.SetEnvironmentVariable(PATTERN_ENV_VAR, "demo.*");
        Environment.SetEnvironmentVariable(MEAN_ENV_VAR, "20ms");

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.Thresholds.Should().ContainSingle();
            var rule = configuration.Gate.Thresholds![0];
            rule.ThresholdMean.Should().Be("20ms");
            rule.ThresholdAllocation.Should().Be("5kb");
        }
        finally
        {
            File.Delete(path);
            Environment.SetEnvironmentVariable(PATTERN_ENV_VAR, null);
            Environment.SetEnvironmentVariable(MEAN_ENV_VAR, null);
        }
    }

    [Fact]
    public void Load_WithEmptyFileThresholds_AndNoEnvironmentThresholds_ShouldLeave_ThresholdsNull()
    {
        // Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            """
            gate:
              input: report.json
              thresholds: []
            """);

        try
        {
            // Act
            var configuration = ConfigurationLoader.Load(path);


            // Assert
            configuration.Gate!.Thresholds.Should().BeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
