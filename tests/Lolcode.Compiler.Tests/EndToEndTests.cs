using System.Diagnostics;
using Lolcode.Compiler;
using Lolcode.Compiler.Text;

namespace Lolcode.Compiler.Tests;

/// <summary>
/// End-to-end tests that compile LOLCODE programs, run the resulting DLL,
/// and verify stdout matches expected output.
/// </summary>
public class EndToEndTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _runtimeDll;

    public EndToEndTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lolcode-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        // Find the runtime DLL from the test's output directory
        string testDir = AppContext.BaseDirectory;
        _runtimeDll = Path.Combine(testDir, "Lolcode.Runtime.dll");
        if (!File.Exists(_runtimeDll))
            throw new FileNotFoundException($"Runtime DLL not found at: {_runtimeDll}");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    private string CompileAndRun(string source, string? stdin = null)
    {
        var sourceText = SourceText.From(source, "test.lol");
        string outputPath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.dll");
        var result = Compilation.Compile(sourceText, outputPath, _runtimeDll);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Compilation failed:\n{errors}");
        }

        // Copy runtime next to output
        string runtimeDest = Path.Combine(Path.GetDirectoryName(result.OutputPath!)!, "Lolcode.Runtime.dll");
        if (!File.Exists(runtimeDest))
            File.Copy(_runtimeDll, runtimeDest, overwrite: true);

        // Run the compiled assembly
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = result.OutputPath!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin != null,
        };

        using var process = Process.Start(psi)!;

        if (stdin != null)
        {
            process.StandardInput.Write(stdin);
            process.StandardInput.Close();
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(10000);

        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException($"Runtime error (exit code {process.ExitCode}):\n{error}");

        return output;
    }

    private void AssertOutput(string source, string expectedOutput, string? stdin = null)
    {
        string actual = CompileAndRun(source, stdin);
        // Normalize line endings
        actual = actual.Replace("\r\n", "\n").TrimEnd('\n');
        expectedOutput = expectedOutput.Replace("\r\n", "\n").TrimEnd('\n');
        actual.Should().Be(expectedOutput);
    }

    // ==================== Basic Programs ====================

    [Fact]
    public void HelloWorld()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "HAI WORLD!"
            KTHXBYE
            """, "HAI WORLD!");
    }

    [Fact]
    public void MultipleVisible()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "line 1"
              VISIBLE "line 2"
              VISIBLE "line 3"
            KTHXBYE
            """, "line 1\nline 2\nline 3");
    }

    [Fact]
    public void VisibleSuppressNewline()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "hello "!
              VISIBLE "world"
            KTHXBYE
            """, "hello world");
    }

    [Fact]
    public void VisibleMultipleArgs()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "a" "b" "c"
            KTHXBYE
            """, "abc");
    }

    // ==================== Variables ====================

    [Fact]
    public void VariableDeclarationAndAssignment()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 42
              VISIBLE x
            KTHXBYE
            """, "42");
    }

    [Fact]
    public void VariableReassignment()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 1
              x R 2
              VISIBLE x
            KTHXBYE
            """, "2");
    }

    [Fact]
    public void VariableDefaultIsNoob()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x
              VISIBLE x
            KTHXBYE
            """, "");
    }

    // ==================== Math ====================

    [Fact]
    public void SumOfInts()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE SUM OF 3 AN 4
            KTHXBYE
            """, "7");
    }

    [Fact]
    public void DiffOfInts()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE DIFF OF 10 AN 3
            KTHXBYE
            """, "7");
    }

    [Fact]
    public void ProduktOfInts()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE PRODUKT OF 3 AN 4
            KTHXBYE
            """, "12");
    }

    [Fact]
    public void QuoshuntOfInts()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE QUOSHUNT OF 7 AN 2
            KTHXBYE
            """, "3");
    }

    [Fact]
    public void ModOfInts()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE MOD OF 7 AN 3
            KTHXBYE
            """, "1");
    }

    [Fact]
    public void BiggrOf()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE BIGGR OF 3 AN 7
            KTHXBYE
            """, "7");
    }

    [Fact]
    public void SmallrOf()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE SMALLR OF 3 AN 7
            KTHXBYE
            """, "3");
    }

    [Fact]
    public void MathWithFloats()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE SUM OF 1 AN 2.5
            KTHXBYE
            """, "3.50");
    }

    [Fact]
    public void NestedMath()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE SUM OF PRODUKT OF 2 AN 3 AN 4
            KTHXBYE
            """, "10");
    }

    // ==================== Boolean Operations ====================

    [Fact]
    public void BothOf_And()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE BOTH OF WIN AN WIN
            KTHXBYE
            """, "WIN");
    }

    [Fact]
    public void EitherOf_Or()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE EITHER OF FAIL AN WIN
            KTHXBYE
            """, "WIN");
    }

    [Fact]
    public void WonOf_Xor()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE WON OF WIN AN WIN
            KTHXBYE
            """, "FAIL");
    }

    [Fact]
    public void NotExpression()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE NOT WIN
            KTHXBYE
            """, "FAIL");
    }

    // ==================== Comparison ====================

    [Fact]
    public void BothSaem_Equal()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE BOTH SAEM 1 AN 1
            KTHXBYE
            """, "WIN");
    }

    [Fact]
    public void BothSaem_NotEqual()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE BOTH SAEM 1 AN 2
            KTHXBYE
            """, "FAIL");
    }

    [Fact]
    public void Diffrint_NotEqual()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE DIFFRINT 1 AN 2
            KTHXBYE
            """, "WIN");
    }

    // ==================== Casting ====================

    [Fact]
    public void MaekCast()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE MAEK 42 A YARN
            KTHXBYE
            """, "42");
    }

    [Fact]
    public void IsNowACast()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 42
              x IS NOW A YARN
              VISIBLE x
            KTHXBYE
            """, "42");
    }

    // ==================== IT Variable ====================

    [Fact]
    public void ItSetByExpression()
    {
        AssertOutput("""
            HAI 1.2
              SUM OF 3 AN 4
              VISIBLE IT
            KTHXBYE
            """, "7");
    }

    // ==================== Conditionals ====================

    [Fact]
    public void IfStatement_YaRly()
    {
        AssertOutput("""
            HAI 1.2
              WIN
              O RLY?
                YA RLY
                  VISIBLE "yes"
                NO WAI
                  VISIBLE "no"
              OIC
            KTHXBYE
            """, "yes");
    }

    [Fact]
    public void IfStatement_NoWai()
    {
        AssertOutput("""
            HAI 1.2
              FAIL
              O RLY?
                YA RLY
                  VISIBLE "yes"
                NO WAI
                  VISIBLE "no"
              OIC
            KTHXBYE
            """, "no");
    }

    // ==================== Loops ====================

    [Fact]
    public void LoopUppin()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A result ITZ ""
              IM IN YR loop UPPIN YR i TIL BOTH SAEM i AN 5
                result R SMOOSH result AN i MKAY
              IM OUTTA YR loop
              VISIBLE result
            KTHXBYE
            """, "01234");
    }

    [Fact]
    public void LoopGtfo()
    {
        AssertOutput("""
            HAI 1.2
              IM IN YR loop UPPIN YR i TIL BOTH SAEM i AN 100
                BOTH SAEM i AN 3
                O RLY?
                  YA RLY
                    GTFO
                OIC
              IM OUTTA YR loop
              VISIBLE i
            KTHXBYE
            """, "3");
    }

    // ==================== Functions ====================

    [Fact]
    public void FunctionCallAndReturn()
    {
        AssertOutput("""
            HAI 1.2
              HOW IZ I add YR a AN YR b
                FOUND YR SUM OF a AN b
              IF U SAY SO
              VISIBLE I IZ add YR 3 AN YR 4 MKAY
            KTHXBYE
            """, "7");
    }

    // ==================== Smoosh ====================

    [Fact]
    public void SmooshConcatenation()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE SMOOSH "hello" AN " " AN "world" MKAY
            KTHXBYE
            """, "hello world");
    }

    // ==================== String Escapes ====================

    [Fact]
    public void StringNewlineEscape()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "line1:)line2"
            KTHXBYE
            """, "line1\nline2");
    }

    [Fact]
    public void StringTabEscape()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "a:>b"
            KTHXBYE
            """, "a\tb");
    }

    // ==================== Comments ====================

    [Fact]
    public void SingleLineComment()
    {
        AssertOutput("""
            HAI 1.2
              BTW this is a comment
              VISIBLE "hello"
            KTHXBYE
            """, "hello");
    }

    // ==================== NUMBAR Formatting ====================

    [Fact]
    public void NumbarPrintsTwoDecimals()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 7.0
              VISIBLE x
            KTHXBYE
            """, "7.00");
    }

    [Fact]
    public void NumbarPiTruncatesToTwoDecimals()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 3.14159
              VISIBLE x
            KTHXBYE
            """, "3.14");
    }
}
