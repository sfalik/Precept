---
title: Citation Audit — Frank Go-Forward Posture Replay
date: 2026-07-14
author: Fact Checker
status: Working
source_under_audit: docs/Working/frank-go-forward-posture-replay-2026-07-14.md
scope: Documentation-only citation integrity audit
---

# Citation Audit — Frank Go-Forward Posture Replay (2026-07-14)

## Citation ledger

| Claim (brief) | Citation | Status | Note |
|---|---|---:|---|
| No invalid configuration; invalid configuration is not reachable | `philosophy.md:51` | ✅ | Exact quote appears at the cited line and supports the claim directly. |
| Representational overflow is owner-parked; disclosed, temporary, not currently prove-or-reject; no doc may claim it prevented; post-MVP target remains | `proof-engine-boundary-ruling-2026-07-06.md:25`, `:275`; `proof-engine-decision-ledger-2026-07-12.md` item #7 | ✅ | Boundary ruling gives the carve-out language verbatim; ledger #7 confirms post-MVP target and “gap is build-order, not a text defect.” |
| Compiler is “total over the fault surface,” not “every declared constraint, full stop” | `proof-engine-boundary-ruling-2026-07-06.md:25` | ✅ | Exact phrase and distinction are present. |
| Ratified target: live fault set is prove-or-reject; prove-or-reject ratified only as package with MVP engine work; §6 pulled into MVP | `boundary-ruling:25`; `decision-ledger` item #1; `decision-ledger` item #1, MVP-scope note | ✅ | The citations support all three subclaims. Ledger #1 is load-bearing here. |
| Built today: engine only handles simplest relational rule shape (`rule X >= Y`); §1a/§2/§3/§6 are scope, not shipped | `proof-engine-linear-solver-feasibility-2026-07-12.md:22` | ✅ | Exact wording supports the current-state claim. |
| Nonlinear relational invariants remain expressible and are governed, with zero expressiveness loss | `boundary-ruling:226` | ✅ | Worked-pattern table says this directly. |
| Linear relational identities are governed for the same reason as nonlinear ones; enforcement is by post-mutation sweep, not ingress | `boundary-ruling:160`; `result-types.md:114`/`:121`; `boundary-ruling:102`/`:106`; `boundary-ruling:159` | ✅ | Composite citation set is strong: row 11 distinguishes invariant vs computed value; row 10 explains why the sweep, not ingress, carries the invariant. |
| Routing here is provenance, not linearity; §1a/§1b are about discharging fault obligations, not proving invariants; product-heavy corpus remains expressible; fault-prone sub-operations still prove-or-reject | `feasibility`; `decision-ledger` item #3; `boundary-ruling:25`; `boundary-ruling:130` | ✅ | Fair synthesis from the cited material. The “21 of 77” count is at `boundary-ruling:25`; prove-or-reject for sub-operations is at `:130`. |
| §1b is deferred, not built, and not scheduled; combining facts is not an expressibility gain | `feasibility:54`; `decision-ledger` item #3 | ✅ | Both claims are explicitly supported. |
| Decidability is the discharge-oracle, not the router; unresolved derived obligations reject; no runtime escape hatch | `boundary-ruling:39`; `decision-ledger` item #9 | ✅ | Both citations support the stated rule directly. |
| Runtime governance uses two distinct mechanisms; keeping them distinct makes the guarantee “precise rather than magical” | `precept-language-spec.md:264` | ✅ | Exact phrase appears at the cited line. |
| The spec names three ingress points: event arguments, construction inputs, direct field edits | `precept-language-spec.md:268` | ✅ | Exact list appears at the cited line. |
| Operation/event arguments are ingress-governed; sentence also covers construction inputs | `runtime-api.md:656`; Fire pipeline `runtime-api.md:361` | ⚠️ | Fire/event args are supported. Construction inputs are true in the corpus, but this sentence’s attached runtime citations do not directly show Create/construction; `precept-language-spec.md:268` or `runtime-api.md:162`/`:180` would support that half more precisely. |
| Editable fields are external-write ingress; same governance applies on Update; non-editable fields reject before value handling | `boundary-ruling:166`; `runtime-api.md:487`; `runtime-api.md:656`; `boundary-ruling:127`; `runtime-api.md:386`; `result-types.md:138`/`:147` | ✅ | Strong support across boundary ruling, runtime API, and result type docs. |
| Ingress checks one incoming value against its own carried constraint; enforcement is operation-blind; runtime makes the carried constraint true | `precept-language-spec.md:268`; `precept-language-spec.md:268`; `philosophy.md:55` | ✅ | Exact support; philosophy line 55 matches the “not a second line of defense” seam. |
| Failure modes: malformed args → `InvalidArgs`; non-editable field → `FieldNotEditable`; constraint failure discards working copy so nothing persists | `result-types.md:113`; `result-types.md:138`/`:147`; `precept-language-spec.md:268`; `result-types.md:114`/`:146`; `boundary-ruling:127` | ✅ | Composite citation set supports the full failure-path description. |
| Post-mutation sweep re-checks all whole-entity invariants at commit and returns `ConstraintsFailed` on violation | `result-types.md:114`/`:121` | ✅ | Exact support. |
| There is no “computed-but-undecidable ⇒ governed” case; Option B was rejected | `boundary-ruling:25`; `decision-ledger` item #1 | ✅ | Exact support. |
| Runtime governance is not a weaker guarantee; it is structural enforcement, not a second line of defense | `philosophy.md:55` | ✅ | This is an interpretive use of the quoted philosophy line, but it is a fair reading and consistent with the surrounding corpus. |
| The routing rule is two-way by provenance: external value at ingress vs declared relational invariant via whole-entity sweep | `precept-language-spec.md:268`; `result-types.md:121` | ✅ | Supported, though `precept-language-spec.md:1969` would state the ingress-vs-sweep split even more directly. |
| Never route by surface spelling; `max 100` and `rule x <= 100` get the same treatment; modifier-vs-rule disposition is incoherent | `boundary-ruling:134`; `boundary-ruling:37` | ✅ | Exact support. |
| Ratified model is organized around a bounded fault surface; nonlinear invariants remain governed; ledger itself uses “fault-scoped” language | `boundary-ruling:25`; `boundary-ruling:226`; `decision-ledger` item #1, folded-in note | ✅ | All three subclaims are supported. |
| “Govern, don’t prove” (Option B) for derived values is explicitly rejected | `decision-ledger` item #1 | ✅ | Exact support. |
| Treating “unprovable” as “defer to runtime” is rejected | `decision-ledger` item #9 | ✅ | Exact support. |
| Presenting §1b as rejected is wrong; it was deferred/open | `decision-ledger` item #3 | ✅ | Exact support. |
| `min 5` and `rule X >= 5` are interchangeable to the proof engine; proof participation depends on decidability, not syntactic form | `boundary-ruling:37`, quoting `spec:1138` | ✅ | Exact support; quoting chain is accurate. |
| What is *not* banned: fault-shaped/proof-heavy observation remains compatible with the model; nonlinear invariants are governed; ledger uses “fault-scoped” | `boundary-ruling:25`; `boundary-ruling:226`; `decision-ledger` item #1, folded-in note | ✅ | Supported. |
| The month-long arc was a “spiral, not a straight line” | `frank-retrospective-proof-engine-arc-2026-07-13.md` §4 | ✅ | The exact phrase appears in the retrospective (`:81`); the section-only citation is coarse, but the retrospective does support the claim materially. |
| The arc swung through the identity thesis’s GOVERN counter-position, the D2 band-split nadir, and the Error position before landing | `retrospective` §1.2 | ✅ | §1.2 supports this sequence directly. |
| The landing added genuinely new material: certificate criterion and the proof that §1b is never an expressibility gain; “biggest single deposit” / “genuinely new” | `retrospective` §2; `decision-ledger` item #2; `decision-ledger` item #3 | ✅ | Supported. The retrospective supplies the quoted characterization; the ledger items identify the two additions. |
| Final posture was arrived at “through genuine debate and reversal,” not held from the start | `retrospective` §1.2, §4 | ⚠️ | The cited sections clearly show a swing, detour, and changed positions. But the quoted phrase “through genuine debate and reversal” does not appear verbatim in those sections; this is a fair inference, not a supported quote. Line-level cites would make the claim cleaner. |

## Summary of ⚠️ / ❌ findings

### ⚠️ Findings

1. **Construction-input support is under-cited in the ingress-arguments sentence.**
   - The sentence’s claim is right at corpus level, but the attached runtime citations are Fire-centric (`runtime-api.md:361`, `:656`).
   - Better support exists for construction specifically: `precept-language-spec.md:268` and `runtime-api.md:162`/`:180`.

2. **The retrospective close quotes a stronger phrase than the cited sections actually say.**
   - `retrospective` §1.2 and §4 do support “the position changed” and “this was a spiral, not a straight line.”
   - They do **not** supply the exact quoted wording **“through genuine debate and reversal”**. That phrase is an interpretive gloss, not a source-backed quote.

### ❌ Findings

- **None.** I did not find a citation in the posture replay that was directly contradicted by the cited document.

## Strengthening opportunities

1. **Use the spec’s ingress-vs-sweep sentence for the runtime split.**
   - `precept-language-spec.md:1969` says the distinction most directly: ingress governs what enters; the sweep governs the result.
   - This is stronger than reconstructing the split from `:264` + `result-types.md:121`.

2. **Use construction-specific runtime API citations where the text mentions construction inputs.**
   - `runtime-api.md:162` and `:180` show Create using the full event/constraint pipeline.
   - These are better than leaning on Fire-only `runtime-api.md:361` when the claim spans construction too.

3. **For the prove-or-reject rationale, canonical docs are now stronger than the working-doc chain.**
   - `docs/compiler-and-runtime-design.md:145`, `:163`, `:165`, `:171` capture obligation creation, no-deferral, composition seam, and why prove-or-reject beat fault-floor.
   - `docs/compiler/proof-engine.md:2609` and `:2617` capture the designed-not-built MVP and the principled §1b deferral.

4. **For the certificate criterion, prefer the canonical spec once the claim is philosophical rather than historical.**
   - `precept-language-spec.md:225` states the four-leg criterion directly and canonically.
   - The current posture doc often cites the ledger/retrospective instead, which is historically correct but weaker as a source of truth.

5. **For the “ratified package” condition, Frank’s canonical-capture note is a sharper source than the replay’s current chain.**
   - `frank-canonical-capture-recommendation-2026-07-14.md:56` states the condition crisply: prove-or-reject is honest only as the strengthened-engine package.

6. **Replace section-only retrospective citations with line citations.**
   - Stronger exact anchors:
     - `frank-retrospective-proof-engine-arc-2026-07-13.md:72` (certificate criterion as new)
     - `:74` (§1b not expressibility gain as new)
     - `:81` (“spiral, not a straight line”)
     - `:109` (identity-crisis framing of the detour)

7. **For the compiler/runtime seam, Frank’s canonical-capture summary is concise and strong.**
   - `frank-canonical-capture-recommendation-2026-07-14.md:71`/`:72` give a compact, posture-aligned statement of governance and composition.

## Bottom line

- **✅ Verified:** 29
- **⚠️ Unverified / under-supported:** 2
- **❌ Contradicted:** 0

**Three most consequential issues:**
1. The only materially weak runtime citation is the one that mentions **construction inputs** while citing Fire-centric runtime lines.
2. The only wording-overreach is the retrospective close’s quoted phrase **“through genuine debate and reversal”** — supported as inference, not as quote.
3. Several claims are correct but could be materially stronger if re-anchored to now-canonical docs (`precept-language-spec.md`, `compiler-and-runtime-design.md`, `proof-engine.md`) instead of replaying the working-doc chain.
