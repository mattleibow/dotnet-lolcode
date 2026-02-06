BTW Test SMOOSH concatenation, AN separator, MKAY, implicit YARN casts
BTW Per spec: SMOOSH has infinite arity, MKAY may be omitted at end of line

HAI 1.2
  BTW basic SMOOSH with explicit MKAY
  I HAS A result1 ITZ SMOOSH "HAI" AN " " AN "WORLD" MKAY
  VISIBLE result1

  BTW SMOOSH with MKAY omitted at end of line
  I HAS A result2 ITZ SMOOSH "OMITTED" AN " MKAY"
  VISIBLE result2

  BTW SMOOSH with NUMBR, NUMBAR, and TROOF (implicit YARN casts)
  I HAS A num ITZ 42
  I HAS A pi ITZ 3.14159
  I HAS A flag ITZ WIN
  I HAS A result3 ITZ SMOOSH "NUM=" AN num AN ", PI=" AN pi AN ", FLAG=" AN flag MKAY
  VISIBLE result3

  BTW SMOOSH used directly in VISIBLE
  VISIBLE SMOOSH "DIRECT " AN "VISIBLE" MKAY
KTHXBYE
