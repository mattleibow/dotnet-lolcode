# Runtime extensions

<span class="badge badge-reference">1.4 reference behavior</span>
<span class="badge badge-supported">Supported</span>

The compiler implements selected behavior from a pinned `lci/future` reference
snapshot. This is called **1.4 reference behavior** here, not a ratified
language standard. It is useful for compatibility work and advanced programs;
ordinary 1.2 learning material does not need warning labels.

## Extensions in this implementation

| Feature | Behavior |
| --- | --- |
| `CAN HAS` | Constructs an official or eligible managed LOLCODE library in the current scope. |
| `INVISIBLE` | Writes the same infinite-arity display form as `VISIBLE` to standard error; `!` suppresses its newline. |
| `I DUZ` | Runs a host-shell command and returns stdout as a YARN while forwarding stderr. |
| `HAS AN` | Is accepted anywhere `HAS A` declares a variable or BUKKIT slot; no vowel-sound check occurs. |
| BLOBs | Opaque managed handles used by `STDIO` and `SOCKS`; they are not source-level cast targets. |

`I DUZ` uses `cmd.exe /C` on Windows and `/bin/sh -c` elsewhere. It is
intentionally capable of process execution; it is not a sandbox. Hosts that
run untrusted programs must apply their own process, filesystem, and network
restrictions. `STDIO` and `SOCKS` similarly expose real host capabilities.

Byte-backed YARNs preserve raw UTF-8 data from `STRING`, library I/O, and
command output through composition and byte streams. They decode only when a
text operation requires it. NUMBAR-to-YARN conversion is different: it
truncates toward zero to exactly two fractional digits.

`BRAINZ` is **not implemented** despite a historical README claim. The support
matrix and provenance conventions are in [versions and support](versions.md);
the exact compatibility contract, resource lifetime, and library behavior are
in the [implementation profile](../reference/implementation-profile.md).
