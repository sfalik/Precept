# Skill: Color Roles Lookup

## Purpose
Quick reference for the locked 8+3 color system and how each color maps to product surfaces.

## The System

### Core 8
| Hex | Role | Use |
|-----|------|-----|
| `#6366F1` | brand | Wordmark, primary CTA, badges, diagram borders, grammar keywords |
| `#818CF8` | brand-light | Hover states, secondary highlights, header accents |
| `#C7D2FE` | brand-muted | Feature card backgrounds, table header tints, callout text |
| `#E5E5E5` | text | Body copy, README prose, headings, button labels |
| `#A1A1AA` | text-secondary | Captions, metadata, table cells, secondary badges |
| `#71717A` | text-muted | Placeholders, disabled labels, timestamps, tertiary text |
| `#27272A` | border | Card outlines, table borders, section separators, inactive states |
| `#0c0c0f` | bg | Page background, surface ground plane |

### Semantic +3
| Hex | Role | Use |
|-----|------|-----|
| `#34D399` | success | Enabled transitions, passing CI, ✅ indicators |
| `#FB7185` | error | Blocked transitions, constraint violations — **product UI only** |
| `#FCD34D` | warning | Unmatched guards, beta/preview callouts, ⚠️ indicators |

### Syntax Accent
| Hex | Role | Use |
|-----|------|-----|
| `#FBBF24` | gold | Rule messages in `because`/`reject` — **syntax only, never UI** |

## README Color Mapping
- **Primary badge:** shields.io `color=6366F1`
- **Secondary badge:** shields.io `color=27272A&labelColor=27272A`
- **CI badge:** shields.io `color=34D399` (passing)
- **Preview badge:** shields.io `color=FCD34D`
- **Error rose:** Never used in README or marketing

## Rules
1. The 8+3 system is closed. No new colors.
2. Gold is syntax-only. Not badges, not UI.
3. Error rose is product UI only. Not README, not marketing.
4. Need a secondary accent? Use brand-light `#818CF8`.
5. Need a subtle background tint? Use brand-muted `#C7D2FE`.
6. State names are Violet `#A898F5`, not brand-light.
7. When auditing drift, trust the locked palette card first and sweep later surface sections for stale literal hexes.

## Source of Truth
`brand/brand-spec.html` §1.4 (palette card) and §1.4.1 (color usage roles).
