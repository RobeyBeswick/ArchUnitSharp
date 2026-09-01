---
title: Graph
---

# Graph — a report, not a rule

The graph module is a report surface: an immutable query over the dependency graph that filters,
collapses, aggregates and counts it into a `GraphSnapshot`, rendered in any of six output formats.

## Query options

The options narrow the scope and shape the labels. They combine freely and are order-independent,
except that collapse rules apply in the order they were added and the first rule that relabels a file
wins.

| Option | What it does |
|---|---|
| `FocusingOn(glob, depth)` | the scope narrows to the matching files plus every file within `depth` hops in either direction; depth zero selects exactly the matching files |
| `ReachableFrom(glob)` | the scope narrows to the matching files plus everything reachable from them by following dependency edges |
| `DependentsOf(glob)` | the scope narrows to the matching files plus everything that can reach them |
| `CollapsedToFolderDepth(n)` | each file relabels to its folder truncated to the first `n` path segments |
| `CollapsedByPattern(glob)` | the matching files relabel to one bucket labeled with the glob itself |
| `IncludingExternalDependencies()` | include edges whose target lies outside the project |
| `IncludingSelfDependencies()` | include the per-file self-edge every file carries |
| `Titled(title)` | the snapshot's title |
| `WithCheckOptions(options)` | the options the rule terminal honours when the scope matched nothing |

External dependencies are excluded by default, and self dependencies with them; both are explicit
opt-ins.

## Render the snapshot

Rendering is two steps: build the snapshot, then render it. `Build()` produces the snapshot every
renderer consumes — identical on every call, because the query is immutable. Each format has an
in-memory `To...()` form and an `ExportAs...(path)` file form that writes the text and returns the
path it wrote:

| Format | In memory | To file |
|---|---|---|
| DOT (Graphviz) | `ToDot()` | `ExportAsDot(path)` |
| Mermaid | `ToMermaid()` | `ExportAsMermaid(path)` |
| D2 | `ToD2()` | `ExportAsD2(path)` |
| CSV | `ToCsv()` | `ExportAsCsv(path)` |
| JSON | `ToJson()` | `ExportAsJson(path)` |
| HTML (self-contained, inline SVG) | `ToHtml()` | `ExportAsHtml(path)` |

```csharp
string mermaid = Project.Graph()
    .FocusingOn("src/Web/**", depth: 2)
    .CollapsedToFolderDepth(2)
    .Titled("Web and its neighbourhood")
    .ToMermaid();
```

## The rule terminal

`Check(CheckOptions?)` is the query's rule terminal: it reports one `EmptyTestViolation` when the
scope matched no files, unless the query's check options allow empty tests, and nothing otherwise.
`Build()` and the render forms are data terminals — an empty snapshot is visible data, not a
violation.

Next: [Metrics](metrics.md).