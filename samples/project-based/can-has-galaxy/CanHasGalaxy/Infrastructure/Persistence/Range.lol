HAI 1.4

OBTW
Galaxy persistence range validation provides the reusable inclusive numeric
bound check used by CHG3 state validation.
TLDR

BTW INRANGE validates a numeric primitive before it is allowed into game state.
HOW IZ I INRANGE YR value AN YR minimum AN YR maximum
  BOTH SAEM value AN BIGGR OF value AN minimum
  O RLY?
    YA RLY
    NO WAI
      FOUND YR FAIL
  OIC
  BOTH SAEM value AN SMALLR OF value AN maximum
  O RLY?
    YA RLY
      FOUND YR WIN
    NO WAI
      FOUND YR FAIL
  OIC
IF U SAY SO

KTHXBYE
