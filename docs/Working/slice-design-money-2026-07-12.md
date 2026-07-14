---
status: Externally-Grounded
slice: "§3 money — bounded money/quantity arithmetic through × and ÷ (compiler-readiness plan v3)"
tier: Light — confirm the ruled seam (readiness plan § slice-tiering; most of this slice is ruled in architecture §1.5)
phase-target: Proof-engine MVP §3 (after Slice 0)
sources-consulted:
  - docs/Working/compiler-readiness-plan-2026-07-12-architecture.md — §1.5 (the ruled seam: catalog wiring + one kernel guard), §1.8 + the §1.5-vs-§1.8 ⊥-rail resolution, §2.3 (F4), §4 per-slice table (":213 — §3 row")
  - docs/Working/compiler-readiness-plan-2026-07-12.md — §3 slice stub :298-333 (goal, key steps, exit criteria, owned docs)
  - docs/Working/certificate-steps-membership-2026-07-12.md — the SETTLED Slice-0 certificate vocabulary; Decision 5 (IntervalArithmetic cites the owning catalog member; "§3 money then needs zero certificate-vocabulary work"); § Semantic Rules IntervalArithmetic recompute rule
  - docs/compiler/proof-engine.md — :8 (implementation state), :604 (computed-field bound containment), :1701-1703 (Strategy: interval containment / IntervalTransfer propagation), :2613 (§3 listed as designed-not-implemented)
  - docs/language/business-domain-types.md — :174-182 (money/quantity arithmetic exactness commitments), :334 (homogeneous decimal backing), :426-428 (bound extraction + unit-normalized containment)
  - docs/language/precept-language-spec.md — §0.1 eleven principles (:92-112), §0.4 execution-model properties
  - docs/philosophy.md, CLAUDE.md — non-negotiables (catalog-before-code, derive-don't-duplicate, doc-sync)
  - src/Precept/Language/Operations.cs — money/quantity/price op-meta family :417-700 (per-cell line anchors in Decision 1); ExchangeRate cells :702-720; business unary Negate cells :58-76 (NegateMoney :58, NegateQuantity :62, NegatePrice :66, NegateDuration :70, NegatePeriod :74 — no transfer, unlike their numeric siblings :48/:52/:56); transfer helpers :1332-1356; NO IntervalTransfer on any business cell (grep-verified: last IntervalTransfer assignment is :250)
  - src/Precept/Language/Operation.cs — :107-108 (`IntervalTransferFn? IntervalTransfer { get; init; }` — the delegate slot)
  - src/Precept/Language/NumericInterval.cs — Empty sentinel :29 ([0,-1]); Add :73-77; Subtract :79-83; Multiply :85-92 (unguarded corners :90); Divide :94-102 (divisor-spans-zero :97; unguarded corners :100); Negate :104-108; Scale :110-117 (guards only IsUnbounded); Shift :119-123; Contains :125-129 (empty-argument ⇒ true); Union :131-135; SentinelAdd :38-43
  - src/Precept/Pipeline/ProofEngine.Intervals.cs — generic transfer dispatch :102-133 (binary :104-112, unary :114-119, function :121-132); IsEmpty backstop :509; ApplyStaticUnitScaling :423-452 (price-space inversion :445-446)
  - samples/invoice-line-item.precept (the flagship money-arithmetic idiom), samples/loan-application.precept (money field/rule conventions)
  - live precept_compile probes (2026-07-13, syntax validation + current-gap evidence): the OrderLine worked sample below compiles syntax-clean and today yields Unresolved IntervalContainment with computedInterval "[−∞ .. +∞]" + PRE0078 on both money computed fields; a decimal probe with 1e28-scale bounds crashes the compiler with OverflowException (Open question 2)
---

# §3 money slice design — bounded money/quantity arithmetic through × and ÷

**How to read this document.** This slice makes the compiler able to *prove* that money, quantity, and price arithmetic stays inside a field's declared `min`/`max` bounds. Today the machinery for such proofs exists and runs for plain numbers (`integer`, `decimal`, `number`), but the business-money operations carry no "interval transfer" — the per-operation rule that says "a value between 1 and 100, times a value between 1 and 50, lands between 1 and 5000." With no transfer the engine computes "unknown range" and honestly refuses to prove, so every bounded money computation rejects with PRE0078 even when the author's bounds make it obviously safe. The fix is ruled to be **catalog wiring** (attach the existing transfer functions to the money operation entries — zero engine code) plus **one kernel guard** (make the interval kernel immune to the "empty interval" sentinel producing garbage). *Interval transfer* = the function on a catalog operation entry that maps operand ranges to a result range. *⊥ (bottom) / Empty* = the empty range, "no value can satisfy this," represented by the sentinel `[0, -1]`.

## Goal

When this slice lands: a computed or assigned money/quantity/price value whose operand bounds make containment provable discharges its `IntervalContainment` obligation (the worked sample's `Total` proves); every wired operation's proof carries the already-settled `IntervalArithmetic` certificate step citing the owning Operations catalog member — **zero new certificate vocabulary** (verified below); every ⊥-in-arithmetic vector (`Empty × x`, `Empty ÷ x`, `x ÷ Empty`, `Empty ± x`, `Scale(Empty)`) is structurally impossible inside the kernel; and every *unwired* cell keeps rejecting conservatively — no over-prove anywhere.

## Worked sample (syntax validated live, 2026-07-13)

```precept
precept OrderLine

# Stateless line-pricing precept: bounded money x decimal and money / decimal.

field UnitPrice as money in 'USD' default '1 USD' min '1 USD' max '100 USD' editable
field Count as decimal default 1 positive min 1 max 50 editable
field Total as money in 'USD' min '1 USD' max '5000 USD' <- UnitPrice * Count
field PerUnit as money in 'USD' min '0.02 USD' max '100 USD' <- Total / Count
```

**Today** (probed live): both `IntervalContainment` obligations are `Unresolved` with computed interval `[−∞ .. +∞]`, and PRE0078 emits on both — the author cannot clear this by hand at all, because no guard or bound can bound an operation the engine has no transfer for. This is the plan's "the one true 'author cannot clear it by hand at all today'" (plan :300).

**After this slice**:
- `Total` **proves**: `[1..100] × [1..50]` — four corners `{1, 50, 100, 5000}` — gives `[1..5000]`, which fits the declared `[1..5000]`.
- `PerUnit` **stays honestly Unresolved**: interval division cannot see that `Total / Count` algebraically equals `UnitPrice`, so it computes `[1..5000] ÷ [1..50] = [0.02..5000]`, which overlaps but exceeds `max '100 USD'`. The obligation's `computedInterval` — the payload MCP and hover project — now carries the *finite* range `[0.02 .. 5000]` instead of `[−∞ .. +∞]`: approximation honesty made visible in the projection, with an authorable fix (compute `PerUnit` from `UnitPrice` directly, or widen the bound). **The rendered PRE0078 message text does not change in this slice**: its template renders only the field name (`Diagnostics.cs:690` — "Numeric computation exceeded the representable range on field '{0}'"), and the renderer's `(computed: …)` clause (`ProofEngine.Diagnostics.cs:119-126`) is passed as an argument the template never renders — verified live: today's message shows no range even though `ComputedInterval` is set. The message *naming* the range is contingent on Slice 0's proposed PRE0078 re-rendering (`certificate-steps-membership-2026-07-12.md`, § Error message), not delivered by this slice's inventory.

Real-corpus anchor: `samples/invoice-line-item.precept` computes `Subtotal <- UnitPrice * Quantity` and `DiscountAmount <- Subtotal * DiscountPercent / 100` — exactly the wired shapes (its `+`/`−` computed fields are Open question 1).

## Scope

- **In scope**: attaching `IntervalTransfer` delegates to the money/quantity/price ×/÷ catalog cells per the per-cell disposition table (Decision 1); the `NumericInterval` ⊥-guard over `Multiply/Divide/Add/Subtract/Scale` (Decision 2, F4); tests for both; doc updates per § Doc-update enumeration.
- **Out of scope (already ruled/settled — cited, not re-decided)**: the generic transfer dispatch (`Intervals.cs:102-133`) — untouched, wiring flows through it automatically; obligation *creation* (`proof-engine.md:604/:1701` — containment obligations already exist for bounded computed/assigned money fields, verified live); the certificate vocabulary (settled Slice-0 doc — zero growth, § Certificate confirmation below); the ⊥-arm *detection* rail at dict-write time (ruled to §2, architecture §1.8 + the §1.5-vs-§1.8 resolution); the divisor-nonzero obligation family (already shipped, `Operations.cs` ProofRequirements + PRE0083, verified live).
- **Deferred / parked**: the `+`/`−` money/quantity/price cells, the five business unary Negate cells (`Operations.cs:58-76`), and the ExchangeRate cells (Open question 1 — family-boundary ambiguity between the plan's "× and ÷" title and the architecture's "whole family"); decimal corner-overflow saturation (Open question 2); all period/duration-space and price-space-cancellation cells (omitted per the §1.5 ruling — "no transfer = Unbounded = conservative reject"; re-open trigger: a magnitude-space verification for those spaces).

## Philosophy Alignment (spec §0.1)

| Principle | Affected? | How served | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | Bounded money arithmetic that cannot be proven safe keeps rejecting at compile time; the slice only widens what is *provably* safe — prevention strength never decreases. | N/A | N/A |
| 2. One file, complete rules | Y | Everything a proof consumes is the authored bounds/guards in the one `.precept` file plus the Operations catalog (the machine-readable spec); no external configuration. | N/A | N/A |
| 3. Deterministic semantics | Y | Transfers are pure functions over decimal endpoints; four-corner arithmetic is exact decimal — same definition ⇒ same interval ⇒ same verdict. | N/A | N/A |
| 4. Full inspectability | Y | Each proof renders through the settled certificate (`IntervalArithmetic` step citing the operation member); the Unresolved residual's `computedInterval` projection (MCP/hover) surfaces the finite computed range instead of `[−∞..+∞]`. | The rendered PRE0078 message itself stays range-less this slice (template renders only the field name, `Diagnostics.cs:690`); message-level range naming rides Slice 0's proposed PRE0078 re-rendering. | Inspectability lands in the obligation projection first, the message second — accepted per this slice's registry-untouched scope. |
| 5. Keyword-anchored readability | N | N/A — no authored-syntax change of any kind. | N/A | N/A |
| 6. Explicit domain meaning | Y | The proof surface now honors the domain types' own operation algebra (`money × decimal → money` proves in money space) instead of stopping at primitives — the domain types stop being second-class in the proof engine. | N/A | Cells whose result magnitude space is unverified stay unproven (omitted transfers) — domain meaning is honored by *refusing* to guess, at a precision cost. |
| 7. Compile-time-first static checking | Y | New compile-time discharge capability; nothing moves to runtime. | N/A | N/A |
| 8. Approximation honesty | Y | Interval results are over-approximations and are shown as computed via the obligation's `computedInterval` projection (the `PerUnit` residual); omitted cells honestly report unbounded rather than pretending a transfer exists; decimal four-corner arithmetic is exact on endpoints (`business-domain-types.md:174` exactness commitment untouched). | N/A | Interval arithmetic forgets operand correlation (`Total / Count ≠ UnitPrice` to the engine) — accepted; the residual's computed-interval projection makes the gap authorable (in the PRE0078 message text too once Slice 0's proposed re-rendering lands). |
| 9. Mandatory rationale | N | N/A — no constraint surface change. | N/A | N/A |
| 10. Totality | Y | The ⊥-guard removes a crash/garbage vector from the compile-time kernel (F4: `x ÷ Empty` throws `DivideByZeroException` today — `Empty=[0,-1]` defeats the `:97` guard since `0 <= 0 && -1 >= 0` is false, then a corner divides by `Min = 0`). | Open question 2: the corners can still throw `OverflowException` on huge-but-legal bounds (confirmed live on the already-shipped decimal path) — a pre-existing kernel crash this slice's wiring gives more exposure. | Parked, not patched (contract: surface, don't settle) — options in Open question 2. |
| 11. Static completeness | Y | Closes part of the P11 gap: bounded money arithmetic becomes provable, so prove-or-reject stops rejecting provably-safe definitions; the F4 fix removes a false-`Proved`/garbage vector (`Empty × [2,3] = [-3,0]`, non-empty garbage sailing past the `:509` backstop). | N/A | Omitted cells remain unprovable — precision, never soundness (§1.5 tradeoff, ruled). |

**Companion commitments.** *Stateless-first-class*: the worked sample is a stateless precept; nothing here touches states. *Domain-expert-primary-author*: the capability targets exactly the invoice/pricing idiom a domain author writes first (`samples/invoice-line-item.precept`).

## Language Design Grounding

**No new language surface.** No token, keyword, type, operator, modifier, construct, or expression form changes; the operations being wired already exist in the catalog and the grammar. No new certificate premise or step kind (confirmation below). The Pre-Design Owner Consultation gate is satisfied upstream: the owner ruled this slice's shape in architecture §1.5 and the plan's §3 stub; this design executes the ruled seam and parks the two boundary ambiguities it found (Open questions 1-2) rather than settling them.

### Certificate confirmation (zero new vocabulary — verified)

The tasking asked this design to verify the claim that §3 needs no certificate vocabulary. Verified on both sides of the seam:

- **Settled vocabulary side**: the Slice-0 membership doc's Decision 5 states the mechanism — "One `IntervalArithmetic` step kind referencing the owning catalog member's transfer — not per-operator step kinds … §3 money then needs **zero** certificate-vocabulary work: new transfers wired in Operations automatically carry certifiable steps" (`certificate-steps-membership-2026-07-12.md`, Decision 5). The recompute rule already covers this slice's cells: `IntervalArithmetic(op, measure, [a,b], [c,d]) ⇓ transfer_op([a,b],[c,d]) — cites the owning catalog member's transfer: a binary op (BinaryOperationMeta.IntervalTransfer) …` (ibid., § Semantic Rules). `UnitNormalization` (member 3) already covers the unit/price rescale line.
- **Engine side**: the dispatch the certificate builder threads through is generic over the catalog — `if (opMeta is BinaryOperationMeta bom && bom.IntervalTransfer is { } transfer) … return transfer(leftInterval, rightInterval);` (`ProofEngine.Intervals.cs:104-112`; unary `:114-119`; function `:121-132`). A newly wired `MoneyTimesDecimal` transfer takes the same code path as `DecimalTimesDecimal` does today; the step instance names the member via the same `Operations.GetMeta` lookup.

So this slice adds **no** member to `CertificateStepKind`, no premise kind, and no payload shape — the certificate obligations here are test-level only (acceptance criteria 8-9).

## Semantic Rules

**No typing rule changes.** Every wired cell already exists in the Operations catalog with its operand/result typing, qualifier policy, and proof requirements (e.g. `MoneyTimesDecimal`, `Operations.cs:437-441`). **No reduction/runtime changes.** `IntervalTransfer` is consumed only by the compile-time proof engine; the evaluator is untouched.

**Proof-obligation rule (the transfer soundness contract).** For every cell wired in this slice, the attached transfer must satisfy, in the *normalized magnitude space* the engine's bounds live in (`NormalizedDeclaredMin/Max`, unit-rescaled per `ApplyStaticUnitScaling`, `Intervals.cs:423-452`):

```
transfer_op([a,b], [c,d])  ⊇  { m(x op y) | m(x) ∈ [a,b], m(y) ∈ [c,d] }
```

where `m(·)` is the magnitude of a value in that normalized space — every concretely reachable result magnitude lies inside the computed interval (an over-approximation is legal; an under-approximation is a soundness bug). Because all seven business types back their magnitudes with `decimal` (`business-domain-types.md:334`) and the wired cells' magnitude semantics is plain decimal `×`/`÷` in a *single shared space*, the existing `MultiplyTransfer`/`DivideTransfer` helpers (`Operations.cs:1337/1339` region) satisfy this contract exactly as they do for `DecimalTimesDecimal`. **The wire-condition** (which cells may be wired) is precisely: *the operation's magnitude semantics in the normalized bound space must be plain decimal arithmetic in one shared space* — cells where operand and result bounds normalize into different or unverified spaces (period/duration factors, price-space cancellation) fail the condition and are omitted. Decision 1 applies this condition cell-by-cell.

**Kernel ⊥ rule (F4).** For `op ∈ {Add, Subtract, Multiply, Divide, Scale}` on `NumericInterval`:

```
IsEmpty(l) ∨ IsEmpty(r)   ⇒   op(l, r) = Unbounded      (first clause inside the method)
```

Why each method needs it (verified against `NumericInterval.cs`, `Empty = [0,-1]` `:29`): `Add(Empty,[2,3]) = [2,2]` (`:76` — non-empty garbage); `Subtract(Empty,[2,3]) = [-3,-3]` (`:82`); `Multiply(Empty,[2,3]) = [-3,0]` (`:90`); `Divide(x, Empty)` **throws** `DivideByZeroException` (the `:97` spans-zero guard evaluates `0 <= 0 && -1 >= 0` = false, so the corner `Min / other.Min` divides by zero at `:100`); `Scale(Empty, 2) = [-2,0]` (`:110-117` guards only `IsUnbounded`). `Negate` and `Shift` are **audited and excluded**: both preserve `Max < Min` structurally (`Negate([0,-1]) = [1,0]`, still empty; `Shift` adds the same offset to both ends) — with one recorded corner: `Shift`'s sentinel saturation (`SentinelAdd`, `:38-43`) can collapse `Empty` to a non-empty point if the offset is `±decimal.MaxValue`, unreachable today because `Shift` is called only with UCUM affine offsets (bounded constants like 273.15). Recorded here so a future caller re-checks (Decision 2, tradeoff leg).

**Soundness preservation (principles 7/10/11).** (i) Every new `Proved` comes only from a transfer meeting the containment contract above — over-approximation can turn a true-safe site provable, never a violating site provable. (ii) Every cell *not* wired returns `Unbounded` from the dispatch's fall-through (`Intervals.cs:112`), the result fails containment, the verdict stays `Unresolved`, and prove-or-reject rejects — omission degrades precision only (architecture §1.5 tradeoff, ruled). (iii) The ⊥-guard removes the F4 false-prove/crash vectors; converting ⊥ to `Unbounded` cannot over-prove (Unbounded never passes containment, `Intervals.cs:505`) and cannot fake infeasibility (per the ruled §1.5-vs-§1.8 resolution, "infeasibility is never inferred from an Unbounded that arithmetic produced" — the infeasibility detector is the §2 dict-write rail, not arithmetic output). (iv) Nothing changes verdict authority, obligation creation, or diagnostics plumbing. (v) The `:509` reader-side backstop this argument leans on has a dead, backstop-less sibling — the zero-caller non-narrowed `TryIntervalContainmentProof` (`Intervals.cs:467-487`), whose comparisons would pass `Empty` against bounds spanning it if a future caller revived it; this slice removes it (Inventory) so the "⊥-over-prove structurally impossible" claim has no dormant counterexample in the file.

## Architecture Grounding

### Precept-internal placement

- **Catalog (`src/Precept/Language/Operations.cs`)** — the transfers are per-member metadata on existing entries, using the existing `IntervalTransferFn? IntervalTransfer` init slot (`Operation.cs:107-108`) and the existing private helpers (`AddTransfer/SubtractTransfer/MultiplyTransfer/DivideTransfer`, `Operations.cs:1332-1356`). Catalog-before-code, derive-don't-duplicate: no new helper, no new delegate type, no parallel arithmetic.
- **Kernel (`src/Precept/Language/NumericInterval.cs`)** — the ⊥-guard lives inside the five methods so *no call site can forget* (ruled, plan :318-320). The one place the MVP touches the kernel.
- **Pipeline (`src/Precept/Pipeline/`)** — **No behavioral change.** The dispatch is already generic (`Intervals.cs:102-133`, verified above); "zero engine code" (architecture §1.5) is confirmed achievable against source. One deletion only: the dead, zero-caller non-narrowed `TryIntervalContainmentProof` (`Intervals.cs:467-487`) comes out (Inventory; drift ledger) — removing unreachable code, not changing any live path.
- **Cross-component propagation**:
  - Runtime evaluator — **None** (compile-time-only metadata).
  - Language server / hover — **None beyond Slice 0's generic certificate rendering**; a money `IntervalArithmetic` step renders through the same template as a decimal one.
  - MCP — **None**: `computedInterval` is already projected per obligation (verified in the live probe output); no DTO change. `docs/tooling/mcp.md` — no update needed.
  - Diagnostics — **None new**: PRE0078 (containment residual) and PRE0083 (division safety) already exist and already fire on these sites; the slice changes their *frequency* and the obligation's `computedInterval` payload precision, not their registry. Note the rendered PRE0078 message text is untouched: its template renders only `{0}` (`Diagnostics.cs:690`), so the finite range surfaces via the obligation projection, not the message (pre-existing template/arg mismatch recorded in the drift ledger; message change rides Slice 0's proposed re-rendering).
  - Tests — new fixtures (Inventory).
- **Reuse verification (thin wiring, not a parallel definition)**: transfers reuse `NumericInterval.Multiply/Divide/Add/Subtract` (`NumericInterval.cs:73-102`) via the shared helpers; bounds reuse `NormalizedDeclaredMin/Max` + `ApplyStaticUnitScaling` (`Intervals.cs:423-452`, price-space inversion `:445-446`); dispatch reuses `Intervals.cs:102-133`; obligation creation reuses the existing `IntervalContainment` path (`proof-engine.md:604/:1701`, verified live). New code = catalog property assignments + five one-line guard clauses + tests (plus one dead-method deletion, Inventory).

### External architectural precedent

No non-trivial architectural change is introduced (no new component, layer, or protocol — property assignments on existing catalog entries plus a defensive clause in an existing value type), so no excerpted comparator is carried. One knowledge-level note, flagged as such (no in-tree mirror covers empty-interval arithmetic; the solver-free mirrors in `research/references/solver-free-static-analysis/` cover domains, not empty-interval operation semantics): the interval-arithmetic standard tradition (IEEE 1788-style) makes the empty interval an explicit value that *propagates* through operations rather than producing numeric garbage — the propagate-Empty pole of Decision 2. The chosen `⇒ Unbounded` variant instead follows the kernel's own existing convention (divisor-spans-zero `⇒ Unbounded`, `NumericInterval.cs:97`) and the ruled §1.8 rail assignment; Decision 2 carries the comparison.

## Inventory of what will be built (file-level)

| Artifact | Path | Content |
|---|---|---|
| Catalog wiring | `src/Precept/Language/Operations.cs` | `IntervalTransfer = MultiplyTransfer/DivideTransfer` on the wired cells (Decision 1 table); no other entry text changes |
| Kernel guard | `src/Precept/Language/NumericInterval.cs` | `IsEmpty`-guard first clause in `Add/Subtract/Multiply/Divide/Scale` (Decision 2) |
| Dead-reader removal | `src/Precept/Pipeline/ProofEngine.Intervals.cs` | Delete the zero-caller non-narrowed `TryIntervalContainmentProof` (`:467-487`) — it lacks the `:509` `IsEmpty` backstop and would pass `Empty` against bounds spanning it if ever revived (drift ledger); if a keep-reason emerges, give it the same backstop instead |
| Kernel tests | `test/Precept.Tests/` (interval kernel test class) | every ⊥-vector row: `Empty ⊕ x`, `x ⊕ Empty`, `Scale(Empty)` ⇒ `Unbounded`; `Negate/Shift` emptiness-preservation checks; divisor-spans-zero regression |
| Proof tests | `test/Precept.Tests/` (proof-engine money fixtures) | per wired cell: one Proved + one honest-Unresolved fixture via `Compiler.Compile` (never type-checker-only helpers); per omitted cell: conservative-reject fixture; wire-condition (magnitude-space commutation) fixtures for the verification-owed cells |
| Corpus reconciliation | `samples/*.precept` `# EXPECT:` lines | derived from corrected canon after owner sign-off — **not designed here** (per the slice methodology; acceptance criteria below are design-level only) |
| Docs | per § Doc-update enumeration | proposed, applied at slice landing |

## Decisions

*Spec-first note*: grepped `proof-engine.md`, `business-domain-types.md`, and the spec for prior settlement of transfer wiring and ⊥ handling — the seam is ruled at architecture §1.5 (owner-ratified plan, commit 3e3fb0dd lineage) and nothing in canon settles the two sub-choices below; neither is language surface.

### Decision 1: Per-cell disposition table — wire, wire-after-verification, omit

**Stakes**: medium-high (which money sites become provable; a wrong "wire" is a soundness bug, a wrong "omit" is only precision).

The whole op-meta family, enumerated not tripped-over — the binary money/quantity/price cells (`:417-700`), the adjacent ExchangeRate cells (`:702-720`), **and** the five business unary Negate cells (`:58-76`), which sit outside the binary block but are the same catalog seam (their pure-numeric siblings `:48/:52/:56` already carry `i => i.Negate()` transfers). Grep-verified that **no** cell below carries a transfer today (last `IntervalTransfer` assignment in the file is `:250`):

| Cell (line) | Result | Disposition | Why |
|---|---|---|---|
| `MoneyTimesDecimal` :437 | money | **Wire** `MultiplyTransfer` | same currency-magnitude space in and out; dimensionless scalar |
| `MoneyDivideDecimal` :442 | money | **Wire** `DivideTransfer` | same space |
| `MoneyDivideMoneySameCurrency` :452 | decimal | **Wire** `DivideTransfer` | same-space magnitudes; dimensionless ratio |
| `QuantityTimesDecimal` :538 | quantity | **Wire** `MultiplyTransfer` | normalized-unit space preserved under scalar multiply |
| `QuantityDivideDecimal` :543 | quantity | **Wire** `DivideTransfer` | same |
| `QuantityDivideQuantitySameDimension` :553 | decimal | **Wire** `DivideTransfer` | both operands normalized to the same UCUM base space (`business-domain-types.md:428`); ratio is space-free |
| `PriceTimesDecimal` :676 | price | **Wire** `MultiplyTransfer` | price normalization is purely multiplicative (`Scale(1/scale)`, `Intervals.cs:445-446`) and commutes with scalar multiply |
| `PriceDivideDecimal` :681 | price | **Wire** `DivideTransfer` | same commutation |
| `MoneyDivideMoneyCrossCurrency` :462 | exchangerate | **Wire-after-verification** | result magnitude is the plain quotient of two currency magnitudes (currencies carry no scale factors), but the *rate field's* bound space must be confirmed factor-one in-slice (acceptance criterion 7); fails ⇒ omit |
| `MoneyDivideQuantity` :472 | price | **Wire-after-verification** | quotient lands in per-normalized-unit price space; must confirm it matches the price bound space produced by the `:445-446` inversion; fails ⇒ omit |
| `MoneyDividePrice` :486 | quantity | **Wire-after-verification** | inverse of the above; same commutation check |
| `QuantityTimesQuantity` :599 | quantity (compound) | **Wire-after-verification** | product of base-normalized magnitudes vs the product-dimension bound space |
| `QuantityDivideQuantityCrossDimension` :563 | compound quantity | **Wire-after-verification** | same class |
| `MoneyDividePeriod` :499, `MoneyDivideDuration` :508, `QuantityDividePeriod` :573, `QuantityDivideDuration` :582, `QuantityTimesPeriod` :610, `QuantityTimesDuration` :614, `PriceTimesPeriod` :654, `PriceTimesDuration` :665 | various | **Omit** | period/duration magnitude space is unverified — named explicitly in the ruling (architecture §1.5: "period/duration magnitude space") |
| `PriceTimesQuantity` :643, `PriceDivideQuantity` :691 | money / price | **Omit** | the price-space cancellation shape — the ruling's "`USD/h × h` cells" class (architecture §1.5) |
| `MoneyPlusMoney` :417, `MoneyMinusMoney` :427, `QuantityPlusQuantity` :518, `QuantityMinusQuantity` :528, `PricePlusPrice` :619, `PriceMinusPrice` :631 | same type | **PARKED — Open question 1** | family-boundary ambiguity (plan title "× and ÷" vs architecture "whole family"; see OQ1) |
| `NegateMoney` :58, `NegateQuantity` :62, `NegatePrice` :66 (unary) | same type | **PARKED — Open question 1** | unary sign flip in the operand's own space — the same one-line soundness argument as the wired scalar cells, and the pure-numeric negations already carry `i => i.Negate()` (`:48/:52/:56`); but negation sits outside the plan title's "× and ÷" exactly like `+`/`−`, so the boundary call is OQ1's. `Negate` is audited emptiness-preserving (§ Semantic Rules), so no kernel-guard interaction either way. Precision-only while parked (no transfer ⇒ Unbounded ⇒ conservative reject) |
| `NegateDuration` :70, `NegatePeriod` :74 (unary) | same type | **PARKED — Open question 1** | same family-boundary park, **plus** these two share the period/duration magnitude-space caveat of the omitted class — if OQ1 rules unary negation in, these two still need the space verification before wiring (wire-after-verification at best, omit by default) |
| `ExchangeRateTimesMoney` :702, `ExchangeRateTimesDecimal` :713, `ExchangeRateDivideDecimal` :717 | money / exchangerate | **PARKED — Open question 1** | outside the named "money/quantity/price" family label but adjacent and same shape |

*Wire-after-verification* means: the cell ships wired **only if** an in-slice test demonstrates the magnitude-space commutation (a fixture whose operands use non-factor-one units, e.g. a `'[lb_av]'`-bounded quantity divisor, proves/rejects consistently with hand-computed normalized arithmetic); if the check cannot be demonstrated, the cell ships omitted. This is the ruled conservative default made test-enforceable — never a soundness risk, per "omission costs only precision, never soundness" (architecture §1.5, tradeoff leg).

- **Rationale**: the ruling itself fixes the principle (wire cells with verified result-space; omit unverified; never over-prove) — this table is the principle applied to every cell with an explicit disposition, per the enumerate-the-whole-family discipline. The three-way split exists because eight cells are *provably* single-space by inspection (dimensionless scalar or same-space ratio), five need a mechanical space check the design pass cannot fully discharge without running the normalizer, and ten are ruled-omitted by name/class.
- **Alternatives considered**: (a) *wire everything and rely on the containment backstop* — rejected: a transfer computing in the wrong magnitude space produces a wrong-but-finite interval, which CAN over-prove (unlike Unbounded); the exact vector the ruling's omit-rule exists to prevent. (b) *omit everything but the four money cells the plan names first* — rejected: under-delivers the ruled "whole family, enumerated not tripped-over" and leaves quantity/price authors with the same cannot-clear-by-hand wall for no soundness reason. (c) *defer the wire-after-verification cells wholesale to a later slice* — viable fallback, rejected as the default because the verification is a test fixture, not new machinery; the omit path remains the automatic outcome of a failed check.
- **Precedent**: every pure numeric op already carries its transfer via the same helpers (`Operations.cs:103-250`); the ruling's own precedent leg ("operand magnitudes already normalize to one space on every read path", architecture §1.5).
- **Tradeoff accepted**: authors of period/duration/price-cancellation arithmetic stay on conservative reject this slice — precision deferred until those spaces are verified (re-open pointer recorded in Scope § Deferred).

### Decision 2: Kernel ⊥-guard — `IsEmpty ⇒ Unbounded`, inside the five methods

**Stakes**: medium (engine-internal representation choice; both candidate variants are sound).

Add as the first clause of `Multiply`, `Divide`, `Add`, `Subtract`, and `Scale`: if either interval operand `IsEmpty` (for `Scale`: the receiver), return `Unbounded`. `Negate`/`Shift` stay unguarded (audited emptiness-preserving, § Semantic Rules), with the `Shift` sentinel-saturation corner recorded.

- **Rationale**: the ruled architecture text offers two variants — "an `IsEmpty ⇒ Unbounded` (or propagate-Empty) guard" (architecture §1.5) — and the surrounding ruled material is written to the first: the §1.5-vs-§1.8 resolution assigns infeasibility *detection* to the dict-write rail precisely because arithmetic output launders ⊥ to Unbounded ("infeasibility is never inferred from an Unbounded that arithmetic produced", architecture §1.10 region), making the kernel guard a last-resort backstop, not a signal source. `⇒ Unbounded` also keeps every downstream consumer safe without becoming ⊥-aware: `Union` — which `CaseSplit` arm-unions use and which is *not* in the guarded surface — computes min/max garbage over a raw `Empty` (`[0,-1] ∪ [5,10] = [0,10]`, `NumericInterval.cs:131-135`), whereas `Unbounded ∪ x = Unbounded` is already correct.
- **Alternatives considered**: *propagate-Empty* (each op returns `Empty` when an operand is empty — the IEEE-1788-style pole) — rejected: it preserves an infeasibility signal that the ruled rail design deliberately reads *before* arithmetic (dict-write time, §1.8), so the preserved signal has no sanctioned consumer; and it obligates every downstream interval consumer (`Union`, containment, rendering) to handle ⊥ correctly forever — a convention the guard exists to remove. The honesty benefit it targets is already delivered where ⊥ still means something (pre-arithmetic).
- **Precedent**: the kernel's own divisor-spans-zero `⇒ Unbounded` convention (`NumericInterval.cs:97`) — same in-kernel, conservative, caller-proof pattern; the reader-side `IsEmpty ⇒ unprovable` backstop (`Intervals.cs:505-509`) stays as defense-in-depth.
- **Tradeoff accepted**: an `Unbounded` produced by the guard is indistinguishable from a genuinely unknown range — the certificate/diagnostic for such a site reads "range unknown" rather than "arm infeasible." Accepted per the ruled rail split (the §2 dict-write rail owns the infeasible-arm story); plus the recorded `Shift` saturation corner rides unguarded until a non-UCUM caller appears.

## Acceptance criteria (design-level, test-shaped — the `# EXPECT:` matrix derives from corrected canon after owner sign-off, not here)

1. **Flagship proves**: the OrderLine worked sample's `Total` obligation is `Proved` with computed interval `[1 .. 5000]`; asserted via `Compiler.Compile` (never type-checker-only `Check` helpers).
2. **Honest residual**: the `PerUnit` obligation stays `Unresolved` with `computedInterval` `[0.02 .. 5000]` — finite, not `[−∞ .. +∞]` — asserted on the obligation's `computedInterval` (the payload MCP/hover project). The rendered PRE0078 message is **unchanged** by this slice (its template renders only the field name, `Diagnostics.cs:690`); a message-level assertion on the range is owed only if/when Slice 0's proposed PRE0078 re-rendering lands (same contingency as criterion 8).
3. **Per-wired-cell coverage**: each **Wire** cell has ≥1 Proved fixture and ≥1 Unresolved/violating fixture (both operands bounded; result outside the declared band).
4. **Per-omitted-cell conservatism**: each **Omit** cell has a fixture asserting the obligation does not prove (no over-prove; computed interval unbounded).
5. **⊥-vectors, kernel level**: unit tests assert `Empty.Add/Subtract/Multiply/Divide/Scale` (and the mirrored operand positions) return `Unbounded`; `Divide(x, Empty)` no longer throws; `Negate(Empty)`/`Shift(Empty, o)` for UCUM-range offsets stay empty.
6. **⊥-vectors, engine level**: a fixture in which a narrowing produces ⊥ upstream of arithmetic yields a verdict that is not `Proved`.
7. **Wire-condition fixtures**: each **Wire-after-verification** cell carries a non-factor-one-unit fixture whose prove/reject outcome matches hand-computed normalized arithmetic (e.g. money ÷ quantity with a `'[lb_av]'`-bounded divisor); a cell whose fixture cannot be made to pass ships omitted, and its conservatism fixture (criterion 4) ships instead.
8. **Certificate (contingent on Slice 0 having landed)**: a money product proof's certificate contains an `IntervalArithmetic` step (Measure = value) citing the `MoneyTimesDecimal` member, plus `UnitNormalization` where a unit rescale participated; the Slice-0 corpus coverage test still reports **zero** uncataloged step content and an unchanged 21-member vocabulary.
9. **No regression**: the divisor-spans-zero behavior (`:97`) and all existing integer/decimal/number transfer fixtures unchanged; full corpus green after `# EXPECT:` reconciliation.

## Dependencies

- **Upstream**: Slice 0 (three-way `ProofVerdict`, certificate format, four fail-open fixes) — **verified not yet built** (no `CertificateStep*`/`ProofVerdict` files exist in `src/Precept/` as of this writing); plan order puts Slice 0 first, and acceptance criterion 8 is contingent on it. The core of this slice (wiring + kernel guard + criteria 1-7, 9) has no hard Slice-0 dependency and degrades to the current `ProofDisposition` surface if sequenced earlier — but the plan's ordering stands unless the owner reorders.
- **Owner rulings**: Open questions 1-2 (neither blocks the core wire-set or the kernel guard).
- **Catalogs**: Operations (existing entries only), no Functions-catalog work (money `min/max/clamp/round` overload transfers are not in the ruled seam; not enumerated here — they stay omitted-conservative by default).

## Doc-update enumeration (per the CLAUDE.md routing table — PROPOSED corrections, none applied by this pass)

| Doc | Proposed change (at slice landing unless marked) |
|---|---|
| `docs/compiler/proof-engine.md` | Strategy-9/interval-containment section: extend ":1703 propagating through `IntervalTransfer` functions on integer / decimal / number arithmetic" to include the wired money/quantity/price cells; document the kernel ⊥-guard and the omit-is-conservative posture; flip the §13/:8 "§3 money — designed, not yet implemented" row when it ships |
| `docs/language/business-domain-types.md` | Add the money/quantity/price *arithmetic proof surface*: which operations participate in bound proofs, which conservatively reject (omitted cells), with the worked sample; keep the exactness commitments (:174-182) untouched |
| `src/Precept/Language/Operations.cs` (catalog = spec surface) | The wiring itself is the catalog update; per-cell XML docs note the transfer where non-obvious |
| `docs/compiler/diagnostic-system.md` | **None** — no code added/changed (PRE0078/PRE0083 pre-exist; payload precision improves, registry untouched) |
| `docs/tooling/mcp.md`, `docs/tooling/language-server.md` | **None** — no DTO or rendering surface change (verified: `computedInterval` already projected) |
| `docs/language/precept-language-spec.md` | **None** — no language-surface change |
| `docs/Working/compiler-readiness-plan-2026-07-12.md` + `-architecture.md` (Working docs) | Correct the family line-range: "`Operations.cs:437–559`" under-spans the actual family (`:417-700`; it excludes `MoneyPlusMoney/MinusMoney` :417/:427, all Price cells :619-691 that the same sentence's "whole family" phrasing and enumerated list partially include, and the five business unary Negate cells :58-76 that a plain "whole op-meta family" reading covers) — this imprecision is the root of Open question 1 and should be corrected to whatever the OQ1 ruling settles |

### Drift ledger (canonical docs this slice owns vs code)

| Doc claim | Code reality | Classification |
|---|---|---|
| `proof-engine.md:8` — "§3 money … designed, not yet implemented" | Correct: no business cell carries `IntervalTransfer` (grep-verified, last assignment `Operations.cs:250`) | **No drift** — honest status |
| `proof-engine.md:1703` — transfers propagate "on integer / decimal / number arithmetic" | Accurate today; becomes stale the moment this slice lands | **Intended-not-yet-built** — update owed at landing (row above) |
| `proof-engine.md:2613` — §3 listed among designed new strategies | Accurate | **No drift** |
| `business-domain-types.md:426-428` — typed-constant bounds extracted for containment; `max '5 kg'` vs `set … '6 [lb_av]'` proves via normalization | Constant path verified at source (`ApplyStaticUnitScaling`, `Intervals.cs:423-452`, handles `TypedTypedConstant` units); untouched by this slice | **No drift** |
| Architecture §1.5 / plan :309 — "`Operations.cs:437–559`" as the family span | Family actually spans `:417-700` (+ adjacent ExchangeRate `:702-720` + business unary Negate `:58-76`); the cited range excludes cells the prose includes and includes `Quantity±Quantity` cells the enumerated list omits | **Unintended imprecision in Working docs** — proposed correction above; surfaced as OQ1, not silently resolved |
| PRE0078 renderer passes a `(computed: …)` range clause (`ProofEngine.Diagnostics.cs:119-126`) | The registry template renders only `{0}` (`Diagnostics.cs:690` — "…on field '{0}'"), so the passed clause never appears in the message; live-verified: today's message shows no range even when `ComputedInterval` is set | **Pre-existing template/arg mismatch** — not this slice's to fix (registry untouched); the fix already has a proposed home in Slice 0's PRE0078 re-rendering (`certificate-steps-membership-2026-07-12.md`, § Error message); recorded so no later pass mistakes the dangling arg for rendered behavior |
| Design's ⊥-vector story cites the `:509` `IsEmpty` backstop in the narrowed containment reader | The dead non-narrowed sibling `TryIntervalContainmentProof` (`ProofEngine.Intervals.cs:467-487`, zero callers, grep-verified) lacks that backstop — its Min/Max comparisons would pass `Empty` `[0,-1]` against bounds spanning it (e.g. declared `[-5..10]`: `0 >= -5 && -1 <= 10` ⇒ true), a `Contains(⊥)`-class over-prove one revival away | **Dormant hazard, inert today** — this slice proposes deleting the dead method (Inventory row; or giving it the same `:509` backstop if a reason to keep it emerges), closing the counterexample to the "⊥-over-prove structurally impossible" claim |

No unintended drift found in the canonical docs this slice owns.

## Open questions (parked for owner — neutral framing; nothing here is settled by this design)

### OQ1 — The wire-set family boundary: `+`/`−` cells, unary Negate cells, and ExchangeRate cells

**Canonical area checked**: architecture §1.5 (quoted: "Attach `IntervalTransfer` to the **whole** money/quantity/price op-meta family (`Operations.cs:437–559`, enumerated not tripped-over: `MoneyTimesDecimal`, `MoneyDivideDecimal`, `MoneyDivideMoneySameCurrency`/`CrossCurrency`, `MoneyDivideQuantity/Price/Period/Duration`, and the quantity siblings)") and the plan §3 stub :298 (title: "bounded money/quantity arithmetic through **× and ÷**"). No locked decision settles the boundary: "whole family" ⊃ the enumerated list ⊃ what the line range `:437-559` actually spans, and three cell groups sit in the gap — the six `+`/`−` cells (`MoneyPlusMoney` :417, `MoneyMinusMoney` :427, `QuantityPlusQuantity` :518, `QuantityMinusQuantity` :528, `PricePlusPrice` :619, `PriceMinusPrice` :631), the five business unary Negate cells (`NegateMoney` :58, `NegateQuantity` :62, `NegatePrice` :66, `NegateDuration` :70, `NegatePeriod` :74 — the pure-numeric negations `:48/:52/:56` already carry transfers, making these the same seam), and the three ExchangeRate cells (`:702/:713/:717`). `Quantity±Quantity` is even *inside* the cited line range while absent from the enumerated list and the "× and ÷" title; the Negate cells sit outside every cited range but inside "whole … op-meta family" on a plain reading.

**Material facts (both directions)**: the `+`/`−` cells are same-space adds/subtracts whose transfer soundness is the same one-line argument as the wired scalar cells, and the flagship sample's `TaxableAmount <- Subtotal - DiscountAmount` / `LineTotal <- TaxableAmount + TaxAmount` (`samples/invoice-line-item.precept:27/:29`) stay unprovable without them; the money/quantity/price Negate cells are trivially same-space sign flips with an already-audited emptiness-preserving kernel op (`Negate`, § Semantic Rules), though the Duration/Period negations inherit the omitted class's space caveat; on the other side, the slice title, its S-M effort estimate, and its test budget were set to the ×/÷ reading, and the kernel guard covers `Add/Subtract` regardless (it protects the *kernel*, not the wire-set). All parked cells are precision-only while parked — unwired ⇒ Unbounded ⇒ conservative reject.

**Options (neutral)**:
- (a) Include the six `+`/`−` cells in this slice's wire-set; the Negate and ExchangeRate cells too (Duration/Period negation still gated on the space verification).
- (b) Hold this slice strictly to the ×/÷ enumerated list; `+`/`−`, Negate, and ExchangeRate cells ride a later pass (or §6/§2-adjacent cleanup) with their own fixtures.
- (c) Include `+`/`−` and the three same-space Negate cells (in-family, flagship-relevant / trivially sound), defer Duration/Period negation (space caveat) and ExchangeRate (outside the family label; conversion semantics deserve their own look).

### OQ2 — Pre-existing kernel crash: decimal corner/endpoint overflow (CONFIRMED live)

**Found during ⊥-guard verification; pre-existing, not introduced by this slice.** The four-corner and endpoint arithmetic is unguarded decimal arithmetic, and C# `decimal` always throws on overflow: `Min * other.Min` (`NumericInterval.cs:90/:100`) and `a + b` in `SentinelAdd` (`:42`) can throw `OverflowException` on legal authored bounds. **Confirmed live** (2026-07-13 probe): `field A/B as decimal min 1 max 1e28`-scale with `C <- A * B` crashes the compiler — the MCP compile tool returns "Internal error … `OverflowException: Value was either too large or too small for a Decimal`" instead of a diagnostic. This is a compiler-crash vector (robustness), not a false-`Proved` (soundness) — but it lives in exactly the methods this slice touches, and money wiring increases its reachable surface (large monetary bounds are ordinary).

**Canonical area checked**: architecture §2.1-2.3 fail-open inventory (F-series covers false-`Proved` holes and F4's ⊥ garbage; corner *overflow* appears nowhere); `bugs.md` conventions would take it as a BUG-NNN entry.

**Options (neutral)**:
- (a) Fold a saturate-to-`Unbounded` overflow guard into the same kernel pass (same "inside the methods, no call site can forget" posture; strictly conservative) — expands this slice's ruled kernel scope beyond F4.
- (b) Route it to the Slice-0 systematic sweep (architecture §2.2) as a robustness sibling of the fail-open family — keeps this slice's kernel scope exactly as ruled.
- (c) File as a standalone `bugs.md` entry with its own fix pass.

### Checked and NOT open

- **String-function length caps catalog-placement gap** (flagged by the Slice-0 revision, `ProofEngine.Lengths.cs:250-291`): verified string-length-measure only — it does not touch money/quantity value intervals or any cell in this slice. Not logged; no scope expansion.
- **Certificate vocabulary growth**: confirmed zero (§ Certificate confirmation) — not an open question.

---

## Status note (why Externally-Grounded, not Locked)

Every decision carries stakes-appropriate legs and every load-bearing claim is source-anchored at file:line, but this design was produced by an autonomous overnight pass: the two open questions are owner calls, the disposition table's parked rows depend on OQ1, and per the slice-review discipline the owner locks (or amends) at the slice boundary review. Nothing here should be treated as settled until that review. No canonical doc was edited by this pass; all doc changes above are proposals.
