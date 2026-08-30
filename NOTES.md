# NOTES

WHY: Issue 27 — the layers module's rule vocabulary is `may only depend on layers(...)` /
`may not depend on layers(...)`, not the files module's `should` / `should not`: the issue names these
verbs verbatim and they read as a sentence ("layer App may only depend on layers Models, Services")
that a plain should/should-not mood cannot reach. The single-boolean rule is still honoured — one
`negate` flag threads through `LayersAssertion.CheckConstraint`, blocklist true, allowlist false —
only the words differ.

WHY: Issue 27 — `defined by` binds a whole-path glob (`MatchTarget.Path`) and `defined by folder`
binds a folder glob (`MatchTarget.PathWithoutFilename`), the same vocabulary the files module's four
selectors use; the issue does not define the match semantics, and this reading reuses the kernel's
matching rather than inventing a second one (as issue 16 did for the files selectors).

WHY: Issue 27 — "blocklist rules are evaluated before allowlist rules" lands as: blocklist constraints
are checked first, and a cross-layer dependency a blocklist already reported on a subject layer is not
re-reported by an allowlist on the same subject; without that dedup a dependency that is both blocked
and outside an allowlist would be reported twice.

WHY: Issue 27 — a layer name that is undeclared, or declared but whose glob matches no file, selects
no files, so the subject-empty and all-targets-empty guards treat both as an empty test rather than a
`UserError`: a typo in either direction is then a failure, not a silent pass (blocklist) or a blanket
failure (allowlist). `may not depend on layers()` with no arguments blocks nothing and passes
trivially; only `may only depend on layers()` is a sealed layer, as the issue specifies.

WHY: Issue 27 — the policy object `ArchUnitSharp.Layers.Layers` is itself the checkable terminal (it
accumulates declarations and rules and is checked as a whole), rather than the files module's one
terminal per rule, because the value of the module is expressing an N-layer policy and checking it in
one `Check()` call; a policy with no rules passes.

WHY: Issue 26 — the native xUnit integration ships as a new project `ArchUnitSharp.Testing.Xunit`
(`XunitAssert` + the `AssertPasses`/`AssertFails` extensions), not inside `ArchUnitSharp.Testing`,
because the agnostic module must stay free of any framework dependency and issue 25's shadowing
rationale bars a type named `Assert`; the adapter holds no rule logic — it calls `Check`, shapes via
`ResultFactory`, and asserts through xUnit's own `Assert.True`/`Assert.False` (the negation idiom) —
and a `[ModuleInitializer]` runs at import to silently detect a live xUnit run (the `xunit.runner.*` /
`xunit.execution.*` assemblies load only when xUnit actually executes tests), falling back to the
agnostic `RuleAssert` otherwise, which is what "NUnit and MSTest covered by the agnostic path" means.
That fallback is why the agnostic helper gains a `Fails` twin of `Passes`: without it the adapter's
negation would have to fabricate prose in the adapter, and the agnostic path could not cover "assert
the rule fails" for NUnit/MSTest as cleanly as the native path does.

WHY: Issue 25 — the framework-agnostic assert helper ships as `RuleAssert.Passes(rule, options?)`
throwing `AssertionFailedException`, not as a type named `Assert`: a public `Assert` in the
`ArchUnitSharp.Testing` namespace would shadow every test framework's own `Assert` type for the
consumer (an enclosing-namespace member beats a using-directive import — verified empirically, so
`using Xunit;` + `using ArchUnitSharp.Testing;` in one file would make the consumer's own
`Assert.True(...)` resolve to the wrong type), breaking the issue's core "works with every test
framework, needs no configuration" promise; the issue's verb `passes` and the optional `options?`
argument are kept verbatim, and the helper is documented in its own doc comments as the documented
fallback.

WHY: Issue 24 — the Testing project lands as the single message-formatting layer (`ViolationFactory`,
`ResultFactory`, `CheckResult`, `Colour`/`Colouriser`), and the pre-existing prose conveniences on
violations (`CycleViolation.Path`, `EmptyTestViolation.RuleDescription`) are kept as the data the
factory renders from rather than removed, because each is public API with tests and the rule
description is the empty-test violation's only datum; the "all message formatting lives in Testing"
rule is satisfied by the factories being the only producers of report messages.

WHY: Issue 23 — the empty-test guard lands as a shared kernel primitive (`EmptyTestGuard` in
`Common.Extraction`) that every terminal routes its empty selection through, because the only
terminals in the library today are the files ones and each already guarded inline; the issue's "on
every terminal, not just the files ones" is made structural by centralising the consequence (default →
`EmptyTestViolation`, opt-out via `CheckOptions.AllowEmptyTests`) in Common, so a future Layers/Slices/
Graph/Metrics terminal reaches the same guard by construction and cannot hand-roll a weaker one. Each
rule still decides what "matched nothing" means for itself (subject alone, or subject-or-object for
the depend-on predicates); only the consequence is shared.

WHY: Issue 22 — `adhere to(fn, message)` hands the custom predicate a type named `FileDetail`, not the
`FileInfo` the issue names, because `System.IO.FileInfo` sits in every consumer's implicit global usings
and a public type of that name would make the predicate's parameter ambiguous for anyone using both
namespaces; `FileDetail` carries exactly the six fields the issue lists.

WHY: Issue 22 — the adhere-to assertion reads each selected file's source text through a provider the
composition root wires onto the `Files` selection (the entry points read from the located project's
root, lazily, per selected file and per check), because the graph stores only identifiers and the issue
requires full source text; eager content materialisation at the entry point would re-read every file on
every call and defeat the graph cache. The provider is the module's only disk boundary and is injected,
so the pure projections and the shared assertion never touch the filesystem themselves, and a selection
built from a bare graph raises a `UserError` instead of fabricating empty text.

WHY: Issue 21 — `should (not) depend on external modules` is a new object type
`DependOnExternalModules` with a `Matching` selector: a glob matched against the external edge target,
which the resolver keeps as the module name as written (e.g. `System.Linq`). Repeats combine with OR,
not the AND of the files-object's selectors, because the issue says the repeat is for OR and "depend on
at least one of" is the third-party-policy reading. Both moods exist through the shared assertion's
negate flag, and the empty-test guard fires when the selection or the object matched nothing — the
object being the set of external module names in the graph matching any selector, so a typo in a
`Matching` glob is an empty test in both moods rather than a silent pass (negated) or a blanket failure
(positive).

WHY: Issue 20 — the depend-on object exposes the same four selectors as the scope (`with name`,
`in folder`, `in path`, `in file`), not just the three the issue enumerates, because the object is a
file selection and the scope's selector vocabulary is the module's fixed set; a selector available on
the scope but not on the object would make the object a different kind of selection for no gain.

WHY: Issue 20 — the two moods of `depend on files` report at different granularities: the positive
mood reports one `FileViolation` per selected file that depends on none of the object's files (the
subject is the datum — a missing dependency has no offending target), while the negated mood reports
one `DependencyViolation` per offending dependency edge (each offending target is a datum a report
should name). This is ArchUnitJava's depend-on semantics. And the empty-test guard fires when the
selection *or* the object matched nothing, so a typo in an object glob is an empty test in both moods
rather than a silent pass (negated) or a blanket failure (positive).

WHY: Issue 18 — `CycleViolation` carries the readable `Path` ("src/A.cs → src/B.cs → src/A.cs")
alongside its data (`Files`), prose on a violation that AGENTS.md says should carry data only, because
there is no Testing project yet to host message construction — the same exception that puts
`EmptyTestViolation.RuleDescription` in this module.

WHY: Issue 18 — `should have no cycles` is exposed only in the positive mood (the issue says so), so
the shared `FilesAssertion.Cycles` threads no mood flag; the "single boolean threaded into one shared
assertion" rule exists to stop forked code paths when both moods exist, and with one mood there is
nothing to fork.

WHY: Issue 18 — cycle detection runs on the subgraph the selection induces (an edge is considered
only when both its endpoints are selected), so a cycle is reported only when every file it passes
through is in the selection; the issue does not define how selectors scope cycle detection, and this
reading keeps a rule over a set of files scoped to exactly that set.

WHY: Issue 17 — the files rule terminal implements `ICheckable` from the Files assembly, but the
interface's unexported guard member is `internal` to Common, so Common now grants
`InternalsVisibleTo` to `ArchUnitSharp.Files`; without it the guard ("outsiders cannot implement the
seam") would also bar the library's own modules. And the `EmptyTestViolation`'s `RuleDescription` is
built in the Files module — the assertion renders the scope from the selectors and appends the mood —
because there is no Testing project yet to host message construction.

WHY: Issue 16 — the entry points (`project files`, `files`) live in a new root project
`src/ArchUnitSharp` (the composition root), not in the Files module, because an entry must locate the
project and drive the graph cache — the extraction wiring a pure domain module must not import. The
Files module's public surface is therefore the `ArchUnitSharp.Files.Files` builder over a graph, and
the root's return type is spelled `ArchUnitSharp.Files.Files` in full: from inside the `ArchUnitSharp`
namespace the namespace `ArchUnitSharp.Files` shadows the type name `Files` (CS0118), and a using-alias
named `Files` conflicts with the namespace member too (CS0576).

WHY: Issue 16 — the four selectors map one-to-one onto the kernel's four `MatchTarget` values, which is
what gives each a distinct meaning: `with name`→`Filename`, `in folder`→`PathWithoutFilename`,
`in path`→`Path`, `in file`→`Classname` (the identifier with its extension stripped and separators
turned into dots, e.g. `src/Models/Car.cs` → `src.Models.Car`). The issue does not define the
selectors' match semantics; this reading reuses the kernel's matching vocabulary without inventing a
second one.

WHY: Issue 13 — projections filter relabelled self-loops (a projected edge whose source and target are
the same label) out of both the edge set and the cycle set, with no keep-self-loops option. AGENTS.md
says "projections filter self-edges out by default"; the option is omitted because no module needs it
yet, and filtering means the cycle set reports only loops between distinct labels, which is what the
layers/slices "free of cycles" checks will want (an intra-layer dependency collapses to a self-loop
and must not be flagged). `ProjectedCycle` consequently requires at least two hops.

WHY: Issue 12 — the per-line ignore directive renders ArchUnitPython's `# archunit: ignore` as the C#
line-comment idiom `// archunit: ignore`, and "named modules" maps to the *referenced namespace/type*
written on the `using` line (the graph has files, not modules), scoped by exact-or-prefix match as
ArchUnitPython scopes module names. The parenthetical-reason clause `ignore(...)` in ArchUnitPython's
regex is dropped: it is undocumented there and adds a fragile match form for no surface value.

WHY: Issue 11 — the graph cache is keyed on the locator's *output* (ProjectLocation), not the
locator's start input, because the located project fully determines the graph, re-locating is cheap,
and the same start directory can locate different projects over time; keying on the start would
duplicate entries and go stale when a .sln/.csproj appears above the start. The analysis toggles
(IgnoreTestCode/IgnoreGeneratedCode) are consumed by the pipeline via SourceFileFilter, so the key's
toggle components are honest — CheckOptions already documented those behaviours.

WHY: Round 4 — test critic (9-tests-1a) again returned no verdict (crash or timeout) with no code
findings. The diff is 2 files / 47 lines — far too small for "too large to review in one pass". Re-ran
the full gate clean: restore, build (0 warnings, 0 errors), format --verify-no-changes, 115 extraction
tests + 111 common tests passed (nothing removed, nothing skipped), no vulnerable packages. This is the
reviewer-tooling hang class documented in Rounds 2 and 3, not a code defect; there is nothing to fix.

WHY: Issue 9 — internal/external classification. The resolver already sets Edge.External and keeps the
raw module name as the target for external edges, because that is inseparable from Issue 8's "resolve
directives to targets" (an unresolved directive had to become an external edge or no edges would be
produced). This issue therefore lands as targeted test coverage locking in the classification: external
edges keep the written module name for alias and global-using directives, and a project-declared parent
namespace (e.g. `using MyApp;` where only `MyApp.Models` is declared) classifies as internal to the
declaring file rather than external. No production change was required; the behaviour was already
correct and verified.

WHY: Issue 8 — conditional compilation. The correctness review claimed UsingDirectiveReader.Collect's
DescendantNodes() walk gathers usings from inactive #if regions; it does not — Roslyn parses inactive
text into disabled trivia, not syntax nodes, so such usings are already excluded from the edge set.
The underlying point stands: the resolver's preprocessor-symbol choice was implicit. It is now
deliberate and documented in ImportResolver — the resolver is the pure half and cannot see the
project's real DefineConstants, so it parses with CSharpParseOptions.Default (no symbols): directives
under a false #if condition are skipped, under a true one kept. That is a documented
over-approximation against any single real build configuration rather than a silent accident.

WHY: Issue 7 — the symlink hazard tests in SourceEnumeratorTests early-return when
`TempProject.TryCreateDirectoryLink` reports the platform refused to create a directory symlink
(Windows without the privilege or developer mode). The tests fully assert on macOS/Linux; on a
Windows host they become no-ops rather than failures, because the hazard they build cannot exist
there without admin rights.

WHY: Issue 7 — file symlinks are excluded from enumeration even when their target lies inside the
project root, rather than being resolved and included. Resolving them would need a target-inside-root
probe per file, duplicate sources via junction loops, and complicate the identifier contract; the
established never-follow posture is applied to files as well as directories and is documented in the
SourceEnumerator remarks.

WHY: Issue 7 — SourceEnumerator walks with a manually-constructed `EnumerationOptions`
(`IgnoreInaccessible = false`), so an unreadable subdirectory surfaces as a `TechnicalError` instead
of being silently skipped. The default overloads ignore inaccessible entries; failing loudly was
chosen so a partial tree cannot silently produce a partial graph.

WHY: Round 2 — idiom critic (6-idiom-1) and test critics (6-tests-1a, 6-tests-1b) again returned no
verdict (crash or timeout). The diff is 6 files / 244 lines, so "too large to review in one pass"
does not apply. Found a likely root cause for the crashes: a stuck VBCSCompiler server process had
`dotnet format` and `dotnet test` hanging for 5+ minutes (the same hang the reviewers' tooling would
hit); killing it lets the full gate complete. Full gate verified clean: restore, build (0 warnings,
0 errors), format --verify-no-changes, 111 tests passed, no vulnerable packages. Nothing to fix in
code; the previous round's NOTE about the idiom critic (5-idiom-1) is subsumed by this one.

WHY: Round 3 — test critic (8-tests-2) returned no verdict (crash or timeout) with no code findings.
Re-ran the full gate on the current diff (11 files / 936 lines, not too large to review in one pass):
restore, build (0 warnings, 0 errors), format --verify-no-changes, 112 extraction tests + 111 common
tests passed (test-attribute count 166 at base -> 213 now, nothing removed, nothing skipped), no
vulnerable packages. The crash is the reviewer-tooling hang class already noted in Round 2, not a
code defect; there is nothing to fix.