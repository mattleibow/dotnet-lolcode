HAI 1.4

OBTW
Galaxy dashboard composes and presents the retained terminal screen.
TLDR

BTW BUILDSCREEN composes one complete retained 78-column Galaxy dashboard.
HOW IZ I BUILDSCREEN YR game AN YR message
  CAN HAS TerminalUi?
  I HAS A chart ITZ I IZ TerminalUi'Z NEWVIEW YR 44 MKAY
  I IZ chart'Z ADDLINE YR "             [0] SOL CRUMB" MKAY
  I IZ chart'Z ADDLINE YR "                    |" MKAY
  I IZ chart'Z ADDLINE YR "[2] FOIL MOON ------+------ [1] LASER NEBULA" MKAY
  I IZ chart'Z ADDLINE YR "                    |" MKAY
  I IZ chart'Z ADDLINE YR "            [3] RELIC VOID" MKAY
  I IZ chart'Z ADDLINE YR "" MKAY
  I IZ chart'Z ADDLINE YR SMOOSH "CURRENT [" AN game'Z sector AN "] " AN I IZ SECTORNAME YR game'Z sector MKAY MKAY
  I HAS A ship ITZ I IZ TerminalUi'Z NEWVIEW YR 24 MKAY
  I IZ ship'Z ADDLINE YR SMOOSH "CAPTAIN " AN game'Z player'Z name MKAY
  I IZ ship'Z ADDLINE YR SMOOSH "TURN " AN game'Z turn AN "  SECTOR " AN game'Z sector MKAY
  I IZ ship'Z ADDLINE YR SMOOSH "CREDITS " AN game'Z player'Z credits MKAY
  I IZ ship'Z ADDLINE YR SMOOSH "CARGO ORE " AN game'Z player'Z ore MKAY
  I IZ ship'Z ADDLINE YR I IZ TerminalUi'Z PROGRESS YR "HULL" AN YR game'Z player'Z hull AN YR 9 AN YR 24 MKAY MKAY
  I IZ ship'Z ADDLINE YR I IZ TerminalUi'Z PROGRESS YR "FUEL" AN YR game'Z player'Z fuel AN YR 9 AN YR 24 MKAY MKAY
  BOTH SAEM game'Z player'Z relic AN 1
  O RLY?
    YA RLY
      I IZ ship'Z ADDLINE YR "RELIC SECURED - GO HOME" MKAY
    NO WAI
      I IZ ship'Z ADDLINE YR "RELIC LOST - SEARCH S3" MKAY
  OIC
  I HAS A leftPanel ITZ I IZ TerminalUi'Z FRAME YR chart AN YR " GALACTIC MAP " MKAY
  I HAS A rightPanel ITZ I IZ TerminalUi'Z FRAME YR ship AN YR " SHIP STATUS " MKAY
  I HAS A body ITZ I IZ TerminalUi'Z HSTACK YR leftPanel AN YR rightPanel AN YR 2 MKAY
  I HAS A banner ITZ I IZ TerminalUi'Z BANNER YR "CAN HAS GALAXY? / SPACE TRADER" AN YR 78 MKAY
  I HAS A comms ITZ I IZ TerminalUi'Z MESSAGE YR message AN YR 78 MKAY
  I HAS A footerContent ITZ I IZ TerminalUi'Z NEWVIEW YR 74 MKAY
  I IZ footerContent'Z ADDLINE YR "STATUS MAP TRAVEL1 TRAVEL2 TRAVEL3" MKAY
  I IZ footerContent'Z ADDLINE YR "MINE SELL FUEL FIGHT MISSION SAVE LOAD QUIT" MKAY
  I IZ footerContent'Z ADDLINE YR "INPUT READY - EMPTY LINE EXITS" MKAY
  I HAS A footer ITZ I IZ TerminalUi'Z FRAME YR footerContent AN YR " COMMAND DECK " MKAY
  I HAS A screen ITZ I IZ TerminalUi'Z VSTACK YR banner AN YR body MKAY
  screen R I IZ TerminalUi'Z VSTACK YR screen AN YR comms MKAY
  screen R I IZ TerminalUi'Z VSTACK YR screen AN YR footer MKAY
  FOUND YR screen
IF U SAY SO

BTW RENDERDASHBOARD presents one complete screen before an interactive prompt.
HOW IZ I RENDERDASHBOARD YR game AN YR message
  CAN HAS TerminalUi?
  I HAS A screen ITZ I IZ BUILDSCREEN YR game AN YR message MKAY
  I IZ TerminalUi'Z PRESENT YR screen MKAY
  FOUND YR screen
IF U SAY SO

KTHXBYE
