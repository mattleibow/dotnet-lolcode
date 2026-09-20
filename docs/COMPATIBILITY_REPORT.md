# Compatibility corpus report

This matrix is regenerated/verified by
`python3 tools/extract-e2e-compatibility.py --check`,
`Lolcode.Compatibility.Flatten -- --check`, and the compatibility CI job.

| Corpus | Cases | Engines | Contract |
| --- | ---: | --- | --- |
| Pinned upstream lci registrations | 325 | pinned lci + dotnet-lolcode | lci `ADD_LOL_TEST` metadata |
| Repository shared registrations | 135 | pinned lci + dotnet-lolcode | Exact stdout bytes except CRLF normalization; stdin/CWD supported |
| Generated EndToEnd shared fixtures | 111 | pinned lci + dotnet-lolcode | Portable inline behavioral programs |
| Hand-authored shared fixtures | 17 | pinned lci + dotnet-lolcode | Core format and error metadata coverage |
| Flattened shared fixtures | 7 | pinned lci + dotnet-lolcode | Semantic single-file linking |
| DotNetOnly registrations | 15 | dotnet-lolcode | Classified parser/spec/runtime/native divergence fixtures |
| DotNetOnly flatten fixtures | 2 | dotnet-lolcode tooling tests | Dynamic/SRS non-hoist and version rejection |

The extraction inventory contains **158** complete inline EndToEnd
HAI/KTHXBYE programs in the migrated language categories. **111** have shared
fixtures, and **14** classified semantic/parser/runtime divergences have
dotnet-only fixtures. **33** stay in C# because they are error-only,
custom-loop/SRS/object behavior, or need a stronger managed assertion.
CodeAnalysis inline programs
are intentionally retained as C# tests because they validate token, tree,
location, recovery, or diagnostic APIs rather than an engine-equivalence
stdout contract.

There is one documented intentional boundary: raw 1.2 concatenation's
caller-before-callee order is not a single-file lci contract. The generated
semantic flattened form is registered and verified instead.

The dotnet-only corpus also includes
`Language/1.2/Boolean/truthiness`, for **15** dotnet-only registrations in
total. See `tests/Compatibility/DotNetOnly/README.md` for the exact
classification and reason for each fixture.
