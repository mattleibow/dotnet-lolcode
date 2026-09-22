#!/usr/bin/env -S dotnet run --file
#:sdk Lolcode.NET.Sdk@0.2.0

BTW FizzBuzz - The classic programming challenge in LOLCODE
BTW Demonstrates: loops, conditionals, modulo, string ops, combined logic

HAI 1.2
  IM IN YR fizzbuzz UPPIN YR i TIL BOTH SAEM i AN 100
    I HAS A number ITZ SUM OF i AN 1
    I HAS A out ITZ ""

    BTW check divisible by 3
    BOTH SAEM MOD OF number AN 3 AN 0
    O RLY?
      YA RLY, out R "Fizz"
    OIC

    BTW check divisible by 5
    BOTH SAEM MOD OF number AN 5 AN 0
    O RLY?
      YA RLY, out R SMOOSH out AN "Buzz" MKAY
    OIC

    BTW if neither, print the number
    BOTH SAEM out AN ""
    O RLY?
      YA RLY, VISIBLE number
      NO WAI, VISIBLE out
    OIC
  IM OUTTA YR fizzbuzz
KTHXBYE
