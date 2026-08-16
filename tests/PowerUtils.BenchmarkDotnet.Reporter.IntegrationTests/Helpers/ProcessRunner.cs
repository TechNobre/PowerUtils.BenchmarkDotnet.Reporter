using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

public static class ProcessRunner
{
    public sealed record Result(int ExitCode, string StandardOutput, string StandardError);

    private static readonly string _toolDllPath = ResolveToolDllPath();

    public static async Task<Result> RunAsync(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(_toolDllPath);
        foreach(var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if(e.Data is not null)
            {
                stdOut.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if(e.Data is not null)
            {
                stdErr.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new Result(process.ExitCode, stdOut.ToString(), stdErr.ToString());
    }

    private static string ResolveToolDllPath()
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        var targetFramework = baseDirectory.Name;
        var configuration = baseDirectory.Parent!.Name;

        // AppContext.BaseDirectory = <repoRoot>/tests/<TestProject>/bin/<Configuration>/<TargetFramework>/
        var repoRoot = baseDirectory.Parent!.Parent!.Parent!.Parent!.Parent!;

        return Path.Combine(
            repoRoot.FullName,
            "src", "bin", configuration, targetFramework, "PBReporter.dll");
    }
}
