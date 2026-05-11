## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs derive from durable catalog shape rather than enum-identity switches or parallel lists.
- Interpolation work must preserve compile-time guarantees first; plans that trade structural certainty for runtime validation are philosophically out of bounds.
- Constraint-propagation scope should stay concrete and bounded: one-hop semantic traces are acceptable, speculative provenance systems are not.

## Live Guidance

- String holes are out of scope for interpolated typed constants; typed-hole composition is the only path that preserves qualifier and proof reasoning.
- Slice 6 is a numeric proof strategy only. Qualifier, dimension, modifier, and presence obligations remain declaration-driven through the existing proof strategies.
- Single-hole whole-value forms (`'{x}'`) are part of Slice 6 scope and should flow through the same helper path as magnitude-slot proofs.
- LOE reviews should call out missing binder/tooling walk updates explicitly so checker and language-server behavior do not drift.

## Historical Summary

- Earlier May review, parser-gap, typed-literal, and catalog-audit detail was archived into `history-archive.md` during the 15 KB summarization pass.
- The full chronology remains in `.squad/decisions.md`; this file now keeps only live architectural guidance and the newest closeout state.

## Recent Updates

### 2026-05-11T20:25:57Z — DTO-free MCP catalog exposure rejected
- Raw catalog serialization is not an MCP-ready public contract today because abstract-base serialization drops subtype data, enums surface as numeric values, and runtime-shaped values leak transport-hostile structure.
- Durable direction: keep the curated MCP projection layer and reduce maintenance only by moving or generating the mapping logic instead of exposing raw core records.

### 2026-05-11T20:03:33Z — Slice 6 closeout recorded
- frank-6 confirmed that Slice 6 stays numeric-only and that qualifier/dimension/presence propagation should not be added because S2/S5 already discharge those obligations from field declarations.
- The same review identified the only scoped gap: a single-hole whole-value interpolated constant should inherit numeric constraints from its source field.

### 2026-05-11T20:03:33Z — Plan patch merged
- frank-7 updated the Slice 6 plan to use `GetSlotSource`, raised the estimate to roughly 90 LOC / 10 tests, and documented the no-qualifier-propagation rationale directly in the plan.
- The compile-time guarantee ruling still stands above the plan: simplification-by-runtime-validation is rejected.

## Learnings

### 2026-05-11 — Dimension-qualified unit slot compatibility analysis
- `f1.unit` accessor resolves to bare `TypeKind.UnitOfMeasure` — no dimension qualifier is carried in the static type. `FixedReturnAccessor.Returns` is just a `TypeKind` enum; `ReturnsQualifier` metadata signals "which qualifier axis this extracts" for proof strategy use, not for narrowing the return type itself.
- `TypedMemberAccess` stores only `TypeKind ResultType` — there is no concept of "qualified return types" on accessor results anywhere in the type system.
- The interpolation plan's Slice 2 slot compatibility check is `TypeKind`-only. It will accept any `unitofmeasure` expression in a unit slot regardless of the source field's dimension vs. the target field's dimension.
- This is a real gap: `field f2 as quantity of 'mass' default '1 {f1.unit}'` with `f1 as quantity of 'length'` compiles clean but produces a dimensionally incoherent quantity at runtime.
- The gap is NOT interpolation-specific — static typed constants have the same underlying issue (content validation checks unit syntax/validity but not dimension compatibility against the field's declaration). Fixing this properly requires either type system enrichment (qualified return types on accessors) or a broader dimension-to-unit consistency validation pass.
- Decision: acknowledged gap, deferred to a separate issue. The S6 "no dimension propagation" rationale holds because S6's concern is obligation discharge, not slot dimensional consistency. The fix belongs in a cross-cutting validation feature, not as an S2/S6 bolt-on.

### 2026-05-11 — MCP DTO-free catalog serialization audit
- `tools/Precept.Mcp` currently carries 63 DTO records total: 36 in `LanguageToolDtos.cs`, 14 in `NewToolDtos.cs`, and 13 in `CompileToolDtos.cs`. `LanguageTool.cs` alone contains 33 mapping helpers because the MCP contract is not a raw mirror of core metadata.
- Direct `System.Text.Json` serialization of core catalog records is technically possible for many flat records, but the current raw output is not MCP-ready: enums serialize as numeric values, nested DU/base-typed properties lose subtype data, and object references like `TokenMeta` expand into noisy nested objects.
- Verified breakpoints from the live code: serializing `ModifierMeta` as its base drops `ApplicableTo` / declaration-site / scope-specific data; `OperatorMeta` as its base drops `Token` / `Tokens`; `ContentValidation` as its base drops closed-set and NodaTime subtype fields; `ActionMeta.ProofRequirements` loses comparison / threshold / subject details because `ProofRequirement` is serialized through the abstract base.
- `ImmutableArray<T>` is not the blocker — `ConstructMeta.Entries` serialized successfully. `FrozenSet<T>` also serialized when exposed directly from the closed-set validation path. The real friction is polymorphism and contract shape, not frozen/immutable collection support.
- Direct UCUM exposure is especially ugly today: `UcumExactFactor` contains `BigInteger`, and raw JSON expands numerator/denominator into `BigInteger` implementation detail objects instead of the MCP DTO's clean string numerators/denominators.
- There is no existing JSON contract configuration in `tools/Precept.Mcp`: `Program.cs` only wires the MCP host. No custom `JsonSerializerOptions`, no `JsonStringEnumConverter`, no `[JsonDerivedType]` / `[JsonPolymorphic]`, and no source-generated JSON context are present.

### 2026-05-11 — Dimension-unit consistency validation integrated into interpolation plan
- The earlier analysis in `frank-dimension-proof-propagation.md` was wrong about the static case: `QuantityValidator.Validate()` (lines 30–53) ALREADY checks dimension-to-unit consistency for static typed constants via `TypedConstantContext.DeclaredQualifiers`.
- The interpolated case is a real gap: `TypedMemberAccess` for `.unit` resolves to bare `TypeKind.UnitOfMeasure` with no dimension provenance. Slice 2's TypeKind-only slot compatibility check cannot detect `f1.unit` (length) going into a mass field.
- Fix chosen: Option B — structural AST pattern match. After slot assignment, pattern-match unit-slot holes for `TypedMemberAccess { ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier: QualifierAxis.Unit }, Object: TypedFieldRef/TypedArgRef }`, extract dimension from receiver's `DeclaredQualifiers`, compare to target field's dimension. ~25 lines.
- Option A (type system enrichment) rejected: adding qualified return types to accessors would require a new type system concept with no proportionate benefit. The information is already available one hop away in the AST.
- New diagnostic: `DimensionMismatchInUnitSlot = 124`. Interpolation plan codes renumbered to 121/122/123 (from 120/121/122) because `ConflictingModifiers = 120` was added after the plan was written.
- Scope: physical dimension consistency for `quantity` and `price` unit slots. Temporal dimension and currency qualifier mismatch are excluded (different axes, narrower surfaces, separate tracking).

### 2026-05-11T22:07:10Z — frank-1 dimension-gap verdict recorded
- frank-1 confirmed that `.unit` access resolves to bare `unitofmeasure`, so the existing interpolation slot-compatibility pass cannot prove or reject dimension mismatches from source-field provenance.
- The deferral recommendation is preserved as rejected-path evidence only; Shane overruled it and the batch closed on a compile-time fix path.

### 2026-05-11T22:07:10Z — frank-2 interpolation plan correction merged
- frank-2 corrected the static-case analysis (`QuantityValidator.Validate()` already covers non-interpolated constants), chose the Slice 2 structural AST match, and added `DimensionMismatchInUnitSlot = 124` with interpolation diagnostics renumbered to 121/122/123.
- The plan now carries the physical-dimension scope boundary plus the added Slice 2 estimate (+25 LOC, 9 tests) for unit-slot consistency.
