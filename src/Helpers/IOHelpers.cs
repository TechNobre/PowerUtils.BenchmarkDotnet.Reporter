using System;
using System.IO;
using System.Text.Json;
using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Helpers;

public static class IOHelpers
{
    public const string REPORT_FILE_ENDS = "-report-full.json";


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

    public static BenchmarkFullJsonResport[] ReadFullJsonReport(string? path)
    {
        var paths = GetFullJsonReport(path);

        var reports = new BenchmarkFullJsonResport[paths.Length];
        for(var i = 0; i < paths.Length; i++)
        {
            var content = File.ReadAllText(paths[i]);

            try
            {
                reports[i] = JsonSerializer.Deserialize<BenchmarkFullJsonResport>(content)
                    ?? throw new InvalidOperationException($"Failed to deserialize the {paths[i]} file.");
            }
            catch (JsonException jsonException)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize the file '{paths[i]}'. {jsonException.Message}",
                    jsonException);
            }

            reports[i].FilePath = Path.GetFullPath(paths[i]);
            reports[i].FileName = Path.GetFileName(paths[i]);
        }

        return reports;
    }

    public static string[] GetFullJsonReport(string? path)
    {
        if(string.IsNullOrWhiteSpace(path))
        {
            throw new FileNotFoundException("The provided path is null or empty.");
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

        if(File.Exists(path) && path.EndsWith(REPORT_FILE_ENDS, StringComparison.InvariantCultureIgnoreCase))
        {
            return [path];
        }

        throw new FileNotFoundException($"The provided path '{path}' doesn't exist or is not a {REPORT_FILE_ENDS} file", path);
    }
}
