# Compatibility report

The compatibility corpus is fixture-folder authoritative. `Shared`,
`KnownLciDivergence`, and `DotNet` classify cases structurally. A divergence
has local pinned-lci evidence in its fixture `README.md`; no central mapping or
classification manifest is required.

The EndToEnd xUnit project discovers the corpus, validates active local CMake
registrations and sidecars, checks generated multi-file `test.lol` files, and
runs dotnet-lolcode plus pinned lci when configured.
