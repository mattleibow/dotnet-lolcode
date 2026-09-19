HAI 1.4

CAN HAS GalaxyCollections?
CAN HAS GalaxyWorld?
CAN HAS GalaxyStory?
CAN HAS STDLIB?

HOW IZ I ADVANCE YR game
  game'Z turn R SUM OF game'Z turn AN 1
  FOUND YR game'Z turn
IF U SAY SO

HOW IZ I STATUS YR game
  FOUND YR SMOOSH "CAPTAIN " AN game'Z player'Z name AN " | T" AN game'Z turn AN " | S" AN game'Z sector AN " | C" AN game'Z player'Z credits AN " | F" AN game'Z player'Z fuel AN " | H" AN game'Z player'Z hull AN " | ORE " AN game'Z player'Z ore MKAY
IF U SAY SO

HOW IZ I CREATEGAME YR name AN YR seed
  CAN HAS STDLIB?
  CAN HAS GalaxyWorld?
  I IZ STDLIB'Z MIX YR seed MKAY
  I HAS A game ITZ A BUKKIT
  game HAS A player ITZ I IZ GalaxyWorld'Z NEWPLAYER YR name MKAY
  game HAS A sector ITZ 0
  game HAS A turn ITZ 0
  game HAS A seed ITZ seed
  game HAS A omen ITZ I IZ STDLIB'Z BLOW YR 3 MKAY
  game HAS A mission ITZ 0
  FOUND YR game
IF U SAY SO

HOW IZ I CREATEFROM YR name AN YR sector AN YR credits AN YR fuel ...
  AN YR hull AN YR ore AN YR relic AN YR turn AN YR seed AN YR omen
  CAN HAS GalaxyWorld?
  I HAS A game ITZ A BUKKIT
  game HAS A player ITZ I IZ GalaxyWorld'Z NEWPLAYER YR name MKAY
  game HAS A sector ITZ sector
  game HAS A turn ITZ turn
  game HAS A seed ITZ seed
  game HAS A omen ITZ omen
  game HAS A mission ITZ 0
  game'Z player'Z credits R credits
  game'Z player'Z fuel R fuel
  game'Z player'Z hull R hull
  game'Z player'Z ore R ore
  game'Z player'Z relic R relic
  FOUND YR game
IF U SAY SO

HOW IZ I TRAVEL YR game AN YR destination
  CAN HAS GalaxyStory?
  BOTH SAEM game'Z player'Z fuel AN 0
  O RLY?
    YA RLY
      FOUND YR "NO FUEL. BUY SUM AT MARKET."
  OIC
  game'Z player'Z fuel R DIFF OF game'Z player'Z fuel AN 1
  game'Z sector R destination
  I IZ ME'Z ADVANCE YR game MKAY
  game'Z omen R MOD OF SUM OF game'Z seed AN game'Z turn AN destination
  game'Z omen R MOD OF game'Z omen AN 3
  FOUND YR SMOOSH I IZ GalaxyStory'Z ARRIVAL YR destination MKAY AN " | " AN I IZ GalaxyStory'Z ENCOUNTER YR game'Z omen MKAY MKAY
IF U SAY SO

HOW IZ I MINE YR game
  I HAS A yield ITZ SUM OF 1 AN MOD OF SUM OF game'Z sector AN game'Z turn AN 2
  I HAS A cargo ITZ game'Z player'Z cargo
  I IZ cargo'Z append YR yield MKAY
  game'Z player'Z ore R SUM OF game'Z player'Z ore AN yield
  I IZ ME'Z ADVANCE YR game MKAY
  FOUND YR SMOOSH "MINED " AN yield AN " ORE. HOLD IZ " AN game'Z player'Z ore MKAY
IF U SAY SO

HOW IZ I BUYFUEL YR game
  CAN HAS GalaxyWorld?
  I HAS A market ITZ I IZ GalaxyWorld'Z NEWMARKET YR game'Z sector MKAY
  BOTH SAEM BIGGR OF game'Z player'Z credits AN market'Z fuelPrice AN game'Z player'Z credits
  O RLY?
    YA RLY
    NO WAI
      FOUND YR "NOT ENUFF CREDITZ. SELL ORE."
  OIC
  game'Z player'Z credits R DIFF OF game'Z player'Z credits AN market'Z fuelPrice
  game'Z player'Z fuel R SMALLR OF 9 AN SUM OF game'Z player'Z fuel AN 2
  FOUND YR SMOOSH "BOUGHT FUEL FOR " AN market'Z fuelPrice AN ". FUEL " AN game'Z player'Z fuel MKAY
IF U SAY SO

HOW IZ I SELLORE YR game
  CAN HAS GalaxyWorld?
  BOTH SAEM game'Z player'Z ore AN 0
  O RLY?
    YA RLY
      FOUND YR "NO ORE IN HOLD."
  OIC
  I HAS A market ITZ I IZ GalaxyWorld'Z NEWMARKET YR game'Z sector MKAY
  I HAS A profit ITZ PRODUKT OF game'Z player'Z ore AN market'Z orePrice
  game'Z player'Z credits R SUM OF game'Z player'Z credits AN profit
  game'Z player'Z ore R 0
  FOUND YR SMOOSH "SOLD ORE FOR " AN profit AN " CREDITZ." MKAY
IF U SAY SO

HOW IZ I FIGHT YR game
  I HAS A damage ITZ SUM OF 1 AN MOD OF SUM OF game'Z omen AN game'Z turn AN 2
  game'Z player'Z hull R BIGGR OF 0 AN DIFF OF game'Z player'Z hull AN damage
  I IZ ME'Z ADVANCE YR game MKAY
  BOTH SAEM game'Z player'Z hull AN 0
  O RLY?
    YA RLY
      FOUND YR "UR SHIP IZ SPACE DUST. GAME OVER."
  OIC
  game'Z player'Z credits R SUM OF game'Z player'Z credits AN 3
  BOTH SAEM game'Z sector AN 3
  O RLY?
    YA RLY
      game'Z player'Z relic R 1
  OIC
  FOUND YR SMOOSH "WON TEH DOGFIGHT. HULL -" AN damage AN ", BOUNTY +3." MKAY
IF U SAY SO

HOW IZ I MAP YR game
  FOUND YR SMOOSH "[0 SOL]--[1 NEBULA]--[2 FOIL MOON]--[3 RELIC VOID]  U R @ " AN game'Z sector MKAY
IF U SAY SO

HOW IZ I MISSIONTEXT YR game
  CAN HAS GalaxyStory?
  FOUND YR I IZ GalaxyStory'Z MISSION YR game'Z player'Z relic MKAY
IF U SAY SO

HOW IZ I HEADLESS
  I HAS A game ITZ I IZ ME'Z CREATEGAME YR "BOT" AN YR 4242 MKAY
  VISIBLE "CAN HAS GALAXY? SIMULATION v1"
  VISIBLE I IZ ME'Z STATUS YR game MKAY
  VISIBLE I IZ ME'Z TRAVEL YR game AN YR 1 MKAY
  VISIBLE I IZ ME'Z MINE YR game MKAY
  VISIBLE I IZ ME'Z SELLORE YR game MKAY
  VISIBLE I IZ ME'Z TRAVEL YR game AN YR 3 MKAY
  VISIBLE I IZ ME'Z FIGHT YR game MKAY
  VISIBLE I IZ ME'Z MISSIONTEXT YR game MKAY
  VISIBLE I IZ ME'Z STATUS YR game MKAY
IF U SAY SO

KTHXBYE
