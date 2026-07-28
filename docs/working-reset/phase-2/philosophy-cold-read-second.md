# Cold read of `docs/philosophy.md` — second reader

**Reader**: an outside evaluator who has never seen this project. Read the philosophy once, straight through, before opening anything else. The verification pass against the spec and the want document comes at the end and did not inform the impressions above it.

**Date**: 2026-07-27. Do not commit.

---

## What I think this is

A compiler for business rules. You describe a business entity — its fields, its constraints, its lifecycle if it has one — in one file, and the compiler refuses to produce an engine unless it can prove that no operation the engine offers can ever put the entity's data in violation of a declared rule. The runtime is then deliberately thin: it never re-checks rules after a change, because the compiler proved no change can break them; the only checks that run are the ones the proofs depend on — validation of values arriving from outside, and guards. It ships as an embeddable .NET library, and the pitch is that it replaces the scattered stack of validators, state checks, and service-layer conditionals that every enterprise codebase accretes.

So: a validator, a state machine, and a small proof engine fused into one artifact, with prove-or-reject as the admission rule. That is a real category, and if the proof engine holds up it is a genuinely different category from everything in the comparison list.

## Where I believed it and where I did not

I believed the positioning. The "Where Precept stands" section is the strongest part of the document, and the reason is a small act of honesty: it concedes that database constraints genuinely prevent rather than detect, then argues about layer rather than pretending to a monopoly on prevention. Someone who wanted to oversell would not have written that parenthesis. The same goes for the tools it says it does not replace — orchestrators, event sourcing, ORMs. The author has clearly used these tools and knows where each one actually fails; the XState failure mode ("a `CancelledOrder` in the right state but with `Total > 0`") is the observation of a practitioner, not a competitor-matrix cell.

I believed the refusal section. "The refusal has a cost, and the author pays it" is the sentence that made me trust the document, because it names the price before I could ask for it. A philosophy that admits its language will be smaller than its grammar suggests is being written by someone who has already hit that wall, not someone imagining it.

I did not fully believe two things.

First, the scale of the proof claim. "A definition that compiles without diagnostics therefore has none of these defects: no unreachable state, no dead end, no type mismatch, no contradictory constraints, no unsatisfiable guard, no division by zero, no overflow, no empty-collection access, no rule that any operation can break." Reachability and dead-end detection are graph problems; those I believe cheaply. But contradiction detection, unsatisfiable guards, and rule preservation across every operation is verification territory, and the document never says what the language gave up to make that decidable — no loops? no multiplication of variables? what happens at the first rule the prover cannot handle? The refusal section gestures at this ("the language admits less than its grammar suggests") without ever naming a single thing that was removed. One concrete example of a rejected construct would have bought more credibility than the whole paragraph of resolve.

Second, the authorship claim. "The author learns how their own rules compose without ever learning to program" is the least earned sentence in the document. The person who writes guards, typed event arguments, and constraint expressions that must jointly close an inductive proof is programming, whatever their job title. I believe a business analyst can *read* a precept and can author one with help; I do not believe the compiler's error messages alone carry a non-programmer through a rejection that says "cannot prove this rule survives this event." Every declarative-rules product of the last thirty years has made this claim, and in every one the developers ended up writing the rules.

## The sentence doing the most work

"What cannot be proven does not compile."

Everything else in the document is a consequence of this sentence. The thin runtime is only safe if it is true. The closed operation set is its restatement. The comparison section is only a different category if it is true — otherwise Precept is a validator with unusual syntax. And it is the sentence most likely to be quietly false in the implementation: the moment one construct compiles with an unproven obligation, or one check is deferred to runtime "for now," the entire document degrades from contract to aspiration. If I were auditing this product, this is the sentence I would spend the whole week attacking.

## The sentence I would cut

"The author learns how their own rules compose without ever learning to program." Cut it and the authorship section still makes its real point — that the language optimizes for the rule-owner and the refusal messages teach — without the one claim that a reader with industry scar tissue will refuse, and whose refusal then contaminates the surrounding claims that deserved belief.

## Marketing or engineering?

Mostly engineering, and unusually well written. The prose has conviction, but conviction backed by mechanism: nearly every strong claim is followed by the reason it can be made ("the set is closed because the guarantee requires it: an operation nobody declared has no such proof and no existence"). Two places drift toward rhetoric: the closing triplet ("a rule that holds on the paths somebody tested is a hope...") is a slogan, though an honest one; and "the refusal teaches" in the authorship section is doing persuasive work the document has not shown evidence for. I would keep reading. I did keep reading.

## What it does not tell me that I would need to know

1. **The shape of the provable fragment.** The document says expressiveness was surrendered but never says what. Before committing a week, I need the list: which rule forms the prover closes, which it refuses, and what the workaround is when it refuses. If ordinary business rules — a percentage cap, a pro-rata calculation — routinely fall outside the fragment, the product is a proof of concept, not a tool. The document is silent on exactly the question its whole promise turns on.

2. **Definition evolution.** Restore is discussed carefully, but only for data written under the *same* definition. Business rules change constantly — that is the whole premise of the product. What happens to ten thousand persisted entities when the definition tightens a constraint they no longer satisfy? A governance product with no story for rule evolution has no story for year two. Not a word here.

3. **Any evidence the proof engine exists.** The opening italics say the document is commitments, not status — which is honest, and also means every sentence could describe software that does not run. Nothing here distinguishes "we have proved ten thousand definitions" from "we intend to." For a philosophy that is acceptable; for a decision to adopt, it is the first thing I would demand.

4. **Cross-entity reality.** Every example is a single entity, and the determinism rule explicitly forbids reaching outside the entity mid-rule. But the motivating examples — "no claim paid against lapsed coverage" — are two-entity rules (the claim and the policy). The document quietly claims a scenario its own scope rule appears to exclude, and never addresses how an entity learns a fact about another one.

5. **Concurrency.** Two operations arrive for the same entity at once. The determinism section covers a single evaluation; nothing covers ordering, conflict, or what "no moment at which it could fail" means under interleaving.

---

## Verification against the two documents behind it

Checked after the impressions above were formed: `docs/language/precept-language-spec.md` and `docs/Working/what-i-want-2026-07-16.md`.

### 1. Overflow — claimed impossible, ruled deferred (the significant one)

The philosophy claims, twice, that overflow is a compile-time impossibility:

> "Division by zero, arithmetic overflow, empty collection access: not risks managed at runtime, but compile-time impossibilities." (§ Prevention, not detection)
> "…no division by zero, no overflow, no empty-collection access…" (same section, the compiles-clean list)

The spec carries an explicit exemption, stated three separate times (§0.1 Principles 7, 10, 11):

> "**Overflow is currently exempted from this principle**, by an explicit ruling deferring the arithmetic overflow model past the first release; see §0.7."

The philosophy's preamble covers gaps where "the compiler or runtime has not yet caught up" — a build lag. This is not a build lag; the spec calls it an explicit ruling deferring the overflow *model* past the first release. A decision to defer is not a commitment not yet implemented, and the philosophy states the overflow guarantee with no qualification at all.

### 2. Time-referencing rules — asserted settled, recorded open

The philosophy presents `now()` in rules as a settled part of the design:

> "`now()` is the sole construct that reads the environment, an author who writes it into a rule is declaring in the open that the rule's correctness depends on when it is evaluated…" (§ What Precept is)

The want document records the existence of such rules as an open question:

> "**Time-referencing rules.** A rule like `ExpiryDate > today` can become false with no event in flight — nothing writes `today`, there is no ingress point for it, and the inductive model has no step where pure passage of time fits. Do rules that reference the current moment exist at all, or does time belong only in guards…? Deliberately left open." (§ Open questions)

This also collides with the philosophy's own strongest sentence about rules — "there is no moment at which it could fail" — since a time-referencing rule can become false with no operation running. The philosophy asserts in passing what the want document deliberately declines to decide.

### 3. Mandatory reason — true only with a qualification the philosophy omits

Philosophy:

> "Every rule carries a mandatory reason: the language will not accept a constraint without its rationale."

Spec (§0.1 Principle 9 and §2.4):

> "Constraint modifiers (§2.4) are rule shorthand and carry a *generated* rationale, which satisfies this requirement without an authored clause…"

A `nonnegative` modifier's reason is compiler-supplied boilerplate ("Qty must be at least 5"), not an authored rationale. The philosophy's sentence is literally true and materially softer than it reads.

### 4. Preview — "complete reasoning" vs. best-effort with undetermined outcomes

Philosophy:

> "…you can preview every possible operation and see not only what would happen but why, with the complete reasoning exposed."

Spec (§ Inspection):

> "Being best-effort does not make inspection exempt from any guarantee… where those premises are absent, the consequence is undetermined and is reported as such rather than evaluated."

Reporting "undetermined" honestly is consistent with the philosophy's spirit, but "see what would happen" carries no hint that some previews answer "cannot say."

### 5. The closed operation set — asserted by document, not yet kept by anything

Philosophy: "Every way the data can change is declared in the definition… the set is closed." The spec's own open-questions section says:

> "**Whether the set of in-operation writers is closed.** …the enumeration is asserted by that section rather than kept by the build. A proof that relies on a fact surviving an operation depends on the list being complete, so this is a soundness dependency, not a documentation nicety — and BUG-033 … is a verified instance of the list being wrong." (§ Open Questions)

Most of that is build state, which the preamble disclaims — but the spec classifies it as a soundness dependency with a verified counterexample, and the philosophy presents closure as an accomplished structural fact of the design rather than a property the design still has to make checkable.

### Not a discrepancy, for the record

The want document's "Philosophy flag" (§ Certificates) says the philosophy "currently describes Fire as 'evaluates all applicable constraints against the resulting configuration'" and needs rewording. The current philosophy already says the opposite — "Nothing re-evaluates the rules after the change" — so that flag appears to have been resolved in the philosophy's favor; the stale text is in the want document, not the philosophy.

---

## Verdict

I would spend the week — on the strength of the positioning honesty, the refusal section, and the coherence of the prevention argument. But I would spend it on exactly two questions the document declines to answer: show me the boundary of what the prover accepts, and show me what happens to persisted entities when the rules change. And I would ask, on day one, why a document this careful states the overflow guarantee unqualified when the spec behind it has ruled the overflow model out of the first release.
