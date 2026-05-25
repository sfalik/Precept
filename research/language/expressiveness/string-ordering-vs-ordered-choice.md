# String Ordering vs. Ordered Choice

> **Research question:** Arbitrary string ordering (`<`, `>`, `<=`, `>=` on arbitrary `string` values) — does it have genuine business use cases, or do ordered choice types cover all real needs?
>
> **Context:** Precept currently does **not** implement `<`, `>`, `<=`, `>=` for arbitrary strings. Only `==` and `!=` exist (`StringEqualsString`, `StringNotEqualsString` in `Operations.cs`, `OperationKind` values 129–130). `Types.cs:301` declares `string` with `TypeTrait.EqualityComparable | TypeTrait.ChoiceElement` — **no `TypeTrait.Orderable`**. The language spec doc claims ordering support, but the compiler readiness review (F-LANG-PRIM-01) flagged this as a doc-vs-implementation discrepancy. The decision point: explicitly drop string ordering (with doc cleanup) vs. implement it (requiring `TypeTrait.Orderable` on `String`, 4 new operations in `Operations.cs`, graph analyzer support, evaluator support, and the full CI/non-CI cascade).

---

## 1. Survey of Actual Business Use Cases for String Ordering

### Category 1: Structured Codes with Implicit Order

**Use case:** Codes where lexicographic order happens to encode a meaningful business hierarchy.

| Example | Business meaning | Ordinal correct? | Ordered choice viable? |
|---------|-----------------|-------------------|----------------------|
| SKU prefixes: `A-CAT` < `B-DOG` | A prefix category system | Only if codes are zero-padded / fixed-width | **Yes** — each code is an explicit choice member |
| Department codes: `D001` < `D010` | Numeric meaning in string | No — `"D100" < "D20"` is true in ordinal | **Maybe** — but codes are open-ended; can't enumerate all |
| Room numbers: `101` < `102` | Numeric order in string | No — `"101" < "2"` is true in ordinal | **No** — rooms are open-ended |
| Legal filing codes: `A1` < `A2` < `B1` | Hierarchical classification | Yes — but fragile with mixed widths | **Yes** if finite, **No** if extensible |

**Verdict:** When codes are truly finite and known (SKU categories, priority codes), ordered choice is the right model. When codes follow a structured pattern with variable values (room numbers, any-numeric-suffix codes), ordinal comparison is wrong — `101` does NOT sort before `2` ordinally. The correct model is **structured parsing** (extract the numeric portion) or a **dedicated type** (not string ordering).

**Precept fit:** Ordered choice handles the finite case. The open-ended structured case requires a dedicated type (like an integer with a string prefix). String ordering would invite the wrong model.

### Category 2: Version Strings

| Example | Ordinal result | Correct order? | Proper model |
|---------|---------------|----------------|--------------|
| `"1.0" < "2.0"` | ✅ | Yes, by coincidence | Not structural |
| `"1.9" < "1.10"` | ❌ (`"9" > "1"`) | No | Needs numeric-aware comparison |
| `"2.0.1" < "2.0.10"` | ❌ | No | Needs semantic versioning |

**Verdict:** Version comparison is a well-known anti-pattern when done ordinally. SemVer requires integer-aware comparison at each dot-separated component. Precept's philosophy says: if the data has structure, the type should reflect it. A `version` type (parsing structured strings) is the right model, not raw string ordering.

**Ordered choice?** No — versions are inherently open-ended. But the right answer isn't string ordering either; it's a dedicated `version` type with proper parsing and comparison logic.

**Precept fit:** String ordering invites the wrong solution. A future `version` type (like the existing `date` and `decimal` types) is the correct approach.

### Category 3: Alphabetical / Dictionary Ordering

| Use case | Business relevance | Where it belongs |
|----------|-------------------|------------------|
| "Sort customer list alphabetically" | Display/presentation layer | **Not Precept** — Precept governs data integrity, not UI |
| "Reject if name starts with 'Z'" | Business rule (unusual but possible) | Precept — but uses `startsWith`, not `<` |
| "Filter records where name < 'M'" | List filtering (UI concern) | **Not Precept** |
| "Classify as 'A-M' or 'N-Z'" | Business classification | **Yes** — but could be a derived field computed via conditional logic on the first character |

**Verdict:** Alphabetical ordering of names/places is overwhelmingly a **display/presentation concern**, not business logic. Precept explicitly governs data integrity, not UI behavior. Filtering lists alphabetically is a UI concern. The rare business rule involving alphabetical classification (e.g., "group A-M vs N-Z") is better expressed as a conditional on extracted structure (first character, length, regex match) rather than ordinal string comparison.

**Ordered choice?** Not directly, but the business rules that actually need alphabetical classification are better modeled with structured access (string `.length`, string `.charAt(0)` — or equivalent built-in functions) rather than arbitrary comparison.

### Category 4: Time-as-String

| Example | Ordinal result | Correct? | Proper model |
|---------|---------------|----------|--------------|
| `"13:00" < "14:00"` | ✅ | Yes, by luck (fixed-width, 24h) | **Date/Time type** |
| `"9:00" < "10:00"` | ❌ (`"9" > "1"`) | No | **Date/Time type** |
| `"11:30 PM" < "1:00 AM"` | ❌ (PM strings sort after AM) | Depends on intent | **Date/Time type** |

**Verdict:** Time comparison as strings is almost never correct ordinally unless the format is guaranteed fixed-width and 24-hour (and even then, it's fragile). This is a well-known anti-pattern. The proper model is a `time` or `datetime` type.

**Precept fit:** Precept already has `time` and `date` types with proper ordering. String ordering on time-like values is exactly the problem these types were designed to solve.

### Category 5: Number-as-String

| Example | Ordinal result | Correct numeric order? |
|---------|---------------|----------------------|
| `"100" < "99"` | ❌ (`"1" < "9"`) | No |
| `"2" < "10"` | ❌ | No |
| `"100" > "99"` | ✅ (by coincidence) | Yes |

**Verdict:** This is the canonical string-ordering anti-pattern. Numbers stored as strings and compared ordinally is a fundamental data modeling error. The fix is: use the `integer` or `decimal` type. If a downstream system requires string input, convert at the boundary.

**Precept fit:** Precept already has `integer` and `decimal` types with full ordering support. String ordering on number-like values is exactly the problem these types address.

### Category 6: Truly Arbitrary String Ordering

| Example | Business meaning of "<" | Usefulness |
|---------|-----------------------|------------|
| `"apple" < "Banana"` | Alphabetical by Unicode code point | **None** — case matters, diacritics differ, locale-dependent |
| `"Zebra" < "apple"` | Ordinal (uppercase before lowercase in Unicode) | **None** — this is an encoding artifact, not business meaning |
| `"123" < "abc"` | Ordinal comparison of unrelated strings | **None** — these are just different data |

**Verdict:** True arbitrary string ordering (comparing strings with no known structure) produces results that are **almost never meaningful for business logic**. The Unicode code point ordering is locale-dependent, case-sensitive, diacritic-sensitive, and culturally meaningless. When someone writes `customer.Name < "M"`, they almost always want case-insensitive alphabetical ordering — which ordinal comparison does NOT provide correctly.

---

## 2. How Other Systems Handle String Ordering

### SQL Databases (PostgreSQL, MySQL, SQL Server, Oracle)

**What they do:** String `<`, `>`, `<=`, `>=` in `WHERE` clauses use **collation**. Collation defines character ordering, which determines what "less than" means.

- **PostgreSQL:** `COLLATE "C"` (byte-order, like ordinal), `COLLATE "en_US"` (dictionary order), `COLLATE "und-x-icu"` (Unicode collation algorithm). Collation controls ALL comparison operators, not just `ORDER BY`.
- **MySQL:** Default `utf8mb4_0900_ai_ci` (accent-insensitive, case-insensitive). All string comparisons use the column's collation.
- **SQL Server:** `COLLATE SQL_Latin1_General_CP1_CI_AS` suffix on expressions. `CI` = case-insensitive, `CS` = case-sensitive.
- **Oracle:** `NLS_COMP = LINGUISTIC` changes all WHERE-clause string comparisons to use linguistic (locale-aware) rules.

**What SQL is actually doing:** SQL's string ordering is **collation-based ordering**, not "ordinal comparison." When a SQL user writes `WHERE name < 'M'`, the database uses the column's collation to determine character ordering. In a case-sensitive collation, `'z' < 'M'` is TRUE (uppercase comes before lowercase in Unicode). In a case-insensitive collation, `'z' > 'M'` is FALSE (z comes after m in dictionary order).

**Why this matters for Precept:** SQL has a well-documented, database-level collation system. The complexity is enormous — every database has dozens of collations, each with different rules. Precept's philosophy says: if the complexity is this high, make the user **explicit** about which ordering they want. Don't assume "ordinal" is the default — because "ordinal" (Unicode code point order) is almost never what business logic actually wants.

**Verdict:** SQL uses string ordering everywhere because it's the only string type available. Precept should not replicate this anti-pattern — structured types (`date`, `integer`, `choice ordered`) are the right model.

### Excel / Google Sheets

**What they do:** String comparison in formulas uses **case-insensitive** ordinal comparison (like `StringComparison.OrdinalIgnoreCase` in .NET). `"Apple" < "banana"` is TRUE.

**Where it's used:** Primarily for `IF(AND(B2>"500", C2="Yes"), ...)` patterns — business rules that happen to use string comparisons. But the dominant pattern is: numbers stored as strings in spreadsheets, compared as strings. This is why spreadsheet formulas are notorious for producing wrong results with `"10" < "9"`.

**Verdict:** Spreadsheet behavior is the **worst precedent** — it codifies the very anti-pattern (treating strings as numbers) that structured types exist to prevent. Don't follow this.

### Programming Languages

**C#/.NET:** `string.CompareTo()`, `string < string` use `CompareInfo` with locale-specific rules. Two modes: ordinal (`StringComparison.Ordinal`) and culture-aware (`StringComparison.CurrentCulture`). Even in .NET, the default (`CurrentCulture`) produces surprising results for non-native speakers.

**JavaScript:** `String.localeCompare()` is locale-aware; `>` / `<` on strings use internal ordinal comparison. Inconsistent behavior depending on which operator you use.

**Python:** String comparison uses Unicode code point order. `ord('z') > ord('A')` — uppercase always sorts before lowercase. This is technically correct but semantically meaningless for business logic.

**Verdict:** In every language, string ordering is acknowledged as **problematic for business logic**. The solutions are always: use a structured type (`Date`, `Decimal`), use locale-aware comparison, or be explicit about ordinal behavior.

### No-Code Platforms (Airtable, Notion, etc.)

**Airtable:** Has `BLANK`, `CHECKBOX`, `COUNT`, `DATE_CREATED`, `DATE_MODIFIED`, `EMAIL`, `MULTISELECT`, `NUMBER`, `PHONENUMBER`, `RATING`, `SELECT`. **No string comparison operators** (`<`, `>`) in formula language. The formula language provides `IF`, `AND`, `OR`, `NOT`, `SWITCH`, `VALUE()`, `TEXT()`, `DATEADD()`, `DATETIME_DIFF()`, `FORMAT()`, etc. — but no string ordering.

**Notion:** Has basic formulas with `prop()` accessors. No string comparison operators for filtering at formula level (filters are UI-level only).

**Verdict:** No-code platforms **do not expose string ordering** in their formula languages. The reasoning is clear: string comparison is not a business-domain primitive. If you need to sort, the UI does it. If you need classification, you use `SWITCH` on discrete values.

---

## 3. Can Ordered Choice Types Cover the Use Cases?

### Discrete Ranks (Priority, Status, Severity)

| Example | Ordered choice expression | Business meaning |
|---------|--------------------------|-----------------|
| Priority: Low < Medium < High | `field Priority as choice("low", "medium", "high") ordered default "low"` | Explicit, self-documenting, type-safe |
| Severity: 1 < 2 < 3 < 4 < 5 | `field Severity as choice("P1", "P2", "P3", "P4", "P5") ordered default "P3"` | Finite set, enumerated, cannot invent new values |
| SLA: Bronze < Silver < Gold < Platinum | `field SLA as choice("bronze", "silver", "gold", "platinum") ordered default "bronze"` | Same pattern |

**Verdict:** **Ordered choice is the perfect model for discrete ranks.** It's explicit, self-documenting, finite, and type-safe. The compiler enforces that only declared members exist. This covers 90%+ of business use cases where someone might reach for string ordering.

### Version-Like Ordering

| Example | Why ordered choice fails | Correct model |
|---------|------------------------|---------------|
| `SemVer: 1.0.0, 2.1.3, 99.0.0` | Open-ended — can't enumerate all versions | **Dedicated `version` type** |
| `ISO dates: 2024-01-01, 2025-12-31` | Open-ended range | **`date` type** (already exists) |
| `Version numbers: v1, v2, v10` | Can't enumerate | **Dedicated `version` type** |

**Verdict:** Ordered choice cannot cover open-ended value spaces. But the solution isn't string ordering — it's a **dedicated type**. For dates, Precept already has `date`. For versions, Precept would need a `version` type with semantic version parsing and comparison. For numeric ranges, Precept has `integer` and `decimal`.

### Alphabetical Display / Filtering

| Example | Ordered choice viable? | Correct model |
|---------|----------------------|---------------|
| "Sort customers by name" | No — names are infinite | **UI concern, not Precept** |
| "Filter by first letter" | No — arbitrary input | Built-in: `startsWith`, `charAt`, conditional logic |
| "Classify by name range A-M vs N-Z" | No — arbitrary input | **Conditional on extracted structure** |

**Verdict:** Alphabetical display is a UI concern. Filtering by arbitrary names requires open-ended input, which ordered choice cannot model. But the business rules that actually matter (classification by extracted structure) are better expressed with string accessors + conditionals, not string ordering.

### Codes with Implicit Order

| Example | Finite? | Ordered choice viable? |
|---------|---------|----------------------|
| SKU categories: A, B, C | **Yes** | **Yes** |
| Room numbers: 101, 102, 103... | **No** (extensible) | No — needs structured type |
| Legal filing: A1, B2, C3 | **Yes** (finite code space) | **Yes** |
| ZIP codes: 10001, 90210 | **Yes** (known range) | **Yes** (or `integer` type) |

**Verdict:** Ordered choice works for truly finite code spaces. For open-ended structured codes (room numbers, any-numeric-suffix), the correct model is a **structured parsing type** — not raw string comparison.

---

## 4. Cost/Benefit Analysis

### Cost of Adding String Ordering

| Component | Effort | Complexity |
|-----------|--------|-----------|
| `TypeTrait.Orderable` on `String` | S | Catalog change |
| 4 new `OperationKind` values (StringLessThanString, etc.) | S | Enum + catalog entry |
| 4 new `BinaryOperationMeta` in `Operations.cs` | S | Metadata |
| Graph analyzer support | M | Cross-reference `Orderable` trait |
| Evaluator support | M | Runtime `Compare` delegate |
| Type checker support | M | Resolve `<`/`>` on strings |
| CI variant (`~` prefix) | L | Requires `HasCIVariant: true`, `CIDiagnosticCode`, enforcement logic, `Tilde` prefix handling |
| Proof system integration | M | Modifier requirement or trait check |
| Grammar / completions / semantic tokens | S | Generated from catalog |
| Docs (spec, primitive-types, runtime) | M | Update all affected docs |
| Tests | M | Unit tests, integration tests, property tests |
| **Total** | **H** | **High complexity** |

**The CI non-CI cascade is the killer:** Adding string ordering means adding the full CI cascade:
1. `~` prefix keyword support (already exists for string types, but not for comparison operators on strings)
2. `HasCIVariant: true` + `CIDiagnosticCode` on `BinaryOperationMeta`
3. `DiagnosticEnricher` logic to detect `~` on ordering operations and emit the right diagnostic
4. Evaluator logic for `CompareInfo.Compare(string1, string2, CompareOptions.IgnoreCase)`
5. Tests for all CI/non-CI combinations
6. Grammar update to allow `~` before `<`, `>`, `<=`, `>=`
7. Semantic tokens for the new CI operators

**This is NOT a small feature.** It's an entire subsystem (CI handling) applied to a new operation surface. Compare: CI for `==` and `!=` on strings was already implemented (OperationKind 197-198). Adding it to `<`, `>`, `<=`, `>=` means extending CI to 4 more operations.

### What Complexity Does String Ordering Add?

**1. Ordinal ≠ business ordering**
- `"100" < "9"` is TRUE ordinally. Any business user who writes this expects FALSE.
- `"Zebra" < "apple"` is TRUE (uppercase sorts before lowercase in Unicode). No business user expects this.
- `"café" < "cat"` depends on whether `é` is treated as base letter `e` + combining accent or as a single character.

**2. CI is locale-dependent**
- `"Å" < "A"` in Swedish: Å is a letter AFTER Z in the Swedish alphabet. `true`.
- `"Å" < "A"` in Unicode ordinal: FALSE (Å = 0x2125 < A = 0x41).
- `"ß" < "B"` in German: TRUE (ß is treated as ss, which starts with s).
- `"ß" < "B"` in Unicode: FALSE (ß = 0xDF > B = 0x42).

Precept would need to choose: ordinal (fast, simple, wrong for business), or CI (locale-dependent, ambiguous, still "wrong" for most business logic). There is no "correct" default for string ordering.

**3. Unicode normalization edge cases**
- `"é"` (U+00E9, precomposed) vs `"\u0065\u0301"` (e + combining acute). Ordinal comparison says they're different. Unicode normalization says they're the same.
- `"ﬁ"` (U+FB01, ligature) vs `"fi"`. Same problem.

**4. Developer gotchas**
- `"10" > "9"` → **TRUE** (wrong — 10 is not less than 9)
- `"-1" > "0"` → **TRUE** (wrong — -1 is not greater than 0)
- `"100" > "99"` → **TRUE** (correct, by coincidence)
- `"2" > "10"` → **TRUE** (wrong — 2 is not greater than 10)
- `"Z" < "a"` → **TRUE** (wrong for any alphabetical use case)

Every single one of these is a bug waiting to happen in production code.

### What Do Users Lose If String Ordering Is Removed?

**Nothing that can't be done better with structured types.**

| What they might want | Better alternative |
|---------------------|-------------------|
| Compare dates stored as strings | Use `date` type |
| Compare numbers stored as strings | Use `integer` or `decimal` type |
| Compare time values | Use `time` type |
| Compare version numbers | Use a `version` type (or parse to integer components) |
| Sort alphabetically | UI concern, not Precept |
| Filter by alphabetical range | Built-in string accessors + conditional logic |
| Finite enum comparison | Ordered choice type |
| Compare structured codes (A01 < A02) | Ordered choice (if finite) or dedicated type |

**The honest answer:** Users lose the ability to do the **wrong thing easily**. String ordering invites the anti-pattern (treating strings as numbers, comparing dates as strings, sorting with Unicode code point order). Removing it forces the correct model.

### What Problems Does String Ordering Invite?

**1. The "it works until it doesn't" bug**
Code like `if (item.SKU < "M100")` works for 99% of SKUs but silently fails for `"L50"` (which is NOT less than `"M100"` ordinally because `"L" < "M"` but `"50"` is never compared — the first differing character decides). This is the same `"10" < "9"` problem, just in a different domain.

**2. The CI cascade never ends**
Once you add CI for `<` and `>`, users will ask: "What about case-sensitive comparison?" "What about locale-aware comparison?" "What about accent-insensitive comparison?" Each answer requires a new variant, a new operator, or a new parameter — expanding the language surface.

**3. The type checker becomes more complex**
The type checker currently resolves `<` based on the `Orderable` trait on the operand types. For strings, this trait doesn't exist. Adding it means the type checker must now handle string as orderable, which means:
- Cross-referencing `Orderable` trait in `GraphAnalyzer`
- Proof requirements for string ordering
- Runtime type dispatch for `Compare` on strings
- CI variant handling (already complex for `==`/`!=`)

---

## 5. Verdict

### Remove String Ordering (Confirm the Current State)

**Recommendation: Remove the ordering claim from documentation. Do not implement it.**

**Rationale:**

1. **String ordering does not have genuine business use cases.** Every scenario where someone might reach for `string < string` is better served by:
   - **Ordered choice** — for finite, explicit value sets (priority, status, severity, category)
   - **Dedicated types** (`date`, `time`, `integer`, `decimal`) — for structured data that happens to serialize to strings
   - **Built-in string accessors + conditionals** — for classification by extracted structure
   - **UI concern** — for display-level sorting/filtering (not Precept's domain)

2. **The anti-pattern is well-documented.** Every system that exposes string comparison admits it's problematic. SQL has collation systems (complexity: 10+ collations per database). Excel has spreadsheet-level bugs from `"10" > "9"`. Python's string ordering produces counterintuitive results. JavaScript's `>` and `<` differ in behavior from `.localeCompare()`. Precept should not replicate these anti-patterns.

3. **Ordered choice covers 90%+ of cases.** Business logic almost always uses discrete, known value sets for classification — exactly what ordered choice models perfectly. The 10% that doesn't fit ordered choice uses structured data that should have a dedicated type.

4. **The cost/benefit ratio is terrible.** Adding string ordering means:
   - 4 new operations in the catalog
   - CI variant support (the full cascade: `~` prefix, diagnostics, evaluator logic)
   - Type checker updates
   - Graph analyzer updates
   - Proof system integration
   - Evaluator runtime support
   - Doc updates
   - Tests
   - All to support a feature that invites bugs

5. **Precept's philosophy says: structure the type, not the comparison.** If the data has a structure (date, number, version), the type should encode that structure. The type system then guarantees correct behavior. String ordering is the anti-pattern of encoding structure in strings rather than types.

### If Someone Insists: The Minimal Viable Path

If the decision is to add string ordering despite the above:

**Phase 1: Ordinal only (no CI)**
- Add `TypeTrait.Orderable` to `String`
- Add 4 `OperationKind` values: `StringLessThanString`, `StringGreaterThanString`, `StringLessThanOrEqualString`, `StringGreaterThanOrEqualString`
- Add `BinaryOperationMeta` entries in `Operations.cs`
- Update type checker and graph analyzer
- Update docs

**Phase 2: CI variant (if needed later)**
- Set `HasCIVariant: true` on the 4 operations
- Add `CIDiagnosticCode` entries
- Update `DiagnosticEnricher` for string ordering CI detection
- Update grammar for `~` prefix on ordering operators
- Update evaluator with `CompareInfo.Compare` delegate

**This is still a significant feature.** The CI cascade alone was weeks of work for `==`/`!=` on strings.

### Migration / Communication Plan

Since Precept does NOT currently implement string ordering for strings, there is **no migration needed**. The only action is:

1. **Fix the documentation** — Remove the ordering rows from `primitive-types.md` that claim `<`, `>`, `<=`, `>=` on `string` are supported. This is the doc-vs-implementation discrepancy flagged by the compiler readiness review (F-LANG-PRIM-01).

2. **Update the language spec** — Remove string from the "orderable same-type" section in `precept-language-spec.md`.

3. **Record the decision** — This research file serves as the rationale for never implementing string ordering.

4. **Teach the alternative** — When users reach for `string < string`, point them to:
   - Ordered choice for finite value sets
   - `date`/`time`/`integer`/`decimal` for structured data
   - Built-in string accessors for classification by structure

---

## 6. Summary Table

| Scenario | String ordering? | Ordered choice? | Structured type? | UI concern? |
|----------|-----------------|-----------------|------------------|-------------|
| Priority: Low < Medium < High | ❌ overkill | ✅ perfect | ❌ | ❌ |
| Severity: P1 < P2 < P3 | ❌ overkill | ✅ perfect | ❌ | ❌ |
| Version comparison | ❌ wrong algorithm | ❌ open-ended | ✅ `version` type | ❌ |
| Date comparison | ❌ wrong algorithm | ❌ open-ended | ✅ `date` type | ❌ |
| Room number sorting | ❌ wrong algorithm | ❌ open-ended | ✅ numeric type | Maybe |
| Alphabetical sort | ❌ meaningless | ❌ infinite set | ❌ | ✅ UI |
| Alphabetical classification | ❌ overkill | ❌ arbitrary input | ✅ string accessors | Maybe |
| Date-as-string ("2024-01-01") | ❌ fragile | ❌ open-ended | ✅ `date` type | ❌ |
| Number-as-string ("100") | ❌ wrong algorithm | ❌ open-ended | ✅ `integer` type | ❌ |
| Time-as-string ("13:00") | ❌ fragile | ❌ open-ended | ✅ `time` type | ❌ |
| SKU categories (A, B, C) | ❌ overkill | ✅ perfect | ❌ | ❌ |
| Arbitrary text comparison | ❌ meaningless | ❌ infinite set | ❌ | ✅ UI |

**Row count:** 0 rows where string ordering is the correct model. 14 rows where it's not.

---

## 7. Decision Rationale

**What:** Explicitly do not implement `<`, `>`, `<=`, `>=` for arbitrary string types. Clean up documentation to match current implementation (no string ordering exists).

**Why:**
- No genuine business use case for arbitrary string ordering. All scenarios are better served by ordered choice (finite sets), structured types (date, integer, decimal), or UI concerns.
- The CI/non-CI cascade for string ordering is a significant implementation cost (4 new operations + full CI variant support).
- String ordering invites well-documented anti-patterns (`"10" < "9"`, Unicode code point ordering, locale confusion).
- Precept's philosophy: encode structure in types, not in comparisons.

**Alternatives considered and rejected:**
1. **Add ordinal-only string ordering** — Rejected because ordinal ordering of arbitrary strings is almost never what business logic needs. `"Zebra" < "apple"` being TRUE is not a feature.
2. **Add CI string ordering** — Rejected because CI ordering is locale-dependent and still meaningless for arbitrary strings. SQL's collation system is the precedent for this complexity, and it's enormous.
3. **Add ordinal string ordering with a warning** — Rejected because warnings don't prevent the well-known bugs (`"10" < "9"`). The type system should prevent wrong behavior, not warn about it.
4. **Add a `version` type alongside string ordering** — Version type is still needed even if string ordering exists. Better to add the version type and not add string ordering.

**Precedent:**
- No-code platforms (Airtable, Notion) do not expose string ordering in formula languages.
- Excel's string comparison produces known bugs due to ordinal behavior.
- Every language library provides warnings about string comparison semantics.
- SQL databases use collation (complex) for string comparison because it's the only option.
- Ordered choice is the clean, explicit, finite alternative that covers discrete business value sets.

**Tradeoff accepted:**
We lose the ability to do the wrong thing easily. Users who might have written `if (SKU < "M100")` must instead use the correct model: an ordered choice for the SKU prefix category, or a structured type. This is the desired outcome — Precept should prevent incorrect data models, not enable them.

---

## 8. Appendix: Implementation Gap (F-LANG-PRIM-01)

The compiler readiness review flagged a specific discrepancy that this decision resolves:

**Doc claim:** `primitive-types.md:87-90, 447` claims `string` supports ordinal `<`, `>`, `<=`, `>=`.

**Implementation reality:** `Types.cs:301` declares `String` with `TypeTrait.EqualityComparable | TypeTrait.ChoiceElement` — **no `Orderable`**. `Operations.cs` has NO `StringLessThanString` or equivalent entries. Only `StringEqualsString` (129) and `StringNotEqualsString` (130) exist.

**Resolution:** Remove the ordering rows from the documentation. This research file provides the rationale for the decision. The fix is documentation-only — no implementation changes needed.

**Affected docs:**
- `docs/language/primitive-types.md` — Remove string ordering rows from operators table
- `docs/language/precept-language-spec.md` — Remove string from "orderable same-type" section
- `docs/Working/compiler-readiness-plan-2026-05-24.md` — Mark F-LANG-PRIM-01 as resolved by documentation-only fix

---

*Research completed: 2026-05-24*
