---
status: Cited
authored: 2026-05-29
author: research (lifecycle-1)
topic: Where comparable compiler/type systems place flow-sensitive, value-narrowing-dependent compatibility checks relative to the base type checker, and how they stage the resulting diagnostics
external-engagement: strong
---

# Flow-Sensitive Compatibility-Check Placement Survey

> Where do comparable compiler/type systems place flow-sensitive, value-narrowing-dependent compatibility checks relative to the base type checker, and how do they stage the resulting diagnostics? This survey informs — but does not settle — the placement question for Precept's assignment-qualifier-compatibility check (PRE0141).

## Background

Precept must let a guarded assignment such as `when X.currency == 'USD' -> set Bal = Bal + Payment` compile when `Payment` is an open `money` field narrowed by the guard. The blocking diagnostic `PRE0141 UnprovedAssignmentQualifierCompatibility` is emitted at the **type-checker** stage; `docs/compiler/diagnostic-system.md` documents this as a deliberate split from the proof-stage `PRE0114 UnprovedQualifierCompatibility` (operand-pair check):

> "`PRE0141 UnprovedAssignmentQualifierCompatibility` is the assignment-time companion to proof-stage `PRE0114 UnprovedQualifierCompatibility`. Use `PRE0141` when the type checker cannot prove a required assignment qualifier axis; keep `PRE0114` for operand-pair proof obligations" — `docs/compiler/diagnostic-system.md` § DiagnosticCode Registry.

Narrowing discharge already works at the proof stage for the binary-op operand obligation. Three placement options are on the table for the assignment-compatibility check, framed by the consuming design pass:

- **(A)** narrow inline at the type checker (no reassignment-invalidation substrate there);
- **(B)** relocate the assignment-compat check to the proof stage (overrides the documented split);
- **(C)** keep it type-stage but feed it a shared flow-narrowing oracle.

This survey reveals which placement comparable systems favor and the diagnostic-staging consequences. It does **not** decide the Precept-specific override (keep-vs-relocate PRE0141) — that is a Tier-3 locked-spec override deferred to the owner-facing design pass.

This file **extends** prior in-tree surveys rather than restating them:

- `compiler-pipeline-architecture-survey.md` already documents Roslyn's four explicit stages (Parse → Declaration → Bind → Emit), TypeScript's binder/checker split, rustc's HIR-typeck → MIR-borrowck IR chain, and Kotlin K2's FIR resolution phases ending in a Phase 6 "Checkers" step. Those pipeline shapes are the substrate; this survey asks the narrower question of *where the flow-sensitive compatibility check sits within them*.
- `context-sensitive-literal-typing-survey.md` already documents TypeScript control-flow narrowing as "contextual typing," Kotlin's expected-type propagation, and Rust's local unification. Those establish the narrowing *mechanism*; this survey asks where the *compatibility decision that consumes narrowing* is staged.
- `proof-attribution-witness-design-survey.md` already documents the Rust borrow checker's multi-span diagnostic model and SPARK/Dafny/Frama-C proof-obligation attribution. This survey reuses the borrow-checker finding for the *staging* angle (typeck/borrowck separation), not the witness-shape angle.
- `research/architecture/README.md` records the standing open question: *"Should we formalize phases explicitly (Kotlin K2 model: RAW → Narrowing → ProofChecks → Diagnostics)?"* — a direct precedent for staging narrowing as its own phase. This survey is the evidence base for that question.

## Methodology

- **Research question** — see title. Two sub-questions per comparator: (1) *where* does flow analysis run relative to the base type checker (inline / separate pass / separate IR)? (2) *how* are its diagnostics staged (woven into base type errors, or deferred to a distinct checkers/reporting phase)?
- **Search strategy** — triaged the three in-tree surveys above and the architecture README open question first. Then fetched primary sources: rustc-dev-guide (MIR borrow-check chapter), Kotlin `JetBrains/kotlin` FIR basics doc, TypeScript Handbook (Narrowing), Roslyn `dotnet/roslyn` nullable-reference-types feature spec + nullability public-API design notes, and the ICFP 2010 occurrence-typing paper (ACM DL DOI + author copy). Web venues: rustc-dev-guide.rust-lang.org, github.com/dotnet/roslyn, github.com/JetBrains/kotlin, typescriptlang.org, dl.acm.org.
- **Inclusion criteria** — systems that (a) have a base type/binding stage AND a flow-sensitive analysis that refines types beyond declared types, and (b) make a compatibility/assignability decision that depends on the refined type. Roslyn nullable, Kotlin FIR DFA, Rust NLL borrowck, TypeScript CFA, and the occurrence-typing formal model all qualify.
- **Exclusion criteria** — pure literal-type resolution (already covered in `context-sensitive-literal-typing-survey.md`); proof-witness serialization (covered in `proof-attribution-witness-design-survey.md`). Excluded systems with no flow-sensitive refinement (Go `go/types`, CEL, Dhall) — they decide compatibility purely on declared types.
- **Source-grade mix** — Primary throughout (official compiler dev guides, official language handbook, peer-reviewed paper). One Tertiary supplement (a Roslyn issue-comment paraphrase of NullableWalker's pass structure) flagged in Threats to Validity.
- **Time bounds** — fetched 2026-05-29. External docs (rustc-dev-guide, TS Handbook, Roslyn/Kotlin docs) track moving `main`/`master` branches; excerpts mirrored to `research/references/flow-sensitive-check-placement/source-excerpts.md`.

## Findings

### Comparator table

| System | Where flow analysis runs relative to base typeck | Are its diagnostics a distinct stage/phase? | Why the split |
|---|---|---|---|
| **Roslyn nullable** (C#) | Separate flow-analysis **walk over bound nodes** (`NullableWalker`), *after* binding. Requires the whole method bound before nullability is known. | **Yes, separate.** Nullable W-warnings are produced by the flow walk, not by the binder; they are independent of declared nullability. | Nullability is *flow-state*, not a declared fact; it can only be computed once binding establishes bound nodes and the analysis can track state across statements. |
| **Kotlin K2 / FIR** | Data-flow analysis is woven into `BODY_RESOLVE`, but **all diagnostics are deferred** to a dedicated terminal `CHECKERS` phase, after the entire FIR tree is resolved. | **Yes, strictly separate.** "It's allowed to report diagnostics only in this phase." Resolution-detected errors are *stashed in the FIR tree* and converted to diagnostics only at CHECKERS. | Clean separation: resolution mutates/refines the tree; reporting reads the fully-resolved tree once. Avoids reporting against not-yet-resolved state. |
| **Rust NLL / borrowck** | Separate pass on a **separate IR (MIR)**, *after* HIR type-check. "We then do a second type check across the MIR," then region inference, then a final reporting walk. | **Yes, separate.** Borrow errors are emitted in a "second walk over the MIR … reporting errors," distinct from typeck's `E03xx` type errors. | MIR is radically desugared (less complex than HIR → fewer borrow-checker bugs) and its CFG is what makes non-lexical (flow-derived) lifetimes computable at all. |
| **TypeScript CFA** | Narrowing is a **checker sub-analysis** consulted inline during assignability, *within* `checker.ts`. Not a separate pass; the checker observes "a different type at each point" as it walks control flow. | **Mixed / inline.** Narrowing-dependent assignment errors (`TS2322`, `TS2345`) are produced by the same checker that does the narrowing — no distinct phase. | TypeScript has one monolithic checker; narrowing and assignability are co-located because the assignability decision *is* the consumer of the narrowed type. |
| **Occurrence typing** (Tobin-Hochstadt & Felleisen, ICFP 2010) | Formally, refinement is **layered onto the base type system**: the type of each variable occurrence is the declared type refined by the predicates that flow-dominate it. Latent-predicate propositions are a separate judgment composed with the base typing judgment. | N/A (formal model — no diagnostic staging). The refinement judgment is a distinct relation but is consulted by the base type rule for each occurrence. | Refinement is a *property of an occurrence in context*, formally distinct from a variable's declared type, but consumed by ordinary type rules at use sites. |

### Detail per comparator

**Roslyn nullable reference types** [Primary; accessed 2026-05-29; mirrored]:

The nullable feature spec frames null-state as flow-derived and independent of declared facts:

> "Flow analysis is used to infer the nullability of variables within executable code. The inferred nullability of a variable is independent of the variable's declared nullability."
> — Roslyn `docs/features/nullable-reference-types.md` (accessed 2026-05-29)

The public-API design notes confirm the analysis is a distinct flow pass that needs whole-method binding, unlike ordinary single-statement binding:

> "To fully determine nullability, we must bind the entire method and run nullability analysis up to the point requested by the caller, unlike today where individual statements can get away with being bound by themselves."
> — Roslyn `docs/compilers/CSharp/Nullability Public API Design Notes.md` (accessed 2026-05-29)

The implementation is `NullableWalker` under `src/Compilers/CSharp/Portable/FlowAnalysis/` — structurally a walk over bound nodes, separate from the binder. Roslyn's nullable warnings are W-warnings (suppressible, non-fatal), distinct from binding's hard type errors.

**Kotlin K2 / FIR** [Primary; accessed 2026-05-29; mirrored]:

FIR's phase list runs `… BODY_RESOLVE, CHECKERS, FIR2IR`. Data-flow analysis happens during body resolution, but *all* diagnostics — including resolution-detected ones — are deferred to the terminal CHECKERS phase:

> "At this point, all FIR tree is already resolved, and it's time to check it and report diagnostics for the user. Note that it's allowed to report diagnostics only in this phase."
> — Kotlin `docs/fir/fir-basics.md`, CHECKERS phase (accessed 2026-05-29)

> "If some diagnostic can be detected only during resolution … then information about such errors is saved right inside the FIR tree and converted to proper diagnostic only on the CHECKERS stage."
> — ibid.

This is the strongest precedent for *fully decoupling* the compatibility decision (which may be reached mid-resolution) from the *diagnostic emission* (deferred to a single reporting phase against fully-resolved state). The README open question's "RAW → Narrowing → ProofChecks → Diagnostics" sketch is essentially this model.

**Rust NLL / borrow check** [Primary; accessed 2026-05-29; mirrored]:

Borrow checking is a separate pass on a separate IR, after HIR type-check:

> "The borrow checker operates on the MIR." … "We then do a second type check across the MIR" (followed by region inference).
> — rustc-dev-guide, MIR borrow check (accessed 2026-05-29)

The deliberate typeck/borrowck separation is justified by IR simplicity and by the CFG being what makes flow-derived lifetimes computable:

> "The MIR is far less complex than the HIR; the radical desugaring helps prevent bugs in the borrow checker" and "using the MIR enables non-lexical lifetimes, which are regions derived from the control-flow graph."
> — ibid.

Diagnostics are emitted in a dedicated final reporting walk, distinct from typeck's type errors:

> "Finally, we do a second walk over the MIR, looking at the actions it does and reporting errors. For example, if we see a statement like `*a + 1`, then we would check that the variable `a` is initialized and that it is not mutably borrowed, as either of those would require an error to be reported."
> — ibid.

This is the strongest precedent for option **(B)**: a flow-sensitive *compatibility-class* check (initialization + non-aliasing are compatibility predicates over flow state) is deliberately relocated to a post-typeck pass on a flow-aware IR, with its own diagnostic stage.

**TypeScript control-flow analysis** [Primary; accessed 2026-05-29; mirrored]:

TypeScript narrows inline and consults the narrowed type *during* assignability — no separate pass:

> "TypeScript follows possible paths of execution that our programs can take to analyze the most specific possible type of a value at a given position. … the process of refining types to more specific types than declared is called narrowing."
> — TypeScript Handbook, Narrowing (accessed 2026-05-29)

> "This analysis of code based on reachability is called control flow analysis, and TypeScript uses this flow analysis to narrow types as it encounters type guards and assignments. … that variable can be observed to have a different type at each point."
> — ibid.

> "when we assign to any variable, TypeScript looks at the right side of the assignment and narrows the left side appropriately."
> — ibid.

This is the precedent for option **(C)** in its co-located form: narrowing and the assignability decision live in one checker, so the compatibility check simply *reads* the flow-narrowed type at the use site. There is no distinct diagnostic phase — `TS2322`/`TS2345` come from the same checker. The cost TypeScript pays is a famously monolithic `checker.ts`.

**Occurrence typing** (Tobin-Hochstadt & Felleisen, ICFP 2010) [Primary; DOI 10.1145/1863543.1863561; accessed 2026-05-29]:

The formal account places refinement *as a layer on the base type system*: each variable *occurrence* is given the declared type refined by the propositions (latent predicates) that flow-dominate that occurrence. The refinement judgment is a distinct relation, but it is composed with — and consumed by — the ordinary type rule at each use site. Formally this is option **(C)**: a distinct narrowing relation, consulted by the base type judgment, not a relocated separate phase. It is the theoretical grounding the D9 qualifier-narrowing design already cites inline.

## Implications for Precept

Mapping the findings onto the three placement options:

- **Option (A) — narrow inline at the type checker with no shared substrate.** *Weakly supported.* TypeScript is the only surveyed system that narrows and decides compatibility in one place, and it does so via a *first-class flow-narrowing engine inside the checker*, not ad-hoc inline logic. No surveyed system narrows-for-compatibility without a reusable narrowing substrate. The survey suggests that if Precept keeps the check type-stage, it should *not* do so without a narrowing oracle — i.e. (A) without (C)'s substrate is the unsupported shape. The "no reassignment-invalidation substrate there" caveat in the option-A framing is exactly the gap every comparator fills with a dedicated flow representation (Roslyn bound-node walk, Rust MIR CFG, TS flow nodes).

- **Option (B) — relocate the assignment-compat check to the proof stage.** *Strongly precedented by Rust.* Rust deliberately moves a flow-sensitive compatibility-class check (init/borrow) to a post-typeck pass on a flow-aware IR (MIR), and emits its diagnostics in a distinct reporting walk. The justification — the flow-aware IR is what makes the check *possible* — maps directly onto "the proof stage is where narrowing discharge already lives." The cost Rust accepts is a second IR and a second pass; Precept's analog cost is overriding the documented type-stage/proof-stage split for PRE0141 vs PRE0114.

- **Option (C) — keep it type-stage but feed it a shared flow-narrowing oracle.** *Strongly precedented by TypeScript and the occurrence-typing formalism.* Both keep the compatibility decision co-located with type checking but back it with a first-class narrowing relation/engine that the decision *reads*. This is the lowest-disruption option relative to the documented split (PRE0141 stays type-stage) while addressing (A)'s substrate gap.

On **diagnostic staging** specifically, the survey reveals a clear majority pattern that is *orthogonal* to where the check computes: **Kotlin K2 and Rust both decouple diagnostic emission into a dedicated terminal phase**, even though one computes during resolution and the other on a separate IR. Roslyn likewise emits nullable warnings from the flow walk, not the binder. Only TypeScript (monolithic checker) co-locates emission with the decision. This means: *whichever* placement Precept chooses for the compatibility computation, staging the resulting diagnostic distinctly (as the current PRE0141/PRE0114 split already does) is the better-precedented choice, and the README open question #2 ("Should diagnostics be deferred to a dedicated phase?") is answered "yes" by the majority of comparators.

The Precept-specific Tier-3 override — whether to *keep* PRE0141 at the type stage or *relocate* it to the proof stage, given the locked `diagnostic-system.md` split — is **deferred to the owner-facing design pass**. This survey supplies the precedent for each option but does not authorize overriding the locked split.

## Conclusions

**Conclusion 1 — Comparable systems compute flow-sensitive compatibility against a first-class flow representation, never ad-hoc inline.**

- *Rationale*: Every surveyed system that decides compatibility on a refined/narrowed type backs that decision with a dedicated flow substrate — Roslyn's bound-node flow walk, Rust's MIR CFG, TypeScript's in-checker flow-node engine, occurrence typing's latent-predicate judgment. None reaches the decision inline without a reusable narrowing representation.
- *Alternatives considered and rejected*: "Inline narrowing with no substrate" (option A in isolation) — rejected because no comparator does it; the closest (TypeScript) still uses a first-class flow engine.
- *Precedent*: Roslyn `NullableWalker`; rustc MIR borrowck; TypeScript CFA; Tobin-Hochstadt & Felleisen ICFP 2010 (all quoted above).
- *Tradeoff accepted*: this conclusion describes the general field, not Precept's choice; building/sharing a substrate has a cost (a second IR for Rust, a large checker for TS).

**Conclusion 2 — The dominant pattern stages flow-sensitive-check diagnostics into a distinct phase, decoupled from where the check computes.**

- *Rationale*: Kotlin K2 forbids diagnostic emission outside its terminal CHECKERS phase; Rust emits borrow errors in a dedicated final MIR walk; Roslyn surfaces nullable warnings from the flow walk rather than the binder. Three of four real compilers separate emission from computation; only TypeScript co-locates.
- *Alternatives considered and rejected*: co-located emission (TypeScript) — viable but tied to a monolithic-checker architecture Precept does not share and the README explicitly flags as a smell to avoid.
- *Precedent*: Kotlin FIR CHECKERS phase; rustc MIR reporting walk; Roslyn nullable W-warnings (all quoted above).
- *Tradeoff accepted*: a deferred diagnostic phase adds bookkeeping (stashing errors in the tree until the reporting phase, as FIR does) — the exact cost the README open question #2 names.

**Conclusion 3 — Both "relocate to a flow-aware later stage" (B) and "keep early but consult a flow oracle" (C) are well-precedented; (A) without a substrate is not.**

- *Rationale*: Rust grounds (B); TypeScript and occurrence typing ground (C); no system grounds bare (A).
- *Alternatives considered and rejected*: declaring a single winner — rejected; the survey's job is to supply precedent for each option, not to settle Precept's Tier-3 override.
- *Precedent*: as above.
- *Tradeoff accepted*: this conclusion deliberately stops short of recommending B vs C for Precept, leaving the locked-split override to the owner.

## What would change this conclusion

- If two or more additional production type checkers (e.g. F#, Scala 3, Flow) are found to compute flow-sensitive compatibility *inline with no flow substrate and no deferred diagnostic phase*, Conclusions 1 and 2 weaken — the "always a substrate / usually a deferred phase" majority would no longer hold.
- If the Roslyn `NullableWalker` "separate pass" framing turns out (on reading the source, not the design notes) to be interleaved with binding rather than a post-binding walk, the Roslyn row's "separate" classification needs revising (currently rests partly on a Tertiary issue-comment paraphrase — see Threats).
- If a comparator is found where relocating the flow-sensitive check to a later IR/stage measurably *worsened* diagnostic quality (e.g. lost source attribution), the "(B) is well-precedented" leg of Conclusion 3 would need a caveat about the attribution cost.

## Open Questions

- The survey establishes *placement* and *diagnostic staging* patterns but does not measure the *incremental-recompilation* consequence the README open question #1 ties to phase formalization. Whether option B or C better supports IDE-time partial re-checking in Precept is unresolved.
- Whether Precept's proof stage already has the reassignment-invalidation substrate that option A is said to lack at the type stage — an implementation-state question for the design pass, not a comparator question.
- Exact `NullableWalker` interleaving with binding (see Threats) — would require reading Roslyn source, not docs.

## Sources

- **rustc-dev-guide — MIR borrow check.** Rust project. Stable identifier: rustc-dev-guide (rolling, `master`); <https://rustc-dev-guide.rust-lang.org/borrow-check.html>. Primary. Accessed 2026-05-29. Mirrored to `research/references/flow-sensitive-check-placement/source-excerpts.md`.
- **Kotlin FIR basics.** JetBrains/kotlin. `docs/fir/fir-basics.md` (`master`); <https://github.com/JetBrains/kotlin/blob/master/docs/fir/fir-basics.md>. Primary. Accessed 2026-05-29. Mirrored.
- **TypeScript Handbook — Narrowing.** Microsoft. <https://www.typescriptlang.org/docs/handbook/2/narrowing.html>. Primary. Accessed 2026-05-29. Mirrored.
- **Roslyn — Nullable Reference Types feature spec.** dotnet/roslyn. `docs/features/nullable-reference-types.md` (`main`); <https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-reference-types.md>. Primary. Accessed 2026-05-29. Mirrored.
- **Roslyn — Nullability Public API Design Notes.** dotnet/roslyn. `docs/compilers/CSharp/Nullability Public API Design Notes.md` (`main`); <https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Nullability%20Public%20API%20Design%20Notes.md>. Primary. Accessed 2026-05-29. Mirrored.
- **Sam Tobin-Hochstadt, Matthias Felleisen. "Logical Types for Untyped Languages."** ICFP '10, pp. 117–128. DOI: 10.1145/1863543.1863561. Author copy: <https://www2.ccs.neu.edu/racket/pubs/icfp10-thf.pdf>. Primary (peer-reviewed). Accessed 2026-05-29.

### Prior in-tree surveys extended (not duplicated)

- `compiler-pipeline-architecture-survey.md` — Roslyn/TS/Rust/K2/Swift pipeline shapes.
- `context-sensitive-literal-typing-survey.md` — TS contextual typing, Kotlin expected-type propagation, Rust local unification (narrowing mechanism).
- `proof-attribution-witness-design-survey.md` — Rust borrow-checker multi-span diagnostics; SPARK/Dafny/Frama-C proof attribution.
- `research/architecture/README.md` § Open questions #1–#2 — the K2 phase-model and deferred-diagnostics questions this survey grounds.

## Inbound citation

This survey is load-bearing for the assignment-qualifier-discharge-placement design pass (the keep-vs-relocate PRE0141 question against the `docs/compiler/diagnostic-system.md` split). That design pass will cite this file for the placement-option precedent and the diagnostic-staging majority pattern. The Tier-3 locked-split override is deferred to that pass and is **not** decided here.
