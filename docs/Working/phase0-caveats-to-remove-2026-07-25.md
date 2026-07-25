# Phase 0 caveats — the list phase 9 removes

| Field | Value |
|---|---|
| Status | Draft register — 2026-07-25 |
| Owner of removal | **Phase 9.** Phase 0 added these; phase 9 takes them out. |
| Plan | [`where-we-are-going-2026-07-25.md`](where-we-are-going-2026-07-25.md) — see § "Phase 0's caveats are temporary, and phase 9 removes them" |

Phase 0 added warnings to the instruction files saying the catalogs are incomplete and the canonical
documents are what completeness is measured against. That is true today and it is not the end state.
Once the catalogs hold the settled model, phase 9 removes every row below and the instruction files
say plainly that the catalogs — and the MCP tools that read them — are the answer for language
questions.

This file exists so that removal is a checklist rather than an archaeology problem. Line numbers are
as of 2026-07-25 and will drift; the quoted text is the reliable handle.

## What the guidance already says, and must keep saying

Two things are settled and should not be re-litigated when these rows come out:

- **The MCP tools are the primary way to answer a DSL question**, ahead of reading source. Phase 0
  did not weaken that and phase 9 does not need to restore it.
- **The one narrow disclaimer** is that while the compiler is being aligned to the spec its *proof
  results* are not trustworthy — `precept_proofs`, and the proof obligations in `precept_compile`'s
  output. Syntax, types, patterns and diagnostics are unaffected. The confirmed case: `field Bal as
  decimal nonnegative max 100` compiles clean, mints an interval containment obligation, marks it
  **Proved**, and reports the declared bound as `[−∞ .. 100]` — `nonnegative` contributes nothing,
  and a single event drives the value to −10. The engine reports a proof for something false.

The reason the disclaimer is narrow, as background: measured 2026-07-25, of 39 places where the
catalogs did not reflect a decision, about 30 were in proof requirements and discharge, and every
place the catalogs *did* reflect a decision correctly was language surface. Do not write anything
that contradicts that. Do not copy it into files as wording.

## Catalog-incompleteness caveats

| File | Line | Quote | What it should say once the catalogs are complete |
|---|---|---|---|
| `CLAUDE.md` | 38 | "The docs here are what we measure against — **not** the catalogs, which are incomplete (see § Catalog System)." | Drop the exclusion. Restore the original description of `docs/language/README.md` as the language surface — spec, canonical types, grammar, catalog. |
| `CLAUDE.md` | 40 | "catalogs as the intended machine-readable form of the language spec … Note that the first of those is a goal, not the current state — see § Catalog System." | Drop the "goal, not the current state" note; restore "catalogs as the language spec". |
| `CLAUDE.md` | 91 | "The **goal** is that catalogs become the language specification in machine-readable form" | Restore the flat statement: *"Catalogs are the language specification in machine-readable form."* § Catalog System line 103 names this explicitly as the sentence to restore. |
| `CLAUDE.md` | 93–103 | "**They are not that yet, and must not be worked as if they were.**" … through "**What ends this suspension:** …" | Delete the whole block. Line 103 states its own exit condition: adding a decision without teaching the catalog fails the build — every write site catalogued, certificate vocabulary created, establishment and preservation declared, exhaustiveness analyzer applied across all of it. |
| `CLAUDE.md` | 100 | "**Never cite a catalog as evidence that something is complete.** The completeness tests prove that every member a catalog *declares* carries metadata. They cannot detect a decision that was never written into the catalog at all." | Removable only once the exhaustiveness analyzer closes the hole this describes. Until then it is true regardless of phase. Re-check rather than delete blind. |
| `CLAUDE.md` | 123 | "**Never compile a `.precept` file and treat the result as the specification.** The compiler is unfinished." | The first clause stays — a compiler is never the spec. Drop "The compiler is unfinished" and the "what got built" framing. |
| `docs/agent-onboarding.md` | 25 | Heading: "Concept 1: Catalogs as the **intended machine-readable form** of the language spec" | Restore "Catalogs as the language spec". |
| `docs/agent-onboarding.md` | 29 | "**They are not that yet.** Measured 2026-07-25: the `CertificateSteps` catalog … does not exist at all; `ProofRequirementKind` has thirteen members and neither establishment nor preservation is among them …" | Delete the paragraph. |
| `docs/README.md` | 9 | "The canonical documents in this tree — not the catalogs, and not what the compiler currently accepts — are what completeness is measured against; see `CLAUDE.md` § Catalog System for the measured gaps and what closes them." | Drop the exclusion and the cross-reference to the (by then deleted) § Catalog System notice. The canonical docs and the catalogs agree at that point. |
| `docs/README.md` | 11 | "catalogs as the **intended** machine-readable form of the language spec" | Restore "catalogs as the language spec" — keep in step with `docs/agent-onboarding.md:25`. |
| `docs/README.md` | 18 | Routing table: "the primary substance, **and what completeness is measured against**" | The added clause can stay or go; it stops being a correction and becomes a plain description. Low priority. |
| `docs/language/catalog-system.md` | 10 | Status row: "Partial — every catalog this document inventories exists in `src/Precept/`, but the set is **incomplete against the specification** (measured 2026-07-25 …)" | Status becomes `Implemented` (or `Full`), with the "incomplete against the specification" clause removed and the review date refreshed. |
| `docs/language/catalog-system.md` | 97 | "**It is not that yet.** As of 2026-07-25 the catalogs do not hold everything the specification requires …" | Delete the paragraph. The sentence above it already states the intent; it becomes a statement of fact. |
| `docs/language/catalog-system.md` | 103 | "**This section is the vision, not the current state.** … the `CertificateSteps` catalog named at `:225` does not exist, and `ProofRequirementKind` carries no establishment or preservation member." | Delete. Rename the section if "Vision" no longer fits what it describes. |
| `docs/language/catalog-system.md` | 116 | Table cell: "All catalogs — as much of the language as has been catalogued so far, **which is not yet all of it**" | Drop the trailing clause. |
| `docs/language/catalog-system.md` | 127 | "As of 2026-07-25 the answer to that test is **no** — this is the principle the work is aimed at, not a description of where the catalogs stand." | The answer becomes yes. Rewrite as the affirmative, or delete and let the test stand on its own. |
| `docs/language/catalog-system.md` | 1285 | "these tests quantify over the members that exist, so they cannot detect something the specification requires that was never added to a catalog at all." | Same as `CLAUDE.md:100` — this is a real property of the completeness tests, not a phase-0 hedge. It goes away only when the exhaustiveness analyzer makes an untaught decision fail the build. Re-check, do not delete blind. |
| `.claude/agents/precept-reviewer.md` | 16 | "Language discipline findings measure against the docs here, **not** against the catalogs, which are incomplete … a catalog is never evidence that something is complete." | Drop the exclusion. A reviewer can then measure against either. |
| `.claude/skills/design/SKILL.md` | 52 | "The docs here are what a design is measured against — **not** the catalogs, which are incomplete … a catalog is never evidence that something is complete." | Same as the reviewer row; keep the two phrased identically. |
| `.github/copilot-instructions.md` | 22 | "In Precept **the goal is** the inverse: catalogs become the language specification in machine-readable form" | Restore the flat statement, matching `CLAUDE.md:91`. |
| `.github/copilot-instructions.md` | 24 | "**The catalogs are not complete yet and must not be worked as though they were.** Measured 2026-07-25 …" | Delete the paragraph, in step with `CLAUDE.md:93–103`. |

## Proof-result disclaimers

These come out when the proof engine is aligned to the spec, not when the catalogs are filled. That
may or may not be the same phase — check before removing.

| File | Line | Quote | What it should say once proofs are trustworthy |
|---|---|---|---|
| `CLAUDE.md` | 350 | "**One exception, while the compiler is being aligned to the spec: its proof results are not trustworthy.** … reports a proof for something false — `field Bal as decimal nonnegative max 100` …" | Delete. § Use the MCP Tools First then reads as it did originally: the tools are the first place to look, source only for what they don't cover. |
| `.claude/agents/precept-reviewer.md` | 97 | "**One exception … its proof results are not trustworthy.** … Never raise or dismiss a finding about what the compiler proves on the strength of what it currently reports." | Delete the bullet; § 8's preceding bullet already routes DSL questions to the MCP tools. |
| `.claude/agents/precept-reviewer.md` | 392 | "a report on what the compiler and catalogs do today. Use them … never as the answer to what the language should do (§ 8)." | The "what the language should do" caution is durable — a compiler is never the spec. Keep; only the § 8 cross-reference may need repointing. |
| `.github/copilot-instructions.md` | 104 | "**One exception, while the compiler is being aligned to the spec: its proof results are not trustworthy.** …" | Delete, in step with `CLAUDE.md:350`. |
| `tools/agent-sources/precept-author/body.md` | 243 | "the compiler is unfinished, so 'it doesn't compile' means it isn't built yet, not that the language forbids it" | Drop the "unfinished" clause; keep "check `precept_syntax` or `precept_compile`" and the bug-capture routing. **Edit the source, then run `node tools/scripts/build-agents.js`** — this text is generated into `.claude/agents/precept-author.md:249` and `.github/agents/precept-author.agent.md:256`, and editing either output directly is reverted by the next build. |

## Related temporary statements phase 0 added — different owner, listed so they are not lost

Neither of these is about the catalogs, so phase 9 does not own them. They are here because they are
the same kind of thing: a truthful statement added in phase 0 that stops being true when a specific
piece of work lands.

| File | Line | Quote | What ends it |
|---|---|---|---|
| `.claude/skills/execute/SKILL.md` | 122 | "The suite is **not green at HEAD**, so 'green after the slice' is the wrong bar … the failing set must be a subset of that baseline." | The test suite going green. Then the bar returns to "tests pass after each slice". The same wording appears in the skill's exit criteria, refusal list, and output description — remove all of them together. |
| `CONTRIBUTING.md` | 224 | "**Nothing currently checks these expectations.** … no test reads either — searching the test projects for `EXPECT` finds no code that parses them." | Building the checking test the paragraph itself describes: compile each sample, assert every `# EXPECT:` row matches an emitted diagnostic exactly, fail on any extra diagnostic or a missing header. |
