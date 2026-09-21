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

## Module and Shared caveat

An ordinary managed import requires an eligible public, non-nested static CLR
type with directly declared, uniquely named public static methods and supported
signatures. A VB `Module` can compile to a static CLR shape, but that does not
make every module directly importable. Overloads, `ByRef`, optional-parameter
patterns, framework-specific types, or compiler-generated shapes may exclude
members. A `Shared` method on a public class may qualify only if the containing
type and entire exported method group meet the documented rules.

Do not describe this as direct VB `CAN HAS` compatibility. Inspect the emitted
assembly or place a small C# static adapter in front of it.

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
