---
status: Externally-Grounded
tier: Light — confirm the ruled seam (the family is already owner-ruled; this pass verifies the seam against source and frames acceptance)
date: 2026-07-13
revised: 2026-07-13 — adversarial-review fixes applied and re-verified against source: breaking-change enumeration re-grounded empirically (run-the-suite-under-flip, not grep), shipped catalog-snippet corpora added to scope, the false "sample gate is green" claim corrected, the Slice-0 Hole-4 sequencing constraint stated in the correct direction, the certificate-exclusion citation made precise, and the single-state migration shape added as a worked sample
slice: structural-severity (compiler-readiness plan v3, 2026-07-12)
ruled-by: docs/Working/compiler-readiness-plan-2026-07-12-structural-severity.md (owner rulings recorded 2026-07-12, §8)
sources-consulted:
  - docs/Working/compiler-readiness-plan-2026-07-12-structural-severity.md: the ruled family table (§1), per-flip four-leg rationale (§2), corpus impact 0/77 (§3), uninhabitability ruling (§4), OQ1 override authorization (§5), terminal-detection precondition (§5a), doc obligations (§6), acceptance (§7), rulings (§8)
  - docs/Working/compiler-readiness-plan-2026-07-12.md: slice stub :370-384; F5 dead-end disposition :129; two-arm-fix outcome-neutrality note :215-223; acceptance frame (full-compile assertions only) :386-395
  - docs/Working/certificate-steps-membership-2026-07-12.md: :50 — lateral structural scans (PRE0082/0153/0154/0155/0159 and kin) carry NO certificates ("verdict = obligation outcome"); this slice touches no certificate vocabulary
  - src/Precept/Language/Diagnostics.cs: the six flip rows :707/:720/:726/:747/:802/:814 (all Severity.Warning today); stay-Warning rows :714/:754/:761/:770/:781/:792/:1141; already-Error rows :288/:733/:740/:867-869/:877-879
  - src/Precept/Pipeline/GraphAnalyzer.cs: StructuralSinkState gate `!stateFlags[...].IsTerminal` :113-127; DeadEndState gated on `terminalStates.Length > 0` :131-149; ComputeDeadEnds terminal exclusion :489-494; IsTerminal from StateModifierMeta.AllowsOutgoing :683; the one per-instance severity override (AlwaysRejecting construction rows) :783
  - src/Precept/Language/Modifiers.cs: Terminal ⇒ AllowsOutgoing: false :242-245 (single catalog source of terminal-ness)
  - src/Precept/Compiler.cs: HasErrors = any(Severity.Error) :40
  - src/Precept/Runtime/Precept.cs: Precept.From throws on HasErrors :36-38 (no engine from an erroring compilation)
  - src/Precept/Pipeline/ProofEngine.Satisfiability.cs: PRE0159 emission :345, PRE0155 emission :413 — both plain Diagnostics.Create, no severity override
  - tools/Precept.LanguageServer/DiagnosticProjector.cs :36-40 / DiagnosticEnricher.cs :129-131: severity mapped per-VALUE, not per-code — the flip propagates with zero tooling edits
  - docs/compiler/diagnostic-system.md :191: Stage-0c reconciled two-severity model + the "tracked drift" caveat that this slice retires
  - docs/compiler/graph-analyzer.md: :452-457 (Warning emission + "may be intentional" rationale), :271 (DeadEndStateFact row "Warning, code 108"), :776-777 (OQ1 resolved-Warning block), :833-839 (code table, no severity column)
  - docs/language/precept-language-spec.md: :92-112 (§0.1 eleven principles), :186/:189 (process-topology capability rows), :209-210 (§0.6 items 7/8 — item 7 already reads "rejects the definition", Stage 0c), :225 (proof philosophy #3)
  - docs/philosophy.md: :19 (deliberate prohibition), :49 (prevention), :51 (the three proven-impossible topology properties), :61 (compile-time structural checking list)
  - docs/compiler/README.md: grep verified — no per-code severity column exists; no edit owed there
  - test/Precept.Tests/SampleCompilesCleanTests.cs: :14-15 strict zero-diagnostic gate, :52-60 assertion; stale "current corpus is 75" comment :26-27 (actual: 77)
  - test/Precept.Tests/GraphAnalyzerTests.cs :34/:67/:558 and DiagnosticsTests.cs :138/:150: Warning-severity assertions that must flip with the code
  - samples/crosswalk-signal.precept (cyclic, no terminal — full read); samples/hiring-pipeline.precept (terminal states, rules, ensures — read)
  - live precept_compile probes (2026-07-13): MembershipCard variant → PRE0119 warning today; FeeSchedule variant → PRE0159 warning + PRE0164 error today; Counter single-state variant → PRE0119 warning today; `state Active initial terminal` → compiles `HasErrors == false` (PRE0158 warning only); the `UnsatisfiableRule` ExampleAfter text (`Diagnostics.cs:820`) → PRE0164 **error today** + PRE0119 warning (probe transcripts/results inline below)
  - empirical flip-run (2026-07-13, this pass): the six rows flipped in the working tree, all four test projects run, tree reverted — `test/Precept.Tests`: 40 failed / 6835 under flip vs 16 / 6835 at baseline (delta **24**); `test/Precept.LanguageServer.Tests`: 13 failed / 417 under flip vs **0** at baseline; `test/Precept.Mcp.Tests` (67) and `test/Precept.Analyzers.Tests` (296) green under flip
  - src/Precept/Language/SyntaxReference.cs :796-837: the "Hollow draft state" anti-pattern pair (Bad + **Good** variants) — both fail `CatalogSnippet_CompilesClean` under flip, as does the "Sub-state encoded" pair; compile-gated at `test/Precept.Tests/SyntaxReferenceTests.cs:192`
  - src/Precept/Language/Quickstart.cs :121-153: the three MinimalExamples survive the flip (no lone non-terminal state; the "Minimal lifecycle" example wires Draft → Complete terminal)
  - samples/Test.precept: committed at 68f83fb7 ("commit this snapshot, even though no decisions are made"); fails `Sample_CompilesClean` at baseline **today** with two PRE0078 NumericOverflow Errors (probed) — pre-existing, unrelated to the six codes
  - docs/Working/compiler-readiness-plan-2026-07-12-architecture.md :155: Hole 4's dead-end→dead-end relabel is a **Slice-0 action** whose outcome-neutrality presumes dead-end = Error already in force
  - test/Precept.LanguageServer.Tests/SemanticTokensHandlerTests.cs :121: existing in-repo use of the `state Draft initial terminal` repair form
---

# Slice design — structural-severity: apply the ruled Warning → Error flips

**Plain-language summary.** Precept's compiler already *detects* six definition defects that its own philosophy says must be *impossible*: a state no entity can ever reach, a state an entity can enter but never leave (undeclared), a state with no route to any endpoint, a `required` state some completion path skips, a rule no value can satisfy, and a rule pair no configuration can satisfy. Today all six are Warnings — the definition still compiles and an engine is still produced. The owner ruled (2026-07-12, recorded in the companion doc) that all six become Errors: the definition is rejected, no engine is produced. This slice applies that ruling in code, repairs everything the flip breaks — 37 tests across two test projects, empirically enumerated below by running the suite under the flip: 5 severity-pin assertions plus 32 fixtures and shipped teaching snippets that relied on now-rejected scaffolding shapes — adds one regression test protecting the flip's precondition (a declared-`terminal` state must never be mis-flagged as a dead end), and syncs the remaining canonical prose. The design work was done in the ruled companion doc; this document **confirms the seam against source**, states the design-level acceptance frame, and enumerates the proposed doc corrections. Nothing here re-decides the family.

**Terms used below, in plain words.**
- *Process-topology defect* — a flaw in the state graph itself: unreachable states, stuck states, skipped required states.
- *Uninhabitable definition* — a definition whose declared rules admit **no** satisfying data at all, so no valid entity could ever exist under it.
- *Structural sink* — a reachable, non-terminal state with literally zero outgoing transitions (internal name `StructuralSinkState`, PRE0119).
- *Dead end* — a reachable, non-terminal state that has outgoing transitions but no path to any terminal state (internal name `DeadEndState`, PRE0108).
- *Dominate* — state A dominates terminal T when every path from the initial state to T passes through A; the `required` modifier claims exactly this.
- *Baseline severity* — the severity written in a diagnostic's metadata row in `Diagnostics.cs`; every emission of that code inherits it unless an emission site explicitly overrides it (only one such override exists in the codebase: `GraphAnalyzer.cs:783`).

## Goal

When this slice lands, a `.precept` definition that trips any of `UnreachableState` (PRE0080), `StructuralSinkState` (PRE0119), `DeadEndState` (PRE0108), `RequiredStateDoesNotDominateTerminal` (code 111), `ContradictoryRule` (PRE0155), or `UnsatisfiableRule` (PRE0159) fails compilation (`HasErrors == true`) and can never produce an engine (`Precept.From` refuses, `Runtime/Precept.cs:36-38`) — while every ruled-Warning diagnostic keeps its current severity, no shipped sample trips any flipped code (verified by compiling the corpus under the flip: the only failing sample, `samples/Test.precept`, fails identically at baseline on pre-existing PRE0078 NumericOverflow Errors — Drift ledger #5), zero sample `.precept` edits are owed, and a new regression test pins that a declared-`terminal` state is never flagged as a sink or dead end.

## Scope

- **In scope**: the six baseline-severity flips in `src/Precept/Language/Diagnostics.cs` (`:707`, `:720`, `:726`, `:747`, `:802`, `:814`); the terminal-detection regression test (companion §5a); updating the five test assertions that pin the old Warning severity **and repairing the 32 fixture/snippet breakages the flip causes beyond them** across `test/Precept.Tests` and `test/Precept.LanguageServer.Tests` (empirically enumerated in § Breaking changes), including the shipped teaching-snippet corpora — the `SyntaxReference.cs` anti-pattern snippets (served via `precept_patterns`) and the `Diagnostics.cs` Example fixtures for the flipped codes (served via `precept_diagnostic`), one of which (`UnsatisfiableRule` ExampleAfter) is an erroring definition **today** (Drift ledger #6); the remaining canonical doc-sync (graph-analyzer.md OQ1 override, spec `:186`/`:189` severity naming, retiring the diagnostic-system.md `:191` tracked-drift caveat, plus the proposed de-garble of the plan's sequencing sentence — enumeration #7) — **proposed as content here, applied at execute with owner sign-off**.
- **Out of scope (already ruled or owned elsewhere — cited, not re-decided)**: which codes flip (owner-ruled 2026-07-12, companion §8); the stay-Warning set (`UnhandledEvent`, `AlwaysRejecting` transition rows, `StateAlwaysRejects`, `FieldNeverSet`, `VacuousRule`, `TautologicalGuard`, `UnsatisfiableGuard` — ruled Warning); the already-Error set (`NoInitialState`, `TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge`, `UnsatisfiableInitialState`, `DefaultViolatesRule`, `AlwaysRejecting` construction-row override — unchanged); the D2 band split (numeric/length/count bounds — a different family, `decision-index.md`); the ProofEngine "dead-end→dead-end vacuously-proved" relabel (architecture §2.1 Hole 4 / §1.9 — a **Slice-0 action**, not this slice's; this slice supplies the `DeadEndState` Error that makes that relabel outcome-neutral, which imposes a real ordering constraint — see § Dependencies).
- **Deferred**: nothing is deferred out of this slice; the family has no remaining unruled member (companion §1 covers every structural-soundness code with an explicit disposition).
- **Certificate vocabulary: not touched.** The flipped codes carry no certificates, on two distinct grounds: the two uninhabitability codes (`ContradictoryRule` PRE0155, `UnsatisfiableRule` PRE0159) are **explicitly** excluded by the settled Slice-0 vocabulary (`certificate-steps-membership-2026-07-12.md:50`, whose exclusion names the proof-stage lateral scans PRE0082/0153/0154/0155/0159 "and kin"); the four graph-stage codes are **not named there and were never in certificate scope at all** — certificates attach to proof obligations, and graph diagnostics are not obligation verdicts (architecture §1.1, "verdict = obligation outcome"). This slice adds no premise kind, no step kind, no language surface.

## Philosophy Alignment

Per `precept-language-spec.md` §0.1 (`:92-112`), every principle, no blanks:

| Principle | Affected? | How served | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | This is the slice's whole point: the six defects `philosophy.md:49/:51` claims are "proven impossible at definition time" stop being detectable-but-shippable Warnings and become structurally blocking Errors — the compiled engine that embodied them can no longer exist. | N/A | An intended permanent-hold state now *must* be declared `terminal` (one token); an uninhabitable rule set that previously compiled-with-warning now rejects. Both accepted in the ruling (companion §2). |
| 2. One file, complete rules | Y | The severity decision is carried entirely by the diagnostic metadata derived from the single definition; no external configuration, suppression flag, or severity override surface is added. | N/A | N/A |
| 3. Deterministic semantics | Y | Severity is a constant in the metadata row — same definition, same diagnostics, same severities, always; no new analysis is introduced. | N/A | N/A |
| 4. Full inspectability | Y | The same diagnostics with the same messages, spans, and FixHints are surfaced; only their blocking force changes — nothing becomes hidden. (One legibility defect on PRE0159's message is surfaced in Open Questions — it predates this slice but matters more once the code blocks.) | N/A | N/A |
| 5. Keyword-anchored readability | N | N/A — no grammar, token, or authored-syntax change; `terminal` already exists and its meaning is unchanged. | N/A | N/A |
| 6. Explicit domain meaning | Y (indirectly) | The dead-end flip converts `terminal` from optional annotation to mandatory intent declaration — an endpoint's finality must be *named*, not inferred (companion §2, "intent is structural, never implicit"). | N/A | N/A |
| 7. Compile-time-first static checking | Y | The checks already run at compile time; the flip makes their verdicts *reject* what they prove invalid, which is this principle's literal wording ("rejects what it can prove invalid, and does not guess" — spec `:104`). All six codes fire only on *proven* defects (BFS/reverse-BFS facts, provably empty rule ranges), so no guessing is introduced. | N/A | N/A |
| 8. Approximation honesty | Y | Graph analysis over-approximates reachability (all edges traversable regardless of guards — spec `:191` overapproximation rule); that over-approximation only *widens* the reachable set, so it can under-flag, never over-flag, unreachable/dead-end states — Errors fire only on structurally certain defects. | N/A | A guard-created dead end (structurally exits exist but no guard can ever pass) stays invisible to this family — it is the proof engine's territory, not a severity question. |
| 9. Mandatory rationale | N | N/A — no constraint surface changes; `because` clauses are untouched. | N/A | N/A |
| 10. Totality | N | N/A — no expression evaluation surface changes; no new fault path. | N/A | N/A |
| 11. Static completeness | Y | Strengthened: "a definition that compiles without diagnostics has no … unreachable business process states, and no structural dead ends" (`philosophy.md:57`) previously held only for authors who *heeded warnings*; after the flip it holds structurally — a compilation with these defects has `HasErrors == true` and `Precept.From` refuses it (`Compiler.cs:40`, `Runtime/Precept.cs:36-38`). | N/A | N/A |

**Companion commitments.** *Stateless-first-class*: the four process-topology codes never arise for stateless precepts (no state graph); the two uninhabitability codes apply identically to stateless and stateful precepts (verified: the FeeSchedule probe below is stateless). *Domain-expert-primary-author*: the messages the author sees are unchanged; the dead-end message already reads as an intent prompt ("…is not marked 'terminal'", `Diagnostics.cs:720`).

## Language Design Grounding

**No new language surface.** This slice changes six severity constants in diagnostic metadata — compiler-output classification, not authored syntax. No keyword, type, operator, modifier, construct, or expression form is added, changed, or removed; `terminal` gains no new semantics (its catalog meaning `AllowsOutgoing: false`, `Modifiers.cs:242-245`, is untouched — what changes is the *consequence* of omitting it where the topology needs it). **Nothing is parked**: the slice needs no new surface, so there are no new-surface open questions from this section.

## Audience and Teachability (worked samples)

All three samples validated live via `precept_compile` (2026-07-13), conventions drawn from `samples/crosswalk-signal.precept` and `samples/hiring-pipeline.precept`.

**Process-topology: the undeclared hold.** Today this compiles "successfully" with warnings; after the flip it rejects:

```precept
precept MembershipCard

field RenewalCount as integer default 0 nonnegative

state Active initial
state Suspended
state Expired terminal

event Suspend
event Expire

from Active on Suspend -> transition Suspended
from Active on Expire -> transition Expired
```

Live result today: `PRE0119` (**warning**) — "State 'Suspended' has no outgoing transitions and is not marked 'terminal'" (plus an unrelated `PRE0158` FieldNeverSet warning, which correctly *stays* a warning under this slice). After the flip, PRE0119 is an **Error**: the author either writes `state Suspended terminal` (declaring the permanent hold — one token, the exact suppression path `graph-analyzer.md:457` itself names) or wires an exit. No shipped sample needs either edit — verified by compiling the corpus under the flip (§ Breaking changes), **not** by asserting the sample gate is green (it is not: `samples/Test.precept` fails at baseline on a pre-existing, unrelated defect — Drift ledger #5).

**The single-state precept: the highest-frequency migration shape.** The most common break in practice is neither sample above — it is the minimal scaffolding shape pervasive in test fixtures and shipped teaching snippets:

```precept
precept Counter

field Count as integer default 0

state Active initial
```

Live result today: `PRE0119` (**warning**) on `Active` — a reachable, non-terminal state with zero outgoing transitions is a structural sink regardless of how few states exist (`GraphAnalyzer.cs:117-119`). After the flip it rejects. The one-token remedy is legal and compiles clean today (probed live: `state Active initial terminal` → `HasErrors == false`, only the unrelated PRE0158 warning) — a state may be both the entry point and a declared endpoint. The alternative, when the lone state was pure scaffolding around a data-shape example, is to drop the state entirely: stateless precepts are first-class. This shape accounts for the large majority of the 32 fixture/snippet breakages enumerated in § Breaking changes, and the repair form already has in-repo precedent (`test/Precept.LanguageServer.Tests/SemanticTokensHandlerTests.cs:121` uses `state Draft initial terminal`).

**Uninhabitability: the rule no value can satisfy.** Stateless, per `samples/fee-schedule.precept` conventions:

```precept
precept FeeSchedule

field BaseFee as decimal default 25 min 0 max 100

rule BaseFee > 200 because "surcharge floor set by the 2026 fee memo"
```

Live result today: `PRE0159` (**warning**, the unsatisfiable rule — no value in [0,100] exceeds 200) alongside `PRE0164` (**error**, the default 25 violates the rule). Note what the probe demonstrates: today the *general* uninhabitability is only a warning, and the compile blocks *only because* the narrower default-check happens to fire too. A guarded variant of the same impossible rule (`when`-scoped, so PRE0164's unguarded-rule fold does not apply) would compile clean-with-warning today — an engine no configuration can ever satisfy. After the flip, PRE0159 blocks on its own — the exact case the owner's ruling names ("an uninhabitable definition with nothing downstream to borrow an Error from", companion §4).

## Semantic Rules

No reduction or typing rule changes — the slice's semantics are entirely in the diagnostic-severity layer:

1. **Flip rule.** The baseline severity of exactly six codes changes `Warning → Error`: `UnreachableState`, `StructuralSinkState`, `DeadEndState`, `RequiredStateDoesNotDominateTerminal` (process-topology); `ContradictoryRule`, `UnsatisfiableRule` (uninhabitability). Verified: all six emit today via plain `Diagnostics.Create` with no per-instance severity override (`ProofEngine.Satisfiability.cs:345/:413`; GraphAnalyzer emission sites; the codebase's only override is `GraphAnalyzer.cs:783` for a different code), so the metadata row is the single point of truth and one edit per code flips every emission.
2. **Unchanged rule.** Every other severity in the family table (companion §1) is untouched — seven ruled-Warning codes stay Warning, five already-Error codes stay Error, and the `AlwaysRejecting` construction-row per-instance Error override stays as-is.
3. **Blocking semantics (existing, reused).** `Severity.Error ⇒ Compilation.HasErrors` (`Compiler.cs:40`) `⇒ Precept.From` throws (`Runtime/Precept.cs:36-38`). No new gate is built; the flip rides the existing one.
4. **Terminal-exclusion invariant (the flip's precondition — verified against source, companion §5a re-confirmed this pass).** A declared-`terminal` state can never be flagged by either dead-end code: `StructuralSinkState` is gated on `!stateFlags[state.Name].IsTerminal` (`GraphAnalyzer.cs:113-127`); `ComputeDeadEnds` filters `!terminalStates.Contains(stateName)` (`GraphAnalyzer.cs:489-494`); terminal-ness derives solely from the Modifiers catalog (`AllowsOutgoing: false`, `GraphAnalyzer.cs:683` + `Modifiers.cs:242-245`); and the zero-terminals case runs only the structural-sink check, never reverse-BFS dead-end flagging (`GraphAnalyzer.cs:131-149`) — so a precept with no terminals cannot drown in false dead-end Errors. This slice adds the regression test pinning the invariant, because under Error severity a future regression here would block legitimate lifecycles instead of merely warning.

**Soundness preservation (principles 7 / 10 / 11).** No proof obligation, discharge path, typing rule, or evaluation path changes. Principle 7: the same proven facts now reject rather than report — strictly stronger, no new guessing (all six codes fire on structurally/provably certain defects only). Principle 10: no evaluation-surface change. Principle 11: strictly strengthened — a fault-class-free compile no longer depends on the author reading warnings. No fail-open path is introduced: a severity flip cannot cause a missed defect; the only new risk direction is *over*-blocking, which the terminal-exclusion invariant (item 4) and the corpus-under-flip check (§ Breaking changes: no sample trips a flipped code) bound.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The change lives where severity already lives: the diagnostic metadata rows in `src/Precept/Language/Diagnostics.cs` — the catalog-side single source that every emission site, the LS, and MCP derive from. No pipeline stage changes behavior; no emission site is touched.

**Reuse verification (thin wiring, verified at file:line).** Zero new mechanism:
- Blocking gate: already exists — `HasErrors` (`Compiler.cs:40`), `Precept.From` refusal (`Runtime/Precept.cs:36-38`).
- Severity derivation: emission sites inherit the metadata row (`Diagnostics.Create`); no parallel severity list exists to update.
- Tooling propagation: the language server maps severity per-*value*, not per-code (`DiagnosticProjector.cs:36-40`, `DiagnosticEnricher.cs:129-131`) — the flip reaches squiggle color/Problems-panel classification with **zero** LS edits. The MCP compile DTO carries the severity string straight through (verified live: the probes above show `"severity":"warning"` from the metadata row) and its `success` flag derives from error presence — zero MCP edits.

**Cross-component propagation.**
- Compiler pipeline: **the six metadata rows only.**
- Runtime: **None** (the existing `HasErrors` refusal covers it).
- Language server: **None** (per-value mapping, verified above).
- MCP server: **None in code** (severity and `success` both derived; `docs/tooling/mcp.md` makes no per-code severity claim — grep-verified). **But the MCP-served catalog *content* is affected**: the `SyntaxReference.cs` anti-pattern snippets (served via `precept_patterns`) and the `Diagnostics.cs` Example fixtures (served via `precept_diagnostic`) contain lone-state scaffolding that errors under the flip — those snippet strings are repaired in this slice (Inventory rows below; § Breaking changes has the enumeration). `Quickstart.cs` MinimalExamples verified unaffected (read: no lone non-terminal state).
- VS Code extension: **None** (renders LSP severities).
- Certificate/proof-ledger surface: **None** (the flipped codes carry no certificates — `certificate-steps-membership-2026-07-12.md:50`).

**Breaking changes (empirically enumerated — run-the-suite-under-flip, 2026-07-13).** A grep for the six code names is structurally insufficient as a discovery method: most of the breakage is in fixtures and shipped snippets that never mention any of the six codes (bare `state Active initial` scaffolding tripping `StructuralSinkState`). Method: the six rows were flipped in the working tree, all four test projects were run, and the tree was reverted. Results:

- **Shipped sample corpus: no sample trips any flipped code.** `samples/Test.precept` fails `Sample_CompilesClean` identically at baseline and under flip (two pre-existing PRE0078 NumericOverflow Errors, unrelated — Drift ledger #5); zero sample `.precept` edits owed. Note this is deliberately **not** the companion §3 claim "the sample gate is green" — that claim is false at HEAD; the corpus evidence for this slice is the flip-run delta, which is zero for samples.
- **`test/Precept.Tests`: 40 failures under flip vs 16 at baseline — 24 caused by the flip.** Only **5** of the 24 are severity-pin assertion sites (`GraphAnalyzerTests.cs:34/:67/:558`, `DiagnosticsTests.cs:138/:150`) — the sites a code-name grep can find. The other **19** are lone-state fixtures and shipped snippets: `RuleDefaultSatisfiabilityTests` (5), `SyntaxReferenceTests.CatalogSnippet_CompilesClean` (4 — the `SyntaxReference.cs` "Hollow draft state" and "Sub-state encoded" anti-pattern pairs, **including both "Good" teaching variants**), `TypeFamilyCoverageTests` (4), `ArgReferenceTests` (2), `Parser.OutcomesCatalogTests` (2), `NameBinder` (`BroadcastFieldTargetTests` + `StateWildcardTests`, 2).
- **`test/Precept.LanguageServer.Tests`: 13 failures under flip vs 0 at baseline** — a corpus the companion doc never measured: `SemanticTokensHandlerTests` (11), `ReferencesHandlerTests` (1), `RenameHandlerTests` (1); same lone-state fixture mechanism.
- **`test/Precept.Mcp.Tests` (67/67) and `test/Precept.Analyzers.Tests` (296/296): green under flip.**

**Repair pattern.** The dominant shape is `state X initial` scaffolding with no transitions. The one-token repair `state X initial terminal` is legal and compiles clean (probed live; in-repo precedent at `SemanticTokensHandlerTests.cs:121`); where a fixture actually exercises transitions, wire an exit instead; where the state was scaffolding around a data-shape test, dropping it for a stateless precept also works. The teaching snippets get whichever repair preserves their pedagogical point (e.g. the Hollow-draft "Good" variant should keep its constructor-pattern shape and add a route to `Approved` or mark `UnderReview` appropriately — chosen at execute so the prose explanation still matches).

**Baseline-red context for the execute pass.** 16 pre-existing `test/Precept.Tests` failures predate this slice and are not its scope: 14 `FieldRefBoundEnforcementTests`, `Sample_CompilesClean(Test.precept)`, and 1 `Parser.ParserIntegrationTests`. The execute gate is **no new failures vs this recorded baseline**, not a green suite. A supplementary grep across `test/` (94 mention sites of the six code names today) remains as a backstop for any severity-pin assertions the suite does not execute — a backstop, not the discovery method.

For external/future `.precept` files: definitions tripping the six codes stop compiling — the intended behavior.

### External architectural precedent

Not carried as a comparator-with-excerpt: this is **not a non-trivial architectural change** — it edits six constant values in an existing metadata catalog and adds one test; no component boundary, data shape, or dispatch structure changes. The *family-level* design rationale (which defects block and why) was the non-trivial decision, and it was made with full four-leg rationale per flip in the ruled companion doc (§2), whose precedent legs cite Precept's own already-Error siblings (`TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge`, `UnsatisfiableInitialState`, `DefaultViolatesRule`) — the internally consistent precedent axis for a severity question.

## Inventory of what will be built (file-level)

| Artifact | Path | Change |
|---|---|---|
| Diagnostic metadata | `src/Precept/Language/Diagnostics.cs` | Six rows `Severity.Warning → Severity.Error` (today at `:707`, `:720`, `:726`, `:747`, `:802`, `:814` — re-grounded at build time); all other rows untouched |
| Regression test (new) | `test/Precept.Tests/GraphAnalyzer/` (new file or `GraphAnalyzerTests.cs`) | Terminal-exclusion invariant: declared-`terminal` states never flagged `DeadEndState`/`StructuralSinkState`, including the zero-terminals and all-states-terminal shapes |
| Severity tests (updated) | `test/Precept.Tests/GraphAnalyzerTests.cs` (`:34`, `:67`, `:558`), `test/Precept.Tests/DiagnosticsTests.cs` (`:138`, `:150`) | Expected severities flip; add blocking assertions (`HasErrors == true` per flipped code); supplementary grep backstop (94 mention sites across `test/` today) confirms no further pins |
| Fixture repairs (flip-caused, empirically enumerated) | `test/Precept.Tests/` — `RuleDefaultSatisfiabilityTests` (5), `TypeFamilyCoverageTests` (4), `ArgReferenceTests` (2), `Parser/OutcomesCatalogTests` (2), `NameBinder` (2); `test/Precept.LanguageServer.Tests/` — `SemanticTokensHandlerTests` (11), `ReferencesHandlerTests` (1), `RenameHandlerTests` (1) | Lone-state scaffolding marked `initial terminal` (in-repo precedent: `SemanticTokensHandlerTests.cs:121`) or wired with an exit, per § Breaking changes repair pattern |
| Shipped snippet repairs | `src/Precept/Language/SyntaxReference.cs` ("Hollow draft state" + "Sub-state encoded" pairs, Bad and Good variants), `src/Precept/Language/Diagnostics.cs` (Example fixtures for the flipped codes using `state Open initial`, incl. the `UnsatisfiableRule` ExampleAfter that **errors today** — Drift ledger #6) | Snippet strings repaired so `CatalogSnippet_CompilesClean` passes and every flipped-code ExampleAfter compiles clean; new gate per AC8 |
| New severity tests | `test/Precept.Tests/` (with the flip) | Each of the six codes: emitted at Error, `HasErrors == true`; each ruled-Warning code: still Warning (full-compile assertions per the plan's acceptance frame — never `Check`/`CheckExpectingClean`) |
| Canonical doc-sync | see § Doc-update enumeration | Proposed corrections below — applied at execute after owner sign-off, not by this pass |

No new types, no new pipeline code, no catalog additions, no sample `.precept` edits. (Teaching-snippet *strings* embedded in `SyntaxReference.cs` / `Diagnostics.cs` are edited — shipped catalog content, not samples; the catalog rows' metadata shape is untouched.)

## Decisions

Most of this slice is ruled; the companion doc carries the family's four-leg rationale per flip (companion §2) and this pass does not re-decide it. One implementation-shape decision remains at this design's altitude:

### D1 — Flip the baseline severity in metadata; do not use per-instance overrides at emission sites

**Stakes: low** (both shapes produce identical author-visible behavior today; the choice affects future drift risk).

- **Rationale.** Severity is per-code truth here — the owner ruled *codes*, not emission contexts. The metadata row is the catalog-side single source every consumer already derives from; flipping it changes every emission, hover, MCP projection, and LS rendering in one edit with no possibility of a missed site. The codebase's only per-instance override (`GraphAnalyzer.cs:783`) exists precisely because *that* ruling was context-dependent (construction rows vs transition rows of one code); none of the six flips is context-dependent (companion §1: "every flip moves a code's *baseline* severity, so no code ends up split").
- **Alternatives rejected.** *Per-instance `with { Severity = Severity.Error }` at each emission site* — rejected: it duplicates the ruling across N sites (two sites alone for the dead-end pair), leaves the metadata row lying about the code's real severity to any consumer that reads metadata without an instance (hover docs, diagnostic-registry tooling, `Diagnostics.GetMeta` tests), and creates the parallel-knowledge drift CLAUDE.md's catalog rules forbid.
- **Precedent.** Every existing Error in the family is a baseline severity in the metadata row (`NoInitialState:288`, `TerminalStateHasOutgoingEdges:733`, `IrreversibleStateHasBackEdge:740`, `UnsatisfiableInitialState:867`, `DefaultViolatesRule:877`); the per-instance mechanism is reserved for genuinely split codes.
- **Tradeoff accepted.** None material — if a future ruling ever wants a context-split on one of these codes, the per-instance mechanism remains available then.

## Acceptance criteria (design-level, test-shaped)

The failing-test matrix derives from corrected canon at execute (gated on owner sign-off); these are the design-level shapes it must cover:

1. **Each flipped code blocks.** For each of the six codes: a minimal definition tripping it (full `Compiler.Compile`, never type-checker-only helpers) yields that code at `Severity.Error` and `compilation.HasErrors == true`, and `Precept.From(compilation)` throws.
2. **Each ruled-Warning code still warns.** For each of `UnhandledEvent`, `AlwaysRejecting` (transition), `StateAlwaysRejects`, `FieldNeverSet`, `VacuousRule`, `TautologicalGuard`, `UnsatisfiableGuard`: a definition tripping it compiles with `HasErrors == false`.
3. **Already-Error set unchanged.** `NoInitialState`, `TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge`, `UnsatisfiableInitialState`, `DefaultViolatesRule`, and the `AlwaysRejecting` construction-row override still emit Error.
4. **Terminal-exclusion regression (new, the §5a precondition).** (a) A declared-`terminal` state with zero outgoing edges produces neither `DeadEndState` nor `StructuralSinkState`; (b) a precept declaring **no** terminal states produces `StructuralSinkState` only for zero-outgoing states and never `DeadEndState` for the rest (the vacuous-reverse-BFS guard, `GraphAnalyzer.cs:131-149`); (c) the MembershipCard shape above errors on `Suspended` and compiles clean once `Suspended` is declared `terminal`.
5. **Standalone uninhabitability blocks.** A *guarded* unsatisfiable rule (no `DefaultViolatesRule` co-fire available to borrow an Error from) fails compilation on `UnsatisfiableRule` alone; likewise a contradictory pair on `ContradictoryRule`.
6. **Corpus untouched by the flip.** No sample trips any flipped code: `SampleCompilesCleanTests` shows **no new failures vs the recorded baseline** with zero sample `.precept` edits. (The pre-existing `Test.precept` failure — Drift ledger #5, PRE0078, unrelated — is out of this slice's scope; its disposition is parked as Open question 3. "The gate is green" is deliberately *not* this criterion, because it is false at HEAD for reasons this slice does not own.)
7. **No flip-caused failures remain.** The full suite (all four test projects) under the landed flip shows no failures beyond the recorded baseline (§ Breaking changes lists the baseline: 14 `FieldRefBoundEnforcementTests` + `Sample_CompilesClean(Test.precept)` + 1 parser test); supplementary grep across `test/` finds no remaining assertion of `Severity.Warning` for any of the six code names.
8. **Shipped snippet corpora compile.** `SyntaxReferenceTests.CatalogSnippet_CompilesClean` passes post-repair, and a new gate compiles every `Diagnostics.cs` ExampleBefore/ExampleAfter for the six flipped codes: each ExampleBefore trips exactly its own code (now at Error), each ExampleAfter compiles `HasErrors == false` (this forces the repair of the `UnsatisfiableRule` ExampleAfter, which errors today). `Quickstart.cs` MinimalExamples stay clean (verified unaffected this pass). Extending the Example gate to all diagnostic codes is optional hardening for a later pass, not this slice's obligation.

## Dependencies

- **Depends on**: nothing upstream — the flip itself needs no other slice's output.
- **Hard ordering constraint: this slice must land BEFORE or WITH Slice 0's Hole-4 relabel — "can land any time" is wrong.** Architecture §2.1 Hole 4 (`compiler-readiness-plan-2026-07-12-architecture.md:155`) makes "stop stamping false `Proved` on dead-end→dead-end rows" a **Slice-0 action**, and argues the relabel is outcome-neutral *because* dead-end = Error is in force ("the row now rejects on its own `DeadEndState` Error"). That neutrality runs in one direction only: if Slice 0 lands the relabel first, definitions with dead-end rows that compile today (dead-end still Warning) would flip from false `Proved` to `Unresolved` — which rejects — changing compile outcomes with no `DeadEndState` Error yet explaining why. The plan's sequencing sentence (`compiler-readiness-plan-2026-07-12.md:556-557`: structural-severity "can land any time after Slice 0's dead-end=Error dependency is available") states the dependency **backwards** — dead-end = Error is *this slice's* deliverable, not something Slice 0 supplies. A proposed de-garble is Doc-update enumeration #7 (owner sign-off; the plan is active canon).
- **Depended on by**: the Hole-4 relabel (above) — flipping `DeadEndState` to Error is what makes relabeling the dead-end→dead-end suppression arm to `Unresolved` outcome-neutral (the compile is already rejected).
- **Stage-0c prerequisite (already done, verified this pass)**: `diagnostic-system.md` `:191` and spec §0.6 item 7 (`:209`) already state the two-severity model — docs-before-code held.

## Doc-update enumeration (proposed corrections — NOT applied by this pass)

Per the CLAUDE.md routing table ("Diagnostic added/changed" + "Pipeline stage behavior" + cross-referencing tables). Each lands in the same execute pass as the code flip, after owner sign-off:

1. **`docs/compiler/diagnostic-system.md`** — retire the `:191` tracked-drift caveat ("where a flipped code … still emits `Severity.Warning` in `Diagnostics.cs`, that is tracked drift") once the code matches the ruled model; the surrounding two-severity prose is already correct (Stage 0c) and needs no other edit.
2. **`docs/compiler/graph-analyzer.md`** — the OQ1 override the owner's ruling authorizes (companion §5):
   - `:452` `Emit Diagnostic(DeadEndState, Warning)` → Error;
   - `:457` the "**Severity:** Warning, not error. Dead-end states may be intentional…" paragraph → rewritten to the `terminal`-declares-intent reconciliation: a permanent hold is a *declared* endpoint (`terminal`); an undeclared no-exit state is the philosophy-forbidden defect and rejects, with the `terminal` one-token fix named;
   - `:271` `DeadEndStateFact` row "emits `DeadEndState` (Warning, code 108)" → Error;
   - `:776-777` the resolved-OQ1 block's "Warning severity, not error: dead-end states may be intentional permanent-hold designs…" sentence → superseded by the ruling (annotate the resolution as overridden 2026-07-12 with the reconciliation, per that doc's resolved-question conventions);
   - execute-time grep for any further "Warning" statements on `UnreachableState`/`RequiredStateDoesNotDominateTerminal` (none found in the sections read this pass; the `:833-839` code table carries no severity column — no edit).
3. **`docs/language/precept-language-spec.md`** `:186`/`:189` — name the severity in the process-topology capability rows: `:186` "Reported as a structural diagnostic" → states that undeclared dead-ends/sinks **reject the definition**; `:189` "surfacing the relevant structural diagnostics" → notes the split (reachability/dead-end/required-coverage reject; outcome-classification hygiene reports). §0.6 item 7 (`:209`) already reads "rejects the definition" — no edit owed.
4. **`docs/compiler/README.md`** — **verified: no per-code severity column exists** (only the doc-map row naming diagnostic-system.md); no edit owed. Recorded here so the slice stub's "any severity column in `docs/compiler/README.md`" obligation is discharged as *checked, none*.
5. **`docs/tooling/mcp.md` / `docs/tooling/language-server.md`** — **verified: no per-code severity claims for the six codes** (`language-server.md:1220` cites `UnreachableState` only as a no-FixHint example, severity-agnostic); no edit owed.
6. **`README.md`** — no claim found asserting these defects block or warn at the checked surfaces; execute pass re-greps before close (the philosophy-level "cannot get stuck" claims become *more* true, not less).
7. **`docs/Working/compiler-readiness-plan-2026-07-12.md` `:551-559` (sequencing note) — proposed de-garble; the plan is active canon, so this edit needs owner sign-off.** Replace "(structural-severity has 0/77 corpus impact and can land any time after Slice 0's dead-end=Error dependency is available)" with wording that states the true direction and the corrected corpus claim, e.g.: "(structural-severity trips no shipped sample and must land **before or with** Slice 0's Hole-4 relabel — the relabel's outcome-neutrality, architecture §2.1 Hole 4, presumes dead-end = Error already in force; dead-end = Error is the structural-severity slice's deliverable)". Rationale for flagging rather than silently fixing: the sentence as written could sequence Slice 0's relabel first and silently change compile outcomes.

### Drift ledger (canonical docs this slice owns vs code)

| # | Surface | Canon says | Code says | Classification | Disposition |
|---|---|---|---|---|---|
| 1 | `diagnostic-system.md:191` + spec `:209` (ruled model) vs `Diagnostics.cs:707/720/726/747/802/814` | The six codes block | All six emit `Severity.Warning` | **Intended-not-yet-built** (docs-before-code, Stage 0c; explicitly tracked as drift in the canon itself) | This slice closes it (the code flip) |
| 2 | `graph-analyzer.md:452/:457/:271/:776-777` | DeadEndState is deliberately Warning ("may be intentional") | Matches code today, but contradicts the owner's 2026-07-12 ruling and the reconciled diagnostic-system.md | **Stale locked stage-doc decision, override authorized** (companion §5 — the ruling is the authorization; this is not silent drift) | Proposed correction #2 above, applied at execute with the flip |
| 3 | spec `:186` defines dead-end as "Non-terminal states where all outgoing rows reject or produce no-transition" | — | Code implements **two** properties: zero-outgoing sinks (`StructuralSinkState`) *and* reverse-BFS no-path-to-terminal (`DeadEndState`, which also catches states whose exits lead only to other dead ends — broader than the spec's phrasing) | **Unintended prose drift** (graph-analyzer.md `:434-457` documents the reverse-BFS semantics correctly; the spec's one-line summary lags it) | Proposed: fold the precise two-property phrasing into the same `:186` edit (correction #3) |
| 4 | `SampleCompilesCleanTests.cs:26-27` comment "current corpus is 75 samples" | — | Corpus is 77 (`ls samples/*.precept | wc -l`) | **Trivial stale test comment** (not canonical doc; noted for the execute pass) | One-line comment fix alongside the test updates |
| 5 | `samples/Test.precept` (committed 68f83fb7, message "commit this snapshot, even though no decisions are made") vs the sample gate's zero-diagnostic contract | Every sample compiles clean | Fails `Sample_CompilesClean` **today** with two PRE0078 NumericOverflow Errors (probed this pass) — also breaks companion §3's "0/77, gate is green" premise | **Pre-existing, out-of-scope defect** (stray committed scratch snapshot; unrelated to the six codes — flip-run confirms no delta) | Disposition parked as Open question 3 (owner call: delete / repair / relocate); this slice's gate is no-new-failures-vs-baseline |
| 6 | `Diagnostics.cs:820` `UnsatisfiableRule` ExampleAfter (shipped teaching content served via `precept_diagnostic`) | ExampleAfter shows the *fixed* definition | The ExampleAfter **errors today**: default 0 violates its own `rule X >= 4` (PRE0164 `DefaultViolatesRule`, probed this pass) — uncaught because `Diagnostics.cs` Examples are not compile-gated | **Pre-existing shipped-content defect**, exposed by this slice's snippet audit | Repaired in this slice's snippet pass; AC8's new Example-compile gate prevents recurrence for the flipped codes |
| 7 | `compiler-readiness-plan-2026-07-12.md:556-557` sequencing sentence | "can land any time after Slice 0's dead-end=Error dependency is available" | Direction reversed: dead-end = Error is **this slice's** deliverable; architecture `:155` makes the Hole-4 relabel a Slice-0 action premised on it | **Garbled active-canon sentence** (dependency direction inverted) | Proposed correction #7, owner sign-off |

## Open questions

No new language surface is needed, so nothing is parked as new surface. Three items for owner review:

1. **Sign-off on the proposed canonical-doc corrections (the OQ1 override text + the plan sequencing de-garble).** The Warning→Error ruling and the override of graph-analyzer.md's resolved-OQ1 Warning decision are recorded as owner-made (companion §5/§8); what this pass could not do is apply the canonical edits (autonomous contract — corrections proposed, not applied). The specific replacement prose for `graph-analyzer.md:452/:457/:271/:776-777`, spec `:186/:189`, **and the plan's direction-reversed sequencing sentence (correction #7 — the plan is active canon)** in § Doc-update enumeration awaits your review before the execute pass writes it. This is an apply-gate on already-ruled content plus one factual de-garble, not a re-opening of the ruling. Options if you want to depart: (a) apply as proposed; (b) adjust the reconciliation prose; (c) revisit the underlying flip (which would be a new ruling superseding 2026-07-12's).
2. **PRE0159 message-rendering defect (surfaced, not settled — predates this slice).** The live probe shows `UnsatisfiableRule`'s message renders the raw internal expression record — `Rule 'TypedBinaryOp { ResultType = Boolean, Span = SourceSpan { … } }' is unsatisfiable…` — instead of the rule's source text, while the sibling `DefaultViolatesRule` renders `Rule 'BaseFee > '200''` correctly. The defect exists today at Warning; this slice makes the code a blocking Error, so the illegible text becomes a rejection message a domain expert must act on. Neutral options: (a) fix the message argument in this slice (likely Tier-1 mechanical — pass the rendered source snippet as `ContradictoryRule`/`DefaultViolatesRule` already do — but it widens the slice beyond the ruled scope); (b) file it in `bugs.md` and fix in its own pass; (c) fold it into the emission-architecture work (Phase 8 witness/message richness). Canonical area checked: `docs/compiler/diagnostic-system.md` message-template conventions make no ruling on this specific argument; no locked decision conflicts with either option.
3. **Disposition of `samples/Test.precept` (pre-existing, out of this slice's scope — needs an owner call because you committed it deliberately).** The file fails `Sample_CompilesClean` today with two PRE0078 NumericOverflow Errors (unrelated to the six codes; the flip changes nothing about it). It was committed at `68f83fb7` with the message "commit this snapshot, even though no decisions are made" — it reads as a deliberately kept scratch snapshot, so this pass does not delete or alter it. Until it is dispositioned, the sample gate cannot be green, which is why AC6/AC7 are framed as no-new-failures-vs-baseline. Neutral options: (a) delete it (the snapshot served its purpose); (b) repair it so it compiles clean and keep it as a real sample (e.g. bound `AvgCost`/`Quantity` so the product is provable — this touches whatever the snapshot was preserving); (c) move it out of `samples/` (to a test fixture or scratch location) so the sample gate covers only intentional samples. Also note the same baseline carries 14 pre-existing `FieldRefBoundEnforcementTests` failures and 1 parser failure — separate defects, listed in § Breaking changes as baseline-red context, not dispositioned here.
