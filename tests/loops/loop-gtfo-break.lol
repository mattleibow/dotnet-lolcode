BTW Test GTFO breaks innermost loop only
BTW GTFO should exit only the immediately enclosing loop

HAI 1.2
  VISIBLE "TESTING GTFO IN NESTED LOOPS::"
  IM IN YR outerloop UPPIN YR i TIL BOTH SAEM i AN 4
    VISIBLE "OUTER:: i = " i
    IM IN YR innerloop UPPIN YR j TIL BOTH SAEM j AN 10
      VISIBLE "  INNER:: j = " j
      BOTH SAEM j AN 2
      O RLY?
        YA RLY
          VISIBLE "  BREAKING INNER LOOP AT j = 2"
          GTFO  BTW this should only break inner loop
      OIC
    IM OUTTA YR innerloop
    VISIBLE "  BACK IN OUTER LOOP"
  IM OUTTA YR outerloop
  
  VISIBLE "ALL LOOPS DONE"
KTHXBYE