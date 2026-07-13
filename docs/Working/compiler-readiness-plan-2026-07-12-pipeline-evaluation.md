---
title: "Compiler Readiness Plan v3 — Stage 0a Pipeline Evaluation + Disposition Ledger"
status: Draft — 2026-07-12 (Stage 0a companion to `compiler-readiness-plan-2026-07-12.md` — v3 of the compiler-readiness-plan lineage, superseding the 2026-06-16 v2; owner-gated at the 0→1 boundary; revised after Fable completeness-critic)
author: Synthesis (Claude, main loop), grounded in four Explore scouts + one Fable completeness-critic
owner: Shane
purpose: The high-level, map-resolution evaluation of the whole compiler pipeline before any MVP code. Produces a keep/refactor/remove disposition for every finding, reconciles the old plan's pending cleanup against the ratified rulings, enumerates the surfaces the MVP touches (obligation families, tooling, corpus), and locks the refactor/removal scope boundary. Deep per-area review is deferred to each slice (Stage 1).
---

# Stage 0a — Pipeline Evaluation + Disposition Ledger

**What this is.** The breadth-first, map-resolution pass over the whole compiler pipeline (`Lexer → Parser → NameBinder → TypeChecker → GraphAnalyzer → ProofEngine`, `Compiler.cs:14–19`) before any MVP code. Every finding gets one disposition — **keep / refactor / remove** — and an **in-scope / deferred** call. Deep per-area review happens in each slice; this page sets direction, the scope boundary, and the surfaces the MVP ripples into.

**Grounding.** Four Explore scouts mapped the proof-engine code, the doc landscape, the plan/test conventions, and the non-proof-engine pipeline. A Fable completeness-critic then found gaps and one critical error; this revision folds all of it in. Items marked *(verify in Slice N)* are stated at map resolution — the per-slice pass confirms them against source before acting. Anchors are the scouts'/critic's; dispositions are this pass's judgment.

---

## 0. Reconcile the old plan's Phase-1 cleanup against the rulings (all five slices)

The 2026-06-16 plan's Phase 1 is still "(NEXT)" and **none of it landed**. It predates the rulings, so every carry-over is re-judged. **Correction (this revision):** an earlier draft ordered removal of "GraphAnalyzer D2 scaffolding" — that was a **label collision** and is wrong (see the ⚠ row). All five Phase-1 slices are now reconciled, not two.

| Old-plan item | Reconciled against rulings | Disposition |
|---|---|---|
| **Slice 1.3 — D2 band-reclassification** (out-of-range / length / count bound violations: error → warning) | **DEAD as a plan item.** #1 (prove-or-reject), #6 (three-way verdict), #10 (dead-row = Error) keep bounds as **rejections (Error)**. | **do not inherit** the reclassification. ⚠ **No code to sweep** — it never landed. |
| ⚠ **`GraphAnalyzer.cs:734–745` "D1/D2"** | These are **internal step-labels** for `EmitAlwaysRejecting` (`AlwaysRejecting`) and `EmitStateAlwaysRejects` (`StateAlwaysRejects`) — **live** emission, `d1FlaggedEvents` consumed at `:178/:812` to suppress duplicate diagnostics. **Not** the plan's D2. | **KEEP** — my earlier "remove" was a name collision; deleting it double-reports. |
| ⚠ **"matched-pair" comments** (`ProofEngine.Analysis.cs:458`, `Intervals.cs:73`) | The collection write-site/read-site **soundness pairing** (Semantic Rule 4/5), **not** the dissolved plan matched-pair rule. | **KEEP** the rationale. Separate nit: the `design §` cite greps to nothing — transient-ref hygiene fix, *not* deletion *(verify in the §6 slice)*. |
| **Slice 1.3 — non-reclassification sub-items** | Survive: the PRE0078 "exceeded representable range" message split + PRE0135 placeholder fix — **the §4a witness slice rewrites exactly these messages**; Coverage-Ledger §B row 5 (PRE0164/PRE0115 reframe as definition-incoherence under the old GATE-H taxonomy #1 folded in, #10 touches). | **refactor** — fold into §4a / diagnostics slices *(verify in Slice)* |
| **Slice 1.4 — honest registry** | Re-judge under three-way: bound violations *are* now statically prevented (rejected). Named ruling-independent survivors (all live): 3 `DivisionByZero` fall-throughs (`ProofEngine.Diagnostics.cs:386,392,490`), pow-exponent misroute, `UnexpectedNull` bijective row, `IndexOutOfBounds`/`KeyNotFound` routes; **and the `ProofStrategy.Literal // vacuously proved` mislabel (`ProofEngine.cs:1181`) — a soundness fix, not just a §5a cosmetic relabel: its dead-end→dead-end arm stamps a false `Proved` on obligations never analyzed (architecture §1.9 / §2.1 Hole 4).** | **refactor** — the Literal-mislabel rides **Slice 0** (architecture §1.9 / §2.1 Hole 4, a fail-open soundness fix), not §5a/Slice 1.4; the rest verify in Slice 1 |
| **Slice 1.4 — D1 overflow attribute** | `FaultCode.NumericOverflow` `[StaticallyPreventable]` (`FaultCode.cs:44`) claims a prevention parked overflow (#7) doesn't deliver. **`Precept0002FaultCodeMustHaveStaticallyPreventable` fails `dotnet build` on an attribute-less member** — so "just strip it" isn't free; the old plan already chose an honest-gap mechanism. #7 keeps the *spec text* as target; the *code attribute* honesty is the real call. | **refactor** — decide the honest-gap mechanism in Slice 1 |
| **Slice 1.5 — canon-amendment package** — **DEAD half** | GATE-F band-softening spec edits (spec §0.6 L256/258, §0.7 L266, item 6 L208, the P11 edits, `evaluator.md §10`) are **REVERSED**: #1/#6 keep bounds prove-or-reject, #7 keeps overflow text as-is, #3 does **not** authorize the spec:256 override. | **do not inherit** — inheriting these softens sentences the owner just ruled to keep |
| **Slice 1.5 — SURVIVING half** (ruling-independent drift) | (a) BUG-030 — `proof-engine.md §7.2` still calls cross-unit cancellation "undecided policy" vs. the owner lock (allow-all-with-surfacing); (b) spec §3A.2 `:1946` still asserts restore-time constraint failure vs. §0.7 trusted-hydration; (c) `fault-system.md` RC-09 13-vs-15 member drift; (d) spec §0.6 items 7–10 "specification-only" mislabel. | **refactor docs** — Stage 0c / owning slices *(verify each)* |

**Register→disposition invariant holds:** all five Phase-1 slices now have a disposition; nothing dropped.

---

## 1. Pipeline stage health (non-proof-engine)

| Stage | Finding | Disposition |
|---|---|---|
| **Lexer** / **Parser** / **NameBinder** | Clean; no stubs; catalog-routed. | **keep** |
| **TypeChecker** (~8,700 lines / 10 files) | No stubs; two live TODOs (`Expressions.Callables.cs:1218`; a doc-prescribed `NotYetImplemented` arm with zero code hits — drift, §7). | **keep**; size/altitude **refactor review** deferred (not MVP-blocking) |
| **GraphAnalyzer** | Live always-rejecting emission (the "D1/D2" above). No dead scaffolding. | **keep** |

No `NotImplementedException` in `Pipeline/` or `Language/`. Root-level scratch files sit outside `src/Precept/` — deferred.

**GraphAnalyzer + proof-stage structural-severity dispositions (the ruled Warning → Error family).** So this ledger holds the full keep/refactor/remove set in one place, the owner-ruled structural-severity family (detailed with four-leg rationale in `compiler-readiness-plan-2026-07-12-structural-severity.md`):

| Diagnostic | Current | Disposition |
|---|---|---|
| `UnreachableState`, `StructuralSinkState`, `DeadEndState`, `RequiredStateDoesNotDominateTerminal` (process-topology) | Warning | **refactor → Error** (`philosophy.md:51` "proven impossible") |
| `ContradictoryRule` (PRE0155), `UnsatisfiableRule` (PRE0159) (uninhabitability) | Warning | **refactor → Error** (a definition no valid entity can inhabit) |
| `UnhandledEvent`, `AlwaysRejecting` (transition), `StateAlwaysRejects`, `FieldNeverSet`, `VacuousRule`, `TautologicalGuard`, `UnsatisfiableGuard` | Warning | **keep** (advisory / inhabitable — not a prevention defect) |
| `UnsatisfiableInitialState`, `DefaultViolatesRule`, `NoInitialState`, `TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge` | Error | **keep** (already Error) |

The canonical *prose* for this split (`diagnostic-system.md`, spec §0.6 item 7) is already reconciled (Stage 0c); the `Diagnostics.cs` code flip + `graph-analyzer.md` + spec `:186/:189` + the terminal-detection regression test ride the structural-severity slice.

---

## 2. Catalog-driven-architecture violations (critic-verified accurate)

| Site | Smell | Disposition | In-scope? |
|---|---|---|---|
| `ProofEngine.Qualifiers.cs:521` | `OperationKind.LookupAccess` identity arm, self-TODO'd | **refactor** | proof-engine area — in-scope |
| `ProofEngine.Analysis.cs:615` | `modKind switch { Min/Max }` bound-source select | **refactor or justify** | evaluate in §6 slice |
| `Construct.cs:35` | `[Obsolete] LeadingToken` shim | **remove** (cheap) | in-scope |
| `TypeChecker.Validation.Modifiers.cs:350,538` | `ModifierKind` per-member switches | **refactor** toward `Modifiers.cs` | **deferred** (no MVP slice touches it) |

---

## 3. Diagnostic-emission architecture (narrowed intersection claim)

- **Built:** `src/Precept.Analyzers/` (`DiagnosticCoverageScanner.cs`, three emission channels; PRECEPT0027/0028/0019).
- **Not retired:** the `DiagnosticStage` enum (`Diagnostic.cs:11`) — **but it is a pure component label with no severity semantics** (its own comment). The three-way verdict is a *severity/verdict-model* change, largely **orthogonal** to `DiagnosticStage`. The genuine shared surface is the `Diagnostics.cs` `GetMeta` rows.
- **Debt:** two allow-list codes "retired — pending removal" (`DiagnosticCoverageAllowLists.cs:30,37`) still carry enum members + meta rows.

**Disposition:** **refactor** `Diagnostics.cs` `GetMeta`; **remove** the two retired codes. **Entanglement probe (done, 2026-07-12):** `DiagnosticStage` has **zero usage in `Precept.Analyzers`** and only three references in `src/Precept` (the `Diagnostic.cs` enum, the `Diagnostics.cs` `GetMeta` stamps, one comment at `DiagnosticCode.cs:305`). Retiring it is a **small, low-entanglement change with no analyzer impact** — confirming the intersection is narrow (the `GetMeta` rows, not a broad emission-path reshape).

---

## 4. Obligation-family enumeration (which families get verdict / witness / certificate)

`ProofRequirement.cs:55–265` defines **12 sealed obligation subtypes**. Ruling #6 names only "bounded-write checks" (the three containments). Every family needs an explicit verdict/witness/certificate disposition or per-slice work inherits only the tripped-over subset (the enumerate-the-whole-family discipline). Map-resolution grid *(each cell verified/refined in its slice)*:

| Family | Three-way verdict? | Witness (§4a)? | Certificate (§5a)? |
|---|---|---|---|
| IntervalContainment, LengthContainment, CountContainment | **yes** (#6 core) | **yes** — concrete breaking value | **yes** |
| Numeric (divisor/sqrt) | yes | value-at-fault | yes |
| Presence, KeyPresence, IndexBounds | verdict yes; **witness = open** (a configuration, not a number) | *(decide in Slice 0/4)* | yes |
| QualifierCompatibility, QualifierChain, AssignmentQualifier, DimensionalProduct, Dimension | verdict yes; witness likely N/A | *(decide in Slice 0)* | yes — all verdicts carry a certificate (#2) |

Every proof verdict carries a certificate (#2 is universal). Witness applies where a concrete counterexample exists; the non-numeric families need an explicit "witness N/A / witness = config" call in Slice 0.

---

## 5. Tooling propagation surface (was silently dropped)

The three-way verdict, §4a witness, and §5a certificate change shapes that cross the tooling boundary — **MCP Tool Sync is non-negotiable (CLAUDE.md)**:
- `Compilation.Proof` (ProofLedger) + `Compilation.Diagnostics` (`Pipeline/Compilation.cs:12–13`) → consumed by `tools/Precept.Mcp/Tools/CompileTool.cs` and `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`.
- `tools/Precept.Mcp/Dtos/CompileToolDtos.cs:14` serializes `Severity` (→ three-way verdict).
- Docs: `docs/tooling/mcp.md`, `docs/tooling/language-server.md`.

**Disposition:** **in-scope, per-slice** — each MVP slice that changes a verdict/witness/certificate shape updates the DTO/formatter + tooling doc in the same commit (the CLAUDE.md MCP-sync rule). Listed here so no slice forgets it.

---

## 6. Corpus / test impact (was missing)

77 `.precept` samples + ~3,600 tests. #1 was ratified **as a package with #2 and the MVP engine precisely because** prove-or-reject over *today's* engine would reject the corpus's house style — §6/§1a flip many of those rejects into accepts, and the verdict/witness/message work changes strings that fixtures assert. The old plan had an explicit corpus compat slice (2.4).

**Disposition:** a **corpus/fixture reconciliation is a first-class per-slice obligation** (update samples + `# EXPECT:` contracts + affected assertions in the same slice), and the DoD includes a green corpus. Flagged here so it isn't discovered late.

---

## 7. Drift + doc-ownership (expanded)

- **Status overclaims:** `proof-engine.md` + `compiler/README.md` mark stage 6 Full/Implemented (contradicts the un-built MVP); `diagnostic-system.md` README "Draft" vs doc "Full". → Stage 0c.
- **Phantom doc with poisoned framing:** `README.md:37` links `soundness-and-coverage.md` describing it as "**verify-don't-trust** … the trusted checker" — an architecture **deleted 2026-06-08, do-not-resurrect**. Creating the doc in Stage 0c must **rewrite** that framing to the certificate model, not inherit it. (Resurrect-by-default hazard.)
- **Spec three-category text:** `precept-language-spec.md` item 5 (~L227, "Truth-based diagnostic classification") frames unresolved as "supply additional constraints," predating #6's "unresolved = rejected" — catches up with spec:223/225 in the same 0c pass.
- **Homes for the ratified edits:** the two spec edits (#2 spec:225, #6 spec:223), the #2 four-part admissibility criterion (needs a canonical home near spec:225), and the #8 philosophy clause (already applied) — Stage 0c owns placement.
- **TypeChecker stub-arm drift** (`type-checker.md:899`, zero code hits) → 0c or touching slice.

---

## 8. Scope boundary (corrected)

**In-scope removals/refactors** (MVP touches or depends on):
- `Construct.cs:35` obsolete shim — **remove**.
- `RestoreConstraintsFailed` (`RestoreOutcome.cs:18`) — **remove**, but it **rides the paired spec §3A.2 `:1946` trusted-hydration edit** (not "clean/unaffected"; deleting the DU while the spec asserts it creates a contradiction).
- `ProofEngine.Qualifiers.cs:521` catalog violation — **refactor** (proof-engine work).
- Honest-registry re-scope + Literal-mislabel + D1 overflow-attribute — **refactor** (Slice 1 / §5a).
- `DiagnosticStage`/`GetMeta` + 2 retired codes — **refactor/remove** (sequencing per §9).
- `DesugarsToRule` (zero synthesis consumers; `Modifiers.cs`) — **its open question IS §6's design question** (does the engine read a synthesized `TypedRule`, or narrow on modifier metadata?) → **resolve in the §6 slice**.

**Deferred (catalogued, capture-pointer):** `ModifierKind` catalog moves; TypeChecker size refactor; root-level scratch files.

**Nothing dropped silently** — every §0–§8 finding is built-in-a-slice, deferred-with-pointer, or keep.

---

## 9. Open sequencing decision (owner call at the 0→1 gate)

**Emission-ownership refactor vs. the MVP's diagnostic changes.** (a) groundwork-first, (b) build-on-current, (c) interleave (retire `DiagnosticStage`/reshape `GetMeta` in the three-way-verdict slice). With the entanglement probe done (§3: zero analyzer coupling, three-file footprint), **(c) is the grounded recommendation** — the three-way-verdict slice already reshapes the `GetMeta` severity rows, and retiring `DiagnosticStage` + removing the two retired codes rides along with no separate-refactor risk. **✅ Owner confirmed (c) (Shane, 2026-07-12):** retire `DiagnosticStage` + remove the two retired codes as part of the three-way-verdict slice.

---

## Owed before the 0→1 gate

- Stage 0b (architecture design lock) + 0c (foundational docs) complete Stage 0; the gate reviews all together.
- (The `DiagnosticStage`↔analyzer entanglement probe is done — §3/§9.)
- The map-resolution items marked *(verify in Slice N)* are confirmed against source at their slice, not now.
