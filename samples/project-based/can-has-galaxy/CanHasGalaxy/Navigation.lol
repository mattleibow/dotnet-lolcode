HAI 1.4

OBTW
Galaxy navigation supplies the fixed sector catalogue, compact MAP summary,
and deterministic TRAVEL state transition. SECTORNAME and MAP are pure views;
TRAVEL mutates fuel, sector, turn, and omen in its supplied game BUKKIT.
GameEngine is loaded for the deterministic seed step, while rendering remains
in Views.lol. Hoisted functions permit cross-file calls without file ordering.
TLDR

CAN HAS GameEngine?

BTW SECTORNAME maps Galaxy's fixed star chart to human-readable sector names.
HOW IZ I SECTORNAME YR number
  number
  WTF?
    OMG 0
      FOUND YR "SOL CRUMB"
    OMG 1
      FOUND YR "NEBULA OF LASER POINTERZ"
    OMG 2
      FOUND YR "MOON OF TIN FOIL"
    OMG 3
      FOUND YR "VOID OF UNSENT EMAIL"
    OMGWTF
      FOUND YR "UNMAPPED SANDWICH"
  OIC
IF U SAY SO

BTW MAP returns a portable one-line map for text-only terminals.
HOW IZ I MAP YR game
  FOUND YR SMOOSH "[0 SOL]--[1 NEBULA]--[2 FOIL MOON]--[3 RELIC VOID]  U R @ " AN game'Z sector MKAY
IF U SAY SO

BTW TRAVEL validates a destination, consumes fuel, and updates deterministic encounter state.
HOW IZ I TRAVEL YR game AN YR destination
  CAN HAS GameEngine?
  BTW A fixed chart makes explicit dispatch clearer than accepting arbitrary coordinates.
  destination
  WTF?
    OMG 0
      GTFO
    OMG 1
      GTFO
    OMG 2
      GTFO
    OMG 3
      GTFO
    OMGWTF
      FOUND YR "STAR CHART DOES NOT GO THERE."
  OIC
  BOTH SAEM game'Z player'Z fuel AN 0
  O RLY?
    YA RLY
      FOUND YR "NO FUEL. BUY SUM AT MARKET."
  OIC
  game'Z player'Z fuel R DIFF OF game'Z player'Z fuel AN 1
  game'Z sector R destination
  I IZ ADVANCE YR game MKAY
  game'Z omen R I IZ GameEngine'Z STEPSEED YR game'Z seed AN YR game'Z turn AN YR 3 MKAY
  FOUND YR SMOOSH "SECTOR " AN destination AN ":: " AN I IZ SECTORNAME YR destination MKAY AN " | " AN I IZ ENCOUNTER YR game'Z omen MKAY MKAY
IF U SAY SO

KTHXBYE
