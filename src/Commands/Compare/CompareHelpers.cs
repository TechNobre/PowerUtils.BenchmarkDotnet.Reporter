using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

public static class CompareHelpers
{
    public const string REPORT_FILE_ENDS = ".json";


    public static List<BenchmarkReport> ReadBenchmarkReports(string? path)
    {
        var benchmarks = new List<BenchmarkReport>();
        var seenFullNames = new HashSet<string?>(StringComparer.OrdinalIgnoreCase);

        foreach(var benchmark in ReadJsonBenchmarkReports(path).SelectMany(s => s.Benchmarks ?? []))
        {
            if(!seenFullNames.Add(benchmark.FullName))
            {
                continue;
            }
            benchmarks.Add(benchmark);
        }

        return benchmarks;
    }

    public static JsonBenchmarkReports[] ReadJsonBenchmarkReports(string? path)
    {
        var paths = GetJsonReport(path);

        var reports = new JsonBenchmarkReports[paths.Length];
        for(var i = 0; i < paths.Length; i++)
        {
            var content = File.ReadAllText(paths[i]);

            try
            {
                reports[i] = JsonSerializer.Deserialize<JsonBenchmarkReports>(content)
                    ?? throw new InvalidOperationException($"Failed to deserialize the {paths[i]} file");
            }
            catch (JsonException jsonException)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize the file '{paths[i]}'. {jsonException.Message}",
                    jsonException);
            }

            reports[i].FilePath = Path.GetFullPath(paths[i]);
            reports[i].FileName = Path.GetFileName(paths[i]);

            foreach(var benchmark in reports[i].Benchmarks ?? [])
            {
                benchmark.Header = new BenchmarkHeader
                {
                    FilePath = reports[i].FilePath,
                    FileName = reports[i].FileName,
                    Title = reports[i].Title,
                    HostEnvironmentInfo = reports[i].HostEnvironmentInfo
                };
            }
        }

        return reports;
    }

    public static string[] GetJsonReport(string? path)
    {
        if(string.IsNullOrWhiteSpace(path))
        {
            throw new FileNotFoundException("The provided path is null or empty");
        }

        if(Directory.Exists(path))
        {
            var files = Directory.GetFiles(
                path,
                $"*{REPORT_FILE_ENDS}",
                SearchOption.AllDirectories);

            if(files.Length == 0)
            {
                throw new FileNotFoundException($"No {REPORT_FILE_ENDS} files found in the provided directory", path);
            }

            return files;
        }

        if(File.Exists(path) && Path.GetExtension(path).Equals(REPORT_FILE_ENDS, StringComparison.OrdinalIgnoreCase))
        {
            return [path];
        }

        throw new FileNotFoundException($"The provided path '{path}' doesn't exist or is not a {REPORT_FILE_ENDS} file", path);
    }
}
