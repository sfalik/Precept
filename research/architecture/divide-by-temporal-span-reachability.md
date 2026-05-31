---
status: Cited — grounded the W-C Part-B disposition (KEEP: the checks are reachable + D15-mandated); cited from the W-C completion note in `docs/Working/compiler-readiness-plan-2026-05-24.md`
authored: 2026-05-30
author: research (internal leg)
topic: Reachability + spec-mandate of the divide-path denominator-compatibility checks (PRE0073 / PRE0074) for period/duration denominators
external-engagement: purely-internal
---

> **Outcome (2026-05-30):** verdict was **(c) reachable + spec-mandated → KEEP** (not dead code). The
> W-C Part-B disposition acted on this: PRE0074 was re-aimed to fire on the denominator operand
> (commit `d2946dc0`); the divisor existence itself is a deliberate, settled D15 extension (see the
> companion `research/language/division-by-temporal-span-prior-art.md` Resolution). This file is the
> reachability/spec-mandate record for that decision.

# Divide-by-Temporal-Span: Reachability of the Denominator-Compatibility Checks (PRE0073 / PRE0074)

> Investigates whether the `OperatorKind.Divide`-only denominator-compatibility code in `TypeChecker.Expressions.cs` (PRE0073 `DurationDenominatorMismatch`, PRE0074 `CompoundPeriodDenominator`) can be reached by any real `.precept` program today, and whether the spec mandates the division-by-period/duration operations it guards.

## Background

`ValidateDenominatorCompatibility` in `src/Precept/Pipeline/TypeChecker.Expressions.cs` runs only for `OperatorKind.Divide`. Its two diagnostic branches concern a period/duration denominator (the right operand of a division) or a period/duration left operand:

- PRE0073 `DurationDenominatorMismatch`
- PRE0074 `CompoundPeriodDenominator`

The W-C Part-B triage needs to classify this as exactly one of:

- **(a) genuinely unreachable AND no spec mandate → dead code, delete**
- **(b) unreachable today BUT spec-mandated → build gap**
- **(c) actually reachable → not dormant**

An earlier inline attempt produced corrupted output that suggested enum members named `MoneyDividePeriodToPrice`, `MoneyDivideDurationToPrice`, `QuantityDivideDurationToRate`, `QuantityDividePeriodToRate`, `PeriodDivideDuration`, `DurationDividePeriod`, some with a `= 999` sentinel. This investigation re-derives the facts cleanly from the actual files.

> **Methodology note on a SECOND output corruption.** During this investigation the tool-output channel intermittently dropped output. An early pass of greps against `Operations.cs` returned *blank* (zero hits) for the four divide-by-span member names — which would have implied the members had no `GetMeta` metadata. A later, recovered pass of the **same** greps returned the real arms (see F2). The blank greps were the corruption, not the truth. Every load-bearing claim below rests on a grep/read whose output came back **non-empty and self-consistent**; the cross-checks (enum ↔ GetMeta ↔ test ↔ spec all agreeing) make the recovered evidence trustworthy. Where a single region could not be re-rendered verbatim, it is flagged in § Threats to Validity.

## Methodology

- **Research question:** Can any real `.precept` source reach the PRE0073/PRE0074 period/duration branches of `ValidateDenominatorCompatibility`, and does the canonical spec mandate the period/duration division operations those branches guard?
- **Search strategy:** Direct read + grep of `src/Precept/Language/OperationKind.cs` (legal-operation enumeration), `src/Precept/Language/Operations.cs` (`GetMeta`, the operand/result-type metadata the type checker consults to resolve an operator), `src/Precept/Pipeline/TypeChecker.Expressions.cs` (the guard method + its call site), `docs/language/business-domain-types.md § D15` (the canonical operator table), and `test/Precept.Tests/OperationsTests.cs` (the operation-metadata test corpus). Plus `git log`/`git log -S` on the enum + Operations files.
- **Inclusion / exclusion:** In scope — the four divide-by-span members (`MoneyDividePeriod`, `MoneyDivideDuration`, `QuantityDividePeriod`, `QuantityDivideDuration`), `DurationDivideDuration`, the guard method, and the D15 mandate. Out of scope — runtime evaluation semantics (question is type-checker reachability); the multiply path except as a wired-comparison reference.
- **Source-grade declaration:** All sources are Precept's own source/spec/test files (Primary for an internal-state question). No external sources — `external-engagement: purely-internal` is honest because the question is exclusively about Precept's own code/spec state.
- **Time bounds:** 2026-05-30, branch `spike/Precept-V2-Radical`, repo HEAD `a42134e3` (working tree).

## Findings

### F1 — The four divide-by-span members exist as ordinary enum members; the corrupted `=999`/`*ToPrice`/`*ToRate` names were artifacts

`OperationKind.cs` defines them as plain sequential members (verbatim, `grep -n "Divide"`):

```
106:    MoneyDividePeriod                        =  72,
107:    MoneyDivideDuration                      =  73,
122:    QuantityDividePeriod                     =  80,
123:    QuantityDivideDuration                   =  81,
 82:    DurationDivideDuration                   =  54,
```

**Corrections to the first corrupted output:**

- **No `= 999` sentinel exists anywhere.** `grep -n "999"` returns nothing.
- The long names (`MoneyDividePeriodToPrice`, `QuantityDivideDurationToRate`, etc.) **do not exist**; the real names are the short forms above.
- **`PeriodDivideDuration` and `DurationDividePeriod` do not exist.** The only `Period`-section members are `PeriodPlusPeriod = 55` / `PeriodMinusPeriod = 56` (`OperationKind.cs:85-86`); the only temporal-LHS divide besides the four span members is `DurationDivideDuration = 54`.

### F2 — `Operations.GetMeta` HAS a real arm for all four divide-by-span members (operand types include a period/duration denominator)

`Operations.cs` `GetMeta(OperationKind)` is the single source of truth for each operation's operand and result `TypeKind`s (`Operations.cs:5-9`: *"Source of truth for the type checker, doc generation, MCP vocabulary, and evaluator dispatch."*). The grep `grep -n "OperationKind\.<name>" Operations.cs` returns a live arm for each of the four, plus the homogeneous duration case (verbatim, line numbers from grep):

```
373:        OperationKind.DurationDivideDuration => new BinaryOperationMeta(
374:            kind, OperatorKind.Divide, PDuration, PDuration, TypeKind.Number,
375:            "Duration ÷ duration → number (ratio)",
378:                new NumericProofRequirement(new ParamSubject(PDuration), OperatorKind.NotEquals, 0m, ...

500:        OperationKind.MoneyDividePeriod => new BinaryOperationMeta(
501:            kind, OperatorKind.Divide, PMoney, PPeriod, TypeKind.Price,
505:                new NumericProofRequirement(new ParamSubject(PPeriod), OperatorKind.NotEquals, 0m, ...

509:        OperationKind.MoneyDivideDuration => new BinaryOperationMeta(
510:            kind, OperatorKind.Divide, PMoney, PDuration, TypeKind.Price,
514:                new NumericProofRequirement(new ParamSubject(PDuration), OperatorKind.NotEquals, 0m, ...

574:        OperationKind.QuantityDividePeriod => new BinaryOperationMeta(
575:            kind, OperatorKind.Divide, PQuantity, PPeriod, TypeKind.Quantity,
579:                new NumericProofRequirement(new ParamSubject(PPeriod), OperatorKind.NotEquals, 0m, ...

583:        OperationKind.QuantityDivideDuration => new BinaryOperationMeta(
584:            kind, OperatorKind.Divide, PQuantity, PDuration, TypeKind.Quantity,
588:                new NumericProofRequirement(new ParamSubject(PDuration), OperatorKind.NotEquals, 0m, ...
```

So the operand/result triples are exactly:

| OperationKind | Left | Right (denominator) | Result |
|---|---|---|---|
| `MoneyDividePeriod` | `PMoney` | **`PPeriod`** | `Price` |
| `MoneyDivideDuration` | `PMoney` | **`PDuration`** | `Price` |
| `QuantityDividePeriod` | `PQuantity` | **`PPeriod`** | `Quantity` |
| `QuantityDivideDuration` | `PQuantity` | **`PDuration`** | `Quantity` |
| `DurationDivideDuration` | `PDuration` | `PDuration` | `Number` |

**This is the decisive reversal of the first draft.** Four divide operations have a **period or duration RIGHT operand (denominator)**, with full `GetMeta` metadata and a divisor-non-zero proof requirement keyed on the period/duration subject. The switch's only terminal arm is `_ => throw new ArgumentOutOfRangeException(...)` at `Operations.cs:1230` — there is no metadata-synthesizing catch-all; each of the four resolves through its own explicit arm. The `GetMeta` switch is documented as the *"exhaustive switch"* (`Operations.cs:40-43`).

### F3 — The four operators type-check, so `ValidateDenominatorCompatibility` IS reachable with a period/duration denominator

Because each divide-by-span member has a `GetMeta` arm with a period/duration right operand (F2), a `.precept` expression such as `money / period`, `money / duration`, `quantity / period`, or `quantity / duration` **resolves to a concrete `OperationKind`** and type-checks (result `Price` or compound `Quantity`). Operator resolution succeeding is the precondition for the divide-specific validation.

The guard is called from the divide-handling path at `TypeChecker.Expressions.cs:1378`:

```
1378:            ValidateDenominatorCompatibility(op, span, ctx, leftQualifiers.Value, rightQualifiers.Value);
```

and the method is at `1386`. Its inline comment (`TypeChecker.Expressions.cs:1375-1376`) states the contract directly:

```
1375:        // PRE0073: Duration denominator mismatch — variable-length temporal unit as denominator
1376:        // PRE0074: Compound period denominator — compound period can't cancel single-unit denominator
```

The full guard body (`TypeChecker.Expressions.cs:1386-1454`) is now confirmed verbatim. The two relevant branches are:

```
1422:        // PRE0073: Duration denominator with a variable-length (calendar) temporal unit.
1427:        var rightTemporalUnit = rightQualifiers.FirstOrDefault(q => q.Axis == QualifierAxis.TemporalUnit)
1428:            as DeclaredQualifierMeta.TemporalUnit;
1429:        if (rightTemporalUnit is not null
1430:            && op.Left.ResultType is TypeKind.Duration or TypeKind.Period
1431:            && rightTemporalUnit.Components.Any(component =>
1432:                TemporalUnits.TryGet(component, out var entry) && entry.IsCalendarBased))
1433:        {
1434:            ctx.Diagnostics.Add(
1435:                Diagnostics.Create(DiagnosticCode.DurationDenominatorMismatch, span, rightTemporalUnit.UnitName));

1440:        // PRE0074: Compound period as denominator against a single-unit denominator.
1443:        var leftTemporalUnit = leftQualifiers.FirstOrDefault(q => q.Axis == QualifierAxis.TemporalUnit)
1444:            as DeclaredQualifierMeta.TemporalUnit;
1445:        if (leftTemporalUnit is not null
1446:            && rightTemporalUnit is not null
1447:            && rightTemporalUnit.Components.Length > 1)
1448:        {
1449:            var compoundDesc = string.Join(" + ", rightTemporalUnit.Components);
1450:            ctx.Diagnostics.Add(
1451:                Diagnostics.Create(DiagnosticCode.CompoundPeriodDenominator, span, compoundDesc, leftTemporalUnit.UnitName));
```

So PRE0073 fires when the **left** operand is `Duration or Period` AND the **right** (denominator) qualifier carries a calendar-based (variable-length) temporal unit. PRE0074 fires when the **right** (denominator) temporal unit is a compound period (`Components.Length > 1`) against a left temporal-unit qualifier. This is precisely the D15 fixed-length boundary (F5): a variable-length calendar unit (`days`+) as a denominator is the PRE0073 case, a compound period as denominator is the PRE0074 case. Both require a real period/duration denominator to fire — which F2 shows is a legal, resolvable operand shape.

**Conclusion of F3:** the PRE0073/PRE0074 branches are **reachable**. A division whose denominator carries a variable-length calendar unit, or a compound-period denominator, reaches the guard after the operator type-checks. This is **not dormant code**.

(Note: the call site at `TypeChecker.Expressions.cs:1378` is the real one — `if (opMeta.Kind == OperatorKind.Divide) ValidateDenominatorCompatibility(...)`. The "184" cited in the corrupted first draft was an unrelated method (`ResolveNumericLiteral`), an artifact of a stale grep result.)

### F4 — Proof engine wires PRE0074 for the compound-period cancellation path

The PRE0074 `CompoundPeriodDenominator` diagnostic is also produced from the **proof engine** (not only the type checker), in `ProofEngine.Diagnostics.cs` — confirming the divide-by-span cancellation path is wired through the proof stage, symmetric with the multiply cancellation path:

```
ProofEngine.Diagnostics.cs:64:   // composite reaches here unproved — emit the precise CompoundPeriodDenominator
ProofEngine.Diagnostics.cs:66:   if (TryCreateCompoundPeriodDenominatorDiagnostic(chainReq, obligation, semantics, out var compoundDiagnostic))
ProofEngine.Diagnostics.cs:210:  private static bool TryCreateCompoundPeriodDenominatorDiagnostic(
ProofEngine.Diagnostics.cs:250:  diagnostic = Diagnostics.Create(DiagnosticCode.CompoundPeriodDenominator, obligation.Site.Span, ...
```

and `ProofEngine.Qualifiers.cs:456` documents the composite-cancellation rule the proof engine enforces: *"cancel — the composite must stay unresolved (→ unproved → CompoundPeriodDenominator)."* This refutes the first draft's "divide path not wired in the proof engine" claim — that was an artifact of greping a nonexistent filename (`ProofEngine.Cancellation.cs` does not exist; the cancellation logic lives in `ProofEngine.Diagnostics.cs` / `ProofEngine.Qualifiers.cs`). The diagnostic codes themselves are defined at `DiagnosticCode.cs:155-156` (`DurationDenominatorMismatch = 73`, `CompoundPeriodDenominator = 74`) with full message templates and `RelatedCodes` wiring at `Diagnostics.cs:647-655`.

### F4b — Test corpus exercises the divide-by-span operations AND both failure branches

`test/Precept.Tests/OperationsTests.cs:245-257` is a `[Theory]` asserting the operand/result metadata for the span-denominator operations (verbatim):

```
246:    [InlineData(OperationKind.MoneyDivideQuantity, TypeKind.Quantity, TypeKind.Price)]
247:    [InlineData(OperationKind.MoneyDividePeriod, TypeKind.Period, TypeKind.Price)]
248:    [InlineData(OperationKind.MoneyDivideDuration, TypeKind.Duration, TypeKind.Price)]
249:    public void MoneyDivide_ProducesPriceFromVariousDenominators(...)
```

and dedicated type-checker tests exercise **both** guard branches:

```
TypeCheckerCurrencyUnitTests.cs:268:  public void PriceField_DurationDenominatorVariable_EmitsDurationDenominatorMismatch()   // PRE0073
TypeCheckerCurrencyUnitTests.cs:293:  public void PriceField_CompoundPeriodDenominator_EmitsCompoundPeriodDenominator()       // PRE0074
TypeCheckerPriceTemporalDenominatorTests.cs:179:  public void PricePerHours_TimesCompositePeriod_EmitsCompoundPeriodDenominator()
TypeCheckerPriceTemporalDenominatorTests.cs:199:  public void PricePerDays_TimesDatetimeSpanningCompositePeriod_EmitsCompoundPeriodDenominator()
```

(plus `Slice11B_TemporalPriceDenominatorTests.cs` and `DiagnosticsTests.cs:421-422`, which lists both codes in the diagnostic-coverage assertion). Both PRE0073 and PRE0074 have live, passing test coverage — independent corroboration that the branches are reachable and exercised.

### F5 — Spec mandate (D15): the four operations are explicitly listed as "Operators enabled" (mandated, shipped)

`docs/language/business-domain-types.md` is the canonical operator-table doc. Its **Status block** (`business-domain-types.md:5-10`) reads:

```
## Status
| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Implemented (with documented gaps — see Compiler Readiness Plan Phase 3 / F-LANG-BIZ findings) |
```

So the doc's status is **Implemented** (with documented gaps), maturity **Full** — not Design/Draft. It does not maintain a separate shipped-vs-aspirational column; the operator tables are presented as the implemented surface, with gaps tracked externally by finding IDs.

§ D15 (`business-domain-types.md:1821`) is titled *"D15. Time-unit denominators use NodaTime vocabulary and cancel against `period` or `duration`"* and its **Operators enabled** list (`:1836-1838`) mandates all four divide forms verbatim:

```
1836:- **Operators enabled:**
1837:  - Period path: `price × period → money`, `money ÷ period → price`, `quantity(compound) × period → quantity`, `quantity ÷ period → quantity(compound)` — cancels any time denominator.
1838:  - Duration path: `price × duration → money`, `money ÷ duration → price`, `quantity(compound) × duration → quantity`, `quantity ÷ duration → quantity(compound)` — cancels `hours`/`minutes`/`seconds` only.
```

The operator tables elsewhere in the doc restate them as live rows: `money / period → price` (`:509`, `:899`), `money / duration → price` (`:510`, `:900`), `quantity / period → quantity (compound)` (`:665`), `quantity / duration → quantity (compound)` (`:666`), each tagged `(D15)`. The fixed-length boundary that PRE0073/PRE0074 enforce is spelled out at `:1838` and `:1990` (*"Emit a compile error for `days`/`weeks`/`months`/`years` denominators with duration operands … explaining the fixed-length boundary"*).

**Conclusion of F5:** the spec **mandates** `money ÷ period`, `money ÷ duration`, `quantity ÷ period`, `quantity ÷ duration` as enabled, implemented operations, and explicitly mandates the fixed-length-boundary compile error that PRE0073/PRE0074 implement. The guard is spec-required behavior, not speculative scaffolding.

### F6 — Git provenance

`git log -S "MoneyDividePeriod"` on **both** `OperationKind.cs` and `Operations.cs` returns a single introducing commit:

```
427f845d feat: implement all 8 language-definition catalogs with tests
```

i.e. the four divide-by-span members and their `GetMeta` arms were introduced together, in the catalog-implementation commit, *with tests* — long-standing, not a freshly-added `=999` stub. This is consistent with F1 (no sentinel) and F4b (test coverage landed alongside the members). The recent `OperationKind.cs` history also includes `5d945711 feat(phase-5 W-D BIZ-01): money / price → quantity operation` and `d4c4048f feat(P3): price / compound-quantity -> price type algebra`, situating the divide-by-span members within the landed Phase-3/Phase-5 business-type-algebra feature line.

## Threats to Validity

- **Intermittent tool-output outage during the session (now resolved).** The output channel dropped output repeatedly mid-session. Critically, an **early grep of `Operations.cs` returned blank** for the four member names, which seeded an initial (wrong) "no GetMeta arm → unreachable" reading; a **recovered re-run of the same greps** returned the real arms (F2). After the channel recovered, **every** load-bearing region was re-read verbatim: the full `GetMeta` arms (F2), the complete `ValidateDenominatorCompatibility` body including both predicate branches (F3), the D15 block and Status (F5), the proof-engine wiring (F4), the dedicated test files (F4b), and the introducing commit (F6). No finding now rests on an un-rendered region. The corruption is fully contained because each claim is corroborated across independent surfaces (enum / GetMeta / guard / proof engine / test / spec) that all agree.
- **Corrupted-prior-output contamination (contained).** Three separate corruptions occurred across this and the prior session: the `=999`/`*ToPrice`/`*ToRate` names; an early blank Operations.cs grep; and a stale "184" call-site line. All three were falsified by recovered non-blank reads. The lesson worth recording: a *blank* grep result in this environment is not reliable evidence of absence — only a non-empty result is trustworthy, and an absence claim needs a second independent confirmation (here: the `_ => throw` exhaustive-switch terminal arm at `Operations.cs:1230` confirms there is no hidden catch-all, so the explicit arms at 500/509/574/583 are the only resolution path).
- **Runtime evaluation not examined.** This investigation is scoped to type-checker reachability (the question asked). Whether the runtime evaluator correctly *computes* `money ÷ period → price` was not checked; the type-level reachability and spec-mandate findings stand independent of evaluator status.

## Implications for Precept

The W-C Part-B triage asked for an (a)/(b)/(c) verdict on `ValidateDenominatorCompatibility`'s period/duration branches:

- The four divide-by-span operators **type-check** (F2: real `GetMeta` arms with period/duration denominators), are **tested** (F4), and are **spec-mandated** (F5: D15 "Operators enabled" + the implemented-status doc). The PRE0073/PRE0074 branches enforce the D15 fixed-length boundary, which the spec explicitly requires as a compile error.
- Therefore the code is **(c) actually reachable — not dormant.** It is live, spec-mandated, type-checker-integrated behavior. There is no deletion case (a) and no build-gap case (b) for the period/duration denominator branches: the operations exist end-to-end (enum → metadata → guard → test → spec).
- The multiply path (`PriceTimesPeriod` etc.) and the divide path are **symmetric** here — both wired — contrary to the first draft's mistaken "asymmetry" reading, which was an artifact of the blank-grep corruption.

There is **no remaining keep/delete/build question** for W-C on this code: the operators are wired end-to-end (enum → `GetMeta` → type-checker guard → proof-engine guard → tests → spec) and spec-mandated. Both failure branches already have dedicated passing tests (F4b: `PriceField_DurationDenominatorVariable_EmitsDurationDenominatorMismatch` for PRE0073; `PriceField_CompoundPeriodDenominator_EmitsCompoundPeriodDenominator` and the `TypeCheckerPriceTemporalDenominatorTests` cases for PRE0074). The W-C triage should mark `ValidateDenominatorCompatibility`'s period/duration branches **KEEP — live, tested, spec-mandated**.

## Classification

- **(c) actually reachable — not dormant.** Decisive evidence: `Operations.cs:500-501,509-510,574-575,583-584` define `GetMeta` arms whose RIGHT operand is `PPeriod`/`PDuration` (a period/duration denominator), so `money/quantity ÷ period/duration` type-check; the guard is called from the divide path at `TypeChecker.Expressions.cs:1378`; and `business-domain-types.md § D15:1836-1838` mandates all four as "Operators enabled" with the fixed-length boundary the guard enforces. `OperationsTests.cs:247-248` independently asserts the metadata.

**Single most decisive piece of evidence:** `Operations.cs:574-575` — `OperationKind.QuantityDividePeriod => new BinaryOperationMeta(kind, OperatorKind.Divide, PQuantity, PPeriod, TypeKind.Quantity, ...)` — a divide operation whose denominator operand is `PPeriod`, with a non-zero-period proof requirement, mandated by D15:1837 (`quantity ÷ period → quantity(compound)`). A period denominator is a legal, resolvable, spec-required operand shape, so the divide-specific period/duration validation is reachable.

## Open Questions

The reachability and spec-mandate questions are fully resolved. Residual items, all minor:

1. ~~**`period`-in-hours cancellation gap.**~~ **Resolved** — that gap was the W-C Slice 1 acceptance side (single-basis `period * price` cancellation) and shipped in commit `fd147dc2`, distinct from this file's divide-path keep/delete question (KEEP, shipped in `d2946dc0`). Both are now closed.
2. **Runtime evaluation parity.** Out of scope here (type-checker reachability only) — but a follow-up could confirm the evaluator computes `money ÷ period → price` consistently with the type result, since the spec marks the doc "Implemented (with documented gaps)."
3. **`QuantityDivide*` failure-path test depth.** `MoneyDivide*` and `Price`-denominator paths have dedicated PRE0073/PRE0074 tests; a quick check that the `QuantityDividePeriod`/`QuantityDivideDuration` denominator failure cases are equally covered would close the symmetry (likely already covered via the shared guard, since the guard keys on operand result type + denominator qualifier, not on the specific OperationKind).
