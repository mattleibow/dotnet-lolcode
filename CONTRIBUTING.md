# Contributing to dotnet-lolcode

Thank you for your interest in contributing to the LOLCODE .NET compiler! 🐱

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (C# 14)
- [VS Code](https://code.visualstudio.com/) (recommended) or any C# IDE
- [Node.js 20+](https://nodejs.org/) (for VS Code extension development only)

### Building

```bash
# Clone the repo
git clone https://github.com/mattleibow/dotnet-lolcode.git
cd dotnet-lolcode

# Build everything
dotnet build

# Run tests
dotnet test

# Run a sample
dotnet run --project src/Lolcode.Cli -- run samples/01-hello-world/hello.lol
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

- **Lexer tests**: Verify tokenization of individual constructs
- **Parser tests**: Verify AST structure for each language feature
- **Binder tests**: Verify type resolution and semantic error detection
- **Emitter tests**: Verify generated IL produces correct runtime behavior
- **End-to-end tests**: Compile a `.lol` file → run the DLL → assert stdout

### Adding Sample Programs

Sample programs live in `samples/` and are numbered by complexity. If you add a new sample:

1. Create a new numbered directory
2. Add a `.lol` file with clear comments explaining the concepts demonstrated
3. Add a corresponding expected output file if applicable
4. Update the sample table in `README.md`

## Project Layout

| Directory | Purpose |
|-----------|---------|
| `src/Lolcode.Compiler/` | Core compiler library |
| `src/Lolcode.Cli/` | CLI tool |
| `src/Lolcode.Sdk/` | MSBuild SDK integration |
| `tests/` | All test projects |
| `samples/` | Example LOLCODE programs |
| `editor/vscode-lolcode/` | VS Code extension |
| `docs/` | Design and specification documents |

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
