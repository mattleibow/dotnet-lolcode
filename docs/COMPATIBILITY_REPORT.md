# Compatibility corpus report

The canonical fixture tree currently contains the following cases. Counts are
validated from the tree rather than generated from C# source:

| Classification | Complete sources | CMake registrations | Execution |
| --- | ---: | ---: | --- |
| Shared | 137 | 138 | dotnet-lolcode and pinned lci |
| KnownLciDivergence | 73 | 73 | dotnet-lolcode only |
| DotNet | 23 | 1 | specialized fixture-backed C# assertions |
| Pinned upstream lci | 325 | 325 | dotnet-lolcode and pinned lci |

`tests/Compatibility/inventory.json` records **204** historic complete inline
EndToEnd programs: 111 map to Shared, 73 map to KnownLciDivergence, and 20 map
to DotNet fixture-backed specialized tests. No complete inline program remains
in C# EndToEnd source.

Known lci divergences are intentionally narrow. The documented 1.2 truthiness,
`IT`, `NOOB` parser/cast, optional `SMOOSH MKAY`, and host-width arithmetic
cases are catalogued in `tools/compatibility-classifications.json`; the
IT-control-flow case is excluded from lci because pinned lci can bus-error.
Object/SRS and custom-loop cases requiring the repository's 1.3 implementation
are isolated in the same divergence root rather than mislabeled as .NET.
