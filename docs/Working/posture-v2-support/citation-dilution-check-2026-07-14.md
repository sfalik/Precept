---
title: Citation Dilution Check — Frank Go-Forward Posture Replay v2
date: 2026-07-14
author: Fact Checker
status: Working
source_under_review: docs/Working/frank-go-forward-posture-replay-2026-07-14-v2.md
companion_check: docs/Working/posture-v2-support/citation-audit-2026-07-14.md
scope: Docs-only verification of whether label simplification weakened citation precision
---

# Citation Dilution Check — Frank Go-Forward Posture Replay v2

## Verdict

**Overall answer: no material citation dilution.**

The v2 prose pass did **not** materially weaken the posture document's citations. The source documents are still named, the explicit line/section anchors for the load-bearing canon/ruling citations are still present, and the two weak spots from my earlier audit were actually **fixed**, not regressed.

That said, I found **2 borderline simplifications** where traceability is a bit weaker than the pre-simplification label form would have been:
- one **generic `fable-analysis-of-frank-response-2026-07-11.md`** reference in §4 that no longer carries a claim/section pointer, and
- one **compressed `critique-c2-false-security.md` block citation** in §7 that now relies on the prose bullets to recover several distinct findings rather than naming the old finding IDs individually.

These are **⚠️ borderline, not broken**. I found **no case** where a source became unidentifiable or where a previously specific anchor was reduced to a vague gesture that no longer lets a reviewer find the underlying point.

## Regression check against the earlier citation audit

### Prior audit status
My earlier audit of v1 found:
- **29 verified** citations
- **2 under-supported** citations
- **0 contradicted** citations

### v2 result against that baseline

**No regression found.**

All 29 previously verified v1 citation-claims still survive in v2 in equal or stronger form. In addition, the **2 earlier warnings are fixed**:

1. **Construction-input support**
   - v1 weakness: ingress/governance prose mentioned construction inputs while citing Fire-centric runtime lines.
   - v2 fix: §5 now cites `precept-language-spec.md:268` **and** `runtime-api.md:162`/`:180` for construction specifically.
   - Result: **resolved**.

2. **Retrospective quote overreach**
   - v1 weakness: *"through genuine debate and reversal"* was presented as if source-backed.
   - v2 fix: §11 now explicitly says that phrase is an **inference, not a source quote**, and anchors the actual retrospective support at `frank-retrospective-proof-engine-arc-2026-07-13.md:81`/`:109`.
   - Result: **resolved**.

## What clearly preserved precision

### 1. Canonical/ruling citations did not dilute
The strongest posture citations are still explicit and traceable:
- `philosophy.md:49`, `:51`, `:55`, `:59`
- `precept-language-spec.md:225`, `:258`, `:262`–`:270`, `:1969`
- `runtime-api.md:162`, `:180`, `:361`, `:386`, `:487`, `:656`
- `result-types.md:113`, `:114`, `:121`, `:138`/`:147`
- `proof-engine-boundary-ruling-2026-07-06.md:25`, `:39`, `:130`, `:134`, `:160`, `:166`, `:226`, `:275`, plus section/Q anchors (`§3`, `§4`, `§8`, `Q5`, `Q6`, `Q8`, `Q9`)
- `proof-engine-decision-ledger-2026-07-12.md` items `#1`, `#3`, `#6`, `#7`, `#9`, `#10`
- `proof-engine-linear-solver-feasibility-2026-07-12.md:22`
- `proof-engine.md:2609`
- `frank-retrospective-proof-engine-arc-2026-07-13.md:81`/`:109`

These are still exactly the kind of identifiers Shane was worried about losing, and they survived.

### 2. The old bare labels were usually replaced by descriptive prose that still points to the same finding
Where v2 removed label-shorthand like `C3-F4` or `Thesis Rec G`, it generally replaced it with a **description of the finding itself** while keeping the **source filename**. In most cases that preserved traceability because the description is specific enough to map back to a unique finding.

## Item-by-item check of the simplified-label references

| V2 location | Current plain-language reference | Old specificity type | Status | Assessment |
|---|---|---|---:|---|
| §2 overflow tension | `critique-c3-philosophy-reconciliation.md` for the `philosophy.md:53` present-tense overclaim | `C3-F5`-style finding ID | ✅ | Precision survived. The prose names the exact issue the critique raises, and it is uniquely identifiable in the source doc. |
| §2 overflow blocker | `critique-c3-philosophy-reconciliation.md` for the number-model prerequisite / build blocker | `C3-F4`-style finding ID | ✅ | Precision survived. The prose description maps directly to the critique's overflow-blocker finding. |
| §6 Event-B trade | `critique-c1-uniqueness.md` for “Event-B out-proves Precept on shared invariants” | `C1-F1`-style finding ID | ✅ | Precision survived. This is the core opening finding of the critique and is uniquely identifiable. |
| §6 no-GOVERNED-without-enforcement | `critique-c3-philosophy-reconciliation.md` | `C3-F9`-style finding ID | ✅ | Precision survived. The v2 prose uses the critique's own load-bearing phrase almost verbatim. |
| §7 severity split | `critique-c3-philosophy-reconciliation.md` for merely-unprovable vs proven-always-violating | `C3-F3`-style finding ID | ✅ | Precision survived. The prose keeps the exact distinction that mattered. |
| §7 false-security block | `critique-c2-false-security.md` plus three inline cautions (inline/ambient, anti-bundling, disclosure-first) | `C2-F1/F2/F5` and `Rec G`-style labels | ⚠️ | Borderline but acceptable. Source doc is named and the prose reproduces the substance of the findings, but one compact citation now covers multiple old findings, so lookup is a little less pinpoint than the old label set. |
| §4 set-vs-rule legibility cost | `fable-analysis-of-frank-response-2026-07-11.md` | `Fable Claim 9`-style label | ⚠️ | Borderline. The source file is preserved, but the claim/section anchor is gone. Because the Fable paper contains multiple relevant asymmetry/legibility arguments, this is less traceable than the old numbered-claim form. |
| §9 band-suppression tradeoff | `fable-analysis-of-frank-response-2026-07-11.md` and `§5` | `Fable Claim 8`-style label | ✅ | Precision survived. The section anchor remains, and the prose names the tradeoff clearly. |

## Borderline items — detailed notes

### 1. `fable-analysis-of-frank-response-2026-07-11.md` in §4
**Status:** ⚠️ borderline weaker

The sentence:
> “the adversarial critique of the earlier position raised exactly this (`fable-analysis-of-frank-response-2026-07-11.md`).”

still identifies the source doc, but it no longer tells the reader **which part** of that doc. Because the Fable analysis contains several related asymmetry/legibility points, this is the one place where the simplified form is meaningfully less pinpoint than the old `Claim 8/9` style.

**Why this is not a failure:**
- the source is still named,
- the surrounding prose makes the topic clear (legibility cost of differently-routed similar intents), and
- the appendix explicitly preserves the old `Fable Claim 8/9` keys.

**My judgment:** weaker than before, but still traceable enough that I would not call it diluted in the strong sense.

### 2. `critique-c2-false-security.md` in §7
**Status:** ⚠️ borderline but still serviceable

The simplified prose does a good job of spelling out the substance:
- global “compiles clean” overtrust,
- inline/ambient rather than query-only rendering,
- anti-bundling,
- disclosure-first,
- still unvalidated on the actual audience.

So the reader can still tell **what** the critique said. The only lost precision is that the old one-to-one mapping to `C2-F1`, `C2-F2`, `C2-F5`, and `Rec G` is no longer explicit in the body sentence.

**Why I am not marking this diluted:**
- the source document is named,
- the subpoints are described in prose, not blurred away,
- §13/open-questions still preserves the design-hypothesis framing, and
- the appendix retains the old key mapping.

## Checks on the v2 self-report

The v2 readability note makes three relevant claims:

1. **“Every one of the 29 audit-verified citations is retained.”**  
   **Verified.** I found no regression against the earlier 29 verified citations.

2. **“Construction inputs re-anchored … and ‘through genuine debate and reversal’ de-quoted …”**  
   **Verified.** Both earlier audit warnings were fixed correctly.

3. **“Bare finding IDs / claim numbers were replaced with plain-language descriptions … keeping the source-document filenames.”**  
   **Verified, with the two borderline caveats above.** In the current file, the source filenames are preserved and the prose descriptions usually keep enough specificity to recover the same underlying point.

## Bottom line

### Overall answer to Shane's question
**No — the citations were not materially diluted.**

### Summary counts
- **✅ Clearly preserved / improved:** 6 simplified-label references checked directly
- **⚠️ Borderline but still traceable:** 2
- **❌ Lost / materially diluted:** 0

### Most important takeaways
1. **The big precision anchors survived.** The line/section/item citations to the boundary ruling, decision ledger, philosophy/spec/runtime docs, and retrospective are still explicit.
2. **The earlier audit's 29 verified citations did not regress.** The prior 2 weak spots were fixed in v2.
3. **Only two places got slightly looser:** one generic Fable-doc reference and one compressed critique-C2 block citation. Neither is broken, but both are the places I'd point to if Shane wants the exact edge of the risk.
