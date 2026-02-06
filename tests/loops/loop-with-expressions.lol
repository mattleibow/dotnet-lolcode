BTW Test complex expressions in TIL and WILE conditions
BTW Loop conditions can use arithmetic and comparison expressions

HAI 1.2
  BTW complex TIL condition with math
  VISIBLE "COMPLEX TIL CONDITION:"
  I HAS A limit ITZ 10
  IM IN YR complexloop UPPIN YR x TIL BOTH SAEM BIGGR OF x AN limit AN x
    VISIBLE "  x = " x ", limit = " limit
  IM OUTTA YR complexloop

  BTW complex WILE condition with multiple comparisons
  VISIBLE "COMPLEX WILE CONDITION:"
  I HAS A a ITZ 1
  I HAS A b ITZ 20
  IM IN YR wilecomplex UPPIN YR y WILE BOTH SAEM SMALLR OF a AN b AN a
    VISIBLE "  y = " y ", a = " a ", b = " b
    a R PRODUKT OF a AN 2
  IM OUTTA YR wilecomplex
  
  VISIBLE "DONE"
KTHXBYE