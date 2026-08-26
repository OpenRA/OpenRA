# Contributing to Tactics & Command Dynamics

**Tactics & Command Dynamics (TCD)** is a fork of
[OpenRA](https://github.com/OpenRA/OpenRA) that adds a command layer to the Red
Alert mod: persistent squads, role-aware tactical formations, and one-button
squad reproduction.

Contributions are welcome — from humans and from AI agents. Both are held to the
Ten Laws below. AI agents have an additional protocol in
[`AGENTS.md`](AGENTS.md); read that first if you are one.

> Looking for OpenRA's own contributing guide (for PRs to the upstream engine)?
> It is at <https://github.com/OpenRA/OpenRA/blob/bleed/CONTRIBUTING.md>. This
> file replaces it for work inside *this* fork.

---

## Quick start

```bash
git clone https://github.com/AbdullahZeynel/OpenRA-tcd.git
cd OpenRA-tcd
git switch tcd

# .NET 10 SDK required — see INSTALL.md
make
./launch-game.sh Game.Mod=ra
```

Before every push:

```bash
make check      # style analysers + YAML/trait lint
make tests      # unit tests
```

On x86_64 the native libraries (SDL2, OpenAL, FreeType, Lua) are downloaded
automatically via NuGet. You only need system packages if you build with
`make DEPENDENCIES=system`.

---

## How the project is organised

- Engine baseline pinned in `ENGINE_BASE`. Every claim about engine behaviour is
  a claim about that commit.
- Our C# lives in `OpenRA.Mods.Tcd/`. Our mod YAML lives in files named `tcd.*`.
- Verified engine facts live in [`docs/ENGINE-NOTES.md`](docs/ENGINE-NOTES.md).
- The build plan and sprint breakdown live in [`docs/PLAN.md`](docs/PLAN.md).
- Long-lived branches: `tcd` (default) and `bleed` (untouched upstream mirror).
  Everything else is short-lived: `feat/…`, `fix/…`, `docs/…`.

---

## The Ten Laws

Cite them by code in review comments: *"this violates L3"*.

### L1 — One task. One branch. One pull request.

Every PR closes exactly one issue. If you cannot write the PR title as a single
sentence without the word "and", it is two PRs. This is the rule that makes every
other rule checkable — a PR that does one thing can be reviewed, reverted, and
reasoned about; a PR that does five cannot.

### L2 — Nothing unrelated. Ever.

No drive-by fixes, no renames, no reformatting, no "while I was in there". If you
find a real bug outside your task, **open an issue and keep walking**. The only
exception is a change genuinely required to compile — say so explicitly in the PR
body, on its own line, starting with `REQUIRED:`.

### L3 — The smallest change that satisfies the Definition of Done.

Not the most elegant, not the most general, not the most future-proof. Smallest.
No abstraction introduced for a second caller that does not exist yet. No
configuration option nobody asked for. No helper class wrapping one method.

Soft diff budget: **300 changed lines**, excluding generated files and assets.
Over that, CI warns and you either split the PR or justify the size in the body.
A conversation trigger, not a hard block.

### L4 — Extend, don't edit.

Preference order, strongest first:

1. New file in `OpenRA.Mods.Tcd`
2. Subclass an engine type
3. Add a trait via YAML
4. Modify an upstream file

Go down that list only when the level above genuinely cannot work, and say which
ones you tried in the PR.

Paths listed in `.github/protected-paths.txt` require the `engine-touch` label
**and** a written justification. CI fails without both. Our permitted upstream
edits are enumerated in [`AGENTS.md` §7](AGENTS.md) — currently four code files.

### L5 — Data before code.

If a behaviour can be expressed as YAML on a trait, it is YAML. Unit roles,
formation spacing, squad size caps, key bindings, tooltips — all data. Hardcoded
lists of unit names in C# are a review rejection.

The test: could a player who does not write C# tune this? If yes, it belongs in
YAML.

### L6 — Verify, don't assume. Cite the source.

Every claim about engine behaviour must be backed by a real file and line at the
commit in `ENGINE_BASE`. Before you use any engine type, method or interface,
open it. Names here are easy to get *almost* right — `INotifyProduction` exists,
`INotifyUnitProduced` does not — and an almost-right name is a compile error at
best and a silent no-op at worst.

Record what you verify in `docs/ENGINE-NOTES.md` so the next person cites the
ledger instead of re-deriving it. Anything you could not verify goes in the PR
body on an `UNVERIFIED:` line. An honest "I could not confirm this" is always
acceptable; a confident guess never is.

### L7 — It builds, it lints, and it ran — or it isn't done.

No PR without `make check` and `make tests` passing locally, with the actual
output quoted in the body. OpenRA's lint catches bad YAML and trait misuse and it
is genuinely good — never work around it, never disable a rule to get green.

Gameplay changes additionally need evidence you played it: what map, what you
did, what happened. Pure logic — formation geometry, composition math — needs
unit tests in `OpenRA.Test`. "Should work" is not a test result.

### L8 — Determinism is sacred.

The simulation must evolve identically on every client from the same orders.
Client-side state — selection, squads, camera, UI — may never influence it. Any
PR touching simulation state ticks the sync checklist item in the template and
explains, in one sentence, why it cannot desync.

This is the only law with no "unless". A desync bug found three months later is
nearly impossible to trace back to the commit that caused it.

### L9 — When in doubt, stop and ask. Doubt is not failure.

Ambiguity in an issue, two defensible designs, a change that would exceed
declared scope, a second failed attempt at the same fix — all of these mean
**stop and ask**, not *pick one and hope*. Asking costs a comment. Guessing costs
a review cycle, and sometimes a subtle bug nobody finds for a month.

The explicit trigger list is in [`AGENTS.md` §6](AGENTS.md). It applies to humans
too.

### L10 — Leave the record.

The PR body says what changed, why, what you verified and how, and what you
deliberately did not do. Six weeks from now this is the only surviving
explanation, and it is how a stranger learns to contribute.

Every PR must be revertable with `git revert` alone. If undoing it needs manual
cleanup, restructure it.

---

## Rules for human contributors

### H1 — Issue first, code second.

No surprise PRs, however good. Open an issue, agree the approach, then build. A
rejected 400-line PR wastes your evening and the maintainer's goodwill — a worse
outcome for the project than the feature not existing.

### H2 — Match the house style.

OpenRA has an established style and ships analysers that enforce it. Do not bring
your own conventions, do not reformat what is there, and let `make check` settle
any argument about formatting.

### H3 — Sign off your commits, and mean it.

`git commit -s` adds a Developer Certificate of Origin sign-off, certifying you
wrote the code and can license it under GPL v3. If an AI wrote part of it, say so
(see A7 in `AGENTS.md`). If you copied it from another project, say where, and
check the licence is compatible.

### H4 — Never commit game assets.

No sprites, audio, video or map files originating from Command & Conquer, Red
Alert, or any Westwood/EA release. OpenRA ships none and downloads them from the
player's own copy. The same line applies here, and it is the line that keeps the
project legal.

### H5 — If it belongs upstream, send it upstream.

A genuine engine bug fix or a broadly useful trait should be a PR to OpenRA, not
a permanent patch in our fork. Every upstream file we carry is a file that will
conflict at every sync. Fewer is better, always.

For upstream PRs, follow OpenRA's own guidelines: rebase onto their latest
`bleed`, add yourself to `AUTHORS`, suggest a CHANGELOG entry, and discuss on
their Discord during review.

### H6 — Review like a colleague, not a gatekeeper.

Cite the law code, say what you would accept instead, and separate blocking
objections from preferences. The Code of Conduct applies to everyone, and
maintainers are held to it hardest. A contributor's first PR decides whether they
ever open a second.

---

## Opening a pull request

1. Comment on the issue with your approach and the exact list of files you will
   touch.
2. Wait for agreement.
3. Branch: `git switch -c feat/short-name`
4. Build it. Run `make check` and `make tests`.
5. Open the PR against `tcd` using the template. Fill in the `scope` block — CI
   compares it against your diff and fails on anything undeclared.
6. Expect review comments citing law codes. They are not personal.

Conventional commit messages, scoped to the feature:

```
feat(squads): select whole squad on member click
fix(formations): keep slots off impassable cells
docs(engine-notes): record production queue lookup
```

---

## Licence

GPL v3, inherited from OpenRA. By contributing you agree your work is licensed
under GPL v3 and that you have the right to license it. The repository stays
public.

The Code of Conduct in [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md) applies to all
project spaces.
