# George P0 Fix — Qualifier Pipeline + DimensionCatalog Cleanup

**Date:** 2025-07  
**Author:** George (Runtime Dev)  
**Branch:** Precept-V2-Radical  
**Commits:** `1f626aea`, `735c1674`

---

## Task A: Qualifier Pipeline Fix (P0)

### Problem
`field Cost as money in 'USD'` was silently discarding the `'USD'` qualifier.
`DeclaredQualifiers` was always `ImmutableArray<DeclaredQualifierMeta>.Empty` for
every field and event arg.

### Root Cause
`ParseTypeReference` returned `SimpleTypeReference` and stopped — no qualifier
token consumption happened. `ParseArgumentList` did a manual type lookup and
stored raw `TypeMeta` (not `ParsedTypeReference`), making qualifier threading
impossible from that path.

### Solution

**Parse tree layer:**
- Added `QualifiedTypeReference(ParsedTypeReference InnerType, ImmutableArray<ParsedQualifier> Qualifiers, SourceSpan)` to `ParsedTypeReference.cs`.
- Added `ParsedQualifier(TokenKind Preposition, QualifierAxis Axis, string Value, SourceSpan ValueSpan)`.
- Added `ResolvedKind` computed property to base `ParsedTypeReference` to avoid cast sites in test code.

**Parser layer:**
- Added `TryParseQualifiers(ParsedTypeReference, TypeMeta)` — consults `typeMeta.QualifierShape` slots, consumes `TokenKind.TypedConstant` (NOT `StringLiteral`), wraps in `QualifiedTypeReference` if any qualifiers found.
- `ParseTypeReference`: calls `TryParseQualifiers` after creating `SimpleTypeReference`.
- `ParseArgumentList`: changed `args` list element type from `(string, TypeMeta, ...)` to `(string, ParsedTypeReference, ...)`, added `TryParseQualifiers` call.

**Slot/symbol layer:**
- `SlotValue.cs` `ArgumentListSlot.Args`: `TypeMeta` → `ParsedTypeReference`.
- `SymbolTable.cs` `DeclaredArg.Type`: `TypeMeta` → `ParsedTypeReference`.
- `NameBinder.cs` fallback empty array: type parameter updated to match.

**TypeChecker layer:**
- `ResolveTypeKind`: added `QualifiedTypeReference` case — delegates to `ResolveTypeKind(qualified.InnerType)`.
- `ExtractQualifiers(ParsedTypeReference, CheckContext)`: returns empty if not `QualifiedTypeReference`; enforces `in`/`of` mutual exclusion per `QualifierShape.InOfExclusive`; maps each `ParsedQualifier` to `DeclaredQualifierMeta` DU subtype.
- `MapCurrencyQualifier`: validates via `CurrencyCatalog.All.ContainsKey`; emits `InvalidCurrencyCode`.
- `MapUnitQualifier`: validates via `UcumCatalog.Parse`; emits `InvalidUnitString`; extracts `PreferredDimensionAlias` for `DeclaredQualifierMeta.Unit`.
- `MapDimensionQualifier`: validates via `DimensionCatalog.All.ContainsKey`; emits `InvalidDimensionString`.
- `MapFromCurrencyQualifier` / `MapToCurrencyQualifier`: same as currency.
- `PopulateFields`: `DeclaredQualifiers: ImmutableArray.Empty` → `ExtractQualifiers(declared.Type, ctx)`.
- `PopulateEvents`: switched from LINQ Select to foreach to thread `ResolveTypeKind(arg.Type)` and `ExtractQualifiers(arg.Type, ctx)`.

### Ambiguities Resolved

| Question | Resolution |
|---|---|
| Qualifier token kind | `TokenKind.TypedConstant` (task pseudocode said `StringLiteral` — wrong). `'USD'` lexes as TypedConstant. |
| `TypedConstant.Text` | Contains value WITHOUT quotes (`'USD'` → `"USD"`). |
| `MutuallyExclusiveQualifiers` span | Use the `QualifiedTypeReference.Span` (full type ref span), no string args. |
| `DeclaredQualifierMeta.Unit` | Requires both `UnitCode` and `DimensionName`; get latter from `UcumCatalog.Parse(value).Unit.PreferredDimensionAlias ?? ""`. |

### Files Modified
- `src/Precept/Pipeline/ParsedTypeReference.cs`
- `src/Precept/Pipeline/Parser.cs`
- `src/Precept/Pipeline/SlotValue.cs`
- `src/Precept/Pipeline/SymbolTable.cs`
- `src/Precept/Pipeline/NameBinder.cs`
- `src/Precept/Pipeline/TypeChecker.cs`
- `test/Precept.Tests/NameBinder/NameBinderTests.cs`
- `test/Precept.Tests/Parser/ParserDirectConstructTests.cs`

---

## Task B: DimensionCatalog API Cleanup

### Problem
`DimensionCatalog` exposed two public properties (`AllAliases: IReadOnlyList`, `AllNames: FrozenSet`) and a private `ByName` dictionary — three surfaces for one concept. This diverged from the `CurrencyCatalog.All: FrozenDictionary<string, ...>` registry pattern.

### Solution
Replaced all three with `public static FrozenDictionary<string, DimensionAlias> All` keyed case-insensitively by alias name. `GetByName` now delegates to `All[name]`.

### Files Modified
- `src/Precept/Language/Ucum/DimensionCatalog.cs`
- `src/Precept/Language/Types.cs` — `DimensionCatalog.AllNames` → `DimensionCatalog.All.Keys.ToFrozenSet(...)`
- `test/Precept.Tests/Language/Ucum/DimensionCatalogTests.cs` — `AllNames` → `All.Keys`

---

## Test Results
- Before: 3721 passing
- After: 3721 passing (0 failures, 0 regressions)
