# Contributing to dotnet-lolcode

Thank you for your interest in contributing to the LOLCODE .NET compiler! 🐱

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (C# 14)
- [VS Code](https://code.visualstudio.com/) (recommended) or any C# IDE

### Building

```bash
# Clone the repo
git clone https://github.com/mattleibow/dotnet-lolcode.git
cd dotnet-lolcode

# Build everything
dotnet build

# Run tests
dotnet test

# Run a sample (project-based)
cd samples/basics/hello-world && dotnet run

# Run a sample (file-based)
dotnet run --file samples/file-based/hello.lol
```

## How to Contribute

### Reporting Bugs

- Use GitHub Issues
- Include the `.lol` source file that triggers the bug
- Include the compiler output / error message
- Include your .NET SDK version (`dotnet --version`)

### Suggesting Features

- Open a GitHub Issue with the `enhancement` label
- Describe the LOLCODE construct or compiler feature you'd like

### Submitting Code

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Make your changes
4. Add/update tests for your changes
5. Ensure all tests pass (`dotnet test`)
6. Commit with a clear message
7. Push and open a Pull Request

### Code Style

- Follow existing code conventions in the project
- Use C# 14 features where appropriate (records, pattern matching, etc.)
- All public APIs should have XML doc comments
- Compiler phases (lexer, parser, binder, emitter) should be independently testable

### Testing Guidelines

Tests are split into two projects:

- **`Lolcode.CodeAnalysis.Tests`** — Unit tests for compiler internals
  - **Lexer tests**: Verify tokenization of individual constructs
  - **Parser tests**: Verify AST structure for each language feature
  - **Runtime tests**: Verify runtime helper behavior (coercion, arithmetic, etc.)

- **`Lolcode.EndToEnd.Tests`** — End-to-end compiler tests (19 category classes)
  - Compile LOLCODE source → run the DLL → assert stdout
  - Categories: BasicProgram, Variables, Math, Boolean, Comparison, Casting, Conditional, Loop, Function, String, Switch, Comment, Formatting, IO, Gtfo, Type, Expression, EdgeCase, Error
  - SDK sample build tests (verify all 16 `.lolproj` samples compile)

### Adding Sample Programs

Sample programs live in `samples/` organized by category (`basics/`, `programs/`, `games/`). If you add a new sample:

1. Create a directory under the appropriate category
2. Add a `.lol` file with clear comments explaining the concepts demonstrated
3. Add a `.lolproj` file (copy from an existing sample)
4. Verify it builds: `dotnet build` and `dotnet run`
5. Update the sample tables in `README.md` and `samples/README.md`

### Working on the MSBuild SDK

The MSBuild SDK (`Lolcode.NET.Sdk`) lets users build `.lolproj` projects with `dotnet build`. Sample projects in `samples/` import the SDK directly from the source tree — no NuGet packaging needed:

1. Build the solution: `dotnet build dotnet-lolcode.slnx`
2. Test a sample: `cd samples/basics/hello-world && dotnet build && dotnet run`

The `samples/Directory.Build.props` overrides the compiler tools path to point at the source-built `Lolcode.Build` output. Changes to the compiler or SDK are picked up immediately after rebuilding the solution.

## Project Layout

| Directory | Purpose |
|-----------|---------|
| `src/Lolcode.CodeAnalysis/` | Core compiler library |
| `src/Lolcode.Runtime/` | Runtime helper library |
| `src/Lolcode.Build/` | MSBuild task (`Lolc`) for SDK integration |
| `src/Lolcode.NET.Sdk/` | MSBuild SDK package (Sdk.props, Sdk.targets) |
| `src/Lolcode.NET.Templates/` | `dotnet new` template pack |
| `tests/` | All test projects |
| `samples/` | Example LOLCODE programs (basics, programs, games) |
| `docs/` | Design and specification documents |

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
