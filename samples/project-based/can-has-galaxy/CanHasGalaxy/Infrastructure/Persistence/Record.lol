HAI 1.4

OBTW
Galaxy persistence record parsing verifies the CHG3 marker and groups parsed
primitive fields in a transient BUKKIT for validation and reconstruction.
TLDR

BTW PARSESTATE places the CHG3 primitive fields in a transient BUKKIT.
HOW IZ I PARSESTATE YR record
  CAN HAS STRING?
  DIFFRINT I IZ STRING'Z AT YR record AN YR 0 MKAY AN "C"
  O RLY?
    YA RLY
      FOUND YR NOOB
  OIC
  DIFFRINT I IZ STRING'Z AT YR record AN YR 1 MKAY AN "H"
  O RLY?
    YA RLY
      FOUND YR NOOB
  OIC
  DIFFRINT I IZ STRING'Z AT YR record AN YR 2 MKAY AN "G"
  O RLY?
    YA RLY
      FOUND YR NOOB
  OIC
  DIFFRINT I IZ STRING'Z AT YR record AN YR 3 MKAY AN "3"
  O RLY?
    YA RLY
      FOUND YR NOOB
  OIC
  BTW Parse all fields before bounded validation so malformed records fail uniformly.
  I HAS A state ITZ A BUKKIT
  state HAS A sector ITZ I IZ READFIELD YR record AN YR 0 MKAY
  state HAS A credits ITZ I IZ READFIELD YR record AN YR 1 MKAY
  state HAS A fuel ITZ I IZ READFIELD YR record AN YR 2 MKAY
  state HAS A hull ITZ I IZ READFIELD YR record AN YR 3 MKAY
  state HAS A ore ITZ I IZ READFIELD YR record AN YR 4 MKAY
  state HAS A relic ITZ I IZ READFIELD YR record AN YR 5 MKAY
  state HAS A turn ITZ I IZ READFIELD YR record AN YR 6 MKAY
  state HAS A seed ITZ I IZ READFIELD YR record AN YR 7 MKAY
  state HAS A omen ITZ I IZ READFIELD YR record AN YR 8 MKAY
  FOUND YR state
IF U SAY SO

KTHXBYE
