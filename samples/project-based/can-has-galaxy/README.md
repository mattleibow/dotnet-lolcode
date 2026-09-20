# CAN HAS GALAXY?

`CAN HAS GALAXY?` is a persistent, line-oriented space-trading roguelike
written entirely in LOLCODE 1.4. It is organized as one realistic multi-file
application instead of seven artificial one-file projects. The same pure
LOLCODE framework projects power its smaller companion, `KITTEH CATACOMBS`.

```bash
dotnet build dotnet-lolcode.slnx --disable-build-servers -p:BuildInParallel=false
dotnet run --project CanHasGalaxy.Cli
dotnet run --project Kitteh.Catacombs
dotnet test ../../../tests/CanHasGalaxy.Tests/CanHasGalaxy.Tests.csproj
```

Run those commands from this directory. Both executables exit safely on an
empty line or EOF, so scripts can use pipes without a terminal emulator.

## Project graph

```text
Lolcode.GameEngine ─┬─> CanHasGalaxy ─> CanHasGalaxy.Cli
                    └──────────────────> Kitteh.Catacombs
Lolcode.TerminalUi ─┬─> CanHasGalaxy ─> CanHasGalaxy.Cli
                    └──────────────────> Kitteh.Catacombs
```

| Project | Purpose |
| --- | --- |
| `Lolcode.GameEngine` | Prototype-backed dynamic list, SRS/BUKKIT first-class-function command router, and deterministic clamp/seed helpers. It contains neither Galaxy rules nor rendering. |
| `Lolcode.TerminalUi` | Portable text helpers plus boxed headers/panels, menus, prompts, and status bars. It uses lines only—no cursor addressing or ANSI control sequences. |
| `CanHasGalaxy` | Galaxy model, navigation, economy, combat, story, primitive save/load codec, and views. `STDIO` handles remain inside save/load functions. |
| `CanHasGalaxy.Cli` | EOF-safe interactive shell that registers Galaxy command functions in the generic router and renders with TerminalUi. |
| `Kitteh.Catacombs` | A compact deterministic crawler that independently registers `LOOK`, `STEP`, and `QUIT` handlers in the same router and uses the same UI widgets. |

`tests/CanHasGalaxy.Tests` is the dedicated C# xUnit test project. It calls
the generated `CanHasGalaxy.GalaxyExports` and `TerminalUi.UiExports` wrappers,
tests persistence in isolated temporary directories, and runs both executable
games. Generic executable-sample discovery stays in `Lolcode.EndToEnd.Tests`;
detailed Galaxy behavior lives only in the dedicated tests.

## File organization and source order

Every `.lol` file is a complete `HAI 1.4` / `KTHXBYE` unit. The projects use
the SDK's normal `**/*.lol` glob, and filenames describe concepts rather than
encoding a compilation sequence:

| Project | Files |
| --- | --- |
| `Lolcode.GameEngine` | `Collections`, `Commands`, `Determinism` |
| `Lolcode.TerminalUi` | `Text`, `Widgets` |
| `CanHasGalaxy` | `Model`, `Navigation`, `Economy`, `Combat`, `Story`, `Persistence`, `Views` |
| Both executables | `Application`, `Commands` |

Direct top-level functions are hoisted compilation-wide, so cross-file calls
and first-class handler references do not depend on source order. The libraries
avoid order-sensitive top-level mutable initialization. Executable imports are
function-local, so either source file may be compiled first even though
`Application.lol` performs the final top-level `RUN` call. Its alphabetical
position deliberately exercises the formerly difficult entry-point-first
order. This keeps the files cohesive without numeric prefixes, tiny projects,
or god modules.

## Playing Galaxy

The startup header, boxed status rows, and `HULL`/`FUEL` bars are supplied by
`TerminalUi`. Commands are case-sensitive:

`STATUS`, `MAP`, `TRAVEL1`, `TRAVEL2`, `TRAVEL3`, `MINE`, `SELL`, `FUEL`,
`FIGHT`, `MISSION`, `SAVE`, `LOAD`, and `QUIT`.

Travel consumes fuel. Mine and sell ore for credits, buy fuel, then fight in
sector 3 to recover the Starfish Relic. The generic router stores each command
handler as a first-class LOLCODE function value in a dynamic SRS slot and
dispatches it with the shared mutable session BUKKIT.

`SAVE` writes `can-has-galaxy.save` in the working directory. The inspectable
format is newline-delimited:

```text
CHG3
<sector>
<credits>
<fuel>
<hull>
<ore>
<relic>
<turn>
<seed>
<omen>
```

`LOAD` validates the marker and every bounded numeric field before constructing
fresh game state. It never returns the live `STDIO` BLOB that was used to read
the file.

## Playing Kitteh Catacombs

`LOOK` describes the current room, `STEP` advances through a finite,
deterministic three-room crawl, and `QUIT` exits. Reaching the final room prints
`CATACOMBS COMPLETE.` This intentionally small game proves reuse by using the
engine's dynamic command registry and TerminalUi header/menu/panel widgets
without importing Galaxy.
