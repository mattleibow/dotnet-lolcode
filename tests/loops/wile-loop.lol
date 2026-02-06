BTW Test WILE condition loop (loop while true)
BTW WILE continues while expression evaluates to WIN

HAI 1.2
  VISIBLE "DOUBLING UNTIL >= 100:"
  I HAS A val ITZ 1
  IM IN YR wileloop UPPIN YR n WILE BOTH SAEM SMALLR OF val AN 100 AN val
    VISIBLE "  val = " val
    val R PRODUKT OF val AN 2
  IM OUTTA YR wileloop

  VISIBLE "FINAL VAL: " val
KTHXBYE