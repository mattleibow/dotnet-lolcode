# Dotnet-only compatibility fixtures

This root is a machine-discoverable classification boundary: `CMakeLists.txt`
registrations below `DotNetOnly` run only through
`DotNetOnlyCompatibilityCorpusTests`; the shared lci theory excludes this
directory. Each fixture keeps the normal lci-style `test.lol`, `test.out`, and
`CMakeLists.txt` layout so its dotnet-lolcode behavior remains executable and
reviewable without claiming pinned-lci equivalence.

`tools/compatibility-classifications.json` is the authoritative
machine-readable catalog used by the extraction script. The moved cases are:

| Fixture | Category | Reason |
| --- | --- | --- |
| `Extracted/boolean/all-of` | Adopted spec semantic difference | Pinned lci uses different primitive truthiness for `ALL OF`. |
| `Extracted/boolean/any-of` | Pinned-lci parser gap | Pinned lci rejects the fixture's bare `NOOB` expression. |
| `Extracted/boolean/either-of` | Adopted spec semantic difference | Pinned lci uses different primitive truthiness for `EITHER OF`. |
| `Extracted/boolean/not` | Adopted spec semantic difference | Pinned lci uses different primitive and string truthiness for `NOT`. |
| `Extracted/casting/casting-rules-matrix` | Pinned-lci parser gap | Pinned lci rejects the bare `NOOB` control-flow expression. |
| `Extracted/casting/maek-troof` | Adopted spec semantic difference | Pinned lci has different primitive and string-to-`TROOF` behavior. |
| `Extracted/casting/noob-explicit-cast` | Pinned-lci parser gap | Pinned lci rejects the accepted `NOOB` cast form. |
| `Extracted/edge-case/it-across-control-flow` | Unsafe native behavior | Pinned lci can bus-error and differs for `IT` across control flow. |
| `Extracted/expression/assignment-no-it` | Adopted spec semantic difference | Pinned lci has different `IT` assignment persistence behavior. |
| `Extracted/expression/chained-expressions` | Adopted spec semantic difference | Pinned lci produces different chained bare-expression `IT` results. |
| `Extracted/expression/it-persists` | Adopted spec semantic difference | Pinned lci has different `IT` persistence behavior. |
| `Extracted/math/large-number-arithmetic` | Host/runtime difference | Pinned lci uses a different integer width and overflow result. |
| `Extracted/string/smoosh` | Pinned-lci parser gap | Pinned lci rejects the optional end-of-line `SMOOSH MKAY` form. |
| `Extracted/variables/it-not-set-by-assignment` | Adopted spec semantic difference | Pinned lci has different `IT` assignment persistence behavior. |
| `Language/1.2/Boolean/truthiness` | Adopted spec semantic difference | Pinned lci has different primitive and string truthiness behavior. |

`ProjectFlatten` contains two tooling-only rejection/ordering probes. They do
not use `ADD_LOL_TEST` and remain covered by `FixtureFlattenerTests`.
