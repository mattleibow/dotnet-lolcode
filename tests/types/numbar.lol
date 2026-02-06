BTW Test NUMBAR (float) type
BTW Per spec: digits with exactly one decimal point, optional leading hyphen
BTW Per spec: NUMBAR cast to YARN truncates to 2 decimal places

HAI 1.2
  BTW positive floats
  I HAS A zero ITZ 0.0
  I HAS A one ITZ 1.0
  I HAS A pi ITZ 3.14159
  I HAS A small ITZ 0.001

  VISIBLE zero
  VISIBLE one
  VISIBLE pi
  VISIBLE small

  BTW negative floats
  I HAS A neg_one ITZ -1.0
  I HAS A neg_pi ITZ -3.14159

  VISIBLE neg_one
  VISIBLE neg_pi

  BTW floats with many decimals (output truncated to 2)
  I HAS A long_decimal ITZ 1.23456789
  VISIBLE long_decimal

  BTW float arithmetic
  I HAS A a ITZ 10.5
  I HAS A b ITZ 3.2

  VISIBLE SUM OF a AN b
  VISIBLE DIFF OF a AN b
  VISIBLE PRODUKT OF a AN b
  VISIBLE QUOSHUNT OF a AN b

  BTW mixed int/float (result is float)
  VISIBLE SUM OF 5 AN 2.5
  VISIBLE PRODUKT OF 3 AN 1.5

  BTW float min/max
  VISIBLE BIGGR OF 3.14 AN 2.71
  VISIBLE SMALLR OF 3.14 AN 2.71
KTHXBYE
