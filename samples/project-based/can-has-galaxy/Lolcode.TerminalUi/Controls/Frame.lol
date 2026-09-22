HAI 1.4

OBTW
TerminalUi frame control adds Unicode boxes to fixed-width retained content.
TLDR

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

KTHXBYE
