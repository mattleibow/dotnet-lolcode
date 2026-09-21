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
`InteropSamples.LolcatExports`. If the type name is omitted, the SDK derives
and sanitizes it from `AssemblyName`. An explicit type name must be a simple,
non-keyword C# identifier without dots. Every `RootNamespace` segment is
sanitized independently and repeated segments are retained. Setting
`RootNamespace` explicitly empty emits the type in the global namespace;
otherwise the .NET SDK's assembly-derived default applies.

## C# calls LOLCODE

The [C#-head sample](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/csharp-head-lolcode-library)
uses an ordinary `ProjectReference`:

```csharp
Console.WriteLine(InteropSamples.LolcatExports.WELCOME("DOTNET", 3));
Console.WriteLine(InteropSamples.LolcatExports.MEOWLEN());
```

Each public wrapper initializes a fresh LOLCODE module object. Its generated
initializer executes only initializer-safe direct top-level forms: imports,
declarations, object definitions, and function definitions. Assignment, I/O,
control flow, and arbitrary executable statements are skipped. Only direct
top-level `HOW IZ I` functions become public wrappers; parameters and results
use `object`.

Public wrapper results detach the complete returned BLOB graph from program
cleanup and transfer ownership to the managed caller, which must dispose
handles when appropriate. See [providers](providers.md) for lifetime details.

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
type, not by ordinary managed-import class-name rules. Functions can be
declared across multiple source files; direct top-level functions are hoisted
before initializer-safe ordered initialization.

The repository version is `0.3.0`. Check
[versions and availability](../language/versions.md) before assuming a package
has been published to the feed you use.
