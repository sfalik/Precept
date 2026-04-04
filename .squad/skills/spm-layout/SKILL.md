# Skill: SPM Layout — Surface Palette Mapping Row Alignment

## Summary

When editing the `.spm-*` surface palette mapping tables in `brand/brand-spec.html`:
1. Every `.spm-row` must be a direct child of a `.spm-grid` wrapper.
2. `.spm-grid` wrappers and `.spm-header` blocks must use **vertical padding only** (`padding: Npx 0`). The parent `.spm-group { padding: 16px 24px }` provides the shared horizontal indent — children must not add their own.

## Why

The `.spm-group` container provides 24px horizontal padding. Both `.spm-header` (CSS class) and `.spm-grid` (inline style) previously also had `padding: 18px/16px 24px`, creating **48px total offset** for content — double what the adjacent `sf-row` comparators render at (24px from `sf-group { padding: 20px 24px }`). This caused swatch misalignment across tables in the same card.

The override `.spm-grid > .spm-row { padding: 12px 0; }` already strips horizontal padding from rows inside the grid. The grid itself is the last point where horizontal padding may exist — and that too must be 0 since `spm-group` owns it.

## Horizontal Padding Ownership Rule

**Only one element in the hierarchy owns horizontal indent per layout region.**

```
spm-surface  (no padding)
  spm-group  ← OWNS horizontal: padding: 16px 24px
    spm-header  ← padding: 18px 0  (vertical only)
    spm-grid    ← style="padding: 16px 0;"  (vertical only)
      spm-row   ← padding: 12px 0  (vertical only, via CSS override)
        spm-swatch  ← renders at 24px from surface ✓
```

## Pattern

### ✅ Correct (single-row group)

```html
<div class="spm-grid" style="padding: 16px 0;">
  <div class="spm-row">
    <!-- swatch + info at 24px from surface, matching sf-swatch -->
  </div>
</div>
```

### ✅ Correct (multi-row group)

```html
<div class="spm-grid spm-grid-2" style="padding: 16px 0;">
  <div class="spm-row">…</div>
  <div class="spm-row">…</div>
</div>
```

### ❌ Wrong (grid adds own horizontal padding — double-indents)

```html
<div class="spm-grid" style="padding: 16px 24px;">
  <div class="spm-row">
    <!-- misaligned: 48px left offset instead of 24px -->
  </div>
</div>
```

### ❌ Wrong (bare wrapper — double-indents the row)

```html
<div style="padding: 16px 24px;">
  <div class="spm-row">
    <!-- visually misaligned — 48px left offset instead of 24px -->
  </div>
</div>
```

## CSS Reference

```css
.spm-group      { padding: 16px 24px; }     /* owns horizontal indent */
.spm-header     { padding: 18px 0; }         /* vertical only */
.spm-grid > .spm-row { padding: 12px 0; }    /* vertical only */
/* spm-grid inline: style="padding: 16px 0;" (vertical only) */
```

## Comparing spm vs sf alignment

Both should render swatches at 24px from the surface left edge:

| System | Container | Row padding | Swatch offset |
|--------|-----------|-------------|---------------|
| `spm-*` | `spm-group { padding: 16px 24px }` | `spm-grid → spm-row { padding: 12px 0 }` | 24px ✓ |
| `sf-*` | `sf-group { padding: 20px 24px }` | `sf-row { padding: 10px 0 }` | 24px ✓ |

## Columns

- `spm-grid` alone → no explicit grid-template-columns (single-column, full-width)
- `spm-grid spm-grid-2` → two equal columns
- `spm-grid spm-grid-3` → three equal columns

## Where This Applies

`brand/brand-spec.html` §2.1 Core Semantic Tokens and Support Tokens groups. Apply the same rule to any future `spm-*` sections (§2.3 Inspector, §2.4 Docs Site, §2.5 CLI/Terminal).
