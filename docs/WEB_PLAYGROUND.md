# Browser playground

`src/Lolcode.Web` is a standalone .NET 10 Blazor WebAssembly application. It
publishes as static files and requires no application server.

The playground currently uses C# so the UI and GitHub Pages deployment can be
exercised before the browser-compatible LOLCODE execution API is available.
The temporary runner is intentionally isolated from the rest of the UI.

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
  playground page.
- `Execution/ICodeRunner.cs` is the boundary between the UI and a language
  implementation.
- `Execution/CSharpCodeRunner.cs` uses the official Roslyn
  `Microsoft.CodeAnalysis.CSharp` package. It loads a curated set of .NET 10
  reference assemblies, emits a uniquely named assembly and portable PDB to
  memory, loads the PE with plain non-collectible `Assembly.Load(byte[], byte[])`,
  invokes its entry point, and captures `Console` output. Browser stack frames
  don't consistently expose source lines for dynamically loaded assemblies, so
  runtime diagnostics fall back to the emitted portable PDB using the failing
  method's metadata token and IL offset. Because browser WebAssembly doesn't
  support `Console.SetIn`,
  the runner supplies input through a generated `FiddleInput.ReadLine()` helper
  instead of pretending `Console.ReadLine()` is available.
- The terminal panel presents only program standard output and standard error.
  The adjacent input panel feeds `FiddleInput.ReadLine()`. A static browser
  application cannot provide an OS shell or run the `dotnet` CLI.

Trimming is disabled for this project because Roslyn and dynamically loaded
user assemblies depend on APIs that static analysis cannot discover.

## Replace the temporary runner with LOLCODE

Once the browser execution API is available:

1. Add a project reference from `Lolcode.Web` to the browser-compatible
   compiler/runtime projects.
2. Implement `ICodeRunner` with `LolcodeScript` or `LolcodeCompilation`,
   mapping its diagnostics into `CodeDiagnostic`.
3. Register the new implementation in `Program.cs`.
4. Change the samples and language label on `Pages/Home.razor`.

No editor, input, terminal, diagnostics, or page-layout redesign is required.
The web project deliberately does not depend on unmerged compiler changes.

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
- The runner is not a process, container, or security boundary. User code can
  access APIs available to the WebAssembly runtime and can interfere with
  process-wide state such as `Console`.
- Source is capped at 100,000 characters, stdin at 32,000 characters, and
  captured output at 128,000 characters. These limits reduce accidental memory
  growth but don't make execution safe.
- The curated reference set supports common console, collection, LINQ, text,
  regex, and JSON programs. It isn't the full .NET reference pack.
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
