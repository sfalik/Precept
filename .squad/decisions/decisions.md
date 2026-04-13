# Decision: README Image Link Fixes

**Date:** 2026-04-07  
**Owner:** J. Peterman (Brand/DevRel)  
**Status:** Completed

## Problem
README.md contained two broken image references that used incorrect relative paths:
- `brand/readme-hero.svg` 
- `brand/readme-hero-dsl.png`

These links pointed to `./brand/` but the actual assets are in `./design/brand/`.

## Solution
Updated both image references in README.md to use the correct path prefix:
- `brand/readme-hero.svg` → `design/brand/readme-hero.svg`
- `brand/readme-hero-dsl.png` → `design/brand/readme-hero-dsl.png`

## Verification
- ✅ Files verified to exist at corrected paths
- ✅ No other image references found in README
- ✅ Documentation context (hero example, contract display) remains accurate

## Impact
- Fixes broken hero example and contract diagram display in the README Quick Example section
- No behavioral changes—purely corrects link resolution for public-facing documentation

## Notes
The README's narrative around the hero example remains valid: it correctly notes that GitHub cannot render the styled DSL treatment, so the README displays the rendered contract (`readme-hero-dsl.png`) alongside copyable DSL source code. The path fix enables both assets to load correctly in GitHub's markdown renderer.

---

# Decision: README hero PNG fallback

- **Context:** GitHub does not render the styled inline HTML contract block in `README.md` as intended.
- **Decision:** Use `brand/readme-hero-dsl.png` as the GitHub-facing contract sample and keep a collapsed plain-text version immediately below for copyability.
- **Why:** The PNG preserves the intended branded syntax presentation on GitHub, while the collapsed source keeps the sample useful to humans and AI agents without turning the section back into a long raw block.
- **Files:** `README.md`, `brand/readme-hero-dsl.png`, `brand/readme-hero-dsl.precept`

---

# Decision: README Hero DSL PNG Rendering

**Author:** Elaine (UX)  
**Date:** 2025-07-21  
**Status:** Proposed  
**Scope:** brand/readme-hero-dsl.png

## Context

The README hero DSL snippet exists as an HTML file (`brand/readme-hero-dsl.html`) with syntax highlighting, and as an SVG state diagram (`brand/readme-hero.svg`). GitHub renders SVG but does not render arbitrary HTML. A PNG rendition of the syntax-highlighted code block is needed for contexts where the HTML source cannot be embedded directly — GitHub README `<img>` tags, social previews, and external documentation.

## Decision

- Render `brand/readme-hero-dsl.html` to `brand/readme-hero-dsl.png` using a headless Chromium screenshot at **2× device pixel ratio** for retina clarity.
- Output: **1268×942 px** (displays at 634×471 effective size) — tight crop of the `<pre>` code block, transparent background.
- The HTML source file remains the **editable source of truth**; the PNG is a derived asset that should be regenerated whenever the HTML changes.
- No fonts are embedded — the PNG captures the rendered output from Cascadia Code / Consolas fallback chain as available on the build machine. For cross-platform consistency, regenerate on a machine with Cascadia Code installed.

## Rationale

- PNG over SVG-from-HTML: GitHub `<img>` tags render PNGs reliably; converting syntax-highlighted HTML to SVG would require manual glyph work. The existing SVG is the state diagram — a different asset.
- 2× scale: GitHub displays images on retina screens. 1× screenshots appear blurry. 2× provides crisp text at reasonable file size (~137 KB).
- Transparent background: allows the PNG to sit on any surface background without a visible matte, matching the body `transparent` in the source HTML.

## Regeneration

If the hero snippet changes, re-render with:

```bash
# One-shot: install puppeteer, screenshot, remove
npm install --no-save puppeteer
node -e "<screenshot script>"  # see commit for full script
npm uninstall puppeteer && rm package.json package-lock.json && rm -rf node_modules
```

Future improvement: automate this as a build script or CI step.

---

## Decision

Treat `docs/HowWeGotHere.md` as a retrospective historical narrative, not as a live trunk-consolidation memo.

## Why

- Shane asked to remove the branch-history section as irrelevant.
- The "worth preserving" material read like an active recommendation set instead of a record of what endured.
- The unresolved/recommendation sections kept pulling the document back into pending-decision framing.

## Applied To

- `docs/HowWeGotHere.md`

---

# Steinbrenner Final Point Decision

- Date: 2026-04-05
- Branch: `feature/language-redesign`
- Decision: Treat the outstanding `docs\HowWeGotHere.md` addition as a coherent single final-point commit on the current branch.

## Why

- The working tree had one substantive product-facing change: a historical/consolidation document that explains how the branch got here and what remains unresolved before trunk return.
- That artifact is self-contained and does not need to be split from adjacent implementation work because there is no adjacent implementation work left unstaged.
- Freezing it in one commit gives the team an auditable reference point for any later trunk-curation exercise.

## Outcome

- Commit the document together with PM bookkeeping updates.
- Use the resulting SHA as the current branch's final planning reference until new work is intentionally started.

---

# Decision: Issue #22 Design Fidelity Directive

**Date:** 2026-04-08
**By:** Shane (user directive)

When implementing issue #22, if anything the team is going to implement strays from the design docs or seems ambiguous, they must stop and ask rather than guess. Design understanding is a prerequisite before coding starts.

---

# Decision: Issue #22 — Data-Only Precepts Design Q&A (12 Decisions)

**Date:** 2026-04-08
**By:** Shane (owner) via Squad Q&A
**Issue:** #22 — Data-only precepts

#### Decision 1: `all` keyword — field name collision
No special handling needed. Adding `all` to `PreceptToken` with `[TokenSymbol("all")]` and `requireDelimiters: true` automatically reserves it. Using `all` as a field/state/precept name is a hard parse error by architecture.

#### Decision 2: Root `edit` model representation
Option A — make `State` nullable on the existing `PreceptEditBlock` record. Root-level edits have `State = null`. No new model type needed.

#### Decision 3: Root `edit` parsing strategy
Parser accepts both root `edit` and `in State edit` forms as valid syntax. The type checker enforces the constraint: root `edit` + states declared = compile error (C55) with migration guidance. Avoids backtracking in the Superpower parser.

#### Decision 4: Events-in-stateless diagnostic code
Reuse C49 (orphaned event). Events in stateless precepts trigger C49 — structurally they are orphaned (no state routing surface). No new diagnostic code needed.

#### Decision 5: Root `edit` + states = compile error diagnostic
New code C55, severity Error. Message: "Root-level `edit` is not valid when states are declared. Use `in any edit all` or `in <State> edit <Fields>` instead."

#### Decision 6: Inspect for stateless — include events
Include events in the Inspect result, each with outcome `Undefined`. Uses existing `TransitionOutcome.Undefined` — no new outcome needed.

#### Decision 7: CreateInstance overloads for stateless
Only the 1-arg `CreateInstance(data)` overload works for stateless precepts. The 2-arg `CreateInstance(state, data)` overload throws `ArgumentException` for any call on a stateless precept, even with null state.

#### Decision 8: C50 severity upgrade — sample impact
Confirmed safe. All 21 existing samples compile clean with zero C50 diagnostics. Upgrading from hint to warning surfaces no new warnings in the sample corpus.

#### Decision 9: C29 invariant pre-flight for stateless
C29 fires at compile time for stateless precepts, same as stateful. Invariants on default values are checked regardless of whether the precept has states.

#### Decision 10: Event warnings — one per event
One C49 warning per event. A stateless precept with 3 events produces 3 separate warnings, consistent with existing C49 behavior.

#### Decision 11: Sample file names
Use `customer-profile.precept`, `fee-schedule.precept`, `payment-method.precept` as placeholder samples. Shane plans a major sample overhaul later.

#### Decision 12: Future root-level pattern
`edit` is the only root-level declaration planned for stateless. No need to design a general extensible root-level pattern. Keep it as a single special case.

---

# Decision: Slice 7 Test Coverage — Known Gaps (Deferred)

**Date:** 2026-04-08
**By:** Soup Nazi (Tester)

Three coverage gaps identified during Slice 7 test writing and explicitly deferred as non-blocking:

1. No direct unit test for `GetEditableFieldNames(null)` internal API — covered indirectly via Inspect/Update paths.
2. No multi-event stateless precept test — only single-event C49 path covered. Multiple C49 warnings (one per event) path is untested.
3. `PreceptInstance.WorkflowName` mismatch on stateless Inspect not covered.

These are known gaps, recorded for future test pass. Not blocking Slice 7 merge.

---
