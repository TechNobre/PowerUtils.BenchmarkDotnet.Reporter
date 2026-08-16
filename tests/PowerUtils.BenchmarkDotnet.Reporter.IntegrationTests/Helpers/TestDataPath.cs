using System;
using System.IO;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

public static class TestDataPath
{
    public static string Resolve(string relativePath)
        => Path.Combine(AppContext.BaseDirectory, "test-data", relativePath);
}
