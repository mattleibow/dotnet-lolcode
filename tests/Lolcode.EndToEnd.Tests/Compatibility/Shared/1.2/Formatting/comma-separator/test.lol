BTW Test comma as statement separator (soft-command-break)
BTW Per spec: comma acts as virtual newline

HAI 1.2
  BTW multiple statements on one line
  I HAS A x ITZ 1, I HAS A y ITZ 2, I HAS A z ITZ 3
  VISIBLE x, VISIBLE y, VISIBLE z

  BTW comma after declaration before assignment
  I HAS A a, a R 10, VISIBLE a

  BTW comma in conditional context
  5, O RLY?, YA RLY, VISIBLE "TRUTHY", OIC

  BTW comma before comment
  VISIBLE "BEFORE COMMENT", BTW this is ignored
  VISIBLE "AFTER COMMENT"

  BTW many statements
  I HAS A n ITZ 0, n R SUM OF n AN 1, n R SUM OF n AN 1, n R SUM OF n AN 1, VISIBLE n
KTHXBYE
