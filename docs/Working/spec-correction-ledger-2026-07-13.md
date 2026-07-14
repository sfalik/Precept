# Spec Correction Ledger — 2026-07-13

> **Status:** Draft — 2026-07-13. Review artifact preceding the spec freeze. **Nothing here is applied.** Mechanical fixes await an owner skim-and-go; owner decisions await a ruling.
>
> **Scope:** A *bounded* drift-correction pass over the canonical spec surface — finding what is WRONG or INCONSISTENT so the spec can be corrected and frozen as the reference the readiness effort measures against. This is **not** the per-clause conformance audit (a later stage). All findings grounded against committed HEAD `5deb9b20` (`5deb9b209724f7210c261c79738c9b0eac8194cc`). Assembled from three lenses: scope-honesty, internal-consistency, and compiler/runtime boundary.

---

## 1. Summary counts

| Class | Count | What it means for the freeze |
|---|---:|---|
| **Mechanical drift-fixes** | 13 | One side is plainly stale. Exact edit given. Apply on owner's say-so; owner skims to confirm. |
| **Owner forks** | 3 | Two defensible readings. Not settled here. Owner rules, then a sweep aligns the loser's docs. |
| **Scope-calls** | 1 | A specced feature partly unbuilt — mark-honestly wording choice. Owner picks the wording. |
| **New-surface parks** | 0 | No finding would add or settle a keyword/type/operator/modifier/construct. Nothing routed to `/design`. |

Owner-action items total: **4** (3 forks + 1 scope-call). Mechanical items the owner only skims: **13**.

**De-duplication note:** three findings were reported by more than one lens and are merged into single rows below — the §5 "complete" overclaim (scope + boundary lenses), the §0.6 status-table staleness (all three lenses), and the collection-types status tension (scope + boundary lenses). Where lenses confirmed different sub-parts, the stronger evidence is folded in.

---

## 2. Mechanical drift-fixes (proposed, not applied)

Each row: one side is stale, the current/canonical side is cited, and the exact edit is given. Several are AI-co-authored sweep misses (a decision landed; a downstream table/list/example was not swept) — cleanup, not decisions.

| # | Location (file:line, HEAD) | What's wrong | Proposed exact edit |
|---|---|---|---|
| M1 | `docs/language/precept-language-spec.md:10` and `:2133` | Top Status says "§5 Proof Engine complete"; §5 pointer says "> **Status:** Implemented." The owning stage doc `docs/compiler/proof-engine.md:8` says the prove-or-reject MVP + Slice-0 fail-open soundness fixes are "**designed, not yet implemented**." Spec claims a completeness the stage doc denies; live fail-open holes exist at HEAD. | `:10` — change `§5 Proof Engine complete` → `§5 Proof Engine — base engine implemented; prove-or-reject MVP + fail-open soundness fixes designed, not yet implemented (see docs/compiler/proof-engine.md)`. `:2133` — change `> **Status:** Implemented.` → `> **Status:** Base engine implemented; prove-or-reject MVP and fail-open soundness fixes designed, not yet implemented — see docs/compiler/proof-engine.md.` Wording mirrors the already-honest `proof-engine.md:8`. Deeper fail-open enumeration is the soundness lens's job, not this edit. |
| M2 | `docs/language/precept-language-spec.md:237-247` (the §0.6 "Implementation status" subsection) plus `:331`, `:1065` | The table marks obligations 7 (contradictory rule), 8 (vacuous), 9 (dead guard PRE0082), 10 (tautological guard), 12 as "Specification-only" and says "the runtime gate does not depend on them." **Item 7 is confirmed live at HEAD** — `ProofEngine.Satisfiability.cs:326-345` emits PRE0155/PRE0159; registered in `Diagnostics.cs:770-814`; MCP probe fired PRE0159/PRE0164 on an unsatisfiable rule set; `diagnostic-system.md:417` treats them as shipped. §0.6 directly contradicts `diagnostic-system.md` and the same section's own prose `:209` ("rejects the definition"). The subsection also carries transient scaffolding ("As of 2026-05-24", "(Phase 5)" columns, `F-LANG-SPEC-*` finding-IDs) forbidden in normative prose, and a dead D2 "runtime gate does not depend on them" clause (the D2 band-split framing the ratified ledger dissolved). | Rewrite the subsection to (a) mark obligations 7-10 implemented and emitting; (b) drop the "As of 2026-05-24" stamp, the "(Phase 5)" columns, and the `F-LANG-SPEC-*` finding-IDs, citing stable `diagnostic-system.md` / `proof-engine.md` instead; (c) strike "the runtime gate does not depend on them, and" (dead D2 framing). **Verify per-row before editing:** only item 7 confirmed live this pass — re-check items 8, 9, 10, and 12 individually (item 12 "sharpened reachability/routing diagnostics" had no direct emission evidence; keep only if genuinely unbuilt). Lower-priority same-rule finding-ID cites at `:331`/`:1065` (`F-LANG-GRAPH-04`) go in the same sweep. **Not settled here:** PRE0155/PRE0159 emit at **Warning**, but §0.6 body `:209` requires rejection and the ratified structural-severity ruling says Error — that Warning→Error flip is decided-but-unapplied readiness-plan work, not a spec edit. |
| M3 | `docs/language/precept-language-spec.md:583`, `:602`, `:1726`, `:1727`, `:1729`, `:1730` | Retired keyword `writable` still live. `spec:331` records `writable` retired for `editable`; §3.8 row `:1667` and diagnostic `:1725` already use `editable`/`EditableOnEventArg (PRE0041)`. But `:1727` still names `WritableOnEventArg`; `:1726`/`:1729`/`:1730` still say `writable`; §1.2 reserved-keyword list `:583` and v2-additions `:602` still include `writable`. Sweep miss. | Rows 1726/1727/1729/1730: `writable`→`editable`, `WritableOnEventArg`→`EditableOnEventArg`. Remove `writable` from the `:583` keyword line and the `:602` v2-additions list. |
| M4 | `docs/language/primitive-types.md:70`, `:85` (vs `precept-language-spec.md:1113-1114`) | `choice` described as string-only. `spec:1114` defines `ChoiceElementType := string \| integer \| decimal \| number \| boolean` and §3.8 diagnostics `:1210` reference `"choice of integer(...)"`. But `primitive-types.md:70` calls choice a "finite string set", `:85` "Finite enumeration", string-only examples throughout — while `:181` is itself aware of `ChoiceElementType`. Headline description under-describes. | Update `primitive-types.md` §choice to state the five backing element types and add a non-string example, matching `spec:1114`. |
| M5 | `docs/language/temporal-type-system.md:157-158` (vs `:202`, `:681`, `:742`) | Summary table presents ISO `PT72H`/`P30D` as canonical duration/period form. Contradicts the locked no-constructor-literal decision (`:681`: "Duration and period have no constructor literal form (Locked Decision) … ISO 8601 … is specialist serialization syntax … not humans"; `:685` maps `PT72H`→`'72 hours'`) AND the actual serialization (`:742`: `DurationPattern.JsonRoundtrip` → `"72:00:00"`, "not ISO 8601 `PT` notation"). So `PT72H` is wrong as both an author literal and a serialization example. | Rows 157-158: drop "or ISO duration/period" from the form column; set the duration serialization example to `72:00:00` (per `:742`) or relabel the column so it clearly denotes machine-serialization, not an author literal. |
| M6 | `docs/language/temporal-type-system.md` — collection-inner-type table vs the paragraph directly below it | Self-contradiction within one section: table shows `set of period` / `set of timezone` / `set of zoneddatetime` = ✗ ("no natural ordering"), but the paragraph immediately below says these "**are valid** … `.min` and `.max` are type errors … The prior restriction … was more restrictive than necessary." Table predates the loosening the paragraph describes. | Change the three ✗ cells to ✓ (equality only; `.min`/`.max` type errors), matching the paragraph. |
| M7 | `src/Precept/Language/Types.cs:14` (vs `docs/language/temporal-type-system.md:462`, `:1025`) | `dayOfWeek` numbering disagreement. Catalog (machine-readable canon) `Types.cs:14`: `"Day of week (0=Sunday)"` — a .NET `System.DayOfWeek` convention. Doc `:462`/`:1025`: "ISO day of week (Monday=1, Sunday=7)." They disagree on Sunday (0 vs 7) and the index base. Load-bearing: `when d.dayOfWeek == 7` matches Sunday under the doc, never under the catalog. | Align the catalog hover string to ISO: `"ISO day of week: Monday=1, Sunday=7"`, matching the NodaTime backing (`IsoDayOfWeek` is Monday=1..Sunday=7) the doc already commits to twice with rationale. Stale side = the catalog string (a .NET convention leak). **Owner should confirm** the language commits to ISO (the doc already states it; this only aligns the catalog to it). |
| M8 | `docs/language/temporal-type-system.md:596` and the §"21. inZone as the sole timezone mediation operation" (ToC `:75`) | Two distinct "Locked Decision #21": `:596` = "`now()` is UTC-only"; the numbered-decisions §21 = "inZone as the sole timezone mediation operation." Same number, two decisions. | Renumber one (the `now()`-UTC one reads as the interloper inside the instant section). |
| M9 | `docs/compiler/diagnostic-system.md:417`, `:446`, `:455` | Three different diagnostic-count figures in one doc: `:417` "164 active diagnostic codes" (enumerates through PRE0164); `:446` "144 arms — one per DiagnosticCode member"; `:455` "(all 115 arms present in source)." 164/144/115 cannot all be right. | Reconcile the `:446`/`:455` code-block comments to the actual `DiagnosticCode` enum member count (verify against `DiagnosticCode.cs`; the `:417` enumeration already reaches PRE0164). |
| M10 | `docs/language/catalog-system.md:288` (vs `docs/compiler/diagnostic-system.md:417`) | Cross-doc count contradiction: `catalog-system.md:288` says `Diagnostics (162)`; `diagnostic-system.md:417` says "164 active diagnostic codes." | Reconcile both to the source `DiagnosticCode` enum count in the same pass as M9. |
| M11 | `docs/language/precept-language-spec.md:1588-1596` | §3.7 function-catalog table is structurally broken: a blockquote `> **CI functions and the catalog.**` at `:1593` sits *between* table rows (`startsWith`/`endsWith`… then blockquote then `toLower`/`toUpper`/`left`…). In Markdown a blockquote between rows terminates the table, detaching the rest into a separate fragment. | Move the `:1593` blockquote to after the final table row. Pure rendering fix. |
| M12 | `docs/runtime/fault-system.md:111-151` (the `FaultCode` block) and `:190` | Fault registry stale: 13 of 15 members. `FaultCode.cs:50-54` has 15 members; the doc block ends at `OutOfRange` and omits `LengthBoundViolation` (14) and `CountBoundViolation` (15). `runtime-api.md:658` already says "15 [StaticallyPreventable] fault codes" — so `fault-system.md` is the lone stale copy of the closed handoff registry. | Append the two missing members (with `[StaticallyPreventable(DiagnosticCode.LengthBoundViolation)]` / `...CountBoundViolation`, values 14/15) to the block at `:151`; change `// ... all 13 members ...` → `// ... all 15 members ...` at `:190`. |
| M13 | `docs/runtime/fault-system.md:125` (vs `src/Precept/Language/FaultCode.cs:23`) | Wrong owning diagnostic: the doc maps `UnexpectedNull` → `DiagnosticCode.NullInNonNullableContext` (type-stage code 19); code maps it → `DiagnosticCode.UnprovedPresenceRequirement` (proof-stage code 116). A direct "which compile-time diagnostic owns this fault class" disagreement. | `NullInNonNullableContext` → `UnprovedPresenceRequirement` at `:125`. Code is HEAD truth; reads as a deliberate move from the type-stage code to the proof obligation — **owner confirm** the mapping change was intended. |

---

## 3. Owner decisions

Nothing here is settled. Each carries a neutral two-sided framing and the prior locked text verbatim. Rulings on the two type-system forks (D1, D2) are already listed STILL OPEN under GATE-J in `docs/Working/proof-engine-decision-ledger-2026-07-12.md:84`.

### D1 — `sqrt` of an integer argument: silently widen, or type-error? (owner-fork)

**Locations:** `precept-language-spec.md:1586` vs `:1250`/`:1264`; `primitive-types.md:372` vs `:613`.

**Prior locked text (widening side), `primitive-types.md:372` (verbatim):** "`integer` may widen to `decimal` (exact) or `number` (exact within safe integer range) implicitly **in any context**." Reinforced by `spec:1250` (integer→number implicit) and `spec:1264` (function-argument context: integer widens).

**Prior locked text (barred side), `spec:1586` sqrt row (verbatim):** "Number-lane only; `decimal` and `integer` inputs are type errors … use `approximate(value)` to convert first."

- **Reading A —** `sqrt(IntegerExpr)` is accepted; the uniform "in any context" widening covers the function-argument position, so the integer silently widens to `number`. The sqrt row's "integer inputs are type errors" is the over-stated side.
- **Reading B —** `sqrt` is strictly number-lane with no implicit widening of its own argument; integer must be bridged explicitly. The general widening rule carries an implicit sqrt carve-out.

**Coupled mechanical sub-point (resolves once D1 does):** the two sqrt rows already disagree on integer independent of the fork — `spec:1586` bars **both** decimal and integer; `primitive-types.md:613` bars **only** decimal ("`sqrt(decimal)` is a type error", silent on integer, i.e. consistent with Reading A). Whichever reading wins, one of these two rows needs an edit.

### D2 — Choice field-vs-field comparison: always error, or allowed for ordered same-type? (owner-fork)

**Locations:** `primitive-types.md:347` vs `precept-language-spec.md:1398`.

**Prior locked text (always-error side), `primitive-types.md:347` (verbatim):** "**Ordinal rank is field-local.** Comparison is valid only between a choice field and a literal from its own declared set. Comparing two choice fields — even with the same member set — is a compile-time error." (Reinforced by `:349`: literals only, "in both assignment and comparison positions.")

**Prior locked text (allowed side), `spec:1398` §3.6 operator table (verbatim):** "`< > <= >=` | `choice of T` (ordered) | `choice of T` (ordered, **same element type, order-preserving subsequence**) | `boolean`" — explicitly permits ordered-choice field-vs-field comparison under a subsequence condition.

- **Reading A —** two ordered choice fields of the same element type, one's declared order an order-preserving subsequence of the other's, are comparable (spec §3.6).
- **Reading B —** choice comparison is strictly field-vs-own-literal; two choice fields never compare (`primitive-types.md`).

### D3 — How a fault leaves the evaluator: throw `FaultException`, or return `EventOutcome.Faulted`? (owner-fork)

**Locations:** `result-types.md` / `runtime-api.md` / `fault-system.md` (Reading A) vs `evaluator.md` / `result-types.md` (Reading B). This is the runtime terminus of the fault handoff — the guarantee's compile/runtime boundary — so it is in-lens. The runtime is unbuilt, so this settles a design shape, not observed behavior.

**Prior locked text — the open question, `fault-system.md:301` (verbatim):** "The new design should decide whether `Fault` is a control-flow exception or a structured value before `Fault` is finalized." (`fault-system.md` Q1, `:288-301`, still frames this as undecided.)

- **Reading A — throw / outside the outcome hierarchy:** `result-types.md:51` ("Faults … throw `FaultException` — they are not in the outcome hierarchy"), `:72`, `:428`; `runtime-api.md:885` ("faults are the exceptional escape hatch … not part of the normal outcome model"); `fault-system.md:8`/`:95`/`:337`.
- **Reading B — returned as an outcome variant:** `evaluator.md:1402` ("Faults are **never thrown** — they are returned as structured outcome variants"), `:1404` (`EventOutcome.Faulted(Fault)`); `result-types.md:104` (`public sealed record Faulted(Fault Fault) : EventOutcome;`), `:117`; `runtime-api.md:172` lists `Faulted` as an outcome.

**Sweep on ruling:** choosing B ⇒ delete the throw/outside-hierarchy language at `result-types.md:51/72/428`, `runtime-api.md:885`, `fault-system.md:8/95/337`, and close Q1. Choosing A ⇒ correct `evaluator.md:1402/1404` and `result-types.md:104/117`. Note `result-types.md` currently carries **both** readings, so it needs an edit either way.

### D4 — `collection-types.md` top Status honesty (scope-call)

**Location:** `docs/language/collection-types.md:9` (`| Doc maturity | Canonical design |`), tension with `:801` ("Locked design … Not yet implemented").

**Facts:** Per CLAUDE.md, "Canonical design" means "grounded in the implementation." Collections are *partially* built — count-bounds ship (`CountBoundViolation` PRE0136 live, `Diagnostics.cs:1293`, `FaultCode.cs:53`; parser type support), and `set`/`lookup`/`log`/`queue` compile today (the `academic-course-registration` sample compiles clean) — while other parts are explicitly unbuilt (`:801` quantifier predicates "Not yet implemented"). The top Status doesn't surface the split, so a reader takes the whole doc as implementation-grounded. **This is a real, intended feature — do not propose deletion; the choice is only about wording.** No prior locked decision governs this Status field.

- **Option (a) — keep "Canonical design" as-is:** the built parts justify it and per-section "Not yet implemented" markers already exist internally.
- **Option (b) — add an `| Implementation state |` row:** enumerate built (cardinality/count-bounds, inner-type value modifiers, parser, `set`/`lookup`/`log`/`queue`) vs not-yet-built (e.g. quantifier predicates per `:801`), matching the honest per-doc pattern the runtime docs use (e.g. `runtime-api.md:8` "Partial stub…").

---

## 4. New-surface parks

**None.** No finding in this pass would add or settle a keyword, type, operator, modifier, construct, or syntax. The two type-system forks (D1 sqrt, D2 choice comparison) each pick between two *already-specced* readings of existing surface — they are reconciliations, not new surface — so they stay in the conformance track and do not route to `/design`.

---

## Appendix — leads checked and cleared at HEAD (do not re-file)

- **§0.6 proof-philosophy #2/#3 are current, not drift.** `spec:223` scopes "proven violations only" to violation *reports* and states the three-way verdict; `:225` replaces the solver ban with the certificate criterion — matching ledger rulings #6 and #2.
- **No surviving D2 band-softening sentence in the spec.** §0.6 (`:223`/`:229`/`:266`) is uniformly prove-or-reject; `proof-engine.md:1257` describes count-band as prove-or-reject (`CountBoundViolation` PRE0136, "never deferred to a runtime check"). `spec:258`'s "governed band" is count-interval seed terminology, not a warn-and-govern carve-out (owner-eyeball only, low confidence). The dead D2 framing survives **only** in the `spec:237` clause folded into M2.
- **`spec:256` single-pass / no-multi-hop sentence is ratified, not drift** — `ledger:51` defers §1b and explicitly does not authorize a `:256` override.
- **exchangerate `.from`/`.to` direction reads internally consistent at HEAD** (`business-domain-types.md:1016`/`:1035`/`:1233` agree).
- **All 15 `[StaticallyPreventable]` fault classes disposition to exactly one side of the compile/runtime boundary per data-origin** — none claimed by both contradictorily, none by neither; business-rule/`ensure` violations cleanly owned by runtime governance.
- **`docs/runtime/*.md` statuses are uniformly honest** (stub/pending/partial/design); the runtime being unbuilt is disclosed correctly. **`diagnostic-system.md:191`** self-documents the Warning-vs-target-Error severity gap as tracked drift — honest, no finding.

## Appendix — in-lens but unverified this pass (leads for the conformance stage, not confirmed findings)

The internal-consistency lens notes the v1 inventory (`docs/Working/compiler-readiness-plan-2026-06-11-appendices/spec-coverage-audit.md` §"Canon inconsistencies reported") lists ~40 further doc-vs-doc items — `graph-analyzer.md` vs spec §0.5, `name-binder.md` vs spec §3.5, `type-checker.md` vs spec §3.3, `proof-engine.md` strategy-count self-contradictions, stage-label routing, PRE0087-vs-0088 misrouting, `.at(N)` proof requirement, `writable` in stage docs. **Treat as leads, not confirmed** until re-grounded at HEAD by the per-clause conformance audit.

- **Ordered choice field-vs-field comparison — impl is a false-clean (D2 follow-up, conformance track WF-1).** With D2 resolved to Reading A, the spec §3.6 rule permits `choice`-field-vs-`choice`-field ordered comparison only when the two fields share the same backing element type and one field's declared value order is an order-preserving subsequence of the other's; any other pairing is a compile-time type error. The current implementation checks only that both operands are declared `ordered` — it accepts *any* two ordered choice fields, including disjoint value sets and mismatched element types, emitting no diagnostic. Confirmed at HEAD: two disjoint ordered `choice of string` fields compared with `<` compile clean (0 diagnostics; the sole proof obligation is "Both choice operands must be declared ordered"). This is a soundness gap (false-clean), not a spec drift. Tighten the impl to enforce the subsequence check — same element type, order-preserving subsequence, else type error — in the conformance track. **Not fixed in this spec-correction pass** (it is code, not a spec edit).
