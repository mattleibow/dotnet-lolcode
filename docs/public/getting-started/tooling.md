# Tooling and first errors

LOLCODE is line-oriented. An editor with plain-text syntax highlighting and
visible whitespace is enough to begin; save files as UTF-8 with a `.lol`
extension. Use `dotnet build` for a diagnostic before trying to run a program.
The compiler reports source locations with `LOL` diagnostic IDs.

| Symptom | Check first |
| --- | --- |
| `dotnet` is unknown | Install the .NET 10 SDK and reopen the terminal. |
| SDK or compiler cannot be found | For repository samples, run `dotnet build dotnet-lolcode.slnx` first. For a standalone file, keep the `#:sdk` line. |
| A program does not parse | Check `HAI 1.2`, `KTHXBYE`, matching `O RLY?` / `OIC`, and function `IF U SAY SO`. |
| A value prints unexpectedly | `VISIBLE` adds a newline; append `!` to suppress it. |
| A type conversion fails | Input starts as YARN. Use `MAEK value A NUMBR` or `value IS NOW A NUMBR` only when its text is numeric. |
| A loop never ends | Recheck the `TIL` or `WILE` condition and the loop variable update. In the playground, reload the tab. |

Read diagnostics as teaching messages: locate the highlighted source, identify
the construct that was expected, make the smallest repair, and rebuild. The
[diagnostics guide](../language/diagnostics.md) explains phases and IDs; the
[reference](../language/reference.md) supplies exact syntax.
