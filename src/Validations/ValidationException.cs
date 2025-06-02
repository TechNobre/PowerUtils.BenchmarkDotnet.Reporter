using System;
using System.Collections.Generic;

namespace PowerUtils.BenchmarkDotnet.Reporter.Validations;

public sealed class ValidationException(string message) : Exception(message)
{
    public ValidationException(List<string> messages)
        : this(string.Join(" | ", messages)) { }
}
