BTW Test loop variable is local to loop, doesn't affect outer scope
BTW Loop variable should not interfere with variables of same name outside

HAI 1.2
  I HAS A i ITZ 99
  VISIBLE "BEFORE LOOP:: i = " i

  IM IN YR testloop UPPIN YR i TIL BOTH SAEM i AN 3
    VISIBLE "  IN LOOP:: i = " i
  IM OUTTA YR testloop

  VISIBLE "AFTER LOOP:: i = " i
  
  BTW test with different variable name
  I HAS A counter ITZ 42
  VISIBLE "BEFORE SECOND LOOP:: counter = " counter
  
  IM IN YR testloop2 UPPIN YR counter TIL BOTH SAEM counter AN 2
    VISIBLE "  IN LOOP2:: counter = " counter
  IM OUTTA YR testloop2
  
  VISIBLE "AFTER SECOND LOOP:: counter = " counter
KTHXBYE