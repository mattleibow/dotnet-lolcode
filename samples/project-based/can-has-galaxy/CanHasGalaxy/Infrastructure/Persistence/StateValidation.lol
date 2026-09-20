HAI 1.4

OBTW
Galaxy persistence state validation applies the exact CHG3 required-field and
bounded-value rules before fresh game state is constructed.
TLDR

BTW VALIDSTATE preserves CHG3's exact required-field and bounded-value rules.
HOW IZ I VALIDSTATE YR state
  NOT I IZ HASREQUIREDFIELDS YR state MKAY
  O RLY?
    YA RLY
      FOUND YR FAIL
  OIC
  NOT I IZ INRANGE YR state'Z sector AN YR 0 AN YR 3 MKAY
  O RLY?
    YA RLY
      FOUND YR FAIL
  OIC
  NOT I IZ INRANGE YR state'Z credits AN YR 0 AN YR 2147483647 MKAY
  O RLY?
    YA RLY, FOUND YR FAIL
  OIC
  NOT I IZ INRANGE YR state'Z fuel AN YR 0 AN YR 9 MKAY
  O RLY?
    YA RLY, FOUND YR FAIL
  OIC
  NOT I IZ INRANGE YR state'Z hull AN YR 0 AN YR 9 MKAY
  O RLY?
    YA RLY, FOUND YR FAIL
  OIC
  NOT I IZ INRANGE YR state'Z ore AN YR 0 AN YR 2147483647 MKAY
  O RLY?
    YA RLY, FOUND YR FAIL
  OIC
  NOT I IZ INRANGE YR state'Z relic AN YR 0 AN YR 1 MKAY
  O RLY?
    YA RLY, FOUND YR FAIL
  OIC
  NOT I IZ INRANGE YR state'Z turn AN YR 0 AN YR 2147483647 MKAY
  O RLY?
    YA RLY, FOUND YR FAIL
  OIC
  NOT I IZ INRANGE YR state'Z seed AN YR 0 AN YR 2147483647 MKAY
  O RLY?
    YA RLY, FOUND YR FAIL
  OIC
  NOT I IZ INRANGE YR state'Z omen AN YR 0 AN YR 2 MKAY
  O RLY?
    YA RLY
      FOUND YR FAIL
  OIC
  FOUND YR WIN
IF U SAY SO

KTHXBYE
