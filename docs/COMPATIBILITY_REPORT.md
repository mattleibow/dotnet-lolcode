# Compatibility corpus report

The canonical fixture tree currently contains the following cases. Counts are
validated from the tree rather than generated from C# source:

| Classification | Complete sources | CMake registrations | Execution |
| --- | ---: | ---: | --- |
| Shared | 177 | 178 | dotnet-lolcode and pinned lci |
| KnownLciDivergence | 41 | 41 | dotnet-lolcode only |
| DotNet | 21 | 1 | specialized fixture-backed C# assertions |
| Pinned upstream lci | 325 | 325 | dotnet-lolcode and pinned lci |

`tests/Compatibility/inventory.json` records **204** historic complete
program mappings: 151 map to Shared, 34 map to KnownLciDivergence, and 19 map
to DotNet fixture-backed specialized tests. It also records 11 narrow
non-fixture inline exceptions in CodeAnalysis and Web tests where constructing
the source text, path, version, PDB lines, or stream bytes is itself asserted.

Known lci divergences are intentionally narrow. Literal `NOOB` parser spelling,
optional `SMOOSH MKAY`, and the nonportable `ALL OF` truthiness/optional-`AN`
forms remain catalogued in `tools/compatibility-classifications.json`; portable
casting and boolean coverage is Shared. The IT-control-flow case is excluded
from lci because pinned lci can bus-error. Object/SRS and custom-loop cases
requiring the repository's 1.3 implementation are isolated in the same
divergence root rather than mislabeled as .NET.
