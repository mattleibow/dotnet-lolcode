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
