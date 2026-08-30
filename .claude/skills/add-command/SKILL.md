---
name: add-command
description: 'Scaffold a new CLI command for this tool following its vertical-slice architecture - one folder per command with Command, Handler, Options, Helpers, Validator, ServiceCollectionExtensions, and Models. Use when adding a new pbreporter subcommand, e.g. "add a new command", "create a stats command", "how do I add a CLI command", or when replicating the Compare command pattern for a new feature.'
---

# Add Command

The CLI tool is structured into vertical slices: each command resides in a dedicated directory under [`src/Commands/`](../../../src/Commands) containing its command definition, options, handler, models, and dependency injection (DI) registration.

Every command implements the [`ICommandModule`](../../../src/Common/ICommandModule.cs) interface. The CLI entry point in [`src/Program.cs`](../../../src/Program.cs) dynamically discovers all registered `ICommandModule` instances from the DI container, so adding a command requires only appending `.Add<Name>Command()` to the service collection chain.

For a concrete, end-to-end implementation, see [`src/Commands/Compare/`](../../../src/Commands/Compare/).

## When to Use

- Adding a new subcommand to the CLI (e.g., `pbreporter stats ...`, `pbreporter validate ...`).

## When Not to Use

- Modifying an existing command (e.g., adding an option or exporter to the `compare` command). Edit the existing command slice directly instead.

## Anatomy of a Command Slice

```
src/Commands/<Name>/
├── Models/                                – Input DTOs, domain models, and value objects [Optional]
├── <Name>Command.cs                       – ICommandModule implementation; defines CLI command structure and wires options [Mandatory]
├── <Name>Handler.cs                       – Business logic entry point; returns an exit code [Mandatory]
├── <Name>Helpers.cs                       – Static I/O or transformation utilities [Optional]
├── <Name>Options.cs                       – Option definitions (static fields), record model, and Parse factory [Mandatory]
├── <Name>ServiceCollectionExtensions.cs   – DI registration extension method Add<Name>Command(...) [Mandatory]
└── <Name>Validator.cs                     – Domain and business-rule validation [Optional]
```

> **Option definition convention**: Do not create a separate `Options/` directory with individual option classes. Define `public static readonly Option<T>` fields directly on the `<Name>Options` record inside `<Name>Options.cs`. When an option supports CLI, environment variable, or configuration file input, consult [`configuration-conventions`](../configuration-conventions/SKILL.md) for naming, precedence, and parsing rules.

## Step-by-Step Workflow

1. **Create the slice directory**: Create `src/Commands/<Name>/`.
2. **Define options**: Create `<Name>Options.cs` with static `Option<T>` instances and a `Parse(ParseResult, <Name>ConfigurationSection?)` factory. Refer to [`configuration-conventions`](../configuration-conventions/SKILL.md) for configuration-aware options.
3. **Define models**: If needed, create DTOs, result models, or value objects under `Models/`.
4. **Implement validation and helpers**: Create `<Name>Validator.cs` and/or `<Name>Helpers.cs` if the command requires domain validation or I/O operations.
5. **Implement the handler**: Create `<Name>Handler.cs` with the primary business logic returning an exit code from [`src/Common/Constants.cs`](../../../src/Common/Constants.cs).
6. **Build the command**: Create `<Name>Command.cs` implementing [`ICommandModule`](../../../src/Common/ICommandModule.cs) and wrapping execution with `GlobalExceptionHandler.Wrap(...)`.
7. **Register dependencies**: Create `<Name>ServiceCollectionExtensions.cs` to register the command module, handler, and supporting services.
8. **Wire into the application**: Append `.Add<Name>Command()` in [`src/Program.cs`](../../../src/Program.cs).
9. **Add tests**: Create unit and integration tests under [`tests/PowerUtils.BenchmarkDotnet.Reporter.Tests/`](../../../tests/PowerUtils.BenchmarkDotnet.Reporter.Tests/) and [`tests/PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests/`](../../../tests/PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests/).
10. **Update documentation**: Add command documentation in `docs/commands/<name>.md`, update [`docs/configuration.md`](../../../docs/configuration.md) if new config keys were introduced, and reference the feature in [`AGENTS.md`](../../../AGENTS.md) and [`README.md`](../../../README.md).

---

## Code Templates & Patterns

### 1. Command Definition (`<Name>Command.cs`)

```csharp
using System.CommandLine;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.<Name>;

public sealed class <Name>Command(<Name>Handler handler) : ICommandModule
{
    public Command Build()
    {
        var command = new Command("<name>", "Short command description.")
        {
            GlobalOptions.ConfigOption,
            <Name>Options.InputOption,
            <Name>Options.OutputOption
        };

        command.SetAction(GlobalExceptionHandler.Wrap(parser =>
        {
            var configPath = parser.GetValue(GlobalOptions.ConfigOption);
            var configuration = ConfigurationLoader.Load(configPath);
            var options = <Name>Options.Parse(parser, configuration.<Name>);

            return handler.Execute(options);
        }));

        return command;
    }
}
```

Key aspects:
- Injects `<Name>Handler` via primary constructor.
- Registers global and command-specific options in the `Command` collection initializer.
- Uses `GlobalExceptionHandler.Wrap` to standardize unhandled exception handling and user-facing errors.
- Loads configuration and merges it with CLI arguments in `<Name>Options.Parse`.

### 2. Options Model (`<Name>Options.cs`)

```csharp
using System.CommandLine;
using System.CommandLine.Parsing;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.<Name>;

public sealed record <Name>Options
{
    public static readonly Option<string> InputOption = new("--input", "-i")
    {
        Description = "Path to the input report file.",
        Required = false
    };

    public static readonly Option<string> OutputOption = new("--output", "-o")
    {
        Description = "Path to the output destination.",
        DefaultValueFactory = _ => "./output"
    };

    public string? Input { get; init; }
    public string Output { get; init; } = "./output";

    public static <Name>Options Parse(ParseResult parser, <Name>ConfigurationSection? config = null)
    {
        var input = parser.GetValue(InputOption)
            ?? config?.Input;

        var output = parser.GetValue(OutputOption)
            ?? config?.Output
            ?? "./output";

        return new <Name>Options
        {
            Input = input,
            Output = output
        };
    }
}
```

Key aspects:
- Houses `public static readonly Option<T>` fields and the parsed option properties in a single `sealed record`.
- Merges CLI values, configuration file settings, and default fallbacks following the precedence rules in [`configuration-conventions`](../configuration-conventions/SKILL.md).

### 3. Command Handler (`<Name>Handler.cs`)

```csharp
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.<Name>;

public sealed class <Name>Handler(
    I<Name>Validator validator,
    Func<string?, ReportData> loadReport)
{
    public int Execute(<Name>Options options)
    {
        validator.Validate(options);

        var data = loadReport(options.Input);

        // Process business logic...

        return Constants.ExitCodes.SUCCESS;
    }
}
```

Key aspects:
- Injects dependencies (validators, delegates, keyed services) via primary constructor.
- Returns an exit code constant from [`src/Common/Constants.cs`](../../../src/Common/Constants.cs) (`SUCCESS`, `WARNING`, `THRESHOLD_HIT`).

### 4. Dependency Registration (`<Name>ServiceCollectionExtensions.cs`)

```csharp
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.<Name>;

public static class <Name>ServiceCollectionExtensions
{
    public static IServiceCollection Add<Name>Command(this IServiceCollection services)
        => services
            .AddTransient<ICommandModule, <Name>Command>()
            .AddTransient<<Name>Handler>()
            .AddTransient<I<Name>Validator, <Name>Validator>()
            .AddTransient<Func<string?, ReportData>>(sp =>
                path => <Name>Helpers.LoadReport(path));
}
```

Key aspects:
- Self-registers the command module as `ICommandModule` for automatic discovery by `Program.cs`.
- Registers the command handler, validators, and functional delegates.

---

## Optional Patterns

### Stateless Helpers via Functional Delegates (`<Name>Helpers.cs`)

For pure operations (e.g., file loading, serialization), use a static utility class exposed to DI as a `Func<...>` delegate instead of introducing unnecessary interfaces:

```csharp
public static class <Name>Helpers
{
    public static ReportData LoadReport(string? path)
    {
        // I/O or parsing logic...
        return new ReportData();
    }
}
```

### Business Validation (`<Name>Validator.cs`)

Encapsulate domain and environment validation rules in a validator class:

```csharp
public interface I<Name>Validator
{
    void Validate(<Name>Options options);
}

public sealed class <Name>Validator : I<Name>Validator
{
    public void Validate(<Name>Options options)
    {
        if (string.IsNullOrWhiteSpace(options.Input))
        {
            throw new DomainException("Input report path must be provided via CLI, environment variable, or configuration.");
        }
    }
}
```

---

## Application Wiring Checklist

When adding a new command, verify the following integration points:

- **Program Entry Point**: Append `.Add<Name>Command()` in [`src/Program.cs`](../../../src/Program.cs). The `ICommandModule` loop will register the command automatically.
- **Configuration Schema**: Add `<Name>ConfigurationSection` to [`src/Common/Configuration/PbReporterConfiguration.cs`](../../../src/Common/Configuration/PbReporterConfiguration.cs) and update [`src/Common/Configuration/ConfigurationLoader.cs`](../../../src/Common/Configuration/ConfigurationLoader.cs) if the command supports YAML/environment variable configuration.
- **Exit Codes**: Reuse standard codes in [`src/Common/Constants.cs`](../../../src/Common/Constants.cs); only introduce new exit codes if there is a distinct outcome category.

---

## Testing Guidelines

Follow repository conventions outlined in [`AGENTS.md`](../../../AGENTS.md):

- **Unit Tests**: Place in [`tests/PowerUtils.BenchmarkDotnet.Reporter.Tests/`](../../../tests/PowerUtils.BenchmarkDotnet.Reporter.Tests/) under `Commands/<Name>/`.
- **Frameworks**: Use **xUnit** (`[Fact]`, `[Theory]`), **NSubstitute** for mocks, and **AwesomeAssertions** for assertions.
- **Naming**: Test classes named `<ClassUnderTest>Tests`; test methods named `When_<Scenario>_Should_<ExpectedBehavior>()` or `Given_<Condition>_Should_<ExpectedBehavior>()`.
- **Coverage**: Cover `<Name>Command`, `<Name>Handler`, `<Name>Options.Parse`, validators, helpers, and value objects.

---

## Common Pitfalls

| Pitfall | Solution |
|---|---|
| Creating an `Options/` folder with multiple option classes | Place `Option<T>` static fields directly inside `<Name>Options.cs`. |
| Manually instantiating commands in `Program.cs` | Register `ICommandModule, <Name>Command` in DI; `Program.cs` auto-discovers all modules. |
| Placing domain validation inside `Option.Validators` | Keep CLI option validators strictly for token/syntax validation; use `<Name>Validator` for domain rules. |
| Creating redundant interface/class pairs for stateless utilities | Use static methods exposed as `Func<...>` delegates in DI. |
| Inconsistent option naming across CLI, env vars, and YAML | Follow [`configuration-conventions`](../configuration-conventions/SKILL.md) naming and mapping conventions. |
