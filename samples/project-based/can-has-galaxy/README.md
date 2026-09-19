# CAN HAS GALAXY?

`CAN HAS GALAXY?` is a persistent terminal space-trading/exploration roguelike
written entirely in LOLCODE 1.4. It deliberately uses a project graph rather
than a monolithic source file, demonstrating the LOLCODE-to-LOLCODE class
library support introduced by the project-reference work.

```bash
dotnet build dotnet-lolcode.slnx
dotnet run --project samples/project-based/can-has-galaxy/Galaxy.Game
dotnet run --project samples/project-based/can-has-galaxy/Galaxy.Simulation
```

## Project graph

```text
Galaxy.Collections ─┬─> Galaxy.World ─> Galaxy.Story ─> Galaxy.Engine
                    │                                      │
                    └──────────────────────────────> Galaxy.Persistence
Galaxy.Engine ───────────────────────────────────────> Galaxy.Game
Galaxy.Persistence ──────────────────────────────────> Galaxy.Game
Galaxy.Engine ───────────────────────────────────────> Galaxy.Simulation
```

| Project | Role |
| --- | --- |
| `Galaxy.Collections` | Reusable dynamic list, pair, and character codec helpers. |
| `Galaxy.World` | Ship/player, sector, and market data factories. |
| `Galaxy.Story` | Deterministic sector names, encounters, and mission text. |
| `Galaxy.Engine` | Turns, travel, mining, trading, combat, mission progress, map/status views, and the seeded headless scenario. |
| `Galaxy.Persistence` | Versioned save/load format backed by `STDIO`. |
| `Galaxy.Game` | EOF-safe interactive terminal game. |
| `Galaxy.Simulation` | Stable batch run sharing the real engine. |

Each `.lolproj` deliberately contains one cohesive `.lol` source file. Multi-file
LOLCODE compilation is deferred, so project references are the clean,
compiler-supported composition boundary rather than an invented include system.

## Playing

Start as a small freighter in sector 0. Travel costs fuel; mine ore, sell it at
the current market, buy fuel, and fight encounters for bounties. Reach sector 3
and win a fight to recover the Starfish Relic.

Commands are case-sensitive:

`STATUS`, `MAP`, `TRAVEL1`, `TRAVEL2`, `TRAVEL3`, `MINE`, `SELL`, `FUEL`,
`FIGHT`, `MISSION`, `SAVE`, `LOAD`, and `QUIT`.

An empty line or EOF exits safely, which makes the game appropriate for the
generic executable sample runner. `SAVE` writes `can-has-galaxy.save` in the
working directory; `LOAD` rejects missing or invalid files without ending the
game.

## Save format

The small explicit text format is intentionally inspectable:

```text
CHG2
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

`CHG2` is the format/version marker. It preserves every value that affects
future simulation, so a loaded game continues deterministically rather than
starting a new timeline with similar ship statistics. The pure-LOLCODE codec
walks the newline-delimited fields with `STRING` byte indexing, validates the
header and field ranges, and rejects malformed state without a hidden managed
serializer.

## LOLCODE and runtime features

The sample uses `HAI 1.4`, `CAN HAS` imports across LOLCODE project references,
BUKKIT factories, dynamic `SRS` slots, object methods with `ME`, prototype
inheritance through `LIEK A`, first-class function values (list methods stored
on a prototype), `STDLIB` seeded randomness, `STRING` byte indexing, and
`STDIO` persistence. The list factory copies its `ADD` method into an `append`
slot as a first-class function value before Engine invokes it. No C# gameplay
or game-data helper is involved: the .NET projects only compile and host the
LOLCODE libraries.
