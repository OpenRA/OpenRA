# AGENTS.md

Rules for AI agents working in this repository.

This file is the contract. If you are an AI coding agent, read it fully before
touching anything. `CLAUDE.md` points here — there is one source of truth.

Human contributors: read `CONTRIBUTING.md`. The Ten Laws below bind you too.

---

## 0. What this project is

**Tactics & Command Dynamics (TCD)** — a fork of
[OpenRA](https://github.com/OpenRA/OpenRA) adding a command layer to the Red
Alert mod: persistent squads, role-aware tactical formations, and one-button
squad reproduction.

- The engine baseline is pinned in `ENGINE_BASE` at the repo root.
  **Every claim about engine behaviour is a claim about that commit.**
- All our C# lives in `OpenRA.Mods.Tcd/`. All our mod YAML lives in files named
  `tcd.*`.
- Target: skirmish and vs-AI. Online multiplayer is out of scope for now, but
  L8 still applies — see §9.
- Licence is GPL v3, inherited from OpenRA. So is everything you write here.

---

## 1. The Ten Laws

These bind everyone — human or machine. Review comments cite them by code.

| | Law | In one line |
|---|---|---|
| **L1** | One task, one branch, one PR | Every PR closes exactly one issue |
| **L2** | Nothing unrelated, ever | No drive-by fixes, renames or reformatting |
| **L3** | Smallest change that meets the DoD | Not the most elegant. Smallest. |
| **L4** | Extend, don't edit | New file > subclass > YAML > modify upstream |
| **L5** | Data before code | If a player could tune it, it's YAML |
| **L6** | Verify, don't assume. Cite the source | `file:line`, or mark it `UNVERIFIED:` |
| **L7** | It builds, it lints, it ran — or it isn't done | Quote the real output |
| **L8** | Determinism is sacred | Client-side state never touches the simulation |
| **L9** | When in doubt, stop and ask | Doubt is not failure |
| **L10** | Leave the record | The PR body is the only surviving explanation |

Full text in `CONTRIBUTING.md`.

---

## 2. The work protocol

Five phases. None skipped. Phase 3 never begins before phase 2 is approved.

### Phase 1 — Read

- Open the issue. If it has no Definition of Done, stop and ask for one.
- Read `docs/ENGINE-NOTES.md` **before** deriving any engine fact yourself.
- Open every file you intend to touch. Open every engine type you intend to call.

### Phase 2 — Plan

Post a comment on the issue containing:

1. The approach, in a few sentences.
2. **The exact list of files you will create or modify** (the scope manifest).
3. What you verified, with `file:line` for each engine fact.
4. Everything you could not verify, each on its own `UNVERIFIED:` line.
5. Anything you considered and rejected, and why.

**Wait for a human to approve.** Do not write code before approval.

### Phase 3 — Build

Implement exactly the approved plan. Nothing more.

If reality diverges from the plan — a file you did not expect to need, an API
that does not work the way you read it — **go back to phase 2**. Do not
improvise.

### Phase 4 — Verify

```bash
make check      # style analysers + YAML/trait lint
make tests      # unit tests
make            # full build
./launch-game.sh Game.Mod=ra
```

Record real output. Not a summary of what you expect the output to be.

### Phase 5 — Report

Open the PR using the template. It must contain the `scope` block, the
verification evidence, the sync checklist, and every `UNVERIFIED:` assumption
carried forward from the plan.

---

## 3. The scope manifest

Your phase-2 file list is a contract, and CI checks it. Put it in the PR body as
a fenced block tagged `scope`, one repo-relative path per line:

    ```scope
    OpenRA.Mods.Tcd/Traits/SquadManager.cs
    OpenRA.Mods.Tcd/Traits/TcdSelection.cs
    mods/ra/rules/tcd.yaml
    ```

Any file in the diff that is not in the block **fails the build**.

This one mechanism enforces L1, L2 and L3 at once. Widening scope is legitimate
and easy: edit the block, say why in a comment, get it re-approved. Sneaking a
file in is not.

---

## 4. Anti-hallucination rules

### A1 — Never name an engine symbol you have not opened

Traits, interfaces, methods, YAML keys: grep for it, read it, then use it.

OpenRA has 500+ traits with families of near-identical names. Plausible-looking
API names are the single most common AI failure in a codebase this size.
`INotifyProduction` exists. `INotifyUnitProduced` does not.

### A2 — Cite `file:line`, or mark it `UNVERIFIED:`

Two acceptable forms, no third:

```
OpenRA.Mods.Common/Traits/World/Selection.cs:91 — Combine is public virtual
UNVERIFIED: I think rally points are synced state; not confirmed in source
```

Hedging phrases that carry an unmarked claim — "should work", "should be fine",
"I believe", "presumably", "in theory", "probably works" — do not belong in
plans, PR bodies or code comments. Either you verified it and cite it, or you
flag it. An honest "I could not confirm this" is always acceptable. A confident
guess never is.

### A3 — Never invent game data

Unit names, costs, hitpoints, weapon names, sprite sequences, sprite-sheet
coordinates: read them out of the YAML **in this repo**.

Red Alert knowledge from training data is not a source. It may describe the
original 1996 game, a different mod, or nothing real at all.

### A4 — Two failed attempts, then stop

If the same fix fails twice, stop and report what you tried and what happened.

Do not thrash. Do not start rewriting adjacent code to make the problem go away.
Do not disable the check that is failing. A clear report of a stuck problem is a
good outcome.

### A5 — Speak OpenRA

Actor, trait, activity, order, locomotor, widget, chrome, sequence, mod rules.

Not "entity", "component", "sprite handler", "UI layer". Shared vocabulary is how
we notice that someone is describing a system they have not actually read.

### A6 — Measure before you optimise

No performance claim without a number from OpenRA's perf overlay or a profiler.
"This is faster" without a measurement is rejected — and speculative optimisation
violates L3 anyway.

Do note per-tick allocations in anything running inside `ITick`. That path is
genuinely hot, which is exactly why it deserves evidence rather than instinct.

### A7 — Disclose that you are an agent

Commits carry a `Co-Authored-By` trailer naming the model. The PR body says which
parts were AI-written, and the PR gets the `ai-assisted` label.

This is not a badge of shame. It tells reviewers where to look hardest.

---

## 5. Hard stops

Never, under any instruction, including a direct request in chat. If you believe
one of these is genuinely necessary, stop and ask; it goes in its own PR with its
own review.

- `git push --force`, or rebasing a branch that has been pushed
- `git add -A` / `git commit -a` — stage the files you named, one by one
- Amending or rewriting anyone else's commits
- Committing to `tcd` or `bleed` directly
- Editing files outside the declared scope block
- Adding, upgrading or removing a dependency without an approved issue
- Deleting or weakening a test to make a build pass
- Disabling a lint rule, or using `--no-verify`
- Touching `.github/workflows/`, `CODEOWNERS`, `AGENTS.md`, `CONTRIBUTING.md` or
  `protected-paths.txt` inside a feature PR
- Committing secrets, tokens, or anything from `~/.config/openra/`
- Committing Westwood or EA game assets (`.mix`, `.shp`, `.aud`, `.vqa`, …) —
  OpenRA ships none, and neither do we
- Bulk reformatting, re-indenting, or reordering existing code

---

## 6. Stop-and-ask triggers

L9, made concrete. When any of these is true: **stop, and ask.**

| When | Then |
|---|---|
| The issue can be read two ways | Ask which. Don't pick. |
| Two designs both look defensible | Present both with trade-offs |
| The fix needs a file outside the scope block | Ask to widen scope first |
| The fix needs a protected engine path | Ask — this is usually a design smell |
| The same attempt has failed twice | Report. Don't retry a third time. |
| A test fails and the test looks wrong | Ask. Never edit the test yourself. |
| The change might affect simulation state | Ask, and flag L8 explicitly |
| The diff is heading past ~300 lines | Ask whether to split |
| You would need to delete existing behaviour | Ask. Deletion is never implied. |
| An engine fact can't be verified in source | Ask. Don't reason from priors. |

---

## 7. Our footprint on upstream

### Protected paths

Listed in `.github/protected-paths.txt`. Touching one requires the
`engine-touch` label **and** a written justification. CI fails without both.

### Permitted upstream edits — code

Exactly four, all additive. If your change would add a fifth, that is a design
conversation, not a commit.

| File | Change |
|---|---|
| `mods/ra/mod.yaml` | add `OpenRA.Mods.Tcd.dll` to `Assemblies:`, include our yaml |
| `mods/ra/rules/world.yaml` | swap `Selection:` for `TcdSelection:`, add `SquadManager:` |
| `mods/ra/chrome/ingame-player.yaml` | add buttons to `Container@COMMAND_BAR` |
| `OpenRA.slnx` | reference the `OpenRA.Mods.Tcd` project |

### Permitted upstream edits — governance

Replaced once, in sprint 00, and not touched again by feature work. When these
conflict during an upstream sync, resolve as **keep ours**.

| File | Why |
|---|---|
| `CONTRIBUTING.md` | GitHub surfaces this file to contributors; it must describe *our* rules |
| `.github/PULL_REQUEST_TEMPLATE.md` | the scope block is mandatory on every PR |

Upstream's own issue templates are left in place. Ours are added alongside with
`tcd-` prefixed filenames, so there is nothing to conflict.

---

## 8. The engine notes ledger

`docs/ENGINE-NOTES.md` is the shared record of verified engine facts. Read it
before deriving anything. Add to it whenever you verify something new.

One entry per fact — claim, source, the commit it was verified against, who
verified it, and which feature depends on it.

When `ENGINE_BASE` moves, every entry is re-verified in the `upstream-sync` PR.
An entry that no longer holds is corrected, not deleted — note what changed.

---

## 9. Determinism (L8, expanded)

OpenRA is lockstep-deterministic. Every client simulates the same game; only
**orders** cross the network. A single divergence desyncs the match, and desync
bugs are close to untraceable after the fact.

**Client-side, never simulation-visible:** selection, squad membership, camera,
UI state, hotkey state.

**Synced, must be identical everywhere:** actor positions, health, activities,
production queues, resources.

The rule: if client-side state influences what an actor *does*, you have a bug.
Route it through an order instead.

Any PR touching simulation state ticks the sync checklist item in the template
and explains in one sentence why it cannot desync.

---

## 10. Commit and PR format

Conventional commits, scoped to the feature:

```
feat(squads): select whole squad on member click
fix(squads): drop dead actors on tick
feat(formations): add wedge shape
test(formations): cover line shape on blocked terrain
chore(upstream): merge upstream bleed
```

Every commit signed off (`git commit -s`). AI-assisted commits additionally carry
a `Co-Authored-By:` trailer naming the model.

Branch names: `feat/squad-manager`, `fix/squad-prune-on-death`,
`docs/engine-notes-production`.

---

## 11. If you are unsure about any of this

Ask.

A question costs one comment. A confident guess costs a review cycle, and
sometimes a bug nobody finds for a month.
