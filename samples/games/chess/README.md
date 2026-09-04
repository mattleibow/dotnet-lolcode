# LOLCHESS

A terminal chess game written entirely in LOLCODE and built with `Lolcode.NET.Sdk`.
You play White against a one-ply material AI.

## Run

```bash
dotnet build dotnet-lolcode.slnx
dotnet run --file samples/games/chess/chess.lol
```

## Input

Enter the lowercase coordinate of the piece followed by its destination:

```text
FROM? e2
TO? e4
```

Enter `quit` or `q` at the `FROM?` prompt to leave the game.

## Rules

The game supports normal movement and captures for every piece, king-safety
validation, check, checkmate, stalemate, pawn double moves, and automatic queen
promotion. Castling, en passant, repetition, and the fifty-move rule are not
implemented.

The AI searches every legal Black move and scores captures, checks, promotion,
and centralization. It does not search the player's possible reply.

## Implementation

The compiler intentionally isolates function variables from outer variables.
To keep the helpers pure, each rank is encoded as a base-13 integer and passed
into move-generation functions. Board mutations happen by returning updated
rank values to the main game loop. Coordinates such as `e4` are converted to
the engine's internal 0-63 square indexes at the input boundary.
