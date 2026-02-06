BTW Text Adventure Game - A simple room-based adventure
BTW Demonstrates: functions, loops, switch, I/O, string ops, state management

HAI 1.2
  BTW game state
  I HAS A room ITZ 1
  I HAS A has_key ITZ FAIL
  I HAS A has_sword ITZ FAIL
  I HAS A game_over ITZ FAIL

  BTW describe the current room
  HOW IZ I describe_room YR r
    r
    WTF?
      OMG 1
        VISIBLE "U R IN A DARK ROOM. THERE IZ A DOOR 2 TEH NORTH."
        VISIBLE "U C A SHINY KEY ON TEH FLOOR."
        GTFO
      OMG 2
        VISIBLE "U R IN A LONG HALLWAY. DOORZ NORTH AN SOUTH."
        VISIBLE "A RUSTY SWORD HANGZ ON TEH WALL."
        GTFO
      OMG 3
        VISIBLE "U R IN A BIG ROOM WIF A SCARY DRAGON!"
        has_sword
        O RLY?
          YA RLY, VISIBLE "U HAZ A SWORD. MEBBE U CAN FITE?"
          NO WAI, VISIBLE "U HAZ NO WEAPON... DIS IZ BAD."
        OIC
        GTFO
      OMG 4
        VISIBLE "U FOUND TEH TREASURE ROOM! GOLD EVRYWARE!"
        GTFO
      OMGWTF
        VISIBLE "U R IN A VOID. HOW U GET HERE?"
    OIC
  IF U SAY SO

  BTW process player action
  HOW IZ I do_action YR action
    BTW room 1 actions
    BOTH SAEM room AN 1
    O RLY?
      YA RLY
        BOTH SAEM action AN "north"
        O RLY?
          YA RLY
            room R 2
            FOUND YR WIN
        OIC
        BOTH SAEM action AN "take key"
        O RLY?
          YA RLY
            has_key
            O RLY?
              YA RLY, VISIBLE "U ALREADY HAZ TEH KEY"
              NO WAI
                has_key R WIN
                VISIBLE "U PICKED UP TEH KEY!"
            OIC
            FOUND YR WIN
        OIC
    OIC

    BTW room 2 actions
    BOTH SAEM room AN 2
    O RLY?
      YA RLY
        BOTH SAEM action AN "south"
        O RLY?
          YA RLY
            room R 1
            FOUND YR WIN
        OIC
        BOTH SAEM action AN "north"
        O RLY?
          YA RLY
            has_key
            O RLY?
              YA RLY
                VISIBLE "U USE TEH KEY 2 OPEN TEH DOOR..."
                room R 3
                FOUND YR WIN
              NO WAI
                VISIBLE "TEH DOOR IZ LOCKED. U NEED A KEY."
                FOUND YR WIN
            OIC
        OIC
        BOTH SAEM action AN "take sword"
        O RLY?
          YA RLY
            has_sword
            O RLY?
              YA RLY, VISIBLE "U ALREADY HAZ TEH SWORD"
              NO WAI
                has_sword R WIN
                VISIBLE "U TOOK TEH RUSTY SWORD! IZ DANGEROUS."
            OIC
            FOUND YR WIN
        OIC
    OIC

    BTW room 3 actions (dragon room)
    BOTH SAEM room AN 3
    O RLY?
      YA RLY
        BOTH SAEM action AN "fight"
        O RLY?
          YA RLY
            has_sword
            O RLY?
              YA RLY
                VISIBLE "U SWING TEH SWORD AN DEFEAT TEH DRAGON!"
                VISIBLE "TEH WAI 2 TEH TREASURE IZ OPEN!"
                room R 4
                FOUND YR WIN
              NO WAI
                VISIBLE "U TRY 2 PUNCH TEH DRAGON..."
                VISIBLE "TEH DRAGON EATZ U. GAME OVER."
                game_over R WIN
                FOUND YR WIN
            OIC
        OIC
        BOTH SAEM action AN "south"
        O RLY?
          YA RLY
            VISIBLE "U RUN AWAY FROM TEH DRAGON!"
            room R 2
            FOUND YR WIN
        OIC
    OIC

    BTW room 4 (treasure - you win!)
    BOTH SAEM room AN 4
    O RLY?
      YA RLY
        VISIBLE "U ALREADY WON! TEH TREASURE IZ YOURZ!"
        game_over R WIN
        FOUND YR WIN
    OIC

    VISIBLE "U CANT DO DAT HERE."
    FOUND YR FAIL
  IF U SAY SO

  BTW main game loop
  VISIBLE "=== LOLCODE ADVENTURE ==="
  VISIBLE "COMMANDZ: north, south, take key, take sword, fight, quit"
  VISIBLE ""

  IM IN YR gameloop UPPIN YR turn TIL BOTH SAEM turn AN 1000
    I IZ describe_room YR room MKAY
    VISIBLE ""

    game_over
    O RLY?
      YA RLY, GTFO
    OIC

    VISIBLE "WAT DO? "!
    I HAS A cmd
    GIMMEH cmd

    BOTH SAEM cmd AN "quit"
    O RLY?
      YA RLY
        VISIBLE "KTHXBAI! TANKS 4 PLAYIN!"
        GTFO
    OIC

    VISIBLE ""
    I IZ do_action YR cmd MKAY
    VISIBLE ""
  IM OUTTA YR gameloop
KTHXBYE
