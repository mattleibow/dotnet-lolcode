BTW Test switch fallthrough when OMG case has no GTFO
BTW Without GTFO, execution continues to next OMG case

HAI 1.2
  I HAS A val ITZ 2
  val
  WTF?
    OMG 1
      VISIBLE "CASE 1"
    OMG 2
      VISIBLE "CASE 2"
      BTW no GTFO here - should fall through
    OMG 3
      VISIBLE "CASE 3"
      GTFO
    OMG 4
      VISIBLE "CASE 4"
  OIC

  VISIBLE "AFTER SWITCH"
KTHXBYE