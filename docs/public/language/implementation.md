# Implementation profile

The [.NET implementation profile](../reference/implementation-profile.md) is the
authoritative record of how this compiler maps LOLCODE values to .NET and which
stable and pinned-future features it supports. It complements, rather than
rewrites, the [normative 1.2 specification](../reference/language-spec.md).

Important learner-facing behavior includes NUMBAR-to-YARN display rounded to
two decimal places, per-scope `IT`, no outer-variable access from functions,
and context-sensitive `GTFO`. Runtime arithmetic rejects NOOB and nonnumeric
YARN values; integer division by zero yields `0`, while floating behavior
follows its documented profile.

For the mechanism behind these rules, trace [binding and lowering](../compiler-course/binding.md)
and [runtime and IL](../compiler-course/runtime-il.md). For an API consumer,
the [generated API reference](../api/index.md) documents the public compiler
surface.
