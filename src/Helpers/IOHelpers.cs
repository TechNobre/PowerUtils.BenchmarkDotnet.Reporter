using System;
using System.IO;
using System.Text.Json;
using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Helpers;

public static class IOHelpers
{
    public const string REPORT_FILE_ENDS = "full.json";


    public delegate void Printer(string message);
    public static void Print(string message)
        => Console.Write(message);


    public delegate void FileWriter(string path, string content);
    public static void WriteFile(string path, string content)
    {
        path = Path.GetFullPath(path);

        var pathDir = Path.GetDirectoryName(path);
        ArgumentNullException.ThrowIfNull(pathDir);
        Directory.CreateDirectory(pathDir);

        File.WriteAllText(path, content);

        Print($"{Environment.NewLine}File exported to: '{path}'");
    }

    public static BenchmarkFullJsonResport ReadFullJsonReport(string? path)
    {
        path = GetFullJsonReport(path);
        var content = File.ReadAllText(path);

        return JsonSerializer.Deserialize<BenchmarkFullJsonResport>(content)
            ?? throw new InvalidOperationException($"Failed to deserialize the {path} file.");
    }

    public static string GetFullJsonReport(string? path)
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

            if(files.Length > 1)
            {
                throw new FileNotFoundException($"Multiple {REPORT_FILE_ENDS} files found in the provided directory", path);
            }

            return files[0];
        }

        if(File.Exists(path) && path.EndsWith(REPORT_FILE_ENDS, StringComparison.InvariantCultureIgnoreCase))
        {
            return path;
        }

        throw new FileNotFoundException($"The provided path '{path}' doesn't exist or is not a {REPORT_FILE_ENDS} file", path);
    }
}
