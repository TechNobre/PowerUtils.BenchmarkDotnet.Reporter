using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration.CompareConfigurationSection;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

public static class ConfigurationLoader
{
    private const string ENV_VAR_PREFIX = "PBREPORTER_";
    private static readonly string[] _segmentSeparator = ["__"];
    private static readonly string[] _defaultConfigFileNames = ["pbreporter.yml", "pbreporter.yaml"];

    private static readonly IReadOnlySet<string> _compareKnownKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "baseline", "target", "formats", "thresholds" };

    private static readonly IReadOnlySet<string> _thresholdEntryKnownKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pattern", "thresholdMean", "thresholdAllocation" };


    // Precedence, lowest to highest: config file < environment variables < CLI arguments
    // (CLI arguments are applied on top of this by each command's own options parser).
    public static PbReporterConfiguration Load(string? explicitConfigFilePath = null)
        => Load(explicitConfigFilePath, Directory.GetCurrentDirectory());

    // Overload with an explicit working directory, so the default-config-file lookup can be
    // tested without mutating the real process working directory (which is shared, global state).
    public static PbReporterConfiguration Load(string? explicitConfigFilePath, string workingDirectory)
    {
        var fileConfiguration = _loadFromFile(explicitConfigFilePath, workingDirectory);
        var envConfiguration = ParseEnvironmentVariables(_readEnvironmentVariables());

        return new PbReporterConfiguration
        {
            Compare = _mergeCompareSections(fileConfiguration.Compare, envConfiguration.Compare)
        };
    }

    public static PbReporterConfiguration ParseEnvironmentVariables(IReadOnlyDictionary<string, string?> environmentVariables)
    {
        var configuration = new PbReporterConfiguration();
        var scopedEntries = new SortedDictionary<int, ScopedThresholdConfig>();

        foreach(var (key, value) in environmentVariables)
        {
            if(string.IsNullOrWhiteSpace(value) || !key.StartsWith(ENV_VAR_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var segments = key[ENV_VAR_PREFIX.Length..].Split(_segmentSeparator, StringSplitOptions.RemoveEmptyEntries);
            _apply(configuration, scopedEntries, segments, value);
        }

        if(scopedEntries.Count > 0)
        {
            configuration.Compare ??= new CompareConfigurationSection();
            configuration.Compare.Thresholds = scopedEntries.Values.ToList();
        }

        return configuration;
    }

    public static PbReporterConfiguration ParseYamlDocument(IReadOnlyDictionary<string, object?> document)
    {
        var configuration = new PbReporterConfiguration();

        if(document.TryGetValue("compare", out var compareNode) && compareNode is IReadOnlyDictionary<string, object?> compareMapping)
        {
            configuration.Compare = _parseCompareSection(compareMapping);
        }

        return configuration;
    }


    private static void _apply(
        PbReporterConfiguration configuration,
        SortedDictionary<int, ScopedThresholdConfig> scopedEntries,
        string[] segments,
        string value)
    {
        if(segments.Length < 2 || !segments[0].Equals("COMPARE", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        configuration.Compare ??= new CompareConfigurationSection();

        if(segments.Length == 2 && segments[1].Equals("BASELINE", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Compare.Baseline = value;
        }
        else if(segments.Length == 2 && segments[1].Equals("TARGET", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Compare.Target = value;
        }
        else if(segments.Length == 2 && segments[1].Equals("FORMATS", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Compare.Formats = [value];
        }
        else if(segments.Length == 2 && segments[1].Equals("THRESHOLD_MEAN", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Compare.ThresholdMean = value;
        }
        else if(segments.Length == 2 && segments[1].Equals("THRESHOLD_ALLOCATION", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Compare.ThresholdAllocation = value;
        }
        else if(segments.Length == 4
            && segments[1].Equals("THRESHOLDS", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(segments[2], out var index))
        {
            if(!scopedEntries.TryGetValue(index, out var entry))
            {
                entry = new ScopedThresholdConfig();
                scopedEntries[index] = entry;
            }

            if(segments[3].Equals("PATTERN", StringComparison.OrdinalIgnoreCase))
            {
                entry.Pattern = value;
            }
            else if(segments[3].Equals("THRESHOLD_MEAN", StringComparison.OrdinalIgnoreCase))
            {
                entry.ThresholdMean = value;
            }
            else if(segments[3].Equals("THRESHOLD_ALLOCATION", StringComparison.OrdinalIgnoreCase))
            {
                entry.ThresholdAllocation = value;
            }
        }
    }

    private static void _assertNoUnknownKeys(
        IReadOnlyDictionary<string, object?> mapping,
        string context,
        IReadOnlySet<string> knownKeys)
    {
        foreach(var key in mapping.Keys)
        {
            if(!knownKeys.Contains(key))
            {
                var supported = string.Join(", ", knownKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
                throw new DomainException($"Unknown key '{key}' in {context}. Supported keys: {supported}.");
            }
        }
    }

    private static CompareConfigurationSection _parseCompareSection(IReadOnlyDictionary<string, object?> mapping)
    {
        _assertNoUnknownKeys(mapping, "the 'compare' configuration section", _compareKnownKeys);

        var section = new CompareConfigurationSection
        {
            Baseline = _getString(mapping, "baseline"),
            Target = _getString(mapping, "target"),
            Formats = _getStringList(mapping, "formats")
        };

        if(mapping.TryGetValue("thresholds", out var thresholdsNode) && thresholdsNode is IReadOnlyList<object?> thresholdsList)
        {
            var scopedEntries = new List<ScopedThresholdConfig>();

            foreach(var item in thresholdsList.OfType<IReadOnlyDictionary<string, object?>>())
            {
                _assertNoUnknownKeys(item, "a 'compare.thresholds' entry", _thresholdEntryKnownKeys);

                var pattern = _getString(item, "pattern");
                var mean = _getString(item, "thresholdMean");
                var allocation = _getString(item, "thresholdAllocation");

                if(pattern is null)
                {
                    // A thresholds entry with no pattern is the global rule for whichever metric(s) it sets.
                    // If more than one such entry sets the same metric, the last one in the file wins.
                    if(mean is not null)
                    {
                        section.ThresholdMean = mean;
                    }

                    if(allocation is not null)
                    {
                        section.ThresholdAllocation = allocation;
                    }

                    continue;
                }

                scopedEntries.Add(new ScopedThresholdConfig
                {
                    Pattern = pattern,
                    ThresholdMean = mean,
                    ThresholdAllocation = allocation
                });
            }

            section.Thresholds = scopedEntries;
        }

        return section;
    }

    private static string? _getString(IReadOnlyDictionary<string, object?> mapping, string key)
        => mapping.TryGetValue(key, out var value) ? value as string : null;

    private static List<string>? _getStringList(IReadOnlyDictionary<string, object?> mapping, string key)
    {
        if(!mapping.TryGetValue(key, out var value))
        {
            return null;
        }

        if(value is string scalar)
        {
            return [scalar];
        }

        if(value is IReadOnlyList<object?> list)
        {
            var result = new List<string>();
            foreach(var item in list)
            {
                if(item is string s)
                {
                    result.Add(s);
                }
            }

            return result.Count > 0 ? result : null;
        }

        return null;
    }

    private static PbReporterConfiguration _loadFromFile(string? explicitPath, string workingDirectory)
    {
        var isExplicit = explicitPath is not null;
        var path = explicitPath ?? _resolveDefaultConfigFilePath(workingDirectory);

        // path is only ever null here when isExplicit is false (no default file found), so the
        // exception below can never fire for a null path.
        if(path is null || !File.Exists(path))
        {
            if(isExplicit)
            {
                throw new DomainException($"The configuration file '{path}' was not found.");
            }

            return new PbReporterConfiguration();
        }

        var document = YamlDocumentParser.Parse(File.ReadAllText(path));
        return ParseYamlDocument(document);
    }

    private static string? _resolveDefaultConfigFilePath(string workingDirectory)
    {
        foreach(var fileName in _defaultConfigFileNames)
        {
            var candidate = Path.Combine(workingDirectory, fileName);
            if(File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static CompareConfigurationSection? _mergeCompareSections(CompareConfigurationSection? lower, CompareConfigurationSection? higher)
    {
        if(lower is null && higher is null)
        {
            return null;
        }

        return new CompareConfigurationSection
        {
            Baseline = higher?.Baseline ?? lower?.Baseline,
            Target = higher?.Target ?? lower?.Target,
            Formats = higher?.Formats ?? lower?.Formats,
            ThresholdMean = higher?.ThresholdMean ?? lower?.ThresholdMean,
            ThresholdAllocation = higher?.ThresholdAllocation ?? lower?.ThresholdAllocation,
            Thresholds = _mergeThresholds(lower?.Thresholds, higher?.Thresholds)
        };
    }

    private static List<ScopedThresholdConfig>? _mergeThresholds(List<ScopedThresholdConfig>? lower, List<ScopedThresholdConfig>? higher)
    {
        if((lower is null || lower.Count == 0) && (higher is null || higher.Count == 0))
        {
            return null;
        }

        var merged = new Dictionary<string, ScopedThresholdConfig>(StringComparer.OrdinalIgnoreCase);

        foreach(var entry in lower ?? [])
        {
            if(entry.Pattern is not null)
            {
                merged[entry.Pattern] = new ScopedThresholdConfig
                {
                    Pattern = entry.Pattern,
                    ThresholdMean = entry.ThresholdMean,
                    ThresholdAllocation = entry.ThresholdAllocation
                };
            }
        }

        foreach(var entry in higher ?? [])
        {
            if(entry.Pattern is null)
            {
                continue;
            }

            if(!merged.TryGetValue(entry.Pattern, out var existing))
            {
                existing = new ScopedThresholdConfig { Pattern = entry.Pattern };
                merged[entry.Pattern] = existing;
            }

            existing.ThresholdMean = entry.ThresholdMean ?? existing.ThresholdMean;
            existing.ThresholdAllocation = entry.ThresholdAllocation ?? existing.ThresholdAllocation;
        }

        return merged.Values.ToList();
    }

    private static IReadOnlyDictionary<string, string?> _readEnvironmentVariables()
    {
        var result = new Dictionary<string, string?>();

        foreach(DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if(entry.Key is string key)
            {
                result[key] = entry.Value as string;
            }
        }

        return result;
    }
}
