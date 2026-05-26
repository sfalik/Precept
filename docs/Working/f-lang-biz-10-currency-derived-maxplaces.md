---
status: Locked 2026-05-25 — refreshed 2026-05-26 (W-H precept-reviewer remediation: added Philosophy Alignment / Language Design Grounding / Architecture Grounding / Audience and Teachability / Semantic Rules sections; tracked follow-ups F-UP-BIZ-10-A through -C; corrected `:519-548` line-range to `:627-645`). Verbatim citation excerpts in § Language Design Grounding remain to be pulled before final lock.
phase-target: Phase 4 (compiler-readiness plan) — fold into business-domain follow-ups; not a Phase 3 commit
authored: 2026-05-25
author: Claude (/lifecycle-2-design — F-LANG-BIZ-10)
comparable-systems-research-status: partial — Java annotation precedent surveyed in-line; full external comparator survey (Joda-Money / JSR-354 / NodaMoney / SQL `NUMERIC(p,s)` derivation) carried over from `research/architecture/compiler/currency-precision-coupling-survey.md`. No directly comparable "derive constraint value from another annotation on the same declaration" pattern surfaced — declared as a partially-novel design choice.
sources-consulted:
  - `src/Precept/Language/Modifiers.cs:237-242` — current `Maxplaces` `ValueModifierMeta` declaration
  - `src/Precept/Language/Modifier.cs:125-147` — `ValueModifierMeta` record shape and `HasValue` flag
  - `src/Precept/Language/Types.cs:524-541` — Currency type-meta with `.minorUnit` accessor (Phase 3 addition)
  - `src/Precept/Language/Types.cs:506-522` — Money type-meta with `QS_Currency` qualifier shape
  - `src/Precept/Language/Types.cs:530-536` — four-accessor inventory on Currency (name / minorUnit / numericCode / symbol)
  - `src/Precept/Language/CurrencyCatalog.cs:9-15` — `CurrencyEntry` record with `MinorUnit: int`
  - `src/Precept/Language/CurrencyCatalog.cs:130-162` — ISO 4217 parsing and `MinorUnit` derivation
  - `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs:627-645` — current `Maxplaces` value validation (literal-integer only); `ValidateModifierValues` switch spans :619-688
  - `src/Precept/Pipeline/Parser.cs:526-569` — `ParseModifierList` already accepts arbitrary expressions for valued modifiers
  - `src/Precept/Pipeline/Parser.cs:540-549` — confirms parser is permissive; type-checker gates
  - `docs/language/business-domain-types.md:1556-1572` — Phase 3 Position 3 ratification: `maxplaces` is explicit-only
  - `docs/language/business-domain-types.md:1738-1742` — opt-in strict-mode framing; ISO 4217 minor-unit available as metadata only
  - `docs/language/business-domain-types.md:521` — current author-form `money in 'USD' maxplaces 2`
  - `docs/Working/compiler-readiness-plan-2026-05-24.md § Phase 3 — Resolved` — F-LANG-BIZ-02 Position 3 four-leg rationale
  - `research/architecture/compiler/currency-precision-coupling-survey.md` — external precedent on currency-precision coupling (Joda-Money / JSR-354 / NodaMoney / Stripe / Square / Adyen)
  - `docs/philosophy.md § Compile-time structural checking` — language commitment to compile-time enforcement
  - `CLAUDE.md § Catalog System` — catalog-first principle for language-surface extensions
  - Java Bean Validation 3.0 (Jakarta Validation) — `@DecimalMax`/`@DecimalMin` accept string-form constants; no annotation-cross-reference mechanism
  - C# language spec — contextual keywords (`var`, `value`, `nameof`, `dynamic`) — Microsoft Learn "C# Keywords" reference, accessed 2026-05-25
  - C# language spec — CS0233 (`sizeof` on managed-reference type) — Microsoft Learn .NET docs, accessed 2026-05-25
  - Roslyn — `nameof` recognition pattern (`src/Compilers/CSharp/Portable/Binder/Binder_Expressions.cs`, Microsoft .NET source browser, accessed 2026-05-25)
  - TypeScript template literal types — compile-time derivation requires statically known substrings (TypeScript handbook, accessed 2026-05-25 — partial source consultation)
  - SQL:2016 standard — `CHECK` constraint semantics; dialect-specific function whitelists (partial source consultation — no verbatim excerpt available without standards-body access)
---

# F-LANG-BIZ-10 — Currency-derived `maxplaces` for money fields

## Goal

When done: an author can write `field Cost as money in 'USD' maxplaces currency.minorUnit` and the type checker resolves `currency.minorUnit` to the integer `2` at compile time via `CurrencyCatalog.Get("USD").MinorUnit`, producing a `maxplaces 2` constraint with identical downstream behavior to the literal-integer form. The form is rejected with a teachable diagnostic when the field's currency qualifier is not a static literal (e.g., interpolated from another field's value).

## Scope

- **In scope**:
  - One new accepted form for the `maxplaces` modifier value on `money` fields: `currency.minorUnit` (a context-sensitive accessor that resolves at compile time against the field's declared currency qualifier).
  - Compile-time resolution path in `ValidateModifierValues` (TypeChecker.Validation.Modifiers.cs § Maxplaces arm).
  - New diagnostic for the dynamic-qualifier rejection case (currency qualifier not a static literal).
  - Doc-update enumeration on `docs/language/business-domain-types.md` (the §§ 521, 1556-1572, 1738-1742 surfaces).
  - Hover/completion updates on the LS side for the new form.

- **Out of scope**:
  - Other modifier values (`min`, `max`, `default`, etc.). This design adds one accepted form to one modifier on one type. Other modifier-value extensions are separate designs.
  - Other business-domain types (`price`, `exchangerate`, `quantity`). `price in 'USD/each'` has a currency axis and could in principle benefit; `exchangerate in 'USD' to 'EUR'` has two; `quantity` does not. All three are deferred to a follow-up design once the money case is validated in production.
  - Cross-currency precision derivation (e.g., `money in 'USD' maxplaces 'JPY'.minorUnit`). This design forbids the cross-currency form by construction — the syntax form `currency.minorUnit` is intentionally tied to the field's own declared qualifier.
  - Crypto / non-ISO 4217 currencies. Those are not in `CurrencyCatalog`; the catalog lookup fails the same way it does for typed-constant validation of an unknown currency code today.
  - Arithmetic in modifier-value position (e.g., `maxplaces currency.minorUnit + 1`). Limited to a single contextual member access; no general expression evaluation. The author who wants `+1` writes the integer.
  - Interpolated currency qualifiers (e.g., `money in '{f.code}' maxplaces …`). Forbidden by the design — the only legal antecedent for `currency.minorUnit` is a static-literal qualifier.

- **Deferred to future**:
  - Extension to `price` and `exchangerate` (a follow-up F-LANG-BIZ-12 or numbered accordingly — F-LANG-BIZ-11 was claimed 2026-05-26 by the boundary-precision spinoff renumbered out of F-LANG-BIZ-09).
  - Generalization to other contextual accessors (e.g., `unit.dimension` for quantity, `from.minorUnit` for exchangerate).
  - Allowing the same form on `default` to derive a default decimal-places display precision.

## Philosophy Alignment

This design must satisfy every applicable principle in `docs/language/precept-language-spec.md § 0.1` (the eleven principles) and `docs/philosophy.md`. Per-principle coverage:

| Principle | Coverage |
|---|---|
| **1. Authoring at the data level** | No change — `maxplaces` is a field-declaration modifier; `currency.minorUnit` is read at the same authoring surface. |
| **2. Cohesive single-file model** | Preserved — all enforcement lives in the `.precept` file; no external configuration. The integer comes from the compiler's catalog, not from runtime state. |
| **3. Compile-time over runtime** | **Load-bearing**. The whole point of `currency.minorUnit` is that resolution happens at type-check time, not runtime. If the field's qualifier is dynamic (`in '{Code}'`), the new diagnostic `MaxplacesCurrencyQualifierNotStatic` rejects the form rather than silently shifting enforcement to runtime. |
| **4. Catalog-driven semantics** | **Tension flagged** in Decision 4. The recognition is hardcoded in the `Maxplaces` arm of `ValidateModifierValues`'s kind-switch, not in catalog metadata. The right long-term shape is `ValueShapeMeta` on `ValueModifierMeta`. Tracked as a follow-up obligation when F-LANG-BIZ-12 (`price` / `exchangerate`) lands a second arm. See § Follow-ups. |
| **5. Prevention not detection** | Preserved — `maxplaces` violations remain compile-time. The currency-derived form computes the same integer the literal form does; downstream proof obligations are unchanged. |
| **6. Honesty about approximation** | No change — `maxplaces` is a precision constraint, not an approximation strategy. The currency-derived form makes the precision source explicit (ISO 4217 minor unit), not less explicit. |
| **7. Compile-time structural checking** | **Load-bearing**. `currency.minorUnit` resolves to a structural constraint identical to `maxplaces 2`. Authors using the new form do not lose any compile-time guarantee. |
| **8. Domain-expert audience** | The form `money in 'USD' maxplaces currency.minorUnit` reads aloud as "money in USD, max decimal places equal to the currency's minor unit" — domain-expert-readable. The contextual identifier `currency` echoes the qualifier the author just wrote, not a synthetic compiler concept. |
| **9. Mandatory rationale** | N/A — `maxplaces` is a structural modifier; no `because` clause. |
| **10. Totality** | Preserved — no new failure mode at runtime. |
| **11. Static completeness** | Preserved — compile-time resolution; runtime sees an integer indistinguishable from the literal form. |

No principle is regressed. Two principles (#4 catalog-driven semantics, #7 compile-time checking) are load-bearing and call out explicit obligations addressed in § Follow-ups.

## Language Design Grounding

The closest comparators across mainstream languages:

- **C# contextual keywords** (`var`, `value`, `nameof`, `dynamic`) — keywords that acquire meaning only in specific syntactic positions, without being reserved globally. Microsoft Learn ("C# Keywords"): contextual keywords are not reserved words; they have meaning only in defined contexts. `currency.minorUnit` mirrors this: `currency` is not a reserved identifier; it acquires meaning only in modifier-value position on a money field.
- **Roslyn `nameof(x)` recognition** — the C# parser sees `nameof` as an identifier; the binder identifies the pattern via context. Reference: `roslyn/src/Compilers/CSharp/Portable/Binder/Binder_Expressions.cs` (the `IsNameofOperator` check). Decision 4 mirrors this — parser is unchanged; type-checker recognizes the pattern.
- **F# anonymous record accessors** — type-provider context drives accessor resolution based on the surrounding declaration; the accessor expression itself is grammar-uniform.
- **Java Bean Validation 3.0 (Jakarta Validation)** — `@DecimalMax(value = "100.00")` and similar accept string-form constants but offer no cross-annotation derivation. The Bean Validation API has no equivalent to "derive constraint value from another annotation on the same declaration." Survey of `jakarta.validation.constraints.*` (Jakarta Validation 3.0 spec) confirms this. The design's Decision 2 acknowledges this is partially novel.
- **SQL `CHECK` constraint expressions** — every dialect ships a whitelist of allowed functions in `CHECK` context; the rest are flagged as non-deterministic. Decision 5's whitelist mirrors this pattern.

**Domain-specific comparators** (currency precision):

- **Joda-Money** — `Money.toBigMoney().rounded(scale, RoundingMode.HALF_EVEN)` where scale defaults from `currency.getDefaultFractionDigits()`. The Java API surfaces minor-unit precision as a programmatic property, not a type-level constraint — closer to Position 3 of F-LANG-BIZ-02 than to Joda-Money's stricter `Money` type.
- **JSR-354 (java.money)** — `MonetaryAmount.getCurrency().getDefaultFractionDigits()`. Same shape as Joda-Money.
- **Stripe / Square / Adyen** — enforce minor-unit precision at the wire boundary (request/response validation), not in the application type system. Aligns with the F-LANG-BIZ-02 Position 3 stance that precision enforcement relocates to boundaries.

> **Verbatim excerpts pending owner ratification.** Microsoft Learn / Jakarta Validation citations above are summarized. Before locking, pull verbatim excerpts (per the § 9a Citation Discipline requirement for irreversible decisions). The C# Keywords reference and Roslyn's `Binder_Expressions.cs` snippet are the load-bearing ones. Pull from Microsoft Learn and the dotnet/roslyn source mirror.

**Strongest external precedent for the chosen syntax**: C# contextual keywords. Strongest external precedent against: SQL's `INFORMATION_SCHEMA.CHARACTER_SETS.CHARACTER_SET_NAME` style (cross-reference by name → Option A — `'USD'.minorUnit`). Decision 2's tradeoff narrative addresses this.

## Architecture Grounding

The change is a single-arm extension to `ValidateModifierValues` (`src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs:627-645`). Pipeline impact:

- **Lexer**: no change.
- **Parser**: no change — `MemberAccess(Identifier("currency"), "minorUnit")` is grammar-valid today; only the type-checker recognizes its contextual meaning.
- **NameBinder**: no change.
- **TypeChecker** (`ValidateModifierValues` → `case ModifierKind.Maxplaces`): the existing arm validates literal-integer values. New: also accept the `MemberAccess(currency, minorUnit)` shape; resolve via `CurrencyCatalog.TryGet(code, out var entry)` against the field's static currency qualifier; stash the resolved `entry.MinorUnit` integer on the `ParsedModifier`. The downstream proof engine and runtime see the same integer as the literal form.
- **GraphAnalyzer**: no change.
- **ProofEngine**: no change — consumes the resolved integer from `ParsedModifier`.
- **Runtime/Evaluator**: no change.
- **Language Server**: completion + hover plumbing (Decision per design § 5 of Inventory).
- **MCP server**: completion items become visible through the existing catalog projection; no DTO changes required.

**Catalog discipline call** (per CLAUDE.md § Catalog System): the recognition adds one arm to an existing kind-switch in `ValidateModifierValues`. Per the non-negotiable rule (*"Never switch on `*Kind` enum identity to dispatch per-member behavior"*), this is a debt rather than a violation — the kind-switch already exists; we're extending one arm, not introducing a new switch. The migration target is `ValueShapeMeta` on `ValueModifierMeta`, described in § Follow-ups.

## Audience and Teachability

**Authored form**:

```precept
field Cost as money in 'USD' maxplaces currency.minorUnit
```

Reads aloud as: *"Field Cost is money in USD with max decimal places equal to the currency's minor unit."* The currency reference is explicit (`'USD'`), the precision source is explicit (`currency.minorUnit`), and the language doesn't pretend either is implicit.

**Hover surface**:

- Hover on `currency.minorUnit` (LS handler): `"currency.minorUnit → 2 (USD, ISO 4217 minor unit)"` — shows the resolved integer alongside the currency code and its source. Author can immediately see what the compiler computed.
- Hover on `maxplaces`: existing description, no change.

**Completion surface**:

- After typing `maxplaces ` on a money field with a static currency qualifier, LS offers `currency.minorUnit` alongside the existing integer-hint completion.

**Diagnostic teachability** (when wrong):

- `field Cost as money in '{Code}' maxplaces currency.minorUnit` → `MaxplacesCurrencyQualifierNotStatic`: *"'currency.minorUnit' requires a static-literal currency qualifier (e.g., 'in 'USD''). The field's currency qualifier here is dynamic and cannot be resolved at compile time."* RecoverySteps direct the author to either pin the qualifier or fall back to literal integer.
- `field Cost as money in 'USD' maxplaces currency.numericCode` → `InvalidModifierValue` with explanation: *"Only `currency.minorUnit` is recognized in `maxplaces` modifier-value position. `.numericCode` is the ISO 4217 numeric code, not a precision value."*

**10-minute teaching path** (sample author who has used `maxplaces 2` before):

1. Author sees in a canonical sample: `field Cost as money in 'USD' maxplaces currency.minorUnit`.
2. Hovers on `currency.minorUnit` — sees `→ 2 (USD, ISO 4217 minor unit)`. Understands: same as `maxplaces 2`, but compiler reads the integer.
3. Tries on their own JPY field: `field Yen as money in 'JPY' maxplaces currency.minorUnit`. Hovers — sees `→ 0`. Understands the form generalizes.
4. Tries with dynamic qualifier — sees `MaxplacesCurrencyQualifierNotStatic` diagnostic. Reads RecoverySteps. Understands the static-qualifier requirement.

The teaching path requires no external doc reading; the LS + diagnostics surface the meaning.

## Semantic Rules

The contextual accessor `currency.minorUnit` is governed by the following rules. Each is testable; failure modes have diagnostic codes.

1. **Recognition position**: `currency.minorUnit` is recognized only as the value expression of a `maxplaces` modifier on a `money` field. Outside this position, the same parse tree (`MemberAccess(Identifier("currency"), "minorUnit")`) is treated as a normal identifier reference and emits `UnknownIdentifier` (no `currency` variable in scope).

2. **Target type requirement**: the owning field must be `money`. On `quantity`/`price`/`exchangerate` or any non-money type, the type checker emits `InvalidModifierValue` with the standard message — the contextual accessor does not generalize to other types in this design.

3. **Static-qualifier requirement**: the field's `in '<Cur>'` qualifier must be a static-literal currency code. Interpolated qualifiers (`in '{Code}'`) emit `MaxplacesCurrencyQualifierNotStatic`. The new diagnostic carries two RecoverySteps: pin the qualifier, or fall back to the integer-literal form.

4. **Catalog lookup**: resolution calls `CurrencyCatalog.TryGet(code, out var entry)`. If the code is unknown, the typed-constant validation for the qualifier itself already emits `UnknownCurrencyCode` upstream; the modifier-value path silently short-circuits to avoid double-reporting.

5. **Whitelist scope**: only the literal accessor name `minorUnit` is recognized. The other Currency accessors (`name`, `numericCode`, `symbol`) parse cleanly but are rejected with `InvalidModifierValue` carrying an explanation. The whitelist is hardcoded in the type checker — see § Follow-ups for the catalog-driven migration path.

6. **Resolution timing**: at type-check time. The resolved integer is stashed on `ParsedModifier` indistinguishably from a literal-integer form. Downstream consumers (proof engine, runtime evaluator) see the integer, never the contextual accessor.

7. **Equivalence**: `maxplaces currency.minorUnit` on `money in 'USD'` is semantically identical to `maxplaces 2` — same constraint, same proof obligations, same runtime enforcement. The forms differ only in author surface; the compiler does not distinguish.

## Inventory of what will be built

### Code changes

**1. Type checker — extended modifier-value validation**

`src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` — `ValidateModifierValues`, `case ModifierKind.Maxplaces` arm:

- Today: accepts `LiteralExpression { LiteralKind: NumberLiteral }` only; rejects everything else as `InvalidModifierValue`.
- After: also accepts a new shape `ContextualMemberAccess { Antecedent: "currency", Member: "minorUnit" }` (or equivalent — see Decision 4 for the AST shape choice). Resolution:
  1. The owning field's declared type must be `money` (already a precondition — `Maxplaces.ApplicableTo = BusinessMagnitudeTypes`).
  2. The field's declared `in '<Cur>'` qualifier must be a **static-literal currency code** (not interpolated). If not, emit `MaxplacesCurrencyQualifierNotStatic` (new diagnostic — see below).
  3. Resolve via `CurrencyCatalog.TryGet(code, out var entry)`. If the code is unknown, the typed-constant validation for the qualifier itself already emits an `UnknownCurrencyCode` diagnostic upstream; the modifier-value path can short-circuit silently in that case (no double-report).
  4. The resolved integer (`entry.MinorUnit`) is stashed on the `ParsedModifier` as a normalized `int` value — downstream proof-engine and runtime consumers see the same numeric value they would see for a literal-integer form.

**2. New diagnostic code**

`src/Precept/Language/DiagnosticCode.cs` — add `MaxplacesCurrencyQualifierNotStatic`.

`src/Precept/Language/Diagnostics.cs` — wire `GetMeta` arm with `Severity: Error`, `Category: Validation`, `RecoverySteps`:
- `"Replace the interpolated currency qualifier with a static literal (e.g., 'USD') so the compiler can resolve currency.minorUnit at compile time."`
- `"Or replace 'currency.minorUnit' with a literal integer (e.g., 'maxplaces 2')."`

Message template: `"'currency.minorUnit' requires a static-literal currency qualifier (e.g., 'in \\'USD\\''). The field's currency qualifier here is dynamic and cannot be resolved at compile time."`

**3. Modifier metadata — no change**

`Maxplaces` stays a `ValueModifierMeta` with `HasValue: true`. The parser already accepts arbitrary expressions (`Parser.cs:546` — `valueExpr = ParseExpression(...)`); no grammar change.

**4. New AST/IR shape for the contextual accessor**

The parser today produces `ParsedExpression` for the modifier value. The contextual-accessor form parses naturally as a `MemberAccess(Identifier("currency"), "minorUnit")`. The type checker recognizes this pattern in the `Maxplaces` arm and treats it as the contextual accessor; outside that arm, the same parse tree is a normal identifier reference that will fail to resolve (no field/variable named `currency` in scope).

This choice keeps the grammar invariant: the parser does not need to know about "contextual identifiers"; the type-checker is the only place that recognizes the pattern, gated by the modifier kind.

**5. Language-server completion + hover**

- `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` — when completing a `maxplaces` value on a `money` field whose qualifier is a static literal, offer `currency.minorUnit` as a completion alongside the standard integer-literal hint. (Existing `HasValue`-driven completion at line 3562 is the wiring point.)
- Hover on `currency.minorUnit` in modifier-value position resolves to the integer the compiler computed: `"currency.minorUnit → 2 (USD, ISO 4217 minor unit)"`.

### Doc changes

- `docs/language/business-domain-types.md § Maxplaces` (line ~1556) — add the new form alongside the integer-literal form, with one example.
- `docs/language/business-domain-types.md § money field constraints` (line ~521) — update the constraint-form summary.
- `docs/language/business-domain-types.md § Opt-in strict mode` (line ~1738) — show the currency-derived form as the idiomatic ISO-strict expression.
- `docs/language/catalog-system.md § Modifiers § Maxplaces` — note the new accepted value form; cross-link to the contextual-accessor rule.
- `docs/compiler/diagnostic-system.md` — register `MaxplacesCurrencyQualifierNotStatic` with RecoverySteps.
- `docs/compiler/type-checker.md § Modifier-value validation` — document the contextual-accessor resolution rule (gated by modifier kind, not parser-level).

### Test stubs

`test/Precept.Tests/TypeChecker/MaxplacesCurrencyDerivedTests.cs` (new file, or extend `TypeCheckerModifierValidationTests.cs` if one exists by that shape):

- `[Fact] CurrencyDerived_MoneyUsd_ResolvesToTwo` — `field Cost as money in 'USD' maxplaces currency.minorUnit` → compiles clean; resolved `MaxplacesValue == 2`.
- `[Fact] CurrencyDerived_MoneyJpy_ResolvesToZero` — `field Yen as money in 'JPY' maxplaces currency.minorUnit` → compiles clean; resolved `MaxplacesValue == 0`.
- `[Fact] CurrencyDerived_MoneyBhd_ResolvesToThree` — `field Dinars as money in 'BHD' maxplaces currency.minorUnit` → compiles clean; resolved `MaxplacesValue == 3`.
- `[Fact] CurrencyDerived_InterpolatedQualifier_EmitsDiagnostic` — `field Cost as money in '{Code}' maxplaces currency.minorUnit` → emits `MaxplacesCurrencyQualifierNotStatic`.
- `[Fact] CurrencyDerived_UnknownCurrencyCode_NoDoubleReport` — `field Cost as money in 'ZZZ' maxplaces currency.minorUnit` → emits only the upstream `UnknownCurrencyCode`; the modifier-value path stays silent.
- `[Fact] CurrencyDerived_RejectedOnNonMoneyTypes` — `field W as quantity in 'kg' maxplaces currency.minorUnit` → emits `InvalidModifierValue` (no `currency` antecedent on `quantity`).
- `[Fact] CurrencyDerived_ProducesSameDiagnosticAsLiteral_WhenValueExceeds` — `field Cost as money in 'USD' maxplaces currency.minorUnit default '1.999 USD'` → emits the same `MaxplacesViolation` diagnostic as the literal-integer form would.

## Decisions

### Decision 1: Add a new accepted form for the `maxplaces` value — do not implicitly derive from the currency

- **Stakes**: **medium**. Reversible at the language-surface level — withdrawing the new form would invalidate any author file using it, but the literal-integer form remains the documented baseline. Not externally-author-visible until the form ships in a release.
- **Rationale**: Phase 3 settled F-LANG-BIZ-02 Position 3 ("decoupled at the default surface; explicit `maxplaces N` as the opt-in strict mode"). F-LANG-BIZ-10 preserves that decoupling — `maxplaces` remains explicit; the new form only **eliminates the literal-integer boilerplate** when the author has already opted in by writing `maxplaces …`. Implicit derivation (regressing to D10) is rejected because it would re-introduce the silent precision constraint that Position 3 retired.
- **Alternatives considered**:
  - **Implicit derivation (D10 redux)**: `money in 'USD'` carries `maxplaces 2` automatically. Rejected: directly contradicts Phase 3 Position 3 and the four-leg rationale documented at `docs/Working/compiler-readiness-plan-2026-05-24.md § Phase 3 — Resolved`. The catalog-precision-coupling survey concluded this is the wrong default.
  - **No new form (status quo)**: authors continue to write `maxplaces 2`. Rejected because the author has to remember `2` for USD, `0` for JPY, `3` for BHD — the catalog already knows; the language can read from the catalog without the author re-asserting.
- **Precedent**: Joda-Money's `MoneyUtils.toMinorUnits` reads `Currency.getDefaultFractionDigits()` programmatically — the author who wants ISO-derived precision writes `currency.getDefaultFractionDigits()` rather than hardcoding the integer. Java Bean Validation does not have an equivalent "derive constraint value from another annotation"; the design accepts that this specific cross-annotation derivation is partially novel.
- **Tradeoff accepted**: A second accepted form for one modifier means more documentation surface and one more test matrix. The catalog grows one compile-time-evaluable accessor pattern. The author who reads `maxplaces 2` next to `maxplaces currency.minorUnit` in two different files has to learn that both are valid.
- **Sources consulted for this decision**:
  - `docs/Working/compiler-readiness-plan-2026-05-24.md § Phase 3 — Resolved` — *"Position 3 — decoupled at the default surface, with explicit `maxplaces N` as the opt-in strict-mode."*
  - `docs/language/business-domain-types.md:1556` — *"`maxplaces` is available on all four magnitude types and is **explicit-only** for all of them — including `money`."*
  - `research/architecture/compiler/currency-precision-coupling-survey.md` (Phase 3 grounding research) — survey conclusions on Joda-Money / JSR-354 / NodaMoney precedent.
  - Java Bean Validation 3.0 (Jakarta Validation) — `@DecimalMax(value = "100.00")` etc. accept string-form constants but offer no cross-annotation derivation pattern.

### Decision 2: Syntax form — `currency.minorUnit` (Option B), not `'USD'.minorUnit` (Option A)

- **Stakes**: **irreversible**. Once a syntax form ships into author-visible language surface, retracting it invalidates every author file using it. This is the single highest-risk decision in this design.
- **Rationale**: Option B (`currency.minorUnit`) names the field's own declared currency qualifier as the antecedent — a single source of truth. Option A (`'USD'.minorUnit`) re-states the currency code in two places (`in 'USD'` and `'USD'.minorUnit`), creating room for the codes to diverge silently (e.g., `money in 'USD' maxplaces 'JPY'.minorUnit` would compile and produce a `maxplaces 0` constraint on a USD field — author surprise without a teachable diagnostic). Option B is DRY by construction.
- **Alternatives considered**:
  - **Option A — `maxplaces 'USD'.minorUnit`** (typed-constant member access): The currency code is restated; the compiler can compare the modifier-value's currency against the qualifier's currency and warn on mismatch. Rejected because:
    1. The mismatch case (`'JPY'.minorUnit` on a USD field) is incoherent — the author either meant to use USD's minor unit or wanted some cross-currency override. Allowing the form invites the misuse. Disallowing the mismatch silently makes the form equivalent to Option B at higher cost.
    2. Re-stating the currency code is the literal-integer form's flaw (the author has to keep two strings in sync — `'USD'` and `2`), just with a different second string.
    3. Option A would naturally generalize to "any typed-constant accessor in modifier-value position," which is a much larger language change than this design wants to commit to.
  - **Option C — bare sentinel `maxplaces native` or `maxplaces auto`**: Opaque to a reader. Doesn't extend to a future world where someone wants the analogous derivation on `unit.dimension` for quantity. Rejected.
  - **Option D — leading-dot accessor `maxplaces .minorUnit`** (Swift-style implicit-member): More concise; reads as "the type's member." Rejected because the antecedent is ambiguous to a reader — *whose* `.minorUnit`? — and Precept has no other contextual-member shorthand to anchor the reader's intuition. The `currency.` prefix makes the antecedent legible at the cost of one extra word.
  - **Option E — verbose `maxplaces self.currency.minorUnit`**: Adds `self` for nothing. Rejected.
- **Precedent**:
  - C# contextual keywords (`var`, `value`, `nameof`, `dynamic`) — context-sensitive identifiers that are not reserved globally but acquire meaning in specific syntactic positions. `currency` in modifier-value position on a money field is analogous: it is not a reserved word; it acquires meaning only inside this specific context.
  - F# anonymous record field accessor in type-providing context (`record.SomeField`) — type-checker recognizes the shape based on the surrounding declaration.
  - **No direct precedent surfaced** for "context-sensitive identifier inside a modifier/annotation value that refers to a sibling annotation on the same declaration." Declared honestly: this is a partially-novel choice. The closest analog is Java Bean Validation's groups (`@NotNull(groups = Foo.class)`) where `Foo.class` is resolved against the class file's metadata — but that is type-level reflection, not annotation-cross-reference.
- **Tradeoff accepted**:
  - The author who reads `maxplaces currency.minorUnit` and tries to write `maxplaces currency.numericCode` (a different `currency` accessor that happens to return integer) will be surprised when the diagnostic rejects it. Resolution: the type checker accepts only the specific shape `currency.minorUnit` — see Decision 5 — and the rejection diagnostic explains the constraint.
  - Adds a second contextual-identifier pattern to the language (the first being `default` in some constructs). The author who reads Precept files now has to know two contextual patterns.
  - Once shipped, the syntax form is irreversible without breaking authors.
- **Strongest counter-evidence**:
  - The leading-dot `.minorUnit` form (Option D) is the natural extension if Precept ever adopts a Swift-style implicit-member access for any other purpose. If that decision happens later, `.minorUnit` would be the better-aligned syntax, and `currency.minorUnit` would look like an outlier. **Mitigation**: Decision 5 confines the pattern to a single recognized shape (`currency.minorUnit`); future contextual-accessor work can introduce `.member` as a parallel form without retracting Decision 2.
  - **Strong external precedent for Option A in some communities**: SQL's `INFORMATION_SCHEMA.CHARACTER_SETS.CHARACTER_SET_NAME` style — where you cross-reference catalog metadata by name — is closer to Option A. **Mitigation**: SQL's catalog references are runtime, not compile-time; the precedent does not transfer cleanly.
- **Reversibility**: Once shipped, retracting `currency.minorUnit` invalidates every author file using it. Mitigations:
  - Ship initially with `currency.minorUnit` recognized only inside the `Maxplaces` arm. If the form proves wrong, downgrade to a `Deprecated` diagnostic in a subsequent release and migrate authors to the integer-literal form.
  - Reject all related forms (`currency.numericCode`, `currency.name`, `currency.symbol`) in modifier-value position even when they parse cleanly — see Decision 5. This keeps the syntactic surface narrow enough to retract if needed.
- **Blast radius**: Author-visible language surface. Affects every author who declares a money field with `maxplaces`. Compile-time semantics; no runtime impact (the value is resolved at type-check time and stashed as the same integer the literal form would produce).
- **Sources consulted for this decision**:
  - `src/Precept/Language/Modifiers.cs:237-242` — *`ModifierKind.Maxplaces => new ValueModifierMeta(kind, Tokens.GetMeta(TokenKind.Maxplaces), "Maximum decimal places", ModifierCategory.Structural, BusinessMagnitudeTypes, HasValue: true, …)`*
  - `src/Precept/Pipeline/Parser.cs:540-549` — *"Valued modifiers parse an expression for their value … `valueExpr = ParseExpression(0, …)`"* — confirms parser is permissive; type-checker gates.
  - C# language spec (Microsoft Learn, accessed 2026-05-25) — contextual keywords list including `var`, `value`, `nameof`, `dynamic`: *"A contextual keyword is used to provide a specific meaning in the code, but it is not a reserved word in C#."* (Excerpt from Microsoft's "C# Keywords" reference page.)
  - Java Bean Validation 3.0 (Jakarta Validation) constraint annotations — `@DecimalMax`, `@DecimalMin`, `@Digits` — accept string-form value constants but offer no annotation-cross-reference pattern. (Survey of Jakarta Validation 3.0 API spec; no analogous mechanism present.)

### Decision 3: Only valid when the `in '<Cur>'` qualifier is a static literal

- **Stakes**: **high**. External-author-visible: the diagnostic the author sees when they write the form on a dynamically-qualified field shapes their mental model of the feature. Reversible at the diagnostic level (can soften from error to warning, can change the message), but the underlying capability (compile-time resolution) cannot be softened without changing what the form does.
- **Rationale**: `CurrencyCatalog.TryGet(code, out _)` requires a known code at compile time. If the field's qualifier is `in '{f.code}'` (interpolated from another field), the code is unknown until runtime. The compile-time resolution fails by construction. The design either (a) errors out at type-check time with a teachable diagnostic (chosen), (b) emits a runtime fallback (rejected — would silently shift constraint timing), or (c) lifts the constraint to runtime entirely (rejected — would break Precept's "compile-time structural checking" principle from `docs/philosophy.md`).
- **Alternatives considered**:
  - **(b) Runtime resolution fallback**: Compile to a runtime catalog lookup at constraint-check time. Rejected because the modifier-value's semantics would change depending on whether the qualifier is static or dynamic — a hidden behavioral fork that makes the language harder to reason about.
  - **(c) Drop the static-qualifier restriction by deferring constraint resolution to runtime**: Rejected for the same reason; also conflicts with `maxplaces` being a structural constraint that desugars to a proof obligation.
  - **Silent skip when qualifier is dynamic**: Rejected — author who writes `currency.minorUnit` on a dynamic-qualifier field expects *something* to happen; the compiler going silent invites the author to assume the constraint is in force when it isn't.
- **Precedent**:
  - C# `sizeof(T)` — only legal when `T` is a value type with a compile-time-known size. The compiler emits CS0233 when used on a managed reference type. Same pattern: compile-time evaluation that fails with a teachable diagnostic when the operand is not compile-time-resolvable.
  - TypeScript template literal types — derived types only when the template's substrings are statically known; otherwise the type falls back to `string`. Different language (gradual typing), but the same principle: compile-time derivation requires compile-time inputs.
- **Tradeoff accepted**: Authors who want a dynamic per-currency precision constraint cannot use `currency.minorUnit`; they must either pin the currency literally or accept that the precision constraint stays runtime-applied via a different mechanism (not in scope here). This is the same restriction that already applies to typed-constant validation in interpolated qualifiers.
- **Strongest counter-evidence**: A common authoring case is a multi-tenant precept where the currency varies per tenant and is stored in a field rather than pinned. Those authors get rejected for `currency.minorUnit` and have to write a workaround. **Mitigation**: the workaround is just the integer literal, which is fine for the multi-tenant case (the integer captures the same intent without the catalog cross-reference). The error message points the author at this workaround in its RecoverySteps.
- **Reversibility**: Diagnostic message and severity are reversible. The underlying compile-time-only nature is structural — softening it would change the feature into something else.
- **Blast radius**: Author-visible diagnostic on a feature that doesn't ship until this design lands; no existing authors are affected.
- **Sources consulted for this decision**:
  - `src/Precept/Language/CurrencyCatalog.cs:130-162` — *"`var minorUnit = ParseMinorUnit(GetChildValue(entry, "CcyMnrUnts")); entries.Add(alphaCode, new CurrencyEntry(alphaCode, numericCode, name, minorUnit, symbol));`"* — confirms `MinorUnit` is precomputed; the lookup is `O(1)` given a static code.
  - `docs/philosophy.md § Compile-time structural checking` — *"Unreachable states, dead-ends, type mismatches, contradictions, unsatisfiable guards, division-by-zero, overflow — all compile-time impossibilities."* — language commits to compile-time enforcement; this decision keeps the new form aligned with that commitment.
  - C# language spec — CS0233 (`sizeof` on managed-reference type) — Microsoft Learn (Microsoft .NET docs, accessed 2026-05-25): *"`sizeof` operator can be applied only to types whose size is constant."*

### Decision 4: Recognize the contextual accessor in the type checker, not in the parser

- **Stakes**: **medium**. Reversible at the architecture layer; rewriting the recognition path is a refactor, not a language-surface change.
- **Rationale**: The parser produces `MemberAccess(Identifier("currency"), "minorUnit")` for `currency.minorUnit` regardless of context. In modifier-value position on a money field, the type checker recognizes that shape and treats it as the contextual accessor. Outside that position, the same parse tree resolves as a normal identifier reference — which fails to find `currency` in scope and emits the existing `UnknownIdentifier` diagnostic. This means the grammar is unchanged, no new tokens, no new AST node — the catalog-first principle (`CLAUDE.md § Catalog System`) is preserved: the type checker uses metadata (the modifier kind and the field's declared type) to drive recognition.
- **Alternatives considered**:
  - **Add a new AST node `ContextualMemberAccess`**: Parser would emit this when it sees `currency.minorUnit` and the surrounding token is a modifier value. Rejected because it requires the parser to know about modifier semantics, which violates the layering (parser is grammar-only; semantic checks are in the type checker). Adds a new node to the AST inventory.
  - **Make `currency` a reserved keyword in some contexts**: Adds a new token. Rejected because (a) reserving a previously-unreserved identifier is a breaking change for any author who happened to name a field `Currency`, and (b) the contextual nature is exactly what makes this not require reservation.
- **Precedent**: Roslyn recognizes `nameof(x)` in expression contexts via the binder, not the parser — the parser sees `nameof` as an identifier; the binder identifies the pattern. This design mirrors that approach.
- **Tradeoff accepted**: A future reader of the parser cannot tell from grammar alone that `currency.minorUnit` has special meaning. The type checker's `Maxplaces` arm is the only place where the meaning is encoded. Mitigation: this design documents the contextual recognition explicitly in `docs/compiler/type-checker.md`.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/Parser.cs:526-569` — `ParseModifierList` uses `ParseExpression` without modifier-kind-specific dispatch; confirms the parser is layered correctly today.
  - `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs:519-548` — *"`/// PRE0035 — InvalidModifierValue: validate that modifiers with values carry valid values. For example, 'maxplaces' must be a non-negative integer.`"* — the type checker is the existing gate for value-shape validation; extending the gate is the natural seam.
  - Roslyn — `nameof` recognition pattern (Microsoft .NET source: `src/Compilers/CSharp/Portable/Binder/Binder_Expressions.cs` — accessed 2026-05-25 via Microsoft .NET source browser); confirms the type-checker-recognizes-not-parser-recognizes precedent.
  - `CLAUDE.md § Catalog System` — *"Never switch on `*Kind` enum identity to dispatch per-member behavior."* — the recognition lives in the `Maxplaces` arm of an existing kind-switch, which is the violation pattern to watch for. **Mitigation**: the kind-switch is already present and accepted for modifier-value validation; this design adds one arm to an existing switch rather than introducing a new kind-switch. If the modifier-value validation surface grows further, the whole switch should move to catalog-driven metadata on `ValueModifierMeta` (a `ValueShapeMeta` or similar) — explicitly out of scope here.

### Decision 5: Recognize only `currency.minorUnit` — reject `currency.name`, `currency.symbol`, `currency.numericCode` in modifier-value position

- **Stakes**: **high**. Author-visible diagnostic shape; constrains future extensibility. Reversible (can broaden the accepted set later) but not narrow-able (can't take a previously-accepted accessor away).
- **Rationale**: Of the four Currency accessors, only `.minorUnit` returns an integer that satisfies `Maxplaces`'s value contract (a non-negative integer). `.numericCode` also returns an integer but represents the ISO 4217 numeric code (e.g., 840 for USD) — using it as a decimal-places count is nonsensical. `.name` and `.symbol` return strings — type-incoherent for `maxplaces`. The recognition is type-driven: the contextual accessor must resolve to a non-negative integer matching `Maxplaces`'s declared value contract.
- **Alternatives considered**:
  - **Accept any `currency.<accessor>` whose return type matches the modifier's value type**: would automatically include `.numericCode`. Rejected because `.numericCode` *type-checks* (it's an integer) but is semantically wrong for precision.
  - **Reject by accessor-name whitelist hardcoded to `minorUnit`**: simpler. Chosen for this initial design.
  - **Add a `UseInModifierValueContext: bool` flag to `FixedReturnAccessor` and gate on it**: catalog-driven, generalizes naturally. Deferred — the catalog metadata extension is a larger change; for one accessor it's overkill. Filed as a follow-up if/when a second contextual accessor lands.
- **Precedent**: SQL `CHECK` constraint expressions accept arbitrary boolean-returning expressions but in practice every dialect ships a small whitelist of allowed functions in `CHECK` context (the rest are flagged as non-deterministic). Same shape: type-allows-many, design-allows-one.
- **Tradeoff accepted**: A future Currency accessor that returns an integer (e.g., a hypothetical `.iso4217Year`) would need its own whitelist entry. The catalog won't auto-include it.
- **Strongest counter-evidence**: An author with strong familiarity with `.numericCode` (e.g., a financial-systems integrator who already uses 840-for-USD in their domain) may reasonably try `currency.numericCode` as a precision source under the misapprehension that "minor unit" and "numeric code" are interchangeable currency-catalog data. The rejection diagnostic must explain *why* the form is rejected ("only `currency.minorUnit` is recognized in modifier-value position because `Maxplaces` is a precision constraint and the ISO 4217 numeric code is not a precision value") — not just *that* it's rejected. **Mitigation**: the test case `CurrencyDerived_RejectedOnNonMinorUnitAccessors` in the acceptance criteria verifies the diagnostic text contains the explanation.
- **Reversibility**: Broadening the accepted set later is safe. Narrowing is breaking.
- **Blast radius**: Author-visible diagnostic surface.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Types.cs:530-536` — *"`new FixedReturnAccessor("name", TypeKind.String, "Currency display name"), new FixedReturnAccessor("minorUnit", TypeKind.Integer, "Minor-unit decimal places (e.g., 2 for USD)"), new FixedReturnAccessor("numericCode", TypeKind.Integer, "ISO 4217 numeric code"), new FixedReturnAccessor("symbol", TypeKind.String, "Currency symbol (e.g., '$')")`"* — the four-accessor inventory.
  - SQL standard (SQL:2016) — `CHECK` constraint semantics; the standard permits any deterministic boolean expression but every dialect restricts the function set. (External survey; no verbatim excerpt — declared as partial source consultation.)

### Decision 6: Resolution timing — at type-check time, not at parse time

- **Stakes**: **low**. Internal pipeline-stage decision; not author-visible.
- **Rationale**: Resolution requires access to the field's declared type and its qualifier shape, which are not available at parse time. The type checker is the first stage where the necessary context exists.
- **Alternatives considered**:
  - **Resolve at constant-folding stage (if such a stage existed downstream of the type checker)**: Precept does not have an explicit constant-folding stage; the proof engine consumes the type-checker's `ParsedModifier` directly.
  - **Resolve at runtime**: Rejected — would defeat the compile-time constraint surface.
- **Precedent**: All other compile-time-derived modifier values (none today; this is the first) would naturally land at the same stage. Existing literal-integer validation already happens in `ValidateModifierValues`.
- **Tradeoff accepted**: None identified. The type checker is the obvious home.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs:519-548` — current validation home.

## Acceptance criteria

- `field Cost as money in 'USD' maxplaces currency.minorUnit` compiles clean and produces a `maxplaces 2` constraint indistinguishable in behavior from the literal-integer form (verified via test: the resolved `MaxplacesValue` field on `ParsedModifier` equals `2`).
- `field Yen as money in 'JPY' maxplaces currency.minorUnit` resolves to `maxplaces 0`.
- `field Dinars as money in 'BHD' maxplaces currency.minorUnit` resolves to `maxplaces 3`.
- `field Cost as money in '{Code}' maxplaces currency.minorUnit` emits `MaxplacesCurrencyQualifierNotStatic` with the two-step RecoverySteps documented in the Inventory section.
- `field Cost as money in 'ZZZ' maxplaces currency.minorUnit` emits only the upstream `UnknownCurrencyCode` diagnostic; the modifier-value path does not double-report.
- `field W as quantity in 'kg' maxplaces currency.minorUnit` emits `InvalidModifierValue` (the `currency` antecedent is not defined for quantity-type fields).
- `field Cost as money in 'USD' maxplaces currency.numericCode` emits a teachable diagnostic that (a) explains *why* the form is rejected (`Maxplaces` is a precision constraint; `.numericCode` is the ISO 4217 numeric code, not a precision value), (b) names the accepted contextual accessor (`currency.minorUnit`), and (c) mentions the integer-literal alternative (`maxplaces 2`).
- `field Cost as money in 'USD' maxplaces currency.name` and `… maxplaces currency.symbol` both emit `InvalidModifierValue` with the standard integer-required message (type mismatch — the literal-form gate catches these without reaching the contextual-accessor recognition path).
- `mcp__precept__precept_compile` on a sample using the new form returns clean.
- LS completion at `field Cost as money in 'USD' maxplaces |` offers `currency.minorUnit` alongside the integer-hint completion.
- LS hover on `currency.minorUnit` shows `"→ 2 (USD, ISO 4217 minor unit)"`.
- All existing `Maxplaces` tests continue to pass (no regression on the literal-integer form).

## Dependencies

- **Upstream**: Phase 3 of the compiler-readiness plan must be complete (lands `.minorUnit` accessor and `CurrencyCatalog.Get`/`TryGet` public API). Confirmed: commits `e07a084e` + `572d16a6` + `6b2d1040`.
- **Downstream**: Enables a future F-LANG-BIZ-12 (price/exchangerate analogous derivation) if/when that design is taken up. Enables a catalog-driven `ValueShapeMeta` refactor (per Decision 4's mitigation) if a second contextual accessor ever lands.

## Doc-update enumeration

| Doc | Section | Update |
|---|---|---|
| `docs/language/business-domain-types.md` | § money field constraints (line ~521) | Add `currency.minorUnit` to the constraint-form summary; show one example. |
| `docs/language/business-domain-types.md` | § Maxplaces (line ~1556) | Document the new accepted form and the static-qualifier restriction. |
| `docs/language/business-domain-types.md` | § Opt-in strict mode (line ~1738) | Update the idiomatic example to show `maxplaces currency.minorUnit` as the discoverable currency-derived form. |
| `docs/language/business-domain-types.md` | § Maxplaces enforcement scope (line ~1558) | No behavioral change — the resolved integer drives the same three-point enforcement (declaration / event-arg input / arithmetic result assignment). |
| `docs/language/catalog-system.md` | § Modifiers § Maxplaces | Cross-link to the contextual-accessor rule; note that this is the first contextual-identifier pattern in modifier-value position. |
| `docs/compiler/type-checker.md` | § Modifier-value validation | Document the contextual-accessor recognition path: parser is unchanged; type checker recognizes the shape in the `Maxplaces` arm. |
| `docs/compiler/diagnostic-system.md` | Diagnostic inventory | Register `MaxplacesCurrencyQualifierNotStatic`. |
| `docs/tooling/language-server.md` | § Completion / § Hover | Document the new completion item and the hover-resolution display. |
| `samples/insurance-claim-adjudication.precept` (or other canonical) | n/a | Migrate at least one `maxplaces 2` declaration to `maxplaces currency.minorUnit` so the new form is discoverable from a top-of-funnel sample. (Sample-edit-constraint exception — documented in commit message body.) |

## Open questions

None at lock time. The five remaining open questions surfaced during research are deferred to follow-up designs:

- Extension to `price` / `exchangerate` — out of scope (deferred).
- Generalization to other contextual accessors (e.g., `unit.dimension` for quantity) — out of scope (deferred until a second use case appears).
- `default` field-value derivation from currency metadata (e.g., `default '0 USD'`) — out of scope; `default` is a separate value modifier with its own value-validation path.
- Author-controlled non-ISO currency precision (crypto, regional currencies) — out of scope; not in the catalog.
- Whether `ValueShapeMeta` should land as a catalog extension (Decision 4 mitigation) — deferred; revisit if a second contextual-accessor design is taken up.

## Falsifiers

This design ships **one irreversible decision** (Decision 2 — the `currency.minorUnit` syntax form). Specific observations that would falsify the design and warrant retraction or revision:

1. **Authors adopt `maxplaces currency.minorUnit` rarely**. If a six-month audit of public/sample `.precept` files finds the literal-integer form (`maxplaces 2`) still outnumbering the currency-derived form by 5:1 or more, the design did not deliver the discoverability or ergonomic win it claims. Action: deprecate the form; migrate authors back to literal-integer; reclaim the syntax slot.
2. **The teachable diagnostic `MaxplacesCurrencyQualifierNotStatic` fires more often than expected in author files**. If telemetry (or audit) shows authors trying the form on dynamically-qualified fields at a rate >10% of total uses, the restriction (Decision 3) is too aggressive or the language-surface example documentation hasn't communicated the constraint clearly. Action: revisit the doc text and the diagnostic RecoverySteps.
3. **A second contextual accessor case appears that doesn't fit the type-checker-arm pattern**. If F-LANG-BIZ-12 (or any analogous design) wants to use `unit.dimension` for `quantity` and the type-checker-arm approach forces N-way duplication, Decision 4 was wrong and the catalog-driven `ValueShapeMeta` extension was the right call. Action: refactor the modifier-value validation to be catalog-driven; the `Maxplaces` arm migrates onto the new metadata.
4. **A directly-comparable system surfaces in the literature after the design ships**. The Decision 2 precedent claim ("no directly comparable annotation-cross-reference precedent") is partial — a surfaced precedent could either ratify or contradict the chosen syntax. If contradicting, the decision narrative loses one of its supports. Action: update the design doc; consider whether the contradicting precedent argues for a syntax change before authors lock in.
5. **The `.minorUnit` accessor returns a value that doesn't fit `Maxplaces`'s contract for some currency**. ISO 4217 lists currencies with `MinorUnit` values from 0 to 4 (and "N.A." for some entries — handled in `CurrencyCatalog` parsing); all are non-negative integers, satisfying `Maxplaces`. If a future ISO 4217 revision changes the schema, the assumption breaks. Action: gate the catalog lookup; emit `InvalidModifierValue` if the resolved value violates `Maxplaces`'s contract.

## Follow-ups

Tracked obligations surfaced by Decisions 4 and 5. These do not block W-H execution but must be carried forward as named, owned items.

### F-UP-BIZ-10-A: `ValueShapeMeta` catalog extension for modifier-value validation

**Trigger**: when a second contextual accessor lands (e.g., `unit.dimension` for `quantity`, or `from.minorUnit`/`to.minorUnit` for `exchangerate` in F-LANG-BIZ-12), or when modifier-value validation grows a fourth distinct shape category.

**Rationale**: Decision 4 added one arm to `ValidateModifierValues`'s existing per-kind switch. The switch is the catalog-discipline smell the CLAUDE.md non-negotiable warns about. Landing one more arm without refactoring would compound the smell. The right shape is a `ValueShapeMeta` (or equivalent metadata record) on `ValueModifierMeta` describing the accepted value-shape categories per modifier kind; the validator becomes catalog-driven.

**Scope at trigger time**:
- New `ValueShapeMeta` record (or DU) on `ValueModifierMeta` describing accepted value-shape categories (literal-integer, contextual-accessor-with-allowed-list, etc.).
- Migrate the `Maxplaces` arm and the new follow-up's arm onto the metadata-driven path.
- Retain backward compatibility: existing `maxplaces 2` continues to compile clean.

**Owner**: language compiler team. Filed against the compiler-readiness plan's "follow-ups" or Phase 7 (API surface solidity).

### F-UP-BIZ-10-B: catalog-driven whitelist for contextual accessors in modifier-value position

**Trigger**: same as F-UP-BIZ-10-A — when a second contextual accessor lands, or when authors request additional Currency accessors in `maxplaces` position.

**Rationale**: Decision 5 hardcoded the recognition to `currency.minorUnit` only. The longer-term shape is a `bool UseInModifierValueContext` flag on `FixedReturnAccessor` (or analogous metadata on the accessor catalog). The whitelist becomes catalog-driven; new accessors can be opted in without touching the type-checker arm.

**Scope at trigger time**: add the flag to `FixedReturnAccessor` (and any analogous accessor subtype); migrate the hardcoded `minorUnit` recognition to consult the flag.

**Owner**: language compiler team. Same phase target as F-UP-BIZ-10-A.

### F-UP-BIZ-10-C: extension to `price`, `exchangerate` (F-LANG-BIZ-12)

**Trigger**: when authors hit the same boilerplate on `price in 'USD/each' maxplaces 2` or `exchangerate in 'USD' to 'EUR' maxplaces 2`.

**Rationale**: this design is intentionally scoped to `money` because the `currency` antecedent is unambiguous there. `price` has a polymorphic `in` axis (currency, unit, or compound); `exchangerate` has two currency qualifiers (`from`, `to`). Each requires its own design pass on the accessor surface.

**Owner**: pending owner triage; not blocking Phase 4.

## Phase-target rationale

This design slots into **Phase 4 (collection completeness + business-domain follow-ups)** of the compiler-readiness plan — not Phase 3. Phase 3 already shipped the prerequisite work (the `.minorUnit` accessor and the public `CurrencyCatalog` API). F-LANG-BIZ-10 is the **first** in a likely series of business-domain ergonomic-improvement features that build on Phase 3's foundation; Phase 4's stub explicitly leaves room for these. Filing under Phase 4 keeps Phase 3 closed cleanly.
