BTW Test O RLY? truthiness evaluation of IT
BTW Per spec: 0=FAIL, ""=FAIL, NOOB=FAIL, 1=WIN, "a"=WIN

HAI 1.2
  BTW test NUMBR 0 (should be FAIL)
  0
  O RLY?
    YA RLY
      VISIBLE "0 IS TRUTHY"
    NO WAI
      VISIBLE "0 IS FALSY"
  OIC

  BTW test empty string (should be FAIL)
  ""
  O RLY?
    YA RLY
      VISIBLE "EMPTY STRING IS TRUTHY"
    NO WAI
      VISIBLE "EMPTY STRING IS FALSY"
  OIC

  BTW test NOOB (should be FAIL)
  I HAS A null_var
  null_var
  O RLY?
    YA RLY
      VISIBLE "NOOB IS TRUTHY"
    NO WAI
      VISIBLE "NOOB IS FALSY"
  OIC

  BTW test positive NUMBR (should be WIN)
  1
  O RLY?
    YA RLY
      VISIBLE "1 IS TRUTHY"
    NO WAI
      VISIBLE "1 IS FALSY"
  OIC

  BTW test non-empty string (should be WIN)
  "a"
  O RLY?
    YA RLY
      VISIBLE "NON-EMPTY STRING IS TRUTHY"
    NO WAI
      VISIBLE "NON-EMPTY STRING IS FALSY"
  OIC
KTHXBYE