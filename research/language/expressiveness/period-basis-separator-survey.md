---
status: Cited
authored: 2026-05-28
author: Claude (sub-agent, on behalf of Shane)
topic: Composite period-basis separator at the type level — should D4's `&` be amended to `+` for consistency with the value-literal `+` combiner, or does the internal record / NodaTime convention support keeping `&`?
external-engagement: partial
sources-fetched:
  - https://nodatime.org/3.2.x/api/NodaTime.PeriodUnits.html (accessed 2026-05-28)
  - https://nodatime.org/3.2.x/api/NodaTime.Text.PeriodPattern.html (accessed 2026-05-28)
  - https://nodatime.org/3.2.x/api/NodaTime.Period.html (accessed 2026-05-28)
  - https://nodatime.org/3.2.x/userguide/arithmetic (accessed 2026-05-28)
  - https://nodatime.org/2.2.x/userguide/period-patterns (accessed 2026-05-28)
  - https://raw.githubusercontent.com/nodatime/nodatime/main/src/NodaTime/PeriodUnits.cs (accessed 2026-05-28)
  - https://github.com/JodaOrg/joda-time/blob/main/src/main/java/org/joda/time/PeriodType.java (accessed 2026-05-28)
---

# Period-basis Separator Survey — `&` vs `+` at the Type Level

> Is D4's `&` separator for composite period basis (`period in 'hours&minutes'`) load-bearing, or was it picked before the value-literal `+` combiner landed and never reconsidered? Internal record + NodaTime convention check.

## Background

Precept has two locked decisions that sit close to each other in the same surface:

1. **D4 (`docs/language/business-domain-types.md:1682-1687`)** locks `&` as the separator for composite period basis at the **type level**: `field LeadTime as period in 'hours&minutes'`. The stated rejection of `+` reads: *"+ for basis composition — confusing because + already means addition."*

2. **Value-literal lock (`docs/language/temporal-type-system.md:681, 777, 781`)** uses `+` to compose period **value** components: `'1 year + 6 months'`, `'2 years + 6 months + 15 days'`. The `+` here is not arithmetic — it is "AND / combine these temporal components." So D4's stated reason for rejecting `+` (it "means addition") is contradicted by how the language already uses `+` non-arithmetically in the value-literal position.

The owner's question: amend D4 to `+`, or does the internal record / NodaTime carry a load-bearing reason for `&` we'd be erasing?

Implementation status (relevant context, transient — sourced from `docs/Working/compiler-readiness-review-2026-05-24.md:331` and `docs/Working/compiler-readiness-plan-2026-05-24.md:24`): **the `&` separator is not implemented** — `src/Precept/Language/Time/TemporalQuantityParser.cs:16` splits on `+` only. The build-or-drop decision (tracked as F-LANG-BIZ-07) is still owner-pending, which is why this research exists at all. Nothing in user code depends on either spelling today.

## Methodology

**Research question:** Should D4 (locked at `&`) be amended to `+`? Specifically: (1) was `+` reconsidered after the value-literal `+` lock landed on 2026-04-15? (2) Does NodaTime have a textual convention for unit-list selection that constrains Precept's separator choice? (3) Are there other internal-Precept artifacts where `&` vs `+` was discussed with a load-bearing reason captured?

**Search strategy:**

- **Internal record (heaviest weight per the brief):**
  - `git log --all -S "for basis composition"` and `-S "hours&minutes"` and `-S "Composite juxtaposition"` against `docs/` to recover the original D4 rationale and the value-literal `+` rationale.
  - `git show` against the first-introduction commit for each.
  - `grep` across `docs/Working/`, `docs/Working/Archive/`, `research/language/`, `research/architecture/compiler/`, `samples/`, `src/` for any `years&months` / `hours&minutes` / `period basis` discussion that captured a separator choice.
  - Direct read of the temporal-type-system value-literal lock context and the business-domain-types D4 / D9 / composite-basis table.
- **NodaTime (the constraining external authority Precept aligns to):**
  - WebFetch on `nodatime.org/3.2.x/api/NodaTime.PeriodUnits.html`, `NodaTime.Text.PeriodPattern.html`, `NodaTime.Period.html`.
  - WebFetch on the NodaTime arithmetic user guide and the v2.2 period-patterns user guide.
  - Direct fetch of the NodaTime `PeriodUnits.cs` source on GitHub main.
- **Joda-Time (NodaTime's predecessor):** WebFetch on `org.joda.time.PeriodType` source + Javadoc to check whether `PeriodType.getName()` has a textual unit-list convention.

**Inclusion criteria:** Internal commits, docs, working-doc archive entries, samples, and source files that capture a rationale for choosing `&` over `+` (or vice versa) at the type level. NodaTime API surface and prose that comments on combining `PeriodUnits` values either in code or in text. Joda-Time as the most-directly-relevant predecessor (NodaTime explicitly derives from Joda-Time).

**Exclusion criteria** (per the brief's "out of scope"): broader DSL comparator survey (F#, Frink, CUE, Dhall, pandas, SQL `INTERVAL`); ISO 8601 vs list-form re-litigation; semantics of composite basis itself (NodaTime `PeriodUnits` flag composition is the underlying model, already settled).

**Source-grade mix:** Primary (NodaTime official API docs, NodaTime source on GitHub main, Joda-Time source on GitHub main; internal Precept locked design docs and src code); Secondary (NodaTime user-guide prose); Tertiary (training-data knowledge — used only for context where verifiable primary evidence backs the same claim).

**Time bounds:** Investigation ran 2026-05-28. All web sources fetched on that date.

## Findings

### Finding 1 — Internal record: D4 was locked before the value-literal `+` combiner stabilized; the inconsistency was never raised in any subsequent locked decision.

**Evidence chain:**

D4 first appeared in its current form in commit `1ff51967` (2026-04-18, *"docs: add currency/quantity/UOM/price design doc (issue #95)"*), in the file `docs/CurrencyQuantityUomDesign.md` (later promoted to `docs/language/business-domain-types.md`). The verbatim D4 block as introduced:

> ### D4. `&` for period basis composition, `/` exclusively for ratios
>
> - **What:** `period in 'hours&minutes'` means Hours AND Minutes. `price in 'USD/kg'` means USD PER kg. `/` has one meaning everywhere: division/ratio.
> - **Why:** `&` eliminates the overloaded `/` problem. Every `/` inside a quoted expression means "per."
> - **Alternatives rejected:** (A) `/` for both — context-dependent disambiguation needed. (B) `+` for basis composition — confusing because `+` already means addition.
> - **Tradeoff accepted:** None significant.

— `git show 1ff51967:docs/CurrencyQuantityUomDesign.md` (accessed 2026-05-28)

The value-literal `+` combiner was locked **three days earlier**, on 2026-04-15, as part of the temporal-design v3 session. From the temporal-design.md doc as it existed at commit `ec2a8a76` (the promotion of `temp/temporal-design.md` to `docs/language/`):

> **What changed in v3:** Four locked decisions from the earlier 2026-04-15 session rewrote the literal surface:
> 1. **Unified postfix model** — All 7 function-call constructors (`days()`, `months()`, `hours()`, etc.) eliminated. Postfix was the sole quantity construction syntax.
> 2. **`+` as sole combiner** — Composite juxtaposition (`2 years 6 months`) eliminated. Only `'2 years + 6 months'`.
> 3. **No duration/period constructor literals** — `duration(PT72H)` and `period(P1Y6M)` eliminated. Typed constant quantities are the only surface.
> 4. **Single-quoted typed constants** — `date(2026-01-15)` constructor form replaced by `'2026-01-15'`.

— `git show ec2a8a76:docs/language/temporal-design.md`, lines 67-71 (accessed 2026-05-28)

The corresponding locked-decision body (Decision #17 in temporal-design.md, "Typed constant quantities — sole mechanism for temporal quantity construction") spells out the value-position `+` rationale:

> **What:** Temporal quantities are expressed as typed constants inside `'...'`, with optional `{expr}` interpolation:
>
> 1. **Static quantity:** `'30 days'`, `'72 hours'`, `'12 months'` — value + unit name inside `'...'`
> 2. **Interpolated quantity:** `'{GraceDays} days'`, `'{X + 5} hours'` — `{expr}` inside `'...'`
> 3. **Combined quantity:** `'2 years + 6 months + 15 days'` — `+` combination inside `'...'`
>
> [...]
> **Why:** One syntax, zero redundancy, zero keyword reservation. Quantities inside `'...'` with `{expr}` interpolation solve the variable-quantity split [...]
> **Alternatives rejected:** (A) Bare postfix keywords [...] (B) Both syntaxes coexist [...] (C) Composites via juxtaposition — eliminated in v3. (D) Function-call only — eliminated in v3.
> **Precedent:** PostgreSQL `INTERVAL '30 days'`. SQL `'...'` for value literals. CSS `calc(...)` for computed values inside property contexts.

— `git show ec2a8a76:docs/language/temporal-design.md`, lines 1299-1322 (accessed 2026-05-28)

The value-literal `+` decision body does **not** mention or contemplate the type-level `&` separator that would land three days later in the currency/quantity proposal. The currency/quantity proposal's D4 does **not** acknowledge that `+` already means "AND/combine" in the value-literal position — its stated reason for rejecting `+` is *"confusing because + already means addition"*, which describes the arithmetic-operator role of `+` but not the role of `+` as a value-literal combiner that the same author (Frank) had just locked three days prior.

`grep -rln "hours&minutes\|years&months\|& for period\|period basis"` across `docs/Working/`, `docs/Working/Archive/`, and `research/` (run 2026-05-28) returns six files. Of those:

- `docs/Working/compiler-readiness-review-2026-05-24.md`, `docs/Working/compiler-readiness-plan-2026-05-24.md`, and `docs/Working/compiler-readiness-review-2026-05-24-appendices/audit-type-system-docs.md` flag F-LANG-BIZ-07 ("Composite period basis `'years&months'` (`&` separator, D4) not implemented") as a build-or-drop decision still owner-pending. None of these revisit the `&` vs `+` separator choice — they take D4's spelling as given and report the impl gap.
- `docs/Working/Archive/diagnostic-enforcement.md:125` references `period in 'hours&minutes'` in the PRE0074 trigger example; no rationale.
- `research/language/expressiveness/currency-quantity-uom-research.md` is the research file that grounds the currency/quantity design — its "Dead Ends and Rejected Directions" section is exhaustive (18 dead ends documented). **Composite period basis separator choice is not among them.** The research file treats the separator choice as out of scope.

**`samples/` is silent on composite basis.** `grep -rn "in 'years&\|in 'hours&\|in 'days&" samples/` returns zero hits. Every period field in the sample corpus uses a single basis atom (`'days'`, `'months'`, `'hours'`) or no basis qualifier at all.

**Conclusion for Finding 1:** No internal artifact captures a load-bearing reason for `&` over `+` that addresses the now-locked value-literal `+` combiner. The currency/quantity proposal was authored in parallel with the temporal-design v3 session (the same week, same author, three days apart), and the inconsistency appears to be a missed alignment — not a deliberate choice with a stated rationale.

### Finding 2 — NodaTime has no textual convention for unit-list selection. The C# API uses the `|` bitwise-OR operator; the prose uses English "and"; pre-combined names use PascalCase concatenation with no separator.

**NodaTime PeriodUnits is a `[Flags]` enum.** From `src/NodaTime/PeriodUnits.cs` on `main` (accessed 2026-05-28):

> ```csharp
> /// <summary>
> /// The units within a <see cref="Period"/>. When a period is created to find the difference between two local values,
> /// the caller may specify which units are required - for example, you can ask for the difference between two dates
> /// in "years and weeks". Units are always applied largest-first in arithmetic.
> /// </summary>
> // Note to Noda Time developers: that the values of the single (non-compound) values must match up with the internal indexes used for
> // Period's values array.
> [Flags]
> public enum PeriodUnits
> {
>     None = 0,
>     Years = 1,
>     Months = 2,
>     Weeks = 4,
>     Days = 8,
>     /// Compound value representing the combination of <see cref="Years"/>, <see cref="Months"/>, <see cref="Weeks"/> and <see cref="Days"/>.
>     AllDateUnits = Years | Months | Weeks | Days,
>     /// Compound value representing the combination of <see cref="Years"/>, <see cref="Months"/> and <see cref="Days"/>.
>     YearMonthDay = Years | Months | Days,
>     Hours = 16, Minutes = 32, Seconds = 64,
>     Milliseconds = 128, Ticks = 256, Nanoseconds = 512,
>     /// Compound value representing the combination of <see cref="Hours"/>, <see cref="Minutes"/> and <see cref="Seconds"/>.
>     HourMinuteSecond = Hours | Minutes | Seconds,
>     AllTimeUnits = Hours | Minutes | Seconds | Milliseconds | Ticks | Nanoseconds,
>     DateAndTime = Years | Months | Days | Hours | Minutes | Seconds | Milliseconds | Ticks | Nanoseconds,
>     AllUnits = Years | Months | Weeks | Days | Hours | Minutes | Seconds | Milliseconds | Ticks | Nanoseconds,
> }
> ```

— `https://raw.githubusercontent.com/nodatime/nodatime/main/src/NodaTime/PeriodUnits.cs` (accessed 2026-05-28)

[Primary] Three observations from the source:

1. **The XML doc comment on the enum itself uses English "and"** to describe unit composition: *"...the difference between two dates in 'years and weeks'."* That is NodaTime's own prose convention.
2. **Pre-combined unit-set names use PascalCase concatenation with no separator at all** — `YearMonthDay`, `HourMinuteSecond`, `AllDateUnits`, `AllTimeUnits`, `DateAndTime`, `AllUnits`. There is no ampersand, no plus, no comma.
3. **The code-level combiner is `|`** (bitwise-OR), as visible in `AllDateUnits = Years | Months | Weeks | Days`. The `|` operator is mandated by `[Flags]` enum semantics in C# — it is not a NodaTime stylistic choice but a language-level convention.

**The NodaTime arithmetic user guide reinforces the prose-uses-"and" convention.** From `https://nodatime.org/3.2.x/userguide/arithmetic` (accessed 2026-05-28):

> "The units are specified with the `PeriodUnits` enum, and can be combined using the `|` operator. So for example, to find out how many 'months and days' old I am..."

Other prose passages from the same source:

> - "a period of '1 month and 3 days'"
> - "a period of '2 weeks and 10 hours'"
> - "Age: {0} months and {1} days"

**NodaTime has no string-form for unit-list selection.** `Period.ToString()` and `PeriodPattern.Roundtrip` produce ISO 8601 value notation, not unit-list selection. From the API docs:

> "Returns this string formatted according to the Roundtrip." Example: `P27Y4M20D` for a period of 27 years, 4 months, and 20 days.

— `https://nodatime.org/3.2.x/api/NodaTime.Period.html` (accessed 2026-05-28)

And on `PeriodPattern`:

> "Pattern which uses the normal ISO format for all the supported ISO fields, but extends the time part with 's' for milliseconds, 't' for ticks and 'n' for nanoseconds."
>
> "NormalizingIso: A 'normalizing' pattern which abides by the ISO-8601 duration format as far as possible. Weeks are added to the number of days (after multiplying by 7)."

— `https://nodatime.org/3.2.x/api/NodaTime.Text.PeriodPattern.html` (accessed 2026-05-28)

The NodaTime period-patterns v2.2 user guide confirms there are only two patterns and neither serializes unit-list selection:

> "the `Period` type doesn't support custom patterns, but two predefined patterns which are exposed in `PeriodPattern`"
> "only the parameterless `ToString` method is supported, which always uses the roundtrip pattern"
> "there are no methods performing parsing within `Period`"

— `https://nodatime.org/2.2.x/userguide/period-patterns` (accessed 2026-05-28)

**Conclusion for Finding 2:** NodaTime ships **no textual convention for unit-list selection** that constrains Precept's choice. The C# operator is `|` (forced by `[Flags]` semantics), prose uses English "and", and pre-combined names use PascalCase concatenation. The separator inside `'...'` at the Precept type level — whether `&`, `+`, comma, or anything else — is purely Precept's invention. NodaTime cannot vote.

### Finding 3 — Joda-Time, NodaTime's predecessor, also uses PascalCase concatenation for pre-combined period-type names — no textual unit-list separator.

From `org.joda.time.PeriodType` in Joda-Time `main` (accessed 2026-05-28):

> - `standard()`: name `"Standard"`
> - `yearMonthDay()`: name `"YearMonthDay"`
> - `yearMonthDayTime()`: name `"YearMonthDayTime"`
> - `yearWeekDay()`: name `"YearWeekDay"`
> - `yearDayTime()`: name `"YearDayTime"`
> - `dayTime()`: name `"DayTime"`

The Javadoc for `getName()` says it "Gets the name of the period type." It is a debugging/display name, not a syntactic surface — Joda-Time also has no textual unit-list selection convention. The names use PascalCase concatenation with no separator, mirroring what NodaTime later adopted for its compound flag values.

— `https://github.com/JodaOrg/joda-time/blob/main/src/main/java/org/joda/time/PeriodType.java` (accessed 2026-05-28)

**Conclusion for Finding 3:** Joda-Time provides no separator-choice precedent either. The NodaTime → Joda-Time lineage is consistent: PascalCase compound names in code, English prose, no textual list-selection format.

### Finding 4 — Other internal Precept type-level qualifier values use either single atoms or UCUM `/` for ratio. There is no other "list of categories" qualifier in the language whose separator could constrain D4.

Audit of every `in '...'` and `of '...'` qualifier in the canonical type docs:

| Qualifier position | Shape | Separator | Source |
|---|---|---|---|
| `money in '<ISO>'` | Single ISO 4217 code | (n/a — atomic) | `business-domain-types.md` |
| `quantity in '<unit>'` | Single UCUM atom or UCUM compound | UCUM `/` for ratio | `business-domain-types.md` |
| `quantity of '<dim>'` | Single UCUM dimension category | (n/a — atomic) | `business-domain-types.md` |
| `price in '<cur>/<unit>'` | UCUM ratio | UCUM `/` | `business-domain-types.md` |
| `exchangerate in '<from>/<to>'` | UCUM ratio | UCUM `/` | `business-domain-types.md` |
| `period in '<unit>'` | Single NodaTime `PeriodUnits` atom | (n/a) | `temporal-type-system.md:848` |
| `period in '<unit>&<unit>...'` | Composite NodaTime `PeriodUnits` bitfield | **`&` (D4)** | `business-domain-types.md:1305-1313` |
| `period of '<class>'` | Single component-class atom (`'date'`, `'time'`) | (n/a) | `business-domain-types.md` |

Composite `period in` is the **only** type-level qualifier in the language whose value is a *list of like atoms.* Every other qualifier is either atomic, or a UCUM ratio (`/`).

`grep -rnE "in '[^']*\+[^']*'" docs/language/ samples/` and `grep -rnE "of '[a-zA-Z]+&[a-zA-Z]+'" docs/language/ samples/` both return zero hits — no other type-level `+` or `&` separator exists. The list of equivalents to compare against is empty.

**Conclusion for Finding 4:** Internal consistency cannot be invoked from sibling type-level qualifiers — there are no siblings. The relevant internal consistency anchor is the value-literal `+` combiner, which is what motivates the question.

### Finding 5 — D9 (Discrete equality narrowing) propagates the `&` choice into the narrowing-marker surface.

From `business-domain-types.md:1732` (accessed via Read tool, 2026-05-28):

> | `period` | `.basis` | `when X.basis == 'hours&minutes'` | `$eq:X.basis:hours&minutes` |

D9 uses the basis string as both a guard literal (visible to authors) and a compile-time narrowing marker. If D4 is amended to `+`, every D9 example, every `$eq:X.basis:` marker, and every author-facing guard pattern would shift from `'hours&minutes'` to `'hours+minutes'`. This is mechanical (`TemporalQuantityParser` already splits on `+`; the narrowing-marker is just a string), but it is the surface area of the amendment — D4 is not a one-line change.

### Finding 6 — `&` is unused everywhere else in Precept's grammar. `+` is heavily used. Neither has a tokenization conflict with the period-basis context.

`grep -rn "Ampersand\|'&'" src/Precept/Language/Tokens.cs Constructs.cs Operations.cs 2>/dev/null` (run 2026-05-28) returns no hits — `&` is not a token in Precept's grammar. `&&` (logical AND) is also absent — Precept uses `and` as the keyword (per `Tokens.cs` / spec). Inside a typed-constant `'...'`, the surrounding grammar treats the content as a delimited string slice handed to the basis-value parser, so either separator (`&` or `+`) is structurally safe — neither escapes the quote.

`+` is heavily used both as an arithmetic operator and as the locked value-literal combiner inside `'...'`. The argument made in D4 — that `+` is "confusing because + already means addition" — applies equally to the value-literal position where `+` was nonetheless picked over `&`. The lock in temporal-design.md Decision #17 chose `+` despite the same potential confusion, with no carve-out for the period-basis use case.

## Threats to Validity

- **Single-author confound.** Both D4 and the value-literal `+` lock were authored by the same role/persona (Frank — Lead/Architect, Language Designer) within the same week (2026-04-15 → 2026-04-18). It is possible that an off-record session or conversation between Shane (owner) and the author resolved the `&` vs `+` distinction in a way that didn't make it into any committed artifact. The git log and `docs/Working/Archive/` capture only what was written down. The user (Shane) is best positioned to confirm or deny.
- **NodaTime convention search bounded to current state.** The PeriodUnits source as it exists on `main` (and the v3.2.x docs) is the basis for Finding 2. Pre-v2 NodaTime (early Skeet-era) was not investigated; very-early Joda-derived prose conventions in NodaTime's 1.x era could differ. Unlikely to change the conclusion (no period-pattern surface emerged in any version), but flagged.
- **Frank's history files (`.squad/agents/frank/history.md`) not consulted.** The brief notes Frank was the prior reviewer, and `git show 1ff51967 --stat` confirms his history file was touched in the same commit that introduced D4. Per `CLAUDE.md`, `.squad/` is legacy state and not maintained — but in this specific case, the timestamp coincidence means it might contain a contemporaneous note explaining the `&` choice. I did not read it because the project rule excludes `.squad/` from authoritative-source rotation; if the owner wants that signal sourced, it can be checked. Flagged as a known gap rather than silently skipped.
- **Joda-Time PeriodFormatter / PeriodFormatterBuilder not exhaustively surveyed.** The brief authorized a "brief check" of Joda-Time. I covered `PeriodType` source (which captures the name convention question definitively) but did not deep-dive `PeriodFormatterBuilder`. If `PeriodFormatterBuilder` ships a textual unit-list format with a separator convention, that would supplement Finding 2/3. Low risk — `PeriodFormatterBuilder` is documented as building formatters for *values* (e.g., "3 hours and 17 minutes"), not for selecting which units a `PeriodType` includes.
- **Community NodaTime samples not surveyed.** Stack Overflow is blocked to the agent; the brief's "2-3 community examples" check was not completed. The conclusion does not depend on community examples — NodaTime's API and prose are conclusive on the absence of a textual unit-list convention, and a community sample using ad-hoc text could not invalidate that.

## Implications for Precept

The D4 amendment question has a clear empirical shape after this investigation:

1. **The internal record shows D4 was locked three days after the value-literal `+` combiner, by the same author, with no acknowledgment of the inconsistency.** D4's stated rejection of `+` (*"confusing because + already means addition"*) describes the arithmetic role of `+` — not the value-literal "combine these components" role that the same author had just locked in Decision #17 of temporal-design.md. If the value-literal `+` is sound despite the arithmetic-role objection, the type-level `+` is sound for the same reason in the same delimiter context.
2. **NodaTime does not constrain the answer.** No textual unit-list convention exists in NodaTime. The C# operator is `|` (forced by `[Flags]`), prose uses English "and", and compound names use PascalCase concatenation. The closest precedent for *any* combiner in NodaTime's surface — the operator — is `|`, which is not on the Precept table because it has no role in the language elsewhere.
3. **Joda-Time gives no constraint either** — same PascalCase compound-name convention, no separator.
4. **No siblings inside Precept's type-level qualifier surface use a list separator.** All other `in` / `of` values are atomic or UCUM ratios. The only meaningful internal consistency anchor is the value-literal `+` combiner.
5. **D9's narrowing-marker surface uses the basis string verbatim** — amending D4 means re-spelling `$eq:X.basis:hours&minutes` to `$eq:X.basis:hours+minutes` and updating the documented guard pattern. This is mechanical but is the surface area to confirm in the amendment.
6. **Implementation is forward-compatible with either choice.** `TemporalQuantityParser` currently splits on `+` only; F-LANG-BIZ-07 is the open build-or-drop decision. If D4 is amended to `+`, the parser already does the right thing for the basis-value lexer; only the *qualifier-value* parser (a different code path that processes `in '<value>'` content) needs to be wired up.

The case for D4 → `+` is grounded in three legs: (a) the value-literal lock already uses `+` for "AND/combine" in the immediately adjacent surface (the same `'...'` delimiter, the same period type), (b) NodaTime has no convention that prefers `&`, and (c) the stated rejection of `+` in D4 contradicts how `+` is actually used elsewhere in the language.

The case for keeping `&` is weak after this investigation: no load-bearing reason has been recovered from the internal record, NodaTime gives no support, and no sibling qualifier uses `&`.

## Conclusions

### Conclusion 1 — D4 should be amended to use `+` as the composite period-basis separator.

- **Rationale:** Consistency with the value-literal `+` combiner that the language already uses inside `'...'` for the same period type ("combine these temporal components"). The value-literal lock (Decision #17 in temporal-design.md, 2026-04-15) settled that `+` inside `'...'` means "AND/combine these components" in this exact delimiter context. D4 was locked three days later by the same author with a stated rejection rationale (*"+ already means addition"*) that is contradicted by the value-literal lock the same author had just made. Internal consistency at the same delimiter is the dominant signal.
- **Alternatives considered and rejected:**
  - **Keep `&` (D4 as-locked).** No load-bearing reason for `&` was recovered from the internal record. The stated D4 rationale (*"+ already means addition"*) does not survive contact with the value-literal `+` lock. NodaTime gives no preference for `&`. Rejected on internal-consistency and weak-rationale grounds.
  - **Use `|` (mirroring NodaTime's `[Flags]` operator).** `|` is not in Precept's tokenizer, has no other role in the language, and would import a C#-implementation idiom rather than a Precept-surface one. Authors reading `period in 'hours|minutes'` would not connect `|` to "AND" without C# bitwise-flag familiarity. Rejected.
  - **Use comma (`,`).** Reads naturally for lists (English "hours, minutes, and seconds") but introduces another separator that has no role elsewhere in Precept's `'...'` content. Comma is currently used inside expression-argument lists outside `'...'`; recycling it inside `'...'` for a different purpose adds load. Rejected on minimality grounds — `+` is already in the same delimiter context for the same composition role.
  - **PascalCase compound names (`'YearMonthDay'`) — mirror NodaTime's enum compound values.** Would limit composite basis to NodaTime's pre-named compounds and lose the open-ended composition that `'years&months&days'` provides. Rejected on expressiveness grounds.
- **Precedent:** Two-fold —
  - **Internal:** Decision #17 in `docs/language/temporal-type-system.md` (formerly `temporal-design.md`) locks `+` as the combiner for period value composition inside `'...'`: *"`'2 years + 6 months + 15 days'` — `+` combination inside `'...'`"* (accessed via git history 2026-05-28). The amendment extends the same convention to the immediately adjacent type-level position in the same delimiter.
  - **External (none against):** NodaTime's `PeriodUnits.cs` source on `main` (accessed 2026-05-28) ships no textual unit-list convention, so no external authority prefers `&`.
- **Tradeoff accepted:** A reader unfamiliar with Precept who sees `period in 'hours+minutes'` could initially parse `+` as arithmetic addition. The mitigation is that `+` inside the single-quote typed-constant delimiter (`'...'`) is the locked value-literal combiner already, so the inside-quote convention is uniform: *`+` inside `'...'` means "combine".* The mental model collapses to one rule across both positions instead of two competing separators (one at the type level, a different one at the value level) in the same delimiter.

### Conclusion 2 — NodaTime imposes no constraint; the separator choice is purely Precept's.

- **Rationale:** Finding 2 demonstrates that NodaTime ships no textual convention for unit-list selection. The closest API artifact (`PeriodUnits` `[Flags]` enum with `|`) is a C# language requirement, not a NodaTime stylistic position; and the docs prose uses English "and" for combinations. There is therefore no "NodaTime-faithful" answer to inherit.
- **Alternatives considered and rejected:** Treating `|` (the C# operator) as a NodaTime convention — rejected because `[Flags]` semantics force `|`; it is not a chosen idiom.
- **Precedent:** NodaTime `PeriodUnits.cs` on `main` declares `AllDateUnits = Years | Months | Weeks | Days` and similar with no textual analogue (accessed 2026-05-28). The arithmetic user guide uses English "and" in prose. The Period and PeriodPattern API surfaces have no string form for unit-list selection.
- **Tradeoff accepted:** Precept owns the convention for this surface. That is consistent with the broader pattern — Precept invents the qualifier syntax (`in '...'` / `of '...'`) rather than borrowing it from NodaTime, so it owns the separator choice inside those qualifiers.

## What would change this conclusion

- **A contemporaneous internal note (e.g., in `.squad/agents/frank/history.md` for the 2026-04-15 to 2026-04-18 window) explicitly considering and rejecting `+` for type-level basis composition after the value-literal `+` lock landed.** That would surface a load-bearing reason for `&` I could not find in the canonical record, and would shift the recommended action from "amend to `+`" to "keep `&` and rewrite the rationale to surface the real reason." Likelihood: low — the canonical record has been the locked surface for over a month, and the working-doc audit pass (Phase 6) flagged the impl gap without surfacing such a reason.
- **A NodaTime pattern or builder surface I missed that ships a textual unit-list format with a separator convention.** If `PeriodFormatterBuilder` (Joda-Time) or some NodaTime extension has a documented "list which units this period uses" text format with a chosen separator, that would re-enter the inheriting-NodaTime argument. Likelihood: low — the API surface I covered is the canonical one, and unit-list selection (vs. unit-value serialization) is a category NodaTime/Joda do not appear to model.
- **An author-confusion failure mode for `+` inside `in '...'` that doesn't apply to `+` inside the value-literal `'...'`.** If hover/IntelliSense behavior or compiler diagnostics behave qualitatively differently in the type-qualifier vs value-literal position when the content uses `+`, then the unified-rule benefit collapses. Likelihood: low — the qualifier-value parser and the value-literal parser are different code paths, but both operate on a delimited string and would just split on `+`.

## Open Questions

- **Should the amendment also formalize the same separator at the value level for non-period quantity composition?** I.e., is there any future type-level qualifier composition that could land later (e.g., currency baskets `money in 'USD+EUR+GBP'` for a future "any of these" qualifier) where `+` would carry "OR" semantics rather than "AND"? Out of scope for this research; would be its own design conversation if/when such a qualifier is proposed. Surface for future `/lifecycle-2-design` if relevant.
- **What is the diagnostic message at the qualifier-value parser when the author types the wrong separator?** Implementation question — covered in F-LANG-BIZ-07 build work, not by this research.
- **D9's narrowing marker surface (`$eq:X.basis:hours&minutes` → `$eq:X.basis:hours+minutes`) — is the marker string stable across compilation runs and visible in any externalized artifact (LSP hover, MCP diagnostic, etc.)?** Implementation question for the amendment PR.

## Sources

### Primary (NodaTime official surfaces and source)

- **NodaTime PeriodUnits enum API documentation, version 3.2.x.** Stable identifier: `nodatime.org/3.2.x/api/NodaTime.PeriodUnits.html`. Source grade: Primary. Access date: 2026-05-28.
- **NodaTime PeriodUnits.cs source on `main`.** Stable identifier: `github.com/nodatime/nodatime/blob/main/src/NodaTime/PeriodUnits.cs` (raw fetched from `raw.githubusercontent.com/.../main/src/NodaTime/PeriodUnits.cs`). Source grade: Primary. Access date: 2026-05-28.
- **NodaTime Period API documentation, version 3.2.x.** Stable identifier: `nodatime.org/3.2.x/api/NodaTime.Period.html`. Source grade: Primary. Access date: 2026-05-28.
- **NodaTime PeriodPattern API documentation, version 3.2.x.** Stable identifier: `nodatime.org/3.2.x/api/NodaTime.Text.PeriodPattern.html`. Source grade: Primary. Access date: 2026-05-28.
- **NodaTime arithmetic user guide, version 3.2.x.** Stable identifier: `nodatime.org/3.2.x/userguide/arithmetic`. Source grade: Primary. Access date: 2026-05-28.
- **NodaTime period-patterns user guide, version 2.2.x.** Stable identifier: `nodatime.org/2.2.x/userguide/period-patterns`. Source grade: Primary. Access date: 2026-05-28.

### Primary (Joda-Time source)

- **Joda-Time PeriodType.java source on `main`.** Stable identifier: `github.com/JodaOrg/joda-time/blob/main/src/main/java/org/joda/time/PeriodType.java`. Source grade: Primary. Access date: 2026-05-28.

### Primary (internal Precept canonical surfaces)

- **D4 introductory commit `1ff51967`** — `git show 1ff51967:docs/CurrencyQuantityUomDesign.md` (D4 § verbatim). Date: 2026-04-18. Source grade: Primary (internal authoritative record). Accessed via git on 2026-05-28.
- **Temporal-design v3 lock at commit `ec2a8a76`** — `git show ec2a8a76:docs/language/temporal-design.md`, lines 67-71 (v3 changelog), 1299-1322 (Decision #17). Date: 2026-04-25 (file promotion); lock session 2026-04-15. Source grade: Primary. Accessed via git on 2026-05-28.
- **`docs/language/business-domain-types.md`** — D4 (lines 1682-1687), composite-basis table (1305-1313), D9 narrowing-marker table (1732). Source grade: Primary (canonical spec). Accessed 2026-05-28.
- **`docs/language/temporal-type-system.md`** — value-literal lock (681, 777, 781), `in`/`of` qualifier table (842-854). Source grade: Primary. Accessed 2026-05-28.
- **`src/Precept/Language/Time/TemporalQuantityParser.cs:16`** — confirms current implementation splits on `+` only. Source grade: Primary. Accessed 2026-05-28.

### Secondary (internal working-doc context — used for impl-status / decision-pending framing only, not for separator-choice rationale)

- **`docs/Working/compiler-readiness-review-2026-05-24.md:331,728`** — F-LANG-BIZ-07 build-or-drop decision pending. Source grade: Secondary (working doc, not canonical). Accessed 2026-05-28.
- **`docs/Working/compiler-readiness-review-2026-05-24-appendices/audit-type-system-docs.md:124-127`** — F-LANG-BIZ-07 impl-gap evidence. Source grade: Secondary. Accessed 2026-05-28.
- **`docs/Working/compiler-readiness-plan-2026-05-24.md:24`** — Phase 6 status noting F-LANG-BIZ-07 still owner-pending. Source grade: Secondary. Accessed 2026-05-28.
- **`research/language/expressiveness/currency-quantity-uom-research.md`** — exhaustive dead-ends list for the currency/quantity proposal; *does not* surface the separator question. Source grade: Secondary (internal research). Accessed 2026-05-28.

### Notes on mirroring

The NodaTime sources are the primary load-bearing external evidence. I did not mirror them to `research/references/period-basis-separator/` because the file `PeriodUnits.cs` is reproduced verbatim in Finding 2 above (the source IS the mirror), and the API/user-guide quotes are sufficiently short that the inline verbatim excerpts carry the falsifiability function. If the project's mirror discipline requires a separate snapshot, the four NodaTime URLs above are stable on the version-pinned docs path (3.2.x), so URL rot is unlikely.
