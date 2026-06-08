---
status: Active — horizon groundwork
authored: 2026-06-05
author: research (architecture/compiler)
topic: Can "bounds-only" compile-time reasoning (intervals, never value-folding) meet Precept's structural-impossibility promise, and what does it cost authors across the real sample corpus?
external-engagement: purely-internal
---

# Bounds-only constraint enforcement — can interval reasoning subsume the default value-fold?

> Evaluates the hypothesis that the compiler should reason **only over field bounds (intervals)** — never folding specific values — and still catch every broken-default precept. Grounded in the shipped proof engine, the `NumericInterval` domain's expressiveness limit, and the full `samples/` corpus. Adversarial on the load-bearing question: does bounds-only **subsume** the value-fold (BUG-027), or is the value-fold **strictly necessary** for some rule/default shapes?

## Background

The shipped proof engine recently gained a **value-fold against defaults** (BUG-027 fix, `DefaultViolatesRule`/PRE0164, 2026-06-05): each unguarded global `rule`'s condition is evaluated to a concrete `true`/`false` against the field-default environment, rejecting only on a provable `false`. This sits alongside the engine's **bounds/interval** machinery: relational narrowing (Slice 2c-i, `FieldToFieldConstraint` → narrowed interval), the literal default-vs-bound containment check (`OutOfRange`), and `BuildNarrowedIntervals`.

A standing question (tracked as BUG-027's fate + the field-ref-bound enforcement architecture for BUG-020/021) is whether the value-fold is **necessary at all**, or whether a purely bounds-based design — narrow each field's interval from declared + relational + computed bounds, check each default against its *narrowed interval*, and **reject any unbounded bound-source** — would catch the same broken-default precepts without folding values. If so, the value-fold could be retired as redundant. This research evaluates that hypothesis honestly, including its failure modes.

This doc evaluates **Precept's own mechanism + corpus + promise** (hence `external-engagement: purely-internal`). The external prior-art question — how interval-domain abstract interpreters vs value/SMT evaluators handle this in the broader field — is covered by the sibling doc [`interval-vs-value-evaluation-prior-art-2026-06-05.md`](interval-vs-value-evaluation-prior-art-2026-06-05.md) (produced in parallel); cross-reference it for the external grounding this doc deliberately does not duplicate.

**Intended consumer:** a `/design` decision on (a) BUG-027's fate — keep the value-fold, retire it for bounds-only, or keep both — and (b) the field-ref-bound enforcement architecture (BUG-020 Slice 2c-ii / BUG-021 Slice 2c-iii).

## Methodology

- **Research question.** Can bounds-only reasoning (narrow + default-vs-narrowed-interval + reject-unbounded-source, with **no** value-folding) meet Principle 1 (structural impossibility) and Principle 11 ("no diagnostics ⟹ no runtime faults") for the broken-default class, and what does reject-unbounded-source cost authors across the real corpus? Head-to-head: does bounds-only subsume the value-fold, or is the value-fold strictly necessary for some shape?
- **Search strategy.** (1) Canonical spec/philosophy reads (`docs/philosophy.md`; `precept-language-spec.md` §0.1 Principles 1/7/10/11, §0.4, §0.6 items 1/2/7/11, §0.7); the boundary assessment (`docs/Working/compile-time-vs-runtime-contract-clarity-2026-06-01.md`); the shipped-mechanism design (`relational-narrowing-core-design-2026-06-03.md`) and the bug ledger (BUG-020/027/028, BUG-017/018/019 family). (2) Source reads of the mechanism the hypothesis would lean on: `NumericInterval.cs` (the abstraction's expressiveness limit), `ProofEngine.Analysis.cs` (`FoldValue`/`EvaluateBinaryOp` — the value-fold; `BuildDefaultEnvironment`; the literal default-bound collector), `ProofEngine.Satisfiability.cs` (`ScanRulesAgainstDefaults`), `ProofEngine.Composition.cs` (relational extraction). (3) Full `samples/*.precept` survey (77 files) tabulating relational rules, computed-field bounds, field-ref bounds, and unbounded bound-sources. (4) **Behavioral confirmation** of *current* primitive behavior via throwaway `Compiler.Compile` tests (idiom from `RuleDefaultSatisfiabilityTests`), removed after capture — used only to ground "what the engine does today", not the analytical hypothesis itself.
- **Inclusion / exclusion.** In: the **broken-default** class (defaults violate a rule, no construction event overwrites them, unfixable at runtime). Excluded: runtime-input governance (event args / edits — out of scope of the compile-time default question), the graph-analysis modifiers, and the external prior-art survey (sibling doc).
- **Source-grade.** Primary throughout — Precept's own canonical specs (locked), source code at `Compiler.Compile` level, and behavioral probes against a fresh build. No external/training-data sources are load-bearing.
- **Time bounds.** Investigation ran 2026-06-05 against the `spike/Precept-V2-Radical` working tree. Source line references are point-in-time; the spec excerpts are stable.

## Findings

### F0 — The abstraction's expressiveness limit (the crux for Q4)

`NumericInterval` (`src/Precept/Language/NumericInterval.cs:6-30`) is a **single closed decimal interval** `[Min, Max]` plus an `IsUnbounded` flag and an `IsEmpty` (`Max < Min`) test. Its only relational combinator is `Intersect` (`:143-150`). It can represent exactly one shape: `X ∈ [a, b]`.

> "A closed numeric interval [Min, Max] used for compile-time overflow proofs."
> — `NumericInterval.cs:3-5`

What the interval domain **cannot** represent (verbatim-grounded by the absence of any such combinator in the file, and by the relational design's own scoping):

- **Equality / disequality** between fields (`A == B`, `A != B`). An interval has no equality element; the relational fact records `>=,>,<=,<` only (`relational-narrowing-core-design-2026-06-03.md` S2: `op ∈ {>=,>,<=,<}`).
- **Disjunction** (`A < 0 OR A > 100`) — a single `[Min,Max]` is a convex set; it cannot hold a hole.
- **Multi-field linear relations beyond difference-bounds** (`A + B == C`, `A + B <= C`). The relational fact is the difference-bound-matrix element form `x − y ≤ c` (design § External precedent: "the relation `x − y ≤ c` is the stored fact") — a two-variable bound, not a three-variable sum.
- **Non-numeric domains** — choice/string equality, boolean rules, currency identity. There is no interval over `string`/`bool`/`currency` at all.

This is not an implementation gap; it is the **definition** of the abstraction. Bounds-only reasoning is, by construction, blind to every relation outside the convex-numeric-interval / difference-bound shape. (The sibling prior-art doc documents that this is the *expected* precision/soundness tradeoff of interval abstract interpretation in the broader field.)

### F1 — What the value-fold (BUG-027) actually does, and over what shapes

`FoldValue` (`ProofEngine.Analysis.cs:249-345`) recursively evaluates a rule condition to a concrete value against the default environment; `EvaluateBinaryOp` (`:351-405`) implements the operators. Crucially it handles, **on concrete folded values**:

- numeric `+ - * / %`, all six comparisons including `Equals`/`NotEquals` (`:367-378`);
- **string equality / inequality** (`:382-391`);
- **boolean equality** (`:393-402`);
- boolean `And`/`Or` (`:354-357`).

So the value-fold decides truth for shapes the interval domain cannot represent — equality, string/boolean rules, multi-field arithmetic — *because it works on values, not bounds*. It rejects **only** on a provable `false` (`ScanRulesAgainstDefaults`, `Satisfiability.cs:79`: `if (ConstantFold(...) is false)`); an unknown/unfoldable default folds to `null` and never rejects (soundness over completeness, §0.6 #1).

### F2 — Behavioral confirmation (current engine, throwaway `Compiler.Compile` probes, since removed)

Each row is a precept compiled today; "rejects" = an `Error` diagnostic. The bounds-only column is the **analytical** disposition (what narrow + default-vs-narrowed-interval + reject-unbounded-source *would* do); the value-fold column is **observed**.

| Probe | Precept (defaults) | Broken default? | Value-fold today (observed) | Bounds-only (analytical) |
|---|---|---|---|---|
| A1 | `X integer min 5 default 3` | yes | `OutOfRange` (literal default-vs-bound) | **rejects** (3 ∉ [5,∞)) — same primitive |
| Q1a | `Amount default 5; Floor default 10; rule Amount>=Floor` (Floor unbounded) | yes | `DefaultViolatesRule` | **rejects** via reject-unbounded-source (Floor is an unbounded bound-source) *or* default-vs-narrowed — see Q1 |
| M1 | `A default 5; B default 7; rule A==B` | yes | `DefaultViolatesRule` | **MISS** — interval can't represent `==` |
| M2 | `Active default false; Closed default false; rule Active==Closed` | no (false==false) | clean | clean (vacuously, no fact) |
| M2b | `Flag default true; rule Flag==false` | yes | `DefaultViolatesRule` | **MISS** — boolean rule |
| M3 | `Status default "open"; rule Status=="closed"` | yes | `DefaultViolatesRule` | **MISS** — string rule |
| M4 | `A default 1; B default 1; C default 10; rule A+B>=C` | yes | `DefaultViolatesRule` | **MISS** — 3-field sum is not a difference-bound |
| M5 | `A default 5; B default 5; rule A!=B` | yes | `DefaultViolatesRule` | **MISS** — `!=`; the violation is a point the interval can't pin |
| M6 | `N default 7; rule N%2==0` | yes | `DefaultViolatesRule` | **MISS** — modulo/parity |
| SUM | `Avail default 0; Alloc default 0; Total default 5; rule Avail+Alloc==Total` | yes | `DefaultViolatesRule` | **MISS** — 3-field equality |
| CUR | `A default 'USD'; B default 'USD'; rule A!=B` | yes | **clean (value-fold MISS too)** | **MISS** — currency `!=` folds to neither string/decimal/bool |
| COMP | `Base default 20; Computed <- Base+1; rule Computed<=10` | yes | **clean (computed default not folded)** | **MISS today**; bounds-only with `IntervalOf(Computed)`=[21,21] *would* reject |

**Headline:** the value-fold rejects **7 of the 8 genuinely-broken non-interval shapes** (M1, M2b, M3, M4, M5, M6, SUM); bounds-only catches **none** of them. The value-fold is not redundant — it is the *only* current mechanism for the equality/string/boolean/multi-field-arithmetic broken-default class.

### F3 — Worked examples (Q1), step by step

Let `⟦F⟧` = `ExtractFieldInterval(F)` (declared bounds + flag-fold). Bounds-only = narrow `⟦X⟧` from declared + relational + computed contributions, then (i) default-vs-narrowed-interval containment, and (ii) reject-unbounded-source.

**(a) BUG-027 Variant-B literal default, Floor unbounded** — `Amount default 5; Floor default 10; rule Amount >= Floor`, `Floor` has no `max`/`min`.
- Relational narrowing: `rule Amount >= Floor` ⇒ `narrowed[Amount] := ⟦Amount⟧ ⊓ [⟦Floor⟧.lo, +∞)`. `Floor` is fully unbounded, so `⟦Floor⟧.lo = −∞` (sentinel) ⇒ **identity degradation** (design S3): `narrowed[Amount]` gains nothing. Default-vs-narrowed cannot reject (5 ∈ (−∞,∞)).
- **Reject-unbounded-source** is the catch: `Floor` is used as a bound-source (RHS of a relational rule) and is unbounded ⇒ "supply a bound on Floor." This rejects — but it rejects the **source**, asking the author to bound `Floor`, *not* because it proved `5 < 10`. **Compare:** the value-fold rejects with the *true* reason (`5 >= 10` is false) and asks the author to fix the **default**, which is the actual defect.
- If the author then bounds `Floor min 10 max 10` (to satisfy reject-unbounded-source), bounds-only *now* narrows `narrowed[Amount] := ⊓ [10, +∞)`, and default `5 ∉ [10,∞)` ⇒ rejects via default-vs-narrowed. So bounds-only *can* reach the right rejection — **but only after forcing a bound the value-fold never needed.**

**(b) Field-ref bound** — `Floor default 10; field Amount min Floor default 5`.
- `min Floor` desugars to `rule Amount >= Floor` (§2.4; BUG-020). Same as (a): `Floor` here is **bounded by its own default-as-pin?** No — `default 10` is *not* a bound (see F4); `⟦Floor⟧ = (−∞,∞)`. Reject-unbounded-source fires on `Floor`. Observed today: **clean** (BUG-020 — the bound is currently inert; neither approach is wired yet). The value-fold would catch the broken default `5 >= 10` directly once the desugar feeds `ScanRulesAgainstDefaults`.

**(c) Computed-field bound** — `field Amount min Computed; field Computed <- Base + 1`, across `Base`:
- *Base pinned* `min 10 max 10`: `IntervalOf(Computed) = IntervalOf(Base+1) = [11,11]`. Bounds-only: `narrowed[Amount] := ⊓ [11,+∞)`, default `5 ∉ [11,∞)` ⇒ **rejects** via default-vs-narrowed. ✓ Bounds-only's *natural* win — the computed interval is exact, no value-fold needed. (The value-fold does *not* reach this: computed-field defaults are excluded from the fold environment — COMP probe, clean today.)
- *Base bounded* `min 0 max 100`: `IntervalOf(Computed) = [1,101]`. `narrowed[Amount] := ⊓ [1,+∞)`. Default `5 ∈ [1,∞)` ⇒ no reject. Sound — `5` *could* be ≥ Computed at runtime; correctly not a *provable* violation.
- *Base unbounded*: `IntervalOf(Computed) = (−∞,∞)` ⇒ identity degradation ⇒ reject-unbounded-source fires on `Base` (transitively, via Computed). Onerous: the author must bound `Base` even though `Amount`'s default may be fine.

**(d) Multi-hop chain** — `X >= Y`, `Y`'s bound from computed `Z`, `Z <- W`, …. Bounds-only is **depth-1 by locked design** (§0.4 no fixpoint; `relational-narrowing-core` reads `⟦Y⟧₀` = `Y`'s *non-relational* interval, never chasing a third field). So `narrowed[X]` sees `Y`'s declared/computed interval but **not** `Y`'s own relational narrowing. If `Y`'s only bound is itself a relational rule (`Y >= Z`), `⟦Y⟧₀` is unbounded and `X`'s narrowing degrades to identity. The cascade (F5) is the cost: to make `X`'s default provable, the author must bound `Y` directly (not rely on `Y >= Z`), and if `Y`'s bound is `Z` computed from `W`, bound `W`. The value-fold sidesteps this entirely for *defaults* — it folds the whole expression tree of concrete default values in one pass (`FoldValue` is recursive, `:261-345`), no depth limit, because it works on values not a relational lattice.

### F4 — A literal `default` is NOT a bound today (decisive for Q3 mitigations)

`BuildDefaultEnvironment` (`Analysis.cs:110-147`) records each field's default *value*; it does **not** synthesize an interval bound from it. `ExtractFieldInterval` reads declared `min`/`max` modifiers, never the default. Confirmed behaviorally: the OR probe — `Floor default 0; Amount min Floor default 10` — compiles **clean** (no bound from `default 0`). So the proposed mitigation "a literal `default` implies a pin `[v,v]`" is **not** how the engine works, and adopting it would be **re-introducing value-reasoning into the bounds lane**: the pin's lower/upper bound *is* the default value, so "narrow from the default-pin then check the default against the narrowed interval" is value-folding wearing an interval costume. This is the central trap in the bounds-only-with-mitigations framing (F6).

### F5 — Empirical corpus survey (Q2)

77 sample files; 149 `rule` declarations; 35 computed fields (`<-`); **0 field-ref bounds** (`min OtherField`) — the surface was inert (BUG-020), so no sample uses it.

**Field-to-field relational rules** (the rules bounds-only narrowing *could* engage) number ~48 by inspection. Their shapes:

| Shape | Bounds-only can represent? | Value-fold decides on defaults? | Corpus examples |
|---|---|---|---|
| `A <= B`, `A >= B`, `A < B`, `A > B` (numeric) | **Yes** (difference-bound) | Yes | `ApprovedAmount <= ClaimAmount` (insurance-claim:47); `TicketsIssued <= SeatsReserved` (event-registration:30); `LicensesInUse <= LicensesAllocated` (saas-license:60) |
| Date/instant `>`/`<`/`>=`/`<=` (temporal, often `is set`-guarded) | **No** — not a numeric interval | Partially (folds only if both defaults statically known; most are `optional` no-default) | **70** rules involve dates/deadlines; e.g. `CheckOutDate > CheckInDate` (hotel:62), `ExpirationDate > EffectiveDate` (policy-endorsement:61) |
| Period-difference `Deadline - Filed <= '72 hours'` (guarded) | **No** — temporal span vs period literal | No (guarded) | prior-auth-appeal:98-100; medical-prior-auth:94-97 |
| Multi-field sum `A + B <= C` / `== C` | **No** — 3-variable | **Yes** | `RefundAmount + ForfeitAmount <= AmountPaid` (venue:97); `LicensesAvailable + LicensesAllocated == TotalLicensesPurchased` (saas-license:62) |
| Scaled `A <= B * k` | **No** — not a difference-bound | Yes (if defaults known) | `ExistingDebt <= AnnualIncome * 3.0` (loan:38); `ProposedPremium <= CurrentPremium * 3` (renewal:55) |
| Equality / disequality `A != B`, `A == "..."` | **No** | Yes (numeric/string/bool); **No** (currency) | `BaseCurrency != QuoteCurrency` (currency-exchange:45); `RiskLevel == "High" when ...` (renewal:60) |
| Collection-count relations `A.count <= B.count` | **No** (count interval is separate) | No | `CriticalFlags.count <= Tests.count` (lab-test:31); `TotalCreditHours >= CourseCount` (academic:61) |

**Unbounded bound-sources (the reject-unbounded-source cost).** The corpus has **214** `money` fields with no `max` vs **10** with a `max`. Money fields are the dominant RHS of relational rules (`<= ClaimAmount`, `<= TotalAmount`, `<= LossAmount`, …). Under reject-unbounded-source, **every** money/numeric field that appears as a relational-rule RHS and lacks a `max` would need a **new `max`** — and a `max` on an open-ended monetary amount is frequently *not natural* (there is no domain ceiling on a claim amount or a loan request). Conservatively, **the majority of the ~48 field-to-field relational rules have at least one unbounded source**, so reject-unbounded-source would force new bounds across a large fraction of the 77 samples. This is the **over-rejection cascade** quantified.

Qualitatively: a *Floor*/*threshold*/*deadline* sometimes has a natural range ("a tolerance band", "a deposit ≤ total"), but a *claim amount*, *loss amount*, *requested loan*, *coverage amount* does **not** — these are genuinely open-ended monetary inputs, and demanding a `max` on them to satisfy a relational rule is onerous and arguably *wrong* (it invents a ceiling the domain doesn't have).

### F6 — Sound-mitigations analysis (Q3)

- **"A literal `default` implies a bound (pin)."** As F4 shows, this *is* value-reasoning: the pin `[v,v]` carries the value, and checking the default against a narrowed-from-pins interval is `5 >= 10` re-expressed. It would catch the same numeric cases the value-fold does — but only the numeric ones (intervals still can't pin a string/boolean/currency/`==`), and it abandons the "no value-folding" purity that motivates the hypothesis. Net: not a sound bounds-*only* mitigation; it's the value-fold under another name, strictly weaker (numeric-only).
- **"Interval inference from defaults."** Same objection — inferring `[v,v]` from a literal default is folding the value into a bound. Sound as arithmetic, but it dissolves the bounds-only/value-fold distinction rather than vindicating bounds-only.
- **Computed-interval default check (the genuine bounds-only win).** `IntervalOf(Computed)` for a *pinned* operand is exact (`[11,11]`), and checking a default against it is honest interval reasoning, no value-fold. This is the one place bounds-only **strictly beats** today's value-fold (which excludes computed defaults — COMP probe). It is additive and sound and should ship **regardless** of BUG-027's fate.

## Threats to Validity

- **The hypothesis is unimplemented; bounds-only dispositions are analytical.** The "bounds-only catches none of M1–M6" column is reasoned from the `NumericInterval` API surface and the relational design's locked scope, not from a built bounds-only mode. The reasoning is robust (an interval has no `==`/string/bool element — this is structural, not a coverage gap), but a future bounds-only implementation that *added* non-interval relational facts would no longer be "bounds-only" and would change the verdict. That hypothetical is exactly the value-fold, which is the point.
- **"Field-to-field relational rule" count (~48) is by grep + inspection, not a parser-AST census.** The `is set`-guarded temporal rules were counted as relational; a stricter "unconditional numeric field-to-field" count would be smaller. The directional finding (a large fraction have unbounded sources) is insensitive to the exact count given 214 unbounded money fields.
- **Probes ran against the working tree, not committed HEAD.** The BUG-027 fix is committed (it appears in § Fixed); the relational narrowing is shipped. The probe results reflect a fresh `Compiler.Compile`, not the MCP (treated as stale per project memory). The throwaway test files were removed after capture.
- **Corpus is the sample set, not author-authored production precepts.** Real-world precepts may bound more fields (or fewer). The 214:10 unbounded-money ratio is strong enough that the cascade conclusion would survive a sizable shift.
- **Currency-equality gap is a shared miss, not a bounds-only-only miss.** The value-fold *also* misses `currency != currency` on defaults (CUR probe). It is reported for fairness, not as evidence for bounds-only; it is an independent value-fold completeness gap (folds only string/decimal/bool).

## Implications for Precept

1. **The value-fold is not subsumable by bounds-only.** Principle 11 ("no diagnostics ⟹ no runtime faults") for the broken-default class requires deciding equality, string, boolean, and multi-field-arithmetic rule violations on defaults. The interval abstraction is structurally blind to all of these (F0). A definition whose **own defaults** violate such a rule is "unfixable at runtime → must be a compile error" (the owner's first principle, §0.6 #11) — and bounds-only would let M1/M2b/M3/M4/M5/M6/SUM **compile clean**, each a Principle-11 hole. Retiring the value-fold for bounds-only would *regress* the guarantee.
2. **Reject-unbounded-source is sound but over-rejects heavily on this corpus.** It never admits a broken default (safe direction), but it forces new `max` bounds on ~the majority of relational-rule sources, including open-ended money fields where a ceiling is unnatural (F5). It also rejects with the *wrong reason* (bound the source) rather than the true defect (fix the default) (F3a).
3. **Bounds-only and the value-fold are complementary, not competing.** Bounds-only owns the *general* obligation (a runtime value `Amount` must satisfy `>= Floor` for *all* admissible `Floor` — only an interval can express that). The value-fold owns the *default-configuration* check (do *these specific* default values satisfy the rule — a concrete-value question intervals can't pose). The shipped engine already runs both; the evidence says keep both.
4. **Field-ref / computed bound enforcement (BUG-020/021) should use the interval path for the runtime obligation AND feed the desugared rule to the value-fold for the default check.** The computed-interval default check (F6) is a free, sound win the value-fold currently lacks.

## Conclusions

### C1 — The value-fold is strictly necessary; bounds-only cannot subsume it.

- **Rationale.** The broken-default class includes equality, string, boolean, and multi-field-arithmetic rules. The `NumericInterval` domain represents exactly `X ∈ [a,b]` and the relational fact represents exactly `x − y ≤ c`; neither can represent `A == B`, `A != B`, `A == "closed"`, a boolean rule, or `A + B == C` (F0). The value-fold decides all of these on defaults (F1) and is observed to reject 7/8 such broken precepts that bounds-only would pass clean (F2). A prevention engine cannot let those compile clean (§0.6 #11; Principle 1/11).
- **Alternatives considered and rejected.** (a) *Bounds-only + reject-unbounded-source as a total replacement* — rejected: it catches **none** of the non-interval broken-default shapes (they have no unbounded source to reject; M5 `A!=B` with both `default 5` has fully-bounded operands and still must reject). (b) *Bounds-only + "default implies a pin"* — rejected: that **is** value-reasoning (F4/F6), strictly weaker than the value-fold (numeric-only), and abandons the purity that motivates the hypothesis. (c) *Keep value-fold only, drop intervals* — rejected: intervals are required for the *general* runtime obligation (`Amount >= Floor` for all `Floor`), which a default-only fold cannot discharge.
- **Precedent.** The shipped engine's own `EvaluateBinaryOp` (`Analysis.cs:351-405`) implements value-level `==`/string/bool/multi-field operators that have no interval analogue; the relational design's locked `op ∈ {>=,>,<=,<}` scope (`relational-narrowing-core-design-2026-06-03.md` S2) is the explicit boundary of what bounds can carry. Behavioral probes (F2) confirm the divergence on a fresh build.
- **Tradeoff accepted.** Two mechanisms instead of one (the value-fold and the interval engine coexist), with the conceptual cost of explaining which check owns which question. Accepted because collapsing to either one alone loses a guarantee (default-coverage or general-obligation coverage).

### C2 — Reject-unbounded-source is sound but imposes an onerous cascade on this corpus; it should bound the *general obligation*, not stand in for the *default check*.

- **Rationale.** Reject-unbounded-source is the correct discharge for the *runtime* obligation (an unbounded `Floor` makes `Amount >= Floor` unprovable for all inputs — identity degradation, §0.6 Principle 10 "emit, never skip"). But as a *replacement* for the default check it (i) misses every non-interval shape and (ii) forces `max` bounds on 214 unbounded money fields where a ceiling is frequently unnatural (F5), rejecting with the wrong reason (F3a).
- **Alternatives considered and rejected.** *Reject-unbounded-source as the sole broken-default mechanism* — rejected per C1 and F5. *Silently skip unbounded sources* — rejected: that is the BUG-017/018/019 soundness hole the family-fix closed ("unprovable ⇒ emit, never skip").
- **Precedent.** The relational design's identity-degradation rule (`relational-narrowing-core-design-2026-06-03.md` S3) and the BUG-017 family fix (emit on unprovable) establish reject-unbounded-source as the obligation discharge; the corpus survey (F5: 214:10 unbounded:bounded money) establishes the cascade cost.
- **Tradeoff accepted.** The general obligation will still require authors to bound a source field when a fault-prone op depends on it — friction in the genuinely-unprovable case — but only there, not as a blanket tax on every relational rule's default.

### C3 — Ship the computed-interval default check regardless of BUG-027's fate.

- **Rationale.** `IntervalOf(Computed)` for a pinned/bounded operand is exact interval reasoning (no value-fold), and checking a field's default against a computed-bound's narrowed interval catches the COMP case the value-fold currently misses (F2/F6). It is additive, sound, and free.
- **Alternatives considered and rejected.** *Extend the value-fold to compute computed-field defaults* — viable but reintroduces value-reasoning over computed expressions (currently excluded as `unfoldable`); the interval path is the cleaner home for computed-default-vs-bound.
- **Precedent.** `CollectComputedFieldBoundObligations` already proves computed *results* against bounds (BUG-017 fix); the default-vs-computed-bound check is the same machinery applied at the default site.
- **Tradeoff accepted.** Slightly more obligation wiring at the default site; negligible.

## What would change this conclusion

- **C1 falsified if** a bounds-only design were exhibited that catches M1, M3, M4, and M5 (equality, string, 3-field sum, disequality-point) **without** evaluating concrete values — i.e., if the interval/relational abstraction were extended to represent equality, string, and multi-field-sum facts. (That extension *is* value/relational evaluation; finding it "bounds-only" would be a definitional reclassification, not a refutation.)
- **C2 falsified if** a corpus census showed the *unconditional numeric field-to-field* rules overwhelmingly have **naturally-bounded** sources (so reject-unbounded-source forces few new bounds) — e.g. if <10% of relational-rule sources are unbounded. Current evidence (214:10 unbounded:bounded money) points the opposite way.
- **C3 falsified if** the computed-interval default check is shown to duplicate an already-shipped obligation (i.e., computed defaults are *already* checked against their field's bounds), making it redundant rather than additive.

## Open Questions

- **Should the desugared field-ref bound (`min Floor`) feed BOTH the interval obligation and `ScanRulesAgainstDefaults`?** F3b suggests yes (the default check wants the value-fold; the runtime obligation wants the interval). The BUG-020 Slice 2c-ii design must decide the wiring explicitly.
- **Currency / choice equality default-folding** (CUR probe miss) is a genuine value-fold completeness gap (folds only string/decimal/bool). Is closing it in scope, or is it acceptable under-emit (§0.6 #1) like BUG-028? Out of scope here; flagged for the design pass.
- **Temporal relational rules** (70 date/instant comparisons, period-differences) are neither numeric-interval nor value-foldable today (most operands are `optional` no-default). Whether the default-coverage guarantee even *applies* to optional-no-default temporal fields is a separate spec question (vacuous-when-unset, cf. the customer-profile guarding pattern in the BUG-027 fix).

## Sources

All sources are **Primary** (Precept's own canonical specs, locked design docs, source at `Compiler.Compile` level, and behavioral probes against a fresh build, 2026-06-05). No external or training-data sources are load-bearing.

- `docs/philosophy.md` — prevention/determinism/totality commitments; "not a second line of defense" (line 55).
- `docs/language/precept-language-spec.md` — §0.1 Principles 1/7/10/11 (lines 92, 104, 110, 112); §0.4 Execution Model Properties (no loops / no fixpoint, line 174); §0.6 items 1/2/7/11 (lines 203, 204, 209, 213) and Proof philosophy 1 (line 221); §0.7 Compile-Time/Runtime Contract (lines 266, 268).
- `docs/Working/compile-time-vs-runtime-contract-clarity-2026-06-01.md` — the acknowledged-unresolved compile/runtime boundary assessment (the boundary this doc's default-coverage question sits inside).
- `docs/Working/Archive/relational-narrowing-core-design-2026-06-03.md` — the shipped relational narrowing: S2 extraction (`op ∈ {>=,>,<=,<}`), S3 identity degradation / depth-1, External precedent (DBM `x − y ≤ c`).
- `docs/Working/bugs.md` — BUG-027 (value-fold, `DefaultViolatesRule`/PRE0164, Fixed 2026-06-05), BUG-020 (field-ref bound inert), BUG-021, BUG-028 (default-fold completeness gaps), BUG-017/018/019 (the "unprovable ⇒ emit, never skip" family).
- `src/Precept/Language/NumericInterval.cs:6-30,143-150` — the single-interval abstraction and its only relational combinator (`Intersect`); the expressiveness limit (F0).
- `src/Precept/Pipeline/ProofEngine.Analysis.cs:110-147,239-405` — `BuildDefaultEnvironment` (default value, not bound — F4), `FoldValue`/`ConstantFold`/`EvaluateBinaryOp` (the value-fold operators — F1).
- `src/Precept/Pipeline/ProofEngine.Satisfiability.cs:39,56-90` — `ScanRulesAgainstDefaults` (rejects only on provable `false`).
- `src/Precept/Pipeline/ProofEngine.Composition.cs:262-323` — `TryGetNumericConstraintFact` / `TryGetStaticNumericValue` (field-op-constant extraction; both-field operands yield no constant fact today).
- `samples/*.precept` (77 files) — the relational-rule / computed-field / unbounded-source survey (F5).
- Behavioral probes (throwaway `Compiler.Compile` tests, removed after capture, 2026-06-05) — the F2 table.
- Sibling (external prior art, cross-reference): [`interval-vs-value-evaluation-prior-art-2026-06-05.md`](interval-vs-value-evaluation-prior-art-2026-06-05.md).
