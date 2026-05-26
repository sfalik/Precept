# Precept `precept_language` MCP audit

## Scope

This audit compares the authoritative language metadata in `src/Precept/Language/` with the actual `precept_language` MCP payload produced by `tools/Precept.Mcp/Tools/LanguageTool.cs`, then evaluates the result from the perspective of an AI agent that must write correct Precept on the first attempt.

Primary implementation references:

- `tools/Precept.Mcp/Tools/LanguageTool.cs:22-45` — actual top-level payload assembly
- `tools/Precept.Mcp/Dtos/LanguageToolDtos.cs:3-278` — actual JSON response shape
- `docs/tooling/mcp.md:181-226,823-824,969-977` — current MCP implementation doc
- `docs/language/catalog-system.md:45-60,1933-1965` — architecture claim that MCP should serialize all 13 catalogs plus `SyntaxReference`
- `docs/runtime/fault-system.md:66-70` — architecture claim that MCP should expose `Faults.All`

## Executive summary

The current tool is not yet sufficient as the sole grounding surface for first-try Precept authoring. It exposes 9 language catalogs (`tokens`, `types`, `modifiers`, `actions`, `constructs`, `constraints`, `operators`, `functions`, `diagnostics`), 4 domain registries, and a hardcoded `firePipeline` array (`LanguageTool.cs:24-45`; `LanguageToolDtos.cs:3-14`). It does **not** expose `Operations`, `ExpressionForms`, `ProofRequirements`, `Outcomes`, `Faults`, or `SyntaxReference`, even though the architecture docs explicitly say MCP should serialize all 13 catalogs plus `SyntaxReference` (`docs/language/catalog-system.md:1952-1965`) and faults (`docs/runtime/fault-system.md:66-70`).

Worse, several authoring-critical fields already exist in catalog metadata but are dropped during serialization: `TypeMeta.ContentValidation`, `TypeMeta.UsageExample`, `TypeMeta.HoverDescription`, `TypeMeta.NotemptyApplicable`, accessor/function/action proof requirements, function/action/modifier snippet templates, and operator/type/function usage examples. The payload is therefore simultaneously **too large** for routine use and **missing the exact guidance an AI author needs most**.

## Part 1 — Catalogs vs. actual `precept_language` exposure

### Actual top-level response today

`LanguageReferenceDto` defines exactly 11 top-level keys (`LanguageToolDtos.cs:3-14`):

- `tokens`
- `types`
- `modifiers`
- `actions`
- `constructs`
- `constraints`
- `operators`
- `functions`
- `diagnostics`
- `domains`
- `firePipeline`

That shape matches `LanguageTool.Language()` exactly (`LanguageTool.cs:24-45`). There is no room in the DTO for `operations`, `expressionForms`, `outcomes`, `proofRequirements`, `faults`, or `syntaxReference`.

### 1. Tokens

**Source metadata**

`TokenMeta` carries `Kind`, `Text`, `Categories`, `Description`, `TextMateScope`, `SemanticTokenType`, `ValidAfter`, `IsAccessModeAdjective`, `IsValidAsMemberName`, and `IsMessagePosition` (`src/Precept/Language/Token.cs:35-73`). `Tokens` also derives lexer-oriented indexes such as `Keywords`, `TwoCharOperators`, `SingleCharOperators`, `PunctuationChars`, `TwoCharOperatorStarters`, and `AccessModeKeywords` (`src/Precept/Language/Tokens.cs:433-510`).

**Exposed**

`MapToken` serializes every field on `TokenMeta` (`LanguageTool.cs:47-58`). This is one of the few sections with essentially complete entry coverage.

**Not exposed / authoring gap**

The derived lexer tables are not exposed. That is acceptable for authoring; an AI writer does not need raw lexer indexes if it has the token metadata. Tokens are not the problem.

### 2. Types

**Source metadata**

`TypeMeta` carries `Kind`, `Token`, `Description`, `Category`, `DisplayName`, `QualifierShape`, `Traits`, `WidensTo`, `ImpliedModifiers`, `Accessors`, `HoverDescription`, `UsageExample`, `NotemptyApplicable`, `ChoiceLiteralTokens`, and `ContentValidation` (`src/Precept/Language/Type.cs:183-216`).

`TypeAccessor` also carries per-accessor `ProofRequirements` (`src/Precept/Language/Type.cs:77-87`), and the concrete type catalog defines substantial authoring guidance:

- qualifier shapes (`src/Precept/Language/Types.cs:26-50`)
- typed-constant/content-validation metadata with examples (`src/Precept/Language/Types.cs:58-136`)
- collection accessor guard requirements such as `count > 0` for `.peek`, `.min`, `.first`, `.last`, etc. (`src/Precept/Language/Types.cs:143-230`)
- per-type usage examples and hover docs throughout the catalog (`src/Precept/Language/Types.cs:291-701`)

**Exposed**

`MapType` serializes: `Kind`, keyword text, `Description`, `Category`, `DisplayName`, expanded `Traits`, `WidensTo`, `ImpliedModifiers`, `QualifierShape`, `Accessors`, and `ChoiceLiteralTokens` (`LanguageTool.cs:60-72`). `MapAccessor` exposes accessor name, description, return shape, parameter type, required traits, and qualifier return axis (`LanguageTool.cs:81-111`).

**Not exposed / authoring gap**

The tool drops all of the following:

- `HoverDescription`
- `UsageExample`
- `NotemptyApplicable`
- `ContentValidation`
- `TypeAccessor.ProofRequirements`

This is a major AI-authoring failure.

Concrete impact:

- The type system knows literal formats and examples for `date`, `time`, `datetime`, `instant`, `timezone`, `period`, `duration`, `currency`, `unitofmeasure`, `dimension`, `money`, `quantity`, `price`, and `exchangerate` (`Types.cs:58-136`), but the MCP tool withholds that metadata.
- The type system knows that `lookup` is **not** `notempty`-applicable (`Types.cs:693-701` and `TypeMeta.NotemptyApplicable` at `Type.cs:196`), but the tool withholds that too.
- The type system knows which accessors require guards, e.g. collection `.peek`, `.first`, `.last`, `.min`, `.max`, `.at(...)` (`Types.cs:146-205`), but the MCP tool only exposes the accessor names, not the proof obligations.

For an AI trying to write correct Precept, this section is under-serialized.

### 3. Operators

**Source metadata**

`OperatorMeta` carries `Kind`, `Description`, `Arity`, `Associativity`, `Precedence`, `Family`, `IsKeywordOperator`, `HoverDescription`, and `UsageExample` (`src/Precept/Language/Operator.cs:27-36`).

**Exposed**

`MapOperator` emits `Kind`, rendered text/tokens, `Arity`, `Associativity`, `Precedence`, `Family`, `IsKeywordOperator`, and `Description` (`LanguageTool.cs:227-246`).

**Not exposed / authoring gap**

The tool drops:

- `HoverDescription`
- `UsageExample`

That omission matters, but the larger problem is structural: operators without the `Operations` catalog do not tell an AI which type combinations are legal, what result type they produce, or what qualifier/proof rules apply. `operators` tells the model what `+` means syntactically; it does **not** tell the model whether `money / money` yields `decimal` or `exchangerate`, or whether `quantity + quantity` requires matching units.

### 4. Actions

**Source metadata**

`ActionMeta` carries `Kind`, `Token`, `Description`, `ApplicableTo`, `SyntaxShape`, `ValueRequired`, `IntoSupported`, `ProofRequirements`, `AllowedIn`, `HoverDescription`, `UsageExample`, `SnippetTemplate`, and `PrimaryActionKind` (`src/Precept/Language/Action.cs:7-27`).

**Exposed**

`MapAction` emits `Kind`, keyword, `Description`, `ApplicableTo`, `AllowedIn`, `SyntaxShape`, `ValueRequired`, `IntoSupported`, and `PrimaryActionKind` (`LanguageTool.cs:167-177`).

**Not exposed / authoring gap**

The tool drops:

- `ProofRequirements`
- `HoverDescription`
- `UsageExample`
- `SnippetTemplate`

This is authoring-critical. The action catalog already knows, for example, that `dequeue`, `pop`, `insert`, `remove ... at`, and `dequeue ... by` carry guard requirements (`src/Precept/Language/Actions.cs:92-103,110-120,145-170,189-201`). The MCP surface hides those obligations, so the AI sees the verb but not the conditions needed to use it safely and idiomatically.

### 5. Modifiers

**Source metadata**

`ModifierMeta` and its DU subtypes carry:

- base: `Kind`, `Token`, `Description`, `Category`, `DesugarsToRule`, `MutuallyExclusiveWith` (`src/Precept/Language/Modifier.cs:89-113`)
- field modifiers: `ApplicableTo`, `HasValue`, `Subsumes`, `ProofSatisfactions`, `HoverDescription`, `UsageExample`, `SnippetTemplate` (`src/Precept/Language/Modifier.cs:116-135`)
- state modifiers: `AllowsOutgoing`, `RequiresDominator`, `PreventsBackEdge` (`src/Precept/Language/Modifier.cs:138-148`)
- event modifiers: `RequiredAnalysis` (`src/Precept/Language/Modifier.cs:151-158`)
- access modifiers: `IsPresent`, `IsWritable` (`src/Precept/Language/Modifier.cs:161-170`)
- anchor modifiers: `Scope`, `Target` (`src/Precept/Language/Modifier.cs:173-181`)

**Exposed**

`LanguageTool` serializes most structural fields for each subtype (`LanguageTool.cs:113-165`).

**Not exposed / authoring gap**

For **field modifiers**, the tool drops:

- `ProofSatisfactions`
- `HoverDescription`
- `UsageExample`
- `SnippetTemplate`

This matters because the field modifier catalog already encodes why modifiers are useful and what they prove. For example, `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, and `maxcount` carry proof-satisfaction metadata (`src/Precept/Language/Modifiers.cs:61-211`). The tool tells an AI where a modifier is legal, but not what downstream proofs it discharges or how to use it idiomatically.

State/event/access/anchor coverage is closer to complete, but field modifiers — the most common authoring surface — are under-described.

### 6. Constructs

**Source metadata**

`ConstructMeta` carries `Kind`, `Name`, `Description`, `UsageExample`, `AllowedIn`, `Slots`, `Entries`, `RoutingFamily`, `SnippetTemplate`, and `ModifierDomain` (`src/Precept/Language/Construct.cs:8-35`). Slot and disambiguation metadata live in `ConstructSlot` (`src/Precept/Language/ConstructSlot.cs:31-35`) and `DisambiguationEntry` (`src/Precept/Language/DisambiguationEntry.cs:11-14`).

**Exposed**

`MapConstruct`, `MapSlot`, and `MapEntry` serialize this catalog well (`LanguageTool.cs:179-204`). This is the strongest current section for authoring.

**Not exposed / authoring gap**

The missing pieces are not entry fields but companion structure:

- `Constructs.ByLeadingToken` and `Constructs.LeadingTokens` (`src/Precept/Language/Constructs.cs:186-195`) are not surfaced.
- There is no higher-level declaration grammar object showing how constructs compose into a whole file.

Still, compared to the other sections, `constructs` is in decent shape.

### 7. Constraints

**Source metadata**

`ConstraintMeta` is a DU with `Invariant`, `StateResident`, `StateEntry`, `StateExit`, and `EventPrecondition`, plus the intermediate `StateAnchored` layer carrying `LeadingToken` (`src/Precept/Language/Constraint.cs:12-41`).

**Exposed**

`MapConstraint` serializes `Kind`, `Description`, a synthesized `Scope`, and the relevant token sequence (`LanguageTool.cs:206-225`).

**Not exposed / authoring gap**

This section is mostly adequate, but still lacks:

- subtype identity beyond flattened `scope`
- examples/snippets
- any compatibility or usage guidance beyond prose description

For AI authoring, constraints are less of a problem than types/actions/operations/outcomes.

### 8. Functions

**Source metadata**

`FunctionMeta` carries `Kind`, `Name`, `Description`, `Overloads`, `Category`, `UsageExample`, `SnippetTemplate`, `HoverDescription`, `HasCIVariant`, `CIVariantOf`, `IsMessagePosition`, and `CIDiagnosticCode` (`src/Precept/Language/Function.cs:32-49`). Each overload carries `Parameters`, `ReturnType`, `QualifierMatch`, and `ProofRequirements` (`src/Precept/Language/Function.cs:18-26`).

**Exposed**

`MapFunction` and `MapOverload` emit `Kind`, `Name`, `Category`, `Description`, overload parameter types, return type, qualifier match, CI linkage, and CI diagnostic code (`LanguageTool.cs:248-264`).

**Not exposed / authoring gap**

The tool drops:

- `UsageExample`
- `SnippetTemplate`
- `HoverDescription`
- `IsMessagePosition`
- `FunctionOverload.ProofRequirements`

This is unnecessary self-sabotage: the function catalog already contains excellent examples (`src/Precept/Language/Functions.cs:41-306`), and some overloads already declare proof obligations such as non-negative exponents and non-negative square-root inputs (`Functions.cs:167-193`). The MCP surface withholds precisely the guidance an AI author would use to avoid trial-and-error.

### 9. Diagnostics

**Source metadata**

`DiagnosticMeta` carries `Code`, `Stage`, `Severity`, `MessageTemplate`, `Category`, `RelatedCodes`, `FixHint`, `PreventsFault`, and `SuggestionSources` (`src/Precept/Language/Diagnostics.cs:23-33`).

**Exposed**

`MapDiagnostic` serializes that shape essentially completely (`LanguageTool.cs:265-275`).

**Not exposed / authoring gap**

The entry fields are present, but the catalog itself does not encode:

- trigger conditions in structured form
- bad/good examples
- recovery examples
- “what authoring decision avoids this” guidance

So diagnostics are complete as a registry, but incomplete as proactive authoring guidance.

### 10. Operations

**Source metadata**

`OperationMeta` is the typed-legality hub. It carries operator kind, operand types, result type, description, bidirectional lookup, `QualifierMatch`, `ProofRequirements`, and CI linkage (`src/Precept/Language/Operation.cs:25-57`). The concrete catalog contains 198 operations (`docs/language/catalog-system.md:536`) and indexes them for lookup (`src/Precept/Language/Operations.cs:1098-1161`).

**Exposed**

Nothing. `LanguageTool.Language()` never touches `Operations.All` (`LanguageTool.cs:24-45`). There is no DTO for operations (`LanguageToolDtos.cs:3-278`).

**Not exposed / authoring gap**

This is the single biggest missing catalog for AI authoring.

Without `operations`, an AI cannot know:

- which operator/type combinations are legal
- what result type each expression produces
- when qualifiers must match vs differ
- when division or other forms require explicit proof/guard conditions
- when bidirectional lookup exists
- when case-insensitive variants require specialized diagnostics

Examples already encoded in the catalog but hidden from MCP include:

- `money / money` same currency -> `decimal`; different currency -> `exchangerate` (`src/Precept/Language/Operations.cs:422-440`)
- `quantity + quantity` requires matching unit qualifiers (`Operations.cs:470-486`)
- `price * quantity` -> `money` (`Operations.cs:574-576`)
- `exchangeRate * money` -> `money` (`Operations.cs:600-602`)

This is exactly the knowledge a first-try AI author needs.

### 11. ProofRequirements

**Source metadata**

`ProofRequirementMeta` covers the five obligation kinds — numeric, presence, dimension, modifier, qualifier compatibility (`src/Precept/Language/ProofRequirement.cs:129-160`; `src/Precept/Language/ProofRequirements.cs:13-29`). The instance hierarchy also defines concrete payloads used inside actions/functions/accessors/operations (`src/Precept/Language/ProofRequirement.cs:42-117`).

**Exposed**

Nothing. `precept_language` does not serialize the proof-requirement catalog or any attached proof-requirement instances.

**Not exposed / authoring gap**

This omission blocks an AI from understanding proactive guard writing. The language knows the difference between:

- “must be present”
- “must be non-zero”
- “must have matching qualifier axis”
- “must carry modifier X”

The tool hides all of it.

### 12. Outcomes

**Source metadata**

`OutcomeMeta` carries `Kind`, `LeadingToken`, `ArgumentKind`, `ParsedSubtype`, `Description`, and `Example` (`src/Precept/Language/Outcomes.cs:47-53`). The catalog is closed and tiny: `transition`, `no transition`, `reject` (`Outcomes.cs:65-89,99-125`; `docs/language/catalog-system.md:541`).

**Exposed**

Nothing.

**Not exposed / authoring gap**

This is a direct transition-authoring gap. `constructs` exposes that a transition row ends with an `Outcome` slot (`src/Precept/Language/Constructs.cs:96-104`), but MCP never defines the outcome vocabulary itself. An AI must infer that `-> no transition` is a two-token form, that `reject` requires a string literal, and that `transition` requires an identifier.

### 13. ExpressionForms

**Source metadata**

`ExpressionFormMeta` carries `Kind`, `Category`, `IsLeftDenotation`, `LeadTokens`, `HoverDocs`, and optional `BindingPower` (`src/Precept/Language/ExpressionForms.cs:64-70`). The catalog covers literals, identifiers, grouped expressions, unary/binary ops, member access, conditional expressions, function/method calls, list literals, postfix presence tests, quantifiers, CI function calls, and interpolated strings (`ExpressionForms.cs:81-114`).

**Exposed**

Nothing.

**Not exposed / authoring gap**

For AI authoring this matters because expression syntax is more than operators. The model needs to know that `if/then/else`, quantifiers, method calls, presence checks, interpolated strings, and list literals are all first-class expression forms.

### 14. Faults

**Source metadata**

`FaultMeta` carries `Code`, `MessageTemplate`, `Severity`, and `RecoveryHint` (`src/Precept/Language/Faults.cs:3-8`). `Faults.All` is the full registry (`Faults.cs:52-53`).

**Exposed**

Nothing.

**Not exposed / authoring gap**

For authoring, faults are secondary to operations/proofs, but they are still AI-first-relevant. The architecture docs explicitly say MCP should expose faults (`docs/runtime/fault-system.md:66-70`), and `DiagnosticMeta.PreventsFault` already points back to them (`src/Precept/Language/Diagnostics.cs:29-32`). That prevention chain is incomplete in the current tool.

### 15. Domain registries

#### CurrencyCatalog

`CurrencyEntry` contains `AlphaCode`, `NumericCode`, `Name`, `MinorUnit`, and `Symbol` (`src/Precept/Language/CurrencyCatalog.cs:8-14`). `MapCurrency` serializes all of it (`LanguageTool.cs:277-278`). Coverage is good.

#### DimensionCatalog

`DimensionAlias` contains `Name`, `Vector`, and `Description` (`src/Precept/Language/Ucum/DimensionCatalog.cs:7-8`). `MapDimension` serializes all of it (`LanguageTool.cs:290-291`). Coverage is good.

#### TemporalUnits

`TemporalUnitEntry` contains `Singular`, `Plural`, `IsCalendarBased`, factories, plus derived `Names`, `IsPeriod`, and `IsDuration` (`src/Precept/Language/Time/TemporalUnits.cs:8-18`). `MapTemporalUnit` exposes `Singular`, `Plural`, `IsCalendarBased`, `IsPeriod`, and `IsDuration` (`LanguageTool.cs:293-294`). Good enough for authoring.

#### UCUM

`UcumAtom` contains `Code`, `Name`, `Vector`, `Scale`, `Prefixable`, and `AnnotationClass` (`src/Precept/Language/Ucum/UcumAtom.cs:3-9`). `MapUcumTier1Unit` serializes those fields plus resolved dimension name (`LanguageTool.cs:280-288`).

The problem is **scope**: `LanguageTool` serializes `UcumAtomCatalog.BrowseTier1()` only (`LanguageTool.cs:41-44`), while the validator accepts the full UCUM grammar and prefixes. `UcumPrefixCatalog.All` exists (`src/Precept/Language/Ucum/UcumPrefixCatalog.cs:5-46`) but is not surfaced. That means the tool provides a 150-entry browse subset, but not the full atom set, not prefixes, and not an explicit UCUM grammar descriptor.

For authoring `quantity`, `price`, and `unitofmeasure`, this is a material gap.

### 16. `SyntaxReference` (not a catalog, but architecturally critical)

`SyntaxReference` carries global grammar facts: `GrammarModel`, comment/identifier/string/number rules, `NullNarrowing`, `TypedConstantRules`, `ExpressionRules`, `PrecedenceTable`, `CommonPatterns`, and `ConventionalOrder` (`src/Precept/Language/SyntaxReference.cs:17-179`).

The catalog-system architecture explicitly says MCP should serialize it as `syntaxReference` (`docs/language/catalog-system.md:1933-1942,1952-1965`). The current DTO and implementation omit it completely (`LanguageToolDtos.cs:3-14`; `LanguageTool.cs:24-45`).

This is the highest-leverage missing authoring surface because it contains exactly the file-wide rules an AI needs.

One caution: `SyntaxReference.CommonPatterns` is not yet entirely safe to ship blindly. Its “Computed field” example currently uses `->` (`src/Precept/Language/SyntaxReference.cs:121-127`), but the compiler accepts `<-` and rejects `->` (`precept_compile` probe: `field B as number <- A + 1` succeeds; `field B as number -> A + 1` fails with `PRE0009`). If `SyntaxReference` is exposed, it must first be compile-validated.

### 17. Spec / architecture drift

There are now three different truths in the repo:

1. **Implementation truth** — `LanguageTool.cs` exposes 9 catalogs + domains + `firePipeline`.
2. **MCP doc truth** — `docs/tooling/mcp.md` describes that same limited implemented surface (`docs/tooling/mcp.md:181-226,823-824,969-975`).
3. **Architecture truth** — `docs/language/catalog-system.md` and `docs/runtime/fault-system.md` say MCP should expose all 13 catalogs + `SyntaxReference` + faults (`docs/language/catalog-system.md:1937,1952-1965`; `docs/runtime/fault-system.md:66-70`).

The architecture docs are closer to what an AI-first product actually needs. The implementation is materially behind that bar.

## Part 2 — What an AI writing agent still needs

### 1. Syntax patterns

Current state:

- `constructs` does provide one `UsageExample` per construct (`LanguageTool.cs:179-191`; `src/Precept/Language/Constructs.cs:41-165`).
- But `types`, `functions`, `actions`, `operators`, and field modifiers also contain examples/snippets in source and MCP drops them.

What is still missing for first-try authoring:

- canonical field/type examples (`field TotalCost as money in 'USD'`)
- canonical action examples (`-> dequeue Queue into Item`)
- canonical modifier examples (`field Notes as string optional maxlength 200`)
- canonical function call examples (`round(amount, 2)`, `pow(base, 2)`)
- canonical outcome examples (`-> no transition`, `-> reject "..."`)
- compile-verified multi-line patterns, not just isolated one-liners

### 2. Co-occurrence rules

Current state:

- Some relationships are exposed: modifier `ApplicableTo`, action `ApplicableTo`, action `AllowedIn`, type `QualifierShape`, construct `AllowedIn`.

What is still missing:

- operator × type legality (`Operations` missing)
- accessor × guard requirements (`TypeAccessor.ProofRequirements` missing)
- function overload proof requirements (`FunctionOverload.ProofRequirements` missing)
- action proof requirements (`ActionMeta.ProofRequirements` missing)
- modifier proof satisfactions (`FieldModifierMeta.ProofSatisfactions` missing)

So the AI gets fragments of co-occurrence, but not the parts that matter most for correctness.

### 3. Required vs. optional elements

The tool never states the minimum valid precept shape.

Compiler probes show:

- `precept Minimal` is valid and produces an empty stateless definition.
- A field-only precept is valid.

That is exactly the sort of authoring baseline the tool should say explicitly. Today the model must discover it experimentally.

### 4. Ordering rules

`SyntaxReference.ConventionalOrder` exists (`src/Precept/Language/SyntaxReference.cs:166-178`) but is not exposed.

Compiler probes also show declaration order is not semantically required: a transition row compiled successfully before the referenced `state` and `event` declarations. That means Precept currently has a **conventional order** but not a strict declaration-order rule. The tool should state that explicitly. Right now it states neither.

### 5. Type × constraint compatibility

This is only partially surfaced.

- Field modifier applicability is exposed.
- But `TypeMeta.NotemptyApplicable` is hidden, so AI cannot know that `lookup` does not accept `notempty` (`src/Precept/Language/Types.cs:693-701`).
- Typed-literal/content-validation compatibility is hidden.
- Operator/type compatibility is hidden because `Operations` is absent.

This is not enough for a first-shot author.

### 6. Common patterns / idioms

The language already has a place for this: `SyntaxReference.CommonPatterns` (`src/Precept/Language/SyntaxReference.cs:107-164`). MCP hides it.

That means the current payload contains a lot of low-level metadata but almost no explicit idioms such as:

- guarded transition with reject fallback
- computed field cluster
- collection membership gate
- stateless validation-only precept

Those patterns are exactly what an AI would reuse.

### 7. Error-avoidance guidance

Diagnostics are useful, but mostly reactive. The tool exposes message templates and fix hints, not structured “this fires when…” guidance.

Examples of still-missing proactive guidance:

- `omit` cannot take a `when` guard (`docs` encoded as diagnostic only)
- event handlers cannot take `when` guards
- `in` and `of` are mutually exclusive except for `price`
- computed fields use `<-`, not `->`

An AI author should not have to learn those through failure.

### 8. Domain type guidance

This is the sharpest practical gap after `Operations`.

The type catalog already knows literal formats and examples (`Types.cs:58-136`), but MCP withholds them. The domain registries provide currencies and dimensions, but not the typed-constant validation model that explains how to use them. UCUM is especially underpowered: a 150-entry browse list is not enough when the grammar accepts prefixed and compound expressions, and the prefix catalog is hidden.

### 9. Scope / visibility rules

The tool only exposes fragments:

- construct nesting (`AllowedIn`)
- token follow sets (`ValidAfter`)
- diagnostic suggestion sources

It does **not** expose a direct authoring matrix for:

- what names are visible in field defaults vs rules vs ensures vs transition actions
- when event arguments are in scope
- whether state names/events/fields are globally referenceable regardless of declaration order

Again: the compiler knows this, but the authoring tool does not explain it.

### 10. Transition syntax completeness

Today an AI can get close, but not all the way, from MCP alone:

- `constructs` tells it a transition row has state target, event target, optional guard, action chain, and outcome slot.
- `actions` tells it what actions exist.
- `constructs` does **not** define the actual outcome vocabulary; `outcomes` is missing.
- `SyntaxReference` is missing, so there is no full row pattern narrative.

So the model still has to infer too much.

## Part 3 — Size and navigability

## Observed size profile

The tool call was reported as roughly 192 KB by the MCP harness, and the saved raw JSON in this environment was roughly 308 KB of text. Either way, the payload is large enough to be expensive in repeated authoring loops.

Approximate section sizes after reserialization:

| Section | Count | Approx chars | Approx tokens |
|---|---:|---:|---:|
| `domains` | 4 sub-sections | ~57,060 | ~14,265 |
| `tokens` | 139 | ~46,434 | ~11,608 |
| `diagnostics` | 116 | ~36,277 | ~9,069 |
| `types` | 32 | ~21,408 | ~5,352 |
| `functions` | 23 | ~9,333 | ~2,333 |
| `modifiers` | 29 total | ~8,829 | ~2,207 |
| `constructs` | 12 | ~7,009 | ~1,752 |
| `actions` | 15 | ~5,284 | ~1,321 |
| `operators` | 21 | ~4,151 | ~1,038 |
| `constraints` | 5 | ~627 | ~157 |
| `firePipeline` | 7 | ~132 | ~33 |

Within `domains`, the main cost center is `ucumTier1Units` alone (~40,525 chars / ~10,131 tokens).

### Is this realistic for a model to consume in one call?

Only for top-tier models, and only if the task is almost entirely “understand the language.” For routine iterative authoring, this is too expensive. A serious agent will need to call the tool more than once per session; spending tens of thousands of tokens every time is gratuitous.

### Is the information organized for fast AI navigation?

Only at the first level. The top-level grouping is sensible, but most sections are flat arrays that require linear scan. There is no section filter, no query parameter, no authoring profile, no “give me the `money` type and related operations” affordance, and no fast path for the 20% of data that answers 80% of authoring questions.

### What is high-priority for writing vs. low-priority?

**High-priority authoring data**

- `syntaxReference`
- `constructs`
- `outcomes`
- `types` with qualifiers, content validation, examples, accessors, and guard requirements
- `modifiers` with applicability, proof effects, and examples
- `actions` with syntax, applicability, proof requirements, and examples
- `functions` with overloads, proof requirements, and examples
- `operations`
- minimal domain vocabularies needed for qualifiers and typed constants

**Lower-priority / tooling-oriented data**

- `textMateScope`
- `semanticTokenType`
- many `ValidAfter` details
- full diagnostic registry on every call
- full UCUM Tier 1 browse data on every call
- `firePipeline` for authoring use cases

### What should a fast path look like?

A fast-path authoring response should be opinionated and compact:

1. `syntaxReference`
2. `minimumViablePrecepts`
3. `constructs` with compile-verified examples
4. `outcomes`
5. `types` including qualifiers, content validation, examples, accessors, and `notemptyApplicable`
6. `modifiers`, `actions`, and `functions` including proof requirements and examples
7. `operations` as the operator/type legality table
8. targeted domain subsets only when requested

That gets an AI from zero to authoring. The current payload instead spends enormous space on token/tooling metadata and a large UCUM browse list while omitting the authoring-critical legality and example data.

## Part 4 — Recommendations

### 1. Add the missing high-value metadata already present in source

#### Add `syntaxReference`

Serialize `SyntaxReference` from `src/Precept/Language/SyntaxReference.cs:17-179` into a new top-level `syntaxReference` object. Proposed shape:

```json
{
  "syntaxReference": {
    "grammarModel": "line-oriented",
    "commentSyntax": "# to end of line",
    "identifierRules": "...",
    "stringLiteralRules": "...",
    "numberLiteralRules": "...",
    "whitespaceRules": "...",
    "nullNarrowing": "...",
    "typedConstantRules": "...",
    "expressionRules": "...",
    "precedenceTable": ["..."],
    "commonPatterns": [
      { "name": "Guarded transition", "description": "...", "dslSnippet": "..." }
    ],
    "conventionalOrder": ["header", "fields", "rules", "states", "..."]
  }
}
```

But fix the invalid computed-field pattern first (`SyntaxReference.cs:121-127`).

#### Expand `types`

`TypeMeta` already has the missing authoring fields (`src/Precept/Language/Type.cs:183-216`). Add them:

- `hoverDescription`
- `usageExample`
- `notemptyApplicable`
- `contentValidation`

Proposed `contentValidation` shape:

```json
{
  "kind": "NodaTime|ClosedSet|Regex|Ucum|Money|Quantity|Price|ExchangeRate",
  "formatDescription": "...",
  "examples": ["..."],
  "nodaTimePattern": "...",
  "literalKind": "...",
  "setName": "...",
  "allowedValues": ["..."]
}
```

Also add `proofRequirements` to `TypeAccessorDto` so the AI can know when accessors require guards.

#### Expand `actions`

Add the existing `ActionMeta` fields currently discarded (`src/Precept/Language/Action.cs:7-27`; `LanguageTool.cs:167-177`):

- `proofRequirements`
- `hoverDescription`
- `usageExample`
- `snippetTemplate`

#### Expand field modifiers

Add the missing `FieldModifierMeta` fields (`src/Precept/Language/Modifier.cs:116-135`):

- `proofSatisfactions`
- `hoverDescription`
- `usageExample`
- `snippetTemplate`

The proof-satisfaction payload matters because it tells the AI which modifiers discharge which obligations.

#### Expand `functions`

Add the missing `FunctionMeta` and `FunctionOverload` fields (`src/Precept/Language/Function.cs:18-49`):

- `usageExample`
- `snippetTemplate`
- `hoverDescription`
- `isMessagePosition`
- overload `proofRequirements`

#### Expand `operators`

Add `hoverDescription` and `usageExample` from `OperatorMeta` (`src/Precept/Language/Operator.cs:27-36`).

### 2. Serialize the omitted authoring-critical catalogs

#### Add `operations`

This is non-negotiable for AI-first authoring. Proposed shape:

```json
{
  "operations": [
    {
      "kind": "MoneyDivideMoneyCrossCurrency",
      "operator": "Divide",
      "lhs": { "type": "Money", "name": "lhs" },
      "rhs": { "type": "Money", "name": "rhs" },
      "resultType": "ExchangeRate",
      "description": "Money ÷ money (different currencies) → exchangerate",
      "bidirectionalLookup": false,
      "qualifierMatch": "Different",
      "proofRequirements": [ ... ],
      "hasCaseInsensitiveVariant": false,
      "caseInsensitiveDiagnosticCode": null
    }
  ]
}
```

Do not serialize raw object-identity subjects; map proof subjects to parameter names/positions and self/ accessor references.

#### Add `outcomes`

The catalog is tiny and high value. Proposed shape:

```json
{
  "outcomes": [
    {
      "kind": "Reject",
      "leadingToken": "reject",
      "argumentKind": "RequiredStringLiteral",
      "description": "Reject the event with an explanation message",
      "example": "-> reject \"...\""
    }
  ]
}
```

#### Add `expressionForms`

This makes expression grammar legible without reading parser code.

#### Add `proofRequirements`

Even if instance requirements are attached elsewhere, the catalog of proof-requirement kinds should still be exposed.

#### Add `faults`

Expose `Faults.All` with `code`, `messageTemplate`, `severity`, and `recoveryHint`. The docs already say this should happen.

### 3. Do not split into many rigid tools; add profiles and section filters instead

A hard tool explosion (`precept_types`, `precept_actions`, `precept_diagnostics`, etc.) would damage discoverability and create an awkward learning surface. The better design is:

- keep `precept_language` as the canonical umbrella tool
- add a `profile` parameter
- add a `sections` filter

Recommended API shape:

```json
{ "profile": "quick|full", "sections": ["syntaxReference", "types", "operations", "domains.currencies"] }
```

Recommended behavior:

- `profile: "quick"` -> authoring fast path
- `profile: "full"` -> complete machine-readable language dump
- `sections` -> targeted retrieval for follow-up questions

Suggested quick profile contents:

- `syntaxReference`
- `constructs`
- `outcomes`
- `types`
- `modifiers`
- `actions`
- `functions`
- `operations`
- compact domain guidance only

Suggested full-only / opt-in sections:

- full token/tooling metadata
- full diagnostics registry
- full UCUM browse data
- `firePipeline`

### 4. Author new content that does not naturally live in the current catalogs

Some authoring guidance is language-wide and should be structured metadata, but not forced awkwardly into existing per-member catalogs.

#### Add minimum viable templates

At minimum:

- header-only stateless precept
- field-only stateless precept
- smallest stateful precept with one initial state, one event, one transition

These should be compile-verified examples.

#### Add compatibility matrices

The AI author needs explicit matrices for:

- modifier × type
- action × target type
- accessor × required guard
- operator/function × type combinations (or a filtered view over `operations`)

#### Add anti-pattern guidance

High-value correction pairs:

- computed fields use `<-`, not `->`
- `omit` cannot be guarded
- event handlers cannot use `when`
- `in` and `of` are mutually exclusive except on `price`
- `lookup` does not support `notempty`
- choice comparisons require `ordered`

#### Add scope/reference rules

Expose a small structured matrix for what is in scope in:

- field defaults
- rules
- state ensures
- event ensures
- transition guards
- transition actions
- state actions

### 5. Rebalance payload composition around authoring value

If the tool stays monolithic, at least move low-authoring-value fields out of the default authoring profile.

Candidates to demote from default authoring payload:

- `TokenMeta.TextMateScope`
- `TokenMeta.SemanticTokenType`
- most `ValidAfter` detail
- full diagnostics registry
- full UCUM browse list
- `firePipeline`

These are valid surfaces, but they are not the first thing an AI author needs.

## Recommended priority order

### P0 — Highest impact, highest leverage

1. **Add `operations` to MCP output.** Without it, operator/type legality is guesswork.
2. **Add `syntaxReference` after compile-validating it.** This is the missing file-wide authoring guide.
3. **Expose `TypeMeta.ContentValidation`, `UsageExample`, `HoverDescription`, `NotemptyApplicable`, and accessor proof requirements.** This unlocks domain-type authoring.
4. **Expose action/function/modifier proof metadata and examples.** This unlocks safe, idiomatic authoring instead of trial-and-error.
5. **Add `outcomes`.** Transition rows are incomplete without it.

### P1 — High impact, moderate effort

6. **Add `profile` + `sections` filtering to `precept_language`.** Stop paying the full payload cost on every authoring loop.
7. **Expose `expressionForms` and `proofRequirements`.** This improves expression authoring and guard reasoning.
8. **Expose `faults`.** Completes the diagnostic/fault prevention chain.

### P2 — Important quality upgrade

9. **Author compile-verified minimum templates, common patterns, and anti-patterns.**
10. **Add scope/reference matrices.**
11. **Decide whether the full UCUM browse surface belongs in authoring fast-path at all.** Probably not.

## Final judgment

If the product promise is “an AI can write correct, idiomatic Precept from the MCP vocabulary alone,” the current `precept_language` surface does not yet satisfy it.

It is good at exposing raw vocabulary. It is not yet good at exposing **authoring legality**, **literal formats**, **guard/proof requirements**, **outcome forms**, **common patterns**, or **minimum valid shapes**. Those are the exact things a model without training data needs most.
