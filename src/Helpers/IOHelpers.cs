using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Helpers;

public static class IOHelpers
{
    public const string REPORT_FILE_ENDS = ".json";


    public delegate void FileWriter(string path, string content);
    public static void WriteFile(string path, string content)
    {
        path = Path.GetFullPath(path);

        var pathDir = Path.GetDirectoryName(path);
        ArgumentNullException.ThrowIfNull(pathDir);
        Directory.CreateDirectory(pathDir);

        File.WriteAllText(path, content);

        Console.Write($"{Environment.NewLine}File exported to: '{path}'");
    }

    public static List<BenchmarkResport> ReadBenchmarkReports(string? path)
    {
        var benchmarks = new List<BenchmarkResport>();
        foreach(var benchmark in ReadJsonBenchmarkResports(path).SelectMany(s => s.Benchmarks ?? []))
        {
            if(benchmarks.Any(b => b.FullName?.Equals(benchmark.FullName) == true))
            {
                continue;
            }
            benchmarks.Add(benchmark);
        }

        return benchmarks;
    }

    public static JsonBenchmarkResports[] ReadJsonBenchmarkResports(string? path)
    {
        var paths = GetJsonReport(path);

        var reports = new JsonBenchmarkResports[paths.Length];
        for(var i = 0; i < paths.Length; i++)
        {
            var content = File.ReadAllText(paths[i]);

            try
            {
                reports[i] = JsonSerializer.Deserialize<JsonBenchmarkResports>(content)
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

        if(File.Exists(path))
        {
            return [path];
        }

        throw new FileNotFoundException($"The provided path '{path}' doesn't exist or is not a {REPORT_FILE_ENDS} file", path);
    }
}
