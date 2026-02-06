using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.Cli;

/// <summary>
/// Entry point for the LOLCODE CLI compiler.
/// Usage: lolcode run &lt;file.lol&gt;
///        lolcode compile &lt;file.lol&gt; [-o output.dll] [--emit-il] [--emit-csharp]
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        string command = args[0].ToLowerInvariant();

        return command switch
        {
            "run" => RunCommand(args[1..]),
            "compile" => CompileCommand(args[1..]),
            "--help" or "-h" => PrintUsage(),
            "--version" or "-v" => PrintVersion(),
            _ => RunFile(args[0]) // Default: treat first arg as file to run
        };
    }

    private static int RunCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Error: No input file specified.");
            Console.Error.WriteLine("Usage: lolcode run <file.lol>");
            return 1;
        }

        return RunFile(args[0]);
    }

    private static int RunFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Error: File not found: {filePath}");
            return 1;
        }

        string source = File.ReadAllText(filePath);
        var sourceText = SourceText.From(source, filePath);

        string runtimePath = FindRuntimeAssembly();
        string tempDir = Path.Combine(Path.GetTempPath(), "lolcode", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            string outputPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(filePath) + ".dll");
            var tree = SyntaxTree.ParseText(sourceText, filePath);
            var compilation = LolcodeCompilation.Create(tree);
            var result = compilation.Emit(outputPath, runtimePath);

            if (!result.Success)
            {
                PrintDiagnostics(result.Diagnostics, filePath);
                return 1;
            }

            // Copy runtime DLL next to compiled output
            string runtimeDest = Path.Combine(tempDir, "Lolcode.Runtime.dll");
            File.Copy(runtimePath, runtimeDest, overwrite: true);

            // Run the compiled assembly
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = result.OutputPath!,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                }
            };

            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static int CompileCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Error: No input file specified.");
            Console.Error.WriteLine("Usage: lolcode compile <file.lol> [-o output.dll] [--emit-il] [--emit-csharp]");
            return 1;
        }

        string filePath = args[0];
        string? outputPath = null;
        bool emitIl = false;
        bool emitCsharp = false;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] is "-o" or "--output" && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
            else if (args[i] is "--emit-il")
            {
                emitIl = true;
            }
            else if (args[i] is "--emit-csharp" or "--emit-cs")
            {
                emitCsharp = true;
            }
        }

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Error: File not found: {filePath}");
            return 1;
        }

        outputPath ??= Path.ChangeExtension(filePath, ".dll");

        string source = File.ReadAllText(filePath);
        var sourceText = SourceText.From(source, filePath);
        string runtimePath = FindRuntimeAssembly();

        var tree = SyntaxTree.ParseText(sourceText, filePath);
        var compilation = LolcodeCompilation.Create(tree);
        var result = compilation.Emit(outputPath, runtimePath);

        if (!result.Success)
        {
            PrintDiagnostics(result.Diagnostics, filePath);
            return 1;
        }

        // Copy runtime next to output
        string outputDir = Path.GetDirectoryName(outputPath) ?? ".";
        string runtimeDest = Path.Combine(outputDir, "Lolcode.Runtime.dll");
        if (!File.Exists(runtimeDest))
            File.Copy(runtimePath, runtimeDest, overwrite: true);

        Console.WriteLine($"Compiled successfully: {result.OutputPath}");

        if (emitIl || emitCsharp)
        {
            EmitDecompiled(result.OutputPath!, emitIl ? "-il" : "");
        }

        return 0;
    }

    private static void EmitDecompiled(string dllPath, string ilSpyArgs)
    {
        // Try to find ilspycmd
        string? ilspycmd = FindIlSpyCmd();
        if (ilspycmd == null)
        {
            Console.Error.WriteLine("Warning: ilspycmd not found. Install with: dotnet tool install -g ilspycmd");
            return;
        }

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ilspycmd,
                Arguments = $"{ilSpyArgs} \"{dllPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            Console.WriteLine();
            Console.WriteLine(output);
        }
        else
        {
            string error = process.StandardError.ReadToEnd();
            Console.Error.WriteLine($"ilspycmd failed: {error}");
        }
    }

    private static string? FindIlSpyCmd()
    {
        // Check PATH first
        try
        {
            var which = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "ilspycmd",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            which.Start();
            string path = which.StandardOutput.ReadToEnd().Trim();
            which.WaitForExit();
            if (which.ExitCode == 0 && !string.IsNullOrEmpty(path))
                return path;
        }
        catch { /* ignore */ }

        // Check common .NET tool location
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string toolPath = Path.Combine(home, ".dotnet", "tools", "ilspycmd");
        if (File.Exists(toolPath))
            return toolPath;

        return null;
    }

    private static void PrintDiagnostics(IEnumerable<Diagnostic> diagnostics, string? sourceFilePath = null)
    {
        string[]? sourceLines = null;
        if (sourceFilePath != null && File.Exists(sourceFilePath))
        {
            try { sourceLines = File.ReadAllLines(sourceFilePath); }
            catch { /* best effort */ }
        }

        foreach (var diagnostic in diagnostics)
        {
            var color = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => ConsoleColor.Red,
                DiagnosticSeverity.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.Gray
            };

            Console.ForegroundColor = color;
            Console.Error.WriteLine(diagnostic);
            Console.ResetColor();

            // Show source context if available
            if (sourceLines != null)
            {
                int lineIndex = diagnostic.Location.StartLine;
                if (lineIndex >= 0 && lineIndex < sourceLines.Length)
                {
                    string lineText = sourceLines[lineIndex];
                    int col = diagnostic.Location.StartCharacter;

                    Console.Error.WriteLine($"  {lineText}");
                    Console.ForegroundColor = color;
                    Console.Error.WriteLine($"  {new string(' ', Math.Max(0, col))}^");
                    Console.ResetColor();
                }
            }
        }
    }

    private static string FindRuntimeAssembly()
    {
        // Look relative to CLI executable
        string exeDir = AppContext.BaseDirectory;
        string runtimePath = Path.Combine(exeDir, "Lolcode.Runtime.dll");
        if (File.Exists(runtimePath))
            return runtimePath;

        // Look in the same directory as the source project
        string devPath = Path.Combine(exeDir, "..", "..", "..", "..", "Lolcode.Runtime", "bin", "Debug", "net10.0", "Lolcode.Runtime.dll");
        if (File.Exists(devPath))
            return Path.GetFullPath(devPath);

        throw new FileNotFoundException("Could not find Lolcode.Runtime.dll. Ensure the runtime project is built.");
    }

    private static int PrintUsage()
    {
        Console.WriteLine("""
            LOLCODE Compiler for .NET

            Usage:
              lolcode run <file.lol>                           Compile and run a LOLCODE file
              lolcode compile <file.lol> [-o out.dll]          Compile a LOLCODE file to a DLL
              lolcode compile <file.lol> --emit-il             Compile and show IL disassembly
              lolcode compile <file.lol> --emit-csharp         Compile and show decompiled C#
              lolcode --help                                   Show this help message
              lolcode --version                                Show version information

            Examples:
              lolcode run hello.lol
              lolcode compile hello.lol -o hello.dll
              lolcode compile hello.lol --emit-il
              dotnet run --project src/Lolcode.Cli -- run samples/01-hello-world/hello.lol
            """);
        return 0;
    }

    private static int PrintVersion()
    {
        Console.WriteLine("lolcode 0.1.0-alpha");
        return 0;
    }
}
