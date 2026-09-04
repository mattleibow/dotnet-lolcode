# LOLCODE Samples

Example programs demonstrating LOLCODE features, organized by complexity.
File-based apps are the primary experience: each sample carries a shebang and
`#:sdk` directive, so it builds and runs without a project file.

```bash
# From the repo root, build the local compiler first:
dotnet build dotnet-lolcode.slnx

# Then run any sample:
dotnet build samples/basics/hello-world/hello.lol
dotnet run --file samples/basics/hello-world/hello.lol
dotnet run --file samples/programs/fizzbuzz/fizzbuzz.lol
dotnet run --file samples/games/arena-game/Game.lol
```

## Basics

Language fundamentals, with one concept per sample.

| Sample | Run | Demonstrates |
|--------|-----|-------------|
| [hello-world](basics/hello-world/) | `dotnet run --file samples/basics/hello-world/hello.lol` | Program structure, VISIBLE |
| [variables](basics/variables/) | `dotnet run --file samples/basics/variables/variables.lol` | I HAS A, ITZ, R, types |
| [math](basics/math/) | `dotnet run --file samples/basics/math/math.lol` | SUM OF, DIFF OF, PRODUKT OF, etc. |
| [conditionals](basics/conditionals/) | `dotnet run --file samples/basics/conditionals/conditionals.lol` | O RLY?, YA RLY, NO WAI, MEBBE |
| [loops](basics/loops/) | `dotnet run --file samples/basics/loops/loops.lol` | IM IN YR, UPPIN, NERFIN, TIL, WILE |
| [functions](basics/functions/) | `dotnet run --file samples/basics/functions/functions.lol` | HOW IZ I, FOUND YR, IF U SAY SO |
| [string-ops](basics/string-ops/) | `dotnet run --file samples/basics/string-ops/strings.lol` | SMOOSH, string escapes |
| [casting](basics/casting/) | `dotnet run --file samples/basics/casting/casting.lol` | MAEK, IS NOW A |
| [switch](basics/switch/) | `dotnet run --file samples/basics/switch/switch.lol` | WTF?, OMG, OMGWTF |

## Programs

Algorithmic demos combining multiple language features.

| Sample | Run | Demonstrates |
|--------|-----|-------------|
| [fizzbuzz](programs/fizzbuzz/) | `dotnet run --file samples/programs/fizzbuzz/fizzbuzz.lol` | Loops + conditionals + math |
| [fibonacci](programs/fibonacci/) | `dotnet run --file samples/programs/fibonacci/fibonacci.lol` | Functions + recursion |
| [recursion](programs/recursion/) | `dotnet run --file samples/programs/recursion/recursion.lol` | Recursive functions |
| [string-calculator](programs/string-calculator/) | `dotnet run --file samples/programs/string-calculator/calculator.lol` | Parsing + switch + functions |

## Games

Interactive programs with user input (`GIMMEH`).

| Sample | Run | Description |
|--------|-----|-------------|
| [guessing-game](games/guessing-game/) | `dotnet run --file samples/games/guessing-game/guess.lol` | Number guessing with I/O, loops, casting |
| [adventure-game](games/adventure-game/) | `dotnet run --file samples/games/adventure-game/adventure.lol` | Room-based text adventure |
| [arena-game](games/arena-game/) | `dotnet run --file samples/games/arena-game/Game.lol` | Turn-based RPG battle |
| [chess](games/chess/) | `dotnet run --file samples/games/chess/chess.lol` | Terminal chess against a one-ply material AI |
| [tic-tac-toe](games/tic-tac-toe/) | `dotnet run --file samples/games/tic-tac-toe/tic-tac-toe.lol` | One or two-player Tic-Tac-Toe with AI |

## Project-Based

The dedicated [project-based hello world](project-based/hello-world/) verifies
that traditional `.lolproj` applications remain supported:

```bash
dotnet run --project samples/project-based/hello-world/hello-world.lolproj
```

## Local Development

The `#:sdk Lolcode.NET.Sdk@0.2.0` directive restores the SDK's MSBuild props and
targets. `samples/Directory.Build.props` always redirects compiler execution to
the source-built `Lolcode.Build.dll`, so build the solution before running any
sample. Missing local compiler binaries are an error and never fall back to the
compiler contained in the package.
