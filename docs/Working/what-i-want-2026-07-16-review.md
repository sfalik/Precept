# Adversarial review — what-i-want-2026-07-16.md

**Status**: Review notes — 2026-07-18
**Reviewer**: independent agent (context-clean, read philosophy.md in full; compiler-and-runtime-design.md §1–§2 and language/README.md for vocabulary)
**Genre judged**: statement of want preceding a feasibility study — not faulted for missing design ceremony. Findings ranked most-likely-to-mislead-a-feasibility-study first.

---

**F1. "Proves or rejects all business rules" has two readings an order of magnitude apart in feasibility, and the doc never picks one.** (Ambiguity + philosophy conflict)

> "In an ideal world, the compiler **proves or rejects all business rules**. That is the strong value proposition."

Reading A (truth-proof): the compiler statically proves each declared rule can never be violated by any reachable operation sequence — rule *truth* is a compile-time theorem. Reading B (enforcement-proof): the compiler proves the *enforcement structure* is airtight — every rule is either statically discharged or covered by a declared ingress constraint the runtime enforces — while rule evaluation on runtime values still happens at runtime.

The doc supports both. "At ingress … the runtime can perform simple validation against those constraints" implies rules over runtime values are runtime-checked (Reading B). But "A business rule the compiler cannot prove is rejected — never handed to the runtime to check instead" reads as Reading A. Under Reading A, philosophy's own flagship rule becomes inexpressible: philosophy.md says

> "A rule that says 'the approved amount cannot exceed the coverage limit when the policy is in force and the claim type is Standard' is enforced structurally — on every operation that touches the entity."

and

> "A Precept rule holds because the runtime structurally prevents any operation from producing a result that violates it."

The compiler cannot prove `ApprovedAmount <= CoverageLimit` as a static truth when both are runtime-edited values — under Reading A that definition is *rejected*, which contradicts the philosophy the doc says it builds on ("this document does not redo that"). A feasibility study will pick whichever reading is convenient. The doc's single most important sentence is the least pinned-down.

**F2. The doc never rules on whether the runtime evaluates constraints against the post-mutation configuration — the exact question the compile/runtime-boundary section exists to answer.** (Ambiguity that will bite)

The runtime's duties are enumerated twice:

> "the **inspect** mechanism … respecting the business process defined by the precept … strict immutable versioning and write guarantees"

> "ingress validation, inspect, process enforcement, strict immutable versioning and write guarantees — not an exhaustive list"

Neither list mentions post-mutation constraint evaluation. philosophy.md states it as core:

> "**Fire**: … executes mutations, evaluates all applicable constraints against the resulting configuration, and commits only if every constraint holds."

Meanwhile the want says:

> "the runtime **can be made lighter due to the complete nature of the compiler** — it should trust that the compiler did its job."

A reader arguing "the runtime only validates ingress; result-side evaluation is dropped because the compiler proved it" and a reader arguing "of course Fire still evaluates all applicable constraints; that's governance, not deferral" can *each* cite this doc. This is precisely the known canon self-contradiction (spec §0.7 vs §3A.4 posture) — the doc restates the vocabulary of both sides without arbitrating. For a document whose stated scope is "where the line sits between **compile-time proof** and **runtime governance**," this is the load-bearing omission.

**F3. The no-deferral boundary is not mechanically applicable: at least three concrete case families fall in neither bucket.** (No-deferral axis)

> "A business rule the compiler cannot prove is rejected — never handed to the runtime to check instead. The runtime still has its own governance duties (ingress validation, …); what it never does is pick up proof work the compiler couldn't finish."

Apply this to:

- **A relational rule over two editable fields** (`EndDate >= StartDate`, or philosophy's approved-amount/coverage-limit rule). The compiler cannot prove it statically. Checking it at Update time: is that "ingress validation" (the rule *is* a declared constraint, and the edit *is* ingress) or "proof work the compiler couldn't finish"? The doc's ingress mechanism — "**require the author to extend constraints to those fields/args**" — is per-field/per-arg phrasing; a relational precondition cannot be decomposed into per-field constraints, so the prescribed remedy doesn't cover the case, and both classifications remain available.
- **Time-dependent rules** (`ExpiryDate > today`). `today` is neither an "editable field" nor an "arg" — it has no ingress door at which "the author extends constraints." Not classifiable by the doc's vocabulary at all.
- **Compiler-demanded vs author-authored constraints.** "I expect the compiler to **require the author to extend constraints**" — if the compiler computes the needed weakest precondition and the author accepts it, is the resulting runtime check an authored ingress constraint (allowed) or a compiler-residual check in costume (forbidden deferral)? "I believe this forces authors to be more explicit" leans against auto-derivation but nothing forbids a design pass from having the compiler synthesize the constraint and calling it "declared."

The boundary as worded is a slogan, not a test. A crisp version needs a decision procedure ("the runtime evaluates exactly the constraints the author wrote, and nothing the compiler synthesized" — or whatever the owner actually means); the current text supports incompatible procedures.

**F4. "We cannot dilute the value proposition" and "we will find a balance" are both asserted with no criterion distinguishing balance from dilution.** (Escape hatch, accidental)

> "If not, we will find a balance — but the balancing comes later … And whatever balance we strike, **we cannot dilute the value proposition in the process of practicality**."

Combined with:

> "I'm happy to **trade off some expressibility to gain proof**, as long as we are still providing a valuable tool that fits the kinds of business problems we intend to solve. That set of business problems is not pinned down…"

Every dial in the trade is unbounded: "some expressibility," "valuable tool," "business problems … not pinned down," "dilute" undefined. A feasibility study can justify any scale-back as "balance, not dilution," and any objector can call the same scale-back "dilution." The doc's stated job — "the balancing … is done against a stated ideal" — requires the ideal to be checkable; the acceptance criterion for the central trade is explicitly declared uncheckable ("not pinned down"). The "not pinned down" admission is honest, but it sits exactly where the reference function of the doc needs a pin.

**F5. "Lighter" and "the complete nature of the compiler" are undefined, and "lighter" silently conflicts with philosophy's runtime description.** (Ambiguity + philosophy conflict; overlaps F2 but is a distinct wording problem)

> "the runtime **can be made lighter due to the complete nature of the compiler**"

Lighter than *what baseline*, along *what axis* (fewer checks? fewer mechanisms? less code?)? Two readings: (a) lighter = no defensive re-proving of compiler-proven facts (traps become unreachable defense-in-depth — consistent with canon §1.1); (b) lighter = the runtime's constraint-evaluation machinery itself shrinks to ingress-only. Reading (b) collides with philosophy's "evaluates all applicable constraints against the resulting configuration" (quoted in F2) and also quietly undermines Inspect, which philosophy defines as "the engine exposes the complete reasoning: conditions evaluated, branches taken, constraints applied" — Inspect needs the full evaluator regardless of what the compiler proved. "Complete nature of the compiler" is likewise doing unstated work: complete *with respect to what obligation set*?

**F6. "Ingress (editable fields, args)" enumerates two doors; canon and the doc's own later text require more, and it's unclear whether the parenthetical is exhaustive or illustrative.** (Ambiguity)

> "At ingress (editable fields, args), I expect the compiler to **require the author to extend constraints to those fields/args**…"

Missing or unaddressed: construction/creation inputs (canon: "event arguments, construction inputs, field edits"), restore/rehydration of persisted state, and — per the doc's own later sentence — cross-entity data ("Cross-entity data is just another ingress"). If the parenthetical is exhaustive, creation inputs are ungoverned and the guarantee has a hole; if illustrative, the constraint-extension requirement's actual domain is undefined. Either way a feasibility study must guess the ingress set, which is the one set the whole ingress mechanism quantifies over.

**F7. "Simple validation" is a load-bearing adjective with two readings.** (Ambiguity)

> "so that the runtime can perform **simple validation** against those constraints"

Reading A: simple = per-value checks only (interval, format, membership) — in which case anything relational is excluded from runtime and, by F3, from the language. Reading B: simple = evaluation of any declared constraint expression, which for guarded relational rules is arbitrary expression evaluation — not obviously "simple," and hard to distinguish from the deferral the doc forbids. The adjective is exactly where a feasibility study will locate its scope decision.

**F8. Unstated load-bearing assumption: every proof precondition on an external value is expressible as a declarable constraint in the language's constraint vocabulary.** (Assumption)

The mechanism "require the author to extend constraints to those fields/args" only closes the loop if the constraint language can express whatever precondition the downstream proof needs (relational facts, cross-field intervals, path-dependent conditions). If it can't — and per-field modifiers cannot express `arg X < field Y` — then prove-or-reject plus this mechanism rejects definitions the author has no way to repair, and the want's "this forces authors to be more explicit — and that leads to better precepts" collapses into "this rejects precepts authors cannot fix." The doc never states this expressibility assumption.

**F9. Unstated load-bearing assumption: language simplicity suffices for decidable proof.** (Assumption)

> "The language was designed to make this possible: simple, not Turing-complete, no loops, no functions."

No loops/functions does not by itself yield decidable or tractable proof — nonlinear integer arithmetic is undecidable, and rule-set consistency over rich types (temporal, money, collections with quantifiers per `collection-types.md`) can be expensive or incomplete even in loop-free languages. As a *want* the doc is entitled to the aspiration, but the sentence is phrased as a settled fact ("was designed to make this possible") that the feasibility study should be checking, not inheriting.

**F10. Unstated load-bearing assumption: cross-entity data arrives through a governable door.** (Assumption)

> "Cross-entity data is just another ingress: it arrives from outside and is validated against declared constraints."

This presumes the language never gets reference/lookup semantics (a field that *reads* another entity at evaluation time) — if it does, there is no arrival moment to validate at. It also silently accepts point-in-time validity: the referenced entity can change after ingress and the constraint remains "validated." Fine if intended; nowhere stated as intended.

**F11. Internal tension: authors "should not have to understand" the compile/runtime difference, but the doc's own mechanisms force them to.** (Internal contradiction, moderate)

> "Authors should not have to understand the difference between things checked at compile time vs things governed at runtime."

versus:

> "I expect the compiler to **require the author to extend constraints to those fields/args**, so that the runtime can perform simple validation against those constraints."

and:

> "This holds on both sides of the boundary: compile-time rejections and runtime refusals alike must explain themselves clearly."

The compiler diagnostic that demands an ingress constraint is comprehensible only as "this value is runtime-supplied, so declare what the runtime should enforce" — the author is being taught the boundary as a condition of compiling. And runtime refusals arriving at runtime necessarily reveal which side of the boundary caught them. The statements are reconcilable if "not have to understand" means "the *guarantee* is uniform even though the mechanisms differ" — but the doc doesn't say that, and a feasibility study could read the sentence as a UX requirement to *hide* the boundary, which the other two sentences make impossible.

**F12. "Not an exhaustive list" is an escape hatch on the load-bearing side of the no-deferral boundary.** (Escape hatch, accidental)

> "The runtime still has its own governance duties (ingress validation, inspect, process enforcement, strict immutable versioning and write guarantees — **not an exhaustive list**); what it never does is pick up proof work the compiler couldn't finish."

The forbidden side ("proof work") is a single fuzzy phrase (F3); the permitted side is an open-ended list. Any future runtime check can be admitted as an unenumerated "governance duty" — e.g., "commit-time consistency re-checking" — and the only thing stopping it is the F3 boundary that cannot be applied mechanically. Openness about runtime duties may be deliberate; leaving the *permitted* side open while the *forbidden* side is fuzzy makes the no-deferral ruling unenforceable as written.

**F13. "Strict immutable versioning and write guarantees" is an undefined compound term used twice as if settled.** (Wording that will bite, minor)

The phrase appears in both runtime-duty lists. Neither philosophy.md nor the established vocabulary (canon has "definition versioning," "Version serialization contract," restore re-validation) defines "write guarantees." A feasibility study cannot check whether it delivered this duty because the duty has no referent. Minor, but it's inside the definitional list the no-deferral ruling depends on.

**F14. The MVP-scoping section imports feasibility balancing into a doc that forbids it, and the post-MVP ideal is undefined.** (Internal tension, minor)

> "the balancing comes later, and it is done against a stated ideal, not instead of one"

versus:

> "## The proof boundary is the single precept (for MVP) — For now, the promise stops at the edge of a single precept."

Is single-precept part of the *ideal* or a practicality balance smuggled into the ideal statement? The doc says saga precepts and message passing are envisioned post-MVP but states no ideal for them — does prove-or-reject extend across precepts? Does no-deferral hold across a message-passing boundary? The mixed genre means the feasibility study inherits an ideal with a scoping decision pre-baked, exactly what the Scope section says shouldn't happen. Mostly deliberate openness, but the genre confusion is real.

**F15. Wording nit: certificate-at-runtime interest sits oddly against "trust that the compiler did its job."** (Nit)

> "I'd also be interested to know whether those certificates could be leveraged at runtime."

versus "it should trust that the compiler did its job." A runtime that re-verifies certificates is precisely one that doesn't trust the compiler. Posed as a question, so not a contradiction — but a design pass could use this sentence to justify runtime re-checking machinery the "lighter runtime" section argues against. One clarifying clause would close it.

---

**Airtight sections:** "Clear explanations" is clean apart from the F11 tension; "Proof certificates — to consider" is properly labeled deliberate openness (F15 is a nit); the historical claim "We have already shrunk the language once" is fine.

**Summary of the attack surface:** the doc succeeds as a statement of *posture* (prove-or-reject, ingress-as-composition, no dilution) but fails at its stated job — being "a clear description … that later feasibility and design work can be checked against" — at exactly the three points where checking will happen: what "prove a business rule" means (F1), whether the runtime evaluates the post-mutation configuration (F2 — the known canon self-contradiction it leaves unarbitrated), and how the no-deferral boundary classifies relational, temporal, and compiler-demanded-constraint cases (F3). Everything else is secondary to those three.
