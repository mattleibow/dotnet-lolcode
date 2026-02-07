# PDB Implementation Fixes - Quick Checklist

## Phase 1: Structural Changes to BoundNodes

### 1.1 Add Syntax to BoundNode Base Class
- [ ] Add `public virtual SyntaxNode? Syntax => null;` to `BoundNode`
- [ ] Import `SyntaxNode` from `Lolcode.CodeAnalysis.Syntax`

### 1.2 Update BoundIfStatement
- [ ] Add `public IfStatementSyntax? Syntax { get; }`
- [ ] Add `IfStatementSyntax? syntax = null` parameter to constructor
- [ ] Update Binder: Pass syntax when creating `BoundIfStatement`

### 1.3 Update BoundSwitchStatement
- [ ] Add `public SwitchStatementSyntax? Syntax { get; }`
- [ ] Add `SwitchStatementSyntax? syntax = null` parameter to constructor
- [ ] Update Binder: Pass syntax when creating `BoundSwitchStatement`

### 1.4 Update BoundLoopStatement
- [ ] Add `public LoopStatementSyntax? Syntax { get; }`
- [ ] Add `LoopStatementSyntax? syntax = null` parameter to constructor
- [ ] Update Binder: Pass syntax when creating `BoundLoopStatement`

### 1.5 Update BoundFunctionDeclaration
- [ ] Add `public FunctionDeclarationSyntax? Syntax { get; }`
- [ ] Add `FunctionDeclarationSyntax? syntax = null` parameter to constructor
- [ ] Update Binder: Pass syntax when creating `BoundFunctionDeclaration`

### 1.6 Update BoundExpressionStatement
- [ ] Add `public ExpressionStatementSyntax? Syntax { get; }`
- [ ] Add `ExpressionStatementSyntax? syntax = null` parameter to constructor
- [ ] Update Binder: Pass syntax when creating `BoundExpressionStatement`

---

## Phase 2: Clause Syntax References

### 2.1 Update BoundOmgClause
- [ ] Add `public OmgClauseSyntax? Syntax { get; }`
- [ ] Add `OmgClauseSyntax? syntax = null` parameter to constructor
- [ ] Note: `BoundOmgClause` is NOT a `BoundNode`, so no base override needed
- [ ] Update Binder: Pass syntax when creating `BoundOmgClause`

### 2.2 Update BoundMebbeClause
- [ ] Add `public MebbeClauseSyntax? Syntax { get; }`
- [ ] Add `MebbeClauseSyntax? syntax = null` parameter to constructor
- [ ] Note: `BoundMebbeClause` is NOT a `BoundNode`, so no base override needed
- [ ] Update Binder: Pass syntax when creating `BoundMebbeClause`

---

## Phase 3: CodeGenerator Sequence Point Logic

### 3.1 Create EmitSequencePoint Helper
- [ ] Add method: `private void EmitSequencePoint(TextSpan span, ILGenerator il)`
- [ ] Research: Determine how to call `ILGenerator.MarkSequencePoint()` in .NET
- [ ] Research: Determine how to integrate with `DebugInformationBuilder` in `PersistedAssemblyBuilder`

### 3.2 Guard EmitBlock
- [ ] Modify `EmitBlock()` to **NEVER** emit a sequence point for the block itself
- [ ] Only emit sequence points for individual statements inside

### 3.3 Update EmitIfStatement
- [ ] Emit sequence point for `ifStatement.Syntax?.ORlyKeyword.Span` ONLY (not entire if block)
- [ ] Emit before evaluating condition

### 3.4 Update EmitLoopStatement
- [ ] Emit sequence point for `loopStatement.Syntax?.LoopHeaderSpan` ONLY
- [ ] Emit before loop body

### 3.5 Update EmitSwitchStatement
- [ ] Emit sequence point for `switchStatement.Syntax?.WtfKeyword.Span` for header
- [ ] For each `BoundOmgClause`: Emit sequence point for `clause.Syntax?.OmgKeyword.Span`
- [ ] Mark IL labels after sequence points

### 3.6 Update EmitFunction
- [ ] Emit sequence point at entry: `functionDecl.Syntax?.HowIzIKeyword.Span`
- [ ] Emit sequence point at exit: `functionDecl.Syntax?.IfUSaySoKeyword.Span`
- [ ] Place exit sequence point **before** implicit return

### 3.7 Guard EmitExpression
- [ ] Ensure no sequence point is emitted for sub-expressions
- [ ] Only `EmitExpressionStatement` should emit sequence point for top-level expression

---

## Phase 4: Verify Syntax Node Properties

### 4.1 Check FunctionDeclarationSyntax
- [ ] Verify `FunctionDeclarationSyntax.HowIzIKeyword` exists
- [ ] Verify `FunctionDeclarationSyntax.IfUSaySoKeyword` exists
- [ ] If missing, add them to `SyntaxNodes.cs`

### 4.2 Check IfStatementSyntax
- [ ] Verify `IfStatementSyntax.ORlyKeyword` exists

### 4.3 Check LoopStatementSyntax
- [ ] Verify header span property exists (may be `LoopHeaderSpan` or derived from tokens)

### 4.4 Check SwitchStatementSyntax
- [ ] Verify `SwitchStatementSyntax.WtfKeyword` exists

### 4.5 Check OmgClauseSyntax
- [ ] Verify `OmgClauseSyntax.OmgKeyword` exists

---

## Phase 5: Test Cleanup

### 5.1 Update EndToEndTestBase.Dispose()
- [ ] Add explicit deletion of `.pdb` files before directory delete
- [ ] Add explicit deletion of `.runtimeconfig.json` files
- [ ] Wrap in try-catch for each file

### 5.2 Update CompileAndRun()
- [ ] After process exits, delete `.pdb` file corresponding to output assembly
- [ ] Delete `.runtimeconfig.json` file
- [ ] Delete `.dll` file itself
- [ ] Wrap in try-catch with best-effort semantics

---

## Validation

### Manual Testing
- [ ] Compile a simple LOLCODE program with Portable PDB
- [ ] Load in debugger (Visual Studio, VS Code, WinDbg)
- [ ] Set breakpoint on first statement
- [ ] Verify breakpoint is hit
- [ ] Step through code and verify correct lines are highlighted
- [ ] Test function entry/exit
- [ ] Test if/switch/loop breakpoints

### Unit Tests
- [ ] Run existing test suite: No regressions
- [ ] Create new test: Verify PDB metadata contains expected sequence points
- [ ] Create new test: Verify debugger can step into functions

### Integration Tests
- [ ] End-to-end test with nested control flow
- [ ] End-to-end test with function calls
- [ ] Cleanup verification: No `.pdb` files left after tests

---

## Notes

- **Syntax import**: Ensure `using Lolcode.CodeAnalysis.Syntax;` is added to `BoundNodes.cs`
- **ILGenerator.MarkSequencePoint**: May require research into .NET 10.0 API
- **DebugInformationBuilder**: May need to update how PDB metadata is built in `Emit()`
- **Order matters**: Do Phase 1 & 2 first (structural), then Phase 3 (logic), then Phase 4 & 5 (validation)
