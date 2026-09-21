# Multiple source files

<span class="badge badge-dotnet">.NET projects</span>
<span class="badge">Compile order matters</span>

Every `Compile` item is a complete LOLCODE compilation unit: it needs its own
`HAI <version>` and `KTHXBYE`. All units in one compilation must use the same
header version. A mismatch is reported against each file that differs from the
first file's version. The header is a compatibility declaration, not a strict
feature gate: it does not promise that every proposal associated with that
number is present. See [versions and support](../language/versions.md).

The SDK normally globs `**/*.lol`. File-based execution suppresses that glob,
so adjacent files do not silently join a `dotnet run --file` build. In a
project, when top-level initialization has a dependency, declare the order
explicitly:

```xml
<ItemGroup>
  <Compile Remove="**/*.lol" />
  <Compile Include="Welcome.lol" />
  <Compile Include="Greeting.lol" />
</ItemGroup>
```

This is the order used by the
[LOLCODE-head library sample](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/lolcode-head-lolcode-library).
Variables, imports, I/O, side effects, and other normal initialization run in
`Compile` order. Consequently, a top-level variable must be declared before a
later ordered initializer uses it.

When a compilation contains more than one syntax tree, direct top-level
`HOW IZ I` declarations are installed before all normal initialization in
1.2, 1.3, and 1.4. Ordinary cross-file calls therefore work regardless of file
order:

```lolcode
BTW Caller.lol
HAI 1.2
VISIBLE I IZ GREETING MKAY
KTHXBYE
```

```lolcode
BTW Greeting.lol
HAI 1.2
HOW IZ I GREETING
  FOUND YR "HAI FROM ANOTHER FILE"
IF U SAY SO
KTHXBYE
```

This hoist does not include SRS/dynamic declarations, nested functions, or
object methods. In single-file 1.3/1.4 programs, textual declaration and
replacement order remains dynamic. Duplicate declarations still follow the
applicable version rules.

The compiler retains per-file paths for parser, binding, and version
diagnostics. MSBuild makes the ordered `Compile` item list, references, project
files, and compiler options incremental inputs. Editing, adding, deleting, or
reordering a source rebuilds the one emitted assembly; unchanged inputs let
MSBuild skip the target.

The canonical runtime and header rules live in the
[implementation profile](../reference/implementation-profile.md).
