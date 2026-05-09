# UCUM / ISO 4217 Implementation Gap — Immediate Architecture

## Status

| Property | Value |
|---|---|
| Branch posture | Spike branch — build the real thing now |
| Updated | 2026-05-09T10:56:10.942-04:00 |
| Scope | Replace the UCUM closed-set placeholder with the actual parser architecture |

## Summary

Currency is already on the correct path: `src/Precept/Language/CurrencyCatalog.cs` establishes the right shape for ISO 4217 metadata, and `ClosedSetValidation` remains appropriate for validating atomic currency codes. UCUM is not on the correct path. The `RecognizedUnits` closed set in `src/Precept/Language/Types.cs` must be replaced by a real UCUM parser and catalog subsystem, with Tier 1 kept strictly as a tooling-discovery tier, not as the acceptance boundary.

The architectural call is simple: **build the UCUM parser as a shared core component inside `src/Precept/Language/`, feed it authoritative atom data from `src/Precept/Data/Ucum/`, and make every consumer — type checker, runtime, language server, and MCP — read from that one subsystem.** No parallel lists. No deferred grammar work. No second validator hiding in tooling.

## Grounding in the Current Branch

### What the branch has today

- `src/Precept/Language/Types.cs`
  - `RecognizedUnits` is a `FrozenSet<string>` with 58 values.
  - `RecognizedDimensions` is a `FrozenSet<string>` with 11 names.
  - `UnitOfMeasureValidation` and `DimensionValidation` are `ClosedSetValidation` instances.
- `src/Precept/Language/Type.cs`
  - `ContentValidation` is a DU with `RegexValidation`, `NodaTimeValidation`, and `ClosedSetValidation`.
- `src/Precept/Pipeline/TypeChecker.Expressions.cs`
  - typed-constant validation dispatches on the `ContentValidation` DU.
- `src/Precept/Language/DeclaredQualifierMeta.cs`
  - unit qualifiers currently carry only `UnitCode` and `DimensionName` strings.
- `tools/Precept.LanguageServer/LanguageServerStubs.cs`
  - completions are still stubs, which means we can wire the correct UCUM-backed shape now instead of building on a placeholder.
- `tools/Precept.Mcp/Tools/CompileTool.cs`
  - field qualifiers are rendered as strings; there is no structured UCUM projection yet.

### What the docs already require

- `docs/language/precept-language-spec.md`
  - D5/D13 require UCUM-backed quantity/unit semantics.
  - `quantity`, `unitofmeasure`, `dimension`, and `price` are first-class business-domain types.
  - `quantity` arithmetic is dimension-aware; compound results are part of the design.
- `docs/language/business-domain-types.md`
  - the intended architecture is already explicit: `UnitCatalog`, full UCUM grammar, discovery tiers, curated dimension names, and exact domain semantics.
- `docs/runtime/evaluator.md`
  - the runtime direction already assumes evaluator-internal `Unit`, `Dimension`, and `UnitTier` shapes.
- `.squad/decisions.md`
  - the durable direction is `CurrencyCatalog` + UCUM-backed quantity/unit handling, with `Unit` remaining evaluator-internal and the public boundary carrying lean identity types.

The branch therefore does **not** need a roadmap. It needs the missing implementation architecture written down plainly.

## Architectural Decision

### 1. Currency stays structured and atomic

`currency` remains a closed atomic code validated from `CurrencyCatalog`. That is correct architecture because ISO 4217 codes are not grammar expressions. `ClosedSetValidation` remains valid for `currency`; the underlying source of truth is the structured catalog, not a flat string set.

### 2. UCUM moves from closed set to parser subsystem

`unitofmeasure` does **not** remain a closed set. It moves to a dedicated UCUM subsystem that:

- lexes and parses valid UCUM expressions,
- resolves atoms and prefixes from a catalog,
- computes exact conversion metadata and a dimension vector,
- classifies the result against Precept's curated dimension catalog,
- returns a structured semantic object to every consumer.

Tier 1 survives only as **proactive discovery** for completions, hover, examples, and builder assistance. It is not the validator.

### 3. The UCUM subsystem lives in `src/Precept/Language/Ucum/`

That is the correct placement.

It must **not** live only in `Runtime/`, because compile-time validation, language-server diagnostics, and MCP compile output need it before evaluator execution exists. It must **not** live in tooling, because tooling is a consumer, not the language authority. It belongs beside the language catalogs as a shared language-level subsystem.

Proposed physical layout:

- `src/Precept/Language/Ucum/` — parser, semantic model, catalog facade, dimension classification
- `src/Precept/Data/Ucum/` — authoritative UCUM source data kept with the core repo data assets
- `src/Precept/Runtime/Measures/` — evaluator-internal wrappers and arithmetic helpers that consume the shared UCUM semantic model, not their own parser

## Full UCUM Parser Architecture

### What must be built

Build a **real UCUM grammar parser**.

That means:

- atoms from the UCUM catalog,
- SI prefixes where the target atom permits them,
- multiplication via `.`,
- division via `/`,
- exponents,
- grouping with parentheses,
- annotations,
- canonicalization,
- exact dimension-vector computation,
- exact conversion metadata,
- consumer-facing diagnostics.

It does **not** mean a Tier 1 whitelist. The whitelist is the current bug.

### Authoritative data model and storage

The source of truth for UCUM atom data should be split into two layers:

1. **Authoritative source artifact** in `src/Precept/Data/Ucum/`
   - `ucum-essence.xml` or the equivalent normalized UCUM source artifact lives here.
   - This is the durable provenance record.
2. **Generated runtime catalog** in `src/Precept/Language/Ucum/`
   - a checked-in generated C# file such as `UcumAtomCatalog.g.cs` materializes the data as frozen collections.
   - Consumers do not parse XML at runtime.

That matches the direction already taken for ISO 4217: keep the external source artifact, but make the runtime use native immutable C# data.

### Core semantic types

These are the specific types the branch should add.

| File | Type | Responsibility |
|---|---|---|
| `src/Precept/Language/Ucum/DimensionVector.cs` | `readonly record struct DimensionVector` | Canonical internal dimension identity. Seven SI exponents plus helpers such as `Multiply`, `Divide`, `Pow`, `IsDimensionless`. |
| `src/Precept/Language/Ucum/UcumAtom.cs` | `sealed record UcumAtom` | One UCUM atom definition: code, print/display name, vector, exact scale, prefixability, tier, annotation class if any. |
| `src/Precept/Language/Ucum/UcumPrefix.cs` | `sealed record UcumPrefix` | One SI prefix: code, name, exact factor, order, applicability rules. |
| `src/Precept/Language/Ucum/UcumExpression.cs` | `abstract record UcumExpression` + concrete node types | Parsed AST / semantic tree for a UCUM expression. |
| `src/Precept/Language/Ucum/UcumTerm.cs` | `sealed record UcumTerm` | Resolved atom-or-group occurrence with exponent and optional annotation. |
| `src/Precept/Language/Ucum/UcumParsedUnit.cs` | `sealed record UcumParsedUnit` | Consumer boundary result: source text, canonical code, vector, exact scale, preferred dimension alias, tier usage, atom set. |
| `src/Precept/Language/Ucum/UcumDiagnostic.cs` | `sealed record UcumDiagnostic` | Parser/catalog diagnostic payload with span, code, and message. |
| `src/Precept/Language/Ucum/UcumParseResult.cs` | `readonly record struct UcumParseResult` | Success/failure wrapper returning `UcumParsedUnit` plus diagnostics. |
| `src/Precept/Language/Ucum/UcumTokenKind.cs` | `enum UcumTokenKind` | Token kinds for the unit parser. |
| `src/Precept/Language/Ucum/UcumToken.cs` | `readonly record struct UcumToken` | Lexed UCUM token with span. |
| `src/Precept/Language/Ucum/UcumLexer.cs` | `static class UcumLexer` | Tokenizes UCUM expressions. |
| `src/Precept/Language/Ucum/UcumParser.cs` | `static class UcumParser` | LL(1) parser producing `UcumParsedUnit`. |
| `src/Precept/Language/Ucum/UcumCatalog.cs` | `static class UcumCatalog` | Shared façade for parse, lookup, browse-by-tier, and atom lookup. |
| `src/Precept/Language/Ucum/UcumAtomCatalog.g.cs` | `static partial class UcumAtomCatalog` | Generated frozen atom table keyed by canonical code. |
| `src/Precept/Language/Ucum/UcumPrefixCatalog.cs` | `static class UcumPrefixCatalog` | Prefix table and longest-match helpers. |
| `src/Precept/Language/Ucum/DimensionCatalog.cs` | `static class DimensionCatalog` + `sealed record DimensionAlias` | Curated Precept dimension names mapped to vectors and business rules. |

### Exact scale representation

UCUM conversion metadata must be stored exactly, not as `double`.

`UcumAtom` should therefore carry an exact factor representation — e.g. a rational numerator/denominator pair plus base-10 exponent, or an equivalent exact form. A floating-point factor would violate the philosophy immediately by injecting approximation into an exact-looking path.

### Dimension catalog shape

`DimensionCatalog` is not the UCUM parser. It is the **friendly-name layer over vectors**.

That catalog should map names such as:

- `length`
- `mass`
- `volume`
- `area`
- `temperature`
- `energy`
- `pressure`
- `speed`
- `force`
- `count`

onto `DimensionVector` entries plus business rules.

The parser accepts valid UCUM expressions whether or not their vector has a friendly alias. The alias catalog exists for:

- `quantity of 'mass'`
- `price of 'speed'`
- `.dimension` display/classification
- autocomplete on the `dimension` type

That is the correct split: **open expression space, curated naming layer**.

### Parser result boundary

The parser must return a structured result, not `bool`.

The correct boundary is `UcumParseResult` returning `UcumParsedUnit`.

Every serious consumer needs more than validity:

- canonical code,
- dimension vector,
- exact scale,
- preferred friendly dimension name if one exists,
- tier information for tooling,
- atom/prefix breakdown for completions and hover,
- diagnostics with spans.

A `bool` throws away the very data the runtime and tooling need.

Convenience helpers such as `IsValidExpression(string)` may exist on top, but they are wrappers over the structured result.

### High-level grammar and parse logic

The grammar is LL(1). The implementation should stay that way.

High-level shape:

```text
Expression  := Product [ '/' Product ]
Product     := Factor { '.' Factor }
Factor      := Primary [ Exponent ] [ Annotation ]
Primary     := AtomOrPrefixedAtom | '(' Expression ')'
AtomOrPrefixedAtom := Atom | Prefix+Atom   // resolved by longest valid catalog match
Exponent    := signed integer suffix
Annotation  := '{' text '}'
```

Implementation notes:

- The lexer treats bracketed atoms such as `[degF]` and `mm[Hg]` components correctly; they are not naively split on punctuation.
- Prefix resolution is longest-match against prefixable atoms; `mg` is parsed as milli + gram, but non-prefixable atoms remain atomic.
- Exponents are folded into the semantic node, not left as raw text.
- `/` creates denominator terms; the semantic layer normalizes the result rather than keeping arbitrary nested slash trees.
- Grouping is preserved during parse and collapsed during semantic reduction.
- Annotations survive in the semantic model even when they do not affect the physical vector, because business consumers care about them — especially count-family units.

The parser is syntax. The semantic reducer computes:

- canonical code,
- reduced numerator/denominator terms,
- exact scale,
- dimension vector,
- alias classification.

### Where the parser is consumed

#### Compile-time validation

This is the first consumer.

1. `src/Precept/Language/Type.cs`
   - add a new `ContentValidation` subtype, `UcumValidation`.
   - `ClosedSetValidation` remains for `currency` and can remain for named `dimension` values derived from `DimensionCatalog`.
2. `src/Precept/Language/Types.cs`
   - `UnitOfMeasureValidation` becomes `UcumValidation`.
   - `DimensionValidation` derives its closed set from `DimensionCatalog.AllNames`.
3. `src/Precept/Pipeline/TypeChecker.Expressions.cs`
   - extend `ValidateContent` to dispatch `UcumValidation` through `UcumCatalog.Parse(...)`.
4. `src/Precept/Pipeline/TypeChecker.cs`
   - stop leaving `DeclaredQualifiers` empty for quantity/price/unit qualifiers.
   - resolve `in 'kg/m2'` and `of 'mass'` through the UCUM subsystem and dimension catalog during type-checking.
5. `src/Precept/Language/DeclaredQualifierMeta.cs`
   - `DeclaredQualifierMeta.Unit` must carry structured parsed-unit metadata, not just `UnitCode` + `DimensionName` strings.

This is the direct replacement for the current `ClosedSetValidation` placeholder path.

#### Language-server completions and validation

The language server should consume the shared subsystem in two distinct modes.

1. **Discovery mode**
   - `PreceptAnalyzer.GetCompletions(...)` and `PreceptDocumentIntellisense.GetCompletions(...)` enumerate Tier 1 atoms and curated dimension aliases.
   - Tier 1 is the proactive suggestion set.
2. **Validation mode**
   - when the cursor is inside a unit expression, the LS parses the in-progress text with the same UCUM parser in tolerant mode.
   - diagnostics come from the same parser; no second regex checker exists in the LS.

Practical behavior:

- after `in '`, suggest Tier 1 atoms and curated count-family codes;
- after `.`, `/`, or `(`, suggest context-valid continuations;
- after a valid partial expression, validate the remainder incrementally;
- show canonical code and display name in completion docs, but insert the canonical code only.

Colloquial names belong in `CompletionItem.Detail` / `Documentation`, not in the accepted syntax.

#### Runtime and evaluator

The evaluator consumes the parsed UCUM model for real semantics, not just validation.

Runtime-specific files should be added under `src/Precept/Runtime/Measures/`:

| File | Type | Responsibility |
|---|---|---|
| `src/Precept/Runtime/Measures/Unit.cs` | `internal sealed class Unit` | Evaluator-internal enriched unit entity built from `UcumParsedUnit`. |
| `src/Precept/Runtime/Measures/MeasureDimension.cs` | `internal readonly record struct MeasureDimension` | Runtime-facing dimension wrapper over `DimensionVector`. |
| `src/Precept/Runtime/Measures/UnitFactory.cs` | `internal static class UnitFactory` | Converts shared parsed units into interned runtime `Unit` instances. |
| `src/Precept/Runtime/Operations/QuantityOperations.cs` | `internal static class QuantityOperations` | D8 addition/subtraction/comparison/conversion/cancellation using parsed unit semantics. |
| `src/Precept/Runtime/Operations/PriceOperations.cs` | `internal static class PriceOperations` | Price × quantity / money ÷ quantity / denominator cancellation. |

Consumption rules:

- **commensurability** is `DimensionVector` equality plus count-family rules;
- **conversion** uses exact scale metadata from the parser output, never string tables;
- **`of 'mass'` filtering** uses `DimensionCatalog` alias classification over the parsed vector;
- **D8 arithmetic** converts to target unit when known, else left operand wins, using the same parsed-unit semantics on both operands;
- **compound results** are built by combining vectors and canonical codes, not by string concatenation.

#### MCP tools

MCP should project canonical UCUM structure, not just source strings.

Required changes:

- `tools/Precept.Mcp/Dtos/CompileToolDtos.cs`
  - replace the flat qualifier string with structured qualifier DTOs for unit/dimension data.
- `tools/Precept.Mcp/Tools/CompileTool.cs`
  - emit canonical code, preferred dimension name, and raw authored text where relevant.
- `precept_inspect`
  - when it lands, surface parsed-unit metadata on quantities, prices, and dimension-based availability/validation results.

`precept_compile` and `precept_inspect` should show:

- authored text,
- canonical unit code,
- friendly dimension alias when one exists,
- dimension vector or equivalent structured identity,
- validation failures from the shared parser.

That keeps MCP an honest thin wrapper over core semantics.

#### Runtime input-argument validation

Runtime boundary validation must also use the shared subsystem.

The validation points are:

- `PreceptValue.FromJson(...)`
- `PreceptValue.FromClr<T>(...)`
- the JSON lane for `Create`, `Fire`, `Update`, and `Restore`
- the typed lane builders behind `IArgBuilder` and `IFieldBuilder`

If a caller provides:

- `unitofmeasure` → parse as a UCUM expression through `UcumCatalog.Parse(...)`
- `quantity` → parse the magnitude, then parse the unit segment through `UcumCatalog.Parse(...)`
- `price` → parse currency separately, then parse the denominator unit segment through `UcumCatalog.Parse(...)`

The runtime rejects bad input before evaluator execution. There is no scenario where tooling accepts a unit string the runtime then treats as opaque text.

## Resolved Architectural Calls (Q2–Q6)

### Q2 — `time` is not a UCUM dimension in Precept

Remove `time` from the UCUM `RecognizedDimensions` replacement.

`quantity of 'time'` is **not** a valid Precept concept. Time quantities belong to the temporal partition:

- elapsed time → `duration`
- calendar distance → `period`

If an author wants "seconds" or "hours," they model time with temporal types, not `quantity`. The compiler should emit a teachable diagnostic telling them to use `duration` or `period` instead of `quantity of 'time'`.

This keeps the UCUM partition honest and preserves the NodaTime boundary already locked by the language docs.

### Q3 — `count` stays, but as a business alias over annotated dimensionless units

`count` remains a named Precept dimension family.

But it is **not** a physical SI dimension. It is a business-domain alias over dimensionless UCUM expressions carrying approved count annotations.

Architectural rule:

- `quantity of 'count'` is valid;
- count-family units classify to the `count` alias;
- count-family units do **not** auto-convert merely because they share the alias;
- addition/subtraction/comparison still require the same canonical unit unless an explicit conversion factor is authored elsewhere.

That gives authors the right category surface without lying about physics.

### Q4 — `speed` and `force` belong in `DimensionCatalog` now

Keep both.

Once the parser is real, derived-dimension aliases are no longer speculative. They are simply curated names over vectors:

- `speed` = length / time
- `force` = mass × length / time²

They should be present in `DimensionCatalog` immediately as first-class aliases used by:

- `quantity of 'speed'`
- `price of 'force'`
- `.dimension` classification
- completions and hover

The catalog stays curated. The parser remains open. That is the right division of responsibility.

### Q5 — parser scope is settled

Build the parser.

Tier 1 is discovery only. Acceptance is grammar-driven. No further branch energy should be spent defending the placeholder.

### Q6 — colloquial names are display-only

Validation and canonical storage use UCUM codes only.

Language-server and MCP presentation may show:

- human-readable names,
- synonyms,
- industry examples,
- migration hints from colloquial forms,

but the accepted authored text is the canonical code. That keeps the compiler, runtime, and persisted values structurally consistent.

## Required File Changes on This Branch

### Existing files to modify

- `src/Precept/Language/Type.cs`
  - add `UcumValidation` to the `ContentValidation` DU.
- `src/Precept/Language/Types.cs`
  - remove `RecognizedUnits` closed-set validation;
  - derive `DimensionValidation` from `DimensionCatalog`;
  - update examples and hover text to canonical UCUM codes.
- `src/Precept/Language/DeclaredQualifierMeta.cs`
  - promote unit qualifiers from raw strings to structured parsed-unit data.
- `src/Precept/Pipeline/TypeChecker.Expressions.cs`
  - route typed-constant unit validation through the parser.
- `src/Precept/Pipeline/TypeChecker.cs`
  - actually resolve and store quantity/price/unit qualifiers.
- `tools/Precept.LanguageServer/LanguageServerStubs.cs`
  - completions and diagnostics consume the shared UCUM subsystem.
- `tools/Precept.Mcp/Tools/CompileTool.cs`
  - emit structured unit metadata.
- `tools/Precept.Mcp/Dtos/CompileToolDtos.cs`
  - add structured qualifier/unit DTOs.

### New files to add

- `src/Precept/Data/Ucum/ucum-essence.xml`
- `src/Precept/Language/Ucum/DimensionVector.cs`
- `src/Precept/Language/Ucum/UcumAtom.cs`
- `src/Precept/Language/Ucum/UcumPrefix.cs`
- `src/Precept/Language/Ucum/UcumExpression.cs`
- `src/Precept/Language/Ucum/UcumTerm.cs`
- `src/Precept/Language/Ucum/UcumParsedUnit.cs`
- `src/Precept/Language/Ucum/UcumDiagnostic.cs`
- `src/Precept/Language/Ucum/UcumParseResult.cs`
- `src/Precept/Language/Ucum/UcumTokenKind.cs`
- `src/Precept/Language/Ucum/UcumToken.cs`
- `src/Precept/Language/Ucum/UcumLexer.cs`
- `src/Precept/Language/Ucum/UcumParser.cs`
- `src/Precept/Language/Ucum/UcumCatalog.cs`
- `src/Precept/Language/Ucum/UcumAtomCatalog.g.cs`
- `src/Precept/Language/Ucum/UcumPrefixCatalog.cs`
- `src/Precept/Language/Ucum/DimensionCatalog.cs`
- `src/Precept/Runtime/Measures/Unit.cs`
- `src/Precept/Runtime/Measures/MeasureDimension.cs`
- `src/Precept/Runtime/Measures/UnitFactory.cs`
- `src/Precept/Runtime/Operations/QuantityOperations.cs`
- `src/Precept/Runtime/Operations/PriceOperations.cs`

## Bottom Line

The gap is no longer "how much Tier 1 can we get away with." The gap is that the branch still has a placeholder where the language contract requires a parser. The correct architecture is a shared UCUM subsystem in `src/Precept/Language/Ucum/`, authoritative data in `src/Precept/Data/Ucum/`, curated dimension aliases over open vector semantics, and one parse result consumed uniformly by compile-time validation, runtime enforcement, LS tooling, and MCP output.

That is the build.
