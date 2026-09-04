# lci conformance corpus

The repository tracks [`justinmeza/lci`](https://github.com/justinmeza/lci)
branch `future` as the `externals/lci` Git submodule. The test project copies
the submodule's CMake metadata and test fixtures into this directory in its
build output.

The upstream project and corpus are licensed under GNU GPL v3; the exact
upstream license is preserved at `upstream/COPYING`.

## Layout

- `externals/lci/test/` is the complete upstream test tree.
- `externals/lci/cmake/` contains the `ADD_LOL_TEST` metadata definition.
- `Conformance/lci/upstream/` is the corresponding build-output layout consumed
  by the xUnit runner.

No upstream corpus file is transformed or duplicated in this repository. Full
relative test directory paths replace upstream's non-unique short CTest names.

## Metadata mapping

`LciConformanceCorpus` discovers each `ADD_LOL_TEST` registration and applies
the upstream defaults:

| Upstream metadata | xUnit behavior |
| --- | --- |
| default `LOLCODE` | read `test.lol` |
| `LOLCODE file` | read the named source file |
| `INPUT file` | send the file contents to stdin |
| `OUTPUT file` | require exit code 0 and exact stdout |
| `ERROR` | require compilation failure or a nonzero process exit |
| `CWD` | run from the registered test directory |
| `test.err` | preserve as an upstream fixture; not asserted by upstream |

The xUnit runner does not invoke Python, CMake, or the lci executable. It
discovers every `ADD_LOL_TEST` registration in the checked-out submodule and
runs every case without an allowlist or skip manifest. Integrity tests reject
duplicate IDs, missing fixtures, and invalid result metadata.

## Upstream corpus observations

- The corpus registers 325 tests: 321 under `1.3-Tests` and 4 under
  `1.4-Tests`.
- 276 tests compare stdout, 49 only require an error, 4 provide stdin, and 1
  requests its source directory as the working directory.
- All 49 error cases contain `test.err`, but `testDriver.py` never reads or
  compares those files.
- The short names passed to CTest are not globally unique: 37 short-name
  groups are duplicated. This import uses full relative directory IDs.
- Three additional `test.lol` files under the 1.4 binding tree are not
  registered and therefore are preserved but not executed.
- The Python driver compares text expectations with byte output without
  decoding, so its stdout path is not Python 3 compatible.
