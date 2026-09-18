#!/usr/bin/env -S dotnet run --file
#:sdk Lolcode.NET.Sdk@0.2.0

BTW Variables - Declaration, initialization, and assignment
BTW Demonstrates: I HAS A, ITZ, R, types (NUMBR, NUMBAR, YARN, TROOF)

HAI 1.2
  BTW declare with initialization
  I HAS A name ITZ "LOLCATZ"
  I HAS A age ITZ 9
  I HAS A weight ITZ 4.2
  I HAS A happy ITZ WIN

  BTW declare without initialization (NOOB)
  I HAS A mystery

  BTW print all variables
  VISIBLE "NAME:: " name
  VISIBLE "AGE:: " age
  VISIBLE "WEIGHT:: " weight                BTW prints 4.20 (NUMBAR → YARN = 2 decimal places)
  BTW TROOF and NOOB do not implicitly cast to YARN, so label them explicitly
  happy
  O RLY?
    YA RLY
      VISIBLE "HAPPY:: WIN"
    NO WAI
      VISIBLE "HAPPY:: FAIL"
  OIC
  BOTH SAEM mystery AN NOOB
  O RLY?
    YA RLY
      VISIBLE "MYSTERY:: NOOB"
    NO WAI
      VISIBLE "MYSTERY:: NOT NOOB"
  OIC

  BTW reassignment
  age R 10
  name R "CEILING CAT"
  VISIBLE "HAPPY BURFDAY! " name " IZ NAO " age
KTHXBYE
