# LOLCODE class libraries

<span class="badge badge-dotnet">.NET projects</span>
<span class="badge">Development availability</span>

Set `OutputType` to `Library` to emit a normal managed class library. The
generated export is a public sealed, constructible `IDisposable` instance type
whose public instance methods represent direct top-level `HOW IZ I` functions.
It has no static wrapper, factory, or initializer API.

```xml
<PropertyGroup>
  <OutputType>Library</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <AssemblyName>LolcatPhraseLibrary</AssemblyName>
  <RootNamespace>InteropSamples</RootNamespace>
  <LolcodeLibraryTypeName>LolcatExports</LolcodeLibraryTypeName>
  <LolcodeLibraryName>LOLCAT_PHRASES</LolcodeLibraryName>
</PropertyGroup>
```

`AssemblyName` controls the DLL name. `RootNamespace` and
`LolcodeLibraryTypeName` form the CLR type name, here
`InteropSamples.LolcatExports`. An omitted type name is deterministically
derived from `AssemblyName`; an explicit value must be a simple, non-keyword
C# identifier without dots.

`LolcodeLibraryName` is separate from both names: it is the direct `CAN HAS`
identifier for the generated library. If omitted, the SDK deterministically
derives it from `AssemblyName`; an explicitly invalid identifier fails the
build.

## C# calls LOLCODE

The [C#-head sample](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/csharp-head-lolcode-library)
uses an ordinary `ProjectReference`:

```csharp
using var library = new InteropSamples.LolcatExports();
Console.WriteLine(library.WELCOME("DOTNET", 3));
Console.WriteLine(library.MEOWLEN());
```

Each instance owns a persistent LOLCODE library scope. Calls on that instance
share its variables and dynamic function slots; another instance is isolated.
Dispose it when finished to release its scope-owned resources. Returned open
BLOB values follow normal direct-C# ownership.

## LOLCODE calls LOLCODE

LOLCODE can import the generated class by its `LolcodeLibraryName`:

```lolcode
HAI 1.4
CAN HAS LOLCAT_PHRASES?
VISIBLE I IZ LOLCAT_PHRASES'Z WELCOME MKAY
KTHXBYE
```

The [LOLCODE-head LOLCODE library sample](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/lolcode-head-lolcode-library)
demonstrates the same form. Functions can span multiple source files; direct
top-level functions are available before initializer-safe ordered
initialization.
