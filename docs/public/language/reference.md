# Syntax and reference hub

Use this hub for exact language rules. The [LOLCODE 1.2 specification](../reference/language-spec.md)
is normative for the stable profile; do not duplicate it here. The
[implementation profile](implementation.md) explains the .NET-specific support
boundary and pinned future behavior.

| Need | Reference topic |
| --- | --- |
| Delimiters, comments, whitespace | `HAI`, `KTHXBYE`, `BTW`, `OBTW` / `TLDR` |
| Values and conversion | NUMBR, NUMBAR, YARN, TROOF, NOOB; `MAEK`, `IS NOW A` |
| Expressions | arithmetic, comparison, boolean forms, `SMOOSH` |
| Flow | `O RLY?`, `WTF?`, loops, `GTFO` |
| Functions | `HOW IZ I`, `I IZ`, `FOUND YR`, scope |
| Text | `:` escapes and `:{name}` interpolation |

Repository examples provide working syntax; start with [hello world](../../../samples/basics/hello-world/hello.lol),
[FizzBuzz](../../../samples/programs/fizzbuzz/fizzbuzz.lol), or the
[guessing game](../../../samples/games/guessing-game/guess.lol). If the
compiler rejects a form, find its `LOL` ID in [diagnostics](diagnostics.md)
before assuming a grammar change.
