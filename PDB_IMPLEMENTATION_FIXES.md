# Portable PDB Implementation: Critical Fixes

## Overview
This document provides specific architectural and code-level fixes for the Portable PDB sequence point generation in the LOLCODE compiler. Each fix addresses a risk that could cause debugger crashes, incorrect line highlighting, or missing breakpoints.

---

## Issue 1: Block Syntax Nullability

**Risk:** `BoundBlockStatement` is synthesized during lowering (e.g., in `RewriteBlock`). If `EmitStatement` blindly accesses `node.Syntax` and emits a sequence point, it will crash with `NullReferenceException`.

**Root Cause:** `BoundBlockStatement` has no `Syntax` property, only a list of statements. Many blocks are created without a direct syntax mapping.

**Fix:**

### 1a. Add Syntax to BoundNode Base Class

```csharp
// File: BoundTree/BoundNodes.cs
internal abstract class BoundNode
{
    /// <summary>The kind of bound node.</summary>
    public abstract BoundKind Kind { get; }
    
    /// <summary>
    /// The source syntax node this bound node was created from.
    /// Null if the bound node is synthetic (created during lowering).
    /// </summary>
    public virtual SyntaxNode? Syntax => null;
}
```

### 1b. Add Syntax Property to Control Flow Statements

```csharp
// File: BoundTree/BoundNodes.cs

/// <summary>O RLY? conditional.</summary>
internal sealed class BoundIfStatement : BoundStatement
{
    public BoundBlockStatement ThenBlock { get; }
    public ImmutableArray<BoundMebbeClause> MebbeClauses { get; }
    public BoundBlockStatement? ElseBlock { get; }
    public IfStatementSyntax? Syntax { get; }  // NEW
    
    public BoundIfStatement(
        BoundBlockStatement thenBlock, 
        ImmutableArray<BoundMebbeClause> mebbeClauses, 
        BoundBlockStatement? elseBlock,
        IfStatementSyntax? syntax = null)  // NEW parameter
    {
        ThenBlock = thenBlock;
        MebbeClauses = mebbeClauses;
        ElseBlock = elseBlock;
        Syntax = syntax;  // NEW
    }
    public override BoundKind Kind => BoundKind.IfStatement;
}

/// <summary>WTF? switch.</summary>
internal sealed class BoundSwitchStatement : BoundStatement
{
    public ImmutableArray<BoundOmgClause> OmgClauses { get; }
    public BoundBlockStatement? DefaultBlock { get; }
    public SwitchStatementSyntax? Syntax { get; }  // NEW
    
    public BoundSwitchStatement(
        ImmutableArray<BoundOmgClause> omgClauses, 
        BoundBlockStatement? defaultBlock,
        SwitchStatementSyntax? syntax = null)  // NEW parameter
    {
        OmgClauses = omgClauses;
        DefaultBlock = defaultBlock;
        Syntax = syntax;  // NEW
    }
    public override BoundKind Kind => BoundKind.SwitchStatement;
}

/// <summary>IM IN YR loop.</summary>
internal sealed class BoundLoopStatement : BoundStatement
{
    // ... existing properties ...
    public LoopStatementSyntax? Syntax { get; }  // NEW
    
    public BoundLoopStatement(
        // ... existing parameters ...
        LoopStatementSyntax? syntax = null)  // NEW parameter
    {
        // ... existing assignments ...
        Syntax = syntax;  // NEW
    }
    // ...
}

/// <summary>HOW IZ I function declaration.</summary>
internal sealed class BoundFunctionDeclaration : BoundStatement
{
    public FunctionSymbol Function { get; }
    public BoundBlockStatement Body { get; }
    public FunctionDeclarationSyntax? Syntax { get; }  // NEW
    
    public BoundFunctionDeclaration(
        FunctionSymbol function, 
        BoundBlockStatement body,
        FunctionDeclarationSyntax? syntax = null)  // NEW parameter
    {
        Function = function;
        Body = body;
        Syntax = syntax;  // NEW
    }
    public override BoundKind Kind => BoundKind.FunctionDeclaration;
}
```

### 1c. Update Binding Layer to Capture Syntax

In the `Binder` class, when creating these bound nodes, pass the syntax:

```csharp
// File: Binding/Binder.cs (example for IfStatement)

private BoundIfStatement BindIfStatement(IfStatementSyntax syntax)
{
    var condition = BindExpression(syntax.Condition);
    var thenBlock = BindBlockStatement(syntax.ThenBlock);
    
    var mebbeClauses = ImmutableArray.CreateBuilder<BoundMebbeClause>();
    foreach (var mebbeSyntax in syntax.MebbeClauses)
    {
        var mebbeCondition = BindExpression(mebbeSyntax.Condition);
        var mebbeBody = BindBlockStatement(mebbeSyntax.Body);
        mebbeClauses.Add(new BoundMebbeClause(mebbeCondition, mebbeBody));
    }
    
    BoundBlockStatement? elseBlock = null;
    if (syntax.ElseBlock != null)
        elseBlock = BindBlockStatement(syntax.ElseBlock);
    
    // NEW: Pass syntax to constructor
    return new BoundIfStatement(
        thenBlock, 
        mebbeClauses.ToImmutable(), 
        elseBlock,
        syntax);  // PASS SYNTAX HERE
}
```

### 1d. CodeGenerator: Never Emit Sequence Point for Blocks

```csharp
// File: CodeGen/CodeGenerator.cs

private void EmitBlock(BoundBlockStatement block)
{
    // IMPORTANT: Do NOT emit a sequence point for the block itself!
    // Blocks are synthetic and often have null Syntax.
    // Only emit sequence points for individual statements inside.
    
    foreach (var statement in block.Statements)
    {
        EmitStatement(statement);
    }
}
```

---

## Issue 2: Control Flow Granularity

**Risk:** `BoundIfStatement.Syntax` spans from `O RLY?` to `OIC`, which can be hundreds of lines. Emitting a sequence point for the entire span causes the debugger to highlight the entire IF block as "the current line".

**Root Cause:** The syntax span is too coarse for debugging granularity.

**Fix:**

### 2a. Emit Sequence Point for Header Token Only

```csharp
// File: CodeGen/CodeGenerator.cs

private void EmitIfStatement(BoundIfStatement ifStatement)
{
    // Emit a sequence point for the condition evaluation
    // Use ONLY the "O RLY?" keyword span, not the entire if block
    if (ifStatement.Syntax != null)
    {
        var headerSpan = ifStatement.Syntax.ORlyKeyword.Span;  // Just the keyword
        EmitSequencePoint(headerSpan, ilGenerator: _il);
    }
    
    // Evaluate condition
    EmitExpression(ifStatement.Condition);
    
    // ... rest of if logic ...
}

private void EmitLoopStatement(BoundLoopStatement loopStatement)
{
    // Emit sequence point for loop header only (IM IN YR keyword)
    if (loopStatement.Syntax != null)
    {
        var headerSpan = loopStatement.Syntax.LoopHeaderSpan;  // Just header
        EmitSequencePoint(headerSpan, ilGenerator: _il);
    }
    
    // ... rest of loop logic ...
}

private void EmitSwitchStatement(BoundSwitchStatement switchStatement)
{
    // Emit sequence point for WTF? keyword only
    if (switchStatement.Syntax != null)
    {
        var headerSpan = switchStatement.Syntax.WtfKeyword.Span;
        EmitSequencePoint(headerSpan, ilGenerator: _il);
    }
    
    // ... rest of switch logic ...
}
```

### 2b. Helper Method for Sequence Point Emission

```csharp
// File: CodeGen/CodeGenerator.cs

/// <summary>
/// Emits a sequence point at the current IL position for the given source span.
/// </summary>
private void EmitSequencePoint(TextSpan span, ILGenerator ilGenerator)
{
    // This requires MethodDebugInformation and SequencePoint collections.
    // Pseudocode for what must be called:
    // ilGenerator.MarkSequencePoint(startLine, startCol, endLine, endCol);
    
    // TODO: Implement with actual ILGenerator.MarkSequencePoint calls
    // See: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit.ilgenerator.marksequencepoint
}
```

---

## Issue 3: Switch Cases (OMG Clauses)

**Risk:** `BoundOmgClause` and `BoundMebbeClause` are not `BoundNode`s and have no `Syntax` property. The debugger cannot step into a specific `OMG "value"` case.

**Root Cause:** Clause types are lightweight containers without syntax references.

**Fix:**

### 3a. Add Syntax Properties to Clause Types

```csharp
// File: BoundTree/BoundNodes.cs

/// <summary>Bound MEBBE clause.</summary>
internal sealed class BoundMebbeClause
{
    public BoundExpression Condition { get; }
    public BoundBlockStatement Body { get; }
    public MebbeClauseSyntax? Syntax { get; }  // NEW
    
    public BoundMebbeClause(
        BoundExpression condition, 
        BoundBlockStatement body,
        MebbeClauseSyntax? syntax = null)  // NEW parameter
    {
        Condition = condition;
        Body = body;
        Syntax = syntax;  // NEW
    }
}

/// <summary>Bound OMG clause.</summary>
internal sealed class BoundOmgClause
{
    public object? LiteralValue { get; }
    public BoundBlockStatement Body { get; }
    public OmgClauseSyntax? Syntax { get; }  // NEW
    
    public BoundOmgClause(
        object? literalValue, 
        BoundBlockStatement body,
        OmgClauseSyntax? syntax = null)  // NEW parameter
    {
        LiteralValue = literalValue;
        Body = body;
        Syntax = syntax;  // NEW
    }
}
```

### 3b. Update Binder to Capture Clause Syntax

```csharp
// File: Binding/Binder.cs

private BoundSwitchStatement BindSwitchStatement(SwitchStatementSyntax syntax)
{
    var omgClauses = ImmutableArray.CreateBuilder<BoundOmgClause>();
    foreach (var omgSyntax in syntax.OmgClauses)
    {
        var value = EvaluateLiteral(omgSyntax.Value);
        var body = BindBlockStatement(omgSyntax.Body);
        // NEW: Pass syntax to constructor
        omgClauses.Add(new BoundOmgClause(value, body, omgSyntax));
    }
    
    BoundBlockStatement? defaultBlock = null;
    if (syntax.DefaultBlock != null)
        defaultBlock = BindBlockStatement(syntax.DefaultBlock);
    
    return new BoundSwitchStatement(
        omgClauses.ToImmutable(), 
        defaultBlock,
        syntax);
}
```

### 3c. CodeGenerator: Emit Sequence Points for Each Case

```csharp
// File: CodeGen/CodeGenerator.cs

private void EmitSwitchStatement(BoundSwitchStatement switchStatement)
{
    // Emit header sequence point
    if (switchStatement.Syntax != null)
        EmitSequencePoint(switchStatement.Syntax.WtfKeyword.Span, _il);
    
    // Evaluate switched expression...
    
    var caseLabels = new List<Label>();
    
    // Emit sequence point for each OMG clause
    foreach (var omgClause in switchStatement.OmgClauses)
    {
        var label = _il.DefineLabel();
        caseLabels.Add(label);
        
        // Mark the case location in debug info
        if (omgClause.Syntax != null)
        {
            EmitSequencePoint(omgClause.Syntax.OmgKeyword.Span, _il);
        }
        
        _il.MarkLabel(label);
        EmitBlock(omgClause.Body);
    }
    
    // Handle default case
    if (switchStatement.DefaultBlock != null)
    {
        var defaultLabel = _il.DefineLabel();
        _il.MarkLabel(defaultLabel);
        EmitBlock(switchStatement.DefaultBlock);
    }
}
```

---

## Issue 4: Function Boundaries

**Risk:** Stepping into a function lands on the first statement (no entry point). Stepping out or reaching implicit "return IT" has no debug location.

**Root Cause:** No sequence point at function entry or implicit return.

**Fix:**

### 4a. Emit Sequence Point at Function Entry

```csharp
// File: CodeGen/CodeGenerator.cs

private void EmitFunction(BoundFunctionDeclaration functionDecl)
{
    var method = _methods[functionDecl.Function.Name];
    _il = method.GetILGenerator();
    
    // Set up local variables, parameters, etc.
    // ...
    
    // NEW: Emit sequence point at function entry (HOW IZ I keyword)
    if (functionDecl.Syntax != null)
    {
        EmitSequencePoint(functionDecl.Syntax.HowIzIKeyword.Span, _il);
    }
    
    // Emit function body
    EmitBlock(functionDecl.Body);
    
    // NEW: Emit sequence point for implicit return (IF U SAY SO keyword)
    if (functionDecl.Syntax != null)
    {
        EmitSequencePoint(functionDecl.Syntax.IfUSaySoKeyword.Span, _il);
    }
    
    // Load IT and return
    _il.Emit(OpCodes.Ldloc, _itLocal);
    _il.Emit(OpCodes.Ret);
}
```

### 4b. Verify Syntax Node Properties Exist

Ensure `FunctionDeclarationSyntax` has these properties:

```csharp
// File: Syntax/SyntaxNodes.cs

public sealed class FunctionDeclarationSyntax : StatementSyntax
{
    public SyntaxToken HowIzIKeyword { get; }      // "HOW IZ I"
    // ... name, params, etc. ...
    public SyntaxToken IfUSaySoKeyword { get; }    // "IF U SAY SO"
    
    public FunctionDeclarationSyntax(
        SyntaxToken howIzIKeyword,
        // ... other parameters ...
        SyntaxToken ifUSaySoKeyword)
    {
        HowIzIKeyword = howIzIKeyword;
        // ...
        IfUSaySoKeyword = ifUSaySoKeyword;
    }
}
```

---

## Issue 5: Implicit Nodes (Expressions)

**Risk:** Expressions created during lowering (implicit casts, string concatenations) have `Syntax = null`. If `EmitExpression` blindly emits a sequence point, it will crash.

**Root Cause:** Implicit expressions are synthetic and have no source location.

**Fix:**

### 5a. Guard Sequence Point Emission in EmitExpression

```csharp
// File: CodeGen/CodeGenerator.cs

private void EmitExpression(BoundExpression expression)
{
    // IMPORTANT: Only emit sequence points for top-level expression statements,
    // not for sub-expressions. Sub-expressions have no meaningful location.
    
    // If this is a top-level statement, the caller (EmitExpressionStatement) 
    // will emit the sequence point using the statement's syntax.
    
    // For expressions with null Syntax, emit no sequence point.
    // This is the expected behavior for implicit/synthetic expressions.
    
    switch (expression.Kind)
    {
        case BoundKind.LiteralExpression:
            EmitLiteralExpression((BoundLiteralExpression)expression);
            break;
        
        case BoundKind.VariableExpression:
            EmitVariableExpression((BoundVariableExpression)expression);
            break;
        
        // ... other cases ...
        
        default:
            throw new ArgumentException($"Unknown expression kind: {expression.Kind}");
    }
}

private void EmitExpressionStatement(BoundExpressionStatement statement)
{
    // Top-level expression statements DO get sequence points.
    // The sequence point location is the entire statement line.
    
    // TODO: Emit sequence point using statement.Syntax
    // (This requires capturing Syntax in BoundExpressionStatement)
    
    EmitExpression(statement.Expression);
    // Result stays in stack for potential IT assignment
}
```

### 5b. Ensure BoundExpressionStatement Captures Syntax

```csharp
// File: BoundTree/BoundNodes.cs

/// <summary>Expression statement (sets IT).</summary>
internal sealed class BoundExpressionStatement : BoundStatement
{
    public BoundExpression Expression { get; }
    public ExpressionStatementSyntax? Syntax { get; }  // NEW
    
    public BoundExpressionStatement(
        BoundExpression expression,
        ExpressionStatementSyntax? syntax = null)  // NEW parameter
    {
        Expression = expression;
        Syntax = syntax;  // NEW
    }
    public override BoundKind Kind => BoundKind.ExpressionStatement;
    
    // Override base Syntax property
    public override SyntaxNode? Syntax { get; }  // Returns syntax of the statement
}
```

---

## Issue 6: Test Cleanup

**Risk:** `.pdb` files remain in the temp directory after tests complete, locking assemblies or cluttering the file system.

**Root Cause:** `EndToEndTestBase.Dispose()` only deletes the temp directory; PDB files may be locked or remain.

**Fix:**

### 6a. Enhance Test Cleanup

```csharp
// File: tests/Lolcode.EndToEnd.Tests/EndToEndTestBase.cs

public void Dispose()
{
    try 
    { 
        // Delete all .dll, .pdb, and .runtimeconfig.json files first
        var files = Directory.GetFiles(_tempDir, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file);
            if (ext == ".dll" || ext == ".pdb" || ext == ".runtimeconfig.json")
            {
                try { File.Delete(file); } 
                catch { /* Log or ignore */ }
            }
        }
        
        // Then delete the entire directory
        Directory.Delete(_tempDir, recursive: true); 
    } 
    catch 
    { 
        /* best effort */ 
    }
}
```

### 6b. Explicit PDB Cleanup in CompileAndRun

```csharp
// File: tests/Lolcode.EndToEnd.Tests/EndToEndTestBase.cs

protected string CompileAndRun(string source, string? stdin = null)
{
    var tree = SyntaxTree.ParseText(source, "test.lol");
    string outputPath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.dll");
    var compilation = LolcodeCompilation.Create(tree);
    var result = compilation.Emit(outputPath, _runtimeDll);

    if (!result.Success)
    {
        var errors = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
        throw new InvalidOperationException($"Compilation failed:\n{errors}");
    }

    string runtimeDest = Path.Combine(Path.GetDirectoryName(result.OutputPath!)!, "Lolcode.Runtime.dll");
    if (!File.Exists(runtimeDest))
        File.Copy(_runtimeDll, runtimeDest, overwrite: true);

    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = result.OutputPath!,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        RedirectStandardInput = stdin != null,
    };

    using var process = Process.Start(psi)!;

    if (stdin != null)
    {
        process.StandardInput.Write(stdin);
        process.StandardInput.Close();
    }

    string output = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();
    process.WaitForExit(10000);

    if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
        throw new InvalidOperationException($"Runtime error (exit code {process.ExitCode}):\n{error}");

    // NEW: Cleanup assembly artifacts
    try
    {
        var dllDir = Path.GetDirectoryName(result.OutputPath!);
        var pdbPath = Path.Combine(dllDir!, Path.GetFileNameWithoutExtension(result.OutputPath!) + ".pdb");
        var runtimeConfigPath = Path.Combine(dllDir!, Path.GetFileNameWithoutExtension(result.OutputPath!) + ".runtimeconfig.json");
        
        if (File.Exists(pdbPath)) File.Delete(pdbPath);
        if (File.Exists(runtimeConfigPath)) File.Delete(runtimeConfigPath);
        if (File.Exists(result.OutputPath!)) File.Delete(result.OutputPath!);
    }
    catch { /* best effort */ }

    return output;
}
```

---

## Summary of Changes

| Issue | Type | Bound Node Changes | CodeGenerator Changes | Test Changes |
|-------|------|-------------------|----------------------|--------------|
| 1. Block Syntax | Structural | Add `Syntax` to `BoundNode`, `BoundIfStatement`, `BoundSwitchStatement`, `BoundLoopStatement`, `BoundFunctionDeclaration` | Guard `EmitBlock` to never emit sequence point | - |
| 2. Control Flow Granularity | Logic | Update constructors in Binder to pass syntax | Emit sequence points for header tokens only, not entire constructs | - |
| 3. Switch Cases | Structural | Add `Syntax` to `BoundOmgClause`, `BoundMebbeClause` | Emit sequence point for each `OMG` clause in `EmitSwitchStatement` | - |
| 4. Function Boundaries | Logic | Add `Syntax` to `BoundFunctionDeclaration` | Emit at entry (`HOW IZ I`) and exit (`IF U SAY SO`) | - |
| 5. Implicit Nodes | Logic | Add `Syntax` to `BoundExpressionStatement` | Guard `EmitExpression` to handle null Syntax | - |
| 6. Test Cleanup | Maintenance | - | - | Enhanced `Dispose()` and `CompileAndRun()` cleanup |

---

## Implementation Priority

1. **High Priority:** Issues 1, 3, 4 (Structural changes to BoundNodes and Syntax capture)
2. **Medium Priority:** Issues 2, 5 (CodeGenerator sequence point logic)
3. **Low Priority:** Issue 6 (Test cleanup, doesn't affect functionality)

## Testing Strategy

After implementing these fixes:
1. Run existing unit tests to ensure no regressions
2. Use a debugger (Visual Studio, VS Code) to:
   - Step into functions and verify entry breakpoint
   - Step through if/switch/loop statements and verify header breakpoint
   - Step into individual switch cases
   - Verify implicit return location at function exit
3. Create dedicated PDB tests that inspect `DebugInformation` metadata
