#!/usr/bin/env -S dotnet run --file
#:sdk Lolcode.NET.Sdk@0.2.0

BTW Inspired by the engine structure in LucasLi1337Unknown/LolCodeChessAI:
BTW https://github.com/LucasLi1337Unknown/LolCodeChessAI
BTW Rewritten for Lolcode.NET.Sdk with compact base-13 rank encoding.

BTW ============================================================
BTW LOLCHESS - terminal chess written for Lolcode.NET.Sdk
BTW
BTW Board squares use 0=a8 through 63=h1.
BTW White pieces are uppercase; black pieces are lowercase.
BTW Castling and en passant are intentionally not implemented.
BTW ============================================================

HAI 1.2

  BTW Each rank is one base-13 NUMBR. A digit stores one square:
  BTW 0 empty, 1-6 white P/N/B/R/Q/K, 7-12 black P/N/B/R/Q/K.
  HOW IZ I WEIGHT YR column
    column
    WTF?
      OMG 0
        FOUND YR 1
      OMG 1
        FOUND YR 13
      OMG 2
        FOUND YR 169
      OMG 3
        FOUND YR 2197
      OMG 4
        FOUND YR 28561
      OMG 5
        FOUND YR 371293
      OMG 6
        FOUND YR 4826809
      OMG 7
        FOUND YR 62748517
    OIC
    FOUND YR 1
  IF U SAY SO

  HOW IZ I ROWGET YR encoded AN YR column
    I HAS A weight ITZ I IZ WEIGHT YR column MKAY
    FOUND YR MOD OF QUOSHUNT OF encoded AN weight AN 13
  IF U SAY SO

  HOW IZ I ROWSET YR encoded AN YR column AN YR piece
    I HAS A weight ITZ I IZ WEIGHT YR column MKAY
    I HAS A old_piece ITZ I IZ ROWGET YR encoded AN YR column MKAY
    FOUND YR SUM OF encoded AN PRODUKT OF DIFF OF piece AN old_piece AN weight
  IF U SAY SO

  HOW IZ I ABSVAL YR value
    BOTH SAEM value AN SMALLR OF value AN 0
    O RLY?
      YA RLY
        FOUND YR DIFF OF 0 AN value
    OIC
    FOUND YR value
  IF U SAY SO

  HOW IZ I SIGNOF YR value
    BOTH SAEM value AN 0
    O RLY?
      YA RLY
        FOUND YR 0
    OIC
    BOTH SAEM value AN BIGGR OF value AN 0
    O RLY?
      YA RLY
        FOUND YR 1
      NO WAI
        FOUND YR -1
    OIC
  IF U SAY SO

  HOW IZ I COLOR YR piece
    BOTH SAEM piece AN 0
    O RLY?
      YA RLY
        FOUND YR 0
    OIC
    BOTH SAEM piece AN SMALLR OF piece AN 6
    O RLY?
      YA RLY
        FOUND YR 1
      NO WAI
        FOUND YR -1
    OIC
  IF U SAY SO

  HOW IZ I PIECETYPE YR piece
    BOTH SAEM piece AN BIGGR OF piece AN 7
    O RLY?
      YA RLY
        FOUND YR DIFF OF piece AN 6
    OIC
    FOUND YR piece
  IF U SAY SO

  HOW IZ I BOARDROW YR square
    FOUND YR QUOSHUNT OF square AN 8
  IF U SAY SO

  HOW IZ I BOARDCOL YR square
    FOUND YR MOD OF square AN 8
  IF U SAY SO

  HOW IZ I SQUAREOF YR row AN YR column
    FOUND YR SUM OF PRODUKT OF row AN 8 AN column
  IF U SAY SO

  HOW IZ I INSIDE YR row AN YR column
    BOTH SAEM row AN SMALLR OF row AN -1
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    BOTH SAEM column AN SMALLR OF column AN -1
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    BOTH SAEM row AN BIGGR OF row AN 8
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    BOTH SAEM column AN BIGGR OF column AN 8
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    FOUND YR WIN
  IF U SAY SO

  HOW IZ I GETSQ YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR square
    I HAS A row ITZ I IZ BOARDROW YR square MKAY
    I HAS A column ITZ I IZ BOARDCOL YR square MKAY
    row
    WTF?
      OMG 0
        FOUND YR I IZ ROWGET YR B0 AN YR column MKAY
      OMG 1
        FOUND YR I IZ ROWGET YR B1 AN YR column MKAY
      OMG 2
        FOUND YR I IZ ROWGET YR B2 AN YR column MKAY
      OMG 3
        FOUND YR I IZ ROWGET YR B3 AN YR column MKAY
      OMG 4
        FOUND YR I IZ ROWGET YR B4 AN YR column MKAY
      OMG 5
        FOUND YR I IZ ROWGET YR B5 AN YR column MKAY
      OMG 6
        FOUND YR I IZ ROWGET YR B6 AN YR column MKAY
      OMG 7
        FOUND YR I IZ ROWGET YR B7 AN YR column MKAY
    OIC
    FOUND YR 0
  IF U SAY SO

  BTW Return WIN when every square strictly between from and to is empty.
  HOW IZ I PATHCLEAR YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
    AN YR from AN YR to AN YR row_step AN YR col_step
    I HAS A walk_row ITZ SUM OF I IZ BOARDROW YR from MKAY AN row_step
    I HAS A walk_col ITZ SUM OF I IZ BOARDCOL YR from MKAY AN col_step
    I HAS A walk_square ITZ I IZ SQUAREOF YR walk_row AN YR walk_col MKAY

    IM IN YR path UPPIN YR path_step WILE DIFFRINT walk_square AN to
      I HAS A blocker ITZ I IZ GETSQ YR B0 AN YR B1 AN YR B2 AN YR B3 ...
        AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR walk_square MKAY
      DIFFRINT blocker AN 0
      O RLY?
        YA RLY
          FOUND YR FAIL
      OIC
      walk_row R SUM OF walk_row AN row_step
      walk_col R SUM OF walk_col AN col_step
      walk_square R I IZ SQUAREOF YR walk_row AN YR walk_col MKAY
    IM OUTTA YR path

    FOUND YR WIN
  IF U SAY SO

  BTW Piece movement without checking whether the moving king stays safe.
  HOW IZ I PSEUDO YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
    AN YR from AN YR to AN YR side
    BOTH SAEM from AN to
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    BOTH SAEM from AN SMALLR OF from AN -1
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    BOTH SAEM to AN SMALLR OF to AN -1
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    BOTH SAEM from AN BIGGR OF from AN 64
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    BOTH SAEM to AN BIGGR OF to AN 64
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC

    I HAS A piece ITZ I IZ GETSQ YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR from MKAY
    I HAS A target ITZ I IZ GETSQ YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR to MKAY

    DIFFRINT I IZ COLOR YR piece MKAY AN side
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    BOTH SAEM I IZ COLOR YR target MKAY AN side
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    BOTH SAEM I IZ PIECETYPE YR target MKAY AN 6
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC

    I HAS A piece_type ITZ I IZ PIECETYPE YR piece MKAY
    I HAS A from_row ITZ I IZ BOARDROW YR from MKAY
    I HAS A from_col ITZ I IZ BOARDCOL YR from MKAY
    I HAS A to_row ITZ I IZ BOARDROW YR to MKAY
    I HAS A to_col ITZ I IZ BOARDCOL YR to MKAY
    I HAS A row_delta ITZ DIFF OF to_row AN from_row
    I HAS A col_delta ITZ DIFF OF to_col AN from_col
    I HAS A abs_row ITZ I IZ ABSVAL YR row_delta MKAY
    I HAS A abs_col ITZ I IZ ABSVAL YR col_delta MKAY

    BOTH SAEM piece_type AN 1
    O RLY?
      YA RLY
        I HAS A direction ITZ -1
        I HAS A start_row ITZ 6
        BOTH SAEM side AN -1
        O RLY?
          YA RLY
            direction R 1
            start_row R 1
        OIC

        BOTH OF BOTH SAEM col_delta AN 0 AN BOTH SAEM row_delta AN direction
        O RLY?
          YA RLY
            BOTH SAEM target AN 0
            O RLY?
              YA RLY
                FOUND YR WIN
            OIC
        OIC

        BOTH OF BOTH SAEM col_delta AN 0 AN BOTH SAEM row_delta AN PRODUKT OF 2 AN direction
        O RLY?
          YA RLY
            BOTH SAEM from_row AN start_row
            O RLY?
              YA RLY
                I HAS A middle_square ITZ I IZ SQUAREOF ...
                  YR SUM OF from_row AN direction AN YR from_col MKAY
                I HAS A middle_piece ITZ I IZ GETSQ ...
                  YR B0 AN YR B1 AN YR B2 AN YR B3 ...
                  AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
                  AN YR middle_square MKAY
                BOTH OF BOTH SAEM target AN 0 AN BOTH SAEM middle_piece AN 0
                O RLY?
                  YA RLY
                    FOUND YR WIN
                OIC
            OIC
        OIC

        BOTH OF BOTH SAEM abs_row AN 1 AN BOTH OF BOTH SAEM abs_col AN 1 AN BOTH SAEM row_delta AN direction
        O RLY?
          YA RLY
            DIFFRINT target AN 0
            O RLY?
              YA RLY
                FOUND YR WIN
            OIC
        OIC
        FOUND YR FAIL
    OIC

    BOTH SAEM piece_type AN 2
    O RLY?
      YA RLY
        EITHER OF BOTH OF BOTH SAEM abs_row AN 2 AN BOTH SAEM abs_col AN 1 ...
          AN BOTH OF BOTH SAEM abs_row AN 1 AN BOTH SAEM abs_col AN 2
        O RLY?
          YA RLY
            FOUND YR WIN
          NO WAI
            FOUND YR FAIL
        OIC
    OIC

    BOTH SAEM piece_type AN 6
    O RLY?
      YA RLY
        BOTH SAEM BIGGR OF abs_row AN abs_col AN 1
        O RLY?
          YA RLY
            FOUND YR WIN
          NO WAI
            FOUND YR FAIL
        OIC
    OIC

    I HAS A row_step ITZ I IZ SIGNOF YR row_delta MKAY
    I HAS A col_step ITZ I IZ SIGNOF YR col_delta MKAY

    BOTH SAEM piece_type AN 3
    O RLY?
      YA RLY
        DIFFRINT abs_row AN abs_col
        O RLY?
          YA RLY
            FOUND YR FAIL
        OIC
        FOUND YR I IZ PATHCLEAR YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
          AN YR from AN YR to AN YR row_step AN YR col_step MKAY
    OIC

    BOTH SAEM piece_type AN 4
    O RLY?
      YA RLY
        EITHER OF BOTH SAEM row_delta AN 0 AN BOTH SAEM col_delta AN 0
        O RLY?
          YA RLY
            FOUND YR I IZ PATHCLEAR YR B0 AN YR B1 AN YR B2 AN YR B3 ...
              AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
              AN YR from AN YR to AN YR row_step AN YR col_step MKAY
        OIC
        FOUND YR FAIL
    OIC

    BOTH SAEM piece_type AN 5
    O RLY?
      YA RLY
        EITHER OF BOTH SAEM abs_row AN abs_col ...
          AN EITHER OF BOTH SAEM row_delta AN 0 AN BOTH SAEM col_delta AN 0
        O RLY?
          YA RLY
            FOUND YR I IZ PATHCLEAR YR B0 AN YR B1 AN YR B2 AN YR B3 ...
              AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
              AN YR from AN YR to AN YR row_step AN YR col_step MKAY
        OIC
        FOUND YR FAIL
    OIC

    FOUND YR FAIL
  IF U SAY SO

  BTW Attack rules differ from movement rules for pawns.
  HOW IZ I CANATTACK YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
    AN YR from AN YR to AN YR side
    I HAS A piece ITZ I IZ GETSQ YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR from MKAY
    DIFFRINT I IZ COLOR YR piece MKAY AN side
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC

    I HAS A piece_type ITZ I IZ PIECETYPE YR piece MKAY
    I HAS A from_row ITZ I IZ BOARDROW YR from MKAY
    I HAS A from_col ITZ I IZ BOARDCOL YR from MKAY
    I HAS A to_row ITZ I IZ BOARDROW YR to MKAY
    I HAS A to_col ITZ I IZ BOARDCOL YR to MKAY
    I HAS A row_delta ITZ DIFF OF to_row AN from_row
    I HAS A col_delta ITZ DIFF OF to_col AN from_col
    I HAS A abs_row ITZ I IZ ABSVAL YR row_delta MKAY
    I HAS A abs_col ITZ I IZ ABSVAL YR col_delta MKAY

    BOTH SAEM piece_type AN 1
    O RLY?
      YA RLY
        I HAS A direction ITZ -1
        BOTH SAEM side AN -1
        O RLY?
          YA RLY
            direction R 1
        OIC
        FOUND YR BOTH OF BOTH SAEM row_delta AN direction AN BOTH SAEM abs_col AN 1
    OIC

    BOTH SAEM piece_type AN 2
    O RLY?
      YA RLY
        FOUND YR EITHER OF BOTH OF BOTH SAEM abs_row AN 2 AN BOTH SAEM abs_col AN 1 ...
          AN BOTH OF BOTH SAEM abs_row AN 1 AN BOTH SAEM abs_col AN 2
    OIC

    BOTH SAEM piece_type AN 6
    O RLY?
      YA RLY
        FOUND YR BOTH SAEM BIGGR OF abs_row AN abs_col AN 1
    OIC

    I HAS A row_step ITZ I IZ SIGNOF YR row_delta MKAY
    I HAS A col_step ITZ I IZ SIGNOF YR col_delta MKAY

    BOTH SAEM piece_type AN 3
    O RLY?
      YA RLY
        DIFFRINT abs_row AN abs_col
        O RLY?
          YA RLY
            FOUND YR FAIL
        OIC
        FOUND YR I IZ PATHCLEAR YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
          AN YR from AN YR to AN YR row_step AN YR col_step MKAY
    OIC

    BOTH SAEM piece_type AN 4
    O RLY?
      YA RLY
        EITHER OF BOTH SAEM row_delta AN 0 AN BOTH SAEM col_delta AN 0
        O RLY?
          YA RLY
            FOUND YR I IZ PATHCLEAR YR B0 AN YR B1 AN YR B2 AN YR B3 ...
              AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
              AN YR from AN YR to AN YR row_step AN YR col_step MKAY
        OIC
        FOUND YR FAIL
    OIC

    BOTH SAEM piece_type AN 5
    O RLY?
      YA RLY
        EITHER OF BOTH SAEM abs_row AN abs_col ...
          AN EITHER OF BOTH SAEM row_delta AN 0 AN BOTH SAEM col_delta AN 0
        O RLY?
          YA RLY
            FOUND YR I IZ PATHCLEAR YR B0 AN YR B1 AN YR B2 AN YR B3 ...
              AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
              AN YR from AN YR to AN YR row_step AN YR col_step MKAY
        OIC
        FOUND YR FAIL
    OIC

    FOUND YR FAIL
  IF U SAY SO

  HOW IZ I FINDKING YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR side
    I HAS A king_piece ITZ 6
    BOTH SAEM side AN -1
    O RLY?
      YA RLY
        king_piece R 12
    OIC

    IM IN YR findking UPPIN YR square TIL BOTH SAEM square AN 64
      I HAS A piece ITZ I IZ GETSQ YR B0 AN YR B1 AN YR B2 AN YR B3 ...
        AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR square MKAY
      BOTH SAEM piece AN king_piece
      O RLY?
        YA RLY
          FOUND YR square
      OIC
    IM OUTTA YR findking

    FOUND YR -1
  IF U SAY SO

  HOW IZ I INCHECK YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR side
    I HAS A king_square ITZ I IZ FINDKING ...
      YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR side MKAY
    BOTH SAEM king_square AN -1
    O RLY?
      YA RLY
        FOUND YR WIN
    OIC

    I HAS A enemy ITZ DIFF OF 0 AN side
    IM IN YR scan UPPIN YR square TIL BOTH SAEM square AN 64
      I IZ CANATTACK YR B0 AN YR B1 AN YR B2 AN YR B3 ...
        AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
        AN YR square AN YR king_square AN YR enemy MKAY
      O RLY?
        YA RLY
          FOUND YR WIN
      OIC
    IM OUTTA YR scan

    FOUND YR FAIL
  IF U SAY SO

  BTW Apply one move to one encoded rank and return the new rank.
  HOW IZ I APPLYROW YR encoded AN YR rank AN YR from AN YR to AN YR piece
    I HAS A result ITZ encoded
    I HAS A from_row ITZ I IZ BOARDROW YR from MKAY
    I HAS A to_row ITZ I IZ BOARDROW YR to MKAY
    I HAS A from_col ITZ I IZ BOARDCOL YR from MKAY
    I HAS A to_col ITZ I IZ BOARDCOL YR to MKAY
    I HAS A placed_piece ITZ piece

    BOTH SAEM I IZ PIECETYPE YR piece MKAY AN 1
    O RLY?
      YA RLY
        EITHER OF BOTH SAEM to_row AN 0 AN BOTH SAEM to_row AN 7
        O RLY?
          YA RLY
            BOTH SAEM I IZ COLOR YR piece MKAY AN 1
            O RLY?
              YA RLY
                placed_piece R 5
              NO WAI
                placed_piece R 11
            OIC
        OIC
    OIC

    BOTH SAEM rank AN from_row
    O RLY?
      YA RLY
        result R I IZ ROWSET YR result AN YR from_col AN YR 0 MKAY
    OIC
    BOTH SAEM rank AN to_row
    O RLY?
      YA RLY
        result R I IZ ROWSET YR result AN YR to_col AN YR placed_piece MKAY
    OIC

    FOUND YR result
  IF U SAY SO

  HOW IZ I INCHECKAFTER YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
    AN YR from AN YR to AN YR side
    I HAS A piece ITZ I IZ GETSQ YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR from MKAY
    B0 R I IZ APPLYROW YR B0 AN YR 0 AN YR from AN YR to AN YR piece MKAY
    B1 R I IZ APPLYROW YR B1 AN YR 1 AN YR from AN YR to AN YR piece MKAY
    B2 R I IZ APPLYROW YR B2 AN YR 2 AN YR from AN YR to AN YR piece MKAY
    B3 R I IZ APPLYROW YR B3 AN YR 3 AN YR from AN YR to AN YR piece MKAY
    B4 R I IZ APPLYROW YR B4 AN YR 4 AN YR from AN YR to AN YR piece MKAY
    B5 R I IZ APPLYROW YR B5 AN YR 5 AN YR from AN YR to AN YR piece MKAY
    B6 R I IZ APPLYROW YR B6 AN YR 6 AN YR from AN YR to AN YR piece MKAY
    B7 R I IZ APPLYROW YR B7 AN YR 7 AN YR from AN YR to AN YR piece MKAY
    FOUND YR I IZ INCHECK YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR side MKAY
  IF U SAY SO

  HOW IZ I LEGAL YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
    AN YR from AN YR to AN YR side
    NOT I IZ PSEUDO YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
      AN YR from AN YR to AN YR side MKAY
    O RLY?
      YA RLY
        FOUND YR FAIL
    OIC
    FOUND YR NOT I IZ INCHECKAFTER ...
      YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
      AN YR from AN YR to AN YR side MKAY
  IF U SAY SO

  HOW IZ I PIECEVALUE YR piece_type
    piece_type
    WTF?
      OMG 1
        FOUND YR 100
      OMG 2
        FOUND YR 320
      OMG 3
        FOUND YR 330
      OMG 4
        FOUND YR 500
      OMG 5
        FOUND YR 900
      OMG 6
        FOUND YR 20000
    OIC
    FOUND YR 0
  IF U SAY SO

  BTW Positive scores favor White; negative scores favor Black.
  HOW IZ I EVALUATE YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7
    I HAS A score ITZ 0
    IM IN YR evaluate UPPIN YR square TIL BOTH SAEM square AN 64
      I HAS A piece ITZ I IZ GETSQ YR B0 AN YR B1 AN YR B2 AN YR B3 ...
        AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR square MKAY
      DIFFRINT piece AN 0
      O RLY?
        YA RLY
          I HAS A worth ITZ I IZ PIECEVALUE YR I IZ PIECETYPE YR piece MKAY MKAY
          BOTH SAEM I IZ COLOR YR piece MKAY AN 1
          O RLY?
            YA RLY
              score R SUM OF score AN worth
            NO WAI
              score R DIFF OF score AN worth
          OIC
      OIC
    IM OUTTA YR evaluate
    FOUND YR score
  IF U SAY SO

  HOW IZ I HASLEGAL YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR side
    IM IN YR froms UPPIN YR from TIL BOTH SAEM from AN 64
      IM IN YR tos UPPIN YR to TIL BOTH SAEM to AN 64
        I IZ LEGAL YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
          AN YR from AN YR to AN YR side MKAY
        O RLY?
          YA RLY
            FOUND YR WIN
        OIC
      IM OUTTA YR tos
    IM OUTTA YR froms
    FOUND YR FAIL
  IF U SAY SO

  HOW IZ I CENTERBONUS YR square
    I HAS A row_distance ITZ I IZ ABSVAL YR DIFF OF I IZ BOARDROW YR square MKAY AN 3 MKAY
    I HAS A col_distance ITZ I IZ ABSVAL YR DIFF OF I IZ BOARDCOL YR square MKAY AN 3 MKAY
    FOUND YR DIFF OF 8 AN SUM OF row_distance AN col_distance
  IF U SAY SO

  BTW Black AI searches one ply: captures, checks, promotion, then center control.
  BTW A move is returned as from * 64 + to.
  HOW IZ I AIMOVE YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7
    I HAS A base_score ITZ I IZ EVALUATE ...
      YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 MKAY
    I HAS A best_score ITZ 999999
    I HAS A best_move ITZ -1

    IM IN YR aifroms UPPIN YR from TIL BOTH SAEM from AN 64
      IM IN YR aitos UPPIN YR to TIL BOTH SAEM to AN 64
        I IZ LEGAL YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
          AN YR from AN YR to AN YR -1 MKAY
        O RLY?
          YA RLY
            I HAS A score ITZ base_score
            I HAS A target ITZ I IZ GETSQ ...
              YR B0 AN YR B1 AN YR B2 AN YR B3 ...
              AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR to MKAY
            I HAS A moving_piece ITZ I IZ GETSQ ...
              YR B0 AN YR B1 AN YR B2 AN YR B3 ...
              AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR from MKAY

            DIFFRINT target AN 0
            O RLY?
              YA RLY
                score R DIFF OF score AN I IZ PIECEVALUE YR I IZ PIECETYPE YR target MKAY MKAY
            OIC

            BOTH OF BOTH SAEM I IZ PIECETYPE YR moving_piece MKAY AN 1 ...
              AN BOTH SAEM I IZ BOARDROW YR to MKAY AN 7
            O RLY?
              YA RLY
                score R DIFF OF score AN 800
            OIC

            I IZ INCHECKAFTER ...
              YR B0 AN YR B1 AN YR B2 AN YR B3 ...
              AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
              AN YR from AN YR to AN YR 1 MKAY
            O RLY?
              YA RLY
                score R DIFF OF score AN 25
            OIC

            score R DIFF OF score AN I IZ CENTERBONUS YR to MKAY

            BOTH OF BOTH SAEM score AN SMALLR OF score AN best_score ...
              AN DIFFRINT score AN best_score
            O RLY?
              YA RLY
                best_score R score
                best_move R SUM OF PRODUKT OF from AN 64 AN to
            OIC
        OIC
      IM OUTTA YR aitos
    IM OUTTA YR aifroms

    FOUND YR best_move
  IF U SAY SO

  HOW IZ I SYMBOL YR piece
    piece
    WTF?
      OMG 0
        FOUND YR "."
      OMG 1
        FOUND YR "P"
      OMG 2
        FOUND YR "N"
      OMG 3
        FOUND YR "B"
      OMG 4
        FOUND YR "R"
      OMG 5
        FOUND YR "Q"
      OMG 6
        FOUND YR "K"
      OMG 7
        FOUND YR "p"
      OMG 8
        FOUND YR "n"
      OMG 9
        FOUND YR "b"
      OMG 10
        FOUND YR "r"
      OMG 11
        FOUND YR "q"
      OMG 12
        FOUND YR "k"
    OIC
    FOUND YR "?"
  IF U SAY SO

  HOW IZ I FILETXT YR column
    column
    WTF?
      OMG 0
        FOUND YR "a"
      OMG 1
        FOUND YR "b"
      OMG 2
        FOUND YR "c"
      OMG 3
        FOUND YR "d"
      OMG 4
        FOUND YR "e"
      OMG 5
        FOUND YR "f"
      OMG 6
        FOUND YR "g"
      OMG 7
        FOUND YR "h"
    OIC
    FOUND YR "?"
  IF U SAY SO

  HOW IZ I SQUARETXT YR square
    I HAS A file ITZ I IZ FILETXT YR I IZ BOARDCOL YR square MKAY MKAY
    I HAS A rank ITZ DIFF OF 8 AN I IZ BOARDROW YR square MKAY
    FOUND YR SMOOSH file AN rank MKAY
  IF U SAY SO

  HOW IZ I SQUAREFROMTXT YR square_text
    IM IN YR squares UPPIN YR square TIL BOTH SAEM square AN 64
      BOTH SAEM square_text AN I IZ SQUARETXT YR square MKAY
      O RLY?
        YA RLY
          FOUND YR square
      OIC
    IM OUTTA YR squares
    FOUND YR -1
  IF U SAY SO

  HOW IZ I PRINTBOARD YR B0 AN YR B1 AN YR B2 AN YR B3 ...
    AN YR B4 AN YR B5 AN YR B6 AN YR B7
    VISIBLE ""
    VISIBLE "    a b c d e f g h"
    VISIBLE "  +-----------------+"
    IM IN YR rows UPPIN YR row TIL BOTH SAEM row AN 8
      VISIBLE DIFF OF 8 AN row " | " !
      IM IN YR columns UPPIN YR column TIL BOTH SAEM column AN 8
        I HAS A square ITZ I IZ SQUAREOF YR row AN YR column MKAY
        I HAS A piece ITZ I IZ GETSQ ...
          YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR square MKAY
        VISIBLE I IZ SYMBOL YR piece MKAY " " !
      IM OUTTA YR columns
      VISIBLE "| " DIFF OF 8 AN row
    IM OUTTA YR rows
    VISIBLE "  +-----------------+"
    VISIBLE "    a b c d e f g h"
    VISIBLE ""
    FOUND YR WIN
  IF U SAY SO

  BTW Initial encoded ranks.
  I HAS A B0 ITZ 669809813
  I HAS A B1 ITZ 475842920
  I HAS A B2 ITZ 0
  I HAS A B3 ITZ 0
  I HAS A B4 ITZ 0
  I HAS A B5 ITZ 0
  I HAS A B6 ITZ 67977560
  I HAS A B7 ITZ 261944453

  VISIBLE "========================================"
  VISIBLE "              LOLCHESS"
  VISIBLE "========================================"
  VISIBLE "U R WHITE. ENTER LOWERCASE SQUARES."
  VISIBLE "EXAMPLE:: e2 THEN e4"
  VISIBLE "ENTER quit AT FROM TO LEAVE."

  IM IN YR game UPPIN YR turn TIL BOTH SAEM turn AN 1000
    I IZ PRINTBOARD YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 MKAY

    NOT I IZ HASLEGAL YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR 1 MKAY
    O RLY?
      YA RLY
        I IZ INCHECK YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR 1 MKAY
        O RLY?
          YA RLY
            VISIBLE "CHECKMATE. LOL AI WINZ."
          NO WAI
            VISIBLE "STALEMATE."
        OIC
        GTFO
    OIC

    I IZ INCHECK YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR 1 MKAY
    O RLY?
      YA RLY
        VISIBLE "UR KING IZ IN CHECK!"
    OIC

    VISIBLE "FROM? " !
    I HAS A player_from_text
    GIMMEH player_from_text

    EITHER OF BOTH SAEM player_from_text AN "quit" AN BOTH SAEM player_from_text AN "q"
    O RLY?
      YA RLY
        VISIBLE "KTHXBAI! TANKS 4 PLAYIN LOLCHESS!"
        GTFO
    OIC

    I HAS A player_from ITZ I IZ SQUAREFROMTXT YR player_from_text MKAY

    VISIBLE "TO? " !
    I HAS A player_to_text
    GIMMEH player_to_text
    I HAS A player_to ITZ I IZ SQUAREFROMTXT YR player_to_text MKAY

    I IZ LEGAL YR B0 AN YR B1 AN YR B2 AN YR B3 ...
      AN YR B4 AN YR B5 AN YR B6 AN YR B7 ...
      AN YR player_from AN YR player_to AN YR 1 MKAY
    O RLY?
      YA RLY
        I HAS A player_piece ITZ I IZ GETSQ ...
          YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR player_from MKAY
        B0 R I IZ APPLYROW YR B0 AN YR 0 AN YR player_from AN YR player_to AN YR player_piece MKAY
        B1 R I IZ APPLYROW YR B1 AN YR 1 AN YR player_from AN YR player_to AN YR player_piece MKAY
        B2 R I IZ APPLYROW YR B2 AN YR 2 AN YR player_from AN YR player_to AN YR player_piece MKAY
        B3 R I IZ APPLYROW YR B3 AN YR 3 AN YR player_from AN YR player_to AN YR player_piece MKAY
        B4 R I IZ APPLYROW YR B4 AN YR 4 AN YR player_from AN YR player_to AN YR player_piece MKAY
        B5 R I IZ APPLYROW YR B5 AN YR 5 AN YR player_from AN YR player_to AN YR player_piece MKAY
        B6 R I IZ APPLYROW YR B6 AN YR 6 AN YR player_from AN YR player_to AN YR player_piece MKAY
        B7 R I IZ APPLYROW YR B7 AN YR 7 AN YR player_from AN YR player_to AN YR player_piece MKAY

        NOT I IZ HASLEGAL YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR -1 MKAY
        O RLY?
          YA RLY
            I IZ PRINTBOARD YR B0 AN YR B1 AN YR B2 AN YR B3 ...
              AN YR B4 AN YR B5 AN YR B6 AN YR B7 MKAY
            I IZ INCHECK YR B0 AN YR B1 AN YR B2 AN YR B3 ...
              AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR -1 MKAY
            O RLY?
              YA RLY
                VISIBLE "CHECKMATE! U WIN!"
              NO WAI
                VISIBLE "STALEMATE."
            OIC
            GTFO
        OIC

        VISIBLE "LOL AI IZ THINKIN..."
        I HAS A ai_move ITZ I IZ AIMOVE ...
          YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 MKAY
        I HAS A ai_from ITZ QUOSHUNT OF ai_move AN 64
        I HAS A ai_to ITZ MOD OF ai_move AN 64
        I HAS A ai_piece ITZ I IZ GETSQ ...
          YR B0 AN YR B1 AN YR B2 AN YR B3 ...
          AN YR B4 AN YR B5 AN YR B6 AN YR B7 AN YR ai_from MKAY

        VISIBLE "AI MOVE:: " I IZ SQUARETXT YR ai_from MKAY " TO " ...
          I IZ SQUARETXT YR ai_to MKAY
        B0 R I IZ APPLYROW YR B0 AN YR 0 AN YR ai_from AN YR ai_to AN YR ai_piece MKAY
        B1 R I IZ APPLYROW YR B1 AN YR 1 AN YR ai_from AN YR ai_to AN YR ai_piece MKAY
        B2 R I IZ APPLYROW YR B2 AN YR 2 AN YR ai_from AN YR ai_to AN YR ai_piece MKAY
        B3 R I IZ APPLYROW YR B3 AN YR 3 AN YR ai_from AN YR ai_to AN YR ai_piece MKAY
        B4 R I IZ APPLYROW YR B4 AN YR 4 AN YR ai_from AN YR ai_to AN YR ai_piece MKAY
        B5 R I IZ APPLYROW YR B5 AN YR 5 AN YR ai_from AN YR ai_to AN YR ai_piece MKAY
        B6 R I IZ APPLYROW YR B6 AN YR 6 AN YR ai_from AN YR ai_to AN YR ai_piece MKAY
        B7 R I IZ APPLYROW YR B7 AN YR 7 AN YR ai_from AN YR ai_to AN YR ai_piece MKAY
      NO WAI
        VISIBLE "NOPE. DAT MOVE IZ ILLEGAL. TRY AGAIN."
    OIC
  IM OUTTA YR game

KTHXBYE
