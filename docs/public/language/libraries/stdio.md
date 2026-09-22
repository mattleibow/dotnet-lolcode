# STDIO library

<span class="badge badge-reference">1.4 reference behavior</span>
<span class="badge badge-reference">Host filesystem access</span>

`STDIO` opens real host files and returns opaque BLOB handles.

| Slot | Behavior |
| --- | --- |
| `OPEN YR filename AN YR mode` | returns a file BLOB, including an error-state BLOB when open fails |
| `DIAF YR file` | WIN for failed, faulted, or closed handles |
| `LUK YR file AN YR length` | reads up to `length` bytes as a byte-backed YARN; nonpositive length returns `""` |
| `SCRIBBEL YR file AN YR data` | writes explicit YARN bytes and flushes |
| `AGEIN YR file` | seeks to the beginning and clears the tracked error on success |
| `CLOSE YR file` | idempotently closes the handle |

Modes are `r`, `w`, `a`, `r+`, `w+`, and `a+`, with their conventional read,
truncate/create, and append meanings.

```lolcode
HAI 1.4
  CAN HAS STDIO?
  I HAS A file ITZ I IZ STDIO'Z OPEN YR "note.txt" AN YR "w+" MKAY
  I IZ STDIO'Z SCRIBBEL YR file AN YR "OH HAI:)" MKAY
  I IZ STDIO'Z AGEIN YR file MKAY
  VISIBLE I IZ STDIO'Z LUK YR file AN YR 64 MKAY !
  I IZ STDIO'Z CLOSE YR file MKAY
KTHXBYE
```

Check `DIAF` after `OPEN` before other operations. An open returned handle is
adopted by the calling LOLCODE scope and is released when that scope is
disposed. Use explicit `CLOSE` for earlier release. A direct C# caller retains
normal .NET ownership of a value it receives.

The compiler is not a filesystem sandbox. Use host permissions and isolation
for untrusted code.
