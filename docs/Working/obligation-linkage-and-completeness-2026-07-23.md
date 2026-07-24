---
status: Draft — UNLOCKED 2026-07-23 by adversarial review; see § Review record. Was `Locked 2026-07-23` for a few hours; do not build against it.
phase-target: TBD — precedes the constraint-obligation build (matrix § Storage names constraint-obligation design time as the trigger for these catalog entries)
comparable-systems-research-status: strong
sources-consulted:
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — the 2026-07-21 citation duty ruling and its structural-fact refinement
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — the three-leg mention set and the site-identity ruling
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — write plan, discharge contract, premise classes
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Amendments — the two amendment classes
  - docs/Working/certificate-steps-membership-2026-07-12.md Decision 1 — the eleven certificate premise kinds
  - docs/compiler/proof-engine.md § Obligation Generation Contract, § Catalog-Driven Obligation Instantiation, § ProofRequirement Catalog DU
  - docs/language/precept-language-spec.md § 0.1 — the eleven design principles
  - docs/language/precept-language-spec.md § 3A.4 — operation execution order and the eight in-operation writers
  - docs/philosophy.md — core commitments
  - research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md — Event-B, Dafny, refinement and dependent types on the assume-vs-discharge line
  - src/Precept/Language/ProofRequirement.cs — the ProofRequirement DU and ProofSubject
  - src/Precept.Analyzers/Precept0027DiagnosticEmissionCoverage.cs — the in-tree catalog-vs-code coverage gate
  - src/Precept.Analyzers/DiagnosticCoverageScanner.cs — how that gate computes its expected set
  - precept_compile probe (2026-07-23, HEAD) — the OrderTotals witness and the modifier/rule asymmetry probe
  - docs/Working/bugs.md — BUG-033, BUG-035, BUG-036
  - src/Precept/Language/ActionKind.cs — the fifteen action kinds, checked against the § 3A.4 writer table
  - src/Precept/Language/Actions.cs — ClearApplicable, which gates `clear` on the Optional modifier for scalars
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Constraint kinds — the five constraint forms and their obligation shapes, incl. edge-triggered entry/exit ensures
---

# Linkage and completeness for constraint establishment and preservation

## Review record — why this is no longer locked (2026-07-23)

Locked and then reviewed adversarially the same day. Three findings, recorded rather than patched.

**1. The admissibility condition has no rejection power.** Both its conjuncts are true by construction. "Every check in the coverage record is discharged" can never be the reason for a rejection, because an undischarged obligation already rejects the file on its own under no-deferral. And "the proof's citation equals the coverage record" is trivially satisfied, since the record is a function of the fact and the same engine computes both sides — there is no independently derived second value to disagree with. So the citation duty as encoded here is a serialization annotation with no accept/reject consequence, and all the rejection power in the design lives in the completeness check.

Three things follow. Decision 1 — which fork to take on the encoding — was immaterial, because the two encodings agree in the only way that matters: neither can fail. Acceptance criterion 7 describes a state the pipeline cannot reach. And the Goal, that `OrderTotals` must not compile, is discharged entirely by the companion design; nothing here is load-bearing for it.

**2. `Expected(C)` misses two of the four mutating operations.** Its preservation leg quantifies over `Handlers`. The update patch on an editable field and `Restore` are both writers § 3A.4 enumerates, and neither belongs to a handler — the spec lists `Create`, `Fire`, `Update` and `Restore` as peer operations, and the matrix names the editable-field door as a first-class write-site category with its own case shape. So a rule over an editable field gets no expected preservation check for the edit door, the set comparison passes, and the file compiles with the rule breakable through `Update`. That is the failure class this document exists to close, reintroduced through its own quantifier.

The same section also contradicts itself four lines apart on whether a cross-field modifier mentions one field or two. The matrix is explicit that it is two. Verified at HEAD: `field Floor as decimal max Ceiling` with a handler writing `Ceiling` produces **zero** proof obligations.

**3. Both witnesses used to justify Decisions 3 and 5 are misattributed.** The qualifier-modifier hole is offered as proof that the per-file check is blind to a missing category — but it is not a missing write-site category. The site is present and demonstrably obligated for the `min` spelling on the same line of the same file, so the per-file set comparison would catch it. And BUG-033, offered as the instance motivating an independently derived expected set, is the *counterexample*: two structurally independent consumers, in different pipeline components, made the identical mistake. This document's own falsifier 4 says that condition means the independence is procedural fiction. It was met before the document was written.

**Also found**: Decision 4 contradicts a locked ruling without quoting it — the matrix rules that a proof consuming a written value names the write it came from, while the decision places `PriorAction` in the group needing no record, and acceptance criterion 12 still demands a test for that situation. `DeclaredDefault` is exempted on grounds ("it *is* an establishment fact") that the 2026-07-21 ruling treats as the definition of a value-fact. The `DeclaredPresence` split enumerates `clear` and stops, without reaching the `omit` reset, on which the matrix carries an unresolved canon conflict. And "the eleven existing requirement kinds" is wrong — the discriminated union has thirteen.

**What survived review**: Decision 2 (two catalog kinds rather than one parameterized kind) is sound and correctly grounded, with the Event-B precedent quoted faithfully. Decision 6 (compiler-internal severity) is right and its dead-rows precedent applies. Set equality rather than containment is correct under the exact-power contract, with the reason stated rather than asserted. The premise partition is total over the eleven certificate premise kinds, even though two placements are wrong. And § The cost's correction to the matrix's own rationale is accurate — it identifies that the citation duty relocates the enumeration rather than removing it, and stops one step short of finding 1.

## Goal

When this is done, a proof that leans on a fact an operation could break cannot be accepted unless the checks that make that fact true, and keep it true, are named, present, and passing — and the compiler independently knows the full list of checks that should exist, so a missing one fails the compile instead of passing silently.

The `OrderTotals` file in § The problem, which compiles clean today while resting a division on a rule nothing enforces, is the test: it must not compile.

## Scope

**In scope.**

- The *linkage*: how a proof records which checks a fact rests on, and what a reader or a re-checker does with that record.
- The *completeness check*: how the compiler knows the full set of checks that should exist for a file, computed without asking the code that creates them.
- Both, covering all four places a proof gets a fact that an operation can break: field modifiers, rules and ensures, qualifiers whose value comes from a field, and a value written earlier in the same operation.
- The catalog entries the above requires, and what the proof certificate has to carry.

**Out of scope.**

- The rule and ensure checks themselves — what they prove, over which sites, and how they discharge. That is the next design pass. This one says what a reference to such a check looks like and how the compiler counts them; it does not build them.
- The independent re-checker that would replay a certificate. The certificate format work already deferred it, and nothing here changes that.
- Diagnostic wording beyond the two new messages this design introduces.

**Deferred to future.**

- Repairing the specific holes this machinery makes visible — the qualifier modifiers that create no write-site check (`nonzero`, `positive`, `nonnegative`), BUG-033's missed `into` target, BUG-035's missing staleness check on a qualifier's source field. This design makes them fail loudly; fixing each is its own work.
- Whether the completeness check can be relaxed for files where no proof consumes any breakable fact. It cannot be relaxed safely without an argument nobody has made yet.

## The problem

Two `.precept` files, both compiled at HEAD on 2026-07-23 through `precept_compile`.

The first shows that Precept already knows how to do half of this, for one spelling only:

```precept
precept BoundVsRule

field Total as decimal default 0 max 1000
field Other as decimal default 0

rule Other <= 1000 because "The other total is capped at the same ceiling"

event Bump(N as decimal)

on Bump
    -> set Total = Bump.N
    -> set Other = Bump.N
```

`Total` produces two checks — one that its default fits inside `max 1000`, and one at the write site, which fails and rejects the file. `Other` produces **none**. Same ceiling, same write, one written as a modifier and one as a rule.

The second is the failure this design exists to stop:

```precept
precept OrderTotals

field ItemCount as integer default 1
field UnitCost as decimal default 1.0
field Total as decimal default 0.0

rule ItemCount > 0 because "An order with no items has nothing to price"

event Reprice(NewCount as integer, NewCost as decimal)

on Reprice
    -> set ItemCount = Reprice.NewCount
    -> set UnitCost = Reprice.NewCost
    -> set Total = Reprice.NewCost / ItemCount
```

At HEAD this compiles with **zero diagnostics**, and the engine reports the divisor check as `Proved` by the strategy that reads rules as facts. The rule `ItemCount > 0` is used to prove the division safe. Nothing checks that the rule is true: `set ItemCount = Reprice.NewCount` writes it from an argument with no constraint at all. Fire `Reprice(0, 5.0)` and the engine divides by zero.

This is the shape the matrix calls out: a proof that is individually valid and replayable, resting on a fact that was never earned. It is silent, it is spread across the file, and it looks exactly like success. A certificate cannot catch it, because a certificate records work that was done and this is work that was never attempted.

Your 2026-07-21 ruling chose the fix: a proof may not use such a fact unless it **names the check that established it and every check that preserves it**, each of them passing. This design builds that.

## Two words used throughout

**A fact an operation can break** — the matrix calls this a *value-fact*. Example: `ItemCount > 0`. Some operation in the file can write `ItemCount` and make it false, so the fact has to be earned somewhere and kept true everywhere.

**A fact fixed by the declaration** — the matrix calls this *type-structural*. Example: `field Price as money in 'USD'`. No operation anywhere can change a field's declared currency; the language has no construct that retypes a slot. Your 2026-07-21 refinement says these cite their declaration and stop — there is nothing to earn and nothing to keep.

The whole design turns on which of the two a fact is, and the test is behavioural, not about how the fact is spelled: *can some operation establish or break it?*

## Philosophy Alignment

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | The `OrderTotals` file is a rule declared and not enforced — precisely the gap between declaring a rule and having it hold on every path that `philosophy.md:49` names as the rule-owner's real pain; refusing the file closes it structurally. | N/A | Files that compile today stop compiling; see § The cost. |
| 2. One file, complete rules | Y | The expected set of checks is computed from the one `.precept` file's own declarations — the mention set and the write-site categories — with no external input (spec § 0.1 #2, "All proof facts … derive from a single file"). | N/A | N/A |
| 3. Deterministic semantics | Y | The expected set is a deterministic function of the file: same file, same set, same verdict; the ordering rule in Decision 3 makes the recorded set byte-identical across runs. | N/A | N/A |
| 4. Full inspectability | Y | The reason a fact was allowed becomes visible: a proof shows which checks earned it, and the coverage record shows the whole set (spec § 0.1 #4, "what the engine could not prove must all be surfaceable"). | N/A | Certificates and the proof output get larger. |
| 5. Keyword-anchored readability | N | N/A — nothing here is authored syntax; no token, keyword, or layout changes. | N/A | N/A |
| 6. Explicit domain meaning over primitive convenience | N | N/A — no type or domain-meaning surface is touched. | N/A | N/A |
| 7. Compile-time-first static checking | Y | The completeness check runs at compile time over an enumerable per-file product and rejects rather than guesses (spec § 0.1 #7, "proves what it can, rejects what it can prove invalid, and does not guess"). | N/A | Compile does strictly more work; see § Operational dimensions. |
| 8. Approximation honesty | N | N/A — no approximate/exact boundary is touched. | N/A | N/A |
| 9. Mandatory rationale (`because`) | N | N/A — no constraint surface changes; existing rationale text is carried unchanged in citations. | N/A | N/A |
| 10. Totality | Y | `OrderTotals` is a live counterexample to "a precept that compiles without diagnostics has no unproven arithmetic faults" (spec § 0.1 #10) — the division is unproven and the file compiles. Refusing it restores the principle. | N/A | N/A |
| 11. Static completeness | Y | Same counterexample, at the level of the compiler-to-evaluator bridge (spec § 0.1 #11): today an evaluator divide-by-zero path is reachable from a clean compile. | The principle says every fault class "is linked to a compiler diagnostic that prevents it"; that link is currently broken for any fault proved from a rule. | N/A |

**Companion commitments.** *Stateless-first-class*: nothing here needs a state machine — the mention set, the write-site categories, and the coverage record are all defined over fields and handlers, and a stateless precept exercises every part of the machinery. The only state-dependent piece is the state-entry establishment site, which simply has no instances in a stateless file. *Domain-expert-primary-author*: the author-facing surface is one new diagnostic (§ Audience and Teachability) that names a rule, a field, and a write site in the author's own vocabulary; the citation records themselves are compiler bookkeeping the author never writes.

## Language Design Grounding

*Scope note*: this design introduces no authored syntax — no token, keyword, construct, modifier, type, operator, accessor, or expression form. It does add catalog member names and certificate content, which reach authors through diagnostics, hover, and MCP output, so the grounding is carried anyway, following the precedent set by the certificate-format design for the same reason.

**General language design.** The question this design answers is one every verification system with mutable state has had to answer: when a proof uses a declared property of a value, what has to be true about that property for the proof to count?

The surveyed answer is uniform and is recorded in `research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md`. Event-B is the closest comparator because its proof-obligation generator ships a soundness argument and draws exactly this line. A purely typing invariant produces nothing:

> "At this stage the model results in no proof obligations since the invariant inv1 is nothing stronger than a typing constraint."

An invariant over a mutable variable produces establishment plus one preservation obligation per mutating event:

> "The resulting model now gives rise to 6 proof obligations in total; 3 of these are to verify that the initialisation establishes invariants inv2 to inv4 and 3 are to verify that the register event maintains invariants inv2 to inv4."

Dafny gives both poles inside one language — a subset type is checked once at the assignment boundary and then assumed, a `newtype` re-imposes the predicate on every operation — and the survey's reading is that neither is "assume forever with no establishment": both have a real establishment site.

What Precept takes: the establish-plus-preserve-per-mutating-site shape, and the refusal to manufacture obligations for facts nothing can change. What Precept diverges on, and this is the substantive divergence, is **who guarantees the generator was complete**. Rodin's guarantee comes from a once-proved meta-theory about its obligation generator. Precept cannot borrow that: it has no such meta-theory, it has a verified instance of its generator being wrong (the qualifier modifiers that produce no write-site check), and the matrix's 2026-07-20 constraint requires the expected set be derived without asking the generating code. So Precept adds a per-file cross-check that no surveyed system performs. § Architecture Grounding defends that addition.

`research/language/README.md` has no domain entry for proof-obligation generation or certificate structure — the language-research index covers authored surface. The architecture-side survey above is the on-point in-tree research, and it is cited rather than duplicated.

**Precept-specific application.** Principles 10 and 11 are the ones directly at stake: both assert that a clean compile implies no runtime faults, and `OrderTotals` falsifies both today. Principle 2 constrains where the expected set may come from — the single file, plus the catalog, which `catalog-system.md` establishes as the language specification in machine-readable form rather than an external input. Principle 3 constrains the recorded set to be deterministic. No deliberate exclusion in the spec is touched: the spec has no statement about obligation-set completeness at all, which is the gap the 2026-07-20 note found.

## Audience and Teachability

*Scope note*: the author-visible surface is one new diagnostic. The rest is compiler bookkeeping.

**Worked example.** The domain expert writes an ordinary priced-order file and gets rejected:

```precept
precept OrderTotals

field ItemCount as integer default 1
field UnitCost as decimal default 1.0
field Total as decimal default 0.0

rule ItemCount > 0 because "An order with no items has nothing to price"

event Reprice(NewCount as integer, NewCost as decimal)

on Reprice
    -> set ItemCount = Reprice.NewCount
    -> set UnitCost = Reprice.NewCost
    -> set Total = Reprice.NewCost / ItemCount
```

The fix is one word — constrain the argument the rule's field is written from — and it is the fix the diagnostic names:

```precept
event Reprice(NewCount as integer positive, NewCost as decimal)
```

**Error message.** The plausible misuse is exactly the above: declaring a rule and then writing one of its fields from an unconstrained input.

```
PRE0165: The rule 'ItemCount > 0' is used to prove line 14 safe, but nothing keeps
         it true. Line 11 sets 'ItemCount' from 'Reprice.NewCount', which carries
         no constraint, so after that line the rule may be false.
         Constrain the argument — 'NewCount as integer positive' — or add a guard
         on this row that establishes 'Reprice.NewCount > 0'.
```

The wording serves the domain expert because it names the two things they wrote — the rule and the line that breaks it — and states the repair in their own vocabulary, rather than reporting an internal condition about obligation sets. It never uses the words obligation, premise, citation, or discharge.

**10-minute teaching path.**

1. `docs/language/precept-language-spec.md` § 2.4 — constraint modifiers on fields and arguments (3 min).
2. `samples/loan-application.precept` — a file where arguments carry constraints and rules rest on them (3 min).
3. The `PRE0165` entry in `docs/compiler/diagnostic-system.md` — the message, and the two repairs (2 min).
4. `docs/language/precept-language-spec.md` § 0.7 — why a constraint on an incoming value is what makes a rule keep holding (2 min).

## Semantic Rules

Three definitions and one admissibility condition. Notation is deliberately light; each line is also stated in words.

**What a derivation consumes.** For a derivation `π` that discharges some obligation, `Facts(π)` is the set of facts `π` uses that some operation in the file could establish or break. Facts fixed by a declaration are not members — by the 2026-07-21 refinement they are context, not consumed premises.

**The expected set.** For a constraint `C` (a `rule`, or any of the four `ensure` forms) the set of checks that must exist is

```
Expected(C)  =  { Establish(C, e) | e ∈ EstablishmentSites(C) }
             ∪  { Preserve(C, h)  | h ∈ Handlers,  mentions(C) ∩ writes(h) ≠ ∅ }
```

in words: one establishment check at each site where the constraint first has to be true, and one preservation check for each handler whose write plan touches any field the constraint mentions.

`mentions(C)` is the matrix's three-leg mention set — the constraint's condition, its activation guard, and, transitively, the inputs of any computed field it mentions, with a desugared cross-field modifier mentioning both fields. `EstablishmentSites(C)` is construction for a `rule`, and construction plus every entry into the anchor state for a residency `ensure`. `writes(h)` is the union of the fields written by the writers § 3A.4 enumerates for that handler; the spec lists eight, and the list is itself an input this design does not verify (see § Open questions).

Preservation is keyed to the **handler**, not to the individual write, per the 2026-07-20 whole-write-plan ruling: the runtime mutates a working copy committed atomically (`precept-language-spec.md:1967`), so mid-handler states are not reachable configurations.

For a field modifier `m` on field `F` the same shape applies with `mentions` replaced by `{F}`; for a qualifier fact whose value comes from a field, `mentions` is that fact's provenance source fields, which the matrix's transport rule already defines as `provdeps`.

**Completeness.** Let `Minted(C)` be the set of checks the compiler actually created for `C`. The file is complete when

```
∀ C ∈ Constraints(file) .  Minted(C) = Expected(C)
```

Set equality, not containment, in both directions: a missing check is the hole this design exists to close, and a surplus check means the minting rule and the expected-set rule disagree, which is equally a defect — under the exact-power contract a compiler that rejects more than the definition licenses is nonconforming just as one that accepts more is.

**Admissibility.** A derivation `π` is admissible only when

```
π admissible  ⟺  ∀ φ ∈ Facts(π) .  cites(π, φ) = Coverage(φ)
                                  ∧ every check in Coverage(φ) is discharged
```

where `Coverage(φ)` is the single per-file record for the constraint or modifier that φ comes from, holding `Expected(·)` and each member's disposition. In words: a proof may use such a fact only by pointing at that fact's coverage record, and only when every check in it passed.

**Soundness preservation.**

*Principle 7 (compile-time-first).* The check adds no guessing: `Expected(C)` is a finite product over declarations in the file, computed by enumeration, and inequality rejects. Nothing is deferred.

*Principle 10 (totality).* This is the principle currently violated, not merely threatened. Today a division can be proved from a rule nothing enforces, so a clean-compiling file has an unproven arithmetic fault. After this design, the derivation that proves the division is inadmissible unless the rule's preservation checks exist and pass, so the divisor's safety no longer rests on an unearned fact.

*Principle 11 (static completeness).* The bridge from compiler to evaluator holds because the two ways it could break are both closed: a fact used with no establishing check makes the derivation inadmissible, and a fact whose preservation set is short of `Expected` fails the completeness check and rejects the file. What remains outside the claim is the correctness of `Expected` itself — it is only as good as the write-site enumeration in § 3A.4, which is asserted rather than enforced, with one verified error already recorded as BUG-033. That dependency is named rather than papered over, and § Open questions carries it.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Three pieces, three layers, and the split is the point.

The *establishment and preservation checks* are catalog metadata — two new `ProofRequirement` subtypes, following the rule the proof-engine doc already states: "The proof engine does NOT maintain its own list of what needs to be proved." They belong beside the eleven existing requirement kinds, not in engine code.

The *expected-set rule* is catalog metadata too, and this is the load-bearing placement. The three legs of the mention set and the eight write-site categories are declared facts about the language, and the expected set is a function of them. Putting the rule in the catalog is what makes the expectation independent of the minting code: the minting path reads the catalog to decide where to create a check, and the completeness path reads the same catalog to decide where one should exist, but they are two derivations from one declaration rather than one derivation checking itself. If instead the expected set were computed by re-running the minting walk, it would agree with itself and see nothing — the circularity the 2026-07-20 note names.

The *completeness check* is a proof-engine output, not a new pipeline stage. It consumes the semantic index and the proof ledger, both of which the proof engine already holds, and produces a verdict. A stage boundary would buy nothing and would need its own artifact type.

There is a fourth piece at a different level entirely, and conflating it with the third is a mistake worth naming. The per-file check answers *did this file get the checks it should have*. It cannot answer *is a whole category of check wired at all* — if the compiler has no code path that ever creates a preservation check for a residency ensure, then both the minting walk and the expected-set walk would have to disagree for the per-file check to notice, and they will not, because the expected-set walk is derived from the catalog and the catalog entry would simply be absent. That question is about the compiler's own source, and it belongs in a Roslyn analyzer, following the existing `PRECEPT0027` gate, whose descriptor reads:

> "Every DiagnosticCode member must have at least one emission site in the pipeline (Diagnostics.Create, CIDiagnosticCode assignment, or ProofEngine dispatch), or be listed in the Gate 1 allow-list with a tracking comment."

The same shape applies here: every requirement kind and every write-site category the catalog declares must have at least one live creation site in the pipeline.

**Cross-component propagation.**

- *Runtime (parser, type checker, evaluator, diagnostics)*: the type checker gains no new stamping responsibility in this pass — the two new requirement subtypes are created by the constraint-obligation work that follows. Diagnostics gain two codes (§ Inventory). The evaluator is untouched. The parser and lexer are untouched.
- *Tooling (syntax highlighting, completions, hover, semantic tokens)*: hover over a constraint gains the coverage record — which handlers must preserve it and whether each passes. No highlighting, completion, or semantic-token change; nothing authored changes.
- *MCP (vocabulary, DTOs, tool output)*: `precept_compile`'s obligation projection gains the coverage record and the citation field; `precept_proofs` gains the same. Both are additive fields on existing DTOs. The two new requirement-kind names enter the MCP vocabulary through the existing catalog formatter with no formatter change.

**Breaking changes.** Yes, three, all internal to a pre-release project with no external consumers.

1. The certificate format gains a required citation field. Certificates issued before this change do not replay. The matrix's amendment protocol already anticipates this and calls it a major definition version.
2. `ProofRequirementKind` gains two members. The analyzer that enforces one-to-one correspondence between the enum and the DU (`PRECEPT0026`) makes this a compile-time-checked change rather than a silent one.
3. Files that compile today stop compiling. This is the substance of the change, not a side effect; § The cost states it.

### External architectural precedent

The architectural problem is: *how does an obligation-generating verifier guarantee it generated all the obligations?*

Event-B's Rodin answers it with a once-proved meta-theory about the generator, and the model's own closed structure. The number of obligations is a function of the model — one initialisation obligation per invariant, one preservation obligation per (invariant, mutating event) pair — which is the same product shape as `Expected(C)` above. Precept takes the product shape directly.

Precept does **not** take the once-proved-generator guarantee, and the divergence is deliberate. Three reasons. First, Precept has no such meta-theory and writing one is not on any plan. Second, Precept has a verified instance of its generator being wrong in exactly this way: a field declared `nonzero`, `positive`, or `nonnegative` produces no write-site check while `min`/`max` on the same field does, so the only spelling that discharges a downstream proof is the one nothing enforces. A meta-theory would not have caught that; it would have been a meta-theory about a generator that has this hole. Third, the matrix's own 2026-07-20 constraint requires the expectation come from somewhere other than the minting code, which is a stronger requirement than Rodin meets.

The honest statement is therefore: **the per-file completeness cross-check is novel relative to the surveyed comparators.** The survey found no system that computes an independent expected-obligation set and compares it against what its generator produced; the surveyed systems all rely on generator correctness. The novelty is warranted because Precept's guarantee is stronger than theirs in the one relevant respect — a Precept file that compiles is claimed to have *no* unproven fault, which makes a single missed obligation a breach of the headline promise rather than an unproved lemma the user can see is unproved.

What makes the novelty affordable rather than reckless is that Precept has an in-tree precedent for the *technique* at a different level. `PRECEPT0027` already computes an expected set from a catalog (every `DiagnosticCode` member) and compares it against what the pipeline actually does (every emission site the scanner finds), with an explicit allow-list for the known gaps. The scanner's own header states the three emission shapes it must recognise, including the catalog-mediated ones — which is the same lesson this design has to absorb: an expected-versus-actual check is only as good as its model of what counts as "actual". That precedent is why the design puts the expected-set rule in the catalog and the category check in an analyzer, rather than inventing both from scratch.

## Inventory of what will be built

**Catalog.**

- `ProofRequirementKind.ConstraintEstablishment` and `ProofRequirementKind.ConstraintPreservation` — two new members (`src/Precept/Language/ProofRequirementKind.cs`).
- `ConstraintEstablishmentProofRequirement` and `ConstraintPreservationProofRequirement` — two new sealed subtypes of the `ProofRequirement` DU (`src/Precept/Language/ProofRequirement.cs`), each carrying the constraint identity, the site, and the mentioned-field set the site was derived from.
- `ProofRequirementMeta` entries for both, each naming its diagnostic code (`src/Precept/Language/ProofRequirements.cs`).
- `WriteSiteCategories` — a new catalog declaring the eight writers § 3A.4 enumerates, each with the phase it runs in and how its target set is read. This is the catalog the expected-set walk reads and the analyzer checks against. New file `src/Precept/Language/WriteSiteCategories.cs`; inventory entry in `docs/language/catalog-system.md`.
- `MentionSetLegs` — the three legs as catalog metadata rather than prose, so the expected-set walk and any future consumer derive from one declaration. Same file or a sibling; decided at build time.

**Supporting types.**

- `CoverageRecord` — per (constraint or modifier, file) the expected set, the minted set, and each member's disposition (`src/Precept/Pipeline/CoverageRecord.cs`).
- `CertificatePremise` gains a required `Coverage` reference on the value-fact-bearing kinds (`DeclaredBound`, `DeclaredQualifier` when field-sourced, `RuleCondition`, `EnsureCondition`, `DeclaredPresence`), and the field is absent by construction on the rest (`src/Precept/Language/CertificatePremise.cs`).

**Pipeline.**

- `ProofEngine.Completeness.cs` — the expected-set walk and the set comparison, producing coverage records and the completeness verdict.
- `ProofLedger` gains the coverage records so downstream consumers read them without recomputation.

**Diagnostics.**

- `PRE0165` — a constraint is used as a fact but a write can break it (author-facing; the message in § Audience and Teachability).
- `PRE0166` — the obligation set for this file is incomplete (compiler-internal; see Decision 6).

**Analyzer.**

- `Precept0030ObligationCategoryCoverage` — every requirement kind and every write-site category the catalog declares has at least one live creation site in the pipeline, with an allow-list carrying a tracking comment per known gap, following `PRECEPT0027`.

**Tooling.**

- `tools/Precept.Mcp/Dtos/CompileToolDtos.cs` — coverage record projection on the obligation DTO.
- Language server hover for a constraint — the coverage record summary.

**Tests.**

- `test/Precept.Tests/ObligationCompletenessTests.cs` — the expected-set walk against hand-computed products; the `OrderTotals` rejection; the surplus direction.
- `test/Precept.Tests/CoverageCitationTests.cs` — a derivation citing a coverage record with an undischarged member is inadmissible.
- `test/Precept.Analyzers.Tests/Precept0030Tests.cs` — a requirement kind with no creation site is flagged; an allow-listed one is not.
- Corpus run: every file in `samples/` reports a complete obligation set, or is listed with the reason it does not.

## Decisions

### Decision 1: A proof cites one coverage record per fact, not a list of individual checks

**Stakes**: high

- **Rationale**: the same constraint is consumed by many proofs. A rule mentioning three fields, written by eight handlers, and consumed at twelve fault sites needs its preservation set stated once, not twelve times — under a per-proof list the file carries ninety-six entries that must all agree, and "must all agree" is a new consistency obligation nobody checks. One record also gives the completeness check exactly one place to attach per constraint rather than one place per consuming proof. And it matches the certificate format Precept already locked, where premises are recorded once and steps refer to them as children rather than restating them.
- **Tradeoff accepted**: the certificate is no longer literally self-contained at the point of consumption — reading why a proof was allowed takes one hop to the coverage record. The mitigation is that the hop is within the same compilation output, not across files or tools, and hover renders the record inline.
- **Alternatives considered**:
  - *Each proof lists the individual checks it depends on.* Rejected on the duplication and cross-list agreement cost above. It reads closer to the ruling's wording but buys nothing the record does not, because the checker must still verify the list against an independently computed expectation either way — which is the finding in § The cost.
  - *No citation; rely on the completeness check alone.* Rejected: the completeness check tells you the set is whole, not that a particular proof's fact came from a set that is whole. Without the citation there is no link from a consumption back to the coverage, so a proof consuming a fact whose constraint was never even enumerated has nothing to fail against.
  - *Citation by constraint name only, with no record object.* Rejected: a name is not checkable. The checker needs the set and its dispositions, which is what the record carries.
- **Precedent**: the locked certificate format — "Steps form a tree/DAG over earlier steps and premises", with steps citing children drawn from the premise list rather than restating premise content. Externally, DRAT proof format cites clauses of the original formula by index rather than by value, for the same reason.
- **Sources consulted for this decision**:
  - `docs/Working/certificate-steps-membership-2026-07-12.md:154-158` — "Step Sⱼ ::= one CertificateStepKind member, citing children ⊆ {P₁…Pₙ} ∪ {S₁…Sⱼ₋₁}, carrying its own Conclusion"
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:118` — "a proof may not consume such a fact unless it **names the obligation that established the fact and every obligation that preserves it**, each itself discharged"
  - `docs/Working/certificate-steps-membership-2026-07-12.md:354` — "DRAT's premise base being the original formula's clauses cited by index (mirror, cited above)"
- **Strongest counter-evidence**: the 2026-07-21 ruling's own wording says a proof "names … every obligation that preserves it", which reads as a per-proof enumeration and is the shape a reader of that sentence would expect. Response: the two encodings check exactly the same two things — that the establishing obligation exists and passed, and that the preserving set is complete and every member passed — so the ruling's substance is met either way; it constrains what must be named and checked, and is silent on serialization. The record is not merely equivalent but strictly stronger on the one axis where they differ: under per-proof lists, two proofs consuming the same fact can cite different lists and nothing detects the disagreement, whereas one record makes that state unrepresentable. The ruling's own stated purpose — making the gap visible at the point of consumption rather than only by auditing the file — is served by both.
- **Reversibility**: `Easy`. Pre-release with no external certificate consumers; switching to per-proof lists is a change to how the same computed set is serialized.
- **Blast radius**: catalogs — none directly (a supporting type). Docs — `docs/compiler/proof-engine.md`, `docs/Working/certificate-steps-membership-2026-07-12.md`, `docs/tooling/mcp.md`. Samples — none. External consumers — none (pre-release).

### Decision 2: Establishment and preservation are two catalog kinds, not one parameterized kind

**Stakes**: high

- **Rationale**: they differ in every respect a requirement subtype exists to capture. Their site sets are disjoint and computed differently — establishment sites are construction and state entries, preservation sites are handlers whose write plan touches the mention set. Their proof conditions have different shapes — establishment asks whether a configuration satisfies the constraint, preservation asks whether the weakest precondition of a write plan holds. Their diagnostics say different things to the author. CLAUDE.md's catalog rule is explicit that varying shapes get a DU base plus sealed subtypes rather than a flat record with nullable fields, and a single kind carrying "site set, which is one of two kinds" with half its fields unused per instance is exactly the shape that rule forbids.
- **Tradeoff accepted**: two catalog members instead of one, and two diagnostic codes instead of one, for what an author may perceive as one concern ("my rule isn't enforced"). Accepted because the two failures have genuinely different repairs — an establishment failure is fixed at the default or the entering transition, a preservation failure at the writing handler — and a single message would have to name both.
- **Alternatives considered**:
  - *One `ConstraintObligation` kind with a site-kind discriminator field.* Rejected on the CLAUDE.md DU rule and on the practical consequence: every consumer would switch on the discriminator to decide which fields are meaningful, which is the enum-identity dispatch the catalog rules name as a violation.
  - *Fold both into the existing `IntervalContainment` kind for the modifier case and add only a rule kind.* Rejected: it would make the modifier and rule spellings structurally different in the catalog, which is the asymmetry this whole line of work exists to remove — the spec's position is that a modifier is sugar for a rule.
- **Precedent**: Event-B generates two distinct obligation kinds for exactly this split, an initialisation obligation and a per-event preservation obligation, rather than one parameterized kind — "3 of these are to verify that the initialisation establishes invariants inv2 to inv4 and 3 are to verify that the register event maintains invariants inv2 to inv4." In-tree, the `ProofRequirement` DU already separates `LengthContainment`, `CountContainment` and `IntervalContainment`, which are three shapes of the same idea distinguished because their payloads differ.
- **Sources consulted for this decision**:
  - `CLAUDE.md` § Catalog System — "**Use discriminated unions for varying shapes.** Don't paper over shape differences with nullable fields on a flat record — use a DU base + sealed subtypes."
  - `research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md:93` — the six-obligation split quoted above
  - `src/Precept/Language/ProofRequirement.cs:11-25` — `ProofSubject` as a closed supporting DU; `ProofRequirement.cs` DU base at `:535` per `docs/compiler/proof-engine.md:535`
  - `docs/compiler/proof-engine.md:531` — the current DU membership, which has "no constraint-establishment or constraint-preservation subtype"
- **Strongest counter-evidence**: the matrix speaks of "the constraint families" and "the constraint-obligation machinery" in the singular throughout, which could be read as one mechanism. Response: it also names them as two families explicitly — "constraint establishment (want :18), constraint preservation (want :19)" — and the singular usage is about the build, not the catalog shape.
- **Reversibility**: `Hard` post-ship, `Easy` now. The member names reach MCP vocabulary and diagnostic codes; merging them later would rename public surface. Pre-release means no external consumer holds them yet.
- **Blast radius**: catalogs — `ProofRequirementKind`, `ProofRequirement`, `ProofRequirements` meta. Docs — `docs/compiler/proof-engine.md` § ProofRequirement Catalog DU, `docs/language/catalog-system.md`, `docs/compiler/diagnostic-system.md`. Samples — none. External consumers — none.

### Decision 3: The expected set is computed from catalog-declared write-site categories and mention-set legs, never by re-running the minting walk

**Stakes**: high

- **Rationale**: this is the whole content of the independence requirement. A check that computes "what should have been minted" by asking the minting code produces agreement by construction and detects nothing — including the verified instance, where the qualifier modifiers produce no write-site check, because the minting code's answer is "none" and so is the expectation's. Independence has to come from a second derivation of the same declared facts, and the catalog is the right source because it is the language specification in machine-readable form rather than an implementation. Concretely: the write-site categories are declared once, the minting path reads them to decide where to create a check, and the completeness path reads them to decide where one must exist. Two consumers, one declaration.
- **Tradeoff accepted**: the check is only as good as the catalog. If a write-site category is missing from `WriteSiteCategories`, both walks are blind to it in the same way, and the check reports completeness on an incomplete model. That is a real residue and it is not closable from inside this design — it is why Decision 5 adds the build-time analyzer, and why the § 3A.4 writer list being asserted rather than enforced stays an open dependency. What the design buys is that the failure mode moves from "a category is silently unwired" to "a category is missing from a declared, reviewable, cross-checked list."
- **Alternatives considered**:
  - *Compute the expectation by walking the minting code and collecting its decision points.* Rejected as circular, in the specific sense the 2026-07-20 note names.
  - *Compute it from a hand-maintained list independent of the catalog.* Rejected: a second hand-maintained list drifts, and Precept's whole architecture exists to avoid parallel copies of what a catalog already knows.
  - *Compute it from the spec text directly at build time.* Rejected as not mechanizable — § 3A.4's writer table is prose, and the catalog is the machine-readable form the project already commits to for exactly this purpose.
- **Precedent**: `PRECEPT0027` does precisely this at the diagnostic level — the expected set is the `DiagnosticCode` enum (the catalog), the actual set is what a semantic scan of the pipeline finds, and the two are compared at build time with an explicit allow-list for gaps. Externally, the closest analogue is a coverage-driven rather than proof-driven guarantee, and the survey found none; see § Architecture Grounding on the novelty.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:113` — "A completeness check that computes 'sites that should have minted' from the same code that decides where to mint is circular — it agrees with itself and sees nothing"
  - `src/Precept.Analyzers/DiagnosticCoverageScanner.cs:8-27` — "Computes three sets from a compilation: All DiagnosticCode enum members (the catalog), Emitted codes (real emission contexts), Test-referenced codes"
  - `docs/language/precept-language-spec.md:1979-1988` — the eight-writer table, with the note "Any analysis that derives write sites from an action's primary field target alone will miss it, and a fact about `D` will appear to survive"
  - `CLAUDE.md` § Catalog System — "**Catalog before code.** A new keyword, type, operator, modifier, or construct goes in the appropriate catalog entry first."
- **Strongest counter-evidence**: the catalog and the minting code are both written by the same author in the same session, so "independence" is procedural rather than logical — a mistaken mental model produces a matching mistake in both. Response: accepted and not fully answerable. What the split does buy is that the catalog is small, declarative, reviewable, and cross-checked against the spec by an analyzer, whereas the minting walk is procedural code spread across several files; the error modes are not identical even though the author is. The residue is recorded in § Open questions and in the falsifiers.
- **Reversibility**: `Easy`. The expected-set walk is one file; changing where it sources from does not touch the catalog shape.
- **Blast radius**: catalogs — new `WriteSiteCategories` and `MentionSetLegs`. Docs — `docs/language/catalog-system.md` (inventory), `docs/compiler/proof-engine.md`, `docs/language/precept-language-spec.md` § 3A.4 (the writer table gains a pointer to its catalog form). Samples — none. External consumers — none.

### Decision 4: The citation duty partitions the eleven certificate premise kinds into three groups, and only one group carries a coverage record

**Stakes**: medium

The eleven premise kinds the certificate format already locked fall out cleanly against the behavioural test:

| Group | Premise kinds | What a proof must show |
|---|---|---|
| Fixed by declaration | `DeclaredModifier`; `DeclaredQualifier` where the qualifier is a literal; `DeclaredPresence` where the field is required | Cite the declaration. Empty establishment and preservation sets, per the 2026-07-21 refinement. No coverage record. |
| A condition on this path | `GuardCondition`, `RejectRowGuard`, `PriorAction` | Nothing to establish or preserve — these are facts about the route, and their survival to the consumption point is the transport rule's frame-and-kill, which the matrix already carries. No coverage record. |
| Breakable by an operation | `DeclaredBound`, `RuleCondition`, `EnsureCondition`, `DeclaredQualifier` where the qualifier's value comes from a field, and `DeclaredPresence` where the field is `optional` with a default | Cite the coverage record. |

`ReachabilityFact` and `DeclaredDefault` sit outside all three: the first is derived from the state graph rather than from a value, and the second *is* an establishment fact rather than a fact needing one.

**Why `DeclaredPresence` splits.** The certificate format defines it as quoting whichever declaration aspect guarantees the field always has a value — "default or required shape" — and those two aspects answer the behavioural test differently. The only writer that can unset a scalar is the `clear` action, and the catalog gates it: `ClearApplicable` lists the seven collection types plus `new ModifiedTypeTarget(null, [ModifierKind.Optional])`, so on the scalar side `clear` reaches a field only when it carries `optional`. A required field's presence is therefore unbreakable by any operation and is context. An `optional` field whose presence rests on a default is breakable the moment the file contains a `clear` on it, and carries a record. This is the same split as `DeclaredQualifier` and for the same reason — one premise kind, two populations, discriminated by the instance rather than by the kind.

- **Rationale**: the four situations the owner scoped are not four mechanisms — they are four premise kinds landing in the same group. Field modifiers are `DeclaredBound`; rules and ensures are `RuleCondition` and `EnsureCondition`; field-sourced qualifiers are the value-fact arm of `DeclaredQualifier`. Recognising that means one linkage mechanism serves all four, and the design does not need a per-situation answer. The partition is also derived rather than invented: it is the behavioural test applied to a vocabulary that already exists and was already closed.
- **Tradeoff accepted**: `DeclaredQualifier` is split by a property of the instance rather than by kind, so a consumer cannot tell from the premise kind alone whether a coverage record is required. Accepted because the alternative — splitting the premise kind in two — would break the certificate format's one-kind-per-author-construct rule, since the author writes the same thing in both cases. The instance carries the discriminator (`provdeps` empty or not) and the matrix's type-structural argument already defines it.
- **Precedent**: the survey's cross-system finding that every system maintains exactly two populations and never conflates them — "every surveyed system maintains the two populations precisely because conflating them either over-generates vacuous obligations or under-checks mutable invariants." The three-way split here is that two-way split plus path conditions, which are not facts about values at all.
- **Sources consulted for this decision**:
  - `docs/Working/certificate-steps-membership-2026-07-12.md:337-347` — the eleven premise kinds, e.g. "**DeclaredBound** — a numeric/length/count bound modifier on a field or arg"
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:127` — "The duty applies to a consumed fact **iff** some operation can establish or break it — a value-fact."
  - `research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md:151` — the alternatives-rejected passage quoted above
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md` § Rule validity, *Type-structural qualifier context* — `provdeps(φ) = ∅` as the discriminator
  - `src/Precept/Language/Actions.cs:50-60` — `ClearApplicable` = the seven collection types plus `new ModifiedTypeTarget(null, [ModifierKind.Optional])`
  - `src/Precept/Language/ActionKind.cs:7-35` — the fifteen action kinds, `Clear = 8` under the comment "Universal (collections + optional scalars)"
  - `docs/Working/certificate-steps-membership-2026-07-12.md:347` — "**DeclaredPresence** — quotes the declaration aspect that guarantees the field always has a value (default or required shape)"

### Decision 5: Completeness is checked at two levels — per file at compile time, and per category at build time

**Stakes**: high

- **Rationale**: the two questions are different and neither check answers the other. *Did this file get every check it should have* is a per-file question, enumerable as a finite product over the file's own declarations, and belongs at compile time so every compile answers it. *Is this category of check wired into the compiler at all* is a question about the compiler's source, and the per-file check structurally cannot see it: if a category is absent from the catalog, both the minting walk and the expected-set walk omit it identically and the comparison passes. The verified qualifier-modifier hole is exactly that failure — the per-file check would have reported completeness on a file whose `nonzero` divisor was never obligated. So the build-time analyzer is not belt-and-braces; it is the only check that can see a whole missing category.
- **Tradeoff accepted**: two mechanisms to maintain, in two languages, with two failure surfaces. Accepted because collapsing them means giving up one of the two questions, and each has a verified instance of the failure it catches.
- **Alternatives considered**:
  - *Per-file check only.* Rejected on the blindness argument above, with the qualifier-modifier hole as the witness.
  - *Build-time analyzer only.* Rejected: an analyzer can confirm a category has at least one creation site; it cannot confirm that a particular file's twelfth handler got its check. The per-file product is where an instance goes missing.
  - *Tests only, for both.* Rejected on the 2026-07-20 constraint — "Tests check the cases someone thought of; the property here is enumerable per file … so it can be checked on every compile rather than sampled."
- **Precedent**: `PRECEPT0027` is the in-tree instance of the build-time half, with its allow-list discipline. The per-file half has no external precedent; see § Architecture Grounding.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:114` — the tests-do-not-cover-this passage quoted above
  - `src/Precept.Analyzers/Precept0027DiagnosticEmissionCoverage.cs:21-25` — "Every DiagnosticCode member must have at least one emission site in the pipeline … or be listed in the Gate 1 allow-list with a tracking comment."
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:116` — "Prior art in this codebase points at build-time checking (catalog-declared positions plus a layered checker) rather than a pipeline stage"
- **Strongest counter-evidence**: the 2026-07-20 note calls where the check lives "the smaller question", which could be read as licence to pick one location and move on. Response: the note is about *location*, and this decision is about *how many questions there are* — it does not contradict the note, it distinguishes two properties the note's own text runs together ("enumerable per file" and "prior art points at build-time checking" are answers to different questions).
- **Reversibility**: `Easy` for the analyzer half (add or remove a build-time gate), `Hard` for the per-file half once diagnostics ship against it.
- **Blast radius**: catalogs — none beyond Decision 3's. Docs — `docs/compiler/proof-engine.md`, `docs/compiler/diagnostic-system.md`, `docs/contributing/`. Samples — every sample must pass the per-file check or be listed. External consumers — none.

### Decision 6: A completeness failure fails the compile as a compiler-internal error, distinct from the author-facing message

**Stakes**: medium

- **Rationale**: the two failures have different audiences and different repairs. `PRE0165` — a rule used as a fact that a write can break — is the author's problem, and § Audience and Teachability writes it in their vocabulary. An incomplete obligation set is not the author's problem: they wrote a legal file and the compiler failed to enumerate its own work. That must not be reported as though they made a mistake, and it must not be silently tolerated either, because tolerating it is the hole. So it fails the compile as `PRE0166` with a message that names the missing triple and says plainly that this is a compiler defect to report.
- **Tradeoff accepted**: an author can hit a message they cannot act on. Accepted because the alternative is worse in both directions — a warning would let the file ship with the guarantee broken, and dressing it as an author error would send them looking for a mistake they did not make.
- **Alternatives considered**:
  - *Report it as an author diagnostic.* Rejected: it is not an author error and no author edit reliably fixes it.
  - *Warning severity.* Rejected: a file that compiles with an incomplete obligation set is exactly the "compiles but the guarantee has an asterisk on it" two-tier trustworthiness the want doc forbids, and which the dead-rows ruling already refused for the same reason.
  - *Silent internal assertion in debug builds only.* Rejected: the failure is silent in production builds, which is the current situation.
- **Precedent**: the 2026-07-20 dead-rows ruling took the same position on a structurally analogous question — a definition that misrepresents itself is an error, not a warning, because warning severity creates the two-tier guarantee. Externally, Rodin surfaces an un-discharged obligation as a model-level failure rather than a tool warning.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:211` — "Warning severity would also create exactly the two-tier trustworthiness the want doc forbids — a file that compiles but whose guarantee has an asterisk on it."
  - `docs/compiler/proof-engine.md:513` — "A missing obligation means a constraint that an author declared is silently not enforced — exactly the prevention-vs-detection failure mode Precept exists to prevent in user code."

## The cost

This design shrinks the set of files that compile. Under the matrix's amendment protocol that makes it a **soundness correction**, not a widening: a major definition version, certificate replay breaks for affected cells, and it is routed to the owner with the witness program. Both requirements are met — `OrderTotals` in § The problem is the witness, compiled at HEAD on 2026-07-23 with zero diagnostics and a `Proved` divisor.

One correction is owed to the matrix itself. The 2026-07-21 ruling's rationale says the citation duty "converts that whole-file property into something a single certificate exhibits, so the gap is visible at the point of consumption rather than inferable only by auditing the entire file." That is half right. A citation makes the *link* local, but "every check that preserves it" is still a claim about the whole file, and for a checker to trust a citation it must verify the cited set is complete — which needs exactly the independently-derived expectation the 2026-07-20 note demanded. The duty relocates where that enumeration is consumed; it does not remove the need for one. The ruling stands; its rationale sentence overstates what the duty alone buys and is corrected when this design is promoted.

## Acceptance criteria

1. `OrderTotals` (§ The problem) fails to compile, with `PRE0165` naming the rule, the consuming site, and the write that breaks it.
2. `OrderTotals` with `NewCount as integer positive` compiles clean, and the divisor derivation's certificate cites the coverage record for `ItemCount > 0` with every member discharged.
3. `BoundVsRule` (§ The problem) rejects on **both** fields, not just `Total` — the rule-spelled ceiling produces the same rejection as the modifier-spelled one.
4. For a file with one rule mentioning two fields and three handlers writing them, the expected set computed by the walk equals the hand-computed product, verified by a test asserting the exact member list.
5. A test that deliberately suppresses minting for one write site causes the compile to fail with `PRE0166` naming that constraint, field, and site — the check catches a missing check.
6. A test that deliberately mints one extra check causes the same failure in the surplus direction.
7. A derivation citing a coverage record with any undischarged member is rejected; a test asserts the derivation is not admitted rather than merely that a diagnostic appears.
8. `Precept0030ObligationCategoryCoverage` fails the build when a requirement kind or write-site category has no creation site, and passes when it is allow-listed with a tracking comment.
9. Every file in `samples/` reports a complete obligation set, or appears in a committed list naming the reason it does not — no silent exclusions.
10. Coverage records are byte-identical across two runs on the same file (determinism, Principle 3).
11. `precept_compile` output carries the coverage record for every constraint, and `precept_proofs` carries the citation on every derivation that consumes a breakable fact.
12. The four situations each have at least one end-to-end test: a field modifier, a rule, a field-sourced qualifier, and a value written earlier in the same operation.

## Dependencies

**Upstream.**

- The matrix's transport rule and its type-structural qualifier argument — this design consumes both (the path-condition group in Decision 4 and the `provdeps` discriminator).
- The eight-writer enumeration in `precept-language-spec.md` § 3A.4 — the expected set is computed against it. Verified complete as a category list (§ Open questions), but asserted rather than build-enforced, with BUG-033 a verified instance of it being consumed wrongly. The `WriteSiteCategories` catalog entry and `Precept0030` together close the enforcement half.
- The certificate format's premise vocabulary — Decision 4 partitions it.

**Downstream.**

- The rule and ensure obligations themselves — the next design pass. It creates the checks this one counts and cites.
- Repair of the qualifier-modifier hole, BUG-033, and BUG-035 — each becomes a loud failure once this ships, rather than a silent one.
- Slice 3's re-ratification, which needs a rule layer that can state establishment and preservation.

## Doc-update enumeration

- `docs/compiler/proof-engine.md` § ProofRequirement Catalog DU — the two new subtypes; § Obligation Generation Contract — the completeness rule as a fifth contract item; § Contracts and Guarantees § Obligation Completeness — replace the current claim with the checked one.
- `docs/language/catalog-system.md` — inventory entries for `WriteSiteCategories` and `MentionSetLegs`; `CoverageRecord` under supporting types.
- `docs/compiler/diagnostic-system.md` — `PRE0165` and `PRE0166`.
- `docs/language/precept-language-spec.md` § 3A.4 — the writer table gains a pointer to its catalog form; § 0.7 — the composition paragraph gains the completeness leg.
- `docs/tooling/mcp.md` — the coverage record on the obligation DTO.
- `docs/tooling/language-server.md` — constraint hover gains the coverage summary.
- `docs/Working/obligation-discharge-matrix-2026-07-19.md` § The cell — the rationale correction in § The cost; § Vocabulary — coverage record as a defined term.
- `docs/Working/certificate-steps-membership-2026-07-12.md` — the premise partition and the added coverage reference.
- `docs/contributing/` — the analyzer's allow-list discipline.
- `README.md` — no change; it makes no claim about obligation completeness.

## Operational dimensions

**Security.** N/A — no source-text ingestion surface changes. The completeness walk consumes an already-parsed semantic index.

**Observability.** The two failure modes are deliberately distinguishable. `PRE0165` names the author's rule and the breaking write. `PRE0166` names the constraint, the field, and the site that should have produced a check and did not, and says plainly that it is a compiler defect. The coverage record is surfaced through `precept_compile`, `precept_proofs`, and constraint hover, so "which handlers must preserve this rule, and did they" is answerable without reading compiler output.

**Evolvability.** N/A — no dependency on an external standard. The catalog entries this design adds are internal.

## Falsifiers

1. If the per-file completeness walk adds more than 10ms to the median sample compile, the enumerate-every-compile decision is wrong for the product's latency budget and the check should move to a build-time or opt-in gate. (Baseline: the full corpus currently compiles in about 44ms.)
2. If more than three files in `samples/` need a coverage exclusion entry to pass acceptance criterion 9, the expected-set rule is over-generating and the mention-set legs or write-site categories are modelled too broadly.
3. If a real defect is found in obligation minting that the per-file check does not catch **and** the build-time analyzer does not catch, the two-level split in Decision 5 is missing a level and the design needs a third.
4. If the completeness check and the minting walk are ever found to have been fixed together in one edit to make a test pass, the independence in Decision 3 is procedural fiction and the expectation needs a genuinely separate source — spec-derived generation, or a second implementer.
5. If a domain expert shown `PRE0166` attempts to edit their file in response, the compiler-internal framing has failed and the message needs to be routed away from the author entirely.

## Open questions

None. Four were carried in the first draft; all four are closed, and how each closed is recorded here because two of them were closed by reading rather than by deciding.

**The coverage-record encoding against the 2026-07-21 ruling** — closed as an engineering call in Decision 1, not escalated. The two encodings check the same two things and differ only in serialization, the record makes cross-proof disagreement unrepresentable rather than merely undetected, and the choice is reversible pre-release. Nothing about the ruling's substance turns on it.

**Completeness of the § 3A.4 writer list** — checked, and the list is complete as an enumeration of writer *categories*. `ActionKind` has exactly fifteen members (`Set`, `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop`, `Clear`, `Append`, `AppendBy`, `Insert`, `RemoveAt`, `Put`, `EnqueueBy`, `DequeueBy`), matching the table's "fifteen kinds"; the `into` target, the state-action positions, default materialization, computed-field recomputation, the `omit` reset, the update patch, and `Restore` account for the rest. Nothing else in the language places a value into a field slot — an argument default writes an argument, not a field, and `Inspect` is non-mutating. What is *not* verified, and never was the same claim, is that the compiler consumes all eight; BUG-033 shows it does not. That is the write-surface completeness dependency the design already carries, and it is precisely what the build-time analyzer in Decision 5 exists to catch.

**The establishment site set for event, entry, and exit ensures** — already answered by the matrix, and the first draft recorded it as open through insufficient reading. Entry and exit ensures are transition-moment obligations, explicitly edge-triggered — "nothing is owed while merely resident" — so the obligation at the moment *is* the check and there is no separate establishment site. An event ensure is ingress-class and discharged by ingress evaluation, which the matrix lists as a discharge mechanism rather than something needing establishment. Establishment as a distinct site set therefore applies to exactly two constraint kinds: `rule`, at construction, and the residency `ensure`, at construction plus every entry into its anchor state. `Expected(C)` in § Semantic Rules is already written this way.

**Whether `DeclaredPresence` is breakable** — checked against the action catalog and it splits, exactly like `DeclaredQualifier`. Decision 4 carries the split and the catalog evidence.
