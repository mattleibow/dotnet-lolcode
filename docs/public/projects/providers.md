# Runtime providers

<span class="badge badge-reference">1.4 reference behavior</span>
<span class="badge">Development availability</span>

Providers make `CAN HAS` libraries modular instead of baking file, network,
text, and random APIs into the compiler. The official descriptors are:

| LOLCODE import | Provider package | Purpose |
| --- | --- | --- |
| `STRING` | `Lolcode.Runtime.String` | UTF-8 byte-oriented string slots |
| `STDLIB` | `Lolcode.Runtime.Stdlib` | Random-number helpers |
| `STDIO` | `Lolcode.Runtime.Stdio` | File BLOB operations |
| `SOCKS` | `Lolcode.Runtime.Socks` | TCP socket BLOB operations |

At build time, a provider package contributes a descriptor containing the
LOLCODE name, assembly name, export type, reservation flag, and contract
version. The runtime rejects malformed, conflicting, or wrong-version
descriptors. The four official names are reserved: an alternate descriptor
cannot replace their contract, and a registered provider wins over a local
managed assembly with the same name.

```lolcode
HAI 1.4
CAN HAS STRING?
VISIBLE I IZ STRING'Z LEN YR "MEOW" MKAY
KTHXBYE
```

## State and resource ownership

One import creates its own `LolcodeLibraryContext`. Its mutable state is shared
by calls through that import but not by another import; `STDLIB` therefore has
a per-import PRNG. Each invocation combines that retained state with the
**invoking caller scope's** resource tracker. Ownership does not follow the
import/module scope: an escaped module can retain provider and lexical state,
while handles allocated by later calls belong to each invoking caller.

Providers register returned `LolBlob` values with
`LolcodeLibraryContext.RegisterResource`. Close and unregister are idempotent.
Lexical blocks and function calls share the invoking execution's tracker, so a
handle is not automatically released merely because an ordinary scope or
function returns. The generated executable or public-wrapper `finally` path
releases remaining handles when that root invocation completes; use explicit
`CLOSE` for earlier release. A public class-library wrapper detaches the
complete returned BLOB graph, which then becomes the managed caller's
responsibility. SOCKS alias BLOBs share a lease; the underlying socket closes
only after all leases close.

`STRING` indexes UTF-8 bytes, not Unicode scalar values. `LEN` reports byte
count and `AT` returns a byte-backed YARN. Those bytes survive string
composition, file/socket I/O, and output until text decoding is necessary.
Read the [implementation profile](../reference/implementation-profile.md) for
the complete library and security behavior, especially `I DUZ`, `STDIO`, and
`SOCKS`. Provider authors should continue to
[custom provider authoring](custom-providers.md).
