HAI 1.4

OBTW
TerminalUi legacy widgets preserve the original immediate-print public APIs.
TLDR

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
