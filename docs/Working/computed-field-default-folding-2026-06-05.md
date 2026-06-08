---
status: Locked 2026-06-05
phase-target: Phase 7 (total language conformance sweep) — Slice 2c-ii inclusion; extends the shared default-fold (`BuildDefaultEnvironment`) shipped for BUG-027 so a rule/ensure/bound over a computed field whose inputs fold participates in Principle-11 enforcement
comparable-systems-research-status: not-applicable — purely Precept-internal proof-engine completeness change (fold a computed field's `<- expr` over an already-foldable default environment); no language-surface or architectural-precedent claim. The one external comparator named (GHC/CUE evaluation-order) is illustrative grounding in the Architecture section, not a locked decision dependency.
sources-consulted:
  - "docs/philosophy.md:53 — 'rejects the definition with a specific message identifying what would make safety provable'; :57 — 'a definition that compiles without diagnostics has no unproven evaluation faults'; prevention/totality commitments."
  - "docs/language/precept-language-spec.md §0.1 Principle 11 (line 213) — 'Rules and initial-state ensures are checked against default field values… Only a provably-false fold rejects; an unknown/unfoldable default never rejects (per Proof philosophy #1–2).'"
  - "docs/language/precept-language-spec.md §0.4 items 1-5 (lines 158-168) — no loops, expression trees finite and acyclic, no fixpoint/widening; finite state space."
  - "docs/language/precept-language-spec.md §0.6 item 1 (line 203) — numeric interval reasoning tracks ranges through assignment chains; §3.5 line 1351/1354 — a computed expression 'is derived from the final configuration… sees every field except those that would form a dependency cycle.'"
  - "docs/language/precept-language-spec.md §3.5 lines 1351/1354 — computed-field scope = all field names except dependency-cycle-forming; 'an assignment cycle has no fixed point and cannot be evaluated' (cycles structurally rejected, not folded)."
  - "docs/compiler/proof-engine.md:483 — `DefaultViolatesRule`/`ScanRulesAgainstDefaults` rule-vs-default fold; 'An unknown/unfoldable default — a computed (`<-`) field… folds to `null` and never rejects'; 'Per-field precision… is a future refinement, deliberately not built here.'"
  - "docs/compiler/proof-engine.md:2024 — the default-fold field-disposition table: 'Has ComputedExpression (IsComputed = true) | Unfoldable — computed fields depend on other fields.' (the drift target this design tightens)."
  - "docs/compiler/name-binder.md:247 — 'a reference to a field declared after the current field… is treated as unresolved'; §7.5 Forward Reference Detection."
  - "src/Precept/Pipeline/ProofEngine.Analysis.cs:110-147 — BuildDefaultEnvironment: literal defaults bind value; interpolated defaults fold via FoldValue over already-accumulated `defaults` in declaration order (:132); line 138 `else if (field.DefaultExpression is not null || field.IsComputed) unfoldable.Add(field.Name)` — the unconditional computed→unfoldable site."
  - "src/Precept/Pipeline/ProofEngine.Analysis.cs:249-345 — FoldValue handles TypedLiteral, TypedFieldRef (UnknownSentinel if field in unfoldable set, :257), TypedBinaryOp, TypedUnaryOp, TypedConditional, TypedPostfixOp, InterpolatedTypedConstant; default → UnknownSentinel. EvaluateBinaryOp (:351) covers +,-,*,/ (guarded /0), comparisons, boolean/string equality."
  - "src/Precept/Pipeline/ProofEngine.Satisfiability.cs:56-88 — ScanRulesAgainstDefaults: early-return on HasConstructionHandler (:66), BuildDefaultEnvironment (:69), skip guarded rules (:76), `ConstantFold(...) is false` → DefaultViolatesRule (:79-86)."
  - "src/Precept/Pipeline/NameBinder.cs:262-325 — computed fields topologically sorted by dependency (`orderedFields`); only genuine cycles (`ReachesSelf`) get CircularComputedField (:312); downstream-of-cycle fields are NOT flagged."
  - "src/Precept/Pipeline/TypeChecker.cs:720-750 — TypedField stamped with IsComputed and ComputedExpression (resolved Pass 1b); ctx.Fields.Add in declaration order. SemanticIndex.cs:444/446 — ComputedExpression is a `TypedExpression?`, IsComputed a bool."
  - "docs/Working/bugs.md BUG-028 — default-fold completeness gap; facet (b) repro literally uses a computed field `field C as integer <- A * 2` as the unfoldable operand that makes a conjunction fold unknown; facet (a) is construction-handler coarseness."
  - "docs/Working/field-reference-bound-enforcement-2026-06-04.md (Locked) Decision 7 / S2 — the field-reference numeric bound desugars to `rule X op Y` into `semantics.Rules` and its DEFAULT enforcement rides the BUG-027 rule-vs-default fold (NOT a narrowed-interval check). This design supplies the foldability when Y (or any rule operand) is computed."
  - "Compiler.Compile probes (2026-06-05, this pass, throwaway test deleted): (1) `field Base default 10` + `field Computed <- Base+1` + `rule Computed <= 5` → HasErrors=False, no PRE0164 (computed unfoldable → rule never folds — the gap). (2) computed-on-computed chain `C1<-Base+1`,`C2<-C1+1`,`rule C2<=5` → clean (chain unfoldable). (3) `field Computed <- Later+1` with `Later` declared AFTER → StructuralSinkState/FieldNeverSet warnings only, HasErrors=False (forward ref neither folds nor errors). The shipped BUG-027 literal case `Amount default 5`,`Floor default 10`,`rule Amount>=Floor` rejects via RuleDefaultSatisfiabilityTests (9/9 pass against HEAD 210229ff); the MCP server's clean verdict on it is stale-build, not a code bug."
---

# Computed-field default folding — folding a computed field's `<- expr` into the shared default environment (Slice 2c-ii inclusion)

> **Provenance.** Slice 2c-ii (`field-reference-bound-enforcement-2026-06-04.md`, Locked) routes a field-reference numeric bound's *default* enforcement through the shared rule-vs-default fold shipped for BUG-027 (commit `210229ff`): the desugared `rule X op Y` lands in `semantics.Rules` and is folded against the default field environment. That fold marks **every computed field unfoldable** (`ProofEngine.Analysis.cs:138`), so when the referenced field — or any operand of a rule/ensure — is computed (`field C as T <- expr`), the fold returns unknown and never rejects, even when the computed value at creation is fully determined by foldable inputs. The owner wants this closed **in-slice** (2c-ii), not deferred: `field Base default 10` + `field Computed <- Base + 1` + `field Amount min Computed default 5` must reject (`Computed = 11`, `Amount = 5` → `5 >= 11` false). This design extends the **shared** `BuildDefaultEnvironment` so a computed field's value at creation is folded from the other fields' default values — improving the rule fold, the ensure fold, and the 2c-ii computed-bound default uniformly. It is a proof-engine **completeness** change in the never-over-reject direction; it adds no language surface.

## Goal

When done, a computed field `field C as T <- expr` whose `expr` evaluates over foldable default inputs binds its folded value in the shared default environment, so that any unguarded global rule, initial-state ensure, or 2c-ii desugared field-reference bound that references `C` and is provably violated at the default configuration rejects at compile time (Principle 11) — while a computed field with any unfoldable input stays unknown and never rejects. Demonstrated by: `field Base as integer default 10` + `field Computed as integer <- Base + 1` + `rule Computed <= 5 because "cap"` now emits `DefaultViolatesRule` (today clean — `Computed = 11`, `11 <= 5` is false); `field Amount as integer min Computed default 5` (2c-ii matrix cell #17b) rejects the `default 5` against the desugared `rule Amount >= Computed` (`5 >= 11` false); a computed-on-computed chain `C1 <- Base + 1`, `C2 <- C1 + 1` folds transitively in declaration order; and `field Computed as integer <- Unbounded` where `Unbounded` has a non-foldable default stays unfoldable → no rejection.

## Scope

- **In scope**:
  - **The fold (Decision 1).** In `BuildDefaultEnvironment`, a computed field is folded by `FoldValue(field.ComputedExpression, accumulated-defaults, unfoldable)` in the existing declaration-order loop, mirroring the interpolated-default handling at `:132`. If it folds, the value binds into `defaults[field.Name]`; if any input is unfoldable (or declared later — see Decision 2), the field is added to `unfoldable` exactly as today.
  - **The ordering rule (Decision 2).** Declaration-order folding, unchanged from the interpolated-default precedent: a computed field referencing a field accumulated earlier in the loop folds; one referencing a later-declared field is unfoldable in this pass (sound under-approximation). Computed cycles are already rejected upstream by the name binder (`CircularComputedField`), so no fixpoint is introduced.
  - **The shared blast radius (Decision 3).** The change lives in `BuildDefaultEnvironment` only; it improves all three consumers — `ScanRulesAgainstDefaults` (rules), `CheckInitialStateSatisfiability` (ensures), and the 2c-ii desugared computed-bound default — with no consumer-side edit.
  - **BUG-028 reconciliation (Decision 4).** State which facet of BUG-028 this closes (the computed-unfoldable facet of the completeness gap, including its facet-(b) repro) and which remain open (construction-handler coarseness; compound short-circuit in the general case).
- **Out of scope** (locked upstream — cited, not re-decided):
  - The rule-vs-default fold mechanism itself (BUG-027 / `DefaultViolatesRule` / PRE0164): shipped, unchanged. This design only feeds it a richer default environment.
  - The 2c-ii desugar (`min Computed` → `rule Amount >= Computed` into `semantics.Rules`): locked in `field-reference-bound-enforcement-2026-06-04.md` Decision 1/2. This design supplies the foldability that makes the desugared rule's default check fire when the operand is computed.
  - Computed-field cycle detection: owned by the name binder (`CircularComputedField`), unchanged. This design relies on it (no cycles reach the fold).
  - New language surface: none. No keyword/type/operator/modifier/construct.
  - The *dependent-op* discharge (divisor/range over a computed field, e.g. `100/Computed`): that is the 2c-i interval/relational reuse, a separate mechanism (operand-interval inference, `IntervalOf`), unchanged here. This design is the **default-configuration value** fold only.
- **Deferred to future**:
  - **Dependency-ordered folding** (fold a computed field referencing a later-declared field by consuming the name binder's topological order). Declaration-order under-approximation is sound; closing this is a completeness refinement (Decision 2, "Alternatives").
  - **BUG-028 facet (a)** (construction-handler whole-precept skip) and **facet (b)** in the *general* (non-computed-operand) compound short-circuit case — both remain BUG-028's, untouched here except where a now-foldable computed operand incidentally closes a specific repro.

## Philosophy Alignment

| Principle | Affected? | How served (1 sentence + cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | A rule/ensure/bound over a computed field whose default value provably violates it now rejects at compile time instead of compiling clean and leaving runtime governance as the only enforcement (`philosophy.md:57`; §0.1 Principle 11). | The fold must NEVER reject a computed field whose value is not fully determined by foldable inputs (no over-reject). | Accept incompleteness (a computed field with any unfoldable input stays unknown → no rejection) over any false rejection. |
| 2. One file, complete rules | Y | The folded computed value derives only from same-file declared defaults read through the existing `BuildDefaultEnvironment`; no external oracle (§0.6 philosophy 4). | N/A | N/A |
| 3. Determinism | Y | The fold is a pure single-pass walk over `ComputedExpression` in declaration order using the existing `FoldValue`; same input → same disposition (§0.4 items 1-3). | N/A | N/A |
| 4. Full inspectability | Y | A computed field that now rejects surfaces through the same `DefaultViolatesRule`/`UnsatisfiableInitialState` message that names the violated rule/ensure (`proof-engine.md:483`). | The message names the *rule*, not the computed field's intermediate value. | Accept the existing message shape; the computed value is implicit in the folded verdict (no new diagnostic). |
| 5. Keyword-anchored readability | N | No syntax change; `<-` computed-field form unchanged. | N/A | N/A |
| 6. Governance not validation | Y | Runtime governance already enforces these constraints operation-blind (§0.7); this adds the separate compile-time proof participation for the computed-input case. | N/A | N/A |
| 7. Compile-time totality | Y | A computed field's creation value is now a provable fact when its inputs are bounded/foldable, contributing to the §0.6-item-1 range reasoning at the default configuration. | A computed field referencing a later-declared field is not folded in declaration order. | Accept declaration-order incompleteness over a dependency-ordered pass (Decision 2). |
| 8. Approximation honesty | N | Operates on the exact `FoldValue` evaluator (decimal/bool/string); no approximate lane. | N/A | N/A |
| 9. Mandatory rationale | N | No new constraint construct; the rule/ensure/bound being checked carries its own `because`. | N/A | N/A |
| 10. Static semantic checking | Y | A definition whose computed-field creation value provably violates a declared rule is rejected before any instance exists, not deferred. | N/A | N/A |
| 11. Static completeness | Y | Closes the computed-input facet of the default-fold under-emit: a provable computed-field default violation that previously compiled clean now rejects, restoring the Principle-11 bridge for that case. | Some computed-field violations remain unprovable (unfoldable input, later-declared reference); those stay clean. | Accept residual incompleteness; runtime governance backstops, and the direction is never-over-reject. |

**Tradeoff detail (Principles 1/7/11).** The fold is deliberately *incomplete but sound*: it binds a computed value only when every input it transitively reads is already foldable in the declaration-order environment; otherwise the field stays in the `unfoldable` set and any rule over it folds to unknown → no rejection. This accepts that some provable violations behind a later-declared reference or a non-constant input are not caught (BUG-028's territory), to guarantee the fold never manufactures a false rejection of a precept whose computed field is not actually determined at creation. This is the §0.6-philosophy-1 posture (soundness over completeness), applied to the computed-field row of `BuildDefaultEnvironment`.

**Companion commitments.** *Stateless-first-class*: the fold operates over field defaults and computed expressions with no dependence on a state machine; a stateless precept with a computed field and a global rule benefits identically (the headline repro is stateless). *Domain-expert-primary-author*: no new vocabulary — a domain expert who writes `field Computed <- Base + 1` and `rule Computed <= 5` already expresses the intent; this slice makes that already-natural pairing reject when the defaults violate it, with no surface the author must learn.

## Language Design Grounding

**N/A — no language surface.** This design introduces or modifies no token, keyword, construct, modifier, type, operator, accessor, or expression form. The `<-` computed-field form, the `rule`/`ensure` constructs, and the six bound modifiers are all spec-defined and unchanged. The change is entirely within the proof engine's default-environment construction — a completeness improvement to a shared internal fold. (The GHC/CUE evaluation-order parallel is drawn in Architecture Grounding as illustrative precedent for the declaration-order-vs-dependency-order decision, not as a language-surface claim.)

## Audience and Teachability

**N/A — no language surface.** The feature is invisible to the author as new syntax; its effect is that an existing, already-teachable construct pairing (a computed field plus a rule/ensure/bound that references it) now rejects at compile time when the defaults provably violate it, via the **existing** `DefaultViolatesRule`/`UnsatisfiableInitialState` diagnostics whose wording is already locked (`Diagnostics.cs`, `proof-engine.md:483`). No new error message, teaching path, or worked example is introduced beyond the headline reject the Goal section demonstrates. The author-facing message a domain expert sees is the same one BUG-027 already ships ("Rule '{0}' is violated by the default field values ({1})…"), now reachable through a computed reference.

## Semantic Rules

This design touches the proof-stage default-fold evaluation, so Semantic Rules apply.

### SR1 — The fold (reduction)

The default environment `Δ` is built by a single declaration-order walk over `semantics.Fields`, accumulating a partial map `defaults : FieldName ⇀ Value` and an `unfoldable : Set<FieldName>`. The computed-field row is added between the interpolated-default row and the catch-all:

```
build Δ:  for each field F in declaration order:
  F.DefaultExpression = literal v                  ⇒  defaults[F] := v
  F.DefaultExpression = interpolated I             ⇒  let r = FoldValue(I, defaults, unfoldable)
                                                       r ≠ ⊥ₛ  ⇒  defaults[F] := r
                                                       r = ⊥ₛ  ⇒  unfoldable += F
  F.IsComputed ∧ F.ComputedExpression = e          ⇒  let r = FoldValue(e, defaults, unfoldable)      [NEW]
                                                       r ≠ ⊥ₛ  ⇒  defaults[F] := r
                                                       r = ⊥ₛ  ⇒  unfoldable += F
  F.DefaultExpression ≠ null (non-foldable kind)    ⇒  unfoldable += F
  F.IsOptional                                      ⇒  defaults[F] := null
  otherwise                                         ⇒  defaults[F] := typeDefault(F)
```

where `⊥ₛ` is `UnknownSentinel`. The NEW row replaces today's behavior, where `F.IsComputed` falls into the `else if (F.DefaultExpression is not null || F.IsComputed)` arm (`:138`) and is unconditionally added to `unfoldable`. `FoldValue` is the existing evaluator (`:249`); it returns `⊥ₛ` for any `TypedFieldRef` whose field is in `unfoldable` or absent from `defaults` (`:257-259`), so a computed expression reading a not-yet-accumulated (later-declared) or already-unfoldable field folds to `⊥ₛ` and the computed field is correctly marked unfoldable.

The computed field's `ComputedExpression` node is a `TypedExpression` of exactly the shapes `FoldValue` already handles (`TypedBinaryOp`, `TypedFieldRef`, `TypedLiteral`, `TypedUnaryOp`, `TypedConditional`, `TypedPostfixOp`, `InterpolatedTypedConstant`); no new `FoldValue` arm is required.

### SR2 — Ordering and termination

**Declaration-order, single-pass, no fixpoint (§0.4).** The walk visits each field once in declaration order. A computed field `C` referencing `D`:
- `D` declared before `C` and foldable ⇒ `defaults[D]` is present ⇒ `C` folds.
- `D` declared before `C` but unfoldable ⇒ `D ∈ unfoldable` ⇒ `FoldValue` returns `⊥ₛ` ⇒ `C` unfoldable (sound under-approximation).
- `D` declared after `C` ⇒ `defaults[D]` absent at `C`'s turn ⇒ `FoldValue` returns `⊥ₛ` ⇒ `C` unfoldable (sound under-approximation; identical to the interpolated-default ordering note at `:125-131`).

**No cycles reach the fold.** Computed-field cycles (self-reference and transitive) are rejected upstream by the name binder with `CircularComputedField` (`NameBinder.cs:312`; spec §3.5 line 1354 — "an assignment cycle has no fixed point and cannot be evaluated"). A definition carrying a cycle has errors and produces no clean fold input. Therefore the computed-field fold terminates in one pass with no fixpoint or widening, satisfying §0.4 items 1-3.

**Computed-on-computed chains fold transitively.** `C1 <- Base + 1` (Base before C1) binds `defaults[C1]`; `C2 <- C1 + 1` (C1 before C2) then reads `defaults[C1]` and binds `defaults[C2]`. The single declaration-order pass suffices because the name binder guarantees a computed field's foldable dependencies are acyclic, and the common authoring case declares dependencies before dependents (the binder's topological order coincides with declaration order whenever the author wrote it that way).

### SR3 — Soundness (per-axis)

**Claim: the folded value equals the computed field's value at creation.** A computed field's value at creation is `expr` evaluated over the *initial* configuration — the other fields' default values (spec §3.5 line 1354: a computed expression "is derived from the final configuration"; at creation, before any mutation, that configuration is the defaults). `FoldValue(e, defaults, unfoldable)` evaluates `e` over exactly that environment using the same arithmetic/boolean/comparison reductions the runtime evaluator applies (`EvaluateBinaryOp`, `:351`). When it returns a non-`⊥ₛ` value, that value is the creation value of the computed field.

- **Never a false reject.** A rule over `C` rejects only when `ConstantFold(rule.Condition, Δ) is false` (`Satisfiability.cs:79`). For that to read `C`, `C` must be in `defaults` (folded to a concrete value); if any input to `C` is unfoldable, `C ∈ unfoldable` and `FoldValue(C-ref)` returns `⊥ₛ`, propagating to the rule's fold as unknown (`null`) → no rejection. So a computed field that is not fully determined at creation can never trigger a rejection.
- **Never a false accept (within the foldable envelope).** When `C` folds to a concrete value and a rule over `C` folds to `false`, the violation is genuine — `C`'s creation value, evaluated by the same reductions the runtime would apply, makes the rule false. Rejecting it is sound (it is exactly the §0.6-item-6 / Principle-11 case).
- **Direction is monotone.** Moving a field from `unfoldable` to `defaults` can only turn an `unknown` fold into a `true`/`false` fold; it can never flip an existing `false` to `true` or vice versa (the same value is read either way). So the change can only *add* rejections of genuinely-violating defaults, never remove a sound rejection nor add an unsound one.

**Soundness-preservation claim (named principles).** Principle 7 (totality) and Principle 11 (static completeness) hold *more* completely after this change — a previously-undischarged computed-input default violation is now caught — and Principle 1 (prevention) is not threatened in the over-reject direction because the `unfoldable` fallback preserves the never-reject-on-unknown invariant. Principle 3 (determinism) holds because the fold is a pure single-pass walk over an acyclic expression set. No principle is weakened.

### SR4 — Satisfiability-isolation invariant (preserved)

The default-fold scans read the bare default environment, never the relational `narrowed` dictionary (`proof-engine.md:487` — the satisfiability-isolation soundness invariant). This design touches only `BuildDefaultEnvironment` (which feeds the *value* environment `Δ`, not the relational narrowing), so the isolation is untouched: the computed-field fold reads `defaults`/`unfoldable`, which the discharge-time relational narrowing never consults and which never consults the narrowing. The "tighter interval makes a scan over-reject" hazard does not arise here because the fold binds *concrete values*, and a rejection fires only on a *proven-false* fold (an exact verdict), not on an interval-emptiness judgment.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The change belongs in the **proof engine** (`ProofEngine.Analysis.cs`, the `BuildDefaultEnvironment` helper), not in catalog metadata and not in the type checker. Rationale: the default-environment fold is a proof-stage analysis (it discharges/refutes Principle-11 obligations against default values), and it already lives in the proof engine as a shared helper between the ensure-fold and rule-fold. The computed-field foldability is a property of *the fold's input construction*, not of the language vocabulary (there is no per-modifier or per-type metadata to add — `IsComputed`/`ComputedExpression` are already carried on `TypedField`). Placing it in `BuildDefaultEnvironment` keeps the single-default-environment invariant the BUG-027 extraction established ("there is one default environment and one fold evaluator, never a fork" — `ProofEngine.Analysis.cs:107`). It is not catalog metadata because no new keyword/type/modifier is involved and the `*Kind`-dispatch prohibition does not apply (no enum-identity switch is added — the change is a single new `if` arm on `field.IsComputed`, a DU/property test, not a per-member behavior switch).

**Cross-component propagation:**
- **Runtime (parser, type checker, evaluator, diagnostics):** None to the parser/type-checker/evaluator. The only behavioral change is *more* `DefaultViolatesRule` (PRE0164) and `UnsatisfiableInitialState` (PRE0115) diagnostics emitted for previously-clean computed-input cases; no new diagnostic code, no message change.
- **Tooling (syntax highlighting, completions, hover, semantic tokens):** None. No catalog member, keyword, or grammar change.
- **MCP (vocabulary, DTOs, tool output):** None. `precept_compile` surfaces the additional PRE0164/PRE0115 diagnostics through the existing diagnostics array; no DTO or vocabulary change. (Note: the MCP server serves its last-spawn build; after this ships the owner should `/mcp reconnect precept` to pick up the new behavior.)

**Breaking changes.** No public-contract change — no API surface, no diagnostic-code addition or rename, no catalog member flowing to grammar/completions/MCP vocabulary. The only externally-observable change is that some previously-compiling definitions (a computed-field default that provably violates a rule/ensure/bound) now correctly reject — a soundness tightening, intended (see Decision 3 blast radius / corpus check).

### External architectural precedent

The decision is **declaration-order folding vs a dependency-ordered pass** for derived values. **GHC** evaluates a recursive `let` group by demand within a single strongly-connected-component analysis, but Precept's §0.4 forbids fixpoint reasoning, so the comparator is the *acyclic-binding* case. **CUE** is the closer precedent: CUE resolves field references through lattice unification and explicitly **rejects reference cycles** that have no fixed point rather than iterating to one — the CUE spec (cuelang.org/docs/references/spec, "Cycles": *"Implementations may report an error if a cycle results in an infinite expansion… A reference to a value that structurally includes itself is an error unless it can be resolved to a finite value."*) — and resolves the acyclic remainder in dependency order. Precept **takes** the "reject cycles, fold the acyclic remainder" stance (the name binder already rejects computed cycles with `CircularComputedField`, mirroring CUE's cycle error), and **diverges** by folding the acyclic remainder in *declaration order* rather than full dependency order: Precept accepts a sound under-approximation (a later-declared reference stays unfoldable this pass) to keep the fold a single linear walk that shares one environment with the interpolated-default and ensure paths, rather than introducing a second topological-ordering pass solely for the default fold. The divergence is justified because (a) the name binder already computed the topological order, so dependency-ordered folding is a *future* completeness refinement, not a blocker; and (b) declaration order is the natural authoring order for computed chains (`Base` then `Computed`), so the under-approximation rarely bites in practice (the headline repro and the 2c-ii matrix cell #17b all declare the input before the computed field).

## Inventory of what will be built

This is a small, single-site change plus its test matrix.

- **`src/Precept/Pipeline/ProofEngine.Analysis.cs`** — `BuildDefaultEnvironment` (≈ lines 116-144): add a computed-field arm *before* the existing `else if (field.DefaultExpression is not null || field.IsComputed)` catch-all, so `field.IsComputed && field.ComputedExpression is not null` folds via `FoldValue(field.ComputedExpression, defaults, unfoldable)`; bind on success, `unfoldable.Add` on `⊥ₛ`. Update the existing catch-all so it no longer absorbs computed fields (it remains for `DefaultExpression is not null` non-foldable defaults). Update the helper's `<summary>` XML doc (lines 104-108) — currently "computed (`<-`) fields… are marked unfoldable" — to state the new conditional foldability.
- **No new types, no catalog entries, no DU changes, no new diagnostic codes.** `IsComputed`, `ComputedExpression`, `FoldValue`, `UnknownSentinel`, `DefaultViolatesRule` (PRE0164), `UnsatisfiableInitialState` (PRE0115) all exist.
- **Tests** (`test/Precept.Tests/ProofEngine/`):
  - Extend `RuleDefaultSatisfiabilityTests.cs` (or a sibling `ComputedFieldDefaultFoldingTests.cs`): rule over a computed field with a provably-violating default rejects (`DefaultViolatesRule`); computed-on-computed chain rejects transitively; computed field with an unfoldable input stays clean; computed field referencing a later-declared field stays clean (declaration-order under-approx); an ensure over a computed field with a violating default rejects (`UnsatisfiableInitialState`).
  - Extend `FieldRefBoundEnforcementTests.cs` (2c-ii matrix cell #17b): `field Base default 10` + `field Computed <- Base + 1` + `field Amount min Computed default 5` rejects via the desugared `rule Amount >= Computed` folded against defaults.
  - Regression: the existing 9 `RuleDefaultSatisfiabilityTests` cells and the ensure-fold tests stay green (never-over-reject).
  - Corpus: `dotnet test` full run + a `samples/` compile sweep — any sample with a computed field whose default provably violates a rule/ensure/bound will newly reject (expected soundness-tax; reconcile per Decision 3).

## Decisions

### Decision 1: Fold a computed field's `<- expr` in `BuildDefaultEnvironment` via `FoldValue` over the accumulated defaults

**Stakes**: medium

- **Rationale**: A computed field's value at creation is `expr` over the other fields' default values (spec §3.5 line 1354 — derived from the configuration; at creation that is the defaults). The shared fold already evaluates that exact expression shape over that exact environment for *interpolated* defaults (`:132`); a computed field is the analogous case and the same `FoldValue` call discharges it. Folding it in-place keeps the single-environment invariant the BUG-027 extraction established and improves all three consumers with no consumer-side edit. The `FoldValue`/`unfoldable` machinery already returns `⊥ₛ` for any unfoldable input, so the never-over-reject guarantee is inherited for free.
- **Tradeoff accepted**: A computed field whose inputs fold now binds a concrete value, so a previously-clean definition whose computed-field default provably violates a rule/ensure/bound will newly reject — a soundness-tax on any existing sample/fixture in that exact shape (Decision 3 corpus check). This is the intended Principle-11 enforcement, not a regression, but it is a behavior change to previously-accepted definitions.
- **Alternatives considered**:
  - **Leave computed unfoldable; add a separate computed-bound default check.** Rejected: duplicates the fold logic, forks the single-environment invariant, and only covers the bound case (not rules/ensures over computed fields) — strictly worse coverage and a CLAUDE.md "never maintain parallel" smell.
  - **Fold lazily at lookup** (resolve a computed field's value on demand when a rule reads it). Rejected: complicates `FoldValue`'s `TypedFieldRef` arm with re-entrant computation and a visited-set, reintroducing the very fixpoint machinery §0.4 forbids; the eager declaration-order pass is simpler and already proven for interpolated defaults.
  - **A separate dependency-ordered pass over computed fields.** Rejected for *this* slice (admitted as a future refinement, Decision 2): more complete but a second ordering pass solely for the default fold, when declaration order already covers the headline repro and the 2c-ii cell. Deferred, not dismissed.
- **Precedent**: The interpolated-default fold (`ProofEngine.Analysis.cs:120-137`) is the in-tree precedent — same `FoldValue`, same accumulated-`defaults` environment, same declaration-order, same `⊥ₛ → unfoldable` degradation. This decision extends that exact pattern one arm.
- **Sources consulted for this decision**:
  - `ProofEngine.Analysis.cs:120-144` — "the interpolated arm: `var folded = FoldValue(field.DefaultExpression, defaults, unfoldable); if (!ReferenceEquals(folded, UnknownSentinel)) defaults[field.Name] = folded; else unfoldable.Add(field.Name);`" — the template; the computed arm mirrors it.
  - `ProofEngine.Analysis.cs:138` — "`else if (field.DefaultExpression is not null || field.IsComputed) unfoldable.Add(field.Name);`" — the site that currently swallows computed fields; the new arm precedes it.
  - `ProofEngine.Analysis.cs:249-259` — "`case TypedFieldRef fieldRef: if (unfoldable.Contains(fieldRef.FieldName)) return UnknownSentinel; return defaults.TryGetValue(...) ? val : UnknownSentinel;`" — the unfoldable/absent → `⊥ₛ` propagation that gives the never-over-reject guarantee.
  - `SemanticIndex.cs:444/446` — "`TypedExpression? ComputedExpression, … bool IsComputed`" — the node is the `TypedExpression?` shape `FoldValue` consumes.
  - Spec §3.5 line 1354 — "a *computed expression* is derived from the final configuration"; at creation the configuration is the defaults, so folding `expr` over `defaults` yields the creation value.

### Decision 2: Fold in declaration order (a later-declared reference stays unfoldable this pass); rely on the name binder for cycle rejection

**Stakes**: medium

- **Rationale**: The existing loop is declaration-order, and the interpolated-default fold already accepts the same declaration-order under-approximation (`:125-131`). A computed field referencing a later-declared field folds to `⊥ₛ` (the field is absent from `defaults` at that point) and is marked unfoldable — sound (never wrong), just incomplete for that case. Computed cycles are already rejected upstream by the name binder (`CircularComputedField`), so the fold sees only acyclic computed expressions and terminates in one pass with no fixpoint, satisfying §0.4.
- **Tradeoff accepted**: A computed field declared *before* a field it references will not fold this pass even though the dependency is acyclic and would fold in dependency order — a completeness gap for that authoring shape (rare; the natural order declares inputs first). Closing it needs a dependency-ordered pass (deferred).
- **Alternatives considered**:
  - **Consume the name binder's topological order** (`orderedFields`, `NameBinder.cs:262-325`) to fold computed fields in dependency order, closing the later-declared-reference gap. Rejected for this slice: the topological order is computed in the binder and not currently threaded to `BuildDefaultEnvironment`; wiring it is a larger change than the slice needs, and declaration order already covers the headline repro and the 2c-ii matrix cell. Admitted as the future refinement (Scope § Deferred).
  - **Iterate the fold to a fixpoint** until no new field folds. Rejected: §0.4 item 1 forbids fixpoint computation; unnecessary because the binder guarantees acyclicity and a single dependency-ordered pass (the deferred refinement) would suffice without iteration.
- **Precedent**: The interpolated-default ordering note (`ProofEngine.Analysis.cs:125-131`) — "field declaration order affects foldability here… FoldValue returns UnknownSentinel and the field is marked unfoldable. This is graceful degradation." The computed-field arm inherits this exact behavior. The name binder's cycle rejection (`NameBinder.cs:312`) is the upstream guarantee that makes single-pass folding total.
- **Sources consulted for this decision**:
  - `ProofEngine.Analysis.cs:125-131` — "NOTE — ordering sensitivity: field declaration order affects foldability here… FoldValue returns UnknownSentinel and the field is marked unfoldable. This is graceful degradation — no error, just a conservative skip."
  - `NameBinder.cs:286-317` — "Fields still carrying unresolved dependencies after the topological drain are either on a cycle or merely downstream… only fields genuinely on a cycle… get a CircularComputedField diagnostic." — cycles rejected upstream; the fold never sees one.
  - Spec §0.4 item 1 (line 158) — "No loops. … This eliminates the need for fixpoint computation and widening operators." — forbids the fixpoint alternative.
  - Spec §3.5 line 1354 — "an assignment cycle has no fixed point and cannot be evaluated." — grounds the binder's cycle rejection and the no-fixpoint single pass.
  - Probe (this pass): `field Computed <- Later + 1` with `Later` declared after → `StructuralSinkState`/`FieldNeverSet` warnings only, `HasErrors=False` (forward ref neither folds nor errors today; declaration-order folding leaves it unfoldable — consistent, sound).

### Decision 3: Land the change in the shared `BuildDefaultEnvironment` (improving rule + ensure + 2c-ii bound uniformly); absorb the corpus soundness-tax

**Stakes**: medium

- **Rationale**: All three Principle-11 default consumers (`ScanRulesAgainstDefaults`, `CheckInitialStateSatisfiability`, the 2c-ii desugared computed-bound default) read the same `BuildDefaultEnvironment` output. Folding computed fields there improves all three with one edit and preserves the single-environment invariant — the architecturally-correct seam. The owner wants the computed-bound case (cell #17b) enforced in-slice, and the cheapest way to get it is to enrich the shared environment rather than special-case the bound path.
- **Tradeoff accepted**: The change is shared, so its behavior shift touches three call sites at once — any existing sample/fixture with a rule, ensure, *or* computed bound over a computed field whose default-folded value violates it will newly reject. This must be reconciled by a corpus sweep before the slice lands (expected: zero or a small number of fixtures that were silently-wrong and should be fixed, not a broad break — the headline shapes are new test fixtures, not existing samples).
- **Alternatives considered**:
  - **Fold computed fields only for the rule path** (a 2c-ii-local change). Rejected: the ensure path (`CheckInitialStateSatisfiability`) shares the same environment and would be left inconsistent (an ensure over a computed field still wouldn't fold), violating the "one environment, no fork" invariant and leaving a known under-emit the slice could have closed for free.
  - **Gate the new folding behind a flag** to limit blast radius. Rejected: pre-release solo-dev context (no downstream consumers to protect; migration cost N/A); a flag would add permanent complexity to guard against a transient corpus break that a sweep resolves once.
- **Precedent**: The BUG-027 extraction itself (`ProofEngine.Analysis.cs:101-108` — "Both `CheckInitialStateSatisfiability` and `ScanRulesAgainstDefaults` call this — there is one default environment and one fold evaluator, never a fork.") established the shared seam this decision enriches.
- **Sources consulted for this decision**:
  - `ProofEngine.Analysis.cs:101-108` — the shared-environment XML doc: "there is one default environment and one fold evaluator, never a fork."
  - `ProofEngine.Satisfiability.cs:69` and `ProofEngine.Analysis.cs:72` — both call `BuildDefaultEnvironment(semantics, out var unfoldable)`; the 2c-ii desugared rule lands in `semantics.Rules` (`field-reference-bound-enforcement-2026-06-04.md` Decision 1) and is folded by `ScanRulesAgainstDefaults`, so it inherits the enriched environment with no further edit.
  - `field-reference-bound-enforcement-2026-06-04.md` S2(b) — "once BUG-027 folds global rules against defaults the field-reference bound's default violation rejects for free — no field-reference-bound-specific default check is added; it rides the general fold." — the computed-input case rides the same general fold once computed fields are foldable.
  - Project memory: solo-dev / pre-release — migration costs and downstream back-compat don't apply, so the corpus-tax is absorbed by a one-time sweep, not a flag.

### Decision 4: Scope the BUG-028 reconciliation — close the computed-unfoldable facet; leave construction-coarseness and general compound-short-circuit open

**Stakes**: low

- **Rationale**: BUG-028 enumerates two completeness facets of the default fold: (a) construction-handler whole-precept skip, and (b) compound short-circuit (`false and <unknown>` folds unknown). Its facet-(b) repro uses a computed field (`field C as integer <- A * 2`) precisely as the *unfoldable operand* that forces the conjunction to unknown. This design makes that operand foldable, so the specific facet-(b) repro now folds `C >= 0` to a concrete value and the conjunction `A >= B and C >= 0` folds to `false` (both operands known) → rejects. So the *computed-input* cause of under-emit is closed; the *general* compound-short-circuit cause (a conjunction with a genuinely-non-computed unknown operand) and the construction-coarseness facet (a) remain BUG-028's.
- **Tradeoff accepted**: BUG-028 is not fully closed — it must be re-scoped (computed-unfoldable facet struck; construction-coarseness and general short-circuit retained) rather than resolved, so the bug entry stays Active with a narrowed scope. (Per CLAUDE.md, `bugs.md` is a transient tracking surface — the re-scope is a doc-update obligation, not a code comment reference.)
- **Sources consulted for this decision**:
  - `bugs.md` BUG-028 facet (b) repro — "`field A integer default 5` + `field B integer default 10` + `field C as integer <- A * 2` + `rule A >= B and C >= 0` → `A >= B` is false on defaults but the conjunction folds `unknown` (C unfoldable) → not rejected." — with computed folding, `C = 10`, `C >= 0` = true, `false and true` (both known) → `EvaluateBinaryOp(And, false, true)` = false → rejects. The computed-input cause is closed.
  - `bugs.md` BUG-028 facet (a) — "when a precept has an `initial` construction event, the fold is skipped entirely (`if (HasConstructionHandler(semantics)) return`)." — untouched by this design (the construction skip precedes `BuildDefaultEnvironment`).
  - `ProofEngine.Analysis.cs:261-270` — `EvaluateBinaryOp` over `And` requires both operands non-`⊥ₛ`; the general short-circuit (`false and ⊥ₛ → false`) is a *separate* evaluator change (BUG-028 facet b proper), not in this design's scope.

## Acceptance criteria

1. **Computed-field rule default violation rejects.** `precept P / field Base as integer default 10 / field Computed as integer <- Base + 1 / rule Computed <= 5 because "cap" / state Active initial` → `Compiler.Compile` has `HasErrors == true` with a `DefaultViolatesRule` (PRE0164) diagnostic naming the rule. (Today: clean.)
2. **2c-ii matrix cell #17b — computed field-reference bound rejects.** `field Base as integer default 10 / field Computed as integer <- Base + 1 / field Amount as integer min Computed default 5` → rejects the `default 5` against the desugared `rule Amount >= Computed` (`5 >= 11` false). (Depends on 2c-ii desugar landing; this design supplies the foldability.)
3. **Computed-on-computed chain folds transitively.** `field Base default 10 / field C1 <- Base + 1 / field C2 <- C1 + 1 / rule C2 <= 5 because "cap"` → rejects (`C2 = 12`, `12 <= 5` false).
4. **Ensure over a computed field rejects.** A `StateResident` ensure on the initial state whose condition over a computed field provably folds false on defaults → `UnsatisfiableInitialState` (PRE0115).
5. **Unfoldable input stays clean (never over-reject).** `field Unb as integer / field Computed <- Unb + 1 / rule Computed <= 5 …` where `Unb` has no foldable default → `HasErrors == false`, no PRE0164 (computed stays unfoldable).
6. **Later-declared reference stays clean (declaration-order under-approx).** `field Computed <- Later + 1 / field Later as integer default 10 / rule Computed <= 5 …` → no PRE0164 (Computed unfoldable this pass). Documented as the accepted incompleteness, not a bug.
7. **Existing folds stay sound.** All 9 `RuleDefaultSatisfiabilityTests` cells, the ensure-fold tests, and the full `dotnet test` suite stay green; a `samples/` compile sweep shows no unintended new rejections beyond genuinely-violating computed-field defaults.
8. **Docs updated.** `proof-engine.md:483/2024`, the §0.1 Principle 11 note, and BUG-028's scope reflect the computed-field foldability (Doc-update enumeration).

## Dependencies

- **Upstream**:
  - BUG-027 / `DefaultViolatesRule` rule-vs-default fold (commit `210229ff`) — shipped; this design enriches its input.
  - The name binder's computed-field cycle rejection (`CircularComputedField`) and dependency analysis — shipped; this design relies on it for single-pass termination.
  - 2c-ii desugar (`field-reference-bound-enforcement-2026-06-04.md` Decision 1/2) for acceptance criterion #2 specifically (the *bound* case); criteria #1/#3/#4 (rule/ensure over a computed field) are independent of 2c-ii and could land standalone.
- **Downstream**:
  - Closes the computed-input facet of BUG-028.
  - Completes the 2c-ii computed-bound default-enforcement story (cell #17b) that Decision 7 of the 2c-ii design left to the default fold.

## Doc-update enumeration

Per the CLAUDE.md routing table (pipeline-stage behavior + diagnostic surface):

- `docs/compiler/proof-engine.md:483` — the `DefaultViolatesRule` paragraph: amend "An unknown/unfoldable default — a computed (`<-`) field… folds to `null` and never rejects" to state that a computed field **whose inputs fold** binds its folded creation value and participates; only a computed field with an unfoldable input (or a later-declared reference, declaration-order) stays unknown.
- `docs/compiler/proof-engine.md:2024` — the default-fold field-disposition table row "Has `ComputedExpression` (`IsComputed = true`) | Unfoldable — computed fields depend on other fields": change to "Foldable when every input is foldable in declaration order; otherwise unfoldable."
- `docs/compiler/proof-engine.md:1987` — the `BuildDefaultEnvironment` description ("computed/non-foldable defaults are marked unfoldable"): qualify "computed defaults are folded when their inputs are foldable."
- `docs/language/precept-language-spec.md §0.1 Principle 11 (line 213)` — the "unknown/unfoldable default never rejects" note is still accurate; add (if the spec maintainer agrees the sentence warrants it) that a computed field's creation value is foldable when its inputs are — OR leave unchanged, since the principle statement (only-provable-false-rejects) is unaffected. *Judgment call: this is a completeness improvement within the existing principle wording, so the spec line likely needs no change; flag for the promote pass.*
- `docs/Working/bugs.md` BUG-028 — re-scope: strike the computed-unfoldable facet (now closed); retain facet (a) construction-coarseness and the general (non-computed-operand) facet (b) compound-short-circuit. (Transient tracking surface — re-scope at promote, not a code reference.)
- `test/Precept.Tests/ProofEngine/` — new/extended test matrix per the Inventory section (the test files are the executable spec).

## Open questions

None. The mechanism (fold computed in `BuildDefaultEnvironment` via `FoldValue`, declaration-order, cycles rejected upstream), the ordering, the soundness spine, the shared blast radius, and the BUG-028 reconciliation are all settled against verified source and canon. The one judgment call (whether the §0.1 Principle 11 spec line needs an edit) is flagged in the Doc-update enumeration as a promote-pass decision and does not block locking, because the principle's normative statement ("only a provably-false fold rejects") is unchanged by this design — the change only enlarges the set of provable folds in the sound direction.
