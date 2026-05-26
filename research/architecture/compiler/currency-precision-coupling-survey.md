---
status: Cited
authored: 2026-05-25
author: Frank (Lead/Architect)
topic: currency precision; ISO 4217 minor-unit; money / currency type coupling
external-engagement: strong
cited-from:
  - docs/language/business-domain-types.md:1740
  - docs/Working/compiler-readiness-plan-2026-05-24.md:126
---

# Currency Precision Coupling — External Survey

> Across the prevailing money libraries, financial APIs, accounting standards, and database conventions, currency identity (ISO 4217) and value precision are almost universally **operationally coupled** but **rarely structurally enforced at construction**; the strongest precedents for type-level enforcement are Joda-Money's `Money` (strict scale rejection) and NodaMoney (auto-rounding by default), while JSR-354's `Money`, Fowler's PoEAA pattern as canonically described, SQL conventions, and ORM mappings deliberately decouple them.

**Status**: Active research artifact (locked 2026-05-25). Grounds the Position 3 decision for F-LANG-BIZ-02 recorded in `docs/Working/compiler-readiness-plan-2026-05-24.md § Resolved for Phase 3`. Companion: internal `exact-decimal-arithmetic-survey.md` (covers System.Decimal / BigDecimal / IEEE 754 decimal / SQL DECIMAL — arithmetic mechanics).

## Background

Phase 3 of the Precept V2 language work is evaluating decision **F-LANG-BIZ-02**: should the type `money in 'USD'` carry an implicit `maxplaces 2` constraint derived from the ISO 4217 minor-unit table, such that `money in 'USD' = 1.001` is rejected (or auto-rounded) at compile/runtime?

This decision sits at the intersection of two existing Precept commitments from `CLAUDE.md` / `docs/philosophy.md`:

1. **Prevention, not detection.** Invalid configurations are structurally impossible.
2. **Honesty about approximation.** Exact and approximate behavior must be visible in the type system.

The existing internal survey at `research/architecture/compiler/exact-decimal-arithmetic-survey.md` covers decimal-arithmetic mechanics (System.Decimal, BigDecimal, IEEE 754 decimal, SQL DECIMAL). What it does **not** cover is whether external systems treat currency identity as a structural input to the value's precision — i.e., whether type identity carries the precision constraint, or whether precision is a separately-configured concern.

## Methodology

For each system surveyed, the binary question asked was: **does construction or arithmetic of a money value bind the precision constraint to the currency identity at the type-system level, such that `Money(USD, 1.001)` is rejected or auto-rounded without an additional explicit precision argument?**

A "yes" requires that the currency identity alone determine the precision rule. A library that accepts `Money(USD, 1.001)` but provides `formatForDisplay(currency)` is a "no" — that is per-call display rounding, not a structural type-level coupling.

In-scope surveyed sources:
- Joda-Money (`Money`, `BigMoney`)
- JSR-354 / `javax.money` (`Money`, `RoundedMoney`, `FastMoney`)
- Martin Fowler's Money pattern (PoEAA)
- Microsoft / .NET community money proposals
- Stripe, PayPal, Square, Adyen payment APIs
- COBOL `PIC` clauses + `REDEFINES`
- IFRS IAS 21 / US GAAP ASC 830
- Banking core systems (Temenos T24)
- SQL DECIMAL conventions
- Hibernate / JPA / EF ORM money mappings
- NodaMoney, NMoneys (.NET community libraries)

Out of scope (covered by internal survey): arithmetic-type mechanics for System.Decimal, BigDecimal, IEEE 754 decimal, Python `decimal`.

## Findings

### Summary table

| System | Structural coupling? | Mechanism | Default behavior on excess precision |
|---|---|---|---|
| **Joda-Money `Money`** | **YES** | Scale fixed to `CurrencyUnit.getDefaultFractionDigits()` | Throws `ArithmeticException` |
| **Joda-Money `BigMoney`** | No | Variable precision | Accepts any scale |
| **JSR-354 `Money` (Moneta default)** | No | Default `MathContext.DECIMAL64`; precision independent of currency | Accepts up to 256 digits |
| **JSR-354 `RoundedMoney`** | Partial — opt-in | Requires explicit `MonetaryRounding` operator; can be `Monetary.getDefaultRounding()` (currency-derived) or any custom rounding | Auto-rounds *if* a currency-derived rounding is configured |
| **JSR-354 `FastMoney`** | No | Fixed scale of 5 regardless of currency | Truncates to 5 |
| **Fowler PoEAA Money** | Implementation-defined | Pattern stores as integer "smallest units"; precision is a per-currency constant in implementations | Implementations typically round to smallest unit |
| **NodaMoney (.NET)** | **YES (default)** | Auto-rounds to currency minor unit after every operation | Auto-rounds with banker's rounding |
| **NMoneys (.NET)** | Not clearly documented (per public README) | Currency value object; precision behavior implementation-detail | Unknown from public surface |
| **`System.Money` / `CurrencyAmount` (.NET BCL)** | N/A | **No such type exists in the BCL**; only community libraries | N/A |
| **Stripe API** | **YES** at API surface | All amounts are integers in the currency's minor unit | Three-decimal currencies (BHD, KWD, OMR, JOD, IQD, LYD, TND) submitted as `value × 1000` |
| **PayPal API** | **YES** at API surface (per-currency rule) | Decimal string, "exactly 2 decimals" for two-decimal currencies; MGA must be multiples of 0.20 | Rejection / required rounding before submission |
| **Square API** | **YES** at API surface | `amount_money.amount` is integer in smallest denomination | Caller must convert |
| **Adyen API** | **YES** at API surface | Minor-unit integer; explicit per-currency decimal table | Caller must convert |
| **COBOL `PIC S9(n)V99`** | YES per field declaration | The field declaration *is* the precision contract; not derived from currency | Truncation / compile error |
| **SQL `DECIMAL(19,4)` convention** | No | Single column scale chosen for all currencies; intentionally *wider* than the narrowest minor unit | Accepts any value within column scale |
| **Hibernate JPA `@Column(precision, scale)`** | No | Scale chosen at mapping time; not derived from currency | Throws on overflow; rounds on assignment |
| **IFRS IAS 21 / US GAAP ASC 830** | No | Standards specify rates, methods, and reporting; do **not** prescribe value-precision rules tied to currency identity | Entity policy choice |

### Detailed findings

#### Joda-Money — YES (strict scale rejection)

Joda-Money is the strongest "yes" precedent. From the `Money` Javadoc: *"Every currency has a certain standard number of decimal places. This is fixed to this number of decimal places."* And on `Money.of(CurrencyUnit, BigDecimal)`: *"No rounding is performed on the amount, so it must have a scale compatible with the currency."* If the scale exceeds the currency's default fraction digits, an `ArithmeticException` is thrown. To accept an excess-precision input the caller must pass an explicit `RoundingMode`: `Money.of(USD, new BigDecimal("1.001"), RoundingMode.HALF_UP)` → `USD 1.00`.

Joda-Money's deliberate split between `Money` (currency-fixed scale) and `BigMoney` (arbitrary scale) is the cleanest precedent for the design choice Precept faces: a fixed/safe variant that's coupled to currency identity, and an explicit "I need more precision" escape hatch with a different type.

Sources: joda-money Javadoc; joda-money landing page.

#### JSR-354 / `javax.money` — Mostly NO; opt-in YES via `RoundedMoney`

The successor specification deliberately **decouples** precision from currency identity. From the JSR-354 RI user guide: `Money` *"is capable of supporting arbitrary precision and scale"* and is *"initialized with `MathContext.DECIMAL64`"* — neither tied to the currency. `FastMoney` has a *"fixed scale of 5 digits"* regardless of whether the currency is JPY (0 minor digits) or BHD (3 minor digits). Currency-aware rounding exists but is opt-in: callers must call `MonetaryRoundings.getRounding(currency)` and apply the operator (`amount.with(rounding)`).

`RoundedMoney` applies a configured `MonetaryOperator` after every operation. The operator *can* be `Monetary.getDefaultRounding()` (which is currency-aware), but it can equally be any custom operator — the type does not enforce that the rounding operator matches the currency.

This is a deliberate API-design statement: JSR-354 surveyed the prior art (including Joda-Money), and chose to **separate** precision from currency identity as a configurable concern (`MonetaryContext` + `MonetaryRounding`), rather than fold precision into the type contract.

Sources: JSR-354 RI user guide on GitHub; Baeldung JSR-354 walkthrough; Michael Scharhag overview.

#### Fowler's Money pattern (PoEAA) — Implementation-defined; the catalog quote is ambiguous

The PoEAA catalog entry says only: *"Monetary calculations are often rounded to the smallest currency unit. When you do this it's easy to lose pennies (or your local equivalent) because of rounding errors."* The catalog page does not prescribe currency-derived precision enforcement — it identifies the *problem* and refers to Chapter 18.

The de-facto implementations (go-money, paqio/money.dart, LitGroup/money.dart, kaiosilveira/poeaa-money, Verraes' PHP version) **do** treat the currency as the precision authority: the amount is stored as an integer count of the smallest subunit (cents for USD, fils for BHD), and multiplication/division round to the smallest unit. So in implementations the coupling is real, but it is a *storage representation* decision rather than a *type-system rejection*. None of the surveyed implementations reject `Money(USD, 1.001)` — they round at construction or store the rounded integer.

The `allocate(proportions)` operation is the canonical Fowler invariant: distribute money among recipients without losing a cent to rounding. This *requires* knowing the smallest unit, but does not require that the type system reject sub-minor-unit literals at construction.

Sources: martinfowler.com/eaaCatalog/money.html; community implementations cited above.

#### .NET — no BCL type; community libraries split

The .NET BCL has no `System.Money` or `System.CurrencyAmount`. The closest dotnet/runtime issues found (#100959 on `ISOCurrencySymbol`, #28913 on a currency-no-symbol format) address formatting, not a value type. There is no shipped or accepted proposal for a Money type in `dotnet/runtime`.

Community libraries split:
- **NodaMoney**: *"By default, `Money` is always rounded to the currency minor unit (use `MoneyContext` to override), which is executed after every arithmetic operation. … Auto-rounding to the minor unit will take place with MidpointRounding.ToEven also known as banker's rounding."* This is the closest **auto-round** analog to Joda-Money's **reject** stance.
- **NMoneys**: public README does not document precision-vs-currency enforcement explicitly.

Sources: dotnet/runtime issue search; NodaMoney README; NMoneys README.

#### Payment APIs — strong YES at the wire format, NOT at the type system

Stripe, Square, and Adyen all require amounts in **minor units as integers**, derived per currency. Stripe documentation: *"All API requests expect `amount` values in the currency's minor unit. … For example, enter 1099 to charge 10.99 USD (or any other two-decimal currency). Enter 10 to charge 10 JPY (or any other zero-decimal currency)."* For three-decimal currencies (BHD, IQD, JOD, KWD, LYD, OMR, TND), the convention is `value × 1000`.

Adyen explicitly publishes a per-currency minor-unit table and warns: *"For CLP, CVE, IDR, and ISK the ISO 4217 standard has a different number of decimals than shown in our currency codes table. When submitting amounts in minor units, the decimals in the table on this page are leading."* This is a real-world contradiction with ISO 4217 — meaning the precision table is **operational**, not just an ISO mirror.

PayPal is the outlier on representation — it accepts decimal strings, but enforces *"exactly 2 decimals"* for two-decimal currencies and special rules for currencies like MGA (*"amounts must be in multiples of 0.20, and will be automatically rounded"*).

These are wire-format contracts, not type-system constraints. But they uniformly confirm: in the production-grade financial domain, the **per-currency minor-unit table is the operative precision rule**, and excess precision is either auto-converted by the SDK or rejected at the boundary.

Sources: docs.stripe.com/currencies; docs.adyen.com/development-resources/currency-codes; developer.squareup.com payment refund object; PayPal developer docs.

#### COBOL `PIC S9(n)V99` — YES at field-declaration level, NOT derived from currency

The mainframe-banking tradition encodes precision in the *field declaration*, not the currency. `PIC S9(13)V99` (signed, 13 integer digits, 2 fractional, packed-decimal `COMP-3`) is the canonical money field for USD/EUR/GBP balances. The currency identity is implicit context (the field's purpose) — the precision is a fixed compile-time property of the field. Multi-currency mainframe systems handle this with `REDEFINES` overlays or by tagging each row with a currency code and a value column whose scale is the widest needed.

This is a *type-level constraint* in the sense that the COBOL compiler rejects values that overflow `V99`. But the constraint is not derived from the currency — it's set by the declarer.

Sources: MainframeMaster COBOL numeric types; pgrocer.net COBOL intro; tek-tips PIC clause threads.

#### IFRS IAS 21 / US GAAP ASC 830 — NO

Neither standard prescribes a value-precision rule keyed to currency identity. IAS 21 prescribes *which exchange rate* to use (closing rate, transaction-date rate, average rate as simplification) and how to present translation differences (FCTR in OCI). ASC 830 prescribes which items are monetary vs. nonmonetary and how to translate. **Rounding precision is an entity-policy choice**, not a standard. The standards are silent on whether USD must be presented to two decimal places.

This is a critical finding: the canonical accounting authorities do not enforce currency-precision coupling. Practitioners do, by convention.

Sources: ifrs.org IAS 21 standard page; iasplus.com IAS 21; PwC viewpoint ASC 830 framework; FASB.

#### SQL conventions — NO; the convention is intentionally to *over-provision*

The widespread `DECIMAL(19,4)` convention for SQL Server money columns is a deliberate **decoupling**: a single column-scale of 4 is chosen to be wider than any common currency minor-unit, so intermediate calculations don't lose precision. *"DECIMAL(19,4) is widely recommended for currency storage … The four decimal places provide a safety margin against rounding errors while still being practical for most currency scenarios."* The Hibernate / JPA pattern follows the same logic: `@Column(precision = 19, scale = 2)` for display amounts, but practitioners note *"For currencies with more decimals (e.g., Bitcoin uses 8 decimals), adjust scale to 8."*

This is the **opposite** of Precept's "structural impossibility" framing — SQL convention picks the loosest-bound scale to accommodate any currency, rather than the per-currency minimum.

Sources: mssqltips.com on money vs. decimal; bornsql.ca on storing currency in SQL Server; javaspring.net on Hibernate BigDecimal mapping.

#### Temenos T24 / core banking — not publicly documented at this level of detail

Public materials describe T24 as multi-currency but do not expose the internal money type definition. This is a gap; the search did not surface authoritative T24 internals from public sources.

## Implications for Precept

The decision space for F-LANG-BIZ-02 is:

- **Position 1 (strict structural coupling, Joda-Money `Money` model)**: `money in 'USD'` implies `maxplaces 2`; `money in 'USD' = 1.001` is a compile/runtime error unless an explicit rounding is provided.
- **Position 2 (auto-round to minor unit, NodaMoney model)**: `money in 'USD' = 1.001` silently rounds to `1.00`.
- **Position 3 (decoupled, JSR-354 `Money` / SQL convention model)**: `money in 'USD'` carries no implicit precision; precision is a separate type modifier (`money in 'USD' maxplaces 4`).

**The external precedent splits cleanly:**

- Position 1 has the strongest pedigree for a *Java-strict* library (Joda-Money) and for the *wire-format* layer of every major payments API (Stripe, Square, Adyen).
- Position 2 has the strongest pedigree for a *.NET-strict* library (NodaMoney) and for Fowler-pattern implementations.
- Position 3 has the strongest pedigree for *standards bodies* (IAS 21, ASC 830 — silent on precision), *the most widely used standardized library* (JSR-354 — explicitly decoupled), and *the database / ORM convention* (`DECIMAL(19,4)`).

The fact that the **standards bodies are silent** is the most load-bearing finding: there is no IFRS/GAAP requirement that USD amounts be stored or computed at exactly 2 decimal places. Precision-vs-currency coupling is a *library design choice*, not a *standard*.

The fact that **JSR-354 explicitly decoupled** after surveying Joda-Money is the second most load-bearing finding: the most recent, deliberated standardization effort in this space deliberately chose not to structurally couple, and instead provided precision as an opt-in operator. Their reason (inferable from `RoundedMoney`'s documentation warning *"RoundedMoney should only be used if you are well aware of its usage, since the immediate rounding may produce unwanted side effects"*) is that intermediate calculations often need more precision than the currency's minor unit — interest calculations, FX rate applications, allocations.

Critical nuance: **wire-format YES** and **type-system YES** are not the same. Every surveyed payment API enforces minor-unit precision **at the boundary** — but their *internal* representation choice (integer of minor units) is a *storage decision*. The Precept question is whether the value's *literal type* in source code rejects sub-minor-unit values. Joda-Money is the only mainstream library that does this; NodaMoney auto-rounds (which arguably violates Precept's "honesty about approximation" principle by silently losing data).

## Conclusions

The four-leg rationale for the recommendation that follows from this external survey:

**Recommendation: Position 3 (decoupled) at the surface, with Position 1 (strict) available as an explicit, opt-in form** — i.e., `money in 'USD'` should *not* automatically carry `maxplaces 2`, but the language should make it cheap and idiomatic to say `money in 'USD' maxplaces 2` when the author wants Joda-Money-style strictness.

- **Rationale**: The standards bodies (IAS 21, ASC 830) do not require currency-derived value precision, and the most recently deliberated mainstream API (JSR-354) explicitly rejected structural coupling on the grounds that intermediate calculations need more precision than the currency minor unit. Precept's "honesty about approximation" principle implies that silent auto-rounding (NodaMoney's Position 2) is the *worst* choice — it hides precision loss from the author. Between Position 1 (reject) and Position 3 (decouple), the precedent weight favors Position 3 at the *default* surface, because financial domain logic frequently computes at higher precision than the display minor unit (interest, FX, allocation).
- **Alternatives considered and rejected**:
  - *Position 1 as default* (Joda-Money model): rejected because it conflates the *display* precision rule with the *computation* precision rule, and forces every interest/FX/allocation use case to explicitly opt into a wider type. Joda-Money pays this cost by providing `BigMoney`; Precept would have to invent the same parallel type.
  - *Position 2 as default* (NodaMoney auto-round model): rejected because silent rounding violates Precept's "honesty about approximation" principle. Precept's philosophy says approximation must be visible in the type system, not hidden behind silently-rounding operators.
- **Precedent**:
  - JSR-354's `Money` (the most recent, most-deliberated standardization in this space) decoupled.
  - SQL `DECIMAL(19,4)` convention (the de-facto cross-language storage convention) decoupled and intentionally over-provisions scale.
  - IAS 21 and ASC 830 (the accounting authorities) are silent on currency-derived precision.
  - Stripe, Square, Adyen *do* enforce per-currency precision — but only at the **wire boundary**, not at the type system. This maps cleanly to Precept enforcing precision on `precept apply` or persistence boundaries, while keeping in-language values flexible.
- **Tradeoff accepted**:
  - Authors who want Joda-Money-style strictness must opt in explicitly (e.g., `money in 'USD' maxplaces 2`). This costs a few characters per declaration.
  - Precept cannot claim that "`money in 'USD'` is structurally constrained to 2 decimals" — that becomes a property of the *opt-in* form, not the bare form.
  - **Philosophy tradeoff**: Position 3 partially relaxes "Prevention, not detection" at the type-system level. Under D10, the implicit-precision constraint WAS a prevention mechanism — sub-minor-unit literals were rejected at compile time. Under Position 3, prevention RELOCATES to the wire boundary (the Phase 4+ F-LANG-BIZ-09 finding) where Precept-governed values cross into external state. Prevention as a principle is preserved; its enforcement point shifts from the type system to the boundary. This is consistent with the broader industry precedent: payment APIs enforce precision at the wire, not the type system.
  - The benefit accepted in exchange: financial-domain computations (interest, allocation, FX) can be written naturally without forcing a parallel `BigMoney` type into the language surface, and the language remains honest about the fact that intermediate computations carry more precision than display.

This recommendation directly answers F-LANG-BIZ-02 with **"No, `money in 'USD'` should not carry an implicit `maxplaces 2`; precision should be a separately-declarable constraint."** It is grounded in the broader precedent weight, not the loudest single example (Joda-Money). The Joda-Money strictness is preserved as an opt-in form, satisfying users who want it without imposing it on every declaration.

## Open Questions

1. **Boundary enforcement** *(promoted to F-LANG-BIZ-09 for Phase 4+ design pass)*: If Precept does not couple precision to currency at the *type* level, does it enforce minor-unit precision at the *operation* level — i.e., at persistence, at `transition apply`, at external integration boundaries? The Stripe/Square/Adyen precedent suggests boundary-layer enforcement is the industry norm. This is the structural follow-up to Position 3 — prevention relocates here.

2. **The opt-in syntax**: What is the exact syntactic form for "I want Joda-Money-style strictness on this field"? Position 3 picks `maxplaces N` (the existing modifier shipped with F-LANG-PRIM-02, 2026-05-10). Alternative not chosen: `minorunits` (would derive from currency identity, but coupling re-emerges through the back door). Discoverability is addressed by sample-corpus tidy at Phase 3 Step 3.5 (one canonical sample uses the opt-in idiom).

3. **Multi-currency arithmetic safety**: Joda-Money and JSR-354 both prohibit `USD + EUR` without explicit conversion. This survey did not investigate that dimension. Precept should separately confirm its position on currency-mismatch arithmetic. The current spec (`docs/language/business-domain-types.md` § Cross-currency arithmetic) declares it a type error; this needs verification.

4. **Hyperinflationary / non-ISO-standard currencies**: Adyen documented that CLP, CVE, IDR, and ISK have operational decimal counts that differ from the ISO 4217 standard. If Precept builds *any* currency-derived precision (even opt-in), it needs a policy for ISO drift. This is a real-world hazard, not a theoretical one. Out of Phase 3 scope; flag for future research.

5. **Temenos T24 / Finacle / Fiserv DNA**: The core-banking systems were not penetrable from public sources. If the Position 3 decision is contested in future, an interview or licensed-documentation pass with one of these vendors would strengthen the empirical base. Not blocking for F-LANG-BIZ-02.

6. **Crypto / sub-cent currencies**: Bitcoin's 8-decimal precision and other crypto conventions sit awkwardly with ISO 4217 framing. Out of scope for F-LANG-BIZ-02 but a known follow-up.

## Sources

- [Joda-Money `Money` Javadoc](https://www.joda.org/joda-money/apidocs/org.joda.money/org/joda/money/Money.html)
- [Joda-Money project page](https://www.joda.org/joda-money/)
- [JSR-354 RI Moneta user guide](https://github.com/JavaMoney/jsr354-ri/blob/master/moneta-core/src/main/asciidoc/userguide.adoc)
- [JSR-354 RI `RoundedMoney.java`](https://github.com/JavaMoney/jsr354-ri/blob/master/moneta-core/src/main/java/org/javamoney/moneta/RoundedMoney.java)
- [Baeldung — Java Money and the Currency API](https://www.baeldung.com/java-money-and-currency)
- [Michael Scharhag — JSR-354 Money and Currency API](https://www.mscharhag.com/java/java-jsr-354-money-and-currency-api)
- [Martin Fowler — PoEAA Money pattern catalog entry](https://martinfowler.com/eaaCatalog/money.html)
- [paqio/money.dart — Fowler Money implementation](https://github.com/paqio/money.dart)
- [Rhymond/go-money — Fowler Money implementation](https://github.com/Rhymond/go-money)
- [Verraes — Fowler Money in PHP](https://verraes.net/2011/04/fowler-money-pattern-in-php/)
- [NodaMoney README](https://github.com/RemyDuijkeren/NodaMoney)
- [NMoneys README](https://github.com/dgg/nmoneys)
- [dotnet/runtime issue #100959 — ISOCurrencySymbol proposal](https://github.com/dotnet/runtime/issues/100959)
- [dotnet/runtime issue #28913 — Currency without symbol formatting](https://github.com/dotnet/runtime/issues/28913)
- [Stripe — Supported currencies](https://docs.stripe.com/currencies)
- [Adyen — Currency codes and minor units](https://docs.adyen.com/development-resources/currency-codes/)
- [Square — POST /v2/payments reference](https://developer.squareup.com/reference/square/payments-api/create-payment)
- [PayPal — Payments v2 API](https://developer.paypal.com/docs/api/payments/v2/)
- [MainframeMaster — COBOL Numeric Data Types (PIC 9, S9, V, COMP, COMP-3)](https://www.mainframemaster.com/tutorials/cobol/numeric-data-types)
- [pgrocer.net — Introduction to COBOL Numeric Data](http://www.pgrocer.net/Cis12/cobol3.html)
- [IFRS Foundation — IAS 21 The Effects of Changes in Foreign Exchange Rates](https://www.ifrs.org/issued-standards/list-of-standards/ias-21-the-effects-of-changes-in-foreign-exchange-rates/)
- [IAS Plus — IAS 21 summary](https://www.iasplus.com/en/standards/ias/ias21)
- [PwC Viewpoint — ASC 830 framework](https://viewpoint.pwc.com/dt/us/en/pwc/accounting_guides/foreign_currency/foreign_currency__2_US/chapter_1_framework__US/13_framework_for_the_US.html)
- [MSSQLTips — SQL money vs. decimal for monetary values](https://www.mssqltips.com/sqlservertip/7431/sql-money-data-type-sql-decimal-monetary-values-sql-server/)
- [Microsoft Learn — money and smallmoney (T-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/data-types/money-and-smallmoney-transact-sql)
- [Born SQL — How should I store currency values in SQL Server?](https://bornsql.ca/blog/how-should-i-store-currency-values-in-sql-server/)
- [javaspring.net — BigDecimal in Java/Hibernate: MySQL Data Type for Money Columns](https://www.javaspring.net/blog/what-type-would-you-map-bigdecimal-in-java-hibernate-in-mysql/)
