BTW Test nested O RLY? conditionals
BTW Verify inner conditionals work correctly within outer ones

HAI 1.2
  I HAS A x ITZ 5
  I HAS A y ITZ 10

  BOTH SAEM BIGGR OF x AN 3 AN x
  O RLY?
    YA RLY
      VISIBLE "OUTER:: X >= 3"
      BOTH SAEM SMALLR OF y AN 15 AN y
      O RLY?
        YA RLY
          VISIBLE "  INNER:: Y <= 15"
          BOTH SAEM x AN 5
          O RLY?
            YA RLY
              VISIBLE "    INNERMOST:: X == 5"
            NO WAI
              VISIBLE "    INNERMOST:: X != 5"
          OIC
        NO WAI
          VISIBLE "  INNER:: Y > 15"
      OIC
    NO WAI
      VISIBLE "OUTER:: X < 3"
  OIC
KTHXBYE