# Coming from Visual Basic .NET

## Familiar words, different rules

VB and LOLCODE both favor readable keywords, but their semantics differ:

| VB.NET intuition | LOLCODE correction |
| --- | --- |
| identifiers are normally case-insensitive | LOLCODE names are case-sensitive |
| `Dim value = 3` infers a static type | `I HAS A value ITZ 3` creates a dynamic binding |
| `=` may mean assignment or equality by context | `R` assigns; `BOTH SAEM` compares |
| `Nothing` uses VB conversion rules | NOOB has LOLCODE-specific truth/cast behavior |
| `CInt`, `CDbl`, `CStr` | `MAEK` creates a converted value; `IS NOW A` replaces a binding |
| `Select Case` chooses cases without implicit fallthrough | `WTF?` falls through unless `GTFO` |
| `For` declares a chosen range/start | LOLCODE loop counters start at zero and use guard/update forms |

## Class-library caveat

A library for `CAN HAS` must be a public, non-nested, sealed, concrete,
non-generic instance class with a public parameterless constructor and a
`LolcodeLibrary` attribute. A typical VB `Module` and `Shared` method have a
static CLR shape, so they do not meet that contract. Use an attributed instance
class or a small C# adapter.

Overloads, `ByRef`, optional-parameter patterns, framework-specific types, or
compiler-generated shapes can also exclude methods. Keep library methods to
the supported value boundary documented in [managed library imports](workflows-and-interop.md).

## Example translation

VB:

```vb
Function Label(score As Integer) As String
    If score >= 60 Then Return "PASS"
    Return "RETRY"
End Function
```

LOLCODE uses prefix comparison and `FOUND YR`; see the equivalent on the
[C# page](from-csharp.md). Remember that a `GIMMEH` value is YARN until you
convert it. Continue to [projects and interoperability](workflows-and-interop.md).
