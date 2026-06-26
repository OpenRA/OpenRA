<!--
AI onboarding file.
Mode: bootstrap
Indexed commit: b0b0544d4adae970fc747e9b01241ef6e80032fc
Last generated: 2026-06-26T11:08:43Z
Generator: generic high-end AI coding agent
Purpose: Help future AI sessions understand this repository quickly.
Audience: Any high-capability AI coding agent, regardless of vendor or model family.
Human edits are allowed. Future refreshes should preserve valid human edits.
-->

# Security Notes

## Security-relevant surfaces

| Surface | Why it matters | Evidence |
|---|---|---|
| Dedicated server settings | Passwords, bans, auth requirements, profile whitelist/blacklist, IP-sharing, flood limits, vote kick. | `OpenRA.Game/Settings.cs` |
| Server handshake/auth | Mod/version/protocol checks, password validation, IP bans, profile signature validation. | `OpenRA.Game/Server/Server.cs` |
| Network transport | TCP connect/read/write, order packets, sync packets, replay recording. | `OpenRA.Game/Network/Connection.cs` |
| Deterministic simulation | Desyncs can break multiplayer/replay correctness. | `OpenRA.Game/Network/OrderManager.cs`, `OpenRA.Game/World.cs` |
| External downloads | GeoIP database, NuGet packages, forum profile/badge data, original game content packages. | `Makefile`, `make.ps1`, `OpenRA.Game/PlayerDatabase.cs`, `mods/ra/mod.yaml` |
| Packaging/install | Scripts write install destinations and publish self-contained binaries. | `packaging/functions.sh` |

## Auth and admission model

`verified`: server settings include a server password, IP ban set, `RequireAuthentication`, profile ID whitelist/blacklist, IP anonymization sharing, GeoIP country sharing, flood limits, and vote-kick settings.

`verified`: server validation rejects clients for game-started state, missing/incorrect password, incompatible mod/version/order protocol, banned IPs, missing required authentication, blacklist membership, and whitelist absence.

`verified`: authenticated profile validation fetches player profile data, decodes a public key, checks revocation, and verifies a signature over a per-connection random token.

## Secrets and private data

Potentially sensitive runtime data includes:

- server passwords and settings files in support directories
- auth signatures, fingerprints, profile IDs, and public keys
- client IP addresses, anonymized IPs, GeoIP-derived countries, and ban lists
- replay files and logs
- downloaded game assets and user maps in support directories

Do not commit support-directory data, logs, private server settings, credentials, or downloaded assets.

## AI-agent rules

- Never add access tokens, passwords, private keys, API keys, auth signatures, or real user IPs to repository files.
- Do not weaken authentication, version/protocol checks, flood limits, ban/whitelist checks, or signature validation without an explicit task and tests/review.
- Treat network packet parsing and order processing as adversarial input surfaces.
- Preserve deterministic simulation. Avoid time, randomness, thread ordering, file I/O, or network I/O in synced game logic unless the source explicitly shows it is safe.
- Be cautious with external URL construction and downloaded content parsing.
- Do not run install/publish/deploy commands unless the user explicitly requests them and paths are safe.

## Source-grounded security review checklist

For server/network changes, inspect:

- `OpenRA.Game/Settings.cs`
- `OpenRA.Game/Server/Server.cs`
- `OpenRA.Game/Network/Connection.cs`
- `OpenRA.Game/Network/OrderManager.cs`
- `OpenRA.Game/World.cs`

For mod/content loading changes, inspect:

- `mods/*/mod.yaml`
- relevant `mods/*/rules/`, `mods/*/chrome/`, `mods/*/scripts/`
- utility validation commands in `OpenRA.Utility` and `OpenRA.Mods.Common`

For packaging changes, inspect:

- `packaging/functions.sh`
- `Makefile`
- `make.ps1`
- `.github/workflows/ci.yml`

## Unknowns

- No formal threat model document was found in the checked files.
- No dependency vulnerability scanning workflow was detected in `.github/workflows/ci.yml`.
- No container hardening or deployment configuration was found in checked root paths.
