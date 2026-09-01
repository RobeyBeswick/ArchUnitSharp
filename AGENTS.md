# AGENTS.md — ArchUnitSharp conventions

ArchUnitSharp is an architecture-testing library for C# and .NET: a fluent, chainable rule DSL over a
graph of the project's files and their dependencies, in the spirit of ArchUnit. This document is the
authority on its architecture, layout, naming and grammar. Where anything else disagrees, this document
wins; where this document is silent, idiomatic C# wins.

## The two highest-value rules in this file

1. **Every terminal reaches the empty-test guard.** A rule that matches nothing is a violation
   (`EmptyTestViolation`) unless `CheckOptions.AllowEmptyTests` is set. This is the single most
   valuable defensive property in the library, and every terminal must implement it.
2. **Builders are immutable.** A half-built rule must be storable in a variable and branchable. A
   fluent method that mutates `this` or a shared field is the library's worst bug class — it compiles,
   passes single-branch tests, and corrupts sibling branches.

## Layout

One project per concern, all under a single solution (`ArchUnitSharp.sln`). Every project that ships
has a test project beside it in `tests/`.

| Project | Holds |
|---|---|
| `src/ArchUnitSharp` | The public surface and composition root: the entry points (`project files`, `layers`, …), `CheckOptions`, and the wiring that connects the domain modules. Nothing else imports this project. |
| `src/ArchUnitSharp.Common` | The kernel: `Edge`, `Graph`, `ImportKind`, `Pattern`/`Filter`, `RegexFactory`, `Violation`, `Checkable`, `TechnicalError`/`UserError`. Lives in the `Common.Extraction` namespace where issue 1 places it. |
| `src/ArchUnitSharp.Extraction` | Reads the C# source tree: project location, source enumeration, import parsing, internal/external classification, per-line ignore directives, self-edges, parallel-edge merging, the graph cache. |
| `src/ArchUnitSharp.Projection` | Pure graph work: projecting edges, nodes and cycles; the per-edge map functions; Tarjan and Johnson cycle detection. |
| `src/ArchUnitSharp.Testing` | Violation formatting, result shaping, and the framework-agnostic assert helper. |
| `src/ArchUnitSharp.Testing.Xunit` | The xUnit-native integration: `XunitAssert`, the `AssertPasses`/`AssertFails` rule extensions, and the import-time detection that makes the surface native under xUnit or silently falls back to the agnostic helper. Nothing else imports this project. |
| `src/ArchUnitSharp.Files` | The files domain module. |
| `src/ArchUnitSharp.Layers` | The layers domain module. |
| `src/ArchUnitSharp.Slices` | The slices domain module. |
| `src/ArchUnitSharp.Graph` | The graph-reports domain module. |
| `src/ArchUnitSharp.Metrics` | The metrics domain module. |
| `tests/…` | A test project beside each `src/` project it tests. |

Each domain module (`Files`, `Layers`, `Slices`, `Graph`, `Metrics`) has the same internal shape:
a public fluent surface and, beneath it, `Assertion` / `Projection` / `Calculation` / `Extraction`
sub-namespaces.

### Dependency rules

These are checked by analyzers where possible, and by review where not:

1. **No domain module imports another domain module.** A shared helper belongs in `Common`.
2. **`Extraction` is the only layer that reads the filesystem or the Roslyn workspace.**
   `Projection`, `Assertion` and `Calculation` are pure — no file I/O, no `DateTime.Now`, no
   environment access. (`Common` is pure by construction.)
3. **Nothing imports the root `ArchUnitSharp` project except the root project itself.** Its path is
   the module path, so no prefix rule can express this — a reviewer checks it by hand.
4. **`Common` imports nothing in the library.** It is the kernel.

### Purity

The pure layers must be pure in substance, not just in imports: no hidden file access, no global
singleton reached through a parameter, no dependence on dictionary iteration order. Output is stable
and sorted so reports are reproducible.

## Naming

Public surface is `PascalCase`. The naming table:

| What | Rule |
|---|---|
| Interfaces | `I` prefix (`ICheckable`), one unexported member so outsiders cannot implement them |
| Async methods | `Async` suffix, and only async methods carry it |
| Options bags | `…Options` suffix, passed as one instance (`Check(CheckOptions)`), never `params` |
| Error types | `TechnicalError`, `UserError`, both implementing `Exception`/`Error` |
| Violation kinds | `…Kind` or an enum of kinds, carrying data not prose |
| Test files | `*Tests.cs` beside the file they test |

## The fluent grammar

The chain is `ENTRY → SCOPE → MOOD → PREDICATE → OBJECT → TERMINAL`:

- Exactly one entry, one mood, one predicate, one terminal. Scopes and objects chainable.
- Entry points are noun phrases (`project files`). Scope verbs are prepositional (`with name`,
  `in folder`, `in path`, `in file`, `in module`). Modifiers are present participles, optional and
  order-independent. Predicates are bare infinitives so `should` + predicate reads as English.
- **Word choice is fixed. No synonyms, ever.** Mood is `Should` and `ShouldNot` and nothing else.
  The six threshold predicates are exactly `ShouldBeBelow`, `ShouldBeAbove`, `ShouldBe`,
  `ShouldBeBelowOrEqual`, `ShouldBeAboveOrEqual`, `ShouldSatisfy`. A `ShouldEqual`,
  `ShouldBeAtMost` or `ShouldBeLessThan` is a defect.
- Mood is a single boolean threaded into one shared assertion, not two forked code paths.
- **Acceptance test:** read the whole chain aloud. If it is not a sentence an architect who does not
  write C# would understand, the name is wrong.

### Builder immutability

- A fluent method returns a new instance; it never mutates `this`. For a `record`, that is `with`;
  for a `class`, a new instance constructed from the current state.
- Copy on receive and copy on return: a `List<T>` accepted from a caller is copied in; a `List<T>`
  stored as state is copied (`ToArray()`) before it leaves, or the getter is `IEnumerable<T>` over a
  copy. `IReadOnlyList<T>` backed by a mutable `List<T>` is a lie — the caller can cast it back.
- A variadic/`params` argument is the caller's array; copy it.
- Any builder with fields that are themselves collections must copy those collections, and clone the
  elements if they are mutable.

## Data-model invariants

- Identifiers normalised and stable: separators normalised, and project-relative **or** absolute
  throughout, never mixed.
- Every file gets a **self-edge**, so a file with no dependencies still appears as a node.
  Projections filter self-edges out by default; node projection depends on them.
- **Parallel edges are merged, import kinds unioned**, so downstream code can assume
  `(source, target)` is unique.
- Globs compile to a `Regex` in exactly **one** place; nothing downstream ever sees a glob.
- Violations carry **data, not prose** — the offending edge, node, cycle, value, threshold. Message
  construction belongs in `Testing`.
- **Zero matches is a violation, not a pass** — the empty-test guard above.

## Errors

A failing rule is a `Violation` in a returned list — never an exception, never a panic. `TechnicalError`
and `UserError` exist for failures that are not rule outcomes (a project that cannot be located, a
syntax error in a pattern). The public surface returns `IReadOnlyList<Violation>`; there is no separate
boolean result. An empty list means the rule passed.

## `Checkable` and `CheckOptions`

`ICheckable { Check(CheckOptions?) -> IReadOnlyList<Violation> }` is the seam the whole library hangs
from. Every terminal implements it; every consumer programs against it and nothing else.
`CheckOptions` carries `AllowEmptyTests`, `Logging` and `LogFile` (the check's log level and its
optional file output), `ClearCache`, and any C#-specific analysis toggles. Options are a bag with
defaults; `Check(null)` means defaults.

## Async

- Never `async void` except an event handler.
- Never return an unawaited `Task`: a method returning `Task` must have its work fully awaited before
  it returns. `Task.WhenAll`, not fire-and-forget.
- `Task.Run` only to move work off a thread; its result must be observed.

## Testing

- **xUnit** (`[Fact]`, `[Theory]`), stdlib `Assert` only. No third-party assertion library.
- **Name the mutation.** A test is a claim that some specific change to the implementation would make
  it fail. A test no mutation breaks is decoration.
- Both levels, every time: pure code (`Assertion`, `Projection`, `Calculation`) gets unit tests
  against hand-built fixture graphs; anything reaching the fluent API gets at least one integration
  test through the public surface.
- **Build the hazard, don't hope for it.** An immutability or aliasing test must construct two
  branches off one parent and assert neither sees the other's data. "The parent is unchanged" is the
  weak half.
- Tests are deterministic: no dictionary-iteration-order assertions, no `DateTime.Now`, no
  culture-sensitive formatting, no shared static fixtures mutated across tests.

## Doc comments

Every public member is documented with XML doc comments (`/// <summary>`, `/// <param>`, `/// <returns>`).
They must be **true**, not just well-formed: a doc that describes behaviour the code does not have is
a finding. Types that are safe for concurrent use say so. Immutability is the point of the builders,
so their docs state that sharing them is safe.

## Deviating

Issues are starting points, not specifications. Deviating is allowed; **deviating silently is not.**
If you diverge from an issue, from a sibling convention, or from this document, write a `WHY:` line in
`NOTES.md` at the repository root — one line, what you did instead and what forced it. A reviewer
blocks undocumented deviation. Do not weaken a check to get it passing; leave a failing check failing
and say so in `NOTES.md`.

## What is sanctioned

- `// nolint`-style suppression only when it names a specific diagnostic and carries a reason.
- `record` for value-semantic types; `class` for behaviour.
- Pattern matching over `is` + cast, and over stringly-typed dispatch.
- `IReadOnlyList<T>`/`IEnumerable<T>` returned over a copy.

## What is not sanctioned

- Weakening `.editorconfig` or analyzer severities to make the build pass.
- `#pragma warning disable` without naming the diagnostic and a reason.
- `dynamic` or reflection for what a sealed hierarchy or an interface can express.
- Adding a dependency the issue does not ask for.