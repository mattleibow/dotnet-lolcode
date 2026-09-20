BTW Test NUMBR (integer) type
BTW Per spec: contiguous digits without decimal, optional leading hyphen

HAI 1.2
  BTW positive integers
  I HAS A zero ITZ 0
  I HAS A one ITZ 1
  I HAS A small ITZ 42
  I HAS A big ITZ 999999

  VISIBLE zero
  VISIBLE one
  VISIBLE small
  VISIBLE big

  BTW negative integers
  I HAS A neg_one ITZ -1
  I HAS A neg_big ITZ -12345

  VISIBLE neg_one
  VISIBLE neg_big

  BTW arithmetic with integers
  I HAS A a ITZ 10
  I HAS A b ITZ 3

  VISIBLE SUM OF a AN b
  VISIBLE DIFF OF a AN b
  VISIBLE PRODUKT OF a AN b
  VISIBLE QUOSHUNT OF a AN b
  VISIBLE MOD OF a AN b

  BTW integer division truncates
  VISIBLE QUOSHUNT OF 7 AN 2
  VISIBLE QUOSHUNT OF -7 AN 2

  BTW min/max with integers
  VISIBLE BIGGR OF 5 AN 10
  VISIBLE SMALLR OF 5 AN 10
  VISIBLE BIGGR OF -5 AN -10
  VISIBLE SMALLR OF -5 AN -10
KTHXBYE
