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

| Library | Common use |
| --- | --- |
| `STRING` | Byte-oriented `LEN` and `AT` operations |
| `STDLIB` | Deterministic seeding and bounded random values |
| `STDIO` | `OPEN`, read/write, close, and failure-state file BLOBs |
| `SOCKS` | TCP bind/listen/connect/accept/send/receive BLOBs |

The project SDK provides these through modular provider packages in the
development surface. `CAN HAS` can also import a regular managed assembly that
is copied beside the application; see [.NET managed imports](../projects/managed-imports.md).
Registered official providers take precedence over a same-named managed DLL.

`BRAINZ` is deliberately **not implemented**. It appears in README text from
the pinned reference source, but has no loader, provider, binding, or tests.
Do not treat it as a 1.4 library or a planned dependency.

Provider descriptors, package selection, per-import state, byte-backed YARN
behavior, and BLOB cleanup are documented in
[runtime providers](../projects/providers.md). Package availability is separate
from language support; use [versions and support](versions.md) when choosing a
published SDK versus the source checkout.
