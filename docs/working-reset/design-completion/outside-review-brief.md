# Brief: independent adversarial review of a design-completion plan

## What Precept is

Precept (this repository; all paths below are relative to its root; branch `spike/Precept-V2-Radical-design-completion`) is a DSL compiler and runtime for .NET. A `.precept` file declares a business entity's fields, rules, states and events. Its central promise is prove-or-reject: every business rule is proved at compile time to hold in every reachable configuration, or the definition is rejected.

## The owner's end goal and rules

In his words where they exist:

1. "A completed and consistent design first, then acceptance criteria, tests, code reviews." The current code not matching the design is expected. Compiler behaviour, tests and the sample files never settle a design question.
2. The design keeps all its detail, compiler and runtime internals included: "you're suggesting to finish the design by removing detail and stripping the docs down? that doesn't work for me."
3. Duplicated facts: "one full statement, deduplicate" — one document owns it, others point to it, nothing dropped.
4. The design states the full intended proof power: "it describes the full proof power, is that not clear from all the forensics and what I want" (want document `docs/Working/what-i-want-2026-07-16.md` :14, :171, :183). What the first build ships is separate and may not shrink the design.
5. Much less work and input from him; long autonomous agent runs reaching him only when needed; he starts each step himself with one short instruction, no scheduled runs.

## Files to read

- The plan under review: `docs/working-reset/design-completion/design-completion-plan.md`
- The want document: `docs/Working/what-i-want-2026-07-16.md`
- History: `docs/working-reset/design-completion/inputs/history-failures.md` and `docs/working-reset/design-completion/inputs/history-rulings.md`
- Technical background: `docs/working-reset/design-completion/inputs/technical-proof-model.md`, `docs/working-reset/design-completion/inputs/technical-precedent.md`, `docs/working-reset/design-completion/inputs/technical-canon-consistency.md`
- The previous plan: `docs/working-reset/compiler-readiness-plan.md`
- The canonical docs: `docs/philosophy.md`, and the folders `docs/language/`, `docs/compiler/`, `docs/runtime/`, `docs/tooling/`

The history and technical files were written by the same AI family that wrote the plan. Treat their claims as unverified: check each one you rely on against the repository and the canonical docs before using it.

## Questions to answer

1. Will this plan reach the end goal stated above? If not, what stops it?
2. Where will it fail, stall, or loop — repeat work without converging?
3. What does it get wrong technically, about the proof model, the compiler, the runtime, or the canonical docs?
4. What does it ask of the owner — decisions, reading, approvals, restarts — that it does not admit to asking?
5. What would you change?

For every finding give a severity (critical, major, minor), the evidence as `file:line`, and the reasoning that connects the evidence to the finding. Separate what you verified in the repository from what you are inferring.

## How to work

- Be adversarial. Your job is to find what is wrong, missing, or unworkable, not to summarise or endorse.
- Do not edit, create, move or delete any file except your report.
- Write your report to `docs/working-reset/design-completion/outside-review-report.md`.

## Writing rules for the report

The repository's own writing rules apply; your tool will not load them, so here they are briefly:

- Plain engineering prose. Expand an idea instead of compressing it into a term nobody outside the project would recognise. No coined phrases and no label codes standing in for a description.
- Do not use these words: source of truth (as a noun phrase), load-bearing, lens, surface (on its own), signal (meaning evidence), orthogonal, spine, harness, the ask, arm (of a switch or procedure).
- Use "denominator" only in its arithmetic sense.
- What a proof produces to demonstrate a violation is a "counterexample", never a "witness", even where a document says witness.
