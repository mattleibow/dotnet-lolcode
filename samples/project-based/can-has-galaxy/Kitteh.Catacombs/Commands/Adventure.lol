HAI 1.4

OBTW
Kitteh Catacombs adventure handlers expose room inspection and deterministic
movement through the shared command router.
TLDR

BTW CATLOOK returns a room description for the next dashboard communications panel.
HOW IZ I CATLOOK YR session
  FOUND YR I IZ CATROOMTEXT YR session'Z room MKAY
IF U SAY SO

BTW CATSTEP advances a finite deterministic three-room catacomb crawl.
HOW IZ I CATSTEP YR session
  BOTH SAEM session'Z room AN 2
  O RLY?
    YA RLY
      FOUND YR "CATACOMBS COMPLETE."
  OIC
  session'Z room R SUM OF session'Z room AN 1
  BOTH SAEM session'Z room AN 2
  O RLY?
    YA RLY
      session'Z running R FAIL
      FOUND YR "CATACOMBS COMPLETE."
    NO WAI
      FOUND YR "U PAD DEEPER. MICE SCATTER."
  OIC
IF U SAY SO

KTHXBYE
