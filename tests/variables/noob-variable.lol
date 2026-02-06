BTW Test uninitialized variables are NOOB
BTW Per spec: until given initial value, variable is untyped (NOOB)

HAI 1.2
  BTW declare without initialization
  I HAS A noob_var

  BTW NOOB printed via VISIBLE casts to YARN which is ""
  VISIBLE "START"
  VISIBLE noob_var
  VISIBLE "END"

  BTW NOOB is falsy (only implicit cast from NOOB is to TROOF = FAIL)
  noob_var
  O RLY?
    YA RLY
      VISIBLE "NOOB IS TRUTHY"
    NO WAI
      VISIBLE "NOOB IS FALSY"
  OIC

  BTW after assignment, no longer NOOB
  noob_var R "NOW DEFINED"
  VISIBLE noob_var

  BTW another uninitialized variable
  I HAS A another
  another
  O RLY?
    YA RLY
      VISIBLE "ANOTHER IS TRUTHY"
    NO WAI
      VISIBLE "ANOTHER IS FALSY"
  OIC
KTHXBYE
