BTW Test nested loops with different labels
BTW Verify inner and outer loops maintain separate counters

HAI 1.2
  VISIBLE "NESTED LOOP TEST::"
  IM IN YR outerloop UPPIN YR i TIL BOTH SAEM i AN 3
    VISIBLE "OUTER i = " i
    IM IN YR innerloop UPPIN YR j TIL BOTH SAEM j AN 2
      VISIBLE "  INNER j = " j
    IM OUTTA YR innerloop
  IM OUTTA YR outerloop

  VISIBLE "DONE"
KTHXBYE