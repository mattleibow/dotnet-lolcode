# Everyday runtime behavior

The [.NET implementation profile](../reference/implementation-profile.md) is the
authoritative record of how this compiler maps LOLCODE values to .NET and which
stable and pinned-future features it supports. It complements, rather than
rewrites, the [normative 1.2 specification](../reference/language-spec.md).

Important learner-facing behavior includes NUMBAR-to-YARN conversion truncated
toward zero to two decimal places, no outer-variable access from ordinary
functions, and context-sensitive `GTFO`. Explicit invalid numeric casts follow
the documented zero/prefix behavior, while numeric operators reject NOOB and
nonnumeric YARN values. Division or modulo by zero raises
`LolRuntimeException`.

`IT` is per function/scope, with versioned control-flow behavior. In 1.2,
conditional and switch declarations/`IT` remain in the enclosing scope; loop
iterations use child scopes but propagate final `IT` and preserve the old value
when zero iterations run. In 1.3/1.4, conditional, switch, and loop bodies are
child scopes whose declarations and `IT` do not leak.

For the mechanism behind these rules, trace [binding and lowering](../compiler-course/binding.md)
and [runtime and IL](../compiler-course/runtime-il.md). For an API consumer,
the [generated API reference](xref:Lolcode.CodeAnalysis) documents the public compiler
surface.
