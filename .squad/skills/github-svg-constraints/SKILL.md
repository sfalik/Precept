# Skill: GitHub SVG Rendering Constraints

## When to use
When creating, reviewing, or debugging SVG assets intended for display in GitHub README files, issues, PRs, or any other GitHub-rendered markdown surface.

## The Two Sandboxes

GitHub has **two different SVG rendering paths** with different constraint sets:

### 1. Inline SVG in Markdown
- Processed by GitHub's **markdown sanitizer**
- **Aggressively strips**: `<style>`, `<script>`, `<foreignObject>`, `class` attributes, most `data-*` attributes, many SVG filter elements
- Retains: basic shapes, `fill`, `stroke`, inline `style` attributes (partially)
- **Verdict: unreliable for anything beyond trivial shapes.** Avoid for branded or complex visuals.

### 2. Referenced SVG via `<img>` or `<picture>`
- Served through GitHub's **camo proxy** (`https://camo.githubusercontent.com/...`)
- **Preserves**: full SVG spec geometry, inline attributes, `viewBox`, `fill`, `stroke`, `opacity`, `transform`, `<text>`, `<tspan>`, gradients, clip paths, masks
- **Strips**: `<script>`, external resource loading (`<image href="...">`, `xlink:href` to remote URLs, `@font-face` with URLs)
- **Cannot load**: custom fonts, external images, external stylesheets
- **Verdict: the correct path for branded SVGs.** Use a standalone `.svg` file referenced from markdown.

## Hard Constraints (referenced SVG path)

| Constraint | Workaround |
|------------|------------|
| No custom fonts | Use `font-family="monospace"` or convert text to `<path>` outlines |
| No `<script>` | All content must be static |
| No `<foreignObject>` | No HTML embedding in SVG |
| No external images | Embed all visual content as SVG geometry |
| No `<style>` blocks | Use inline `fill`, `stroke`, `font-family` attributes on each element |
| No CSS classes | Inline all styling |
| Percentage width/height stripped | Use explicit px values: `width="800" height="280"` |
| Max practical size | Keep under 100KB for fast load + readable diffs |

## Recommended Integration Pattern

```markdown
<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="path/to/dark.svg">
    <img alt="Descriptive alt text" src="path/to/default.svg" width="800">
  </picture>
</p>
```

- `<picture>` + `<source>` enables dark/light mode variants
- Start with a single dark variant; add light later additively
- Always include `alt` text for accessibility
- Always set `width` on `<img>` to control display size

## Font Decision Tree

```
Need text in SVG on GitHub?
├── Is exact brand font critical? → Convert to <path> outlines (costly to maintain)
└── Is monospace structural feel sufficient? → Use font-family="monospace" (recommended)
```

## Source of Truth
GitHub documentation on [supported HTML elements](https://github.github.com/gfm/) and empirical testing of the camo proxy behavior.
