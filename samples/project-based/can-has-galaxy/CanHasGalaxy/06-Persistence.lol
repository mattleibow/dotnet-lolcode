HAI 1.4

CAN HAS STDIO?
CAN HAS STRING?

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

BTW INRANGE validates a numeric primitive before it is allowed into game state.
HOW IZ I INRANGE YR value AN YR minimum AN YR maximum
  BOTH SAEM value AN BIGGR OF value AN minimum
  O RLY?
    YA RLY
    NO WAI
      FOUND YR FAIL
  OIC
  BOTH SAEM value AN SMALLR OF value AN maximum
  O RLY?
    YA RLY
      FOUND YR WIN
    NO WAI
      FOUND YR FAIL
  OIC
IF U SAY SO

BTW SAVE owns its STDIO handle and serializes only primitive state fields.
HOW IZ I SAVE YR game AN YR filename
  CAN HAS STDIO?
  I HAS A file ITZ I IZ STDIO'Z OPEN YR filename AN YR "w" MKAY
  I IZ STDIO'Z DIAF YR file MKAY
  O RLY?
    YA RLY
      FOUND YR FAIL
  OIC
  I HAS A record ITZ SMOOSH "CHG3:)" AN game'Z sector AN ":)" ...
    AN game'Z player'Z credits AN ":)" AN game'Z player'Z fuel AN ":)" ...
    AN game'Z player'Z hull AN ":)" AN game'Z player'Z ore AN ":)" ...
    AN game'Z player'Z relic AN ":)" AN game'Z turn AN ":)" ...
    AN game'Z seed AN ":)" AN game'Z omen AN ":)" MKAY
  I IZ STDIO'Z SCRIBBEL YR file AN YR record MKAY
  I IZ STDIO'Z CLOSE YR file MKAY
  FOUND YR WIN
IF U SAY SO

BTW LOAD owns its STDIO handle, validates every primitive, and returns a fresh state BUKKIT.
HOW IZ I LOAD YR filename
  CAN HAS STDIO?
  CAN HAS STRING?
  I HAS A file ITZ I IZ STDIO'Z OPEN YR filename AN YR "r" MKAY
  I IZ STDIO'Z DIAF YR file MKAY
  O RLY?
    YA RLY
      FOUND YR NOOB
  OIC
  I HAS A record ITZ I IZ STDIO'Z LUK YR file AN YR 512 MKAY
  I IZ STDIO'Z CLOSE YR file MKAY
  BTW Header and range validation deliberately precede state construction.
  I HAS A valid ITZ WIN
  DIFFRINT I IZ STRING'Z AT YR record AN YR 0 MKAY AN "C"
  O RLY?
    YA RLY, valid R FAIL
  OIC
  DIFFRINT I IZ STRING'Z AT YR record AN YR 1 MKAY AN "H"
  O RLY?
    YA RLY, valid R FAIL
  OIC
  DIFFRINT I IZ STRING'Z AT YR record AN YR 2 MKAY AN "G"
  O RLY?
    YA RLY, valid R FAIL
  OIC
  DIFFRINT I IZ STRING'Z AT YR record AN YR 3 MKAY AN "3"
  O RLY?
    YA RLY, valid R FAIL
  OIC
  I HAS A sector ITZ I IZ READFIELD YR record AN YR 0 MKAY
  I HAS A credits ITZ I IZ READFIELD YR record AN YR 1 MKAY
  I HAS A fuel ITZ I IZ READFIELD YR record AN YR 2 MKAY
  I HAS A hull ITZ I IZ READFIELD YR record AN YR 3 MKAY
  I HAS A ore ITZ I IZ READFIELD YR record AN YR 4 MKAY
  I HAS A relic ITZ I IZ READFIELD YR record AN YR 5 MKAY
  I HAS A turn ITZ I IZ READFIELD YR record AN YR 6 MKAY
  I HAS A seed ITZ I IZ READFIELD YR record AN YR 7 MKAY
  I HAS A omen ITZ I IZ READFIELD YR record AN YR 8 MKAY
  BOTH SAEM sector AN NOOB
  O RLY?
    YA RLY, valid R FAIL
  OIC
  BOTH SAEM credits AN NOOB
  O RLY?
    YA RLY, valid R FAIL
  OIC
  BOTH SAEM fuel AN NOOB
  O RLY?
    YA RLY, valid R FAIL
  OIC
  BOTH SAEM hull AN NOOB
  O RLY?
    YA RLY, valid R FAIL
  OIC
  BOTH SAEM ore AN NOOB
  O RLY?
    YA RLY, valid R FAIL
  OIC
  BOTH SAEM relic AN NOOB
  O RLY?
    YA RLY, valid R FAIL
  OIC
  BOTH SAEM turn AN NOOB
  O RLY?
    YA RLY, valid R FAIL
  OIC
  BOTH SAEM seed AN NOOB
  O RLY?
    YA RLY, valid R FAIL
  OIC
  BOTH SAEM omen AN NOOB
  O RLY?
    YA RLY, valid R FAIL
  OIC
  valid
  O RLY?
    YA RLY
      NOT I IZ INRANGE YR sector AN YR 0 AN YR 3 MKAY
      O RLY?
        YA RLY, valid R FAIL
      OIC
      NOT I IZ INRANGE YR credits AN YR 0 AN YR 2147483647 MKAY
      O RLY?
        YA RLY, valid R FAIL
      OIC
      NOT I IZ INRANGE YR fuel AN YR 0 AN YR 9 MKAY
      O RLY?
        YA RLY, valid R FAIL
      OIC
      NOT I IZ INRANGE YR hull AN YR 0 AN YR 9 MKAY
      O RLY?
        YA RLY, valid R FAIL
      OIC
      NOT I IZ INRANGE YR ore AN YR 0 AN YR 2147483647 MKAY
      O RLY?
        YA RLY, valid R FAIL
      OIC
      NOT I IZ INRANGE YR relic AN YR 0 AN YR 1 MKAY
      O RLY?
        YA RLY, valid R FAIL
      OIC
      NOT I IZ INRANGE YR turn AN YR 0 AN YR 2147483647 MKAY
      O RLY?
        YA RLY, valid R FAIL
      OIC
      NOT I IZ INRANGE YR seed AN YR 0 AN YR 2147483647 MKAY
      O RLY?
        YA RLY, valid R FAIL
      OIC
      NOT I IZ INRANGE YR omen AN YR 0 AN YR 2 MKAY
      O RLY?
        YA RLY, valid R FAIL
      OIC
  OIC
  valid
  O RLY?
    YA RLY
      FOUND YR I IZ CREATEFROM YR "CAPTAIN" AN YR sector AN YR credits AN YR fuel ...
        AN YR hull AN YR ore AN YR relic AN YR turn AN YR seed AN YR omen MKAY
    NO WAI
      FOUND YR NOOB
  OIC
IF U SAY SO

KTHXBYE
