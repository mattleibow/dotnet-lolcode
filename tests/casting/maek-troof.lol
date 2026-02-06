BTW Test MAEK <expression> A TROOF for numbers, strings, and NOOB
BTW Per spec: 0 and empty string are FAIL, non-zero/non-empty are WIN, NOOB is FAIL

HAI 1.2
  BTW numeric zero is FAIL
  VISIBLE "0 AS TROOF: " MAEK 0 A TROOF

  BTW empty string is FAIL
  VISIBLE "\"\" AS TROOF: " MAEK "" A TROOF

  BTW non-zero number is WIN
  VISIBLE "42 AS TROOF: " MAEK 42 A TROOF

  BTW non-empty string is WIN
  VISIBLE "\"hai\" AS TROOF: " MAEK "hai" A TROOF

  BTW NOOB casts to FAIL
  I HAS A nothing ITZ NOOB
  VISIBLE "NOOB AS TROOF: " MAEK nothing A TROOF
KTHXBYE
