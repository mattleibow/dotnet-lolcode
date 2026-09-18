# LOLCODE for Visual Studio Code

Syntax highlighting and practical authoring support for LOLCODE projects and
file-based apps targeting .NET.

## Features

- TextMate highlighting for directives, comments, YARN strings and escapes,
  interpolation, number and TROOF literals, types, operators, declarations,
  function calls, BUKKIT members, and control-flow keywords.
- LOLCODE-aware comments, quote pairing, word selection, indentation, and folding.
- Snippets for programs, variables, output, input, conditionals, loops,
  functions, calls, and switches.
- **LOLCODE: Install .NET Templates**, which installs the `dotnet new` templates
  needed to create LOLCODE projects, source files, and file-based apps.

## Quick start

1. Install the extension from the Visual Studio Marketplace.
2. Open a `.lol` file.
3. Run **LOLCODE: Install .NET Templates** from the Command Palette.

After installing the templates, choose the scaffold that matches your intent:

| Command | Creates |
|---|---|
| `dotnet new lolconsole -n MyApp` | A `.lolproj` console application |
| `dotnet new lol -n Greeter` | A plain `Greeter.lol` file for an existing project |
| `dotnet new lolcode -n hello` | A file-based `hello.lol` app |

The file-based template includes the required `dotnet run --file` shebang and
`#:sdk` directive; the project source-file template deliberately does not.

## Install for development

Copy or symlink `editor/vscode-lolcode` into your VS Code extensions directory, or
package it with the VS Code extension toolchain:

```bash
cd editor/vscode-lolcode
npx @vscode/vsce package
code --install-extension vscode-lolcode-0.1.0.vsix
```

Reload VS Code after installing. The extension recognizes `.lol` files and
file-based .NET LOLCODE scripts whose first line is a `dotnet run --file` shebang.

## Snippets

| Prefix | Expands to |
|---|---|
| `hai` | Program block |
| `var` | Variable declaration |
| `visible`, `gimmeh` | Output and input statements |
| `if`, `switch`, `loop` | Flow-control blocks |
| `func`, `call` | Function declaration and call |

## Scope and privacy

This extension does not collect telemetry or send source code to external
services. It does not provide build/run commands, diagnostics, formatting,
debugging, or language-server features. Use the standard .NET CLI to compile
and run LOLCODE projects and file-based apps.

## Development

The grammar is in `syntaxes/lolcode.tmLanguage.json`; editing behavior is in
`language-configuration.json`. Keep the keyword and literal rules aligned with
`src/Lolcode.CodeAnalysis/Syntax/SyntaxFacts.cs` and
`src/Lolcode.CodeAnalysis/Syntax/Lexer.cs`. Run `npm test` to validate the
manifest, editor configuration, and representative grammar patterns before
packaging.

## Publishing

Pushing a `v<major>.<minor>.<patch>` tag runs the repository release workflow:
it packages the NuGet artifacts and the VSIX, publishes NuGet packages, and
publishes the VSIX to the Visual Studio Marketplace. Before the first extension
release, create the `mattleibow` Marketplace publisher and configure a trusted
publishing policy for this repository's release workflow. The workflow uses
GitHub Actions OIDC instead of a long-lived Marketplace token. NuGet publishing
also uses OIDC; configure the NuGet trusted-publishing policy and set the
`NUGET_USER` repository secret to the NuGet.org profile name.
