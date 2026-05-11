# Case-Insensitive Comparison: Implementation Survey

> **Context**: Issue #16 (built-in functions) — evaluating whether Precept should add a `~=` operator for case-insensitive string comparison. This survey documents how other constraint, rule, and expression systems handle case-insensitive comparison, what the tilde symbol means across languages, and what cascade implications a prefix-modifier operator would introduce.

---

## 1. Rule Engines

### Drools DRL (JBoss / Red Hat)

- **No dedicated case-insensitive operator.**
- Standard relational operators: `==` (uses `equals()`), `!=` (uses `!equals()`), `<`, `<=`, `>`, `>=`.
- `matches` / `not matches` — Java regex pattern matching. Case-insensitive comparison achievable via `(?i)` inline flag: `name matches "(?i)john"`.
- `soundslike` — Soundex-based phonetic matching (fuzzy, not CI).
- `str[startsWith]`, `str[endsWith]`, `str[length]` — bracket-parameter string operators, all case-sensitive.
- `contains` / `not contains` — collection membership AND string substring check, case-sensitive.
- `in` / `notin` — value in a list, case-sensitive.
- MVEL expression dialect available but adds no CI operator.
- **Pattern**: Regex with `(?i)` flag, or method call to Java's `equalsIgnoreCase()`.

### NRules (.NET)

- **No dedicated case-insensitive operator.**
- Rules are authored as C# classes with lambda-expression conditions: `.Match(() => quote, q => q.Driver.Name == "John")`.
- Since conditions are native C# expressions, CI comparison uses `string.Equals(other, StringComparison.OrdinalIgnoreCase)` or `string.Compare()`.
- **Pattern**: Defers entirely to the host language (C#).

### InRule (Commercial .NET)

- Commercial product with closed documentation.
- Rule authoring uses .NET expression syntax.
- Expected to follow the same pattern as NRules: defers to .NET string comparison APIs.

### Easy Rules (Java)

- Minimal Java rule engine with MVEL expression support.
- No dedicated CI operator. Uses MVEL/Java method calls for CI comparison.
- **Pattern**: Host language method calls.

---

## 2. Validation Frameworks

### FluentValidation (.NET)

- **No dedicated case-insensitive operator.**
- `.Equal()` / `.NotEqual()` accept a `StringComparer` parameter: `.Equal("expected", StringComparer.OrdinalIgnoreCase)`.
- `.IsEnumName(caseSensitive: false)` — boolean parameter for CI.
- `.Matches()` — accepts regex (which can include `(?i)` flag).
- **Pattern**: Parameter-based CI — the comparison method takes an explicit comparer or boolean flag.

### Zod (TypeScript)

- **No case-insensitive string comparison.**
- String validations (all case-sensitive): `.regex()`, `.includes()`, `.startsWith()`, `.endsWith()`.
- String transforms: `.toLowerCase()`, `.toUpperCase()`, `.trim()`.
- `z.stringbool()` has `case: "sensitive"` option (defaults to CI for boolean parsing).
- **Pattern**: Transform-then-validate — normalize input via `.toLowerCase()`, then compare.

### Valibot (TypeScript)

- Similar architecture to Zod. String validations are case-sensitive.
- CI comparison achieved through `transform()` pipeline step followed by validation.
- **Pattern**: Transform-then-validate, same as Zod.

### JSON Schema

- `pattern` keyword uses ECMA 262 (JavaScript) regex syntax.
- **No support for regex flags in the `pattern` keyword itself** — cannot specify `(?i)` inline in a portable way.
- Recommended to "stick to the subset" of ECMA 262. Inline modifiers like `(?i)` are not widely supported across JSON Schema implementations.
- Must use verbose character classes like `[jJ][oO][hH][nN]` for CI matching.
- **Pattern**: Regex-only, no flags, no CI support in practice.

---

## 3. Policy Languages

### Cedar (AWS)

- **No case-insensitive comparison at all.**
- `"Something" == "something"` evaluates to `false`.
- `like` operator exists for wildcard matching only (`*` as wildcard), case-sensitive.
- No `toLowerCase()` / `toUpperCase()` functions exist.
- No string manipulation functions of any kind.
- **Pattern**: Deliberate omission — Cedar's security model avoids ambiguity from locale-dependent case folding.

### OPA / Rego (Open Policy Agent)

- **No dedicated case-insensitive operator.**
- Standard operators only: `==`, `!=`, `<`, `>`, `<=`, `>=`.
- Built-in string functions: `lower()`, `upper()`, `contains()`, `startswith()`, `endswith()`, `trim()`, `sprintf()`.
- CI comparison idiom: `lower(input.name) == lower("John")`.
- **Pattern**: Function-based normalization — `lower()` + standard `==`.

### Casbin (Apache)

- **No dedicated case-insensitive operator.**
- Matcher expressions use standard operators: `r.sub == p.sub && r.obj == p.obj`.
- Built-in functions for pattern matching: `keyMatch()`, `regexMatch()`, `globMatch()`.
- CI comparison via `regexMatch()` with `(?i)` flag or custom function.
- **Pattern**: Built-in function with regex, or custom function registration.

---

## 4. Expression Languages

### SpEL (Spring Expression Language)

- **No dedicated case-insensitive operator.**
- Relational operators: `==`, `!=`, `<`, `<=`, `>`, `>=` (also textual aliases: `eq`, `ne`, `lt`, `gt`, `le`, `ge` — these keyword aliases are case-insensitive as tokens, but do NOT perform case-insensitive comparison).
- `matches` operator — Java regex pattern matching. CI via `(?i)` flag: `'hello' matches '(?i)HELLO'`.
- `between` operator — range check, case-sensitive for strings.
- String operators: `+` (concatenation), `-` (char subtraction), `*` (repeat). No CI operator.
- `OperatorOverloader` mechanism exists but only for math operations (`ADD`, `SUBTRACT`, `MULTIPLY`, `DIVIDE`, `MODULUS`, `POWER`).
- CI comparison in practice: call Java method `'hello'.equalsIgnoreCase('HELLO')`.
- **Pattern**: `matches` with regex `(?i)` flag, or host-language method call.

### FEEL (DMN / Camunda)

- **No dedicated case-insensitive operator.**
- Standard comparison: `=`, `!=`, `<`, `<=`, `>`, `>=` — all case-sensitive.
- String functions: `upper case(string)`, `lower case(string)`, `contains(string, match)`, `starts with(string, match)`, `ends with(string, match)`, `substring()`, `string length()`, `trim()`.
- `matches(input, pattern, flags)` — regex with **explicit `"i"` flag** for case-insensitive matching: `matches("FooBar", "foo", "i")` → `true`.
- `replace(input, pattern, replacement, flags)` — also supports `"i"` flag.
- CI equality idiom: `lower case(a) = lower case(b)`.
- **Pattern**: Function-based normalization (`lower case()` + `=`) or regex with flag parameter (`matches(x, y, "i")`).

### MVEL (Standalone)

- Java-based expression language used in Drools and Easy Rules.
- Supports direct Java method calls: `name.equalsIgnoreCase("John")`.
- No dedicated CI operator in the expression syntax itself.
- **Pattern**: Host-language method call.

### JEXL (Apache Commons)

- Java-based expression language.
- Standard operators only. Supports method calls on Java objects.
- CI comparison via `str.equalsIgnoreCase(other)`.
- **Pattern**: Host-language method call.

---

## 5. Tilde (`~`) Semantics Across Languages

The tilde symbol carries **widely divergent meanings** across programming languages. The proposed `~=` operator for Precept would enter a crowded semantic space:

| Language/Context | Operator | Meaning |
|---|---|---|
| **Lua** | `~=` | **Inequality (NOT EQUAL)** — core relational operator |
| **Lua** | `~` (unary) | Bitwise NOT |
| **Lua** | `~` (binary) | Bitwise XOR |
| **C / C++ / C# / Java / Python / JavaScript / PHP** | `~` | Bitwise NOT (unary complement) |
| **AWK** | `~`, `!~` | Regex match / regex not-match |
| **Perl** | `=~`, `!~` | Regex binding operator |
| **Groovy** | `=~`, `==~` | Regex find / regex match |
| **PostgreSQL** | `~`, `~*`, `!~`, `!~*` | POSIX regex match (CI variant: `~*`) |
| **Eiffel** | `~` | Object equality (deep, field-by-field `is_equal`) |
| **D** | `~` | Concatenation operator |
| **R** | `~` | Model formula separator (LHS ~ RHS) |
| **Raku** | `~~` | Smartmatch operator |
| **Raku** | `~` | String concatenation |
| **CSS** | `~` | Subsequent-sibling combinator |
| **Haskell** | `~` | Lazy pattern match / type equality constraint |
| **APL / MATLAB** | `~` | Logical NOT |
| **Standard ML** | `~` | Prefix for negative numbers |
| **Mathematics** | `~`, `≈` | Equivalence relation / "approximately equal" |
| **Unix shells** | `~` | Home directory |

### Critical Confusability Risk: Lua

Lua — a widely-used embeddable scripting language — uses `~=` as its **inequality operator** (equivalent to `!=` in C-family languages). This is core Lua syntax used in every codebase:

```lua
if x ~= nil then    -- "if x is not equal to nil"
if status ~= "ok" then   -- "if status is not equal to 'ok'"
```

Any developer with Lua experience will read Precept's `~=` as "not equal," which is the **exact opposite** of the intended "approximately equal / case-insensitive equal" semantics.

### PostgreSQL's `~*` Precedent

PostgreSQL is the **only surveyed system** that uses tilde for case-insensitive matching, and it uses `~*` (not `~=`) specifically for CI regex matching:

```sql
SELECT 'FooBar' ~ 'foo';    -- false (CS regex match)
SELECT 'FooBar' ~* 'foo';   -- true  (CI regex match)
SELECT 'FooBar' !~* 'foo';  -- false (CI regex not-match)
```

This is regex matching, not equality comparison.

---

## 6. Cascade Analysis

If Precept adds `~=` as a case-insensitive equality operator, **users will immediately expect case-insensitive variants of every string operator**:

| Current Operator | Expected CI Variant | Cascade Pressure |
|---|---|---|
| `==` | `~=` | The initial proposal |
| `!=` | `~!=` or `!~=` | Immediate — negation of CI equality |
| `contains` | `~contains` | High — common validation pattern |
| `startsWith` | `~startsWith` | High — common validation pattern |
| `endsWith` | `~endsWith` | High — common validation pattern |

### No Precedent for Prefix-Modifier Pattern

No surveyed system uses a `~` prefix modifier to create CI variants of existing operators. The pattern `~` + `operator` = "case-insensitive version of operator" is **novel and unattested**.

### How Other Systems Handle the Cascade

Systems that need CI for multiple operations use one of these strategies:

1. **Function-based normalization** (OPA, FEEL): `lower(x) == lower(y)`, `lower(x).contains(lower(y))` — one function covers ALL operators.
2. **Parameter-based approach** (FluentValidation): Each method takes a comparer argument — `StringComparer.OrdinalIgnoreCase`.
3. **Regex with CI flag** (Drools, FEEL, SpEL, PostgreSQL): `matches(input, pattern, "i")` — a single mechanism for all pattern operations.

All three strategies avoid the cascade problem by providing a **single mechanism** that composes with existing operators rather than duplicating the operator set.

### Parser and Grammar Impact

A `~` prefix modifier would require:
- New token recognition for `~=`, `~!=`, `~contains`, `~startsWith`, `~endsWith` (each as a distinct operator).
- TextMate grammar additions for each CI operator variant.
- Completion provider entries for each variant.
- MCP documentation for each new operator.
- A parser rule for `~` + `operator` composition (or individual hard-coded recognition).

A function-based approach (`lower()`) requires: one new built-in function, no grammar changes, no new operators, no cascade.

---

## 7. Summary of Findings

### The Dominant Pattern

Across **all 16+ systems surveyed**, the universal finding is:

> **No constraint, rule, or expression language provides a dedicated case-insensitive comparison operator.**

Every system uses one of three strategies:

| Strategy | Systems | Mechanism |
|---|---|---|
| **Function normalization** | OPA/Rego, FEEL, Zod, Valibot | `lower(x) == lower(y)` |
| **Parameter / comparer** | FluentValidation, NRules, .NET ecosystem | `.Equal(x, StringComparer.OrdinalIgnoreCase)` |
| **Regex with CI flag** | Drools, FEEL, SpEL, Casbin, PostgreSQL | `matches(x, "pattern", "i")` |

Some systems (Cedar, JSON Schema) deliberately provide **no CI comparison at all**.

### `~=` Is Not Established

The `~=` token for case-insensitive comparison has **zero precedent** in any surveyed language or framework. Its only established meaning is **inequality** (Lua), making it actively confusing.

### The Tilde Confusability Risk Is Real

Lua's `~=` (NOT EQUAL) is the most direct conflict. Developers familiar with Lua, AWK (`~` = regex match), Perl (`=~` = regex bind), or C-family languages (`~` = bitwise NOT) will all bring incorrect expectations. The mathematical "approximately equal" reading (≈) is plausible but not established in any programming language's operator syntax.

### Function-Based Approach Avoids Cascade

A `lower()` function in Precept's built-in function library would:
- Provide CI comparison (`lower(name) == lower("John")`) with zero new operators.
- Compose naturally with all existing operators (`lower(x) != lower(y)`, `lower(x).contains(lower(y))` patterns).
- Avoid the cascade problem entirely.
- Follow the dominant pattern established by OPA, FEEL, Zod, and every other surveyed system.
- Require no parser, grammar, completion, or MCP changes beyond the function itself.
