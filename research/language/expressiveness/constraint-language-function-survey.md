# Constraint/DSL Language Function Library Survey

**Date:** 2026-04-11
**Author:** Frank (Lead/Architect, Language Designer)
**Request:** Shane — validate Issue #16 function library against comparable systems
**Issue:** #16 — Built-in Functions

---

## Context

Precept's Issue #16 proposes 23 function signatures across three numeric types and string:

| Category | Functions |
|----------|-----------|
| **Number** | `abs`, `floor`, `ceil`, `round(1)`, `truncate`, `min(variadic)`, `max(variadic)` |
| **Integer** | `abs`, `min(variadic)`, `max(variadic)` |
| **Decimal** | `abs`, `floor`, `ceil`, `round(1→int)`, `round(2,N)`, `truncate`, `min(variadic)`, `max(variadic)` |
| **String** | `toLower`, `toUpper`, `startsWith`, `endsWith`, `trim` |

This survey evaluates five comparable systems: FEEL (DMN), Cedar, Drools/NRules, Zod/Valibot, and FluentValidation. The goal is to determine whether the proposal is sufficient, and whether any functions should be added, deferred, or excluded.

Governing principle: *"The expression surface must be complete enough that every domain-relevant constraint is expressible, while remaining closed and decidable."*

### Methodology Note

**The sample corpus (`.precept` files in `samples/`) is not valid evidence for inclusion or exclusion of functions.** The samples were authored without the functions under evaluation — they cannot exercise capabilities that don't exist yet. Additionally, the corpus is incomplete: it covers ~24 verticals from a universe of hundreds.

Each function is evaluated on:
1. **Domain relevance** — does the function serve real constraint needs across business verticals (insurance, loans, hiring, hospitality, logistics, maintenance, billing, subscriptions, healthcare, government)?
2. **System consensus** — how many of the 5 surveyed systems include it?
3. **Precept principles** — is the function pure, total, and deterministic?
4. **Expressibility gap** — does its absence force authors to omit constraints the domain genuinely needs, or use verbose workarounds that obscure intent?

Sample files may be referenced as *illustrations* of a domain need, but "no sample uses this" is never evidence against inclusion.

---

## 1. FEEL Comparison (DMN — Friendly Enough Expression Language)

FEEL is the closest comparable to Precept: it's the expression language for DMN decision tables, used to express business constraints and decision logic.

### 1.1 Numeric Functions

| FEEL Function | Precept Equivalent | Notes |
|---|---|---|
| `abs(n)` | `abs(x)` | ✅ Direct match |
| `floor(n)` | `floor(x)` | ✅ Direct match |
| `floor(n, scale)` | — | FEEL allows floor-to-N-places. Precept has `round(x, N)` but not floor/ceil with scale. |
| `ceiling(n)` | `ceil(x)` | ✅ Direct match (different name) |
| `ceiling(n, scale)` | — | Same gap as floor(n, scale). |
| `decimal(n, scale)` | `round(x, N)` | ✅ Functionally equivalent (FEEL's `decimal` is round-to-scale) |
| `round up(n, scale)` | — | Specific rounding mode. Precept uses banker's rounding only. |
| `round down(n, scale)` | `truncate(x)` | `truncate` is round-toward-zero to integer. FEEL's is to arbitrary scale. |
| `round half up(n, scale)` | — | FEEL offers 4 rounding modes; Precept offers 1 (banker's). |
| `round half down(n, scale)` | — | Same. |
| `modulo(a, b)` | `%` operator | ✅ Precept has `%` operator — no function needed |
| `sqrt(n)` | — | Mathematical function. Not domain-relevant for business constraints. |
| `log(n)` | — | Mathematical function. Exclude. |
| `exp(n)` | — | Mathematical function. Exclude. |
| `odd(n)` | — | Parity check. Expressible as `n % 2 != 0`. |
| `even(n)` | — | Parity check. Expressible as `n % 2 == 0`. |
| `random number()` | — | Non-deterministic. Violates Precept's determinism guarantee. Exclude permanently. |

### 1.2 String Functions

| FEEL Function | Precept Equivalent | Notes |
|---|---|---|
| `upper case(s)` | `toUpper(s)` | ✅ Direct match |
| `lower case(s)` | `toLower(s)` | ✅ Direct match |
| `starts with(s, match)` | `startsWith(s, match)` | ✅ Direct match |
| `ends with(s, match)` | `endsWith(s, match)` | ✅ Direct match |
| `contains(s, match)` | `contains` operator | ✅ Precept's `contains` is an operator, not a function. Works on strings and collections. |
| `trim(s)` | `trim(s)` | ✅ Direct match |
| `string length(s)` | `.length` accessor | ✅ Precept exposes `.length` on strings — no function needed |
| `substring(s, start)` | — | String slicing. See recommendations below. |
| `substring(s, start, len)` | — | String slicing. See recommendations below. |
| `substring before(s, m)` | — | String extraction before a delimiter. |
| `substring after(s, m)` | — | String extraction after a delimiter. |
| `matches(s, regex)` | — | Regex matching. See recommendations below. |
| `replace(s, regex, rep)` | — | Regex replacement. Mutating-ish. |
| `split(s, delim)` | — | String-to-list. Precept has no list-of-string type. |
| `is blank(s)` | — | Equivalent to `trim(s) == ""` or combined with `notempty` constraint. |
| `uuid()` | — | Non-deterministic. Violates determinism. Exclude permanently. |

### 1.3 List Functions

| FEEL Function | Precept Equivalent | Notes |
|---|---|---|
| `count(list)` | `.count` accessor | ✅ Already covered |
| `min(list)` | `.min` accessor | ✅ Already covered |
| `max(list)` | `.max` accessor | ✅ Already covered |
| `sum(list)` | `.sum` accessor | ✅ Already covered |
| `list contains(list, e)` | `contains` operator | ✅ Already covered |
| `mean(list)` | — | Average. See recommendations. |
| `median(list)` | — | Statistical. Likely v2+ at best. |
| `stddev(list)` | — | Statistical. Exclude for v1. |
| `all(list)` / `any(list)` | `all` / `any` keywords | ✅ Precept has these as guard quantifiers |
| `is empty(list)` | `.count == 0` or `notempty` constraint | ✅ Already expressible |
| `sort`, `append`, `concatenate`, `flatten`, `reverse`, etc. | — | List mutation/construction. Precept collections are action-mutated, not expression-constructed. Exclude. |

### 1.4 FEEL Summary

**Precept covers 15 of FEEL's ~20 domain-relevant functions.** The gaps are:
- `substring` / string slicing — domain-relevant for extracting prefixes from structured identifiers (policy numbers, claim IDs, permit codes). Candidate for v2.
- `matches` (regex) — strongest v2 candidate; 4-system consensus (see consensus matrix)
- `contains` for strings — already an operator in Precept
- Statistical aggregates (mean, median) — domain-relevant for threshold constraints (average claim amounts, average spend tiers). Candidate for v2.
- Rounding mode variants — Precept's single banker's rounding is sufficient for financial domains

---

## 2. Cedar Comparison (AWS Authorization Policy Language)

Cedar is a **pure constraint language** — policies evaluate to permit/forbid with no mutation. This makes it the most philosophically aligned comparison.

### 2.1 What Cedar Provides

Cedar is deliberately minimal in its function surface:

| Cedar Feature | Precept Equivalent | Notes |
|---|---|---|
| `==`, `!=`, `<`, `<=`, `>`, `>=` | Same operators | ✅ Identical |
| `&&`, `\|\|`, `!` | `and`, `or`, `not` | ✅ Keyword equivalents |
| `+`, `-`, `*` (long only) | `+`, `-`, `*`, `/`, `%` | ✅ Precept has more (division, modulo) |
| `like` (wildcard match) | — | Simpler than regex. Interesting but narrow. |
| `decimal()` (string→decimal) | N/A (Precept has native decimal type) | Not needed |
| `datetime()`, `duration()` | N/A (Precept has no date/time types) | Not applicable currently |
| `.contains()`, `.containsAll()`, `.containsAny()` | `contains` operator | Partial — Precept's `contains` is single-element. No `containsAll`/`containsAny`. |
| `.isEmpty()` | `.count == 0` | ✅ Expressible |
| `if/then/else` | `if/else` guard branches | ✅ Covered |
| `has` (attribute presence) | Nullable + `!= null` | ✅ Covered differently |

### 2.2 What Cedar Excludes

Cedar has **no**:
- `abs`, `floor`, `ceil`, `round`, `min`, `max` — no numeric functions at all
- `toLower`, `toUpper`, `trim`, `startsWith`, `endsWith` — no string functions
- No division operator, no modulo
- No aggregate functions

### 2.3 Cedar Takeaway

Cedar validates Precept's overall approach — **keep the function library small** — but Cedar is so minimal it doesn't help validate specific function choices. Cedar does confirm that a constraint language can be fully useful with a very small expression surface. The one interesting Cedar feature Precept lacks is **`containsAll` / `containsAny`** for set operations, which Precept could express as collection accessors in a later version.

---

## 3. Drools/NRules Comparison

Drools DRL and NRules are rule-engine expression languages. They sit closer to general-purpose programming than Precept.

### 3.1 Drools DRL Built-in Functions

Drools primarily delegates to the host language (Java) for functions, but DRL MVEL/FEEL-mode provides:

| Drools/NRules Feature | Precept Equivalent | Notes |
|---|---|---|
| `Math.abs()`, `Math.min()`, `Math.max()` | `abs`, `min`, `max` | ✅ Match |
| `Math.floor()`, `Math.ceil()`, `Math.round()` | `floor`, `ceil`, `round` | ✅ Match |
| `String.toLowerCase()`, `toUpperCase()` | `toLower`, `toUpper` | ✅ Match |
| `String.startsWith()`, `endsWith()`, `contains()` | `startsWith`, `endsWith`, `contains` | ✅ Match |
| `String.trim()` | `trim` | ✅ Match |
| `String.length()` | `.length` accessor | ✅ Match |
| `String.matches(regex)` | — | Regex. Same gap as FEEL. |
| `String.substring(start, end)` | — | Same gap as FEEL. |
| `String.replace()` | — | String mutation. |
| Collection `size()`, `contains()`, `isEmpty()` | `.count`, `contains`, `.count == 0` | ✅ Match |
| `accumulate` (sum, avg, min, max, count) | Collection accessors | ✅ Partially covered |
| Type coercion functions | N/A | Precept is statically typed — not needed |

### 3.2 NRules (.NET)

NRules delegates entirely to C# — no built-in function library of its own. It uses LINQ and .NET BCL methods directly. This confirms that .NET rule engines don't need a custom function library beyond what the host language provides. For Precept, which *is* the language, the proposed functions fill this role.

### 3.3 Drools/NRules Takeaway

**Full alignment.** Every function in Precept's proposal has a Drools equivalent. The main gap is `matches` (regex), which Drools inherits from Java. Drools also confirms that `abs`, `min`, `max`, `floor`, `ceil`, `round` are the core numeric functions rule authors actually use.

---

## 4. Zod/Valibot Comparison

Zod and Valibot are schema validation libraries. Their "functions" are validation methods (constraints), not expression-level functions. This distinction matters for Precept.

### 4.1 String Operations

| Zod/Valibot | Is it a function or constraint? | Precept Coverage |
|---|---|---|
| `.min(n)`, `.max(n)`, `.length(n)` | Constraint (string length) | ✅ `minlength`, `maxlength` constraints |
| `.regex(pattern)` | Constraint (pattern match) | ⚠️ Gap — no regex in Precept |
| `.startsWith(s)`, `.endsWith(s)` | Constraint (prefix/suffix check) | ✅ `startsWith`, `endsWith` functions |
| `.includes(s)` | Constraint (substring check) | ✅ `contains` operator |
| `.trim()` | Transform (whitespace removal) | ✅ `trim` function |
| `.toLowerCase()`, `.toUpperCase()` | Transform (case conversion) | ✅ `toLower`, `toUpper` functions |
| `.uppercase()`, `.lowercase()` | Constraint (must be all-upper/all-lower) | ⚠️ No constraint equivalent, but `toLower(x) == x` is expressible |
| `.email()` | Format validator | ❌ Precept doesn't validate email format — correct for its domain |
| `.uuid()` | Format validator | ❌ Same — format validation is a validation-library concern |
| `.url()`, `.ipv4()`, `.ipv6()` | Format validators | ❌ Infrastructure/network domain, not Precept's domain |
| `.datetime()`, `.date()`, `.time()` | Format validators | ❌ Precept has no date/time types yet |

### 4.2 Numeric Operations

| Zod/Valibot | Is it a function or constraint? | Precept Coverage |
|---|---|---|
| `.min(n)`, `.max(n)` | Constraint (range bounds) | ✅ `min`, `max` constraint keywords |
| `.positive()`, `.nonnegative()` | Constraint | ✅ `positive`, `nonnegative` constraint keywords |
| `.negative()`, `.nonpositive()` | Constraint | ⚠️ Not in Precept. Expressible as `max 0` or invariant. |
| `.multipleOf(n)` / `.step(n)` | Constraint | ⚠️ Not in Precept. Expressible as `x % n == 0`. |
| `.int()` | Constraint (integer check) | ✅ Separate `integer` type in Precept |
| `.finite()` | Constraint | ✅ Precept numbers are always finite (no Infinity/NaN) |

### 4.3 Zod/Valibot Takeaway

**Most Zod/Valibot operations map to Precept's constraint keywords, not functions.** The main gaps are:
- **`regex` / `matches`**: The only cross-system gap that consistently appears
- Format validators (email, UUID, URL): Not relevant to Precept's domain — these are infrastructure/identity concerns, not business-entity integrity constraints

---

## 5. FluentValidation Comparison

FluentValidation is .NET's dominant validation library. Its validators are closest to Precept's constraint model.

| FluentValidation Validator | Is it a function or constraint? | Precept Coverage |
|---|---|---|
| `NotNull()` | Constraint (non-null) | ✅ `nullable` / `!= null` invariant |
| `NotEmpty()` | Constraint (non-empty) | ✅ `notempty` constraint keyword |
| `Equal()`, `NotEqual()` | Constraint (equality) | ✅ `==`, `!=` operators |
| `Length(min, max)` | Constraint (string length range) | ✅ `minlength`, `maxlength` constraint keywords |
| `MinimumLength(n)`, `MaximumLength(n)` | Constraint | ✅ Same |
| `LessThan(n)`, `GreaterThan(n)` | Constraint (comparison) | ✅ `<`, `>` operators |
| `LessThanOrEqualTo(n)`, `GreaterThanOrEqualTo(n)` | Constraint | ✅ `<=`, `>=` operators |
| `InclusiveBetween(a, b)` | Constraint (range) | ✅ Expressible as `x >= a and x <= b` |
| `ExclusiveBetween(a, b)` | Constraint (range) | ✅ Expressible as `x > a and x < b` |
| `Matches(regex)` | Constraint (pattern match) | ⚠️ Gap — no regex in Precept |
| `EmailAddress()` | Format validator | ❌ Domain-specific. Not Precept's concern. |
| `CreditCard()` | Format validator (Luhn) | ❌ Domain-specific. Not Precept's concern. |
| `PrecisionScale(p, s)` | Constraint (decimal precision) | ✅ `maxplaces` constraint keyword |
| `IsInEnum()` | Constraint (valid enum value) | ✅ `choice` type in Precept |
| `Must(predicate)` | Custom predicate | N/A — Precept invariants serve this role |

### 5.1 FluentValidation Takeaway

**Near-complete coverage.** Precept's constraint keywords handle almost everything FluentValidation provides as built-in validators. The only recurring gap is `Matches(regex)` — regex-based pattern validation.

---

## 6. Consensus Matrix

Functions/operations that appear in **3 or more** surveyed systems, rated for Precept relevance:

| Function | FEEL | Cedar | Drools | Zod | FV | Count | Precept Status | Recommendation |
|---|---|---|---|---|---|---|---|---|
| `abs` | ✅ | — | ✅ | — | — | 2 | ✅ Proposed | **Ship as proposed** |
| `floor` | ✅ | — | ✅ | — | — | 2 | ✅ Proposed | **Ship as proposed** |
| `ceil` | ✅ | — | ✅ | — | — | 2 | ✅ Proposed | **Ship as proposed** |
| `round` | ✅ | — | ✅ | — | — | 2 | ✅ Proposed | **Ship as proposed** |
| `truncate` | ✅ | — | ✅ | — | — | 2 | ✅ Proposed | **Ship as proposed** |
| `min` (variadic) | ✅ | — | ✅ | ✅ | — | 3 | ✅ Proposed | **Ship as proposed** |
| `max` (variadic) | ✅ | — | ✅ | ✅ | — | 3 | ✅ Proposed | **Ship as proposed** |
| `toLower` | ✅ | — | ✅ | ✅ | — | 3 | ✅ Proposed | **Ship as proposed** |
| `toUpper` | ✅ | — | ✅ | ✅ | — | 3 | ✅ Proposed | **Ship as proposed** |
| `startsWith` | ✅ | — | ✅ | ✅ | — | 3 | ✅ Proposed | **Ship as proposed** |
| `endsWith` | ✅ | — | ✅ | ✅ | — | 3 | ✅ Proposed | **Ship as proposed** |
| `trim` | ✅ | — | ✅ | ✅ | — | 3 | ✅ Proposed | **Ship as proposed** |
| `contains` (string) | ✅ | ✅ | ✅ | ✅ | — | 4 | ✅ `contains` operator | **Already covered** |
| `length` (string) | ✅ | — | ✅ | ✅ | ✅ | 4 | ✅ `.length` accessor | **Already covered** |
| `regex/matches` | ✅ | — | ✅ | ✅ | ✅ | 4 | ❌ Gap | **Consider for v2** |
| `isEmpty` (collection) | ✅ | ✅ | ✅ | — | ✅ | 4 | ✅ `.count == 0` | **Already expressible** |
| `count` (collection) | ✅ | — | ✅ | — | — | 2 | ✅ `.count` accessor | **Already covered** |
| `sum` (collection) | ✅ | — | ✅ | — | — | 2 | ✅ `.sum` accessor | **Already covered** |
| `between` (range) | — | — | — | — | ✅ | 1 | Expressible via `>=`/`<=` | **Already expressible** |
| `email` (format) | — | — | — | ✅ | ✅ | 2 | ❌ Not proposed | **Exclude permanently** |
| `substring` | ✅ | — | ✅ | — | — | 2 | ❌ Not proposed | **Consider for v2** |
| `mean/average` | ✅ | — | ✅ | — | — | 2 | ❌ Not proposed | **Consider for v2** |

---

## 7. Recommendations

### 7.1 Ship as Proposed (No Changes Needed)

All 23 signatures in the Issue #16 proposal are validated by this survey. Every proposed function appears in at least 2 surveyed systems, and the core set (`abs`, `floor`, `ceil`, `round`, `min`, `max`, `toLower`, `toUpper`, `startsWith`, `endsWith`, `trim`) appears in 3+. The proposal is well-scoped and well-grounded.

### 7.2 Include Now — Additional Functions

**None.** The current proposal covers all domain-relevant, pure, total, deterministic functions needed for v1. No surveyed system revealed a function that (a) appears in 3+ systems, (b) is pure/total/deterministic, (c) serves a domain need Precept can't already express, AND (d) isn't already in the proposal.

The strongest "near-miss" is `contains` for string substring testing, but Precept already has the `contains` operator that works on strings. No addition needed.

### 7.3 Consider for v2

| Function | Consensus | Domain Example | Rationale | Pure/Total? |
|---|---|---|---|---|
| **`matches(s, pattern)`** (regex) | 4/5 systems | Insurance: policy/claim number formats; Loans: SSN, account number patterns; Healthcare: patient IDs, NPI numbers; Government: case/permit ID formats; Logistics: tracking number patterns; Billing: invoice reference codes | Strongest v2 candidate — highest consensus of any non-proposed function. Deferred from v1 because: (a) regex is a sub-language — adds irreducible complexity to the DSL grammar, parser, and tooling, (b) needs design work on whether pattern is a string literal or a new type. `choice` covers enumerated-value constraints but does NOT cover format validation of structured identifiers, which is a distinct and widespread domain need. | Pure: yes. Total: yes (always returns boolean). Deterministic: yes. |
| **`substring(s, start, len?)`** | 2/5 systems (FEEL, Drools) | Insurance: extract claim category prefixes; Government: parse jurisdiction codes from permit IDs; Logistics: extract carrier codes from tracking numbers; Billing: extract department codes from invoice references | Operationally useful for decomposing structured identifiers. Tension with Precept's constraint-not-transformation principle, though reading a substring for comparison purposes IS a constraint operation. OOB index behavior needs design. | Pure: yes. Total: debatable (OOB indices). Deterministic: yes. |
| **`mean(collection)`** / **`.avg` accessor** | 2/5 systems (FEEL, Drools) | Insurance: average claim amount thresholds for fraud flags; Subscriptions: average monthly usage for tier classification; Healthcare: average wait time constraints; Billing: average spend thresholds for discount eligibility; Hiring: average interview score minimums | Genuine domain need across verticals where threshold constraints reference central tendency. Natural fit as `.avg` accessor alongside `.min`, `.max`, `.sum`. Workaround (`.sum / .count`) is clean but obscures intent. | Pure: yes. Total: yes if non-empty is preconditioned (same as `.min`/`.max`). Deterministic: yes. |
| **`containsAll(set, set)`** / **`containsAny(set, set)`** | 2/5 systems (FEEL, Cedar) | Government: all required documents submitted; Healthcare: all mandatory screenings completed; Hiring: all required interview stages passed; Insurance: all required coverage types present; Compliance: any of listed certifications held | Set-completeness and set-overlap checks. Currently expressible only through multiple `contains` conjunctions, which is verbose and error-prone for larger required sets. Strong candidate as operators or collection methods. | Pure: yes. Total: yes. Deterministic: yes. |

### 7.4 Exclude Permanently

| Function | Reason | Precept Principle Violated |
|---|---|---|
| `random()` / `uuid()` | Non-deterministic | Determinism guarantee — fire/inspect must produce the same result for the same inputs |
| `sqrt`, `log`, `exp` | Mathematical/scientific functions | Not domain-relevant for business entity integrity constraints |
| `email()`, `creditCard()`, `url()`, `ipv4()` | Format validators | Infrastructure/identity concerns, not business-entity lifecycle constraints. A Precept governs *how an entity evolves*, not *what format a string is in*. Format validation belongs in the ingestion layer, not the domain integrity layer. |
| `replace(s, pattern, rep)` | String mutation/transformation | Precept functions are for constraint expressions, not data transformation. `set` actions handle mutation. |
| `split(s, delim)` | String-to-list construction | Precept has no dynamically-constructed collections. Collections are declared fields mutated by actions. |
| `sort`, `reverse`, `append`, `flatten` | List construction/mutation | Same — collections are action-mutated, not expression-constructed. |
| `odd()`, `even()` | Parity checks | Trivially expressible as `x % 2 == 0` / `x % 2 != 0`. Adding dedicated functions violates minimal ceremony. |

---

## 8. Cross-Cutting Observations

### 8.1 Precept's Constraint Keywords Cover What Other Systems Implement as Functions

A key finding: many things other systems expose as functions (range checks, non-null, non-empty, length bounds, positivity) are already first-class **constraint keywords** in Precept (`nonnegative`, `positive`, `min`, `max`, `notempty`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`). This means Precept needs *fewer* functions than comparable systems — the constraint surface absorbs validation that other systems must encode as function calls.

### 8.2 Collection Accessors Cover What Other Systems Implement as Functions

Similarly, FEEL's `count()`, `min()`, `max()`, `sum()` are functions; Precept's `.count`, `.min`, `.max`, `.sum` are **accessors** on collection types. Same information, different mechanism. No function duplication needed.

### 8.3 Cedar Confirms the "Small Is Correct" Thesis

Cedar — the most constraint-oriented system surveyed — has the smallest function surface of all: no numeric functions, no string functions, no aggregates. It proves that a constraint language can be complete and useful with an extremely small expression surface. Precept's 23 functions is already significantly more than what the most similar system provides.

### 8.4 The Regex Gap Is Real and Domain-Significant

`matches` / regex is the only function with 4-system consensus that Precept doesn't cover. Every surveyed business vertical has structured identifiers with format constraints that `choice` cannot express — policy numbers, claim IDs, SSNs, permit codes, tracking numbers, invoice references, patient IDs. Format validation of structured identifiers is a fundamental constraint need, distinct from enumerated-value validation.

The deferral to v2 rests on principled design cost, not absence of need:
- Adding regex embeds a **sub-language** in Precept — irreducible complexity in grammar, parser, type checker, and tooling
- Design decisions are needed: string literal patterns vs. a dedicated pattern type, supported regex subset, error reporting for invalid patterns
- **Recommendation:** Strongest v2 candidate. Design a constrained subset (literal patterns only, no backreferences, no dynamic construction) and ship early in the v2 cycle.

---

## 9. Final Assessment

**The Issue #16 proposal is well-calibrated for v1.** It covers every pure, total, deterministic function that appears in 2+ surveyed systems and serves domain-relevant constraint expression. No function meets ALL of these criteria simultaneously: (a) 3+ system consensus, (b) pure/total/deterministic, (c) addresses an expressibility gap not already covered by Precept's constraint keywords or collection accessors, AND (d) doesn't introduce irreducible design complexity (like a sub-language).

The proposal's restraint is validated by principled analysis: Cedar proves constraint languages work with far less, while FEEL shows Precept has already covered the domain-critical subset of a much larger function library. Precept's constraint keywords and collection accessors cover significant ground that other systems must handle via functions, keeping the function count appropriately minimal.

**The v2 pipeline is stronger than previously assessed.** After removing sample-corpus bias from the evaluation:
- **`matches` (regex)** has the strongest case — 4/5 consensus, domain-critical across every surveyed vertical, pure/total/deterministic. Deferred solely for sub-language design complexity.
- **`.avg` accessor** has genuine cross-vertical need for threshold constraints referencing central tendency.
- **`containsAll` / `containsAny`** address real verbosity pain for multi-element set checks in compliance and workflow domains.
- **`substring`** serves structured-identifier decomposition needs, though it sits in tension with the constraint-not-transformation principle.
