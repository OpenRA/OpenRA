# PvPHit engine fork

Branch `pvphit-release-20250330` is cut from the upstream tag `release-20250330` and
carries the PvPHit mod in-tree at `mods/pvphit`. Client, dedicated server and utility
are all built from this branch; newer upstream engine code is folded in only by a
deliberate rebase of the branch, never by building from `bleed`. The identity and
result contract the mod implements lives in
[pabl-o-ce/pvphit `docs/openra/`](https://github.com/pabl-o-ce/pvphit/tree/main/docs/openra).

## What differs from stock `release-20250330`

| Change | Where | Why |
| --- | --- | --- |
| In-game account login (username + password → key link) | cherry-pick of upstream `1fce287392`, adapted into a new **Settings → Account** tab (`mods/{common,cnc}/chrome/settings-account.yaml`, `AccountSettingsLogic.cs`) | the tag could only link a key by pasting it on a website; PvPHit needs `POST {Link}` |
| In-game unlink revokes the key on PvPHit | `PlayerDatabase.Unlink` (new, empty for stock mods), `LocalPlayerProfile.UnlinkAccount()` — deletes `player.oraid`, then posts `fingerprint`, `timestamp` and `Sign("unlink", fingerprint, timestamp)` to `{Unlink}` fire-and-forget (result in the `debug` log as `Unlink <fp>: HTTP <code> <body>`); both "Unlink Account" (`AccountSettingsLogic.cs`) and the lobby "Destroy key" (`LocalProfileLogic.cs`) use it | at the tag an unlink only deleted the local keypair, so every unlink/reinstall left an active key on the service until the 5-key cap locked the player out (`Error: too many keys`). Contract: pvphit `docs/openra/profile-service.md`, `POST {Unlink}` |
| `mods/pvphit` + `mods/pvphit-content` | `pvphit` inherits every `ra\|…` package; only identity differs (`Metadata`, `PlayerDatabase`, per-mod user map folder). `pvphit-content` is its content installer: a copy of `ra-content`'s manifest that mounts the `ra-content` installer data and whose `ModContent.Mod` is `pvphit`, so installing (or cancelling) returns to PvPHit instead of stock Red Alert. No `DiscordService` until a PvPHit Discord application exists | gameplay stays byte-identical to Red Alert (O1 baseline) |
| Separate support directory for the pvphit client | `launch-game.sh`, `packaging/linux/openra.appimage.in` create `$XDG_CONFIG_HOME/openra-pvphit/` (`mkdir -p`; the engine aborts on a missing support dir) and prepend `Engine.SupportDir=` pointing at it when `Game.Mod=pvphit` and no support dir is given; the crash dialog then points at its `Logs` | `Settings.Game.AuthProfile` is a user setting persisted to the shared `settings.yaml`; a separate dir keeps the PvPHit key (`player.oraid`), settings, logs and replays apart from the stock forum identity |
| Packaging | `packaging/functions.sh` (`install_data pvphit` ships `mods/pvphit` + `mods/pvphit-content` + `mods/ra` + `mods/ra-content` + `mods/common-content`), `packaging/linux/buildpackage.sh` (`pvphit*` tags build `OpenRA-PvPHit-x86_64.AppImage` with no `-devel` suffix and no OpenRA update channel/zsync; no Discord URI handler; ra artwork reused until O7), `Makefile test` checks `pvphit` MiniYAML (content installer mods are not lint targets, as upstream) | one artifact contains client, server and utility |

`mods/pvphit/mod.yaml` points `PlayerDatabase` at:

```
PlayerDatabase:
	Profile: https://pvphit.com/api/openra/info/
	Link: https://pvphit.com/api/openra/link
	Unlink: https://pvphit.com/api/openra/unlink
	Forum: https://pvphit.com/
```

## Artifact contract (consumed by pvphit-game)

| Item | Value |
| --- | --- |
| Release tag / engine version / mod version | `pvphit-20250330.N` (same string for all three; stamped by `make version VERSION=<tag>` — `VERSION` file and every `mods/*/mod.yaml`) |
| Mod id | `pvphit` |
| Linux artifact | `OpenRA-PvPHit-x86_64.AppImage` + `OpenRA-PvPHit-x86_64.AppImage.sha256` on the fork's GitHub release for the tag |
| Inside the AppImage | `usr/lib/openra/{OpenRA,OpenRA.Server,OpenRA.Utility}` and `mods/{pvphit,pvphit-content,ra,common,common-content,ra-content}`; `--appimage-extract` works without FUSE (pvphit-game's Dockerfile does this) |
| Server launch | `OpenRA.Server Game.Mod=pvphit Server.Name=… Server.ListenPort=… Server.Password=… Server.AdvertiseOnline=False Server.RecordReplays=True Server.RequireAuthentication=True Server.ProfileIDWhitelist=<id1>,<id2> [Server.Map=<uid>] Engine.SupportDir=<dir>` (same argument set pvphit-game passes today for `ra`) |
| Client launch | `openra-pvphit` (AppImage) or `./launch-game.sh Game.Mod=pvphit`; both create and apply the separate support dir (`~/.config/openra-pvphit/`); an explicit `Engine.SupportDir=` argument wins |
| Compatibility | a client joins only a server with the same engine version, mod id and mod version; a stock `ra` client is rejected with the usual mod/version mismatch |
| Supported targets | Linux x86_64 AppImage now; Windows and macOS packaging follow the stock `packaging/` scripts and are follow-ups |

pvphit-game pins the artifact in three places that move together: `docker/openra/Dockerfile`
(tag + sha256, `ORA_MOD=pvphit`), `packer/openra-snapshot.pkr.hcl`, and
`games.scoring_rules.release` (surfaced to players as `game_version`).

## Building from a clean checkout

```sh
git clone -b pvphit-release-20250330 git@github.com:pabl-o-ce/OpenRA.git
cd OpenRA
# .NET 6 SDK (the tag targets net6.0). With only a newer SDK installed:
export DOTNET_ROLL_FORWARD=Major
make all                        # bin/OpenRA.dll, OpenRA.Server.dll, OpenRA.Utility.dll
make test                       # --check-yaml for ts, d2k, cnc, ra, pvphit
./launch-game.sh Game.Mod=pvphit # creates ~/.config/openra-pvphit/ on first run
# launch-dedicated.sh reads its settings from environment variables only (it ignores
# positional arguments), so pass them in the environment:
Mod=pvphit ListenPort=1234 RequireAuthentication=True ProfileIDWhitelist=100000 SupportDir=~/.config/openra-pvphit/ ./launch-dedicated.sh
```

Pass `Server.*`/`Game.Mod=`/`Engine.SupportDir=` directly to `bin/OpenRA.Server.dll` instead
(the form pvphit-game uses, see the server launch row above) when you need a setting the
script does not expose.

Release build (what the GitHub release contains):

```sh
TAG=pvphit-20250330.1
make version VERSION=$TAG        # stamps VERSION + mods/*/mod.yaml
mkdir -p /tmp/openra-out
packaging/linux/buildpackage.sh $TAG /tmp/openra-out
sha256sum /tmp/openra-out/OpenRA-PvPHit-x86_64.AppImage > /tmp/openra-out/OpenRA-PvPHit-x86_64.AppImage.sha256
```

`buildpackage.sh` treats every tag starting with `pvphit` as a release build: the AppImage
name carries no `-devel`/`-playtest` suffix (so it matches the artifact contract above) and
no OpenRA update channel or `.zsync` file is produced, because PvPHit releases are not
served by `master.openra.net`. Stock `release*`/`playtest*`/`pkgtest*` tags are unchanged.
The script still builds the stock ra/cnc/d2k AppImages alongside `OpenRA-PvPHit-x86_64.AppImage`.

Point the mod at a local profile service for testing by editing the four
`PlayerDatabase` URLs in `mods/pvphit/mod.yaml` (e.g. `http://localhost:5000/api/openra/…`).

## Keeping stock OpenRA usable

The stock `ra` mod is untouched apart from the Account settings tab and still links to
forum.openra.net through its default `PlayerDatabase`. Because the pvphit client uses its
own support directory, switching between the two never deletes or replaces the other's key.
The pvphit AppImage registers only its own `openra-pvphit-<tag>://` URI scheme; it does not
claim Red Alert's `discord-699222659766026240://` handler, so the two AppImages can be
integrated on the same desktop without fighting over Discord invites.
