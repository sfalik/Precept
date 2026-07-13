# Structural-Soundness Severity — block vs. warn (owner-gated)

**status:** Draft — owner-gated
**companion to:** `compiler-readiness-plan-2026-07-12-architecture.md` (v3 lineage); prior lineage `compiler-readiness-plan-2026-06-16.md`
**date:** 2026-07-12

> A `/design`-rigor pass on one question: which of Precept's structural-soundness diagnostics must
> **block compilation (Error)** vs. **report (Warning)**, so the code honors the prevention claim
> `philosophy.md` already makes. **The owner has ruled the whole family (2026-07-12).** Two sub-families
> flip Warning → Error:
>
> - **Process-topology** — a state graph the compiler proves impossible per `philosophy.md:51`:
>   `UnreachableState`, `StructuralSinkState`, `DeadEndState`, `RequiredStateDoesNotDominateTerminal`.
> - **Uninhabitability** — a definition no valid entity can inhabit: `UnsatisfiableRule` (PRE0159) and
>   its pairwise sibling `ContradictoryRule` (PRE0155).
>
> The remaining rule/guard-hygiene and dead-declaration diagnostics stay Warning (they are advisory,
> not uninhabitability, and not a `philosophy.md`-named property). This doc records the ruling as
> owner-made on **both** sub-families and grounds each flip in four-leg rationale.
>
> **State of the flip (2026-07-12).** The canonical *prose* is already reconciled: Stage 0c updated
> `diagnostic-system.md`'s severity-model section (the old `:180` "structural-soundness diagnostics
> report" sentence) and the spec §0.6 item-7 uninhabitability contract (`:209`) to the two-severity
> model this doc rules — docs-before-code. What still **rides a Stage-1 implementation slice** is the
> *code* flip in `src/Precept/Language/Diagnostics.cs` (which still emits `Severity.Warning` for the six
> codes — tracked drift), the `graph-analyzer.md` OQ1 override, the spec's process-topology severity
> naming at `:186`/`:189`, and the terminal-detection regression test. The obligations, marked done vs.
> remaining, are in §6.

---

## 0. The tension, stated exactly

Three sources disagree about the severity of process-topology defects:

- **`philosophy.md:51`** claims the compiler *proves three properties impossible at definition time*:

  > "Every declared state is reachable — there are no lifecycle positions that exist on paper but
  > that no sequence of operations can reach. Every non-terminal state has a path forward — there
  > are no dead ends where an entity gets stuck with no way to advance. Required states are
  > guaranteed to lie on every path to completion — no entity can skip a mandatory step. …
  > The compiler proves these impossible at definition time — before any entity exists."

  `philosophy.md:61` lists the structural problems the compiler "catches": "unreachable states,
  dead-end states, type mismatches, constraint contradictions, unsatisfiable guard combinations …"

- **`diagnostic-system.md`** stated the original uniform posture — the sentence formerly at `:180`,
  since reconciled by Stage 0c (see §6). That sentence read:

  > "Fault-prevention obligations block; structural-soundness diagnostics report. … a *contradictory*,
  > *vacuous*, or *unsatisfiable* rule the compiler **reports** … these are structural rule-hygiene
  > diagnostics and stay `Severity.Warning`."

  This was the drift this pass resolved; its canonical prose has now been rewritten to the two-severity
  model recorded below, so the live disagreement that remains is `graph-analyzer.md` and the code.

- **`graph-analyzer.md:457` and `:776-777`** (the stage design doc) document dead-end as intended-Warning
  and give the rationale:

  > "**Severity:** Warning, not error. Dead-end states may be intentional in designs that use certain
  > states as permanent holds (e.g., a 'Suspended' state from which entities are never expected to
  > complete). The author can suppress the warning by adding the `terminal` modifier if the state is
  > intended as an endpoint."

The stage doc's Warning rationale (a permanent hold is intentional) is **exactly the case
`philosophy.md:51` says is impossible** ("no dead ends where an entity gets stuck"). This is the
drift the owner is resolving. See §5 for the reconciliation — the `terminal` escape hatch the stage
doc names is what preserves the legitimate "permanent hold" design *without* leaving the defect a
Warning: a permanent hold declared `terminal` is not a dead-end, and an *undeclared* dead-end is the
bug the philosophy forbids.

**Scope note.** "Structural-soundness / process-topology" here is the graph-analyzer family plus the
proof-stage rule-hygiene family. The band-split decision **D2** (`decision-index.md`; min/max/length/
count *numeric/cardinality* bounds → warn-on-proven-violation) is a **different** family and is not
touched by this pass.

---

## 1. The full family table

Every structural-soundness / process-topology diagnostic, where it is emitted, its current severity,
and the philosophy sentence (if any) that speaks to it.

The **Current severity** column is what `src/Precept/Language/Diagnostics.cs` emits *today* (this pass
changes no code); the **Ruled** column is the owner-ruled target the Stage-1 slice (§6) will apply.

| Code | Emitted at (meta) | Current severity | **Ruled** | Philosophy sentence that speaks to it |
|---|---|---|---|---|
| `NoInitialState` (PRE0044) | `Diagnostics.cs:288` | **Error** | Error (no change) | Precondition — a stateful precept with no initial state has no BFS root; every topology property is undefined without it. |
| `UnreachableState` (PRE0080) | `Diagnostics.cs:707` | Warning | **Error** | `philosophy.md:51` "Every declared state is reachable — there are no lifecycle positions that exist on paper but that no sequence of operations can reach." |
| `StructuralSinkState` | `Diagnostics.cs:720` | Warning | **Error** | `philosophy.md:51` "no dead ends where an entity gets stuck with no way to advance" (the zero-outgoing case). |
| `DeadEndState` (PRE0108) | `Diagnostics.cs:726` | Warning | **Error** | `philosophy.md:51` "Every non-terminal state has a path forward … no dead ends where an entity gets stuck." |
| `RequiredStateDoesNotDominateTerminal` (code 111) | `Diagnostics.cs:747` | Warning | **Error** | `philosophy.md:51` "Required states are guaranteed to lie on every path to completion — no entity can skip a mandatory step." |
| `TerminalStateHasOutgoingEdges` | `Diagnostics.cs:733` | **Error** | Error (no change) | `precept-language-spec.md:421` `terminal` = "no outgoing transitions" — a declared-modifier contract. |
| `IrreversibleStateHasBackEdge` | `Diagnostics.cs:740` | **Error** | Error (no change) | `precept-language-spec.md:423` `irreversible` = "no path back to any ancestor state" — a declared-modifier contract. |
| `AlwaysRejecting` — transition rows | `Diagnostics.cs:754` | Warning | Warning (no change) | Not named by `philosophy.md:51`. Dead-declaration / deliberate-prohibition hygiene. |
| `AlwaysRejecting` — **construction** rows | `GraphAnalyzer.cs:783` | **Error** (per-instance override) | Error (no change) | Implied by `philosophy.md:19` — an all-reject construction path means the entity can never be created. |
| `StateAlwaysRejects` | `Diagnostics.cs:761` | Warning | Warning (no change) | Not named by `philosophy.md:51`. Per-(state,event) hygiene ("remove the row"). |
| `UnhandledEvent` | `Diagnostics.cs:714` | Warning | Warning (no change) | Not named by `philosophy.md:51`. Dead-declaration hygiene. |
| `FieldNeverSet` (PRE0158) | `Diagnostics.cs:1141` (graph stage) | Warning | Warning (no change) | Not named as impossible — a default-only field is a valid configuration. |
| `ContradictoryRule` (PRE0155) | proof stage (`Diagnostics.cs:802`) | Warning | **Error** | Uninhabitability — `Diagnostics.cs:802` message: "no valid configuration can satisfy both". See §2 (uninhabitability flip) and §4. |
| `UnsatisfiableRule` (PRE0159) | proof stage (`Diagnostics.cs:814`) | Warning | **Error** | Uninhabitability — `Diagnostics.cs:814` message: "no valid value can satisfy it". See §2 (uninhabitability flip) and §4. |
| `VacuousRule` (PRE0154) | proof stage (`Diagnostics.cs:792`) | Warning | Warning (no change) | Not named as impossible — an always-true rule is inert (redundant), not uninhabitable. |
| `UnsatisfiableGuard` (PRE0082) | proof stage (`Diagnostics.cs:770`) | Warning | Warning (no change) | `philosophy.md:61` "catches … unsatisfiable guard combinations" — *catches*; a dead guard row is inert. |
| `TautologicalGuard` (PRE0153) | proof stage (`Diagnostics.cs:781`) | Warning | Warning (no change) | Not named as impossible — an always-true guard is inert. |
| `UnsatisfiableInitialState` (PRE0115) | proof stage (`Diagnostics.cs:869`) | **Error** | Error (no change) | Rejects at creation per spec `:213`; folds an initial-state `ensure` against defaults. |
| `DefaultViolatesRule` (PRE0164) | proof stage (`Diagnostics.cs:879`) | **Error** | Error (no change) | Proof/Error — an unguarded `rule` provably violated by field defaults; entity invalid at creation. |

(Line numbers are `Diagnostics.cs` metadata rows, confirmed by grep this pass. Emission sites in
`GraphAnalyzer.cs` are cited in §2/§5a where load-bearing.)

**Existing per-instance severity precedent.** `GraphAnalyzer.cs:783` already emits `AlwaysRejecting`
with `... with { Severity = Severity.Error }` for construction rows — one `DiagnosticCode` carrying two
severities today. This refuted the old `diagnostic-system.md` claim that "Precept has no per-instance
severity mechanism" (Stage 0c has since corrected that section to acknowledge the mechanism). It is a
working precedent that the flips below do **not** need: every flip in §2 moves a code's *baseline*
severity, so no code ends up split.

---

## 2. Block-vs-warn decision, per diagnostic

The dividing line has **two** Error branches and one Warning branch:

- **Flip to Error — process-topology:** `philosophy.md:51` guarantees the property is proven impossible.
  The three properties it names — reachability, dead-end-freedom, required-state coverage — become Error.
- **Flip to Error — uninhabitability:** a definition no valid entity can inhabit (a single rule no value
  can satisfy, or a rule pair no configuration can satisfy) is itself a prevention-worthy defect. Owner
  principle (2026-07-12): *"a definition no valid entity can inhabit → Error."*
- **Stay Warning:** advisory hygiene / dead-declaration / redundancy notes the philosophy does not claim
  to prevent, and which do **not** make the entity uninhabitable.

### Flip to Error (philosophy proves these impossible)

#### `DeadEndState` + `StructuralSinkState` → Error  *(owner-ruled)*

The owner has ruled dead-end states are an Error. `StructuralSinkState` is the strict zero-outgoing
sibling of the same property and moves with it (`GraphAnalyzer.cs:112-127` splits the one philosophy
property "every non-terminal state has a path forward" into two messages — a state with no outgoing
edges at all, and a state with edges that still can't reach a terminal).

**The framing is an evolution of the `terminal` marker, not "block dead-ends."** Originally `terminal`
was an *optional* way for an author to assert intent — "this state is meant to have no way out." This
flip makes that intent **mandatory**: every state with no path forward must be *explicitly* declared
`terminal`. A no-exit state the author did **not** mark `terminal` *is* the dead-end, and that is the
Error. The compiler is not second-guessing the process design — it is requiring the author to state
intent explicitly. This is Precept's core posture: **intent is structural, never implicit** (the same
posture that makes `terminal`/`required`/`irreversible` declarations load-bearing rather than
inferred).

- **Rationale — make terminal-intent explicit and mandatory.** `philosophy.md:51` states
  dead-end-freedom is *proven impossible at definition time*. A no-exit state is only a *bug* when the
  author did not declare it an intended endpoint; the `terminal` marker is exactly how intent is
  declared. Promoting the diagnostic to Error converts `terminal` from optional annotation to required
  one: an intended endpoint must say so, and an *un*declared no-exit state is, by definition, the
  "entity gets stuck with no way to advance" case the philosophy forbids. The rejection message is
  therefore an **intent prompt**, not a vague structural complaint — "this state has no way out — mark
  it `terminal` if this is an intended endpoint, or add a transition that advances it."
- **Alternatives rejected.** (a) *Keep `terminal` optional / dead-end a Warning, as `graph-analyzer.md:457`
  intends* — rejected: it lets a no-exit state ship in a compiled engine with its finality *inferred*
  rather than declared, violating intent-is-structural; the stage doc's "permanent holds are
  intentional" rationale is precisely the intent that should be *written down*, not assumed. (b) *A new
  "intentional hold" modifier distinct from `terminal`* — rejected: `terminal` already means "no way
  out, by design"; a permanent hold and an intended endpoint are the same structural shape, so a second
  keyword is redundant surface.
- **Precedent.** Sibling declared-topology contracts `TerminalStateHasOutgoingEdges` and
  `IrreversibleStateHasBackEdge` are already **Error** (`GraphAnalyzer.cs:232,241`; `Diagnostics.cs:733,
  740`) — Precept already blocks when a *declared* structural intent is contradicted. Making
  terminal-intent mandatory extends the same "intent is structural" rule from "don't contradict your
  declaration" to "declare your endpoints."
- **Tradeoff accepted.** Authors must now annotate intended endpoints with `terminal` — a small,
  philosophy-aligned cost (one token where the ending is clearly intended, not a redesign). The upside:
  no lifecycle can ship with an *implicit* dead end whose finality was never stated.

#### `UnreachableState` → Error

- **Rationale.** `philosophy.md:51` "Every declared state is reachable … no lifecycle positions that
  exist on paper but that no sequence of operations can reach." Reachability is one of the three
  properties said to be proven impossible to violate.
- **Alternatives rejected.** *Keep Warning* — rejected: it leaves a state the philosophy says cannot
  exist sitting in a compiled engine. Unlike dead-end, `graph-analyzer.md` gives **no** Warning
  rationale for it (`:434` just "Emit Diagnostic(UnreachableState)"), so there is no design intent to
  reconcile — only unexplained drift from the philosophy.
- **Precedent.** Same class as dead-end; `philosophy.md:51` lists the two side by side.
- **Tradeoff accepted.** A scaffolding state added before its wiring exists now blocks the build until
  it is reachable or removed. Acceptable: an unreachable state is dead surface, and the fix (wire it or
  delete it) is exactly what the philosophy wants forced.

#### `RequiredStateDoesNotDominateTerminal` → Error

- **Rationale.** `philosophy.md:51` "Required states are guaranteed to lie on every path to completion —
  no entity can skip a mandatory step." A `required` state that dominates no terminal means some path to
  completion bypasses it — the guarantee is violated. The author *explicitly declared* `required`; a
  Warning lets that declared guarantee silently not hold.
- **Alternatives rejected.** *Keep Warning* — rejected: it lets a declared-intent guarantee compile
  while false, the same failure mode `TerminalStateHasOutgoingEdges` (already Error) prevents for
  `terminal`. `graph-analyzer.md` gives no Warning rationale for this code.
- **Precedent.** The other two declared-modifier contracts (`terminal`, `irreversible`) are Error.
  `required` is the third modifier in the same family (`spec §… 421-423`) and should match.
- **Tradeoff accepted.** A `required` modifier on a state the topology doesn't actually force now blocks
  the build. Correct: either the modifier is wrong (remove it) or the graph is wrong (add the missing
  domination) — both are defects the author should resolve, not defer.

### Flip to Error (uninhabitability — `UnsatisfiableRule` + `ContradictoryRule`)  *(owner-ruled 2026-07-12)*

The owner ruled these two flip on the principle **"a definition no valid entity can inhabit → Error."**
Both describe the same defect at two arities:

- `UnsatisfiableRule` (PRE0159, `Diagnostics.cs:814`) — a **single** rule whose predicate no value can
  satisfy under the field's declared bounds (message: "no valid value can satisfy it").
- `ContradictoryRule` (PRE0155, `Diagnostics.cs:802`) — the **pairwise** sibling: two rules on the same
  field whose combined constraints no configuration can satisfy (message: "no valid configuration can
  satisfy both"). `Diagnostics.cs:815/803` already cross-link the two codes as related.

They flip **together** because they are the identical defect — declared rules with no satisfying
configuration — differing only in whether one rule or a rule-pair produces the empty solution set.
Flipping one and not the other would split an identical defect by an incidental arity.

**This override is scoped to the unsatisfiable/contradictory subset — not the whole §0.6 items 7/8 set.**
`VacuousRule` and `TautologicalGuard` stay Warning (next subsection): a redundant always-true rule does
**not** make the entity uninhabitable — every value satisfies it — so it is inert redundancy, not a
prevention defect.

- **Rationale.** An *uninhabitable* definition is a prevention-worthy defect, not mere hygiene: if the
  declared rules admit no satisfying configuration, **no valid entity can ever exist** — the compiler is
  building an engine that rejects every possible input. This is the prevention substance `philosophy.md:49`
  states ("No errors. No bugs. Business logic cannot produce an answer that violates a declared rule") and
  `philosophy.md:51` grounds ("every rule declared on the entity's data is enforced on every operation …
  No operation can produce a result that violates a declared rule — the invalid configuration is not
  reachable"): a rule set no configuration can satisfy has no valid configuration to enforce toward, so it
  should reject at compile time rather than compile to an engine that can govern nothing. Precept already
  **Errors the creation-time form of exactly this defect**: `UnsatisfiableInitialState` (PRE0115,
  `Diagnostics.cs:869`, Error) and `DefaultViolatesRule` (PRE0164, `Diagnostics.cs:879`, Error) — an
  uninhabitable rule set is the same prevention family, one arity wider. The single-rule (PRE0159) and
  rule-pair (PRE0155) cases are the same emptiness, so they carry the same severity.
- **Alternatives rejected.**
  (a) *Keep both Warning, per the pre-Stage-0c `diagnostic-system.md` posture* — rejected: that posture
  argued a contradiction which defeats a fault-prone operation is already blocked by *that operation's
  own* fault-prevention Error, so the structural diagnostic need not re-severity. The owner's counter
  (recorded): that argument only covers a contradiction *attached to* a dependent fault-prone operation;
  a rule set that is uninhabitable *on its own* — with no dependent operation to borrow an Error from —
  compiled clean under that posture and ships an engine no entity can inhabit. Uninhabitability is the
  prevention surface itself, not hygiene downstream of another check. (See §4 and the tradeoff below.)
  (b) *Flip only `ContradictoryRule` (the "constraint contradictions" `philosophy.md:61` names) and
  leave `UnsatisfiableRule` Warning* — rejected: `philosophy.md:61` uses the weaker verb "catches", but
  the deciding axis the owner chose is uninhabitability, not which philosophy verb applies; a single
  self-unsatisfiable rule is exactly as uninhabitable as a contradictory pair. Splitting them is an
  arity accident.
  (c) *Flip the whole §0.6 items 7/8 set including `VacuousRule`* — rejected: a vacuous (always-true)
  rule leaves the entity fully inhabitable (every value satisfies it); it is redundancy, not a
  prevention defect. The override is deliberately scoped to the empty-solution-set subset.
- **Precedent.** Precept already **Errors** the creation-time uninhabitability cases:
  `UnsatisfiableInitialState` (PRE0115, `Diagnostics.cs:869`, Error) rejects when an initial-state
  `ensure` cannot be satisfied by the defaults, and `DefaultViolatesRule` (PRE0164, `Diagnostics.cs:879`,
  Proof/Error) rejects when an unguarded `rule` is provably violated by the defaults. Both already treat
  "no valid entity can be created" as a hard Error. Flipping `UnsatisfiableRule`/`ContradictoryRule`
  extends the same posture from "the *default configuration* is invalid" to "*no* configuration is
  valid" — a strictly stronger uninhabitability, so if the narrower case already rejects, the broader
  one should too.
- **Tradeoff accepted.** This **overrides the documented rationale formerly at `diagnostic-system.md:180`**
  (since reconciled by Stage 0c), which argued the contradictory/vacuous/unsatisfiable class stays Warning
  because "a contradiction that defeats a fault-prone operation is blocked by that operation's own
  fault-prevention Error." The cost taken on: a pure uninhabitable rule set with **no** dependent
  fault-prone operation used to compile clean (the Warning surfaced it but did not block); it now rejects.
  Accepted because that is precisely the case the old rationale missed — an uninhabitable definition with
  nothing downstream to borrow an Error from would otherwise ship an engine no entity can inhabit, which
  the prevention claim forbids. That earlier posture was therefore not wholesale wrong — it correctly keeps
  `VacuousRule`/`TautologicalGuard` Warning — but its blanket "contradictory/unsatisfiable stays Warning"
  claim is narrowed: those two flip, the vacuous/tautological pair does not. Corpus impact is **0/77** (§3)
  — no shipped sample trips either code.

### Keep as Warning (advisory — philosophy does not claim these impossible)

#### `UnhandledEvent`, `AlwaysRejecting` (transition rows), `StateAlwaysRejects` → stay Warning

- **Rationale.** None of these is named in `philosophy.md:51`'s "proven impossible" set. They are
  *dead-declaration* and *deliberate-prohibition* hygiene: an event with no rows, or an event that
  always rejects, is a legible authoring signal ("remove it, or this is intentionally forbidden"),
  not an unsound configuration. `philosophy.md:19` explicitly blesses deliberate prohibition: "An event
  can also be explicitly forbidden — a deliberate prohibition, not just an unrouted trigger." An
  all-reject event *is* a forbidden event; making it an Error would criminalize a supported design.
- **Alternatives rejected.** *Flip to Error for consistency* — rejected: consistency with the flipped
  set is not the axis; the axis is whether the philosophy claims prevention. It does not here.
- **Precedent.** `graph-analyzer.md:536` documents `UnhandledEvent` as Warning ("provably dead
  declaration"); the construction-row carve-out already Errors the one sub-case that *is* fatal
  (`GraphAnalyzer.cs:783` — a precept that can never be constructed), which is the correct, narrower
  application of the prevention claim.
- **Tradeoff accepted.** A genuinely-stuck event mistaken for an intentional prohibition stays a
  Warning the author might ignore. Acceptable: the two are indistinguishable without author intent, and
  the philosophy explicitly protects the intentional reading.

#### `FieldNeverSet` → stay Warning

- **Rationale.** A field with no write site holds its declared default (or `omit`) forever — a *valid*
  configuration, not an unsound one. `philosophy.md:51` makes no claim that every field must be
  writable. It is an authoring signal (`Diagnostics.cs:1146` FixHint: "wire it, make it computed, or
  delete it"), analogous to an unused-variable lint.
- **Alternatives rejected.** *Flip to Error* — rejected: it would reject legitimate constant/default-
  only fields (reference data, fixed configuration), which `philosophy.md:9` explicitly supports
  ("reference and configuration entities").
- **Precedent.** `graph-analyzer.md:556` documents it as Warning.
- **Tradeoff accepted.** A field the author *meant* to wire but forgot stays a Warning. Acceptable —
  the value is still governed (it just never changes), so no rule can be violated by it.

#### `VacuousRule`, `TautologicalGuard` → stay Warning

- **Rationale.** These are rule/guard *redundancy* notes — an always-true rule that governs nothing, an
  always-true guard that narrows nothing. They do **not** make the entity uninhabitable: every value
  satisfies them, so a valid entity still exists. `philosophy.md:51` does not name inertness as a
  proven-impossible property; it is an authoring signal ("this rule/guard does nothing — remove it").
- **Alternatives rejected.** *Flip to Error with the unsatisfiable/contradictory pair* — rejected: the
  uninhabitability flip is scoped to the empty-solution-set subset, and a vacuous/tautological construct
  has the *opposite* solution set (everything), so it is inert redundancy, not a prevention defect.
- **Precedent.** `diagnostic-system.md` (as reconciled by Stage 0c) keeps the vacuous/tautological pair
  Warning; spec §0.6 item 8 (vacuous, `:210`) and item 10 (tautological guard, `:212`) stay reports —
  while item 7 (contradictory/unsatisfiable, `:209`) now reads "rejects the definition" (the
  uninhabitability flip, already applied to the spec). This pass leaves the inert-hygiene half of the
  posture intact and overrides only the contradictory/unsatisfiable half (see the uninhabitability flip
  above and §4).
- **Tradeoff accepted.** A redundant rule/guard the author meant to make load-bearing stays a Warning
  they might ignore. Low harm — an inert rule can never be violated — and the Warning still surfaces it.

`UnsatisfiableGuard` (PRE0082) also stays Warning (`spec §0.6 item 9` "reports"; a dead guard *row* is
inert — the row never fires — not an uninhabitable entity; the field can still hold satisfying values
under a different row).

`ContradictoryRule` and `UnsatisfiableRule` **flip to Error** — see the uninhabitability flip above and
§4 for the recorded override of the (now-reconciled) `diagnostic-system.md` report-only posture.

---

## 3. Corpus impact

**Zero reconciliation cost for every ruled flip — quantified.**

`SampleCompilesCleanTests.cs:58` asserts every one of the 77 `samples/*.precept` files compiles with
**zero diagnostics** — warnings *and* errors ("Strict full-clean guarantee: any diagnostic (warning or
error) on any sample fails the test", `SampleCompilesCleanTests.cs:14-15`). That green gate is itself
the quantification: `StructuralSinkState` and `DeadEndState` are Warnings *today*, so if **any** sample
had an unmarked no-exit state it would already emit one of them and fail the zero-diagnostic gate.
Because the suite is green, the count of samples needing a `terminal` added is **0 of 77**.

Concretely: of the 77 samples, 68 declare states and **61 already use `terminal`**. The 7 stateful
samples without a `terminal` marker are necessarily cyclic lifecycles (every state retains a path
forward) — if any had a no-exit state, it would fail the clean gate today. So the "make terminal-intent
mandatory" flip requires **no sample edits**: the corpus already declares terminal-intent everywhere an
endpoint exists.

The same holds for `UnreachableState` and `RequiredStateDoesNotDominateTerminal` — both are currently
Warnings, so the green corpus is by construction free of them, and the flip rejects **0** samples.

The two uninhabitability codes are identical: `ContradictoryRule` (`Diagnostics.cs:802`) and
`UnsatisfiableRule` (`Diagnostics.cs:814`) are Warnings today, so any sample tripping one would already
fail the zero-diagnostic gate. Because the suite is green, **0 of 77** samples declare an uninhabitable
rule set — the flip rejects no sample. (This is expected: a shipped example with rules no entity can
satisfy would be a broken example.)

**Where the one-token fix *would* apply (author-facing, not corpus):** a future or external `.precept`
with an intended endpoint the author forgot to mark `terminal` now gets the Error + intent-prompt
message and adds one token. That is the mechanical, low-cost fix path the flip is designed to force —
it just does not apply to the shipped corpus, which is already clean.

**Test-suite impact (not corpus, but flagged for the execute pass):** any unit test that asserts one
of the flipped codes is emitted as `Severity.Warning` will need its expected severity updated. That is
the real (small) reconciliation surface — a grep for the flipped code names across
`test/Precept.Tests/` at execute time, not a `.precept` rewrite.

---

## 4. Uninhabitability — RULED (owner, 2026-07-12): `UnsatisfiableRule` + `ContradictoryRule` → Error

This section previously flagged `UnsatisfiableRule` as a borderline case to surface, not flip. **The
owner has now ruled it (2026-07-12): both `UnsatisfiableRule` and `ContradictoryRule` flip to Error**,
on the principle *"a definition no valid entity can inhabit → Error."* The full four-leg rationale is in
§2 (the uninhabitability flip block); this section records the reasoning that carried the tension to a
ruling.

- `UnsatisfiableRule` (`Diagnostics.cs:814`): "no valid value can satisfy it" under the field's declared
  bounds — the *field itself* is uninhabitable: no value the field can hold satisfies its own rule.
- `ContradictoryRule` (`Diagnostics.cs:802`): "no valid configuration can satisfy both" — the pairwise
  sibling, uninhabitable across two rules on the field.

**The override the owner authorized.** The prose formerly at `diagnostic-system.md:180` argued the whole
contradictory/vacuous/unsatisfiable class stays Warning because a contradiction that defeats a
fault-prone operation is already blocked by *that operation's own* Error. The owner's counter (recorded):
that argument only reaches a contradiction *attached to* a dependent operation. An **uninhabitable
definition on its own** — declared rules with no satisfying configuration and no dependent operation to
borrow an Error from — compiled clean under that posture and ships an engine no entity can inhabit. That is
the prevention surface itself (`philosophy.md:49`/`:51` — a rule set no configuration can satisfy leaves no
valid configuration to enforce toward), not hygiene downstream of another check. So the flip is authorized,
and it is **scoped**: `VacuousRule` and `TautologicalGuard` stay Warning, because a redundant always-true
construct leaves the entity fully inhabitable — the override applies only to the empty-solution-set subset,
not the whole §0.6 items 7/8 set. (`diagnostic-system.md`'s canonical prose has since been reconciled to
this two-severity model — Stage 0c; see §6.)

`UnsatisfiableInitialState` (PRE0115, `Diagnostics.cs:869`) and `DefaultViolatesRule` (PRE0164,
`Diagnostics.cs:879`) are **already Error** (verified) — the creation-time uninhabitability cases. This
flip brings the *definition-wide* uninhabitability cases into line with them.

---

## 5. Reconciling the stage-doc conflict (flag, don't silently override)

`graph-analyzer.md:457` and the OQ1-resolved note at `:776-777` are a **resolved design decision** that
states DeadEndState is intentionally Warning. That is a locked stage-doc decision, and this pass cannot
override it unilaterally — the owner's dead-end=Error ruling *is* the authorization to override it.

The reconciliation preserves the legitimate design the stage doc was protecting:

- The stage doc's worry: a "Suspended"-style permanent hold is a valid design and shouldn't be rejected.
- The resolution: that design is expressed by marking the hold `terminal` (an intended endpoint) — which
  the stage doc *itself* names as the suppression path (`:457` "add the `terminal` modifier"). A hold so
  marked is no longer a dead-end. An *un*marked reachable non-completing state is exactly the
  "entity gets stuck with no way to advance" bug `philosophy.md:51` forbids.
- Net: flipping to Error loses no capability. It converts an ignorable Warning into a forced, one-token
  declaration of intent (`terminal`) — the correct posture for a prevention engine.

**No conflict with the readiness-plan locked decisions.** `decision-index.md` decision **D2** (band split)
governs min/max/length/count *numeric* bounds, a different family; none of D1–D4 or the Phase-0 gates
(GATE-A…O) rule on graph process-topology severity. This pass overrides two locked canonical
statements — `graph-analyzer.md`'s OQ1 dead-end-is-Warning note (process-topology flips) and the
contradictory/unsatisfiable-stays-Warning rationale formerly at `diagnostic-system.md:180`
(uninhabitability flips, scoped to that subset — §2/§4). The owner's ruling authorizes both. The
`diagnostic-system.md` prose override is **already applied** (Stage 0c); the `graph-analyzer.md` OQ1
override rides the Stage-1 slice (§6).

---

## 5a. Terminal-detection correctness is a precondition of the dead-end Error flip (verified)

A **terminal** state is itself structurally a dead-end *by design* — it also has "no path forward."
The *only* thing separating a legitimate terminal from a dead-end bug is the `terminal` declaration
(`spec:186`: "**NON-terminal** states where all outgoing rows reject or produce no-transition"). So a
Warning→Error flip on dead-end raises the stakes: a mis-classification would now **block compilation on
a normal lifecycle** (every entity ends somewhere), not merely warn. Terminal-detection soundness is
therefore a **precondition** of the flip. Verified against source:

- **(a) Dead-end / sink computation excludes terminal states — explicitly.**
  `ComputeDeadEnds` (`GraphAnalyzer.cs:489-494`) returns only states where
  `reachableStates.Contains(s) && !terminalStates.Contains(s) && !reverseVisited.Contains(s)` — terminal
  states are removed by an explicit `!terminalStates.Contains` predicate. `StructuralSinkState`
  (`GraphAnalyzer.cs:117-119`) is likewise gated on `!stateFlags[state.Name].IsTerminal`. A declared
  terminal can never appear in either set.

- **(b) Terminal-ness is recognized soundly — catalog-derived, not hand-coded.**
  `IsTerminal` comes from `GetStateFlags` (`GraphAnalyzer.cs:683`): `isTerminal |= !stateMeta.AllowsOutgoing`,
  reading `StateModifierMeta.AllowsOutgoing`. The `terminal` modifier sets `AllowsOutgoing: false`
  (`Modifiers.cs:242-245`) — the single catalog source. A state is terminal **iff** it carries the
  `terminal` modifier. No parallel keyword list, no per-state guessing.

- **(c) The vacuous-flag catastrophe is already guarded.** When a precept declares **zero** terminal
  states, `ComputeDeadEnds` is *not* run — `GraphAnalyzer.cs:132-149` computes DeadEndState (message B)
  only when `terminalStates.Length > 0`, because reverse-reachability from an empty terminal set would
  flag *every* state. With no terminals, only `StructuralSinkState` fires (states with literally zero
  outgoing edges). So the Error flip cannot turn "author declared no terminals yet" into a wall of
  false dead-end Errors.

**Conclusion:** terminal-exclusion is airtight, so the Error flip does not risk blocking a legitimate
terminal. The one intended tightening: a legitimate end state the author **forgot to mark `terminal`**
becomes a reachable non-terminal sink → now an Error. That is correct under `philosophy.md:51` (an end
state must *declare* its finality) and is already enforced clean across the corpus
(`SampleCompilesCleanTests` — every sample marks its endpoints `terminal`). **Recommendation:** no
separate terminal-detection fix is needed; but the execute slice that flips severity should include a
test asserting a declared-`terminal` state is never emitted as `DeadEndState`/`StructuralSinkState`,
pinning the precondition so a future refactor of `GetStateFlags` can't silently break it.

### Intent-vs-structure lens applied to the rest of the family

The dead-end/terminal boundary is the sharpest intent-vs-structure line, but the lens generalizes:

- **Unreachable state (→ Error):** there is **no declaration** that says "this state is intentionally
  unreachable" — unlike `terminal` for an intended endpoint. An unreachable state is *always*
  structure-without-intent: either scaffolding to be wired or dead surface to delete. There is no
  legitimate-intent reading to protect, which is why the Error flip is unambiguous (contrast dead-end,
  which needed the `terminal` carve-out). If a future design ever wants "intentionally orphan" states,
  that would need its own modifier — it does not exist today, so the flip is safe now.
- **Required-coverage (→ Error):** the intent *is* the declaration — the author wrote `required`. Error
  enforces the declared intent rather than overriding it; there is no un-declared legitimate case to
  protect.
- **Unhandled event / always-rejecting (stay Warning):** here intent is genuinely ambiguous at the
  structure level — an all-reject event is indistinguishable from a *deliberately forbidden* one
  (`philosophy.md:19`), so structure alone cannot justify an Error. Warning is the honest severity.

## 6. Doc-sync obligations (enumerate the touches)

Flipping the family means the uniform "structural-soundness diagnostics report" posture is no longer
true — the process-topology subset **and** the uninhabitability subset now block. **Docs-before-code
status:** the canonical *prose* touches (2, and the §0.6 item-7 half of 4) are **already applied by
Stage 0c**; the remaining touches (1, 3, the `:186`/`:189` half of 4, 5, 6) **ride the Stage-1
implementation slice**. Marked per item:

1. **`src/Precept/Language/Diagnostics.cs` — Stage-1 remaining (the live drift).** Flip
   `Severity.Warning` → `Severity.Error` at:
   - `:707` (`UnreachableState`), `:720` (`StructuralSinkState`), `:726` (`DeadEndState`),
     `:747` (`RequiredStateDoesNotDominateTerminal`) — process-topology;
   - `:802` (`ContradictoryRule`, PRE0155), `:814` (`UnsatisfiableRule`, PRE0159) — uninhabitability.
   Leave the Warning rows for `UnhandledEvent` (`:714`), `AlwaysRejecting` transition (`:754`),
   `StateAlwaysRejects` (`:761`), `FieldNeverSet` (`:1141`), `VacuousRule` (`:792`),
   `TautologicalGuard` (`:781`), and `UnsatisfiableGuard` (`:770`) unchanged. `UnsatisfiableInitialState`
   (`:869`) and `DefaultViolatesRule` (`:879`) are already Error — no change.
2. **`docs/compiler/diagnostic-system.md` — DONE (Stage 0c, docs-before-code).** The old `:180`
   "fault-prevention obligations block; structural-soundness diagnostics report" sentence has been split
   into the two-severity posture: *process-topology* structural-soundness (reachability, dead-end/sink,
   required-coverage) blocks; *uninhabitability* rule diagnostics (`ContradictoryRule`,
   `UnsatisfiableRule`) **block** — a definition no valid entity can inhabit rejects; *inert rule/guard
   hygiene* (`VacuousRule`, `TautologicalGuard`, `UnsatisfiableGuard`) still **reports**. The section also
   now flags the still-Warning code as **tracked drift** until item 1 lands, and **corrects the "Precept
   has no per-instance severity mechanism" claim** against the working `AlwaysRejecting` construction-row
   override (`GraphAnalyzer.cs:783`). No further edit owed here beyond keeping it in sync when the code
   flip lands.
3. **`docs/compiler/graph-analyzer.md` — Stage-1 remaining.** `:457`, `:776-777` (OQ1), `:452`, `:271`/`:839` (the
   "DeadEndState (Warning, code 108)" statements) and any "Emit … Warning" lines for `UnreachableState` /
   `RequiredStateDoesNotDominateTerminal` must change to Error, and the OQ1 "may be intentional"
   rationale must be rewritten to the `terminal`-declares-intent reconciliation (§5). This is the OQ1
   **override** the owner's ruling authorizes.
4. **`docs/language/precept-language-spec.md`** — split:
   - **`:186,189` — Stage-1 remaining.** "Reported as a structural diagnostic" / "surfacing the relevant
     structural diagnostics" should name the severity split for the process-topology properties
     (dead-end/sink/reachability/required-coverage = definition-rejecting).
   - **§0.6 item 7 — DONE (Stage 0c).** The contradictory/unsatisfiable verb now reads "the definition is
     **uninhabitable** … the compiler **rejects the definition**" (`:209`), and item 8 (vacuous, `:210`)
     correctly stays a report. No further §0.6 item-7/8 edit owed; items 9/10 (guard-inert/tautological)
     were already reports.
5. **Cross-referencing tables** — any severity column in `docs/compiler/README.md` or diagnostic-registry
   tables that lists these six codes (`UnreachableState`, `StructuralSinkState`, `DeadEndState`,
   `RequiredStateDoesNotDominateTerminal`, `ContradictoryRule`, `UnsatisfiableRule`) as Warning.
6. **Tests** — update expected-severity assertions for the **six** flipped codes (execute-time grep
   across `test/Precept.Tests/`), and **add a regression test pinning "a declared-`terminal` state is
   never flagged `DeadEndState`/`StructuralSinkState`"** (the terminal-exclusion precondition of the
   dead-end Error flip — §5a). Confirm `SampleCompilesCleanTests` still passes with zero `.precept`
   edits.

---

## 7. Acceptance criteria

- `Diagnostics.cs` emits `UnreachableState`, `StructuralSinkState`, `DeadEndState`,
  `RequiredStateDoesNotDominateTerminal` (process-topology) **and** `ContradictoryRule`,
  `UnsatisfiableRule` (uninhabitability) at `Severity.Error`; all six make `HasErrors == true` so no
  engine is produced for a definition that trips them.
- `UnhandledEvent`, `AlwaysRejecting` (transition), `StateAlwaysRejects`, `FieldNeverSet`, `VacuousRule`,
  `TautologicalGuard`, and `UnsatisfiableGuard` remain `Severity.Warning`.
- `UnsatisfiableInitialState` and `DefaultViolatesRule` remain `Severity.Error` (no change).
- `AlwaysRejecting` construction-row Error override is unchanged.
- `SampleCompilesCleanTests` still passes with zero changes to any `.precept` file (corpus is already
  clean — 0/77 trip any flipped code).
- `diagnostic-system.md` no longer describes the contradictory/unsatisfiable rule cases as
  reporting-only; it states the two-severity split (uninhabitable → Error; vacuous/tautological/inert →
  Warning) and drops the "no per-instance severity mechanism" claim (**this half already satisfied —
  Stage 0c**). `graph-analyzer.md`'s OQ1 "may be intentional" rationale is replaced by the
  `terminal`-declares-intent reconciliation (Stage-1 remaining).
- A new test asserts each flipped code produces `Severity.Error` and suppresses engine production, plus
  the terminal-exclusion regression test (§5a).

---

## 8. Owner decisions recorded / requested

All decided by the owner on **2026-07-12**. Corpus impact **0/77** — every sample compiles clean.

- **Ruled → Error (process-topology, `philosophy.md:51` "proven impossible"):** `UnreachableState`,
  `StructuralSinkState`, `DeadEndState`, `RequiredStateDoesNotDominateTerminal`. (`StructuralSinkState`
  is the zero-outgoing sibling of the dead-end property.)
- **Ruled → Error (uninhabitability, "a definition no valid entity can inhabit → Error"):**
  `UnsatisfiableRule` (PRE0159) and `ContradictoryRule` (PRE0155). Scoped override of the report-only
  posture formerly at `diagnostic-system.md:180` (prose since reconciled — Stage 0c) — see §2/§4.
- **Ruled to stay Warning:** `UnhandledEvent`, `AlwaysRejecting` (transition), `StateAlwaysRejects`,
  `FieldNeverSet`, `VacuousRule`, `TautologicalGuard`, `UnsatisfiableGuard`.
- **Confirmed already Error (no change):** `UnsatisfiableInitialState` (PRE0115), `DefaultViolatesRule`
  (PRE0164), `NoInitialState` (PRE0044), `TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge`.
- **Override authorized:** flipping dead-end overrides the locked `graph-analyzer.md` OQ1 Warning
  decision (Stage-1 remaining), and flipping the two rule codes overrides the Warning posture formerly at
  `diagnostic-system.md:180` (the contradictory/unsatisfiable half only; prose override already applied —
  Stage 0c). The owner's ruling authorizes both; §5 preserves the design OQ1 protected, and §2/§4 record
  the owner's counter to that report-only rationale.
