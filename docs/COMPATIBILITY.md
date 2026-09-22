# Compatibility

The current fixture inventory is **188 Shared**, **41 KnownLciDivergence**,
**21 DotNet**, and **325 upstream** registrations. The corpus structure below
is authoritative; these counts are a concise current snapshot rather than a
separate status report.

`tests/Lolcode.EndToEnd.Tests/Compatibility` is the canonical source for repository-owned complete
LOLCODE programs. The fixture root is self-contained and has exactly three
classification roots:

* `Shared` contains portable language fixtures. Each fixture has `test.lol`, a
  local `CMakeLists.txt` with an active `ADD_LOL_TEST` registration, and its
  required `test.out`, `test.in`, `test.err`, or `test.diag` sidecars.
* `KnownLciDivergence` contains dotnet-lolcode regressions that pinned lci
  cannot safely execute or match. Every such fixture has a local `README.md`
  recording the observed pinned-lci evidence.
* `DotNet` contains SDK, managed-library, byte-stream, filesystem, and other
  host integration fixtures consumed by focused C# tests.

A multi-file fixture keeps its ordered canonical units plus `sources.txt` and
its generated lci-compatible `test.lol` in the same folder. The xUnit suite
verifies that the generated file is current; no separate flatten command is
required.

`test.diag` starts with an exact `LOLdddd` diagnostic ID. It can additionally
specify `message:` or `location:` assertions. Diagnostic IDs are matched
structurally, never against formatted diagnostic text.

Run all fixture validation and execution with:

```sh
dotnet test tests/Lolcode.EndToEnd.Tests/Lolcode.EndToEnd.Tests.csproj
```

When `LCI_PATH` and `REQUIRE_LCI=1` are set, the same test project runs pinned
lci fixtures. CI builds pinned lci as setup and invokes only `dotnet test` for
fixture validation and execution.
