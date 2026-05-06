## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Catalogs remain the language truth; runtime, tooling, and docs derive behavior from metadata and shape rather than hardcoded enum identity or parallel lists.
- Public API surfaces must expose stable CLR/JSON interchange types; evaluator internals stay internal and never leak into the durable surface.
- Operation legality lives in `Language.Operations`; computation lives in `TypeRuntime` plus the runtime dispatch registry. The evaluator stays zero-knowledge.
- Identity-type work follows the dual-shape rule: enriched internal entities when metadata/lifetime demands it, lightweight API-boundary code/value shapes when callers need stable interchange.
- Collection internals are settled around universal `PreceptValue[]` backing, stride-2 pair storage, static `CollectionActions` helpers, and evaluator-owned copy-on-write.
- Collection CLR adapters are lazy at the `Version` level and eager on first materialization, not per-index lazy.
- Working docs drift quickly during heavy deliberation; canonical docs and squad records must be synchronized as soon as a decision locks.
- Investigation docs may be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- OQ-3f (quantity constraint granularity): Three levels — `quantity`, `quantity of '<dim>'`, `quantity in '<unit>'` — is the correct architecture. Precept must make invalid configurations structurally impossible at declaration time; dimension constraints serve real business needs that unit-only constraints can't express.
- OQ-CUR-1 (Currency symbol): Include it. The "not in ISO 4217" purity argument loses to practical reality. Every business system renders currency symbols; forcing callers to maintain a parallel lookup is a catalog-architecture violation.
- OQ-CUR-3 (currency typed lanes): Both `Get<Currency>()` and `Get<string>()` must work. The alpha-code string lane is too common a use case (serialization, logging) to deny. Dual-lane pattern is consistent with Version's own raw/typed lane model.
- OQ-CUR-4 (ISO 4217 shipping): Embedded resource. ~180 rows, mostly-stable, negligible amendment impact. A separate data package is packaging complexity without commensurate benefit at v1.
- §11 (Precept.Types namespace): No separate assembly. `Money`, `Quantity`, `Currency`, `UnitOfMeasure`, `MeasureDimension`, `UnitCatalog`, `CurrencyCatalog`, `Price`, and `ExchangeRate` live in the `Precept.Types` namespace within the existing `Precept` assembly. Shane locked this 2026-05-05.
- §5/§12 reconciliation: `Unit`, `Dimension`, and `UnitTier` are evaluator-internal. `Quantity` at the public surface uses `UnitOfMeasure` (the proxy), not the internal `Unit`. `QuantityFieldDescriptor` uses `UnitOfMeasure?` / `MeasureDimension?`. When cleaning investigation docs, always verify that CLR shape definitions in earlier sections match the final reconciled shapes in later sections — §7.1's Price CLR shape had drifted from the §8.8 decision.

## Learnings

- Working docs (`runtime-api-public-surface-spec.md`) can fall out of sync with later-locked authoritative design docs. The temporal type system doc's NodaTime alignment directive supersedes any earlier BCL-type mappings in working docs.
- The temporal doc's Explicit Exclusions table is the authority on what is NOT in scope - check it before promoting any temporal-adjacent type to "confirmed."
- The NodaTime alignment directive is absolute: expose NodaTime types directly at the backing layer. No DateOnly/TimeOnly wrappers, no custom temporal structs. The pattern is `field DueDate as date` -> `NodaTime.LocalDate` backing.
- `PreceptValue` is an internal-only type (§13 axiom, locked Shane directive). The public raw-lane indexer on both `Version` and `FiredArgs` returns `JsonElement`, never `PreceptValue`. Any doc that shows `PreceptValue` in a public signature is wrong and must be corrected.
- UCUM scope is "full grammar + tiered discovery" (OQ-3b resolved). The locked decision is NOT a subset — it's the full grammar accepted, with Tier 1 (~150 atoms) surfaced proactively. Every occurrence of "UCUM subset" in canonical docs describing what unit codes are valid is a documentation error.
- `currency` backing type is `sealed class Currency` backed by `CurrencyCatalog` (frank-114, locked 2026-05-04). The old string-backed representation is superseded. The four accessors (`.name`, `.symbol`, `.minorUnit`, `.numericCode`) are part of the locked design; `.symbol` is pending OQ-CUR-1.
- When working doc decisions lock, all canonical docs that reference the old state must be updated in the same pass. Letting the lag accumulate creates conflicting sources of truth across the codebase.

## Recent Updates

### 2026-05-05 - Replaced "Backing type" terminology with "CLR shape" throughout `business-domain-types.md`

- **Terminology decision:** "Backing type" implies something else is doing the backing — it is correct only when describing what primitive underlies a public surface. Since `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, and `exchangerate` are now first-class CLR types (sealed classes or readonly record structs), the correct framing is **"CLR shape"** — describing the .NET structure callers receive from `Get<T>()`.
- **Seven occurrences replaced** (all `**Backing type:**` labels → `**CLR shape:**`):
  - `money` (line 340): label fix + content corrected from `decimal + ISO 4217 currency code` (implies bare string) to `Amount (decimal) + Currency (Currency, interned from CurrencyCatalog)`.
  - `currency` (line 409): label fix + "backed by CurrencyCatalog" rephrased to "Instances are interned from CurrencyCatalog" (Currency is not backed by the catalog; it is sourced from it).
  - `quantity` (line 455): label fix + content corrected from `decimal magnitude + unit identifier (UCUM or entity-scoped)` (implies bare string) to `Amount (decimal) + Unit (UnitOfMeasure, validated UCUM code interned from UnitCatalog)`.
  - `unitofmeasure` (line 532): label fix + content corrected from `string` to `readonly record struct UnitOfMeasure(string Code)` with proxy note (UnitOfMeasure is not a string — it is a typed API proxy struct).
  - `dimension` (line 583): label fix + content corrected from `string` to `readonly record struct MeasureDimension(string Name)` with proxy note.
  - `price` (line 673): label fix only (content was already correct from prior session).
  - `exchangerate` (line 788): label fix only (content was already correct from prior session).
- **No PreceptValue conflation found** in the doc — the internal evaluator struct does not appear anywhere in `business-domain-types.md`.
- **Other "backing" uses left intact:** `decimal backing` in D12 (refers to arithmetic precision, not public type shape), `number (backed by double)` (correct: number's CLR type is double), NodaTime "backing model" (refers to the external library, not a type shape). These usages are precise and not the "Backing type:" pattern.

### 2026-05-05 - Corrected stale backing type lines for `price` and `exchangerate` (frank-task)

- `docs/language/business-domain-types.md` line 673 (`price`): replaced `\`decimal\` magnitude + \`string\` numerator currency + \`string\` denominator unit` with `\`Amount\` (\`decimal\`) + \`Currency\` (\`Currency\`, interned from \`CurrencyCatalog\`) + \`Unit\` (\`UnitOfMeasure\`, interned from \`UnitCatalog\`)`.
- `docs/language/business-domain-types.md` line 788 (`exchangerate`): replaced `\`decimal\` magnitude + \`string\` numerator currency + \`string\` denominator currency` with `\`Rate\` (\`decimal\`) + \`From\` (\`Currency\`, interned from \`CurrencyCatalog\`) + \`To\` (\`Currency\`, interned from \`CurrencyCatalog\`)`.
- Accessor tables for both types verified consistent with the corrected CLR shapes: `price` accessors (`.currency` → `currency`, `.unit` → `unitofmeasure`, `.amount` → `decimal`); `exchangerate` accessors (`.from` → `currency`, `.to` → `currency`, `.amount` → `decimal`). No accessor-table changes needed.
- Root cause: these lines predated the CurrencyCatalog/UnitOfMeasure identity-type decisions and were identified as a known gap in the frank-151 audit (see entry above). This pass closes that gap.



- Performed systematic section-by-section audit of `docs/working/precept-value-types-investigation.md` against `docs/language/business-domain-types.md`, `docs/runtime/runtime-api.md`, and `docs/runtime/evaluator.md`.
- Applied 6 corrections:
  - §7.1 Money CLR shape: `string Currency` → `Currency Currency` (stale pre-OQ-CUR-2)
  - §7.2 ExchangeRate size note: `decimal + string + string` / "string fields" → `decimal + Currency + Currency` / "`Currency` instances interned from `CurrencyCatalog`"
  - §8.2: Replaced stale "flagged as OQ-CUR-4" forward reference with the locked resolution (embedded resource in `CurrencyCatalog` assembly)
  - §Addendum shape table: `sealed class (subtype) / Entity lifetime` → `32-byte tagged struct / Opaque tagged union; no per-value heap allocation`
  - §13.2 shape table: `PreceptValue subtype hierarchy / sealed class / GC-tracked` → `32-byte tagged struct / Opaque tagged union; all field and arg values at runtime`
  - §8.8 + §8.11: OQ-CUR-2 status `✅ Presumed agreed` → `✅ Locked` (§8.10 is definitive; consistency with all other decisions in the table)
- Audit summary written to `.squad/decisions/inbox/frank-151-audit-summary.md`.
- Notable: canonical `business-domain-types.md` still has stale `string`-backed "Backing type" lines for `price` and `exchangerate` — a known gap outside the scope of this audit task. Should be cleaned in a dedicated canonical-doc correction pass.

### 2026-05-06 - Get<string>() scope broadened to universal (Shane correction)

- Corrected `docs/runtime/runtime-api.md` line 550: `Get<string>()` rule now covers every type Precept supports — not just registered business-domain value types. Explicitly includes primitives and scalars (`int`, `decimal`, `bool`, `string`, `DateTime`, etc.). Framed as analogous to `.ToString()` in .NET. Five business-domain canonical-form examples retained (Quantity, Money, Price, ExchangeRate, Currency) as illustrative specifics under the universal rule.

### 2026-05-05T20:38:31Z - Shane sign-offs applied (OQ-CUR-1, OQ-CUR-3 generalized, §11)

- Added `Symbol` (string) to `Currency` sealed class description and `.symbol` accessor row in `docs/language/business-domain-types.md`. Symbol sourced from curated supplement; disambiguated form noted where ISO 4217 alpha codes share a symbol.
- Added universal `Get<string>()` rule to `docs/runtime/runtime-api.md` — all registered value types support it, returning the canonical string representation (UCUM literal, ISO 4217 amount string, ratio string, or alpha code as appropriate).
- Rewrote `docs/working/precept-value-types-investigation.md` §11: no separate `Precept.Types` assembly; types are organized under the `Precept.Types` namespace within the existing `Precept` assembly. Assembly-split framing and "Pending Shane sign-off" removed. Header status updated to "all verdicts locked."



- Reconciled `docs/working/precept-value-types-investigation.md` against `docs/language/temporal-type-system.md` and `docs/language/business-domain-types.md`.
- Corrected §7.4 `DateRange` from confirmed to deferred, aligned `date` to `NodaTime.LocalDate`, and removed the stale `DateOnly` public-surface claim.
- All remaining sections stayed consistent; the only follow-up tensions left for Shane are the DX-layer well-known-constants surface and the authoritative `currency` accessor doc lag.

### 2026-05-05T15:20:17Z - Value-types investigation ledger sync recorded

- Scribe merged Frank's value-types sync and catalog-delegate evaluation inbox notes into `.squad/decisions/decisions.md`.
- The canonical record now reflects the 9-14 investigation integration plus the `OperationMeta`/executor separation verdict.
- Processed inbox files were cleared after deduplication and ledger merge.

### 2026-05-05T11:32:50Z - Value types investigation reconciled against authoritative docs

- Reconciled `docs/working/precept-value-types-investigation.md` against `docs/language/temporal-type-system.md` and `docs/language/business-domain-types.md`.
- 7.4 DateRange: Corrected from confirmed to deferred - the temporal type system doc explicitly defers DateInterval/daterange in its Exclusions table. Changed CLR type from DateOnly to NodaTime.LocalDate to match the locked backing type decision. Removed the erroneous "public API uses DateOnly" claim (the NodaTime alignment directive says expose NodaTime directly).
- 7.6 Summary Table: Updated DateRange row to reflect deferred status.
- All other sections (1-6, 7.1-7.3, 7.5, 8-14) verified as consistent with authoritative docs. No changes needed.
- Flagged two tensions for Shane: 12 well-known constants vs 4 "no blessed subset" principle; 8 currency accessors extending beyond what the authoritative doc currently specifies (locked Shane decision, not a contradiction).
- Decision record: `.squad/decisions/inbox/frank-value-types-reconciliation.md`.

### 2026-05-05T05:19:25Z - Collection types investigation fully archived

- Full walkthrough of `docs/working/precept-collection-types-investigation.md` is complete and the document is now archived under `docs/working/Archived/`.
- `docs/runtime/evaluator.md` now carries the OQ-C3 direction-model closure in 7.4.1 C: `EnqueueByPriority` takes `SortDirection`, and the new "Direction model (OQ-C3)" subsection makes declared-direction storage explicit.
- Remaining collection implementation guidance is already disseminated into the evaluator documentation; detailed provenance remains in `.squad/decisions.md`.

### Historical summary through 2026-05-05

- 2026-05-04 locked the execution architecture baseline: `TypeRuntime` owns per-type behavior, the runtime aggregation registry owns flat dispatch, and the evaluator never regains type-specific knowledge.
- Collection API direction stabilized around CLR-friendly adapters (`PreceptList<T>`, `PreceptLookup<TKey, TValue>`, `KeyedElement<TValue, TKey>`) with evaluator-owned CoW and declared-direction pair storage.
- Currency, unit, and dimension work converged on catalog-backed identity types with strict provenance notes and API-boundary-specific shapes.
- Use `.squad/decisions.md` for full per-decision provenance; keep `history.md` focused on durable operating context plus the newest closures.
