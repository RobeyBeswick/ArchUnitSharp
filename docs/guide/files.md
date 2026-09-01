---
title: Files
---

# Files — what your files may and may not do

The files module is the rule surface over a scoped set of files: cycles, naming, location, internal
and external dependencies, and custom predicates over source text.

## The selectors

The scope is the subject of the rule. The four selectors match a different part of a file's
project-relative identifier — `src/Models/Car.cs` in each example:

| Selector | Matches | Example |
|---|---|---|
| `WithName(glob)` | the file's name, no directory part | `Car.cs` |
| `InFolder(glob)` | the file's identifier with its name removed | `src/Models` |
| `InPath(glob)` | the whole path, folders and name | `src/Models/Car.cs` |
| `InFile(glob)` | the file name with its extension stripped and every separator turned into a dot — the name the file would carry as a class | `src.Models.Car` |

Selectors combine with AND. Each selector's `except` companion (`Except("glob")` or
`Except(Filter)`) narrows that one selector:

```csharp
Project.ProjectFiles()
    .InPath("src/**")
    .Except("src/**/generated/**")
    .Should()
    .HaveNoCycles()
    .AssertPasses();
```

## The moods and predicates

`Should()` begins a positive rule, `ShouldNot()` a negated one. Both thread a single boolean into the
shared assertion.

### should exist

The selected files must exist. Because the selection is drawn from the graph's own nodes, every
selected file exists, so the rule passes for a non-empty selection and the empty-test guard reports a
selection that matched nothing.

### should have name, should be in folder, should be in path

Every selected file must match the predicate's glob against the corresponding part of its identifier:

```csharp
Project.ProjectFiles()
    .InFolder("src/Models")
    .Should()
    .HaveName("*.cs")
    .AssertPasses();
```

### should (not) depend on files

Every selected file must (or must not) depend on at least one file matching every selector applied
to the returned object. The object is a file selection with the same four selectors:

```csharp
Project.ProjectFiles()
    .InFolder("src/Web")
    .ShouldNot()
    .DependOn()
    .InFolder("src/Persistence")
    .AssertPasses();
```

The positive mood reports one violation per selected file that depends on none of the object's
files; the negated mood reports one violation per offending dependency edge. The empty-test guard
fires when the selection *or* the object matched nothing.

### should (not) depend on external modules

The same shape over the targets of external edges — names no file in the project declares, kept as
written (`System.Linq` for `using System.Linq;`). The object's `Matching(glob)` selector matches the
module name; repeats combine with OR, so the rule reads "must not depend on any of":

```csharp
Project.ProjectFiles()
    .InFolder("src/domain")
    .ShouldNot()
    .DependOnExternalModules()
    .Matching("Newtonsoft.Json")
    .AssertPasses();
```

### should adhere to

A custom predicate over each selected file's full source text. The predicate receives one
`FileDetail` — the project-relative path, name without extension, extension, directory, full source
text and non-blank line count — and must return `true` for the file to pass:

```csharp
Project.ProjectFiles()
    .InFolder("src/services")
    .Should()
    .AdhereTo(file => file.NonBlankLineCount < 300, "services must stay below 300 non-blank lines")
    .AssertPasses();
```

### should have no cycles

The projected dependency graph of the selected files must be acyclic; each cycle is one violation.
A cycle is reported only when every file it passes through is selected. This predicate exists only
in the positive mood.

```csharp
Project.ProjectFiles()
    .InFolder("src")
    .Should()
    .HaveNoCycles()
    .AssertPasses();
```

Next: [Layers](layers.md).