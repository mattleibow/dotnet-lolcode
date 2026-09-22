HAI 1.4

OBTW
TerminalUi retained-view core creates fixed-width BUKKIT views with dynamic
SRS line storage. It performs no rendering or terminal I/O.
TLDR

BTW NEWVIEW creates a fixed-width retained collection of SRS-backed lines.
HOW IZ I NEWVIEW YR width
  I HAS A safeWidth ITZ BIGGR OF width AN 0
  O HAI IM viewPrototype
    BTW ADDLINE fits ASCII content before it enters the retained SRS slot.
    HOW IZ I ADDLINE YR text
      CAN HAS STRING?
      I HAS A slot ITZ SMOOSH "line" AN ME'Z count MKAY
      I HAS A fitted ITZ ""
      I HAS A length ITZ I IZ STRING'Z LEN YR text MKAY
      I HAS A limit ITZ SMALLR OF length AN ME'Z width
      IM IN YR viewClip UPPIN YR index TIL BOTH SAEM index AN limit
        fitted R SMOOSH fitted AN I IZ STRING'Z AT YR text AN YR index MKAY MKAY
      IM OUTTA YR viewClip
      IM IN YR viewPad UPPIN YR index TIL BOTH SAEM index AN ME'Z width
        BOTH SAEM index AN SMALLR OF index AN limit
        O RLY?
          YA RLY
          NO WAI
            fitted R SMOOSH fitted AN " " MKAY
        OIC
      IM OUTTA YR viewPad
      ME HAS A SRS slot ITZ fitted
      ME'Z count R SUM OF ME'Z count AN 1
      FOUND YR ME'Z count
    IF U SAY SO

    BTW ADDRAW stores already composed Unicode-safe output at its logical width.
    HOW IZ I ADDRAW YR text
      I HAS A slot ITZ SMOOSH "line" AN ME'Z count MKAY
      ME HAS A SRS slot ITZ text
      ME'Z count R SUM OF ME'Z count AN 1
      FOUND YR ME'Z count
    IF U SAY SO

    BTW GETLINE retrieves one stored fixed-width line by its zero-based index.
    HOW IZ I GETLINE YR index
      I HAS A slot ITZ SMOOSH "line" AN index MKAY
      FOUND YR ME'Z SRS slot
    IF U SAY SO
  KTHX
  I HAS A view ITZ LIEK A viewPrototype
  view HAS A width ITZ safeWidth
  view HAS A count ITZ 0
  FOUND YR view
IF U SAY SO

BTW ADDLINE is an exported convenience wrapper around a retained view method.
HOW IZ I ADDLINE YR view AN YR text
  FOUND YR view IZ ADDLINE YR text MKAY
IF U SAY SO

BTW GETLINE exposes an individual retained line for tests and compositors.
HOW IZ I GETLINE YR view AN YR index
  FOUND YR view IZ GETLINE YR index MKAY
IF U SAY SO

BTW VIEWWIDTH exposes a view's logical display width without UTF-8 byte counting.
HOW IZ I VIEWWIDTH YR view
  FOUND YR view'Z width
IF U SAY SO

BTW VIEWCOUNT exposes the number of retained lines for composition tests.
HOW IZ I VIEWCOUNT YR view
  FOUND YR view'Z count
IF U SAY SO

KTHXBYE
