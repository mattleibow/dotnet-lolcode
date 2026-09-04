# LOLCODE 1.4 Reference-Implementation Changes

This document describes observable language changes present in Justin Meza's [`lci` interpreter `future` branch](https://github.com/justinmeza/lci/tree/future), pinned for this review at commit [`9377c404c79a122a4698d98118eef44310c751be`](https://github.com/justinmeza/lci/commit/9377c404c79a122a4698d98118eef44310c751be) (23 February 2026). It is a behavioral delta from the archived [LOLCODE 1.3 Draft](archive/lolcode-spec-v1.3.md), not a community-ratified specification.

> **No archived 1.4 specification is known to this repository.** The local archive and `lolcode-spec` repository stop at the unfinished 1.3 draft. Every rule below is therefore identified from the pinned interpreter source and executable behavior.

Compiler support and .NET-specific guidance are kept in the non-normative [Implementation Profile](LANGUAGE_IMPLEMENTATION.md).

---

## Provenance Warning: BRAINZ

The 2026 `lci` README claims a `BRAINZ` neural-network library. No loader, binding, or test was added. At the pinned commit, `CAN HAS BRAINZ?` does nothing and later access to `BRAINZ` fails because the variable does not exist. **BRAINZ is not an implemented 1.4 feature.**

---

## 1. Version Header

The 1.4 fixtures conventionally use:

```lolcode
HAI 1.4
KTHXBYE
```

`lci` parses but does not enforce the version value, so `HAI 1.4` selects no distinct mode and is not itself an observable semantic change.

## 2. Library Import System (`CAN HAS`)

### Overview

LOLCODE 1.4 reuses the `CAN HAS` phrase found in the sparse 1.0 inclusion/requirement proposal. In `lci/future`, it loads named built-in libraries exposed through BUKKIT semantics.

### Syntax

```
CAN HAS <library>[?]
```

The `?` is optional. The library name is an identifier. The implemented names are `STDIO`, `SOCKS`, `STDLIB`, and `STRING`. An unknown name is silently ignored; attempting to use the missing library later fails as an undefined variable.

### Effective Grammar (`parser.c`)

```
ImportStmtNode ::= TT_CANHAS IdentifierNode TT_QUESTION? TT_NEWLINE
```

### Behavior

After import, the library is available as a BUKKIT variable in the current scope. `lci` accepts both a function-value call through `'Z` and the 1.3 object-call spelling:

```lolcode
HAI 1.4
    CAN HAS STDIO?
    I HAS A file
    file R I IZ STDIO'Z OPEN YR "read.dat" AN YR "r" MKAY
    BTW Equivalent call form:
    BTW file R STDIO IZ OPEN YR "read.dat" AN YR "r" MKAY
KTHXBYE
```

## 3. Built-in Libraries

### 3.1 `STDIO` — File I/O

Provides file operations. Loaded as a BUKKIT with the following function slots:

| Slot | Signature | C Equivalent | Returns |
|------|-----------|-------------|---------|
| `OPEN` | `YR filename AN YR mode` | `fopen(filename, mode)` | File handle (BLOB) |
| `DIAF` | `YR file` | `ferror(file) \|\| file == NULL` | TROOF |
| `LUK` | `YR file AN YR length` | `fread(buf, 1, length, file)` | YARN (sanitized) |
| `SCRIBBEL` | `YR file AN YR data` | `fwrite(data, 1, len, file)` | (none) |
| `AGEIN` | `YR file` | `rewind(file)` | (none) |
| `CLOSE` | `YR file` | `fclose(file)` | (none) |

File handles are opaque BLOB values. Operations returning "(none)" return `NOOB`. The implementation does not define a portable text encoding, ownership model, or complete error contract; callers use `DIAF` to detect a null or failed stream.

**Example — reading a file:**

```lolcode
HAI 1.4
    CAN HAS STDIO?
    I HAS A file
    file R I IZ STDIO'Z OPEN YR "read.dat" AN YR "r" MKAY
    I HAS A var
    var R I IZ STDIO'Z LUK YR file AN YR 45 MKAY
    VISIBLE var
KTHXBYE
```

**Supported file modes:** `"r"`, `"w"`, `"a"`, `"r+"`, `"w+"`, `"a+"` (standard C modes).

### 3.2 `SOCKS` — TCP Socket Networking

Provides TCP networking via a wrapper around POSIX sockets. Loaded as a BUKKIT:

| Slot | Signature | C Equivalent | Returns |
|------|-----------|-------------|---------|
| `RESOLV` | `YR addr` | `inet_lookup(addr)` | YARN (IP address) |
| `BIND` | `YR addr AN YR port` | `inet_open(h, TCP, addr, port)` | Host handle (BLOB) |
| `LISTN` | `YR local` | `inet_accept(h, host)` | Remote handle (BLOB) |
| `KONN` | `YR local AN YR addr AN YR port` | `inet_setup + inet_connect` | Remote handle (BLOB) |
| `CLOSE` | `YR local` | `inet_close(host)` | Host handle (BLOB) |
| `PUT` | `YR local AN YR remote AN YR data` | `inet_send(...)` | NUMBR (bytes sent) |
| `GET` | `YR local AN YR remote AN YR amount` | `inet_receive(...)` | YARN (sanitized) |

**Example — DNS lookup:**

```lolcode
HAI 1.4
    CAN HAS SOCKS?
    I HAS A addr
    addr R I IZ SOCKS'Z RESOLV YR "localhost" MKAY
    VISIBLE addr
KTHXBYE
```

The special address `"ANY"` maps to `INADDR_ANY` for binding to all interfaces.

`GET` returns an empty YARN when the underlying receive operation returns a negative result. Blocking, address-family, and broader socket-error behavior remain implementation-defined.

### 3.3 `STDLIB` — Random Numbers

Provides seeded random number generation:

| Slot | Signature | C Equivalent | Returns |
|------|-----------|-------------|---------|
| `MIX` | `YR seed` | `srand(seed)` | (none) |
| `BLOW` | `YR max` | `rand() % max` | NUMBR |

**Example:**

```lolcode
HAI 1.4
    CAN HAS STDLIB?
    I IZ STDLIB'Z MIX YR 0 MKAY
    I HAS A val ITZ I IZ STDLIB'Z BLOW YR 10 MKAY
    VISIBLE val
KTHXBYE
```

`BLOW 0` returns `0`. Behavior for negative maxima is not specified by tests.

### 3.4 `STRING` — String Operations

Provides basic string manipulation:

| Slot | Signature | C Equivalent | Returns |
|------|-----------|-------------|---------|
| `LEN` | `YR string` | `strlen(string)` | NUMBR |
| `AT` | `YR string AN YR position` | `string[position]` | YARN (single char) |

Both operations use encoded bytes, not Unicode scalar values or grapheme clusters. `AT` returns an empty YARN when the index is negative or outside the byte range.

## 4. `INVISIBLE` — Standard Error Output

A counterpart to `VISIBLE` that prints to stderr instead of stdout.

### Grammar

```
PrintStmtNode ::= TT_VISIBLE ExprNodeList TT_BANG? TT_NEWLINE
                 | TT_INVISIBLE ExprNodeList TT_BANG? TT_NEWLINE
```

Behaves identically to `VISIBLE` (including the `!` newline suppression) but outputs to stderr.

> **Note:** This keyword was community-adopted across multiple LOLCODE interpreters before being added to lci. It was never mentioned in the 1.2 or 1.3 specification drafts. The commit message in lci notes: *"Neither the 1.2 specification nor any of the proposals for 1.3 mention using INVISIBLE for this purpose. Nevertheless, the operator and the behavior described herein have seen sufficiently wide adoption in other LOLCODE interpreters."*

## 5. `I DUZ` — System Command Execution

Executes a system command and returns its standard output as a YARN.

### Effective Grammar (`parser.c`)

```
SystemCommandExprNode ::= TT_IDUZ ExprNode
```

This is an **expression**, not a statement. Its operand may be any expression; the resulting value is converted to a command string and passed to the system shell. Standard output is returned as a YARN. A successful command that produces no standard output returns an empty YARN.

The EBNF comment in `parser.h` still says `TT_DUZ IdentifierNode`; both the token name and operand restriction are stale relative to `parser.c`.

Exit status, standard error, shell selection, encoding, and failure behavior are not specified as portable language contracts.

## 6. `HAS AN` — Grammatically Correct Declaration

An alternate form of variable declaration for identifiers starting with a vowel sound:

```lolcode
I HAS AN APPLE ITZ "red"     BTW grammatically correct
I HAS A BANANA ITZ "yellow"  BTW also correct
```

`HAS A` and `HAS AN` are semantically identical. No vowel-sound validation occurs. `HAS AN` is accepted for ordinary variable declarations and BUKKIT slot declarations, including the same SRS forms as `HAS A`.

## 7. `R NOOB` — Dedicated Parsing, Unchanged Semantics

In 1.2 and 1.3, `<var> R NOOB` is assignment of the NOOB literal. `lci/future` gives `R NOOB` a dedicated token and `DeallocationStmtNode`, but execution still replaces the value with NOOB while retaining the declaration. This is an internal parser distinction, not a new observable deallocation rule.

### Effective Grammar (`parser.c`)

```
DeallocationStmtNode ::= IdentifierNode TT_RNOOB TT_NEWLINE
```

---

## 8. Opaque BLOB Values

The library binding system introduces an interpreter runtime value for opaque native handles. BLOB is not a source-level type keyword: it has no literal, cannot be named as a `MAEK` target, and has no specified casts or equality behavior. It can only be received from and passed back to built-in library functions.

Lifetime, ownership, truthiness, and invalid-handle behavior are not coherently specified. The pinned interpreter also has unsafe equality behavior for BUKKIT, function, and BLOB values; that defect is not promoted into a language rule.

---

## Source References

All behavior documented above was checked against pinned commit `9377c404c79a122a4698d98118eef44310c751be`:

| File | Key Content |
|------|------------|
| `tokenizer.h` / `tokenizer.c` | Tokens and lexical behavior |
| `parser.h` / `parser.c` | Grammar, optional import punctuation, `HAS AN`, and expression parsing |
| `interpreter.h` / `interpreter.c` | Runtime value kinds, loop execution, `I DUZ`, and `R NOOB` behavior |
| `binding.c` | Library implementations: `STDIO`, `SOCKS`, `STDLIB`, `STRING` |
| `inet.h` / `inet.c` | TCP socket abstraction layer |
| `test/1.4-Tests/13-Bindings/` | Registered tests for STDIO open/read and SOCKS lookup/open-close |

The pinned branch contains three commits after January 2023:

- `e1d2f6464fbd755cbb2c7667f28749a6f0949fbe` (22 February 2026) fixes interpreter defects, including numeric-token validation, unterminated block comments, four-byte UTF-8 escapes, BOM detection, STRING bounds, `BLOW 0`, negative socket receives, and empty `I DUZ` output. These are conformance and safety fixes, not a new 1.4 syntax layer.
- `dd0daf8b575f99f87b58bbf6f48c579ba048a89b` (22 February 2026) documents BRAINZ in the README but adds no working implementation.
- `9377c404c79a122a4698d98118eef44310c751be` (23 February 2026) changes only an external README link.

The upstream CTest registration covers only four 1.4 binding cases; STRING and STDLIB behavior above was confirmed directly against the executable and source rather than inferred from comprehensive upstream tests.
