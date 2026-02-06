BTW Test O RLY? with only YA RLY branch (no NO WAI)
BTW Should execute YA RLY when true, do nothing when false

HAI 1.2
  BTW test true condition - should execute
  WIN
  O RLY?
    YA RLY
      VISIBLE "EXECUTED"
  OIC

  BTW test false condition - should do nothing
  FAIL
  O RLY?
    YA RLY
      VISIBLE "NOT EXECUTED"
  OIC

  VISIBLE "DONE"
KTHXBYE