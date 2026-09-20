HAI 1.4

CAN HAS TerminalUi?

BTW STATUS returns a concise one-line public status view of Galaxy state.
HOW IZ I STATUS YR game
  FOUND YR SMOOSH "CAPTAIN " AN game'Z player'Z name AN " | T" AN game'Z turn AN " | S" AN game'Z sector AN " | C" AN game'Z player'Z credits AN " | F" AN game'Z player'Z fuel AN " | H" AN game'Z player'Z hull AN " | ORE " AN game'Z player'Z ore MKAY
IF U SAY SO

BTW RENDERSTATUS composes Galaxy state with reusable TerminalUi widgets.
HOW IZ I RENDERSTATUS YR game
  CAN HAS TerminalUi?
  I IZ TerminalUi'Z PANEL YR "STATUS" AN YR I IZ STATUS YR game MKAY MKAY
  I IZ TerminalUi'Z STATUSBAR YR "HULL" AN YR game'Z player'Z hull AN YR 9 MKAY
  I IZ TerminalUi'Z STATUSBAR YR "FUEL" AN YR game'Z player'Z fuel AN YR 9 MKAY
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
