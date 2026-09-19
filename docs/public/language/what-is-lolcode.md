# What is LOLCODE?

LOLCODE is an esoteric programming language written in a deliberately playful
register: `HAI`, `VISIBLE`, and `O RLY?` are real language forms. Adam Lindsay
created the language in 2007, drawing its vocabulary from the lolspeak used by
the lolcat internet meme. The joke is visible, but the language still provides
recognizable variables, expressions, flow control, functions, input, and
output.

This project does not claim an endorsement or affiliation with LOLCODE's
creator or community. It is an independent .NET implementation and teaching
repository. When discussing syntax, prefer the project's authoritative
[1.2 specification](../reference/language-spec.md); when researching history, consult
the historical source documents below rather than a retelling:

- [LOLCODE 1.0](../reference/archive/lolcode-spec-v1.0.md)
- [LOLCODE 1.1](../reference/archive/lolcode-spec-v1.1.md)
- [LOLCODE 1.2 draft](../reference/archive/lolcode-spec-v1.2-draft.md)
- [LOLCODE 1.2 historical text](../reference/archive/lolcode-spec-v1.2.md)

The language is a useful learning medium because familiar programming ideas
are visible in unusual spellings. Compare `O RLY?` with any language's
if/else, or trace [FizzBuzz](../tutorials/fizzbuzz.md) before reading the
[compiler course](../compiler-course/index.md).

## How the language evolved

The archived specifications show a language that changed through community
discussion rather than a single frozen design:

- 1.0 and 1.1 established the early vocabulary and program shape.
- 1.2 clarified prefix expressions, types, casts, flow control, loops, and
  functions; it is the stable profile taught by this site.
- Later 1.3 and 1.4-oriented work explored objects, dynamic identifiers,
  libraries, and additional runtime behavior. This compiler supports the
  subset recorded in its [implementation profile](implementation.md).

Multiple implementations helped turn the original idea into something people
could run. Secondary histories record an early PHP parser by Jeff Jones, the
`lci` interpreter maintained by Justin Meza, and an earlier .NET compiler by
Nick Johnson. This repository is a separate modern .NET implementation and
does not claim continuity with or endorsement from those projects.

## Sources and further reading

- [The original LOLCODE site](http://www.lolcode.org/)
- [LOLCODE on Wikipedia](https://en.wikipedia.org/wiki/LOLCODE)
- [LOLCODE on Esolang](https://esolangs.org/wiki/LOLCODE)
- The archived specifications linked above
