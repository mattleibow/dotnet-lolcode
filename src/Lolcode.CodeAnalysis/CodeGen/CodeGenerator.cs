using System.Collections.Immutable;
using System.Diagnostics.SymbolStore;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Lolcode.CodeAnalysis.Binding;
using Lolcode.CodeAnalysis.BoundTree;
using Lolcode.CodeAnalysis.Symbols;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis.CodeGen;

/// <summary>
/// Generates a .NET assembly from a bound tree using PersistedAssemblyBuilder.
/// LOLCODE bindings use runtime scopes; direct bindings also receive debug shadow locals.
/// Runtime calls go through <c>Lolcode.Runtime.LolRuntime</c>.
/// </summary>
internal sealed class CodeGenerator
{
    private readonly BoundBlockStatement _boundTree;
    private readonly string _assemblyName;
    private readonly string _runtimeAssemblyPath;
    private readonly Text.SourceText? _sourceText;
    private readonly string? _sourceFilePath;
    private ISymbolDocumentWriter? _document;

    private TypeBuilder _typeBuilder = null!;
    private ILGenerator _il = null!;
    private readonly Dictionary<string, LocalBuilder> _locals = new(StringComparer.Ordinal);
    private readonly Dictionary<BoundFunctionDeclaration, MethodBuilder> _functionMethods =
        new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<FunctionSymbol, BoundFunctionDeclaration> _functionDeclarations =
        new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<BoundFunctionDeclaration, ImmutableArray<MethodBuilder>>
        _parameterResolverMethods = new(ReferenceEqualityComparer.Instance);
    private LocalBuilder _scopeLocal = null!;
    private Type _scopeType = null!;
    private Type _objectType = null!;
    private Type _functionType = null!;
    private Type _functionBodyType = null!;
    private Type _parameterNameResolverType = null!;
    private Type _functionTargetType = null!;
    private Type _identifierResolverType = null!;
    private Type _resolvedSlotType = null!;

    private readonly record struct ControlFlowTarget(Label Label, int ExceptionDepth);

    // Targets retain their protected-region depth so transfers run enclosing finally blocks.
    private readonly Stack<ControlFlowTarget> _loopBreakTargets = new();
    private readonly Stack<ControlFlowTarget> _switchBreakTargets = new();
    private ControlFlowTarget _functionReturnTarget;
    private int _exceptionDepth;
    private LocalBuilder? _functionReturnValue;

    // Runtime method references
    private MethodInfo _printMethod = null!;
    private MethodInfo _loadLibraryMethod = null!;
    private MethodInfo _executeSystemCommandMethod = null!;
    private MethodInfo _disposeScopeMethod = null!;
    private MethodInfo _writeByteOrderMarkMethod = null!;
    private MethodInfo _createYarnLiteralMethod = null!;
    private MethodInfo _interpolateYarnMethod = null!;
    private MethodInfo _readLineMethod = null!;
    private MethodInfo _addMethod = null!;
    private MethodInfo _subtractMethod = null!;
    private MethodInfo _multiplyMethod = null!;
    private MethodInfo _divideMethod = null!;
    private MethodInfo _moduloMethod = null!;
    private MethodInfo _greaterMethod = null!;
    private MethodInfo _smallerMethod = null!;
    private MethodInfo _andMethod = null!;
    private MethodInfo _orMethod = null!;
    private MethodInfo _xorMethod = null!;
    private MethodInfo _notMethod = null!;
    private MethodInfo _bothSaemMethod = null!;
    private MethodInfo _switchCaseMatchesMethod = null!;
    private MethodInfo _diffrintMethod = null!;
    private MethodInfo _smooshMethod = null!;
    private MethodInfo _isTruthyMethod = null!;
    private MethodInfo _castToYarnMethod = null!;
    private MethodInfo _castToNumbrMethod = null!;
    private MethodInfo _castToNumbarMethod = null!;
    private MethodInfo _castToTroofMethod = null!;
    private MethodInfo _explicitCastMethod = null!;
    private MethodInfo _createScopeMethod = null!;
    private MethodInfo _createChildScopeMethod = null!;
    private MethodInfo _createInvocationScopeMethod = null!;
    private MethodInfo _createObjectMethod = null!;
    private MethodInfo _invokeResolvedMethod = null!;
    private MethodInfo _resolveParameterNameMethod = null!;
    private MethodInfo _getItMethod = null!;
    private MethodInfo _setItMethod = null!;
    private MethodInfo _resolveIdentifierNameMethod = null!;
    private MethodInfo _beginIdentifierPathMethod = null!;
    private MethodInfo _prepareIdentifierSegmentMethod = null!;
    private MethodInfo _setIdentifierSegmentMethod = null!;
    private MethodInfo _resolveIdentifierSlotMethod = null!;
    private MethodInfo _resolveIdentifierNamespaceMethod = null!;
    private MethodInfo _getResolvedValueMethod = null!;
    private MethodInfo _resolveDeclarationSlotMethod = null!;
    private MethodInfo _declareResolvedValueMethod = null!;
    private MethodInfo _declareParameterMethod = null!;
    private MethodInfo _assignResolvedValueMethod = null!;
    private MethodInfo _resolveFunctionSlotMethod = null!;

    /// <summary>
    /// Creates a new emitter.
    /// </summary>
    public CodeGenerator(BoundBlockStatement boundTree, string assemblyName, string runtimeAssemblyPath,
        Text.SourceText? sourceText = null, string? sourceFilePath = null)
    {
        _boundTree = boundTree;
        _assemblyName = assemblyName;
        _runtimeAssemblyPath = runtimeAssemblyPath;
        _sourceText = sourceText;
        _sourceFilePath = sourceFilePath;
    }

    /// <summary>
    /// Emits the assembly to the specified output path.
    /// </summary>
    /// <returns>The path to the emitted DLL.</returns>
    public string Emit(string outputPath)
    {
        var runtimeAssembly = Assembly.LoadFrom(_runtimeAssemblyPath);
        var runtimeType = runtimeAssembly.GetType("Lolcode.Runtime.LolRuntime")
            ?? throw new InvalidOperationException("Could not find LolRuntime type");
        _scopeType = runtimeAssembly.GetType("Lolcode.Runtime.LolScope")!;
        _objectType = runtimeAssembly.GetType("Lolcode.Runtime.LolObject")!;
        _functionType = runtimeAssembly.GetType("Lolcode.Runtime.LolFunction")!;
        _functionBodyType = runtimeAssembly.GetType("Lolcode.Runtime.LolFunctionBody")!;
        _parameterNameResolverType =
            runtimeAssembly.GetType("Lolcode.Runtime.LolParameterNameResolver")!;
        _functionTargetType = runtimeAssembly.GetType("Lolcode.Runtime.LolFunctionTarget")!;
        _identifierResolverType = runtimeAssembly.GetType("Lolcode.Runtime.LolIdentifierResolver")!;
        _resolvedSlotType = runtimeAssembly.GetType("Lolcode.Runtime.LolResolvedSlot")!;

        ResolveRuntimeMethods(runtimeType);

        var assemblyBuilder = new PersistedAssemblyBuilder(
            new AssemblyName(_assemblyName),
            typeof(object).Assembly);

        var moduleBuilder = assemblyBuilder.DefineDynamicModule(_assemblyName);

        // PDB: define document for source file
        if (_sourceText != null && !string.IsNullOrEmpty(_sourceFilePath))
        {
            var lolcodeLanguageGuid = new Guid("4C4F4C43-4F44-4500-0000-000000000001");
            _document = moduleBuilder.DefineDocument(
                Path.GetFullPath(_sourceFilePath), lolcodeLanguageGuid,
                SymLanguageVendor.Microsoft, SymDocumentType.Text);
        }

        _typeBuilder = moduleBuilder.DefineType(
            "Program",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed);

        int functionIndex = 0;
        var emittedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var funcDecl in EnumerateFunctions(_boundTree))
        {
            string preferredName =
                funcDecl.Scope?.DirectName == "I" &&
                funcDecl.Scope.Slot is null &&
                funcDecl.Identifier?.DirectName is { } directName &&
                funcDecl.Identifier.Slot is null
                    ? directName
                    : $"__lol_function_{functionIndex}";
            string emittedName = emittedNames.Add(preferredName)
                ? preferredName
                : $"__lol_function_{functionIndex}_{preferredName}";
            functionIndex++;
            var method = _typeBuilder.DefineMethod(
                emittedName,
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(object),
                [_scopeType, _objectType, typeof(object[]), _resolvedSlotType.MakeArrayType()]);
            _functionMethods[funcDecl] = method;
            _functionDeclarations[funcDecl.Function] = funcDecl;
            var parameterResolvers = ImmutableArray.CreateBuilder<MethodBuilder>();
            for (int parameterIndex = 0;
                 parameterIndex < funcDecl.ParameterIdentifiers.Length;
                 parameterIndex++)
            {
                parameterResolvers.Add(_typeBuilder.DefineMethod(
                    $"__lol_parameter_{functionIndex}_{parameterIndex}",
                    MethodAttributes.Private | MethodAttributes.Static,
                    _resolvedSlotType,
                    [_scopeType]));
            }
            _parameterResolverMethods[funcDecl] = parameterResolvers.ToImmutable();
        }

        // Define Main entry point
        var mainMethod = _typeBuilder.DefineMethod(
            "Main",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            typeof(void),
            Type.EmptyTypes);

        // Emit function bodies
        foreach (var pair in _functionMethods)
            EmitFunction(pair.Value, pair.Key);
        foreach (var pair in _parameterResolverMethods)
        {
            for (int index = 0; index < pair.Value.Length; index++)
                EmitParameterResolver(pair.Value[index], pair.Key.ParameterIdentifiers[index]);
        }

        // Emit Main body
        _il = mainMethod.GetILGenerator();
        _locals.Clear();

        _il.BeginScope();

        if (_sourceText is { Length: > 0 } && _sourceText[0] == '\uFEFF')
            _il.Emit(OpCodes.Call, _writeByteOrderMarkMethod);

        _scopeLocal = _il.DeclareLocal(_scopeType);
        _il.Emit(OpCodes.Call, _createScopeMethod);
        _il.Emit(OpCodes.Stloc, _scopeLocal);
        var mainIt = _il.DeclareLocal(typeof(object));
        _locals["IT"] = mainIt;
        SetLocalSymInfo(mainIt, "IT");
        _il.Emit(OpCodes.Ldnull);
        _il.Emit(OpCodes.Stloc, mainIt);

        _il.BeginExceptionBlock();
        foreach (var statement in _boundTree.Statements)
            EmitStatement(statement);
        _il.BeginFinallyBlock();
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        _il.Emit(OpCodes.Call, _disposeScopeMethod);
        _il.EndExceptionBlock();

        _il.EndScope();
        _il.Emit(OpCodes.Ret);

        _typeBuilder.CreateType();

        // Save assembly with PDB
        var dllPath = outputPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? outputPath
            : Path.ChangeExtension(outputPath, ".dll");
        Directory.CreateDirectory(Path.GetDirectoryName(dllPath) ?? ".");

        var metadataBuilder = assemblyBuilder.GenerateMetadata(out var ilStream, out var mappedFieldData, out MetadataBuilder pdbBuilder);
        var entryPointHandle = MetadataTokens.MethodDefinitionHandle(mainMethod.MetadataToken);

        string? pdbPath = null;

        if (_document != null)
        {
            try
            {
                pdbPath = Path.ChangeExtension(dllPath, ".pdb");

                // Serialize PDB first (need BlobContentId for PE debug directory)
                var portablePdbBlob = new BlobBuilder();
                var portablePdbBuilder = new PortablePdbBuilder(
                    pdbBuilder, metadataBuilder.GetRowCounts(), entryPointHandle,
                    idProvider: content =>
                    {
                        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                        foreach (var blob in content)
                            hasher.AppendData(blob.GetBytes().Array!, blob.GetBytes().Offset, blob.GetBytes().Count);
                        return BlobContentId.FromHash(hasher.GetHashAndReset());
                    });
                BlobContentId pdbContentId = portablePdbBuilder.Serialize(portablePdbBlob);

                using (var pdbStream = File.Create(pdbPath))
                    portablePdbBlob.WriteContentTo(pdbStream);

                // Build PE with debug info
                var debugDirectoryBuilder = new DebugDirectoryBuilder();
                debugDirectoryBuilder.AddCodeViewEntry(
                    Path.GetFileName(pdbPath), pdbContentId, portablePdbBuilder.FormatVersion);

                var peBuilder = new ManagedPEBuilder(
                    header: new PEHeaderBuilder(
                        imageCharacteristics: Characteristics.ExecutableImage,
                        subsystem: Subsystem.WindowsCui),
                    metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                    ilStream: ilStream,
                    mappedFieldData: mappedFieldData,
                    debugDirectoryBuilder: debugDirectoryBuilder,
                    entryPoint: entryPointHandle);

                var peBlob = new BlobBuilder();
                peBuilder.Serialize(peBlob);

                using (var fs = new FileStream(dllPath, FileMode.Create, FileAccess.Write))
                    peBlob.WriteContentTo(fs);
            }
            catch
            {
                // PDB failed — fall back to DLL without debug info
                pdbPath = null;
                var peBuilder = new ManagedPEBuilder(
                    header: new PEHeaderBuilder(
                        imageCharacteristics: Characteristics.ExecutableImage,
                        subsystem: Subsystem.WindowsCui),
                    metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                    ilStream: ilStream,
                    mappedFieldData: mappedFieldData,
                    entryPoint: entryPointHandle);

                var peBlob = new BlobBuilder();
                peBuilder.Serialize(peBlob);

                using (var fs = new FileStream(dllPath, FileMode.Create, FileAccess.Write))
                    peBlob.WriteContentTo(fs);
            }
        }
        else
        {
            // No PDB requested — emit without debug info
            var peBuilder = new ManagedPEBuilder(
                header: new PEHeaderBuilder(
                    imageCharacteristics: Characteristics.ExecutableImage,
                    subsystem: Subsystem.WindowsCui),
                metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                ilStream: ilStream,
                mappedFieldData: mappedFieldData,
                entryPoint: entryPointHandle);

            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);

            using (var fs = new FileStream(dllPath, FileMode.Create, FileAccess.Write))
                peBlob.WriteContentTo(fs);
        }

        // Also write runtime config
        WriteRuntimeConfig(dllPath);

        return dllPath;
    }

    private void ResolveRuntimeMethods(Type runtimeType)
    {
        _printMethod = runtimeType.GetMethod(
            "Print",
            [typeof(object[]), typeof(bool), typeof(bool)])!;
        _loadLibraryMethod = runtimeType.GetMethod("LoadLibrary")!;
        _executeSystemCommandMethod = runtimeType.GetMethod("ExecuteSystemCommandValue")!;
        _disposeScopeMethod = runtimeType.GetMethod("DisposeScope")!;
        _writeByteOrderMarkMethod = runtimeType.GetMethod("WriteByteOrderMark")!;
        _createYarnLiteralMethod = runtimeType.GetMethod("CreateYarnLiteral")!;
        _interpolateYarnMethod = runtimeType.GetMethod("InterpolateYarnValue")!;
        _readLineMethod = runtimeType.GetMethod("ReadLine")!;
        _addMethod = runtimeType.GetMethod("Add")!;
        _subtractMethod = runtimeType.GetMethod("Subtract")!;
        _multiplyMethod = runtimeType.GetMethod("Multiply")!;
        _divideMethod = runtimeType.GetMethod("Divide")!;
        _moduloMethod = runtimeType.GetMethod("Modulo")!;
        _greaterMethod = runtimeType.GetMethod("Greater")!;
        _smallerMethod = runtimeType.GetMethod("Smaller")!;
        _andMethod = runtimeType.GetMethod("And")!;
        _orMethod = runtimeType.GetMethod("Or")!;
        _xorMethod = runtimeType.GetMethod("Xor")!;
        _notMethod = runtimeType.GetMethod("Not")!;
        _bothSaemMethod = runtimeType.GetMethod("BothSaem")!;
        _switchCaseMatchesMethod = runtimeType.GetMethod("SwitchCaseMatches")!;
        _diffrintMethod = runtimeType.GetMethod("Diffrint")!;
        _smooshMethod = runtimeType.GetMethod("SmooshValue")!;
        _isTruthyMethod = runtimeType.GetMethod("IsTruthy")!;
        _castToYarnMethod = runtimeType.GetMethod("CastToYarn")!;
        _castToNumbrMethod = runtimeType.GetMethod("CastToNumbr")!;
        _castToNumbarMethod = runtimeType.GetMethod("CastToNumbar")!;
        _castToTroofMethod = runtimeType.GetMethod("CastToTroof")!;
        _explicitCastMethod = runtimeType.GetMethod("ExplicitCast")!;
        _createScopeMethod = runtimeType.GetMethod("CreateScope")!;
        _createChildScopeMethod = runtimeType.GetMethod("CreateChildScope")!;
        _createInvocationScopeMethod = runtimeType.GetMethod("CreateInvocationScope")!;
        _createObjectMethod = runtimeType.GetMethod("CreateObject")!;
        _invokeResolvedMethod = runtimeType.GetMethod("InvokeResolved")!;
        _resolveParameterNameMethod = runtimeType.GetMethod("ResolveParameterName")!;
        _getItMethod = runtimeType.GetMethod("GetIt")!;
        _setItMethod = runtimeType.GetMethod("SetIt")!;
        _resolveIdentifierNameMethod = runtimeType.GetMethod("ResolveIdentifierName")!;
        _beginIdentifierPathMethod = runtimeType.GetMethod("BeginIdentifierPath")!;
        _prepareIdentifierSegmentMethod = runtimeType.GetMethod("PrepareIdentifierSegment")!;
        _setIdentifierSegmentMethod = runtimeType.GetMethod("SetIdentifierSegment")!;
        _resolveIdentifierSlotMethod = runtimeType.GetMethod("ResolveIdentifierSlot")!;
        _resolveIdentifierNamespaceMethod = runtimeType.GetMethod("ResolveIdentifierNamespace")!;
        _getResolvedValueMethod = runtimeType.GetMethod("GetResolvedValue")!;
        _resolveDeclarationSlotMethod = runtimeType.GetMethod("ResolveDeclarationSlot")!;
        _declareResolvedValueMethod = runtimeType.GetMethod("DeclareResolvedValue")!;
        _declareParameterMethod = runtimeType.GetMethod("DeclareParameter")!;
        _assignResolvedValueMethod = runtimeType.GetMethod("AssignResolvedValue")!;
        _resolveFunctionSlotMethod = runtimeType.GetMethod("ResolveFunctionSlot")!;
    }

    private static IEnumerable<BoundFunctionDeclaration> EnumerateFunctions(BoundBlockStatement block)
    {
        foreach (BoundStatement statement in block.Statements)
        {
            if (statement is BoundFunctionDeclaration declaration)
            {
                yield return declaration;
                foreach (var function in EnumerateFunctions(declaration.Body))
                    yield return function;
            }

            BoundBlockStatement? nested = statement switch
            {
                BoundObjectDefinition definition => definition.Body,
                BoundLoopStatement loop => loop.Body,
                _ => null,
            };
            if (nested is not null)
                foreach (var function in EnumerateFunctions(nested))
                    yield return function;

            if (statement is BoundIfStatement conditional)
            {
                foreach (var function in EnumerateFunctions(conditional.ThenBlock))
                    yield return function;
                foreach (var clause in conditional.MebbeClauses)
                    foreach (var function in EnumerateFunctions(clause.Body))
                        yield return function;
                if (conditional.ElseBlock is not null)
                    foreach (var function in EnumerateFunctions(conditional.ElseBlock))
                        yield return function;
            }
            else if (statement is BoundSwitchStatement @switch)
            {
                foreach (var clause in @switch.OmgClauses)
                    foreach (var function in EnumerateFunctions(clause.Body))
                        yield return function;
                if (@switch.DefaultBlock is not null)
                    foreach (var function in EnumerateFunctions(@switch.DefaultBlock))
                        yield return function;
            }
        }
    }

    private void EmitFunction(MethodBuilder method, BoundFunctionDeclaration decl)
    {
        _il = method.GetILGenerator();
        _locals.Clear();

        _il.BeginScope();
        EmitSequencePoint(decl);
        _il.Emit(OpCodes.Nop);

        _scopeLocal = _il.DeclareLocal(_scopeType);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldarg_1);
        _il.Emit(OpCodes.Call, _createInvocationScopeMethod);
        _il.Emit(OpCodes.Stloc, _scopeLocal);
        var functionIt = _il.DeclareLocal(typeof(object));
        _locals["IT"] = functionIt;
        SetLocalSymInfo(functionIt, "IT");
        _il.Emit(OpCodes.Ldnull);
        _il.Emit(OpCodes.Stloc, functionIt);

        // Parameters are accessible by name
        for (int i = 0; i < decl.Function.Parameters.Length; i++)
        {
            var parameterLocal = _il.DeclareLocal(typeof(object));
            _locals[decl.Function.Parameters[i].Name] = parameterLocal;
            SetLocalSymInfo(parameterLocal, decl.Function.Parameters[i].Name);
            _il.Emit(OpCodes.Ldloc, _scopeLocal);
            _il.Emit(OpCodes.Ldarg_3);
            _il.Emit(OpCodes.Ldc_I4, i);
            _il.Emit(OpCodes.Ldelem_Ref);
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Ldc_I4, i);
            _il.Emit(OpCodes.Ldelem_Ref);
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, parameterLocal);
            _il.Emit(OpCodes.Call, _declareParameterMethod);
        }

        // Return handling
        _functionReturnTarget = new ControlFlowTarget(_il.DefineLabel(), _exceptionDepth);
        _functionReturnValue = _il.DeclareLocal(typeof(object));
        _il.Emit(OpCodes.Ldnull);
        _il.Emit(OpCodes.Stloc, _functionReturnValue);

        foreach (var statement in decl.Body.Statements)
            EmitStatement(statement);

        // If no FOUND YR was executed, return IT by default
        _il.EndScope();
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        _il.Emit(OpCodes.Call, _getItMethod);
        _il.Emit(OpCodes.Stloc, _functionReturnValue);

        _il.MarkLabel(_functionReturnTarget.Label);
        _il.Emit(OpCodes.Ldloc, _functionReturnValue);
        _il.Emit(OpCodes.Ret);
    }

    private void EmitStatement(BoundStatement statement)
    {
        switch (statement)
        {
            case BoundVariableDeclaration s:
                EmitSequencePoint(s);
                EmitVariableDeclaration(s);
                break;
            case BoundAssignment s:
                EmitSequencePoint(s);
                EmitAssignment(s);
                break;
            case BoundVisibleStatement s:
                EmitSequencePoint(s);
                EmitVisible(s);
                break;
            case BoundGimmehStatement s:
                EmitSequencePoint(s);
                EmitGimmeh(s);
                break;
            case BoundExpressionStatement s:
                EmitSequencePoint(s);
                EmitExpressionStatement(s);
                break;
            case BoundIfStatement s:
                if (s.Syntax is IfStatementSyntax ifSyntax)
                    EmitSequencePointForToken(ifSyntax.ORlyKeyword);
                EmitIf(s);
                break;
            case BoundSwitchStatement s:
                if (s.Syntax is SwitchStatementSyntax switchSyntax)
                    EmitSequencePointForToken(switchSyntax.WtfKeyword);
                EmitSwitch(s);
                break;
            case BoundLoopStatement s:
                if (s.Syntax is LoopStatementSyntax loopSyntax)
                    EmitSequencePointForToken(loopSyntax.ImInKeyword);
                EmitLoop(s);
                break;
            case BoundGtfoStatement s:
                EmitSequencePoint(s);
                EmitGtfo(s);
                break;
            case BoundReturnStatement s:
                EmitSequencePoint(s);
                EmitReturn(s);
                break;
            case BoundCastStatement s:
                EmitSequencePoint(s);
                EmitCastStatement(s);
                break;
            case BoundScopedDeclaration s:
                EmitSequencePoint(s);
                EmitScopedDeclaration(s);
                break;
            case BoundIdentifierAssignment s:
                EmitSequencePoint(s);
                EmitIdentifierAssignment(s);
                break;
            case BoundObjectDefinition s:
                EmitSequencePoint(s);
                EmitObjectDefinition(s);
                break;
            case BoundFunctionDeclaration s:
                EmitSequencePoint(s);
                EmitFunctionDeclaration(s);
                break;
            case BoundImportStatement s:
                EmitSequencePoint(s);
                EmitImport(s);
                break;
        }
    }

    private void EmitVariableDeclaration(BoundVariableDeclaration decl)
    {
        var local = _il.DeclareLocal(typeof(object));
        var slot = _il.DeclareLocal(_resolvedSlotType);
        _locals[decl.Variable.Name] = local;
        SetLocalSymInfo(local, decl.Variable.Name);
        EmitResolvedDeclarationSlot(new BoundIdentifier(decl.Variable.Name, null, null));
        _il.Emit(OpCodes.Stloc, slot);
        _il.Emit(OpCodes.Ldloc, slot);
        if (decl.Initializer != null) EmitExpression(decl.Initializer);
        else _il.Emit(OpCodes.Ldnull);
        _il.Emit(OpCodes.Dup);
        _il.Emit(OpCodes.Stloc, local);
        _il.Emit(OpCodes.Call, _declareResolvedValueMethod);
    }

    private void EmitAssignment(BoundAssignment assignment)
    {
        var value = _il.DeclareLocal(typeof(object));
        EmitExpression(assignment.Expression);
        _il.Emit(OpCodes.Stloc, value);
        EmitResolvedSlot(new BoundIdentifier(assignment.Variable.Name, null, null));
        _il.Emit(OpCodes.Ldloc, value);
        if (_locals.TryGetValue(assignment.Variable.Name, out var local))
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, local);
        }
        _il.Emit(OpCodes.Call, _assignResolvedValueMethod);
    }

    private void EmitScopedDeclaration(BoundScopedDeclaration declaration)
    {
        var destination = _il.DeclareLocal(_scopeType);
        var slot = _il.DeclareLocal(_resolvedSlotType);
        EmitResolvedNamespace(declaration.Scope);
        _il.Emit(OpCodes.Stloc, destination);
        EmitResolvedDeclarationSlot(declaration.Name, destination);
        _il.Emit(OpCodes.Stloc, slot);
        _il.Emit(OpCodes.Ldloc, slot);
        if (declaration.Initializer is null) _il.Emit(OpCodes.Ldnull);
        else EmitExpression(declaration.Initializer);
        _il.Emit(OpCodes.Call, _declareResolvedValueMethod);
    }

    private void EmitIdentifierAssignment(BoundIdentifierAssignment assignment)
    {
        var value = _il.DeclareLocal(typeof(object));
        EmitExpression(assignment.Expression);
        _il.Emit(OpCodes.Stloc, value);
        EmitResolvedSlot(assignment.Target);
        _il.Emit(OpCodes.Ldloc, value);
        _il.Emit(OpCodes.Call, _assignResolvedValueMethod);
    }

    private void EmitFunctionDeclaration(BoundFunctionDeclaration declaration)
    {
        var destination = _il.DeclareLocal(_scopeType);
        var slot = _il.DeclareLocal(_resolvedSlotType);
        EmitResolvedNamespace(declaration.Scope!);
        _il.Emit(OpCodes.Stloc, destination);
        EmitResolvedDeclarationSlot(declaration.Identifier!, destination);
        _il.Emit(OpCodes.Stloc, slot);
        _il.Emit(OpCodes.Ldloc, slot);
        _il.Emit(OpCodes.Ldc_I4, declaration.Function.Parameters.Length);
        _il.Emit(OpCodes.Ldnull);
        _il.Emit(OpCodes.Ldftn, _functionMethods[declaration]);
        ConstructorInfo delegateConstructor = _functionBodyType.GetConstructor([typeof(object), typeof(IntPtr)])!;
        _il.Emit(OpCodes.Newobj, delegateConstructor);
        ImmutableArray<MethodBuilder> parameterResolvers = _parameterResolverMethods[declaration];
        _il.Emit(OpCodes.Ldc_I4, parameterResolvers.Length);
        _il.Emit(OpCodes.Newarr, _parameterNameResolverType);
        ConstructorInfo resolverConstructor =
            _parameterNameResolverType.GetConstructor([typeof(object), typeof(IntPtr)])!;
        for (int index = 0; index < parameterResolvers.Length; index++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, index);
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Ldftn, parameterResolvers[index]);
            _il.Emit(OpCodes.Newobj, resolverConstructor);
            _il.Emit(OpCodes.Stelem_Ref);
        }
        ConstructorInfo functionConstructor = _functionType.GetConstructor(
            [typeof(int), _functionBodyType, _parameterNameResolverType.MakeArrayType()])!;
        _il.Emit(OpCodes.Newobj, functionConstructor);
        _il.Emit(OpCodes.Call, _declareResolvedValueMethod);
    }

    private void EmitObjectDefinition(BoundObjectDefinition definition)
    {
        var outerScope = _il.DeclareLocal(_scopeType);
        var objectLocal = _il.DeclareLocal(_objectType);
        var declarationSlot = _il.DeclareLocal(_resolvedSlotType);
        EmitResolvedDeclarationSlot(definition.Name);
        _il.Emit(OpCodes.Stloc, declarationSlot);
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        _il.Emit(OpCodes.Stloc, outerScope);
        _il.Emit(OpCodes.Ldloc, outerScope);
        if (definition.Parent is null)
            _il.Emit(OpCodes.Ldnull);
        else
        {
            EmitResolvedValue(definition.Parent);
        }
        _il.Emit(OpCodes.Ldc_I4, definition.Mixins.Length);
        _il.Emit(OpCodes.Newarr, typeof(object));
        for (int index = 0; index < definition.Mixins.Length; index++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, index);
            EmitResolvedValue(definition.Mixins[index]);
            _il.Emit(OpCodes.Stelem_Ref);
        }
        _il.Emit(OpCodes.Call, _createObjectMethod);
        _il.Emit(OpCodes.Stloc, objectLocal);
        _il.Emit(OpCodes.Ldloc, objectLocal);
        _il.Emit(OpCodes.Stloc, _scopeLocal);

        _il.BeginExceptionBlock();
        _exceptionDepth++;
        EmitStatements(definition.Body);
        _il.BeginFinallyBlock();
        _il.Emit(OpCodes.Ldloc, outerScope);
        _il.Emit(OpCodes.Stloc, _scopeLocal);
        _il.EndExceptionBlock();
        _exceptionDepth--;

        _il.Emit(OpCodes.Ldloc, declarationSlot);
        _il.Emit(OpCodes.Ldloc, objectLocal);
        _il.Emit(OpCodes.Call, _declareResolvedValueMethod);
    }

    private void EmitVisible(BoundVisibleStatement visible)
    {
        _il.Emit(OpCodes.Ldc_I4, visible.Arguments.Length);
        _il.Emit(OpCodes.Newarr, typeof(object));

        for (int i = 0; i < visible.Arguments.Length; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);
            EmitExpression(visible.Arguments[i]);
            _il.Emit(OpCodes.Stelem_Ref);
        }

        _il.Emit(visible.SuppressNewline ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        _il.Emit(visible.WritesToStandardError ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        _il.Emit(OpCodes.Call, _printMethod);
    }

    private void EmitImport(BoundImportStatement import)
    {
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        if (import.Library.DirectName is { } directName)
        {
            _il.Emit(OpCodes.Ldstr, directName);
        }
        else
        {
            EmitExpression(import.Library.DynamicName!);
            _il.Emit(OpCodes.Call, _resolveIdentifierNameMethod);
        }
        _il.Emit(OpCodes.Call, _loadLibraryMethod);
    }

    private void EmitGimmeh(BoundGimmehStatement gimmeh)
    {
        var input = _il.DeclareLocal(typeof(string));
        _il.Emit(OpCodes.Call, _readLineMethod);
        _il.Emit(OpCodes.Stloc, input);
        EmitResolvedSlot(gimmeh.Target);
        _il.Emit(OpCodes.Ldloc, input);
        if (TryGetDirectName(gimmeh.Target, out string name) &&
            _locals.TryGetValue(name, out var local))
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, local);
        }
        _il.Emit(OpCodes.Call, _assignResolvedValueMethod);
    }

    private void EmitExpressionStatement(BoundExpressionStatement exprStmt)
    {
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        EmitExpression(exprStmt.Expression);
        if (_locals.TryGetValue("IT", out var itLocal))
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, itLocal);
        }
        _il.Emit(OpCodes.Call, _setItMethod);
    }

    private void EmitIf(BoundIfStatement ifStmt)
    {
        var endLabel = _il.DefineLabel();

        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        _il.Emit(OpCodes.Call, _getItMethod);
        _il.Emit(OpCodes.Call, _isTruthyMethod);
        var yaRlyFalse = _il.DefineLabel();
        _il.Emit(OpCodes.Brfalse, yaRlyFalse);

        EmitBlock(ifStmt.ThenBlock);
        _il.Emit(OpCodes.Br, endLabel);

        _il.MarkLabel(yaRlyFalse);

        for (int i = 0; i < ifStmt.MebbeClauses.Length; i++)
        {
            var clause = ifStmt.MebbeClauses[i];
            EmitExpression(clause.Condition);
            _il.Emit(OpCodes.Call, _isTruthyMethod);
            var nextClause = _il.DefineLabel();
            _il.Emit(OpCodes.Brfalse, nextClause);

            EmitBlock(clause.Body);
            _il.Emit(OpCodes.Br, endLabel);

            _il.MarkLabel(nextClause);
        }

        if (ifStmt.ElseBlock != null)
        {
            EmitBlock(ifStmt.ElseBlock);
        }

        _il.MarkLabel(endLabel);
    }

    private void EmitSwitch(BoundSwitchStatement switchStmt)
    {
        var endLabel = _il.DefineLabel();
        _switchBreakTargets.Push(new ControlFlowTarget(endLabel, _exceptionDepth));

        var matched = _il.DeclareLocal(typeof(bool));
        _il.Emit(OpCodes.Ldc_I4_0);
        _il.Emit(OpCodes.Stloc, matched);

        foreach (var clause in switchStmt.OmgClauses)
        {
            var skipBody = _il.DefineLabel();
            var enterBody = _il.DefineLabel();

            _il.Emit(OpCodes.Ldloc, matched);
            _il.Emit(OpCodes.Brtrue, enterBody);

            _il.Emit(OpCodes.Ldloc, _scopeLocal);
            _il.Emit(OpCodes.Call, _getItMethod);
            EmitLiteralValue(clause.LiteralValue);
            _il.Emit(OpCodes.Call, _switchCaseMatchesMethod);
            _il.Emit(OpCodes.Brfalse, skipBody);

            _il.MarkLabel(enterBody);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Stloc, matched);

            EmitBlock(clause.Body);

            _il.MarkLabel(skipBody);
        }

        if (switchStmt.DefaultBlock != null)
        {
            var skipDefault = _il.DefineLabel();
            _il.Emit(OpCodes.Ldloc, matched);
            _il.Emit(OpCodes.Brtrue, skipDefault);

            EmitBlock(switchStmt.DefaultBlock);

            _il.MarkLabel(skipDefault);
        }

        _il.MarkLabel(endLabel);
        _switchBreakTargets.Pop();
    }

    private void EmitLoop(BoundLoopStatement loop)
    {
        var loopStart = _il.DefineLabel();
        var leaveLoop = _il.DefineLabel();
        var loopEnd = _il.DefineLabel();
        var outerScope = _il.DeclareLocal(_scopeType);
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        _il.Emit(OpCodes.Stloc, outerScope);
        _il.Emit(OpCodes.Ldloc, outerScope);
        _il.Emit(OpCodes.Call, _createChildScopeMethod);
        _il.Emit(OpCodes.Stloc, _scopeLocal);

        _il.BeginExceptionBlock();
        _exceptionDepth++;
        _loopBreakTargets.Push(new ControlFlowTarget(leaveLoop, _exceptionDepth));

        string? varName = loop.Variable?.Name;
        if (varName != null)
        {
            EmitResolvedDeclarationSlot(new BoundIdentifier(varName, null, null));
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Box, typeof(int));
            _il.Emit(OpCodes.Call, _declareResolvedValueMethod);
        }

        _il.MarkLabel(loopStart);

        if (loop.Condition != null)
        {
            EmitExpression(loop.Condition);
            _il.Emit(OpCodes.Call, _isTruthyMethod);

            if (loop.IsTil == true)
                _il.Emit(OpCodes.Brtrue, leaveLoop);
            else
                _il.Emit(OpCodes.Brfalse, leaveLoop);
        }

        EmitBlock(loop.Body);

        // Increment/decrement loop variable
        if (varName != null && (loop.Operation != null || loop.OperationCall != null))
        {
            if (loop.Operation == "UPPIN")
            {
                EmitResolvedSlot(new BoundIdentifier(varName, null, null));
                EmitLoadLocal(varName);
                EmitLiteralValue(1);
                _il.Emit(OpCodes.Call, _addMethod);
                _il.Emit(OpCodes.Call, _assignResolvedValueMethod);
            }
            else if (loop.Operation == "NERFIN")
            {
                EmitResolvedSlot(new BoundIdentifier(varName, null, null));
                EmitLoadLocal(varName);
                EmitLiteralValue(1);
                _il.Emit(OpCodes.Call, _subtractMethod);
                _il.Emit(OpCodes.Call, _assignResolvedValueMethod);
            }
            else if (loop.OperationCall is not null)
            {
                EmitFunctionCall(loop.OperationCall);
                var updated = _il.DeclareLocal(typeof(object));
                _il.Emit(OpCodes.Stloc, updated);
                EmitResolvedSlot(new BoundIdentifier(varName, null, null));
                _il.Emit(OpCodes.Ldloc, updated);
                _il.Emit(OpCodes.Call, _assignResolvedValueMethod);
            }
        }

        _il.Emit(OpCodes.Br, loopStart);
        _il.MarkLabel(leaveLoop);
        _il.Emit(OpCodes.Leave, loopEnd);

        _loopBreakTargets.Pop();

        _il.BeginFinallyBlock();
        _il.Emit(OpCodes.Ldloc, outerScope);
        _il.Emit(OpCodes.Stloc, _scopeLocal);
        _il.EndExceptionBlock();
        _exceptionDepth--;
        _il.MarkLabel(loopEnd);
    }

    private void EmitGtfo(BoundGtfoStatement gtfo)
    {
        switch (gtfo.Context)
        {
            case ControlFlowContext.Loop when _loopBreakTargets.Count > 0:
                EmitControlTransfer(_loopBreakTargets.Peek());
                break;
            case ControlFlowContext.Switch when _switchBreakTargets.Count > 0:
                EmitControlTransfer(_switchBreakTargets.Peek());
                break;
            case ControlFlowContext.Function:
                if (_functionReturnValue != null)
                {
                    _il.Emit(OpCodes.Ldnull);
                    _il.Emit(OpCodes.Stloc, _functionReturnValue);
                }
                EmitControlTransfer(_functionReturnTarget);
                break;
        }
    }

    private void EmitReturn(BoundReturnStatement ret)
    {
        EmitExpression(ret.Expression);
        if (_functionReturnValue != null)
        {
            _il.Emit(OpCodes.Stloc, _functionReturnValue);
        }
        EmitControlTransfer(_functionReturnTarget);
    }

    private void EmitCastStatement(BoundCastStatement cast)
    {
        var slot = _il.DeclareLocal(_resolvedSlotType);
        EmitResolvedSlot(cast.Target);
        _il.Emit(OpCodes.Stloc, slot);

        _il.Emit(OpCodes.Ldloc, slot);
        _il.Emit(OpCodes.Ldloc, slot);
        _il.Emit(OpCodes.Call, _getResolvedValueMethod);
        _il.Emit(OpCodes.Ldstr, cast.TargetType);
        _il.Emit(OpCodes.Call, _explicitCastMethod);
        if (TryGetDirectName(cast.Target, out string name) &&
            _locals.TryGetValue(name, out var local))
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, local);
        }
        _il.Emit(OpCodes.Call, _assignResolvedValueMethod);
    }

    private void EmitControlTransfer(ControlFlowTarget target) =>
        _il.Emit(_exceptionDepth > target.ExceptionDepth ? OpCodes.Leave : OpCodes.Br, target.Label);

    private static bool TryGetDirectName(BoundIdentifier identifier, out string name)
    {
        if (identifier.Slot is null && identifier.DirectName is { } directName)
        {
            name = directName;
            return true;
        }

        name = "";
        return false;
    }

    private void EmitBlock(BoundBlockStatement block)
    {
        var outerScope = _il.DeclareLocal(_scopeType);
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        _il.Emit(OpCodes.Stloc, outerScope);
        _il.Emit(OpCodes.Ldloc, outerScope);
        _il.Emit(OpCodes.Call, _createChildScopeMethod);
        _il.Emit(OpCodes.Stloc, _scopeLocal);

        _il.BeginExceptionBlock();
        _exceptionDepth++;
        EmitStatements(block);
        _il.BeginFinallyBlock();
        _il.Emit(OpCodes.Ldloc, outerScope);
        _il.Emit(OpCodes.Stloc, _scopeLocal);
        _il.EndExceptionBlock();
        _exceptionDepth--;
    }

    private void EmitStatements(BoundBlockStatement block)
    {
        foreach (var statement in block.Statements)
            EmitStatement(statement);
    }

    private void EmitExpression(BoundExpression expression)
    {
        switch (expression)
        {
            case BoundLiteralExpression e:
                EmitLiteralValue(e.Value);
                break;
            case BoundInterpolatedStringExpression e:
                EmitInterpolatedString(e);
                break;
            case BoundVariableExpression e:
                EmitLoadLocal(e.Variable.Name);
                break;
            case BoundItExpression:
                _il.Emit(OpCodes.Ldloc, _scopeLocal);
                _il.Emit(OpCodes.Call, _getItMethod);
                break;
            case BoundUnaryExpression e:
                EmitExpression(e.Operand);
                _il.Emit(OpCodes.Call, _notMethod);
                _il.Emit(OpCodes.Box, typeof(bool));
                break;
            case BoundBinaryExpression e:
                EmitBinary(e);
                break;
            case BoundSmooshExpression e:
                EmitSmoosh(e);
                break;
            case BoundAllOfExpression e:
                EmitAllOf(e);
                break;
            case BoundAnyOfExpression e:
                EmitAnyOf(e);
                break;
            case BoundComparisonExpression e:
                EmitComparison(e);
                break;
            case BoundCastExpression e:
                EmitCast(e);
                break;
            case BoundFunctionCallExpression e:
                EmitFunctionCall(e);
                break;
            case BoundIdentifierExpression e:
                EmitResolvedValue(e.Identifier);
                break;
            case BoundObjectCreationExpression e:
                EmitObjectCreation(e);
                break;
            case BoundSystemCommandExpression e:
                EmitExpression(e.Command);
                _il.Emit(OpCodes.Call, _executeSystemCommandMethod);
                break;
        }
    }

    private void EmitInterpolatedString(BoundInterpolatedStringExpression expression)
    {
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        EmitStringArray(expression.TextParts);
        EmitStringArray(expression.Names);
        _il.Emit(OpCodes.Call, _interpolateYarnMethod);
    }

    private void EmitStringArray(ImmutableArray<string> values)
    {
        _il.Emit(OpCodes.Ldc_I4, values.Length);
        _il.Emit(OpCodes.Newarr, typeof(string));
        for (int index = 0; index < values.Length; index++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, index);
            _il.Emit(OpCodes.Ldstr, values[index]);
            _il.Emit(OpCodes.Stelem_Ref);
        }
    }

    private void EmitObjectCreation(BoundObjectCreationExpression creation)
    {
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        if (creation.Parent is null)
            _il.Emit(OpCodes.Ldnull);
        else
        {
            EmitResolvedValue(creation.Parent);
        }
        _il.Emit(OpCodes.Ldc_I4, creation.Mixins.Length);
        _il.Emit(OpCodes.Newarr, typeof(object));
        for (int index = 0; index < creation.Mixins.Length; index++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, index);
            EmitResolvedValue(creation.Mixins[index]);
            _il.Emit(OpCodes.Stelem_Ref);
        }
        _il.Emit(OpCodes.Call, _createObjectMethod);
    }

    private void EmitLiteralValue(object? value)
    {
        switch (value)
        {
            case null:
                _il.Emit(OpCodes.Ldnull);
                break;
            case int i:
                _il.Emit(OpCodes.Ldc_I4, i);
                _il.Emit(OpCodes.Box, typeof(int));
                break;
            case double d:
                _il.Emit(OpCodes.Ldc_R8, d);
                _il.Emit(OpCodes.Box, typeof(double));
                break;
            case string s:
                _il.Emit(OpCodes.Ldstr, s);
                if (s.Contains(":(", StringComparison.Ordinal) ||
                    s.Contains(":[", StringComparison.Ordinal))
                {
                    _il.Emit(OpCodes.Call, _createYarnLiteralMethod);
                }
                break;
            case bool b:
                _il.Emit(b ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Box, typeof(bool));
                break;
        }
    }

    private void EmitBinary(BoundBinaryExpression binary)
    {
        EmitExpression(binary.Left);
        EmitExpression(binary.Right);

        MethodInfo method = binary.OperatorKind switch
        {
            BoundBinaryOperatorKind.Addition => _addMethod,
            BoundBinaryOperatorKind.Subtraction => _subtractMethod,
            BoundBinaryOperatorKind.Multiplication => _multiplyMethod,
            BoundBinaryOperatorKind.Division => _divideMethod,
            BoundBinaryOperatorKind.Modulo => _moduloMethod,
            BoundBinaryOperatorKind.Maximum => _greaterMethod,
            BoundBinaryOperatorKind.Minimum => _smallerMethod,
            BoundBinaryOperatorKind.LogicalAnd => _andMethod,
            BoundBinaryOperatorKind.LogicalOr => _orMethod,
            BoundBinaryOperatorKind.LogicalXor => _xorMethod,
            _ => throw new InvalidOperationException($"Unknown operator kind: {binary.OperatorKind}")
        };

        _il.Emit(OpCodes.Call, method);

        // Boolean operators return bool, need to box
        if (binary.OperatorKind is BoundBinaryOperatorKind.LogicalAnd
            or BoundBinaryOperatorKind.LogicalOr
            or BoundBinaryOperatorKind.LogicalXor)
        {
            _il.Emit(OpCodes.Box, typeof(bool));
        }
    }

    private void EmitSmoosh(BoundSmooshExpression smoosh)
    {
        _il.Emit(OpCodes.Ldc_I4, smoosh.Operands.Length);
        _il.Emit(OpCodes.Newarr, typeof(object));

        for (int i = 0; i < smoosh.Operands.Length; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);
            EmitExpression(smoosh.Operands[i]);
            _il.Emit(OpCodes.Stelem_Ref);
        }

        _il.Emit(OpCodes.Call, _smooshMethod);
    }

    private void EmitAllOf(BoundAllOfExpression allOf)
    {
        var falseLabel = _il.DefineLabel();
        var endLabel = _il.DefineLabel();

        foreach (var operand in allOf.Operands)
        {
            EmitExpression(operand);
            _il.Emit(OpCodes.Call, _isTruthyMethod);
            _il.Emit(OpCodes.Brfalse, falseLabel);
        }

        _il.Emit(OpCodes.Ldc_I4_1);
        _il.Emit(OpCodes.Br, endLabel);

        _il.MarkLabel(falseLabel);
        _il.Emit(OpCodes.Ldc_I4_0);

        _il.MarkLabel(endLabel);
        _il.Emit(OpCodes.Box, typeof(bool));
    }

    private void EmitAnyOf(BoundAnyOfExpression anyOf)
    {
        var trueLabel = _il.DefineLabel();
        var endLabel = _il.DefineLabel();

        foreach (var operand in anyOf.Operands)
        {
            EmitExpression(operand);
            _il.Emit(OpCodes.Call, _isTruthyMethod);
            _il.Emit(OpCodes.Brtrue, trueLabel);
        }

        _il.Emit(OpCodes.Ldc_I4_0);
        _il.Emit(OpCodes.Br, endLabel);

        _il.MarkLabel(trueLabel);
        _il.Emit(OpCodes.Ldc_I4_1);

        _il.MarkLabel(endLabel);
        _il.Emit(OpCodes.Box, typeof(bool));
    }

    private void EmitComparison(BoundComparisonExpression cmp)
    {
        EmitExpression(cmp.Left);
        EmitExpression(cmp.Right);

        if (cmp.IsEquality)
            _il.Emit(OpCodes.Call, _bothSaemMethod);
        else
            _il.Emit(OpCodes.Call, _diffrintMethod);

        _il.Emit(OpCodes.Box, typeof(bool));
    }

    private void EmitCast(BoundCastExpression cast)
    {
        EmitExpression(cast.Operand);
        _il.Emit(OpCodes.Ldstr, cast.TargetType);
        _il.Emit(OpCodes.Call, _explicitCastMethod);
    }

    private void EmitFunctionCall(BoundFunctionCallExpression call)
    {
        if (call.StaticDispatch &&
            call.Scope?.DirectName == "I" &&
            call.Scope.Slot is null &&
            call.Identifier?.DirectName is not null &&
            call.Identifier.Slot is null &&
            _functionDeclarations.TryGetValue(
                call.Function,
                out BoundFunctionDeclaration? declaration))
        {
            var directParameterNames = _il.DeclareLocal(_resolvedSlotType.MakeArrayType());
            var directArguments = _il.DeclareLocal(typeof(object[]));
            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
            _il.Emit(OpCodes.Newarr, _resolvedSlotType);
            _il.Emit(OpCodes.Stloc, directParameterNames);
            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
            _il.Emit(OpCodes.Newarr, typeof(object));
            _il.Emit(OpCodes.Stloc, directArguments);

            ImmutableArray<MethodBuilder> resolvers = _parameterResolverMethods[declaration];
            for (int index = 0; index < call.Arguments.Length; index++)
            {
                _il.Emit(OpCodes.Ldloc, directParameterNames);
                _il.Emit(OpCodes.Ldc_I4, index);
                _il.Emit(OpCodes.Ldloc, _scopeLocal);
                _il.Emit(OpCodes.Call, resolvers[index]);
                _il.Emit(OpCodes.Stelem_Ref);

                _il.Emit(OpCodes.Ldloc, directArguments);
                _il.Emit(OpCodes.Ldc_I4, index);
                EmitExpression(call.Arguments[index]);
                _il.Emit(OpCodes.Stelem_Ref);
            }

            _il.Emit(OpCodes.Ldloc, _scopeLocal);
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Ldloc, directArguments);
            _il.Emit(OpCodes.Ldloc, directParameterNames);
            _il.Emit(OpCodes.Call, _functionMethods[declaration]);
            return;
        }

        var target = _il.DeclareLocal(_functionTargetType);
        var destination = _il.DeclareLocal(_scopeType);
        EmitResolvedNamespace(call.Scope ?? new BoundIdentifier("I", null, null));
        _il.Emit(OpCodes.Stloc, destination);
        EmitResolvedSlot(
            call.Identifier ?? new BoundIdentifier(call.Function.Name, null, null),
            destination);
        _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
        _il.Emit(OpCodes.Call, _resolveFunctionSlotMethod);
        _il.Emit(OpCodes.Stloc, target);

        var parameterNames = _il.DeclareLocal(_resolvedSlotType.MakeArrayType());
        var arguments = _il.DeclareLocal(typeof(object[]));
        _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
        _il.Emit(OpCodes.Newarr, _resolvedSlotType);
        _il.Emit(OpCodes.Stloc, parameterNames);
        _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
        _il.Emit(OpCodes.Newarr, typeof(object));
        _il.Emit(OpCodes.Stloc, arguments);
        for (int index = 0; index < call.Arguments.Length; index++)
        {
            _il.Emit(OpCodes.Ldloc, parameterNames);
            _il.Emit(OpCodes.Ldc_I4, index);
            _il.Emit(OpCodes.Ldloc, _scopeLocal);
            _il.Emit(OpCodes.Ldloc, target);
            _il.Emit(OpCodes.Ldc_I4, index);
            _il.Emit(OpCodes.Call, _resolveParameterNameMethod);
            _il.Emit(OpCodes.Stelem_Ref);

            _il.Emit(OpCodes.Ldloc, arguments);
            _il.Emit(OpCodes.Ldc_I4, index);
            EmitExpression(call.Arguments[index]);
            _il.Emit(OpCodes.Stelem_Ref);
        }

        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        _il.Emit(OpCodes.Ldloc, target);
        _il.Emit(OpCodes.Ldloc, parameterNames);
        _il.Emit(OpCodes.Ldloc, arguments);
        _il.Emit(OpCodes.Call, _invokeResolvedMethod);
    }

    private void EmitParameterResolver(MethodBuilder method, BoundIdentifier parameter)
    {
        _il = method.GetILGenerator();
        _locals.Clear();
        _scopeLocal = _il.DeclareLocal(_scopeType);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Stloc, _scopeLocal);
        EmitResolvedSlot(parameter);
        _il.Emit(OpCodes.Ret);
    }

    private void EmitLoadLocal(string name)
    {
        EmitResolvedValue(new BoundIdentifier(name, null, null));
    }

    private void EmitStoreLocal(string name)
    {
        var value = _il.DeclareLocal(typeof(object));
        _il.Emit(OpCodes.Stloc, value);
        EmitResolvedSlot(new BoundIdentifier(name, null, null));
        _il.Emit(OpCodes.Ldloc, value);
        _il.Emit(OpCodes.Call, _assignResolvedValueMethod);
    }

    private void EmitResolvedValue(BoundIdentifier identifier)
    {
        EmitResolvedSlot(identifier);
        _il.Emit(OpCodes.Call, _getResolvedValueMethod);
    }

    private void EmitResolvedSlot(BoundIdentifier identifier, LocalBuilder? destination = null)
    {
        EmitIdentifierResolver(identifier, destination);
        _il.Emit(OpCodes.Call, _resolveIdentifierSlotMethod);
    }

    private void EmitResolvedDeclarationSlot(
        BoundIdentifier identifier,
        LocalBuilder? destination = null)
    {
        EmitIdentifierResolver(identifier, destination);
        _il.Emit(OpCodes.Call, _resolveDeclarationSlotMethod);
    }

    private void EmitResolvedNamespace(BoundIdentifier identifier)
    {
        EmitIdentifierResolver(identifier);
        _il.Emit(OpCodes.Call, _resolveIdentifierNamespaceMethod);
    }

    private void EmitIdentifierResolver(
        BoundIdentifier identifier,
        LocalBuilder? destination = null)
    {
        var parts = new List<BoundIdentifier>();
        for (BoundIdentifier? current = identifier; current is not null; current = current.Slot)
            parts.Add(current);

        var resolver = _il.DeclareLocal(_identifierResolverType);
        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        _il.Emit(OpCodes.Ldloc, destination ?? _scopeLocal);
        _il.Emit(OpCodes.Call, _beginIdentifierPathMethod);
        _il.Emit(OpCodes.Stloc, resolver);

        for (int index = 0; index < parts.Count; index++)
        {
            if (index > 0)
            {
                _il.Emit(OpCodes.Ldloc, resolver);
                _il.Emit(OpCodes.Call, _prepareIdentifierSegmentMethod);
            }

            _il.Emit(OpCodes.Ldloc, resolver);
            if (parts[index].DirectName is { } name)
                _il.Emit(OpCodes.Ldstr, name);
            else
            {
                EmitExpression(parts[index].DynamicName!);
                _il.Emit(OpCodes.Call, _resolveIdentifierNameMethod);
            }
            _il.Emit(OpCodes.Call, _setIdentifierSegmentMethod);
        }

        _il.Emit(OpCodes.Ldloc, resolver);
    }

    private void EmitSequencePoint(BoundNode node)
    {
        if (_document == null || _sourceText == null) return;
        if (node.Syntax is null || node.Syntax.Span.Length == 0) return;
        EmitSequencePointForSpan(node.Syntax.Span);
    }

    private void EmitSequencePointForToken(SyntaxToken token)
    {
        if (_document == null || _sourceText == null) return;
        if (token.Span.Length == 0) return;
        EmitSequencePointForSpan(token.Span);
    }

    private void EmitSequencePointForSpan(TextSpan span)
    {
        var loc = TextLocation.FromSpan(_sourceText!, span);
        _il.MarkSequencePoint(_document!,
            loc.StartLine + 1,       // 0-based → 1-based
            loc.StartCharacter + 1,  // 0-based → 1-based
            loc.EndLine + 1,         // 0-based → 1-based
            loc.EndCharacter + 1);   // 0-based → 1-based
    }

    private void SetLocalSymInfo(LocalBuilder local, string name)
    {
        if (_document != null)
            local.SetLocalSymInfo(name);
    }

    private static void WriteRuntimeConfig(string dllPath)
    {
        var configPath = Path.ChangeExtension(dllPath, ".runtimeconfig.json");
        var config = """
            {
              "runtimeOptions": {
                "tfm": "net10.0",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "10.0.0"
                }
              }
            }
            """;
        File.WriteAllText(configPath, config);
    }
}
