# Sample gallery

The repository samples are runnable documentation. Each file-based sample has
a `#:sdk` directive and can be launched with `dotnet run --file <path>` after a
source build. The complete source index is available in the
[repository](https://github.com/mattleibow/dotnet-lolcode/blob/main/samples/README.md).

## File-based apps

| Area | Samples |
| --- | --- |
| Basics | [hello world](../../../samples/basics/hello-world/hello.lol), [variables](../../../samples/basics/variables/variables.lol), [math](../../../samples/basics/math/math.lol), [conditionals](../../../samples/basics/conditionals/conditionals.lol), [loops](../../../samples/basics/loops/loops.lol), [functions](../../../samples/basics/functions/functions.lol), [string operations](../../../samples/basics/string-ops/strings.lol), [casting](../../../samples/basics/casting/casting.lol), [switch](../../../samples/basics/switch/switch.lol), [comments](../../../samples/basics/comments/comments.lol) |
| Programs | [FizzBuzz](../../../samples/programs/fizzbuzz/fizzbuzz.lol), [fibonacci](../../../samples/programs/fibonacci/fibonacci.lol), [recursion](../../../samples/programs/recursion/recursion.lol), [string calculator](../../../samples/programs/string-calculator/calculator.lol), [file I/O](../../../samples/programs/file-io/file-io.lol), [stack](../../../samples/programs/stack/stack.lol), [truth machine](../../../samples/programs/truth-machine/truth-machine.lol) |
| Games | [guessing game](../../../samples/games/guessing-game/guess.lol), [adventure](../../../samples/games/adventure-game/adventure.lol), [arena](../../../samples/games/arena-game/Game.lol), [chess](../../../samples/games/chess/chess.lol), [tic-tac-toe](../../../samples/games/tic-tac-toe/tic-tac-toe.lol) |

`file-io`, `stack`, and `truth-machine` exercise pinned future-reference
behavior. See [versions](versions.md) before using them as a 1.2-only lesson.

## Project scenarios

| Scenario | What it demonstrates |
| --- | --- |
| [Hello world](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/hello-world) | `Lolcode.NET.Sdk/0.2.0` project workflow |
| [C# head / LOLCODE library](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/csharp-head-lolcode-library) | C# constructs and disposes a generated LOLCODE library instance |
| [LOLCODE head / C# library](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/lolcode-head-csharp-library) | `CAN HAS` constructs an attributed C# library instance |
| [LOLCODE head / LOLCODE library](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples/project-based/lolcode-head-lolcode-library) | Generated-library import and explicit `Compile` order: `Welcome.lol`, then `Greeting.lol` |

The last three scenarios document current `0.3.0` source behavior and use the
repository's source-built compiler and runtime binaries. The simple hello-world
project and file-based samples retain the published `0.2.0` SDK pin so they
remain directly restorable from public feeds.
