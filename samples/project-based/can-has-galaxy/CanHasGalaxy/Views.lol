HAI 1.4

OBTW
Galaxy view builder composes domain-only game state into a 78-column retained
TerminalUi dashboard. Public STATUS remains a compact testable state summary;
BUILDSCREEN constructs the banner, framed star chart, framed ship sidebar,
communications panel, command footer, and prompt label without printing.
RENDERDASHBOARD is the one snapshot side effect used by the CLI. TerminalUi is
imported function-locally, source order is immaterial, and this file owns no
mutable game state or command dispatch.
TLDR

BTW STATUS returns a concise one-line public status view of Galaxy state.
HOW IZ I STATUS YR game
  FOUND YR SMOOSH "CAPTAIN " AN game'Z player'Z name AN " | T" AN game'Z turn AN " | S" AN game'Z sector AN " | C" AN game'Z player'Z credits AN " | F" AN game'Z player'Z fuel AN " | H" AN game'Z player'Z hull AN " | ORE " AN game'Z player'Z ore MKAY
IF U SAY SO

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

BTW RENDERSTATUS remains a compatibility wrapper for previous callers.
HOW IZ I RENDERSTATUS YR game
  I IZ RENDERDASHBOARD YR game AN YR "STATUS REQUESTED." MKAY
  FOUND YR I IZ STATUS YR game MKAY
IF U SAY SO

BTW HEADLESS runs a finite deterministic scenario for scripts and smoke tests.
HOW IZ I HEADLESS
  I HAS A game ITZ I IZ CREATEGAME YR "BOT" AN YR 4242 MKAY
  VISIBLE "CAN HAS GALAXY? SIMULATION v2"
  VISIBLE I IZ STATUS YR game MKAY
  VISIBLE I IZ TRAVEL YR game AN YR 1 MKAY
  VISIBLE I IZ MINE YR game MKAY
  VISIBLE I IZ SELLORE YR game MKAY
  VISIBLE I IZ TRAVEL YR game AN YR 3 MKAY
  VISIBLE I IZ FIGHT YR game MKAY
  VISIBLE I IZ MISSIONTEXT YR game MKAY
  VISIBLE I IZ STATUS YR game MKAY
  FOUND YR game
IF U SAY SO

KTHXBYE
