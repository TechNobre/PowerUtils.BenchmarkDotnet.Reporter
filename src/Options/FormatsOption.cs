using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace PowerUtils.BenchmarkDotnet.Reporter.Options;

public sealed class FormatsOption : Option<string[]>
{
    private static readonly HashSet<string> _allowedValues = new(StringComparer.OrdinalIgnoreCase) { "console", "markdown", "json", "hit-txt" };

    public FormatsOption()
        : base("--format", "-f")
    {
        Description = "Output format for the report.";
        DefaultValueFactory = _ => ["console"];

        Validators.Add(static result =>
        {
            var values = result.Tokens
                .Select(token => token.Value);
            foreach(var value in values)
            {
                if(!_allowedValues.Contains(value))
                {
                    result.AddError($"Invalid format '{value}'. Allowed values: {string.Join(", ", _allowedValues)}");
                }
            }
        });
    }
}
