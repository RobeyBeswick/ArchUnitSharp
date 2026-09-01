---
title: Testing and configuration
---

# Testing and configuration

## Asserting a rule

A rule's terminal implements `ICheckable` and is checked with `Check(CheckOptions?)`, which returns
`IReadOnlyList<Violation>` — an empty list means the rule passed. Asserting that outcome is the
testing module's job.

**Under xUnit**, reference `ArchUnitSharp.Testing.Xunit` and end the chain with `.AssertPasses()` or
`.AssertFails()`:

```csharp
using ArchUnitSharp;
using ArchUnitSharp.Testing.Xunit;
using Xunit;

[Fact]
public void Web_must_not_depend_on_Persistence()
{
    Project.ProjectFiles()
        .InFolder("src/Web")
        .ShouldNot()
        .DependOn()
        .InFolder("src/Persistence")
        .AssertPasses();
}
```

The adapter is native under xUnit (an import-time detector makes the surface xUnit-native when a live
xUnit run loads) and silently falls back to the framework-agnostic helper otherwise, so NUnit and
MSTest are covered by the same call.

**Under any other framework**, or with no adapter, use the framework-agnostic helper
`RuleAssert.Passes(rule, options?)` / `RuleAssert.Fails(rule, options?)`. It works with every
framework and needs no configuration — a passing rule returns normally, a failing one raises
`AssertionFailedException`, which every test framework reports as a failed test.

```csharp
using ArchUnitSharp.Testing;

RuleAssert.Passes(
    Project.ProjectFiles().InFolder("src/Web").ShouldNot().DependOn().InFolder("src/Persistence"),
    new CheckOptions());
```

## CheckOptions

`CheckOptions` is the single options bag passed to `Check`; `null` means the defaults. Every
property defaults to the least surprising value for a rule run.

| Property | Default | What it controls |
|---|---|---|
| `AllowEmptyTests` | `false` | whether a rule that matches nothing is allowed to pass; when `false` it is an `EmptyTestViolation` |
| `Logging` | `None` | how much a check logs while it runs; levels are `None`, `Debug`, `Info`, `Warn`, `Error` |
| `LogFile` | `null` | the optional file a check's log is written to, so a CI run can archive the log as a build artifact |
| `ClearCache` | `false` | whether the extraction cache is bypassed so the graph is rebuilt from source |
| `IgnoreTestCode` | `false` | whether files in test folders are excluded from the analysis |
| `IgnoreGeneratedCode` | `false` | whether generated source files (such as `*.g.cs` and `*.designer.cs`) are excluded |

```csharp
var options = new CheckOptions
{
    AllowEmptyTests = true,
    Logging = LoggingLevel.Info,
    LogFile = new LogFileOptions { Directory = "logs" },
};
RuleAssert.Passes(rule, options);
```

The empty-test guard is the most valuable defensive property in the library: a rule that matches
nothing is a violation, not a pass, so a typo in a glob is a failure — never a silent pass. Opt out
only when an empty result is genuinely valid.

## Errors

A failing rule is a `Violation` in a returned list — never an exception, never a panic. Failures
that are not rule outcomes use the two error types from the kernel: `TechnicalError` for a project
that cannot be located or a file that cannot be read or written, and `UserError` for a malformed
pattern or diagram. Both propagate unchanged through the assertion helpers.

Next: [Limitations](limitations.md).