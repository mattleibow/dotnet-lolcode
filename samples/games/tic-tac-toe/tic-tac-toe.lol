#!/usr/bin/env -S dotnet run --file
#:sdk Lolcode.NET.Sdk@0.2.0

BTW Tic-Tac-Toe - A one or two-player terminal game
BTW Demonstrates: functions, loops, switch, I/O, conditionals, simple game AI

HAI 1.2
  HOW IZ I SHOW_GUIDE
    VISIBLE ""
    VISIBLE " 1 | 2 | 3"
    VISIBLE "---+---+---"
    VISIBLE " 4 | 5 | 6"
    VISIBLE "---+---+---"
    VISIBLE " 7 | 8 | 9"
    VISIBLE ""
    FOUND YR 0
  IF U SAY SO

  HOW IZ I SHOW_BOARD YR c1 AN YR c2 AN YR c3 ...
    AN YR c4 AN YR c5 AN YR c6 ...
    AN YR c7 AN YR c8 AN YR c9
    BOTH SAEM c1 AN "1"
    O RLY?
      YA RLY, c1 R " "
    OIC
    BOTH SAEM c2 AN "2"
    O RLY?
      YA RLY, c2 R " "
    OIC
    BOTH SAEM c3 AN "3"
    O RLY?
      YA RLY, c3 R " "
    OIC
    BOTH SAEM c4 AN "4"
    O RLY?
      YA RLY, c4 R " "
    OIC
    BOTH SAEM c5 AN "5"
    O RLY?
      YA RLY, c5 R " "
    OIC
    BOTH SAEM c6 AN "6"
    O RLY?
      YA RLY, c6 R " "
    OIC
    BOTH SAEM c7 AN "7"
    O RLY?
      YA RLY, c7 R " "
    OIC
    BOTH SAEM c8 AN "8"
    O RLY?
      YA RLY, c8 R " "
    OIC
    BOTH SAEM c9 AN "9"
    O RLY?
      YA RLY, c9 R " "
    OIC
    VISIBLE ""
    VISIBLE " " c1 " | " c2 " | " c3
    VISIBLE "---+---+---"
    VISIBLE " " c4 " | " c5 " | " c6
    VISIBLE "---+---+---"
    VISIBLE " " c7 " | " c8 " | " c9
    VISIBLE ""
    FOUND YR 0
  IF U SAY SO

  HOW IZ I HAS_WON YR player ...
    AN YR c1 AN YR c2 AN YR c3 ...
    AN YR c4 AN YR c5 AN YR c6 ...
    AN YR c7 AN YR c8 AN YR c9
    BOTH OF BOTH SAEM c1 AN player AN BOTH OF BOTH SAEM c2 AN player AN BOTH SAEM c3 AN player
    O RLY?
      YA RLY, FOUND YR WIN
    OIC
    BOTH OF BOTH SAEM c4 AN player AN BOTH OF BOTH SAEM c5 AN player AN BOTH SAEM c6 AN player
    O RLY?
      YA RLY, FOUND YR WIN
    OIC
    BOTH OF BOTH SAEM c7 AN player AN BOTH OF BOTH SAEM c8 AN player AN BOTH SAEM c9 AN player
    O RLY?
      YA RLY, FOUND YR WIN
    OIC
    BOTH OF BOTH SAEM c1 AN player AN BOTH OF BOTH SAEM c4 AN player AN BOTH SAEM c7 AN player
    O RLY?
      YA RLY, FOUND YR WIN
    OIC
    BOTH OF BOTH SAEM c2 AN player AN BOTH OF BOTH SAEM c5 AN player AN BOTH SAEM c8 AN player
    O RLY?
      YA RLY, FOUND YR WIN
    OIC
    BOTH OF BOTH SAEM c3 AN player AN BOTH OF BOTH SAEM c6 AN player AN BOTH SAEM c9 AN player
    O RLY?
      YA RLY, FOUND YR WIN
    OIC
    BOTH OF BOTH SAEM c1 AN player AN BOTH OF BOTH SAEM c5 AN player AN BOTH SAEM c9 AN player
    O RLY?
      YA RLY, FOUND YR WIN
    OIC
    BOTH OF BOTH SAEM c3 AN player AN BOTH OF BOTH SAEM c5 AN player AN BOTH SAEM c7 AN player
    O RLY?
      YA RLY, FOUND YR WIN
    OIC
    FOUND YR FAIL
  IF U SAY SO

  HOW IZ I CELL_OPEN YR position ...
    AN YR c1 AN YR c2 AN YR c3 ...
    AN YR c4 AN YR c5 AN YR c6 ...
    AN YR c7 AN YR c8 AN YR c9
    position
    WTF?
      OMG 1, FOUND YR BOTH SAEM c1 AN "1"
      OMG 2, FOUND YR BOTH SAEM c2 AN "2"
      OMG 3, FOUND YR BOTH SAEM c3 AN "3"
      OMG 4, FOUND YR BOTH SAEM c4 AN "4"
      OMG 5, FOUND YR BOTH SAEM c5 AN "5"
      OMG 6, FOUND YR BOTH SAEM c6 AN "6"
      OMG 7, FOUND YR BOTH SAEM c7 AN "7"
      OMG 8, FOUND YR BOTH SAEM c8 AN "8"
      OMG 9, FOUND YR BOTH SAEM c9 AN "9"
    OIC
    FOUND YR FAIL
  IF U SAY SO

  HOW IZ I WOULD_WIN YR position AN YR mark ...
    AN YR c1 AN YR c2 AN YR c3 ...
    AN YR c4 AN YR c5 AN YR c6 ...
    AN YR c7 AN YR c8 AN YR c9
    NOT I IZ CELL_OPEN YR position ...
      AN YR c1 AN YR c2 AN YR c3 ...
      AN YR c4 AN YR c5 AN YR c6 ...
      AN YR c7 AN YR c8 AN YR c9 MKAY
    O RLY?
      YA RLY, FOUND YR FAIL
    OIC

    position
    WTF?
      OMG 1
        c1 R mark
        GTFO
      OMG 2
        c2 R mark
        GTFO
      OMG 3
        c3 R mark
        GTFO
      OMG 4
        c4 R mark
        GTFO
      OMG 5
        c5 R mark
        GTFO
      OMG 6
        c6 R mark
        GTFO
      OMG 7
        c7 R mark
        GTFO
      OMG 8
        c8 R mark
        GTFO
      OMG 9
        c9 R mark
        GTFO
    OIC

    FOUND YR I IZ HAS_WON YR mark ...
      AN YR c1 AN YR c2 AN YR c3 ...
      AN YR c4 AN YR c5 AN YR c6 ...
      AN YR c7 AN YR c8 AN YR c9 MKAY
  IF U SAY SO

  HOW IZ I PICK_AI_MOVE YR c1 AN YR c2 AN YR c3 ...
    AN YR c4 AN YR c5 AN YR c6 ...
    AN YR c7 AN YR c8 AN YR c9
    BTW Win immediately when possible.
    IM IN YR winning UPPIN YR candidate TIL BOTH SAEM candidate AN 9
      I HAS A winning_position ITZ SUM OF candidate AN 1
      I IZ WOULD_WIN YR winning_position AN YR "O" ...
        AN YR c1 AN YR c2 AN YR c3 ...
        AN YR c4 AN YR c5 AN YR c6 ...
        AN YR c7 AN YR c8 AN YR c9 MKAY
      O RLY?
        YA RLY, FOUND YR winning_position
      OIC
    IM OUTTA YR winning

    BTW Block the player's immediate winning move.
    IM IN YR blocking UPPIN YR candidate TIL BOTH SAEM candidate AN 9
      I HAS A blocking_position ITZ SUM OF candidate AN 1
      I IZ WOULD_WIN YR blocking_position AN YR "X" ...
        AN YR c1 AN YR c2 AN YR c3 ...
        AN YR c4 AN YR c5 AN YR c6 ...
        AN YR c7 AN YR c8 AN YR c9 MKAY
      O RLY?
        YA RLY, FOUND YR blocking_position
      OIC
    IM OUTTA YR blocking

    BTW Prefer the center, then corners, then the first open edge.
    I IZ CELL_OPEN YR 5 ...
      AN YR c1 AN YR c2 AN YR c3 ...
      AN YR c4 AN YR c5 AN YR c6 ...
      AN YR c7 AN YR c8 AN YR c9 MKAY
    O RLY?
      YA RLY, FOUND YR 5
    OIC

    I IZ CELL_OPEN YR 1 ...
      AN YR c1 AN YR c2 AN YR c3 ...
      AN YR c4 AN YR c5 AN YR c6 ...
      AN YR c7 AN YR c8 AN YR c9 MKAY
    O RLY?
      YA RLY, FOUND YR 1
    OIC
    I IZ CELL_OPEN YR 3 ...
      AN YR c1 AN YR c2 AN YR c3 ...
      AN YR c4 AN YR c5 AN YR c6 ...
      AN YR c7 AN YR c8 AN YR c9 MKAY
    O RLY?
      YA RLY, FOUND YR 3
    OIC
    I IZ CELL_OPEN YR 7 ...
      AN YR c1 AN YR c2 AN YR c3 ...
      AN YR c4 AN YR c5 AN YR c6 ...
      AN YR c7 AN YR c8 AN YR c9 MKAY
    O RLY?
      YA RLY, FOUND YR 7
    OIC
    I IZ CELL_OPEN YR 9 ...
      AN YR c1 AN YR c2 AN YR c3 ...
      AN YR c4 AN YR c5 AN YR c6 ...
      AN YR c7 AN YR c8 AN YR c9 MKAY
    O RLY?
      YA RLY, FOUND YR 9
    OIC

    IM IN YR fallback UPPIN YR candidate TIL BOTH SAEM candidate AN 9
      I HAS A fallback_position ITZ SUM OF candidate AN 1
      I IZ CELL_OPEN YR fallback_position ...
        AN YR c1 AN YR c2 AN YR c3 ...
        AN YR c4 AN YR c5 AN YR c6 ...
        AN YR c7 AN YR c8 AN YR c9 MKAY
      O RLY?
        YA RLY, FOUND YR fallback_position
      OIC
    IM OUTTA YR fallback
    FOUND YR 0
  IF U SAY SO

  I HAS A c1 ITZ "1"
  I HAS A c2 ITZ "2"
  I HAS A c3 ITZ "3"
  I HAS A c4 ITZ "4"
  I HAS A c5 ITZ "5"
  I HAS A c6 ITZ "6"
  I HAS A c7 ITZ "7"
  I HAS A c8 ITZ "8"
  I HAS A c9 ITZ "9"
  I HAS A player ITZ "X"
  I HAS A players ITZ 0
  I HAS A moves ITZ 0
  I HAS A game_over ITZ FAIL
  I HAS A result ITZ "QUIT"
  VISIBLE "=== LOLCODE TIC-TAC-TOE ==="

  IM IN YR setup UPPIN YR attempt TIL DIFFRINT players AN 0
    VISIBLE "HOW MANY PLAYERZ? PICK 1 OR 2:: "!
    I HAS A mode
    GIMMEH mode
    mode
    WTF?
      OMG "1"
        players R 1
        GTFO
      OMG "2"
        players R 2
        GTFO
      OMGWTF
        VISIBLE "NOPE! PICK 1 OR 2."
    OIC
  IM OUTTA YR setup

  BOTH SAEM players AN 1
  O RLY?
    YA RLY, VISIBLE "U R X. TEH AI IZ O."
    NO WAI, VISIBLE "PLAYERZ TAKE TURNZ AS X AN O."
  OIC
  VISIBLE "PICK A CELL FROM 1 TO 9, OR TYPE q 2 QUIT"
  I IZ SHOW_GUIDE MKAY

  IM IN YR game UPPIN YR turn TIL game_over
    I IZ SHOW_BOARD YR c1 AN YR c2 AN YR c3 ...
      AN YR c4 AN YR c5 AN YR c6 ...
      AN YR c7 AN YR c8 AN YR c9 MKAY

    VISIBLE "PLAYER " player ", PICK UR CELL:: "!
    I HAS A choice
    GIMMEH choice
    I HAS A valid_move ITZ FAIL

    choice
    WTF?
      OMG "1"
        BOTH SAEM c1 AN "1"
        O RLY?
          YA RLY
            c1 R player
            valid_move R WIN
        OIC
        GTFO
      OMG "2"
        BOTH SAEM c2 AN "2"
        O RLY?
          YA RLY
            c2 R player
            valid_move R WIN
        OIC
        GTFO
      OMG "3"
        BOTH SAEM c3 AN "3"
        O RLY?
          YA RLY
            c3 R player
            valid_move R WIN
        OIC
        GTFO
      OMG "4"
        BOTH SAEM c4 AN "4"
        O RLY?
          YA RLY
            c4 R player
            valid_move R WIN
        OIC
        GTFO
      OMG "5"
        BOTH SAEM c5 AN "5"
        O RLY?
          YA RLY
            c5 R player
            valid_move R WIN
        OIC
        GTFO
      OMG "6"
        BOTH SAEM c6 AN "6"
        O RLY?
          YA RLY
            c6 R player
            valid_move R WIN
        OIC
        GTFO
      OMG "7"
        BOTH SAEM c7 AN "7"
        O RLY?
          YA RLY
            c7 R player
            valid_move R WIN
        OIC
        GTFO
      OMG "8"
        BOTH SAEM c8 AN "8"
        O RLY?
          YA RLY
            c8 R player
            valid_move R WIN
        OIC
        GTFO
      OMG "9"
        BOTH SAEM c9 AN "9"
        O RLY?
          YA RLY
            c9 R player
            valid_move R WIN
        OIC
        GTFO
      OMG "q"
        game_over R WIN
        GTFO
      OMG "quit"
        game_over R WIN
        GTFO
      OMGWTF
        BTW Invalid input is handled below.
    OIC

    game_over
    O RLY?
      YA RLY, GTFO
    OIC

    valid_move
    O RLY?
      YA RLY
        moves R SUM OF moves AN 1
        I IZ HAS_WON YR player ...
          AN YR c1 AN YR c2 AN YR c3 ...
          AN YR c4 AN YR c5 AN YR c6 ...
          AN YR c7 AN YR c8 AN YR c9 MKAY
        O RLY?
          YA RLY
            result R player
            game_over R WIN
          MEBBE BOTH SAEM moves AN 9
            result R "DRAW"
            game_over R WIN
          NO WAI
            BOTH SAEM players AN 1
            O RLY?
              YA RLY
                I HAS A ai_move ITZ I IZ PICK_AI_MOVE ...
                  YR c1 AN YR c2 AN YR c3 ...
                  AN YR c4 AN YR c5 AN YR c6 ...
                  AN YR c7 AN YR c8 AN YR c9 MKAY
                ai_move
                WTF?
                  OMG 1
                    c1 R "O"
                    GTFO
                  OMG 2
                    c2 R "O"
                    GTFO
                  OMG 3
                    c3 R "O"
                    GTFO
                  OMG 4
                    c4 R "O"
                    GTFO
                  OMG 5
                    c5 R "O"
                    GTFO
                  OMG 6
                    c6 R "O"
                    GTFO
                  OMG 7
                    c7 R "O"
                    GTFO
                  OMG 8
                    c8 R "O"
                    GTFO
                  OMG 9
                    c9 R "O"
                    GTFO
                OIC
                moves R SUM OF moves AN 1
                VISIBLE "TEH AI PICKZ CELL " ai_move "!"
                I IZ HAS_WON YR "O" ...
                  AN YR c1 AN YR c2 AN YR c3 ...
                  AN YR c4 AN YR c5 AN YR c6 ...
                  AN YR c7 AN YR c8 AN YR c9 MKAY
                O RLY?
                  YA RLY
                    result R "O"
                    game_over R WIN
                  MEBBE BOTH SAEM moves AN 9
                    result R "DRAW"
                    game_over R WIN
                OIC
              NO WAI
                BOTH SAEM player AN "X"
                O RLY?
                  YA RLY, player R "O"
                  NO WAI, player R "X"
                OIC
            OIC
        OIC
      NO WAI
        VISIBLE "NOPE! PICK AN EMPTY CELL FROM 1 TO 9."
    OIC
  IM OUTTA YR game

  I IZ SHOW_BOARD YR c1 AN YR c2 AN YR c3 ...
    AN YR c4 AN YR c5 AN YR c6 ...
    AN YR c7 AN YR c8 AN YR c9 MKAY

  result
  WTF?
    OMG "X"
      VISIBLE "PLAYER X WINZ!"
      GTFO
    OMG "O"
      BOTH SAEM players AN 1
      O RLY?
        YA RLY, VISIBLE "TEH AI WINZ!"
        NO WAI, VISIBLE "PLAYER O WINZ!"
      OIC
      GTFO
    OMG "DRAW"
      VISIBLE "IZ A DRAW!"
      GTFO
    OMGWTF
      VISIBLE "KTHXBAI! TANKS 4 PLAYIN!"
  OIC
KTHXBYE
