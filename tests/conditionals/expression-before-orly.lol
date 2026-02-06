BTW Test expression before O RLY? sets IT, then conditional reads it
BTW Verify IT persists across statements and O RLY? uses current value

HAI 1.2
  BTW expression sets IT, next statement reads it
  SUM OF 3 AN 7
  O RLY?
    YA RLY
      VISIBLE "10 IS TRUTHY"
    NO WAI
      VISIBLE "10 IS FALSY"
  OIC

  BTW another expression overwrites IT
  DIFF OF 5 AN 5
  O RLY?
    YA RLY
      VISIBLE "0 IS TRUTHY"
    NO WAI
      VISIBLE "0 IS FALSY"
  OIC

  BTW assignment doesn't affect IT
  I HAS A x ITZ 99
  x R 100
  BTW IT should still be 0 from above
  O RLY?
    YA RLY
      VISIBLE "STILL TRUTHY"
    NO WAI
      VISIBLE "STILL FALSY"
  OIC

  BTW new expression sets IT again
  BOTH SAEM 1 AN 1
  O RLY?
    YA RLY
      VISIBLE "TRUE IS TRUTHY"
    NO WAI
      VISIBLE "TRUE IS FALSY"
  OIC
KTHXBYE