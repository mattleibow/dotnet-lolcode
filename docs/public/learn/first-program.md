# 01 Your first program

**Prerequisite:** none.

**Outcome:** create a LOLCODE source file, run it, and explain the relationship
between an editor, a terminal command, and program output.

## Source code is a set of instructions

A computer does not guess what you mean. You write instructions in a **source
file**, a plain-text file whose name ends in `.lol`. An editor changes the
file. A compiler checks and translates it. A terminal shows commands and
output.

You can begin in the [browser playground](../getting-started/playground.md), or
create `hello.lol` for the [file-based workflow](../getting-started/file-based.md).
Enter this complete program:

```lolcode
HAI 1.2
  VISIBLE "HAI, WORLD!"
KTHXBYE
```

Expected output:

```text
HAI, WORLD!
```

`HAI 1.2` begins a complete compilation unit and declares its language
compatibility version. `KTHXBYE` ends it. The statement between them uses
`VISIBLE` to write a value followed by a newline. Indentation helps people read
the program; the words and delimiters tell the compiler what it means.

For a local file pinned to the latest published package, place these host
lines above `HAI`:

```lolcode
#!/usr/bin/env -S dotnet run --file
#:sdk Lolcode.NET.Sdk@0.2.0
```

The shell and .NET host use those lines; the LOLCODE lexer skips them. Run with:

```bash
dotnet run --file hello.lol
```

Building the compiler from a source checkout is optional for consumers. Do it
only when you are contributing to this repository or deliberately using its
unpublished source build.

## Practice: predict, run, explain, modify, create

1. **Predict:** What lines will this program print?

   ```lolcode
   HAI 1.2
     VISIBLE "ONE"
     VISIBLE "TWO"
   KTHXBYE
   ```

2. **Run:** Execute it. Did the output order match the source order?
3. **Explain:** Say what each of the four kinds of line does: start, output,
   output, end.
4. **Modify:** Change `"TWO"` to your name. Run again.
5. **Create:** Write a fresh program that prints a greeting and a question on
   separate lines.

<details>
<summary>Hint and explained solution</summary>

Use one `VISIBLE` statement for each line:

```lolcode
HAI 1.2
  VISIBLE "OH HAI!"
  VISIBLE "HOW R U?"
KTHXBYE
```

The quotes mark text values. `VISIBLE` adds the line ending, so two statements
produce two lines. Different wording is equally correct.
</details>

## Common mistakes

- **Missing `KTHXBYE`:** the parser reaches the file end before the program is
  complete.
- **Curly “smart quotes”:** source code needs ordinary `"..."` quotes.
- **Saving as `hello.lol.txt`:** enable file extensions and keep `.lol`.
- **Typing a command into the source file:** `dotnet run ...` belongs in the
  terminal, not between `HAI` and `KTHXBYE`.
- **Assuming every error is failure:** diagnostics identify a location and a
  `LOL` code. Read the first one before changing several things.

## Checkpoint

You can identify the editor, source file, command, compiler, and output, and
you can run a complete program. Next: [values, output, and variables](foundations.md).
