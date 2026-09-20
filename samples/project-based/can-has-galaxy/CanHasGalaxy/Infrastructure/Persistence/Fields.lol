HAI 1.4

OBTW
Galaxy persistence field parsing converts one unsigned newline-delimited CHG3
field into a NUMBR. It performs no file I/O and returns NOOB for malformed
characters or missing fields.
TLDR

BTW READFIELD parses one unsigned newline-delimited numeric field from a save record.
HOW IZ I READFIELD YR record AN YR target
  CAN HAS STRING?
  I HAS A length ITZ I IZ STRING'Z LEN YR record MKAY
  I HAS A value ITZ 0
  I HAS A field ITZ -1
  IM IN YR scanner UPPIN YR position TIL BOTH SAEM position AN length
    I HAS A character ITZ I IZ STRING'Z AT YR record AN YR position MKAY
    BOTH SAEM field AN target
    O RLY?
      YA RLY
        BOTH SAEM character AN ":)"
        O RLY?
          YA RLY
            FOUND YR value
          NO WAI
            character
            WTF?
              OMG "0"
                value R PRODUKT OF value AN 10
                GTFO
              OMG "1"
                value R SUM OF PRODUKT OF value AN 10 AN 1
                GTFO
              OMG "2"
                value R SUM OF PRODUKT OF value AN 10 AN 2
                GTFO
              OMG "3"
                value R SUM OF PRODUKT OF value AN 10 AN 3
                GTFO
              OMG "4"
                value R SUM OF PRODUKT OF value AN 10 AN 4
                GTFO
              OMG "5"
                value R SUM OF PRODUKT OF value AN 10 AN 5
                GTFO
              OMG "6"
                value R SUM OF PRODUKT OF value AN 10 AN 6
                GTFO
              OMG "7"
                value R SUM OF PRODUKT OF value AN 10 AN 7
                GTFO
              OMG "8"
                value R SUM OF PRODUKT OF value AN 10 AN 8
                GTFO
              OMG "9"
                value R SUM OF PRODUKT OF value AN 10 AN 9
                GTFO
              OMGWTF
                FOUND YR NOOB
            OIC
        OIC
      NO WAI
        BOTH SAEM character AN ":)"
        O RLY?
          YA RLY
            field R SUM OF field AN 1
        OIC
    OIC
  IM OUTTA YR scanner
  FOUND YR NOOB
IF U SAY SO

KTHXBYE
