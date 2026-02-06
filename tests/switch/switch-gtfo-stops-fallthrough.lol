BTW Test GTFO stops fall-through at precise location
BTW GTFO should break out immediately, preventing further case execution

HAI 1.2
  I HAS A test ITZ 2
  test
  WTF?
    OMG 1
      VISIBLE "CASE 1"
      BTW no GTFO - would fall through
    OMG 2
      VISIBLE "CASE 2"
      BTW no GTFO - would fall through
    OMG 3
      VISIBLE "CASE 3"
      GTFO  BTW this stops the fall-through
    OMG 4
      VISIBLE "CASE 4 - SHOULD NOT EXECUTE"
    OMG 5
      VISIBLE "CASE 5 - SHOULD NOT EXECUTE"
  OIC

  VISIBLE "AFTER SWITCH"
KTHXBYE