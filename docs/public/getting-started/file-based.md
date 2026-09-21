# File-based apps

A file-based app is the fastest local route: the file contains its .NET SDK
directive, so no project file is needed. Create `hello.lol`:

```lolcode
#!/usr/bin/env -S dotnet run --file
#:sdk Lolcode.NET.Sdk@0.2.0
HAI 1.2
  VISIBLE "HAI WORLD!"
KTHXBYE
```

Then use normal .NET commands:

```bash
dotnet build hello.lol
dotnet run --file hello.lol
```

The shebang is useful on systems that honor it; the `#:sdk` directive selects
the SDK. The lexer treats both as trivia, not LOLCODE statements. File-based
execution suppresses the project `**/*.lol` glob, so adjacent source files do
not join this program. When deliberately running samples from a source
checkout, first build the solution; repository configuration uses the freshly
built compiler.

Try `samples/basics/hello-world/hello.lol`, then continue with
[values and variables](../learn/foundations.md). For a growing program
with build settings, use a [project](project-based.md).
