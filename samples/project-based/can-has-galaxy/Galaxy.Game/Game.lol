HAI 1.4

CAN HAS GalaxyEngine?
CAN HAS GalaxyPersistence?

I HAS A game ITZ I IZ GalaxyEngine'Z CREATEGAME YR "CAPTAIN" AN YR 7 MKAY
I HAS A running ITZ WIN
VISIBLE "=== CAN HAS GALAXY? ==="
VISIBLE "TYPE:: STATUS MAP TRAVEL1 TRAVEL2 TRAVEL3 MINE SELL FUEL FIGHT MISSION SAVE LOAD QUIT"

IM IN YR commandLoop UPPIN YR tick WILE BOTH SAEM running AN WIN
  VISIBLE "> "!
  I HAS A command
  GIMMEH command
  BOTH SAEM command AN ""
  O RLY?
    YA RLY
      VISIBLE "EOF. SAFE LANDIN."
      running R FAIL
    NO WAI
      command
      WTF?
        OMG "STATUS"
          VISIBLE I IZ GalaxyEngine'Z STATUS YR game MKAY
          GTFO
        OMG "MAP"
          VISIBLE I IZ GalaxyEngine'Z MAP YR game MKAY
          GTFO
        OMG "TRAVEL1"
          VISIBLE I IZ GalaxyEngine'Z TRAVEL YR game AN YR 1 MKAY
          GTFO
        OMG "TRAVEL2"
          VISIBLE I IZ GalaxyEngine'Z TRAVEL YR game AN YR 2 MKAY
          GTFO
        OMG "TRAVEL3"
          VISIBLE I IZ GalaxyEngine'Z TRAVEL YR game AN YR 3 MKAY
          GTFO
        OMG "MINE"
          VISIBLE I IZ GalaxyEngine'Z MINE YR game MKAY
          GTFO
        OMG "SELL"
          VISIBLE I IZ GalaxyEngine'Z SELLORE YR game MKAY
          GTFO
        OMG "FUEL"
          VISIBLE I IZ GalaxyEngine'Z BUYFUEL YR game MKAY
          GTFO
        OMG "FIGHT"
          VISIBLE I IZ GalaxyEngine'Z FIGHT YR game MKAY
          GTFO
        OMG "MISSION"
          VISIBLE I IZ GalaxyEngine'Z MISSIONTEXT YR game MKAY
          GTFO
        OMG "SAVE"
          I IZ GalaxyPersistence'Z SAVE YR game AN YR "can-has-galaxy.save" MKAY
          O RLY?
            YA RLY
              VISIBLE "SAVE OK:: can-has-galaxy.save"
            NO WAI
              VISIBLE "SAVE FAIL."
          OIC
          GTFO
        OMG "LOAD"
          I HAS A loaded ITZ I IZ GalaxyPersistence'Z LOAD YR "can-has-galaxy.save" MKAY
          BOTH SAEM loaded AN NOOB
          O RLY?
            YA RLY
              VISIBLE "NO VALID SAVE."
            NO WAI
              game R loaded
              VISIBLE "LOAD OK."
          OIC
          GTFO
        OMG "QUIT"
          VISIBLE "KTHXBAI, CAPTAIN."
          running R FAIL
          GTFO
        OMGWTF
          VISIBLE "COMMAND NOT IN STAR CHART."
      OIC
  OIC
IM OUTTA YR commandLoop

KTHXBYE
