# Evaluator Architecture Survey

> External research supporting Issue #115 / PR #116 — numeric lane integrity campaign.
>
> **Researchers:** Frank (architecture patterns, comparable systems), George (implementation patterns, .NET precedent)
> **Date:** 2026-04-19
> **Design doc:** `docs/EvaluatorDesign.md` (DD1–DD9)

---

## 1. Expression Evaluator Architecture Patterns

### CEL (Common Expression Language) — Most Relevant

**Source:** [github.com/google/cel-spec — language definition](https://github.com/google/cel-spec/blob/master/doc/langdef.md)

**Key insight:** CEL uses the same Parse → Type-Check → Evaluate three-phase pipeline as Precept, with three distinct numeric types: `int` (int64), `uint` (uint64), `double` (IEEE 754 64-bit). Operators are named functions with typed overloads — `_+_(int, int) → int`, `_+_(double, double) → double`. Cross-type arithmetic is a **type error**, not implicit promotion. Cross-type equality is allowed.

**Relevance to Precept:** Directly validates the dispatch-table model. The function-dispatch pattern (`_+_(e1, e2)` with per-type overloads) maps cleanly to Precept's evaluator.

**DD impact:** Validates DD1, DD2. CEL's strictness is stronger than Precept's (no implicit widening at all), confirming our three-lane model is well within established precedent.

**Recommendation:** Adopt — the operator-as-typed-overload dispatch pattern.

### FEEL (DMN)

**Source:** OMG DMN 1.4 specification; FEEL uses IEEE 754-2008 Decimal128 (Java BigDecimal with DECIMAL128 MathContext, 34 digits).

**Key insight:** FEEL uses a single number type (Decimal128). No int/float distinction. Avoids the lane problem entirely by collapsing everything to one high-precision type.

**Relevance to Precept:** Validates that Precept's three-lane approach is more rigorous than the standard business decision language. FEEL's single-type model trades performance for simplicity; Precept's three-lane model gives authors explicit control over precision semantics.

**DD impact:** Neutral on DD1 (different approach, both valid). Validates DD2 (decimal closure) — FEEL achieves this trivially since everything is decimal.

**Recommendation:** Inform — useful as a comparison point, but Precept should not adopt the single-type model.

### NRules (.NET Rule Engine)

**Source:** [nrules.net/articles/architecture.html](https://nrules.net/articles/architecture.html)

**Key insight:** Three-phase pipeline: Fluent DSL / Rule# → Canonical Rules Model (AST) → Compiled Rete network. Delegates type semantics entirely to .NET's own type system via C# expression trees (LINQ).

**Relevance to Precept:** NRules doesn't need its own type dispatch because it compiles to .NET expressions. Precept can't delegate this way — the DSL has its own type system that must map to .NET types while preserving lane semantics.

**DD impact:** Validates that Precept needs its own dispatch layer (DD1).

**Recommendation:** Inform — confirms the problem is real; NRules avoids it by not having its own expression language.

### XState Guards

**Source:** XState v5 documentation — guard evaluation model.

**Key insight:** Guards are pure synchronous functions returning `true`/`false`. Higher-level composition: `and([...])`, `or([...])`, `not(...)`. Expression evaluation is delegated to JavaScript — no custom evaluator.

**Relevance to Precept:** Validates Precept's guard evaluation pattern, but XState's delegation to the host language is not applicable — Precept must implement its own type-preserving runtime.

**DD impact:** Neutral.

**Recommendation:** Inform only.

### JsonLogic

**Key insight:** No type system. JavaScript coercion semantics. Shows what NOT to do — implicit coercion leads to unpredictable results.

**Recommendation:** Reject — anti-pattern for a type-preserving evaluator.

---

## 2. Numeric Precision in Business Rule Systems

### C# Numeric Promotions — Authoritative for .NET

**Source:** [C# Language Specification §12.4.7 — Numeric Promotions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/)

**Key insight:** C# itself **forbids** mixing `decimal` with `float`/`double` in arithmetic — it is a binding-time error. Predefined operator overloads exist per type: `int operator *(int, int)`, `decimal operator *(decimal, decimal)`, etc. Integer types widen to the wider operand type. Decimal is isolated from IEEE 754 types.

**Relevance to Precept:** Our lane isolation directly mirrors the host language's own rules. The three-lane model with implicit integer widening and forbidden decimal↔number mixing is not novel — it's what C# already does.

**DD impact:** Strongly validates DD1, DD2, DD6. The C# spec is the authoritative precedent for our design.

**Recommendation:** Adopt — cite C# §12.4.7 as primary precedent.

### .NET Generic Math (`INumber<T>`)

**Source:** [devblogs.microsoft.com — .NET 7 Generic Math](https://devblogs.microsoft.com/dotnet/dotnet-7-generic-math/)

**Key insight:** .NET generic math explicitly separates `decimal` from IEEE 754 types. `decimal` participates in `IFloatingPoint` but NOT `IFloatingPointIeee754`. APIs like generic `Sqrt` are discussed in terms of IEEE 754 types only.

**Relevance to Precept:** Validates DD1 and DD8. The .NET type hierarchy itself treats decimal as fundamentally different from double.

**DD impact:** Validates DD1, DD8. Supports explicit special-casing for `sqrt(decimal)`.

**Recommendation:** Inform — validates our model; `INumber<T>` doesn't help with runtime dynamic dispatch but confirms the type separation is correct.

### .NET Decimal Performance

**Source:** [.NET performance benchmarks — Perf.Decimal.cs](https://github.com/dotnet/performance/blob/main/src/benchmarks/micro/libraries/System.Runtime/Perf.Decimal.cs)

**Key insight:** The .NET team benchmarks decimal operations directly, confirming decimal performance is a real concern. However, no strong official source justifies a fixed "10-20x slower" ratio — actual performance depends on the operation and hardware.

**Relevance to Precept:** The design doc's "~10–20x for division" claim should be softened to "materially slower" unless verified with local benchmarks.

**DD impact:** Challenges the specific performance claim in DD2's tradeoff section.

**Recommendation:** Soften performance claim or add a local BenchmarkDotNet verification task.

### SQL Server DECIMAL/NUMERIC

**Source:** [learn.microsoft.com — decimal and numeric (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/data-types/decimal-and-numeric-transact-sql)

**Key insight:** Fixed precision and scale, each `(precision, scale)` combination is a different data type. Conversion from `float` to `decimal` can lose precision. Uses rounding for narrowing conversions.

**Relevance to Precept:** SQL's strict separation of DECIMAL and FLOAT through expression evaluation is a long-established precedent for lane preservation.

**DD impact:** Validates DD1, DD6.

### Java BigDecimal

**Source:** Java SE documentation — `java.math.BigDecimal`.

**Key insight:** `equals` considers scale (`2.0 ≠ 2.00`), `compareTo` doesn't. `divide` requires explicit rounding mode or throws `ArithmeticException`. `new BigDecimal(double)` is unpredictable — `new BigDecimal(0.1) ≠ 0.1`. This is the most cited numeric precision pitfall in business software.

**Relevance to Precept:** The `double → decimal` constructor trap is exactly what DD6 prevents at the runtime boundary. `CoerceToDecimal()` rejecting `double` inputs avoids the BigDecimal footgun.

**DD impact:** Strongly validates DD6.

### F# Operators — Strictest Precedent

**Source:** [learn.microsoft.com — F# symbol and operator reference](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/symbol-and-operator-reference/)

**Key insight:** NO implicit numeric conversions at all. `1 + 1.0` is a type error. Statically Resolved Type Parameters (SRTP) enable generic numeric code resolved at compile time.

**Relevance to Precept:** F#'s strictness is the gold standard for type preservation. Precept should aim between CEL's strictness and C#'s promotion rules — stricter than C# (no decimal↔number mixing) but more permissive than F# (allow implicit integer widening).

**DD impact:** Validates DD1, DD2, DD5.

---

## 3. Visitor vs Recursive Descent vs Compiled Evaluation

### Tree-Walk Interpreter (Current Precept Approach)

**Source:** Crafting Interpreters (Robert Nystrom) — tree-walking evaluation.

**Key insight:** Precept's `PreceptExpressionEvaluator` is a tree-walk interpreter: `Evaluate()` dispatches on expression type via pattern matching, recursive calls to children, runtime type checking via `is` patterns. At 779 lines, it is well within the manageable size for this approach. Simple, easy to extend incrementally.

**Recommendation:** Keep — no architectural rewrite needed. Add dispatch table within the existing structure.

### Pratt Parser Dispatch Pattern

**Source:** Bob Nystrom — "Pratt Parsers: Expression Parsing Made Easy."

**Key insight:** Token-type → parselet dispatch: `Map<TokenType, Parselet>` replaces if/else chains. This pattern maps directly to a type-directed dispatch model — register evaluation functions by `(operator, type, type)` triple instead of by token type.

**Relevance to Precept:** The recommended dispatch architecture for the lane-integrity work.

**DD impact:** Supports the implementation strategy for DD1 and DD2.

**Recommendation:** Adopt — the dispatch-table pattern from Pratt parsing, applied to type-directed evaluation.

### Compiled Evaluation (NRules Rete, Roslyn IL)

**Key insight:** NRules compiles to Rete networks, Roslyn compiles to IL. Both are overkill for Precept's evaluator — Precept evaluates individual expressions in a business-rule context, not rule networks or general-purpose programs.

**Recommendation:** Reject — over-engineering for a ~800-line evaluator.

---

## 4. Type-Directed Dispatch Patterns

### The Problem in Precept Today

`TryToNumber()` at `PreceptExpressionEvaluator.cs` line ~734 collapses ALL numeric types to `double`:
- `decimal dec` → `(double)dec` — **lane violation**
- `long l` → `(double)l` — precision loss for large longs (beyond ±2⁵³)

Every arithmetic and comparison operator first checks `is long` (integer lane), then falls through to `TryToNumber` which collapses everything to double. The decimal lane doesn't exist at the evaluator level.

### CEL's Named Function Dispatch (Recommended)

Operators as named functions with typed overloads. Cross-type arithmetic is a type error. This is the cleanest model for a three-lane evaluator.

### C#'s Operator Overload Resolution (Reference)

Separate predefined operator implementations per type (§12.13). The compiler selects the best overload via numeric promotion rules. Decimal never mixes with float/double.

### Recommended Dispatch Table

| Left \ Right | `integer` | `decimal` | `number` |
|---|---|---|---|
| **integer** | → `long` result | → `decimal` result (promote int→decimal) | → `double` result (promote int→double) |
| **decimal** | → `decimal` result (promote int→decimal) | → `decimal` result | Type error |
| **number** | → `double` result (promote int→double) | Type error | → `double` result |

This mirrors C#'s binary numeric promotion rules (§12.4.7.3).

### Implementation Strategy

Replace `TryToNumber(object?, out double)` with type-aware arithmetic dispatch:
1. Classify operands into lanes: integer (long), decimal (C# decimal), number (double)
2. Select operation by `(operator, leftLane, rightLane)` triple
3. Reject cross-lane operations between decimal and number
4. Promote integer to the lane of the other operand when mixed
5. Return typed result preserving the lane

Can be done incrementally in the existing tree-walk evaluator — no architectural rewrite.

---

## 5. JSON and Runtime Boundary Patterns

### System.Text.Json Decimal Fidelity

**Source:** [.NET Runtime — Utf8JsonReader.TryGet.cs](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/src/System/Text/Json/Reader/Utf8JsonReader.TryGet.cs)

**Key insight:** JSON numbers are syntactically just number tokens; fidelity comes from which typed accessor you call. `GetDecimal()` vs `GetDouble()` must be called based on expected field type at ingress, not after-the-fact. A value may fail as decimal and still succeed as double.

**DD impact:** Strongly validates DD6.

**Recommendation:** Adopt — at every JSON ingress point, bind directly to the expected type.

### NodaMoney Decimal Preservation

**Source:** [NodaMoney — MoneyJsonConverter.cs](https://github.com/remyduijkeren/nodamoney/blob/main/src/NodaMoney/Serialization/MoneyJsonConverter.cs)

**Key insight:** Production money library preserves decimal fidelity: reads with `reader.GetDecimal()`, builds `Money` from `decimal` rather than `double`. String forms supported as a separate opt-in parsing path.

**DD impact:** Validates DD2, DD6.

---

## 6. Testing Patterns for Numeric Fidelity

### NCalc Type-Asserting Tests

**Source:** [NCalc — DecimalsTests.cs](https://github.com/ncalc/ncalc/blob/main/test/NCalc.Tests/DecimalsTests.cs)

**Key insight:** NCalc explicitly asserts both value and runtime type: float stays float, double stays double, decimal stays decimal. Includes closure checks, precision-sensitive examples (`0.3 - 0.2 - 0.1`), and "integers remain integers" tests even when decimal mode is enabled.

**DD impact:** Validates DD1, DD2, DD3, DD4. Provides a test pattern template.

**Recommendation:** Adopt — assert runtime type + value on every numeric evaluator test.

### Property-Based Testing (FsCheck)

**Source:** [FsCheck documentation](https://fscheck.github.io/FsCheck/Properties.html) and [FsCheck decimal tests](https://github.com/fscheck/fscheck/blob/main/tests/FsCheck.Test/Arbitrary.fs)

**Key insight:** Custom generators and shrinkers for decimal-specific properties. Integrated shrinking produces minimal failing examples that expose bad coercion rules quickly.

**DD impact:** Supportive across DD1–DD9.

**Recommendation:** Consider — property-based tests for lane closure, comparison consistency, and boundary values. Capture shrunk counterexamples as permanent regression tests.

---

## 7. Context-Sensitive Literal Typing Precedent

### C# Literal Rules

**Source:** [learn.microsoft.com — floating-point numeric types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types)

**Key insight:** Unsuffixed real literals are `double`. Requires `m`/`M` suffix for decimal. No target-typed literal inference for numeric types.

**DD impact:** Challenges DD9 as host-language precedent. C# does NOT do context-sensitive literal typing.

### F# Literal Rules

**Source:** [learn.microsoft.com — F# Literals](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/literals)

**Key insight:** Unsuffixed fractional literals are `float` (double). `decimal` requires `m`/`M` suffix. Same as C#.

**DD impact:** Challenges DD9 if framed as language precedent.

### Kotlin Literal Rules

**Source:** [kotlinlang.org — Numbers](https://kotlinlang.org/docs/numbers.html)

**Key insight:** Defaults fractional literals to `Double`. Rejects implicit widening across numeric types. Explicit conversion required.

**DD impact:** Challenges DD9 as precedent. Validates DD1, DD2, DD6.

### NCalc / DynamicExpresso — Evaluator-Library Precedent

**Source:** [NCalc ExpressionOptions](https://github.com/ncalc/ncalc/blob/main/src/NCalc.Core/ExpressionOptions.cs), [DynamicExpresso README](https://github.com/dynamicexpresso/dynamicexpresso/blob/main/README.md)

**Key insight:** Both support configurable default number type — NCalc's `DecimalAsDefault` flag, DynamicExpresso's configurable literal parsing. These are evaluator-library precedents, not language precedents. Both treat it as an explicit semantic policy.

**DD impact:** Supports DD9 as a plausible **DSL policy choice**, but NOT as mainstream language behavior.

**Recommendation:** Reframe DD9 in the design doc — present as deliberate DSL policy for business-domain authors, not as host-language precedent. Cite NCalc/DynamicExpresso as evaluator-library precedent.

---

## 8. Production Evaluator Architecture in .NET

### Roslyn Expression Evaluator

**Source:** [github.com/dotnet/roslyn — ExpressionCompiler](https://github.com/dotnet/roslyn)

**Key insight:** Separates compilation/binding/evaluation from result presentation. Lane-sensitive logic lives in the bound/evaluator core, not in UI or diagnostics.

**DD impact:** Validates keeping lane assignment in a dedicated evaluation layer.

### OData $filter Evaluator

**Source:** [AspNetCoreOData](https://github.com/OData/AspNetCoreOData)

**Key insight:** Binds typed literals into a typed expression tree using explicit suffixes. Preserves numeric intent through the bind phase. Uses `5.00m` syntax for decimal filter values.

**DD impact:** Validates DD4. Mildly challenges DD9 (OData uses explicit literal markers).

### Entity Framework Core Type Mapping

**Source:** [EF Core — DecimalTypeMapping](https://github.com/dotnet/efcore)

**Key insight:** Preserves decimal semantics by carrying type mapping metadata through translation rather than recomputing numeric intent from values late in the pipeline. Provider-specific decimal shims exist because decimal semantics are important enough to preserve even when the backend is awkward.

**DD impact:** Strongly validates DD1, DD2, DD6. Supports representing lane as metadata on expressions/values.

---

## Summary

| Design Decision | External Evidence | Strength | Action |
|-----------------|-------------------|----------|--------|
| DD1: Three lanes | CEL, C# spec §12.4.7, F#, Kotlin | **Strong** | Validated — no change needed |
| DD2: Decimal closure | C# spec (decimal↔double forbidden), NCalc, NodaMoney | **Strong** | Validated — no change needed |
| DD3: Integer surfaces | CEL (int64 distinct from double), C# operator overloads | **Strong** | Validated — no change needed |
| DD4: Parser literal fidelity | OData, EF Core type mapping, DynamicExpresso | **Strong** | Validated — no change needed |
| DD5: round() bridge | CEL (no implicit crossing), C# spec (decimal↔double error) | **Strong** | Validated — no change needed |
| DD6: No double→decimal at boundary | Java BigDecimal footgun, NodaMoney, System.Text.Json | **Strong** | Validated — no change needed |
| DD7: Proof engine in double | N/A (domain-specific) | **N/A** | No external precedent applies |
| DD8: sqrt(decimal) approximate | .NET generic math (decimal excluded from IFloatingPointIeee754) | **Moderate** | Validated — document as explicit bridge |
| DD9: Context-sensitive literals | NCalc, DynamicExpresso (evaluator precedent); C#, F#, Kotlin (language counter-precedent) | **Moderate** | **Reframe** as deliberate DSL policy, not host-language precedent |

## Open Questions

1. **Decimal performance claim:** The "10–20x slower for division" claim in DD2 lacks a strong official source. Run local BenchmarkDotNet verification or soften to "materially slower."
2. **Decimal overflow policy:** NCalc silently falls back to `double` infinity on decimal overflow. Precept should decide: error or silent widening? (Recommendation: error — silent widening is exactly the lane violation our design prevents.)
3. **Dispatch table implementation:** Should the `(operator, leftType, rightType)` dispatch be a literal lookup table, pattern-matching, or method-overload resolution? CEL uses function registration; C# uses compiler overload resolution. For a tree-walk evaluator, pattern matching with explicit cases is likely cleanest.
