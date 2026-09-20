BTW Test that assignment and declaration do NOT update IT
BTW Per spec: assignment statements have no side effects with IT

HAI 1.2
  BTW set IT to a known value
  42
  VISIBLE IT

  BTW declaration does not change IT
  I HAS A x ITZ 100
  VISIBLE IT

  BTW assignment does not change IT
  x R 200
  VISIBLE IT

  BTW another assignment
  I HAS A y
  y R 999
  VISIBLE IT

  BTW expression statement DOES change IT
  SUM OF 1 AN 1
  VISIBLE IT

  BTW but declaration right after does NOT change IT
  I HAS A z ITZ 777
  VISIBLE IT

  BTW assignment with expression does NOT change IT
  BTW (the expression is evaluated, but IT is not updated)
  x R SUM OF 10 AN 20
  VISIBLE IT
KTHXBYE
