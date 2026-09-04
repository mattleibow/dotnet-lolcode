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

`dotnet-lolcode` targets the 1.2 stable profile. It requires and retains one token
after `HAI`, matching pinned `lci`, but does not validate that token or select
semantics from its value.

| Area | Support | Notes |
|---|---:|---|
| Stable primitive values and operators | Yes | NUMBR, NUMBAR, YARN, TROOF, and NOOB |
| Variables, `IT`, flow control, and functions | Yes | Function variables are isolated from outer variables |
| Loops | Yes | Includes reference-style custom unary operations |
| Typed default initialization | Yes | The 1.3 `ITZ A <type>` form initializes primitive defaults |
| TYPE runtime values | No | Bare type words remain syntax, not first-class values |
| BUKKIT | No | Reserved in 1.2; 1.3 object semantics are not implemented |
| SRS and 1.3 object features | No | The archived 1.3 document is an unfinished draft |
| 1.4 libraries, `INVISIBLE`, `I DUZ`, and `HAS AN` | No | Recorded only in the reference-implementation delta |

## .NET Value Representation

All LOLCODE variables are emitted as `System.Object` locals. Runtime helpers inspect
and coerce the boxed value.

| LOLCODE value | Runtime representation |
|---|---|
| `NUMBR` | `System.Int32` |
| `NUMBAR` | `System.Double` |
| `YARN` | `System.String`; source literals with deferred Unicode escapes use an internal runtime wrapper until string use |
| `TROOF` | `System.Boolean` |
| `NOOB` | `null` |

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

The compiler accepts the `lci` custom-operation spelling:

```lolcode
IM IN YR loop I IZ bump YR i MKAY TIL BOTH SAEM i AN 10
```

It also accepts the archived bare spelling (`bump YR i`) for compatibility. The
function must take exactly one argument; its return value becomes the next loop value.

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

## Future-Version Engineering Notes

These constraints are implementation guidance, not additions to the language deltas:

- SRS requires runtime identifier resolution across every identifier position, not
  merely variable lookup.
- 1.3 unifies function and variable namespaces and permits a function binding to be
  overwritten.
- BUKKIT slots may be indexed by NUMBR or YARN, so a string-only dictionary would
  not model the draft faithfully.
- Prototype traversal must detect cycles. Mixin copying and missing-slot
  materialization must preserve the draft's unresolved edge cases rather than silently
  choosing JavaScript semantics.
- 1.3's global/local `IT` statements contradict one another and require a language
  decision before implementation.
- A future BLOB representation should own native resources safely rather than expose
  raw pointers.
- `I DUZ`, SOCKS, and STDIO expose process, network, and filesystem capabilities and
  require an explicit security policy before support is considered.

## `lci/future` Validation Notes

The pinned interpreter was built and its full 325-test corpus was executed with a
Python-3-compatible equivalent of the upstream Python-2-era test driver. Direct probes
were added for behavior not covered by that corpus, including TYPE rejection, loop
ordering, custom loop functions, NUMBAR truncation, invalid numeric casts, optional
`CAN HAS` punctuation, alternate library calls, and the nonfunctional BRAINZ claim.

The February 2026 `lci` changes mostly fix safety and edge cases. They do not establish
a new formal language version. BRAINZ exists only in README text at the pinned commit
and is intentionally excluded from the implemented 1.4 library set.
