BTW Test GTFO in nested contexts (switch inside loop)
BTW GTFO should break innermost context: switch, not the loop

HAI 1.2
  VISIBLE "NESTED SWITCH INSIDE LOOP::"
  IM IN YR outerloop UPPIN YR i TIL BOTH SAEM i AN 3
    VISIBLE "LOOP iteration " i
    I HAS A switch_val ITZ 2
    switch_val
    WTF?
      OMG 1
        VISIBLE "  switch case 1"
        GTFO  BTW breaks switch only
      OMG 2
        VISIBLE "  switch case 2"
        GTFO  BTW breaks switch only, loop should continue
      OMG 3
        VISIBLE "  switch case 3"
    OIC
    VISIBLE "  back in loop after switch"
  IM OUTTA YR outerloop
  
  VISIBLE "ALL DONE"
KTHXBYE