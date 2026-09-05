# LOLCODE Tic-Tac-Toe

A one or two-player terminal Tic-Tac-Toe game written entirely in LOLCODE and
built with `Lolcode.NET.Sdk`.

## Run

```bash
dotnet build dotnet-lolcode.slnx
dotnet run --file samples/games/tic-tac-toe/tic-tac-toe.lol
```

The game shows a numbered position guide before starting with an empty board.
Choose one player to play as `X` against a defensive AI, or two players to
alternate locally as `X` and `O`. Enter the number of an empty cell to place a
mark, or enter `q` or `quit` to leave the game.
