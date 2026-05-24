---
name: lifecycle-5-promote
description: Stage 5 of the engineering lifecycle — lift "why" content from a design doc to its canonical home, archive the design with cross-link, verify doc-touch obligations completed. Triggers on — promote, canonicalize, "this shipped, update the docs", archive this design, "where does this go in canonical", lift to canonical, "the spec needs updating now". Takes an implementation-complete design doc and one or more target canonical docs; extracts decisions and rationale; produces a diff for owner review.
---

# Precept Design Promotion

Stage 5 of the engineering lifecycle. The transition that fails today. Each instance of "I keep finding things stranded in Archive" is a Stage 5 failure for some past slice. This skill makes the right thing easy to do.

## When to use

- A design doc in `docs/Working/` has shipped (implementation complete)
- The author is about to (or has just) moved the design doc to `docs/Working/Archive/`
- An older file is already in `docs/Working/Archive/` without a "Promoted to:" header — backfill the promotion
- Any time canonical docs need to receive design rationale from a recently-shipped slice

## When NOT to use

- Design is still being iterated (use `/lifecycle-2-design`)
- Design was abandoned — move to Archive with `**Status:** Historical — design dropped, no canonical replacement` header (no promotion needed); skill helps add the header
- Concept was superseded by a different design that already shipped — header `**Status:** Historical — superseded by [link]`

## Required workflow

```
/lifecycle-5-promote <design-doc> --to <canonical-doc> [--to <additional-canonical-doc>]
```

The skill:

1. Reads the source design doc in full
2. Extracts every "why" element:
   - Rationale (per decision)
   - Alternatives considered (and rejection reasons)
   - Precedent (research / prior art)
   - Tradeoff accepted
3. Reads each target canonical doc, identifies the appropriate § (typically § Design Rationale and Decisions, but topic-dependent)
4. Produces a proposed diff for owner review:
   - Each canonical doc gets relevant why-content lifted in
   - Pointer-philosophy: enumerable content (member lists, type shapes, file paths) goes in pointers to code, NOT lifted from design doc
   - Cross-reference back to the archived design as historical record
5. Verifies doc-touch obligations from the design doc's "Doc-update enumeration" section — flags any not-yet-touched canonical docs
6. Waits for owner confirmation
7. Applies the diff:
   - Canonical doc updates
   - Archive header on design doc: `**Promoted to:** <canonical>` (or multiple if multi-target)
   - If design doc not yet in Archive, moves it
8. Verification pass:
   - No "Status: Pending" / "TODO" markers for what was just lifted
   - All doc-touch obligations completed
   - Cross-links resolve correctly

## Behavioral guards

The skill enforces:

1. **Archive header is mandatory.** Skill refuses to move/archive a design doc without one of:
   - `**Promoted to:** <canonical link>`
   - `**Status:** Historical — superseded by <link>`
   - `**Status:** Historical — design dropped, no canonical replacement`

2. **Pointer-philosophy applied to lifted content.** Per the catalog-system.md rewrite pattern: enumerable content (counts, member lists, field shapes) becomes pointers to code; only conceptual why-content is hand-lifted. Skill flags lifted content that looks enumerable and suggests converting to pointer.

3. **Doc-touch verification.** Cross-checks against the design doc's "Doc-update enumeration" section. If the design said it would touch `docs/X.md` and `docs/Y.md`, the skill verifies both were updated. Surfaces gaps.

4. **No fabrication.** If the source design lacks four-leg rationale (e.g., Archive-sourced pre-policy design), the skill lifts what's there honestly. Does not invent Alternatives/Precedent/Tradeoff to satisfy four-leg requirement.

5. **Diff before apply.** Owner sees the full diff before anything writes. Catches misrouted content, wrong canonical doc, missed sections.

## Composability

- **Input**: `--from <design-doc> --to <canonical>` (one or more `--to`)
- **Output**: updated canonical doc(s) + archived design doc with header + verification report

Pairs with `/lifecycle-7-audit` (when shipped, Phase 9): audit periodically verifies canonical docs retain their why; if a canonical doc has been edited away from what was lifted, audit surfaces the regression.

## Anti-patterns to refuse

- Archive a design doc without a header — refuse the move
- Lift enumerable content into canonical (count, field list, member enumeration) — flag for pointer conversion
- Fabricate four-leg structure not present in source — refuse; lift what's there with explicit "no precedent recorded in source"
- Apply diff without owner review — always show diff first

## Quick reference

| Symptom | Skill response |
|---|---|
| Move to Archive, no header | Refuse; require header choice |
| Lifted content contains "TokenKind has N members" | Convert to pointer to `TokenKind.cs` |
| Source design has Decision + Rationale only (no four legs) | Lift as-is; note "no precedent recorded in source" |
| Doc-update enumeration says docs/X.md but skill didn't update it | Surface gap; prompt to update |
| Canonical doc already has section header for what's being lifted | Insert into existing section; don't duplicate |

## Backfill mode

For Archive files already moved without headers (today's situation — 4+ Stage-4 failures found):

```
/lifecycle-5-promote --backfill <archive-doc> --to <canonical-doc>
```

Same workflow but skips the "move to Archive" step (file is already there). Extracts, lifts, applies header, verifies. This is what Phase 1 will use to clear the existing backlog.
