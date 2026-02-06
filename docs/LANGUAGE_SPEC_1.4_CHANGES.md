# LOLCODE 1.4 Changes Specification

This document describes features present in the LOLCODE 1.4 implementation but **never formally specified**. All information is extracted from the [`lci` interpreter's `future` branch](https://github.com/justinmeza/lci/tree/future) (Justin Meza's reference interpreter), specifically from `tokenizer.h`, `parser.h`, `binding.c`, and the `test/1.4-Tests/` directory.

> ⚠️ **There is no official LOLCODE 1.4 specification document.** The `lolcode-spec` repository contains only `v1.2/` and `v1.3/` — no 1.4 was ever written. The features below are **implementation-defined** by the reference interpreter.

> *These changes are **not implemented** in the dotnet-lolcode compiler (which targets 1.2). They are documented here for completeness and future planning.*

---

## Status

| Feature | Category | Source | This Compiler |
|---------|----------|--------|---------------|
| Version header `HAI 1.4` | Changed | Test files | ❌ Not implemented |
| `CAN HAS <lib>?` library imports | New Feature | tokenizer.h, parser.h | ❌ Not implemented |
| `STDIO` built-in library | New Feature | binding.c | ❌ Not implemented |
| `SOCKS` socket library | New Feature | binding.c, inet.c | ❌ Not implemented |
| `STDLIB` random library | New Feature | binding.c | ❌ Not implemented |
| `STRING` string library | New Feature | binding.c | ❌ Not implemented |
| `INVISIBLE` (stderr output) | New Feature | tokenizer.h, parser.h | ❌ Not implemented |
| `I DUZ` system commands | New Feature | tokenizer.h, parser.h | ❌ Not implemented |
| `HAS AN` (alternate declaration) | Syntactic Sugar | tokenizer.h | ❌ Not implemented |
| `R NOOB` (explicit deallocation token) | Tokenizer Change | tokenizer.h | ❌ Not implemented |

---

## 1. Version Header

Programs targeting 1.4 use:

```lolcode
HAI 1.4
KTHXBYE
```

## 2. Library Import System (`CAN HAS`)

### Overview

LOLCODE 1.4 reintroduces `CAN HAS` from the original 1.0 spec, but with a completely different mechanism. Instead of file inclusion, it loads **named built-in libraries** that expose functions via BUKKIT (object) semantics.

### Syntax

```
CAN HAS <library>?
```

The `?` is a required token (not syntactic sugar). The library name is an identifier (e.g., `STDIO`, `SOCKS`, `STDLIB`, `STRING`).

### Grammar (from parser.h EBNF)

```
ImportStmtNode ::= TT_CANHAS IdentifierNode TT_QUESTION TT_NEWLINE
```

### Behavior

After import, the library is available as a BUKKIT variable in the current scope, with functions accessible via the `'Z` slot operator (from 1.3):

```lolcode
HAI 1.4
    CAN HAS STDIO?
    I HAS A file
    file R I IZ STDIO'Z OPEN YR "read.dat" AN YR "r" MKAY
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

### 3.4 `STRING` — String Operations

Provides basic string manipulation:

| Slot | Signature | C Equivalent | Returns |
|------|-----------|-------------|---------|
| `LEN` | `YR string` | `strlen(string)` | NUMBR |
| `AT` | `YR string AN YR position` | `string[position]` | YARN (single char) |

## 4. `INVISIBLE` — Standard Error Output

A counterpart to `VISIBLE` that prints to stderr instead of stdout.

### Grammar (from parser.h EBNF)

```
PrintStmtNode ::= TT_VISIBLE ExprNodeList TT_BANG? TT_NEWLINE
                 | TT_INVISIBLE ExprNodeList TT_BANG? TT_NEWLINE
```

Behaves identically to `VISIBLE` (including the `!` newline suppression) but outputs to stderr.

> **Note:** This keyword was community-adopted across multiple LOLCODE interpreters before being added to lci. It was never mentioned in the 1.2 or 1.3 specification drafts. The commit message in lci notes: *"Neither the 1.2 specification nor any of the proposals for 1.3 mention using INVISIBLE for this purpose. Nevertheless, the operator and the behavior described herein have seen sufficiently wide adoption in other LOLCODE interpreters."*

## 5. `I DUZ` — System Command Execution

Executes a system command and returns its standard output as a YARN.

### Grammar (from parser.h EBNF)

```
SystemCommandExprNode ::= TT_DUZ IdentifierNode
```

This is an **expression** (not a statement) — the result of the system command can be assigned or used in expressions. The identifier is evaluated and its string value is passed to the system shell.

> **Security consideration:** This provides arbitrary shell command execution. A .NET implementation would need careful sandboxing.

## 6. `HAS AN` — Grammatically Correct Declaration

An alternate form of variable declaration for identifiers starting with a vowel sound:

```lolcode
I HAS AN APPLE ITZ "red"     BTW grammatically correct
I HAS A BANANA ITZ "yellow"  BTW also correct
```

Both `HAS A` and `HAS AN` are semantically identical. This was contributed by community member D.E. Akers with reference to [forum discussion](http://forum.lolcode.org/viewtopic.php?f=4&t=13#p19).

## 7. `R NOOB` — Tokenizer-Level Deallocation

In 1.2 and 1.3, `<var> R NOOB` is parsed as an assignment of the NOOB literal. In the 1.4 tokenizer, `R NOOB` is a dedicated token (`TT_RNOOB`) that maps to a `DeallocationStmtNode` rather than an assignment — giving the interpreter the ability to truly release resources rather than just reassigning to nil.

### Grammar (from parser.h EBNF)

```
DeallocationStmtNode ::= IdentifierNode TT_RNOOB TT_NEWLINE
```

---

## Implementation Notes for .NET

### BLOB Type

The library binding system introduces a `BLOB` value type (opaque pointer) that has no LOLCODE-level operations — it can only be passed between library functions. In a .NET implementation, this could map to `System.IntPtr` or a wrapper object.

### Library Loading Model

Libraries are loaded as BUKKIT objects in the current scope. This means:
- The `CAN HAS` statement creates a scope-level variable (e.g., `STDIO`)
- Library functions are BUKKIT slots accessed via `'Z` (e.g., `STDIO'Z OPEN`)
- Function calls use the standard `I IZ <obj>'Z <func> YR ... MKAY` syntax
- This naturally builds on the 1.3 BUKKIT/slot system

### Security Considerations

- `I DUZ` provides unrestricted shell command execution
- `SOCKS` provides raw TCP networking
- `STDIO` provides arbitrary filesystem access
- A production .NET implementation should consider sandboxing or omitting these features

---

## Source References

All features documented above were extracted from the `future` branch of [justinmeza/lci](https://github.com/justinmeza/lci/tree/future):

| File | Key Content |
|------|------------|
| `tokenizer.h` | Token definitions: `TT_INVISIBLE`, `TT_IDUZ`, `TT_CANHAS`, `TT_QUESTION`, `TT_HASAN`, `TT_RNOOB` |
| `parser.h` | EBNF grammar, AST node types: `ImportStmtNode`, `BindingStmtNode`, `SystemCommandExprNode` |
| `binding.c` | Library implementations: `STDIO`, `SOCKS`, `STDLIB`, `STRING` |
| `inet.h` / `inet.c` | TCP socket abstraction layer |
| `test/1.4-Tests/13-Bindings/` | Test cases for stdio, sockets, stdlib |

Last significant feature work on `future` branch: 2015–2016. Last commit: January 2023.
