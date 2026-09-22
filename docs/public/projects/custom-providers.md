# Custom module authoring

<span class="badge badge-dotnet">Advanced .NET extension</span>

A custom `CAN HAS` module is an ordinary referenced managed assembly. It does
not need a provider package, transitive MSBuild props, or `@(LolcodeLibrary)`
metadata. Add a `ProjectReference` or `Reference`; normal build and publish
copy the assembly beside the application.

## Export shape

Without extra metadata, the import name is the assembly simple name. The
runtime selects a public, non-nested static class by the ordinary
assembly-name/type-name convention. Its eligible public static methods become
slots on the imported module BUKKIT:

```lolcode
CAN HAS ExampleTools?
VISIBLE I IZ ExampleTools'Z FORMAT YR "HAI" MKAY
```

Methods are not added to global scope. They use the supported managed boundary
types and overload rules described in [managed imports](managed-imports.md).
Only the four trusted built-in libraries can receive
`LolcodeLibraryContext`; custom modules cannot use that parameter or register
resources with the built-in execution tracker.

## Add a friendly alias

Use the optional assembly attribute when the desired `CAN HAS` name or export
type does not match the assembly convention:

```csharp
using Lolcode.Runtime;

[assembly: LolcodeModule("FRIENDLY", typeof(Example.Tools))]
```

The export type must be declared in that assembly and must be a public,
non-nested static class or a valid generated LOLCODE library export. Alias
names use LOLCODE identifier characters. `STRING`, `STDLIB`, `STDIO`, and
`SOCKS` are reserved. Invalid metadata, duplicate aliases across references,
and attempts to claim a reserved name produce compiler diagnostic `LOL9003`.

## State and resources

Custom static methods own any state or host resources they create. Prefer plain
values at the LOLCODE boundary and keep disposable/native resources behind an
application-specific managed API. The automatic per-import state and BLOB
cleanup contract is reserved for the SDK-bundled libraries.

## Validate the module

Test:

- convention and friendly-alias imports;
- invalid, duplicate, and reserved aliases (`LOL9003`);
- each supported signature and rejected overload group;
- framework-dependent and single-file publish asset behavior;
- hostile filenames, addresses, sizes, and host failures where relevant.

The SDK-bundled official libraries are always available as runtime assets and
cannot be disabled or replaced through custom module metadata; see
[runtime libraries](providers.md).
