# LOLCODE head with a C# library

Build the local compiler first, then run the LOLCODE executable from the repository root:

```bash
dotnet run --project samples/project-based/lolcode-head-csharp-library/LolcodeHead
```

`lolcode-head-csharp-library` references a C# project whose public sealed
`InteropSamples.TextTools` class opts into LOLCODE with
`[LolcodeLibrary("ManagedTextPackage")]`. `CAN HAS ManagedTextPackage?`
constructs one `TextTools` instance for that importing scope and exposes its
eligible public instance methods as function slots on the library BUKKIT.
Methods may use `object`, `string`, `int`, `double`, and `bool`; LOLCODE
arguments are converted at the call boundary and `void` returns NOOB.
Overloads and unsupported signatures are rejected by library discovery.

Libraries must opt in explicitly with `LolcodeLibrary`; arbitrary DLLs and
static classes are not imported by filename convention.
