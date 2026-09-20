# lci compatibility corpus

`tests/Compatibility` is the repository-owned behavioral corpus. Every shared
case has one directory, an lci-compatible `CMakeLists.txt`, and the normal
`ADD_LOL_TEST` metadata:

```cmake
INCLUDE(AddLolTest)
ADD_LOL_TEST(example OUTPUT test.out INPUT test.in CWD)
```

The supported lci arguments are exactly `LOLCODE`, `OUTPUT`, `INPUT`, `ERROR`,
and `CWD`. `test.lol` is the default source; `test.out`, `test.in`, and support
files are case-local. `ERROR` checks for a nonzero process result. An optional
`test.err` is **not** interpreted by lci: dotnet-lolcode treats its contents as
a stable diagnostic substring, rather than an error-exit-number contract.

## Engines and CI

The shared `LciRegistrationParser` discovers both the pinned upstream corpus
and this tree. `CompatibilityCorpusTests` runs every registered shared case
against dotnet-lolcode and, when `LCI_PATH` is set, pinned lci. Locally the lci
theory is visibly skipped if the executable is unavailable. CI sets both
`LCI_PATH` and `REQUIRE_LCI=1`, so absence of the native executable fails.

`PinnedLciConformanceTests` runs all 325 upstream registrations with pinned
lci; the existing `LciConformanceTests` continues to run all 325 upstream
registrations with dotnet-lolcode. The Ubuntu `compatibility` job builds lci
from the pristine submodule, publishes its TRX results, and runs both engines.
Network-bound fixtures remain outside the shared corpus unless explicitly
bounded and gated.

## Compatibility boundary

* STRING, STDLIB, STDIO, and SOCKS APIs are portable only where the lci
  contract defines them. Managed assemblies, LOLCODE assembly loading, CLR
  exports, and project files are dotnet-only.
* `tests/Compatibility/DotNetOnly` is intentionally not CMake-registered.
  It keeps managed-safety, BLOB ownership/use-after-close, interop,
  trimming/NativeAOT, and .NET-specific flattening probes out of lci.
* STDLIB random values are not cross-engine values. Portable tests assert
  bounds and reseed behavior, never a particular random sequence.
* Error exit numbers and unsafe native BLOB behavior are not equivalence
  contracts.
* A raw 1.2 source concatenation can leave a caller before its callee.
  Project flattening below produces a standard single-file program and is the
  compatibility contract; this does not claim a raw-concatenation equivalence.

## Multi-file fixture flattening

Each multi-file case stores canonical ordered units in `sources.txt`. Generate
its standard `test.lol` with:

```sh
dotnet run --project tools/Lolcode.Compatibility.Flatten/Lolcode.Compatibility.Flatten.csproj
dotnet run --project tools/Lolcode.Compatibility.Flatten/Lolcode.Compatibility.Flatten.csproj -- --check
```

The flattener validates a common HAI version and emits one outer
`HAI`/`KTHXBYE`. It hoists only direct program-level `HOW IZ I <direct-name>`
declarations, in source/statement order. It preserves every other statement's
order and does not hoist SRS, nested, or object functions. Initial launcher
trivia (`#!` and `#:`) is removed only while producing the portable fixture.
`DotNetOnly/ProjectFlatten` contains non-hoisting and mismatch-rejection
probes; they are deliberately not registered as lci tests.

## Extracted EndToEnd fixtures

`tools/extract-e2e-compatibility.py` inventories the selected complete inline
HAI/KTHXBYE EndToEnd programs and creates meaningful category/method fixture
directories. Run it after editing those source tests:

```sh
python3 tools/extract-e2e-compatibility.py
python3 tools/extract-e2e-compatibility.py --check
```

The script's explicit retained sets document tests that require newer
SRS/object/custom-loop semantics or stronger C# assertions. Lexer/parser
trees, diagnostics/spans, PDB/debugger/emission APIs, MSBuild/package/publish
tests, CLR interop, managed BLOB safety, trimming, and NativeAOT remain C#.
