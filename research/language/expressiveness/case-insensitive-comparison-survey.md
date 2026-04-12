# Case-Insensitive Comparison Operator Survey

**Research question:** Is there precedent for `~=` as a case-insensitive comparison operator? How do languages and DSLs handle case-insensitive string comparison? What are the cascade implications?

**Context:** Precept Issue #16 is evaluating whether to add a `~=` operator for case-insensitive string equality. This survey catalogs prior art across 15+ systems.

---

## 1. `~=` Precedent Analysis

### Systems that use `~=`

| System | `~=` meaning | Confusability risk |
|--------|-------------|-------------------|
| **Lua 5.4** | **Inequality** (not equal). From §3.4.4: "The operator `~=` is exactly the negation of equality (`==`)." The `~` alone is bitwise XOR/NOT. | **Critical.** Lua has 350K+ GitHub repos. A Precept `~=` meaning "case-insensitive equal" would directly contradict a well-known operator in a widely-used language. |
| **CSS Selectors** | `[attr~=value]` matches when `value` is a whitespace-separated word in the attribute. Not case-insensitive — it's a containment test on space-delimited tokens. | **High.** Different semantics entirely. Web developers would misread `~=` as token-matching. |

### Systems that use `=~`

| System | `=~` meaning | Notes |
|--------|-------------|-------|
| **Perl** | Regex binding operator. `$str =~ /pattern/i` applies a regex. The `/i` flag makes it case-insensitive. | `=~` is regex, not CI comparison |
| **Ruby** | Regex match. `"hello" =~ /world/` returns match position or `nil`. | Same as Perl — regex, not CI |

### Verdict

**No language or DSL uses `~=` for case-insensitive comparison.** The symbol is taken by Lua (inequality) and CSS (word-match). The reversed form `=~` is taken by Perl and Ruby (regex binding). Using `~=` for CI comparison would be unprecedented and confusing.

---

## 2. Case-Insensitive Comparison Catalog

### Pattern A: Function Composition (`toLower`/`toUpper` wrapping)

The most common approach. No dedicated CI operator — users wrap operands with case-conversion functions.

| System | Approach | Example |
|--------|----------|---------|
| **OData 4.01** | `tolower()` / `toupper()` functions. Spec explicitly states: "case-insensitive comparison can be achieved in combination with tolower or toupper." All string functions (`contains`, `startswith`, `endswith`, `indexof`) are case-sensitive. | `$filter=tolower(Name) eq 'milk'` |
| **FEEL / DMN** | `lower case()` / `upper case()` functions. `contains()`, `starts with()`, `ends with()` are all case-sensitive. | `lower case(Name) = "milk"` |
| **XPath / XQuery** | `lower-case()` / `upper-case()` functions from the Functions and Operators spec. | `lower-case($name) = 'milk'` |
| **SQL (standard)** | `LOWER()` / `UPPER()` functions. Case sensitivity depends on collation. | `WHERE LOWER(name) = 'milk'` |

**Characteristics:** Explicit, composable, no new operators needed, but verbose when comparing both sides. Every string operation must be independently wrapped.

### Pattern B: Three-Variant Operator Family (PowerShell)

PowerShell has the most comprehensive CI system of any language surveyed. Default comparison is case-insensitive, with explicit prefixes for CI/CS variants.

| Base operator | CI variant | CS variant | Purpose |
|--------------|-----------|-----------|---------|
| `-eq` | `-ieq` | `-ceq` | Equality |
| `-ne` | `-ine` | `-cne` | Inequality |
| `-lt` | `-ilt` | `-clt` | Less than |
| `-gt` | `-igt` | `-cgt` | Greater than |
| `-le` | `-ile` | `-cle` | Less or equal |
| `-ge` | `-ige` | `-cge` | Greater or equal |
| `-like` | `-ilike` | `-clike` | Wildcard match |
| `-match` | `-imatch` | `-cmatch` | Regex match |
| `-contains` | `-icontains` | `-ccontains` | Collection contains |
| `-replace` | `-ireplace` | `-creplace` | String replace |

**Characteristics:** Complete cascade — CI extends to every string-touching operator. Systematic naming with `-i`/`-c` prefix. But requires tripling the operator surface.

### Pattern C: Modifier Flag / Parameter

A flag or parameter attached to the comparison that toggles case sensitivity.

| System | Approach | Example |
|--------|----------|---------|
| **CSS Selectors 4** | `i` flag suffix on attribute selectors. `s` flag for explicit case-sensitive. | `[data-name="foo" i]` |
| **Perl regex** | `/i` flag on regex patterns. | `$str =~ /pattern/i` |
| **Ruby regex** | `/i` flag on regex literals. | `str =~ /pattern/i` |
| **Kotlin** | `ignoreCase` parameter on string methods. | `a.equals(b, ignoreCase = true)` |
| **C# (.NET)** | `StringComparison` enum parameter. | `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` |

**Characteristics:** Localized control per-comparison. No new operators. But the flag mechanism varies widely (suffix, parameter, enum).

### Pattern D: Dedicated CI Method / Function

A named method specifically for case-insensitive comparison.

| System | Approach | Example |
|--------|----------|---------|
| **Ruby** | `casecmp` method returns 0/-1/1. `casecmp?` returns boolean. | `"ABC".casecmp?("abc") # => true` |
| **C (POSIX)** | `strcasecmp()` function. | `strcasecmp(a, b) == 0` |
| **Python** | `str.casefold()` for aggressive CI, or `str.lower()` comparison. No CI operator. | `a.casefold() == b.casefold()` |
| **Java** | `String.equalsIgnoreCase()` method. | `a.equalsIgnoreCase(b)` |

**Characteristics:** Self-documenting, discoverable, single-purpose. But only covers equality — doesn't extend to contains/startsWith/etc.

### Pattern E: Environment / Session Setting

Case sensitivity controlled at environment or schema level, not per-comparison.

| System | Approach | Notes |
|--------|----------|-------|
| **SQL collation** | Column/table/database-level collation (`CI`/`CS` suffixes). | `COLLATE SQL_Latin1_General_CP1_CI_AS` — all comparisons on that column become CI |
| **Bash** | `shopt -s nocasematch` toggles CI for `case` and `[[` pattern matching. | Session-wide, affects all subsequent comparisons |
| **MySQL** | Default collation is `utf8mb4_0900_ai_ci` (accent-insensitive, case-insensitive). | CI is the default in MySQL |

**Characteristics:** Set-and-forget, no per-comparison boilerplate. But makes behavior non-local and harder to reason about.

### Pattern F: No CI Support

Some policy/rule DSLs deliberately omit case-insensitive comparison.

| System | Notes |
|--------|-------|
| **Cedar** | `==` is case-sensitive. `"Something" == "something"` is `false`. No `toLower`/`toUpper` functions. No CI mechanism at all. String operations limited to `like` (wildcard match, case-sensitive). Cedar's philosophy prioritizes simplicity and formal verification over string manipulation convenience. |

---

## 3. Cascade Analysis

The critical design question: if you add CI equality, does CI need to extend to `contains`, `startsWith`, `endsWith`, `!=`, relational operators?

### Systems with Full Cascade

| System | Cascade scope | Mechanism |
|--------|--------------|-----------|
| **PowerShell** | All operators — equality, relational, wildcard, regex, contains, replace | `-i`/`-c` prefix on every operator |
| **SQL collation** | All comparisons, `LIKE`, ordering, grouping on that column | Schema-level setting |

### Systems with No Cascade (Manual)

| System | What happens | User burden |
|--------|-------------|-------------|
| **OData** | Must wrap both sides with `tolower()` for every single operation | `$filter=contains(tolower(Name),tolower('milk'))` |
| **FEEL** | Same — `lower case()` on every comparison | `contains(lower case(Name), lower case("milk"))` |
| **Kotlin** | Each method has its own `ignoreCase` parameter | `a.contains(b, ignoreCase = true)` |
| **C# (.NET)** | Each method has its own `StringComparison` parameter | `a.Contains(b, StringComparison.OrdinalIgnoreCase)` |

### Cascade Pressure

If Precept adds `~=` for CI equality, users will immediately ask:
1. What about `~!=` (CI not-equal)?
2. What about CI `contains`? (`~contains`? `icontains`?)
3. What about CI `startsWith` / `endsWith`?
4. What about CI relational comparisons (`~<`, `~>`)?

PowerShell's experience shows this cascade is real — they needed 30+ operator variants. OData and FEEL avoided it by not having a CI operator at all, pushing CI to the function layer.

---

## 4. Pattern Taxonomy Summary

| Pattern | Symbol cost | Cascade cost | Readability | Discoverability | Precept fit |
|---------|------------|-------------|-------------|----------------|-------------|
| **A. Function composition** | None | None (already scales) | Medium (verbose) | High | **Strong** — already has `toLower`/`toUpper` |
| **B. Operator variants** | Very high (3x operators) | Built-in | Medium | Low (must memorize prefixes) | **Poor** — violates keyword-dominant principle |
| **C. Modifier flag** | Low | Per-operation | High | Medium | **Medium** — could work as keyword modifier |
| **D. Dedicated function** | Low | Each function independently | High | High | **Strong** — fits function library |
| **E. Environment setting** | None | Implicit (all comparisons) | Low (non-local) | Low | **Poor** — violates explicitness principle |
| **F. No support** | None | None | N/A | N/A | Viable but limits expressiveness |

---

## 5. Recommendation for Precept

### Against `~=` as an Operator

1. **No precedent.** Zero languages use `~=` for case-insensitive comparison. Lua uses it for inequality. CSS uses it for word-matching. This is not an obscure risk — Lua is widely known.

2. **Cascade pressure.** Adding `~=` creates immediate demand for `~!=`, CI `contains`, CI `startsWith`, CI `endsWith`. PowerShell needed 30+ variants. Precept has no mechanism to satisfy this demand without a proliferation of symbolic operators.

3. **Violates Keyword vs Symbol Framework.** From the locked design framework: "Symbols are reserved for universal mathematical notation" and "Default to keyword." CI comparison is not universal math notation — it's a domain-specific string behavior. The tiebreaker says keyword.

4. **Violates Principle #13.** "Keywords for domain, symbols for math." Case-insensitive comparison is domain behavior, not math.

### Recommended Alternatives (in preference order)

#### Option 1: Function composition (do nothing — already supported)

```
toLower(Status) == toLower(userInput)
```

Precept already has `toLower` and `toUpper`. This is how OData, FEEL, XPath, and SQL handle CI comparison. It's explicit, composable, and introduces zero new syntax.

**Pro:** No language surface change. Already works.
**Con:** Verbose for repeated use.

#### Option 2: Dedicated `equalsIgnoreCase` function

```
equalsIgnoreCase(Status, userInput)
```

Following Java's `equalsIgnoreCase`, Ruby's `casecmp?`, and C's `strcasecmp`. A named function is self-documenting and discoverable.

**Pro:** Keyword-based, self-documenting, follows Precept conventions.
**Con:** Only covers equality. Would need `containsIgnoreCase`, `startsWithIgnoreCase`, etc. for cascade.

#### Option 3: `ignorecase` keyword modifier (future consideration)

```
Status == userInput ignorecase
```

A keyword modifier that can attach to any comparison operator. Similar to CSS's `i` flag but using a keyword per Precept conventions.

**Pro:** Composes with all operators. Keyword-based.
**Con:** New syntactic concept (post-expression modifier). Needs careful design.

### Bottom Line

**Do not add `~=`.** The function composition pattern (Option 1) already works, matches the dominant industry approach, and avoids all cascade and confusability problems. If CI comparison becomes a frequent enough pain point in real usage, consider Option 2 (named function) as a convenience — but only after empirical evidence of need.

---

## 6. Risk Matrix

| Risk | Severity | Likelihood | Mitigation |
|------|----------|-----------|------------|
| `~=` confused with Lua's inequality | Critical | High | Don't use `~=` |
| `~=` confused with CSS word-match | High | Medium | Don't use `~=` |
| Cascade demand after adding any CI operator | High | Certain | Use function pattern instead |
| `toLower`/`toUpper` wrapping too verbose | Medium | Medium | Add `equalsIgnoreCase` function if needed |
| No CI support at all | Low | Low | Function composition already covers this |

---

## Sources

- Lua 5.4 Reference Manual §3.4.4 — https://www.lua.org/manual/5.4/manual.html
- CSS Selectors Level 4 §7 — https://www.w3.org/TR/selectors-4/#attribute-case
- PowerShell about_Comparison_Operators — https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_comparison_operators
- Cedar Operators and Functions — https://docs.cedarpolicy.com/policies/syntax-operators.html
- OData 4.01 URL Conventions §5.1.1 — https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.html
- DMN FEEL Handbook — https://kiegroup.github.io/dmn-feel-handbook/
- Precept Language Design §Keyword vs Symbol Design Framework — `docs/PreceptLanguageDesign.md`
