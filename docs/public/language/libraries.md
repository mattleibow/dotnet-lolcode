# Libraries and `CAN HAS`

<span class="badge badge-reference">1.4 reference behavior</span>
<span class="badge badge-supported">Supported in development</span>

`CAN HAS` constructs a LOLCODE library instance and binds its library BUKKIT
in the current scope. The optional `?`, direct names, and SRS names are
accepted:

```lolcode
HAI 1.4
CAN HAS STRING?
VISIBLE I IZ STRING'Z LEN YR "MEOW" MKAY
KTHXBYE
```

Call a library method through `I IZ LIBRARY'Z METHOD ... MKAY` or
`LIBRARY IZ METHOD ... MKAY`. Repeating an import in the same scope is a
no-op. Separate importing scopes construct separate instances. The instance's
state and lifetime are therefore the same whether its consumer is LOLCODE or
C#.

## Official libraries

| Library | Common use | Method reference |
| --- | --- | --- |
| `STRING` | Byte-oriented `LEN` and `AT` operations | [STRING](libraries/string.md) |
| `STDLIB` | Deterministic seeding and bounded random values | [STDLIB](libraries/stdlib.md) |
| `STDIO` | Open/read/write/rewind/close and failure-state file BLOBs | [STDIO](libraries/stdio.md) |
| `SOCKS` | TCP resolve/bind/listen/connect/send/receive BLOBs | [SOCKS](libraries/socks.md) |

All four are attributed library classes in the single `Lolcode.Runtime.dll`
distributed by the SDK. Normal build and publish use that one runtime DLL;
`CAN HAS` is the point at which a library is constructed.

`BRAINZ` is deliberately **not implemented**. It appears in historical
reference text, but has no loader, binding, or tests. Do not treat it as an
available library.

## Managed libraries

A referenced C# assembly can expose one or more libraries. Mark each library
class directly:

```csharp
using Lolcode.Runtime;

[LolcodeLibrary("TEXT")]
public sealed class TextLibrary
{
    public string REPEAT(string value, int count) => string.Concat(Enumerable.Repeat(value, count));
}
```

An attributed class must be public, non-nested, sealed, concrete,
non-generic, and have a public parameterless constructor. Its eligible public
instance methods use only `object`, `string`, `int`, `double`, `bool`, or
`void`. Overloads, generic methods, `ref`/`out`, pointer, byref-like, and
other unsupported signatures are rejected. Each attribute identifies the direct `CAN HAS` name; only explicitly marked
classes are imported.

See [library authoring](../projects/custom-providers.md) for the full C#
contract and [runtime library behavior](../projects/providers.md) for instance
and BLOB lifetimes.
