HAI 1.4
CAN HAS STRING?
CAN HAS STDIO?
I HAS A selected ITZ I IZ STRING'Z AT YR "é" AN YR 0 MKAY
I HAS A selected2 ITZ I IZ STRING'Z AT YR "é" AN YR 1 MKAY
I HAS A reassembled ITZ SMOOSH selected AN selected2 MKAY
VISIBLE I IZ STRING'Z LEN YR reassembled MKAY
BOTH SAEM reassembled AN "é"
O RLY?
  YA RLY
    VISIBLE "same"
OIC
BOTH SAEM selected AN "Ã"
O RLY?
  YA RLY
    VISIBLE "unexpected"
  NO WAI
    VISIBLE "different"
OIC
I HAS A explicitlyYarn ITZ MAEK selected A YARN
BOTH SAEM explicitlyYarn AN selected
O RLY?
  YA RLY
    VISIBLE "cast"
OIC
reassembled
WTF?
  OMG "é"
    VISIBLE "switch"
    GTFO
  OMGWTF
    VISIBLE "unexpected switch"
OIC
VISIBLE reassembled
VISIBLE ":{selected}:{selected2}"
I HAS A file ITZ I IZ STDIO'Z OPEN YR "selected.dat" AN YR "w" MKAY
I IZ STDIO'Z SCRIBBEL YR file AN YR selected MKAY
I IZ STDIO'Z CLOSE YR file MKAY
KTHXBYE
