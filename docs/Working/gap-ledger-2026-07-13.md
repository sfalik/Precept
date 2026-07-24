# Compiler Conformance Gap Ledger — against the frozen spec

**Status:** Draft — 2026-07-13 (owner morning-review)
**Denominator:** the FROZEN canonical spec, committed `342e66db` (tag `spec-freeze-2026-07-13`).
**Grounded at:** HEAD `634cf0aa`. Verified: `git diff --stat 342e66db..HEAD -- docs/language docs/compiler docs/runtime` is **empty** — HEAD's spec surface is byte-identical to the freeze. The one intervening commit (`634cf0aa`) touches only `docs/Working/`. All source and spec line numbers below are valid against both.
**Freeze-identifier note (resolved):** one angle flagged that `342e66db`, the tag, and HEAD looked like three identifiers. Confirmed: the tag `spec-freeze-2026-07-13` points *at* `342e66db`; HEAD is one docs-only commit past it. No ambiguity in the denominator.

This is **analysis only.** No code or spec was edited. Nothing in the OWNER DECISIONS section is settled — each needs an owner ruling before WF-2 can build acceptance criteria on it.

---

## 1. Summary + count-gates

### 1.1 Master count by disposition (merged, de-duplicated across all angles)

| Disposition | Gaps (old → new) | Meaning |
|---|---|---|
| **implement-against-locked-spec** | **33 → 60** | Clear locked-spec answer; proceed to build. (was 11 behavioral + 5 diag/test + 17 doc-drift; +26 cell-level from §5 → 30 behavioral + 8 diag/test + 21 doc-drift; +1 base-ledger from the v2 reconstruction margin flag = BUG-020 → **31 behavioral** + 8 diag/test + 21 doc-drift) |
| **owner-fork** | **5 → 6** | Two defensible readings or a genuine contradiction; owner must rule. §5 adds OF6 (lowercase currency codes). |
| **new-surface-park** | **0 standalone** | The only new-surface item is the build-arm of owner-fork OF2 (function-arg constraints) — folded into that fork, routes to `/design` only if the owner chooses "build." |
| **out-of-scope-with-trigger** | **7 → 6** | Deferred; each names its re-open condition. OS7 is now **CLOSED** (§5 does the cell-by-cell enumeration it deferred); OS6 narrowed. |
| **Total dispositioned gaps** | **45 → 72** | Plus 2 flagged notes (not gaps). |

**Two finalization corrections applied (2026-07-14, against Frank's direction review + the v2-completion reconstruction):**
- **G03 scope widened (no new row).** The structural-severity work-unit (WU-STRUCT-SEV) previously enrolled only the 2 Proof-severity codes (ContradictoryRule/UnsatisfiableRule). It now enrolls **6 codes** — the 4 Graph state-structural codes (UnreachableState, StructuralSinkState, DeadEndState, RequiredStateDoesNotDominateTerminal) are folded in; `diagnostic-system.md:187–191` already tracks the same Warning→Error drift for all six. This expands G03's coverage within its single row; it does not add a dispositioned-gap row.
- **BUG-020 added (+1 row).** The v2-completion reconstruction (`v2-completion-reconciliation-2026-07-14.md:152`) flagged BUG-020 as absent from the ledger's 45. It is added as a base-ledger implement-against-locked-spec **behavioral/soundness** row (G60, §2.A). This is the only row-count change: 71 → 72.

De-duplication key: section-anchor + quoted claim (for spec-claim rows) or `file:line` + code (for source rows). 8 gaps were reported by two or more angles and collapsed to one row (noted per row). **§5 cell-level additions (G34–G59, OF6) were de-duplicated against every G/OS/OF row above** — cells whose gap was already family-captured are noted "already-covered," not re-filed.

### 1.2 Count-gate per enumeration angle

The count-gate for each angle is: *denominator − dispositioned = 0, or the shortfall is named.*

| Angle | Denominator | Findings | Dispositioned | Gate |
|---|---|---|---|---|
| **1 — by spec claim** | 15 claim families; 33 normative claims probed live | 11 | 11 | **MET.** Named shortfall: cell-by-cell enumeration of the 4 type-docs' per-operator/per-accessor matrices was probed at *family-representative* level, not exhaustively → **OS7** (out-of-scope-with-trigger). |
| **2 — by catalog member** | 685 members across 13 catalogs | 3 behavioral (0 metadata-unused) | 3 | **MET.** Metadata-level "defined-but-unused" is structurally impossible here (verified: every catalog has `GetMeta` exhaustive-switch + `All` from `Enum.GetValues` + a reflection completeness test). Only behavioral holes remain. |
| **3 — by diagnostic code** | 164 `PRE####` codes | 20 with no scanner-detected emission (144 emit live — build-verified via `PRECEPT0027`/`0029` green) | 20 | **MET.** All 20 dispositioned: 2 dead→implement, 3 stale-comment→implement, 2 retired→implement, 9 dead-specific(check-runs)→out-of-scope, 1 coalesce-live + 1 MCP-only + 1 reserved→no-gap, 1 uncertain→folded into G05. |
| **4 — obligation-creation positions** | 33 faultable expression positions | 11 creation holes | 11 | **MET.** 22 positions enroll correctly; 9 holes→implement, 2 (message holes)→owner-fork; +1 structural observation (unary slot)→out-of-scope. Every hole has a positive-creation probe. |
| **5 — by fault mode** | 15 `FaultCode` members | 1 dead + 3 support gaps | 4 | **MET.** 14/15 fault modes live-and-rejecting; 1 dead→owner-fork; +1 test-gap→implement, +1 doc-drift→implement, +1 runtime-defer→out-of-scope. |
| **Structural floor** | 1,506 heading blocks / 30 files (fence-aware) | 4 residue | 4 | **MET.** **Zero truly-empty leaf sections** — no dead heading both spec-claim readers walked past. Residue = 1 numbering gap, 4 soft-TBD placeholders, navigational-block class, transient-label class. |

### 1.3 Reconciliation of the two independent spec-claim extractions

Two teams extracted the spec-claim denominator independently: **Angle 1** (probe-driven — 15 families, 33 claims exercised live) and the **independent re-extractor** (register-only — 343 claim entries, ~1,430 row-level assertions, no probes, surfacing 16 doc-vs-doc inconsistencies).

**Diff:**
- **Only Angle 1 caught** (because they are probe-detectable behavioral gaps, invisible to a document register): G02 computed-narrowing, G04 relational-contradiction, G12–G14 message defects.
- **Only the re-extractor caught** (because they are pure doc-vs-doc contradictions, invisible to a "does it compile?" probe): the 16 internal inconsistencies — G25 maxplaces-from-currency, G26 SyntaxReference-stale, G27 count-arithmetic, G28 PRECEPT0007-collision, G29 `is in [...]`, G30 positive/nonnegative-framing, and owner-forks OF3 (notempty), OF4 (period-in-days), OF5 (integer-overflow).
- **Both caught:** G01/D2 subsequence (re-extractor #12 + Angle 1 F2), G18 §0.6 severity (re-extractor #11 + Angle 1 F8), G19 D3-label (re-extractor #8 + Angle 1 F9).

**Residual honesty (state plainly):** the two extractions plus the structural floor form an **agreement + lint + structural-floor backstop, NOT a totality proof.** They cross-check each other (a claim one missed, the other likely caught) and the floor confirms no spec section is silently empty — but a claim *mis-stated identically in both extractions* would pass through unnoticed, and neither pass proves the 4 type-docs' operator/accessor matrices were enumerated cell-by-cell (OS7). Completeness here is "two independent readers + a structural-emptiness floor agreed," not "every claim provably enumerated."

**Named blind spot — severity-correctness is an unwalked conformance axis (Frank §1).** No enumeration angle systematically checks *whether a diagnostic emits at the correct severity per the ruled model* — only emission-presence. Angle 3 ("by diagnostic code") walks *does it emit?*, not *does it emit at the ruled block/report severity?*. Severity drift therefore only surfaced where Angle 1 happened to probe it (G03/G04). This is not hypothetical: it demonstrably let four Graph state-structural codes through — they emit `Severity.Warning` today while `diagnostic-system.md:187–191` rules them block/Error (folded into G03 above). Alongside "a claim mis-stated identically in both extractions," this severity axis is a second known escape hatch: an axis of conformance that no angle walks.

**Structural-completeness credit the honesty statement should take (Frank §1).** Two of the backstops are stronger than mere reader agreement: Angle 2's catalog no-orphan-member property is a genuine *structural* completeness guarantee (every catalog member is reachable by construction — exhaustive `GetMeta` switch + `Enum.GetValues` + reflection test), and the 144/164 build-verified emission split is a real lint floor. Those two are load-bearing, not just "two readers agreed."

**Structural-floor result:** 0 zero-claim leaf sections across all 1,506 blocks. The floor is fully met.

---

## 2. The gap list

Grouped by disposition. Each row: spec-anchor (quoted) | source evidence (`file:line`) | live probe (verbatim) | classification | single owner work-unit. Work-unit labels are analysis-local groupings for WF-2, not existing plan labels.

### 2.A implement-against-locked-spec — behavioral / soundness (12)

**G01 — Ordered-choice comparison skips the subsequence / same-element-type check** *(D2 lead; reported by Angle 1 F2, Angle 2 Finding B, v1-recon D2)*
- **Spec:** `precept-language-spec.md:1398` — `< > <= >=` on `choice of T (ordered)` requires the right operand be "same element type, **order-preserving subsequence**." `primitive-types.md:347-350` restates.
- **Source:** the only obligation raised for ordered-choice comparison is the `ordered` modifier requirement (`DeclarationAttribute` strategy); no value-set/element-type check in the choice-compare path (`Operations.cs` entries `ChoiceLessThanChoice=133`…`136`; `TypeChecker.Expressions.cs`).
- **Probe (verbatim):** disjoint sets — `field A as choice of string("Low","Med","High") ordered` + `field B as choice of string("Red","Green","Blue") ordered` + `rule A < B` → `{"success":true,"0 type errors"}`, obligation `"Both choice operands must be declared ordered" → Proved`. Reversed-order same-values (`("Low","Med","High")` vs `("High","Med","Low")`) → `success:true, 0 type errors`. Mismatched element type (`choice of integer`) also Proved.
- **Classification:** diverges (soundness) — **genuinely partial** per the v2-completion reconstruction (`v2-completion-reconciliation-2026-07-14.md:109-111`): the choice-ordered design (Locked 2026-05-26) shipped the ordered-comparison *routing* (`ordered` propagation, `.min`/`.max`, ordinal compare on ordered-choice element), but the *subsequence / same-element-type soundness check* the spec mandates was never in that design's scope and was never built. The routing is done; the check is the residual hole. **Owner:** WU-CHOICE-CMP.
- Note: whether disjoint-type comparison is a hard `TypeMismatch` vs an unprovable/rejected subsequence obligation is an *implementation-shape* question for the design pass, not a spec change; the gap itself (no check at all) is unambiguous. Subsequence *direction* (which side may be the subsequence) is unstated in both spec locations — see Note N1.

**G02 — Computed-field `<-` does not enforce numeric-lane narrowing** *(Angle 1 F1)*
- **Spec:** `precept-language-spec.md:1256` "No implicit narrowing. A `number` value cannot be assigned to an `integer` or `decimal` field without an explicit bridge"; `:1738` computed type-mismatch → `TypeMismatch`.
- **Probe:** `field I as integer <- N` / `field D as decimal <- N` (N `number`) → `success:true, 0 type errors`. Contrast the action path `set I = N` → `PRE0018 "Expected a integer value here, but got 'number'"`. The `set` path enforces; the computed `<-` path does not. Widening (`integer <- number`) correctly accepted, so only the narrowing check is missing.
- **Classification:** diverges (soundness). **Owner:** WU-COMPUTED-NARROW.

**G03 — Uninhabitable definitions accepted at Warning, not Error (structural-severity flip)** *(Angle 1 F3)*
- **Spec:** `precept-language-spec.md:209` (§0.6 item 7): contradictory/empty-range rules make a definition "**uninhabitable** … the compiler **rejects the definition** … a prevention-worthy defect, not hygiene."
- **Probe:** `rule N > 100` + `rule N < 10` + initial event assigning N → `success:true`, only `PRE0155 severity:"warning"`. Same Warning severity for `UnsatRule` (PRE0159) and `ContradictoryRules` (PRE0155).
- **Scope widened to 6 codes (Frank §2).** The canonical diagnostic doc already tracks this exact Warning→Error drift for **six** codes, not two: `diagnostic-system.md:191` names `UnreachableState, StructuralSinkState, DeadEndState, RequiredStateDoesNotDominateTerminal, ContradictoryRule, UnsatisfiableRule` as tracked drift, and `:187–189` rules the first four (Graph state-structural) into the **block/Error** set as process-topology defects the philosophy proves impossible. All four Graph codes verified `Severity.Warning` today (`Diagnostics.cs:707, :720, :726, :747`); the two Proof codes likewise (`:802, :814`). The four Graph state-structural codes — previously dropped from the ledger — are **enrolled into WU-STRUCT-SEV**. The report-class codes (PRE0082/0153/0154/0080/0111) correctly stay Warning per §0.6 items 8/2 and `diagnostic-system.md:189` — NOT part of this flip. (Corroborating anchor for G18/G03: `diagnostic-system.md:182/187/191` — §0.6 item 7 *defers* the severity model to this doc explicitly, and the doc is precise, not understating, about the drift.)
- **Classification (corrected per v2 reconstruction §2.1/§3b):** severity/**policy**, not a fresh soundness discovery. Detection is already live (the satisfiability cluster shipped it — register #6); the Warning→Error flip is the known-open policy question `bugs.md` **BUG-026** (*"the Warning severity was defaulted by analogy … with no warn-vs-error deliberation … the current Warning is drift, not a settled decision"*), which is exactly the v3 Stage-0 structural-severity slice. **Keep with corrected classification — this is a severity/policy gap, not a strike.**
- **Soundness intact (probe evidence, this pass):** a contradiction does **not** over-prove a dependent fault-prone op — it rejects at Error. Probe `field X as integer min 0 max 5` + `rule X >= 10` + `set R = 100 / X` → `PRE0083 (error) "Division is unsafe: 'X' can be zero"` with the `Numeric "Divisor must be non-zero"` obligation **Unresolved**; plus `PRE0159 (warning)` (the unsatisfiable rule) and `PRE0164 (error)` (default X=0 violates the rule). What is missing is the *warning severity flip on the uninhabitability signal*, not prevention of the dependent fault. **Owner:** WU-STRUCT-SEV.

**G04 — Relational field-to-field rule contradiction not detected at all** *(Angle 1 F4)*
- **Spec:** `precept-language-spec.md:209` — reject when "two rules' ranges are provably incompatible."
- **Probe:** `rule A > B` + `rule A < B` (jointly unsatisfiable) → `success:true`, only an unrelated PRE0119; no PRE0155. Contrast the same-field-vs-literal case (G03), which *is* detected. Single-hop (same two fields), within the depth-bounded reasoning §0.4 permits.
- **Classification (corrected per v2 reconstruction §2.1/§3b):** **known-deferred inspectability, not a soundness hole.** This is `bugs.md` **BUG-025** verbatim — *"Relational (field-vs-field) contradictory rules are invisible to the satisfiability scan — no warning, no rule attribution (inspectability gap, NOT a soundness hole)"* — explicitly scoped out on the record in `relational-contradiction-reject-2026-06-04.md` because it is not required to close the BUG-024 over-prove. Soundness is intact via the shipped BUG-024 guard: probe `field X as integer min 0 max 5` + `rule X >= 10` + `set R = 100 / X` → `PRE0083 (error)` with the divisor obligation **Unresolved** — any dependent fault-prone op rejects at Error regardless of the missing attribution. What's missing is the author-facing **warning attribution (PRE0155)**, not the prevention. **Keep with corrected classification — a missing diagnostic (BUG-025 class), not a strike.** **Owner:** WU-RULE-CONTRA.

**G05 — `KeyPresence` obligation never created for `lookup for K` reads (fail-open)** *(Angle 2 Finding A; corroborated by Angle 3 F-DIAG-06's uncertain PRE0099)* — **NEW, not in WF-0 leads**
- **Spec:** `collection-types.md:655` "`F for K` … requires a `when F contains K` guard. The compiler raises `KeyPresenceSafety` if the guard is absent"; `:747`, `:764` restate.
- **Source:** `Types.cs:347` Lookup exposes only `.count`; the `for K` read flows through `LookupAccess`, which binds no obligation. The only `KeyPresenceProofRequirement` creation site (`Actions.cs:176`) is `RequireAbsence:true` (the mutation precondition). No `RequirePresence` creation site for the read. Meta (`ProofRequirement.cs:243`), diagnostic mapping (`ProofEngine.Diagnostics.cs:181/460/484`), and discharge strategy (`Strategies.cs:669`) all exist — the member is half-wired.
- **Probe:** `on Read -> set out = limits for Read.k` (unguarded) → `success:true, diagnosticCount:0, proofObligations:[]`. Adding `when limits contains Read.k` produces byte-identical clean output → the obligation is never generated. Positive control: list `routing.at(5)` unguarded → `PRE0100` raised.
- **Classification:** missing (obligation-creation-site hole). A P7/P11 prevention breach. **Owner:** WU-KEYPRESENCE.

**G06 — Guard positions never enrolled (6 positions)** *(Angle 4 Finding 1; BUG-031)*
- **Spec:** Principle 11 (`:112`) + §0.7 (`:266`) — obligations "at every fault-prone operation … no deferral."
- **Source:** `CollectObligations` (`ProofEngine.cs:208-270`) never passes any `.Guard` to `WalkExpression`; `AccessModes` is not iterated. Positions: `TypedTransitionRow.Guard` (`SemanticIndex.cs:526`), `TypedRule.Guard` (571), `TypedEnsure.Guard` (582), `TypedAccessMode.Guard` (592), `TypedStateHook.Guard` (600), `TypedEventRow.Guard` (612).
- **Probe:** baseline — `rule A > 0` (A optional) → `PRE0116` "Cannot prove that 'A' is present," `success:false`. Same fault in each guard position (e.g. `from S1 on E when A > 0`, `rule B > 0 when A > 0`, `on E when A > 0 -> …`) → `proofObligations:[]`, `success:true` in all six.
- **Classification:** missing (creation). **Owner:** WU-OBLIG-CREATE.
- Note: the readiness plan files BUG-031 under a *discharge* sweep; per the code it is a *creation* gap (obligation never born, not mis-discharged).

**G07 — Member-call arguments never enrolled** *(Angle 4 Finding 2; BUG-032)*
- **Source:** `WalkExpression`'s `TypedMemberAccess` case (`ProofEngine.cs:319-323`) emits `ma.ProofRequirements` and recurses `ma.Object` only — `ma.Arguments` (`SemanticIndex.cs:113`) never walked.
- **Probe:** `rule Coll.at(A) > 0` (Coll list, A optional) → the `.at` index-bounds obligation fires, but the optional read `A` (which raises PRE0116 in a walked position) produces **no** Presence obligation.
- **Classification:** missing (creation). **Owner:** WU-OBLIG-CREATE.

**G08 — Field default-expression sub-tree never walked** *(Angle 4 Finding 3)*
- **Source:** `CollectDefaultObligations` (`ProofEngine.Analysis.cs:420-451`) runs only bound-collectors on `TypedField.DefaultExpression` (`SemanticIndex.cs:443`); never calls `WalkExpression`.
- **Probe:** `field X as integer default 10 / 0` → `success:true, proofObligations:[]`. Identical `10 / 0` in `set B = 10 / 0` → `PRE0083`. Not constant-folding — the obligation exists on the node, is never collected from the default position. Sub-case: `default [10/0]` list elements share the hole.
- **Classification:** partial. **Owner:** WU-OBLIG-CREATE.

**G09 — Event-argument default-expression sub-tree never walked** *(Angle 4 Finding 4)*
- **Source:** `CollectArgDefaultObligations` (`ProofEngine.Analysis.cs:762-787`) mirrors G08 — bound-checks only on `TypedArg.DefaultExpression` (`SemanticIndex.cs:494`).
- **Probe:** `event E(X as integer default 10 / 0)` consumed via `set B = E.X` → `success:true, proofObligations:[]`.
- **Classification:** partial. **Owner:** WU-OBLIG-CREATE (shared fix shape with G08).

**G10 — `PRE0088 ChoiceElementTypeMismatch` is dead (false-clean)** *(Angle 3 F-DIAG-01)*
- **Source:** enum `DiagnosticCode.cs:88`; allow-listed `DiagnosticCoverageAllowLists.cs:55` "no emission site wired." Zero emission sites (grep).
- **Probe:** `field X as choice of string("a", 5) default "a"` + `field Y as choice of integer(1, "b") default 1` → `success:true, diagnosticCount:0`. A type-mismatched choice member compiles clean.
- **Spec:** choice element typing locked (`primitive-types.md §choice`; parser table `precept-language-spec.md:1207` lists the code).
- **Classification:** missing (dead, false-clean). **Owner:** WU-CHOICE-ELEM.

**G11 — `PRE0090 ChoiceMissingElementType` is dead** *(Angle 3 F-DIAG-02)*
- **Source:** enum `DiagnosticCode.cs:90`; allow-listed `:56`. Zero emission sites.
- **Probe:** `field X as choice("a","b")` → only unrelated `PRE0093`; the missing-element-type code never fires (the bare-`choice(...)` path may reject earlier via `ExpectedToken`).
- **Classification:** missing/dead. **Owner:** WU-CHOICE-ELEM — confirm whether the grammar makes this structurally unreachable (then remove) or wire it. (Neutral note, not a fork.)

**G60 — Field-reference modifier bound (`min Floor`) silently accepted but never enforced** *(BUG-020; surfaced by the v2-completion reconstruction margin flag, `v2-completion-reconciliation-2026-07-14.md:152`)* — **NEW, was absent from the ledger's 45**
- **Spec:** `precept-language-spec.md §2.4` — a field-reference bound `min Floor` is a relational constraint equivalent to `rule Amount >= Floor`; it must be bound and proof-participated.
- **Source:** `TryGetComparableModifierValue` (`TypeChecker.Validation.Modifiers.cs:462-480`) returns `null` for an `IdentifierExpression` bound; the null is dropped silently (`TypeChecker.cs:559-577`). The bound is parsed and discarded — never desugared, never bound to proof.
- **Probe (verbatim, this pass):** `field Floor as number default 10` + `field Amount as number min Floor default 5` → `success:true`, only `PRE0119` (no outgoing transition) + two `PRE0158` (no write site). No error despite `5 < 10` plainly violating `Amount >= Floor` — the bound is accepted and inert. (The *undeclared-name* half of BUG-020 is already fixed: `field Amount as number min Nonexistent default 0` → `PRE0017 "Field 'Nonexistent' is not declared"`. The residual is the **enforcement** half only.)
- **Classification:** diverges (soundness — a declared governance bound that does nothing). Enforcement is **subsumed by the locked relational-rules-and-bounds design** (`min Floor` desugars to a relational rule that participates in single-pass narrowing). **Owner:** WU-RULE-CONTRA (shares the relational-narrowing fix shape).

### 2.B implement-against-locked-spec — diagnostic quality + test coverage (5)

**G12 — `PRE0018` template misused for operator/function type errors** *(Angle 1 F5)* — messages like `"Expected a period value here, but got 'period'"`, `"Expected a sqrt value here, but got 'decimal'"` (sqrt is a function, not a type). Rejections correct; messages self-contradictory. Spec §0.8 principle 3 (`:290`). **Owner:** WU-DIAG-MSG.

**G13 — `PRE0084` hardcodes "sqrt(...)" in the message for `pow` obligations** *(Angle 1 F6)* — `field R as integer <- pow(B, E)` → `"'E' can be negative … so sqrt(...) is unsafe"` (obligation description correctly reads "Exponent must be non-negative for integer pow"). **Owner:** WU-DIAG-MSG.

**G14 — `PRE0078`/`PRE0135`/`PRE0114` message-content defects** *(Angle 1 F7)* — PRE0078 says "representable range" for a declared `[0,10]` bound violation; PRE0135 emits a literal unfilled `?` placeholder ("has ? character(s)"); PRE0114 labels a dimension ("mass"/"length") as a "Unit." **Owner:** WU-DIAG-MSG.

**G15 — Duplicate emission for qualifier mismatches** *(Angle 1 F7)* — `PRE0070`+`PRE0114` (currency) and `PRE0071`+`PRE0114` (dimension) both fire for one defect. **Owner:** WU-DIAG-DEDUP (the emission-ownership/dedup workstream).

**G16 — Diagnostic `ExampleBefore` snippets don't trigger their own code** *(Angle 5 F-3)* — `Diagnostics.cs:574` (PRE0063 example uses `queue.first` → actually emits PRE0020) and `:582` (PRE0064 example uses `set` remove → compiles clean). Underlying prevention IS live via correct shapes. Mechanical doc-fidelity repoint. **Owner:** WU-DOC-SYNC.

**G17 — No emission-reachability guard behind `[StaticallyPreventable]`** *(Angle 5 F-2)* — `Precept0002` checks only attribute *presence*; `FaultMapDerivationTests` checks only map derivation. Neither asserts the referenced `DiagnosticCode` is *emittable* → G10/G11/OF2's deadness produces green tests. Add an emission-coverage test asserting every `PreventsFault` code has ≥1 positive-creation probe. **Owner:** WU-TEST-EMIT. (This test would have caught G10, G11, and the dead arm of OF2.)

### 2.C implement-against-locked-spec — doc-drift / hygiene (17)

All **Owner:** WU-DOC-SYNC. No spec-behavior change; doc/enum/comment corrections against locked reality.

| ID | Gap | Anchor / evidence |
|---|---|---|
| **G18** | §Status/§5 understate how much prove-or-reject is live (fault/bounded-write checks reject at Error today); §0.6 status table (`:239-245`) omits the Warning-vs-Error severity dimension | `precept-language-spec.md:10, :2133, :239-245` |
| **G19** | Transient `(D3 baseline)` label + `DNN` decision-ID cluster inside canonical spec | `precept-language-spec.md:1730` (the actual `(D3 baseline)` literal; the WF-0 lead's cited `:1069` reads "**D3 default**" — a *different* token). Cluster: D3/D94/D130/D131/D132/D143 at 1069,1073,1076,1078,1081,1083,1356,1730 |
| **G20** | Stale `C120` prefix for the conflicting-modifier diagnostic; emitted code is `PRE0120` | `precept-language-spec.md:1683, :1696` |
| **G21** | Three allow-list tracking comments falsely say "no emission site wired" — the codes are LIVE (PRE0083 DivisionByZero, PRE0084 SqrtOfNegative, PRE0101 KeyUniquenessGuard); they misinform the build into thinking shipped safety checks are unbuilt | `DiagnosticCoverageAllowLists.cs:52, :53, :27` |
| **G22** | Two retired codes still in enum + metadata + doc: PRE0014 (`EventHandlerDoesNotSupportGuard`), PRE0019 (`NullInNonNullableContext`, "pending removal") | `DiagnosticCode.cs:37, :48`; `Diagnostics.cs:129, :176`; `diagnostic-system.md:240, :587` |
| **G23** | §1.7 numbering discontinuity — body jumps §1.6→§1.8; `git log -S'### 1.7'` returns nothing (never existed). Mechanical renumber unless §1.7 was a reserved slot | `precept-language-spec.md:708→776`, Contents `:42` |
| **G24** | D4 collection status row states "Not yet built: quantifier predicates" — but quantifiers are **live** (probe: `rule each t in Tags (t.length > 0)` → `success:true, 0 type errors`). Freeze carried a known-stale claim into the denominator | `collection-types.md` header row (v1-recon MISS) |
| **G25** | Stale D10 text: "Default `maxplaces` derived from currency code" contradicts retired-D10 "no implicit maxplaces" in the same doc | `business-domain-types.md:1989` vs `:528, :1579, :1762, :1864` |
| **G26** | `SyntaxReference` snippet stale: claims strings have "no interpolation" and numbers have "no scientific" — both contradicted by §1.3 | `catalog-system.md:1219-1220` vs `precept-language-spec.md:644, :621` |
| **G27** | Catalog count arithmetic: `ValueModifierMeta` "15 members" vs "14"; Operations "201 entries" vs "9 unary + 194 binary" (=203); `Constructs (15)`/`Modifiers (29)` residual count-drift | `catalog-system.md:481 vs :492`, `:504 vs :529`, `:271, :282` |
| **G28** | `PRECEPT0007` cited for two different analyzers (calendar-variable-period vs GetMeta-exhaustiveness) | `temporal-type-system.md:262` vs `catalog-system.md:906` |
| **G29** | `is in [...]` recommended as idiomatic substitute for string ordering — **no such operator/production exists** in the grammar | `primitive-types.md:112, :539, :547` |
| **G30** | `positive`+`nonnegative` framed as "conflict structurally" in one place, `RedundantModifier` (warning) in another | `primitive-types.md:214/247/282` vs `:596`, `precept-language-spec.md:1695` |
| **G31** | `log of T by P` missing-uniqueness-guard: spec text (`collection-types.md:392`) names `UnguardedCollectionAccess` (an *access* code), but the implementation emits the correct mutation-appropriate uniqueness code. **Probe verified doc-drift, not behavioral (Frank §4).** Verbatim probe: unguarded `append AuditLog Record.Msg by Record.Seq` → `PRE0101 (error) "Key 'element' may already exist in 'AuditLog' — add a 'when not (AuditLog contains element)' guard before appending"` + obligation `KeyPresence "Ordering key must not already exist in the log-by (uniqueness)" → Unresolved`. PRE0101 = `KeyUniquenessGuard`, a mutation precondition on the append — **not** an access code raised on a mutation. So the code is correct; only the spec text is wrong → **stays doc-drift.** (Corrects this row's original premise, which asserted `UnguardedCollectionAccess` fires; it does not. Minor: the message placeholder reads `'element'`, not the actual key expr — message-quality note, entangled with the G12 family.) | `collection-types.md:384, :392` vs `:724-729` |
| **G32** | `log of T by P` present in spec's mincount/maxcount-applicable list but omitted from collection-types.md's enumeration | `precept-language-spec.md:1674` vs `collection-types.md:777` (grants at `:427`) |
| **G33** | `## Contents` / `## Cross-References` navigational blocks carry no explicit "non-normative" marker (pass the floor only by convention) | structural-floor S3 |

### 2.D out-of-scope-with-trigger (7)

| ID | Gap | Re-open trigger |
|---|---|---|
| **OS1** | Assignment-out-of-declared-bounds reports as `NumericOverflow` "exceeded representable range" (borrows overflow wording; `FaultCode.OutOfRange`'s own template never surfaces for this case). Prevention *holds* — message precision only | Diagnostic-message/taxonomy sweep, or owner decides assignment-out-of-bounds warrants its own code. No spec statement mandates the split. *(Angle 2 C)* |
| **OS2** | Nine dead-*specific* codes where the check RUNS via a generic code — PRE0011/0012 (parser → PRE0009), 0022/0051 (→ PRE0018), 0059/0060/0061/0062 (temporal → PRE0018/0052), 0065 (→ PRE0104). Soundness bar met; specificity gap only | A diagnostic-quality/teachability workstream, or spec text that *names* the specific code as the contract for that fault. **Do not classify as soundness holes** — the check runs. *(Angle 3 F-DIAG-05.)* Note: PRE0022 overlaps OF2 |
| **OS3** | Four soft-marked `_TBD_` Open-Questions placeholder sections (spec `:2155`, primitive `:686`, temporal `:1926`, business `:2077`) | The named stage advances (evaluator / proposal-advance). *(Floor S2)* |
| **OS4** | `FaultSiteLink` runtime fault-identity collapses to `FaultCode.DivisionByZero` backstop for `OutOfRange` and `QualifierMismatch` (non-bijective). Compile-time prevention fully live; only the stamped runtime identity is wrong | When runtime fault-surfacing / carries-proof identity is built and the reported code becomes observable. *(Angle 5 F-4)* |
| **OS5** | `TypedUnaryOp`/`UnaryOperationMeta` have no `ProofRequirements` slot. Only unary ops are negations, whose sole fault (MinValue overflow) is caught at the enclosing containment site — empty slot defensible-by-construction | If the frozen numeric-overflow model makes any unary op *independently* faultable (not subsumed by an enclosing obligation). Verify against the overflow spec. *(Angle 4 structural obs)* |
| **OS6** *(NARROWED — §5.2 G59)* | ~~three malformed-basis codes~~ **now LIVE (PRE0160/0118/0162) → converted to doc-drift G59.** Residual OS6 = only `MissingOrderingKey` PRE0151 (currently PRE0048 fires), still reserved | When the ordering-key feature's implementation lands. *(Re-extract #16; §5.2 update)* |
| **OS7** *(CLOSED — §5)* | ~~Full cell-by-cell enumeration of the 4 type-docs' per-operator/per-accessor matrices~~ **DONE.** §5 enumerates ~984 cells across the 4 matrices, all cell families live-probed; the long tail surfaced 26 implement gaps (G34–G59) + OF6. No sub-matrix left at family-representative level | — closed. *(§5)* |

### 2.E Notes (flagged, not gaps)

- **N1** — Subsequence *direction* (which operand may be the subsequence of the other) is unstated in both `precept-language-spec.md:1398` and `primitive-types.md:348`. Relevant to G01's implementation shape; surfaces during WU-CHOICE-CMP design. *(Re-extract #12)*
- **N2** — `clear` on optional scalar fields (reset-to-unset) is claimed only in the spec's action table (`:1711`); `collection-types.md` models `clear` as collection-only. Not contradictory, but a single-source claim worth confirming during any `clear` work. *(Re-extract #14)*

---

## 3. OWNER DECISIONS — the morning-review section

> ## ⚠ THIS SECTION IS A 2026-07-13 SNAPSHOT AND IS STALE
>
> **Rulings landed after this ledger was written and were never swept back into it.** OF5 was ruled by the owner on 2026-07-14 — the day after — and its entry below still says "owner must rule" and still quotes spec text that was deleted that same day. A reader taking this section at face value on 2026-07-23 concluded the whole plan was gated on six open forks, and was wrong about at least one of them.
>
> **Before treating any fork here as open, check for a ruling dated after 2026-07-13.** The owner's rulings from that window live in commit messages, in `docs/Working/exhaustive-gap-analysis-2026-07-14/`, in the owner-authored want doc (`what-i-want-2026-07-16.md`), and in the obligation-discharge matrix, which carries owner rulings from 2026-07-19 onward. They do not live here.
>
> Each fork is being re-checked; resolved ones are being stamped in place with the ruling, its date, and its citation. A fork with no stamp has not yet been re-checked — it is not thereby open.

Nothing here is settled. Each item is framed two-sided with the locked text quoted. WF-2 cannot write acceptance criteria on these until the owner rules.

### OF1 — Do constraint-message interpolation holes carry proof obligations? *(Angle 4 Finding 5)* — **most consequential** — ⚠️ **RE-CHECKED 2026-07-23: the value arm is already committed in the matrix layer; only the presence arm is a live fork. Canon currently contradicts itself.**

> **Value-fault arm — effectively decided, post-ledger.** The matrix's fault case shape names message interpolation as a fault evaluation site (`obligation-discharge-matrix-2026-07-19.md:205`, a generative section the owner reads at ratification), citing the want doc's own worked example, which puts a division inside a refusal message (`what-i-want-2026-07-16.md:72`). Eight cells were authored against it, **all `defined`** — four at `reject-message-interpolation`, four at `constraint-rationale-interpolation` (`fault-6-division-primitive-message-interpolation.cells.json`). The compiler's silence is booked there as a **build gap, not a definitional exclusion**.
>
> **But canon still says deferred.** `docs/compiler/soundness-and-coverage.md:229` records the whole fork as deferred pending an owner ruling. So the canonical doc and the matrix — which is destined to become canon — disagree. That is the live inconsistency, and it is narrower than this entry's framing: the value arm needs **confirmation or reversal of a commitment already made**, not a fresh two-sided decision.
>
> **Presence arm — genuinely open, and its proposed resolution is new language surface.** No matrix cell covers Presence × interpolation; `Presence` is one of four fault kinds the matrix records as having no validity argument at all. The recommendation below resolves it via *"a language guarantee that message rendering is presence-tolerant"* — that guarantee exists nowhere in `docs/`, `research/` or `src/`, and creating it is a Tier-2 language-surface change under the consultation gate. It cannot ride along with an OF1 ruling.
>
> **✅ VALUE-FAULT ARM RULED BY THE OWNER, 2026-07-23**: *"yes we should catch that at compile time."* Value-fault expressions in a constraint message enroll. This confirms the commitment the matrix had already made and resolves the canon self-contradiction; `docs/compiler/soundness-and-coverage.md` is corrected in the same pass, its row split so the value arm reads in-scope-not-yet-implemented with the obligation-creation sweep as its completion trigger.
>
> **The presence arm remains OPEN** and was deliberately not settled by that ruling — it is a different question, and the resolution on file depends on new language surface.
>
> **Pairs with BUG-053** (filed 2026-07-23), which records the same non-minting one position over, for optional event arguments.

**Structural fact (certain):** message holes create **zero** obligations of any kind. `CollectObligations` walks a rule/ensure's `.Condition` only; `.Message` (`SemanticIndex.cs:572, :583`) is never passed to `WalkExpression`.
- **Probe:** `rule B > 0 because "bad {10 / 0}"` → `success:true, proofObligations:[]` (no PRE0083). An optional-field hole `because "value {A}"` likewise raises no PRE0116.

**The fork (genuinely two-sided):**
- **Read A — enroll (prove-or-reject is unqualified).** Locked text, §0.7 (`:266`): *"It never compiles a fault-prone operation in the hope a runtime check catches it; there is no deferral."* Principle 11 (`:112`): *"Every fault class that the evaluator can produce … is linked to a compiler diagnostic that prevents it."* A `{10 / 0}` in a message is a fault-prone operation that crashes when the message renders on violation — leaving it unenrolled is a literal breach.
- **Read B — exempt (messages are diagnostic text).** A message evaluates lazily only on constraint failure, and interpolating a *possibly-absent optional field* is the message's whole purpose (reporting which field failed). Firing a Presence obligation there would force authors to guard the very field they are reporting.

The locked text neither carves out messages nor enumerates them as faultable positions. **Question for the owner:** should message holes carry obligations — for all fault classes (Read A), for value-faults but not presence (a split), or not at all (Read B)? This gates 2 of WU-OBLIG-CREATE's positions and, more broadly, defines the boundary of the prove-or-reject guarantee.

**Frank's recommendation (not ratified — owner must rule):** **SPLIT.** Value-faults (div-by-zero, overflow, sqrt-of-negative) in a message MUST enroll — a `because "bad {10/0}"` crashes the evaluator when the message renders on violation, and §0.7's "no deferral" applies verbatim. Presence obligations on optional fields MUST NOT enroll — interpolating the possibly-absent field is the message's whole job. Frank's clean resolution is a *language guarantee that message rendering is presence-tolerant* (an unset optional interpolates to empty/"unset", never throws), making the presence obligation unnecessary by construction while value-faults still enroll: route the value-fault arm into WU-OBLIG-CREATE; the presence arm becomes a runtime null-tolerance spec note.

### OF2 — `FunctionArgConstraintViolation` (PRE0022 / `FaultCode` 8): build the capability, or remove the scaffolding? *(Angle 5 F-1)* — has a **new-surface arm** — ✅ **RE-CHECKED 2026-07-23: NOT A FORK. The locked spec answers it, and the entry's central premise is false.**

> **The machinery it says is missing exists.** There is no type called `FunctionSignature`; the catalog record is `FunctionOverload`, and it carries `ProofRequirements` — per-parameter constraints targeted by object identity through `ParamSubject`, analyzer-enforced by `PRECEPT0005`. It is populated for `pow` and `sqrt`, walked by the proof engine, and **rejects today**: `pow(B, -1)` produces `Exponent must be non-negative for integer pow`, `Unresolved`, and the file is refused.
>
> **The locked spec names this code for this exact example.** `precept-language-spec.md:1614` — *"| Arg constraint violation | `round(x, -1)` — negative places | `FunctionArgConstraintViolation` |"*. The per-parameter semantics are defined in the same table (`:1581`, `:1585`, `:1596`) and repeated in `primitive-types.md:646`, `:613`, `:684`. The entry's claim that *"no locked spec grounds it"* is false.
>
> **So the standing posture decides it**: locked spec plus missing implementation is a build, not an owner fork, and the Pre-Design gate covers *new* surface — which this is not, since the catalog field, the subject type, the analyzer guard and the proof-engine walk all exist.
>
> **What is actually left, none of it a decision:** correct the drift at `soundness-and-coverage.md:196`, whose two parentheticals are both false and which was authored in a commit that flags its own content as not correct as it stands; fix the diagnostic routing (**BUG-055** — `pow` violations currently report *"so `sqrt(...)` is unsafe"*, and PRE0022, the right code, is unused); add the missing `round`/`mid` requirements as catalog data; drop the allow-list entry and its false comment.
>
> **One sentence genuinely worth the owner's** — not a blocker: prior research set a boundary that catalog parameter constraints must stay *computability-only*, or a discretionary band smuggles a business rule into the fault lane. Everything pending is computability, so nothing crosses it; it is a rule for future entries.

**Structural fact (certain):** the `[StaticallyPreventable]` attribute is present (so `Precept0002` passes) but **PRE0022 has zero emission sites**, and `FunctionSignature`/`Parameter` (`Functions.cs`) carries no per-argument constraint field — only a return-value flag. No machinery lets a function argument declare or violate a constraint.
- **Probe:** the diagnostic's own canonical `ExampleBefore` — `round(X, -1)` — compiles clean (`success:true`, only PRE0158 warning). `round`'s `places` param is `integer` with no constraint, so `-1` is type-valid.

**The fork:**
- **(a) Build it** → add a per-parameter value-constraint field to `FunctionSignature` + PRE0022 emission + define which functions carry which arg constraints. **This is new catalog/language surface → route to `/design` (Pre-Design Owner Consultation gate) before any work.**
- **(b) Remove it** → delete `FaultCode 8` + `DiagnosticCode.FunctionArgConstraintViolation` + metadata as unbuilt scaffolding, per the "remove only unintended drift" posture.

Neither reading is grounded by locked spec text — `diagnostic-system.md:243/596/635` merely re-lists the code; no doc defines the constraint semantics. The closest item (`primitive-types.md:701`, `maxplaces -1` → `InvalidModifierValue`) is a *modifier-value* check under a *different* code. This is a genuine hole in the guarantee (a `[StaticallyPreventable]` fault whose prevention is not live); closing it in either direction is an owner call.

**Frank's recommendation (not ratified — owner must rule):** **REMOVE the scaffolding now; re-open via `/design` only on concrete demand.** Two reasons: (1) the "build" arm is new catalog/language surface with zero locked-spec grounding — it cannot even be proposed inline (routes to `/design` under the Pre-Design Owner Consultation gate) and there is no demonstrated demand; the canonical example (`round(X,-1)`) is already served by the live `InvalidModifierValue` path. (2) The dead `[StaticallyPreventable]` scaffolding is actively harmful — it makes `Precept0002` pass green while the guarantee is hollow (the G17 false-green pathology); removing `FaultCode 8` + `DiagnosticCode.FunctionArgConstraintViolation` + metadata restores the honesty of the fault-prevention invariant. Also closes half of OS2's PRE0022 overlap.

### OF3 — `notempty` on collection *fields*: string-only, or also collection cardinality? *(Re-extract #1)* — ✅ **RULED 2026-06-03, five weeks BEFORE this ledger. NOT OPEN.**

> **Ruling:** `notempty` is **string-only**. Owner-authorized in `docs/Working/Archive/modifier-name-axis-overlap-2026-06-03.md` (`Locked 2026-06-03`), Decision 1 — *"Retarget `notempty` from `StringAndCollectionTypes` to `StringOnly`"* — reversing the collection half of an earlier locked decision via owner Direction A. Promoted to canon 2026-06-04 and shipped in `c3209ad2`: *"it's now a string/element modifier … and no longer names collection cardinality — that's `mincount 1`"*.
>
> **It is structurally irreversible.** `Modifiers.cs` targets `StringOnly`, and analyzer `PRECEPT0031` fails the build if any value modifier spans both a collection kind and a scalar kind. Re-widening it would require deleting an analyzer.
>
> **The recommendation below reached the right answer for a weaker reason** — it counted three locked surfaces against one. The actual ground is an owner-authorized locked design that predates the ledger.
>
> **Residual drift, not decisions:** `primitive-types.md:582` still carries the pre-retarget text and even links to the doc that now contradicts it; and the `ElementPositionValueTokens` comment at `Modifiers.cs:356` still lists `notempty` among modifiers excluded as collection-applicable, which the code three lines below contradicts.
>
> **A 2026-07-23 probe wrongly reported `notempty` on a collection as "completely inert".** That was a false finding from a probe that exercised only a cardinality site. Verified: `field Tags as set of string notempty` with an element write mints `LengthContainment [1 .. ∞]` and rejects (`PRE0135`). It is live and element-routed; it is inert against *cardinality* only, which is the ruling working as intended.

- `primitive-types.md:582` says `notempty` applies to `string` **and** to `set, queue, stack, log, bag, list, queue of T by P` ("equivalent to `mincount 1`").
- `precept-language-spec.md:1150, :1671, :435` and `collection-types.md:794` say `notempty` is **string-only**; in inner-type position it constrains each element, and collection *cardinality* is exclusively `mincount 1`.

Two locked texts give incompatible answers for `field Tags as set of string notempty` as a field-level modifier. **Question:** is field-level `notempty` on a collection a synonym for `mincount 1` (primitive-types reading) or a type error / inner-only (spec + collection-types reading)?

**Frank's recommendation (not ratified — owner must rule):** **string-only; `primitive-types.md:582` is the drift.** Three locked surfaces say string-only (`precept-language-spec.md:1150, :1671, :435`; `collection-types.md:794`) against primitive-types.md's one overreach. Architecturally decisive: collection cardinality vocabulary is deliberately single-source (`mincount`/`maxcount` in the Constraints catalog); making `notempty` a second spelling of `mincount 1` creates two ways to say one thing and muddies the inner-type-vs-field-level distinction (`set of string notempty` already means *each element* non-empty). Frank would downgrade this from owner-fork to **doc-drift-with-forced-resolution** — fix `primitive-types.md:582` to string-only — but keep it surfaced because it touches Constraints-catalog applicability metadata (language surface).

### OF4 — Are `in`-qualified periods orderable? *(Re-extract #9)* — ✅ **RULED BY THE OWNER 2026-07-24: outcome (a) — orderable when same-basis-pinned, enforced at the comparison site.**

> **Ruling (owner, 2026-07-24):** `in`-qualified periods are **orderable exactly when both operands share one declared single basis** (`period in 'days'` vs `period in 'days'`), scalar and collection alike. Cross-basis (`days` vs `months`) and unqualified periods are **not** orderable and must be rejected at the comparison site. This is outcome (a) with the same-basis guard; it subsumes (c) (the collection-accessor position is the special case where a single basis is structurally guaranteed).
>
> **Deciding evidence — NodaTime 3.3.3, verified by reflection (2026-07-24).** `NodaTime.Period` implements `IEquatable` but **not** `IComparable`/`IComparable<Period>` — no `CompareTo`, no `op_LessThan`. NodaTime deliberately omits period ordering for D14's own reason (no fixed length without a reference date). `NodaTime.Duration` *is* fully orderable (`IComparable`, `IComparisonOperators`, `<`) because it is a fixed span. **But** a single-basis period's live component is a plain integer (`Period.FromDays(5).Days` → `Int32`), so same-basis ordering never calls `Period.CompareTo` — it reduces to integer comparison of the one extracted component, which is total and well-defined. Cross-basis comparison is exactly what NodaTime refuses and what has no coherent answer. So the ruling draws the orderable/not line precisely where NodaTime (and arithmetic) draws it: same basis yes, mixed basis no.
>
> **Downstream consequences (build, not settled here):**
> - **D14 (`temporal-type-system.md:1476`) needs the same-basis carve-out** — it currently reads as an absolute "no ordering, `==`/`!=` only." `collection-types.md:533` is *not* drift under this ruling; it is the same-basis rule already applied in the accessor position.
> - **New mechanism required → `/design`.** `TypeTrait` is a flat per-`TypeKind` flag; a qualifier-conditional, same-basis-checked ordering trait does not exist. This is new language surface (ordering operators on a type that has none today) and routes through the pre-design gate before build. Neither reading is implemented at HEAD.
> - **Close the business-type false-clean in the same rule.** `set of money` **unqualified** → `.max` compiles today (`collection-types.md:530` says it should be a type error — a live false-clean cross-currency ordering). The same "orderable only when a single basis/currency is shared" rule should govern both the temporal and the business lanes; design them together.
> - **BUG-047 is no longer mechanical.** Its `positive`-on-`period` strip was justified as a mechanical D14 correction; under (a) a qualified period *is* orderable against `Period.Zero`, so `positive`/`nonnegative` on a same-basis period is meaningful. Do not action the strip; re-evaluate it under this ruling.

---

*(Original 2026-07-23 re-check retained below for the reasoning that led here.)*

> **⚠️ RE-CHECKED 2026-07-23: NOT already ruled, and this entry understates it. It is a behavioural fork with three outcomes, not a doc fix.**

> **The 2026-05-30 owner Resolution rules the premise, not the conclusion.** That Resolution permits a single-basis `period` *divisor* because it has "a well-defined unit *and* count". That is the premise the YES reading needs, and it is owner-authored. It also disposes of the objection that D14 already considered this: D14's rejected alternatives are *reference-date* and *approximate* ordering, and single-basis ordering is neither.
>
> **But division is one-operand admissibility; ordering is two-operand compatibility.** `period in 'days'` and `period in 'months'` are each individually admissible as divisors, yet comparing them is exactly "Is 1 month greater than 30 days?" — D14's own stated reason. So single-basis pinning does not discharge D14; **same-basis** pinning would. `collection-types.md:533` escapes this only because a set's elements share one declared basis. The scalar `<`/`>` case has no such guarantee.
>
> **A third canonical text contradicts both sides and nobody cited it.** `temporal-type-system.md:842` grants `nonnegative` on an *unqualified* `period`, "compared against `Period.Zero`" — an ordering relation, live in the catalog. D14's "`==` and `!=` only" is already not literally true.
>
> **Neither reading is implemented.** `TypeTrait` is a flat per-`TypeKind` flag and no qualifier-conditional trait mechanism exists. Verified 2026-07-23: `set of period in 'days'` → `.max` is rejected (`PRE0104`), so `collection-types.md:533` does not work today; and `rule Grace < Limit` on two `period in 'days'` fields is rejected with `PRE0018` *"Expected a period value here, but got 'period'"* — a message that never mentions ordering.
>
> **It cannot be ruled in isolation.** The same trait mechanism fails the opposite way for the business types: `set of money` **unqualified** → `.max` **compiles**, which `collection-types.md:530` says should be a type error. That is a live false-clean cross-currency ordering. The v1 spec-coverage audit reached the same conclusion independently and logged *"No decision recorded"*.
>
> **Three outcomes, not two:** (a) orderable when same-basis-pinned everywhere; (b) never orderable, and `collection-types.md:533` is the drift; (c) orderable in the collection-accessor position only, where a single declared basis is structurally guaranteed, but not for scalar comparison. (c) is consistent with all three texts and is currently proposed by nobody.
>
> **Do not action BUG-047's `positive`-on-`period` strip ahead of this.** That bug calls it a mechanical correction binding on D14; if OF4 rules (a) or (c), it stops being mechanical.

- `collection-types.md:533` grants `.min`/`.max` on `set of period in 'days'` (single-component ordering).
- Temporal D14 (`:1476`) states periods have **no ordering, `==`/`!=` only**, with no qualified-basis exception; `business-domain-types.md` nowhere grants ordering to `in`-qualified periods.

Two defensible readings (unit-pinned periods orderable by their single component vs. periods never orderable). **Question:** does pinning a period to a single unit basis make it orderable?

**Frank's recommendation (not ratified — owner must rule):** **YES — orderable when unit-pinned; D14 needs the carve-out.** The two texts aren't in conflict once you read D14's own qualifier: the `in <basis>` qualifier is exactly the mechanism that removes the incomparability (`'1 month'` has no fixed length, but a single-basis period is a total order on its one component — the same honesty-about-approximation move the qualifier system exists for). `collection-types.md:533` already did the reasoning; D14 (`:1476`) just never carved out the exception and reads as an absolute "no ordering" (the drift). Resolution: orderable when pinned to a single unit basis; update D14 to reference the `in`-qualified exception. Doc fix, not a behavioral fork.

### OF5 — `integer` overflow stance: three postures in one doc *(Re-extract #10)* — ✅ **RULED BY THE OWNER 2026-07-14. NOT OPEN. The entry below is a stale snapshot; read this stamp instead.**

> **Ruling (owner, 2026-07-14 — commit `90f0792f`):** `integer` is **fixed-width 64-bit today**. The overflow *model* — compile-time proof-or-reject versus adopting an arbitrary-precision representation — is a **deferred post-MVP decision**, with arbitrary precision still a live option. Recorded in prose at `docs/Working/exhaustive-gap-analysis-2026-07-14/coverage-report.md:49`.
>
> **This overrode the recommendation below.** The recommendation wanted `:81`'s arbitrary-precision language deleted as wrong against the runtime; the owner instead kept the model open and had `:81` rewritten to state fixed-width *and disclose the deferral*. `:197`/`:203` were deliberately left as the target end-state, consistent with the standing "the spec states the target" principle.
>
> **The quoted contradiction no longer exists.** `primitive-types.md:81` now reads *"Fixed-width 64-bit whole numbers; no rounding. Overflow handling … is a deferred post-MVP decision"*. The text this entry quotes was deleted on 2026-07-14.
>
> **Reaffirmed twice since**, each an explicit owner act routing a new case *into* the same deferral rather than reopening it: temporal-arithmetic overflow (`d32d0135`, 2026-07-20) and approximate-lane infinity (`ce72725d`, 2026-07-21).
>
> **The philosophy-adjacent flag was already cleared** — on 2026-07-12, before this ledger was written: `proof-engine-decision-ledger-2026-07-12.md:62` records the owner leaving the present-tense overflow claims as correct-as-target, with no `philosophy.md` edit and Principle 11 un-reworded.
>
> **OS5 does not re-activate.** Its trigger is a *frozen* overflow model; the 07-14 ruling deferred the model rather than freezing it, so the condition is unmet. Stated directly in the same commit's record: *"Unary `-long.MinValue` negation overflow sits in the same parked lane and re-activates when the overflow model is ruled."*
>
> **Still genuinely open, and separate from OF5**: GATE-O (fault-code disposition for integer-conversion overflow and non-finite values), and underflow, which the matrix records as open and unrouted. Neither is this fork.



`primitive-types.md` states, for `integer`: `:81` "Arbitrary-precision whole numbers; **no overflow**"; `:197` "**Overflow is a type error**"; `:205` "Checked overflow." These are three mutually exclusive postures. **Question:** is `integer` arbitrary-precision (no overflow possible), or fixed-width with checked/type-error overflow? This determines whether OS5 (unary-op obligation) and the whole `NumericOverflow` obligation family have an `integer`-lane surface at all.

**Frank's recommendation (not ratified — owner must rule) — ⚠️ PHILOSOPHY-ADJACENT, needs explicit owner ratification:** **fixed-width, checked Int64 overflow; `:81` is wrong against the runtime.** Frank settled this by reading the code: integer literals parse with `long.TryParse` (`TypeChecker.Expressions.cs:180, :203`; `TypedConstants.cs:115`; modifier-value checks `Validation.Modifiers.cs:548/578/599`) → integer is backed by **Int64, fixed-width**; there is no arbitrary-precision type in the boundary (`PreceptValue` is still a stub). So `:197`/`:205` match reality and `:81` "arbitrary-precision / no overflow" is aspirational copy that would make the entire integer-lane `NumericOverflow` family dead scaffolding. Frank's ruling: fixed-width Int64 with checked overflow — provable overflow is a compile-time Error, unprovable is an obligation (also Error); fix `primitive-types.md:81`.
- **⚠️ Frank explicitly flags this as philosophy-adjacent (per his charter — he surfaces, does not resolve):** "arbitrary-precision, no overflow" is a *core-guarantee / type-honesty* claim under `docs/philosophy.md` "Honesty about approximation." Deciding integer is fixed-width Int64 changes what the language promises about `integer` — **the owner must ratify the posture explicitly before the doc is edited**, even though the code has decided it de facto. **Not to be treated as a mere doc-drift fix.**
- **OS5 re-activation flag (Frank):** if OF5 rules fixed-width, OS5 (the unary-op `ProofRequirements` empty slot) should **re-activate** — `-integer` where the operand can be `long.MinValue` is then an independently faultable overflow (`-long.MinValue` overflows), and OS5's "empty slot defensible-by-construction" assumption (MinValue-negation always caught by an enclosing containment site) becomes *testable and must be verified, not assumed*. Don't leave OS5 in the deferred bucket once OF5 rules "fixed-width."

### OF6 — Currency-code case: enforce uppercase-only, or normalize case-insensitively? — ⚠️ **RE-CHECKED 2026-07-23: genuinely open, and there is now a concrete argument that was not available when this was filed.**

> **Canon says uppercase, four times, but never as a Decision.** No lettered Decision block in `business-domain-types.md` addresses case — D13 (self-contained registries) and D13a (string-form serialization) are both silent on it. What exists is one teachable error row (`:587` — *"Currency codes must be uppercase (ISO 4217). Use `'USD'`."*) plus content-shape descriptions at `:559`, `:2021`, `:2025`. Normative in direction, but with no four-leg rationale behind it.
>
> **The implementation does the opposite, and arrived there silently.** `CurrencyCatalog` uses `OrdinalIgnoreCase` throughout and `ToUpperInvariant` on every lookup; `field A as money in 'usd'` compiles clean, as does cross-case (`in 'usd' default '100 USD'`). The `:587` teachable is not implemented at all. Case-folding entered in two commits — one Copilot co-authored — neither of whose messages mentions currency case. That is the AI-co-authored-departure shape, not a recorded reversal.
>
> **Determinism does not decide it.** The obvious argument — that case-insensitive comparison is culture-dependent — is already answered permissively in canon: `primitive-types.md:494` rules ordinal comparison legitimate precisely because it defuses the Turkish dotless-i problem, and the currency catalog uses exactly that sanctioned form.
>
> **⭐ New deciding evidence, not in this ledger.** Enumerating ISO 4217 against every UCUM atom and prefix combination turns up genuine case-folding collisions: **`ZAR`** (South African Rand) against **`Zar`** (zetta-are) and **`zar`** (zepto-are), plus `GIP`/`GiP`. Both UCUM forms are live. And in the polymorphic `price in` slot the currency registry is consulted **first** (`TypeChecker.cs:651-653`), so `price in 'Zar' of 'mass'` compiles **clean** — a valid UCUM unit silently captured as a currency — while `price in 'ar' of 'mass'` is correctly refused. Under uppercase-only lookup the collision cannot arise. This is a concrete cost of the permissive reading against the documented disambiguation rule at `:377`, and it did not feature in the original framing.
>
> **A related asymmetry worth ruling in the same breath**: the parallel unit teachable (*"Unit names are lowercase. Use `'kg'`."*) **is** enforced — UCUM lookup is strictly ordinal, and `quantity in 'KG'` is refused. So the two registries currently disagree about whether their own documented case rule is real.
>
> **The two readings, neutrally.** *Enforce uppercase*: make the currency lookup ordinal, wire the existing teachable as a live diagnostic, align with the already-enforced UCUM rule; cost is rejecting `'usd'` from authors and from any ingestion path that supplies lowercase. *Keep case-insensitive*: delete the teachable and soften four shape descriptions to case-insensitive-with-canonical-uppercase; cost is accepting the `Zar` ambiguity and adding a normalization step at `TypeChecker.cs:653`, which currently stores the author's raw text while the validator canonicalizes — leaving every downstream comparison responsible for remembering the ignore-case comparer. *(§5 cell-level, business D-BIZ-8)*

**Structural fact (certain):** lowercase/mixed-case currency codes compile clean. Probe: `field c1 as currency default 'usd'` / `'Usd'` / `'eur'` → all `success:true` (no PRE0053); only genuinely-invalid `'USDX'` → `PRE0053 "'USDX' is not a valid currency"`.

**The fork (genuinely two-sided):**
- **Read A — enforce uppercase-only.** Locked teachable, `business-domain-types.md:587`: *"Currency codes must be uppercase (ISO 4217). Use 'USD'."* Content-shape statement `:559`: *"content shape `<3-uppercase-letters>`."* Under Read A the current acceptance of `'usd'` is a missing-rejection soundness/teachability gap → implement-against-locked-spec.
- **Read B — case-insensitive normalization is acceptable.** ISO 4217 codes are conventionally uppercase, but normalizing to uppercase before lookup is a reasonable ingestion convenience; the `:587` teachable is then the stale artifact. The design body states the shape as `<3-uppercase-letters>` but never states case-sensitivity as a hard admission rule outside the teachable table.

**Question for the owner:** is lowercase currency input a compile error (Read A — wire the rejection to match `:587`/`:559`), or is case-insensitive normalization the intended behavior (Read B — the teachable message is stale doc-drift to fix)? Not settled here.

**Not reviewed by Frank.** OF6 arose from the §5 cell-level pass, after Frank's direction review (which covered OF1–OF5 only). No Frank recommendation exists for this fork — it needs an owner ruling with no prior agent recommendation on record.

---

## 4. v1 reconciliation result

Source: `docs/Working/compiler-readiness-plan-2026-06-11-appendices/spec-coverage-audit.md` (v1 audit, ~2026-06-11). v1 denominator joined for this backstop: **236 non-`fully` rows + 98 canon-inconsistency bullets = 334 items** (v1 has no stable IDs; joined manually).

**Load-bearing fact (git-verified this pass):** the ONLY commit touching `src/Precept` since the v1 audit is the freeze itself, whose only `.cs` change is one hover/label string (`Types.cs` dayOfWeek wording). **No pipeline logic changed.** Verification: the v1 audit landed in commit `5f66ef4f` (2026-06-12); `git log --oneline 5f66ef4f..HEAD -- src/Precept` returns exactly one line — `342e66db` (the freeze) — and `git show 342e66db -- src` shows its sole source hunk is `Types.cs` (`dayOfWeek` label: `"Day of week (0=Sunday)"` → `"ISO day of week: Monday=1, Sunday=7"`, one line). Therefore:

**Reframe — the completed v2 build-out corroborates (does not contradict) the re-derivation (per `v2-completion-reconciliation-2026-07-14.md`).** A natural objection to the "~236 re-derive, byte-identical since v1" bucket is: "but a large body of v2 work completed — doesn't that shrink the gap list?" It does not, and the v2-completion reconstruction confirms *why* by the same git fact. The real completed v2 work (the ~2026-05-23 → 2026-06-05 proof-engine/relational/satisfiability build-out) all landed **before** the v1 audit (06-11/06-12), so it is already inside *both* the v1 denominator *and* the current HEAD probes — it is baked into the baseline and **cannot shift the percentage** (`v2-completion-reconciliation-2026-07-14.md:14-18, :146`). The v2 *plan* (06-16) contributed ~0 code and was superseded before its Phase 0 cleared. So the completed work is real and large, but it *corroborates* the re-derivation rather than reducing the current gap list — every re-derived row was probed at a HEAD that already contains that work. What the reconstruction *does* change is the *reading* of two rows (G03=BUG-026 policy, G04=BUG-025 inspectability — reclassified in §2.A above), not the count. **§4 stands — not struck.**

| Bucket | Count | Basis |
|---|---|---|
| **Re-derived** (still a gap) | ~236 behavioral rows + ~55 canon-inconsistencies | Code byte-identical; confirmed by a spread of representative live probes across every soundness family (cross-counting-unit false-Proved, BUG-030 cross-unit product, list-literal-default, BUG-031 guard hole, default-expr interior fault, exponent-literal silent-zero, rule-condition-non-boolean, pow/sqrt raw message) — all reproduce exactly as v1 recorded. |
| **Closed-since** (a probe now passes) | **0 behavioral** | No code was fixed; the only "closures" are doc-side (counted as Superseded). |
| **Superseded** (Stage 0 freeze changed the clause) | ~30 rows/bullets (~8 *partial*) | Traces entirely to the 13 freeze spec-changes (freeze commit message is the ledger). Examples: D1 sqrt(integer) now conformant; D2 choice-field doc rewritten (but the *soundness* half re-derives = **G01**); §0.6/§5 status honesty (residual defects re-derive); diagnostic count→164; writable→editable sweep; dayOfWeek→ISO; now() decision-number collision; temporal serialization shapes. "Partial" = doc contradiction closed but a behavioral gap in the same row re-derives. |
| **MISS / new catch** | **1** | The freeze's D4 collection status row states quantifier predicates are "Not yet built" — but they are **live** (probe confirmed). The freeze carried a known-stale status claim into the frozen denominator → **G24** (doc-status fix). |

**Chased-down miss:** G24 is the single miss; verified live (`rule each t in Tags (t.length > 0)` → `success:true, 0 type errors`) and folded into the master list as a WU-DOC-SYNC item.

**Residual re-derivations verified at HEAD:** `catalog-system.md:271,282` still `Constructs (15)`/`Modifiers (29)` (freeze fixed only `Diagnostics (164)`) → part of **G27**; `diagnostic-system.md` FaultCode 13-vs-15 listing likely re-derives (freeze touched `fault-system.md`, not this listing) — verify at build.

**No owner-fork or new-surface item arises from reconciliation itself.** Forks embedded in re-derived rows (BUG-030 unit-granularity policy, `dequeue by H` selector-vs-capture, `reject` StringExpr-vs-literal) re-derive with their v1 owner-fork disposition unchanged — the freeze did not settle them; they are not re-listed here as new decisions.

---

## What this ledger does and does not certify

**Does:** enumerate every conformance gap surfaced by five enumeration angles + an independent spec-claim re-extraction + a structural floor + a v1 reconciliation, each gap probe-verified at HEAD, de-duplicated, dispositioned, and single-owned. Every count-gate is met with named shortfalls.

**Does not:** prove the spec-claim denominator is *totally* enumerated. The completeness here is "two independent readers + a zero-empty-section structural floor agreed," plus the build-verified emission split (144/164 live) and the structurally-guaranteed no-orphan-catalog-member property — an **agreement + lint + floor backstop, not a totality proof.** A claim mis-stated identically by both extractions could still be unmodeled. *(The OS7 caveat — type-doc operator/accessor cells outside the family probes — is now discharged by §5: every cell of the 4 matrices was enumerated and probed.)*

---

## 5. Cell-level matrix enumeration (closes OS7)

**Denominator / grounding:** frozen spec `342e66db` (tag `spec-freeze-2026-07-13`); each of the 4 type-docs verified byte-identical at HEAD `634cf0aa` (`git diff spec-freeze-2026-07-13..HEAD` empty on each file). Every row below carries a `file:line` spec cell + a verbatim live `precept_compile` probe (MCP current). This section CLOSES the OS7 deferral: WF-1 probed the 4 type-doc matrices at *family-representative* level; this pass enumerates and probes every cell. Analysis only — no code, no spec edits, no fork settled.

### 5.1 Per-matrix enumeration counts

| Matrix | Cells enumerated | Cells probed | Compiles | Diverging (new) | Notes |
|---|---|---|---|---|---|
| **Temporal** (`temporal-type-system.md`) | ~240 (8 types × operator/accessor/nav/literal families) | all cell families | 13 | 3 (G34–G36) | +3 message-quality (G54, G55; M2→G12, M4→OS2), invariants hold |
| **Business** (`business-domain-types.md`) | ~249 (17 families) | ~233 (16 field-constraint combos reasoned) | 18 | 9 (G37–G43, G56, and D-BIZ-8→OF6) | +2 ledger updates (OS6→G59; friendly-units→G57); diag→G12/G14/G15/OS2 |
| **Collection** (`collection-types.md`) | ~290 (9 kinds × 6 sub-matrices) | all cell families | 17 | 7 (G44–G48, G53, F-COLL-7→G12) | 5 already-ledgered confirmed live (G05/G24/G31/OF3/OS1) |
| **Primitive** (`primitive-types.md`) | ~205 (operator/lane/literal/function/constraint grids) | ~160 (rest conformant-by-construction: symmetric mirrors + catalog-absence) | 19 | 6 (G49–G52, +P-MINMAX→G41, P-INTERP→G48) | G12/G13 re-confirmed; P-CHOICE-EQSET extends G01 |
| **Total** | **~984 cells** | **~923 probed (67 live compiles)** | | **26 new gaps + OF6** | |

**Bulk result:** the overwhelming majority of cells **conform** — all 4 reports document large conformant families (full ordering/equality operator tables, accessor strict-hierarchy bars, arithmetic composition, literal content-validation, collection action/accessor grids). The 26 divergences are precisely the OS7 long tail: cells that diverge while their family's representative passed in WF-1, or cells never probed at all. No soundness hole where a *barred* operation compiled clean was found in temporal; the soundness divergences below (G45, G48, G51, G52) are *missing rejections* the family probes never reached.

### 5.2 NEW cell-level gaps (not covered by any existing row)

#### implement-against-locked-spec — behavioral / soundness (19)

**G34 — Inline temporal quantity constant rejected in `date`/`time` left-operand contexts** *(temporal F1)*
- **Spec:** `temporal-type-system.md:440` "`date ± '30 days'` → typed constant resolves to `period` in date context"; `:504` `time ± '3 hours'`; `:1138` motivation `set DueDate = CreatedDate + '30 days'`.
- **Probe:** `field D1 as date … rule D1 + '30 days' >= D2` → `PRE0056 "Dates must be written as YYYY-MM-DD. Use '30 days'"`; `time + '30 minutes'` → `PRE0057`. Boundary: `instant/datetime/zoneddatetime + '30 days'` all clean; `date + (period field-ref)` clean — bug is isolated to `date`/`time` left operand with an inline quantity literal (context-resolution treats it as a date/time literal). **Owner:** WU-TEMPORAL-QTY.

**G35 — Legacy IANA abbreviation timezones accepted (missing rejection)** *(temporal F2)*
- **Spec:** `temporal-type-system.md:900-901` "`'EST'` … legacy IANA abbreviation … **Error**. Use `'America/New_York'`"; `'MST'/'CET'/'EET'/'MET'/'WET'/'HST'` same (Decision #7, `:1427`).
- **Probe:** `field A as timezone default 'EST' … 'HST'` → all seven compile clean (only PRE0158). Contrast `'Not/A/Timezone'` and `'Pacific Standard Time'` → PRE0053 rejected. Validation delegates to NodaTime TZDB (which holds these fixed-offset zones); the mandated stricter rejection layer is absent. **Owner:** WU-TZ-VALID.

**G36 — Deprecated-alias timezone warning not emitted** *(temporal F3)*
- **Spec:** `temporal-type-system.md:889` "Deprecated aliases produce warnings."
- **Probe:** `field DEP as timezone default 'US/Eastern'` → only PRE0158; no deprecation diagnostic. Same validation layer as G35. **Owner:** WU-TZ-VALID.

**G37 — `money / money` (different currencies) rejected instead of deriving `exchangerate`** *(business D-BIZ-1; SEVERE)*
- **Spec:** `business-domain-types.md:507` "`money / money` (different currencies) → `exchangerate`"; restated `:996/:1639/:1879`. `:397` cross-currency guard "applies only to additive arithmetic."
- **Probe:** `field X as exchangerate <- A / B` (A USD, B EUR) → `PRE0070 "Cannot combine 'A' (USD) with 'B' (EUR) — different currencies"`. `precept_operations Money` lists `MoneyDivideMoneyCrossCurrency` (qualifier `Different`) — the operation exists but the additive guard pre-empts it, so it is dead-lettered. **Owner:** WU-FX-DERIVE.

**G38 — `exchangerate in 'USD/EUR'` currency-pair qualifier not split** *(business D-BIZ-2; SEVERE)*
- **Spec:** `business-domain-types.md:982` "Fixed | `as exchangerate in 'USD/EUR'`"; `:358/:974/:1403/:1485/:2024`.
- **Probe:** `field FxRate as exchangerate in 'USD/EUR' default '1.08 USD/EUR'` → `PRE0076 "'USD/EUR' is not a recognized ISO 4217 currency code"` + PRE0068. Contrast: bare `exchangerate default '1.08 USD/EUR'` (no `in`) → clean; `price in 'USD/each'` and `price in 'USD' of 'mass'` split correctly. The `in`-qualifier splitter handles price currency/unit but not exchangerate currency/currency. **Owner:** WU-FX-QUAL.

**G39 — Time-denominator compound units entirely undeclarable (breaks the D15 surface)** *(business D-BIZ-3; SEVERE)*
- **Spec:** D15 (`:1821`+), Level B (`:1063-1091`), worked examples `price in 'USD/hours'` (`:227/:933`), `quantity in 'kg/hour'` (`:1069`), `quantity in 'each/day'` (`:1070`).
- **Probe:** `field P1 as price in 'USD/hours'` → `PRE0053`; `quantity in 'kg/hour'/'each/day'/'miles/day'` → `PRE0075 "'kg/hour' is not a valid unit"`. Non-time compounds `each/case`, `kg/each` compile clean — failure is specific to NodaTime time-vocab denominators the compound-unit validator does not admit. Consequence: every D15 cancellation cell (`price × period → money`, `money ÷ duration → price`, etc.) is untestable because its operands cannot be declared. **Owner:** WU-COMPOUND-TIME.

**G40 — `floor`/`ceil`/`truncate` rejected on `money`/`quantity` though D16 lists them inherited** *(business D-BIZ-4)*
- **Spec:** `business-domain-types.md:1859` "`floor`, `ceil`, `truncate` | `money`, `quantity` … For `money`: floors/ceils to integer magnitude"; not in either type's "not supported" table.
- **Probe:** `floor(A)` (A money) → `PRE0018 "Expected a floor value here, but got 'money'"`; same for ceil/truncate on money and all three on quantity. `precept_types functions` confirms `floor/ceil/truncate` are `decimal→integer`/`number→integer` only. Contrast: `abs`, `round(x,2)`, `clamp` DO carry money/quantity signatures and compile clean. **Owner:** WU-FN-OVERLOAD. (If the owner reconsiders whether these should return integer-magnitude money, an owner note — but D16 as written mandates support.)

**G41 — `min(...)`/`max(...)` selection functions unparseable as the RHS of a computed field `<-`** *(business D-BIZ-5 + primitive P-MINMAX-COMPUTED; merged)*
- **Spec:** `business-domain-types.md:1861` `min(A,B)`/`max(A,B)` selection functions; `primitive-types.md:609-610` list them as numeric functions; `<-` takes any expression. Collides with the `min N`/`max N` modifier keywords (`primitive-types.md:214`).
- **Probe (precise boundary from primitive pass):** `field mm as integer <- min(I, I)` → `PRE0009 "Expected expression here, but found 'min'"`; same for money. But `rule min(I,I) > 0` / `set R = min(I,I)` / `ensure max(I,I) > 0` all compile clean — the parser admits `min`/`max` as calls in rule/set/ensure positions but **not** in the computed-field `<-` RHS. (The business pass over-generalized this to "unreachable everywhere"; the primitive pass scoped it to `<-`.) **Owner:** WU-MINMAX-PARSE.

**G42 — Compound counting-unit division-inversion `each ÷ each/case → case` rejected** *(business D-BIZ-6)*
- **Spec:** `business-domain-types.md:1098` "`each / each/case` → `case` … division inverts"; `:1102/:308/:1720`.
- **Probe:** `field r3 as quantity in 'case' <- Eaches / EachPer` → `PRE0137 "explicit counting units must match exactly"`. Multiplication direction works: `EachPer * Cases → each` and `KgEach * Eaches → kg` both `Proved`. Only the division-inversion is blocked. **Doc tension note:** `:397` (PRE0137 applies to "division when both operands are dimensionless with differing unit names") could be read to justify the rejection, but the normative cancellation table `:1096-1102` + 3 worked examples bless the inversion — the operator table wins; flag `:397`-vs-`:1098` for doc reconcile. **Owner:** WU-COUNT-DIV.

**G43 — `unitofmeasure` accepts structural chars `/`, `^`, `.` (only `*` rejected)** *(business D-BIZ-7)*
- **Spec:** `business-domain-types.md:715` "Structural characters (`/`, `*`, `^`, `.`) are rejected in atomic unit positions"; teachable `:756` (`'kg/m'` → InvalidUnitString).
- **Probe:** `'kg/m'`, `'kg^2'`, `'kg.m'` → all clean; only `'kg*m'` → `PRE0053`. Three of four structural chars pass through, defeating the atomic-only guarantee. **Owner:** WU-UOM-ATOMIC.

**G44 — List-literal `default [...]` rejected for `set`/`queue`/`stack`/`log`/`bag` (only `list` works)** *(collection F-COLL-1)*
- **Spec:** `collection-types.md:780` "`default [...]` | `set, queue, stack, log, bag, list`"; `:116` `set of string default []`; `:883` "Empty list as default | Valid."
- **Probe:** `field A as set of string default ["x"]` → `PRE0018 "Expected a set value here, but got 'list'"`; same `default []`, and for queue/stack/log/bag. `list of string default ["x"]` clean. The proof engine *does* compute element obligations (`set of integer min 0 max 100 default [200]` → `IntervalContainment … Unresolved` + PRE0078) — element machinery is wired, but the type-checker rejects the literal for every non-`list` kind. (This is the concrete cell for the ledger `:232` "list-literal-default" probe that carried no G-id.) **Coverage note:** untested — 0 samples use a collection default. **Owner:** WU-LIST-LIT-KIND.

**G45 — Unqualified `money`/`quantity`/`price` `.min`/`.max` compile clean (spec: type error)** *(collection F-COLL-2; soundness)*
- **Spec:** `collection-types.md:541` "Unqualified `money`: `.min`/`.max` are type errors (cross-currency ordering undefined)"; `:543` quantity, `:544` price.
- **Probe:** `field Sm as set of money` + `set OutM = Sm.max` → `success:true, 0 type errors` (only PRE0158); same `set of quantity`.min, `set of price`.max. Contrast currency/dimension/timezone → `PRE0104 … Orderable`. The Orderable trait is applied unconditionally to money/quantity/price; the qualifier-gate (qualified-only orderable) is not enforced on the collection-accessor path → ill-defined cross-currency comparison admitted. **Owner:** WU-COLL-ORDER-QUAL.

**G46 — `set CollectionField = <same-kind collection>` compiles clean (spec: `ScalarOperationOnCollection`)** *(collection F-COLL-3)*
- **Spec:** `collection-types.md:174` "`set SetField = Expr` | `ScalarOperationOnCollection`"; `:885` "you cannot assign a list literal via `set` … rather than bulk replacement"; `:926`.
- **Probe:** `field S as set of string` + `field S2 as set of string` + `set S = S2` → no diagnostic (accepted). `set S = <scalar N>` → `PRE0018` (a TypeMismatch, **not** the spec-named PRE0048). So bulk collection replacement slips through, and `ScalarOperationOnCollection` (PRE0048) never fires for the `set`-on-collection cell family (scalar RHS caught by PRE0018, matching-collection RHS caught by nothing). **Owner:** WU-COLL-SET-BULK.

**G47 — `.at(N)` (log & list) and `remove F at N` (list) over-obligate: index guard does not discharge a redundant non-empty obligation** *(collection F-COLL-4; proof incompleteness)*
- **Spec:** `collection-types.md:337` `.at(N)` proof requirement `N >= 0 and N < F.count`; `:744` `remove F at N` same. No separate non-empty guard is prescribed (`0 <= N < count` entails `count > 0`).
- **Probe:** under guard `when R.i >= 0 and R.i < Ls.count`, `set AtE = Ls.at(R.i)` → `PRE0100` + obligations `[{IndexBounds … Proved},{Numeric "List must be non-empty" Unresolved}]`; `remove Ls at R.i` → `PRE0064` + same split. Adding `and Ls.count > 0` → clean (both Proved). The engine fails to derive `count > 0` from `0 <= N < count`. `insert F Expr at N` is correct (`<=`, no non-empty implied). **Owner:** WU-PROOF-COUNT-ENTAIL.

**G48 — Collection-typed `{expr}` inside string interpolation not rejected (`InvalidInterpolationCoercion` PRE0051 is dead)** *(collection F-COLL-5 + primitive P-INTERP-COLL; merged)*
- **Spec:** `collection-types.md:889-891` "Collections are a type error inside string interpolation … emit `InvalidInterpolationCoercion`"; `primitive-types.md:122` "Collections are a type error."
- **Probe:** `field Tags as set of string` + `set Msg = "Tags are {Tags} here"` → `success:true, 0 type errors`; `field ic as string <- "list is {Tags}"` → clean. Scalar accessor `"{Tags.count}"` correctly clean; other interpolation checks live (`{Undefined}` → PRE0017, `{S-S}` → PRE0018). Source: `DiagnosticCode.cs:103` + factory `Diagnostics.cs:489` exist, **zero emission sites** (grep). **Ledger reconciliation:** contradicts OS2's "`0051 (→ PRE0018)` check runs" — for interpolation (PRE0051's primary domain) **nothing fires**; belongs at implement, not out-of-scope-specificity. **Owner:** WU-INTERP-COLL.

**G49 — Fractional literal fails to resolve into the `number` lane at binary-operator-peer and function-argument positions** *(primitive P-NUMLIT; HIGH)*
- **Spec:** `primitive-types.md:452` "Comparison peer: `Score > 50.0` where `Score` is `number` → `50.0` resolves as `number`"; `:442`.
- **Probe:** `field N as number` + `rule N > 0.0` → `PRE0018 "Expected a number value here, but got 'decimal'"`; identical for `N >= 2.0`, `N == 1.5`, `(N+I) > 0.0`, `number <- N + 0.5`, `min(N, 0.5) > 1.0`. **Scope map:** resolves correctly in assignment-target (`set N = 0.5`), field-constraint (`number min 0.5`), default (`number default 0.5`); breaks in binary-operator-peer and function-arg contexts. `N > 5` (whole) fine; decimal controls (`D > 0.5`) fine — fault is `number`-specific. The spec's own canonical example fails. **Owner:** WU-NUMLIT-LANE.

**G50 — Exponent-form literals not lane-restricted to `number`** *(primitive P-EXPLIT)*
- **Spec:** `primitive-types.md:443` "Exponent (`1.5e2`) | `number` only | Always `number`. Type error if target is `integer` or `decimal`."
- **Probe:** `field ed as decimal default 1.5e2` → clean (accepted); `decimal <- D + 1.5e2` → clean; `rule D == 1.5e2` → clean; `field eie as integer default 1.5e2` → `PRE0018 "…got 'decimal'"` (proving `1.5e2` resolved as decimal, not number). Spec requires a type error against the decimal target; instead accepted. **Owner:** WU-EXPLIT-LANE.

**G51 — `~string` comparison/prefix enforcement is field-reference-scoped, not applied to all `~string`-typed expressions** *(primitive P-CI-SCOPE; soundness — CI guarantee bypassable via event args)*
- **Spec:** `primitive-types.md:146` "`==` or `!=` on **any `~string`-typed expression** (either operand) is a compile error"; `:167` "Event arg declarations: `event Foo(Email as ~string)` is valid … declaring CI comparison intent."
- **Probe:** field-level fires — `field E as ~string` + `rule E == "x"` → PRE0066; `startsWith(E,"a")` → PRE0097. But `event Ev(X as ~string)` + `rule Ev.X == "x"` → **clean** (no PRE0066); `!=`, `startsWith`, `endsWith`, and guard `when Ev.X == "x"` all clean. Also `(if B then E else E) == "x"` clean (if/else-unified `~string` bypasses). **Owner:** WU-CI-EXPR-SCOPE.

**G52 — Non-member choice literal in a `default` value is not validated** *(primitive P-CHOICE-DEFAULT; prevention gap)*
- **Spec:** `primitive-types.md:350` "Every literal assigned to or compared against a choice field must be a member … in both assignment and comparison positions."
- **Probe:** `field C as choice of string("a","b") default "z"` → no PRE0086 (only PRE0158). Contrast: comparison `rule Cok == "z"` → `PRE0086 "'z' is not a declared value"`; set-assign `set C = "q"` → PRE0086. Member check is live for `set` and comparison but absent for `default` — a field is admitted holding a value outside its declared set. Distinct from G10/G11 (choice *element-type* dead codes; this is *member-value* validation via the live PRE0086). **Owner:** WU-CHOICE-DEFAULT.

#### implement-against-locked-spec — diagnostic quality (3)

**G53 — Wrong-collection-kind action cells name the inverse diagnostic code throughout the spec** *(collection F-COLL-6; diagnostic-identity)*
- **Spec (~15 cells):** `collection-types.md:172` "`enqueue SetField Expr` | `CollectionOperationOnScalar`" … `:926` "Applying an action to the wrong collection kind emits `CollectionOperationOnScalar`."
- **Probe + source:** `enqueue/push/append/dequeue/pop/insert/put` on `set S` → seven × `PRE0048` (`ScalarOperationOnCollection`), **not** `CollectionOperationOnScalar` (PRE0047). `TypeChecker.Expressions.Callables.cs:427-437` branches wrong-kind → PRE0048. Positive control: `add` on a scalar field → PRE0047 (live for its real case). Prevention holds; every wrong-kind spec cell names the inverse code; `:926` sentence-1 inverted. **Design smell (flagged, not settled):** *both* code names are misnomers for the wrong-*kind* case (target IS a collection, op IS a collection op) — a distinct "action-not-applicable-to-this-collection-kind" identity is arguably absent. **Owner:** WU-DOC-SYNC (+ naming question flagged).

**G54 — `PRE0056`/`PRE0057` "Use `X`" suggestions echo the invalid input verbatim** *(temporal M1)*
- **Spec:** §0.8 principle 3 (teachable, corrected-form suggestions).
- **Probe:** `"Dates must be written as YYYY-MM-DD. Use '03/15/2026'"`, `"…Use '30 days'"` — the suggestion echoes the rejected input rather than a corrected form. Entangled with G34; distinct from G14's three cited instances. **Owner:** WU-DIAG-MSG.

**G55 — `instant - '2 months'` emits a nonsensical suggestion via instant-literal validation** *(temporal M3)*
- **Probe:** `instant - '2 months'` → `PRE0058 "Instants must end with Z to indicate UTC. Use '2 monthsZ'"` — the `-` path routes the quantity through instant-literal validation; the `+` path gives the cleaner PRE0053. Diagnostic-quality/path-inconsistency. **Owner:** WU-DIAG-MSG.

#### implement-against-locked-spec — doc-drift / hygiene (4)

**G56 — `money / price → quantity` live but absent from the money operator table** *(business D-BIZ-9)*
- Money operator table `business-domain-types.md:499-511` omits `money / price`; but `:334` states "`money / price → quantity`" and `precept_operations Money` lists `MoneyDividePrice`. Probe: `field Q as quantity in 'kg' <- A / Pr` → `success:true, QualifierChain … Proved`. Operation implemented + spec'd at `:334`, missing from the type's own operator table. **Owner:** WU-DOC-SYNC.

**G57 — Spec-example friendly unit names are not valid UCUM atoms** *(business, from D5)*
- Dimension table `:385/:387/:388` and teachables `:471/:705/:959` cite `mi`, `in`, `lb`, `oz`, `gal`. Probe: `mi/in/lb/oz/gal` → PRE0075 invalid; valid: `m/km/ft/cm/mm/g/t/L`. Per D5 ("any valid UCUM expression"), UCUM canonical forms are bracketed (`[mi_i]`, `[lb_av]`, `[gal_us]`) — rejection is correct, **spec examples are wrong** (and `in` additionally collides with the `in` keyword). **Owner:** WU-DOC-SYNC (fix example unit names) — or owner-note if friendly aliases were intended (leans doc-drift given D5).

**G58 — `maxplaces currency.minorUnit` expression form undocumented** *(business, from sample)*
- Sample `insurance-claim-adjudication.precept` uses `maxplaces currency.minorUnit`; probe confirms it compiles clean. Field-constraint table `:1577` documents only `maxplaces N` (numeric literal). Adjacent to G25 (D10 currency-derived-precision) but distinct — the *explicit expression form* is undocumented, not the retired implicit-default. **Owner:** WU-DOC-SYNC.

**G59 — Three malformed-basis codes are now LIVE (former OS6 sub-part → doc-drift)** *(business, OS6 update)*
- OS6 cited three "reserved/unemitted" malformed-basis diagnostics (`business-domain-types.md:1326-1332`, "code assignment lands with implementation"). Probes: duplicate component `period in 'hours + hours'` → `PRE0160`; unknown component `period in 'fortnights'` → `PRE0118`; empty component `period in 'years +'` → `PRE0162`. The spec parenthetical `:1332` is now stale. **Owner:** WU-DOC-SYNC (remove the "codes land with implementation" note; codes are PRE0160/0118/0162). *(OS6 narrowed to just `MissingOrderingKey` PRE0151.)*

### 5.3 Cells de-duplicated against existing rows (confirmed live, NOT re-filed)

- **G01 extended** *(primitive P-CHOICE-EQSET)*: cross-set `==`/`!=` between disjoint choice sets (`choice("a","b") == choice("x","y")`) type-checks clean — the distinct equality cell G01's ordering-only probe never reached. Same root path (`Operations.cs` choice-compare entries carry no value-set check). **Covered by G01 as a family**; noted here as the equality-cell extension + shares G01 Note N1.
- **G05** confirmed live (lookup `for K` fail-open): `set Out = Lk for E.k` unguarded → `success:true, proofObligations:[]`; adding `when Lk contains E.k` byte-identical.
- **G12** (PRE0018 template misuse) — family far broader than the two cited instances: confirmed across `-exchangerate` ("got 'Negate'"), `exchangerate + exchangerate` / `< exchangerate` ("got 'exchange rate'"), `abs(exchangerate)`, `floor/ceil/truncate(price|money|quantity)`, `price*price`, `money*money`, all `<` on currency/uom/dimension; plus **collection F-COLL-7** `contains` on a scalar field → `PRE0018 "Expected a string value here, but got 'string'"`. Soundness holds everywhere. Note: G12 should be scoped to the full operator × type product, not the examples cited.
- **G13** re-confirmed: `pow(I,I)` → PRE0084 hardcodes "sqrt(...)".
- **G14** confirmed: `quantity == quantity` (different dimensions) → PRE0114 labels dimension as "Unit"; temporal M4 (`PRE0104` generic message vs spec `:1745` specific) is a message-specificity instance → **OS2 family**.
- **G15** confirmed: PRE0070+PRE0114 (money cross-currency) and PRE0071+PRE0114 (quantity cross-dimension) duplicate emission.
- **G24** confirmed + strengthened: `each`/`any`/`no` compile; `queue of T by P` two-field projection type-checks; doc `:10`/`:802` "Not yet built" both stale.
- **G31** confirmed: `append CL E.claim by E.sev` unguarded → PRE0101 (`KeyPresence`), not the spec-stated `UnguardedCollectionAccess` (implementation correct, spec text wrong side); sub-note: message placeholder reads `'element'` not the actual expr.
- **OF3** — `field NE as set of string notempty` → clean; adds evidence for the "routes to element position, valid" arm; still owner-forked.
- **OS1/G14** — out-of-range element/default → PRE0078 "exceeded representable range" for a declared `[0,100]` bound.
- **OS2** — content-validation for bad currency/unit/dimension surfaces generic PRE0053 not the specific `InvalidCurrencyCode`/`InvalidUnitString` codes (`:2068-2070`); `in`/`of` on wrong types via generic PRE0009/PRE0052 cascades; `money * number` via generic PRE0018 not the teachable "requires exact decimal scalars." Soundness holds; specificity only.

### 5.4 OS7 disposition

**CLOSED.** Every cell family of all 4 type-doc matrices (~984 cells, ~923 live-probed across 67 `precept_compile` calls) was enumerated and probed. No sub-matrix was left at family-representative level. The ~61 unprobed cells are conformant-by-construction (symmetric operator mirrors verified one-sided; lane-blocks confirmed by catalog-absence; business field-constraint stacking combos reasoned from individual-constraint probes) — none indicated divergence. The long tail yielded 26 new implement gaps (G34–G59) + 1 owner-fork (OF6), none previously in the ledger.
