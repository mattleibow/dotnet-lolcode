# Projects and interoperability

## Choose the host

- A file-based app is the shortest executable experiment. Its `#:sdk` pin is
  part of host configuration, not the `HAI` language version. Adjacent `.lol`
  files are suppressed from the project glob for this workflow.
- A `.lolproj` records source items, references, output kind, provider assets,
  publishing settings, and incremental inputs.
- `LolcodeScript` embeds parse/compile/run in a managed host without creating
  path outputs.

Use [SDK and MSBuild reference](../../projects/sdk-reference.md) for property
details rather than duplicating them here.

## Mixed-language architecture

A clean solution usually gives each language a narrow responsibility:

- LOLCODE executable calling an eligible managed static adapter;
- C# executable calling generated public wrappers from a LOLCODE class library;
- a host embedding `LolcodeScript`;
- separate provider packages for registered runtime capabilities.

Ordinary managed `CAN HAS` import selects an eligible public, non-nested static
CLR type and only methods declared directly on that type. Supported public
static methods need unique names and supported primitive/object signatures.
Any overload group is excluded even if one member would otherwise qualify.

Generated `[LolcodeLibrary]` export selection is a different path. A generated
LOLCODE library exposes its marked export type and factory contract; do not
confuse that with ordinary managed type-name selection.

## C#, Visual Basic, and F# adapters

Do not assume arbitrary VB or F# source artifacts can be imported directly
because they compile to IL. Eligibility is based on the resulting public static
CLR type and method signatures:

- VB `Module` members often compile to a suitable static type, but inspect the
  public shape and avoid unsupported overloads or optional/by-reference forms.
- F# module functions often compile with curried or FSharp.Core-specific
  signatures that are not eligible.

When the emitted surface is unsuitable, add a small C# adapter with one public
static class and uniquely named methods using `object`, `string`, `int`,
`double`, `bool`, or `void`. Keep records, discriminated unions, async/task
types, option values, and complex domain models behind that boundary.

## Publish deliberately

Framework-dependent publishing carries registered providers through normal
assets. `PublishSingleFile` bundles registered providers and loads them from
the default context; dynamic managed DLL imports remain external beside the
host. Trimming and NativeAOT do not support dynamic `CAN HAS` managed imports.

Continue with your language-specific page or [port a program](port-a-program.md).
