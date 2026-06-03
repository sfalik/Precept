# Relational Rules & Bounds Plan — 2026-06-02

**Status**: Active
**Companion docs**: design `relational-rules-and-bounds-design-2026-06-02.md` (Locked 2026-06-02); grounding `proof-engine-contract-grounding-2026-06-02.md` (breach repros → failing tests); research `research/architecture/compiler/relational-constraint-representation-survey.md` (D2/D4); `bugs.md` (BUG-017/018/019/020); `compiler-readiness-plan-2026-05-24.md` § Phase 8 (BUG-018 = the planned "3b-count" slice — this plan executes it; cross-link, do not duplicate).
**Scope gate**: makes every declared bound (numeric/length/count, literal or field-reference) enforced per §0.7 — closing the soundness breaches and making relational rules participate in proof. Spike-branch mode: this doc is the execution hub; commits land directly on `spike/Precept-V2-Radical`.

## Phase summary

| # | Goal | Items | Decisions required | Effort | Status |
|---|---|---|---|---|---|
| 1a | Undeclared field-ref bound errors (binder name-resolution) | BUG-020 (undeclared-name half) | none | S (0.5d) | ✅ Done — `e2b7c9c1` |
| 2 (merged) | Relational narrowing (rule-sourced, octagon-style) **+ the emit-unconditionally breaches it lets discharge** | Decision 2/4 + BUG-017, BUG-018, BUG-019, BUG-021 + field-ref bounds (5/6/7) | BUG-021 lower-direction diagnostic code (PRE0078 vs PRE0079) | XL (7–10d) | Next |
| Promote | Canonical doc-sync | business-domain-types:426, spec §3.8 | none | S (0.5d) | Stub |

> **Re-sequenced 2026-06-02 (execution finding).** The original Slice 1 (the emit-unconditionally breaches) and Slice 2 (the narrowing) had to **merge**: the step-0 probe + the BUG-017 build showed emit-unconditionally *over-rejects genuinely-safe samples* without the narrowing/interval-propagation reach — BUG-017 turned `supplier-quality-management` red on a provably-safe `(Q+D)/2 ≤ 100` field (`IntervalOf` doesn't propagate through decimal `/`), and BUG-021's naive form rejected ~55 sample set-actions. This is the design's Falsifier #1 ("obviously-safe sample goes red ⇒ the *reach* must improve, not the emit decision"). Emit-unconditionally and narrowing-reach are two sides of one coin — they land together. **BUG-020 was the only cleanly-independent piece (binder name-resolution, no proof-reach dependency) — landed alone (1a).** Everything else is the merged slice. BUG-021 carries one focused decision into it: the lower-bound violation's diagnostic code (`OutOfRange`/PRE0079 is semantically right, but `IntervalContainmentProofRequirement` is catalog-mapped to `NumericOverflow`/PRE0078 — per-direction code selection needed). *(Review update: BUG-021 is narrower than first thought — it's `nonnegative`/`positive` flag modifiers not folded into the set-action create path; explicit `min` already emits PRE0078, so the code "decision" largely dissolves into matching the existing PRE0078.)*

## Execution progress

- **2a ✅ `9820b8c1`** — BUG-017 (emit computed-field bound obligations unconditionally) + `FlagLowerBound` folded into `ExtractFieldInterval` (operand reach) + a `GuardConstraint.IsArg` discriminator in the satisfiability scans (a real false-`TautologicalGuard` the flag-fold surfaced). Adversarial review flagged a candidate discharge-path soundness blocker; **probe-verified as a false positive** (bare divisors bind to the event arg via D20, so the discharge matches arg-to-arg correctly). Suite green 6680/417/67/291; supplier sample green.
- **2b-i ✅** — BUG-019 (string-length containment, non-literal RHS). Emit-unconditionally + a sound `StringLengthIntervalOf` domain (literal, field/arg-ref, concat, conditional, interpolation-with-bounded-holes, member-access hook, length-stable functions); 20 corpus samples updated to carry the matching source `maxlength` (§0.7 Composition); doc-sync (proof-engine.md / diagnostic-system.md / spec §0.6). **4 samples left red on two genuine residual gaps → Slices 3 and 4 below** (committed with the failures per owner direction).
- **2b-ii — next** — BUG-018 (collection-count interval + prover, revive `CountBoundViolation`).
- **2c** — relational narrowing (D2/D4) + field-ref bounds (5/6/7) + BUG-021 (flag-fold into the set-action path, gated on the narrowing reach for the ~55 samples).

### New slices (residual gaps surfaced by 2b-i — both go through design)

- **Slice 3 — collection element-type length (design + build).** A string read out of a collection (`Queue.peek`, accessors) into a length-capped field can't be proven: the grammar's `CollectionInnerType` (spec:1099-1105) carries only a type-qualifier, no length/value modifier, so element lengths are unexpressible. **New language surface → `/lifecycle-2-design`.** Consultation gate satisfied 2026-06-02: surfaced both candidate shapes (declared inner-type modifier vs. inferred-from-write-sites); checked `collection-types.md` (inner types carry type-qualifiers but no value modifiers; the `capacity`-modifier rejection is cardinality, a different axis) and grammar spec:1099-1105; **owner chose the declared inner-type value modifier** (`queue of string maxlength 200`) — explicit, governed, inspectable, symmetric with how inner types already carry qualifiers. Design pass formalizes: typing (per-element modifier↔inner-type validity), element governance at ingress, write-site containment obligations, read-site reach (accessors/quantifier bindings carry the element bound), and the modifier scope. Unblocks it-helpdesk-ticket, restaurant-waitlist, utility-outage-report.

  **✅ Design Locked 2026-06-02 → [`collection-inner-type-value-modifiers-2026-06-02.md`](collection-inner-type-value-modifiers-2026-06-02.md)** (owner-authorized after adversarial `precept-reviewer` pass; verdict lock-ready). Scope = **full scalar parity**. Catalog + code reuse verified against source (reuses `Modifiers.ApplicableTo`, `ValidateValueModifiers`, `TryLengthContainmentProof` + numeric interval path, the `TypedElementType` DU bound vocabulary, PRE0033/PRE0135/OutOfRange — new code is parser-position + element extraction + 3 read-reach plug-ins). **Build principles (into `/lifecycle-3-plan`):** string-length-first (unblocks the 3 samples; defers the money/quantity normalization watch-cell to the follow-on), and share the per-element write-site obligation generator with the scalar set-action path (don't fork it). **Plan: [`collection-inner-type-value-modifiers-plan-2026-06-02.md`](collection-inner-type-value-modifiers-plan-2026-06-02.md)** — Phase 1 (string-length, closes the 3 samples) → Phase 2 (numeric/quantifier/qualified parity). Next: execute Phase 1 (`/lifecycle-4-execute`).
- **Slice 4 — open-ended interpolation into a capped string. ✅ RESOLVED 2026-06-02 (ruling).** `statistical-process-control` `CurrentAlertReason` was built by interpolating open-ended `quantity` values (uncapped magnitude + runtime unit) → genuinely unbounded; Precept correctly rejects a `maxlength` there. Owner ruling: the field is free-form → dropped its `maxlength` (sample edit). No language change. SPC green. (A future nicety — a *targeted* diagnostic naming the unboundable hole instead of generic `LengthBoundViolation` — is noted but not built.)

### Discovered during execution

- **D20 bare-identifier scoping — spec/impl drift. ✅ RESOLVED 2026-06-02 → Option C (dotted-only).** `ResolveIdentifier` resolved a **bare** identifier to an in-scope **event arg before a same-named field**. A provenance follow-up established the spec already mandated dotted-only access (§3.5 line 1363) and the bare→arg path was scaffold drift (ported from v1, never specified) — spec/impl drift, not a live language decision. Owner chose **Option C**: a bare identifier names a field/binding only; a bare reference to an in-scope arg is rejected with the new `UnqualifiedEventArgReference` (PRE0163); the out-of-scope case stays `EventArgOutOfScope` (PRE0050). Implemented this slice (see `bare-identifier-arg-field-shadowing-2026-06-02.md` § Resolution). **Interaction with 2a (adversarially re-verified):** 2a dismissed an adversarial blocker on the basis "bare divisors bind to the arg" — under Option C a bare divisor now binds to the **field**. The 2a discharge stays sound: `IsArg` is set only at genuine dotted-`TypedArgRef` sites, and interval-source dispatch keys on node type (`TypedFieldRef`→`ExtractFieldInterval`, `TypedArgRef`→`ExtractArgInterval`), so resolution and interval-source moved in lockstep — no divisor/overflow obligation discharges against a constraint the bound entity does not carry. (A bare divisor now reads the field's interval; an arg-only divisor is rejected by PRE0163.) Note: a `min`-bounded field divisor is still conservatively **over-rejected** by PRE0083 — the `min`/`nonzero` → divisor-lower-bound fold is not wired for the divisor obligation the way 2a wired it for the `(Q+D)/2 ≤ max` containment case. That is a **pre-existing completeness gap, independent of Option C, in the safe (over-reject) direction** — not a soundness defect; logged for a later slice. Full proof suite green post-change; the `IsArg` discriminator remains live for **dotted** arg refs in guards.

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

**Items in scope**: BUG-017, BUG-018, BUG-019, BUG-020 (undeclared-name half only; enforcement of field-ref bounds is Slice 2), **BUG-021** (set-action lower-bound containment — found by the step-0 probe; same Decision-3 fix shape).

**Decisions required before kicking off**: none (Decision 3 settled-spec).

**Step-by-step execution**:
0. **Entry probe — DONE 2026-06-02** (`precept_compile`, MCP reconnected). Resolved the discrepancy: a set-action into a field with a **max** (or both bounds) creates the `IntervalContainment` obligation and emits (`PRE0078`) — the upper direction is sound (Phase-2 was right). A set-action into a **min-only** field does NOT create a lower-bound containment obligation — a value provably below `min` compiles clean (precept-author was right, on the lower direction). That is **BUG-021**, folded into this slice. So BUG-017's fix is the computed-field collector only (the set-action *upper* path is sound); BUG-021 adds the set-action *lower* (min) path.
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
