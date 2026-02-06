using Lolcode.Compiler;
using Lolcode.Compiler.Text;

namespace Lolcode.Cli;

/// <summary>
/// Entry point for the LOLCODE CLI compiler.
/// Usage: lolcode run &lt;file.lol&gt;
///        lolcode compile &lt;file.lol&gt; [-o output.dll]
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
            var result = Compilation.Compile(sourceText, outputPath, runtimePath);

            if (!result.Success)
            {
                PrintDiagnostics(result.Diagnostics);
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
            Console.Error.WriteLine("Usage: lolcode compile <file.lol> [-o output.dll]");
            return 1;
        }

        string filePath = args[0];
        string? outputPath = null;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] is "-o" or "--output" && i + 1 < args.Length)
            {
                outputPath = args[++i];
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

        var result = Compilation.Compile(sourceText, outputPath, runtimePath);

        if (!result.Success)
        {
            PrintDiagnostics(result.Diagnostics);
            return 1;
        }

        // Copy runtime next to output
        string outputDir = Path.GetDirectoryName(outputPath) ?? ".";
        string runtimeDest = Path.Combine(outputDir, "Lolcode.Runtime.dll");
        if (!File.Exists(runtimeDest))
            File.Copy(runtimePath, runtimeDest, overwrite: true);

        Console.WriteLine($"Compiled successfully: {result.OutputPath}");
        return 0;
    }

    private static void PrintDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
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
              lolcode run <file.lol>                  Compile and run a LOLCODE file
              lolcode compile <file.lol> [-o out.dll]  Compile a LOLCODE file to a DLL
              lolcode --help                           Show this help message
              lolcode --version                        Show version information

            Examples:
              lolcode run hello.lol
              lolcode compile hello.lol -o hello.dll
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
