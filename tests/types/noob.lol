BTW Test NOOB (untyped/null) behavior
BTW Per spec: NOOB only implicit cast to TROOF = FAIL
BTW Per spec: explicit cast to NUMBR=0, NUMBAR=0.0, YARN=""

HAI 1.2
  BTW uninitialized is NOOB
  I HAS A noob_var

  BTW NOOB printed is empty string (cast to YARN)
  VISIBLE "BEFORE"
  VISIBLE noob_var
  VISIBLE "AFTER"

  BTW NOOB is falsy
  noob_var
  O RLY?
    YA RLY
      VISIBLE "NOOB IS TRUTHY"
    NO WAI
      VISIBLE "NOOB IS FALSY"
  OIC

  BTW explicit cast NOOB to TROOF
  I HAS A noob2
  VISIBLE MAEK noob2 A TROOF

  BTW explicit cast NOOB to NUMBR
  I HAS A noob3
  VISIBLE MAEK noob3 A NUMBR

  BTW explicit cast NOOB to NUMBAR
  I HAS A noob4
  VISIBLE MAEK noob4 A NUMBAR

  BTW explicit cast NOOB to YARN
  I HAS A noob5
  VISIBLE "YARN::"
  VISIBLE MAEK noob5 A YARN
  VISIBLE "::END"

  BTW NOOB in boolean expression (implicitly cast to FAIL)
  I HAS A noob6
  VISIBLE NOT noob6
  VISIBLE EITHER OF noob6 AN WIN
KTHXBYE
