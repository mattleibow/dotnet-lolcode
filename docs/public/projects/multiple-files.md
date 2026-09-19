# Multiple source files

<span class="badge badge-dotnet">.NET projects</span>
<span class="badge">Compile order matters</span>

Every `Compile` item is a complete LOLCODE compilation unit: it needs its own
`HAI <version>` and `KTHXBYE`. All units in one compilation must use the same
header version. A mismatch is reported against each file that differs from the
first file's version. The header is a compatibility declaration, not a strict
feature gate: it does not promise that every proposal associated with that
number is present. See [versions and support](../language/versions.md).

The SDK normally globs `**/*.lol`. When top-level initialization has a
dependency, declare the order explicitly:

```xml
<ItemGroup>
  <Compile Remove="**/*.lol" />
  <Compile Include="02-Formatting.lol" />
  <Compile Include="01-State.lol" />
</ItemGroup>
```

This is the order used by the
[LOLCODE-head library sample](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/lolcode-head-lolcode-library).
Top-level statements run in `Compile` order. Consequently, a top-level
variable must be declared before a later file uses it, and a 1.3/1.4
runtime-function value must be initialized before it is called.

Function declarations are collected across every syntax tree, so ordinary
function calls work regardless of file order:

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

Duplicate functions and variables still diagnose the later declaration. The
compiler retains per-file paths for parser, binding, and version diagnostics,
and MSBuild makes `CoreCompile` incremental from the selected `Compile` items,
references, project files, and compiler options. Changing one input rebuilds
the emitted assembly; unchanged inputs let MSBuild skip the target.

The canonical runtime and header rules live in the
[implementation profile](../reference/implementation-profile.md).
