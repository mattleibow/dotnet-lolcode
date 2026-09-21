# Custom provider authoring

<span class="badge badge-dotnet">Advanced .NET extension</span>

A provider package registers a `CAN HAS` name and a managed export type through
MSBuild metadata. This is different from an ordinary adjacent managed import
and from a generated LOLCODE class library.

## Export shape

Provider slots are public static methods. They may use the ordinary supported
primitive/object parameter and result types. A provider-only extension allows
one first, exact `LolcodeLibraryContext` parameter for:

- per-import state through `GetOrCreateState`;
- registering returned `LolBlob` resources into the invoking caller scope.

Ordinary managed DLL imports cannot request that context. Keep provider
implementation classes out of consumer source contracts; user code calls slots
through the imported BUKKIT.

## Register the descriptor

An official provider package contributes a `buildTransitive` props file:

```xml
<Project>
  <ItemGroup>
    <LolcodeLibrary
      Include="MYLIB|Example.Provider|Example.Provider.Library|false|1" />
  </ItemGroup>
</Project>
```

The fields are LOLCODE name, assembly simple name, export type name, reserved
flag, and contract version. Current contract version is `1`. Mark third-party
names unreserved. The runtime rejects malformed, conflicting, wrong-version,
or attempted replacement descriptors for the four reserved official names.

## Resource ownership

Register every returned BLOB with the invocation context. Ownership follows the
caller that invokes the slot, not the import/module that stored provider state.
Make close/dispose idempotent. If multiple wrappers alias one operating-system
resource, use shared lease/reference ownership so the resource closes only
after all aliases release it. Public generated wrappers detach returned BLOB
graphs for the managed caller to own.

## Package and test

Reference `Lolcode.Runtime`, include the descriptor as a transitive build asset,
and test:

- duplicate and conflicting registration;
- each supported signature and rejected overload group;
- state isolation across imports;
- resource cleanup, explicit close, double close, and use after close;
- framework-dependent and single-file publish asset behavior;
- hostile filenames, addresses, sizes, and host failures where relevant.

Consumers can disable implicit official defaults without removing explicit
references; see [provider configuration](providers.md).
