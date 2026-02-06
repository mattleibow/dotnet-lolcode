using System.Diagnostics;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis.Tests;

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
              I HAS A found ITZ 0
              IM IN YR loop UPPIN YR i TIL BOTH SAEM i AN 100
                BOTH SAEM i AN 3
                O RLY?
                  YA RLY
                    found R i
                    GTFO
                OIC
              IM OUTTA YR loop
              VISIBLE found
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

    // ==================== Error Cases ====================

    private void AssertRuntimeError(string source, string expectedErrorSubstring)
    {
        var sourceText = SourceText.From(source, "test.lol");
        string outputPath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.dll");
        var result = Compilation.Compile(sourceText, outputPath, _runtimeDll);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Compilation failed (expected runtime error instead):\n{errors}");
        }

        string runtimeDest = Path.Combine(Path.GetDirectoryName(result.OutputPath!)!, "Lolcode.Runtime.dll");
        if (!File.Exists(runtimeDest))
            File.Copy(_runtimeDll, runtimeDest, overwrite: true);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = result.OutputPath!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(psi)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(10000);

        process.ExitCode.Should().NotBe(0, "Expected a runtime error");
        error.Should().Contain(expectedErrorSubstring);
    }

    private void AssertCompileError(string source, string expectedDiagnosticId)
    {
        var sourceText = SourceText.From(source, "test.lol");
        string outputPath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.dll");
        var result = Compilation.Compile(sourceText, outputPath, _runtimeDll);

        result.Success.Should().BeFalse("Expected compilation to fail");
        result.Diagnostics.Should().Contain(d => d.Id.Contains(expectedDiagnosticId));
    }

    [Fact]
    public void NoobInArithmeticThrowsError()
    {
        AssertRuntimeError("""
            HAI 1.2
              I HAS A x
              VISIBLE SUM OF x AN 1
            KTHXBYE
            """, "NOOB");
    }

    [Fact]
    public void NonNumericYarnInArithmeticThrowsError()
    {
        AssertRuntimeError("""
            HAI 1.2
              VISIBLE SUM OF "hello" AN 1
            KTHXBYE
            """, "Cannot cast YARN to numeric");
    }

    [Fact]
    public void UndeclaredVariableError()
    {
        AssertCompileError("""
            HAI 1.2
              VISIBLE x
            KTHXBYE
            """, "LOL");
    }

    // ==================== Edge Cases ====================

    [Fact]
    public void EmptyProgram()
    {
        AssertOutput("""
            HAI 1.2
            KTHXBYE
            """, "");
    }

    [Fact]
    public void MultipleVisibleOnSameLine()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "A", VISIBLE "B"
            KTHXBYE
            """, "A\nB");
    }

    [Fact]
    public void VisibleSuppressNewlineExclamation()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "hello "!
              VISIBLE "world"
            KTHXBYE
            """, "hello world");
    }

    [Fact]
    public void NestedIfElse()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 42
              BOTH SAEM x AN 42
              O RLY?
                YA RLY
                  BOTH SAEM x AN 42
                  O RLY?
                    YA RLY
                      VISIBLE "DEEP WIN"
                  OIC
                NO WAI
                  VISIBLE "NO"
              OIC
            KTHXBYE
            """, "DEEP WIN");
    }

    [Fact]
    public void MebbeChain()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 2
              BOTH SAEM x AN 1
              O RLY?
                YA RLY
                  VISIBLE "ONE"
                MEBBE BOTH SAEM x AN 2
                  VISIBLE "TWO"
                MEBBE BOTH SAEM x AN 3
                  VISIBLE "THREE"
                NO WAI
                  VISIBLE "OTHER"
              OIC
            KTHXBYE
            """, "TWO");
    }

    [Fact]
    public void SwitchFallThrough()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 1
              x
              WTF?
                OMG 1
                  VISIBLE "ONE"
                OMG 2
                  VISIBLE "TWO"
                  GTFO
                OMG 3
                  VISIBLE "THREE"
                  GTFO
              OIC
            KTHXBYE
            """, "ONE\nTWO");
    }

    [Fact]
    public void SwitchOmgwtfDefault()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 99
              x
              WTF?
                OMG 1
                  VISIBLE "ONE"
                  GTFO
                OMGWTF
                  VISIBLE "DEFAULT"
              OIC
            KTHXBYE
            """, "DEFAULT");
    }

    [Fact]
    public void AllOfShortCircuit()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE ALL OF WIN AN WIN AN WIN MKAY
              VISIBLE ALL OF WIN AN FAIL AN WIN MKAY
            KTHXBYE
            """, "WIN\nFAIL");
    }

    [Fact]
    public void AnyOfShortCircuit()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE ANY OF FAIL AN FAIL AN WIN MKAY
              VISIBLE ANY OF FAIL AN FAIL AN FAIL MKAY
            KTHXBYE
            """, "WIN\nFAIL");
    }

    [Fact]
    public void WonOfXor()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE WON OF WIN AN FAIL
              VISIBLE WON OF WIN AN WIN
              VISIBLE WON OF FAIL AN FAIL
            KTHXBYE
            """, "WIN\nFAIL\nFAIL");
    }

    [Fact]
    public void SmooshConcatenationInAssignment()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ SMOOSH "HAI " AN "WORLD" MKAY
              VISIBLE x
            KTHXBYE
            """, "HAI WORLD");
    }

    [Fact]
    public void MaekCastExpression()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ "42"
              VISIBLE MAEK x A NUMBR
              VISIBLE MAEK x A NUMBAR
            KTHXBYE
            """, "42\n42.00");
    }

    [Fact]
    public void IsNowACastStatement()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ "123"
              x IS NOW A NUMBR
              VISIBLE SUM OF x AN 1
            KTHXBYE
            """, "124");
    }

    [Fact]
    public void RecursiveFunction()
    {
        AssertOutput("""
            HAI 1.2
              HOW IZ I factorial YR n
                BOTH SAEM n AN 0
                O RLY?
                  YA RLY
                    FOUND YR 1
                OIC
                FOUND YR PRODUKT OF n AN I IZ factorial YR DIFF OF n AN 1 MKAY
              IF U SAY SO
              VISIBLE I IZ factorial YR 5 MKAY
            KTHXBYE
            """, "120");
    }

    [Fact]
    public void FunctionMultipleArgs()
    {
        AssertOutput("""
            HAI 1.2
              HOW IZ I sum3 YR a AN YR b AN YR c
                FOUND YR SUM OF SUM OF a AN b AN c
              IF U SAY SO
              VISIBLE I IZ sum3 YR 1 AN YR 2 AN YR 3 MKAY
            KTHXBYE
            """, "6");
    }

    [Fact]
    public void LoopNerfin()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A result ITZ ""
              IM IN YR loop NERFIN YR i TIL BOTH SAEM i AN -3
                result R SMOOSH result AN i MKAY
              IM OUTTA YR loop
              VISIBLE result
            KTHXBYE
            """, "0-1-2");
    }

    [Fact]
    public void LoopWile()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 1
              I HAS A result ITZ ""
              IM IN YR loop UPPIN YR i WILE DIFFRINT i AN 3
                result R SMOOSH result AN i MKAY
              IM OUTTA YR loop
              VISIBLE result
            KTHXBYE
            """, "012");
    }

    [Fact]
    public void BooleanNotOperator()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE NOT WIN
              VISIBLE NOT FAIL
              VISIBLE NOT ""
              VISIBLE NOT 0
              VISIBLE NOT 1
            KTHXBYE
            """, "FAIL\nWIN\nWIN\nWIN\nFAIL");
    }

    [Fact]
    public void StringEscapeSequences()
    {
        // Test tab and colon escapes (quote escape is tricky with C# raw strings)
        AssertOutput("""
            HAI 1.2
              VISIBLE "tab:>here"
              VISIBLE "colon::::"
            KTHXBYE
            """, "tab\there\ncolon::");
    }

    [Fact]
    public void StringEscapeQuote()
    {
        // :" in LOLCODE is escaped quote character
        AssertOutput("HAI 1.2\n  VISIBLE \"has:\"quote\"\nKTHXBYE", "has\"quote");
    }

    [Fact]
    public void IntegerDivisionTruncates()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE QUOSHUNT OF 7 AN 2
            KTHXBYE
            """, "3");
    }

    [Fact]
    public void FloatDivision()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE QUOSHUNT OF 7.0 AN 2
            KTHXBYE
            """, "3.50");
    }

    [Fact]
    public void ModuloOperator()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE MOD OF 10 AN 3
              VISIBLE MOD OF 10.0 AN 3
            KTHXBYE
            """, "1\n1.00");
    }

    [Fact]
    public void BiggrofSmallrof()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE BIGGR OF 3 AN 7
              VISIBLE SMALLR OF 3 AN 7
              VISIBLE BIGGR OF 7 AN 3
              VISIBLE SMALLR OF 7 AN 3
            KTHXBYE
            """, "7\n3\n7\n3");
    }

    [Fact]
    public void DiffrintComparison()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE DIFFRINT 1 AN 2
              VISIBLE DIFFRINT 1 AN 1
            KTHXBYE
            """, "WIN\nFAIL");
    }

    [Fact]
    public void BothSaemCrossTypeNoAutocast()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE BOTH SAEM "3" AN 3
              VISIBLE BOTH SAEM 3.0 AN 3
            KTHXBYE
            """, "FAIL\nWIN");
    }

    [Fact]
    public void GimmehInput()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A line
              GIMMEH line
              VISIBLE line
            KTHXBYE
            """, "hello world", stdin: "hello world\n");
    }

    [Fact]
    public void ItVariableSetByExpression()
    {
        AssertOutput("""
            HAI 1.2
              SUM OF 2 AN 3
              VISIBLE IT
            KTHXBYE
            """, "5");
    }

    [Fact]
    public void ItVariableInConditional()
    {
        AssertOutput("""
            HAI 1.2
              BOTH SAEM 1 AN 1
              O RLY?
                YA RLY
                  VISIBLE "YES"
                NO WAI
                  VISIBLE "NO"
              OIC
            KTHXBYE
            """, "YES");
    }

    [Fact]
    public void LineContinuation()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ...
                ITZ 42
              VISIBLE x
            KTHXBYE
            """, "42");
    }

    [Fact]
    public void CommaAsSeparator()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 1, I HAS A y ITZ 2
              VISIBLE SUM OF x AN y
            KTHXBYE
            """, "3");
    }

    [Fact]
    public void ExplicitCastNoobToNumbr()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x
              VISIBLE MAEK x A NUMBR
            KTHXBYE
            """, "0");
    }

    [Fact]
    public void ExplicitCastNoobToTroof()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x
              VISIBLE MAEK x A TROOF
            KTHXBYE
            """, "FAIL");
    }

    [Fact]
    public void ExplicitCastNoobToYarn()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x
              I HAS A y ITZ SMOOSH ">" AN MAEK x A YARN AN "<" MKAY
              VISIBLE y
            KTHXBYE
            """, "><");
    }

    [Fact]
    public void LargeNumberArithmetic()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE PRODUKT OF 100000 AN 100000
            KTHXBYE
            """, "1410065408");
    }

    [Fact]
    public void NegativeNumbers()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ -42
              VISIBLE x
              VISIBLE SUM OF x AN 100
            KTHXBYE
            """, "-42\n58");
    }

    [Fact]
    public void EmptyStringIsFalsy()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ ""
              x
              O RLY?
                YA RLY
                  VISIBLE "TRUTHY"
                NO WAI
                  VISIBLE "FALSY"
              OIC
            KTHXBYE
            """, "FALSY");
    }

    [Fact]
    public void ZeroIsFalsy()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ 0
              x
              O RLY?
                YA RLY
                  VISIBLE "TRUTHY"
                NO WAI
                  VISIBLE "FALSY"
              OIC
            KTHXBYE
            """, "FALSY");
    }

    [Fact]
    public void NonZeroIsTruthy()
    {
        AssertOutput("""
            HAI 1.2
              I HAS A x ITZ -1
              x
              O RLY?
                YA RLY
                  VISIBLE "TRUTHY"
                NO WAI
                  VISIBLE "FALSY"
              OIC
            KTHXBYE
            """, "TRUTHY");
    }

    [Fact]
    public void BtwComment()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "before" BTW this is a comment
              VISIBLE "after"
            KTHXBYE
            """, "before\nafter");
    }

    [Fact]
    public void ObtwTldrBlockComment()
    {
        AssertOutput("""
            HAI 1.2
              OBTW
                This is a block comment
                spanning multiple lines
              TLDR
              VISIBLE "after block"
            KTHXBYE
            """, "after block");
    }

    [Fact]
    public void FunctionGtfoReturnsNoob()
    {
        AssertOutput("""
            HAI 1.2
              HOW IZ I myFunc
                GTFO
              IF U SAY SO
              I HAS A result ITZ I IZ myFunc MKAY
              VISIBLE BOTH SAEM result AN ""
            KTHXBYE
            """, "FAIL");
    }

    [Fact]
    public void VisibleMultipleArgsWithAn()
    {
        AssertOutput("""
            HAI 1.2
              VISIBLE "A" AN "B" AN "C"
            KTHXBYE
            """, "ABC");
    }
}
