#!/usr/bin/env -S dotnet run --file
#:sdk Lolcode.NET.Sdk@0.2.0

BTW Stack - Reusable LIFO storage with BUKKIT and SRS
BTW Inspired by https://esolangs.org/wiki/LOLCODE

HAI 1.3
  O HAI IM stackBlueprint
    I HAS A top ITZ 0
    I HAS A highest ITZ 0

    HOW IZ I push YR value
      BOTH SAEM ME'Z top AN ME'Z highest
      O RLY?
        YA RLY
          ME HAS A SRS ME'Z top ITZ value
          ME'Z highest R SUM OF ME'Z highest AN 1
        NO WAI
          ME'Z SRS ME'Z top R value
      OIC
      ME'Z top R SUM OF ME'Z top AN 1
    IF U SAY SO

    HOW IZ I pop
      BOTH SAEM ME'Z top AN 0
      O RLY?
        YA RLY
          FOUND YR "STACK EMPTY"
        NO WAI
          ME'Z top R DIFF OF ME'Z top AN 1
          I HAS A value ITZ ME'Z SRS ME'Z top
          ME'Z SRS ME'Z top R NOOB
          FOUND YR value
      OIC
    IF U SAY SO
  KTHX

  I HAS A stack ITZ LIEK A stackBlueprint
  stack IZ push YR "first" MKAY
  stack IZ push YR "second" MKAY
  VISIBLE stack IZ pop MKAY
  stack IZ push YR "third" MKAY
  VISIBLE stack IZ pop MKAY
  VISIBLE stack IZ pop MKAY
  VISIBLE stack IZ pop MKAY
KTHXBYE
