HAI 1.4

OBTW
Galaxy game model owns fresh game construction, save reconstruction, and clock
advancement. It has no rendering or persistence effects.
TLDR

BTW CREATEGAME creates a fresh deterministic Galaxy state BUKKIT.
HOW IZ I CREATEGAME YR name AN YR seed
  I HAS A game ITZ A BUKKIT
  game HAS A player ITZ I IZ NEWPLAYER YR name MKAY
  game HAS A sector ITZ 0
  game HAS A turn ITZ 0
  game HAS A seed ITZ seed
  game HAS A omen ITZ 0
  game HAS A mission ITZ 0
  FOUND YR game
IF U SAY SO

BTW CREATEFROM reconstructs validated primitive save data into a Galaxy state.
HOW IZ I CREATEFROM YR name AN YR sector AN YR credits AN YR fuel ...
  AN YR hull AN YR ore AN YR relic AN YR turn AN YR seed AN YR omen
  I HAS A game ITZ I IZ CREATEGAME YR name AN YR seed MKAY
  game'Z sector R sector
  game'Z turn R turn
  game'Z omen R omen
  game'Z player'Z credits R credits
  game'Z player'Z fuel R fuel
  game'Z player'Z hull R hull
  game'Z player'Z ore R ore
  game'Z player'Z relic R relic
  FOUND YR game
IF U SAY SO

BTW ADVANCE moves the game clock and returns its new deterministic turn number.
HOW IZ I ADVANCE YR game
  game'Z turn R SUM OF game'Z turn AN 1
  FOUND YR game'Z turn
IF U SAY SO

KTHXBYE
