# Mixed-language projects

Build the local compiler first, then run an executable head from the repository root:

```bash
dotnet run --project samples/project-based/mixed-language/csharp-head-lolcode-library/CSharpHead
dotnet run --project samples/project-based/mixed-language/lolcode-head-csharp-library/LolcodeHead
dotnet run --project samples/project-based/mixed-language/lolcode-head-lolcode-library/LolcodeNumericApp
```

`csharp-head-lolcode-library` shows a C# executable calling a top-level,
directly named LOLCODE `HOW IZ I` function through a public static
`InteropSamples.LolcatExports.WELCOME(object, object)` wrapper using a normal
compile-time `ProjectReference`. The C# app itself uses top-level statements.
Its referenced assembly is `LolcatPhraseLibrary.dll`; the configured CLR export
type is deliberately different. The LOLCODE library has no entry point.

`lolcode-head-csharp-library` imports the ordinary `ManagedTextPackage.dll`
with `CAN HAS ManagedTextPackage?`. The package has no LOLCODE dependency or
attributes. Its sole top-level public static type is the namespaced
`InteropSamples.TextTools`, demonstrating that the assembly/import and CLR type
names are independent. Public static methods may use `object`, `string`, `int`,
`double`, and `bool`; LOLCODE arguments are converted at the call boundary.
`void` returns NOOB. Overloaded method names and unsupported signatures are not
exported.

`lolcode-head-lolcode-library` imports a LOLCODE class library with `CAN HAS
LolNumericLibrary?`. Its assembly name differs from the generated
`InteropSamples.LolcodeExports` type, which uses the `RootNamespace`-based
default. The type is marked with `[LolcodeLibrary]`; it remains a normal CLR
type available to C# and reflection. The default export type is
`$(RootNamespace).LolcodeExports` when `RootNamespace` is set, otherwise
`LolcodeExports`; set `LolcodeLibraryTypeName` to configure a fully qualified
type name atomically.

Namespaced top-level C# static classes are supported. Nested types are
intentionally ignored. Generated LOLCODE libraries use `[LolcodeLibrary]`.
For an ordinary unmarked assembly, a sole top-level public static type is used;
if several exist, exactly one simple type-name match to the assembly/import name
is required. Ambiguous assemblies are ignored.

Each `.lolproj` in these samples has exactly one source file. Although the SDK
accepts and globs multiple `.lol` files, compiler binding currently uses only
the first syntax tree, so multi-file LOLCODE projects are not yet supported
correctly. When multi-file binding is implemented, all files will merge into
one compilation-level export type rather than creating one type per filename.
