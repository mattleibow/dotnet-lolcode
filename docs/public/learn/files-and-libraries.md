# 11 Libraries, files, and resources

**Prerequisite:** project workflow and basic functions.

**Outcome:** import a development library, distinguish provider state from
resource ownership, and use file/network capabilities deliberately.

> [!IMPORTANT]
> `CAN HAS`, official providers, BLOB handles, and related syntax are 1.4
> reference behavior in this implementation. They are a development
> progression, not stable LOLCODE 1.2.

## A library adds named operations

```lolcode
HAI 1.4
  CAN HAS STRING?
  VISIBLE I IZ STRING'Z LEN YR "MEOW" MKAY
  VISIBLE I IZ STRING'Z AT YR "MEOW" AN YR 1 MKAY
KTHXBYE
```

Expected output:

```text
4
E
```

The project SDK supplies official providers for:

- `STRING`: UTF-8 byte length and indexing;
- `STDLIB`: seeded and bounded random values;
- `STDIO`: file handles and byte-preserving I/O;
- `SOCKS`: TCP socket handles.

These are real host capabilities. `STDIO` can change files, `SOCKS` can use the
network, and `I DUZ` can launch a shell command. The compiler is not a security
sandbox.

## Handles have owners

Files and sockets are represented by opaque BLOB handles. A provider call
allocates returned resources into the **invoking caller scope's** tracker, not
the import/module scope. An imported module can escape and retain its provider
and lexical state, while later calls still register new handles with each
caller that invokes them.

`CLOSE` and unregister are idempotent. Public class-library wrappers detach the
entire returned BLOB graph from program cleanup, transferring responsibility
to the managed caller. Socket BLOB aliases share a lease; the underlying socket
closes only after every lease closes.

Start with `STRING` because it creates no operating-system handle. Move to
`STDIO` only after reading its [slot reference](../language/libraries/stdio.md)
and deciding where the file should live and who closes it.

## Practice: predict, run, explain, modify, create

1. **Predict:** What does `STRING'Z LEN` return for ASCII `"CAT"`?
2. **Run:** Confirm it returns 3.
3. **Explain:** Why might a non-ASCII character have a byte length greater than
   one?
4. **Modify:** Loop over positions 0 through length minus one and print `AT`.
5. **Create:** Build a project-only “text inspector” that reads a YARN and
   prints its UTF-8 byte length plus its first byte-backed character.

<details>
<summary>Hint and explained solution</summary>

```lolcode
HAI 1.4
  CAN HAS STRING?
  I HAS A text
  VISIBLE "TEXT? " !
  GIMMEH text
  I HAS A size ITZ I IZ STRING'Z LEN YR text MKAY
  VISIBLE "UTF-8 BYTEZ:: " size
  BOTH SAEM size AN 0
  O RLY?
    NO WAI
      VISIBLE "FIRST:: " I IZ STRING'Z AT YR text AN YR 0 MKAY
  OIC
KTHXBYE
```

The zero-length check prevents asking for a first byte that does not exist.
</details>

## Common mistakes

- Assuming `STRING` indexes Unicode characters rather than UTF-8 bytes.
- Treating a BLOB as a source-level cast type.
- Forgetting to close an operating-system handle because generated cleanup
  exists; explicit lifetime remains clearer and matters across wrappers.
- Assuming provider state belongs globally to the process; it is per import.
- Using dynamic managed imports in trimmed or NativeAOT applications.

## Checkpoint

You can identify an import, provider state, a caller-owned resource, and the
security boundary. Next: [build and share an application](capstone.md).
