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
| `Lolcode.GameEngine` | Feature folders isolate collection primitives, command routing, and deterministic numeric helpers. It contains neither Galaxy rules nor rendering. |
| `Lolcode.TerminalUi` | Feature folders isolate text formatting, retained views, layouts, controls, rendering, and legacy widgets. |
| `CanHasGalaxy` | Domain feature folders isolate model, navigation, economy, combat, story, and persistence parsing/validation/save/load. `STDIO` handles remain inside save/load functions. |
| `CanHasGalaxy.Cli` | Feature folders separate application startup/registration from navigation, economy, combat, story, persistence, and lifecycle commands. |
| `Kitteh.Catacombs` | A compact deterministic crawler split into application, domain, presentation, and command features while reusing the same engine and UI. |

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
| `Lolcode.GameEngine` | `Collections/List`, `Collections/Pair`, `Commands/Router`, `Determinism/Numbers` |
| `Lolcode.TerminalUi` | `Text/Formatting`, `Core/View`, `Layout`, `Controls`, `Rendering`, `Compatibility/LegacyWidgets` |
| `CanHasGalaxy` | `Domain/{Model,Navigation,Economy,Combat,Story}`, `Infrastructure/Persistence`, `Presentation` |
| `CanHasGalaxy.Cli` | `Application/{Registration,GameLoop}`, `Commands/{Navigation,Economy,Combat,Story,Persistence,Lifecycle}` |
| `Kitteh.Catacombs` | `Application/GameLoop`, `Domain/Rooms`, `Presentation/Dashboard`, `Commands/{Adventure,Lifecycle}` |

Direct top-level functions are hoisted compilation-wide, so cross-file calls
and first-class handler references do not depend on source order. The libraries
avoid order-sensitive top-level mutable initialization. Executable imports are
function-local, so either source file may be compiled first even though
`Application/GameLoop.lol` performs the final top-level `RUN` call. This keeps
the files cohesive without numeric prefixes, tiny projects, or god modules.

## Retained terminal UI

`Lolcode.TerminalUi` is a pure-LOLCODE retained view framework. `NEWVIEW`
creates a BUKKIT with `width`, `count`, `ADDLINE`, and `GETLINE`; every line is
stored under a dynamic SRS slot after `FIT` truncates and pads it. `FRAME`,
`HSTACK`, and `VSTACK` create new views, while `BANNER`, `MESSAGE`, and
`PROGRESS` provide reusable dashboard pieces. `TOTEXT` serializes a finished
view and `PRESENT` prints precisely that one complete snapshot.

The apps target a 78-column screen. Content is ASCII-only before composition:
the STRING provider counts UTF-8 bytes, so Unicode box characters are inserted
only after fixed-width fitting. This keeps `┌─┐` frames visually stable without
using their byte lengths for layout.

Galaxy uses a banner, framed 44-column star-chart viewport, framed 24-column
ship sidebar, hull/fuel bars, mission/turn/cargo/credit data, communications
panel, command footer, and prompt. Catacombs reuses the exact composition model
for room art/description on the left and run/depth/status information on the
right; it does not import Galaxy.

`PRESENT` is deliberately scroll-safe: it never clears the terminal, so piped
input and captured tests show readable complete screens. `CLEAR` is available
as an opt-in ANSI clear/home helper for a future live redraw client, but neither
game requires it. Command handlers return communications messages instead of
printing fragments, and the owning application renders the next complete
screen before prompting.

## Playing Galaxy

Commands are case-sensitive:

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
engine's dynamic command registry and the retained TerminalUi compositors
without importing Galaxy.
