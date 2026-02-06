BTW Test OMGWTF default case when no OMG matches
BTW OMGWTF executes when IT doesn't match any OMG literal

HAI 1.2
  BTW test default case execution
  I HAS A unknown ITZ 99
  unknown
  WTF?
    OMG 1
      VISIBLE "ONE"
      GTFO
    OMG 2
      VISIBLE "TWO"
      GTFO
    OMG 3
      VISIBLE "THREE"
      GTFO
    OMGWTF
      VISIBLE "DEFAULT CASE"
  OIC

  BTW test with matching case (default should not execute)
  I HAS A known ITZ 2
  known
  WTF?
    OMG 1
      VISIBLE "ONE"
      GTFO
    OMG 2
      VISIBLE "TWO"
      GTFO
    OMGWTF
      VISIBLE "DEFAULT NOT REACHED"
  OIC
KTHXBYE