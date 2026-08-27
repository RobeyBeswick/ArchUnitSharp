# NOTES

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