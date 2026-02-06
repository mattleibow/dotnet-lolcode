BTW Test GTFO breaks out of switch case context
BTW GTFO in switch should break case execution, not anything else

HAI 1.2
  VISIBLE "TESTING GTFO IN SWITCH::"
  I HAS A test_val ITZ 2
  test_val
  WTF?
    OMG 1
      VISIBLE "  case 1"
    OMG 2
      VISIBLE "  case 2 - before GTFO"
      GTFO  BTW should break switch case execution
      VISIBLE "  case 2 - after GTFO (should not execute)"
    OMG 3
      VISIBLE "  case 3 (should not execute due to GTFO)"
  OIC
  
  VISIBLE "AFTER SWITCH"
KTHXBYE