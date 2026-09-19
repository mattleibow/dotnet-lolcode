# Sample gallery

The repository samples are runnable documentation. Each file-based sample has
a `#:sdk` directive and can be launched with `dotnet run --file <path>` after a
source build.

| Goal | Sample |
| --- | --- |
| First output, variables, math, casts, control flow | [hello world](../../../samples/basics/hello-world/hello.lol) |
| FizzBuzz, recursion, calculator, file I/O | [FizzBuzz](../../../samples/programs/fizzbuzz/fizzbuzz.lol) |
| Guessing, adventure, arena, chess, tic-tac-toe | [adventure](../../../samples/games/adventure-game/adventure.lol) |
| Traditional SDK project | [Program.lol](../../../samples/project-based/hello-world/Program.lol) |

`file-io`, `stack`, and `truth-machine` use behavior associated with the pinned
future reference. See [versions](versions.md) before using those as a 1.2-only
lesson. The samples README records provenance for community adaptations and
their compatibility notes.
