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

# Known Unknowns

## Bootstrap scan limitations

- `unknown`: the user requested base branch `main`, but repository metadata reports default branch `bleed`; direct branch search did not find `main`. These files were indexed from `bleed`.
- `unknown`: the connector did not expose a full recursive directory listing, so this bootstrap used targeted source/config/doc inspection plus repository search.
- `unknown`: full inventory of all utility commands, traits, maps, and scripts was not enumerated.
- `unknown`: full issue and PR template inventory could not be listed through directory fetching. Direct checks for common template paths did not find files.

## Missing or not detected

- No `global.json` was found; .NET SDK selection is therefore not pinned by that file. .NET 8 is verified from `Directory.Build.props`, `INSTALL.md`, and CI.
- No `Dockerfile` or `docker-compose.yml` was found in checked root paths.
- No database migration tooling was detected.
- No formal threat model or dependency-vulnerability workflow was found in checked files.
- No prior AI-onboarding manifest/index files were found.
- No generated model-specific AI instruction files were detected by repository search or direct checks.

## Areas needing human review before risky edits

Ask a maintainer or inspect much more deeply before changing:

- network protocol compatibility or handshake semantics
- deterministic simulation, `ISync` state, shared RNG, or `World.SyncHash()` behavior
- replay recording/playback format
- dedicated server authentication and profile verification
- packaging/install destinations and release publishing behavior
- mod package load order and content installer behavior
- cross-platform build target/runtime behavior

## Potential documentation conflicts

- `README.md` points to the upstream project repository and wiki for many links. This fork is `Pummelchen/OpenRA`; do not assume fork-specific docs exist unless verified.
- The README says the wiki Hacking page is outdated. Treat current source/config as higher priority than older wiki material.

## Refresh notes

On a future refresh, compare `.ai/MANIFEST.json.indexed_commit` to the new head commit. If the old commit is unreachable, do a full rescan and record `full_rescan_due_to_missing_previous_commit` in the manifest and changelog.
