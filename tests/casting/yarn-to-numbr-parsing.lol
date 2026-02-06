BTW Test parsing YARN to NUMBR and error on non-numeric
BTW Per spec: integer parses directly, decimal truncates, non-numeric causes runtime error

HAI 1.2
  BTW integer string
  VISIBLE "\"42\" AS NUMBR: " MAEK "42" A NUMBR

  BTW decimal string truncates toward zero
  VISIBLE "\"3.14\" AS NUMBR: " MAEK "3.14" A NUMBR

  BTW non-numeric string should cause runtime error (no further output)
  MAEK "LOL" A NUMBR
KTHXBYE
