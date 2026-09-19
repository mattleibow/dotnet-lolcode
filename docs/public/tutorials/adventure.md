# Adventure game: a larger terminal application

The [adventure sample](../../../samples/games/adventure-game/adventure.lol) is a
small text game with a room number, two inventory TROOF values, and a
`game_over` flag. It demonstrates a practical design constraint in this
implementation: functions cannot read the caller's outer variables. The
`describe_room` function receives the room and sword state as parameters; the
main loop owns mutations.

Play it by entering `take key`, `north`, `take sword`, `north`, and `fight`.
For each turn the program:

1. calls `describe_room`;
2. reads one command with `GIMMEH`;
3. checks room-specific actions with nested `O RLY?` statements;
4. updates state and loops until `GTFO` or `game_over`.

The code favors direct, readable state transitions over a hidden framework.
That makes it a good exercise in tracing control flow. Try adding a `look`
command or a fifth room, then run the existing sample after each change. For a
different terminal challenge, see [calculator](calculator.md); for compiler
handling of scope and flow, see [binding and lowering](../compiler-course/binding.md).
