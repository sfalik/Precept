# Business String Ordering Use Cases: Case-Insensitive `<`, `>`, `<=`, `>=`

> **Research question:** Does case-insensitive string *ordering* (`<`, `>`, `<=`, `>=` on arbitrary strings) have real business use cases — as opposed to case-insensitive *equality* (`==`, `!=`), which is well-documented?
>
> **Key distinction:** CI equality answers "does 'john' match 'John'?" CI ordering answers "does 'apple' come before 'Banana'?" The semantics are fundamentally different because ordering depends entirely on collation rules, and different locales give different results.

---

## 1. SQL Collation Systems

### PostgreSQL

PostgreSQL's collation system is the **clearest documentation** that CI ordering is a first-class, well-established concept in database systems. From the PostgreSQL 18 docs:

> "The collation feature allows specifying the sort order and character classification behavior of data per-column, or even per-operation."

> "When the database system has to perform an ordering or a character classification, it uses the collation of the input expression. This happens, for example, with `ORDER BY` clauses and **function or operator calls such as `<`**."

This is definitive: `COLLATE` affects **all** comparison operators (`<`, `>`, `<=`, `>=`) in WHERE clauses, not just `ORDER BY`. Concrete examples:

```sql
-- Per-operation collation on comparison operators
SELECT * FROM test WHERE name < 'John' COLLATE "de_DE";
SELECT * FROM test WHERE name >= ('foo' COLLATE "fr_FR");
SELECT * FROM test WHERE a < b COLLATE "und-x-icu";
```

ICU collations provide explicit control levels: `ignore_accent_case`, `upper_first`, `num_ignore_punct`. At `level3`/`level4` settings, `'Å' = 'A'` can even be true — and this applies to `<`, `>`, `<=`, `>=` too.

ICU collation settings include `ignoreCase` at multiple strength levels:
- `level1`: base letter only (e.g., `'a' = 'A'`, `'b' = 'B'`)
- `level2`: accent-insensitive
- `level3`: case-insensitive
- `level4`: secondary+tertiary (used for dictionary-order sorting)

### MySQL

MySQL's default collation is `utf8mb4_0900_ai_ci` — **accent-insensitive, case-insensitive**. This collation affects **all** comparisons, not just `ORDER BY`:

```sql
-- CI default: 'John' < 'alice' is FALSE (j > a)
SELECT * FROM users WHERE name < 'm';

-- Override to binary (CS):
SELECT * FROM users WHERE name < 'm' COLLATE utf8mb4_0900_bin;

-- Override to CS with accent-sensitivity:
SELECT * FROM users WHERE name >= 'm' COLLATE utf8mb4_bin;
```

The `_ci` suffix explicitly means "case-insensitive" in the collation name — it's not just about sorting. `WHERE name < 'value'` with a CI collation column performs case-insensitive ordering comparison.

### SQL Server

SQL Server's `COLLATE` keyword works on expressions in WHERE clauses and comparison operators:

```sql
-- CI comparison in WHERE clause
SELECT * FROM products WHERE name < 'Z' COLLATE SQL_Latin1_General_CP1_CI_AS;

-- CS override in same database
SELECT * FROM products WHERE name > 'a' COLLATE SQL_Latin1_General_CP1_CS_AS;
```

The `CI` (case-insensitive) and `CS` (case-sensitive) suffixes on collation names directly control the behavior of **all** comparison operators (`<`, `>`, `<=`, `>=`, `=`), not just `ORDER BY`. Collation defines the character ordering — and `<`, `>` are ordering operators by definition.

### Oracle

Oracle's `NLS_COMP` and `NLS_SORT` parameters change how string comparison operators work in WHERE clauses:

```sql
-- Session-level configuration
ALTER SESSION SET NLS_COMP = LINGUISTIC;
ALTER SESSION SET NLS_SORT = LINGUISTIC;
-- Now WHERE name < 'm' uses linguistic (locale-aware) comparison

-- Per-query configuration
SELECT * FROM products WHERE name < 'm'
  COLLATE BINARY_AI;  -- accent- and case-insensitive
```

Oracle's `NLS_COMP` parameter specifically says "controls how string comparisons are performed" including **all comparison operators** in WHERE clauses.

### SQL Verdict

**SQL is the only category where CI ordering is a first-class feature.** But it's important to understand what SQL is actually doing:

- SQL's `<`, `>`, `<=`, `>=` operators use collation because collation defines **character ordering** (which character comes "before" another in a locale's dictionary order).
- This is not a "CI comparison" feature per se — it's **locale-aware ordering** that happens to include case-insensitivity as one dimension.
- The same collation also controls `ORDER BY`, `GROUP BY`, `LIKE`, `DISTINCT`, and more.
- SQL users don't typically think "I need CI ordering" — they think "I need this column to use the right collation" and all operations (including `<`, `>`) follow.

### Real-world SQL ordering scenarios

1. **Inventory/supply chain**: "Find parts where the part code is less than `M100`" — `WHERE part_code < 'M100' COLLATE Latin1_General_CI_AS`. Part codes may be stored as strings with mixed-case prefixes.
2. **Legal/medical records**: "Find records filed before `Z` in the first letter" — `WHERE file_number < 'Z' COLLATE Latin1_General_CI_AS` to match 'z' and 'Z' alike.
3. **Logistics**: "Find shipments where the routing code sorts before `C`" — `WHERE routing_code < 'C' COLLATE ..._CI`.
4. **General filtering**: In any system where users filter lists, a "less than X" filter should match both 'X' and 'x'.

---

## 2. No-Code / Low-Code Platforms

None of the six major platforms surveyed support CI string ordering. Here are the details:

| Platform | CI Ordering? | Available String Ops | Notes |
|----------|-------------|---------------------|-------|
| **Airtable** | ❌ No | `IF`, `FINDI()` (CI substring), text concatenation | `<`, `>`, `<=`, `>=` on text use codepoint-based comparison. No `LOWER()`/`UPPER()` functions available. Formula language has no string normalization. |
| **Notion** | Partial | `toLowerCase()`, `toUpperCase()` | `toLowerCase(a) < toLowerCase(b)` is **possible** but requires user-applied normalization. No built-in CI operator. Formula system: `"apple" < "Banana"` → TRUE (codepoint). |
| **Glide** | ❌ No | equals, contains, startsWith filters only | No `<`, `>` operators on strings in filter views at all. |
| **Bubble** | ❌ No | equals, starts with, ends with, contains | No `<`, `>` operators on strings in conditionals. |
| **Retool** | ❌ No | Query-builder filter toggles | Some filters have case sensitivity toggles, but no direct CI ordering operators. Relies on SQL queries for CI ordering (using database collation). |
| **Power Automate** | ❌ No | equals, starts with, ends with, contains | No `<`, `>` operators on strings in condition actions at all. |

### Notion detail (the closest match)

Notion's formula system has `toLowerCase()` and `toUpperCase()`, so a workaround exists:

```
prop("Status") < toLowerCase("ACTIVE")
```

This is function-based normalization — the user must manually normalize both sides. There is no built-in CI operator or modifier.

### No-Code Verdict

**Zero of six platforms support built-in CI ordering.** Four platforms don't even offer `<`, `>` on strings at all. Two platforms (Airtable, Notion) offer codepoint-based ordering. Only Notion provides any path to CI ordering, and it requires explicit user normalization via `toLowerCase()`.

The no-code/low-code platforms' approach is revealing: they either don't offer string ordering at all (4/6), or they use codepoint ordering with an explicit normalization function when users need case normalization (2/6).

---

## 3. Business Rules Engines

All five engines surveyed require workarounds for CI string ordering. None provide dedicated CI ordering operators.

| Engine | CI Ordering? | String Ops | CI Workaround |
|--------|-------------|-----------|---------------|
| **Drools (DRL)** | ❌ No | `==`, `!=`, `<`, `<=`, `>`, `>=` (all CS) | `name matches "(?i)john"` (regex with `(?i)` flag), or `str.equalsIgnoreCase()` via Java method call |
| **NRules (.NET)** | ❌ No | C# expressions | `string.Compare(x, y, StringComparison.OrdinalIgnoreCase)` |
| **Camunda / DMN / FEEL** | ❌ No | `=`, `!=`, `<`, `<=`, `>`, `>=` (all CS) | `lower case(a) < lower case(b)` — function wrapping. `matches(x, "pattern", "i")` for regex with flag. |
| **Azure Logic Apps** | ❌ No | `equals()`, `contains()`, `startsWith()`, `endsWith()` only | No `<`, `>` string operators at all |
| **Flowable** | ❌ No | MVEL/JUEL expressions | Host-language method calls: `str.equalsIgnoreCase()`, `str.compareTo()` |

### Drools detail

Drools DRL supports all six relational operators on strings, but they are **all case-sensitive**:

```
// Case-sensitive ordering — 'Banana' < 'apple' is TRUE
rule "Sort order check"
  when
    $item : Item(name < "m")
  then
    // matches "Apple" but not "banana"
end
```

For CI comparison, the only options are:
- Java method calls: `str.equalsIgnoreCase("John")` (equality only, not ordering)
- Regex with `(?i)` flag: `name matches "(?i)[a-m]"`

### FEEL detail (Camunda/DMN)

FEEL's comparison operators `<`, `<=`, `>`, `>=` are case-sensitive for strings:

```
// Case-sensitive: 'Banana' < 'apple' is TRUE (B=66, a=97)
'Apple' < 'banana'  -- true

// CI workaround requires function wrapping:
lower case('Apple') < lower case('banana')  -- true
```

### Rules Engine Verdict

**Zero business rules engines support built-in CI ordering.** The universal pattern is:
1. All string comparison operators are case-sensitive (codepoint-based).
2. CI equality uses either regex with `(?i)` flag or host-language method calls.
3. CI ordering, when needed, uses function-based normalization: `lower(x) < lower(y)`.

---

## 4. Spreadsheet / Formula Systems

| Platform | CI Ordering? | Default String Comparison | Sort Behavior |
|----------|-------------|--------------------------|---------------|
| **Excel** | ❌ No | `=` is CI; `<`, `>`, `<=`, `>=` are codepoint-based | Sort is case-insensitive by default (checkbox to make CS) |
| **Google Sheets** | ❌ No | Same as Excel | Same as Excel |
| **LibreOffice Calc** | ❌ No | Same as Excel | Checkbox for "Case-sensitive sort" — affects sort, not formula operators |
| **Airtable** | ❌ No | Codepoint-based; no `LOWER()` function | Sort can be alphabetical or codepoint |

### Excel detail

Excel's formula language has a fascinating split:

- **Equality (`=`) is case-insensitive**: `"ABC" = "abc"` → `TRUE`
- **Ordering (`<`, `>`, `<=`, `>=`) is codepoint-based**: `'A' < 'a'` → `TRUE` (because 65 < 97)
- **Sort** is case-insensitive by default, but this is a UI concern, not a formula concern

```excel
=IF(A1="ABC", "matched")        -- matches "abc" (CI equality)
=IF(A1<"M", "less than M")      -- codepoint-based: 'A' < 'M' but 'a' > 'M'
```

This split is significant: Excel deliberately treats equality as case-insensitive but ordering as codepoint-based. `'Banana' < 'apple'` in Excel evaluates to `TRUE` because 'B'(66) < 'a'(97) in ASCII.

### Google Sheets

Identical behavior to Excel. `=A1 < B1` for text uses codepoint ordering. No `LOWER()` comparison option in filter dialogs.

### Spreadsheet Verdict

**Zero spreadsheet systems support CI ordering.** Excel's formula language actually provides a negative precedent: it makes `=` case-insensitive but keeps `<`, `>`, `<=`, `>=` as codepoint-based ordering. This is a deliberate design choice.

The sort behavior (case-insensitive by default) is a UI/display concern — not a formula operator concern. This is the key distinction: **sort order for display is not the same as business rules ordering**.

---

## 5. Programming Languages

This category is the **only one with explicit CI ordering support** — but it's provided via **function/method calls**, not operators.

| Language | Default `< >` | CI Ordering Mechanism | Notes |
|----------|--------------|----------------------|-------|
| **Python** | CS (codepoint) | `locale.strcoll(x, y)` | Uses locale collation. Also `locale.strxfrm(x)` for pre-transformation. Rarely used in practice. |
| **JavaScript** | CS (codepoint) | `localeCompare(x, locale, {sensitivity: 'base'})` | Returns -1/0/1 for ordering. Used for CI sorting. |
| **C#** | CS (codepoint via `==`) | `string.Compare(x, y, StringComparison.OrdinalIgnoreCase)` | Returns -1/0/1. Also `string.Compare(x, y, CultureInfo.CurrentCulture)` for culture-aware. |
| **Java** | CS via `compareTo()` | `compareToIgnoreCase()`, `Collator.compare()` | `Collator.setStrength(Collator.PRIMARY)` → case+accent insensitive. |
| **PHP** | CS (byte-value) | `strcasecmp(x, y)`, `strnatcasecmp(x, y)` | Returns -1/0/1. `strnatcasecmp()` for natural ordering. |
| **Ruby** | CS (`<=>`) | `casecmp()`, `casecmp?()` | Returns -1/0/1 for ordering. `casecmp?()` for boolean. |
| **PowerShell** | **CI by default** | `-ilt`, `-igt`, `-ile`, `-ige` | **Sole language with CI as default.** Explicit `-c` prefix for CS variants. |

### PowerShell — The Sole Exception

PowerShell is the **only programming language** with built-in CI ordering operators:

```powershell
# CI ordering operators (default behavior, -i prefix)
"apple" -ilt "banana"    # true
"Apple" -igt "Zebra"     # true (A > Z is false, so not true... wait: 'A' < 'Z', so 'Apple' -igt 'Zebra' is false)
"Zebra" -ile "apple"     # false
"banana" -ige "apple"    # true

# CS operators (explicit -c prefix)
"apple" -clt "banana"    # true
"Apple" -cgt "Zebra"     # false (A < Z)
```

The full operator family includes 14+ variants: `-ilt`, `-igt`, `-ile`, `-ige`, `-clt`, `-cgt`, `-cle`, `-cge` for ordering; `-ieq`, `-ceq` for equality; `-ine`, `-cne` for inequality; `-ilike`, `-clike` for wildcard; `-imatch`, `-cmatch` for regex.

**Why PowerShell has CI by default:** PowerShell's design philosophy is that end users (not programmers) are the primary audience, and end users expect case-insensitive behavior. This is a domain-specific design choice that does not generalize to other languages.

### Language Verdict

**All major languages except PowerShell use codepoint-based ordering by default.** Every language except PowerShell provides CI ordering via function/method calls — not operators:

- C#: `string.Compare(x, y, OrdinalIgnoreCase)`
- Java: `String.compareToIgnoreCase()`
- Ruby: `casecmp()`
- PHP: `strcasecmp()`
- JavaScript: `localeCompare(x, locale, {sensitivity: 'base'})`
- Python: `locale.strcoll(x, y)`

None of these languages have CI ordering as the **default** for `<`/`>` operators. PowerShell is the sole exception, and its justification (end-user audience) doesn't apply to a business-rule DSL.

---

## 6. Policy / Expression Languages (Additional Survey)

| System | CI Ordering? | CI Mechanism |
|--------|-------------|-------------|
| **OPA / Rego** | ❌ No | `lower(x) < lower(y)` — function wrapping |
| **OData 4.01** | ❌ No | `tolower(Name) lt 'm'` — function wrapping |
| **XPath / XQuery** | ❌ No | `lower-case($name) < 'm'` — function wrapping |
| **FEEL (DMN)** | ❌ No | `lower case(a) < lower case(b)` — function wrapping |
| **SpEL** | ❌ No | `'hello'.equalsIgnoreCase()` — Java method call |
| **Cedar** | ❌ No | No CI mechanism at all (deliberate) |
| **MVEL** | ❌ No | `str.equalsIgnoreCase()` — host-language method |

Every policy and expression language surveyed uses the same pattern: function-based normalization. The operators (`<`, `>`, `<=`, `>=`) remain codepoint-based.

---

## 7. Critical Synthesis

### Summary Table

| Category | CI Ordering Support | Mechanism |
|----------|-------------------|-----------|
| **SQL (PostgreSQL)** | ✅ Yes | `COLLATE` on all comparison operators |
| **SQL (MySQL)** | ✅ Yes | Default CI collation affects all ops |
| **SQL (SQL Server)** | ✅ Yes | `COLLATE` on expressions |
| **SQL (Oracle)** | ✅ Yes | `NLS_COMP` + `NLS_SORT` |
| **No-Code (6 platforms)** | ❌ 0/6 | Codepoint-based or no string ops |
| **Rules Engines (5)** | ❌ 0/5 | Host-language methods or `lower(x) < lower(y)` |
| **Spreadsheets (4)** | ❌ 0/4 | Codepoint-based; Excel equality is CI but ordering is not |
| **Programming Languages (7)** | ❌ 6/7 | Function calls only; PowerShell is the sole exception |
| **Policy/Expression (7)** | ❌ 0/7 | Function-based normalization |

**Bottom line: 6 of 7 categories have ZERO built-in CI ordering support.**

### What SQL Is Actually Doing

SQL's collation-based CI ordering is a side effect of **general-purpose locale-aware character ordering**, not a dedicated CI comparison feature. When PostgreSQL says "collation affects `<` and `>`", it means:

> "The collation defines the total ordering of characters according to this locale's rules. Since `<` and `>` are total-ordering operators, they use this ordering."

Case-insensitivity is just one dimension of the collation (along with accent-insensitivity, punctuation-insensitivity, dictionary order, etc.). SQL doesn't have "CI comparison" as a standalone concept — it has "collation" which controls **all** string behavior.

### The Business-Logic Use Case

The key question for Precept: **Is there a genuine business need for CI ordering?**

**The only plausible business scenario** is: "List items alphabetically, with 'apple' coming before 'Banana'." But this is a **display/concern** issue, not a **business rule** issue:

- **Display**: Sort a list of SKUs alphabetically for presentation → presentation layer concern (SQL `ORDER BY`, UI sort, etc.)
- **Business rule**: "Shipment priority code must be less than Z" → This is a constraint on data values. If the domain expert means "less than z or Z," they would express that as an explicit set membership (`priority in ('A'..'Z')`) or a regex match, not as a CI comparison.

### Counterexamples Found

1. **SQL collation systems** — PostgreSQL, MySQL, SQL Server, Oracle all apply collation to `<`, `>`, `<=`, `>=` operators. This is the strongest precedent. However, it's a general-purpose locale feature, not a business-DSL feature.

2. **PowerShell** — Explicit CI ordering operators (`-ilt`, `-igt`, `-ile`, `-ige`). Justified by end-user audience, which doesn't apply to Precept's domain-expert audience.

3. **Programming language libraries** — `strcasecmp()` (C/PHP), `casecmp()` (Ruby), `compareToIgnoreCase()` (Java), `localeCompare()` (JavaScript), `string.Compare()` (C#). These are **function-based**, not operator-based, and are used in imperative code, not DSL expressions.

4. **PowerShell's broader pattern** — The `-i`/`-c` prefix pattern extends to **all** string operators (14+ variants). This is a complete cascade, not a single isolated operator.

### The Cascade Problem (Revisited)

If Precept adds CI ordering (`<`, `>`, `<=`, `>=`), users will immediately expect:

1. CI not-equal (`!=`)? — Already asked for CI equality, so CI inequality is inevitable.
2. CI `contains`? — Extremely common validation pattern.
3. CI `startsWith` / `endsWith`? — Equally common.
4. CI `matches` / regex? — Domain experts want CI pattern matching.

PowerShell's experience (30+ operator variants) shows this cascade is real. Every expression language that avoided this used **function-based normalization** — one function (`lower`/`tolower`) that composes with all operators.

### Function-Based Normalization: The Universal Solution

Every system that supports CI ordering at all provides it through one of these patterns:

```
# PostgreSQL / SQL Server / MySQL
WHERE name < 'Z' COLLATE Latin1_General_CI_AS

# OData / FEEL / XPath / OPA / Precept (already has toLower/toUpper)
tolower(name) < tolower('Z')

# Excel workaround (no LOWER function in formulas, but conceptually the same)
# Not possible in Excel formulas — highlights that CI ordering in formulas is rare

# PowerShell (the exception)
"apple" -ilt "banana"

# Programming languages (function calls)
strcasecmp('apple', 'banana')  # PHP/C
String.compareToIgnoreCase("apple", "banana")  # Java
string.Compare("apple", "banana", OrdinalIgnoreCase)  # C#
```

Precept already has `toLower` and `toUpper` built-in functions. The CI ordering idiom `toLower(x) < toLower(y)` **already works**. This is exactly how OData, FEEL, XPath, and OPA handle it.

---

## 8. Verdict for Precept

### There is no genuine business need for built-in CI ordering operators.

**Evidence:**

1. **5 of 7 categories surveyed have zero CI ordering support.** SQL is the sole database category, and even there it's a side effect of general-purpose locale collation, not a dedicated CI feature.

2. **No no-code/low-code platform offers CI ordering.** 4/6 don't even offer `<`, `>` on strings. Of the 2 that do, 0 offer CI variants.

3. **No business rules engine offers CI ordering.** All 5 surveyed require workarounds (host-language methods, function wrapping).

4. **No spreadsheet formula language offers CI ordering.** Excel deliberately splits equality (CI) from ordering (codepoint-based). This is a deliberate design decision.

5. **PowerShell is the sole exception** — and it's justified by its end-user audience, which doesn't apply to Precept's domain-expert audience.

6. **No programming language has CI ordering as the default** for `<`, `>` operators. All 6 non-PowerShell languages use codepoint-based ordering by default, providing CI comparison via function calls.

7. **No policy/expression language offers CI ordering.** All 7 use function-based normalization.

8. **The only plausible business use case** (alphabetical display) belongs in the presentation layer, not in business rules.

9. **Precept already has the solution:** `toLower(x) < toLower(y)` works today. This matches the dominant industry approach.

### The cascade risk

Adding CI ordering operators (`<`, `>`, `<=`, `>=`) would be a more significant surface expansion than adding `~=` for CI equality:

- CI equality: 1 new operator (`~=`) or 1 new function (`equalsIgnoreCase`).
- CI ordering: 4 new operators (`~<`, `~>`, `~<=`, `~>=`) — plus immediate cascade to `~!=`, CI `contains`, CI `startsWith`, CI `endsWith`.

### Recommendation

**Do not add built-in CI ordering operators.** The function-based normalization (`toLower(x) < toLower(y)`) already works, matches the dominant industry approach, and avoids both the cascade problem and the semantic ambiguity of collation-dependent ordering.

If domain experts express a genuine need for CI ordering in real usage (not hypothetical scenarios), the most ergonomic path forward is:

- **Option A (preferred):** Add `collate` keyword modifier (future consideration): `toLower(x) < toLower(y) collate`
- **Option B:** Add `compareIgnoreCase(x, y)` function returning -1/0/1, composable with `==`, `<`, `>`.
- **Option C:** Keep function wrapping as-is — it already works and matches OData, FEEL, XPath, and OPA.

---

## 9. Sources

- PostgreSQL 18 Documentation §23.2 Collation Support — https://www.postgresql.org/docs/current/collation.html
- PostgreSQL 18 Documentation §23.2.2 Managing Collations — https://www.postgresql.org/docs/current/sql-createcollation.html
- MySQL 8.4 Reference Manual: Case-Insensitivity — https://dev.mysql.com/doc/refman/8.4/en/case-insensitivity.html
- SQL Server: COLLATE — https://learn.microsoft.com/en-us/sql/t-sql/statements/collation-and-character-set-support
- Oracle Database: NLS Parameters — https://docs.oracle.com/en/database/oracle/oracle-database/23/nlspg/index.html
- PowerShell about Comparison Operators: -lt, -gt, -le, -ge — https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_comparison_operators
- Lua 5.4 Reference Manual §3.4.4 — https://www.lua.org/manual/5.4/manual.html
- PowerShell `-ilt` / `-igt` / `-ile` / `-ige` operator family
- Drools DRL Rule Language Reference
- Camunda DMN / FEEL Handbook — https://kiegroup.github.io/dmn-feel-handbook/
- OData 4.01 URL Conventions §5.1.1 — https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.html
- Precept Case-Insensitive Comparison Survey — `research/language/expressiveness/case-insensitive-comparison-survey.md`
- Precept Case-Insensitive Implementation Survey — `research/language/expressiveness/case-insensitive-implementation-survey.md`
