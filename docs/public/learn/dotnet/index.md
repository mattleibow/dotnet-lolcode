# LOLCODE for .NET developers

This route assumes you can already read code, use the `dotnet` CLI, and reason
about values, control flow, and functions. It does not repeat the beginner
course or copy SDK/reference tables. Its purpose is to expose the places where
familiar .NET intuition gives the wrong answer.

## Fast route

1. [Concept map](concept-map.md) — map expressions, bindings, functions,
   objects, projects, and compilation to familiar terms.
2. [Semantic differences](semantic-differences.md) — dynamic values, `IT`,
   casts/coercion, equality, scopes, initialization, and advanced runtime rules.
3. Choose [C#](from-csharp.md), [Visual Basic .NET](from-vbnet.md), or
   [F#](from-fsharp.md) for language-specific traps.
4. [Projects and interoperability](workflows-and-interop.md) — SDK, references,
   publishing, mixed-language boundaries, and adapters.
5. [Port a program](port-a-program.md) — translate behavior rather than syntax.

Then use the authoritative [language reference](../../language/reference.md)
and [SDK pages](../../projects/index.md) for exact forms.

## Thirty-second example

```lolcode
HAI 1.2
  HOW IZ I twice YR value
    FOUND YR PRODUKT OF value AN 2
  IF U SAY SO

  I IZ twice YR 21 MKAY
  VISIBLE IT
KTHXBYE
```

Expected output is `42`. The call result enters per-scope `IT`; function
variables are isolated from the caller; runtime values are boxed as
`System.Object`; and arithmetic semantics live in runtime helpers.

If your goal is embedding rather than authoring programs, go directly to
[embedding and scripting](../../projects/embedding.md) after reading semantic
differences.
