Read-only inventory complete. Full map below. All line numbers are file:line in the committed working tree.

---

# Precept proof engine — current-state inventory

The proof engine is a static, pass-based checker that runs after type-checking. For each operation that could fault (divide, sqrt, collection access, overflow, out-of-band assignment, etc.) it collects an **obligation** ("this must be true"), then tries a fixed list of **strategies** to discharge it. If none succeeds, it emits a diagnostic. Its guiding rule is **prove-or-reject**: an unprovable case is rejected, never deferred to runtime (stated in code at `ProofEngine.cs:432`, `ProofEngine.Lengths.cs:44`, etc.).

## 1. File inventory

### Pipeline (the engine itself) — `src/Precept/Pipeline/`

| File | Lines | Role (one line) |
|---|---|---|
| `ProofEngine.cs` | 1200 | Entry point `Prove()`; obligation collection (walks actions/rules/ensures/fields); the sequential-flow prefix-write tracking; **count/cardinality** obligation generation (`CountContainmentObligation`, `AdvanceCount`, `SeedCountInterval`, `BuildSiblingCountExclusions`); the `TryDischarge` strategy cascade. |
| `ProofEngine.Strategies.cs` | 1385 | The discharge strategies: Literal, DeclarationAttribute, GuardInPath, FlowNarrowing (field-to-field relational), CollectionGrowth, IndexBounds, KeyPresence; guard-branch decomposition into DNF (`ExtractGuardBranches`); operator subsumption tables. |
| `ProofEngine.Intervals.cs` | 841 | Interval computation `IntervalOf`/`IntervalOfNarrowed` over expressions; field/arg/element band extraction; guard-narrowed per-field interval builder (`BuildNarrowedIntervals`); **relational half-line** narrowing (`RelationalHalfLine`); sibling-reject negation (`NegateConstraintToInterval`); unit scaling. |
| `ProofEngine.Composition.cs` | 903 | The **sign-set abstract domain** (`NumericSignSet` +/0/−, add/mul/div/negate transfer); trusted-rule/ensure fact folding; unconditional field-to-field relational facts; interpolated-assignment modifier propagation; blocked-constraint (circularity) discipline. |
| `ProofEngine.Qualifiers.cs` | 954 | Qualifier-compatibility proof (currency/unit/dimension match); dimensional-product proof (`DimensionVector`); qualifier resolution across field/element/interpolated-constant. |
| `ProofEngine.Satisfiability.cs` | 539 | Lateral "Pass 1.5": unsatisfiable/tautological guards, contradictory/vacuous/self-unsatisfiable rules, default-violates-rule; `NarrowByConstraint`, per-field interval fold. |
| `ProofEngine.Analysis.cs` | 831 | Obligation *collectors* for defaults / computed fields / length / count; initial-state satisfiability via **constant folding** (`ConstantFold`/`FoldValue`/`EvaluateBinaryOp`); constraint-influence projection. |
| `ProofEngine.QualifierNarrowing.cs` | 316 | Assignment-qualifier discharge via guard narrowing; `NarrowedValueFromGuard` (the "guard narrows it to X" diagnostic hint). |
| `ProofEngine.Lengths.cs` | 351 | String-length interval (`StringLengthIntervalOf`) and its containment proof; **count containment** proof `TryCountContainmentProof`. |
| `ProofEngine.Diagnostics.cs` | 495 | Turns an unresolved obligation into a diagnostic; maps requirement-kind → diagnostic code + fault code. |
| `ProofLedger.cs` | 147 | Output record: `ProofObligation` (with `Disposition`, `Strategy`, `ComputedInterval`, and the `ReassignedBefore`/`CountEstablishedBefore`/`CountInvalidatedBefore` flow-state stamps), `ProofDisposition`, `ProofStrategy` enum, `FaultSiteLink`. |

### Language / catalog (the vocabulary the engine reads) — `src/Precept/Language/`

| File | Lines | Role |
|---|---|---|
| `NumericInterval.cs` | 156 | The interval type: `Add/Subtract/Multiply/Divide/Negate/Scale/Shift/Union/Intersect/Contains`, sentinel (±∞)-safe, `IsEmpty` = ⊥. |
| `ProofRequirement.cs` | 432 | The 13 requirement subtypes (DU); `ProofSubject` (Param/Self); `ProofRequirementMeta` (kind→diagnostic); `ProofSatisfaction` (positive carrier facts from modifiers). |
| `ProofRequirementKind.cs` | 54 | The 13-value enum. |
| `ProofRequirements.cs` | 38 | `GetMeta(kind)` switch + `All`. |
| `Constraint.cs` / `ConstraintKind.cs` / `Constraints.cs` | 41 / 25 / 42 | Constraint (rule/ensure) identity metadata — not the prover. |
| `Numeric/TypedConstantNormalizer.cs` | 109 | UCUM/currency magnitude normalization (used by the interval math for quantity/price). |

Interval-transfer wiring lives in the operation/function catalogs: `Operation.cs:108` (`IntervalTransferFn`), `Function.cs:31` (`FunctionIntervalTransferFn`), declared per-entry in `Operations.cs` and `Functions.cs`.

## 2. Capability present / absent (with cites)

### Interval arithmetic — PRESENT (decimal domain)
- **Ops:** `+ − ×` and unary negate carry interval transfers for Integer/Decimal/Number (`Operations.cs:109,113,117,140,144,148,171,175…`; `NumericInterval.cs:73–108`). Division: `NumericInterval.Divide` returns `Unbounded` when the divisor interval straddles 0 (`NumericInterval.cs:94–102`); wired via `DivideTransfer` (`Operations.cs:126,157`).
- **Multiplication is corner-evaluated** — min/max of the four endpoint products (`NumericInterval.cs:90`). Dependency-blind (X·X over [−1,1] yields [−1,1]).
- **Functions:** `min`/`max`/`clamp`, `abs` (sign-aware split), `floor`/`ceil`/`truncate`/`round` all have hand-written `IntervalTransfer` (`Functions.cs:44–149`).
- **Modulo:** no `IntervalTransfer` — result is `Unbounded` (`Operations.cs:128,159,190,221…`, none carry a transfer).
- **`sqrt` / `pow`:** carry a *proof requirement* (operand ≥ 0; exponent ≥ 0 — `Functions.cs:189`) but **no `IntervalTransfer`**, so their result interval is `Unbounded`.
- **Monotone handling:** per-function, not general. `abs` splits on sign; `floor`/`ceil`/`round` apply the monotone function to both endpoints. There is **no** "declare monotone → auto-derive endpoint transfer" framework — each transfer is written by hand.

### Relational reasoning (field-to-field) — PRESENT but shallow, single-hop
- **spec:256 discharge** lives in `TryFlowNarrowingProof` (`Strategies.cs:1078`): proves `A − B ⊕ 0` from a relation `A op B`. The operand shape is **subtraction only** (`IsSubtractionOp`, `Strategies.cs:1277`).
- Relations are extracted **only** as `fieldRef op fieldRef` (`ExtractFieldToFieldLeaf`, `Strategies.cs:1240`) — no constants, no coefficients (`aX + bY ≤ c` is not representable).
- **Depth is one hop, explicitly non-transitive.** The interval fold reads the related field via the *bare* non-relational interval "ONE HOP … never the dict being built" (`Intervals.cs:629–654`); the sign fold says the same ("never from the subject's own unproven obligation … cannot chase a third field," `Composition.cs:512, 538`). There is no closure or chaining across ≥2 relations.
- Sources: the row/handler guard, **or** unconditional (`when`-less) rules (`RuleSourcedFieldToFieldBranches`, `Strategies.cs:1159`; `CollectUnconditionalRelationalFacts`, `Composition.cs:539`). Guarded rules contribute no global relation.
- Guard structure is decomposed into disjunctive normal form: AND cross-products, OR unions branches (`ExtractGuardBranches`, `Strategies.cs:794`). Every OR branch must independently discharge. This is the only "search" and it is bounded by guard size (exponential in AND/OR nesting).
- An empty relational intersection is treated as ⊥/infeasible and withheld, not as a vacuous pass (`Intervals.cs:646–654`; `RelationContradictsSubjectBounds`, `Composition.cs:562`).

### Count / cardinality delta (spec:258) — PRESENT
- Every mutation of a `mincount`/`maxcount` field advances a single per-field integer interval by a sound per-action delta: grow +1/+1 (or +0/+1 for a dedup set), shrink −1/−1 (or −1/0 for a possible-no-op remove), clear → [0,0], set → drop (`CountContainmentObligation` + `AdvanceCount`, `ProofEngine.cs:579–671`).
- Seed = declared `[mincount, maxcount]`, narrowed by same-context `count` guards and by routed reject-row siblings in the integer domain (`SeedCountInterval`, `BuildSiblingCountExclusions`, `ProofEngine.cs:689,761`).
- Discharge is prove-or-reject against the band (`TryCountContainmentProof`, `Lengths.cs:333`). Length containment is the exact sibling (`TryLengthContainmentProof`, `Lengths.cs:25`).

### Obligation framework — how one is created / carried / discharged
- **13 requirement kinds** (`ProofRequirementKind.cs`): Numeric, Presence, Dimension, Modifier, QualifierCompatibility, QualifierChain, IntervalContainment, LengthContainment, CountContainment, KeyPresence, IndexBounds, DimensionalProduct, AssignmentQualifier.
- **Created** four ways: (1) catalog-declared `ProofRequirement[]` on an operator/function/accessor/action (`Operation.cs:93`, `Function.cs:24`, `Type.cs:118`, `Action.cs:24`); (2) a `DynamicObligationGenerator` on an action (`Action.cs:30`, used by set-into-bounded-field, `Actions.cs:76`); (3) the presence-walk over optional field refs (`WalkExpression`, `ProofEngine.cs:275`); (4) the default/computed/count/length collectors (`Analysis.cs:420,538,586,648,693`).
- **Carried** as `ProofObligation` (`ProofLedger.cs:24`): requirement + site expression + context + the sequential-flow stamps (`ReassignedBefore` etc., so a guard fact goes stale after the field is rewritten — `WalkActions`, `ProofEngine.cs:409`).
- **Discharge tiers** = an ordered if-cascade in `TryDischarge` (`ProofEngine.cs:1054`), first hit wins: Literal → DeclarationAttribute → CollectionGrowth → GuardInPath → FlowNarrowing → three Qualifier strategies → DimensionalProduct → CompositionalConstraint(sign-set) → IntervalContainment → then kind-specific Length/Count/KeyPresence/IndexBounds. Miss on all → `Unresolved` → `CreateDiagnostic` (`Diagnostics.cs:11`). Strategies are named in the `ProofStrategy` enum (`ProofLedger.cs:100`).

### Confirmed ABSENT today
- **No linear-arithmetic solver.** No Farkas, Fourier–Motzkin, simplex, octagon, DBM, or LP code anywhere in `src/Precept` (grep returned nothing). The single "octagon/DBM" token is an *analogy in a comment* (`Composition.cs:554`). The only numeric abstract domains are the closed **interval** and the 3-point **sign set**.
- **No counterexample / witness generation.** A failed proof produces `Unresolved` → a diagnostic that *describes* the requirement and, for qualifier/assignment/count kinds, a guard-narrowed-vs-required hint (`NarrowedValueFromGuard`, `Diagnostics.cs:103,163`) — but **never a concrete falsifying field assignment**. "witness" appears only in a doc-comment for the contradictory-rule *partner* rule (`DiagnosticCode.cs:352`), not a value witness.
- **No weakest-precondition / backward substitution.** Collection and flow are forward (forward obligation walk + forward prefix-write stamping). The only backward-flavored moves are local: guard negation for sibling reject rows (`NegateConstraintToInterval`, `Intervals.cs:819`) and relational half-lines. No expression-level WP, no printable derived precondition.

## 3. Extension points (how a new proof capability is added)

There is **no strategy registry or visitor table** — dispatch is a hand-written cascade plus subtype pattern-matching. Adding capability touches these seams:

1. **New requirement kind:** add the enum value (`ProofRequirementKind.cs`), the DU record (`ProofRequirement.cs`), and the meta case in `ProofRequirements.GetMeta` (`ProofRequirements.cs:13`) with its `DiagnosticCode`. The `[CatalogDU]` attribute + a Roslyn analyzer enforce completeness.
2. **Attach obligations to language surface:** every catalog entry type already accepts `ProofRequirement[]?` — operators (`Operation.cs:93`), actions (`Action.cs:24` + `DynamicObligationGenerator` at `:30`), functions (`Function.cs:24`), type accessors (`Type.cs:118,156,170`).
3. **New discharge strategy:** write a `Try…Proof(obligation, semantics)` method and splice it into the `TryDischarge` cascade (`ProofEngine.cs:1054`), plus a `ProofStrategy` enum value (`ProofLedger.cs:100`).
4. **New interval transfer** for an op/function: set `IntervalTransfer` / `FunctionIntervalTransferFn` on the catalog entry (`Operation.cs:108`, `Function.cs:31`) — consumed generically by `IntervalOfNarrowed` (`Intervals.cs:104,124`).
5. **Positive carrier facts from modifiers:** `ProofSatisfaction` DU (`ProofRequirement.cs:383`), matched by `SatisfactionCovers` (`Strategies.cs:311`) — how `nonnegative`/`positive`/`nonzero` discharge sign/interval obligations.

Note: *what* to prove is catalog-driven (rule-compliant); *how* to discharge is the hand-written cascade in `TryDischarge` — that cascade is the one place a new strategy is wired in by editing code, not by adding a catalog row.

## 4. Reusable vs greenfield — per target capability

- **Linear-relation proving** — *Partial reuse.* Reuse: single-hop `fieldRef op fieldRef` extraction, sign-set fold, half-line interval narrowing, DNF guard decomposition (`Strategies.cs:1078,1174`; `Composition.cs:539`; `Intervals.cs:670`). Greenfield: constants/coefficients in relations, operand shapes beyond subtraction, and any **transitive closure** (current code is deliberately one-hop, `Intervals.cs:629`, `Composition.cs:512`). A Farkas/FM/elimination core is entirely new.

- **Product / corner evaluation** — *Reusable.* `NumericInterval.Multiply`/`Divide` already do sentinel-safe 4-corner min/max (`NumericInterval.cs:85–102`); sign-set has `MultiplySignSets`/`DivideSignSets` (`Composition.cs:847,870`). Greenfield only if you need dependency-aware products (shared variables) or >2-way joint corners.

- **Aggregate bound-transfer** (e.g. `.sum` ∈ count × element-band) — *Mostly greenfield.* Reuse: `NumericInterval` algebra, the count interval machinery, and element-band extraction `GetElementNumericBounds`/`ElementNumericInterval` (`Intervals.cs:279,304). But `.sum`/`.average` are `FixedReturnAccessor` and are **explicitly excluded** from carrying the element band today (`Intervals.cs:84–97`) — the transfer rule itself does not exist.

- **Monotone functions** — *Partial.* Reuse the per-function `IntervalTransfer` pattern and the `abs` sign-split precedent (`Functions.cs:44–149`). Greenfield: a general "monotone" declaration that auto-derives the endpoint transfer, and transfers for `sqrt`/`pow`/`modulo` (currently none → `Unbounded`).

- **Weakest-precondition + printing** — *Greenfield.* No WP or backward-substitution machinery exists. Reusable substrate: the DNF guard extractor, `NumericInterval` narrowing, guard-negation (`Intervals.cs:819`), and the `DescribeExpression` renderer (`ProofEngine.cs:986`) for printing. The backward computation and a printable derived precondition are new.

- **Counterexample witnesses** — *Greenfield, but with a running start.* Today failure → `Unresolved` → descriptive diagnostic; no falsifying assignment. Reusable: the per-field narrowed `NumericInterval` is already computed and even **stored on the obligation** (`ComputedInterval`, `ProofLedger.cs:31`; enriched at `ProofEngine.cs:141`), and interval intersection already localizes ⊥ (`Intervals.cs:646`) — a witness generator could sample those intervals. The witness *object*, its propagation back to concrete field values, and its rendering are new.

- **Certificate emission / independent checker** — *Greenfield, with a proto-certificate present.* The `ProofLedger` already carries, per obligation, `Disposition` + which `Strategy` discharged it + `ComputedInterval` + `FaultSiteLink`s, plus structured `ProducedFacts` consumed by LS/MCP (`ProofLedger.cs:6–31,100`). That is a record of *what* was proved and *by which* strategy — not a machine-recheckable proof term. A serialized certificate format and a separate re-verifier are entirely new; the ledger is the natural attachment point.