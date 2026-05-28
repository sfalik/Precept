---
status: Cited
authored: 2026-05-28
author: Claude (sub-agent, on behalf of Shane)
topic: Whitespace handling across every single-quoted typed-constant / qualifier surface in Precept — is "lenient on spaces around the composite period-basis `+`" consistent with existing treatment or a departure?
external-engagement: purely-internal
---

# Literal Whitespace Consistency Survey — `'...'` Typed-Constant / Qualifier Surfaces

> Across every single-quoted `'...'` surface in Precept, how is internal whitespace treated — required, forbidden, or tolerated — and would leniency around the new composite period-basis `+` separator (`'hours+minutes'` vs `'hours + minutes'`) follow the existing convention or invent a new one?

## Background

D4 in [`docs/language/business-domain-types.md`](../../../docs/language/business-domain-types.md) has been amended to use `+` (not `&`) as the composite period-basis separator: `period in 'hours+minutes'`. The amendment was grounded by [`period-basis-separator-survey.md`](period-basis-separator-survey.md), which established that `+` matches the period **value-literal** combiner `'1 year + 6 months'` (temporal design D17) and that NodaTime imposes no separator convention.

A spacing question remained open after that survey, and D4 as currently written does not resolve it: should the parser be **lenient** about whitespace around the basis `+` (accept both `'hours+minutes'` and `'hours + minutes'`), and what is the **canonical** form `.basis` returns / hover and MCP show / proof markers carry? The spec already fixes the canonical *order* (coarse-to-fine: `years → months → weeks → days → hours → minutes → seconds`, `business-domain-types.md:1319`) and shows all canonical examples compact (`'hours+minutes'`), but never states the whitespace rule explicitly.

Before locking a whitespace rule for one new surface in isolation, this survey establishes whether Precept already has a *consistent* whitespace convention across its `'...'` surfaces that the basis decision should simply follow.

## Methodology

**Research question:** Does Precept have an existing, consistent whitespace convention for the interior of `'...'` typed-constant / qualifier strings, and is "lenient on spaces around the composite-basis `+`" consistent with it or a departure?

**Search strategy (purely internal — no external comparators):**

- **Canonical spec docs:** `docs/compiler/literal-system.md` (the two-door model and the type-grammar slot-classification notation), `docs/language/business-domain-types.md` (money/quantity/price/period qualifier grammar + the amended D4), `docs/language/temporal-type-system.md` (value-literal type grammar around `+`).
- **Ground-truth source (weighted highest — code over docs where they disagree):** every validator under `src/Precept/Language/` that processes `'...'` content (`CurrencyValidator`, `MoneyValidator`, `QuantityValidator`, `PriceValidator`, `ExchangeRateValidator`, `ClosedSetValidator`, `Time/TemporalValidator`, `Time/TemporalQuantityParser`, `Time/TemporalParser`), the UCUM tokenizer/parser (`Ucum/UcumLexer`, `Ucum/UcumParser`), the qualifier-declaration parse path (`Pipeline/Parser.Types.cs`, `Pipeline/QualifierUnitHelpers.cs`).
- **Empirical verification:** the `precept_compile` MCP tool, fed deliberate whitespace variants of each surface, to observe actual accept/reject behavior rather than infer it from the code alone.
- **De-facto convention:** `grep` across `samples/*.precept` for the whitespace authors actually write inside qualifier/literal strings.

**Inclusion criteria:** Every distinct `'...'`-delimited typed-constant or qualifier surface in the language and the code path that validates its interior. Internal whitespace specifically (leading/trailing trim, magnitude–unit gap, separator-adjacent spaces around `/` and `+`).

**Exclusion criteria** (per brief): external language comparators; the `&` vs `+` separator choice (settled — see prior survey); whether composite basis should exist (settled).

**Source-grade mix:** Primary throughout — Precept's own source code, canonical spec docs, shipped sample files, and live `precept_compile` output. No external sources, so no Secondary/Tertiary grades apply.

**Time bounds:** Investigation ran 2026-05-28. All source reads and `precept_compile` runs on that date against the working tree on branch `spike/Precept-V2-Radical`.

**Folder placement justification:** Filed in `research/language/expressiveness/` rather than `research/architecture/compiler/` because the deliverable feeds a *language-surface* decision (the canonical form of a qualifier — what authors type, what `.basis` returns, what hover shows) and directly extends `period-basis-separator-survey.md`, which lives here. The implementation paths are the *evidence*; the decision is a language one.

## Findings

### Finding 1 — There are two distinct whitespace regimes, split by what the interior content *is*, not by the delimiter.

Every `'...'` surface uses the same delimiter, but two different parsing regimes process the interior:

- **Regex/field validators** (money, quantity-magnitude, price, exchangerate, temporal-quantity value literals): the whole interior is `.Trim()`-ed once (leading/trailing only), then matched against a regex that fixes the internal spacing.
- **UCUM unit expressions** (the unit portion of `quantity` / `price` / the `in '<unit>'` qualifier): tokenized by a whitespace-skipping lexer, so internal whitespace is fully ignored.

These two regimes disagree about internal whitespace. The decisive variable is *which sub-parser owns the bytes*, not the quote.

### Finding 2 — The regex/field validators forbid no-space magnitude–unit, require `\s+` there, and forbid spaces around `/`. Leading/trailing is trimmed.

**Money** [Primary — `src/Precept/Language/MoneyValidator.cs:8-9`]:

> ```csharp
> private static readonly Regex Pattern =
>     new(@"^([+-]?\d+(?:\.\d+)?)\s+([A-Za-z]{3})$", RegexOptions.Compiled);
> ```
> ...
> ```csharp
> var match = Pattern.Match(rawText.Trim());
> ```

The `\s+` between magnitude and currency is **mandatory** (one-or-more whitespace); `'100USD'` cannot match. `rawText.Trim()` removes only leading/trailing whitespace. **Empirically confirmed** via `precept_compile`:

- `money default '100USD'` → `PRE0053 '100USD' is not a valid money`
- `money default '100  USD'` (two spaces) → accepted (only the unrelated PRE0158 no-write-site warning)

So the magnitude–unit gap is *required* (`\s+`) and *lenient on count* (one or more spaces collapse to the same match), but the canonical reconstruction is always a single space (`MoneyValidator.cs:24`: `$"{amount...} {currencyResult.CanonicalText}"`).

**Price** [Primary — `src/Precept/Language/PriceValidator.cs:8-9`]:

> ```csharp
> private static readonly Regex Pattern =
>     new(@"^([+-]?\d+(?:\.\d+)?)\s+([A-Za-z]{3})/(.+)$", RegexOptions.Compiled);
> ```

The `/` is wedged directly between `[A-Za-z]{3}` and `(.+)` with **no whitespace allowance**. A space before `/` makes the currency capture `'USD '` and fail ISO-4217 lookup. **Empirically confirmed:**

- `price in 'USD / kg'` → `PRE0076 'USD ' is not a recognized ISO 4217 currency code`
- `price in 'USD/kg'` → accepted

**Exchangerate** [Primary — `src/Precept/Language/ExchangeRateValidator.cs:8-9`]: identical shape, `^([+-]?\d+(?:\.\d+)?)\s+([A-Za-z]{3})/([A-Za-z]{3})$` — `/` between two currency codes, no surrounding whitespace tolerated.

**Quantity magnitude** [Primary — `src/Precept/Language/QuantityValidator.cs:8-9, 17`]: `^([+-]?\d+(?:\.\d+)?)\s+(.+)$` on `rawText.Trim()`. Same `\s+` requirement on the magnitude–unit gap; the unit portion `(.+)` is handed to the UCUM parser (Finding 3). **Empirically confirmed:** `quantity in 'each/case' default '5each/case'` → `PRE0053 '5each/case' is not a valid quantity`.

**Currency** [Primary — `src/Precept/Language/CurrencyValidator.cs:7`]: `rawText.Trim().ToUpperInvariant()` then catalog lookup — leading/trailing trimmed, case-folded; an atomic code has no internal separator to space.

**Temporal value literals** [Primary — `src/Precept/Language/Time/TemporalQuantityParser.cs:9, 16, 76`]:

> ```csharp
> private static readonly Regex PartPattern =
>     new(@"^([+-]?\d+)\s+([A-Za-z]+)$", RegexOptions.Compiled);
> ...
> var parts = rawText.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
> ...
> var canonicalText = string.Join(" + ", normalizedParts);
> ```

This is the **direct precedent for the basis `+`** (see Finding 5). The magnitude–unit gap inside each component requires `\s+` (so `'30days'` is rejected — empirically `'72hours'` → `PRE0053`), and the `+` combiner is split with `TrimEntries`, so whitespace around `+` is fully absorbed.

### Finding 3 — The UCUM unit parser ignores internal whitespace entirely. `'each / case'` and `'each/case'` are the same unit.

[Primary — `src/Precept/Language/Ucum/UcumLexer.cs:13-17`]:

> ```csharp
> var current = expression[index];
> if (char.IsWhiteSpace(current))
> {
>     index++;
>     continue;
> }
> ```

The UCUM lexer skips every whitespace character before tokenizing. So spaces anywhere inside a UCUM expression — including around the ratio `/` — are discarded. **Empirically confirmed:** `quantity in 'each / case'` compiles with no error (only PRE0158). The canonical code is reconstructed without spaces (`UcumParser.cs:211`: `$"{left.CanonicalCode}/{denominatorCode}"`).

This is the one surface that is *lenient* about spaces around `/`. It directly contradicts the price/exchangerate `/` (Finding 2), which is strict — because the price `/` is matched by a field regex *before* the unit substring ever reaches UCUM, whereas the quantity unit `/` lives entirely inside the UCUM substring.

### Finding 4 — Date / time / datetime / instant / zoneddatetime / timezone literals are format-rigid via NodaTime ISO patterns; no pre-trim, no internal-whitespace tolerance.

[Primary — `src/Precept/Language/Time/TemporalParser.cs:30, 44, 62, 79`]: each kind calls a NodaTime pattern directly — `LocalDatePattern.Iso.Parse(rawText)`, `LocalTimePattern.ExtendedIso.Parse(rawText)`, `LocalDateTimePattern.ExtendedIso.Parse(rawText)`, `InstantPattern.ExtendedIso.Parse(rawText)` — with no `.Trim()` and no whitespace normalization. These ISO patterns are positional and reject stray internal whitespace by construction. `timezone` (`'America/New_York'`) and `stateref` are atomic identifiers; the `/` in a zone id is part of the IANA name, not a ratio. `stateref` / `dimension` via `ClosedSetValidator.cs:7` does a bare `AllowedValues.Contains(rawText)` with **no trim at all** — the strictest surface of all (even leading/trailing whitespace fails).

### Finding 5 — The most direct precedent — the value-literal `+` combiner — is fully lenient on surrounding whitespace and canonicalizes to a single spaced `+`.

The basis `+` being decided is the same character, same delimiter, same period type as the value-literal `+`. The value-literal `+` (`TemporalQuantityParser.cs:16`) splits with `StringSplitOptions.TrimEntries`, so every surrounding-whitespace variant is accepted. **Empirically confirmed:**

- `period default '1 year + 6 months'` (spaced) → accepted
- `period default '1 year+6 months'` (compact) → accepted
- `period default '1 year +6 months'` (asymmetric) → accepted

And the canonical output is a single spaced `+` (`canonicalText = string.Join(" + ", normalizedParts)`, line 76). The literal-system doc's type-grammar notation matches this: compound forms are written `H[m₁] T(' ') T(tu₁) T(' + ') H[m₂] ...` — the fixed separator token is `T(' + ')`, spaced ([Primary — `docs/compiler/literal-system.md:284`; `docs/language/temporal-type-system.md:126`]).

So the immediately adjacent surface — the one D4 itself cites as the reason `+` was chosen (`business-domain-types.md:1317`: *"the same combiner used in period value literals (`'1 year + 6 months'`, D17)"*) — is **lenient on input, spaced on canonical**.

### Finding 6 — The composite period-basis qualifier does not exist in the implementation yet; the whitespace decision is genuinely forward-looking for that surface.

[Primary — empirical] `period in 'hours+minutes'` and `period in 'hours + minutes'` both fail identically today with `PRE0118 'hours+minutes' is not a recognized temporal unit`. The qualifier-value path validates the basis string against the *single*-temporal-unit set; the composite form is unrecognized regardless of spacing. This confirms the prior survey's impl-status note (`period-basis-separator-survey.md` Finding 1: the `&`/`+` basis separator "is not implemented"). The decision therefore sets behavior for a surface being built, not a behavior already shipped — but it should still align with the established conventions above.

### Finding 7 — The de-facto sample convention is uniformly compact; no shipped sample puts spaces around `/` or `+` inside a qualifier/literal.

[Primary — `grep` across `samples/*.precept`, 2026-05-28] Every compound unit qualifier is compact:

- `samples/equipment-lease-agreement.precept:49` — `field HourlyOverageRate as price in 'USD/h'`
- `samples/inventory-item.precept:45` — `field StockingUnitsPerPurchaseUnit as quantity in '{StockingUnit}/{PurchaseUnit}' default '1 {StockingUnit}/{PurchaseUnit}'`

No sample writes `'USD / h'`, `'1 year + 6 months'`, or `'1 year+6 months'` — composite period value literals do not appear in the corpus at all, and every temporal value literal is the single `'<n> <unit>'` form (`'72 hours'`, `'30 days'`, `'365 days'`). Searches for spaced `/`/`+` inside quotes and for no-space `'NNunit'` forms both return zero hits. Authors write magnitude-then-single-space-then-unit, and compact ratios.

## Threats to Validity

- **Empirical surface is the working tree, not a release.** The `precept_compile` runs reflect branch `spike/Precept-V2-Radical` on 2026-05-28. If the validators are refactored before the basis qualifier is implemented, the regex/lexer specifics in Findings 2-3 could shift. The *direction* (regex-fixed vs UCUM-lenient) is structural and unlikely to flip, but the exact `\s+`/`Trim` details are version-bound.
- **Interpolated-qualifier path not separately whitespace-tested.** Findings cover static `'...'` content. Interpolated typed constants (`'{X} days'`) re-parse after substitution through the same validators, so the same whitespace rules apply post-substitution — but I did not run interpolated-variant compile tests for every surface. D4 already forbids interpolated *composite* bases (`business-domain-types.md:1325`), so the basis decision is unaffected.
- **"Lenient on count" vs "lenient on presence" conflation risk.** The regex validators are lenient on the *number* of spaces in a required gap (`\s+` matches one-or-more) but strict on *presence* (the gap cannot be zero where required, and cannot be inserted where forbidden). A reader could over-read "lenient" — the leniency is narrow. Flagged so the consuming decision doesn't generalize it.
- **Single-author empirical loop.** All compile probes were authored by the investigator; a different probe set could surface an edge I didn't hit (e.g., tab vs space, Unicode whitespace). `\s` and `char.IsWhiteSpace` cover both, so low risk.

## Implications for Precept

The language does **not** have a single uniform whitespace convention across `'...'` — it has a coherent *pair* of conventions split by content type (Finding 1). But for the specific decision at hand, the relevant comparators all point the same way:

1. **The required-gap surfaces (money/quantity/price/exchangerate/temporal-value) are lenient on whitespace *count* and canonicalize to a single space.** None of them reject extra spaces in a gap that legitimately holds whitespace; all of them reconstruct a normalized single-spaced canonical form.
2. **Spaces around `/` are forbidden where `/` is a field-regex separator (price/exchangerate) but ignored where `/` is inside a UCUM expression (quantity unit).** This is the one genuine inconsistency in the language — and it is invisible to most authors because the de-facto convention (Finding 7) is uniformly compact.
3. **The single most-relevant precedent — the value-literal `+`, same character / delimiter / type — is lenient on surrounding whitespace and canonicalizes to spaced `+` (`' + '`).** D4 explicitly invokes this surface as the justification for choosing `+`. Diverging on whitespace would make the *same character in the same delimiter on the same type* behave differently between the value position and the type-qualifier position.
4. **The de-facto author convention is compact** (`'USD/h'`), so a compact canonical form matches what authors already write and read in the corpus.

These pull slightly differently on *canonical form*: the value-literal `+` precedent says spaced (`'hours + minutes'`); the sample/UCUM-ratio convention and the spec's own D4 examples say compact (`'hours+minutes'`). They agree completely on *input leniency*: accept both.

## Conclusions

### Conclusion 1 — Input leniency around the basis `+` is fully consistent with existing treatment; reject is the departure.

- **Rationale:** Every gap-bearing `'...'` surface in the language is lenient on whitespace *count* (money/quantity/price/exchangerate via `\s+`; the value-literal `+` via `TrimEntries`). The value-literal `+` — the same character in the same delimiter on the same `period` type — accepts `'1 year + 6 months'`, `'1 year+6 months'`, and `'1 year +6 months'` interchangeably (Finding 5, empirically confirmed). A basis `+` that *rejected* `'hours + minutes'` while the value `+` *accepted* `'1 year + 6 months'` would be the inconsistency, not the leniency.
- **Alternatives considered and rejected:**
  - *Strict-compact (reject any space around basis `+`).* Rejected: it contradicts the value-literal `+` precedent that D4 itself cites, and contradicts the count-leniency of every other gap surface. It would also surprise an author who learned the value-literal `+` first.
  - *Strict-spaced (require spaces around basis `+`).* Rejected: contradicts the compact de-facto sample convention (Finding 7) and the spec's own compact D4 examples; no other Precept surface *requires* separator-adjacent spaces.
- **Precedent:** `TemporalQuantityParser.cs:16` (`Split('+', ... TrimEntries ...)`) and the empirical accept of all three spacing variants of `'1 year + 6 months'`; `MoneyValidator`/`QuantityValidator` `\s+` count-leniency.
- **Tradeoff accepted:** "Lenient" is narrow — it means lenient on the *count* of whitespace around the separator, not a license to insert whitespace inside atoms. The basis tokenizer must still reject genuinely malformed forms (e.g., `'hours +'`, empty components), exactly as `TrimEntries | RemoveEmptyEntries` already does for the value `+`.

### Conclusion 2 — Canonical form should be compact (`'hours+minutes'`), matching the spec's D4 examples, the de-facto sample convention, and the UCUM-ratio canonicalization — i.e., recommendation (b): lenient input + compact canonical.

- **Rationale:** The basis string is a *type-qualifier* (a list of unit categories), closer in kind to the UCUM ratio `'USD/h'` and the spec's own composite-basis table (all compact, `business-domain-types.md:1311-1315`) than to a value expression. Authors in the corpus write compact ratios uniformly (Finding 7). The spec already commits to compact canonical examples and a canonical *order*; making the canonical *spacing* compact too keeps `.basis`, hover, MCP, and proof markers (`$eq:X.basis:hours+minutes`, `business-domain-types.md:1323`) aligned with what's already written down. The one precedent pulling toward spaced canonical (the value `+`, which joins with `" + "`) is a *value*-construction surface where the components are magnitude+unit phrases that read as prose (`'1 year + 6 months'`); a basis is a terse category list (`'hours+minutes'`) where compact reads better and matches the ratio sibling.
- **Alternatives considered and rejected:**
  - *Spaced canonical (`'hours + minutes'`), recommendation (a).* Rejected: it would force `.basis` and the proof marker to diverge from the compact spelling already committed in D4 (`business-domain-types.md:1313, 1323`), and from the compact ratio convention authors already use. Choosing it would require re-editing the spec's existing canonical examples.
  - *Strict-compact (recommendation c).* Rejected per Conclusion 1 — input must be lenient.
  - *Follow-dominant-convention without naming it (recommendation d).* Subsumed: the dominant convention *for separators inside qualifier strings* is compact (UCUM `/`, sample `/`, D4's own examples), so (d) resolves to (b). The value-literal `+`'s spaced canonical is the minority and is a value, not a qualifier.
- **Precedent:** `business-domain-types.md:1311-1323` (compact composite-basis table + compact proof marker); `samples/equipment-lease-agreement.precept:49` and `samples/inventory-item.precept:45` (compact ratios); `UcumParser.cs:211` (compact `/` canonicalization).
- **Tradeoff accepted:** Compact canonical diverges from the value-literal `+`'s spaced canonical, so the language will show `'1 year + 6 months'` (value) but `'hours+minutes'` (basis) — two canonical spacings for the same character. This is justified by the different *kind* of the two surfaces (value phrase vs terse category list) and is bounded: the *input* rule is identical (both lenient), so an author never has to remember two input rules — only the normalizer differs, and the normalizer's output matches the sibling each surface most resembles (value `+` → prose-spaced; basis `+` → ratio-compact).

## What would change this conclusion

- **If the basis `+` is implemented by *reusing* `TemporalQuantityParser` (which joins with `" + "`)**, the path of least resistance produces *spaced* canonical, flipping Conclusion 2 toward (a) on implementation-cost grounds. If that reuse is chosen, the spec's compact D4 examples and the `$eq:` marker must be re-spelled to spaced — or the reused joiner overridden for the basis path. Likelihood: moderate — the basis parser is a natural cousin of the value parser.
- **If a future author-usability signal shows compact basis lists (`'days+hours+minutes'`) are hard to read** and spaced reads materially better, the canonical-form leg of Conclusion 2 weakens (input leniency in Conclusion 1 is unaffected). Likelihood: low for 2-3 components.
- **If the price/exchangerate `/` regex is later relaxed to tolerate spaces** (making the whole language uniformly space-lenient around separators), the "compact is the dominant separator convention" premise of Conclusion 2(d) weakens — though the sample convention would still favor compact canonical. Likelihood: low; no open finding requests it.

## Open Questions

- **Which code path implements the composite-basis qualifier validator** — a new basis-specific splitter, or reuse of `TemporalQuantityParser`? This determines the default canonical spacing (see first falsifier) and is an implementation decision for the F-LANG-BIZ-07 build, not this survey.
- **Should the price/exchangerate `/` be made whitespace-lenient for consistency with the UCUM `/`?** Surfaced here as a genuine internal inconsistency (Finding 2 vs 3). It is out of scope for the basis decision but worth a separate finding if uniformity is a goal. The de-facto compact convention (Finding 7) means no author is currently affected.

## Sources

All Primary (internal Precept artifacts and live tooling output; no external sources).

- **`src/Precept/Language/MoneyValidator.cs`** (lines 8-9, 13, 24) — money regex + trim + canonical reconstruction. Accessed 2026-05-28.
- **`src/Precept/Language/PriceValidator.cs`** (lines 8-9, 13, 30) — price regex with strict `/`. Accessed 2026-05-28.
- **`src/Precept/Language/ExchangeRateValidator.cs`** (lines 8-9) — exchangerate regex with strict `/`. Accessed 2026-05-28.
- **`src/Precept/Language/QuantityValidator.cs`** (lines 8-9, 17, 71) — quantity magnitude regex + UCUM hand-off + canonical. Accessed 2026-05-28.
- **`src/Precept/Language/CurrencyValidator.cs`** (line 7) — trim + case-fold. Accessed 2026-05-28.
- **`src/Precept/Language/ClosedSetValidator.cs`** (line 7) — no-trim `Contains`. Accessed 2026-05-28.
- **`src/Precept/Language/Time/TemporalQuantityParser.cs`** (lines 9, 16, 76) — value-literal `+` split with `TrimEntries`, spaced canonical join. Accessed 2026-05-28.
- **`src/Precept/Language/Time/TemporalParser.cs`** (lines 16, 30, 44, 62, 79) — NodaTime ISO pattern dispatch, no pre-trim. Accessed 2026-05-28.
- **`src/Precept/Language/Ucum/UcumLexer.cs`** (lines 13-17) — whitespace-skipping tokenizer. Accessed 2026-05-28.
- **`src/Precept/Language/Ucum/UcumParser.cs`** (line 211) — compact `/` canonicalization. Accessed 2026-05-28.
- **`src/Precept/Pipeline/Parser.Types.cs`** (lines 260-307) — qualifier-declaration parse path; hands raw `'...'` text to validators. Accessed 2026-05-28.
- **`docs/compiler/literal-system.md`** (lines 276-296) — type-grammar `T(' + ')` / `T(' ')` / `T('/')` separator notation. Accessed 2026-05-28.
- **`docs/language/business-domain-types.md`** (lines 1307-1325) — amended D4 (`+` separator), composite-basis table, canonicalization, compact proof marker. Accessed 2026-05-28.
- **`docs/language/temporal-type-system.md`** (lines 126, 201) — value-literal compound type grammar; `+` as sole combiner. Accessed 2026-05-28.
- **`samples/equipment-lease-agreement.precept:49`, `samples/inventory-item.precept:45`** — de-facto compact ratio convention. Accessed 2026-05-28.
- **`precept_compile` MCP tool** — empirical accept/reject across whitespace variants of money/quantity/price/period/duration/basis surfaces. Run 2026-05-28.
- **`research/language/expressiveness/period-basis-separator-survey.md`** — prior survey establishing the `+` separator choice and the basis-qualifier impl gap; this file extends it. Accessed 2026-05-28.
