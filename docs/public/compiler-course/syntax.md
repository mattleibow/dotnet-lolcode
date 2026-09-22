# Source text, tokens, parser, and AST

`SourceText`, `TextLine`, `TextSpan`, and `TextLocation` in `Text/` make every
later error point back to the original source. Do not pass bare line numbers
around a compiler; spans survive as the common currency for diagnostics,
syntax, and debug information.

`Syntax/Lexer.cs` recognizes keywords, identifiers, literals, comments,
directives, and string escapes. `SyntaxFacts.cs` centralizes spelling and token
knowledge, while `SyntaxKind.cs` is the stable vocabulary. Keeping this
knowledge in one place prevents the lexer and parser from disagreeing.

`Syntax/Parser.cs` is a hand-written recursive-descent parser. It consumes
tokens, recovers after errors, and creates immutable syntax types in
`SyntaxNodes.cs`: `CompilationUnitSyntax`, `VisibleStatementSyntax`,
`IfStatementSyntax`, `LoopStatementSyntax`, and expression forms. Public
`SyntaxTree.ParseText` is the phase boundary exposed to consumers.

**Exercise:** Add a harmless syntax-only test for a known construct, following
the parser tests in `tests/Lolcode.CodeAnalysis.Tests`. Verify both the node
shape and diagnostic-free parse. Do not bind it yet; that separation is the
point. Continue with [binding](binding.md).
