# v2 Completion Reconciliation — what v2 finished vs. the fresh gap ledger

**Status:** Draft analysis — 2026-07-14. Analysis only; no code, no spec edited.
**Arbiter:** live `precept_compile` (MCP confirmed current — `precept_ping` → ok; every probe below run this pass).
**Inputs:** the v2 plan `compiler-readiness-plan-2026-06-16.md` (+ companions); every doc under `docs/Working/Archive/` and `docs/Working/Superseded/`; `docs/Working/bugs.md` (§Fixed + §Active); the fresh gap ledger `docs/Working/gap-ledger-2026-07-13.md`.
**Arms the owner; settles nothing.** A doc's "completed/Promoted/SHIPPED" claim is a *lead*; the probe is truth.

---

## 0. The load-bearing framing correction (read first)

"The v2 effort" splits into two things that must not be conflated:

1. **The v2 *plan* (`compiler-readiness-plan-2026-06-16.md`) delivered essentially no code.** It is a forward-looking plan: Phase 0 is marked **CURRENT**, Phase 1 **NEXT**, its own status line reads *"Draft plan … promote to `Locked` only once the Phase-0 design gates clear"* (`compiler-readiness-plan-2026-06-16.md:3`), and it was superseded by the v3 proof-engine plan (07-12) before Phase 0 cleared. Phases 0–7 of the v2 plan were **not executed**. The gap ledger independently confirms this: *"the ONLY commit touching `src/Precept` since the v1 audit is the freeze itself, whose only `.cs` change is one hover/label string … No pipeline logic changed"* (`gap-ledger-2026-07-13.md:228`).

2. **The real completed body of work is the pre-plan build-out** — the cluster of Locked/Promoted designs from ~2026-05-23 to 2026-06-05 that the v2 plan *cites* as "already shipped" (2c-i relational narrowing, BUG-014/015/016, BUG-017/018/019, BUG-027, D9, etc.). This is what "v2 completed" actually denotes, and it is what constitutes the "~75–80% built" baseline. Every one of these landed **before** the v1 audit (06-11), so it is already baked into the gap ledger's HEAD probes.

**Consequence for the percentage question:** the documented v2 completion does **not move** the ~75–80% figure up or down — it is already inside the probe baseline. What it changes is the *reading* of the residual 20–25%: several ledger items that read as fresh "soundness holes" are in fact **known, deliberately-deferred gaps** the completed designs scoped out on the record (G03=BUG-026, G04=BUG-025), and the completed register **explains** what the 75–80% is made of.

---

## 1. The v2 completed-work register

Every row: attesting doc (path) → the doc's own completion marker → what it delivered → canonical doc it promoted to. Grouped. Probe-confirmed rows are flagged **[PROBE ✓]** with the verbatim result in §2 where they arbitrate a ledger item.

### 1.A Proof engine — soundness & discharge machinery (the bulk of the build-out)

| # | Delivered capability | Attesting doc | Marker | Promoted to |
|---|---|---|---|---|
| 1 | Interval containment (Strategy 7/8) + overflow prevention | `Archive/interval-proof-engine-design.md` | "✅ Approved — Shane sign-off; impl in progress" + `Archive/overflow-prevention-design-analysis.md` "SUPERSEDED by implementation" | `proof-engine.md § Strategy 7/8` |
| 2 | **Relational rules & bounds core** — FlowNarrowing (Strategy 4), single-pass octagon-closure-minus-widening, field-to-field discharge | `Archive/relational-rules-and-bounds-design-2026-06-02.md` + `Archive/relational-narrowing-core-design-2026-06-03.md` | **"Promoted to: proof-engine.md §Strategy 4 + spec §0.6 — 2026-06-04"**; core Locked 06-04 | `proof-engine.md § Strategy 4`, `precept-language-spec.md §0.6 item 2` **[PROBE ✓]** |
| 3 | Relational contradiction-reject (dependent fault-prone op rejects when a rule contradicts subject bounds) | `Archive/relational-contradiction-reject-2026-06-04.md` (Locked 06-04) + `bugs.md` BUG-024 | "✅ Fixed 2026-06-04 (Slice 2c-i)" | `proof-engine.md § Satisfiability` **[PROBE ✓]** |
| 4 | Relational subject-resolution discharge seam ((X−Y) divisor resolves through the relation) | `Archive/relational-subject-resolution-discharge-2026-06-04.md` | Locked 06-04 | `proof-engine.md § Strategy 4` |
| 5 | Guarded-rule DROP filter (BUG-023 — guarded magnitude/sign rule no longer leaks as unconditional fact) | `bugs.md` BUG-023 | "✅ Fixed 2026-06-04 (Slice 2c-i)" | `proof-engine.md` |
| 6 | **Satisfiability cluster** — UnsatisfiableGuard/ContradictoryRule/VacuousRule/TautologicalGuard/UnsatisfiableRule (PRE0153–0159) + cross-row interval composition + reachability-gated FieldNeverSet | `Archive/proof-engine-satisfiability-cluster-design.md` + `Archive/satisfiability-attribution-and-reachability-design.md` | "Promoted 2026-05-27 — commits a6c35e36, ffb7288b, c16be77b, 42cee783; 11599944" | `proof-engine.md § Pass 1.5`, `diagnostic-system.md PRE0153–0159`, `precept-language-spec.md §0.6` (5 of 13 obligations → Implemented) **[PROBE ✓]** |
| 7 | Count containment (Strategy 10, PRE0136) — both grow/shrink directions, catalog-derived deltas | `Archive/count-bound-discharge-semantics-2026-06-03.md` + `bugs.md` BUG-018 | "Promoted 2026-06-04, commit c27a382b"; BUG-018 "✅ Fixed" | `proof-engine.md § Strategy 10`, `diagnostic-system.md PRE0136`, `collection-types.md` |
| 8 | Length containment (Strategy 9, PRE0135) — non-literal RHS gate removed | `bugs.md` BUG-019 | "✅ Fixed 2026-06-02" | `proof-engine.md § Strategy 9` |
| 9 | BUG-017 computed-field unbounded-result no longer silently skipped; family partition | `Archive/phase8-value-level-obligation-ownership-2026-06-01.md` + `bugs.md` BUG-017 | "Promoted; ship commits ecce5d51, 9820b8c1"; BUG-017 "✅ Fixed 2026-06-02 (9820b8c1)" | `proof-engine.md § Computed-field bound containment` |
| 10 | Reassignment / sequential-staleness invalidation (BUG-014/015/016; `ReassignedBefore`, `ActionMeta.Effect`, Strategy 11 CollectionGrowth) | `bugs.md` BUG-014/015/016 | all "✅ Fixed 2026-05-29" | `proof-engine.md § Strategy 3/11`, `precept-language-spec.md §0.6 item 7` |
| 11 | Discrete-equality narrowing for `choice`/`==` guards (BIZ-08) | `bugs.md` F-LANG-BIZ-08 | "✅ Fixed Phase 5 W-D, commit 08fae0ef" | `business-domain-types.md § Discrete Equality Narrowing` |
| 12 | Dimensional product (money÷price→quantity, quantity×quantity; Strategy 6; PRE0157/PRE0137) | `Archive/biz-operator-extensions-design.md` + `Archive/qualifier-and-dimensionless-product-design.md` | "Promoted 2026-05-27, commits 5d945711, e71e09d2, 08fae0ef, fdf33095"; "Promoted 2026-05-28, commit 11599944" | `proof-engine.md § Strategy 6`, `business-domain-types.md § Dimensional Cancellation`, `diagnostic-system.md PRE0157/PRE0137` |
| 13 | D9 qualifier narrowing (S1–S4 + basis-cancellation) | `Archive/d9-qualifier-narrowing-design.md` (+ analysis/plan) | "Implemented + promoted 2026-05-29" (S1 4e5f837c … S4 5949de4b; W-C `.basis` fd147dc2) | `business-domain-types.md § D9` |
| 14 | Assignment-qualifier discharge (Strategy 5, PRE0141, proof-stage re-stage) | `Archive/assignment-qualifier-discharge-placement.md` + `Archive/frank-qualifier-deferred-scoping.md` | "Promoted 2026-05-31 — shipped (`ProofEngine.QualifierNarrowing.cs`)" | `proof-engine.md § Strategy 5`, `diagnostic-system.md PRE0141` |
| 15 | Rule-vs-default fold (BUG-027, PRE0164, shared `BuildDefaultEnvironment`) | `bugs.md` BUG-027 | "✅ Fixed 2026-06-05" (commit 210229ff per plan) | `proof-engine.md § Satisfiability` |
| 16 | Bounds-qualifier rules (PRE0133/PRE0134) + quantity UCUM-base normalization | `Archive/frank-bounds-qualifier-audit.md` + `Archive/quantity-normalization-design.md` | both "Promoted 2026-05-24" | `business-domain-types.md § Bounds qualification rules`, `proof-engine.md § Normalization boundary` |
| 17 | Frank obligation-generation contract (emit-for-all-declared-constraints, metadata-derived) | `Archive/frank-catalog-obligation-audit.md` | "Promoted 2026-05-24" | `proof-engine.md § Obligation Generation Contract` |
| 18 | Composite period basis (BIZ-07 / D4 amendment) | `Archive/lifecycle-review-f-lang-biz-07-...md` | "fully shipped (Phase 6), commit 03dfc4da" | `business-domain-types.md § D4` |

### 1.B Language / type surface

| # | Delivered | Attesting doc | Marker | Promoted to |
|---|---|---|---|---|
| 19 | Field-never-set (PRE0150) + `writable→editable` keyword retirement | `Archive/field-never-set-diagnostic.md` | "Promoted 2026-05-27, commits 0d61f792…2ac416b5; reachability 11599944" | `precept-language-spec.md §2.2`, `catalog-system.md § Modifier Catalog`, `graph-analyzer.md §6.7`, `diagnostic-system.md PRE0150` |
| 20 | Choice inner-type + `ordered` propagation (`.min`/`.max`, ordinal compare on ordered-choice element) | `Archive/choice-inner-and-ordered-propagation-design.md` | Locked 2026-05-26 (D-1…D-6 ratified) | `collection-types.md`, `SemanticIndex.cs TypedElementType` |
| 21 | Ordinal compare ordered-choice field vs choice-literal (BUG-012) | `bugs.md` BUG-012 | (§Fixed) | `proof-engine.md` |
| 22 | `clear` / `notempty` lifted on `lookup` (F-LANG-COLL-13) | `Archive/clear-on-lookup-design.md` + `bugs.md` F-LANG-COLL-13 | "✅ Fixed Phase 4 W-J 2026-05-26" | `collection-types.md`, `precept-language-spec.md` |
| 23 | Collection element value-modifiers + modifier-axis-overlap analyzer (PRECEPT0031) | `Archive/modifier-name-axis-overlap-2026-06-03.md` | "Promoted 2026-06-04, commits c3209ad2, eeb9a9c6" | `precept-language-spec.md §2.3/2.4`, `collection-types.md`, `type-checker.md`, `proof-engine.md` |
| 24 | Bare-identifier arg/field shadowing → dotted-only event args (PRE0163) | `Archive/bare-identifier-arg-field-shadowing-2026-06-02.md` | "Resolved Option C; canon synced ship commit d6ee8778" | `precept-language-spec.md §3.4/3.5`, `type-checker.md Decision 20`, `diagnostic-system.md PRE0163` |
| 25 | Typed constants + proof coverage (Parts A–H) | `Archive/typed-constants-and-proof-coverage-plan.md` | "Promoted 2026-05-24; Parts A–G ✅ Done" | `catalog-system.md`, LS §7.3 |
| 26 | Currency-derived maxplaces (F-LANG-BIZ-10) | `Archive/f-lang-biz-10-currency-derived-maxplaces.md` | Locked 2026-05-26 (W-H shipped) | `business-domain-types.md` |

### 1.C Emission / diagnostic architecture & tooling

| # | Delivered | Attesting doc | Marker | Promoted to |
|---|---|---|---|---|
| 27 | Diagnostic-enforcement mission — 49 gaps closed, PRECEPT0027–0030 analyzers, 30+ codes wired. **Explicit residual: PRE0099 KeyPresence + PRE0101 staged "but blocked on Lookup type expansion."** | `Archive/diagnostic-enforcement.md` + `-implementation-notes.md` | "Promoted 2026-05-24; mission closed 2026-05-14" | `catalog-system.md § Roslyn Enforcement Layer` |
| 28 | Emission-ownership (Stage-2) + code-mediation standardization + DiagnosticStage partition | `Archive/phase8-slice2-code-mediation-2026-06-01.md` + `Superseded/phase8-slice3-emission-ownership-2026-05-31.md` | "Promoted; ship commit 59fe2666"; slice3 Locked 06-01 | `diagnostic-system.md § Emission shapes / DiagnosticStage` |
| 29 | Catalog-compliance / Roslyn enforcement taxonomy (Pattern A–H) | `Archive/catalog-compliance-audit.md` | "Promoted 2026-05-24" | `catalog-system.md § Architectural Violation Patterns` |
| 30 | Language server (semantic tokens, hover, interval hover, typed-literal completions, syntax coloring) + MCP DTO-free | `Archive/{language-server-implementation-plan, hover-design, interval-hover-design, elaine-typed-literal…, kramer-typed-literal…, syntax-coloring-fix-design, mcp-dto-free-design}.md` | all "Promoted 2026-05-24" | `tooling/language-server.md`, `tooling/mcp.md`, `catalog-system.md` |

**Breadth confirmed.** The completed v2 corpus spans the relational/octagon proof core, the satisfiability cluster, count/length/interval containment, reassignment-staleness, dimensional & qualifier machinery, choice/collection modifier surface, emission-ownership, and the whole tooling layer — a large, real body of shipped work, consistent with the "~75–80% built" characterization.

---

## 2. Reconciliation against the gap ledger

For each ledger gap: does a completed-v2 doc claim that area delivered? Three outcomes. **The probe is the arbiter.**

### 2.1 The two headline reclassifications (doc record recasts the ledger's severity)

These are the load-bearing findings. Both gaps are **REAL** (keep them), but the completed-v2 record establishes they are **known, deliberately-deferred non-soundness gaps** — not the fresh "soundness holes" the ledger's §2.A framing implies.

**G04 — relational field-to-field contradiction "not detected at all"** → this is **`bugs.md` BUG-025**, verbatim: *"Relational (field-vs-field) contradictory rules are invisible to the satisfiability scan — no warning, no rule attribution (**inspectability gap, NOT a soundness hole**)"* (`bugs.md:121`). Its root cause and deferral are on the record: *"Named as out-of-scope in `relational-contradiction-reject-2026-06-04.md` § Scope / Open Questions … because it is not required to close the BUG-024 over-prove"* (`bugs.md:129`); *"Workaround used: none needed — soundness is intact: BUG-024's contradiction-reject guard makes any dependent fault-prone op reject regardless of this gap. This is purely a missing author-facing diagnostic"* (`bugs.md:130`).
- **Arbiter — soundness IS intact (BUG-024 live at HEAD).** Probe `field X as integer min 0 max 5` / `rule X >= 10` / `field R as integer <- 100 / X` → obligation `Numeric "Divisor must be non-zero" → **Unresolved**`; `PRE0083 (error) "Division is unsafe: 'X' can be zero"`; plus `PRE0159 (warning) "…unsatisfiable under the declared bounds on field 'X'"`. The contradiction does **not** over-prove the dependent op — it rejects at Error. BUG-024 is shipped and live.
- **Arbiter — the missing warning reproduces.** Probe `field A as integer` / `field B as integer` / `rule A > B` / `rule A < B` → **no PRE0155**; only `PRE0164` (defaults A=0,B=0 violate) + parser-recovery `PRE0009` noise. Contrast the same-field-vs-literal case (probe `rule N > 100` / `rule N < 10`) → `PRE0155 (warning) "…contradicts an earlier rule on field 'N'"`. Field-op-field contradiction gets no attribution; field-op-literal does.
- **Disposition: doc-claims-done-but-gap-persists → corrected to KNOWN-DEFERRED-INSPECTABILITY.** The relational-contradiction-reject design shipped its *in-scope* half (dependent-op reject) and explicitly deferred the *out-of-scope* half (satisfiability-scan warning attribution). The ledger re-found the deferred half and mis-filed it under "behavioral / soundness (§2.A)." **Keep G04, but reclassify: it is a missing diagnostic (BUG-025 class), not a soundness hole.** Ledger over-states severity; the completion record under-claims nothing.

**G03 — uninhabitable definition accepted at Warning, not Error** → this is **`bugs.md` BUG-026**, verbatim: *"Should a provably-unsatisfiable rule or guard block compilation (Error), or only warn? — **current behavior is Warning-only, genuinely unspecified in canon**"* (`bugs.md:105`); *"the Warning severity was defaulted by analogy … with no warn-vs-error deliberation … So the current Warning is drift, not a settled decision"* (`bugs.md:113`).
- **Arbiter.** Probe `rule N > 100` / `rule N < 10` → `PRE0155 severity: "warning"` (detection is live; the satisfiability cluster shipped it). Severity is Warning, spec §0.6 item 7 wants reject/Error.
- **Disposition: not-in-v2-scope (known-open policy).** Detection = completed v2 (satisfiability cluster, register #6). The Warning→Error flip is an *unsettled policy question* the completed work explicitly parked as BUG-026 — and is exactly the v3 Stage-0 structural-severity slice. **Keep G03; it is a real severity gap, correctly an open policy decision, not a fresh discovery.**

*(Side-observation, both probes: `PRE0155`/`PRE0159` render the raw `TypedBinaryOp { … }` AST dump instead of readable rule text. That is `bugs.md` BUG-029 (diagnostic-quality), Active — a real defect the ledger's message-defect family (G12–G14) does not explicitly list. Noted, not a new gap claim.)*

### 2.2 confirmed-done (feature works AND probe confirms) — false-positive check

The ledger is **probe-based**, so it does not carry behavioral false-positives. The one place a completed feature could look broken is a doc-status claim; the ledger handled it correctly:

**G24 — collection-types.md says quantifier predicates "Not yet built."** Completed status is the reverse.
- **Arbiter.** Probe `field Tags as set of string` / `rule each t in Tags (t.length > 0)` → `0 type errors` (parses and type-checks clean). Quantifiers are **live**.
- **Disposition: confirmed-done FEATURE, real DOC-DRIFT gap.** The ledger correctly filed G24 as doc-sync (the doc under-claims a shipped feature). **Not a false-positive to strike — keep as WU-DOC-SYNC.** This is the ledger doing exactly the right thing.

**Register cross-check for a false-positive: relational narrowing.** The completed register's flagship (#2) could be a false-positive risk if the ledger claimed relational discharge was broken — it does not.
- **Arbiter.** Probe `field OnHand as integer min 0` / `field Reserved as integer min 0` / `rule OnHand > Reserved` / `field R as integer <- 100 / (OnHand - Reserved)` → obligation `Numeric "Divisor must be non-zero" strategy:"FlowNarrowing" → **Proved**`. The rule narrows `(OnHand−Reserved) > 0` and discharges the divisor. Relational core is live and the ledger correctly does **not** flag it. No false-positive.

**Net: zero behavioral false-positives to strike from the ledger.** The completed register corroborates the ledger's accuracy rather than deflating it.

### 2.3 doc-claims-done-but-gap-persists (completion was partial/buggy)

**G01 — ordered-choice comparison skips the subsequence / same-element-type check.** The choice-ordered design (register #20, Locked 2026-05-26) shipped `ordered` propagation and `.min`/`.max`/ordinal compare — but its scope was *element ordering*, not the *comparison-operand subsequence check* the spec mandates (`precept-language-spec.md:1398`).
- **Arbiter.** Probe `field A as choice of string("Low","Med","High") ordered` / `field B as choice of string("Red","Green","Blue") ordered` / `rule A < B` (disjoint sets) → the **only** obligation is `Modifier "Both choice operands must be declared ordered" → Proved`. No subsequence obligation, no same-element-type check, no type error on comparing disjoint value sets.
- **Disposition: doc-claims-partial → gap REAL.** The ordered-comparison *routing* shipped; the *subsequence/element-type soundness check* was never built. Keep G01 (soundness). The completed design did not over-claim this specific check (it wasn't in its scope) — the over-claim, if any, is the spec asserting a check the impl never got. **Keep as WU-CHOICE-CMP.**

### 2.4 not-in-v2-scope (v2 never claimed to complete this area) — genuinely open

These map cleanly onto **unbuilt phases of the v2 plan** and/or **Active (unfixed) bugs**. v2 planned them; v2 never executed them.

| Ledger gap | v2 status | Arbiter / evidence |
|---|---|---|
| **G05** KeyPresence obligation never created for `lookup for K` reads | Register #27 *explicitly* names PRE0099 KeyPresence "staged but **blocked on Lookup type expansion**" (`Archive/diagnostic-enforcement-implementation-notes.md`); v2 plan Slice 6.1 lists `KeyPresenceSafety` still on the allow-list to wire | Probe `on Read -> set outv = limits for Read.k` (unguarded) → `proofObligations:[]`, no KeyPresence diagnostic. Reproduces. **Real; acknowledged-blocked, never claimed done.** |
| **G06** Guard positions never enrolled (6 positions) | = `bugs.md` **BUG-031 (Active)**; = v2 plan Phase 2 Slice 2.2 "close the 10 fail-open positions" — unbuilt | Ledger probe stands; plan itself cites BUG-031 as live (`…-06-16.md` §5). Not-in-v2-scope. |
| **G07** Member-call arguments never enrolled | = `bugs.md` **BUG-032 (Active)**; v2 Phase 2 Slice 2.2 | Ledger probe stands. Not-in-v2-scope. |
| **G08 / G09** Field / event-arg default-expression sub-tree never walked | v2 Phase 2 Slice 2.2 (same walk-totality fix) | Ledger probes stand. Not-in-v2-scope. |
| **G02** Computed `<-` numeric-lane narrowing not enforced | Computed-field fold is `Superseded/computed-field-default-folding-2026-06-05.md` (Locked, **phase-target Phase 7/Slice 2c-ii — never built**); v2 plan Slice 5.2 | Ledger probe stands. Not-in-v2-scope. |
| **G10 / G11** dead `PRE0088` / `PRE0090` choice diagnostics | v2 Phase 4 Slice 4.4 "Choice family completeness" — unbuilt | Ledger probes stand. Not-in-v2-scope. |
| **G12–G17** diagnostic-quality / message / dedup / emission-reachability-test | v2 Phase 4 Slice 4.3 + Phase 6 Slices 6.1–6.7 — unbuilt (BUG-029 for the AST-dump messages is Active) | Not-in-v2-scope. |
| **G18–G33** doc-drift / hygiene (17) | v2 Phase 1 Slice 1.5 + per-phase doc-touch — unbuilt; these are canon-vs-code / canon-vs-canon drift, no behavior change | Ledger's doc-vs-doc extraction stands. Not-in-v2-scope. |
| **OF1–OF5, OS1–OS7** owner-forks / out-of-scope | v2 carried the ancestors of several (OF2 PRE0022 = plan Slice 4.3; OF5 integer-overflow = plan D1 PARKED family) as open gates | Not-in-v2-scope (open by design). |

None of these was ever marked completed by a v2 doc. Several are literally the unbuilt slices of the v2 plan, or Active (unfixed) entries in `bugs.md`. They are genuinely open.

---

## 3. Net finding

**Of the gap ledger's 45 dispositioned items:**

- **(a) confirmed-done false-positives to strike: 0.** The ledger is probe-based and carries no behavioral false-positives. G24 (quantifiers "not built") is a confirmed-done *feature* but the ledger correctly filed it as a *doc-drift* gap, not as a broken feature — so it stays. Relational narrowing, the largest completed deliverable, was correctly never flagged as broken.

- **(b) real gaps despite (or adjacent to) a v2 "done" claim: 3, of which 2 are severity/classification reclassifications, not strikes.**
  - **G01** (ordered-choice subsequence) — the ordered-comparison *routing* shipped; the *subsequence/element-type check* did not. Genuinely partial. Keep as soundness.
  - **G04** (relational contradiction) — **reclassify from "soundness (§2.A)" to known-deferred inspectability (=BUG-025)**; soundness is intact via the shipped BUG-024 (probe-confirmed: dependent divisor rejects at Error). Keep, corrected severity.
  - **G03** (uninhabitable Warning-not-Error) — **detection shipped (satisfiability cluster); the Warning→Error flip is the known-open BUG-026 policy question**, now the v3 structural-severity slice. Keep as a severity/policy gap, not a fresh soundness discovery.

- **(c) genuinely-open, never-in-v2-scope: 42** (the remaining behavioral holes G02/G05–G11, the diagnostic-quality set G12–G17, the doc-drift set G18–G33, and OF1–5 / OS1–7). These map onto the **unbuilt phases of the v2 plan** (Phase 2 walk-totality, Phase 4 choice/datatype, Phase 6 diagnostics) and **Active bugs** (BUG-031, BUG-032, BUG-025, BUG-026, BUG-028, BUG-029). v2 planned them and never executed them.

**Does documented v2 completion change the "~75–80% built" picture?** **Confirmed, not moved.** The completed v2 work all landed *before* the v1 audit (06-11), so it is already inside the gap ledger's HEAD probes — it cannot shift the percentage. What it changes is the *reading* of the residual:

1. **The v2 *plan* (06-16) contributed ~0 code** — it was a Draft forward plan superseded before Phase 0 cleared. The "built" percentage is the pre-plan May–June build-out, not the plan.
2. **Two ledger "soundness" items are actually known, on-the-record deferrals** (G03=BUG-026 policy, G04=BUG-025 inspectability with soundness proven intact). The prevention guarantee is in better shape than a first read of §2.A suggests: the contradiction cases *reject the dependent fault-prone op at Error* (probe-confirmed) — what's missing is the author-facing *warning*, not the *prevention*.
3. **The genuinely-open residual is well-characterized and largely already-ticketed** — the ledger's 42 not-in-v2-scope items are the unbuilt v2-plan slices plus Active bugs, not surprises.

**Margin note (not a ledger gap, flagged for completeness):** `bugs.md` BUG-020 (field-reference modifier bound `min Floor` accepted but inert; enforcement half unbuilt — v2 plan Slice 5.1 flagship) does not appear as a distinct row in the gap ledger's 45. If real at HEAD it is a ledger *omission* (under-count), the opposite of a false-positive; out of scope for this reconciliation but worth a probe before the ledger is treated as complete.

---

## What this doc certifies

**Does:** map the completed v2 build-out (register §1) and arbitrate each gap-ledger item against it with live probes (§2), yielding the strike/keep/reclassify verdicts and net finding (§3).

**Does not:** re-audit the ledger's own completeness, re-probe every one of the 45 items (the ledger already probed them at HEAD this-pass-adjacent; I re-probed the contested/headline ones and confirmed the MCP reproduces them), or settle BUG-025/BUG-026 severity — those remain owner calls. Every "completed" claim herein is a doc lead confirmed (or corrected) by a verbatim probe; where I did not re-probe, I say so.
