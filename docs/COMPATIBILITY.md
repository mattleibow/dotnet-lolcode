# Compatibility corpus

`tests/Compatibility` is the canonical source for complete repository-owned
LOLCODE programs. Its semantic layout keeps engine classification structural:

* `Shared/<version>/<category>/<case>` runs on dotnet-lolcode and pinned lci.
  A registered case has `test.lol`, `CMakeLists.txt`, and exact byte fixtures
  (`test.out`, optionally `test.in`) using lci's `ADD_LOL_TEST` contract.
* `KnownLciDivergence/<version>/<category>/<case>` runs only on dotnet-lolcode.
  It is a language case with a documented, evidenced pinned-lci parser,
  semantic, host-width, or unsafe-native divergence. It is not a .NET feature.
  The classification catalog is `tools/compatibility-classifications.json`.
* `DotNet/<version>/<category>/<case>` contains .NET SDK, managed-library,
  provider, byte-stream, filesystem, project, and other host integration
  sources. Focused C# tests load these fixtures for their non-language
  assertions; they do not embed a second program source.

## Fixture contract

`test.lol` is the canonical source. `sources.txt` lists ordered units for a
multi-file fixture; the flattener produces its portable `test.lol`. `test.out`
and `test.in` retain exact bytes. `CWD` uses ordinary fixture support files.

`ERROR` in `CMakeLists.txt` preserves lci's nonzero-exit contract. A dotnet
fixture can additionally use:

* `test.err` — stable runtime-error substring; and
* `test.diag` — stable compile diagnostic ID, optionally followed by a message
  substring or location assertion in future only when a test needs it.

The dotnet runner checks both files when present. lci sees only its existing
`ERROR` contract. Runtime-error fixtures may also have `test.out`, whose exact
bytes are checked by dotnet-lolcode without changing lci behavior.

## Discovery and validation

`CompatibilityCorpusTests` discovers only `Shared`; its paired theories execute
the same CMake registrations on dotnet-lolcode and pinned lci. The separate
`KnownLciDivergenceCompatibilityCorpusTests` runs the documented divergences
on dotnet-lolcode. Specialized fixture-backed C# tests cover .NET host
assertions that cannot be expressed by lci CMake metadata.

Run the migration guard after test changes:

```sh
python3 tools/validate-e2e-fixtures.py
dotnet run --project tools/Lolcode.Compatibility.Flatten/Lolcode.Compatibility.Flatten.csproj -- --check
```

The guard validates `tests/Compatibility/inventory.json` (historic C# identity
→ canonical fixture/classification) and fails when C# EndToEnd code reintroduces
a complete `HAI ... KTHXBYE` program.
