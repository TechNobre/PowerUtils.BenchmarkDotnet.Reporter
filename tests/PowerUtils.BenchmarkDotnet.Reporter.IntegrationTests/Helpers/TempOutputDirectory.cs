using System;
using System.IO;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

public sealed class TempOutputDirectory : IDisposable
{
    public string Path { get; }

    public TempOutputDirectory()
        => Path = Directory.CreateTempSubdirectory("pbreporter-it-").FullName;

    public string CombinePath(string fileName)
        => System.IO.Path.Combine(Path, fileName);

    public void Dispose()
    {
        if(Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
