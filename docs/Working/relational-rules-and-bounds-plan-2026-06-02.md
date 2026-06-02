# Relational Rules & Bounds Plan — 2026-06-02

**Status**: Active
**Companion docs**: design `relational-rules-and-bounds-design-2026-06-02.md` (Locked 2026-06-02); grounding `proof-engine-contract-grounding-2026-06-02.md` (breach repros → failing tests); research `research/architecture/compiler/relational-constraint-representation-survey.md` (D2/D4); `bugs.md` (BUG-017/018/019/020); `compiler-readiness-plan-2026-05-24.md` § Phase 8 (BUG-018 = the planned "3b-count" slice — this plan executes it; cross-link, do not duplicate).
**Scope gate**: makes every declared bound (numeric/length/count, literal or field-reference) enforced per §0.7 — closing the soundness breaches and making relational rules participate in proof. Spike-branch mode: this doc is the execution hub; commits land directly on `spike/Precept-V2-Radical`.

## Phase summary

| # | Goal | Items | Decisions required | Effort | Status |
|---|---|---|---|---|---|
| 1 | Bound-containment obligations are created unconditionally and discharge-or-emit; an undeclared field-ref bound errors | BUG-017, BUG-018, BUG-019, BUG-020 (undeclared-name half) | none (Decision 3 settled) | L (4–6d) | Active |
| 2 | Relational rules narrow the subject's interval (rule-sourced, single-pass); field-ref bounds participate in proof as sugar | Decision 2 (B), Decision 4 (octagon-style), field-ref bounds (Decisions 5/6/7) | none (design Locked) | L–XL (5–8d) | Stub |
| Promote | Canonical doc-sync | business-domain-types:426, spec §3.8 | none | S (0.5d) | Stub |

## Decisions captured

All from the Locked design (`relational-rules-and-bounds-design-2026-06-02.md`); none re-litigated here:
- **D1** — bound/relational rule admitted + governed (runtime, operation-blind); a dependent fault-prone op is prove-or-reject, no deferral (§0.7/§2.4).
- **D2 = B** — discharge relational/field-ref bounds through the relational path (extend Strategy 4 rule-sourced), not a containment data-model variant; grounded by the relational-constraint-representation survey (a relation is a pair-fact; bounded relational domains are B-shaped).
- **D3** — fix the three breaches by emit-the-obligation-unconditionally, discharge-may-fail (§0.7 no deferral).
- **D4** — narrowing is single-pass, depth-bounded — octagon strong-closure-minus-loop-widening (a bounded O(n³) op, not a fixpoint).
- **D5/D6/D7** — cross-lane (§3.6), qualifier-bearing (§428 + line-426 doc-sync), computed-field (in-scope, narrows from `IntervalOf`).

## Open decisions

**None gate execution** — the design is Locked. One *empirical* item, resolved by probe at the start of Slice 1 (not a design choice):
- **Set-action subtraction obligation coverage** — the precept-author probe found a set-action subtraction into a bounded field emits *no* obligation; the Phase-2 grounding called the set-action interval path *sound*. Slice 1 step 0 probes this via `Compiler.Compile` to determine whether BUG-017's fix touches only the computed-field collector or the set-action path too. This is a finding to establish, not a decision to make.

## Heavyweight phase block — Slice 1 (current)

**Goal**: every bounded mutation/computed-result creates its containment obligation unconditionally and discharges-or-emits; a bound referencing an undeclared field errors instead of being silently swallowed. No precept that can violate a declared numeric/length/count bound compiles clean.

**Items in scope**: BUG-017, BUG-018, BUG-019, BUG-020 (undeclared-name half only; enforcement of field-ref bounds is Slice 2).

**Decisions required before kicking off**: none (Decision 3 settled-spec).

**Step-by-step execution**:
0. **Entry probe** — `Compiler.Compile(src).Diagnostics` (MCP reconnected 2026-06-02; ground-truth via the test harness, not the MCP) on a set-action subtraction into a bounded field, with and without `max`/`min`, to resolve the obligation-coverage discrepancy above. Record the result; it scopes step 2.
1. **Failing-test matrix first (TDD)** — turn the grounding-doc repros into failing tests in `test/Precept.Tests/`: BUG-017 (`A min 0` unbounded → `C min 0 max 100 <- A*10` expects `NumericOverflow`); BUG-018 (`set of integer maxcount 1` + over-`add` expects `CountBoundViolation`); BUG-019 (`maxlength 10` + `set Name = First + Last` expects `LengthBoundViolation`); BUG-020 (`min Undeclared` expects an undeclared-name error). Plus the bounded-operand control cases (must still compile clean).
2. **BUG-017** — `ProofEngine.Analysis.cs:452-454`: remove `if (interval.IsUnbounded) continue;` so the `IntervalContainmentProofRequirement` is created on the computed-field path whenever the field has bounds (mirroring the sound set-action path); Strategy 8 already returns false on unbounded → Unresolved → emit. (If step 0 shows the set-action path also skips, fix it the same way; if sound, leave it.)
3. **BUG-018** — implement `TryCountContainmentProof` (`ProofEngine.Lengths.cs:41-42`, mirror `TryLengthContainmentProof`); add the count-containment obligation generator in `Actions.cs` for growing mutations (`add`/`append`/`enqueue`/`push`/`insert`/`put`); revive the dead `CountBoundViolation` (PRE0136 / FaultCode 15) and fix its `DiagnosticCoverageAllowLists.cs` slot. **Larger part**: needs a collection-count interval (beyond today's boolean `count > 0` model) for exact `maxcount`/`mincount` proof.
4. **BUG-019** — `Actions.cs:308-311`: drop the `is TypedLiteral{String}` gate; generate `LengthContainmentProofRequirement` for non-literal string RHS; extend `TryLengthContainmentProof` to a string-length interval (concat = sum of operand length intervals; field-ref = its declared length interval; unbounded → emit). **Larger part**: the string-length interval domain.
5. **BUG-020 (undeclared half)** — make a modifier-value field reference that resolves to no declared field an error (undeclared-name), instead of being swallowed to `null` (`TryGetComparableModifierValue` / the binder). Enforcement of *resolved* field-ref bounds is Slice 2.
6. **Adversarial diff review** (soundness-critical: proof engine) before each commit; flag correctness/requirement gaps, not style.

**Dependencies**: none — Slice 1 is the foundation (and the reject-when-unprovable floor Slice 2's relational narrowing relies on).

**Exit criteria** (testable):
- The four failing tests from step 1 now pass; the bounded-operand control cases still compile clean.
- `dotnet test` green across all four projects; numeric-constant golden snapshots byte-identical (literal-bound paths untouched).
- `SampleCompilesCleanTests` green, OR every newly-emitting sample is documented as a genuine latent bound violation (Falsifier check: >~3 false reds ⇒ narrowing too weak / wording needs work, not a wrong emit decision).
- `CountBoundViolation` appears live in `precept_compile` proof-obligation output.
- BUG-017/018/019 flipped to Fixed in `bugs.md`; BUG-020's undeclared-name half marked fixed (enforcement half noted for Slice 2).

**Estimated effort**: L (4–6d) — BUG-017 ~0.5d (one-line + obligation), BUG-020 undeclared ~0.5d; BUG-018 + BUG-019 are the bulk (the count-interval and string-length-interval domains, ~3–4d combined).

**Doc-update obligations** (per CLAUDE.md routing; land in this slice):
- `docs/compiler/proof-engine.md` — Strategy 9 (Length) extends to non-literal RHS; Strategy 10 (Count) becomes live; remove the `TryCountContainmentProof` stub note; § Implementation State.
- `docs/compiler/diagnostic-system.md` — `CountBoundViolation` (136) dead → live; `LengthBoundViolation`/`NumericOverflow` reachable on new paths.
- `docs/language/precept-language-spec.md §0.6` — implementation-status: count/length non-literal and computed-unbounded cases now covered.
- `docs/Working/bugs.md` — BUG-017/018/019 → Fixed.
- NOT in this slice: business-domain-types:426 / §3.8 (promote-stage, Slice 2 / Promote).

## Lightweight phase stub — Slice 2 (next)

**Goal**: a relational rule (`rule X >= Y`) narrows the subject's interval so downstream divisor/overflow/range obligations discharge; field-reference bounds participate in proof as sugar over that mechanism.
**Scope**: Decision 2 (B — extend Strategy 4 / `GuardRelationImpliesObligation` from guard-sourced + subtraction-only to **rule-sourced `>=`/`<=`**, one shared narrowing core); Decision 4 (single-pass, depth-bounded octagon-style closure; default depth 1, raise to fixed small N only if a Falsifier trips); field-ref bounds (Decisions 5 §3.6, 6 §428, 7 computed-field in-scope). Soundness-critical → adversarial diff review.
**Decisions required**: none (design Locked); build-time tuning only (closure depth).
**Effort**: L–XL (5–8d).
**Status**: Stub — TBD pending Slice 1 completion (the reject-when-unprovable floor + the interval domains it builds are prerequisites).

## Promote stage (after Slice 2)

Doc-sync obligations (via `/lifecycle-5-promote`): business-domain-types line 426 (bound-interpretation → field-references); spec §3.8 `InvalidModifierBounds` (field-ref pairs: fire only when provably contradictory). Plus promote the design's mechanism into `proof-engine.md` / `compiler-and-runtime-design.md`.

## Definition of done

- Both slices' exit criteria met; every Acceptance criterion in the Locked design passes (division demonstrator discharges via the relational rule; the three breach repros reject; mutual/self-reference compile).
- BUG-017/018/019 Fixed; BUG-020 fully fixed (undeclared in Slice 1, enforcement in Slice 2).
- Relational narrowing live (rule-sourced); field-ref bounds enforced and proof-participating.
- Promote-stage doc-syncs landed; the readiness plan's Phase-8 3b-count row marked complete (BUG-018).

## Discovered during planning

Sources the plan touches that are not verbatim in the design's `sources-consulted` (per the skill's plan-touches ⊆ design-sources check) — uncovered during execution sequencing, not design oversights:
- `src/Precept/**/DiagnosticCoverageAllowLists.cs` — the dead-`CountBoundViolation` allow-list slot must move to a legitimate Gate-2 entry once it emits (BUG-018 mechanics). Implementation detail of the design's "revive CountBoundViolation."
- The name-resolution/binder path that emits an undeclared-name diagnostic for a modifier-value field reference (BUG-020 undeclared half) — the design cited `TryGetComparableModifierValue` (`Modifiers.cs`) where the reference is dropped to `null`; the *emit-an-error-instead* site may also touch the binder/`UndeclaredField` path. Confirm at build.

## Plan update protocol

After each slice: check off in this doc, mark the phase row ✅ with the commit hash, flip the relevant `bugs.md` entries, and pause for review at the slice boundary (no auto-advance). When Slice 1 lands, promote Slice 2 from stub to heavyweight. Surface any mid-build design gap as an Open Decision routed back to `/lifecycle-2-design` — do not patch the Locked design inline.
