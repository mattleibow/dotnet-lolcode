BTW Test empty string behavior: value, truthiness (FAIL), concatenation
BTW Per spec: empty YARN is falsy and concatenates as empty text

HAI 1.2
  I HAS A empty ITZ ""
  I HAS A nonempty ITZ "LOL"

  BTW show empty string in brackets
  VISIBLE "EMPTY:[" empty "]"
  VISIBLE "NONEMPTY:[" nonempty "]"

  BTW empty string is falsy in O RLY?
  empty
  O RLY?
    YA RLY
      VISIBLE "SHOULD NOT SEE"
    NO WAI
      VISIBLE "EMPTY IS FAIL"
  OIC

  BTW casting empty and non-empty YARN to TROOF
  VISIBLE "MAEK :":" A TROOF:: " MAEK empty A TROOF
  VISIBLE "MAEK :"LOL:" A TROOF:: " MAEK nonempty A TROOF

  BTW concatenating with empty string
  I HAS A base ITZ ""
  I HAS A joined ITZ SMOOSH base AN "A" MKAY
  VISIBLE joined
KTHXBYE
