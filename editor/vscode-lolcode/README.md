# LOLCODE for Visual Studio Code

This extension adds syntax highlighting, basic editing configuration, authoring
snippets, and a command to install the LOLCODE .NET templates. It does not provide
a debugger or language-server integration.

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

## Templates and snippets

Run **LOLCODE: Install .NET Templates** from the Command Palette to install the
`Lolcode.NET.Templates` package through `dotnet new install`. This requires the
.NET SDK and network access to the package source.

The extension supplies snippets for programs (`hai`), variables (`var`), output
(`visible`), input (`gimmeh`), conditionals (`if`), loops (`loop`), functions
(`func`), calls (`call`), and switches (`switch`).

## Development

The grammar is in `syntaxes/lolcode.tmLanguage.json`; editing behavior is in
`language-configuration.json`. Keep the keyword and literal rules aligned with
`src/Lolcode.CodeAnalysis/Syntax/SyntaxFacts.cs` and
`src/Lolcode.CodeAnalysis/Syntax/Lexer.cs`. Run `npm test` to validate the
manifest, editor configuration, and representative grammar patterns before
packaging.
