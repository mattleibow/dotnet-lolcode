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

The loader considers public, non-nested, static classes. One eligible class is
used when it is the only candidate. With several candidates, exactly one
simple type-name match to the import/assembly name is required. A single type
marked `[LolcodeLibrary]` wins regardless of the names; multiple marked types,
ambiguous names, and nested types are ignored.

Only unique public static method names with supported signatures become LOLCODE
slots. `object`, `string`, `int`, `double`, and `bool` parameters and return
values map to LOLCODE values; `void` becomes NOOB. Arguments are converted at
the call boundary. Generic methods, `ref`/`out` parameters, decimal values,
and overload groups are not exported. A provider may additionally accept one
first `LolcodeLibraryContext` parameter; ordinary C# imports cannot.

An import name is an assembly *simple name*, not a path. Empty names, path
separators, rooted paths, drive-qualified names, and `.` or `..` are rejected,
so an import cannot escape the application base directory. Registered
providers take precedence over a same-named DLL. Unknown imports and duplicate
imports leave no new binding, matching the reference behavior.

This is a development-revision feature. Check the package's release notes and
[versions and support](../language/versions.md) before using it outside a
source checkout.
