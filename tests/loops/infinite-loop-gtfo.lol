BTW Test infinite loop with GTFO break
BTW Loop with no operation/condition that uses GTFO to exit

HAI 1.2
  VISIBLE "ENTERING INFINITE LOOP:"
  I HAS A count ITZ 0
  IM IN YR infiniteloop
    VISIBLE "  iteration " count
    count R SUM OF count AN 1
    BOTH SAEM count AN 3
    O RLY?
      YA RLY
        VISIBLE "  breaking at count = 3"
        GTFO
    OIC
  IM OUTTA YR infiniteloop

  VISIBLE "LOOP EXITED"
KTHXBYE