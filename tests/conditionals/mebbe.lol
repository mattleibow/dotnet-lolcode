BTW Test multiple MEBBE (else-if) chains
BTW MEBBE expressions are evaluated in order until one is WIN

HAI 1.2
  BTW test first MEBBE case
  I HAS A val ITZ 1
  val
  O RLY?
    YA RLY
      VISIBLE "FIRST"
    MEBBE BOTH SAEM val AN 1
      VISIBLE "MEBBE 1"
    MEBBE BOTH SAEM val AN 2
      VISIBLE "MEBBE 2"
    NO WAI
      VISIBLE "NONE"
  OIC

  BTW test second MEBBE case
  I HAS A val2 ITZ 2
  FAIL
  O RLY?
    YA RLY
      VISIBLE "FIRST"
    MEBBE BOTH SAEM val2 AN 1
      VISIBLE "MEBBE 1"
    MEBBE BOTH SAEM val2 AN 2
      VISIBLE "MEBBE 2"
    NO WAI
      VISIBLE "NONE"
  OIC

  BTW test NO WAI fallback
  I HAS A val3 ITZ 99
  FAIL
  O RLY?
    YA RLY
      VISIBLE "FIRST"
    MEBBE BOTH SAEM val3 AN 1
      VISIBLE "MEBBE 1"
    MEBBE BOTH SAEM val3 AN 2
      VISIBLE "MEBBE 2"
    NO WAI
      VISIBLE "NONE"
  OIC
KTHXBYE