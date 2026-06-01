# Phase 8 Slice 1 — Diagnostic-Emission Inventory & Classification — 2026-05-31

**Status**: **Phase 8 Slice 1** — read-only emission inventory & classification, complete 2026-05-31. Grounds the Slice 3 `/lifecycle-2-design` pass on a dedicated diagnostic-emission phase. No code changed. This is the inventory that defines Slice 3's scope.

**What this is**: every diagnostic-emission site in the compiler, enumerated from source (not inferred), classified as *legitimately-immediate* vs *obligation-in-disguise* vs *flow-sensitive-but-stage-correct*, plus the spec/implementation gap (declared-but-unwired codes) that bounds the *future* emission surface Slice 3 must design for.

**Method (closure, not idiom)**: Every `Diagnostic` in the compiler is born through exactly **one** factory — `Diagnostics.Create(DiagnosticCode, SourceSpan, params object?[])` (`src/Precept/Language/Diagnostics.cs:1330`). There are **zero** `new Diagnostic(...)` calls, no `Error`/`Warn`/`Info` variants. So the emission universe = the **239 `Diagnostics.Create` calls**, which live in exactly **18 pipeline files**. Nothing in the MCP server, language server, or runtime creates compiler diagnostics — they consume. This makes the wired-site enumeration provably complete.

---

## 1. Classification rubric

A check is decided against `docs/compiler/proof-engine.md` Decision 3 (type checker *stamps* `ProofRequirement`s; proof engine *discharges* them). Discriminating question per site: *could a guard/narrowing/flow-value-fact legitimately make a value that looks incompatible/unsafe here actually compatible/safe?*

- **S — legitimately immediate**: decided from declared types / AST structure / static-constant folding / statically-resolved-**concrete** values. No narrowing changes the answer.
- **O — obligation in disguise** (the Slice 1 target, the Site-A class): a value-level property matching a `ProofRequirement` kind whose answer could change under narrowing/flow, emitted immediately **only because the proof engine doesn't walk this context** (field/arg defaults, bounds, computed/non-`set` expressions).
- **F — flow-sensitive but stage-correct**: definite-assignment / read-before-write / coverage / reachability / dependency-graph analysis, decidable from typed structure without value reasoning. Not a proof obligation; relevant only to Slice 3's *emission-staging* axis.

`ProofRequirement` kinds (the value-level obligation taxonomy, `src/Precept/Language/ProofRequirement.cs`): Numeric, Presence, Dimension, QualifierCompatibility, QualifierChain, AssignmentQualifier, DimensionalProduct, IntervalContainment, LengthContainment, CountContainment, KeyPresence, IndexBounds.

---

## 2. Emission-locus map (wired surface — exhaustive, site-verified)

| Stage | Sites | S | O | F | Mechanism |
|---|---|---|---|---|---|
| Lexer | 14 | 14 | 0 | 0 | `Diagnostics.Create` → builder |
| Parser (4 files) | 42 | 40 | 0 | 2 | `Diagnostics.Create` → builder |
| Name-binder | 10 | 9 | 0 | 1 | `Diagnostics.Create` → builder |
| **Type checker** (9 files) | **158** (+3 catalog-CI) | ~140 | **3** | ~15 | `ctx.Diagnostics.Add(Diagnostics.Create(...))` |
| Graph analyzer | 11 | 1 | 0 | 10 | `diagnostics.Add(Diagnostics.Create(...))` |
| Proof engine | 1 obligation point + **6** direct | — | (obligation surface) | — | `CreateDiagnostic` + 6 SAT scans |

**~240 emission sites, 6 stages, 3 value classes.** The proof engine has **two** emission loci: the single obligation-discharge point (`ProofEngine.cs:158` → `CreateDiagnostic` → 17 `Diagnostics.Create` in `ProofEngine.Diagnostics.cs`) and **6 proof-stage-direct** whole-program SAT scans (5 in `ProofEngine.Satisfiability.cs` + `UnsatisfiableInitialState` at `ProofEngine.cs:169`) which are *not* stamped obligations.

---

## 3. Class-O — obligation-in-disguise population (2 wired, all type-checker — corrected from 3; see note below)

All three share one root cause: they evaluate a proof-shaped property **inline because the proof engine does not walk their context** (field/arg defaults, bounds, computed/non-`set`).

| Code | Site | Proof analog | Narrowing-unlock? |
|---|---|---|---|
| `OutOfRange` (PRE0079) | `TypeChecker.Validation.Modifiers.cs` via `TryReportNumericViolation` (~561) | `NumericProofRequirement` / `UnprovedModifierRequirement` | No — fires only on static constants (`TryGetStaticMagnitude` bail); **uniformity-only** |
| ~~`MaxPlacesExceeded`~~ | `TypeChecker.cs:1091` | ~~numeric bound~~ → **none** | **Corrected 2026-06-01 (Slice 3 Decision 6): NOT Class-O.** Per `business-domain-types.md:1581-1590`, maxplaces is static-only at compile time; its non-static enforcement (arithmetic-result-at-`set`) is **runtime**, not proof. No `ProofRequirement` kind, no proof role → legitimately **type-stage-owned (Class-S)**. Stays `Type`, not relocated. |
| `UnprovedAssignmentQualifierCompatibility` (residual) | `TypeChecker.Expressions.AssignmentQualifiers.cs:223` | `AssignmentQualifierProofRequirement` (already discharged at proof for walked contexts) | Would unlock if the context were walked |

> **Correction (2026-06-01)**: this slice originally counted **3** wired Class-O checks. Slice 3's spec-first verification (Decision 6) found `MaxPlacesExceeded` is not a proof-obligation-in-disguise — its value-level enforcement is runtime, not proof. The genuine wired Class-O population is **2** (`OutOfRange` + the assignment-qualifier residual); both relocate to the proof stage in Slice 3b. `MaxPlacesExceeded` is Class-S (type-stage-owned).

**Key structural finding**: the qualifier / unit / dimension families are *already correctly split* — resolved→immediate, open→stamped — and the code says so:
- Assignment qualifiers (`AssignmentQualifiers.cs:185-234`): `Resolved`+incompatible → immediate `QualifierMismatch`/`DimensionCategoryMismatch` (S); `Unknown`+proof-walked → **stamps** `AssignmentQualifierProofRequirement`; `Unknown`+not-walked → the residual above.
- Operand arithmetic (`Expressions.cs:1283-1286`): **returns early, deferring to the proof stage, whenever the op carries a `QualifierCompatibilityProofRequirement`.** The immediate `CrossCurrencyArithmetic`/`CrossDimensionArithmetic`/`CrossCountingUnitOperation` emits fire only for statically-resolved-concrete-different qualifiers (S). `×`/`÷` route to `DimensionalProductProofRequirement` (PRE0157) at proof.

So Slice 1 is **not** a pervasive "convert scattered emits to obligations" job. The obligation-conversion surface is 3 checks, one cause.

---

## 4. Class-F — flow-sensitive, stage-correct (NOT Slice 1 candidates)

Decidable from typed structure without value reasoning. Listed because Slice 3's *emission-staging* axis (orthogonal to obligation-conversion) covers them; they must **not** be mis-scoped as Site-A-class work.

- **Type checker** (`Validation.FieldState.cs`): `UninitializedFieldReadInInitialAssignment`, `UninitializedCrossFieldReadInInitialAssignment`, `ConstructionGuardReadsUninitializedField`, `MaterializedFieldSelfReference` (definite-assignment); `OmittedFieldReadInState`, `OmittedFieldSetInTargetState`, `RequiredFieldUnassignedOnEntry`, `RequiredFieldsNeedInitialEvent`, `InitialEventMissingAssignments` (coverage / definite-assignment — verified: `RequiredFieldUnassignedOnEntry` gates on `if (!hasSet)`).
- **Graph analyzer** (10/11 sites): `UnreachableState`, `StructuralSinkState`, `DeadEndState`, `UnhandledEvent`, `TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge`, `RequiredStateDoesNotDominateTerminal`, `AlwaysRejecting`, `StateAlwaysRejects`, `FieldNeverSet` (reachability / dominance / reject-DU-subtype / write-site). `AlwaysRejecting`/`StateAlwaysRejects` decide reject-ness from the row-outcome DU subtype, **not** guard satisfiability — not obligations.
- **Front-end**: `PreEventGuardNotAllowed`, `NonAssociativeComparison` (parser AST-shape); `CircularComputedField` (binder dependency-cycle).

---

## 5. Cross-stage dual-emissions (verified) — the concrete Slice 3 symptom

The same `DiagnosticCode` emitted from two stages, reconciled today by context-split, an ad-hoc guard, or (the name-resolution family) **not reconciled at all** — a latent double-emission.

> **Correction (2026-06-01, name-resolution investigation)**: this section originally listed **3** duals. The genuine count is **6** — the name-resolution family `UndeclaredField`/`UndeclaredState`/`UndeclaredEvent` is also dual-emitted (binder + type checker, ~10 sites) and was missed because it spans many sites. Added below. Slice 3 D4 (amended) consolidates all six.

| Code | Locus A | Locus B | How reconciled today |
|---|---|---|---|
| `UnprovedAssignmentQualifierCompatibility` | type checker `AssignmentQualifiers.cs:223` | proof engine `ProofEngine.Diagnostics.cs:89` | deliberate context split (walked vs not-walked) |
| `CircularComputedField` | name-binder `NameBinder.cs:305` | type checker `Validation.Structural.cs:248` | two independent cycle detectors |
| `NoInitialState` | type checker `TypeChecker.cs:704` | graph analyzer `GraphAnalyzer.cs:88` | ad-hoc `HasDiagnostic(...)` dedup guard at `GraphAnalyzer.cs:85` |
| `UndeclaredField` | name-binder `NameBinder.cs:714,775` | type checker `Normalization.cs:455`, `Expressions.Callables.cs:394,962`, `Expressions.cs:949` | **none — double-emission** (tolerant tests, `TypeCheckerTransitionTests.cs:196-199`) |
| `UndeclaredState` | name-binder `NameBinder.cs:734` | type checker `Normalization.cs:328`, `TypeChecker.cs:1214` | **none — double-emission** |
| `UndeclaredEvent` | name-binder `NameBinder.cs:754` | type checker `Normalization.cs:208`, `TypeChecker.cs:1150,1300` | **none — double-emission** |

Investigation conclusion: the name-resolution family is **redundant re-resolution, not type-gated** — all consolidatable to the binder (Bind owns; type checker defers to `UnresolvedTarget` markers). See Slice 3 D4 (amended).

---

## 6. Spec/implementation gap — the *future* emission surface (emit-or-retire)

The wired inventory is complete for *implemented* code, but the spec declares more than is wired. **162** `DiagnosticCode` members; **~140** referenced in any pipeline emission path; **23** never referenced — of which **4** (`CaseInsensitiveFieldRequiresTilde*`) are actually wired dynamically via catalog `CIDiagnosticCode` metadata (literal-grep false positives) and **1** (`McpToolInternalError`) is MCP-server-side. So **~18 spec-declared compiler diagnostics are genuinely unimplemented** — same territory as the plan's 148-vs-162 discrepancy and the Phase 9 emit-or-retire findings.

Triage by **declared enum category + registry `TriggerCondition`/spec rule** (grounded 2026-05-31 via `DiagnosticCode.cs` / `Diagnostics.cs` message+trigger + `precept-language-spec.md`):

| Class when wired | Codes |
|---|---|
| **S — Parse/syntactic** | `EventHandlerDoesNotSupportGuard`, `InvalidCallTarget`, `UnexpectedKeyword` |
| **S — Type/content** | `InvalidInterpolationCoercion`, `ChoiceElementTypeMismatch`, `ChoiceMissingElementType` (F-LANG-SPEC-09), `MissingOrderingKey`, `NonOrderableCollectionExtreme` (F-LANG-SPEC-13), `MissingTemporalUnit` |
| **S — typed-constant temporal content** | `InvalidDateFormat`, `InvalidDateValue`, `InvalidTimeValue`, `InvalidInstantFormat`, `InvalidTimezoneId`, `FractionalUnitValue` (literal whole-number check — message keys on the literal) |
| **S — declaration-shape / constant-arg** | `UnqualifiedPeriodArithmetic` (fix is a `period of '...'` declaration qualifier — spec § temporal table:1371/1374); `FunctionArgConstraintViolation` (spec ex. `round(x, -1)` — constant-literal arg constraint:1577; the narrowable numeric variant already routes through proof-stage `NumericProofRequirement`) |
| **O — future obligation surface (confirmed)** | `NullInNonNullableContext` — trigger: *"An optional field used in a context requiring a guaranteed non-null value **without a prior 'is set' guard**"* (`Diagnostics.cs:179`); guard-discharged **presence** → belongs in the existing proof-stage `PresenceProofRequirement` / `UnprovedPresenceRequirement` channel (or retires as a dup of it) |

**Grounded result**: of the 4 provisional future-O candidates, **only `NullInNonNullableContext` is a genuine future-O** (presence-family); the other 3 are S (literal / declaration-shape / constant-arg, narrowable variants already covered by existing proof obligations).

**Implication for Slice 3**: the wired obligation surface is **2** Class-O (corrected from 3 — `MaxPlacesExceeded` reclassified Class-S per Slice 3 D6); the *eventual* surface is 2 wired + 1 future ≈ **3**, all presence/numeric/qualifier families the proof engine already models. Slice 3 must design the emission model against the **spec** surface, with a defined home for `NullInNonNullableContext` (presence channel), or Phase 9 will wire it into the legacy scatter and re-create the problem Slice 3 exists to fix. **Phase 8 (emission architecture) and Phase 9 (completeness/emit-or-retire) are coupled at this seam**: Slice 3 defines the model + the future-emitter home; Phase-9 wiring targets that model.

---

## 7. What Slice 3 inherits from this slice

1. Obligation-conversion is narrow: **2** wired Class-O (`OutOfRange`, the assignment-qualifier residual — `MaxPlacesExceeded` is Class-S per Slice 3 D6) + 1 future-O, one root cause (non-proof-walked contexts: defaults/bounds/computed). The design question is *extend the proof-walk to those contexts, or unify only emission and accept inline evaluation there.* (Separately: **6** cross-stage dual-emissions, not 3 — see §5 correction.)
2. "One terminal emission phase" spans ≥4 loci, most of them flow/graph/SAT, **not** obligations — it is not equivalent to "make everything a proof obligation." Class F and the 6 proof-direct SAT scans must have a home in the model.
3. The 3 dual-emissions (one with an ad-hoc dedup guard) are the symptom to cure.
4. Design against the spec surface (§6), not the wired surface.

**Tangential (parked for Phase 9, not Slice 1)**: `NonAssociativeComparison` category/stage label mismatch (`TypeSystem` category on a `Parse`-stage check, `Parser.Expressions.cs:415`); the 3 dual-emissions as diagnostic-identity candidates.

## Evidence grade

§§2-5 are code-grounded and exhaustive for the wired surface. §6's unimplemented-code triage is grounded in registry `TriggerCondition`/message templates + spec rules (2026-05-31); the future-O verdict (1 of 4: `NullInNonNullableContext`) is confirmed against `Diagnostics.cs:179` + spec. The only residual uncertainty is whether `NullInNonNullableContext` should be *implemented as* the existing `UnprovedPresenceRequirement` or *retired as a dup* — a Slice 3/Phase-9 decision, not an open classification question.
