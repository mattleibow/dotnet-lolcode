# Versions and support

<span class="badge">Compatibility guide</span>

Three facts are easy to confuse. This page keeps them separate:

1. **Language provenance** says where a spelling or behavior comes from.
2. **Implementation support** says whether this compiler implements it.
3. **Availability** says which package or host can actually provide it.

`HAI` version tokens are compatibility declarations, not strict full-feature
gates. In particular, multi-file compilations require every complete
`HAI`/`KTHXBYE` unit to have the same header, while 1.3/1.4 runtime-name forms
need their applicable header. A token never means every proposal bearing that
number exists.

## Language provenance and implementation support

| Provenance | Examples | Support |
| --- | --- | --- |
| **Stable core** | 1.2 values, flow, functions, `VISIBLE`, and normal files | **Supported** |
| **1.3 draft** | BUKKIT, SRS, `ME`, prototypes, mixins, dynamic names | **Supported**; draft contradictions are resolved by the pinned reference behavior |
| **1.4 reference behavior** | `CAN HAS`, `INVISIBLE`, `I DUZ`, `HAS AN`, provider libraries | **Supported** except **BRAINZ**, which is **not implemented** |
| **.NET extension** | `.lolproj`, generated CLR exports, managed DLL imports, provider descriptors | **Supported in the source development revision** |
| **Historical source** | 1.0/1.1 and archived drafts | Documentation evidence, not competing compiler rules |

“Partial” is reserved for a feature with an intentionally incomplete
implementation. Current examples: TYPE as a first-class runtime value is
**not implemented**, while the 1.3 draft itself is historical and internally
inconsistent. Do not turn a historical claim into support merely because it
appears in an archived document.

## Availability

| Delivery | What it means |
| --- | --- |
| `Lolcode.NET.Sdk/0.2.0` | Published SDK used by the simple file-based and hello-world examples. |
| `0.3.0-local` source checkout | Development revision built from this repository; it is where the project, interop, and provider documentation is validated. |
| .NET host | Projects target `net10.0`; file-based apps need a compatible `dotnet` host and SDK resolution. |

Do not infer that a development-only interop or provider feature ships in
published `0.2.0`. Conversely, package availability does not alter the
language provenance of a feature.

## Reading badges

Advanced pages use quiet inline badges such as **1.3 draft provenance**,
**1.4 reference behavior**, **Supported**, **Partial**, **Not implemented**,
or **Development availability**. Ordinary 1.2 material stays uncluttered.
For exact semantics, use the canonical
[implementation profile](../reference/implementation-profile.md); for the
source texts, see the [1.3](../reference/language-spec-1.3-changes.md) and
[1.4](../reference/language-spec-1.4-changes.md) deltas.
