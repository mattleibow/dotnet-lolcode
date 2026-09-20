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
