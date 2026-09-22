# Quality, compatibility, and a new-language roadmap

A compiler test strategy should follow observable contracts from the smallest
phase to the complete host:

| Layer | What to prove |
| --- | --- |
| lexer/parser | tokens, trees, spans, recovery, and diagnostics |
| binder/runtime | name, scope, conversion, operator, and control-flow semantics |
| code generation | executable behavior, metadata, PDBs, and failure cleanup |
| SDK | source ordering, incremental inputs, references, publish assets, and file-based isolation |
| hosts | scripting streams, playground diagnostics, input/output, and runtime failures |
| compatibility | behavior shared with the reference engine and documented intentional differences |

`tests/Lolcode.CodeAnalysis.Tests` covers compiler/runtime units and emission
contracts. `tests/Lolcode.EndToEnd.Tests` compiles and runs programs, exercises
SDK samples, and owns the compatibility fixture architecture.
`tests/Lolcode.Web.Tests` covers the browser host. Prefer end-to-end assertions
on stdout, diagnostics, assemblies, or deployed assets over private IL shape.

## Compatibility corpus: what the counts mean

The repository-owned fixture tree currently records:

- **189 Shared registrations** run against dotnet-lolcode and, when configured,
  the pinned lci engine. This includes 188 conventional `test.lol` fixtures
  plus one custom-source registration.
- **41 KnownLciDivergence fixtures** run against the target compiler only.
  Each carries fixture-local README evidence explaining the pinned-lci parser,
  semantic, extension, or test-contract difference.
- **31 DotNet fixture sources** cover compiler/API/host behavior that has no
  lci equivalent.

The pinned upstream inventory has **325 registrations**. The pinned-lci runner
discovers and runs all 325 when `LCI_PATH` is configured. Tests marked
`LciTheory` skip those comparisons when it is absent; CI can require the
variable. Three specialized upstream fixture tests additionally cover two
unregistered STDLIB programs and the coordinated SOCKS accept program.

This structure avoids two misleading claims: target-only divergences are not
presented as cross-engine agreement, and engine comparisons are not presented
as unconditional on machines without the pinned executable.

## Adding a compatibility case

1. Decide whether the behavior is shared, a documented pinned-lci divergence,
   or .NET/compiler-host-specific.
2. Add the smallest complete fixture and its expected output/error/diagnostic.
3. For a divergence, add local README evidence that states the observed engine
   behavior and why the target intentionally differs.
4. Keep IDs and registration metadata unique; corpus integrity tests reject
   missing files and conflicts.
5. Add a focused unit/API test when the fixture alone cannot identify the
   responsible contract.

## Build your next language

1. Specify a tiny syntax and its observable runtime behavior.
2. Preserve source spans; test lexing and parsing before semantics.
3. Introduce symbols and bound nodes rather than generating directly from
   syntax.
4. State scope/version rules explicitly. Do not let code-generation structure
   accidentally define language behavior.
5. Lower or emit a small semantic core and centralize dynamic operations in a
   runtime.
6. Add path, stream, SDK, and host tests as soon as those deployment surfaces
   exist.
7. Adopt a reference corpus carefully: separate agreement, intentional
   divergence, and host-specific behavior with evidence.

**Final checkpoint:** choose one construct and write its grammar, binding rule,
scope/version behavior, runtime contract, emission plan, focused tests, and
compatibility evidence before editing the implementation.
