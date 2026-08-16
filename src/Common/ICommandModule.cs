using System.CommandLine;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public interface ICommandModule
{
    Command Build();
}
