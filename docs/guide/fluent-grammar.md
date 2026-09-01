---
title: The fluent grammar
---

# The fluent grammar

Every rule is one chain: `ENTRY → SCOPE → MOOD → PREDICATE → OBJECT → TERMINAL`. Exactly one entry,
one mood, one predicate and one terminal; scopes and objects chainable.

| Stage | What it does | Examples |
|---|---|---|
| **Entry** | Choose the architectural vocabulary. | `project files` / `files`, `project layers` / `layers`, `project slices` / `slices`, `project graph` / `graph`, `project metrics` / `metrics` |
| **Scope** | Select the subjects; repeated scopes combine with AND. | `with name`, `in folder`, `in path`, `in file`, and the metrics module's `for classes matching` |
| **Mood** | Choose the expected direction. | `should` and `should not` on the files and slices surfaces; `may only depend on layers` and `may not depend on layers` on the layers surface; the metrics threshold predicates carry the mood themselves |
| **Predicate** | State the policy. | `should exist`, `should have name`, `should be in folder`, `should be in path`, `should depend on files`, `should depend on external modules`, `should adhere to`, `should have no cycles`, `should contain dependency`, `should adhere to diagram` |
| **Object** | Select the target of a relational rule. | a file selection with the scope's selectors, or an external-module selection with `matching` (repeats combine with OR) |
| **Terminal** | Execute or render. | `Check(CheckOptions?)` returns `IReadOnlyList<Violation>` — an empty list means the rule passed — or the report terminals `to ...()` / `export as ...(path)` render data |

## Two properties every rule inherits

**A rule that matches nothing is a violation, not a pass.** The empty-test guard reports an
`EmptyTestViolation` unless `CheckOptions.AllowEmptyTests` is set, so a typo in a glob is a failure,
never a silent pass.

**Builders are immutable.** A half-built chain can be stored in a variable and branched, and two
branches never see each other's selectors.

## Fixed word choice

The word choice is fixed: a `should equal` or `should be at most` is a defect. The six threshold
predicates are exactly `should be below`, `should be above`, `should be`,
`should be below or equal to`, `should be above or equal to`, and `should satisfy`.

## A worked example

```csharp
Project.ProjectFiles()
    .InPath("src/**")
    .Except("src/**/generated/**")
    .Should()
    .HaveNoCycles()
    .AssertPasses();
```

This reads as one sentence — *project files in path 'src/**' except in path 'src/**/generated/**'
should have no cycles* — and is checked in one call.

Next: [Files](files.md).