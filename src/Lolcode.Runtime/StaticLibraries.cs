namespace Lolcode.Runtime;

/// <summary>Creates a scope-bound LOLCODE library module.</summary>
/// <param name="scope">The scope importing the library.</param>
/// <returns>A new library module for the import.</returns>
public delegate LolObject LolcodeLibraryFactory(LolScope scope);

/// <summary>Implements one LOLCODE function supplied by a statically registered library.</summary>
/// <param name="context">The context for the active library invocation.</param>
/// <param name="arguments">The evaluated LOLCODE arguments.</param>
/// <returns>The LOLCODE result, or <see langword="null"/> for NOOB.</returns>
public delegate object? LolcodeLibraryFunction(
    LolcodeLibraryContext context,
    object?[] arguments);

/// <summary>
/// Describes one library factory declared in an application's static dependency closure.
/// </summary>
public sealed class LolcodeLibraryRegistration
{
    /// <summary>Gets the LOLCODE name used by <c>CAN HAS</c>.</summary>
    public string Name { get; }

    /// <summary>Gets the direct factory used to construct an imported module.</summary>
    public LolcodeLibraryFactory Factory { get; }

    /// <summary>Creates a static library registration.</summary>
    /// <param name="name">The LOLCODE import name.</param>
    /// <param name="factory">The direct module factory.</param>
    public LolcodeLibraryRegistration(string name, LolcodeLibraryFactory factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);
        Name = name;
        Factory = factory;
    }
}

/// <summary>
/// Builds one scope-bound library module without reflection or runtime type discovery.
/// Provider factories use this type to expose an explicit static-import contract.
/// </summary>
public sealed class LolcodeLibraryBuilder
{
    private readonly LolObject _module;
    private readonly LolcodeLibraryContext _importContext;

    /// <summary>Creates a builder for a library imported into <paramref name="scope"/>.</summary>
    /// <param name="scope">The importing LOLCODE scope.</param>
    public LolcodeLibraryBuilder(LolScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        _module = LolRuntime.CreateLibraryObject(scope);
        _importContext = new LolcodeLibraryContext(scope.Resources);
    }

    /// <summary>Adds a fixed-arity LOLCODE function to the module.</summary>
    /// <param name="name">The exported LOLCODE function name.</param>
    /// <param name="arity">The required number of arguments.</param>
    /// <param name="function">The direct function implementation.</param>
    public void AddFunction(string name, int arity, LolcodeLibraryFunction function)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(arity);
        ArgumentNullException.ThrowIfNull(function);
        if (_module.Values.ContainsKey(name))
            throw new ArgumentException($"The library already exports '{name}'.", nameof(name));

        LolParameterNameResolver[] resolvers = Enumerable.Range(0, arity)
            .Select(index => (LolParameterNameResolver)(caller =>
                new LolResolvedSlot(caller, $"arg{index}")))
            .ToArray();
        _module.Values.Add(name, new LolFunction(
            arity,
            (caller, _, arguments, _) =>
            {
                try
                {
                    LolcodeLibraryContext context = _importContext.ForInvocation(caller.Resources);
                    object? result = function(context, arguments);
                    if (result is LolBlob blob)
                        context.RegisterResource(blob);
                    return result;
                }
                catch (LolRuntimeException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new LolRuntimeException(exception.Message);
                }
            },
            resolvers));
    }

    /// <summary>Completes the module and returns it to the importing runtime.</summary>
    public LolObject Build() => _module;
}
