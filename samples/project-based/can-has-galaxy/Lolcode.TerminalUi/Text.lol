HAI 1.4

OBTW
TerminalUi text primitives provide ASCII-width-safe fitting for the retained
terminal views. Public REPEAT, PADRIGHT, TRUNCATE, and FIT return YARNs only;
they do not print or retain state. STRING is imported inside the functions that
need it so multi-file project source order stays irrelevant. Width calculations
deliberately operate on ASCII application content because STRING LEN measures
UTF-8 bytes; frame glyphs are added later by compositors.
TLDR

BTW REPEAT builds a portable fixed-width text run for line-oriented widgets.
HOW IZ I REPEAT YR text AN YR count
  BTW Non-positive dimensions produce an empty run instead of an unbounded loop.
  BOTH SAEM count AN SMALLR OF count AN 0
  O RLY?
    YA RLY
      FOUND YR ""
  OIC

  I HAS A result ITZ ""
  IM IN YR repeatLoop UPPIN YR index TIL BOTH SAEM index AN count
    result R SMOOSH result AN text MKAY
  IM OUTTA YR repeatLoop
  FOUND YR result
IF U SAY SO

BTW PADRIGHT adds spaces until text reaches the requested display width.
HOW IZ I PADRIGHT YR text AN YR width
  CAN HAS STRING?
  I HAS A result ITZ SMOOSH text MKAY
  I HAS A length ITZ I IZ STRING'Z LEN YR result MKAY

  BTW Text that already meets the requested width is returned unchanged.
  BOTH SAEM length AN BIGGR OF length AN width
  O RLY?
    YA RLY
      FOUND YR result
  OIC

  I HAS A padding ITZ DIFF OF width AN length
  IM IN YR padLoop UPPIN YR index TIL BOTH SAEM index AN padding
    result R SMOOSH result AN " " MKAY
  IM OUTTA YR padLoop
  FOUND YR result
IF U SAY SO

BTW TRUNCATE returns no more than width ASCII bytes from text.
HOW IZ I TRUNCATE YR text AN YR width
  CAN HAS STRING?
  BOTH SAEM width AN SMALLR OF width AN 0
  O RLY?
    YA RLY
      FOUND YR ""
  OIC
  I HAS A result ITZ ""
  I HAS A length ITZ I IZ STRING'Z LEN YR text MKAY
  I HAS A limit ITZ SMALLR OF length AN width
  IM IN YR truncateLoop UPPIN YR index TIL BOTH SAEM index AN limit
    result R SMOOSH result AN I IZ STRING'Z AT YR text AN YR index MKAY MKAY
  IM OUTTA YR truncateLoop
  FOUND YR result
IF U SAY SO

BTW FIT truncates then pads ASCII content to one exact requested width.
HOW IZ I FIT YR text AN YR width
  I HAS A clipped ITZ I IZ TRUNCATE YR text AN YR width MKAY
  FOUND YR I IZ PADRIGHT YR clipped AN YR width MKAY
IF U SAY SO

KTHXBYE
