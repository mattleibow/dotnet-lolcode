# Libraries and CAN HAS

<span class="badge badge-reference">1.4 reference behavior</span>
<span class="badge badge-supported">Supported in development</span>

`CAN HAS` imports a library BUKKIT into the current scope. The syntax accepts
optional `?`, direct names, and SRS names:

```lolcode
HAI 1.4
CAN HAS STRING?
VISIBLE I IZ STRING'Z LEN YR "MEOW" MKAY
KTHXBYE
```

Library slots can be called through `I IZ LIBRARY'Z SLOT ... MKAY` or the
object-call spelling `LIBRARY IZ SLOT ... MKAY`. Importing the same name again
does nothing; an unknown name is ignored until code tries to use that missing
binding. These are reference-behavior choices, not proof of a ratified 1.4
standard.

## Official libraries

| Library | Common use | Slot reference |
| --- | --- | --- |
| `STRING` | Byte-oriented `LEN` and `AT` operations | [STRING](libraries/string.md) |
| `STDLIB` | Deterministic seeding and bounded random values | [STDLIB](libraries/stdlib.md) |
| `STDIO` | Open/read/write/rewind/close and failure-state file BLOBs | [STDIO](libraries/stdio.md) |
| `SOCKS` | TCP resolve/bind/listen/connect/send/receive BLOBs | [SOCKS](libraries/socks.md) |

The project SDK bundles these modular assemblies and adds them as private
runtime references. They are not separately installed NuGet packages.
`CAN HAS` can also import a regular referenced managed assembly by convention
or through a `LolcodeModule` alias; see
[.NET managed imports](../projects/managed-imports.md). Official names take
precedence over a same-named managed DLL and cannot be claimed by an alias.

`BRAINZ` is deliberately **not implemented**. It appears in README text from
the pinned reference source, but has no loader, provider, binding, or tests.
Do not treat it as a 1.4 library or a planned dependency.

Built-in loading, per-import state, byte-backed YARN behavior, and BLOB cleanup
are documented in [runtime libraries](../projects/providers.md). Authors
exposing referenced assemblies should read
[custom module authoring](../projects/custom-providers.md). Package
availability is separate from language support; use
[versions and support](versions.md) when choosing a published SDK versus the
source checkout.
