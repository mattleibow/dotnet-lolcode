using System.Globalization;
using System.Text;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.CodeAnalysis.Text;
using Microsoft.Build.Framework;

namespace Lolcode.Build;

/// <summary>
/// MSBuild task that compiles LOLCODE source files (.lol) into a .NET assembly.
/// This is the core integration point between the MSBuild build system and the
/// LOLCODE compiler (<see cref="LolcodeCompilation"/>).
/// </summary>
public sealed class Lolc : Microsoft.Build.Utilities.Task
{
    /// <summary>Source .lol files to compile.</summary>
    [Required]
    public ITaskItem[] Sources { get; set; } = [];

    /// <summary>Path to the output assembly (e.g., obj/Debug/net10.0/MyApp.dll).</summary>
    [Required]
    public ITaskItem OutputAssembly { get; set; } = null!;

    /// <summary>Path to Lolcode.Runtime.dll for the compiler to reference.</summary>
    [Required]
    public string RuntimeAssemblyPath { get; set; } = null!;

    /// <summary>Assembly references (from NuGet, framework, project references).</summary>
    public ITaskItem[] ReferencePath { get; set; } = [];

    /// <summary>The assembly name (defaults to project name).</summary>
    public string AssemblyName { get; set; } = "";

    /// <summary>Output type: Exe or Library.</summary>
    public string OutputType { get; set; } = "Exe";

    /// <summary>
    /// Fully qualified CLR type name for a library's LOLCODE export container,
    /// as composed by the SDK from <c>RootNamespace</c> and
    /// <c>LolcodeLibraryTypeName</c>. Ignored for executable output.
    /// </summary>
    public string LolcodeLibraryTypeName { get; set; } = "";

    /// <summary>LOLCODE <c>CAN HAS</c> name for generated library output.</summary>
    public string LolcodeLibraryName { get; set; } = "";

    /// <summary>
    /// When true, skip actual compilation (design-time builds).
    /// Visual Studio calls this during design-time to gather metadata without compiling.
    /// </summary>
    public bool SkipCompilerExecution { get; set; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        if (SkipCompilerExecution)
        {
            Log.LogMessage(MessageImportance.Low, "Lolc: Skipping compilation (design-time build).");
            return true;
        }

        if (Sources.Length == 0)
        {
            Log.LogMessage(MessageImportance.Normal, "Lolc: No LOLCODE source files to compile.");
            return true;
        }

        var outputPath = OutputAssembly.ItemSpec;

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        Log.LogMessage(MessageImportance.Normal,
            "Lolc: Compiling {0} source file(s) to {1}", Sources.Length, outputPath);
        try
        {
            // Parse all source files
            var trees = new SyntaxTree[Sources.Length];
            for (int i = 0; i < Sources.Length; i++)
            {
                var filePath = Sources[i].ItemSpec;
                if (!File.Exists(filePath))
                {
                    Log.LogError(subcategory: null, errorCode: "LOL9000", helpKeyword: null,
                        file: filePath, lineNumber: 0, columnNumber: 0,
                        endLineNumber: 0, endColumnNumber: 0,
                        message: "Source file not found: {0}", filePath);
                    return false;
                }

                var source = File.ReadAllText(filePath);
                var sourceText = SourceText.From(source, filePath);
                trees[i] = SyntaxTree.ParseText(sourceText, filePath);
            }

            // Compile
            var compilation = LolcodeCompilation.Create(trees);
            var result = compilation.Emit(
                outputPath,
                RuntimeAssemblyPath,
                ReferencePath.Select(reference => reference.ItemSpec),
                OutputType,
                LolcodeLibraryTypeName,
                LolcodeLibraryName);

            // Report diagnostics in MSBuild format
            foreach (var diagnostic in result.Diagnostics)
            {
                var location = diagnostic.Location;
                var file = location.FileName ?? Sources[0].ItemSpec;
                int line = location.StartLine + 1;
                int col = location.StartCharacter + 1;
                int endLine = location.EndLine + 1;
                int endCol = location.EndCharacter + 1;

                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    Log.LogError(subcategory: null, errorCode: diagnostic.Id, helpKeyword: null,
                        file: file, lineNumber: line, columnNumber: col,
                        endLineNumber: endLine, endColumnNumber: endCol,
                        message: diagnostic.Message);
                }
                else if (diagnostic.Severity == DiagnosticSeverity.Warning)
                {
                    Log.LogWarning(subcategory: null, warningCode: diagnostic.Id, helpKeyword: null,
                        file: file, lineNumber: line, columnNumber: col,
                        endLineNumber: endLine, endColumnNumber: endCol,
                        message: diagnostic.Message);
                }
                else
                {
                    Log.LogMessage(MessageImportance.Normal,
                        "{0}({1},{2}): {3}: {4}", file, line, col, diagnostic.Id, diagnostic.Message);
                }

            }

            if (result.Success)
            {
                Log.LogMessage(MessageImportance.Normal,
                    "Lolc: Successfully compiled to {0}", result.OutputPath);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            Log.LogError(subcategory: null, errorCode: "LOL9001", helpKeyword: null,
                file: null, lineNumber: 0, columnNumber: 0,
                endLineNumber: 0, endColumnNumber: 0,
                message: "Internal compiler error: {0}", ex.Message);
            return false;
        }
    }
}

/// <summary>
/// Converts an assembly name into a valid, non-keyword CLR identifier for use
/// as a generated C#-consumable type name.
/// </summary>
public sealed class MakeValidClrIdentifier : Microsoft.Build.Utilities.Task
{
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
            "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
            "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
            "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
            "object", "operator", "out", "override", "params", "private", "protected", "public",
            "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
            "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while",
        };

    /// <summary>Assembly name to convert.</summary>
    [Required]
    public string Input { get; set; } = "";

    /// <summary>Converted valid CLR identifier.</summary>
    [Output]
    public string Identifier { get; private set; } = "";

    /// <summary>Whether to generate a direct LOLCODE identifier instead of a CLR identifier.</summary>
    public bool Lolcode { get; set; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        Identifier = Lolcode ? NormalizeLolcode(Input) : Normalize(Input);
        return true;
    }

    internal static string Normalize(string input)
    {
        var builder = new StringBuilder();
        var isFirst = true;
        foreach (Rune rune in input.EnumerateRunes())
        {
            if (isFirst)
            {
                if (IsIdentifierStart(rune))
                {
                    builder.Append(rune.ToString());
                }
                else
                {
                    builder.Append('_');
                    if (IsIdentifierPart(rune))
                        builder.Append(rune.ToString());
                }

                isFirst = false;
            }
            else if (IsIdentifierPart(rune))
            {
                builder.Append(rune.ToString());
            }
            else
            {
                builder.Append('_');
            }
        }

        string identifier = builder.Length == 0 ? "_" : builder.ToString();
        if (CSharpKeywords.Contains(identifier))
            return $"_{identifier}";

        return identifier;
    }

    private static bool IsIdentifierStart(Rune rune) =>
        rune.Value <= char.MaxValue &&
        (rune.Value == '_' || Rune.GetUnicodeCategory(rune) is
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or
            UnicodeCategory.LetterNumber);

    private static bool IsIdentifierPart(Rune rune) =>
        rune.Value <= char.MaxValue &&
        (IsIdentifierStart(rune) || Rune.GetUnicodeCategory(rune) is
            UnicodeCategory.DecimalDigitNumber or
            UnicodeCategory.ConnectorPunctuation or
            UnicodeCategory.NonSpacingMark or
            UnicodeCategory.SpacingCombiningMark);

    internal static string NormalizeLolcode(string input)
    {
        var builder = new StringBuilder();
        foreach (char character in input)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');

        if (builder.Length == 0)
            return "Library";
        if (!char.IsLetter(builder[0]))
            builder.Insert(0, "L_");
        return builder.ToString();
    }
}

/// <summary>
/// Converts every dot-separated namespace segment to a valid, non-keyword CLR
/// identifier while preserving its original order and repetitions.
/// </summary>
public sealed class NormalizeClrNamespace : Microsoft.Build.Utilities.Task
{
    /// <summary>Namespace to normalize.</summary>
    [Required]
    public string Input { get; set; } = "";

    /// <summary>Namespace with every segment normalized for CLR consumption.</summary>
    [Output]
    public string NormalizedNamespace { get; private set; } = "";

    /// <inheritdoc/>
    public override bool Execute()
    {
        NormalizedNamespace = string.Join(
            ".",
            Input.Split('.', StringSplitOptions.None).Select(MakeValidClrIdentifier.Normalize));
        return true;
    }

}
