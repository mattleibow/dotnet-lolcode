HAI 1.4

OBTW
Galaxy combat implements the deterministic FIGHT transition. It imports
GameEngine locally for bounded damage, mutates only the provided game/player
BUKKIT, and returns a communications-ready outcome YARN. This file owns no
terminal output, persistence handles, or top-level initialization. Its direct
function is hoisted, so model and story helpers may reside in any SDK glob
order.
TLDR

CAN HAS GameEngine?

BTW FIGHT applies deterministic damage, bounty, and relic progress to Galaxy state.
HOW IZ I FIGHT YR game
  CAN HAS GameEngine?
  I HAS A damage ITZ SUM OF 1 AN I IZ GameEngine'Z STEPSEED YR game'Z omen AN YR game'Z turn AN YR 2 MKAY
  game'Z player'Z hull R I IZ GameEngine'Z CLAMP YR DIFF OF game'Z player'Z hull AN damage AN YR 0 AN YR 9 MKAY
  I IZ ADVANCE YR game MKAY
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

KTHXBYE
