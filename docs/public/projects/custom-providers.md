# Library authoring

<span class="badge badge-dotnet">Advanced .NET extension</span>

Expose a C# library to `CAN HAS` by referencing its assembly normally and
marking each public library class with `LolcodeLibraryAttribute`. One assembly
may contain several libraries: use one attributed class for each direct
LOLCODE library name.

```csharp
using Lolcode.Runtime;

namespace Example;

[LolcodeLibrary("TEXT")]
public sealed class TextLibrary : IDisposable
{
    public string REPEAT(string value, int count) =>
        string.Concat(Enumerable.Repeat(value, count));

    public void Dispose()
    {
    }
}
```

```lolcode
CAN HAS TEXT?
VISIBLE I IZ TEXT'Z REPEAT YR "HAI" AN YR 3 MKAY
```

The compiler reads resolved-reference metadata without running the target
assembly. The class must be public, non-nested, sealed, concrete,
non-generic, and expose a public parameterless constructor. It is an instance
class: public instance methods become slots in the imported library BUKKIT.

## Supported method boundary

Parameters and return values may be `object`, `string`, `int`, `double`,
`bool`, or `void`; `void` returns NOOB. Methods must be public instance
methods. Overloads, generic methods, `ref`/`out`, pointer, byref-like, and
other unsupported signatures are rejected. Avoid static wrappers and factory
or initializer APIs.

Use `IDisposable` when the library owns resources. An importing LOLCODE scope
disposes its library instances. Open `LolBlob` values returned to LOLCODE are
automatically adopted by the calling scope. Direct C# consumers retain normal
.NET ownership and disposal responsibilities.

The `LolcodeLibrary` name is the direct `CAN HAS` identifier. Runtime discovery
errors use `LOL9003`; consult the compiler diagnostics for the precise reported
validation problem.
