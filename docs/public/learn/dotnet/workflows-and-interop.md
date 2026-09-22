# Projects and interoperability

## Choose the host

- A file-based app is the shortest executable experiment. Its `#:sdk` pin is
  part of host configuration, not the `HAI` language version. Adjacent `.lol`
  files are suppressed from the project glob for this workflow.
- A `.lolproj` records source items, references, output kind, runtime assets,
  publishing settings, and incremental inputs.
- `LolcodeScript` embeds parse/compile/run in a managed host without creating
  path outputs.

Use [SDK and MSBuild reference](../../projects/sdk-reference.md) for property
details rather than duplicating them here.

## Mixed-language architecture

A clean solution usually gives each language a narrow responsibility:

- LOLCODE executable calling an attributed managed library;
- C# executable calling a generated LOLCODE class-library instance;
- a host embedding `LolcodeScript`;
- SDK-bundled `STRING`, `STDLIB`, `STDIO`, and `SOCKS` runtime capabilities;
- referenced managed libraries marked with `LolcodeLibrary`.

`CAN HAS` discovers an eligible attributed library class: public, non-nested,
sealed, concrete, non-generic, and constructible with a public parameterless
constructor. Its eligible public instance methods use the supported
primitive/object boundary. Overloads and unsupported signatures are rejected.
Generated LOLCODE class libraries use the same instance model.

## C#, Visual Basic, and F# adapters

Do not assume arbitrary VB or F# source artifacts can be imported directly
because they compile to IL. Eligibility is based on the resulting public
attributed instance class and method signatures:

- VB `Module` members compile to a static type and therefore need an
  instance-class adapter.
- F# module functions often compile with curried or FSharp.Core-specific
  signatures that are not eligible.

When the emitted surface is unsuitable, add a small attributed C# instance
class with uniquely named methods using `object`, `string`, `int`, `double`,
`bool`, or `void`. Keep records, discriminated unions, async/task types, option
values, and complex domain models behind that boundary.

## Publish deliberately

Framework-dependent publishing carries the single SDK runtime and referenced
libraries through normal assets. `PublishSingleFile` bundles SDK/runtime
assets. Trimming is deferred because reflection-based library discovery needs
an explicit rooting policy.

Continue with your language-specific page or [port a program](port-a-program.md).
