# STRING library

<span class="badge badge-reference">1.4 reference behavior</span>

`STRING` exposes byte-oriented operations over a YARN's UTF-8 representation.

```lolcode
HAI 1.4
  CAN HAS STRING?
  VISIBLE I IZ STRING'Z LEN YR "MEOW" MKAY
  VISIBLE I IZ STRING'Z AT YR "MEOW" AN YR 2 MKAY
KTHXBYE
```

Expected output:

```text
4
O
```

| Slot | Result |
| --- | --- |
| `LEN YR value` | NUMBR byte count of the value's YARN representation |
| `AT YR value AN YR position` | one byte-backed YARN, or `""` when position is negative or out of range |

This is not Unicode scalar or grapheme indexing. ASCII characters occupy one
byte; other text can occupy several. Byte-backed YARNs preserve exact bytes
through equality, concatenation, interpolation, provider I/O, and output until
a text operation must decode them.

Provider implementation classes are internal. Import and call the LOLCODE
slots rather than depending on a CLR type.
