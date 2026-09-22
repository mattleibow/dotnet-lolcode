namespace Lolcode.Runtime;

public static partial class LolRuntime
{
    /// <summary>Creates a lexical child of an existing namespace.</summary>
    public static LolScope CreateChildScope(LolScope parent) => new(parent, parent.Caller);

    /// <summary>Creates a module BUKKIT whose resources are owned by the importing scope.</summary>
    public static LolObject CreateLibraryObject(LolScope importingScope) =>
        new(importingScope, importingScope.Caller) { IsLibraryModule = true };

    /// <summary>
    /// Creates a function invocation namespace. Library modules retain their receiver
    /// as lexical parent while the invocation caller owns resources and libraries.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public static LolScope CreateInvocationScope(LolScope caller, LolObject? receiver) =>
        receiver is { IsLibraryModule: true }
            ? new LolScope(receiver, receiver, caller.Resources, caller.Libraries)
            : new LolScope(caller, receiver ?? caller.Caller);

    /// <summary>Creates a BUKKIT with an optional prototype and copied mixins.</summary>
    public static LolObject CreateObject(LolScope scope, object? parent, object?[] mixins)
    {
        LolScope prototype = parent switch
        {
            null => scope,
            LolObject obj => obj,
            _ => throw new LolRuntimeException("BUKKIT prototype is not a BUKKIT"),
        };
        var result = new LolObject(prototype, prototype.Caller);
        for (int index = mixins.Length - 1; index >= 0; index--)
        {
            if (mixins[index] is not LolObject mixin)
                throw new LolRuntimeException("BUKKIT mixin is not a BUKKIT");
            foreach (var pair in mixin.Values)
                result.Values[pair.Key] = pair.Value;
        }
        return result;
    }

    /// <summary>Resolves an SRS expression to its identifier spelling.</summary>
    public static string ResolveIdentifierName(object? value) =>
        CastToYarn(ExplicitCast(value, "YARN"));

    /// <summary>Begins resolving an identifier path relative to a captured destination.</summary>
    public static LolIdentifierResolver BeginIdentifierPath(
        LolScope evaluationScope,
        LolScope destination) =>
        new(evaluationScope, destination);

    /// <summary>Captures the object selected by the preceding path segment.</summary>
    public static void PrepareIdentifierSegment(LolIdentifierResolver resolver)
    {
        if (resolver.Name is null)
            return;

        string name = resolver.Name;
        resolver.Current = Lookup(resolver.Current, name) as LolObject
            ?? throw new LolRuntimeException($"'{name}' is not a BUKKIT");
        resolver.Name = null;
        resolver.Traversed = true;
    }

    /// <summary>Adds one already evaluated segment to an identifier path.</summary>
    public static void SetIdentifierSegment(LolIdentifierResolver resolver, string name)
    {
        if (resolver.SegmentCount == 0)
        {
            if (name == "I")
            {
                resolver.SegmentCount++;
                return;
            }
            if (name == "ME")
            {
                resolver.Current = GetCallingObject(resolver.EvaluationScope);
                resolver.SegmentCount++;
                return;
            }
        }

        resolver.Name = name;
        resolver.SegmentCount++;
    }

    /// <summary>Finishes an identifier path as a captured terminal binding location.</summary>
    public static LolResolvedSlot ResolveIdentifierSlot(LolIdentifierResolver resolver)
    {
        if (resolver.Name is null)
            throw new LolRuntimeException("Identifier path does not select a binding");

        bool isIt = resolver.SegmentCount == 1 &&
            resolver.Name == "IT" &&
            ReferenceEquals(resolver.Current, resolver.EvaluationScope);
        bool rebaseable = !resolver.Traversed &&
            ReferenceEquals(resolver.Current, resolver.EvaluationScope);
        return new LolResolvedSlot(resolver.Current, resolver.Name, isIt, rebaseable);
    }

    /// <summary>Finishes an identifier path as a namespace or BUKKIT.</summary>
    public static LolScope ResolveIdentifierNamespace(LolIdentifierResolver resolver)
    {
        if (resolver.Name is null)
            return resolver.Current;
        return Lookup(resolver.Current, resolver.Name) as LolObject
            ?? throw new LolRuntimeException("Selected namespace is not a BUKKIT");
    }

    /// <summary>Reads a previously captured terminal binding.</summary>
    public static object? GetResolvedValue(LolResolvedSlot slot) =>
        slot.IsIt ? slot.Owner.It : Lookup(slot.Owner, slot.Name);

    /// <summary>Checks a declaration collision before evaluating its value or body.</summary>
    public static LolResolvedSlot ResolveDeclarationSlot(LolIdentifierResolver resolver)
    {
        LolResolvedSlot slot = ResolveIdentifierSlot(resolver);
        if (slot.Owner.Values.ContainsKey(slot.Name))
            throw new LolRuntimeException($"Binding already exists: {slot.Name}");
        return slot;
    }

    /// <summary>Initializes a declaration location that was checked before value evaluation.</summary>
    public static void DeclareResolvedValue(LolResolvedSlot slot, object? value) =>
        slot.Owner.Values[slot.Name] = ResolveAssignedYarn(value);

    /// <summary>Declares an invocation parameter using its caller-resolved identifier.</summary>
    public static void DeclareParameter(
        LolScope invocationScope,
        LolResolvedSlot slot,
        object? value)
    {
        if (slot.Rebaseable)
            slot = new LolResolvedSlot(invocationScope, slot.Name, slot.Name == "IT");
        if (slot.Owner.Values.ContainsKey(slot.Name))
            throw new LolRuntimeException($"Binding already exists: {slot.Name}");
        DeclareResolvedValue(slot, value);
    }

    /// <summary>Updates a previously captured binding location.</summary>
    public static void AssignResolvedValue(LolResolvedSlot slot, object? value)
    {
        value = ResolveAssignedYarn(value);
        if (slot.IsIt)
        {
            slot.Owner.It = value;
            return;
        }

        if (slot.Owner is LolObject obj && slot.Name == "parent")
        {
            obj.Prototype = value switch
            {
                null => null,
                LolObject prototype => prototype,
                _ => throw new LolRuntimeException("BUKKIT parent is not a BUKKIT"),
            };
            return;
        }

        var visited = new HashSet<LolScope>(ReferenceEqualityComparer.Instance);
        for (LolScope? current = slot.Owner; current is not null && visited.Add(current);)
        {
            if (current.Values.ContainsKey(slot.Name))
            {
                current.Values[slot.Name] = value;
                return;
            }
            current = current is LolObject currentObject
                ? currentObject.Prototype
                : current.Parent;
        }
        throw new LolRuntimeException($"Binding does not exist: {slot.Name}");
    }

    /// <summary>Reads a binding or slot through an evaluated identifier path.</summary>
    public static object? GetValue(LolScope scope, string[] path)
    {
        if (path.Length == 0)
            throw new LolRuntimeException("Empty identifier");
        if (path.Length == 1 && path[0] == "IT")
            return scope.It;
        LolScope current = ResolveStartingScope(scope, path[0], out int index);
        object? value = null;
        for (; index < path.Length; index++)
        {
            value = Lookup(current, path[index]);
            if (index + 1 < path.Length)
                current = value as LolObject
                    ?? throw new LolRuntimeException($"'{path[index]}' is not a BUKKIT");
        }
        return value;
    }

    /// <summary>
    /// Declares a binding in a selected namespace, rejecting an existing local BUKKIT slot.
    /// </summary>
    public static void DeclareValue(
        LolScope scope,
        string[] namespacePath,
        string[] namePath,
        object? value)
        => DeclareValueCore(scope, namespacePath, namePath, value);

    /// <summary>
    /// Declares a binding selected by an SRS identifier, rejecting local namespace collisions.
    /// </summary>
    public static void DeclareDynamicValue(
        LolScope scope,
        string[] namespacePath,
        string[] namePath,
        object? value)
        => DeclareValueCore(scope, namespacePath, namePath, value);

    private static void DeclareValueCore(
        LolScope scope,
        string[] namespacePath,
        string[] namePath,
        object? value)
    {
        LolScope destination = ResolveNamespace(scope, namespacePath);
        LolScope parent = TraverseToParent(scope, destination, namePath);
        string name = namePath[^1];
        if (parent.Values.ContainsKey(name))
            throw new LolRuntimeException($"Binding already exists: {name}");
        parent.Values[name] = ResolveAssignedYarn(value);
    }

    /// <summary>Updates an existing binding or BUKKIT slot.</summary>
    public static void AssignValue(LolScope scope, string[] path, object? value)
    {
        if (path.Length == 0)
            throw new LolRuntimeException("Empty identifier");

        value = ResolveAssignedYarn(value);
        if (path.Length == 1 && path[0] == "IT")
        {
            scope.It = value;
            return;
        }

        LolScope parent = TraverseToParent(scope, scope, path);
        string name = path[^1];

        if (parent is LolObject obj)
        {
            if (name == "parent")
            {
                obj.Prototype = value switch
                {
                    null => null,
                    LolObject prototype => prototype,
                    _ => throw new LolRuntimeException("BUKKIT parent is not a BUKKIT"),
                };
                return;
            }
        }

        var visited = new HashSet<LolScope>(ReferenceEqualityComparer.Instance);
        for (LolScope? current = parent; current is not null && visited.Add(current);)
        {
            if (current.Values.ContainsKey(name))
            {
                current.Values[name] = value;
                return;
            }
            current = current is LolObject currentObject
                ? currentObject.Prototype
                : current.Parent;
        }
        throw new LolRuntimeException($"Binding does not exist: {name}");
    }

    /// <summary>Installs a function value in a selected namespace.</summary>
    public static void DeclareFunction(
        LolScope scope,
        string[] namespacePath,
        string[] namePath,
        LolFunction function) =>
        DeclareValue(scope, namespacePath, namePath, function);

    /// <summary>
    /// Installs an SRS-selected function, rejecting local namespace collisions.
    /// </summary>
    public static void DeclareDynamicFunction(
        LolScope scope,
        string[] namespacePath,
        string[] namePath,
        LolFunction function) =>
        DeclareDynamicValue(scope, namespacePath, namePath, function);

    /// <summary>Invokes a function selected from a namespace or BUKKIT.</summary>
    public static object? Invoke(
        LolScope scope,
        string[] namespacePath,
        string[] functionPath,
        object?[] arguments)
    {
        LolFunctionTarget target =
            ResolveFunctionTarget(scope, namespacePath, functionPath, arguments.Length);
        var parameterNames = new LolResolvedSlot[arguments.Length];
        for (int index = 0; index < parameterNames.Length; index++)
            parameterNames[index] = ResolveParameterName(scope, target, index);
        return InvokeResolved(scope, target, parameterNames, arguments);
    }

    /// <summary>Captures a function and receiver before its arguments are evaluated.</summary>
    public static LolFunctionTarget ResolveFunctionTarget(
        LolScope scope,
        string[] namespacePath,
        string[] functionPath,
        int argumentCount)
    {
        LolScope destination = ResolveNamespace(scope, namespacePath);
        LolObject? receiver = destination as LolObject;
        LolScope current = destination;
        object? value = null;
        for (int index = 0; index < functionPath.Length; index++)
        {
            value = Lookup(current, functionPath[index]);
            if (index + 1 < functionPath.Length)
            {
                current = value as LolObject
                    ?? throw new LolRuntimeException($"'{functionPath[index]}' is not a BUKKIT");
                receiver = (LolObject)current;
            }
        }

        if (value is not LolFunction function)
            throw new LolRuntimeException($"Undefined function: {functionPath[^1]}");
        if (function.Arity != argumentCount)
            throw new LolRuntimeException(
                $"Function '{functionPath[^1]}' expects {function.Arity} arguments, got {argumentCount}");

        return new LolFunctionTarget(function, receiver);
    }

    /// <summary>Captures the callable currently stored at a resolved function slot.</summary>
    public static LolFunctionTarget ResolveFunctionSlot(
        LolResolvedSlot slot,
        int argumentCount)
    {
        object? value = GetResolvedValue(slot);
        if (value is not LolFunction function)
            throw new LolRuntimeException($"Undefined function: {slot.Name}");
        if (function.Arity != argumentCount)
            throw new LolRuntimeException(
                $"Function '{slot.Name}' expects {function.Arity} arguments, got {argumentCount}");
        return new LolFunctionTarget(function, slot.Owner as LolObject);
    }

    /// <summary>Invokes a previously captured function target.</summary>
    public static object? InvokeResolved(
        LolScope scope,
        LolFunctionTarget target,
        LolResolvedSlot[] parameterNames,
        object?[] arguments) =>
        target.Function.Body(scope, target.Receiver, arguments, parameterNames);

    /// <summary>Resolves one parameter identifier before its argument is evaluated.</summary>
    public static LolResolvedSlot ResolveParameterName(
        LolScope scope,
        LolFunctionTarget target,
        int index) =>
        target.Function.ParameterNameResolvers[index](scope);

    /// <summary>Gets the current scope's implicit IT value.</summary>
    public static object? GetIt(LolScope scope) => scope.It;

    /// <summary>Sets the current scope's implicit IT value.</summary>
    public static void SetIt(LolScope scope, object? value) => scope.It = value;

    private static LolScope ResolveNamespace(LolScope scope, string[] path)
    {
        if (path.Length == 1 && path[0] == "I")
            return scope;
        if (path.Length == 1 && path[0] == "ME")
            return GetCallingObject(scope);
        return GetValue(scope, path) as LolObject
            ?? throw new LolRuntimeException("Selected namespace is not a BUKKIT");
    }

    private static LolScope ResolveStartingScope(LolScope scope, string first, out int index)
    {
        if (first == "I")
        {
            index = 1;
            return scope;
        }
        if (first == "ME")
        {
            index = 1;
            return GetCallingObject(scope);
        }
        index = 0;
        return scope;
    }

    private static LolObject GetCallingObject(LolScope scope) =>
        scope.Caller ?? throw new LolRuntimeException("ME used without a calling BUKKIT");

    private static LolScope TraverseToParent(
        LolScope evaluationScope,
        LolScope destination,
        string[] path)
    {
        if (path.Length == 0)
            throw new LolRuntimeException("Empty identifier");
        LolScope current = destination;
        int index = 0;
        if (path[0] == "I")
            index = 1;
        else if (path[0] == "ME")
        {
            current = GetCallingObject(evaluationScope);
            index = 1;
        }
        for (; index + 1 < path.Length; index++)
        {
            current = Lookup(current, path[index]) as LolObject
                ?? throw new LolRuntimeException($"'{path[index]}' is not a BUKKIT");
        }
        return current;
    }

    private static object? Lookup(LolScope scope, string name)
    {
        if (scope is LolObject bukkit && name == "parent")
            return bukkit.Prototype;
        var visited = new HashSet<LolScope>(ReferenceEqualityComparer.Instance);
        for (LolScope? current = scope; current is not null && visited.Add(current);)
        {
            if (current.Values.TryGetValue(name, out object? value))
                return value;
            current = current is LolObject obj ? obj.Prototype : current.Parent;
        }
        throw new LolRuntimeException($"Binding does not exist: {name}");
    }

    private static object? ResolveAssignedYarn(object? value) =>
        value is YarnLiteral yarn ? ResolveUnicodeEscapes(yarn.Value) : value;
}
