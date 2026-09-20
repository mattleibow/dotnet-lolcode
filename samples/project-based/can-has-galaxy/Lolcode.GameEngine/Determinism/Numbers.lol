HAI 1.4

OBTW
GameEngine deterministic numeric helpers expose pure CLAMP and STEPSEED
functions for both sample games without global mutable state.
TLDR

BTW CLAMP keeps a numeric state value inside an inclusive deterministic range.
HOW IZ I CLAMP YR value AN YR minimum AN YR maximum
  BOTH SAEM value AN SMALLR OF value AN minimum
  O RLY?
    YA RLY
      FOUND YR minimum
  OIC
  BOTH SAEM value AN BIGGR OF value AN maximum
  O RLY?
    YA RLY
      FOUND YR maximum
  OIC
  FOUND YR value
IF U SAY SO

BTW STEPSEED derives a bounded deterministic value without mutable random state.
HOW IZ I STEPSEED YR seed AN YR turn AN YR modulus
  I HAS A raw ITZ SUM OF PRODUKT OF seed AN 17 AN PRODUKT OF turn AN 31
  FOUND YR MOD OF raw AN modulus
IF U SAY SO

KTHXBYE
