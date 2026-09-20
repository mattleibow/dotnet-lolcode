HAI 1.4

OBTW
TerminalUi retained-view and compositor layer. NEWVIEW creates a BUKKIT whose
ADDLINE and GETLINE methods store fixed-width ASCII lines in dynamic SRS slots;
count and width remain explicit state. FRAME, HSTACK, VSTACK, BANNER, MESSAGE,
and PROGRESS compose new views without printing. TOTEXT converts a completed
view to one multiline YARN and PRESENT prints exactly one snapshot. Unicode
box glyphs are inserted only after FIT has measured ASCII content. The optional
CLEAR function is intentionally separate, so normal applications remain safe
for pipes and test capture. Top-level functions are hoisted; no file relies on
initialization order.
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

BTW FRAME adds a Unicode box around fixed-width retained ASCII content.
HOW IZ I FRAME YR view AN YR title
  I HAS A result ITZ I IZ NEWVIEW YR SUM OF view'Z width AN 4 MKAY
  I HAS A top ITZ SMOOSH ":(250C)" AN I IZ REPEAT YR ":(2500)" AN YR SUM OF view'Z width AN 2 MKAY AN ":(2510)" MKAY
  I HAS A bottom ITZ SMOOSH ":(2514)" AN I IZ REPEAT YR ":(2500)" AN YR SUM OF view'Z width AN 2 MKAY AN ":(2518)" MKAY
  I IZ result'Z ADDRAW YR top MKAY
  I HAS A heading ITZ I IZ FIT YR title AN YR view'Z width MKAY
  I IZ result'Z ADDRAW YR SMOOSH ":(2502) " AN heading AN " :(2502)" MKAY
  IM IN YR frameLines UPPIN YR index TIL BOTH SAEM index AN view'Z count
    I IZ result'Z ADDRAW YR SMOOSH ":(2502) " AN view IZ GETLINE YR index MKAY AN " :(2502)" MKAY
  IM OUTTA YR frameLines
  I IZ result'Z ADDRAW YR bottom MKAY
  FOUND YR result
IF U SAY SO

BTW HSTACK places retained views side by side with a fixed ASCII gap.
HOW IZ I HSTACK YR left AN YR right AN YR gap
  I HAS A safeGap ITZ BIGGR OF gap AN 0
  I HAS A total ITZ SUM OF SUM OF left'Z width AN safeGap AN right'Z width
  I HAS A result ITZ I IZ NEWVIEW YR total MKAY
  I HAS A rows ITZ BIGGR OF left'Z count AN right'Z count
  IM IN YR hstackRows UPPIN YR index TIL BOTH SAEM index AN rows
    I HAS A leftLine ITZ I IZ REPEAT YR " " AN YR left'Z width MKAY
    I HAS A rightLine ITZ I IZ REPEAT YR " " AN YR right'Z width MKAY
    BOTH SAEM index AN SMALLR OF index AN DIFF OF left'Z count AN 1
    O RLY?
      YA RLY
        leftLine R left IZ GETLINE YR index MKAY
    OIC
    BOTH SAEM index AN SMALLR OF index AN DIFF OF right'Z count AN 1
    O RLY?
      YA RLY
        rightLine R right IZ GETLINE YR index MKAY
    OIC
    I IZ result'Z ADDRAW YR SMOOSH leftLine AN I IZ REPEAT YR " " AN YR safeGap MKAY AN rightLine MKAY
  IM OUTTA YR hstackRows
  FOUND YR result
IF U SAY SO

BTW VSTACK appends two views after fitting both to the widest shared width.
HOW IZ I VSTACK YR upper AN YR lower
  I HAS A width ITZ BIGGR OF upper'Z width AN lower'Z width
  I HAS A result ITZ I IZ NEWVIEW YR width MKAY
  IM IN YR upperRows UPPIN YR index TIL BOTH SAEM index AN upper'Z count
    I IZ result'Z ADDRAW YR SMOOSH upper IZ GETLINE YR index MKAY AN I IZ REPEAT YR " " AN YR DIFF OF width AN upper'Z width MKAY MKAY
  IM OUTTA YR upperRows
  IM IN YR lowerRows UPPIN YR index TIL BOTH SAEM index AN lower'Z count
    I IZ result'Z ADDRAW YR SMOOSH lower IZ GETLINE YR index MKAY AN I IZ REPEAT YR " " AN YR DIFF OF width AN lower'Z width MKAY MKAY
  IM OUTTA YR lowerRows
  FOUND YR result
IF U SAY SO

BTW BANNER creates a full-width double-line title frame.
HOW IZ I BANNER YR title AN YR width
  I HAS A safeWidth ITZ BIGGR OF width AN 4
  I HAS A innerWidth ITZ DIFF OF safeWidth AN 4
  I HAS A result ITZ I IZ NEWVIEW YR safeWidth MKAY
  I IZ result'Z ADDRAW YR SMOOSH ":(2554)" AN I IZ REPEAT YR ":(2550)" AN YR DIFF OF safeWidth AN 2 MKAY AN ":(2557)" MKAY
  I IZ result'Z ADDRAW YR SMOOSH ":(2551) " AN I IZ FIT YR title AN YR innerWidth MKAY AN " :(2551)" MKAY
  I IZ result'Z ADDRAW YR SMOOSH ":(255A)" AN I IZ REPEAT YR ":(2550)" AN YR DIFF OF safeWidth AN 2 MKAY AN ":(255D)" MKAY
  FOUND YR result
IF U SAY SO

BTW MESSAGE creates a framed full-width communications view.
HOW IZ I MESSAGE YR text AN YR width
  I HAS A safeWidth ITZ BIGGR OF width AN 4
  I HAS A content ITZ I IZ NEWVIEW YR DIFF OF safeWidth AN 4 MKAY
  I IZ content'Z ADDLINE YR text MKAY
  FOUND YR I IZ FRAME YR content AN YR " COMMS " MKAY
IF U SAY SO

BTW PROGRESS returns a non-printing deterministic fixed-width progress row.
HOW IZ I PROGRESS YR label AN YR current AN YR maximum AN YR width
  I HAS A safeMaximum ITZ BIGGR OF maximum AN 0
  I HAS A displayCurrent ITZ BIGGR OF 0 AN SMALLR OF current AN safeMaximum
  I HAS A filledCount ITZ 0
  BOTH SAEM safeMaximum AN 0
  O RLY?
    YA RLY
    NO WAI
      filledCount R QUOSHUNT OF PRODUKT OF displayCurrent AN 10 AN safeMaximum
  OIC
  I HAS A result ITZ SMOOSH label AN " [" AN I IZ REPEAT YR "#" AN YR filledCount MKAY AN I IZ REPEAT YR "." AN YR DIFF OF 10 AN filledCount MKAY AN "] " AN displayCurrent AN "/" AN safeMaximum MKAY
  FOUND YR I IZ FIT YR result AN YR width MKAY
IF U SAY SO

BTW TOTEXT serializes retained lines as a newline-separated snapshot.
HOW IZ I TOTEXT YR view
  I HAS A result ITZ ""
  IM IN YR textRows UPPIN YR index TIL BOTH SAEM index AN view'Z count
    BOTH SAEM index AN 0
    O RLY?
      YA RLY
      NO WAI
        result R SMOOSH result AN ":)" MKAY
    OIC
    result R SMOOSH result AN view IZ GETLINE YR index MKAY
  IM OUTTA YR textRows
  FOUND YR result
IF U SAY SO

BTW PRESENT writes one scroll-safe completed view snapshot.
HOW IZ I PRESENT YR view
  I HAS A text ITZ I IZ TOTEXT YR view MKAY
  VISIBLE text
  FOUND YR text
IF U SAY SO

BTW CLEAR optionally emits ANSI clear/home; callers must opt in for live terminals.
HOW IZ I CLEAR
  VISIBLE ":(1B)[2J:(1B)[H"!
  FOUND YR WIN
IF U SAY SO

BTW BOXLINE preserves the original simple public compatibility formatter.
HOW IZ I BOXLINE YR text
  FOUND YR SMOOSH "| " AN text AN " |" MKAY
IF U SAY SO

BTW HEADER preserves the original immediate-print convenience API.
HOW IZ I HEADER YR title
  I HAS A edge ITZ I IZ REPEAT YR "=" AN YR 42 MKAY
  VISIBLE edge
  VISIBLE I IZ BOXLINE YR title MKAY
  VISIBLE edge
  FOUND YR title
IF U SAY SO

BTW PANEL preserves the original immediate-print convenience API.
HOW IZ I PANEL YR label AN YR value
  I HAS A text ITZ SMOOSH label AN ":: " AN value MKAY
  VISIBLE I IZ BOXLINE YR text MKAY
  FOUND YR value
IF U SAY SO

BTW STATUSBAR preserves its historical printed string and clamp behavior.
HOW IZ I STATUSBAR YR label AN YR current AN YR maximum
  BOTH SAEM maximum AN SMALLR OF maximum AN 0
  O RLY?
    YA RLY
      I HAS A emptyResult ITZ SMOOSH label AN " [] 0/0" MKAY
      VISIBLE emptyResult
      FOUND YR emptyResult
  OIC
  I HAS A displayCurrent ITZ BIGGR OF 0 AN SMALLR OF current AN maximum
  I HAS A filled ITZ ""
  I HAS A empty ITZ ""
  IM IN YR meter UPPIN YR index TIL BOTH SAEM index AN maximum
    BOTH SAEM BIGGR OF displayCurrent AN SUM OF index AN 1 AN displayCurrent
    O RLY?
      YA RLY
        filled R SMOOSH filled AN "#" MKAY
      NO WAI
        empty R SMOOSH empty AN "." MKAY
    OIC
  IM OUTTA YR meter
  I HAS A result ITZ SMOOSH label AN " [" AN filled AN empty AN "] " AN displayCurrent AN "/" AN maximum MKAY
  VISIBLE result
  FOUND YR result
IF U SAY SO

BTW MENU preserves the original concise command-help API.
HOW IZ I MENU YR commands
  I HAS A result ITZ SMOOSH "COMMANDS:: " AN commands MKAY
  VISIBLE result
  FOUND YR result
IF U SAY SO

BTW PROMPT writes a portable prompt without relying on ANSI terminal behavior.
HOW IZ I PROMPT YR text
  VISIBLE text " "!
  FOUND YR text
IF U SAY SO

KTHXBYE
