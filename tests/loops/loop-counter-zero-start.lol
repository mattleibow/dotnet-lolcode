BTW Test UPPIN loop counter starts at 0 by default
BTW Loop variable for UPPIN should start at 0, not 1

HAI 1.2
  VISIBLE "UPPIN COUNTER STARTING VALUE:"
  IM IN YR starttest UPPIN YR counter TIL BOTH SAEM counter AN 3
    VISIBLE "  counter = " counter
  IM OUTTA YR starttest
  
  VISIBLE "FIRST VALUE WAS 0, NOT 1"
KTHXBYE