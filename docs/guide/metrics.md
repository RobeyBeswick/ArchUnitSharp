---
title: Metrics
---

# Metrics — thresholds, not moods

A metric rule reads each selected file's source to measure it and asserts every subject's value
against a threshold. The scope is the same file selection as the files module (`WithName`,
`InFolder`, `InPath`, each with `except`), plus `ForClassesMatching(glob)`, which keeps the files
that declare at least one class whose fully qualified name matches. A class-level metric's subjects
are the matching classes; a file-level metric's subjects are the files that contain one, measured
whole.

```csharp
Project.Metrics()
    .InFolder("src")
    .Count()
    .MethodCount()
    .ShouldBeBelow(20)
    .AssertPasses();
```

## The sections

`Metrics` hands the scope to one of four sections:

| Section | What it measures |
|---|---|
| `Count()` | the count metrics: `MethodCount`, `FieldCount` (per class), `LinesOfCode`, `Statements`, `Imports`, `Classes`, `Interfaces` (per file). There is no `functions` metric — C# has no file-level function distinct from a type member. |
| `Lcom()` | the cohesion family: `Lcom96a`, `Lcom96b`, `Lcom1` … `Lcom5`, `LcomStar`, the LCOM formulas as the sibling libraries define them. |
| `Distance()` | Robert C. Martin's dependency-derived set: `Abstractness`, `Instability`, `DistanceFromMainSequence`, `CouplingFactor`, `NormalisedDistance`, plus the zone guards `NotInZoneOfPain()` and `NotInZoneOfUselessness()`. |
| `CustomMetric(name, description, calculation)` | a rule over a caller-named metric whose `int` value a `Func<ClassInfo, int>` computes from one class's full information. |

## The six threshold verbs

The threshold vocabulary is fixed — exactly `ShouldBeBelow`, `ShouldBeAbove`, `ShouldBe`,
`ShouldBeBelowOrEqual`, `ShouldBeAboveOrEqual` and `ShouldSatisfy` — on every selection (count
selections take an `int` threshold, LCOM and distance selections a `double`). There is no `Should` /
`ShouldNot` mood: a comparison's negation is another comparison, not a separate rule shape.

```csharp
Project.Metrics()
    .InFolder("src")
    .Distance()
    .Instability()
    .ShouldBeBelow(0.8)
    .AssertPasses();
```

## HTML reports

Each section also exports its measurements as a self-contained HTML page — `Count().ExportAsHtml(path)`,
`Lcom().ExportAsHtml(path)`, `Distance().ExportAsHtml(path)` — with the title, timestamp and
stylesheet coming from `MetricsExportOptions`. A report is a data form, not a rule, so an empty scope
exports an explicit *No metric data.* page rather than a violation.

Next: [Testing and configuration](testing.md).