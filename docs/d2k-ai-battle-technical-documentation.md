# D2K AI Battle - Technical Documentation

## Architecture

```
MainMenuLogic.cs              →  AI Battle button → opens setup panel
    ↓
AIBattleLogic.cs              →  Setup screen (map, factions, teams, speed)
    ↓
Game.cs + AIBattleState.cs    →  Speed control (1x-128x), pause, multi-tick
    ↓
AIBattleObserverLogic.cs      →  Live observer UI with speed buttons
    ↓
AIBattleResultsLogic.cs       →  Post-battle stats (winner, kills, damage)
    ↓
AIBattleReplayLogic.cs        →  Replay viewer with timeline scrubber
```

## Key Files

| File | What It Does |
|------|--------------|
| `AIBattleState.cs` | Global state: `IsAIBattle`, `SpeedMultiplier`, `IsPaused`, timestep math |
| `AIBattleLogic.cs` | Setup panel: map picker, AI slot config, game options |
| `AIBattleObserverLogic.cs` | Speed control buttons during live battle |
| `AIBattleReplayLogic.cs` | Timeline scrubber, rewind via restart-and-fast-forward |
| `AIBattleResultsLogic.cs` | Winner display, per-player stats table |
| `TimelineScrubberWidget.cs` | Draggable progress bar for replay seeking |
| `Game.cs:820-870` | Multi-tick loop for >32x speeds |

## Speed System

```
SpeedMultiplier: 1, 2, 4, 8, 16, 32, 64, 128

Timestep = BaseTimestep / SpeedMultiplier  (min 1ms)
TicksPerFrame = 1 for ≤32x, else SpeedMultiplier/32

Example: 128x with 40ms base
  → Timestep = 1ms (floor), TicksPerFrame = 4
  → Achieves 128x total speedup
```

## Rewind Mechanism

```
Seek backward requested
    ↓
Store: targetTick, renderPlayer
    ↓
Restart replay via Game.JoinReplay()
    ↓
Fast-forward at max speed to targetTick
    ↓
Restore renderPlayer, resume normal playback
```

## Chrome Files

| File | Purpose |
|------|---------|
| `aibattle.yaml` | Setup panel (map preview, AI slots, options) |
| `aibattle-results.yaml` | Results screen layout |
| `ingame-observer.yaml` | Speed control buttons container |
| `ingame-aibattle-replay.yaml` | Timeline + shroud toggle overlay |

## Game Options

| Option | Default | Effect |
|--------|---------|--------|
| Explored Map | Off | Start with map revealed |
| Fog of War | On | Hide enemy units out of view |
| Starting Cash | 5000 | Per-player credits |
| Game Speed | default | Base tick rate |
| Sim Speed | 4x | Initial playback multiplier |

## Decision Log

| Decision | Why |
|----------|-----|
| Static `AIBattleState` class | Global access from Game loop, no dependency injection needed |
| Rewind = restart + fast-forward | Avoids complex state serialization; replays already support restart |
| 128x max speed | Beyond this, rendering bottlenecks; diminishing returns |
| Multi-tick at >32x | 1ms is minimum timestep; need multiple ticks to go faster |
| Results stored in `AIBattleManager` | Persists across game→shell transition |

## Testing

1. `make all && ./OpenRA.exe Game.Mod=d2k`
2. Singleplayer → AI Battle
3. Configure map + 2+ AI players → Start Battle
4. Verify: speed buttons work, pause works, battle ends with results
5. Click "Watch Replay" → verify timeline scrubbing, rewind works
