# Class libraries

<span class="badge badge-dotnet">.NET projects</span>
<span class="badge">Development availability</span>

Set `OutputType` to `Library` to emit a DLL with public static CLR wrappers for
the top-level LOLCODE functions. The SDK produces a normal managed class
library: it has no executable entry point, and its generated export type is
marked for later LOLCODE imports.

```xml
<PropertyGroup>
  <OutputType>Library</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <AssemblyName>LolcatPhraseLibrary</AssemblyName>
  <RootNamespace>InteropSamples</RootNamespace>
  <LolcodeLibraryTypeName>LolcatExports</LolcodeLibraryTypeName>
</PropertyGroup>
```

`AssemblyName` controls the DLL name. `RootNamespace` and
`LolcodeLibraryTypeName` compose the generated export type, here
`InteropSamples.LolcatExports`. If no type name is set, the SDK derives a valid
CLR identifier from the assembly name. Type names must be simple CLR
identifiers; namespace components are normalized separately.

## C# calls LOLCODE

The [C#-head sample](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/csharp-head-lolcode-library)
uses an ordinary `ProjectReference`:

```csharp
Console.WriteLine(InteropSamples.LolcatExports.WELCOME("DOTNET", 3));
Console.WriteLine(InteropSamples.LolcatExports.MEOWLEN());
```

Each public wrapper initializes a fresh LOLCODE module object. Public wrapper
results transfer any returned BLOB ownership to the managed caller, which must
dispose the handle when appropriate. See [providers](providers.md) for BLOB
lifetime details.

## LOLCODE calls LOLCODE

The [LOLCODE-head sample](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/lolcode-head-lolcode-library)
imports the generated DLL by assembly name:

```lolcode
HAI 1.4
CAN HAS LolcatPhraseLibrary?
VISIBLE I IZ LolcatPhraseLibrary'Z WELCOME MKAY
KTHXBYE
```

Generated libraries are selected by their single `[LolcodeLibrary]` export
type, not by its CLR namespace or type name. Functions can be declared across
multiple source files, while their top-level initialization still follows the
explicit project `Compile` order.

This export and import support is part of the source-checkout `0.3.0-local`
development surface. Do not infer that it ships in the published `0.2.0` SDK
just because that version appears in simple project examples.
