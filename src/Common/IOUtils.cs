using System;
using System.IO;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class IOUtils
{
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
}
