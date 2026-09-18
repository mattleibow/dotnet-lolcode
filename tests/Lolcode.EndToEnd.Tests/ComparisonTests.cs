namespace Lolcode.EndToEnd.Tests;

public class ComparisonTests : EndToEndTestBase
{
    [Fact]
    public void BothSaem()
    {
        AssertOutput("""
            BTW BOTH SAEM: equality for same type same value vs diff value
            HAI 1.2
              VISIBLE MAEK BOTH SAEM 3 AN 3 A NUMBR
              VISIBLE MAEK BOTH SAEM 3 AN 4 A NUMBR
              VISIBLE MAEK BOTH SAEM "hai" AN "hai" A NUMBR
              VISIBLE MAEK BOTH SAEM "hai" AN "hai!" A NUMBR
              VISIBLE MAEK BOTH SAEM WIN AN WIN A NUMBR
              VISIBLE MAEK BOTH SAEM FAIL AN WIN A NUMBR
            KTHXBYE
            """, "1\n0\n1\n0\n1\n0");
    }

    [Fact]
    public void Diffrint()
    {
        AssertOutput("""
            BTW DIFFRINT: inequality
            HAI 1.2
              VISIBLE MAEK DIFFRINT 3 AN 4 A NUMBR
              VISIBLE MAEK DIFFRINT 3 AN 3 A NUMBR
              VISIBLE MAEK DIFFRINT "hai" AN "hai" A NUMBR
              VISIBLE MAEK DIFFRINT WIN AN FAIL A NUMBR
            KTHXBYE
            """, "1\n0\n0\n1");
    }

    [Fact]
    public void GreaterThanIdiom()
    {
        AssertOutput("""
            BTW Greater-than-or-equal idiom: BOTH SAEM x AN BIGGR OF x AN y
            HAI 1.2
              I HAS A x ITZ 5
              I HAS A y ITZ 3
              VISIBLE MAEK BOTH SAEM x AN BIGGR OF x AN y A NUMBR
              x R 3
              y R 5
              VISIBLE MAEK BOTH SAEM x AN BIGGR OF x AN y A NUMBR
              x R 5
              y R 5
              VISIBLE MAEK BOTH SAEM x AN BIGGR OF x AN y A NUMBR
              x R 3.0
              y R 3
              VISIBLE MAEK BOTH SAEM x AN BIGGR OF x AN y A NUMBR
              x R 2
              y R 2.5
              VISIBLE MAEK BOTH SAEM x AN BIGGR OF x AN y A NUMBR
            KTHXBYE
            """, "1\n0\n1\n1\n0");
    }

    [Fact]
    public void LessThanIdiom()
    {
        AssertOutput("""
            BTW Greater-than idiom: DIFFRINT x AN SMALLR OF x AN y (x > y)
            HAI 1.2
              I HAS A x ITZ 5
              I HAS A y ITZ 3
              VISIBLE MAEK DIFFRINT x AN SMALLR OF x AN y A NUMBR
              x R 3
              y R 5
              VISIBLE MAEK DIFFRINT x AN SMALLR OF x AN y A NUMBR
              x R 5
              y R 5
              VISIBLE MAEK DIFFRINT x AN SMALLR OF x AN y A NUMBR
              x R 3.0
              y R 2
              VISIBLE MAEK DIFFRINT x AN SMALLR OF x AN y A NUMBR
              x R 3.0
              y R 3
              VISIBLE MAEK DIFFRINT x AN SMALLR OF x AN y A NUMBR
            KTHXBYE
            """, "1\n0\n0\n1\n0");
    }

    [Fact]
    public void NoAutoCastEquality()
    {
        AssertOutput("""
            BTW BOTH SAEM has NO automatic casting: YARN "3" vs NUMBR 3
            HAI 1.2
              VISIBLE MAEK BOTH SAEM "3" AN 3 A NUMBR
              VISIBLE MAEK BOTH SAEM "3.0" AN 3 A NUMBR
              VISIBLE MAEK DIFFRINT "3" AN 3 A NUMBR
            KTHXBYE
            """, "0\n0\n1");
    }

    [Fact]
    public void NumbarComparison()
    {
        AssertOutput("""
            BTW NUMBAR comparison and NUMBR vs NUMBAR promotion
            HAI 1.2
              VISIBLE MAEK BOTH SAEM 3 AN 3.0 A NUMBR
              VISIBLE MAEK BOTH SAEM 3.14 AN 3.140 A NUMBR
              VISIBLE MAEK DIFFRINT 3 AN 3.1 A NUMBR
              VISIBLE MAEK DIFFRINT 3.14 AN 3.13 A NUMBR
            KTHXBYE
            """, "1\n1\n1\n1");
    }
}
