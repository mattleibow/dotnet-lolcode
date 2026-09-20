HAI 1.4

OBTW
Kitteh Catacombs dashboard composes room art, adventure state, communications,
and commands with the shared retained TerminalUi framework.
TLDR

BTW CATSCREEN presents a retained 78-column crawler dashboard snapshot.
HOW IZ I CATSCREEN YR session AN YR message
  CAN HAS TerminalUi?
  I HAS A room ITZ I IZ TerminalUi'Z NEWVIEW YR 44 MKAY
  session'Z room
  WTF?
    OMG 0
      I IZ room'Z ADDLINE YR "              /\_/\       TUNA GATE" MKAY
      I IZ room'Z ADDLINE YR "             ( o.o )      LOCKED EAST" MKAY
      I IZ room'Z ADDLINE YR "              > ^ <       MOSSY STONE" MKAY
      I IZ room'Z ADDLINE YR "          ____/   \____" MKAY
      GTFO
    OMG 1
      I IZ room'Z ADDLINE YR "        .---.   .---.   .---." MKAY
      I IZ room'Z ADDLINE YR "       ( MOUSE) ( MOUSE) ( MOUSE)" MKAY
      I IZ room'Z ADDLINE YR "        '---'---'---'---'---'" MKAY
      I IZ room'Z ADDLINE YR "             CRUMBLY BRIDGE" MKAY
      GTFO
    OMGWTF
      I IZ room'Z ADDLINE YR "                 /\_/\ " MKAY
      I IZ room'Z ADDLINE YR "                ( ^.^ )" MKAY
      I IZ room'Z ADDLINE YR "             .---| T |---." MKAY
      I IZ room'Z ADDLINE YR "             '---TUNA----'" MKAY
  OIC
  I IZ room'Z ADDLINE YR "" MKAY
  I IZ room'Z ADDLINE YR I IZ CATROOMTEXT YR session'Z room MKAY MKAY
  I HAS A run ITZ I IZ TerminalUi'Z NEWVIEW YR 24 MKAY
  I IZ run'Z ADDLINE YR "KITTEH RUN STATUS" MKAY
  I IZ run'Z ADDLINE YR SMOOSH "ROOM " AN session'Z room AN " OF 2" MKAY
  I IZ run'Z ADDLINE YR I IZ TerminalUi'Z PROGRESS YR "DEPTH" AN YR session'Z room AN YR 2 AN YR 24 MKAY MKAY
  I IZ run'Z ADDLINE YR "GOAL - TUNA RELIC" MKAY
  I IZ run'Z ADDLINE YR "HEARTS - 9 / 9" MKAY
  I HAS A leftPanel ITZ I IZ TerminalUi'Z FRAME YR room AN YR " CATACOMBS " MKAY
  I HAS A rightPanel ITZ I IZ TerminalUi'Z FRAME YR run AN YR " ADVENTURE " MKAY
  I HAS A body ITZ I IZ TerminalUi'Z HSTACK YR leftPanel AN YR rightPanel AN YR 2 MKAY
  I HAS A banner ITZ I IZ TerminalUi'Z BANNER YR "KITTEH CATACOMBS / TUNA RUN" AN YR 78 MKAY
  I HAS A comms ITZ I IZ TerminalUi'Z MESSAGE YR message AN YR 78 MKAY
  I HAS A footerContent ITZ I IZ TerminalUi'Z NEWVIEW YR 74 MKAY
  I IZ footerContent'Z ADDLINE YR "LOOK STEP QUIT" MKAY
  I IZ footerContent'Z ADDLINE YR "INPUT READY - EMPTY LINE EXITS" MKAY
  I HAS A footer ITZ I IZ TerminalUi'Z FRAME YR footerContent AN YR " COMMAND DECK " MKAY
  I HAS A screen ITZ I IZ TerminalUi'Z VSTACK YR banner AN YR body MKAY
  screen R I IZ TerminalUi'Z VSTACK YR screen AN YR comms MKAY
  screen R I IZ TerminalUi'Z VSTACK YR screen AN YR footer MKAY
  I IZ TerminalUi'Z PRESENT YR screen MKAY
  FOUND YR screen
IF U SAY SO

KTHXBYE
