# Managed library imports

<span class="badge badge-dotnet">.NET interop</span>
<span class="badge">Development availability</span>

Reference a managed library with a normal `ProjectReference` or `Reference`,
then import the name declared by its attributed library class:

```lolcode
HAI 1.4
CAN HAS ManagedTextPackage?
I HAS A message ITZ I IZ ManagedTextPackage'Z Repeat YR "HAI" AN YR 3 MKAY
VISIBLE message
KTHXBYE
```

The [LOLCODE-head C# library sample](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/lolcode-head-csharp-library)
uses this arrangement.

## What `CAN HAS` discovers

The compiler discovers public classes marked with:

```csharp
[LolcodeLibrary("ManagedTextPackage")]
public sealed class TextTools
{
    public string Repeat(string value, int count) => string.Concat(Enumerable.Repeat(value, count));
}
```

Each attribute name is a direct import name. The type must be public,
non-nested, sealed, concrete, non-generic, and constructible with a public
parameterless constructor. Multiple attributed classes in one assembly are
multiple libraries.

Methods are public instance methods with supported `object`, `string`, `int`,
`double`, `bool`, and `void` boundaries. Overloads, generic methods,
`ref`/`out`, pointers, byref-like values, and unsupported signatures are
rejected. Imported methods stay in the library BUKKIT and are never global
functions.

An importing scope owns the constructed instance. Repeated imports in that
scope are no-ops; separate importing scopes get separate instances. If the
class implements `IDisposable`, importing-scope disposal disposes it. Open
returned `LolBlob` values are adopted by the calling LOLCODE scope.

Only explicitly attributed classes are imported. `LOL9003` remains the catalog
diagnostic for library-discovery failures.
