HAI 1.4

OBTW
Galaxy trading owns mining, fuel purchase, and ore sale state transitions.
TLDR

CAN HAS GameEngine?

BTW MINE mutates cargo and ore totals using a small deterministic yield.
HOW IZ I MINE YR game
  I HAS A yield ITZ SUM OF 1 AN MOD OF SUM OF game'Z sector AN game'Z turn AN 2
  I IZ game'Z player'Z cargo'Z ADD YR yield MKAY
  game'Z player'Z ore R SUM OF game'Z player'Z ore AN yield
  I IZ ADVANCE YR game MKAY
  FOUND YR SMOOSH "MINED " AN yield AN " ORE. HOLD IZ " AN game'Z player'Z ore MKAY
IF U SAY SO

BTW BUYFUEL validates credits and caps fuel through the shared engine helper.
HOW IZ I BUYFUEL YR game
  CAN HAS GameEngine?
  I HAS A market ITZ I IZ NEWMARKET YR game'Z sector MKAY
  BOTH SAEM BIGGR OF game'Z player'Z credits AN market'Z fuelPrice AN game'Z player'Z credits
  O RLY?
    YA RLY
    NO WAI
      FOUND YR "NOT ENUFF CREDITZ. SELL ORE."
  OIC
  game'Z player'Z credits R DIFF OF game'Z player'Z credits AN market'Z fuelPrice
  game'Z player'Z fuel R I IZ GameEngine'Z CLAMP YR SUM OF game'Z player'Z fuel AN 2 AN YR 0 AN YR 9 MKAY
  FOUND YR SMOOSH "BOUGHT FUEL FOR " AN market'Z fuelPrice AN ". FUEL " AN game'Z player'Z fuel MKAY
IF U SAY SO

BTW SELLORE exchanges every held ore unit for the local deterministic price.
HOW IZ I SELLORE YR game
  BOTH SAEM game'Z player'Z ore AN 0
  O RLY?
    YA RLY
      FOUND YR "NO ORE IN HOLD."
  OIC
  I HAS A market ITZ I IZ NEWMARKET YR game'Z sector MKAY
  I HAS A profit ITZ PRODUKT OF game'Z player'Z ore AN market'Z orePrice
  game'Z player'Z credits R SUM OF game'Z player'Z credits AN profit
  game'Z player'Z ore R 0
  FOUND YR SMOOSH "SOLD ORE FOR " AN profit AN " CREDITZ." MKAY
IF U SAY SO

KTHXBYE
