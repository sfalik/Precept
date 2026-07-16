---
status: Draft — 2026-07-14
phase-target: Proof-engine MVP Slice 0 (fail-open fixes)
canonical-home: docs/compiler/soundness-and-coverage.md §6 (Soundness-hole coverage)
sources-consulted:
  - docs/compiler/soundness-and-coverage.md: §1 (soundness model — direct-engine soundness with re-checker deferred), §3.1 (NumericOverflow row — GATE-O), §6 (GATE-O ruling, retained in canon)
  - docs/compiler/proof-engine.md: prove-or-reject MVP, discharge cascade, verdict DU
---

# Soundness-hole coverage — Slice-0 fail-open fixes

**Why this lives in Working, not in canon.** This is a tactical implementation to-do list: a set of
failing-test-first Slice-0 fixes whose entries are *removed as the holes close*. That churn profile is
transient, so it lives here rather than in `soundness-and-coverage.md` §6. The canonical GATE-O
LEAVE-AS-IS ruling stays in §6; only this checklist was lifted out. When these fixes land, this doc is
archived and §6 records the closed state (or the definitional residue, if any) in canonical terms.

With the independent re-checker deferred (see `soundness-and-coverage.md` §5b), MVP soundness comes from
fixing the engine directly. An adversarial pass found four fail-open holes — each a discharge path that
returns a proved/clean verdict on an input it has not actually proven. Under prove-or-reject, unprovable
must **reject**, never pass. Each is a failing-test-first Slice-0 fix:

1. **Undecidable-magnitude default bound** — an undecidable bound check that silently stamped `Proved`
   must route to `Unresolved` (reject).
2. **Arg / field name collision** — a guard on an event arg must not narrow a same-named field's interval;
   add an arg-vs-field discriminator at the narrowing site.
3. **Sequential staleness ignored** — the bound-check narrowing path must consult `ReassignedBefore` and
   drop facts whose subject was reassigned earlier in the same handler, matching the sibling paths.
4. **Dead-end→dead-end suppression stamped as proved** — stop stamping a false `Proved` on unanalyzed
   dead-end rows; render the honest `Unresolved` (outcome-neutral once dead-end is an Error).

Plus the **systematic fail-open sweep**: audit every `bool?`-returning discharge path (does `null` route
to reject?) and every dict-write site into the narrowed-interval dictionary (does it guard the empty
interval before storing?). The four named holes are the found instances, not a closed set.
