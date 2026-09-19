# Playground, testing, conformance, and a new-language roadmap

`src/Lolcode.Web` is a Blazor WebAssembly playground around `LolcodeScript`.
`Components/CodeEditor.razor` owns editing; `Execution/LolcodeCodeRunner.cs`
creates scripts and maps diagnostics/runtime failures; `Pages/Home.razor`
coordinates the UI. Its browser constraints are intentional: code runs in the
same WebAssembly runtime and cannot be treated as a sandbox. Read
[the playground architecture](https://github.com/mattleibow/dotnet-lolcode/blob/main/docs/dev/browser-playground.md)
before changing it.

Tests divide responsibilities. `tests/Lolcode.CodeAnalysis.Tests` covers lexer,
parser, runtime, and compiler units. `tests/Lolcode.EndToEnd.Tests` compiles,
runs, and asserts program output across language categories and the pinned
`externals/lci` conformance corpus. `tests/Lolcode.Web.Tests` covers the web
surface. Prefer an end-to-end test when a feature crosses binding, lowering,
runtime, and IL.

## Build your next language

1. Specify a tiny syntax and its runtime behavior.
2. Preserve text spans, then add lexer and parser tests before semantics.
3. Introduce symbols and bound nodes rather than generating from syntax.
4. Lower complex flow into a small core, and centralize dynamic rules in a runtime.
5. Emit or interpret that core, test user-observable behavior, and add tooling.
6. Expand one feature at a time; record gaps in a roadmap.

**Final checkpoint:** choose one new construct, state its grammar, binding rule,
lowered form, runtime contract, and end-to-end test before editing code. This
is the course's reusable method, not just its LOLCODE answer.
