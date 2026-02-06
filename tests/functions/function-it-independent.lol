HAI 1.2
  BTW Test that each function has its own IT, independent of caller's IT

  HOW IZ I modifyIT
    SUM OF 50 AN 50
    BTW Above sets IT to 100 inside function
    VISIBLE "Function IT: " IT
  IF U SAY SO

  SUM OF 10 AN 10
  BTW IT is now 20 in main scope
  VISIBLE "Before call: " IT

  I IZ modifyIT MKAY

  VISIBLE "After call: " IT
  BTW IT in main should still be 20
KTHXBYE
