# The phase-2 trace — what it is, what it covers, and what he has to read

**Written**: 2026-08-13. **Closing record added**: 2026-08-26.
**Range traced**: `b93d1db1..HEAD` (`dbc2a3db`).

## What this folder is

Phase 2 changed 23 of the project's canonical documents. These seven files justify every one of those
changes, hunk by hunk, by sorting each into one of four categories:

1. **Rests on the owner's own text** in `docs/Working/what-i-want-2026-07-16.md`.
2. **Rests on a ruling of his**, with his words quoted verbatim.
3. **An internal repair** — canon contradicting itself, or contradicting the code, or a broken link.
4. **Traces to nothing he wrote or ruled.**

Category 4 is the only category that asks anything of him. The rest is there so that a claim can be checked
rather than trusted.

| Document | Canonical files walked |
|---|---|
| [`builder-and-descriptors.md`](builder-and-descriptors.md) | `runtime/precept-builder.md`, `runtime/descriptor-types.md`, `runtime/README.md` |
| [`diagnostics-and-soundness.md`](diagnostics-and-soundness.md) | `compiler/diagnostic-system.md`, `compiler/soundness-and-coverage.md` |
| [`evaluator.md`](evaluator.md) | `runtime/evaluator.md` |
| [`language-documents.md`](language-documents.md) | `language/precept-language-spec.md`, `language/temporal-type-system.md`, `language/catalog-system.md`, `language/primitive-types.md`, `language/collection-types.md`, `glossary.md`, `tooling/mcp.md`, `tooling/language-server.md`, `contributing/anti-patterns.md` |
| [`philosophy-and-architecture.md`](philosophy-and-architecture.md) | `philosophy.md`, `compiler-and-runtime-design.md` |
| [`proof-engine-and-graph-analyzer.md`](proof-engine-and-graph-analyzer.md) | `compiler/proof-engine.md`, `compiler/graph-analyzer.md`, `compiler/README.md` |
| [`runtime-outcomes.md`](runtime-outcomes.md) | `runtime/fault-system.md`, `runtime/runtime-api.md`, `runtime/result-types.md` |

## The trace has run, and its coverage was checked by someone else

All 23 changed canonical files are walked. No file is missed and none is walked twice. A separate check
recounted every hunk figure these documents state against the real diff, and each one matches at one of two
context settings — some documents counted at zero context (`git diff -U0`), some at three (plain
`git diff`). Each header now says which, because a bare number is unreadable otherwise: `evaluator.md` is
47 hunks at three lines of context and 99 at zero, and "47" on its own does not tell a reader whether the
walk was complete.

## What was verified, and what was not

The trace states roughly 250 warrants — one per change, most of them a quotation from the want document or
from a recorded ruling. **Nine of those were opened and checked against the source they cite. All nine
held.** The other ~241 have not been re-derived by anybody but the writer.

That is the honest state of it. A reader who wants to rely on a particular entry should open its citation;
the entries give file and line so that this is a minute's work rather than an afternoon's.

## What he actually has to read: the category-4 short lists

Eleven items across the seven documents. **Five of the eleven are the same question** — the "and nothing
wider" narrowing of what an ingress check evaluates — appearing in five different documents, and that
question is already on the plan's § 4 list waiting for him. So there are seven distinct things here, not
eleven.

| Where | Item |
|---|---|
| [`philosophy-and-architecture.md`](philosophy-and-architecture.md#traces-to-nothing-he-wrote-or-ruled) | 1. The rewrite of `docs/philosophy.md` went well past the eight corrections prepared for approval, and the status line claims his approval with no words of his behind it |
| | 2. "and nothing wider" in `compiler-and-runtime-design.md:137` and `:151` |
| | 3. A new paragraph in philosophy about an AI agent meeting the same refusal a person does |
| | 4. The market-size paragraph was deleted for a reason the research file it links to contradicts |
| [`proof-engine-and-graph-analyzer.md`](proof-engine-and-graph-analyzer.md#traces-to-nothing-he-wrote-or-ruled) | 1. A new open question about whether the reference-identity invariant is needed at all |
| | 2. "and nothing wider" at `proof-engine.md:133`, `:2170`, `:2597` — **and these three carry no disputed marking**, unlike the eight canon sites that do |
| [`evaluator.md`](evaluator.md#traces-to-nothing-he-wrote-or-ruled) | 1. "and nothing wider" in `evaluator.md` |
| | 2. An index of open questions added to the document — asserts nothing, only makes existing questions findable |
| [`language-documents.md`](language-documents.md#traces-to-nothing-he-wrote-or-ruled) | "and nothing wider" in `precept-language-spec.md:274` and `:1992` — flagged as disputed in canon's own voice |
| [`runtime-outcomes.md`](runtime-outcomes.md#traces-to-nothing-he-wrote-or-ruled) | "and nothing wider" in `runtime-api.md:402` and `result-types.md:152` |
| [`builder-and-descriptors.md`](builder-and-descriptors.md#traces-to-nothing-he-wrote-or-ruled) | An index of open questions added to three documents |
| [`diagnostics-and-soundness.md`](diagnostics-and-soundness.md#traces-to-nothing-he-wrote-or-ruled) | **No category-4 item.** Two internal repairs added canonical text that goes beyond the contradiction they were fixing; both are quoted in full there |

## One finding was raised and has been withdrawn

`language-documents.md` filed the non-linear rules paragraph at `precept-language-spec.md:270` as
category 4, because the want-document lines it rests on were typed by an agent in the 2026-07-19 amendment
rather than by the owner. **On 2026-08-26 he settled it**: he reviewed and approved the want document, that
amendment included, so the content carries his authority. Nothing comes out of canon. The one thing
genuinely wrong is that want:135 is written in the first person as though he wrote it, which is a phrasing
problem inside the want document and not a defect downstream of it.

The finding is kept in full at [`language-documents.md`](language-documents.md) § *Raised and withdrawn*,
because the documentary work under it is correct and worth keeping; what changed is the conclusion drawn
from it. The full record of his answer is in
[`../want-amendment-marking.md`](../want-amendment-marking.md) § *Answered 2026-08-26*.

## What the trace did not do

- It does not say whether a change is **correct** — only whether it traces to something he wrote or ruled.
  Several correctly warranted changes describe a compiler that does not exist yet; that is the intended
  posture under walk item 23 and is not treated as a defect.
- **It edits no canonical file.** Where a trace says a canon sentence is wrong, the sentence is still there.
- It covers only the 23 files listed above. Working papers, plans and registers were read as evidence but
  are not themselves traced.
