# Relational Rules & Bounds Plan — 2026-06-02

**Status**: Active
**Companion docs**: design `relational-rules-and-bounds-design-2026-06-02.md` (Locked 2026-06-02); grounding `proof-engine-contract-grounding-2026-06-02.md` (breach repros → failing tests); research `research/architecture/compiler/relational-constraint-representation-survey.md` (D2/D4); `bugs.md` (BUG-017/018/019/020); `compiler-readiness-plan-2026-05-24.md` § Phase 8 (BUG-018 = the planned "3b-count" slice — this plan executes it; cross-link, do not duplicate).
**Scope gate**: makes every declared bound (numeric/length/count, literal or field-reference) enforced per §0.7 — closing the soundness breaches and making relational rules participate in proof. Spike-branch mode: this doc is the execution hub; commits land directly on `spike/Precept-V2-Radical`.

## Phase summary

| # | Goal | Items | Decisions required | Effort | Status |
|---|---|---|---|---|---|
| 1a | Undeclared field-ref bound errors (binder name-resolution) | BUG-020 (undeclared-name half) | none | S (0.5d) | ✅ Done — `e2b7c9c1` |
| 2 (merged) | Relational narrowing (rule-sourced, octagon-style) **+ the emit-unconditionally breaches it lets discharge** | Decision 2/4 + BUG-017, BUG-018, BUG-019, BUG-021 + field-ref bounds (5/6/7) | BUG-021 lower-direction diagnostic code (PRE0078 vs PRE0079) | XL (7–10d) | 2a ✅ / 2b-i ✅ / 2b-ii ✅ → **2c next** |
| 2c-i | Relational narrowing **core** (field-to-field `>=`/`<=` → provable interval; R-NARROW-*, single-pass depth-1) | Decision 2 (=B) + Decision 4 | none | L (4–6d) | ✅ Done — `7ebbb68c` build / `e7faae3a` promote (closed BUG-023/024; filed BUG-025/026; subject-resolution + contradiction-reject amendments folded in) |
| 2c-ii-prereq (BUG-027) | Fold global rules against **default** values (Principle 11); a default-violating unguarded rule rejects | none (locked spec §0.1 P11) | diagnostic code: reuse PRE0115 vs new (gate) | S–M (1–2d) | **Next** — prerequisite for 2c-ii numeric default enforcement |
| 2c-ii | Field-ref modifier bounds enforced — **full family** (`min`/`max` reuse 2c-i; `minlength`/`maxlength`/`mincount`/`maxcount` via a band-edge DU) + rich author message | BUG-020 enforcement half + Decisions 5/6/7 | design CORRECTED — the first locked design (`field-reference-bound-enforcement-2026-06-04.md`) was found **unsound** by adversarial review (numeric default mechanism + length/count integer-vs-string semantics); re-locked via `/design` | L (5–7d) | Heavyweight — gates on 2c-i **and** BUG-027 |
| 2c-iii | BUG-021 set-action lower-bound containment (`min`/`nonnegative`/`positive`) | BUG-021 | lower-direction diagnostic code (build-time confirm: match PRE0078 vs split PRE0079) | S–M (1–3d) | Stub (gates on 2c-i) |
| Promote | Canonical doc-sync | business-domain-types:426, spec §3.8 | none | S (0.5d) | Stub |

> **Re-sequenced 2026-06-02 (execution finding).** The original Slice 1 (the emit-unconditionally breaches) and Slice 2 (the narrowing) had to **merge**: the step-0 probe + the BUG-017 build showed emit-unconditionally *over-rejects genuinely-safe samples* without the narrowing/interval-propagation reach — BUG-017 turned `supplier-quality-management` red on a provably-safe `(Q+D)/2 ≤ 100` field (`IntervalOf` doesn't propagate through decimal `/`), and BUG-021's naive form rejected ~55 sample set-actions. This is the design's Falsifier #1 ("obviously-safe sample goes red ⇒ the *reach* must improve, not the emit decision"). Emit-unconditionally and narrowing-reach are two sides of one coin — they land together. **BUG-020 was the only cleanly-independent piece (binder name-resolution, no proof-reach dependency) — landed alone (1a).** Everything else is the merged slice. BUG-021 carries one focused decision into it: the lower-bound violation's diagnostic code (`OutOfRange`/PRE0079 is semantically right, but `IntervalContainmentProofRequirement` is catalog-mapped to `NumericOverflow`/PRE0078 — per-direction code selection needed). *(Review update: BUG-021 is narrower than first thought — it's `nonnegative`/`positive` flag modifiers not folded into the set-action create path; explicit `min` already emits PRE0078, so the code "decision" largely dissolves into matching the existing PRE0078.)*

## Execution progress

- **2a ✅ `9820b8c1`** — BUG-017 (emit computed-field bound obligations unconditionally) + `FlagLowerBound` folded into `ExtractFieldInterval` (operand reach) + a `GuardConstraint.IsArg` discriminator in the satisfiability scans (a real false-`TautologicalGuard` the flag-fold surfaced). Adversarial review flagged a candidate discharge-path soundness blocker; **probe-verified as a false positive** (bare divisors bind to the event arg via D20, so the discharge matches arg-to-arg correctly). Suite green 6680/417/67/291; supplier sample green.
- **2b-i ✅** — BUG-019 (string-length containment, non-literal RHS). Emit-unconditionally + a sound `StringLengthIntervalOf` domain (literal, field/arg-ref, concat, conditional, interpolation-with-bounded-holes, member-access hook, length-stable functions); 20 corpus samples updated to carry the matching source `maxlength` (§0.7 Composition); doc-sync (proof-engine.md / diagnostic-system.md / spec §0.6). **4 samples left red on two genuine residual gaps → Slices 3 and 4 below** (committed with the failures per owner direction).
- **2b-ii ✅ `c27a382b`** — BUG-018 (collection-count enforcement). Reworked through `/design` to **Reading A — obligation / prove-or-reject** (an emit-on-provable-violation first cut was reverted after owner challenge; the unbiased committed-HEAD-grounded redo landed Reading A, matching the length/divisor siblings). Sound per-kind/per-action count-interval, catalog-derived (`TypeMeta.DeduplicatesElements`, `ActionMeta.EffectIsConditional`); locked design `count-bound-discharge-semantics-2026-06-03.md` + grounding survey `count-cardinality-bound-proof-survey.md` rode along. Adversarial review: 87 probes, zero soundness holes. Suite green 6761/417/296/67.
- **2c-i ✅ `7ebbb68c` (build) / `e7faae3a` (promote)** — relational narrowing core (D2/D4): declared `rule X >= Y` narrows X's interval/sign and discharges downstream divisor/range obligations, single-pass depth-1. Two mid-build design gaps surfaced and were each routed through an unbiased `/design` and built clean: **subject-resolution** (the divisor obligation's Site is the Divide node, not the subtraction — resolve via `ResolveSubject` before the subtraction gate) and the **empty-intersection contradiction guard** (a relation contradicting a field's own bounds must reject, not vacuously prove — closed BUG-024). Closed BUG-023 (guarded-rule leak). Filed BUG-025 (relational contradictions get no warning — inspectability) and BUG-026 (bare-unsatisfiable-rule severity policy). Twice adversarially reviewed; 7569 tests green; samples 78/78. Canon promoted (the `proof-engine.md §1343` "Division is NOT covered" drift corrected).
- **2c — in progress (re-sequenced 2026-06-05)** — field-ref bounds + BUG-021. The 2c-ii full-family `/design` produced a design an adversarial `precept-reviewer` found **unsound** (numeric default routed through "default ∈ narrowed interval" — can't reject when the referenced field is unbounded; and length/count misread as string-length-compare when they take an integer count). The fix surfaced a prerequisite: **BUG-027** (global rules are not folded against defaults — a default-violating rule compiles clean, a Principle-11 gap). New order: **BUG-027 (execute) → 2c-ii design correction (`/design` amendment) → 2c-ii build (execute → promote) → 2c-iii**. See § Heavyweight phase block — Slice 2c and the plan `/home/sfalik/.claude/plans/streamed-swinging-sprout.md`.

### New slices (residual gaps surfaced by 2b-i — both go through design)

- **Slice 3 — collection element-type length (design + build).** A string read out of a collection (`Queue.peek`, accessors) into a length-capped field can't be proven: the grammar's `CollectionInnerType` (spec:1099-1105) carries only a type-qualifier, no length/value modifier, so element lengths are unexpressible. **New language surface → `/design`.** Consultation gate satisfied 2026-06-02: surfaced both candidate shapes (declared inner-type modifier vs. inferred-from-write-sites); checked `collection-types.md` (inner types carry type-qualifiers but no value modifiers; the `capacity`-modifier rejection is cardinality, a different axis) and grammar spec:1099-1105; **owner chose the declared inner-type value modifier** (`queue of string maxlength 200`) — explicit, governed, inspectable, symmetric with how inner types already carry qualifiers. Design pass formalizes: typing (per-element modifier↔inner-type validity), element governance at ingress, write-site containment obligations, read-site reach (accessors/quantifier bindings carry the element bound), and the modifier scope. Unblocks it-helpdesk-ticket, restaurant-waitlist, utility-outage-report.

  **✅ Design Locked 2026-06-02 → [`collection-inner-type-value-modifiers-2026-06-02.md`](collection-inner-type-value-modifiers-2026-06-02.md)** (owner-authorized after adversarial `precept-reviewer` pass; verdict lock-ready). Scope = **full scalar parity**. Catalog + code reuse verified against source (reuses `Modifiers.ApplicableTo`, `ValidateValueModifiers`, `TryLengthContainmentProof` + numeric interval path, the `TypedElementType` DU bound vocabulary, PRE0033/PRE0135/OutOfRange — new code is parser-position + element extraction + 3 read-reach plug-ins). **Build principles (into `/plan`):** string-length-first (unblocks the 3 samples; defers the money/quantity normalization watch-cell to the follow-on), and share the per-element write-site obligation generator with the scalar set-action path (don't fork it). **Plan: [`collection-inner-type-value-modifiers-plan-2026-06-02.md`](collection-inner-type-value-modifiers-plan-2026-06-02.md)** — Phase 1 (string-length, closes the 3 samples) → Phase 2 (numeric/quantifier/qualified parity). **✅ Both phases done — Phase 1 `4a003b64`, Phase 2 `fcbaaf71`; BUG-019 fully closed, full scalar parity live.** Deferred (not blocking): `notempty`-in-element (Open Decision → `/design`); quantifier-binding sign-set discharge (completeness gap, safe direction).
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

## Heavyweight phase block — Slice 2c (next, promoted 2026-06-03)

**Slice goal**: a relational rule (`rule X >= Y`, field-to-field) narrows the subject's interval so downstream divisor/overflow/range obligations discharge; the field-reference bound modifiers (`min Floor` …) participate in proof as sugar over that same mechanism; and a set-action lower-bound (`min`/`nonnegative`/`positive`) violation is rejected at the create path. All three pieces are coupled by one narrowing reach — built in dependency order as three sub-slices (only the first at full heavyweight detail per guard 3; the other two are stubs until 2c-i lands).

> **Why three coupled sub-slices, not three slices.** The design's re-sequencing finding (plan § Phase summary note) established that emit-decisions and narrowing-reach are two sides of one coin: BUG-017's emit-unconditionally over-rejected a provably-safe sample *because the reach was too weak* (`IntervalOf` didn't propagate through decimal `/`). 2c is the **reach** side of that coin for the relational source. The discharge-side capability (2c-i) is the foundation both enforcement halves (2c-ii field-ref bounds, 2c-iii BUG-021 set-action) depend on — neither can be built first without re-introducing the over-rejection the design's Falsifier #1 forbids. So 2c-i lands the SOUND reach, then 2c-ii/2c-iii lean on it.

### Sub-slice dependency order

```
2c-i  Relational narrowing core (foundation)  ──┬──►  2c-ii  Field-ref modifier bounds (BUG-020 enforcement half)
                                                 └──►  2c-iii BUG-021 set-action lower-bound containment
```
2c-ii and 2c-iii are independent of each other; both gate on 2c-i.

---

### Sub-slice 2c-i — Relational narrowing core (HEAVYWEIGHT)

**Goal**: make a field-to-field relational rule (`rule X >= Y because "…"`) contribute a provable interval to `X` (and the symmetric bound to `Y`) per Decisions 2/4, so a downstream divisor/overflow/range obligation depending on `X` discharges — a single-pass octagon-strong-closure-minus-widening narrowing that is a sound over-approximation and degrades to identity when the source field's relevant bound is infinite.

**Items in scope**: Decision 2 (= B, relational path) + Decision 4 (single-pass depth-bounded closure) — the discharge-side core only. No enforcement-half emission (that is 2c-ii/2c-iii).

**Decisions required before kickoff**: none (Decisions 2 and 4 are Locked). One build-time tuning parameter, not a design choice: **closure depth** — default depth 1 (a relation reads `Y`'s *already-declared* interval, one hop, no transitive chase per Decision 4's R-NARROW-* "`⟦Y⟧` is the interval `Y` has *before* this narrowing step"); raise to a fixed small N only if Falsifier #2 (a sample needs a `X>=Y, Y>=Z, Z min 0` chain) trips. This is a falsifier-gated knob, not a gate.

**Probe-grounded core gap (verified in source, 2026-06-03):**
- `ProofEngine.Composition.cs:180-208` `TryGetNumericConstraintFact` extracts only `field op static` (both arms call `TryGetStaticNumericValue` on the non-subject operand). A `field op field` rule (`OnHand > Reserved`) matches neither arm → produces no `ScopedNumericFact` → contributes nothing to the sign-set / interval reach. **This is the core gap.**
- `ProofEngine.cs:88-93` `ScopedNumericFact` carries a `decimal Value` — it structurally *cannot* represent `X >= Y` (the RHS is a field, not a magnitude). The fact vocabulary must gain a field-relation shape (a relational fact, or a sibling record) to carry the pair-fact (Decision 2 = B: "a relation is a fact about the *pair* `(X, Y)`").
- `ProofEngine.Composition.cs:145-149` already iterates `semantics.Rules[i].Condition` through `TryGetNumericConstraintFact` (rules already flow in as fact candidates) — so the wiring point is established; only the `field op field` extraction + its consumption is missing.
- `ProofEngine.Intervals.cs:202-219` `ExtractFieldInterval` composes `GetFieldBounds` + `FlagLowerBound` into the operand interval — this is where the relational narrowing must contribute the tightened bound (one-hop read of the related field's `ExtractFieldInterval`, `⊓`'d per R-NARROW-GE/LE).
- `ProofEngine.Strategies.cs:1213-1238` `GuardRelationImpliesObligation` is the existing guard-sourced, subtraction-only field-to-field machinery (Decision 2's "extend from guard-sourced to rule-sourced; one shared core"). The rule-sourced path must **share this core**, not fork it.

**Decision 2 = B realized**: the relation discharges through the relational/field-to-field path (extending `GuardRelationImpliesObligation` + the `ScopedNumericFact` fact stream to a pair-fact), **not** an `IntervalContainmentProofRequirement` field-reference DU variant — no data-model/DTO change (design Falsifier #4: if B turns out to need a DTO change, the A-vs-B call was wrong; this sub-slice's first commit confirms B needs none).

**Failing-test-matrix sketch (TDD — write these red first, in `test/Precept.Tests/ProofEngine/RelationalNarrowingTests.cs`):** the enumerated input space = {relation op} × {downstream obligation} × {source-field boundedness} × {direction}:

| # | Input (rule + downstream op) | Expected | Falsifies |
|---|---|---|---|
| 1 | `rule OnHand > Reserved` + `BatchCost / (OnHand - Reserved)` (the design demonstrator) | **compiles clean** (divisor provably ≥ 1) | reach too weak (over-reject) |
| 2 | same, but `Reserved` **unbounded above** and division by bare `OnHand` | **rejects**, names the unbounded field | over-prove (false safe) |
| 3 | `rule X >= Y`, `Y min 0`, then `Z / X` | X gains lower bound 0 → if 0 reachable, **rejects** (X may be 0) | direction/over-prove |
| 4 | `rule X <= Y`, `Y max 100`, overflow obligation on `X` | X's upper bound tightened to 100 → overflow **discharges** | R-NARROW-LE direction |
| 5 | `rule X >= Y` with `Y` unbounded-below | identity narrowing → downstream lower-bound obligation **stays unresolved → rejects** | identity-degradation soundness |
| 6 | mutual `rule A >= B` + `rule B >= A` | **compiles** (conjoins to `A == B`, no hang) | fixpoint formed |
| 7 | self `rule X >= X` | **compiles** (vacuous, no hang) | fixpoint formed |
| 8 | `>` vs `>=` boundary: `rule X > Y` (strict) discharging `X - Y >= 1` vs `X - Y > 0` | strict relation discharges strict + `>= 1` integer; non-strict does NOT discharge `!= 0` on reals | off-by-one in `GuardRelationImpliesObligation` reuse |

**Dependencies**: Slice 1 / 2a / 2b (done) — the emit-unconditionally floor and the interval domains those built are the reject-when-unprovable substrate the narrowing tightens *against*. No dependency on 2c-ii/2c-iii.

**Exit criteria** (testable):
- Tests 1–8 above pass; specifically the design demonstrator (`rule OnHand > Reserved` makes `BatchCost / (OnHand - Reserved)` discharge) **compiles clean** without an author-added `when (OnHand - Reserved) != 0` guard.
- Tests 2 and 5 (the over-prove falsifiers) **reject** — the narrowing never manufactures a false "safe".
- Mutual (test 6) and self (test 7) reference **compile and terminate** (no fixpoint, per §3.5 line 1343 / Decision 4).
- `dotnet test` green across all four projects; numeric-constant golden snapshots byte-identical (literal-bound paths untouched — the relational path is additive).
- Decision 2 = B confirmed: zero change to `IntervalContainmentProofRequirement` / `CompileToolDtos.cs` (Falsifier #4 not tripped).

**Falsifier / corpus check** (the soundness gate — this is the design's Principle-1 falsifier, the most important one):
- **Never over-prove.** No relational narrowing may discharge an obligation a runtime trap would then fire on. Test 2 and Test 5 are the standing guards; the adversarial diff review must trace every new discharge path for a false-"safe".
- **Must not over-reject the genuinely-safe corpus.** `SampleCompilesCleanTests` newly-red count after 2c-i: the ~55 set-action samples and the relational-demonstrator-shaped samples must stay green. **>~3 newly-red ⇒ the reach is too weak (an identity-narrowing that should have tightened), not the inputs being unprovable** — fix the reach, re-run, before advancing. (Design Falsifier #1, applied to the relational source.)

**Estimated effort**: L (4–6d) — the fact-vocabulary extension (`ScopedNumericFact` → pair-fact) + sharing the `GuardRelationImpliesObligation` core for the rule source + the `ExtractFieldInterval` narrowing contribution + the depth-1 closure are the bulk; soundness-critical so the adversarial review is a non-trivial slice of the band.

**Doc-update obligations** (per CLAUDE.md routing; land in this sub-slice):
- `docs/compiler/proof-engine.md` — § Proof Strategies / Strategy 4: document the rule-sourced relational narrowing (R-NARROW-GE/LE), the single-pass depth-1 bound, and that guard-sourced + rule-sourced share one core; § Implementation State.
- `docs/language/precept-language-spec.md §0.6` — item 2 (relational reasoning over multiple fields) implementation-status: now live for rule-sourced `>=`/`<=`.
- NOT in this sub-slice: business-domain-types:426 / §3.8 / §2.4 modifier-desugar wording (2c-ii); `CountBoundViolation` (already done in 2b-ii); diagnostic-system.md relational-unprovable wording (rides with 2c-ii's author-facing message).

---

### Lightweight stub — Sub-slice 2c-ii — Field-reference modifier bounds enforced (BUG-020 enforcement half)

**Goal**: `field Amount as number min Floor` desugars at `PopulateRules` (`TypeChecker.Normalization.cs:86`) to `rule Amount >= Floor because "<generated>"` (§2.4 line 1112) and participates in proof through the 2c-i narrowing core; the literal-only gate at `TryGetComparableModifierValue` (`TypeChecker.Validation.Modifiers.cs:462`) is lifted so a resolved field-ref bound is admitted; the unprovable dependent operation emits the design's **target author-facing message** (design § Audience "Error message" block — names the business field `Floor`, states `Amount >= Floor`, offers bound-or-guard repair), attributed to the modifier syntax not the synthetic rule.
**Scope**: Decisions 5 (cross-lane §3.6), 6 (qualifier-compatibility §428 + line-426 doc-sync), 7 (computed-field bound narrows from its inferred interval). Generated rationale wording (§2.4 / Philosophy Principle-9 row: generic "X must be at least Floor" until a wording decision lands).
**Decisions required**: none (Decisions 5/6/7 Locked). Build-time confirm: whether the desugar-at-`PopulateRules` produces a `TypedRule` the 2c-i fact stream already picks up, or needs an explicit synthetic-rule tag for attribution back to the modifier span.
**Effort**: M–L (3–5d).
**Dependencies**: 2c-i (the narrowing core the desugared rule discharges through).
**Status**: Stub — TBD pending 2c-i completion and the attribution-confirm above.

---

### Lightweight stub — Sub-slice 2c-iii — BUG-021 set-action lower-bound containment

**Goal**: a set-action whose value is provably below a field's `min`/`nonnegative`/`positive` lower bound is rejected at the create path (today the set-action *upper* path emits `PRE0078`/`NumericOverflow`, but the *lower* (min/flag) direction creates no containment obligation — see plan § Execution-progress step-0 probe finding). Folds the lower-bound flag/`min` into the set-action `IntervalContainmentProofRequirement` create path, gated on the 2c-i narrowing reach so the ~55 genuinely-safe set-action samples are not over-rejected.
**Scope**: the set-action create-path obligation collector in `ProofEngine.Analysis.cs` (the `IntervalContainmentProofRequirement` stamp) + `ExtractFieldInterval`'s `FlagLowerBound` (already folds `nonnegative`/`positive` → 0 in the *read* path; the gap is the *containment-obligation* create path for the min direction).
**Decisions required (build-time confirm, NOT pre-decided — surface as a focused choice at kickoff)**: the lower-bound violation's diagnostic code. `OutOfRange`/PRE0079 is semantically right for a below-`min` value; but `IntervalContainmentProofRequirement` is catalog-mapped to `NumericOverflow`/PRE0078, and the plan's review note found BUG-021 is narrower than first thought (explicit `min` already emits PRE0078; the residual is the `nonnegative`/`positive` flag modifiers not folded into the create path) — so the "decision" largely dissolves into **matching the existing PRE0078**. Confirm at build whether a per-direction code split (PRE0078 upper / PRE0079 lower) is worth it or whether matching the sibling PRE0078 is the right minimal move.
**Effort**: S–M (1–3d).
**Dependencies**: 2c-i (the narrowing reach that keeps the ~55 set-action samples green).
**Status**: Stub — TBD pending 2c-i completion and the diagnostic-code build-time confirm above.

## Promote stage (after Slice 2c — all three sub-slices)

Doc-sync obligations (via `/promote`): business-domain-types line 426 (bound-interpretation → field-references, Decision 6); spec §3.8 `InvalidModifierBounds` (field-ref pairs: fire only when provably contradictory). Plus promote the design's mechanism (Decisions 2/4, R-NARROW-*) into `proof-engine.md` / `compiler-and-runtime-design.md`. (The per-sub-slice doc-touches above — proof-engine.md Strategy 4, spec §0.6 item 2, §2.4 desugar, diagnostic-system.md — land *with* their sub-slice; this promote stage is the remaining canonical lift.)

## Definition of done

- All sub-slices' exit criteria met; every Acceptance criterion in the Locked design passes:
  - division demonstrator (`rule OnHand > Reserved` → `BatchCost / (OnHand - Reserved)`) discharges via the relational rule and compiles clean (2c-i);
  - `field Amount as number min Floor` is enforced and proof-participating, and emits the target author-facing message when `Floor` is unbounded (2c-ii);
  - BUG-021 lower-bound set-action below `min`/`nonnegative`/`positive` rejects (2c-iii);
  - mutual (`A min B` + `B min A`) and self (`X min X`) reference compile and terminate per §3.5 (2c-i).
- BUG-017/018/019 Fixed (done, slices 2a/2b); BUG-020 fully fixed (undeclared in 1a, enforcement in 2c-ii); BUG-021 Fixed (2c-iii).
- Relational narrowing live (rule-sourced); field-ref bounds enforced and proof-participating.
- The soundness falsifier held across all sub-slices: zero over-prove (no narrowing-discharged obligation a runtime trap then fires on), and the ~55 set-action samples + relational demonstrators stay green (>~3 newly-red ⇒ reach gap, fixed before advance).
- Promote-stage doc-syncs landed; the readiness plan's Phase-8 3b-count row marked complete (BUG-018).

## Discovered during planning

Sources the plan touches that are not verbatim in the design's `sources-consulted` (per the skill's plan-touches ⊆ design-sources check) — uncovered during execution sequencing, not design oversights:
- `src/Precept/**/DiagnosticCoverageAllowLists.cs` — the dead-`CountBoundViolation` allow-list slot must move to a legitimate Gate-2 entry once it emits (BUG-018 mechanics). Implementation detail of the design's "revive CountBoundViolation."
- The name-resolution/binder path that emits an undeclared-name diagnostic for a modifier-value field reference (BUG-020 undeclared half) — the design cited `TryGetComparableModifierValue` (`Modifiers.cs`) where the reference is dropped to `null`; the *emit-an-error-instead* site may also touch the binder/`UndeclaredField` path. Confirm at build.

**Slice 2c plan-touches (guard-7 set-membership run, 2026-06-03).** The design's `sources-consulted` names the discharge surface generically as "Strategy 4 / `GuardRelationImpliesObligation`" (`proof-engine.md` lines 1330/1397) and the emission sites (`ProofEngine.Analysis.cs:452-454`, `ProofEngine.Lengths.cs`). The build-real narrowing-fact surfaces below were probed during planning and are NOT verbatim in `sources-consulted`. They are **implementation refinements of the cited Strategy-4 / Decision-2-B mechanism, not new design surface** — the design's Decision 2 explicitly chose "extend the field-to-field machinery; guard-sourced and rule-sourced share one core," which IS this surface. Listed here per guard 7 (execution-sequencing discoveries, not design oversights — no re-lock needed):
- `src/Precept/Pipeline/ProofEngine.Composition.cs:180-208` (`TryGetNumericConstraintFact`) — the actual fact-extraction site that today handles only `field op static`; the `field op field` arm is the core 2c-i change. The design cited `ProofEngine.Strategies.cs` Strategy 4 as *the* relational machinery; the *fact-stream* half lives in Composition.cs. Same mechanism, finer-grained file.
- `src/Precept/Pipeline/ProofEngine.cs:88-93` (`ScopedNumericFact` record) — the fact vocabulary must gain a field-relation (pair-fact) shape to carry `X >= Y`. This is the data shape Decision 2 = B ("a relation is a fact about the pair") implies; the design located it at the strategy level, the build locates it at the fact-record level. Implementation refinement.
- `src/Precept/Pipeline/ProofEngine.Intervals.cs:202-219` (`ExtractFieldInterval`/`FlagLowerBound`) — where the narrowed relational bound is `⊓`'d into the operand interval (R-NARROW-GE/LE contribution) and where 2c-iii reads the flag lower bound. The design's `IntervalContainment` discussion references this interval composition without naming the function.
- `src/Precept/Pipeline/TypeChecker.Normalization.cs:86` (`PopulateRules`) — 2c-ii's desugar hook where `min Floor` becomes a synthetic `TypedRule`. The design's § Semantic Rules describes this desugaring (§2.4 line 1112) but cites the spec, not the impl hook.
- `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs:462` (`TryGetComparableModifierValue`) — the literal-only gate 2c-ii lifts. Design Decision 3's sources cite `Modifiers.cs` for BUG-020; this is the same family at the modifier-value-extraction site.

None of the above contradicts a Locked decision or introduces surface the design didn't anticipate — they are the file:line realizations of Decisions 2/4 (relational path, shared core) and the §2.4 desugar. **No design re-lock required.**

## Plan update protocol

After each (sub-)slice: check off in this doc, mark the phase row ✅ with the commit hash, flip the relevant `bugs.md` entries, and pause for review at the slice boundary (no auto-advance). When 2c-i lands, promote 2c-ii and 2c-iii from stub to heavyweight (only N+1 at full detail per the planning guard). Surface any mid-build design gap as an Open Decision routed back to `/design` — do not patch the Locked design inline.
