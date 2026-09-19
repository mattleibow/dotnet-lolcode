# Browser playground

The [playground](https://mattleibow.github.io/dotnet-lolcode/playground/)
compiles and runs LOLCODE in your browser. Paste a program, provide
deterministic `GIMMEH` input in the input panel, then run it. Standard output,
standard error, compiler diagnostics, and runtime failures stay in the page.

Use it to change one thing at a time while learning: replace a literal with a
variable, make a condition false, or inspect the result of a cast. When you are
ready to save a program, move to the [file-based workflow](file-based.md).

> [!WARNING]
> The playground is convenient, not isolation. Generated code runs on the
> browser UI thread; an infinite loop can freeze the tab. Reload after
> experiments that leave the app in a bad state. Its limits and implementation
> are documented in the repository's
> [Browser playground architecture](https://github.com/mattleibow/dotnet-lolcode/blob/main/docs/dev/browser-playground.md).

For local playground development, run:

```bash
dotnet watch --project src/Lolcode.Web/Lolcode.Web.csproj
```

Open `http://localhost:5138/dotnet-lolcode/playground/`. The deployed page uses
the same path so asset URLs work beneath the GitHub Pages repository site.
