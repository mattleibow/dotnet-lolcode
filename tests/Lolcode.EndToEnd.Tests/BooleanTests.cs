namespace Lolcode.EndToEnd.Tests;

public class BooleanTests : EndToEndTestBase
{
    [Fact]
    public void AllOf()
    {
        AssertOutput("""
            BTW ALL OF: infinite arity AND; MKAY terminator; MKAY may be omitted at EOL
            HAI 1.2
              VISIBLE MAEK ALL OF WIN AN WIN AN WIN MKAY A NUMBR
              VISIBLE MAEK ALL OF WIN AN WIN AN FAIL MKAY A NUMBR
              VISIBLE MAEK ALL OF 1 AN "hai" AN 2 MKAY A NUMBR
              VISIBLE MAEK ALL OF WIN AN WIN AN WIN MKAY A NUMBR
              VISIBLE MAEK ALL OF WIN AN WIN AN FAIL MKAY A NUMBR
              VISIBLE MAEK ALL OF 1 2 3 MKAY A NUMBR
            KTHXBYE
            """, "1\n0\n1\n1\n0\n1");
    }

    [Fact]
    public void AnyOf()
    {
        AssertOutput("""
            BTW ANY OF: infinite arity OR; MKAY terminator; MKAY may be omitted at EOL
            HAI 1.2
              VISIBLE MAEK ANY OF FAIL AN FAIL AN WIN MKAY A NUMBR
              VISIBLE MAEK ANY OF FAIL AN FAIL AN FAIL MKAY A NUMBR
              VISIBLE MAEK ANY OF "" AN 0 AN "x" MKAY A NUMBR
              VISIBLE MAEK ANY OF "" AN 0 AN NOOB MKAY A NUMBR
              VISIBLE MAEK ANY OF FAIL AN WIN MKAY A NUMBR
            KTHXBYE
            """, "1\n0\n1\n0\n1");
    }

    [Fact]
    public void BooleanAutoCast()
    {
        AssertOutput("""
            BTW Boolean auto-cast: 0=FAIL, ""=FAIL, 42=WIN, "hai"=WIN, NOOB=FAIL
            HAI 1.2
              VISIBLE MAEK MAEK 0 A TROOF A NUMBR
              VISIBLE MAEK MAEK "" A TROOF A NUMBR
              VISIBLE MAEK MAEK 42 A TROOF A NUMBR
              VISIBLE MAEK MAEK "hai" A TROOF A NUMBR
              I HAS A n
              VISIBLE MAEK MAEK n A TROOF A NUMBR
            KTHXBYE
            """, "0\n0\n1\n1\n0");
    }

    [Fact]
    public void BothOf()
    {
        AssertOutput("""
            BTW BOTH OF (AND): WIN/FAIL and auto-cast to TROOF from other types
            HAI 1.2
              VISIBLE MAEK BOTH OF WIN AN WIN A NUMBR
              VISIBLE MAEK BOTH OF WIN AN FAIL A NUMBR
              VISIBLE MAEK BOTH OF 1 AN "hai" A NUMBR
              VISIBLE MAEK BOTH OF 0 AN "hai" A NUMBR
              VISIBLE MAEK BOTH OF 42 AN "" A NUMBR
              I HAS A x
              VISIBLE MAEK BOTH OF x AN WIN A NUMBR
              VISIBLE MAEK BOTH OF 0.0 AN WIN A NUMBR
            KTHXBYE
            """, "1\n0\n1\n0\n0\n0\n0");
    }

    [Fact]
    public void EitherOf()
    {
        AssertOutput("""
            BTW EITHER OF (OR): truthiness across values
            HAI 1.2
              VISIBLE MAEK EITHER OF WIN AN FAIL A NUMBR
              VISIBLE MAEK EITHER OF FAIL AN FAIL A NUMBR
              VISIBLE MAEK EITHER OF 0 AN "hai" A NUMBR
              VISIBLE MAEK EITHER OF "" AN 0 A NUMBR
              VISIBLE MAEK EITHER OF NOOB AN FAIL A NUMBR
              VISIBLE MAEK EITHER OF NOOB AN "x" A NUMBR
            KTHXBYE
            """, "1\n0\n1\n0\n0\n1");
    }

    [Fact]
    public void Not()
    {
        AssertOutput("""
            BTW NOT: unary negation with truthiness auto-cast
            HAI 1.2
              VISIBLE MAEK NOT WIN A NUMBR
              VISIBLE MAEK NOT FAIL A NUMBR
              VISIBLE MAEK NOT 0 A NUMBR
              VISIBLE MAEK NOT 42 A NUMBR
              VISIBLE MAEK NOT "" A NUMBR
              VISIBLE MAEK NOT "hai" A NUMBR
              VISIBLE MAEK NOT NOOB A NUMBR
            KTHXBYE
            """, "0\n1\n1\n0\n1\n0\n1");
    }

    [Fact]
    public void WonOf()
    {
        AssertOutput("""
            BTW WON OF (XOR): FAIL if same, WIN if different
            HAI 1.2
              VISIBLE MAEK WON OF WIN AN WIN A NUMBR
              VISIBLE MAEK WON OF WIN AN FAIL A NUMBR
              VISIBLE MAEK WON OF 1 AN "hai" A NUMBR
              VISIBLE MAEK WON OF 0 AN "" A NUMBR
              VISIBLE MAEK WON OF 42 AN 0 A NUMBR
            KTHXBYE
            """, "0\n1\n0\n0\n1");
    }
}
