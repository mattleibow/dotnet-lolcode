HAI 1.4

OBTW
TerminalUi vertical layout appends retained views without terminal output.
TLDR

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

KTHXBYE
