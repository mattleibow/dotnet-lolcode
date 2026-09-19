HAI 1.4

BTW BOXLINE formats one stable panel row without terminal cursor controls.
HOW IZ I BOXLINE YR text
  FOUND YR SMOOSH "| " AN text AN " |" MKAY
IF U SAY SO

BTW HEADER renders a consistently boxed title for any line-oriented game.
HOW IZ I HEADER YR title
  I HAS A edge ITZ I IZ REPEAT YR "=" AN YR 42 MKAY
  VISIBLE edge
  VISIBLE I IZ BOXLINE YR title MKAY
  VISIBLE edge
  FOUND YR title
IF U SAY SO

BTW PANEL renders a labelled informational row.
HOW IZ I PANEL YR label AN YR value
  I HAS A text ITZ SMOOSH label AN ":: " AN value MKAY
  VISIBLE I IZ BOXLINE YR text MKAY
  FOUND YR value
IF U SAY SO

BTW STATUSBAR renders a deterministic text progress meter.
HOW IZ I STATUSBAR YR label AN YR current AN YR maximum
  BTW Invalid or empty ranges render a stable empty bar instead of looping.
  BOTH SAEM maximum AN SMALLR OF maximum AN 0
  O RLY?
    YA RLY
      I HAS A emptyResult ITZ SMOOSH label AN " [] 0/0" MKAY
      VISIBLE emptyResult
      FOUND YR emptyResult
  OIC

  BTW Clamp the filled portion so values outside the range remain readable.
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

BTW MENU renders a concise command-help line shared by interactive games.
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
