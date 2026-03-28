Color system research — how developer tools and open-source libraries build color identities.

Purpose:
- Study color strategies across the spectrum: from single-dev GitHub libraries to venture-backed platforms.
- Extract principles for building a color identity that works at library scale (README badges, docs, icon, VS Code extension) and can grow.
- Ground in real examples with source references.

Sources referenced:
- Vercel Geist Design System color scales (vercel.com/geist/colors)
- GitHub Primer color system (primer.style/foundations/color/overview)
- Tailwind CSS color palette (tailwindcss.com/docs/customizing-colors)
- Linear brand guidelines (linear.app/brand)
- Observed color choices across open-source libraries

---

## Scale matters — calibrate to reality

Precept is a .NET library on GitHub. Its color system needs to work in:
- README badges and shields (tiny, inline)
- VS Code extension UI (syntax highlighting, preview panel, diagnostics)
- Icon / favicon (32px–512px)
- Documentation site (if/when one exists)
- NuGet package listing

It does NOT currently need:
- A full 11-step color scale like Tailwind
- Multiple themes with design tokens like Primer
- Marketing landing pages with gradient hero sections

Start minimal. Define one or two brand colors with clear usage rules. Expand only when a real surface demands it.

---

## How libraries and tools use color

### Tier 1: GitHub libraries (our actual scale)

| Library | Primary color | Where it appears | Notes |
|---------|--------------|-----------------|-------|
| Serilog | Red/coral | Logo, NuGet icon, docs site | Single accent color. Warm. Distinctive against .NET's purple. |
| Polly | Blue | Logo, README badge | Clean blue. Professional. Blends with .NET ecosystem. |
| MediatR | Purple/violet | NuGet icon | Leans into .NET's purple family. Minimal brand surface. |
| FluentValidation | Blue | NuGet icon, docs | Generic blue. Low brand investment — functional, not memorable. |
| Dapper | Orange | NuGet icon | Warm accent. Stands out in NuGet search results. |
| Bogus | Green | NuGet icon, README | Playful green. Matches the "fake data" personality. |
| AutoMapper | Red | Logo, NuGet icon | Bold red. Simple wordmark + color = recognizable at a glance. |
| Newtonsoft.Json | Blue-black | NuGet icon (square "N" logo) | The most downloaded NuGet package ever (7.9B). Minimal brand investment — a small square logo. Ubiquity IS the brand. |
| xUnit | Teal/green | .NET Foundation branding | Inherits .NET Foundation visual identity. No custom color — the category affiliation does the work. |
| Hangfire | Orange-gold | Logo, dashboard UI | Warm orange. The dashboard UI carries the brand more than the logo does — the product surface IS the brand surface. |
| Humanizer | Yellow | NuGet icon | Warm, friendly yellow. Matches the library's personality — making code more human. |
| BenchmarkDotNet | Green-teal | NuGet icon, docs | Technical green. "Measurement" and "precision" connotations. |
| FluentAssertions | Blue | NuGet icon | Standard blue. Professional but undifferentiated. Brand recognition comes from the name and API, not the color. |
| Refit | Orange | NuGet icon | Another warm accent. Stands out against the blue/purple .NET mainstream. |
| Swashbuckle | Green | NuGet icon | Swagger-green inheritance. The upstream brand's color flows downstream. |

**Pattern at library scale**: One color, used consistently across the few surfaces that exist (NuGet icon, README badge, logo). The color IS the brand. No scales, no systems, no tokens. Most libraries never get beyond this level of color investment — and they don't need to. The successful ones pick a color that's distinctive in NuGet search results and consistent with their personality.

**Notable pattern**: Warm colors (red, orange, gold) stand out in the .NET ecosystem because Microsoft's own palette leans cool (blue, purple). Libraries that go warm are easier to spot.

### Tier 2: Developer tools with design ambitions

| Tool | Color approach | Key insight |
|------|---------------|------------|
| Tailwind CSS | Sky blue accent on white/dark | The blue is iconic. Tailwind owns "that specific blue" in the CSS framework space. One color, used everywhere. |
| Prisma | Dark indigo/navy + teal accent | Two-color system. Dark = authority. Teal = the "different" accent that sets it apart from generic dev tools. |
| Next.js | Pure black and white | Zero color. The absence of color IS the brand. Monochrome projects extreme confidence. |
| Astro | Purple/violet gradient | Gradient gives energy and modernity. Works because the brand personality is "fun and fast." |
| Remix | Blue + dark | Slight retro feel. The blue is distinctive but restrained. |

**Pattern at tool scale**: Still 1–2 colors, but applied more deliberately. The color is chosen to *feel like the product*.

### Tier 3: Platforms (aspirational reference)

| Platform | Color system | Key insight |
|----------|-------------|------------|
| Vercel (Geist) | 10 semantic color scales, 10 neutral steps per scale. Gray, blue, red, amber, green, teal, purple, pink backgrounds. Functional roles: backgrounds (steps 1–3), borders (4–6), high-contrast bg (7–8), text (9–10). | Full design system. Dark-first. High contrast. Geist uses P3 colors on supported displays. |
| GitHub (Primer) | Neutral scales 0–13 with inverted light/dark direction. Semantic roles: accent (blue), success (green), danger (red), warning (yellow), done (purple), sponsors (pink). Three token tiers: base → functional → component. | Enterprise-grade system. Key insight: base tokens are *never used directly* — always through functional tokens. This prevents coupling to raw values. |
| Linear | Two brand colors only: Mercury White (#F4F5F8) and Nordic Gray (#222326). Desaturated blue reserved for backgrounds. | Extreme restraint. Two neutrals + one subtle accent. The brand is the *absence* of color noise. Linear proves you can build a premium brand with near-zero color. |
| Stripe | "Blurple" (#635BFF) + slate. Three logo colors: slate, blurple, white. | One ownable color ("blurple" is literally their word). The name itself is distinctive. The color is so specific it's a brand asset. |

---

## Color strategy principles

### From industry research and design systems

1. **Start with one color.** Linear, Stripe, Tailwind, and Next.js all prove that one accent (or none) is enough. Adding a second color is a design decision, not a default.

2. **Own a specific hue.** Stripe's "blurple" is memorable because it's between blue and purple — a no-man's-land most brands avoid. Picking a color that's *slightly off* from the obvious choice makes it ownable.

3. **Dark-first for developer tools.** Vercel Geist, GitHub's dark theme, and most terminal-adjacent tools default to dark backgrounds. Developers spend their days in dark editors. A brand that feels native to that context earns trust.

4. **Color carries meaning sparingly.** The Primer system uses semantic colors (green=success, red=danger) only for *functional UI states*, not branding. Brand color and semantic color are separate concerns.

5. **Monochrome first, color second.** From the icon research: if the mark doesn't work in pure black and white, color won't save it. Color should *enhance* an already-working form, not compensate for weakness.

6. **Contrast is non-negotiable.** Primer requires minimum 7:1 contrast for high-contrast themes. Geist defines explicit text/icon steps (9–10) that guarantee readability. Any brand color must clear WCAG AA on both light and dark backgrounds.

7. **The .NET ecosystem is purple.** Microsoft's .NET brand uses purple/violet heavily. Many .NET libraries lean into this (MediatR, NuGet itself). Going purple is safe but undifferentiated. Going *away* from purple is a deliberate brand choice.

---

## Color directions for Precept

### What the product personality suggests

Precept's personality (from philosophy.md):
- Governed, visible, sound
- Prevention, not detection
- Structural certainty
- Small, exact, self-contained

This maps to: **cool, precise, authoritative, high-contrast.** Not warm. Not playful. Not gradient-heavy.

### Option A: Deep teal / dark cyan

- Hex neighborhood: #0D9488 to #115E59
- **Why**: Cool but not cold. Sits between blue (trust, authority) and green (safety, validity). Teal is underused in the .NET ecosystem. Reads as "precise and sound" without being generic blue.
- **Risk**: Could feel medical/clinical without warmth.
- **References**: Prisma uses a similar range. Tailwind's teal scale.

### Option B: Slate blue / steel

- Hex neighborhood: #475569 to #64748B
- **Why**: The color of structural materials — steel, brushed metal. Projects weight and permanence. Works naturally on dark backgrounds.
- **Risk**: Low saturation can feel generic or absent. Needs a sharper accent for emphasis.
- **References**: Linear's Nordic Gray. Vercel's gray-first approach.

### Option C: Deep indigo

- Hex neighborhood: #3730A3 to #4338CA
- **Why**: Authority, depth, precision. Sits between blue and purple — distinctive enough to own. Has the weight of a structural material.
- **Risk**: Closer to .NET's purple ecosystem. Must be distinctly "not Microsoft purple."
- **References**: Stripe's "blurple" approach. Indigo scale in Tailwind.

### Option D: No brand color (monochrome only)

- Just black, white, and grays.
- **Why**: Next.js proves this works. The icon and wordmark carry the brand alone. No color to manage, no palette conflicts.
- **Risk**: Needs a very strong icon to compensate. Harder to spot in a crowded NuGet search.
- **References**: Next.js, Vercel (nearly monochrome).

---

## Minimum viable color system

For a GitHub library, the practical system is:

```
Brand color:      [one ownable accent]
Text:             neutral-900 (light) / neutral-100 (dark)
Muted text:       neutral-500
Background:       white (light) / neutral-950 (dark)
Border:           neutral-200 (light) / neutral-800 (dark)
Accent on dark:   [brand color at full saturation]
Accent on light:  [brand color darkened for contrast]
```

This covers: README badges, NuGet icon, VS Code extension chrome, docs site. Everything else can be added later.

---

## What NOT to do

- Don't pick a full 11-step scale before you have a one-page docs site. You'll redesign it.
- Don't use semantic colors (green=good, red=bad) in the brand palette. Those belong to the UI, not the identity.
- Don't match the .NET purple ecosystem unless you're deliberately leveraging it.
- Don't use gradients in the core brand color. Gradients age fast and complicate reproduction across surfaces.
