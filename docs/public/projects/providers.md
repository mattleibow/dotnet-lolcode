# Runtime library behavior

<span class="badge badge-reference">1.4 reference behavior</span>
<span class="badge">Development availability</span>

`STRING`, `STDLIB`, `STDIO`, and `SOCKS` are library classes in the single
`Lolcode.Runtime.dll` payload supplied by `Lolcode.NET.Sdk`. They are ordinary
LOLCODE libraries from a consumer's perspective: `CAN HAS` constructs an
instance and binds its public methods in one library BUKKIT.

```lolcode
HAI 1.4
CAN HAS STRING?
VISIBLE I IZ STRING'Z LEN YR "MEOW" MKAY
KTHXBYE
```

Normal builds and publishes include one runtime DLL. There are no separate
runtime-library packages or per-library runtime files to configure.

## Instance state and disposal

One CLR library instance corresponds to one imported LOLCODE library BUKKIT.
Calls through that BUKKIT share the instance's mutable state; another importing
scope gets a distinct instance. Importing the same library again in one scope
does not make another instance.

If a library instance implements `IDisposable`, the importing scope disposes
it. This is also the model used by generated LOLCODE class libraries. Direct
C# callers use normal .NET ownership and dispose the instance they construct.

`STRING` indexes UTF-8 bytes, not Unicode scalar values. `LEN` reports byte
count and `AT` returns a byte-backed YARN. Read the
[implementation profile](../reference/implementation-profile.md) for complete
file, network, and byte-preservation behavior.

## BLOB ownership

An open `LolBlob` returned from a library call is automatically adopted by the
calling LOLCODE scope; closed values are not adopted. Scope disposal releases
adopted BLOBs and disposable library instances. `CLOSE` remains useful for
earlier release and is idempotent. A direct C# caller retains ordinary .NET
ownership of values it receives.

To expose a managed library through `CAN HAS`, see
[library authoring](custom-providers.md).
