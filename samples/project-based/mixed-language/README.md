# Mixed-language projects

Build the local compiler first, then run the LOLCODE executable from the repository root:

```bash
dotnet run --project samples/project-based/mixed-language/lolcode-head-csharp-library/LolcodeHead
```

`lolcode-head-csharp-library` imports the ordinary `ManagedTextPackage.dll`
with `CAN HAS ManagedTextPackage?`. The package has no LOLCODE dependency or
attributes. Its sole top-level public static type is the namespaced
`InteropSamples.TextTools`, demonstrating that the assembly/import and CLR type
names are independent. Public static methods may use `object`, `string`, `int`,
`double`, and `bool`; LOLCODE arguments are converted at the call boundary.
`void` returns NOOB. Overloaded method names and unsupported signatures are not
exported.

Namespaced top-level C# static classes are supported. Nested types are ignored.
For an ordinary assembly with several eligible types, exactly one simple
type-name match to the assembly/import name is required; ambiguous assemblies
are ignored.
