HAI 1.4

OBTW
Galaxy loading orchestrates local STDIO reading, parsed-state validation, and
fresh game construction. The resource handle remains local to this function.
TLDR

BTW LOAD validates every persisted primitive and returns fresh state or NOOB.
HOW IZ I LOAD YR filename
  CAN HAS STDIO?
  I HAS A file ITZ I IZ STDIO'Z OPEN YR filename AN YR "r" MKAY
  I IZ STDIO'Z DIAF YR file MKAY
  O RLY?
    YA RLY
      FOUND YR NOOB
  OIC
  I HAS A record ITZ I IZ STDIO'Z LUK YR file AN YR 512 MKAY
  I IZ STDIO'Z CLOSE YR file MKAY
  I HAS A state ITZ I IZ PARSESTATE YR record MKAY
  I IZ VALIDSTATE YR state MKAY
  O RLY?
    YA RLY
      FOUND YR I IZ CREATEFROM YR "CAPTAIN" AN YR state'Z sector AN YR state'Z credits AN YR state'Z fuel ...
        AN YR state'Z hull AN YR state'Z ore AN YR state'Z relic AN YR state'Z turn AN YR state'Z seed AN YR state'Z omen MKAY
    NO WAI
      FOUND YR NOOB
  OIC
IF U SAY SO

KTHXBYE
