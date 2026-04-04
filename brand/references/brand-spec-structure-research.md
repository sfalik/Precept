# Brand Spec Structure Research
## Does "Surface-First" Align with How Leading Design Systems Organize Themselves?

*Authored by J. Peterman, Brand/DevRel — Precept*  
*Research date: 2025-07*  
*Status: Living document*

---

## Purpose

Shane is restructuring `brand/brand-spec.html` from a **color-category-first** organization (color palette, typography, wordmark as top-level sections) to a **visual-surfaces-first** organization (each product surface as the primary topic, with color/typography/spacing described within that surface).

This document answers four questions with evidence from real design systems:

1. How do leading design systems structure their brand specs?
2. Where does brand identity live vs. surface application?
3. How do developer-tool-specific systems handle mixed surfaces?
4. Is "visual surfaces" recognized vocabulary? What else is used?

Plus a fifth: Is embedding brand research and explorations as living sections — not appendices — common practice?

---

## Research Method

All findings below are sourced from live URLs fetched during this research session. Systems researched: GitHub Primer, GitHub Brand, IBM Design Language, IBM Carbon, Material Design M3, Atlassian Design System, Vercel Geist, VS Code Theme API, Stripe brand assets, Tailwind UI, Primer Brand (GitHub marketing). Where a URL was inaccessible (Linear, Raycast brand pages), it is noted.

---

## Findings Table

| System | URL Fetched | Top-Level Structure | Surfaces? | Identity First? | Notes |
|--------|-------------|---------------------|-----------|-----------------|-------|
| **GitHub Brand** | `brand.github.com` | Logo → Typography → Color → Illustration → Mascots | ✗ | ✅ | Pure identity layer. No surfaces. Gateway to two distinct implementation systems. |
| **GitHub Primer Product** | `primer.style/product` | Getting Started → Components → **Primitives** (tokens) → Patterns → Octicons | ✗ | ✗ | Token-layer ("Primitives") is explicitly separate from components. No surfaces. |
| **GitHub Primer Brand** | `primer.style/brand` | Getting Started → Components → Theming → Animation | Partial | ✗ | Marketing-context-specific system. Theming section implies context-awareness, not surface naming. |
| **IBM Design Language** | `ibm.com/design/language` | Philosophy (PoV, Principles) → Typography → Color → Grid → Logos → Iconography → Illustration → Photography → Data Viz → Infographics → Layout → Animation | ✗ | ✅ Strong | Identity and visual language first. Surface application deferred entirely to Carbon. |
| **IBM Carbon** | `carbondesignsystem.com` | Designing (kits) → IBM Design Language → Components → Patterns | ✗ | ✗ | Implementation layer. References IBM Design Language for identity. Token → Component → Pattern model. |
| **Material Design M3** | `m3.material.io` | Get Started → Styles (Color, Typography, Elevation, Shape, Motion, Iconography) → Components → Patterns | Semantic role | ✗ | Color system uses surface-aware role names (surface, on-surface, surface-container) but organized by token, not by UI area. |
| **Atlassian Design** | `atlassian.design/foundations` | Foundations (Color, Typography, Iconography, Grid, Spacing) → Components → Patterns | ✗ | ✗ | Strict token-first. No surface organizing. Components grouped by interaction type (forms, navigation, messaging, overlays). |
| **Vercel Geist** | `vercel.com/geist` | Brand Assets → Icons → Components → **Colors** → Grid → Typeface | ✅ Explicit | Partial | Colors page explicitly organized by surface role: Background 1/2, Component Backgrounds (1–3), Borders (4–6), High Contrast (7–8), Text & Icons (9–10). Identity (Brand Assets) sits first. |
| **VS Code Theme API** | `code.visualstudio.com/api/references/theme-color` | **Workbench colors by surface** (Window, Text, Action, Button, Dropdown, Input, Badge, List/Tree, Activity Bar, Editor, Diff Editor, Breadcrumbs, Status Bar, Title Bar, Panel, Minimap, Notification, Terminal, Debug Toolbar, Welcome Page, Source Control, Extensions) → Syntax colors (tokens) | ✅ Fully | ✗ | The most surface-explicit system in the study. Colors are defined surface-by-surface. No token abstraction at the editor color level — you assign colors directly to named surfaces. |
| **Stripe Brand Assets** | `stripe.com/newsroom/brand-assets` | Wordmark (slate/blurple/white) → Badge → Screenshots | ✗ | ✅ | No published design system. Brand assets page only. Identity documentation only. |
| **Tailwind UI** | `tailwindui.com` | UI Blocks (Hero, CTA, Pricing, Stats, Testimonials, Team, FAQs, Footers, Flyout Menus, 404, Landing Pages) → Templates | ✗ | ✗ | Component/pattern-first, organized by page-section purpose. No surface or token layer. |
| **Linear** | `linear.app/design` | Not accessible (JS-rendered, no content) | — | — | Could not fetch. |
| **Raycast Brand** | `raycast.com/brand` | Not accessible (JS-rendered, no content) | — | — | Could not fetch. |

---

## Pattern Summary

Three distinct organizing models emerge from the systems studied.

### Model 1: Token-First (the industry default)

**Who:** GitHub Primer Product, Atlassian, IBM Carbon, Material M3  
**Structure:** Foundations/Primitives (color tokens, type tokens, spacing tokens) → Components → Patterns  
**How identity fits:** Separate system or separate top-level section before tokens  
**How surfaces fit:** Not a first-class concept; components are the unit of application

This is the dominant model for large, multi-product design systems. It maximizes reuse and enforces consistency through abstraction. The tradeoff: you must mentally map tokens → components → surfaces to understand how the design actually *looks* in any given context.

### Model 2: Identity-First, Implementation-Separate (the layered model)

**Who:** IBM (Design Language + Carbon split), GitHub (brand.github.com + primer.style split)  
**Structure:** Brand identity is a separate artifact from the implementation system, with an explicit handoff point  
**How surfaces fit:** Not addressed; surfaces are the domain of teams building on the system

This model works when you serve many teams and need clear governance. The IBM split between "IBM Design Language" (the why and the visual vocabulary) and "Carbon" (the how and the code) is the clearest example. GitHub's split between `brand.github.com` and `primer.style` follows the same logic.

### Model 3: Surface-First (the practitioner's model)

**Who:** VS Code Theme API, Vercel Geist Colors  
**Structure:** Colors, dimensions, and properties are organized by the UI surface where they apply  
**How identity fits:** Either absent (VS Code) or in a separate section first (Vercel puts Brand Assets before Colors)  
**How surfaces fit:** Surfaces ARE the organizing principle

This model is less common in published systems but is the most actionable for practitioners building in a specific product. VS Code's theme color reference is organized entirely by named UI surface — 30+ named surfaces, each with its own color properties. Vercel Geist organizes colors by surface role (Background, Component Background, Border, High Contrast, Text).

---

## Surface-First Precedent

Surface-first organizing IS used by well-regarded systems — specifically by developer-tool systems with complex, multi-context UIs.

**VS Code** (`code.visualstudio.com/api/references/theme-color`):  
The entire workbench color reference is organized by surface. Not by color family. Not by token. By surface: Window Border, Text colors, Action colors, Button controls, Activity Bar, Editor Groups, Editor, Diff Editor, Breadcrumbs, Status Bar, Title Bar, Panel, Minimap, Notification Center, Quick Pick, Terminal, Debug Toolbar, Testing, Welcome Page, Source Control, Extensions Panel. Each surface lists its applicable color properties.

This is exactly the pattern Shane is proposing for Precept.

**Vercel Geist** (`vercel.com/geist/colors`):  
Colors are organized not by hue or by token name but by how they are *used* on surfaces: "Background 1 — Default element background," "Background 2 — Secondary background," "Colors 1–3 — Component Backgrounds," "Colors 4–6 — Borders," "Colors 7–8 — High Contrast Backgrounds," "Colors 9–10 — Text and Icons." This is surface-role organization at the token layer — a hybrid.

**Material M3's color role system** (implied from navigation: `m3.material.io/styles/color`):  
Uses roles named "surface," "on-surface," "surface-variant," "surface-container" explicitly. The color system speaks in surface terms even though it organizes by style category. It's surface-aware at the semantics level.

---

## Vocabulary Observed

The field does not use a single term. Here is what was actually found:

| Term | Used by | Meaning |
|------|---------|---------|
| **Surfaces** | Material Design M3, VS Code (implicitly) | Background layers in a UI hierarchy; M3 uses it as a formal token-role name |
| **Workbench colors** | VS Code | Colors assigned to the IDE's UI surfaces and components |
| **Contexts** | Not found in fetched sources (appears in brand marketing literature) | Application-specific or audience-specific adaptations of identity |
| **Experiences** | IBM Brand Systems (`ibm.com/brand/experience-guides`) | Business, audience, and category-level usage guidance; closest to "touchpoints" |
| **Touchpoints** | Traditional brand/marketing | Physical and digital channels (not digital surfaces specifically) |
| **Product surfaces** | Informal in design discourse; no canonical published source found | Reasonable; descriptive; used by practitioners |
| **Pages / Page sections** | Tailwind UI | UI pattern categories for marketing pages |
| **Color roles** | Material M3 | Semantic color assignments (primary, secondary, tertiary, surface, background, error) |
| **Themes** | VS Code, GitHub Primer | Dark/light mode and color configuration |

**Conclusion on vocabulary:** "Visual surfaces" is not standard published vocabulary, but it is intelligible, precise, and defensible. The closest canonical term is VS Code's "workbench colors" (which are organized by surface). "Product surfaces" is used informally in design teams. For Precept's purposes, "visual surfaces" is correct and distinctive — it signals that each surface has its own visual contract, which is precisely Precept's thesis.

---

## Recommendation for Precept

Surface-first organization is **the right structure for Precept**, and it is supported by precedent from the most technically rigorous systems in this study.

Here is why Precept specifically benefits from it:

Precept ships five visually distinct surfaces:
1. **Syntax editor** — the `.precept` file in VS Code; tokenized, dark, 8-shade semantic color system
2. **State diagram** — VS Code preview panel; diagrammatic, shape-carries-structure, same hue families
3. **Inspector panel** — runtime verdict surface; emerald/coral/yellow; separate color grammar from authoring
4. **Documentation site** — prose + code samples; light or dark; brand color as accent
5. **CLI output** — terminal; reduced palette; structured output formatting

These five surfaces share a color vocabulary but have fundamentally different visual grammars. Organizing a brand spec around those shared palette tokens would force a reader to mentally assemble how a color is used; organizing around the surfaces shows directly what each surface looks like and why.

VS Code's theme reference proves this model works at scale. Vercel Geist proves it works for a developer tool with strong brand identity. Material M3 proves surface vocabulary belongs in a serious design system's color language.

**The recommended structure for `brand/brand-spec.html`:**

```
1. Brand Identity
   1.1 Wordmark (Cascadia Cove, small caps treatment, do/don't)
   1.2 Color System (8-shade palette + runtime verdicts as abstract reference)
   1.3 Typography (font family, weight scale, code vs. prose)
   1.4 Voice (tone coordinates, do/don't table)

2. Visual Surfaces
   2.1 Syntax Editor  (color + typography + constraint signaling in context)
   2.2 State Diagram  (hue families + shape rules in context)
   2.3 Inspector Panel (runtime verdict colors in context)
   2.4 Documentation  (light/dark application, brand accent usage)
   2.5 CLI Output     (terminal palette, structured output)

3. Brand Research & Explorations  ← living section, not appendix
   3.1 Color system research
   3.2 Typography research
   3.3 Adjacent product references
   3.4 Aesthetic brand references
   3.5 Active explorations (brand/explorations/)
```

---

## Where Brand Identity Belongs

The data is unambiguous: **identity comes first.**

Every system studied that has both identity elements and surface application puts identity before surfaces:

- GitHub: `brand.github.com` (identity) before `primer.style` (surfaces via components)
- IBM: Design Language (identity) before Carbon (implementation)
- Vercel Geist: Brand Assets (identity) before Colors (surface-organized tokens)
- VS Code: Documentation begins with "what are colors for VS Code" before listing surfaces

There is no example in the study where surface documentation precedes or replaces identity documentation. The pattern is invariant: establish *what the brand is* before explaining *where it appears.*

For Precept, this means the wordmark, the abstract color palette (8 shades + verdicts with their names and locked roles), and voice all belong at the top — as a system reference. The visual surfaces sections then show how that system is applied to each context.

The value of this structure: a contributor reading the syntax editor section can look up exactly which palette entries apply. A designer reading the state diagram section sees the same palette but different application rules. The identity section is the shared contract; the surface sections are the expression.

---

## On Living Research Sections

Shane's proposal: brand research documentation and explorations as living sections (not appendices).

**Assessment:** Not standard in publicly published design systems, but common in internal and product-team design systems — particularly Figma-based systems where "explorations" pages are active workspaces, not archives.

IBM Design Language has a "Gallery" section showing real-world brand applications — it functions as a living showcase. GitHub has a design blog (`github.blog/tag/design/`) for current practice documentation. These are closest to the proposal.

The absence of this pattern in published systems is likely a consequence of publishing constraints, not design philosophy. Precept is a single-product brand spec, not a multi-team governance document. The Precept brand spec serves the squad building Precept, not external licensees. In that context, living research sections are not just acceptable — they are the right call.

Embedding `brand/explorations/` as a linked living section rather than a hidden folder signals that brand evolution is a first-class activity, not a private process. That is the correct posture for a product in active brand formation.

---

## Sources

| Source | URL |
|--------|-----|
| GitHub Brand | `https://brand.github.com/` |
| GitHub Primer Product | `https://primer.style/product/` |
| GitHub Primer Brand | `https://primer.style/brand/` |
| IBM Design Language | `https://www.ibm.com/design/language/` |
| IBM Carbon (Designing) | `https://carbondesignsystem.com/designing/get-started/` |
| Material Design M3 | `https://m3.material.io/` (navigation) |
| Atlassian Design — Foundations | `https://atlassian.design/foundations/` |
| Atlassian Design — Components | `https://atlassian.design/components` |
| Vercel Geist — Introduction | `https://vercel.com/geist/introduction` |
| Vercel Geist — Colors | `https://vercel.com/geist/colors` |
| Vercel Geist — Brand Assets | `https://vercel.com/geist/brands` |
| VS Code Theme API | `https://code.visualstudio.com/api/extension-guides/color-theme` |
| VS Code Theme Color Reference | `https://code.visualstudio.com/api/references/theme-color` |
| Stripe Brand Assets | `https://stripe.com/newsroom/brand-assets` |
| Tailwind UI | `https://tailwindui.com/` |
| Linear Design | `https://linear.app/design` (JS-rendered, no content) |
| Raycast Brand | `https://www.raycast.com/brand` (JS-rendered, no content) |
