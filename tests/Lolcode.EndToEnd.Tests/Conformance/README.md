# Upstream `lci` Conformance Tests

`LciConformanceTests` runs the official test corpus from the
[`externals/lci`](../../../externals/lci) submodule. MSBuild maps upstream
`COPYING`, `cmake/`, and `test/` files into
`Conformance/lci/upstream/` beneath the test output directory; no upstream
file is transformed or committed a second time.

## Registration Parser

`LciConformanceCorpus` recursively reads every upstream
`test/**/CMakeLists.txt` and discovers `ADD_LOL_TEST(...)` commands
case-insensitively, as required by CMake. It supports the metadata used by
upstream's `AddLolTest.cmake`:

| Argument | Runner behavior |
|---|---|
| first positional value | Upstream CTest display name |
| `SOURCE <path>` | Source file; defaults to `test.lol` |
| `INPUT <path>` | Standard input fixture |
| `OUTPUT <path>` | Exact standard output fixture |
| `ERROR` | Requires a nonzero result |
| `CWD` | Runs with the fixture directory as the working directory |

Each case ID is its full directory path relative to `test/`. Upstream short
CTest names are not unique, so they are unsuitable as xUnit identities.

Every discovered registration runs unconditionally. There is no allowlist,
status manifest, or skip classification. New registrations therefore become
tests as soon as the submodule gitlink advances. Corpus integrity tests reject
duplicate IDs, missing referenced files, conflicting result metadata, and an
empty registration inventory.

## Upstream Driver Differences

The .NET runner reproduces the semantics of upstream's CMake/Python driver
without invoking Python, CMake, or the `lci` executable:

- `OUTPUT` compares exact standard output.
- `ERROR` checks only for a nonzero result. This matches upstream; its
  `test.err` fixtures are retained but are not asserted by `testDriver.py`.
- Full relative paths avoid the 37 groups of duplicate short CTest names.

The pinned tree also contains three 1.4 fixtures that are not registered by
upstream CMake: two nondeterministic STDLIB programs and one interactive
socket-accept program. Deterministic/coordinated tests cover those behaviors
separately rather than pretending they are upstream registrations.
