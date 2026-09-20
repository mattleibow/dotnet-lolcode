HAI 1.4

CAN HAS STRING?

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

KTHXBYE
