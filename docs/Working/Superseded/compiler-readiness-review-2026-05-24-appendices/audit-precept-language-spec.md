# Appendix — `precept-language-spec.md` Audit (Sub-Agent Report)

**Date**: 2026-05-24
**Scope**: `docs/language/precept-language-spec.md` (2,038 lines) + `docs/language/README.md` vs `src/Precept/` implementation
**Author**: Sub-agent of the 2026-05-24 compiler-readiness review
**Findings**: 13 — 3 P0, 5 P1, 5 P2
**Parent doc**: [../compiler-readiness-review-2026-05-24.md](../compiler-readiness-review-2026-05-24.md)

## § A. Methodology

Read `precept-language-spec.md` exhaustively (§0 Preamble incl. design principles, language model, governance, execution properties, graph analyzer contract §0.5, proof engine contract §0.6; §1 Lexer; §2 Parser; §3 Name binding & type checking; §3A Language semantics; §4 Graph analyzer; §5 Proof engine; Open Questions; Cross-references). Cross-checked against:
- Catalog source: `Tokens.cs` (518 lines), `TokenKind.cs`, `Constructs.cs`, `Functions.cs`, `Modifiers.cs`, `DiagnosticCode.cs`, `Operators.cs`, `Types.cs`, `Quickstart.cs`
- Pipeline source: `Parser.Expressions.cs`, `Lexer.cs`, `ProofEngine.cs`, `ProofEngine.Diagnostics.cs`, `TypeChecker.Validation.CI.cs`
- Runtime stubs: `Evaluator.cs`, `Version.cs`
- Authoritative gap list: `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs`
- 14 MCP `precept_compile` probes verifying suspected drift cases

## § B. Coverage Matrix (by spec section)

| Spec § | Feature area | Catalog/Impl status |
|---|---|---|
| §0.1 #9 | Mandatory `because` on every rule AND ensure | 🔴 **DRIFT** — `Constructs.cs:110` requires for rule; `:133,174` makes optional for ensures |
| §0.1 #11 | Static completeness | ⚠️ depends on §0.6 #9–#10 (unimplemented) |
| §0.5 #1 | BFS/DFS reachability from initial | ✅ `GraphAnalyzer.cs:295` |
| §0.5 #2 | Terminal state identification | ✅ wired |
| §0.5 #3 | Dead-end state detection | ✅ `GraphAnalyzer.cs:132,248` |
| §0.5 #4 | Edge analysis for `guarded`/`entry`/`isolated`/`universal` event modifiers | 🔴 **DRIFT** — none of these modifiers exist in catalog |
| §0.5 #5 | Dominator analysis for `required`/`milestone` | ⚠️ partial — Required ✓, Milestone missing |
| §0.5 #6 | Reverse-reachability for `irreversible`/`sealed after` | ⚠️ partial — Irreversible ✓, `sealed after` missing |
| §0.5 #7 | Row-partition analysis for `writeonce`/`sealed after` | 🔴 **DRIFT** — neither exists |
| §0.5 #8 | Outcome-type analysis for `advancing`/`settling`/`completing`/`absorbing` | 🔴 **DRIFT** — none exist |
| §0.6 #1 | Numeric interval reasoning | ✅ `NumericInterval.cs`, `ProofEngine.Intervals.cs` |
| §0.6 #2 | Relational reasoning across fields | ✅ `FieldToFieldConstraint` |
| §0.6 #3 | Divisor safety (DivisionByZero) | ✅ PRE0083 wired |
| §0.6 #4 | Non-negative proof obligations (sqrt, pow) | ✅ PRE0084 wired |
| §0.6 #5 | Unit-aware interval reasoning (UCUM) | ✅ verified `5 kg` vs `10 [lb_av]` compares correctly |
| §0.6 #6 | Assignment range impossibility | ⚠️ partial — NumericOverflow wired; OutOfRange deferred |
| §0.6 #7 | Contradictory rule detection | 🔴 **DRIFT** — no diagnostic code; verified silent acceptance |
| §0.6 #8 | Vacuous (tautological) rule detection | 🔴 **DRIFT** — no diagnostic |
| §0.6 #9 | Dead guard detection (always-false `when`) | 🔴 **DRIFT** — PRE0082 defined; "no emission site wired" |
| §0.6 #10 | Tautological guard detection (always-true `when`) | 🔴 **DRIFT** — no diagnostic |
| §0.6 #11 | Compile-time rule enforcement against defaults | ✅ UnsatisfiableInitialState PRE0115 wired |
| §0.6 #12 | Sharpening of routing diagnostics from proven-dead guards | 🔴 not impl (depends on #9–#10) |
| §0.6 #13 | Structured proof attribution | ✅ `ProofLedger.cs` |
| §1.1 | Token vocabulary tables | ⚠️ omits `BackArrow` token |
| §1.2 | Reserved keyword set | ✅ `Tokens.Keywords` |
| §1.3 | Literal syntax | ✅ `Lexer.cs` |
| §1.5 | Operator scan priority | ⚠️ omits `<-` |
| §1.6 | Dual-use disambiguation | ✅ `DisambiguationEntry.cs` |
| §2.1 | Expression precedence | ⚠️ §2.1 table omits `<-` BackArrow |
| §2.2-2.7 | Declaration grammar, error recovery, parser diagnostics | ✅ wired |
| §3.1-3.6 | Binding/checking, widening, name resolution, expression typing | ✅ wired |
| §3.7 | Built-in function catalog | ⚠️ `~startsWith`/`~endsWith` first-arg-must-be-`~string` not enforced |
| §3.8 | Semantic checks | ⚠️ `ChoiceElementTypeMismatch` "no emission site wired" |
| §3.9 | Error recovery / ErrorType | ✅ `TypedErrorExpression` |
| §3.10 | Diagnostic catalog grouping | ⚠️ 11 codes "no emission site wired" |
| §3A.1 | Constraint semantics | ⚠️ Runtime evaluator that enforces collect-all/first-match is stub |
| §3A.2 | Outcome verdict space | 🔴 **DRIFT** — `Outcomes.cs` enum exists; `Evaluator.Fire`/`Update` throw NotImplementedException |
| §3A.3 | Constraint violation subject attribution | 🔴 not implemented (runtime missing) |
| §3A.4 | Mutation atomicity | 🔴 runtime missing |
| §3A.5 | Entity construction | ⚠️ compile-time enforcement complete; runtime `Create()` missing |
| §3A.6 | Inspection as first-class operation | 🔴 `Evaluator.InspectFire/InspectUpdate` throw |
| §4 | Graph analyzer | ⚠️ partial — implementation present for `terminal`/`required`/`irreversible`; not for §0.5 unimplemented modifier surface |
| §5 | Proof engine | ⚠️ partial — qualifier-oriented checks delivered; guard/rule satisfiability not |

## § C. Drift Findings (Full)

### F-LANG-SPEC-01 — Mandatory `because` not enforced on `ensure` declarations [P0]
**Evidence**: Spec line 20, 106 declare non-negotiable; `Constructs.cs:133,174` makes `SlotOptBecauseClause` for state/event ensure (vs required `SlotBecauseClause` for rule). MCP verified: `in Draft ensure Score >= 0` (no because) compiles clean.
**Description**: Non-negotiable Principle 9 violated. Doc says "every rule and ensure must carry…" but only rule enforces.
**Fix**: Promote `because` to required slot on `StateEnsure`/`EventEnsure`, OR amend spec to scope mandatory rationale to rule only.
**Effort**: S

### F-LANG-SPEC-02 — Dead guard detection (`UnsatisfiableGuard` PRE0082) unimplemented [P0]
**Evidence**: Spec §0.6 #9; code defined `DiagnosticCode.cs:256`; `DiagnosticCoverageAllowLists.cs:74` "no emission site wired". MCP verified: `when Score > 200` on `min 0 max 100` compiles clean.
**Description**: Promised proof engine feature with diagnostic code reserved but no detection logic.
**Fix**: Wire proof-engine pass evaluating each `when` guard interval; emit PRE0082 when provably empty.
**Effort**: M

### F-LANG-SPEC-03 — Contradictory rule detection unimplemented [P1]
**Evidence**: Spec §0.6 #7; no `DiagnosticCode.ContradictoryRule`; MCP verified: contradictory rules silently accepted.
**Fix**: Add diagnostic code + pairwise interval intersection check in proof engine.
**Effort**: M

### F-LANG-SPEC-04 — Vacuous rule detection unimplemented [P2]
**Evidence**: Spec §0.6 #8; no diagnostic code; no impl.
**Fix**: Add diagnostic + proof-engine pass (often shares machinery with F-03).
**Effort**: M

### F-LANG-SPEC-05 — Tautological guard detection unimplemented [P2]
**Evidence**: Spec §0.6 #10; no diagnostic; MCP verified: `when Score >= 0` on `min 0` compiles clean.
**Fix**: As above; share pass with F-02.
**Effort**: S-M

### F-LANG-SPEC-06 — `~startsWith`/`~endsWith` don't enforce first-arg-must-be-`~string` [P1]
**Evidence**: Spec §3.7:1496-1497; `Functions.cs:296-316` declares `(string, string) → boolean`; `TypeChecker.Validation.CI.cs` only enforces inverse (string passed to ~startsWith silently accepted).
**Fix**: Tighten overload signature, OR add validation in CI.cs.
**Effort**: S

### F-LANG-SPEC-07 — Runtime Evaluator entirely `NotImplementedException` [P0]
**Evidence**: `Evaluator.cs:79-155` — Fire/Update/InspectFire/InspectUpdate/Restore all throw; `Version.cs:85-108` same. Spec §3A makes entire semantic model dependent on this.
**Description**: Largest single gap between spec and code. §3A.2-6 (verdict space, attribution, atomicity, inspection, construction execution) all depend on stubbed runtime. This is the next-phase work the review is gating.
**Fix**: Land runtime evaluator with descriptor-keyed model (D8/R4). Until then mark §3A "Implementation State: compile-time only; runtime evaluator pending."
**Effort**: L (this is the runtime phase itself)

### F-LANG-SPEC-08 — §0.5 names ~11 modifiers that don't exist in catalog [P1]
**Evidence**: Spec §0.5 #4-#8 names `guarded`, `entry`, `isolated`, `universal`, `milestone`, `sealed after`, `writeonce`, `advancing`, `settling`, `completing`, `absorbing`. `ModifierKind.cs` confirms NONE exist. Spec §0.5 header claims "§4 is implemented."
**Description**: Author reading §0.5 reasonably believes these ship today; they don't.
**Fix**: Either (a) mark §0.5 as forward-looking with per-modifier status table, or (b) build the modifiers (catalog + lexer + parser + analyzer + diagnostics).
**Effort**: S (option a) or L (option b)

### F-LANG-SPEC-09 — `ChoiceElementTypeMismatch`/`ChoiceMissingElementType` defined but never emitted [P2]
**Evidence**: Spec §2.7:1121, §3.8:1657; codes defined; allow list line 77-78 "no emission site wired".
**Fix**: Wire diagnostics or remove from spec.
**Effort**: S

### F-LANG-SPEC-10 — Temporal-content validation diagnostics unwired; crashes MCP on invalid date inputs [P1]
**Evidence**: Spec §3.3 specifies content validation; PRE0055-58, PRE0053 all "no emission site wired" (allow list lines 68-72). MCP verified: `'2026-13-01'`, `'2026-02-31'`, `'bad-date'`, `'25:00:00'`, `'2026-01-01T10:00:00'` for instant all return `An error occurred invoking 'precept_compile'` — i.e., unhandled exception path.
**Description**: Both spec compliance gap AND robustness defect — invalid input throws instead of producing structured diagnostic.
**Fix**: Wire structured validation emitting `InvalidDateValue`/`InvalidDateFormat`/`InvalidTimeValue`/`InvalidInstantFormat`/`InvalidTypedConstantContent` instead of throwing. Add regression tests.
**Effort**: M (validators exist in `TypedConstantValidation.cs`; needs hookup + exception path closed)

### F-LANG-SPEC-11 — `<-` (BackArrow) in code/grammar but omitted from §1.1/§1.5/§2.1 tables [P2]
**Evidence**: `TokenKind.BackArrow = 139`; `Tokens.cs:338`; spec §1.1, §1.5, §2.1 tables omit it; §2.2:834 does include it in field decl grammar.
**Fix**: Add "Computed field arrow" row to §1.1 Operators; add `<-` to §1.5 scan list; note in §2.1 it's structural separator.
**Effort**: S (spec edit only)

### F-LANG-SPEC-12 — `OutOfRange` (PRE0079) constant-literal bounds check deferred [P2]
**Evidence**: Spec §3.10:1740; allow list line 64 "Deferred: constant-literal bounds check not wired".
**Fix**: Wire dedicated check or remove from spec.
**Effort**: S

### F-LANG-SPEC-13 — `CollectionOperationOnScalar` (47) + `NonOrderableCollectionExtreme` (65) unwired [P2]
**Evidence**: Allow list lines 67, 73; spec §3.10 lists them.
**Fix**: Decide consolidation (route through TypeMismatch) vs distinct emission; update spec or code.
**Effort**: S

## § D. Coverage Gaps (in code but not in spec)

1. **`BackArrow` token (`<-`)** — see F-LANG-SPEC-11.
2. **Diagnostic codes 128-148** — added since spec last refreshed. Many referenced in spec body but §3.10 group table tops out at PRE0124.
3. **`SemanticTokenTypes` / `ExpressionForms` catalogs** — tooling internals.
4. **`SyntaxReference.cs` / `Quickstart` catalog** — agent-facing.
5. **`approximate(integer)` works via widening** — spec says "decimal only", slightly stricter than lived behavior.
6. **`now()` only temporal function** — no spec sentence about why (no `today()` etc.).
7. **MCP-server proof obligation serialization** — `proofObligations` JSON projection not described.

## § E. Confidence Statement

**Exhaustively covered**: §0 (Preamble + 0.5/0.6 contracts), §1 (Lexer), §2 (Parser), §3.7 (Built-in function catalog), §3A (Language semantics), §0.6 proof contract.

**Partially covered**: §2.3 type-reference grammar, §3.6 binary operator matrix (sampled — not fully diffed), §3.8 modifier validation matrix, §3.10 diagnostic catalog (message-template correctness not verified per-code).

**Key load-bearing findings to action first**:
1. **F-LANG-SPEC-07** (P0) — Runtime evaluator stubs
2. **F-LANG-SPEC-01** (P0) — Non-negotiable Principle 9 not enforced on ensures
3. **F-LANG-SPEC-02** (P0) — Dead-guard detection ships only as unused diagnostic code
4. **F-LANG-SPEC-08** (P1) — §0.5 names ~11 modifiers that don't exist
5. **F-LANG-SPEC-10** (P1) — Temporal validation diagnostics unwired; invalid input CRASHES MCP

The proof engine's design contract (§0.6) is the section with densest drift — **5 of its 13 numbered requirements (Items 7, 8, 9, 10, 12) are documented as if implemented but have no detection logic.** The runtime contract in §3A is the largest single gap. The graph-analyzer contract §0.5 makes promises about a modifier surface that has not been built.
