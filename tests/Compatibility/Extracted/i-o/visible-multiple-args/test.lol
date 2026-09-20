BTW Test VISIBLE with multiple arguments and auto-concatenation
BTW Per spec: VISIBLE concatenates all arguments after casting to YARN

HAI 1.2
  I HAS A x ITZ 10
  I HAS A y ITZ 20

  VISIBLE "X=" x ", Y=" y
  VISIBLE "SUM=" SUM OF x AN y "!"
  VISIBLE "MIXED " x " AND " "STRING"
KTHXBYE
