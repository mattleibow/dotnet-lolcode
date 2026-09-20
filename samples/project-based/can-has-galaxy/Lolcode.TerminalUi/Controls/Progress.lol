HAI 1.4

OBTW
TerminalUi progress control returns deterministic fixed-width meter text.
TLDR

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

KTHXBYE
