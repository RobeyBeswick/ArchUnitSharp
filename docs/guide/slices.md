---
title: Slices
---

# Slices — architecture by feature, not by layer

The slices module groups files into slices and asserts rules over the dependencies between them.
`DefinedBy("src/features/(**)/*.cs")` names each file's slice from the text a `(**)` capture matches;
`DefinedByRegex(pattern)` uses the first regular-expression capture instead. A file no definition
names is unsliced and outside every rule's scope, and a definition whose pattern contains no capture
is a `UserError` — a slice definition that cannot name a slice is a malformed pattern.

## Forbidden and required dependencies

`ShouldNot().ContainDependency(from, to)` counts a dependency from a *sliced* file matching `from` to
any file matching `to` (sliced or not, internal only) as a violation; `Should().ContainDependency`
reports each slice that contains none. The empty-test guard fires when the slicing selects no files,
`from` matches no sliced file, or `to` matches no file of the graph, so a typo in any of the three is
a failure in both moods.

```csharp
Project.Slices()
    .DefinedBy("src/features/(**)/*.cs")
    .ShouldNot()
    .ContainDependency("src/features/**", "src/legacy/**")
    .AssertPasses();
```

## Adhere to a PlantUML diagram

`Should().AdhereToDiagram(text)` (and `AdhereToDiagramInFile(path)`) checks the actual
slice-to-slice dependencies against a checked-in PlantUML component diagram: a dependency between two
slices the diagram has no arrow for is one violation per slice pair. A diagram arrow the code does
not realise is not a violation. The diagram is parsed when the rule is built, so a malformed
declaration or arrow is a `UserError` naming its line.

The modifiers `IgnoringOrphanSlices()` and `IgnoringExternalSlices()` narrow which dependencies the
diagram is held to, and affect only the adhere-to-diagram predicates. `adhere to diagram` exists only
in the positive mood, like the files module's `should have no cycles`.

```csharp
Project.Slices()
    .DefinedBy("src/features/(**)/*.cs")
    .Should()
    .IgnoringExternalSlices()
    .AdhereToDiagram("""
        @startuml
        component [auth]
        component [search]
        [auth] --> [search]
        @enduml
        """)
    .AssertPasses();
```

## PlantUML reports

`ToPlantUml()` renders the slicing's projected dependency graph as a PlantUML component diagram —
one `component [Name]` per slice and one arrow per dependency between slices — and
`ExportAsPlantUml(path)` writes it to a file. These are data terminals: an empty slicing renders a
valid empty document, not a violation.

Next: [Graph](graph.md).