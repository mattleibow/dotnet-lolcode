namespace Lolcode.EndToEnd.Tests;

public class CastingTests : EndToEndTestBase
{
    [Fact]
    public void CastingRulesMatrix()
    {
        AssertOutput("""
            BTW Comprehensive casting matrix tests for LANGUAGE_SPEC Casting Rules Summary
            BTW Per spec: verify each From -> To case with representative values

            HAI 1.2
              BTW NUMBR -> YARN
              VISIBLE "NUMBR->YARN:: " MAEK 42 A YARN

              BTW NUMBAR -> YARN (2 decimal places)
              I HAS A numbar ITZ 3.14159
              VISIBLE "NUMBAR->YARN:: " MAEK numbar A YARN

              BTW YARN -> NUMBR
              VISIBLE "YARN :"123:"->NUMBR:: " MAEK "123" A NUMBR
              VISIBLE "YARN :"4.56:"->NUMBR:: " MAEK "4.56" A NUMBR

              BTW YARN -> NUMBAR
              VISIBLE "YARN :"4.56:"->NUMBAR:: " MAEK "4.56" A NUMBAR

              BTW YARN -> TROOF
              VISIBLE "YARN :":"->TROOF:: " MAEK MAEK "" A TROOF A NUMBR
              VISIBLE "YARN :"LOL:"->TROOF:: " MAEK MAEK "LOL" A TROOF A NUMBR

              BTW NUMBR -> TROOF
              VISIBLE "NUMBR 0->TROOF:: " MAEK MAEK 0 A TROOF A NUMBR
              VISIBLE "NUMBR 5->TROOF:: " MAEK MAEK 5 A TROOF A NUMBR

              BTW NUMBAR -> TROOF
              I HAS A zeroNumbar ITZ 0.0
              I HAS A nonzeroNumbar ITZ -1.5
              VISIBLE "NUMBAR 0.0->TROOF:: " MAEK MAEK zeroNumbar A TROOF A NUMBR
              VISIBLE "NUMBAR -1.5->TROOF:: " MAEK MAEK nonzeroNumbar A TROOF A NUMBR

              BTW TROOF -> NUMBR
              VISIBLE "TROOF WIN->NUMBR:: " MAEK WIN A NUMBR
              VISIBLE "TROOF FAIL->NUMBR:: " MAEK FAIL A NUMBR

              BTW TROOF -> NUMBAR
              VISIBLE "TROOF WIN->NUMBAR:: " MAEK WIN A NUMBAR
              VISIBLE "TROOF FAIL->NUMBAR:: " MAEK FAIL A NUMBAR

              BTW NOOB -> TROOF (implicit and explicit)
              I HAS A nothing ITZ NOOB
              VISIBLE "NOOB->TROOF (explicit):: " MAEK MAEK nothing A TROOF A NUMBR
              nothing
              O RLY?
                YA RLY
                  VISIBLE "NOOB->TROOF (implicit in O RLY?):: WIN BRANCH"
                NO WAI
                  VISIBLE "NOOB->TROOF (implicit in O RLY?):: FAIL BRANCH"
              OIC

              BTW NOOB -> other (explicit)
              VISIBLE "NOOB->NUMBR:: " MAEK nothing A NUMBR
              VISIBLE "NOOB->NUMBAR:: " MAEK nothing A NUMBAR
              VISIBLE "NOOB->YARN:: [" MAEK nothing A YARN "]"
            KTHXBYE
            """, "NUMBR->YARN: 42\nNUMBAR->YARN: 3.14\nYARN \"123\"->NUMBR: 123\nYARN \"4.56\"->NUMBR: 4\nYARN \"4.56\"->NUMBAR: 4.56\nYARN \"\"->TROOF: 0\nYARN \"LOL\"->TROOF: 1\nNUMBR 0->TROOF: 0\nNUMBR 5->TROOF: 1\nNUMBAR 0.0->TROOF: 0\nNUMBAR -1.5->TROOF: 1\nTROOF WIN->NUMBR: 1\nTROOF FAIL->NUMBR: 0\nTROOF WIN->NUMBAR: 1.00\nTROOF FAIL->NUMBAR: 0.00\nNOOB->TROOF (explicit): 0\nNOOB->TROOF (implicit in O RLY?): FAIL BRANCH\nNOOB->NUMBR: 0\nNOOB->NUMBAR: 0.00\nNOOB->YARN: []");
    }

    [Fact]
    public void IsNowA()
    {
        AssertOutput("""
            BTW Test in-place casting with IS NOW A
            BTW Per spec: IS NOW A mutates the variable's runtime type

            HAI 1.2
              I HAS A val ITZ "42"
              VISIBLE "START YARN:: " val

              val IS NOW A NUMBR
              VISIBLE "AFTER IS NOW A NUMBR:: " val

              val IS NOW A NUMBAR
              VISIBLE "AFTER IS NOW A NUMBAR:: " val

              val IS NOW A YARN
              VISIBLE "AFTER IS NOW A YARN:: " val

              I HAS A flag ITZ 0
              flag IS NOW A TROOF
              VISIBLE "0 IS NOW A TROOF:: " MAEK flag A NUMBR
            KTHXBYE
            """, "START YARN: 42\nAFTER IS NOW A NUMBR: 42\nAFTER IS NOW A NUMBAR: 42.00\nAFTER IS NOW A YARN: 42.00\n0 IS NOW A TROOF: 0");
    }

    [Fact]
    public void MaekNumbar()
    {
        AssertOutput("""
            BTW Test MAEK <expression> A NUMBAR from YARN, NUMBR, and TROOF
            BTW Per spec: results print with two decimal places when cast to YARN

            HAI 1.2
              BTW YARN to NUMBAR (integer)
              VISIBLE "YARN :"42:" AS NUMBAR:: " MAEK "42" A NUMBAR

              BTW YARN with decimal to NUMBAR
              VISIBLE "YARN :"3.14:" AS NUMBAR:: " MAEK "3.14" A NUMBAR

              BTW NUMBR to NUMBAR
              VISIBLE "NUMBR 7 AS NUMBAR:: " MAEK 7 A NUMBAR

              BTW TROOF to NUMBAR
              VISIBLE "WIN AS NUMBAR:: " MAEK WIN A NUMBAR
              VISIBLE "FAIL AS NUMBAR:: " MAEK FAIL A NUMBAR
            KTHXBYE
            """, "YARN \"42\" AS NUMBAR: 42.00\nYARN \"3.14\" AS NUMBAR: 3.14\nNUMBR 7 AS NUMBAR: 7.00\nWIN AS NUMBAR: 1.00\nFAIL AS NUMBAR: 0.00");
    }

    [Fact]
    public void MaekNumbr()
    {
        AssertOutput("""
            BTW Test MAEK <expression> A NUMBR from YARN, NUMBAR, and TROOF
            BTW Per spec: YARN parses to integer, NUMBAR truncates, TROOF WIN/FAIL become 1/0

            HAI 1.2
              BTW YARN to NUMBR (integer)
              VISIBLE "YARN :"42:" AS NUMBR:: " MAEK "42" A NUMBR

              BTW YARN with decimal truncates toward zero
              VISIBLE "YARN :"3.14:" AS NUMBR:: " MAEK "3.14" A NUMBR

              BTW NUMBAR to NUMBR truncates decimal part
              I HAS A pi ITZ 3.14159
              VISIBLE "NUMBAR 3.14159 AS NUMBR:: " MAEK pi A NUMBR

              BTW TROOF to NUMBR
              VISIBLE "WIN AS NUMBR:: " MAEK WIN A NUMBR
              VISIBLE "FAIL AS NUMBR:: " MAEK FAIL A NUMBR
            KTHXBYE
            """, "YARN \"42\" AS NUMBR: 42\nYARN \"3.14\" AS NUMBR: 3\nNUMBAR 3.14159 AS NUMBR: 3\nWIN AS NUMBR: 1\nFAIL AS NUMBR: 0");
    }

    [Fact]
    public void MaekTroof()
    {
        AssertOutput("""
            BTW Test MAEK <expression> A TROOF for numbers, strings, and NOOB
            BTW Per spec: 0 and empty string are FAIL, non-zero/non-empty are WIN, NOOB is FAIL

            HAI 1.2
              BTW numeric zero is FAIL
              VISIBLE "0 AS TROOF:: " MAEK MAEK 0 A TROOF A NUMBR

              BTW empty string is FAIL
              VISIBLE ":":" AS TROOF:: " MAEK MAEK "" A TROOF A NUMBR

              BTW non-zero number is WIN
              VISIBLE "42 AS TROOF:: " MAEK MAEK 42 A TROOF A NUMBR

              BTW non-empty string is WIN
              VISIBLE ":"hai:" AS TROOF:: " MAEK MAEK "hai" A TROOF A NUMBR

              BTW NOOB casts to FAIL
              I HAS A nothing ITZ NOOB
              VISIBLE "NOOB AS TROOF:: " MAEK MAEK nothing A TROOF A NUMBR
            KTHXBYE
            """, "0 AS TROOF: 0\n\"\" AS TROOF: 0\n42 AS TROOF: 1\n\"hai\" AS TROOF: 1\nNOOB AS TROOF: 0");
    }

    [Fact]
    public void MaekYarn()
    {
        AssertOutput("""
            BTW Test MAEK <expression> A YARN from NUMBR and NUMBAR
            BTW Per spec: NUMBAR is truncated to 2 decimal places

            HAI 1.2
              BTW NUMBR to YARN
              VISIBLE "NUMBR 42 AS YARN:: " MAEK 42 A YARN

              BTW NUMBAR to YARN with 2 decimal digits
              VISIBLE "NUMBAR 3.14159 AS YARN:: " MAEK 3.14159 A YARN

            KTHXBYE
            """, "NUMBR 42 AS YARN: 42\nNUMBAR 3.14159 AS YARN: 3.14");
    }

    [Fact]
    public void MaekTroofToYarnFails()
    {
        AssertRuntimeError("""
            HAI 1.2
              VISIBLE MAEK WIN A YARN
            KTHXBYE
            """, "TROOF");
    }

    [Fact]
    public void NoobExplicitCast()
    {
        AssertOutput("""
            BTW Test explicit casts from NOOB to TROOF, NUMBR, NUMBAR, and YARN
            BTW Per spec: NOOB casts to FAIL/0/0.0/"" depending on target type

            HAI 1.2
              I HAS A nothing ITZ NOOB

              VISIBLE "NOOB AS TROOF:: " MAEK MAEK nothing A TROOF A NUMBR
              VISIBLE "NOOB AS NUMBR:: " MAEK nothing A NUMBR
              VISIBLE "NOOB AS NUMBAR:: " MAEK nothing A NUMBAR
              VISIBLE "NOOB AS YARN:: [" MAEK nothing A YARN "]"
            KTHXBYE
            """, "NOOB AS TROOF: 0\nNOOB AS NUMBR: 0\nNOOB AS NUMBAR: 0.00\nNOOB AS YARN: []");
    }

    [Fact]
    public void NumbarToYarnTruncation()
    {
        AssertOutput("""
            BTW Test NUMBAR to YARN truncation to two decimal places
            BTW Per spec: NUMBAR prints with two decimal digits when cast to YARN

            HAI 1.2
              I HAS A positive ITZ 1.239
              VISIBLE "POSITIVE AS YARN:: " MAEK positive A YARN

              I HAS A negative ITZ -1.239
              VISIBLE "NEGATIVE AS YARN:: " MAEK negative A YARN
            KTHXBYE
            """, "POSITIVE AS YARN: 1.23\nNEGATIVE AS YARN: -1.23");
    }

    [Fact]
    public void YarnToNumbrParsing()
    {
        AssertOutput("""
            BTW Test parsing YARN to NUMBR
            BTW Reference behavior: decimal truncates and non-numeric becomes zero

            HAI 1.2
              BTW integer string
              VISIBLE ":"42:" AS NUMBR:: " MAEK "42" A NUMBR

              BTW decimal string truncates toward zero
              VISIBLE ":"3.14:" AS NUMBR:: " MAEK "3.14" A NUMBR

              BTW non-numeric string becomes zero
              VISIBLE ":"LOL:" AS NUMBR:: " MAEK "LOL" A NUMBR
            KTHXBYE
            """, "\"42\" AS NUMBR: 42\n\"3.14\" AS NUMBR: 3\n\"LOL\" AS NUMBR: 0");
    }
}
