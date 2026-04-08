# Skill: GitHub README Width Audit

## When to use

Use when someone asks how wide a GitHub README hero image, diagram, or code treatment can practically render on the live repo page before GitHub stops widening it.

## Goal

Translate GitHub's live layout ceiling into a concrete art-direction target the design team can actually use.

## Method

1. Check the live repo page on `github.com`, not just local markdown.
2. Identify the rendered README article container and its layout class.
3. Inspect the linked GitHub CSS for the article/container max-width rules.
4. Confirm how README images are constrained (`max-width: 100%` is the critical rule).
5. Measure the local asset's natural pixel dimensions.
6. Convert the layout ceiling into:
   - a **hard rendered-width ceiling**
   - a **recommended visible artwork width**
   - a **whitespace budget** if the asset needs breathing room

## Current GitHub heuristic

- Repo content shell: about **1280px**
- Rendered README/article column: about **1012px**
- Safe visible artwork target inside a full-width hero: about **880-920px**

That last range is the one to hand to design when a README hero needs to feel closer to nearby prose/code at GitHub's max width.

## Translation rule

If the canvas width is known:

`displayed artwork width = source artwork width × (rendered README width / source canvas width)`

Rearrange that to solve for source artwork width, then put the leftover width into whitespace rather than extra content.

## Example

For a `1268px`-wide PNG shown in a README that caps at `1012px`:

- scale factor = `1012 / 1268 ≈ 0.798`
- to get about `900px` of visible artwork at render time:
  - source artwork width should be about `900 / 0.798 ≈ 1128px`
  - remaining `~140px` can become whitespace

## Output expectation

Report findings in plain human terms:

- what GitHub visibly caps
- the number design should optimize against
- whether whitespace is useful
- whether widening the source asset would change anything on GitHub
