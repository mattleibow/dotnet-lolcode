using System.Collections.Immutable;
using System.Diagnostics.SymbolStore;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Lolcode.CodeAnalysis.Binding;
using Lolcode.CodeAnalysis.BoundTree;
using Lolcode.CodeAnalysis.Symbols;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.CodeAnalysis.Text;
using Lolcode.Runtime;

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
    private readonly string? _runtimeAssemblyPath;
    private readonly byte[]? _runtimeAssemblyImage;
    private readonly IReadOnlyList<string> _referenceAssemblyPaths;
    private readonly bool _isLibrary;
    private readonly string? _libraryTypeName;
    private readonly IReadOnlyList<string> _libraryDescriptors;
    private readonly IReadOnlyList<SyntaxTree> _syntaxTrees;
    private readonly IReadOnlyDictionary<SyntaxNode, SyntaxTree> _syntaxTreeOwners;
    private readonly Dictionary<string, ISymbolDocumentWriter> _documents =
        new(StringComparer.Ordinal);

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
    private Type _systemObjectType = null!;
    private Type _voidType = null!;
    private Type _stringType = null!;
    private Type _booleanType = null!;
    private Type _int32Type = null!;
    private Type _doubleType = null!;
    private Type _intPtrType = null!;
    private Type _libraryAttributeType = null!;

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
    private MethodInfo _configureLibrariesMethod = null!;
    private MethodInfo _executeSystemCommandMethod = null!;
    private MethodInfo _disposeScopeMethod = null!;
    private MethodInfo _transferPublicLibraryResultMethod = null!;
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
    private MethodInfo _createLibraryObjectMethod = null!;
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
    public CodeGenerator(
        BoundBlockStatement boundTree,
        string assemblyName,
        string? runtimeAssemblyPath,
        byte[]? runtimeAssemblyImage = null,
        IEnumerable<string>? referenceAssemblyPaths = null,
        IReadOnlyList<SyntaxTree>? syntaxTrees = null,
        IReadOnlyDictionary<SyntaxNode, SyntaxTree>? syntaxTreeOwners = null,
        bool isLibrary = false,
        string? libraryTypeName = null,
        IEnumerable<string>? libraryDescriptors = null)
    {
        _boundTree = boundTree;
        _assemblyName = assemblyName;
        _runtimeAssemblyPath = runtimeAssemblyPath;
        _runtimeAssemblyImage = runtimeAssemblyImage;
        _referenceAssemblyPaths = referenceAssemblyPaths?.ToArray() ?? [];
        _syntaxTrees = syntaxTrees ?? [];
        _syntaxTreeOwners = syntaxTreeOwners ?? new Dictionary<SyntaxNode, SyntaxTree>();
        _isLibrary = isLibrary;
        _libraryTypeName = libraryTypeName;
        _libraryDescriptors = libraryDescriptors?.ToArray() ?? [];
    }

    /// <summary>
    /// Emits the assembly to caller-provided streams.
    /// </summary>
    public void Emit(
        Stream peStream,
        Stream? pdbStream = null,
        string? pdbFileName = null,
        CancellationToken cancellationToken = default)
        => EmitCore(peStream, pdbStream, pdbFileName, toleratePdbFailure: false, cancellationToken);

    /// <summary>
    /// Emits an assembly for the compatibility path API, omitting optional symbols
    /// when portable PDB serialization fails.
    /// </summary>
    /// <returns>Whether portable PDB symbols were emitted.</returns>
    public bool EmitWithOptionalPdb(
        Stream peStream,
        Stream pdbStream,
        string pdbFileName,
        CancellationToken cancellationToken = default)
        => EmitCore(peStream, pdbStream, pdbFileName, toleratePdbFailure: true, cancellationToken);

    private bool EmitCore(
        Stream peStream,
        Stream? pdbStream,
        string? pdbFileName,
        bool toleratePdbFailure,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var metadataLoadContext = CreateMetadataLoadContext();
        var runtimeAssembly = _runtimeAssemblyImage is null
            ? metadataLoadContext.LoadFromAssemblyPath(
                _runtimeAssemblyPath
                ?? throw new InvalidOperationException("No Lolcode.Runtime assembly source was supplied."))
            : metadataLoadContext.LoadFromByteArray(_runtimeAssemblyImage);
        var coreAssembly = metadataLoadContext.CoreAssembly
            ?? throw new InvalidOperationException("Could not resolve the target core assembly.");
        _systemObjectType = GetCoreType(coreAssembly, "System.Object");
        _voidType = GetCoreType(coreAssembly, "System.Void");
        _stringType = GetCoreType(coreAssembly, "System.String");
        _booleanType = GetCoreType(coreAssembly, "System.Boolean");
        _int32Type = GetCoreType(coreAssembly, "System.Int32");
        _doubleType = GetCoreType(coreAssembly, "System.Double");
        _intPtrType = GetCoreType(coreAssembly, "System.IntPtr");
        _scopeType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolScope));
        _objectType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolObject));
        _functionType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolFunction));
        _functionBodyType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolFunctionBody));
        _parameterNameResolverType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolParameterNameResolver));
        _functionTargetType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolFunctionTarget));
        _identifierResolverType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolIdentifierResolver));
        _resolvedSlotType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolResolvedSlot));
        _libraryAttributeType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolcodeLibraryAttribute));

        var runtimeType = GetRequiredRuntimeType(runtimeAssembly, typeof(LolRuntime));
        ResolveRuntimeMethods(runtimeType);

        var assemblyBuilder = new PersistedAssemblyBuilder(
            new AssemblyName(_assemblyName),
            coreAssembly);

        var moduleBuilder = assemblyBuilder.DefineDynamicModule(_assemblyName);

        var lolcodeLanguageGuid = new Guid("4C4F4C43-4F44-4500-0000-000000000001");
        foreach (SyntaxTree syntaxTree in _syntaxTrees)
        {
            if (string.IsNullOrEmpty(syntaxTree.FilePath))
                continue;

            string documentPath = Path.GetFullPath(syntaxTree.FilePath);
            if (!_documents.ContainsKey(documentPath))
            {
                _documents.Add(
                    documentPath,
                    moduleBuilder.DefineDocument(
                        documentPath,
                        lolcodeLanguageGuid,
                        SymLanguageVendor.Microsoft,
                        SymDocumentType.Text));
            }
        }

        _typeBuilder = moduleBuilder.DefineType(
            _isLibrary ? _libraryTypeName ?? "LolcodeExports" : "Program",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed);
        if (_isLibrary)
        {
            _typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                _libraryAttributeType.GetConstructor(Type.EmptyTypes)
                    ?? throw new MissingMethodException(_libraryAttributeType.FullName, ".ctor"),
                []));
        }

        int functionIndex = 0;
        var emittedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var funcDecl in EnumerateFunctions(_boundTree))
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                _systemObjectType,
                [_scopeType, _objectType, _systemObjectType.MakeArrayType(), _resolvedSlotType.MakeArrayType()]);
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

        MethodBuilder? mainMethod = _isLibrary
            ? null
            : _typeBuilder.DefineMethod(
                "Main",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                _voidType,
                []);

        // Emit function bodies
        foreach (var pair in _functionMethods)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EmitFunction(pair.Value, pair.Key);
        }
        foreach (var pair in _parameterResolverMethods)
        {
            for (int index = 0; index < pair.Value.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EmitParameterResolver(pair.Value[index], pair.Key.ParameterIdentifiers[index]);
            }
        }

        EmitPublicFunctionWrappers();
        EmitGeneratedLibraryFactory();

        if (mainMethod is not null)
        {
            _il = mainMethod.GetILGenerator();
            _locals.Clear();

            _il.BeginScope();

            if (_syntaxTrees.FirstOrDefault()?.Text is { Length: > 0 } firstSourceText &&
                firstSourceText[0] == '\uFEFF')
                _il.Emit(OpCodes.Call, _writeByteOrderMarkMethod);

            _scopeLocal = _il.DeclareLocal(_scopeType);
            _il.Emit(OpCodes.Call, _createScopeMethod);
            _il.Emit(OpCodes.Stloc, _scopeLocal);
            EmitLibraryConfiguration();
            var mainIt = _il.DeclareLocal(_systemObjectType);
            _locals["IT"] = mainIt;
            SetLocalSymInfo(mainIt, "IT");
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Stloc, mainIt);

            _il.BeginExceptionBlock();
            foreach (var statement in _boundTree.Statements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EmitStatement(statement);
            }
            _il.BeginFinallyBlock();
            _il.Emit(OpCodes.Ldloc, _scopeLocal);
            _il.Emit(OpCodes.Call, _disposeScopeMethod);
            _il.EndExceptionBlock();

            _il.EndScope();
            _il.Emit(OpCodes.Ret);
        }

        _typeBuilder.CreateType();

        cancellationToken.ThrowIfCancellationRequested();
        var metadataBuilder = assemblyBuilder.GenerateMetadata(out var ilStream, out var mappedFieldData, out MetadataBuilder pdbBuilder);
        var entryPointHandle = mainMethod is null
            ? default(MethodDefinitionHandle)
            : MetadataTokens.MethodDefinitionHandle(mainMethod.MetadataToken);
        DebugDirectoryBuilder? debugDirectoryBuilder = null;
        var pdbEmitted = false;

        if (pdbStream != null)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
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
                cancellationToken.ThrowIfCancellationRequested();
                portablePdbBlob.WriteContentTo(pdbStream);

                debugDirectoryBuilder = new DebugDirectoryBuilder();
                debugDirectoryBuilder.AddCodeViewEntry(
                    pdbFileName ?? $"{_assemblyName}.pdb",
                    pdbContentId,
                    portablePdbBuilder.FormatVersion);
                pdbEmitted = true;
            }
            catch (Exception ex) when (toleratePdbFailure && IsOptionalPdbFailure(ex))
            {
                debugDirectoryBuilder = null;
            }
        }

        var peBuilder = new ManagedPEBuilder(
            header: CreatePeHeader(),
            metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
            ilStream: ilStream,
            mappedFieldData: mappedFieldData,
            debugDirectoryBuilder: debugDirectoryBuilder,
            entryPoint: entryPointHandle);

        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);
        cancellationToken.ThrowIfCancellationRequested();
        peBlob.WriteContentTo(peStream);
        cancellationToken.ThrowIfCancellationRequested();
        return pdbEmitted;
    }

    private static bool IsOptionalPdbFailure(Exception exception)
    {
        return exception is
            IOException or
            UnauthorizedAccessException or
            ArgumentException or
            InvalidOperationException or
            NotSupportedException or
            System.Security.Cryptography.CryptographicException;
    }

    private MetadataLoadContext CreateMetadataLoadContext()
    {
        IEnumerable<string> referencePaths = _referenceAssemblyPaths.Count > 0
            ? _referenceAssemblyPaths
            : Directory.EnumerateFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
        var resolverPaths = referencePaths
            .Append(_runtimeAssemblyPath)
            .Where(static path => !string.IsNullOrEmpty(path))
            .Select(static path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!resolverPaths.Any(path =>
            string.Equals(
                Path.GetFileName(path),
                "System.Runtime.dll",
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "The target reference assemblies do not contain System.Runtime.dll.");
        }

        return new MetadataLoadContext(
            new PathAssemblyResolver(resolverPaths),
            coreAssemblyName: "System.Runtime");
    }

    private static Type GetCoreType(Assembly coreAssembly, string fullName) =>
        coreAssembly.GetType(fullName)
        ?? throw new InvalidOperationException(
            $"Could not find core type '{fullName}' in '{coreAssembly.FullName}'.");

    private PEHeaderBuilder CreatePeHeader() =>
        _isLibrary
            ? PEHeaderBuilder.CreateLibraryHeader()
            : new PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage,
                subsystem: Subsystem.WindowsCui);

    private void ResolveRuntimeMethods(Type runtimeType)
    {
        _printMethod = GetRequiredRuntimeMethod(
            runtimeType,
            nameof(LolRuntime.Print),
            [_systemObjectType.MakeArrayType(), _booleanType, _booleanType]);
        _loadLibraryMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.LoadLibrary));
        _configureLibrariesMethod = GetRequiredRuntimeMethod(
            runtimeType,
            nameof(LolRuntime.ConfigureLibraries),
            [_scopeType, _stringType.MakeArrayType()]);
        _executeSystemCommandMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.ExecuteSystemCommandValue));
        _disposeScopeMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.DisposeScope));
        _transferPublicLibraryResultMethod = GetRequiredRuntimeMethod(
            runtimeType,
            nameof(LolRuntime.TransferPublicLibraryResult),
            [_scopeType, _systemObjectType]);
        _writeByteOrderMarkMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.WriteByteOrderMark));
        _createYarnLiteralMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.CreateYarnLiteral));
        _interpolateYarnMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.InterpolateYarnValue));
        _readLineMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.ReadLine));
        _addMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Add));
        _subtractMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Subtract));
        _multiplyMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Multiply));
        _divideMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Divide));
        _moduloMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Modulo));
        _greaterMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Greater));
        _smallerMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Smaller));
        _andMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.And));
        _orMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Or));
        _xorMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Xor));
        _notMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Not));
        _bothSaemMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.BothSaem));
        _switchCaseMatchesMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.SwitchCaseMatches));
        _diffrintMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.Diffrint));
        _smooshMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.SmooshValue));
        _isTruthyMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.IsTruthy));
        _castToYarnMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.CastToYarn));
        _castToNumbrMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.CastToNumbr));
        _castToNumbarMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.CastToNumbar));
        _castToTroofMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.CastToTroof));
        _explicitCastMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.ExplicitCast));
        _createScopeMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.CreateScope));
        _createChildScopeMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.CreateChildScope));
        _createLibraryObjectMethod = GetRequiredRuntimeMethod(
            runtimeType,
            nameof(LolRuntime.CreateLibraryObject),
            [_scopeType]);
        _createInvocationScopeMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.CreateInvocationScope));
        _createObjectMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.CreateObject));
        _invokeResolvedMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.InvokeResolved));
        _resolveParameterNameMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.ResolveParameterName));
        _getItMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.GetIt));
        _setItMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.SetIt));
        _resolveIdentifierNameMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.ResolveIdentifierName));
        _beginIdentifierPathMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.BeginIdentifierPath));
        _prepareIdentifierSegmentMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.PrepareIdentifierSegment));
        _setIdentifierSegmentMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.SetIdentifierSegment));
        _resolveIdentifierSlotMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.ResolveIdentifierSlot));
        _resolveIdentifierNamespaceMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.ResolveIdentifierNamespace));
        _getResolvedValueMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.GetResolvedValue));
        _resolveDeclarationSlotMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.ResolveDeclarationSlot));
        _declareResolvedValueMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.DeclareResolvedValue));
        _declareParameterMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.DeclareParameter));
        _assignResolvedValueMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.AssignResolvedValue));
        _resolveFunctionSlotMethod = GetRequiredRuntimeMethod(runtimeType, nameof(LolRuntime.ResolveFunctionSlot));
    }

    private void EmitLibraryConfiguration()
    {
        if (_libraryDescriptors.Count == 0)
            return;

        _il.Emit(OpCodes.Ldloc, _scopeLocal);
        _il.Emit(OpCodes.Ldc_I4, _libraryDescriptors.Count);
        _il.Emit(OpCodes.Newarr, _stringType);
        for (int index = 0; index < _libraryDescriptors.Count; index++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, index);
            _il.Emit(OpCodes.Ldstr, _libraryDescriptors[index]);
            _il.Emit(OpCodes.Stelem_Ref);
        }
        _il.Emit(OpCodes.Call, _configureLibrariesMethod);
    }

    private static Type GetRequiredRuntimeType(Assembly runtimeAssembly, Type expectedType)
    {
        return runtimeAssembly.GetType(expectedType.FullName!, throwOnError: true)!;
    }

    private static MethodInfo GetRequiredRuntimeMethod(
        Type runtimeType,
        string methodName,
        Type[]? parameterTypes = null)
    {
        return parameterTypes is null
            ? runtimeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(runtimeType.FullName, methodName)
            : runtimeType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                parameterTypes,
                modifiers: null)
            ?? throw new MissingMethodException(runtimeType.FullName, methodName);
    }

    private void EmitPublicFunctionWrappers()
    {
        if (!_isLibrary)
            return;

        foreach (var declaration in _boundTree.Statements.OfType<BoundFunctionDeclaration>())
        {
            if (declaration.Scope?.DirectName != "I" ||
                declaration.Scope.Slot is not null ||
                declaration.Identifier?.DirectName is not { } name ||
                declaration.Identifier.Slot is not null)
            {
                continue;
            }

            MethodBuilder wrapper = _typeBuilder.DefineMethod(
                name,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                _systemObjectType,
                Enumerable.Repeat(_systemObjectType, declaration.Function.Parameters.Length).ToArray());
            _il = wrapper.GetILGenerator();
            _locals.Clear();
            _loopBreakTargets.Clear();
            _switchBreakTargets.Clear();
            _exceptionDepth = 0;
            _functionReturnValue = null;
            _il.BeginScope();
            var rootScope = _il.DeclareLocal(_scopeType);
            var module = _il.DeclareLocal(_objectType);
            var arguments = _il.DeclareLocal(_systemObjectType.MakeArrayType());
            var parameterSlots = _il.DeclareLocal(_resolvedSlotType.MakeArrayType());
            var result = _il.DeclareLocal(_systemObjectType);

            _il.Emit(OpCodes.Call, _createScopeMethod);
            _il.Emit(OpCodes.Stloc, rootScope);
            _il.Emit(OpCodes.Ldloc, rootScope);
            _il.Emit(OpCodes.Call, _createLibraryObjectMethod);
            _il.Emit(OpCodes.Stloc, module);
            _scopeLocal = module;
            var wrapperIt = _il.DeclareLocal(_systemObjectType);
            _locals["IT"] = wrapperIt;
            SetLocalSymInfo(wrapperIt, "IT");
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Stloc, wrapperIt);
            _il.BeginExceptionBlock();
            EmitLibraryConfiguration();
            EmitLibraryInitializer();

            _il.Emit(OpCodes.Ldc_I4, declaration.Function.Parameters.Length);
            _il.Emit(OpCodes.Newarr, _systemObjectType);
            for (int index = 0; index < declaration.Function.Parameters.Length; index++)
            {
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Ldc_I4, index);
                _il.Emit(OpCodes.Ldarg, index);
                _il.Emit(OpCodes.Stelem_Ref);
            }
            _il.Emit(OpCodes.Stloc, arguments);

            _il.Emit(OpCodes.Ldc_I4, declaration.Function.Parameters.Length);
            _il.Emit(OpCodes.Newarr, _resolvedSlotType);
            _il.Emit(OpCodes.Stloc, parameterSlots);
            ImmutableArray<MethodBuilder> resolvers = _parameterResolverMethods[declaration];
            for (int index = 0; index < resolvers.Length; index++)
            {
                _il.Emit(OpCodes.Ldloc, parameterSlots);
                _il.Emit(OpCodes.Ldc_I4, index);
                _il.Emit(OpCodes.Ldloc, rootScope);
                _il.Emit(OpCodes.Call, resolvers[index]);
                _il.Emit(OpCodes.Stelem_Ref);
            }

            _il.Emit(OpCodes.Ldloc, rootScope);
            _il.Emit(OpCodes.Ldloc, module);
            _il.Emit(OpCodes.Ldloc, arguments);
            _il.Emit(OpCodes.Ldloc, parameterSlots);
            _il.Emit(OpCodes.Call, _functionMethods[declaration]);
            _il.Emit(OpCodes.Stloc, result);
            _il.Emit(OpCodes.Ldloc, rootScope);
            _il.Emit(OpCodes.Ldloc, result);
            _il.Emit(OpCodes.Call, _transferPublicLibraryResultMethod);
            _il.Emit(OpCodes.Stloc, result);

            _il.BeginFinallyBlock();
            _il.Emit(OpCodes.Ldloc, rootScope);
            _il.Emit(OpCodes.Call, _disposeScopeMethod);
            _il.EndExceptionBlock();
            _il.Emit(OpCodes.Ldloc, result);
            _il.EndScope();
            _il.Emit(OpCodes.Ret);
        }
    }

    private void EmitGeneratedLibraryFactory()
    {
        if (!_isLibrary)
            return;

        MethodBuilder factory = _typeBuilder.DefineMethod(
            "__CreateLolcodeLibrary",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            _objectType,
            [_scopeType]);
        _il = factory.GetILGenerator();
        var module = _il.DeclareLocal(_objectType);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Call, _createLibraryObjectMethod);
        _il.Emit(OpCodes.Stloc, module);

        _scopeLocal = module;
        _locals.Clear();
        EmitLibraryConfiguration();
        var moduleIt = _il.DeclareLocal(_systemObjectType);
        _locals["IT"] = moduleIt;
        _il.Emit(OpCodes.Ldnull);
        _il.Emit(OpCodes.Stloc, moduleIt);
        EmitLibraryInitializer();

        _il.Emit(OpCodes.Ldloc, module);
        _il.Emit(OpCodes.Ret);
    }

    private void EmitLibraryInitializer()
    {
        foreach (BoundStatement statement in _boundTree.Statements)
        {
            switch (statement)
            {
                case BoundImportStatement:
                case BoundVariableDeclaration:
                case BoundScopedDeclaration:
                case BoundObjectDefinition:
                case BoundFunctionDeclaration:
                    EmitStatement(statement);
                    break;
            }
        }
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
        var functionIt = _il.DeclareLocal(_systemObjectType);
        _locals["IT"] = functionIt;
        SetLocalSymInfo(functionIt, "IT");
        _il.Emit(OpCodes.Ldnull);
        _il.Emit(OpCodes.Stloc, functionIt);

        // Parameters are accessible by name
        for (int i = 0; i < decl.Function.Parameters.Length; i++)
        {
            var parameterLocal = _il.DeclareLocal(_systemObjectType);
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
        _functionReturnValue = _il.DeclareLocal(_systemObjectType);
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
                    EmitSequencePointForToken(ifSyntax.ORlyKeyword, ifSyntax);
                EmitIf(s);
                break;
            case BoundSwitchStatement s:
                if (s.Syntax is SwitchStatementSyntax switchSyntax)
                    EmitSequencePointForToken(switchSyntax.WtfKeyword, switchSyntax);
                EmitSwitch(s);
                break;
            case BoundLoopStatement s:
                if (s.Syntax is LoopStatementSyntax loopSyntax)
                    EmitSequencePointForToken(loopSyntax.ImInKeyword, loopSyntax);
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
        var local = _il.DeclareLocal(_systemObjectType);
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
        var value = _il.DeclareLocal(_systemObjectType);
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
        var value = _il.DeclareLocal(_systemObjectType);
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
        ConstructorInfo delegateConstructor = _functionBodyType.GetConstructor([_systemObjectType, _intPtrType])!;
        _il.Emit(OpCodes.Newobj, delegateConstructor);
        ImmutableArray<MethodBuilder> parameterResolvers = _parameterResolverMethods[declaration];
        _il.Emit(OpCodes.Ldc_I4, parameterResolvers.Length);
        _il.Emit(OpCodes.Newarr, _parameterNameResolverType);
        ConstructorInfo resolverConstructor =
            _parameterNameResolverType.GetConstructor([_systemObjectType, _intPtrType])!;
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
            [_int32Type, _functionBodyType, _parameterNameResolverType.MakeArrayType()])!;
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
        _il.Emit(OpCodes.Newarr, _systemObjectType);
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
        _il.Emit(OpCodes.Newarr, _systemObjectType);

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
        var input = _il.DeclareLocal(_stringType);
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

        var matched = _il.DeclareLocal(_booleanType);
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
            _il.Emit(OpCodes.Box, _int32Type);
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
                var updated = _il.DeclareLocal(_systemObjectType);
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
                _il.Emit(OpCodes.Box, _booleanType);
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
        _il.Emit(OpCodes.Newarr, _stringType);
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
        _il.Emit(OpCodes.Newarr, _systemObjectType);
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
                _il.Emit(OpCodes.Box, _int32Type);
                break;
            case double d:
                _il.Emit(OpCodes.Ldc_R8, d);
                _il.Emit(OpCodes.Box, _doubleType);
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
                _il.Emit(OpCodes.Box, _booleanType);
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
            _il.Emit(OpCodes.Box, _booleanType);
        }
    }

    private void EmitSmoosh(BoundSmooshExpression smoosh)
    {
        _il.Emit(OpCodes.Ldc_I4, smoosh.Operands.Length);
        _il.Emit(OpCodes.Newarr, _systemObjectType);

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
        _il.Emit(OpCodes.Box, _booleanType);
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
        _il.Emit(OpCodes.Box, _booleanType);
    }

    private void EmitComparison(BoundComparisonExpression cmp)
    {
        EmitExpression(cmp.Left);
        EmitExpression(cmp.Right);

        if (cmp.IsEquality)
            _il.Emit(OpCodes.Call, _bothSaemMethod);
        else
            _il.Emit(OpCodes.Call, _diffrintMethod);

        _il.Emit(OpCodes.Box, _booleanType);
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
            var directArguments = _il.DeclareLocal(_systemObjectType.MakeArrayType());
            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
            _il.Emit(OpCodes.Newarr, _resolvedSlotType);
            _il.Emit(OpCodes.Stloc, directParameterNames);
            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
            _il.Emit(OpCodes.Newarr, _systemObjectType);
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
        var arguments = _il.DeclareLocal(_systemObjectType.MakeArrayType());
        _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
        _il.Emit(OpCodes.Newarr, _resolvedSlotType);
        _il.Emit(OpCodes.Stloc, parameterNames);
        _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
        _il.Emit(OpCodes.Newarr, _systemObjectType);
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
        var value = _il.DeclareLocal(_systemObjectType);
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
        if (node.Syntax is null || node.Syntax.Span.Length == 0) return;
        EmitSequencePointForSpan(node.Syntax.Span, node.Syntax);
    }

    private void EmitSequencePointForToken(SyntaxToken token, SyntaxNode containingSyntax)
    {
        if (token.Span.Length == 0) return;
        EmitSequencePointForSpan(token.Span, containingSyntax);
    }

    private void EmitSequencePointForSpan(TextSpan span, SyntaxNode owningSyntax)
    {
        if (!_syntaxTreeOwners.TryGetValue(owningSyntax, out SyntaxTree? syntaxTree) ||
            string.IsNullOrEmpty(syntaxTree.FilePath))
        {
            return;
        }

        string documentPath = Path.GetFullPath(syntaxTree.FilePath);
        if (!_documents.TryGetValue(documentPath, out ISymbolDocumentWriter? document))
            return;

        var loc = TextLocation.FromSpan(syntaxTree.Text, span);
        _il.MarkSequencePoint(document,
            loc.StartLine + 1,       // 0-based → 1-based
            loc.StartCharacter + 1,  // 0-based → 1-based
            loc.EndLine + 1,         // 0-based → 1-based
            loc.EndCharacter + 1);   // 0-based → 1-based
    }

    private void SetLocalSymInfo(LocalBuilder local, string name)
    {
        if (_documents.Count > 0)
            local.SetLocalSymInfo(name);
    }

}
