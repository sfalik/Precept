Typography research — how developer tools and open-source libraries make type choices.

Purpose:
- Study typography decisions across the spectrum: GitHub libraries → developer tools → platforms.
- Extract principles for choosing typefaces that work at library scale (README, docs, VS Code, NuGet listing).
- Ground in real usage, not aspirational design systems.

Sources referenced:
- Inter typeface documentation (rsms.me/inter)
- Vercel Geist font and design system (vercel.com/geist, vercel.com/font)
- Google Fonts "Pairing typefaces" guide (fonts.google.com/knowledge/choosing_type/pairing_typefaces)
- GitHub Primer typography (primer.style)
- Observed typography usage across open-source libraries and developer tools

---

## Scale matters — what Precept actually needs

Precept's typographic surfaces today:
- **README.md** — rendered by GitHub's own stylesheet (you don't control the font)
- **NuGet package listing** — rendered by nuget.org (you don't control the font)
- **VS Code extension** — rendered by VS Code's own UI (you mostly don't control the font)
- **Code samples** — displayed in whatever monospace font the user's editor uses
- **Docs site** — IF one exists, this is the first surface where type choices matter

What you CAN control:
- Logo / wordmark typeface (the "Precept" word set in a specific font)
- Documentation site typography (when you build one)
- Marketing materials (when they exist)

What you CANNOT control (and shouldn't fight):
- How code renders in editors
- How README renders on GitHub
- How the NuGet listing looks

**Implication**: The typography "system" for a library is really just one decision: what typeface is the wordmark set in? Everything else is inherited from the platform.

---

## How libraries and tools handle typography

### Tier 1: GitHub libraries (our actual scale)

| Library | Typography approach | What it tells us |
|---------|-------------------|-----------------|
| Serilog | No custom wordmark. Plain text name in README. Logo is an icon (needle/thread), not a wordmark. | The brand is the icon + the name. No typeface investment. Most libraries are here. |
| Polly | Logo includes "Polly" in a custom sans-serif setting. Clean, professional, nothing fancy. | The wordmark is the minimum viable typography investment — a name in a clean font, placed next to an icon. |
| Dapper | No wordmark. Just the text name. | At 638M downloads, Dapper proves you can be enormously successful with zero typographic investment. |
| Newtonsoft.Json | Square "N" logo. No wordmark. Name rendered in platform defaults wherever it appears. | 7.9 billion downloads. The most-used .NET package in history. No wordmark. The name does all the work. |
| FluentValidation | No logo, no wordmark. Text only. | Brand recognition comes entirely from the name and the API's fluency. Typography is irrelevant at this scale. |
| xUnit | Inherits .NET Foundation branding. No custom type. | Institutional affiliation provides the visual framework. |
| AutoMapper | Simple wordmark, "AutoMapper" in a straightforward sans-serif. | Minimal but intentional. The font choice is clean and functional — it says "this is a tool, not a toy." |
| MediatR | No custom wordmark. | The CamelCase name formatting (MediatR with a capital R) IS the typographic identity. The casing pattern makes it recognizable without a font. |

**Pattern at library scale**: Most successful .NET libraries have zero typographic investment beyond the name itself. The ones that do invest use a clean sans-serif wordmark. Nobody uses a serif, nobody uses a display font, nobody uses custom lettering. The typeface is the least important brand decision at library scale.

**The name IS the typography**: Serilog, MediatR, Dapper, Polly — these names are short, distinctive, and visually recognizable in any font. The typographic identity comes from the word shape, not the glyph shape.

### Tier 2: Developer tools (next level up)

| Tool | Type approach | Key insight |
|------|-------------|------------|
| Tailwind CSS | Custom wordmark in a geometric sans-serif. Documentation uses Inter or system fonts. | Clean, modern, developer-friendly. The wordmark has slight character but nothing showy. |
| Prisma | Custom geometric sans-serif wordmark. Docs use Inter. | Same pattern: custom wordmark + system/Inter for body text. Dev tools converge here. |
| Next.js | Bold "N" lettermark + "Next.js" in a clean grotesque. Docs use Geist Sans. | Extreme typographic restraint. Two weights (regular and bold), one typeface family, monochrome. |
| Astro | Custom wordmark with slight personality (rounded terminals). Docs use a standard system stack. | The wordmark carries a bit more personality — matching Astro's "fun" brand personality. |
| Remix | Geometric sans-serif wordmark. Confident, no-frills. | Developer tools rarely deviate from the geometric sans-serif pattern. |

**Pattern at tool scale**: Custom wordmark (geometric sans, slightly distinctive) + Inter or system fonts for documentation. This is the sweet spot for developer tools that want to look professional without overinvesting.

### Tier 3: Platforms (aspirational reference)

| Platform | Type system | Key insight |
|----------|-----------|------------|
| Vercel (Geist) | Two custom typefaces: Geist Sans and Geist Mono. Designed specifically for developers. Geist Sans has clean lines at small sizes; Geist Mono is optimized for code. Available at vercel.com/font. | Custom typeface is a major brand investment. Vercel can justify it — they're a platform used daily. For a library, this is overkill. |
| GitHub (Primer) | Uses system font stack for body, Mona Sans and Hubot Sans for brand/marketing. | Even GitHub — one of the largest dev platforms — uses system fonts for product UI. Custom fonts are reserved for brand expression, not product. |
| Linear | Custom wordmark. Product uses system font stack or a refined Inter-like sans. Two brand colors, spare typography. | Extreme restraint. The type system is practically invisible — which IS the brand. "We removed everything unnecessary." |
| Stripe | Custom wordmark. Docs use a clean system stack. Marketing uses carefully chosen typefaces per campaign. | Documentation — where developers spend the most time — uses the simplest possible type. Custom type is reserved for marketing moments. |

---

## Typeface families worth knowing

### The developer-tool default stack

These are the typefaces that appear most often in developer tool documentation and marketing:

**Sans-serif (body/UI text)**:
- **Inter** (rsms.me/inter) — The de facto standard for developer tools. Free, open source. Designed by Rasmus Andersson specifically for screens. Tall x-height for legibility at small sizes. 2000+ glyphs, 147 languages, weights 100–900. Variable font with optical sizing. Used by Figma, Linear, Notion, and hundreds of others.
- **Geist Sans** (vercel.com/font) — Vercel's custom typeface. Clean, tight, developer-optimized. Free.
- **System font stack** (`-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, ...`) — Zero network cost, guaranteed legibility, native feel. GitHub Primer uses this for product UI.

**Monospace (code)**:
- **Geist Mono** — Vercel's code font. Clean ligatures.
- **JetBrains Mono** — Popular in IDEs. Ligature support.
- **Fira Code** — Mozilla-originated. Ligature-heavy.
- **Cascadia Code** — Microsoft's terminal font. Comes with Windows Terminal.
- System monospace stack (`ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace`) — what most docs use.

### Google Fonts advice on pairing (fonts.google.com/knowledge)

From the Google Fonts guide on pairing typefaces (Elliot Jay Stocks):

1. **Do you really need a second typeface?** Often one family with weight and size variation is enough. Don't add a pairing just to add variety.
2. **If you pair, create contrast.** Serif + sans-serif is the safest pairing. Two typefaces too similar to each other create "dissonance" — like musical notes too close together.
3. **Jessica Hische's family model**: Think of type pairings as siblings, cousins, or distant relatives. Siblings share x-height, contrast, width, and mood. Cousins share two or three of those. Distant relatives share only one.
4. **Check the x-height matches** when pairing. Mismatched x-heights make inline pairing look awkward.

---

## Typography directions for Precept

### What the product personality suggests

From philosophy.md: governed, visible, sound, exact, self-contained, structural certainty.

This maps to: **geometric or neo-grotesque sans-serif for the wordmark**. Sharp, clean, no ornamentation. Not rounded, not humanist, not playful.

### Option A: Inter for everything

- Wordmark: "Precept" set in Inter SemiBold or Bold.
- Docs: Inter for body, system monospace for code.
- **Why**: Zero risk. Universally legible. Free. The "you can't go wrong" choice.
- **Risk**: Generic. Every other developer tool uses Inter. No typographic distinctiveness.

### Option B: System stack for docs, distinctive wordmark only

- Wordmark: "Precept" in a slightly distinctive geometric sans — something like Geist Sans, Söhne, or Satoshi for the wordmark only.
- Docs: System font stack.
- **Why**: The wordmark gets character; everything else stays zero-cost and maximum-legible.
- **Risk**: Minimal. The wordmark font only needs to work at display sizes.

### Option C: No wordmark — icon only

- Let the icon carry the brand. The text "Precept" is always rendered in whatever font the platform provides (GitHub, NuGet, VS Code).
- **Why**: Most successful .NET libraries do exactly this. Newtonsoft.Json, Dapper, MediatR, FluentValidation — no custom wordmarks, billions of downloads.
- **Risk**: Requires a strong icon. Harder to build recognition without a consistent typographic mark.

---

## Typography principles for Precept

1. **The wordmark is your one typography decision.** Everything else is platform-controlled. Don't build a type system until you have a surface that needs one.

2. **Match the product personality, not the trend.** Precept is precise, authoritative, and structural. The wordmark should feel like a specification, not a startup pitch.

3. **Geometric sans-serif is the safe choice for a reason.** Developer tools cluster here because geometric sans communicates precision, modernity, and technical competence. It's appropriate for Precept.

4. **Don't fight the medium.** Your README will render in GitHub's font. Your NuGet listing will render in Microsoft's font. Your VS Code extension will render in the editor's font. Typography matters only in the surfaces you control.

5. **"Precept" is a strong word shape.** Seven letters, three syllables, distinctive letter sequence (P-r-e-c-e-p-t). It's recognizable in any font. This is an asset — the name doesn't need a fancy typeface to be memorable.

6. **Monospace matters for code samples.** When you control the rendering (docs site, marketing), use a quality monospace font with ligatures for `.precept` code samples. JetBrains Mono or Fira Code both render DSL keywords well.
