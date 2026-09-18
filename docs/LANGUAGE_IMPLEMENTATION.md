# LOLCODE Implementation Profile

This document is the non-normative companion to the language documents:

- [LOLCODE 1.2 Stable Profile](LANGUAGE_SPEC.md)
- [LOLCODE 1.2 to 1.3 Draft Changes](LANGUAGE_SPEC_1.3_CHANGES.md)
- [LOLCODE 1.4 Reference-Implementation Changes](LANGUAGE_SPEC_1.4_CHANGES.md)

The language documents define or report language behavior. This profile records what
`dotnet-lolcode` implements, how values map to .NET, and which reference-interpreter
choices were adopted. Keeping these layers separate prevents CLR design choices from
becoming accidental LOLCODE rules.

## Reference Baselines

| Material | Role |
|---|---|
| `archive/lolcode-spec-v1.2.md` | Primary archived source for the stable 1.2 profile |
| `archive/lolcode-spec-v1.2-draft.md` | Earlier 1.2 witness used to identify unresolved edits |
| `archive/lolcode-spec-v1.3.md` | Unfinished source for the 1.2-to-1.3 draft delta |
| `justinmeza/lci` `future` at `9377c404c79a122a4698d98118eef44310c751be` | Executable reference for ambiguities and informal 1.4 behavior |

The `lci` branch is evidence of implemented behavior, not proof that its crashes,
stale grammar comments, or README-only claims are language rules.

## Current Compiler Target

`dotnet-lolcode` implements the 1.2 stable profile plus the pinned `lci/future`
1.3 object/indirect-identifier behavior and implemented 1.4 behavior. It requires
and retains one token after `HAI`, matching pinned `lci`; `HAI 1.3` and `HAI 1.4`
enable runtime name resolution where static resolution is not possible.

| Area | Support | Notes |
|---|---:|---|
| Stable primitive values and operators | Yes | NUMBR, NUMBAR, YARN, TROOF, and NOOB |
| Variables, `IT`, flow control, and functions | Yes | Function variables are isolated from outer variables |
| Loops | Yes | Includes reference-style custom unary operations |
| Typed default initialization | Yes | The 1.3 `ITZ A <type>` form initializes primitive defaults |
| TYPE runtime values | No | Bare type words remain syntax, not first-class values |
| BUKKIT | Yes | Runtime namespaces with prototype lookup, methods, `ME`, alternate definitions, and mixin copying |
| SRS and 1.3 object features | Yes | Runtime-resolved variable, function, object, parameter, and slot identifiers |
| 1.4 libraries, `INVISIBLE`, `I DUZ`, and `HAS AN` | Yes | Matches the pinned implementation; README-only BRAINZ remains unsupported |

## .NET Value Representation

All LOLCODE values are represented as `System.Object`. Bindings live in runtime
`LolScope` dictionaries so SRS and the unified variable/function namespace can
select them dynamically.

| LOLCODE value | Runtime representation |
|---|---|
| `NUMBR` | `System.Int32` |
| `NUMBAR` | `System.Double` |
| `YARN` | `System.String`; internal wrappers preserve deferred source escapes or exact byte sequences from libraries and commands |
| `TROOF` | `System.Boolean` |
| `NOOB` | `null` |
| `BUKKIT` | `LolObject`, with an ordinal slot dictionary and prototype reference |
| function | `LolFunction`, containing arity and an emitted .NET delegate |
| `BLOB` | `LolBlob`, an opaque managed owner around a file or TCP socket resource |

TYPE has no runtime representation. Type words accepted after `MAEK`, `IS NOW A`,
or `ITZ A` are parser tokens, not first-class values.

## Adopted 1.2 Clarifications

### Explicit Numeric Casts

The archived 1.2 source says an invalid numeric YARN produces an error, but does not
distinguish explicit casts from operator coercion. The compiler follows `lci`:

- explicit invalid YARN-to-NUMBR and YARN-to-NUMBAR casts produce `0` and `0.0`;
- explicit numeric casts follow C-style prefix parsing: leading whitespace and
  trailing nonnumeric text are accepted, while NUMBR also recognizes octal and hex;
- using a nonnumeric YARN in a numeric operator raises a runtime error;
- NUMBAR-to-YARN conversion truncates toward zero and emits exactly two fractional
  digits.

### Loops

The compiler and pinned `lci` agree on this order:

1. Create a fresh loop-local NUMBR initialized to `0`.
2. Evaluate the optional TIL/WILE guard.
3. Execute the body.
4. Apply UPPIN, NERFIN, or the custom unary function.
5. Repeat.

`GTFO` exits before step 4. The loop variable temporarily shadows an outer variable
and disappears when the loop exits.

The compiler accepts the `lci` custom-operation spelling, with full identifiers
for both the destination scope and function name:

```lolcode
IM IN YR loop operations IZ SRS functionName YR i MKAY TIL BOTH SAEM i AN 10
```

The function must take exactly one argument; its return value becomes the next loop value.

### Other Implementation Choices

| Area | Compiler behavior |
|---|---|
| Integer storage and overflow | Signed 32-bit `System.Int32`; unchecked overflow |
| Floating-point storage | IEEE 754 binary64 (`System.Double`) |
| `:o` | U+0007 BELL |
| `:[<name>]` | Curated Unicode-name subset; unsupported names are diagnosed |
| Unicode escape timing | Source YARN escapes resolve when converted/printed; input text is never reinterpreted as source escapes |
| UTF-8 BOM | Accepted as leading source trivia and reproduced at the start of program output |
| `VISIBLE` newline | `Console.WriteLine`, using the host platform newline |
| Equality | Numeric NUMBR/NUMBAR values compare numerically; other primitive types do not auto-cast |
| `WTF?` equality | Exact runtime type and value; OMG uniqueness uses the same typed identity |
| YARN conversion | Implicit NOOB and all TROOF conversions fail; explicit NOOB conversion produces `""` |
| Zero divisor | Integer and floating division/modulo raise `LolRuntimeException` |
| `GTFO` nesting | Applies to the innermost enclosing loop or switch; in a function it returns NOOB |

## Implemented 1.3 Object Choices

- `I HAS A x ITZ A BUKKIT` creates a BUKKIT whose fallback namespace is the
  creating scope, matching pinned `lci`.
- Slot lookup follows the BUKKIT prototype chain with reference-identity cycle
  detection. Assignment updates the scope that owns an inherited slot; it does
  not materialize a receiver-local shadow.
- Direct and SRS slot declarations reject an existing slot on the destination
  BUKKIT, but may override a slot inherited from its prototype.
- `parent` reads or rewires the prototype. Assigning NOOB terminates that chain.
- Object calls bind `ME` to the receiver independently from lexical lookup.
  Bare names in a method search invocation locals and the caller's lexical
  scopes; receiver slots require `ME`.
- A BUKKIT created inside a method inherits the active `ME` caller separately
  from its prototype, including when its default prototype is the creating scope.
- Functions and variables share runtime bindings. A function value can be stored
  in a slot and invoked there; ordinary assignment can replace it.
- SRS explicitly casts its expression to YARN and is evaluated independently at
  every identifier path segment.
- Mixin slots are shallow-copied in reverse argument order before the declared
  parent is installed. The compiler accepts both `ITZ LIEK A parent` and the
  draft's `ITZ A parent SMOOSH ...` form.
- A BUKKIT or function cannot be cast to a primitive. BUKKIT equality is object
  identity.
- Conditional clauses, switch clauses, and loop bodies execute in fresh child
  scopes. Their declarations and `IT` do not leak, while assignment can update
  bindings found in lexical parents.

The draft's `omgwtf`/`izmakin` prose is not exercised or implemented by pinned
`lci`; its references to undefined `canhas` remain ambiguous rather than being
given invented behavior.

## Implemented 1.4 Runtime Policy

`CAN HAS` installs `STDIO`, `SOCKS`, `STDLIB`, or `STRING` as a BUKKIT in the
current scope. Both `I IZ LIBRARY'Z SLOT ...` and `LIBRARY IZ SLOT ...` call
forms use the ordinary object/function machinery. The optional `?`, direct and
SRS library names, duplicate imports, and silently ignored unknown names match
the pinned interpreter. BRAINZ remains unsupported because the pinned tree has
no loader, binding, or tests for it.

`LolBlob` is deliberately not a source-level type. A program cannot name it in
a cast or construct one. File streams and sockets are held by managed owners;
`CLOSE` is idempotent, use after close raises `LolRuntimeException`, and every
handle is also registered with the program's shared resource tracker and closed
from a generated `finally` block when `Main` exits. This replaces lci's raw
pointers and undefined double-close/use-after-close behavior without changing
successful library calls.

- `STDIO` maps the six C modes to `FileStream`, shares open files sufficiently
  for lci's repeated-open fixture, encodes ordinary YARNs as UTF-8, and preserves
  byte-backed YARN data exactly. `OPEN`
  returns an error-state BLOB rather than throwing for path, mode, permission,
  or I/O failures; `DIAF` reports failed, faulted, or closed handles. Other
  operations reject invalid handles explicitly.
- `SOCKS` uses managed TCP sockets. `ANY` maps to `IPAddress.Any`, name lookup
  prefers IPv4 to preserve the `localhost` fixture, `GET` maps EOF and receive
  errors to an empty YARN, and all accepted/connected sockets join the same
  cleanup tracker. Accept and receive retain lci's blocking behavior.
- `STDLIB` uses a per-import managed PRNG, avoiding process-global races while
  preserving bounded values, deterministic reseeding, and `BLOW 0 == 0`.
- `STRING` indexes UTF-8 encoded bytes. `LEN` is the byte count; `AT` returns an
  empty YARN out of bounds and otherwise returns a byte-backed YARN. Equality,
  concatenation, interpolation, file/socket I/O, and process output preserve
  those bytes until they form ordinary text or reach a byte stream.
- `INVISIBLE` uses the same infinite-arity conversion and `!` rules as
  `VISIBLE`, but writes to `Console.Error`.
- `I DUZ` invokes `cmd.exe /C` on Windows and `/bin/sh -c` elsewhere, drains
  stdout and stderr without deadlock, preserves stdout as raw YARN bytes
  (including `""`), forwards stderr bytes unchanged, and raises
  `LolRuntimeException` when the shell cannot be launched.
  Shell execution is intentionally unrestricted language behavior; embedders
  must sandbox untrusted programs and apply their own process, filesystem, and
  network policy.

## Future-Version Engineering Notes

These constraints are implementation guidance, not additions to the language deltas:

- 1.3's global/local `IT` statements contradict one another and require a language
  decision beyond pinned `lci`'s per-scope `IT`.
- `I DUZ`, SOCKS, and STDIO intentionally expose process, network, and filesystem
  capabilities. The compiler does not claim these APIs are a security boundary.

## `lci/future` Validation Notes

The pinned interpreter corpus contains 325 registrations and the compiler runs
all 325 unconditionally with no skip manifest. Three source fixtures omitted
from upstream CMake registration are also run: the seeded and unseeded STDLIB
programs use portable range/reseeding assertions, and the SOCKS accept program
uses a bounded coordinated client. Direct probes cover behavior not represented
by exact-output registrations, including TYPE rejection, loop ordering, custom
loop functions, NUMBAR truncation, invalid numeric casts, every library slot,
optional `CAN HAS` punctuation, alternate library calls, `INVISIBLE`, `I DUZ`,
safe handle cleanup, and the nonfunctional BRAINZ claim.

The February 2026 `lci` changes mostly fix safety and edge cases. They do not establish
a new formal language version. BRAINZ exists only in README text at the pinned commit
and is intentionally excluded from the implemented 1.4 library set.
