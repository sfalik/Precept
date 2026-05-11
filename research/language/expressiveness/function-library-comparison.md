# Function Library Comparison — Excel, SQL, .NET

External research grounding for [#16 — Built-in function library](https://github.com/sfalik/Precept/issues/16). This document maps relevant functions from Excel, SQL, and .NET `System.Math` against Precept's proposed 23-signature library to identify gaps, validate coverage, and recommend additions or exclusions.

**Evaluation filter:** Precept is a constraint language governing business entity lifecycle integrity. Every function must be pure, total, deterministic, and free of side effects. General-purpose computation, formatting, and data transformation functions are out of scope.

> **Methodology correction (2025-04-11):** An earlier version of this document used "no sample demonstrates the need" as evidence to exclude functions. This is circular reasoning — the sample `.precept` files cannot use functions that don't exist yet, and the sample corpus is not complete. This revision eliminates sample-corpus-based reasoning from all evaluations.
>
> **Corrected evaluation criteria:**
> - **Domain relevance:** Would real business entities (insurance, loans, hiring, hospitality, logistics, maintenance, billing, subscriptions, healthcare, government, legal) use this function in lifecycle constraints?
> - **Design principle fit:** Is the function pure, total, deterministic?
> - **Consensus across systems:** How many surveyed systems (Excel, SQL, .NET) include it?
> - **Prevention gap:** Does the absence of this function prevent authors from expressing a constraint the domain needs?
>
> Every function previously dismissed with sample-based reasoning has been re-evaluated against these criteria.

---

## 1. Excel Comparison Table

### Math / Numeric

| Excel Function | Signature | Precept Equivalent | Gap? | Domain Relevance | Verdict |
|---|---|---|---|---|---|
| `ABS(n)` | number → number | `abs(n)` (proposed) | No | Symmetric range checks. `maintenance-work-order`: `abs(ActualHours - EstimatedHours) <= 4` replaces one-sided guard. | ✅ Covered |
| `FLOOR(n, sig)` | number × number → number | `floor(n)` (proposed, 0-arg floor-to-integer) | No | Rounding allocation quantities down. `event-registration`: integer seat counts from computed decimals. | ✅ Covered |
| `CEILING(n, sig)` | number × number → number | `ceil(n)` (proposed, 0-arg ceil-to-integer) | No | Rounding up for minimum allocations. `apartment-rental`: ceil of income ratios. | ✅ Covered |
| `ROUND(n, d)` | number × number → number | `round(n, N)` (implemented) | No | Already in production: `travel-reimbursement`, `event-registration` use `round(expr, 2)`. | ✅ Covered |
| `ROUNDUP(n, d)` | number × number → number | None | Partial | Always rounds away from zero. For positive numbers, equivalent to `ceil`. For Precept's domain (non-negative financial amounts), `ceil` suffices. Away-from-zero rounding on negative values has no domain-relevant lifecycle use. | ❌ Exclude |
| `ROUNDDOWN(n, d)` | number × number → number | `truncate(n)` (proposed) | Partial | For positive numbers, same as `truncate`/`floor`. Multi-decimal-place round-down is covered by `round` with appropriate precision; a dedicated function adds no constraint capability beyond what `round` and `truncate` provide. | ❌ Exclude |
| `TRUNC(n, d)` | number × number → number | `truncate(n)` (proposed) | No | Dropping fractional parts. `fee-schedule`: truncating intermediate fee calculations. | ✅ Covered |
| `MIN(a, b, ...)` | variadic → number | `min(a, b, ...)` (proposed) | No | Capping values: `insurance-claim` approved ≤ claimed. `hiring-pipeline`: min of score thresholds. | ✅ Covered |
| `MAX(a, b, ...)` | variadic → number | `max(a, b, ...)` (proposed) | No | Floor values: `loan-application` minimum credit score enforcement via computed values. | ✅ Covered |
| `MOD(n, d)` | number × number → number | `%` operator | No | Modulo already available as infix. Same semantics. | ✅ Covered (operator) |
| `SIGN(n)` | number → number | None | Minimal | Returns -1/0/1. Direction-of-change constraints ("adjustment must be positive", "balance must not change sign") are expressible with comparison operators: `x > 0`, `x < 0`, `x == 0`. The composed form `(a > 0 and b > 0) or (a < 0 and b < 0)` covers "same sign" checks. The integer result provides no additional constraint power beyond what comparisons already offer. Present in 3/3 surveyed systems, but for general math — not constraint-specific use. | ❌ Exclude |
| `INT(n)` | number → number | `floor(n)` | No | `INT` rounds toward negative infinity, same as `floor`. Covered. | ✅ Covered |
| `POWER(b, e)` | number × number → number | `pow(base, exponent)` (proposed) | Gap — Include | Exponentiation. Domain uses: compound interest ceilings in loan and insurance constraints (`principal * pow(1 + rate, periods)`), penalty escalation that doubles each overdue period (billing, government), declining-balance depreciation thresholds (maintenance, asset management). Present in 3/3 surveyed systems. With exponent restricted to non-negative integers, the function is total. Without `pow`, these constraints are inexpressible — manual expansion (`rate * rate * rate * ...`) is neither general nor practical. | ✅ Include now |
| `SQRT(n)` | number → number | None | No gap | Square root. Partial for negative inputs — violates totality. No business-entity lifecycle constraint in any domain (insurance, loans, healthcare, etc.) requires computing square roots; statistical calculations are host-application concerns. | ❌ Exclude |
| `LOG(n, b)` | number × number → number | None | No gap | Logarithms. Partial for non-positive inputs — violates totality. No domain-relevant lifecycle constraint requires logarithmic computation. | ❌ Exclude |
| `LN(n)` | number → number | None | No gap | Natural log. Same reasoning as `LOG` — partial function, no domain need. | ❌ Exclude |

### String / Text

| Excel Function | Signature | Precept Equivalent | Gap? | Domain Relevance | Verdict |
|---|---|---|---|---|---|
| `LOWER(s)` | string → string | `toLower(s)` (proposed) | No | Case-insensitive guards. `customer-profile`: comparing contact methods regardless of case. | ✅ Covered |
| `UPPER(s)` | string → string | `toUpper(s)` (proposed) | No | Normalizing comparisons. `payment-method`: normalizing currency codes. | ✅ Covered |
| `TRIM(s)` | string → string | `trim(s)` (proposed) | No | Input sanitization in constraints. `hiring-pipeline`: ensuring trimmed names aren't empty. | ✅ Covered |
| `LEFT(s, n)` | string × number → string | None | Minimal | Extract first N characters. Prefix matching in business constraints (account number prefixes, policy type codes) is a real domain need, but `startsWith(s, prefix)` already covers the boolean constraint case. `left` returns a string value, which is a transformation operation — Precept governs what data must satisfy, and prefix-presence is testable without extraction. | ❌ Exclude |
| `RIGHT(s, n)` | string × number → string | None | Minimal | Extract last N characters. Suffix matching (file extensions, trailing codes) is a real domain pattern, but `endsWith(s, suffix)` covers the boolean constraint case. Same design-principle reasoning as `LEFT` — extraction is transformation, not constraint validation. | ❌ Exclude |
| `MID(s, start, len)` | string × number × number → string | None | Gap | Positional substring extraction. Domain need: embedded format codes within identifiers — insurance policy numbers with embedded state codes at positions 3-4, account numbers with embedded branch codes, serial numbers with embedded manufacturer codes. `startsWith`/`endsWith` only cover prefix/suffix; mid-string positional validation requires extraction. Without `substring`, authors cannot express "characters 3-5 of PolicyNumber must be a valid state code." Prevention gap exists for mid-string format validation. | ⏳ Consider v2 |
| `LEN(s)` | string → number | `.length` accessor (issue #10) | No | String length validation. `insurance-claim`: `DecisionNote.length <= 500`. `building-access-badge`: `AccessReason.length <= 200`. Already in use in samples. | ✅ Covered (accessor) |
| `FIND(find, within)` | string × string → number | None | Minimal | Returns position index. The constraint use case is "does it contain?" not "where?" — `contains` operator (issue #15) covers the boolean case. The integer position has no constraint utility in business-entity lifecycle rules. | ❌ Exclude |
| `SEARCH(find, within)` | string × string → number | None | Minimal | Case-insensitive FIND. Same analysis — the integer position is not useful for constraint logic. The boolean presence test is the domain need, covered by `contains`. | ❌ Exclude |
| `SUBSTITUTE(s, old, new)` | string × string × string → string | None | Minimal | String mutation. Domain uses exist — normalizing formats (stripping hyphens from SSNs, normalizing phone numbers) for comparison. However, Precept's design principle is to govern what data *must look like*, not to transform it. Normalization is a host-application responsibility: the host normalizes, Precept validates the normalized result. This is a design-principle exclusion — substitution is transformation, not constraint validation. | ❌ Exclude |
| `REPLACE(s, start, n, new)` | string × number × number × string → string | None | Minimal | Positional string mutation. Same design-principle reasoning as SUBSTITUTE — transformation belongs in the host application. Precept validates; it does not rewrite. | ❌ Exclude |
| `TEXT(n, fmt)` | value × string → string | None | No gap | Number-to-string formatting. Display formatting is a presentation concern, not a constraint concern. No domain-relevant lifecycle constraint requires format-rendering a number into a string. | ❌ Exclude |
| `CONCAT(a, b, ...)` | variadic → string | None | Small gap | String concatenation. Computed string values in `set` actions are a real domain pattern: display names from components, formatted references, combined identifiers. However, this is better addressed as a `+` operator overload on strings (separate proposal) rather than a function. | ⏳ Consider v2 |
| `EXACT(a, b)` | string × string → boolean | `==` operator | No | Case-sensitive comparison. Precept `==` on strings is already case-sensitive. | ✅ Covered (operator) |
| `REPT(s, n)` | string × number → string | None | No gap | String repetition. No domain-relevant lifecycle constraint requires repeating a string. | ❌ Exclude |

### Logical

| Excel Function | Signature | Precept Equivalent | Gap? | Domain Relevance | Verdict |
|---|---|---|---|---|---|
| `IF(cond, t, f)` | bool × T × T → T | Conditional expression (issue #9) | No | Covered by the conditional expression proposal, not the function library. | ✅ Covered (separate proposal) |
| `AND(a, b, ...)` | variadic bool → bool | `and` operator (issue #31) | No | Logical conjunction. Already an operator. | ✅ Covered (operator) |
| `OR(a, b, ...)` | variadic bool → bool | `or` operator (issue #31) | No | Logical disjunction. Already an operator. | ✅ Covered (operator) |
| `NOT(b)` | bool → bool | `not` operator (issue #31) | No | Logical negation. Already an operator. | ✅ Covered (operator) |
| `IFS(c1,v1,c2,v2,...)` | paired → T | Conditional expression chaining | No | Multi-branch conditional covered by chained conditionals when #9 ships. | ✅ Covered (separate proposal) |
| `SWITCH(expr, v1,r1,...)` | matched → T | Conditional expression chaining | No | Same as IFS for constraint purposes. | ✅ Covered (separate proposal) |

### Type Checking

| Excel Function | Signature | Precept Equivalent | Gap? | Domain Relevance | Verdict |
|---|---|---|---|---|---|
| `ISNUMBER(v)` | any → bool | Static typing | No | Precept fields are statically typed. A `number` field is always a number. Type checking is structurally unnecessary. | ❌ Exclude |
| `ISTEXT(v)` | any → bool | Static typing | No | Same reasoning — static typing eliminates the need. | ❌ Exclude |
| `ISBLANK(v)` | any → bool | `== null` | No | Nullable fields use `== null` / `!= null`. | ✅ Covered (syntax) |
| `ISERROR(v)` | any → bool | Totality guarantee | No | All Precept functions are total — they cannot produce errors. Error checking is structurally unnecessary. | ❌ Exclude |

---

## 2. SQL Comparison Table

### Math

| SQL Function | Signature | Precept Equivalent | Gap? | Domain Relevance | Verdict |
|---|---|---|---|---|---|
| `ABS(n)` | number → number | `abs(n)` (proposed) | No | Same as Excel analysis. | ✅ Covered |
| `CEILING(n)` | number → number | `ceil(n)` (proposed) | No | Same as Excel analysis. | ✅ Covered |
| `FLOOR(n)` | number → number | `floor(n)` (proposed) | No | Same as Excel analysis. | ✅ Covered |
| `ROUND(n, d)` | number × int → number | `round(n, N)` (implemented) | No | Same as Excel analysis. | ✅ Covered |
| `TRUNCATE(n, d)` | number × int → number | `truncate(n)` (proposed) | No | SQL TRUNCATE takes optional decimal places; Precept truncate is to-integer. For multi-place truncation, `round` with mode could cover it, but the proposal's to-integer `truncate` matches the most common domain need (drop all decimals). | ✅ Covered |
| `MOD(n, d)` | number × number → number | `%` operator | No | Covered by operator. | ✅ Covered (operator) |
| `POWER(b, e)` | number × number → number | `pow(base, exponent)` (proposed) | Gap — Include | Same as Excel analysis. Domain patterns across insurance, loans, and billing. With integer exponent restriction, total. | ✅ Include now |
| `SIGN(n)` | number → int | None | Minimal | Same as Excel analysis. Comparison operators cover the constraint need; the integer result adds no constraint power. | ❌ Exclude |
| `SQRT(n)` | number → number | None | No gap | Same as Excel analysis. Partial function; no domain need in business lifecycle constraints. | ❌ Exclude |
| `GREATEST(a, b, ...)` | variadic → number | `max(a, b, ...)` (proposed) | No | SQL's `GREATEST` is Precept's variadic `max`. Direct mapping. | ✅ Covered |
| `LEAST(a, b, ...)` | variadic → number | `min(a, b, ...)` (proposed) | No | SQL's `LEAST` is Precept's variadic `min`. Direct mapping. | ✅ Covered |

### String

| SQL Function | Signature | Precept Equivalent | Gap? | Domain Relevance | Verdict |
|---|---|---|---|---|---|
| `LOWER(s)` | string → string | `toLower(s)` (proposed) | No | Covered. | ✅ Covered |
| `UPPER(s)` | string → string | `toUpper(s)` (proposed) | No | Covered. | ✅ Covered |
| `TRIM(s)` | string → string | `trim(s)` (proposed) | No | Covered. | ✅ Covered |
| `LTRIM(s)` | string → string | None | Minimal | Left-only trim. Domain need for asymmetric whitespace handling is negligible across business domains — full `trim` covers the common case. No business-entity lifecycle constraint requires selectively preserving trailing whitespace while stripping leading whitespace. | ❌ Exclude |
| `RTRIM(s)` | string → string | None | Minimal | Right-only trim. Same design-principle reasoning as LTRIM — full `trim` covers the domain need. | ❌ Exclude |
| `SUBSTRING(s, start, len)` | string × int × int → string | None | Gap | Positional format validation is a real domain need: embedded codes within structured identifiers (insurance policy numbers, account numbers, serial codes). `startsWith`/`endsWith` cover prefix/suffix but not mid-string extraction. Same analysis as Excel `MID`. | ⏳ Consider v2 |
| `LEFT(s, n)` | string × int → string | None | Minimal | Same as Excel analysis. `startsWith` covers the boolean constraint case; extraction is transformation. | ❌ Exclude |
| `RIGHT(s, n)` | string × int → string | None | Minimal | Same as Excel analysis. `endsWith` covers the boolean constraint case; extraction is transformation. | ❌ Exclude |
| `LENGTH(s)` / `LEN(s)` | string → int | `.length` accessor (issue #10) | No | Covered. | ✅ Covered (accessor) |
| `REPLACE(s, old, new)` | string × string × string → string | None | Minimal | String mutation. Same design-principle reasoning as Excel SUBSTITUTE — transformation is a host-application responsibility. Precept validates; it does not rewrite. | ❌ Exclude |
| `CONCAT(a, b, ...)` | variadic → string | None | Small gap | Same as Excel analysis. | ⏳ Consider v2 |
| `REVERSE(s)` | string → string | None | No gap | No domain-relevant lifecycle constraint requires reversing strings. | ❌ Exclude |
| `LPAD(s, len, pad)` | string × int × string → string | None | No gap | Padding is a display/formatting concern — not a lifecycle constraint operation. | ❌ Exclude |
| `RPAD(s, len, pad)` | string × int × string → string | None | No gap | Same as LPAD. | ❌ Exclude |
| `POSITION(sub IN s)` / `CHARINDEX(sub, s)` | string × string → int | None | Minimal | Position finding. The integer position is not useful for lifecycle constraints. `contains` covers the boolean presence test, which is the domain need. | ❌ Exclude |

### Type / Null

| SQL Function | Signature | Precept Equivalent | Gap? | Domain Relevance | Verdict |
|---|---|---|---|---|---|
| `COALESCE(a, b, ...)` | variadic nullable → T | Conditional expression (issue #9) | No | Null-coalescing is a specific case of conditional: `x == null ? default : x`. The expression audit flags this as L7 (significant gap), but it is addressed by the conditional expression proposal, not the function library. A dedicated `??` operator could be considered separately. | ✅ Covered (separate proposal) |
| `NULLIF(a, b)` | T × T → T? | None | No gap | Returns null if a == b. Extremely niche even in SQL; primary use case (avoiding division by zero) is handled by Precept's totality guarantees. No domain-relevant lifecycle constraint requires conditionally nulling a value based on equality. | ❌ Exclude |
| `CAST(expr AS type)` / `CONVERT` | any → T | None | Design question | Type conversion between `integer`, `decimal`, and `number`. This is a type system design decision (how do types coerce in mixed expressions), not a function library question. The type checker already handles `integer` + `decimal` promotion. | ❌ Exclude (type system concern) |

### Conditional

| SQL Function | Signature | Precept Equivalent | Gap? | Domain Relevance | Verdict |
|---|---|---|---|---|---|
| `CASE WHEN c THEN v ... END` | conditional → T | Conditional expression (issue #9) | No | Core conditional expression. Covered by separate proposal. | ✅ Covered (separate proposal) |
| `IIF(cond, t, f)` | bool × T × T → T | Conditional expression (issue #9) | No | Same. | ✅ Covered (separate proposal) |
| `GREATEST(a, b, ...)` | variadic → T | `max(a, b, ...)` (proposed) | No | Already covered above. | ✅ Covered |
| `LEAST(a, b, ...)` | variadic → T | `min(a, b, ...)` (proposed) | No | Already covered above. | ✅ Covered |

---

## 3. .NET `System.Math` Comparison

| .NET Method | Signature | Precept Equivalent | Gap? | Domain Relevance | Verdict |
|---|---|---|---|---|---|
| `Math.Abs(n)` | number → number | `abs(n)` (proposed) | No | Covered. | ✅ Covered |
| `Math.Floor(n)` | double → double | `floor(n)` (proposed) | No | Covered. | ✅ Covered |
| `Math.Ceiling(n)` | double → double | `ceil(n)` (proposed) | No | Covered. | ✅ Covered |
| `Math.Round(n, d)` | double × int → double | `round(n, N)` (implemented) | No | Covered. Uses MidpointRounding.ToEven (banker's rounding), matching Precept's semantics. | ✅ Covered |
| `Math.Truncate(n)` | double → double | `truncate(n)` (proposed) | No | Covered. | ✅ Covered |
| `Math.Min(a, b)` | T × T → T | `min(a, b, ...)` (proposed, variadic) | No | Precept's variadic version is a superset. | ✅ Covered |
| `Math.Max(a, b)` | T × T → T | `max(a, b, ...)` (proposed, variadic) | No | Same. | ✅ Covered |
| `Math.Sign(n)` | number → int | None | Minimal | Returns -1/0/1. Comparison operators (`> 0`, `< 0`, `== 0`) cover the constraint use case. The integer result adds no constraint power — direction checks are inherently boolean predicates in lifecycle rules. | ❌ Exclude |
| `Math.Pow(b, e)` | double × double → double | `pow(base, exponent)` (proposed) | Gap — Include | Exponentiation. Domain patterns: compound interest ceilings (loans, insurance), penalty escalation (billing, government), declining-balance depreciation (maintenance). (1) With exponent restricted to non-negative integers, the function is total. (2) Compound interest constraints are real — a loan precept may need to enforce `TotalObligation <= Principal * pow(1 + Rate, MaxPeriods)` as an invariant. (3) Without `pow`, these constraints are inexpressible. | ✅ Include now |
| `Math.Sqrt(n)` | double → double | None | No gap | Partial for negative inputs. No domain-relevant lifecycle constraint requires square roots — statistical/scientific computation is a host-application concern. | ❌ Exclude |
| `Math.Log(n)` | double → double | None | No gap | Partial for non-positive inputs. No domain-relevant lifecycle constraint requires logarithms. | ❌ Exclude |
| `Math.Log10(n)` | double → double | None | No gap | Same reasoning as `Math.Log`. | ❌ Exclude |
| `Math.Log2(n)` | double → double | None | No gap | Same reasoning as `Math.Log`. | ❌ Exclude |
| `Math.Clamp(v, min, max)` | T × T × T → T | `clamp(value, low, high)` (proposed) | Gap — Include | Clamp a value within a range. Domain uses are universal: capping approved insurance amounts, bounding discount percentages (0–100), enforcing dosage limits in healthcare, bounding benefit amounts in government programs, capping penalty amounts in billing. While composable as `max(low, min(high, v))`, the DSL readability improvement is significant — `clamp(Score, 0, 100)` is immediately clear to non-programmer domain experts; `min(max(Score, 0), 100)` requires mental unpacking. For a constraint language designed for business authors, readability of range-bounding — one of the most common constraint patterns — justifies a dedicated function. | ✅ Include now |
| `Math.BitOperations.*` | various | None | No gap | Bitwise operations. No domain-relevant lifecycle constraint requires bitwise operations. | ❌ Exclude |
| `Math.IEEERemainder` | double × double → double | `%` operator | No | Covered by modulo operator (different semantics, but `%` is the appropriate constraint-language version). | ✅ Covered (operator) |
| `MathF.*` (float variants) | various | N/A | N/A | `MathF` mirrors `Math` for `float`. Precept uses `decimal`/`number`/`integer` — no need for float-specific variants. | N/A |

---

## 4. Recommendations

### Include Now — Add to Proposal

The corrected methodology reveals two functions that should be added to the current proposal, plus one that requires a cross-cutting design decision:

| Function | Signature | Domain Evidence | Totality | Consensus |
|---|---|---|---|---|
| `pow(base, exponent)` | number × integer → number | Compound interest ceilings (loans, insurance), penalty escalation that doubles per period (billing, government, legal), declining-balance depreciation thresholds (maintenance, asset management). Without `pow`, these constraints are inexpressible — manual expansion is neither general nor practical. | **Total if exponent is restricted to non-negative integers.** `pow(x, 0)` = 1 for all x (including x=0, by convention). No undefined inputs. Fractional exponents of negative bases are avoided by type restriction. | 3/3 systems (Excel POWER, SQL POWER, .NET Math.Pow) |
| `clamp(value, low, high)` | number × number × number → number | Range-bounding is one of the most common constraint patterns across all business domains: capping approved insurance amounts, bounding discount percentages (0–100), enforcing dosage limits (healthcare), bounding benefit amounts (government), capping penalties (billing), constraining scores and ratings. While composable as `max(low, min(high, v))`, the readability improvement is significant for a DSL targeting business authors. `clamp(Score, 0, 100)` is immediately clear; `min(max(Score, 0), 100)` requires mental unpacking. | **Total.** Defined for all numeric inputs. When low > high, returns high (same as .NET `Math.Clamp`). | 1/3 systems (.NET Math.Clamp), but the conceptual pattern is universal — Excel/SQL users compose it from MIN/MAX |

**String contains (design decision needed):** The `contains` operator (issue #15) currently covers collection membership (`MySet contains "value"`). There is a real domain gap for **string containment** — testing whether a string contains a substring. Domain uses: email format validation (`Email contains "@"`), forbidden character detection, substring presence in free-text fields, domain checks in URLs/emails. This is pure, total, deterministic, and present in virtually every programming system. **Recommendation:** Extend `contains` to support string operands (issue #15 scope) or add a dedicated `strContains(haystack, needle)` function. This is a design decision, not a function-library question, but it represents a real prevention gap that this analysis surfaced.

### Consider for v2

| Function | Rationale | Domain Evidence | Blocker |
|---|---|---|---|
| `substring(s, start, length)` | Positional format validation: extracting embedded codes from structured identifiers. Insurance policy numbers with embedded state codes, account numbers with embedded branch identifiers, serial numbers with embedded manufacturer codes. `startsWith`/`endsWith` cover prefix/suffix but not mid-string positions. | Moderate — positional format validation is real but less common than prefix/suffix validation. Most format validation can be expressed with `startsWith`, `endsWith`, `.length`, and collection constraints. | Design question: Is positional substring extraction a constraint operation or a transformation? It returns a value for comparison, which straddles the line. |
| String concatenation (as `+` operator overload, not a function) | Building computed string values in `set` actions: display names from components, formatted references, combined identifiers. | Moderate — computed string values are a real pattern in lifecycle management (composing display fields, building reference strings). | Not a function library question — this is an operator overload on string type. Separate proposal. |

### Exclude

| Category | Functions | Reason |
|---|---|---|
| **Partial functions** | `sqrt`, `log`, `ln`, `log10`, `log2` | Undefined for negative/zero inputs. Violates totality. No domain-relevant lifecycle constraint requires these mathematical operations — statistical and scientific computation belongs in the host application. |
| **Sign/direction** | `sign` | Comparison operators (`> 0`, `< 0`, `== 0`) express direction-of-change constraints directly. The composed form `(a > 0 and b > 0) or (a < 0 and b < 0)` covers "same sign" checks. The integer result (-1/0/1) provides no additional constraint power beyond what comparisons already offer. |
| **Substring extraction** | `left`, `right` | Boolean test functions (`startsWith`, `endsWith`) cover the constraint use case for prefix/suffix validation. Extracting substrings as values is a transformation operation — Precept validates, it does not extract. (`substring` for mid-string positional validation is considered for v2 separately.) |
| **String mutation** | `substitute`, `replace`, `reverse` | Precept governs what data must look like, not how to transform it. String replacement and normalization are host-application responsibilities — the host normalizes, Precept validates the normalized result. This is a design-principle exclusion: transformation ≠ constraint. |
| **String formatting** | `text`, `rept`, `lpad`, `rpad` | Display formatting is a presentation concern, not a lifecycle constraint operation. |
| **Position finding** | `find`, `search`, `position`, `charindex` | The integer position has no constraint utility — the domain need is boolean presence testing, covered by `contains`. |
| **Directional trim** | `ltrim`, `rtrim` | Standard `trim` covers the domain need. No business-entity lifecycle constraint requires selectively preserving whitespace on one side. |
| **Type checking** | `isnumber`, `istext`, `isblank`, `iserror` | Static typing eliminates type checks. `== null` covers blank tests. Totality eliminates error checks. These are structurally unnecessary in Precept. |
| **Type conversion** | `cast`, `convert`, `nullif` | Type coercion is a type system design question, not a function. `nullif` is SQL-specific (avoid division-by-zero) and unnecessary given Precept's totality guarantee. |
| **Bitwise operations** | `Math.BitOperations.*` | No domain-relevant lifecycle constraint requires bitwise operations. |

---

## 5. Domain Evidence — Cross-Domain Gap Analysis

> **Methodology note:** This section evaluates gaps by domain relevance across business verticals, not by sample coverage. The question is: "Would a precept author in this domain need this function?" — not "Does a sample file use it?"

### `pow(base, exponent)` — Prevention gap: YES

| Domain | Constraint Pattern | Why `pow` is required |
|---|---|---|
| **Loans / Lending** | Compound interest ceilings: `TotalObligation <= Principal * pow(1 + MonthlyRate, Periods)` | Loan lifecycle constraints must cap total obligation including compound interest. Without `pow`, the compound growth formula is inexpressible. |
| **Insurance** | Reserve accumulation: `ReserveTarget >= BasePremium * pow(1 + GrowthRate, YearsActive)` | Insurance reserves grow exponentially. Lifecycle rules enforcing minimum reserves need exponentiation. |
| **Billing / Penalties** | Penalty escalation: `LateFee <= BaseLateFee * pow(2, OverduePeriods)` | "Penalty doubles each period" is a real billing pattern. Without `pow`, authors must hard-code period-specific caps. |
| **Government** | Inflation adjustment: `BenefitCap >= BaseAmount * pow(1 + InflationRate, YearsSinceBase)` | Government benefit programs adjust for inflation over time, requiring exponential growth invariants. |

**Design constraint:** Exponent must be restricted to non-negative integers for totality. `pow(x, 0) = 1` for all x. `pow(0, n) = 0` for n > 0. No undefined inputs.

### `clamp(value, low, high)` — Prevention gap: NO (but readability gap: YES)

| Domain | Constraint Pattern | `clamp` vs. composed form |
|---|---|---|
| **Insurance** | `set ApprovedAmount = clamp(RequestedAmount, 0, PolicyLimit)` | vs. `set ApprovedAmount = min(max(RequestedAmount, 0), PolicyLimit)` |
| **Billing / Subscriptions** | `set RetentionDiscount = clamp(ComputedDiscount, 0, 100)` | vs. `set RetentionDiscount = min(max(ComputedDiscount, 0), 100)` |
| **Healthcare** | `set DosageAmount = clamp(CalculatedDose, MinDose, MaxDose)` | vs. `set DosageAmount = min(max(CalculatedDose, MinDose), MaxDose)` |
| **Government** | `set BenefitAmount = clamp(EligibleAmount, 0, AnnualCap)` | vs. `set BenefitAmount = min(max(EligibleAmount, 0), AnnualCap)` |
| **Any scored entity** | `set FinalScore = clamp(RawScore, 0, 100)` | vs. `set FinalScore = min(max(RawScore, 0), 100)` |

Range-bounding is universal. The `clamp` form communicates intent directly ("bound this value to a range") while the composed form requires the reader to mentally reconstruct the range semantics. For a DSL targeting business authors, this readability difference justifies a dedicated function.

### `sign(number)` — Prevention gap: NO

Direction-of-change constraints are real (e.g., "adjustment must be positive", "balance must not change sign"). But these are inherently boolean predicates: `Adjustment > 0`, `(OldBalance > 0 and NewBalance > 0) or (OldBalance < 0 and NewBalance < 0)`. The integer result of `sign` (-1/0/1) provides no additional constraint power. Comparison operators are both sufficient and more readable in constraint expressions. Excludes on design-principle grounds.

### `substring(s, start, length)` — Prevention gap: MODERATE

Positional format validation exists in real domains: insurance policy numbers with embedded state codes at positions 3–4, account numbers with embedded branch identifiers, serial numbers with embedded manufacturer codes, government form numbers with embedded year codes. `startsWith`/`endsWith` cover prefix and suffix but cannot test mid-string positions. Without `substring`, a precept author cannot express "characters 3–5 of PolicyNumber must match StateCode" as a constraint.

However, this need is less common than prefix/suffix validation, and the strongest version of this use case (complex format parsing) may be better served by regex support (out of scope for Precept). Mid-string positional extraction straddles the line between constraint validation and data transformation. Consider for v2.

### `replace` / `substitute` — Prevention gap: WEAK

String normalization use cases exist: comparing differently-formatted versions of phone numbers, stripping hyphens from identifiers for comparison. But this is fundamentally a transformation operation. Precept's design principle is that the host application normalizes data before lifecycle rules evaluate it. If a precept needs to compare `"555-1234"` and `"5551234"`, the host should normalize before submission, and the precept should validate the normalized form. This is a design-principle exclusion, not a capability gap — the prevention gap is real but the correct resolution is host-side normalization, not in-language string mutation.

### String `contains` — Prevention gap: YES

The `contains` operator currently covers collection membership. String containment — "does this string include this substring?" — is a distinct domain need:

| Domain | Constraint Pattern |
|---|---|
| **Any entity with email** | `Email contains "@"` (basic format validation) |
| **Insurance / Legal** | `ClaimDescription contains "fraud"` triggers review workflow |
| **HR / Hiring** | `Resume contains RequiredCertification` |
| **Government** | `ApplicationNotes contains "expedite"` flags priority processing |

This is pure, total, deterministic, and present in virtually every surveyed system. The gap is real. Recommendation: address via `contains` operator overload for strings (issue #15 scope) or as a new function. This is a design decision that should be tracked.

---

## Summary

> **Methodology correction applied.** This revision eliminates sample-corpus-based reasoning. All evaluations now use domain relevance, design-principle fit, cross-system consensus, and prevention-gap analysis.

The corrected analysis recommends expanding the proposal from 23 to 25 signatures by adding **`pow(base, exponent)`** and **`clamp(value, low, high)`**:

- **`pow`** closes a real prevention gap — compound interest, penalty escalation, and exponential growth constraints are inexpressible without it. With exponent restricted to non-negative integers, the function is total. All 3 surveyed systems include it.
- **`clamp`** closes a readability gap — range-bounding is one of the most common constraint patterns across all business domains, and `clamp(x, lo, hi)` is dramatically clearer than `min(max(x, lo), hi)` for a DSL targeting business authors. Total for all inputs.
- **String `contains`** is flagged as a real prevention gap that needs a design decision — extend the `contains` operator to strings (issue #15) or add a dedicated function. This is not a function-library question per se, but the gap is surfaced here because the analysis uncovered it.
- **`substring`** moves from "Exclude" to "Consider v2" — mid-string positional format validation is a real domain need, though less common than prefix/suffix validation.
- **`sign`**, **`left`/`right`**, **`replace`/`substitute`** remain excluded but with corrected reasoning — design-principle and domain-based exclusion, not sample-based.

The numeric core (`abs`, `floor`, `ceil`, `round`, `truncate`, `min`, `max`, **`pow`**, **`clamp`**) now covers the complete set of business-grade numeric operations that are pure, total, and domain-relevant. The string functions (`toLower`, `toUpper`, `startsWith`, `endsWith`, `trim`) remain well-calibrated for constraint-relevant string operations.

### Verdict Changes from Previous Version

| Function | Previous Verdict | Revised Verdict | Reason for Change |
|---|---|---|---|
| `pow(base, exponent)` | ⏳ Consider v2 | ✅ **Include now** | Compound interest, penalty escalation, exponential growth are real domain patterns. Previous exclusion relied on "no sample expresses this" — circular reasoning. With integer exponent, total. |
| `clamp(value, low, high)` | ⏳ Consider v2 (convenience) | ✅ **Include now** | Range-bounding is universal across domains. DSL readability improvement justifies dedicated function despite composability from min/max. |
| `substring(s, start, len)` | ❌ Exclude permanently | ⏳ **Consider v2** | Mid-string positional format validation is a real domain need. Previous exclusion relied on "no sample needs substring values" — circular reasoning. |
| String `contains` | Not assessed | 🔍 **Design decision needed** | Real prevention gap for string containment testing. Recommend addressing in issue #15 scope. |
| `sign(number)` | ❌ Exclude permanently | ❌ Exclude | Verdict unchanged. Reasoning corrected from sample-based to domain-based (comparisons cover the constraint need). |
| `left` / `right` | ❌ Exclude permanently | ❌ Exclude | Verdict unchanged. Reasoning corrected: `startsWith`/`endsWith` cover the boolean constraint case; extraction is transformation. |
| `replace` / `substitute` | ❌ Exclude permanently | ❌ Exclude | Verdict unchanged. Reasoning corrected: design-principle exclusion (transformation ≠ constraint), not sample-based. |
| `concat` (as function) | ❌ Exclude permanently | ❌ Exclude (function form) | Function form excluded; `+` operator overload on strings remains ⏳ Consider v2 as a separate proposal. |
