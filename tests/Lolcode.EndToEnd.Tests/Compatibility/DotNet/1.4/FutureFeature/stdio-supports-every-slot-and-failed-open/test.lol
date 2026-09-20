HAI 1.4
CAN HAS STDIO?
I HAS A file ITZ I IZ STDIO'Z OPEN YR "library.dat" AN YR "w+" MKAY
I IZ STDIO'Z DIAF YR file MKAY
O RLY?
  YA RLY
    VISIBLE "unexpected open failure"
  NO WAI
    VISIBLE "opened"
OIC
I IZ STDIO'Z SCRIBBEL YR file AN YR "HAI" MKAY
I IZ STDIO'Z AGEIN YR file MKAY
VISIBLE I IZ STDIO'Z LUK YR file AN YR 3 MKAY
I IZ STDIO'Z CLOSE YR file MKAY
I HAS A missing ITZ I IZ STDIO'Z OPEN YR "missing/path" AN YR "r" MKAY
I IZ STDIO'Z DIAF YR missing MKAY
O RLY?
  YA RLY
    VISIBLE "failed safely"
  NO WAI
    VISIBLE "unexpected success"
OIC
KTHXBYE
