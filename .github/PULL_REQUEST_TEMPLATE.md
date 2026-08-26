<!--
  Read AGENTS.md (agents) or CONTRIBUTING.md (humans) before filling this in.
  Every section below is required. CI checks several of them.
-->

## What and why

Closes #

<!-- One paragraph. What changed, and why. If you need the word "and", it is probably two PRs (L1). -->

## Scope

<!--
  L1 / L2 / L3. List every file this PR creates or modifies, one repo-relative
  path per line, inside the fence. CI compares this against the actual diff and
  FAILS on anything undeclared.

  Widening scope is fine: edit this block and say why in a comment.
-->

```scope
```

## Verification (L7)

<!-- Paste real output. Not a description of expected output. -->

- [ ] `make check` passes
- [ ] `make tests` passes
- [ ] `make` builds

```
paste output here
```

**Played it:** <!-- which map, what you did, what happened. Delete if not a gameplay change. -->

**Unit tests:** <!-- which cases you added, or why none apply. -->

## Engine facts used (L6)

<!--
  Every engine claim this PR relies on, with file:line at the commit in
  ENGINE_BASE. Cite docs/ENGINE-NOTES.md where an entry already exists;
  add new entries for anything you verified yourself.
-->

-

## Unverified assumptions (A2)

<!-- One per line, each starting with UNVERIFIED:. Write "None." if there are none. -->

None.

## Determinism (L8)

- [ ] This PR does not touch simulation state
- [ ] This PR touches simulation state, and here is why it cannot desync:

<!-- explanation -->

## Deliberately not done

<!-- What you left out on purpose, and where it is tracked. Prevents "why didn't you also…" in review. -->

-

## Checklist

- [ ] One issue, one branch, one PR (L1)
- [ ] Nothing unrelated in the diff (L2)
- [ ] Smallest change that meets the Definition of Done (L3)
- [ ] No new upstream files touched, or `engine-touch` label applied with justification (L4)
- [ ] Tunable behaviour lives in YAML, not hardcoded in C# (L5)
- [ ] Revertable with `git revert` alone (L10)
- [ ] Commits signed off (`git commit -s`) (H3)
- [ ] AI-assisted work disclosed and labelled `ai-assisted` (A7)
