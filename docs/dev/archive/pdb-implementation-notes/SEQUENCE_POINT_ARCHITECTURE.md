# Portable PDB Sequence Point Architecture

## What is a Sequence Point?

A **sequence point** is a mapping in the debug information (PDB) that associates a range of IL bytecode with a source code location (file, line, column). When a debugger hits that IL instruction, it knows what source line to display.

**Critical Rule:** Sequence points should be granular enough for intuitive debugging but not so granular that they confuse the user.

---

## Design Principles

### 1. **Statement-Level, Not Expression-Level**
- Each **statement** in LOLCODE gets at most one sequence point
- Sub-expressions do NOT get sequence points (they're part of the parent statement)
- Top-level expressions (which are implicit assignment to `IT`) get one sequence point

**Why?** Expression-level sequence points create too much granularity and confuse debuggers.

### 2. **Control Flow Header, Not Body**
- IF, LOOP, SWITCH statements emit a sequence point for the **header** (condition/keyword), not the body
- The body's statements emit their own sequence points

**Why?** When you step into an IF, you want to stop at the condition, not inside the block.

### 3. **Function Boundaries Are Explicit**
- Function entry: Sequence point at `HOW IZ I` keyword
- Function exit: Sequence point at `IF U SAY SO` keyword (implicit return)
- Both are necessary for "step in" and "step out" to work correctly

**Why?** Debuggers use these boundaries to handle call stacks and step commands.

### 4. **Synthetic Nodes Have No Sequence Points**
- Nodes created during lowering (not from source syntax) should NOT emit sequence points
- Example: `BoundBlockStatement` created in `RewriteBlock` has no source location

**Why?** Emitting a sequence point for code the user didn't write causes confusion ("Why is the debugger here?")

---

## Implementation Architecture

### Control Flow for Sequence Point Emission

```
EmitStatement(BoundStatement stmt)
  ├─ For control flow statements (IF, LOOP, SWITCH):
  │  ├─ Check stmt.Syntax != null
  │  └─ EmitSequencePoint(stmt.Syntax.HeaderTokenSpan)
  │
  ├─ For expression statements:
  │  ├─ Check stmt.Syntax != null
  │  └─ EmitSequencePoint(stmt.Syntax.Span)  // Entire statement line
  │
  ├─ For other statements (variable decl, assignment, etc.):
  │  ├─ Check stmt.Syntax != null
  │  └─ EmitSequencePoint(stmt.Syntax.Span)
  │
  └─ Never emit for blocks, only for statements inside
```

### Statement Kinds and Their Sequence Points

| Statement | Sequence Point Location | Example |
|-----------|------------------------|---------|
| `BoundVariableDeclaration` | Entire statement (`I HAS A x ITZ 5`) | Yes |
| `BoundAssignment` | Entire statement (`x R y PLUS 1`) | Yes |
| `BoundVisibleStatement` | Entire statement (`VISIBLE x`) | Yes |
| `BoundGimmehStatement` | Entire statement (`GIMMEH x`) | Yes |
| `BoundExpressionStatement` | Entire statement (bare expression) | Yes |
| `BoundBlockStatement` | **NONE** (synthetic, no source) | No |
| `BoundIfStatement` | **Header only** (`O RLY?` keyword) | Yes, header only |
| `BoundLoopStatement` | **Header only** (`IM IN YR` keyword) | Yes, header only |
| `BoundSwitchStatement` | **Header only** (`WTF?` keyword) | Yes, header only |
| `BoundOmgClause` | **Clause header only** (`OMG "value"` keyword) | Yes, header only |
| `BoundMebbeClause` | **Clause header only** (`MEBBE` keyword) | Yes, header only |
| `BoundGtfoStatement` | Entire statement (`GTFO`) | Yes |
| `BoundReturnStatement` | Entire statement (`GTFO WITH value`) | Yes |
| `BoundFunctionDeclaration` | **Both entry and exit** (`HOW IZ I` and `IF U SAY SO`) | Yes, both |

---

## Pseudocode for EmitSequencePoint

```csharp
private void EmitSequencePoint(TextSpan sourceSpan, ILGenerator il)
{
    if (sourceSpan.IsEmpty || _sourceText == null)
        return;  // Skip if no valid source location
    
    // Convert TextSpan to line/column
    var (startLine, startCol) = _sourceText.GetLineAndColumn(sourceSpan.Start);
    var (endLine, endCol) = _sourceText.GetLineAndColumn(sourceSpan.End);
    
    // Emit IL instruction to mark this source location
    // NOTE: This requires integration with DebugInformationBuilder
    //
    // For PersistedAssemblyBuilder, you may need to:
    // 1. Collect sequence points during IL generation
    // 2. Pass them to the PDB builder during Emit()
    //
    // Pseudocode (not real API):
    // il.MarkSequencePoint(startLine, startCol, endLine, endCol);
    
    _debugInfo.AddSequencePoint(il.Offset, sourceSpan);
}
```

---

## Integration with PersistedAssemblyBuilder

The tricky part: How to actually add sequence points to the PDB when using `PersistedAssemblyBuilder`.

### Current Status (Known Limitation)

`System.Reflection.Emit` has **limited PDB support** in .NET. The traditional `ILGenerator.MarkSequencePoint()` method exists, but `PersistedAssemblyBuilder` may not respect it fully or may require a different approach.

### Options to Investigate

**Option 1: Use ILGenerator.MarkSequencePoint (If Supported)**
```csharp
private SequencePointBuilder _sequencePoints = new();

private void EmitSequencePoint(TextSpan sourceSpan, ILGenerator il)
{
    var (startLine, startCol) = _sourceText.GetLineAndColumn(sourceSpan.Start);
    var (endLine, endCol) = _sourceText.GetLineAndColumn(sourceSpan.End);
    
    // Attempt to mark the current IL position
    // NOTE: This API exists in System.Reflection.Emit but may not work 
    //       with PersistedAssemblyBuilder in all .NET versions
    // il.MarkSequencePoint(...);
    
    // Fallback: Collect sequence points and apply later
    _sequencePoints.Add(new SequencePoint(
        offset: il.GetMethodBodyOffset(),
        startLine: startLine,
        startCol: startCol,
        endLine: endLine,
        endCol: endCol));
}
```

**Option 2: Use DebugInformationBuilder (Recommended)**

If `PersistedAssemblyBuilder` exposes a way to build debug information, use it:

```csharp
// Pseudocode - verify actual API
var assemblyBuilder = new PersistedAssemblyBuilder(...);
var debugBuilder = new DebugInformationBuilder(assemblyBuilder);

// During code generation, collect sequence points
_sequencePoints.Add((offset, line, col, endLine, endCol));

// After generating all IL, apply to methods
foreach (var method in _methods.Values)
{
    debugBuilder.SetMethodSequencePoints(method, _sequencePoints);
}

// When emitting
assemblyBuilder.Emit(writer, debugBuilder);
```

**Option 3: Direct PDB File Manipulation (Advanced)**

If neither option works, generate the PDB using a separate library like `SOS.Debugging.DebugInfo`:

```csharp
// After assembling, create PDB with sequence point data
var pdbBuilder = new PdbBuilder(...);
foreach (var (method, points) in _methodSequencePoints)
{
    pdbBuilder.AddMethodSequencePoints(method, points);
}
pdbBuilder.WriteToFile(pdbPath);
```

---

## Testing Sequence Points

### 1. **Metadata Inspection Test**

```csharp
[Test]
public void SequencePointsAreEmitted()
{
    var source = """
        HAI 1.2
        VISIBLE "Hello"
        KTHXBYE
        """;
    
    var compilation = CompileToAssembly(source, out var pdbPath);
    
    // Read the PDB
    using var pdbReader = new PortableExecutableReader(File.OpenRead(pdbPath));
    var debugInfo = pdbReader.ReadDebugInformation();
    
    // Assert that sequence points exist for the VISIBLE statement
    var sequencePoints = debugInfo.GetSequencePointsForMethod("Main");
    Assert.That(sequencePoints.Length, Is.GreaterThan(0));
}
```

### 2. **Debugger Breakpoint Test**

```csharp
[Test]
public void CanSetBreakpointOnIfStatement()
{
    var source = """
        HAI 1.2
        x R 5
        O RLY?
            VISIBLE "yes"
        OIC
        KTHXBYE
        """;
    
    // Compile with PDB
    var assembly = CompileToAssembly(source, out var pdbPath);
    
    // Attempt to debug and set breakpoint
    // (This requires a debugger engine, typically done manually)
    // Pseudocode:
    // var debugger = new DebuggerEngine(assembly, pdbPath);
    // debugger.SetBreakpoint("test.lol", line: 3, column: 6);  // O RLY?
    // var hitBreakpoint = debugger.Run();
    // Assert.That(hitBreakpoint.SourceLine, Is.EqualTo(3));
}
```

### 3. **No Synthetic Block Sequence Point Test**

```csharp
[Test]
public void SyntheticBlocksHaveNoSequencePoint()
{
    // A block created during lowering should have no Syntax and no sequence point
    var block = new BoundBlockStatement(ImmutableArray<BoundStatement>.Empty);
    
    Assert.That(block.Syntax, Is.Null);
    Assert.DoesNotThrow(() => EmitBlock(block));
    
    // Verify no sequence point was emitted for the block
    // (This is implicit in the codegen logic)
}
```

---

## Debugging PDB Generation

### If Sequence Points Are Not Being Emitted

1. **Check syntax capture in Binder:**
   ```csharp
   // In BindIfStatement:
   if (syntax == null)
       Debug.WriteLine("WARNING: IfStatementSyntax is null");
   ```

2. **Check EmitSequencePoint calls:**
   ```csharp
   private void EmitSequencePoint(TextSpan sourceSpan, ILGenerator il)
   {
       Debug.WriteLine($"Emitting sequence point: {sourceSpan.Start}-{sourceSpan.End}");
       // ...
   }
   ```

3. **Verify PDB is generated:**
   ```bash
   dotnet --version
   # Check if PersistedAssemblyBuilder generates .pdb files
   # Look at output directory for .pdb alongside .dll
   ```

4. **Use PDB validation tools:**
   ```bash
   # Visual Studio, VS Code, or WinDbg should read the PDB
   # Test in debugger by stepping through code
   ```

---

## Common Pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| No sequence points at all | Debugger can't step, all lines unmarked | Verify `EmitSequencePoint` is called; check PDB generation |
| Sequence points too coarse | Large blocks highlighted as single line | Emit for statement headers, not entire blocks |
| Sequence points too fine | Every sub-expression is a stop point | Only emit for top-level statements and control headers |
| Null Syntax access | NullReferenceException in EmitStatement | Guard all `.Syntax` accesses with `if (node.Syntax != null)` |
| Missing function entry/exit | Can't step in/out of functions | Emit at `HOW IZ I` and `IF U SAY SO` keywords |
| Clause sequence points missing | Can't step into switch cases | Add `Syntax` to `BoundOmgClause`, emit in loop |
| PDB not generated | No debug info at all | Verify `PersistedAssemblyBuilder.Emit()` creates `.pdb` |

---

## References

- [System.Reflection.Emit.ILGenerator.MarkSequencePoint](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit.ilgenerator.marksequencepoint)
- [Portable PDB Format](https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md)
- [Roslyn Source Link and PDB](https://github.com/dotnet/roslyn/wiki/PDB-SourceLink-Support)
- [Debugging .NET with WinDbg](https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools)
