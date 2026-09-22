# Managed C# imports

<span class="badge badge-dotnet">.NET interop</span>
<span class="badge">Development availability</span>

`CAN HAS` can load a regular managed DLL located beside the compiled
application. Add it through an ordinary `ProjectReference`, then import the
assembly's simple name:

```lolcode
HAI 1.4
CAN HAS ManagedTextPackage?
I HAS A message ITZ I IZ ManagedTextPackage'Z Repeat YR "HAI" AN YR 3 MKAY
VISIBLE message
KTHXBYE
```

The [mixed-language sample](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/mixed-language)
imports a C# project whose public `TextTools` type is namespaced independently
from its `ManagedTextPackage` assembly name.

## Selection and method shape

Without alias metadata, the loader considers public, non-nested, static classes.
One eligible class is used when it is the only candidate. With several
candidates, exactly one simple type-name match to the import/assembly name is
required. A generated type marked `[LolcodeLibrary]` uses its generated
factory contract instead.

An assembly may opt into a friendly name and explicit export type:

```csharp
[assembly: LolcodeModule("TEXT", typeof(ManagedTextPackage.TextTools))]
```

The compiler discovers aliases from resolved references. The alias must be a
valid LOLCODE identifier, cannot use the reserved built-in names, and must be
unique across the reference set. Invalid metadata reports `LOL9003`.

Only public static methods declared directly on the selected type participate;
inherited static methods do not. A method name becomes a slot only when its
entire group contains exactly one method. Any overload group is excluded, even
when one overload would otherwise have an eligible signature. `object`,
`string`, `int`, `double`, and `bool` parameters and return values map to
LOLCODE values; `void` becomes NOOB. Arguments are converted at the call
boundary. Generic methods, `ref`/`out` parameters, and decimal values are not
exported. Only SDK-bundled built-ins may accept one first
`LolcodeLibraryContext` parameter; ordinary and aliased managed imports cannot.

An import name is an assembly *simple name*, not a path. Empty names, path
separators, rooted paths, drive-qualified names, and `.` or `..` are rejected,
so an import cannot escape the application base directory. Built-in modules take precedence over a same-named DLL. Imported methods remain
slots on the module BUKKIT; they are never installed into global scope. Unknown
imports and duplicate imports leave no new binding, matching the reference
behavior.

Generated LOLCODE libraries marked with `[LolcodeLibrary]` use their generated
factory selection path, distinct from ordinary managed class and method
selection. Managed imports work from emitted CLR shape, not source language:
Visual Basic modules and F# modules may require a C# adapter when signatures
are overloaded, curried, by-reference, or framework-specific. Check
[versions and support](../language/versions.md) before deployment.
