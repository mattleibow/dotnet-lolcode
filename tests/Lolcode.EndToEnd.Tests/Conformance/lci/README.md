# lci conformance corpus

This directory vendors the complete registered test corpus from
[`justinmeza/lci`](https://github.com/justinmeza/lci), branch `future`, at
commit `9377c404c79a122a4698d98118eef44310c751be`.

The upstream project and corpus are licensed under GNU GPL v3; the exact
upstream license is preserved at `upstream/COPYING`.

## Layout

- `upstream/test/` is the complete upstream test tree, preserved byte-for-byte.
- `upstream/cmake/` preserves the CMake metadata parser and `ADD_LOL_TEST`
  definition used to derive test behavior.
- `status.json` explicitly classifies every registered test as `pass` or
  `skip`. Every skipped test includes a feature category and concrete reason.
- `upstream-tree.sha256` fingerprints every imported path and byte so fixtures
  outside the CMake metadata cannot drift unnoticed.

No upstream corpus file was transformed. The only generated file is
`status.json`, whose IDs are the registered test directories relative to
`upstream/test/`. Full relative paths replace upstream's non-unique short CTest
names.

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

The xUnit runner does not invoke Python, CMake, lci, or a live checkout.
Corpus integrity tests require exactly 325 unique registrations and 325 unique
classifications, verify all referenced files, and reject missing, orphaned, or
invalid classifications. They also require all 1,376 imported files to match
the pinned tree fingerprint.

The current compiler passes 266 registrations. The remaining 59 registrations
are individually discovered as skipped tests and classified in `status.json`.

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
