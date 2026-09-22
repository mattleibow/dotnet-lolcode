# 10 Growing into a .NET project

**Prerequisite:** one working `.lol` program.

**Outcome:** create a `.lolproj`, understand source-item order, split a program
without changing its behavior, and use normal build/run commands.

## A project records a repeatable build

A file-based app is ideal for one experiment. A project becomes useful when
you need several files, references, publishing settings, or a library output.

> [!IMPORTANT]
> The multi-file behavior in this lesson belongs to the repository's `0.3.0`
> source revision, which is not published yet. Build the source checkout and
> start from its project-based samples, or pack `0.3.0` into a local feed.
> Use the published `0.2.0` SDK for the earlier single-file lessons.

```xml
<Project>
  <Import Project="../../src/Lolcode.NET.Sdk/Sdk/Sdk.props" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <Import Project="../../src/Lolcode.NET.Sdk/Sdk/Sdk.targets" />
</Project>
```

The relative import paths above match a project beneath this repository's
`samples/project-based` directory. For another location, adjust them or use a
locally packed SDK.

Put a complete source unit in `Program.lol`, then run:

```bash
dotnet restore
dotnet build
dotnet run
```

The default SDK glob includes project `.lol` files. File-based execution
deliberately suppresses that project glob, so adjacent files do not
accidentally join `dotnet run --file one.lol`.

## Multiple files form one assembly

Every selected source file is a complete `HAI`/`KTHXBYE` unit, and all units
must use the same version. With more than one syntax tree, direct top-level
`HOW IZ I` declarations are hoisted before normal initialization in 1.2,
1.3, and 1.4. Calls can therefore cross file order. Dynamic/SRS functions,
nested functions, and object methods are not hoisted.

Variables, imports, side effects, and other executable statements still run in
`Compile` order. In single-file 1.3/1.4 programs, textual function replacement
remains dynamic. Make order explicit when initialization depends on it:

```xml
<ItemGroup>
  <Compile Remove="**/*.lol" />
  <Compile Include="State.lol" />
  <Compile Include="Program.lol" />
</ItemGroup>
```

Changing, adding, deleting, or reordering a `Compile` item is a semantic and
incremental-build input. The SDK rebuilds one assembly; it does not compile
each `.lol` file into an independent output.

## A safe first split

Move a pure function into `Math.lol`:

```lolcode
HAI 1.2
  HOW IZ I double YR value
    FOUND YR PRODUKT OF value AN 2
  IF U SAY SO
KTHXBYE
```

Call it from `Program.lol`:

```lolcode
HAI 1.2
  VISIBLE I IZ double YR 6 MKAY
KTHXBYE
```

Expected output:

```text
12
```

This split is order-independent because `double` is a direct top-level
function. A top-level variable initializer would remain order-dependent.

## Practice: predict, run, explain, modify, create

1. **Predict:** Does reversing these two files break the call?
2. **Run:** Reverse their explicit `Compile` order and confirm it still prints
   12.
3. **Explain:** Why would a top-level `VISIBLE` or import still move?
4. **Modify:** Add a second function file and call both functions.
5. **Create:** Convert your guessing game into a project with pure helper
   functions separated from interactive program state.

<details>
<summary>Hint and explained solution</summary>

Keep `GIMMEH`, mutable state, and the main loop in `Program.lol`. Move functions
such as `isCorrect` into `Rules.lol`. Both files retain matching `HAI 1.2` and
`KTHXBYE`. If a helper starts reading main-program variables, redesign it to
accept parameters instead.
</details>

## Common mistakes

- Removing `HAI`/`KTHXBYE` from secondary files.
- Mixing header versions.
- Assuming all top-level statements are hoisted.
- Depending on filesystem enumeration instead of explicit `Compile` order.
- Expecting nearby `.lol` files to join file-based execution.

## Checkpoint

You can explain what the project records, what gets hoisted, and what remains
ordered. Continue with [multiple source files](../projects/multiple-files.md)
for the complete contract. Next: [libraries, files, and resources](files-and-libraries.md).
