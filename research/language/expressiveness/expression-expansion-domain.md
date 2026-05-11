# Expression Expansion — Domain Research

Research grounding for the Batch 2 expression-expansion effort covering five open proposals: [#9 — Conditional expressions](https://github.com/sfalik/Precept/issues/9), [#10 — String `.length`](https://github.com/sfalik/Precept/issues/10), [#15 — String `.contains()`](https://github.com/sfalik/Precept/issues/15), [#16 — Built-in function library](https://github.com/sfalik/Precept/issues/16), and [#31 — Logical keywords](https://github.com/sfalik/Precept/issues/31).

This file is durable research, not a proposal body. It captures why these five capabilities belong together, what adjacent systems do, which vocabulary and scope contracts must be explicit, and which directions look attractive but weaken Precept's semantics.

## Background and Problem

Precept's current expression surface is a many-sorted first-order decidable fragment with three base sorts (`number`, `string`, `boolean`), typed identifiers, a fixed set of arithmetic/comparison/logical operators, one membership operator (`contains` on collections), and four collection accessors (`.count`, `.min`, `.max`, `.peek`). There are no conditional expressions, no string accessors, no built-in functions, and the logical connectives use C-family symbols (`&&`, `||`, `!`) rather than English keywords.

The expression audit ([expression-language-audit.md](./expression-language-audit.md)) classifies the resulting limitations by severity:

- **Critical:** No conditional value expression (L1) — forces full row duplication when rows differ in a single field value. 14+ samples exhibit this pattern. No string `.length` (L2) — table-stakes validation entirely inexpressible. No `abs`/`min`/`max` (L5) — symmetric range constraints and capped values impossible.
- **Significant:** `contains` restricted to collections (L6) — basic substring tests inexpressible. No null-coalescing (L7) — nullable field defaults require separate guard rows.
- **Moderate:** No `any`/`all` quantifiers (L9). String ordering unsupported (L10). Division-by-zero silent (L11).

The sample corpus makes the pain concrete:

| Sample | Expression gap exhibited |
|---|---|
| `hiring-pipeline.precept` | Two rows identical except transition target — L1 conditional expression would collapse them |
| `loan-application.precept` | 93-character guard expression with no reuse mechanism — needs named rules (#8) *and* richer value expressions |
| `subscription-cancellation-retention.precept` | Cannot express tier-dependent discount without separate rows — L1 |
| `maintenance-work-order.precept` | `ActualHours <= EstimatedHours + 4` is one-sided — `abs()` (L5) needed for symmetric range |
| `travel-reimbursement.precept` | `Miles * MileageRate` without rounding — `round()` (L5) needed for production accuracy |
| All 21 samples | String fields with no length validation (L2); `&&`/`||`/`!` everywhere that `and`/`or`/`not` would read better |

Expression expansion applies equally to stateless precepts. For data-only entities, where invariants and computed fields are the entire governance surface, expression gaps directly reduce the product's ability to deliver governed integrity — the principle that an entity's data satisfies its declared rules at every moment.

### Why these five proposals belong in one research pass

Every decision in this domain changes the shape of the expression language authors see. The vocabulary surface — what counts as an operator, what counts as an accessor, what counts as a function, and what counts as a keyword — must be coherent across all five proposals. Researching them independently would produce contradictory naming conventions, inconsistent null policies, and an unpredictable vocabulary boundary. Treating them as one expression-expansion domain grounds all five decisions in the same precedent base, the same philosophy filter, and the same vocabulary taxonomy.

---

## Vocabulary Boundaries

The expression surface introduces four distinct vocabulary categories. Each has different grammar rules, different precedent families, and different extension costs. Drawing the boundary explicitly prevents vocabulary creep.

### Operators

Binary and unary symbols or keywords that participate in the expression grammar at a defined precedence level. The parser treats them as infix or prefix tokens.

| Current | Category | Example |
|---|---|---|
| `+`, `-`, `*`, `/`, `%` | Arithmetic (symbols) | `Score + Bonus` |
| `==`, `!=`, `>`, `>=`, `<`, `<=` | Comparison (symbols) | `Amount > 0` |
| `&&`, `||` | Logical (symbols → `and`, `or` per #31) | `X && Y` → `X and Y` |
| `!` | Unary logical (symbol → `not` per #31) | `!Done` → `not Done` |
| `contains` | Membership (keyword operator) | `Tags contains "urgent"` |

**Extension rule:** New operators are justified only when the operation is binary/unary, has natural precedence placement, and reads fluently inline. Operators should not carry parenthesized arguments — that is function syntax.

### Accessors

Dotted property reads on typed identifiers. The parser treats them as `Identifier.Member` atoms, not method calls. They return a scalar value.

| Current | Available on | Returns |
|---|---|---|
| `.count` | `set<T>`, `queue<T>`, `stack<T>` | `number` |
| `.min` | `set<T>` (numeric inner) | inner type |
| `.max` | `set<T>` (numeric inner) | inner type |
| `.peek` | `queue<T>`, `stack<T>` | inner type |

**Proposed addition:** `.length` on `string` → `number`. Mirrors `.count` for collections: a parameterless size read on a typed value.

**Extension rule:** Accessors are justified when the operation is a parameterless property read on a known type. They should not accept arguments — that distinguishes them from methods and functions. The accessor surface should remain small and discoverable: a handful of well-known properties per type, not an extensible method dictionary.

### Methods

Dotted calls with parenthesized arguments on typed identifiers: `Identifier.method(arg)`. This is a new grammar form not present in the current parser.

**Proposed addition:** `.contains(substring)` on `string` → `boolean`. The receiver is a string expression; the argument is a string literal.

**Extension rule:** Methods are justified when an operation is (a) scoped to a specific receiver type, (b) requires at least one argument, and (c) reads naturally as a property of the receiver. Methods should remain narrow and literal-argument-only in v1 — no cross-field method arguments, no chaining, no side effects.

**Relationship to `contains` operator:** The existing `contains` keyword is a binary infix operator (`Collection contains Value`). Proposal #15 introduces `.contains(substring)` as a *method* on strings. These are different vocabulary categories despite sharing a name. The infix `contains` tests set membership; the method `.contains()` tests substring presence. This is consistent with how C# and LINQ treat `Contains` differently on `ICollection<T>` vs `string`. The type checker disambiguates by receiver type.

### Functions

Prefix calls with parenthesized arguments: `functionName(arg1, arg2)`. This requires a new AST node (`PreceptFunctionCallExpression`) and a static function registry in the type checker.

**Proposed additions (from #16):**

| Function | Signature | Category |
|---|---|---|
| `abs(x)` | `number → number` | Numeric |
| `floor(x)` | `number → number` | Numeric |
| `ceil(x)` | `number → number` | Numeric |
| `round(x)` | `number → number` | Numeric |
| `min(a, b)` | `number × number → number` | Numeric |
| `max(a, b)` | `number × number → number` | Numeric |
| `startsWith(s, prefix)` | `string × string → boolean` | String |
| `endsWith(s, suffix)` | `string × string → boolean` | String |
| `toLower(s)` | `string → string` | String |
| `toUpper(s)` | `string → string` | String |
| `trim(s)` | `string → string` | String |

**Extension rule:** Functions are justified when an operation is (a) not scoped to a single receiver type, or (b) takes multiple arguments of different types, or (c) would read unnaturally as a method or accessor. The function registry is static, finite, and deterministic — no user-defined functions, no dynamic dispatch, no varargs.

### What stays out of scope

| Category | Examples | Why excluded |
|---|---|---|
| User-defined functions | `function isEligible(score, income)` | Lambda scoping breaks flat-identifier model; undecidable in general |
| Regular expressions | `matches("^[A-Z]{3}-\\d+$")` | Decidability risk (ReDoS with unrestricted patterns); no sample demand |
| String interpolation | `"Total: ${Amount}"` | Template syntax; not needed for constraint evaluation |
| Type conversion | `toString(x)`, `toNumber(s)` | Precept's type system is explicit; conversion blurs type boundaries |
| Collection aggregates | `sum()`, `avg()`, `median()` | Deferred — no blocking sample demand; revisit post-v1 |
| Quantified expressions | `all Tag in Tags : Tag.length > 0` | Lambda scoping; high grammar/evaluator cost; deferred |

---

## Precedent Survey

### Databases

| System | Conditional expressions | String accessors | Built-in functions | Logical keywords | Documentation |
|---|---|---|---|---|---|
| **PostgreSQL** | `CASE WHEN ... THEN ... ELSE ... END` | `length(s)`, `position(sub IN s)` | `abs()`, `round()`, `floor()`, `ceil()`, `greatest()`, `least()` | `AND`, `OR`, `NOT` | [String functions](https://www.postgresql.org/docs/current/functions-string.html), [Math functions](https://www.postgresql.org/docs/current/functions-math.html), [Conditional expressions](https://www.postgresql.org/docs/current/functions-conditional.html) |
| **SQL Server** | `CASE WHEN ... THEN ... ELSE ... END`, `IIF(cond, t, f)` | `LEN(s)`, `CHARINDEX(sub, s)` | `ABS()`, `ROUND()`, `FLOOR()`, `CEILING()` | `AND`, `OR`, `NOT` | [String functions](https://learn.microsoft.com/en-us/sql/t-sql/functions/string-functions-transact-sql), [Math functions](https://learn.microsoft.com/en-us/sql/t-sql/functions/mathematical-functions-transact-sql) |
| **MySQL** | `CASE`, `IF(cond, t, f)` | `LENGTH(s)`, `LOCATE(sub, s)` | `ABS()`, `ROUND()`, `FLOOR()`, `CEIL()`, `LEAST()`, `GREATEST()` | `AND`, `OR`, `NOT` | [String functions](https://dev.mysql.com/doc/refman/8.0/en/string-functions.html), [Control flow](https://dev.mysql.com/doc/refman/8.0/en/control-flow-functions.html) |
| **SQLite** | `CASE WHEN ... THEN ... ELSE ... END`, `IIF(cond, t, f)` | `length(s)`, `instr(s, sub)` | `abs()`, `round()`, `min()`, `max()` | `AND`, `OR`, `NOT` | [Core functions](https://www.sqlite.org/lang_corefunc.html), [Expression syntax](https://www.sqlite.org/lang_expr.html) |

**Pattern:** Every SQL dialect uses keyword logical operators (`AND`, `OR`, `NOT`), keyword-form conditional expressions (`CASE`/`IF`), built-in math and string functions with `name(args)` call syntax, and string length as a core function. SQL has no accessor syntax (`.length`); everything is a function call.

**Precept implication:** SQL's universal `AND`/`OR`/`NOT` keyword convention validates #31. SQL's `CASE WHEN...THEN...ELSE...END` is more verbose than `if...then...else` but establishes that keyword-form conditionals are standard in declarative languages. The function-call convention (`abs(x)`) validates #16's syntax, but Precept should keep its accessor pattern (`.length`) for type-scoped parameterless reads rather than converting everything to function calls.

### Languages

| System | Conditional expressions | String accessors | Built-in functions | Logical keywords | Documentation |
|---|---|---|---|---|---|
| **Python** | `value_if_true if condition else value_if_false` | `len(s)`, `s.startswith()`, `s.endswith()`, `"sub" in s` | `abs()`, `min()`, `max()`, `round()` | `and`, `or`, `not` | [Built-in functions](https://docs.python.org/3/library/functions.html), [String methods](https://docs.python.org/3/library/stdtypes.html#string-methods) |
| **C#** | `condition ? valueTrue : valueFalse` | `s.Length`, `s.Contains(sub)`, `s.StartsWith(p)` | `Math.Abs()`, `Math.Min()`, `Math.Max()`, `Math.Round()` | `&&`, `||`, `!` (symbols) | [String class](https://learn.microsoft.com/en-us/dotnet/api/system.string), [Math class](https://learn.microsoft.com/en-us/dotnet/api/system.math) |
| **Kotlin** | `if (cond) a else b` (expression form) | `s.length`, `s.contains(sub)`, `s.startsWith(p)` | `abs()`, `minOf()`, `maxOf()` | `&&`, `||`, `!` (symbols) | [String class](https://kotlinlang.org/api/core/kotlin/-string/), [Math](https://kotlinlang.org/api/core/kotlin.math/) |
| **TypeScript** | `condition ? a : b` | `s.length`, `s.includes(sub)`, `s.startsWith(p)` | `Math.abs()`, `Math.min()`, `Math.max()`, `Math.round()` | `&&`, `||`, `!` (symbols) | [String](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String) |

**Pattern:** General-purpose languages split between keyword logical operators (Python) and symbolic (C#, TS, Kotlin). All provide conditional expressions, string length as a property or function, substring tests, and bounded math functions.

**Precept implication:** Python's `and`/`or`/`not` keywords validate #31 — Precept's keyword choice puts it in the same family as the most widely-taught programming language. Kotlin's `if...else` as an expression (not a statement) validates #9's keyword form. C#'s `.Length` property validates the accessor pattern for #10.

### Rule and Decision Engines

| System | Conditional expressions | String tests | Built-in functions | Logical keywords | Documentation |
|---|---|---|---|---|---|
| **FEEL (DMN)** | `if condition then value else value` | `string length(s)`, `contains(s, sub)`, `starts with(s, p)`, `ends with(s, p)` | `abs(n)`, `floor(n)`, `ceiling(n)`, `round up/down/half up(n, scale)`, `min(...)`, `max(...)` | `and`, `or`, `not()` | [FEEL spec (OMG DMN)](https://www.omg.org/spec/DMN/1.4), [Camunda FEEL reference](https://docs.camunda.io/docs/components/modeler/feel/language-guide/feel-expressions-introduction/) |
| **Drools DRL** | `eval()` with Java ternary; no native conditional expression in LHS | Java `.length()`, `.contains()`, `.startsWith()` | Java `Math.abs()`, `Math.min()`, `Math.max()` via `eval()` | `&&`, `||`, `not` pattern | [Drools docs](https://docs.drools.org/latest/drools-docs/drools/language-reference/) |
| **Cedar (AWS)** | No conditional expressions — policies evaluate to `permit`/`forbid` | `.contains(s)`, `ip()`, `decimal()` extension functions | **Deliberately minimal.** No math functions. No `abs`, no `round`. | `&&`, `||`, `!` (symbols) | [Cedar language spec](https://docs.cedarpolicy.com/policies/syntax-grammar.html), [Cedar operators](https://docs.cedarpolicy.com/policies/syntax-operators.html) |
| **CEL (Google)** | `condition ? trueValue : falseValue` (ternary) | `string.contains(sub)`, `string.startsWith(p)`, `string.endsWith(s)`, `string.matches(re)` (RE2 regex), `size(string)` | **No math functions in standard library.** No `abs`, no `round`, no `min`, no `max`. Extension functions can add them per deployment. | `&&`, `||`, `!` (symbols, **commutative** — non-deterministic evaluation order) | [CEL language spec](https://github.com/google/cel-spec/blob/master/doc/langdef.md), [CEL overview](https://cel.dev/overview/cel-overview) |
| **NRules (.NET)** | C# ternary in rule actions; no native conditional | C# string methods via lambda expressions | C# `Math` class via lambdas | C# symbols within lambdas | [NRules wiki](https://github.com/NRules/NRules/wiki) |

**Pattern:** FEEL is the strongest comparator: keyword-form `if...then...else`, keyword logical operators (`and`, `or`, `not`), a comprehensive built-in function library including string operations and math. Cedar is the strongest *counter-precedent*: it deliberately omits math functions and conditional expressions to maintain formal analyzability. CEL occupies a middle position: it has the ternary conditional and string operations, but deliberately omits math functions from the standard library — these are added per-deployment via extension functions. CEL's comprehension macros (`all`, `exists`, `filter`, `map`) give it the richest collection-query surface of any system in this category, at the cost of exponential worst-case complexity.

**Precept implication:** FEEL validates every proposed expansion except regex. Cedar warns against function-library sprawl — Precept should be closer to FEEL's function surface than to a general-purpose stdlib, but it should explicitly document which FEEL functions it *excludes* and why. The absence of conditional expressions in Cedar is a conscious design choice for authorization policy; it does not apply to Precept, whose expression surface serves value computation in mutations, not just boolean policy evaluation. CEL validates three Precept positions: (1) a small, fixed string-function surface (`contains`, `startsWith`, `endsWith`, `size`) is sufficient for real policy evaluation; (2) conditional expressions are the most-used construct beyond basic comparisons; (3) Precept's deterministic left-to-right short-circuit for logical operators is safer than CEL's commutative semantics, which trade determinism for partial-data tolerance. CEL's comprehension macros validate that collection querying is a real need, but Precept should address it through fixed-form constructs (not general iteration) to preserve its strictly-linear complexity bound. See [cel-comparison.md](../references/cel-comparison.md) for the full language-level analysis.

### Enterprise Platforms

| System | Conditional expressions | String tests | Built-in functions | Logical keywords | Documentation |
|---|---|---|---|---|---|
| **Salesforce (formula fields)** | `IF(condition, trueValue, falseValue)` | `LEN(text)`, `CONTAINS(text, sub)`, `BEGINS(text, prefix)` | `ABS(n)`, `FLOOR(n)`, `CEILING(n)`, `ROUND(n, places)`, `MIN(a,b)`, `MAX(a,b)` | `&&`, `||`, `!` (symbols) | [Formula operators](https://help.salesforce.com/s/articleView?id=sf.customize_functions.htm) |
| **ServiceNow** | Calculated fields use JavaScript: `condition ? a : b` | JavaScript `length`, `includes()`, `startsWith()` | JavaScript `Math.abs()`, `Math.round()` | JavaScript operators | [Calculated fields](https://docs.servicenow.com/csh?topicname=c_CalculatedFields.html) |
| **Dynamics 365** | Calculated columns use FetchXML conditions, Power Fx `If()` | `Len(text)`, `Find(sub, text)` | `Abs(n)`, `Round(n, places)`, `Min(a,b)`, `Max(a,b)` | `And()`, `Or()`, `Not()` as functions | [Power Fx operators](https://learn.microsoft.com/en-us/power-platform/power-fx/operators) |
| **Guidewire** | Gosu: `condition ? a : b`, `if/else` expression | `.length`, `.contains(sub)`, `.startsWith(p)` | `Math.abs()`, `Math.round()` | `and`, `or`, `not` (keywords) | [Gosu language](https://gosu-lang.github.io/) |

**Pattern:** Enterprise platforms universally provide conditional expressions, string length, substring tests, and bounded math functions. Salesforce's formula-field library (200+ functions) is the maximum; Guidewire's Gosu language is the minimum within this category.

**Precept implication:** Precept's proposed function surface is deliberately closer to Guidewire/Gosu than to Salesforce. Enterprise platforms confirm that `abs`, `round`, `min`, `max`, string `length`, and `contains` are baseline expectations. String `startsWith`/`endsWith` are present across all four platforms — they are not exotic additions.

### Pure Validators

| System | Conditional expressions | String tests | Built-in functions | Logical keywords | Documentation |
|---|---|---|---|---|---|
| **FluentValidation** | `.When(cond)` / `.Unless(cond)` on rules; no inline ternary | `.MaximumLength(n)`, `.MinimumLength(n)`, `.Matches(regex)` | No standalone functions; method-chained validators | Implicit `&&` via chaining | [Built-in validators](https://docs.fluentvalidation.net/en/latest/built-in-validators.html) |
| **Zod** | `.refine(val => cond ? a : b)` via JS; no native conditional | `.min(n)`, `.max(n)`, `.length(n)`, `.includes(sub)`, `.startsWith(p)`, `.endsWith(p)` | `.transform()` pipeline; no standalone math | Implicit via JS `&&`/`||` | [Zod string](https://zod.dev/?id=strings) |
| **Valibot** | `transform()` pipeline; no native conditional | `minLength()`, `maxLength()`, `includes()`, `startsWith()`, `endsWith()` | Pipeline-style transforms | Implicit via TS operators | [Valibot string](https://valibot.dev/api/string/) |
| **JSON Schema** | `if/then/else` (Draft 7+) for conditional schemas | `minLength`, `maxLength`, `pattern` (regex) | No functions | Implicit via `allOf`/`anyOf`/`not` combinators | [JSON Schema conditionals](https://json-schema.org/understanding-json-schema/reference/conditionals) |

**Pattern:** Validators provide string length and substring tests as core vocabulary. Conditional logic is either absent (FluentValidation — conditions are on rules, not on values) or added late as a recognized gap (JSON Schema Draft 7). No validator provides standalone math functions — they delegate to the host language.

**Precept implication:** Precept must match validator table-stakes for string operations (`.length`, `.contains`). But Precept is not a pure validator — it also computes mutation values in `set` expressions, which requires conditional expressions (#9) and math functions (#16) that validators never needed.

### State Machines

| System | Expression surface | Documentation |
|---|---|---|
| **XState** | JavaScript expressions in guards and actions; no constrained expression language | [XState guards](https://xstate.js.org/docs/guides/guards.html) |
| **Stateless (.NET)** | C# delegates for guards; no field model | [Stateless repo](https://github.com/dotnet-state-machine/stateless) |

**Pattern:** State machines have no constrained expression language — they delegate to the host language entirely.

**Precept implication:** Precept's expression surface is a capability no state-machine library provides. This is a differentiation point, not a comparison target.

### End-User Tools

| System | Conditional expressions | String tests | Built-in functions | Logical keywords | Documentation |
|---|---|---|---|---|---|
| **Excel** | `IF(condition, trueValue, falseValue)` | `LEN(text)`, `FIND(sub, text)`, `SEARCH(sub, text)` | `ABS(n)`, `ROUND(n, places)`, `MIN(...)`, `MAX(...)`, `FLOOR(n, sig)`, `CEILING(n, sig)` | `AND()`, `OR()`, `NOT()` as functions | [Excel functions](https://support.microsoft.com/en-us/office/excel-functions-alphabetical-fed26811-0dc5-4e13-abfe-199b57aab1a1) |
| **Google Sheets** | `IF(condition, trueValue, falseValue)` | `LEN(text)`, `FIND(sub, text)`, `SEARCH(sub, text)` | `ABS(n)`, `ROUND(n, places)`, `MIN(...)`, `MAX(...)` | `AND()`, `OR()`, `NOT()` as functions | [Sheets function list](https://support.google.com/docs/answer/1052432?hl=en) |
| **Notion formulas** | `if(cond, t, f)` | `length(prop)`, `contains(prop, sub)` | `abs(n)`, `round(n)`, `min(a,b)`, `max(a,b)` | `and(a,b)`, `or(a,b)`, `not(a)` | [Notion formulas](https://www.notion.so/help/formulas) |

**Pattern:** Every end-user tool provides `IF`, `LEN`/`length`, `ABS`, `ROUND`, `MIN`, `MAX`, and `AND`/`OR`/`NOT`. Spreadsheets treat everything as functions (including logical operators). Notion provides the minimal set closest to Precept's needs.

**Precept implication:** The spreadsheet mental model is directly relevant to Precept's audience. When a business analyst sees `field TotalFee as number`, they expect to be able to write something like `abs(ActualHours - EstimatedHours)`. The absence of basic math functions reads as broken, not minimal.

### Policy and Authorization

| System | Expression surface | Documentation |
|---|---|---|
| **OPA/Rego** | Full-language expressions; `some x in collection`; string `startswith()`, `contains()`; no conditional expression (policies return sets) | [Rego builtins](https://www.openpolicyagent.org/docs/latest/policy-reference/#built-in-functions) |
| **Cedar** | Minimal: `==`, `!=`, `<`, `>`, `<=`, `>=`, `in`, `has`, `like`, `.contains()`, `&&`, `||`, `!` — deliberately no math, no conditionals | [Cedar operators](https://docs.cedarpolicy.com/policies/syntax-operators.html) |

**Pattern:** Policy engines are the most conservative expression surfaces. Cedar deliberately omits math functions, conditional expressions, and string length.

**Precept implication:** Cedar's conservatism is appropriate for authorization decisions (permit/forbid) where the evaluation must be formally verifiable. Precept's expression surface serves a broader set of concerns — value computation, data quality assertions, and mutation guards — that justify a wider function library. However, Cedar's deliberate omission of `abs`, `round`, and conditional expressions is a reminder that every addition has a cost. Precept should ship the smallest function set that eliminates the documented sample-corpus pain, then extend only with evidence.

### Industry Data Standards

| Standard | Expression capability | Documentation |
|---|---|---|
| **FHIR** | FHIRPath: `where()`, `exists()`, `contains()`, `startsWith()`, `length`, `matches()`, `iif(cond, t, f)` — rich expression language for resource navigation | [FHIRPath](https://hl7.org/fhirpath/) |
| **ACORD** | No expression language; schema-only vocabulary | [ACORD standards](https://www.acord.org/standards-architecture) |
| **ISO 20022** | No expression language; constraint expressions delegated to implementation | [ISO 20022](https://www.iso20022.org/) |

**Pattern:** FHIR is notable for having its own constrained expression language (FHIRPath) with conditional expressions, string functions, and collection predicates. Other standards define vocabulary, not evaluation.

**Precept implication:** FHIRPath validates that even industry standards need expression power for navigating and constraining governed data. The convergence between FHIRPath, FEEL, and Precept's proposed surface is striking — all three need conditionals, string tests, and bounded math.

---

## Cross-Category Pattern

The precedent survey covers 25+ systems across 9 categories. The convergence is unusually strong:

| Capability | Systems that provide it | Systems that omit it | Precept gap? |
|---|---|---|---|
| Conditional value expression | 20+ (every database, language, platform, FEEL, spreadsheets, FHIRPath) | Cedar, pure validators (by design) | **Yes — #9** |
| String length | 20+ (every system with string operations) | Cedar, state machines (no field model) | **Yes — #10** |
| Substring test | 20+ (`contains`, `FIND`, `includes`, `LOCATE`) | State machines only | **Yes — #15** |
| Math functions (`abs`, `round`, `min`, `max`) | 18+ (databases, languages, platforms, FEEL, spreadsheets) | Cedar (deliberately), validators (delegate to host), state machines | **Yes — #16** |
| Keyword logical operators | SQL, Python, FEEL, Guidewire, JSON Schema, Alloy, Excel, Sheets, Notion | C#, TypeScript, Kotlin (symbols), Cedar (symbols) | **Yes — #31** |
| String `startsWith`/`endsWith` | 18+ (languages, validators, platforms, FEEL, FHIRPath, OPA) | Databases (use `LIKE`/`LEFT`), Cedar (uses `like` glob pattern) | Proposed in #16 |
| Regular expressions | 12+ (validators, databases, OPA, FHIRPath) | Cedar, FEEL (omits regex), Precept | **Deliberately excluded** |
| User-defined functions | General-purpose languages only | Every constrained DSL (Cedar, FEEL, Alloy, SQL CHECK, JSON Schema) | **Deliberately excluded** |

The gap pattern is clear: Precept's current expression surface lacks capabilities that every system in its positioning neighborhood provides. The proposed expansions (#9, #10, #15, #16, #31) bring Precept to the baseline, not past it. The exclusions (regex, user-defined functions, quantifiers) keep it within the decidable, constrained-DSL family.

---

## Philosophy Fit

Each expansion is evaluated against the design principles in `docs/PreceptLanguageDesign.md` and the positioning in `docs/philosophy.md`.

### #9 — Conditional expressions (`if...then...else`)

**Prevention, not detection.** Conditional expressions *improve* prevention. Without them, authors duplicate entire transition rows to vary one field value. Duplicated rows drift — one copy gets updated while the other doesn't. The conditional expression eliminates the duplication that makes drift possible.

**One file, complete rules.** A single `set Fee = if IsMember then 0 else 25` replaces two transition rows. The rule lives in one place.

**Determinism and inspectability.** Both branches type-check; both are visible. The condition, the true value, and the false value are all inspectable. No hidden evaluation.

**AI-readable authoring.** `if...then...else` is the most widely recognized conditional form across languages. AI agents generate it correctly without ambiguity. The keyword form (vs. `? :`) is more tokenizable and completable by language servers.

**Self-contained rows (Principle 7).** The conditional expression *preserves* self-contained rows — it collapses two rows into one, making the surviving row more complete, not less.

**Keyword-anchored flat statements (Principle 3).** `if` does not appear at line start — it is always embedded in an expression context (after `=`, inside `invariant`, etc.). The teaching model is explicit: `when` skips rows; `if` selects values. The compiler rejects line-start `if` with a redirect message.

### #10 — String `.length`

**Table stakes.** Every system in the survey provides string length. Its absence is embarrassing, not minimal.

**Accessor consistency.** `.length` mirrors `.count` on collections — a parameterless size read. Same vocabulary category, same grammar form, same type (`number`).

**Prevention.** String-length constraints are standard data-quality rules: "description must not exceed 500 characters" cannot be expressed today. That is a prevention gap.

### #15 — String `.contains()`

**Narrow membership test.** `.contains(literal)` tests substring presence — the string analogue of the collection `contains` operator. No regex, no pattern engine, no format validators.

**Null safety.** `.contains()` on a nullable string is a compile error unless the author has null-checked first. Explicit null handling, consistent with the accessor model.

**Vocabulary boundary.** This is a *method* (`.contains(arg)`), not an accessor (`.length`) or an operator (`contains`). The method form establishes the precedent for future typed-receiver calls without committing to a general method-call surface.

### #16 — Built-in function library

**Bounded, static, deterministic.** The function registry is finite, known at compile time, and every function is a pure computation with no side effects. This preserves decidability and inspectability.

**Infrastructure cost.** The function-call AST node (`PreceptFunctionCallExpression`) and static registry are a one-time parser/checker investment. Every future function is a registry entry — no grammar change needed.

**Cedar counter-signal.** Cedar deliberately omits math functions. But Cedar evaluates authorization policies (permit/forbid), not data mutations. Precept's `set` expressions compute values — `abs()`, `round()`, `min()`, `max()` are justified by the computation domain that Cedar's expression surface never needed to serve.

**Configuration feel.** Functions like `abs(x)` and `round(x, 2)` are familiar from spreadsheets, not from general-purpose programming. They read as configuration vocabulary, not as a programming API.

### #31 — Logical keywords (`and`, `or`, `not`)

**Principle 13 compliance.** The locked design framework (§Keyword vs Symbol Design Framework) explicitly specifies keywords for logical operators and symbols for math/comparison. `#31` is the implementation of an already-decided principle.

**Precedent alignment.** SQL, Python, FEEL, Alloy, Guidewire, and every end-user tool use keyword logical operators. Precept's audience overlaps more with these systems than with C-family symbol conventions.

**Readability.** `when not IsPremium and Score >= 680 or HasOverride` reads as a business rule. `when !IsPremium && Score >= 680 || HasOverride` reads as code. The keyword form aligns with Precept's "English-ish but not English" principle (Principle 2).

**`!=` stays.** The inequality comparison operator `!=` is in the *comparison* family (same as `==`, `>`, `<`), not the *logical* family. Every keyword-for-logic system (SQL, Python, Alloy, FEEL) retains `!=` without issue.

---

## Null Safety Policy

The five proposals share a null-safety surface that must be consistent.

### Current model

- Fields can be `nullable` — the type checker tracks `StaticValueKind.Null` flags.
- `&&` short-circuit provides null narrowing: `X != null && X.count > 0` type-checks because `X` is narrowed to non-null on the right side.
- Collection fields default to empty, not null. Collection accessors (`.count`, `.min`, `.max`, `.peek`) on an empty collection return `0`, the inner type's default, or fail depending on the accessor.

### Policy for new constructs

| Construct | Null behavior | Rationale |
|---|---|---|
| `if...then...else` (#9) | If the condition narrows a nullable to non-null, the `then` branch sees the narrowed type. Both branches type-check independently. The result type is the common type of both branches. | Consistent with `&&` narrowing. |
| `.length` (#10) | Nullable string `.length` is a compile error unless the string is null-checked first (`Name != null && Name.length > 0`). No implicit `0` for null strings. | Conservative: explicit null handling. Mirrors the existing `.count` model where collection is never null. String nullable is different — the author must narrow. |
| `.contains(sub)` (#15) | Nullable string `.contains()` is a compile error without null check. | Same conservative policy as `.length`. |
| `abs()`, `round()`, etc. (#16) | Null arguments produce a compile error if the function signature requires non-null. If a nullable expression is passed, the author must narrow first. | Static rejection, not runtime surprise. |
| `and`, `or`, `not` (#31) | Identical to `&&`, `||`, `!` — keyword tokens with the same type rules and narrowing behavior. | Pure token swap; no semantic change. |

### Null-coalescing (`??`) — deferred

Null-coalescing is identified in the audit (L7) but has no open proposal. It would be the natural complement to the conservative null policy above: `Name ?? "default"` narrows a nullable string to non-null. When a proposal is opened, the research base is already established — it belongs in the expression-expansion family, not as a standalone feature.

---

## String Surface

The proposals collectively define Precept's string expression surface. It must be explicit.

### What ships

| Operation | Form | Category | Result type |
|---|---|---|---|
| String length | `Field.length` | Accessor | `number` |
| Substring test | `Field.contains("sub")` | Method | `boolean` |
| String equality | `Field == "value"` | Operator (existing) | `boolean` |
| String inequality | `Field != "value"` | Operator (existing) | `boolean` |
| String concatenation | `"prefix" + Field` | Operator (existing) | `string` |
| Prefix test | `startsWith(Field, "prefix")` | Function (#16) | `boolean` |
| Suffix test | `endsWith(Field, "suffix")` | Function (#16) | `boolean` |
| Case normalization | `toLower(Field)`, `toUpper(Field)` | Function (#16) | `string` |
| Whitespace trim | `trim(Field)` | Function (#16) | `string` |

### What does not ship

| Operation | Why excluded |
|---|---|
| Regex / pattern matching | Decidability risk (ReDoS); no blocking sample demand; delegate to host application |
| `substring(s, start, end)` | Extracting substrings is a *transformation*, not a *constraint*. Precept constrains data shape; it does not reshape strings. Defer to Wave 2 if demand emerges. |
| String ordering (`<`, `>`, `<=`, `>=`) | Low sample demand (audit L10 — "Moderate"). Revisit when choice types (#25) ship — ordered choices may subsume lexicographic comparison use cases. |
| String interpolation / templates | Template syntax; not needed for constraint evaluation or value computation. |
| `indexOf(s, sub)` | Returns a position integer. The only use case is "does the string contain the substring" which `.contains()` already covers. |

### `startsWith` and `endsWith` — function vs. operator

The expression-evaluation reference identifies `startsWith`/`endsWith` as candidates for comparison-level operators (like `contains`). The audit suggests the same placement. However, proposal #16 places them as *functions* (`startsWith(s, prefix)`), not operators.

**Recommendation: functions.** The function form is consistent with FEEL (`starts with(s, p)`) and avoids adding more keyword operators to the comparison level. The operator `contains` already exists as a keyword operator for collection membership — adding `startsWith` and `endsWith` as operators would crowd the comparison level with three keyword operators. Functions keep the comparison level focused on universal math/equality operators plus the single membership keyword.

---

## Semantic Contracts

### 1. Expression position scope

The expression expansion introduces new construct forms. Each must be scoped:

| Construct | `set` RHS | `when` guard | `invariant` | State/event `assert` | Computed field (#17) |
|---|---|---|---|---|---|
| `if...then...else` (#9) | ✓ | ✓ (in boolean expressions) | ✓ | ✓ | ✓ |
| `.length` (#10) | ✓ | ✓ | ✓ | ✓ | ✓ |
| `.contains()` (#15) | N/A (boolean result, not a value) | ✓ | ✓ | ✓ | ✓ (in boolean-typed computed fields) |
| Functions (#16) | ✓ | ✓ | ✓ | ✓ | ✓ |
| `and`/`or`/`not` (#31) | N/A (token swap) | ✓ | ✓ | ✓ | ✓ |

All new constructs are valid in all expression positions. There is no "set RHS only" restriction for any of these additions. All expression positions work identically in stateless precepts — none of the new constructs are state-dependent. The audit's earlier suggestion to restrict ternary to `set` RHS only (§5 cross-cutting notes) is superseded — `if...then...else` yielding boolean is valuable in invariants (`invariant (if IsPremium then Score >= 700 else Score >= 600) because "..."`) and requires no special handling since both branches type-check.

### 2. Precedence integration

| New construct | Precedence placement | Rationale |
|---|---|---|
| `if...then...else` | Lowest (below `or`) | Total expression; wraps entire condition-value-value triple. Parentheses for nesting: `set X = if A then (if B then 1 else 2) else 3`. |
| `.length`, `.contains()` | Atom level (as dotted access / method call) | Highest precedence — resolved during atom parsing, before any operator. |
| Functions | Atom level | `abs(X - Y)` parses as atom `abs(...)` containing the expression `X - Y`. |
| `and` | Same as `&&` (precedence 2) | Token swap, no semantic change. |
| `or` | Same as `||` (precedence 1) | Token swap. |
| `not` | Same as `!` (precedence 7) | Token swap. |

### 3. Type checking contracts

| Construct | Type rule |
|---|---|
| `if C then T else E` | `C` must be `boolean`. `T` and `E` must produce the same type. Result type = common type of `T`/`E`. |
| `Field.length` | `Field` must be `string` (or `string nullable` after null narrowing). Result: `number`. |
| `Field.contains(sub)` | `Field` must be `string` (or narrowed). `sub` must be `string` literal. Result: `boolean`. |
| `abs(x)` | `x` must be `number`. Result: `number`. (Overloads for future `integer`/`decimal`.) |
| `round(x)` | `x` must be `number`. Result: `number`. Future: `round(decimal, N)` where `N` is literal integer. |
| `min(a, b)`, `max(a, b)` | Both must be `number`. Result: `number`. |
| `startsWith(s, p)`, `endsWith(s, p)` | Both must be `string`. Result: `boolean`. |
| `toLower(s)`, `toUpper(s)`, `trim(s)` | `s` must be `string`. Result: `string`. |

### 4. Tooling and MCP surface

- `precept_language` must return the complete function registry, new accessors, new method forms, and updated operator keywords.
- `precept_compile` must parse and type-check all new constructs, returning diagnostics for type mismatches.
- `precept_inspect` must evaluate conditional expressions, show function results, and reflect new accessor values in preview output.
- `precept_fire` must execute all new constructs during the mutation pipeline.
- Language server completions must suggest `if` after `=`, `.length` after string-typed identifiers, `.contains(` after string-typed `.`, function names in expression positions, and `and`/`or`/`not` in boolean contexts.

---

## Dead Ends and Rejected Directions

### Ternary operator (`? :`) for #9

Rejected in `conditional-logic-strategy.md`. Colon symbol conflicts with type syntax (Principle 3). Language server cannot complete after `?`. AI agents produce higher error rates with symbol ternary than keyword `if...then...else`. Every keyword-oriented DSL in the survey (FEEL, SQL, Alloy) uses a keyword form.

### `choose...when...otherwise` for #9

Considered and rejected. Eliminates false-affordance of `if` (readers might expect imperative branching) but loses the universal recognition of `if...then...else`. The false-affordance risk is managed through compiler error messages, not keyword avoidance.

### `implies` for conditional constraints

Rejected in `conditional-logic-strategy.md`. Formal logic register (`A implies B` ≡ `not A or B`) creates double-negative confusion. FluentValidation, Cedar, DMN all avoid it. The `when` guard on invariants (#14) is the correct form.

### `unless` keyword for negation

Rejected (7-to-3 against in precedent survey). `unless` breaks down on compound conditions (De Morgan confusion with `unless A and B`). Precept's one-canonical-form principle means `when not` is the single unambiguous way to express negative guards.

### `else reject` (#12) for inline fallback

Declined. Semantic dual-meaning of `else`: in value context it selects a value (`if...then...else`); in action context it would route to rejection. Overloading `else` with two incompatible meanings violates the one-meaning-per-keyword principle.

### Regex/pattern matching for string operations

Deferred indefinitely. Decidability risk (ReDoS with unrestricted patterns). No blocking sample demand. The survey shows Cedar and FEEL both omit regex — only full-language validators (Zod `.regex()`, FluentValidation `.Matches()`) and databases (`LIKE`, `REGEXP`) include it. These systems can afford regex because they delegate execution to a host engine with timeout protection. Precept's deterministic evaluation model cannot absorb unbounded regex execution.

### Collection aggregate functions (`sum`, `avg`, `median`)

Deferred. The current sample corpus does not exhibit demand — `LineItems.sum` appears in the audit as a hypothetical, not an actual authoring pain point. The function-call infrastructure (#16) makes adding aggregates trivial later, but shipping them without demand violates the "evidence before expansion" principle.

### User-defined functions / lambda expressions

Permanently excluded. Lambda scoping breaks the flat-identifier model. User-defined functions introduce undecidability. Every constrained DSL in the survey (Cedar, FEEL, Alloy, SQL CHECK, JSON Schema) deliberately omits them. If a business rule requires custom computation, it belongs in the host application, not in the precept.

### Varargs for min/max

Excluded for v1. FEEL and SQL allow `min(a, b, c, ...)` with variable arity. Precept's v1 function registry uses fixed signatures only. Two-argument `min(a, b)` covers all sample cases. Nesting handles three-way: `min(a, min(b, c))`. Variable arity adds parser complexity and type-checker branching with negligible practical benefit.

### Method chaining on strings

Excluded. `.contains("x").length` is meaningless (`.contains()` returns boolean, not string), but `.toLower().contains("x")` could read naturally. However, chaining requires the parser to handle arbitrary `.method()` sequences on expression results, which is a significant grammar extension. The function form (`contains(toLower(Field), "x")`) achieves the same result with no grammar cost.

---

## Proposal Implications

### Sequencing recommendation

1. **#31 (logical keywords)** — pure token swap, zero semantic risk, immediately improves readability across the entire corpus. Should ship first as a standalone change.
2. **#10 (string `.length`)** — single accessor addition, mirrors existing `.count` pattern, lowest implementation cost. Ship immediately after or alongside #31.
3. **#9 (conditional expressions)** — new AST node, but well-understood grammar. Eliminates the highest-frequency row-duplication pattern. Ship in the same wave as #10.
4. **#15 (string `.contains()`)** — new method-call form, but narrow scope (literal argument only). Can ship independently or bundled with #16.
5. **#16 (built-in functions)** — largest parser investment (function-call atom). Should ship after #9 and #10 establish the expression expansion pattern. Initial set: `abs`, `round`, `min`, `max`, `startsWith`, `endsWith`. String transformation functions (`toLower`, `toUpper`, `trim`) can follow.

### Interaction with other proposals

- **#8 (named rules):** Named rules reference fields in boolean expressions. All new expression constructs (#9, #10, #15, #16) are usable inside rule bodies, expanding what named rules can express.
- **#14 (conditional invariants):** `invariant ... when ...` benefits from #31 (`when not`) and #9 (conditional values inside the invariant expression).
- **#17 (computed fields):** Computed field expressions will use `if...then...else`, `.length`, functions, etc. The expression expansion is a prerequisite for meaningful computed fields.
- **#25 (choice types):** Choice types reduce the need for boolean-flag negation that motivates some uses of `not`. They also reduce the need for inline constant set tests (`Priority in ["Low", "Medium", "High"]`).
- **#27 (decimal type):** `round(decimal, N)` is the first expression function. The function-call infrastructure (#16) is a prerequisite for decimal usability.

---

## References

### Internal

- [expression-language-audit.md](./expression-language-audit.md) — severity-ranked gap inventory
- [expression-evaluation.md](../references/expression-evaluation.md) — formal decidability framework
- [conditional-logic-strategy.md](./conditional-logic-strategy.md) — `when`/`if` split and dead ends
- [conditional-invariant-survey.md](../references/conditional-invariant-survey.md) — 10-system survey
- [computed-fields.md](./computed-fields.md) — quality bar and cross-category precedent model
- [fluent-validation.md](./fluent-validation.md) — FluentValidation gap analysis
- [zod-valibot.md](./zod-valibot.md) — Zod/Valibot gap analysis
- [linq.md](./linq.md) — LINQ pipeline and ternary gap analysis
- [fluent-assertions.md](./fluent-assertions.md) — FluentAssertions comparison

### External

- FEEL specification: [OMG DMN 1.4](https://www.omg.org/spec/DMN/1.4), [Camunda FEEL reference](https://docs.camunda.io/docs/components/modeler/feel/language-guide/feel-expressions-introduction/)
- Cedar language: [Syntax grammar](https://docs.cedarpolicy.com/policies/syntax-grammar.html), [Operators](https://docs.cedarpolicy.com/policies/syntax-operators.html)
- PostgreSQL: [String functions](https://www.postgresql.org/docs/current/functions-string.html), [Math functions](https://www.postgresql.org/docs/current/functions-math.html), [Conditionals](https://www.postgresql.org/docs/current/functions-conditional.html)
- SQL Server: [String functions](https://learn.microsoft.com/en-us/sql/t-sql/functions/string-functions-transact-sql), [Math functions](https://learn.microsoft.com/en-us/sql/t-sql/functions/mathematical-functions-transact-sql)
- Python: [Built-in functions](https://docs.python.org/3/library/functions.html), [String methods](https://docs.python.org/3/library/stdtypes.html#string-methods)
- Salesforce: [Formula operators and functions](https://help.salesforce.com/s/articleView?id=sf.customize_functions.htm)
- Zod: [String schema](https://zod.dev/?id=strings)
- FluentValidation: [Built-in validators](https://docs.fluentvalidation.net/en/latest/built-in-validators.html)
- FHIRPath: [Specification](https://hl7.org/fhirpath/)
- JSON Schema: [Conditional subschemas](https://json-schema.org/understanding-json-schema/reference/conditionals)
- Drools: [Language reference](https://docs.drools.org/latest/drools-docs/drools/language-reference/)
- Excel: [Function reference](https://support.microsoft.com/en-us/office/excel-functions-alphabetical-fed26811-0dc5-4e13-abfe-199b57aab1a1)
- Pierce, *Types and Programming Languages* — typed arithmetic expressions (Ch. 8)
- Veanes et al., "Symbolic Finite Automata" (CACM 2021) — decidable Boolean algebras over string predicates
