# Browser playground

`src/Lolcode.Web` is a standalone .NET 10 Blazor WebAssembly application. It
publishes as static files and requires no application server.

The playground compiles LOLCODE to .NET IL and executes it entirely inside the
browser. Source, deterministic `GIMMEH` input, `VISIBLE` standard output,
`INVISIBLE` standard error, compiler diagnostics, and runtime failures never
leave the page.

## Run locally

The app uses `/dotnet-lolcode/` as its base path to match the GitHub Pages
repository URL. The launch profile supplies the matching local path base:

```bash
dotnet watch --project src/Lolcode.Web/Lolcode.Web.csproj
```

Open `http://localhost:5138/dotnet-lolcode/` if the browser doesn't open
automatically.

To produce the same static output used by deployment:

```bash
dotnet publish src/Lolcode.Web/Lolcode.Web.csproj \
  --configuration Release \
  --output artifacts/lolcode-web
```

The deployable site is in `artifacts/lolcode-web/wwwroot`.

## Architecture

- `Components/CodeEditor.razor` owns the editor contract. The initial
  implementation is a polished, dependency-free textarea. Monaco and
  CodeMirror were not added because either introduces a JavaScript package,
  asset pipeline, and interop layer for functionality the MVP doesn't yet
  require. A future editor can replace this component without changing the
  playground page. The gutter is a single bounded text node, and a small local
  module keeps it vertically synchronized with the source textarea.
- `Execution/ICodeRunner.cs` is the boundary between the UI and a language
  implementation.
- `Execution/LolcodeCodeRunner.cs` creates a reusable `LolcodeScript`, obtains
  its `LolcodeCompilation` for runtime source mapping, and runs it with
  `LolcodeScriptExecutionOptions`. The scripting API emits a uniquely named PE
  in memory, loads the assembly, scopes `GIMMEH`/`VISIBLE`/`INVISIBLE` I/O,
  invokes the entry point, and returns a structured `LolcodeScriptState`.
- Compiler diagnostics map directly from `LolcodeScriptState`. Browser stack
  frames don't consistently expose source lines for dynamically loaded
  assemblies, so runtime diagnostics fall back to the compilation's portable
  PDB using the failing method's metadata token and IL offset.
- The terminal panel presents `VISIBLE` standard output and distinctly labeled
  `INVISIBLE` standard error streams; it doesn't attempt to reconstruct their
  relative interleaving. The adjacent input panel feeds `GIMMEH`. A static
  browser application cannot provide an OS shell or run the `dotnet` CLI.

## Browser execution limitations

Running arbitrary managed code in the page is convenient, not secure
isolation:

- User code executes on the browser UI thread and in the same WebAssembly
  runtime as the app. An infinite loop can freeze the tab. There is no reliable
  timeout because the UI thread cannot interrupt the running method.
- `Assembly.Load` places every successful compilation into the current runtime.
  Assemblies can't be unloaded individually in this hosting model. Refresh the
  page after many runs to reclaim memory. Collectible `AssemblyLoadContext`
  isn't supported in browser WebAssembly and isn't used by this runner.
- The runner is not a process, container, or security boundary. It executes in
  the same WebAssembly runtime as the playground.
- Source is capped at 100,000 characters, stdin at 32,000 characters, and each
  captured standard stream is truncated after 128,000 UTF-8 bytes. The
  compiler and executing program can still allocate additional memory.
- Browser platform restrictions still apply. There is no native process,
  arbitrary filesystem, or general outbound socket access.

Only run code you trust. Reload the page if a program changes global state or
after repeated compilations.

## GitHub Pages deployment

`.github/workflows/pages.yml` publishes `src/Lolcode.Web` on pushes to `main`
that affect the playground or its workflow. It:

1. Uses .NET 10 to publish the standalone app.
2. verifies the `/dotnet-lolcode/` base path;
3. copies `index.html` to `404.html` so GitHub Pages can boot the Blazor router
   for direct SPA routes;
4. uploads `wwwroot`, including `.nojekyll`; and
5. deploys through GitHub's Pages environment with `pages: write` and
   `id-token: write`.

Repository administrators must select **GitHub Actions** under
**Settings > Pages > Build and deployment > Source** once. No deployment
branch is used.
