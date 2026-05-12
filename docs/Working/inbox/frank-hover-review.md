# Frank — Hover Design v2 Review

**Date:** 2026-05-12  
**Artifact:** `docs/Working/hover-design.md`  
**Verdict:** APPROVED with notes

---

## Good

**G1: Philosophy framing is right.** "Hover is a fast trust surface, not an onboarding tutorial" (line 12) — this is exactly the right design instinct. It aligns with the philosophy's emphasis on inspectability (§0.1 principle 4) without turning hover into a mini-tutorial.

**G2: Status indicators are philosophically honest.** The three-tier `✅ Proof verified` / `⚡ Runtime checked` / `⚠️ Unverified` (lines 185–188) directly reflects the spec's truth-based diagnostic classification (§0.6 principle 5): proved safe, enforced at runtime, or unresolved. This is Precept being honest about what it knows. Excellent.

**G3: `because` text prioritization is correct.** "The authored `because` text is usually the best human explanation, so it should outrank raw expressions whenever it exists" (line 14) — this is exactly right. `because` is a mandatory language requirement (§0.1 principle 9), not a comment. Leading with it respects the authoring model.

**G4: Reject semantics are correctly distinguished.** The reject row hover (§8, lines 152–153) says "deliberate business rejection" — this tracks the spec's §3A.1 distinction: "`reject` is authored prohibition, not failed data truth." The semantic distinction between rejection and constraint failure is preserved.

**G5: Target user calibration is good.** "Analyst comfortable in SQL or Python" (line 20) — this matches `docs/philosophy.md` § Who authors a precept. The tone throughout is factual without being patronizing. Terms like "type-safe quantity comparison" and "qualifier resolves from" are appropriate for this audience.

**G6: Transition row hover captures the multi-stage picture.** The §5 example (lines 99–107) shows graph reachability, guard summary, mutations, AND proof gaps in one hover. This is exactly the right information density for the construct that carries the most pipeline-stage complexity.

**G7: Construct-level resolver order is correct.** "Resolve the enclosing construct first" (line 194) — this is the right architectural instinct. Token-level hover is the fallback, not the entry point.

---

## Blockers

**B1: `rule` hover scope claim is misleading (§2, line 52).** The hover says "Enforced in: all reachable states." Rules are NOT state-scoped — they are *global data truth* that hold after every mutation (spec §3A.1: "`rule` expresses global data truth — constraints that hold after every mutation"). Framing this as "all reachable states" makes it sound like the graph analyzer is partitioning rule enforcement per-state. It's not. Rules apply unconditionally (or conditionally via their guard), regardless of state topology. Stateless precepts have no states and rules still hold.

**Fix:** Change "Enforced in: all reachable states" to something like "Scope: global — enforced after every mutation" or "Scope: global data truth" to match the spec's language. For guarded rules, show "Scope: global when `<guard>`."

**B2: Field hover status line is inaccurate (§1, line 33).** `⚡ Runtime checked — validated on update, fire, and inspect` — rules and ensures on a field are NOT "validated on inspect." Inspection is a *non-mutating preview* (spec §3A.6: "all executed on a working copy without committing"). The constraint evaluation happens during inspection, yes, but the enforcement semantics are fundamentally different — inspection doesn't reject or commit. Saying "validated on ... inspect" conflates enforcement with preview. Also, `update` and `fire` are the *caller operations* — the engine enforces on the *mutation working copy*, not "on update." The phrasing should describe what the engine does, not the caller's API.

**Fix:** Rephrase to something like "⚡ Runtime checked — enforced on every mutation before commit" or "enforced before any state change commits." Drop `inspect` from the enforcement claim.

---

## Notes

**N1: `ensure` hover (§6) should show the scope anchor.** The ensure example says "Scope: `Listed` only" (line 122), which is correct for `in Listed ensure`, but the design doesn't explicitly distinguish the four ensure anchor types (`in`, `to`, `from`, `on`) in the rendered output. Since these have fundamentally different enforcement semantics (residency vs. entry vs. exit vs. event-argument truth — spec §3A.1 lines 1707–1711), the scope line should name the anchor: "Scope: residency (`in Listed`)" vs. "Scope: entry gate (`to Listed`)" vs. "Scope: event args (`on Submit`)". This is not just labeling — the author needs to know *when* their ensure fires.

**N2: Computed fields are absent.** The design covers stored fields but never mentions `computed` fields (declared with `<-`). Computed fields have fundamentally different hover semantics: they are never directly writable, their value derives from an expression over other fields, and hovering one should show the dependency chain. This is a coverage gap. At minimum, the field hover template should note "computed from: `<expression>`" when applicable, and suppress the writable-state map (since computed fields are structurally non-writable, spec §3.6: `ComputedFieldNotWritable`).

**N3: `omit` declarations are absent.** The design covers `modify ... editable` (§7) but not `in <State> omit ...` — structural field exclusion. `omit` means the field is *structurally absent* in that state, not just read-only. This is a semantically distinct access mode (spec keyword table line 265) and deserves its own hover template or at least coverage in the access declaration section.

**N4: State modifiers are absent from state hover.** The state hover (§3) shows reachability and incoming/outgoing edges, but doesn't surface state modifiers: `terminal`, `required`, `irreversible`, `success`, `warning`, `error` (spec §1.1 lines 293–300). These are author-declared structural intent — the graph analyzer cross-checks them. A `required` state, for instance, means "every initial→terminal path visits here." That's exactly the kind of structural guarantee hover should surface.

**N5: Event `initial` modifier is absent from event hover.** The event hover (§4) doesn't mention the `initial` modifier. The construction event is semantically special (spec §3A.5) — it's the entity's constructor. Hovering an `initial` event should say so explicitly, since it changes how the event is invoked (`Create(args)` vs. `Fire(event, args)`).

**N6: The "Typical effects" line in event hover (§4, line 88) may be hard to produce.** "Typical effects: set `ListPrice`, increment `PriceChangeCount`" requires cross-referencing all transition rows for this event across all source states and summarizing mutations. This is a derived projection that may not be cheap or always meaningful (different rows may have very different mutation sets). Flagging for Kramer's feasibility assessment — if it's expensive, the V1 boundary should exclude it.

**N7: "Selected when: no earlier `FulfillOrder` row guard matches" (§8, line 155) implies fallback ordering.** This is semantically correct — reject rows are typically the last-resort fallback. But the phrasing "no earlier ... row guard matches" embeds knowledge of row ordering that's a first-match routing semantic (spec §3A.1: "transition rows are evaluated in declaration order — the first matching guard wins"). Good — just make sure the hover doesn't say "fallback" for reject rows that aren't positionally last.

---

## Summary

The design is strong. The philosophy alignment, status indicator system, and target-user calibration are all correct. The two blockers are precision issues — `rule` scope and field enforcement timing — that would misrepresent how the engine works if shipped as-is. Fix those, and this is ready for Kramer to implement.
