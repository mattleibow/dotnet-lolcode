#!/usr/bin/env -S dotnet run --file
#:sdk Lolcode.NET.Sdk@0.2.0

BTW File I/O - Read a file with the lci/future STDIO binding
BTW Modern adaptation of Example 2 at https://en.wikipedia.org/wiki/LOLCODE

HAI 1.4
  CAN HAS STDIO?

  I HAS A file ITZ I IZ STDIO'Z OPEN YR "LOLCATS.TXT" AN YR "r" MKAY
  I IZ STDIO'Z DIAF YR file MKAY
  O RLY?
    YA RLY
      INVISIBLE "COULD NOT OPEN LOLCATS.TXT"
    NO WAI
      VISIBLE I IZ STDIO'Z LUK YR file AN YR 4096 MKAY!
      I IZ STDIO'Z CLOSE YR file MKAY
  OIC
KTHXBYE
