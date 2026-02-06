BTW Test GTFO breaks out of loop context
BTW GTFO in loop should exit the loop, not anything else

HAI 1.2
  VISIBLE "TESTING GTFO IN LOOP:"
  I HAS A counter ITZ 0
  IM IN YR testloop UPPIN YR i TIL BOTH SAEM i AN 10
    VISIBLE "  iteration " i
    counter R SUM OF counter AN 1
    BOTH SAEM counter AN 3
    O RLY?
      YA RLY
        VISIBLE "  breaking at counter = 3"
        GTFO  BTW should break the loop
    OIC
    VISIBLE "  end of iteration " i
  IM OUTTA YR testloop
  
  VISIBLE "AFTER LOOP"
KTHXBYE