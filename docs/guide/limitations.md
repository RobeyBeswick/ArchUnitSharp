---
title: Limitations
---

# What is not implemented yet

- **No NuGet package is published.** Install by referencing the projects from source, as described
  in [Getting started](getting-started.md).
- **Some predicates are positive-mood only.** `should have no cycles` (files) and `should adhere to
  diagram` (slices) have no `should not` form.
- **No `functions` count metric.** C# has no file-level function distinct from a type member, so the
  metric the siblings carry is skipped rather than faked.
- **`except` is limited.** It narrows the files and metrics selectors and the depend-on objects;
  layer definitions, slice definitions and graph report options do not take it.
- **The graph is an import graph.** Dependencies are the project's `using` directives resolved
  through Roslyn, not every type reference inside a method body.

## Contributing

This repository is built from scratch, issue by issue, by the
[ArchUnitDev](https://github.com/RobeyBeswick/ArchUnitDev) loop, and the issue queue is the roadmap.
The conventions it builds against are in
[AGENTS.md](https://github.com/RobeyBeswick/ArchUnitSharp/blob/main/AGENTS.md), and deviations from
them are recorded in
[NOTES.md](https://github.com/RobeyBeswick/ArchUnitSharp/blob/main/NOTES.md).