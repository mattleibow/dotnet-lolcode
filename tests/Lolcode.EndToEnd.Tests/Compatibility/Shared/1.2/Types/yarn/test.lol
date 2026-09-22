BTW Test YARN (string) type
BTW Per spec: demarked with double quotes, escape with colon

HAI 1.2
  BTW basic strings
  I HAS A empty ITZ ""
  I HAS A hello ITZ "HELLO WORLD"

  VISIBLE "START"
  VISIBLE empty
  VISIBLE "END"
  VISIBLE hello

  BTW escape sequences
  VISIBLE "LINE1:)LINE2"
  VISIBLE "COL1:>COL2"
  VISIBLE "QUOTE:: :"HAI:""
  VISIBLE "COLON:: ::"
  VISIBLE "BELL:::o"

  BTW hex escape :(<hex>)
  VISIBLE "HEX A:: :(41)"
  VISIBLE "HEX NEWLINE:: :(0A)AFTER"

  BTW variable interpolation :{var}
  I HAS A name ITZ "CEILING CAT"
  I HAS A age ITZ 9
  VISIBLE "NAME:: :{name}"
  VISIBLE "AGE:: :{age}"
  VISIBLE ":{name} IS :{age} YEARS OLD"

  BTW string in string context
  I HAS A a ITZ "FIRST"
  I HAS A b ITZ "SECOND"
  VISIBLE SMOOSH a AN " AND " AN b MKAY

  BTW multiline via escape
  VISIBLE "LINE A:)LINE B:)LINE C"
KTHXBYE
