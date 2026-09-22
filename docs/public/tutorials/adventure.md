# Build a terminal adventure

**You need:** decisions, loops, functions, and a state plan.

**You will build:** a stable 1.2 room game with inventory, commands, and a
testable transition model.

## 1. Model state before rooms

Use a small set of facts:

```lolcode
I HAS A room ITZ 1
I HAS A hasKey ITZ FAIL
I HAS A hasSword ITZ FAIL
I HAS A gameOver ITZ FAIL
```

Write on paper what each command may change. For example, `north` from room 1
may require `hasKey`; `fight` in the final room may require `hasSword`.

## 2. Separate description from mutation

Functions cannot read caller variables, so pass what a description needs:

```lolcode
HOW IZ I describeRoom YR room AN YR hasSword
  room
  WTF?
    OMG 1
      VISIBLE "A SMALL ROOM. A DOOR LEADS NORTH."
      GTFO
    OMG 2
      VISIBLE "A HALLWAY."
      GTFO
    OMGWTF
      VISIBLE "A DRAGON WAITS."
  OIC
IF U SAY SO
```

Description produces output but does not mutate the game. Main owns state
changes, making turn order easier to trace.

## 3. Build one room and command

```lolcode
I IZ describeRoom YR room AN YR hasSword MKAY
I HAS A command
VISIBLE "> " !
GIMMEH command

BOTH SAEM room AN 1
O RLY?
  YA RLY
    BOTH SAEM command AN "take key"
    O RLY?
      YA RLY
        hasKey R WIN
        VISIBLE "KEY TAKEN"
    OIC
OIC
```

Run after every command addition. Do not write all rooms before proving the
loop.

## 4. Add movement with prerequisites

For `north`, first test the current room, then inventory:

```lolcode
BOTH SAEM command AN "north"
O RLY?
  YA RLY
    hasKey
    O RLY?
      YA RLY, room R 2
      NO WAI, VISIBLE "THE DOOR IZ LOCKD"
    OIC
OIC
```

Assignments do not update `IT`; putting `hasKey` on its own line does.

## 5. Wrap the turn loop

```lolcode
IM IN YR adventure WILE NOT gameOver
  BTW describe, read, and process one command
IM OUTTA YR adventure
```

Include a `quit` command that sets `gameOver` or uses `GTFO`. Then add rooms one
at a time. Compare with the maintained
[`adventure.lol`](../../../samples/games/adventure-game/adventure.lol), which
uses a room number, inventory TROOF values, and a main-owned mutation loop.

## Walkthrough test

Run this command sequence:

```text
take key
north
take sword
north
fight
```

For each turn, record room and inventory before and after. Also test:

- `north` before taking the key;
- `fight` without the sword;
- an unknown command;
- `look` without state change;
- `quit` from every room.

<details>
<summary>Extension: make transitions testable</summary>

Create small predicate functions such as `canMoveNorth YR room AN YR hasKey`.
Keep mutation in main, but move yes/no rules into functions. That gives each
rule a compact input/output test table.
</details>

The key lesson is state ownership: one turn reads state, selects an action,
updates a small subset, and repeats. Continue to
[planning complete programs](../learn/planning-programs.md).
