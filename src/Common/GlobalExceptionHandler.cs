using System;
using System.CommandLine;
using System.CommandLine.Help;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class GlobalExceptionHandler
{
    public static Func<ParseResult, int> Wrap(Func<ParseResult, int> action)
        => parser =>
        {
            try
            {
                return action(parser);
            }
            catch(DomainException ex)
            {
                parser.InvocationConfiguration.Error.WriteLine($"Error: {ex.Message}");
                parser.InvocationConfiguration.Error.WriteLine();
                new HelpAction { MaxWidth = 160 }.Invoke(parser);
                return Constants.ExitCodes.ERROR;
            }
        };
}
