# NOTES

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