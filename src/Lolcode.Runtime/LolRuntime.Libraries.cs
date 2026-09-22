using System.Reflection;
using System.Runtime.CompilerServices;

namespace Lolcode.Runtime;

public static partial class LolRuntime
{
    private static readonly ConditionalWeakTable<object, LolObject> GeneratedLibraries = new();

    // ==================== Namespaces and BUKKITs ====================

    /// <summary>Creates an empty root namespace.</summary>
    public static LolScope CreateScope() => new();

    /// <summary>Closes all managed BLOB handles created by a program scope.</summary>
    public static void DisposeScope(LolScope scope) => scope.Resources.Dispose();

    /// <summary>Throws when a disposed library instance is invoked.</summary>
    public static void ThrowIfDisposed(LolScope scope) => scope.Resources.ThrowIfDisposed();

    /// <summary>
    /// Transfers a BLOB returned through a public library wrapper to the managed caller.
    /// The caller owns and must dispose the returned handle.
    /// </summary>
    public static object? TransferPublicLibraryResult(LolScope scope, object? value)
    {
        var visitedScopes = new HashSet<LolScope>(ReferenceEqualityComparer.Instance);

        void TransferValue(object? candidate)
        {
            switch (candidate)
            {
                case LolBlob blob:
                    scope.Resources.Detach(blob);
                    break;
                case LolObject obj:
                    TransferScope(obj);
                    break;
            }
        }

        void TransferScope(LolScope candidate)
        {
            if (!visitedScopes.Add(candidate))
                return;

            TransferValue(candidate.It);
            foreach (object? member in candidate.Values.Values)
                TransferValue(member);

            // Follow only the lexical chain reachable through the returned BUKKIT's
            // prototype lookup, not unrelated caller/receiver state.
            if (candidate is LolObject obj)
            {
                if (obj.Prototype is not null)
                    TransferScope(obj.Prototype);
            }
            else if (candidate.Parent is not null)
            {
                TransferScope(candidate.Parent);
            }
        }

        TransferValue(value);
        return value;
    }

    /// <summary>Loads an explicitly registered library into the current scope.</summary>
    public static void LoadLibrary(LolScope scope, string name)
    {
        if (scope.Values.ContainsKey(name))
            return;
        if (!scope.Libraries.TryGet(name, out LolcodeLibraryDefinition definition))
            return;

        try
        {
            Type? type = Type.GetType(
                $"{definition.TypeName}, {definition.AssemblyName}",
                throwOnError: false);
            if (type is null)
                throw new LolRuntimeException(
                    $"LOLCODE library '{name}' is unavailable.");
            object? instance = null;
            try
            {
                instance = Activator.CreateInstance(type)
                    ?? throw new LolRuntimeException($"LOLCODE library '{name}' returned no instance.");
                LolObject library = GeneratedLibraries.TryGetValue(instance, out LolObject? generated)
                    ? generated
                    : CreateManagedLibrary(scope, type, instance);
                if (instance is IDisposable disposable)
                    scope.Resources.RegisterLibrary(disposable);
                scope.Values[name] = library;
            }
            catch
            {
                (instance as IDisposable)?.Dispose();
                throw;
            }
        }
        catch (TargetInvocationException ex)
        {
            throw new LolRuntimeException(
                ex.InnerException?.Message ?? $"Unable to construct LOLCODE library '{name}'.");
        }
        catch (LolRuntimeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LolRuntimeException($"Unable to construct LOLCODE library '{name}': {ex.Message}");
        }
    }

    /// <summary>Registers a compiler-discovered library definition for a scope.</summary>
    public static void RegisterLibraryDefinition(
        LolScope scope, string name, string assemblyName, string typeName) =>
        scope.Libraries.Register(name, assemblyName, typeName);

    /// <summary>
    /// Associates a compiler-generated CLR library instance with its persistent
    /// LOLCODE library BUKKIT.
    /// </summary>
    public static void RegisterGeneratedLibraryInstance(object instance, LolObject library)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(library);
        GeneratedLibraries.Add(instance, library);
    }

    private static LolObject CreateManagedLibrary(LolScope scope, Type type, object instance)
    {
        var library = new LolObject(scope, scope.Caller);
        MethodInfo[] methods = type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(static method => !method.IsSpecialName && method.Name != nameof(IDisposable.Dispose))
            .GroupBy(static method => method.Name, StringComparer.Ordinal)
            .Where(static group => group.Take(2).Count() == 1)
            .Select(static group => group.Single())
            .Where(IsSupportedManagedMethod)
            .OrderBy(static method => method.Name, StringComparer.Ordinal)
            .ToArray();

        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            string[] parameterNames = parameters
                .Select((parameter, index) => parameter.Name ?? $"arg{index}")
                .ToArray();
            LolParameterNameResolver[] resolvers = parameterNames
                .Select(parameterName =>
                    (LolParameterNameResolver)(caller => new LolResolvedSlot(caller, parameterName)))
                .ToArray();
            library.Values[method.Name] = new LolFunction(
                parameters.Length,
                (caller, _, arguments, _) =>
                {
                    return InvokeManagedMethod(method, instance, parameters, arguments, caller);
                },
                resolvers);
        }

        return library;
    }

    internal static bool IsSupportedManagedMethod(MethodInfo method)
    {
        if (method.IsGenericMethodDefinition || method.ContainsGenericParameters ||
            method.ReturnType.IsByRef || method.ReturnType.IsPointer || method.ReturnType.IsByRefLike ||
            !IsSupportedManagedReturnType(method.ReturnType))
        {
            return false;
        }

        return method.GetParameters().All(parameter =>
            !parameter.IsOut &&
            !parameter.ParameterType.IsByRef &&
            !parameter.ParameterType.IsPointer &&
            !parameter.ParameterType.IsByRefLike &&
            IsSupportedManagedValueType(parameter.ParameterType));
    }

    internal static object? ConvertManagedArgument(object? value, Type targetType)
    {
        if (targetType == typeof(object))
            return value;
        if (targetType == typeof(string))
            return CastToYarn(value);
        if (targetType == typeof(int))
            return CastToNumbr(value);
        if (targetType == typeof(double))
            return CastToNumbar(value);
        if (targetType == typeof(bool))
            return CastToTroof(value);

        throw new LolRuntimeException(
            $"Unsupported managed library parameter type: {targetType.FullName}");
    }

    private static bool IsSupportedManagedReturnType(Type type) =>
        type == typeof(void) || IsSupportedManagedValueType(type);

    private static bool IsSupportedManagedValueType(Type type) =>
        type == typeof(object) ||
        type == typeof(string) ||
        type == typeof(int) ||
        type == typeof(double) ||
        type == typeof(bool);

    internal static object? InvokeManagedMethod(
        MethodInfo method,
        object instance,
        ParameterInfo[] parameters,
        object?[] arguments,
        LolScope caller)
    {
        try
        {
            if (parameters.Length != arguments.Length)
                throw new LolRuntimeException("Managed library parameter count does not match LOLCODE call.");

            var convertedArguments = new object?[parameters.Length];
            for (int index = 0; index < arguments.Length; index++)
            {
                convertedArguments[index] =
                    ConvertManagedArgument(arguments[index], parameters[index].ParameterType);
            }

            object? result = method.Invoke(instance, convertedArguments);
            if (result is LolBlob blob && !blob.IsClosed)
                caller.Resources.Register(blob);
            return result;
        }
        catch (TargetInvocationException ex)
        {
            throw new LolRuntimeException(ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            throw new LolRuntimeException(ex.Message);
        }
    }
}
