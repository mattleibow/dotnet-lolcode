---
title: Getting started
---
# Getting started

This route gets a first LOLCODE program running, then gives you a choice:
experiment in the browser, run a single file, or grow into a project. You need
the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for local
work. Consumers do not need this repository. Clone it with submodules only if
you plan to contribute to or deliberately build the compiler from source:

```bash
git clone --recurse-submodules https://github.com/mattleibow/dotnet-lolcode.git
cd dotnet-lolcode
dotnet build
```

| I want to... | Take this route |
| --- | --- |
| Try an idea without installing anything | [Browser playground](playground.md) |
| Run one `.lol` file | [File-based app](file-based.md) |
| Keep several source files and normal build settings | [Project-based app](project-based.md) |
| Configure an editor or fix an early error | [Tooling and troubleshooting](tooling.md) |

> [!TIP]
> Read [the first learning lesson](../learn/first-program.md) alongside setup.
> It explains what `HAI`, `VISIBLE`, and `KTHXBYE` mean rather than asking you
> to memorize them.
