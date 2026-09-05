using System.Globalization;
using System.Reflection;
using System.Text;

namespace Lolcode.Runtime;

internal sealed record LolByteYarn(byte[] Bytes);

internal static class YarnByteSink
{
    internal static void Write(TextWriter writer, byte[] bytes, bool suppressNewline)
    {
        if (TryGetStream(writer, out Stream stream))
        {
            writer.Flush();
            stream.Write(bytes);
            if (!suppressNewline)
                stream.Write(Encoding.UTF8.GetBytes(writer.NewLine));
            stream.Flush();
            return;
        }

        string text = Encoding.UTF8.GetString(bytes);
        if (suppressNewline)
            writer.Write(text);
        else
            writer.WriteLine(text);
    }

    private static bool TryGetStream(TextWriter writer, out Stream stream)
    {
        var visited = new HashSet<TextWriter>(ReferenceEqualityComparer.Instance);
        TextWriter? current = writer;
        while (current is not null && visited.Add(current))
        {
            if (current is StreamWriter streamWriter)
            {
                stream = streamWriter.BaseStream;
                return true;
            }

            current = current.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(static field => typeof(TextWriter).IsAssignableFrom(field.FieldType))
                .Select(field => field.GetValue(current) as TextWriter)
                .FirstOrDefault(static nested => nested is not null);
        }

        stream = null!;
        return false;
    }
}

/// <summary>
/// Runtime support library for compiled LOLCODE programs.
/// All LOLCODE values are represented as <see cref="object"/> at runtime.
/// Types: <see cref="int"/> (NUMBR), <see cref="double"/> (NUMBAR),
/// <see cref="string"/> (YARN), <see cref="bool"/> (TROOF), or <c>null</c> (NOOB).
/// </summary>
[System.Diagnostics.DebuggerNonUserCode]
public static class LolRuntime
{
    private sealed record YarnLiteral(string Value);

    // ==================== Namespaces and BUKKITs ====================

    /// <summary>Creates an empty root namespace.</summary>
    public static LolScope CreateScope() => new();

    /// <summary>Closes all managed BLOB handles created by a program scope.</summary>
    public static void DisposeScope(LolScope scope) => scope.Resources.Dispose();

    /// <summary>
    /// Loads a named built-in library into the current scope.
    /// Unknown names and duplicate imports are ignored.
    /// </summary>
    public static void LoadLibrary(LolScope scope, string name)
    {
        if (scope.Values.ContainsKey(name))
            return;
        LolObject? library = LolLibraries.Create(scope, name);
        if (library is not null)
            scope.Values[name] = library;
    }

    /// <summary>Creates a lexical child of an existing namespace.</summary>
    public static LolScope CreateChildScope(LolScope parent) => new(parent, parent.Caller);

    /// <summary>Creates a function invocation namespace.</summary>
    [System.Diagnostics.DebuggerStepThrough]
    public static LolScope CreateInvocationScope(LolScope caller, LolObject? receiver) =>
        new(caller, receiver ?? caller.Caller);

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

    // ==================== Type Coercion ====================

    /// <summary>
    /// Casts a value to TROOF (boolean).
    /// NOOB → FAIL, 0 → FAIL, 0.0 → FAIL, "" → FAIL, everything else → WIN.
    /// </summary>
    public static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            double d => d != 0.0,
            string s => s.Length > 0,
            YarnLiteral yarn => yarn.Value.Length > 0,
            LolByteYarn yarn => yarn.Bytes.Length > 0,
            LolObject => throw new LolRuntimeException("Cannot cast BUKKIT to TROOF"),
            LolFunction => throw new LolRuntimeException("Cannot cast function to TROOF"),
            LolBlob => throw new LolRuntimeException("Cannot cast BLOB to TROOF"),
            _ => true
        };
    }

    /// <summary>
    /// Casts a value to NUMBR (int).
    /// </summary>
    public static int CastToNumbr(object? value)
    {
        return value switch
        {
            null => 0,
            int i => i,
            double d => (int)d,
            bool b => b ? 1 : 0,
            string s => ParseNumbrPrefix(s),
            YarnLiteral yarn => ParseNumbrPrefix(ResolveUnicodeEscapes(yarn.Value)),
            LolByteYarn yarn => ParseNumbrPrefix(System.Text.Encoding.Latin1.GetString(yarn.Bytes)),
            LolObject => throw new LolRuntimeException("Cannot cast BUKKIT to NUMBR"),
            LolFunction => throw new LolRuntimeException("Cannot cast function to NUMBR"),
            LolBlob => throw new LolRuntimeException("Cannot cast BLOB to NUMBR"),
            _ => 0
        };
    }

    /// <summary>
    /// Casts a value to NUMBAR (double).
    /// </summary>
    public static double CastToNumbar(object? value)
    {
        return value switch
        {
            null => 0.0,
            int i => (double)i,
            double d => d,
            bool b => b ? 1.0 : 0.0,
            string s => ParseNumbarPrefix(s),
            YarnLiteral yarn => ParseNumbarPrefix(ResolveUnicodeEscapes(yarn.Value)),
            LolByteYarn yarn => ParseNumbarPrefix(System.Text.Encoding.Latin1.GetString(yarn.Bytes)),
            LolObject => throw new LolRuntimeException("Cannot cast BUKKIT to NUMBAR"),
            LolFunction => throw new LolRuntimeException("Cannot cast function to NUMBAR"),
            LolBlob => throw new LolRuntimeException("Cannot cast BLOB to NUMBAR"),
            _ => 0.0
        };
    }

    /// <summary>
    /// Casts a value to YARN (string).
    /// NUMBAR is truncated to 2 decimal places.
    /// </summary>
    public static string CastToYarn(object? value)
    {
        return value switch
        {
            null => throw new LolRuntimeException("Cannot cast NOOB to YARN"),
            bool => throw new LolRuntimeException("Cannot cast TROOF to YARN"),
            int i => i.ToString(CultureInfo.InvariantCulture),
            double d => FormatNumbar(d),
            string s => s,
            YarnLiteral yarn => ResolveYarnLiteral(yarn.Value),
            LolByteYarn yarn => System.Text.Encoding.Latin1.GetString(yarn.Bytes),
            LolObject => throw new LolRuntimeException("Cannot cast BUKKIT to YARN"),
            LolFunction => throw new LolRuntimeException("Cannot cast function to YARN"),
            LolBlob => throw new LolRuntimeException("Cannot cast BLOB to YARN"),
            _ => value.ToString() ?? ""
        };
    }

    /// <summary>Creates a source YARN whose Unicode escapes resolve when the value is used.</summary>
    public static object CreateYarnLiteral(string value) => new YarnLiteral(value);

    internal static object CreateByteYarn(byte value) => new LolByteYarn([value]);

    internal static byte[] GetYarnBytes(object? value) =>
        value is LolByteYarn yarn
            ? yarn.Bytes
            : Encoding.UTF8.GetBytes(CastToYarn(value));

    internal static byte[] GetExplicitYarnBytes(object? value) =>
        value is null
            ? []
            : value is LolByteYarn yarn
                ? yarn.Bytes
                : GetYarnBytes(ExplicitCast(value, "YARN"));

    /// <summary>
    /// Builds a source YARN by resolving its parsed interpolation names in the active scope.
    /// This compatibility API represents byte-backed YARNs as Latin-1 text.
    /// </summary>
    public static string InterpolateYarn(LolScope scope, string[] textParts, string[] names)
        => CastToYarn(InterpolateYarnValue(scope, textParts, names));

    /// <summary>
    /// Builds a source YARN while preserving byte-backed interpolated values.
    /// </summary>
    public static object InterpolateYarnValue(LolScope scope, string[] textParts, string[] names)
    {
        if (textParts.Length != names.Length + 1)
            throw new ArgumentException("Interpolation text and name counts do not match.");

        var values = new object?[textParts.Length + names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            values[index * 2] = ResolveUnicodeEscapes(textParts[index]);
            values[(index * 2) + 1] =
                names[index] == "IT" ? scope.It : Lookup(scope, names[index]);
        }
        values[^1] = ResolveUnicodeEscapes(textParts[^1]);
        return ConcatenateYarns(values);
    }

    /// <summary>Resolves Unicode escapes in a source YARN literal.</summary>
    public static string ResolveYarnLiteral(string value) => ResolveUnicodeEscapes(value);

    private static string ResolveUnicodeEscapes(string value)
    {
        if (!value.Contains(":(", StringComparison.Ordinal) &&
            !value.Contains(":[", StringComparison.Ordinal))
        {
            return value;
        }

        var result = new System.Text.StringBuilder(value.Length);
        for (int index = 0; index < value.Length;)
        {
            if (index + 2 < value.Length && value[index] == ':' && value[index + 1] == '(')
            {
                int end = value.IndexOf(')', index + 2);
                if (end < 0)
                    throw new LolRuntimeException("Invalid Unicode code point.");

                ReadOnlySpan<char> hex = value.AsSpan(index + 2, end - index - 2);
                if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int codePoint) ||
                    !System.Text.Rune.IsValid(codePoint))
                {
                    throw new LolRuntimeException("Invalid Unicode code point.");
                }

                result.Append(char.ConvertFromUtf32(codePoint));
                index = end + 1;
                continue;
            }

            if (index + 2 < value.Length && value[index] == ':' && value[index + 1] == '[')
            {
                int end = value.IndexOf(']', index + 2);
                if (end < 0)
                    throw new LolRuntimeException("Invalid Unicode normative name.");

                string name = value[(index + 2)..end];
                char? character = ResolveUnicodeName(name);
                if (!character.HasValue)
                    throw new LolRuntimeException($"Invalid Unicode normative name: {name}.");

                result.Append(character.Value);
                index = end + 1;
                continue;
            }

            result.Append(value[index]);
            index++;
        }

        return result.ToString();
    }

    private static char? ResolveUnicodeName(string name)
    {
        return name switch
        {
            "SPACE" => ' ',
            "TAB" or "CHARACTER TABULATION" => '\t',
            "NEWLINE" or "LINE FEED" or "LINE FEED (LF)" => '\n',
            "CARRIAGE RETURN" or "CARRIAGE RETURN (CR)" => '\r',
            "NULL" => '\0',
            "BELL" => '\a',
            "BACKSPACE" => '\b',
            "FORM FEED" or "FORM FEED (FF)" => '\f',
            "VERTICAL TAB" or "LINE TABULATION" => '\v',
            "QUOTATION MARK" => '"',
            "COLON" => ':',
            "EXCLAMATION MARK" => '!',
            "QUESTION MARK" => '?',
            "NUMBER SIGN" => '#',
            "DOLLAR SIGN" => '$',
            "CENT SIGN" => '\u00A2',
            "EURO SIGN" => '\u20AC',
            "PERCENT SIGN" => '%',
            "AMPERSAND" => '&',
            "APOSTROPHE" => '\'',
            "LEFT PARENTHESIS" => '(',
            "RIGHT PARENTHESIS" => ')',
            "ASTERISK" => '*',
            "PLUS SIGN" => '+',
            "COMMA" => ',',
            "HYPHEN-MINUS" => '-',
            "FULL STOP" => '.',
            "SOLIDUS" => '/',
            _ => null,
        };
    }

    private static int ParseNumbrPrefix(string value)
    {
        ReadOnlySpan<char> text = value.AsSpan().TrimStart();
        if (text.IsEmpty)
            return 0;

        int sign = 1;
        if (text[0] is '+' or '-')
        {
            sign = text[0] == '-' ? -1 : 1;
            text = text[1..];
        }

        int numberBase = 10;
        int prefixLength = 0;
        if (text.Length >= 2 && text[0] == '0' && text[1] is 'x' or 'X')
        {
            numberBase = 16;
            prefixLength = 2;
        }
        else if (text.Length > 1 && text[0] == '0')
        {
            numberBase = 8;
        }

        int digitCount = 0;
        long result = 0;
        for (int index = prefixLength; index < text.Length; index++)
        {
            int digit = DigitValue(text[index]);
            if (digit < 0 || digit >= numberBase)
                break;

            digitCount++;
            result = result * numberBase + digit;
            if (result > (long)int.MaxValue + (sign < 0 ? 1L : 0L))
                return sign < 0 ? int.MinValue : int.MaxValue;
        }

        if (digitCount == 0)
            return 0;

        long signed = sign * result;
        return (int)Math.Clamp(signed, int.MinValue, int.MaxValue);
    }

    private static int DigitValue(char character) => character switch
    {
        >= '0' and <= '9' => character - '0',
        >= 'a' and <= 'f' => character - 'a' + 10,
        >= 'A' and <= 'F' => character - 'A' + 10,
        _ => -1,
    };

    private static double ParseNumbarPrefix(string value)
    {
        ReadOnlySpan<char> text = value.AsSpan().TrimStart();
        int index = 0;
        if (index < text.Length && text[index] is '+' or '-')
            index++;

        int wholeDigits = ConsumeDecimalDigits(text, ref index);
        int fractionalDigits = 0;
        if (index < text.Length && text[index] == '.')
        {
            index++;
            fractionalDigits = ConsumeDecimalDigits(text, ref index);
        }

        if (wholeDigits == 0 && fractionalDigits == 0)
            return 0.0;

        int exponentStart = index;
        if (index < text.Length && text[index] is 'e' or 'E')
        {
            index++;
            if (index < text.Length && text[index] is '+' or '-')
                index++;
            if (ConsumeDecimalDigits(text, ref index) == 0)
                index = exponentStart;
        }

        return double.TryParse(
            text[..index],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double result)
            ? result
            : 0.0;
    }

    private static int ConsumeDecimalDigits(ReadOnlySpan<char> text, ref int index)
    {
        int start = index;
        while (index < text.Length && text[index] is >= '0' and <= '9')
            index++;
        return index - start;
    }

    private static string FormatNumbar(double value)
    {
        if (double.IsFinite(value) && Math.Abs(value) <= (double)(decimal.MaxValue / 100m))
        {
            var truncated = decimal.Truncate((decimal)value * 100m) / 100m;
            return truncated.ToString("F2", CultureInfo.InvariantCulture);
        }

        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Casts a value to TROOF.
    /// </summary>
    public static bool CastToTroof(object? value) => IsTruthy(value);

    /// <summary>
    /// Performs an explicit MAEK or IS NOW A cast.
    /// </summary>
    public static object? ExplicitCast(object? value, string targetType)
    {
        return targetType switch
        {
            "TROOF" => (object)CastToTroof(value),
            "NUMBR" => (object)CastToNumbr(value),
            "NUMBAR" => (object)CastToNumbar(value),
            "YARN" when value is null => string.Empty,
            "YARN" when value is LolByteYarn => value,
            "YARN" => (object)CastToYarn(value),
            "NOOB" => null,
            _ => throw new InvalidOperationException($"Unknown type: {targetType}")
        };
    }

    // ==================== Arithmetic ====================

    /// <summary>
    /// Coerces a value to a numeric type for arithmetic.
    /// NOOB → runtime error (per spec: "Any operations on a NOOB that assume another type result in an error").
    /// Non-numeric YARN → runtime error.
    /// bool: WIN→1, FAIL→0.
    /// </summary>
    private static object CoerceToNumeric(object? value)
    {
        return value switch
        {
            int => value,
            double => value,
            bool b => b ? 1 : 0,
            YarnLiteral yarn => CoerceToNumeric(ResolveUnicodeEscapes(yarn.Value)),
            LolByteYarn yarn => CoerceToNumeric(Encoding.Latin1.GetString(yarn.Bytes)),
            string s when s.Contains('.') => double.TryParse(s, CultureInfo.InvariantCulture, out double d)
                ? (object)d
                : throw new LolRuntimeException("Cannot cast YARN to numeric: " + s),
            string s => int.TryParse(s, CultureInfo.InvariantCulture, out int i)
                ? (object)i
                : throw new LolRuntimeException("Cannot cast YARN to numeric: " + s),
            null => throw new LolRuntimeException("Cannot use NOOB in arithmetic"),
            _ => throw new LolRuntimeException("Cannot use value in arithmetic: " + value)
        };
    }

    /// <summary>
    /// Determines if the result should be NUMBAR (double).
    /// If either operand is double, result is double.
    /// </summary>
    private static bool IsFloatOperation(object a, object b) => a is double || b is double;

    /// <summary>SUM OF a AN b</summary>
    public static object Add(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
            return CastToNumbar(ca) + CastToNumbar(cb);
        return CastToNumbr(ca) + CastToNumbr(cb);
    }

    /// <summary>DIFF OF a AN b</summary>
    public static object Subtract(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
            return CastToNumbar(ca) - CastToNumbar(cb);
        return CastToNumbr(ca) - CastToNumbr(cb);
    }

    /// <summary>PRODUKT OF a AN b</summary>
    public static object Multiply(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
            return CastToNumbar(ca) * CastToNumbar(cb);
        return CastToNumbr(ca) * CastToNumbr(cb);
    }

    /// <summary>QUOSHUNT OF a AN b</summary>
    public static object Divide(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
        {
            double divisor = CastToNumbar(cb);
            if (divisor == 0.0)
                throw new LolRuntimeException("Division by zero");
            return CastToNumbar(ca) / divisor;
        }
        int intDivisor = CastToNumbr(cb);
        if (intDivisor == 0)
            throw new LolRuntimeException("Division by zero");
        return CastToNumbr(ca) / intDivisor;
    }

    /// <summary>MOD OF a AN b</summary>
    public static object Modulo(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
        {
            double divisor = CastToNumbar(cb);
            if (divisor == 0.0)
                throw new LolRuntimeException("Modulo by zero");
            return CastToNumbar(ca) % divisor;
        }
        int intDivisor = CastToNumbr(cb);
        if (intDivisor == 0)
            throw new LolRuntimeException("Modulo by zero");
        return CastToNumbr(ca) % intDivisor;
    }

    /// <summary>BIGGR OF a AN b</summary>
    public static object Greater(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
        {
            double da = CastToNumbar(ca), db = CastToNumbar(cb);
            return da >= db ? da : db;
        }
        int ia = CastToNumbr(ca), ib = CastToNumbr(cb);
        return ia >= ib ? ia : ib;
    }

    /// <summary>SMALLR OF a AN b</summary>
    public static object Smaller(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
        {
            double da = CastToNumbar(ca), db = CastToNumbar(cb);
            return da <= db ? da : db;
        }
        int ia = CastToNumbr(ca), ib = CastToNumbr(cb);
        return ia <= ib ? ia : ib;
    }

    // ==================== Boolean Operations ====================

    /// <summary>BOTH OF a AN b (AND)</summary>
    public static bool And(object? a, object? b) => IsTruthy(a) && IsTruthy(b);

    /// <summary>EITHER OF a AN b (OR)</summary>
    public static bool Or(object? a, object? b) => IsTruthy(a) || IsTruthy(b);

    /// <summary>WON OF a AN b (XOR)</summary>
    public static bool Xor(object? a, object? b) => IsTruthy(a) ^ IsTruthy(b);

    /// <summary>NOT a</summary>
    public static bool Not(object? a) => !IsTruthy(a);

    // ==================== Comparison ====================

    /// <summary>
    /// BOTH SAEM: equality with NO auto-casting between different type families.
    /// NUMBR/NUMBAR promotes to NUMBAR. All other cross-type comparisons → FAIL.
    /// </summary>
    public static bool BothSaem(object? a, object? b)
    {
        a = ResolveYarnLiteral(a);
        b = ResolveYarnLiteral(b);

        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        if (IsYarn(a) && IsYarn(b))
            return GetYarnBytes(a).AsSpan().SequenceEqual(GetYarnBytes(b));

        // Same type comparison
        if (a.GetType() == b.GetType())
        {
            return a.Equals(b);
        }

        // NUMBR/NUMBAR cross-promotion
        if ((a is int || a is double) && (b is int || b is double))
        {
            return CastToNumbar(a) == CastToNumbar(b);
        }

        // Different type families → FAIL (no auto-casting)
        return false;
    }

    /// <summary>DIFFRINT: inequality (opposite of BOTH SAEM).</summary>
    public static bool Diffrint(object? a, object? b) => !BothSaem(a, b);

    /// <summary>Matches a WTF? case using exact runtime type and value equality.</summary>
    public static bool SwitchCaseMatches(object? value, object? caseValue)
    {
        value = ResolveYarnLiteral(value);
        caseValue = ResolveYarnLiteral(caseValue);

        if (value is null || caseValue is null)
            return value is null && caseValue is null;
        if (IsYarn(value) && IsYarn(caseValue))
            return GetYarnBytes(value).AsSpan().SequenceEqual(GetYarnBytes(caseValue));
        return value.GetType() == caseValue.GetType() && value.Equals(caseValue);
    }

    private static object? ResolveYarnLiteral(object? value) =>
        value switch
        {
            YarnLiteral yarn => ResolveUnicodeEscapes(yarn.Value),
            _ => value,
        };

    private static bool IsYarn(object value) => value is string or LolByteYarn;

    // ==================== String Operations ====================

    /// <summary>
    /// SMOOSH: concatenate all arguments after casting each to YARN.
    /// </summary>
    public static string Smoosh(params object?[] args) => CastToYarn(SmooshValue(args));

    /// <summary>SMOOSH preserving the exact bytes of byte-backed YARN operands.</summary>
    public static object SmooshValue(params object?[] args) => ConcatenateYarns(args);

    private static object ConcatenateYarns(object?[] values)
    {
        if (!values.Any(static value => value is LolByteYarn))
        {
            var text = new StringBuilder();
            foreach (object? value in values)
                text.Append(CastToYarn(value));
            return text.ToString();
        }

        var bytes = new List<byte>();
        foreach (object? value in values)
            bytes.AddRange(GetYarnBytes(value));
        return new LolByteYarn([.. bytes]);
    }

    // ==================== I/O ====================

    /// <summary>
    /// VISIBLE: print arguments concatenated as YARN.
    /// </summary>
    public static void Print(object?[] args, bool suppressNewline) =>
        Print(args, suppressNewline, standardError: false);

    /// <summary>
    /// VISIBLE or INVISIBLE: print arguments concatenated as YARN to the selected stream.
    /// </summary>
    public static void Print(object?[] args, bool suppressNewline, bool standardError)
    {
        TextWriter writer = standardError ? Console.Error : Console.Out;
        if (args.Any(static arg => arg is LolByteYarn))
        {
            byte[] bytes = GetYarnBytes(ConcatenateYarns(args));
            YarnByteSink.Write(writer, bytes, suppressNewline);
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var arg in args)
            sb.Append(CastToYarn(arg));

        if (suppressNewline)
            writer.Write(sb.ToString());
        else
            writer.WriteLine(sb.ToString());
    }

    /// <summary>Executes a command through the host shell and returns standard output as text.</summary>
    public static string ExecuteSystemCommand(object? command) =>
        DecodeProcessOutput(GetYarnBytes(ExecuteSystemCommandValue(command)));

    /// <summary>
    /// Executes a command through the host shell and returns standard output while preserving its bytes.
    /// </summary>
    public static object ExecuteSystemCommandValue(object? command)
    {
        string commandText = CastToYarn(ExplicitCast(command, "YARN"));
        bool windows = OperatingSystem.IsWindows();
        string shell = windows
            ? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe"
            : "/bin/sh";
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = shell,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(windows ? "/C" : "-c");
        startInfo.ArgumentList.Add(commandText);

        try
        {
            using var process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new LolRuntimeException($"Unable to launch system shell: {shell}");
            Task<byte[]> output = ReadAllBytesAsync(process.StandardOutput.BaseStream);
            Task<byte[]> error = ReadAllBytesAsync(process.StandardError.BaseStream);
            process.WaitForExit();
            byte[] outputBytes = output.GetAwaiter().GetResult();
            byte[] errorBytes = error.GetAwaiter().GetResult();
            if (errorBytes.Length > 0)
                YarnByteSink.Write(Console.Error, errorBytes, suppressNewline: true);
            return new LolByteYarn(outputBytes);
        }
        catch (LolRuntimeException)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or System.ComponentModel.Win32Exception or
                IOException or UnauthorizedAccessException)
        {
            throw new LolRuntimeException($"Unable to execute system command: {commandText}", ex);
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var result = new MemoryStream();
        await stream.CopyToAsync(result);
        return result.ToArray();
    }

    private static string DecodeProcessOutput(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// GIMMEH: read a line of input.
    /// </summary>
    public static string ReadLine()
    {
        return Console.ReadLine() ?? "";
    }

    /// <summary>Writes the UTF-8 byte-order mark preserved from source.</summary>
    public static void WriteByteOrderMark() => Console.Write('\uFEFF');
}

/// <summary>
/// Exception thrown for runtime errors in LOLCODE programs.
/// </summary>
public class LolRuntimeException : Exception
{
    public LolRuntimeException(string message) : base(message) { }
    public LolRuntimeException(string message, Exception inner) : base(message, inner) { }
}
