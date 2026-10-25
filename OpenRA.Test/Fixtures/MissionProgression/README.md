# Two-mission progression fixture

This optional integration fixture uses stock TS terrain and an isolated Support
folder. It does not install maps into the normal mission browser or write to the
installed game content. Build the engine and provide installed TS content first.

```sh
python3 OpenRA.Test/Fixtures/MissionProgression/prepare.py \
  --engine "$PWD" --content /path/to/Support/Content \
  --out /tmp/openra-progression/Support --case direct

dotnet bin/OpenRA.dll Engine.EngineDir="$PWD" \
  Engine.SupportDir=/tmp/openra-progression/Support \
  Game.Mod=ts Launch.Map=progression-first
```

The output directory must not already exist. The first map completes its objective
automatically. Click Continue in the results dialog: Lua requests the second map,
returns, and the engine launches it using a local server. The second map remains
playable and logs PROGRESSION-SECOND-TICKING. Its difficulty is `hard`, inherited
from the first map despite its own `normal` default.

The fixture obtains the second map UID using `OpenRA.Utility --map-hash`; it does
not hardcode an installation path or a content-dependent UID into source files.
`Map.StartMission` takes a UID, not a map directory or display title. It returns
whether the request was queued. A queued launch is deferred until the current Lua
call returns. The script must handle false, for example by keeping its selection
UI open or showing an error message.

Additional cases:

- `movie --movie /path/to/installed.vqa`: plays that real VQA after results and
  starts the next map on natural completion. The preparer creates a local content
  overlay with symlinks to installed assets and a private copy of the movie.
  `Media.PlayMovieFullscreen` reads mod-mounted files, not the map package.
- `missing`: rejects unknown/empty UIDs and keeps the current ended world.
- `loss`: fails the first objective; Lua receives false and does not advance.
- `restart`: the same defeat fixture, for testing the normal Menu → Restart path.
- `locked`: the second map locks difficulty to normal; it must retain that value.
- `unsupported`: the second map does not offer hard; it must use normal.

Every case logs a rejected launch request made while the first mission is still
running. Successful progression cases request the same map twice in one callback:
the first request should return true, the second false. PROGRESSION-RETURNED must
precede PROGRESSION-SECOND, and UNEXPECTED-FIRST-TIMER must never appear.

The fixture does not implement a campaign store, mission-selection browser, or a
save-game schema. It is not part of the ordinary headless NUnit suite. The source
maps remain in the isolated Support folder so they can be inspected and linted
with `dotnet bin/OpenRA.Utility.dll ts --check-yaml MAP_DIRECTORY`.
