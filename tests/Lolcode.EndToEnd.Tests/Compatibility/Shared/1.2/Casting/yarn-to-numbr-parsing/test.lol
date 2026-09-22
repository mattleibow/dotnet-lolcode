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
