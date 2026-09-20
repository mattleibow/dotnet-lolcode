HAI 1.4

OBTW
TerminalUi horizontal layout composes retained views without printing.
TLDR

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

KTHXBYE
