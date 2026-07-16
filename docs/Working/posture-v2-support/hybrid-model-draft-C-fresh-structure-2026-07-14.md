---
title: The Hybrid Model — Prove-or-Reject and Governed
status: Draft C — 2026-07-14 (fresh structural attempt; for owner comparison)
author: Frank (Lead/Architect & Language Designer)
owner: Shane
---

# The Hybrid Model — Prove-or-Reject and Governed

## 1. One question decides how every value is enforced

A `.precept` definition is, at bottom, a collection of values under constraints. Fields hold values. Events carry values in. Expressions derive new values from old ones. Rules, bounds, and ensures constrain what those values may be. When someone asks "how does Precept make sure this constraint holds," the honest answer is always the same: it depends on the value the constraint is about — specifically, on **where that value came from.**

That is the whole model. Every constraint in a precept is enforced by one of two mechanisms, and which mechanism applies is decided by a single question asked of the value under constraint:

> **Did a Precept expression compute this value, or did the outside world supply it?**

If the value was **computed** — derived by an expression inside the definition — then every limit and rule that constrains it, and its freedom from evaluation faults, is settled **at compile time**. The compiler proves the value stays within all its declared constraints and cannot fault, or it **rejects the definition**. This mechanism is **prove-or-reject**.

If the value was **supplied** — entering the entity from outside the definition as a construction input, an event argument, or a direct edit of an editable field — then its declared constraints are enforced **at runtime**, on every operation, with no code path that can bypass them. This mechanism is **governance**.

Precept runs both. It is called the Hybrid model because a single definition uses both mechanisms at once — a computed total is proven, the external figures it was computed from are governed, and the two compose into one guarantee. This document is the classification model: how to determine, for any value or constraint in a definition, which mechanism enforces it and why. The pipeline machinery that *implements* each mechanism lives in [`compiler-and-runtime-design.md`](../../compiler-and-runtime-design.md); this document routes, that document builds.

### 1.1 The two mechanisms are one net over two regions

The reason a single question suffices is that Precept's guarantee is a *closed net*. [`compiler-and-runtime-design.md` §1](../../compiler-and-runtime-design.md) states it directly: coverage is defined structurally, as an exhaustive sweep over every place an invalid configuration could arise, and there are exactly two such places — a computed value that could land outside its declared limits, and an externally-supplied value crossing an operation boundary. The first is covered at compile time by prove-or-reject; the second is covered at runtime by governance. Neither region has a silent-skip path: a computed value the compiler cannot prove safe rejects the definition, and a supplied value that violates its contract is discarded before it commits. Where a hazard-list approach defaults to "unconsidered, therefore allowed," Precept defaults to "unproven, therefore rejected; ungoverned, therefore nothing commits." The guarantee fails closed.

Prove-or-reject and governance are the two regions of that one net. They deliver the same absolute guarantee — an invalid configuration cannot exist — through different mechanisms, because a computed value and a supplied value are reached at different times by different means. A computed value is fully determined by the definition, so its safety is a fact knowable before any entity exists; the compiler settles it once. A supplied value is unknown until a caller provides it, so its safety is enforced each time it arrives; the runtime settles it on every operation. Provenance is the axis along which the net divides because provenance is exactly what determines *when* and *by what means* a value can be pinned down.

This grounds the model in Precept's core commitments ([`philosophy.md`](../../philosophy.md)). **Prevention, not detection**: both mechanisms make invalid configurations structurally impossible rather than catching them afterward. **Governance, not validation**: the runtime half enforces declared constraints on every operation with no bypassable path, which is the philosophy's headline commitment. **Determinism**: same definition, same data, same outcome, in both regions. **Honesty about the guarantee's shape**: the compiler proves a *structural* fact about computed values and enforces a *declared contract* on supplied values, and it never presents one as the other.

### 1.2 You classify values and obligations, never constructs

The single most important discipline in applying this model: **the thing you classify is a value and the obligation attached to it, never the construct that spells the constraint.** A `field`, a `rule`, a `max` modifier, an `ensure` — these are not sorted into a "proven" pile and a "governed" pile. What gets sorted is each *value* the definition contains, by its provenance; the constraints then attach to whatever values they mention, and each of those values carries the mechanism its own provenance dictates.

This is why one construct can participate in both mechanisms without contradiction. A `rule` relating two supplied fields is governed as a constraint on those supplied values, and the very same rule can serve as a premise the compiler uses to prove a computed value safe. Nothing about that is a special case once the unit of classification is the value rather than the rule. Section 5 makes this concrete, because the confusion it dissolves — "is a rule proven or governed?" — is the question that recurs most often, and it recurs precisely when a reader tries to classify the rule instead of the values under it.

The rest of this document is the elaboration of the one question. Section 2 makes the question mechanical — how to read any value's provenance with certainty. Section 3 develops governance, the runtime region. Section 4 develops prove-or-reject, the compile-time region. Section 5 resolves the rule question directly. Section 6 states what the model deliberately refuses to do. Section 7 places the model in the surrounding canon.

---

## 2. Reading a value's provenance

Classification is only as rigorous as the provenance question is decidable, so this section makes it mechanical. Given any value in a definition, you determine its provenance by finding where the value originates in the dataflow — and the answer is always one of two kinds, with a sharp line between them.

### 2.1 The two origins

**Supplied (external origin).** A value is supplied when it enters the entity from outside the definition. There are exactly three entry points:

- **Construction inputs** — the arguments to the initial event that builds the entity (for example, `CatalogItems` in `event Create(CatalogItems as set of string notempty mincount 1, …) initial` in [`samples/shopping-cart.precept`](../../../samples/shopping-cart.precept)).
- **Event arguments** — values carried into an operation by a fired event (for example, `LoanDays` in `event Checkout(PatronId as string notempty maxlength 100, LoanDays as integer positive)` in [`samples/library-book-checkout.precept`](../../../samples/library-book-checkout.precept)).
- **Direct edits** — a value written straight into an `editable` field via the update operation (for example, `UnitPrice` and `Quantity`, both declared `editable`, in [`samples/invoice-line-item.precept`](../../../samples/invoice-line-item.precept)).

**Computed (internal origin).** A value is computed when a Precept expression inside the definition derives it. The forms are:

- **Computed fields** (`<-`) — a field whose value *is* an expression, recomputed on every read, that cannot be assigned (for example, `field Subtotal as money in '{CurrencyCode}' <- UnitPrice * Quantity` in the invoice sample).
- **Assignment results** (`set F = expr`) — a value produced by evaluating an expression in an action and written into a field (for example, `set Balance = Deposits - Withdrawals`).
- **Derived collection state** — the count or contents a collection reaches after a grow or shrink action (`add`, `enqueue`, `remove`, `dequeue`, …), where the resulting cardinality is a computed quantity even though the element may have been supplied.

The dividing line is the one the language spec draws at [§0.7](../../language/precept-language-spec.md): governance enforces a supplied value's constraints "at the moment it enters, before any computation derives from it." The instant a Precept expression consumes a supplied value and produces a result, that result is a computed value with an internal origin. An event argument at the slot it feeds is supplied; the sum, product, or difference an expression makes from it is computed.

### 2.2 What determines provenance, and what is irrelevant to it

Provenance depends only on origin: was this value produced by an expression in the definition, or handed in from outside? Five things that might look decisive are irrelevant to it, and holding this line is what keeps the model airtight:

- **The spelling of the constraint.** `max 100` and `rule X <= 100` are the same constraint in different syntax ([spec §2.4](../../language/precept-language-spec.md); a modifier desugars to a rule). They impose identical obligations. A value's provenance — and therefore its enforcement mechanism — is the same under either spelling.
- **The difficulty of the proof.** A computed value that is hard to prove safe is still a computed value; difficulty changes the *outcome* of prove-or-reject (discharge versus rejection), never the mechanism.
- **The decidability of a constraint.** Decidability governs whether the compiler can discharge a prove-or-reject obligation, as Section 4.3 develops. It has no bearing on provenance and never reassigns a value from one region to the other.
- **The position of a declaration in the file.** A `default` and a `set` action that assign the same field are both internal to the definition; neither is nearer to "external" by virtue of where it sits.
- **The number of fields a constraint mentions.** A one-field bound and a five-field relational rule are both governed when they constrain supplied values and both usable as premises when they help prove a computed one. How many fields a constraint spans is a matter of its shape; the mechanism still follows each value's origin.

### 2.3 The procedure

To classify any constraint's enforcement, do not look at the constraint first. Look at each value the constraint is about:

1. **Name the value under constraint.** For `field Total max 100`, the value is whatever occupies `Total`. For `rule A >= B`, there are two values, `A` and `B`.
2. **Trace each value to its origin.** Is it a construction input, event argument, or direct edit (supplied), or does an expression inside the definition derive it (computed)?
3. **Apply the mechanism per value.** A supplied value is governed (Section 3). A computed value is proven-or-rejected (Section 4).
4. **A constraint over more than one value is enforced per value.** If it relates supplied values, it is a governed constraint (Section 3.2). If it constrains a computed value, it also becomes a prove-or-reject obligation on that computed value — and may serve as a premise for it. One constraint, classified through each value it touches (Section 5).

Everything that follows is this procedure worked out in depth for each region and then turned on the case where a single rule sits across the line.

### 2.4 The procedure on a whole definition

Applied to a complete definition, the procedure classifies every value with no residue. Take the stateless [`samples/invoice-line-item.precept`](../../../samples/invoice-line-item.precept), which governs one invoice line — price, quantity, discount, tax — and its derived totals. It declares two kinds of field, and the procedure sorts them cleanly:

- The **source fields** are `editable`: `CurrencyCode`, `Description`, `UnitPrice`, `Quantity`, `DiscountPercent`, `TaxRate`. Each is written by direct edit from the host's line-editor. Every value they hold is supplied. Their declared constraints — `UnitPrice` `nonnegative`, `Quantity` `positive`, `DiscountPercent` `nonnegative max 100 maxplaces 2`, `TaxRate` `nonnegative max 1 maxplaces 4` — are governed, enforced at ingress as each edit lands (Section 3.1).
- The **computed fields** use `<-`: `Subtotal <- UnitPrice * Quantity`, `DiscountAmount <- Subtotal * DiscountPercent / 100`, `TaxableAmount <- Subtotal - DiscountAmount`, `TaxAmount <- TaxableAmount * TaxRate`, `LineTotal <- TaxableAmount + TaxAmount`. Every value they hold is computed. Their money-typed bounds, the division by `100`, and the interval containment of each result are prove-or-reject obligations, settled at compile time (Section 4).

The classification is exhaustive and reversible: point at any field and the procedure names its mechanism, and the two lists together account for every value in the definition. The source fields' `nonnegative`/`positive`/`max` modifiers are also what make the computed fields provable — the same declared facts that ingress governs on the source values serve as the premises that discharge the computed values' containment obligations. That overlap is the composition of the two regions (Section 5), and it is visible here in a nine-field file with no lifecycle at all.

---

## 3. Supplied values are governed

Every value that enters the entity from outside the definition is enforced against its declared constraints by the runtime, on every operation, with no path that reaches a commit without passing. This is governance — the philosophy's "governance, not validation": the engine structurally refuses to let the value commit in a configuration that violates a declared rule, on every operation, so the enforcement holds by the structure of the engine rather than by a caller remembering to invoke a check.

Governance is delivered through **two enforcement points**, and they are equally fundamental. The runtime checks a supplied value's own contract **at ingress**, as the value enters and before the working copy derives anything from it, and it checks every applicable constraint against the finished configuration **at the post-mutation sweep**, after all mutations complete. The first upholds each value's contract as it arrives; the second upholds every whole-configuration rule against the result. Both are governance, both run on every operation, and both draw from the same discard-whole atomicity. Neither exists to compensate for a shortcoming of the other; they cover different obligations, and the definition needs both to be governed completely.

### 3.1 Ingress — a value's own contract, as it enters

At ingress the runtime enforces the constraints a supplied value carries *on its own slot* — the bounds and shape declared on the field it enters or the event argument that carries it — before any computation derives a dependent value from it. This is per-value and operation-blind: it holds the value's contract whether or not any other part of the definition ever reads the field.

The obligations settled at ingress are the ones expressible against a single entering value in isolation:

- **Numeric and unit bounds.** In [`samples/library-book-checkout.precept`](../../../samples/library-book-checkout.precept), `event Checkout(…, LoanDays as integer positive)` declares its loan length `positive`; a `Checkout` fired with a non-positive `LoanDays` is refused as the argument enters. In [`samples/statistical-process-control.precept`](../../../samples/statistical-process-control.precept), the measurement limits are `quantity in '{MeasurementUnit}'` fields set by direct edit; an edit supplying a magnitude in an incompatible unit, or outside the field's declared band, is refused at ingress against that field's own contract.
- **String shape.** `event Checkout(PatronId as string notempty maxlength 100, …)` requires the patron identifier to be non-empty and at most 100 characters; a supplied `PatronId` that is empty or over-length is refused as it enters.
- **Presence and collection shape on a single supplied value.** `event Create(…, CatalogItems as set of string notempty mincount 1, …) initial` in [`samples/shopping-cart.precept`](../../../samples/shopping-cart.precept) requires the supplied catalog to be a non-empty set of non-empty strings; a construction input violating that shape is refused before the entity is built.

In each case the value is checked against the contract of the slot it lands in, independently of the rest of the configuration. That independence is exactly what makes ingress the right home for these obligations, and exactly why it cannot be the only enforcement point: a constraint that relates *several* values cannot be judged from any one of them alone.

### 3.2 The post-mutation sweep — every rule against the finished configuration

Some constraints are properties of the whole configuration rather than of one entering value. A rule relating two dates, a geometric ordering among five limits, a state's requirement that a field be present, an event's precondition tying an argument to a stored field — none of these can be settled as a single value enters, because their truth depends on the completed set of values the operation produces. The post-mutation sweep is the enforcement point for exactly these.

The mechanism, per [spec §3A.4](../../language/precept-language-spec.md): all mutations of an operation execute on a working copy; after every mutation completes, the engine evaluates **every applicable constraint** — global rules (unconditional and guarded), state ensures (`in`/`to`/`from`), and event ensures — against that completed working copy. If all pass, the working copy is promoted to committed state. If any fails, the working copy is discarded whole and the entity is unchanged, and the operation returns `ConstraintsFailed` carrying each violated constraint with its declared `because` rationale ([`runtime/result-types.md`](../../runtime/result-types.md)). No partially-committed configuration with a violated rule is ever observable, even transiently.

The obligations the sweep settles, across domains and constraint kinds:

- **Cross-field temporal rules.** In [`samples/academic-course-registration.precept`](../../../samples/academic-course-registration.precept), `rule RegistrationCloses > RegistrationOpens when RegistrationOpens is set and RegistrationCloses is set` relates two supplied dates. Neither date can be judged as it enters; the sweep evaluates the ordering against the finished configuration and discards any working copy in which the close date is not after the open date.
- **Cross-field quantity ordering.** The control-chart geometry in the SPC sample — `rule LowerControlLimit < LowerWarningLimit`, `rule LowerWarningLimit < TargetValue`, `rule TargetValue < UpperWarningLimit`, `rule UpperWarningLimit < UpperControlLimit` — is a chain of relationships among five supplied `quantity` limits. The sweep enforces the whole ordering after edits complete; an edit that leaves the limits out of order is discarded.
- **State ensures.** In the library sample, `in CheckedOut ensure BorrowerId is set` requires the borrower to be recorded whenever the book is checked out. The sweep checks this against the configuration produced by any operation that lands the entity in `CheckedOut`, discarding a working copy that would leave a checked-out book without a borrower.
- **Event ensures.** `on ResolveLoss ensure ResolveLoss.SettlementAmount >= '0.00 USD'` in the library sample constrains a supplied event argument in the context of the operation it belongs to. The requirement is upheld against the working copy the event produces.
- **Cross-field count relationships.** In the academic sample, `rule TotalCreditHours >= CourseCount because "…every course carries at least one credit"` relates two accumulated counts. The sweep enforces the relationship on the completed configuration after an add or drop.

Across all of these the pattern is uniform: the constrained values are supplied (or accumulated from supplied inputs), the constraint is a property of the finished configuration, and the sweep enforces it against the completed working copy with discard-whole atomicity. Governance covers both the per-value contracts of Section 3.1 and the whole-configuration rules here; a definition is governed completely only because both enforcement points run on every operation.

| Enforcement point | When it runs | What it settles | Example |
|---|---|---|---|
| **Ingress** | As each external value enters, before the working copy derives anything from it | A single supplied value's own-slot contract — its declared bounds, units, string shape, presence, collection shape | `LoanDays as integer positive`; `PatronId as string notempty maxlength 100` |
| **Post-mutation sweep** | After all mutations complete, against the finished working copy | Every applicable whole-configuration constraint — global rules, state ensures, event ensures — with discard-whole atomicity | `rule RegistrationCloses > RegistrationOpens`; `in CheckedOut ensure BorrowerId is set` |

### 3.3 Guards, editability, and the reach of governance

Two refinements round out the runtime region. First, a **guarded** rule (`rule … when condition`) is governed exactly like an unconditional one, restricted to the configurations its guard selects: the sweep evaluates it only where the guard holds. The academic sample's `rule TotalCreditHours <= 15 when StudentLevel == "Graduate"` governs graduate registrations and stays silent on others; the guard makes the rule precise, and precision does not weaken the guarantee. Second, **editability windows** decide *where in the lifecycle* a field may be supplied by direct edit — `in Available modify BookTitle editable` in the library sample opens `BookTitle` to edits only on the shelf. Editability changes which operations can supply a value; it does not change that a supplied value is governed. A field editable in one state and locked in another is governed wherever an edit can reach it.

### 3.4 Governance delivers prevention as it commits

A supplied value's constraint is enforced *before* the operation commits, on a working copy the engine discards whole if the constraint fails. The invalid configuration is never promoted and never observable; the operation reports a structured refusal instead. That is prevention in the same sense the compile-time region delivers it — an invalid configuration cannot come into being — reached by refusing to commit rather than by settling everything before any entity exists. The regions differ in *when* and *by what means* the refusal happens, and both make the guarantee hold. Governance is the primary and complete enforcement for every value whose origin is external: it settles that value's declared contract as a first-class mechanism, standing on its own rather than backing up a proof.

---

## 4. Computed values are proven, or the definition is rejected

Every value a Precept expression derives inside the definition is settled at compile time. For each such value the compiler discharges the full set of obligations that constrain it, or it rejects the definition and names what would make the value provably safe. There is no runtime step that finishes the job; for a computed value there is no external door at which a runtime check could sit, so the compile-time proof is the entire guarantee.

### 4.1 The scope is every constraint on the value, plus fault-freedom

The scope of prove-or-reject is stated most precisely by starting from the value and gathering everything that constrains it:

> **Every computed value must be provably within every declared bound and business rule that constrains it, and provably free of evaluation faults — or the definition is rejected.**

The organizing idea is the *value and its full set of constraints*, not any particular category of hazard. When an expression derives a value, the compiler asks: what limits does this value have to respect, and can each be proven to hold for every configuration the definition admits? Those limits include the ordinary declared bounds and rules on the value — a computed total declared `nonnegative`, a computed amount that must stay within a `max`, a computed string that must fit a `maxlength`, a derived count that must respect a `maxcount`. A computed value that could land outside any one of these is a prove-or-reject failure, exactly as much as a division that could divide by zero.

Freedom from **evaluation faults** is one enumerable part of that scope — the part concerned with the expression completing at all rather than with where its result lands. It covers division by zero, `sqrt`/`pow` domain violations, empty-collection and out-of-range access, and representational overflow. These are real obligations and the proof engine discharges them, and they are one coverage detail inside the broader scope. A business-rule bound on a computed value — "this derived total must be non-negative" — is a prove-or-reject obligation in full standing even though violating it throws nothing and involves no fault in the ordinary sense. [`compiler-and-runtime-design.md` §1](../../compiler-and-runtime-design.md) makes the same point from the other direction: the short list of division, overflow, and access faults is an *illustration* of the proof engine's coverage, and the boundary is the exhaustive sweep over every site where a computed value could leave its declared limits.

One honest boundary within the fault detail: **representational overflow** — a computed value exceeding what its numeric type can represent — is currently a disclosed, owner-parked gap backstopped by a defense-in-depth runtime trap, and no document may claim it prevented. This is a deliberately narrower guarantee on that one fault, held apart so the surface stays honest ([`philosophy.md`](../../philosophy.md) on honesty about the guarantee's shape). It is a disclosed gap, not prevention relocated to runtime, and it does not touch the containment obligations above: a computed value exceeding a *declared* `max` is a live prove-or-reject failure, distinct from a value exceeding its *type's* range.

### 4.2 The three outcomes, two of which reject

For each obligation on a computed value the proof engine reaches exactly one of three verdicts ([spec §0.6](../../language/precept-language-spec.md), Proof philosophy #2 and #5):

- **Proven.** The value provably stays within the constraint for every admitted configuration. The obligation discharges, carrying a legible certificate that an independent checker could replay.
- **Proven-violating.** The value provably breaks the constraint on some reachable configuration. The definition is rejected, with a concrete witness configuration that violates.
- **Unresolved.** The compiler can neither prove nor disprove the constraint. The definition is rejected, with the weakest precondition that would discharge the obligation printed verbatim.

Both non-proven verdicts reject. An unresolved obligation rejects because the definition left a reachable case with no authored disposition, and prevention does not admit that ([spec §0.6](../../language/precept-language-spec.md), Proof philosophy #5). The author resolves an unresolved verdict by supplying the constraint its printed precondition names — a bound on a source field, a guard on the action, a relational rule the prover can use.

### 4.3 Decidability decides discharge, and only discharge

Whether an obligation lands on Proven versus Unresolved is a matter of decidability: can the proof engine, from the facts the definition declares, establish that the computed value stays within its constraint? A literal divisor of `100` is trivially decidable; a bound whose satisfaction depends on an unbounded source field is not.

Decidability decides whether a prove-or-reject obligation discharges or the definition is rejected. It does one thing more and no other: it grades how strong a *premise* a declared constraint provides when the compiler tries to discharge some other value's obligation (Section 5). Decidability never reassigns an obligation from the compile-time region to the runtime region. An undecidable bound on a computed value does not "fall to governance" — there is no such move. It rejects the definition, and the author strengthens the definition until the obligation is decidable. Routing by provenance is what makes this stable: if the compiler routed by difficulty instead, every newly hard case would become a candidate for deferral, and the guarantee would erode one hard expression at a time. Because routing is by origin, a computed value is proven or the definition is rejected, always.

### 4.4 Worked obligations across domains

- **Money — interval containment through a computation, with division.** In [`samples/invoice-line-item.precept`](../../../samples/invoice-line-item.precept), `field DiscountAmount as money in '{CurrencyCode}' <- Subtotal * DiscountPercent / 100` is a computed field. `DiscountPercent` is declared `nonnegative max 100`, `Subtotal` derives from `UnitPrice` (`nonnegative`) and `Quantity` (`positive`). The proof engine propagates these operand intervals forward through `*` and `/`, discharges the division (`100` is a literal, provably non-zero), and checks the resulting interval against `DiscountAmount`'s own bounds. The structural modifiers on the source fields are what make the computed total provable; strip them and the containment obligation would reject. Every computed field in this stateless line item — `Subtotal`, `TaxableAmount`, `TaxAmount`, `LineTotal` — is settled the same way, entirely at compile time.
- **Quantity — a derived stock level staying non-negative.** In [`samples/inventory-item.precept`](../../../samples/inventory-item.precept), `set QuantityOnHand = QuantityOnHand - RecordShrinkage.Qty` derives a new stock level, and `QuantityOnHand` is declared `nonnegative`. The obligation is that the subtraction cannot drive the level below zero. It discharges from the event ensure `on RecordShrinkage ensure RecordShrinkage.Qty <= QuantityOnHand` — a premise that bounds the amount written off by the stock on hand. Without that premise the obligation would be unresolved and the definition rejected. (This same example returns in Section 5, because the premise is itself a governed constraint.)
- **Collection cardinality — a derived count against a bound.** A grow action into a capacity-bounded collection derives a new count that must respect the bound. For a `queue of T maxcount 1`, two successive `add` actions drive the count to 2, which the proof engine derives from the sequential count interval and rejects with `CountBoundViolation` ([spec §0.6](../../language/precept-language-spec.md); the count is "no result outside a declared bound," discharged by an author guard such as `when C.count < 1`, never deferred). The author routes the at-capacity case to a reject row or guards the grow.
- **Collection access — an accessor precondition.** A `dequeue` or `pop` on a possibly-empty collection is an empty-access obligation on a computed access. It discharges when a preceding grow establishes `count > 0` on the path, or when an authored `when C.count > 0` guard does; otherwise the definition is rejected.
- **String length — a computed string against a cap.** [`samples/statistical-process-control.precept`](../../../samples/statistical-process-control.precept) shows prove-or-reject deciding a design. `CurrentAlertReason` is assembled by interpolating measured `quantity` values whose magnitudes are open-ended, so no `maxlength` on that field could be proven to hold; the field is therefore declared without a length cap, and the sample says so explicitly ("the proof engine correctly rejects a length cap here"). Had a cap been declared, the containment obligation on the computed string would be unresolved and the definition rejected — the guarantee shaping the definition rather than being bypassed by it.

Each of these is a computed value whose full set of constraints — containment in a declared bound, freedom from a fault, or both — is settled at compile time. The mechanism is uniform across money, quantities with units, collections, and strings; the domain and the constraint kind vary, the classification does not.

### 4.5 Units are part of the containment reasoning

Because Precept's numeric types carry dimension and unit, the proof engine's interval reasoning is unit-aware ([spec §0.6](../../language/precept-language-spec.md), obligation #5). When a computed value's bound and the expression producing it use statically convertible units within one physical dimension, the engine normalizes magnitudes to base-unit equivalents before checking containment. A computed `quantity` in `mm` proven against a bound expressed in `in` is one obligation the engine discharges by conversion, so a cross-unit comparison such as a `mm` sample against an `in` limit becomes a compile-time impossibility rather than a runtime surprise — the design the SPC sample relies on when it types every measured field as `quantity in '{MeasurementUnit}'`. Explicit counting-unit mismatches that are not statically convertible (`each` versus `box`) are not silently bridged; they require matching qualifiers or an explicit conversion. Units do not add a region to the model — a computed quantity is proven and a supplied quantity is governed, the same as any other value — they enter the model as part of what "within the declared bound" means for a dimensioned value.

---

## 5. The rule question — when a rule is proven, governed, or both

The question that recurs most, and the one the value-first discipline of Section 2.3 exists to answer, is: **is a `rule` proven or governed?** The framing of the question is what traps people. A rule is not a value; it is a constraint over the values it names. It is therefore never routed by the one question of Section 1 — that question is asked of values, and a rule has no single provenance to read. You classify a rule by classifying each value it constrains.

### 5.1 A rule is always governed, and may additionally be a premise

Two facts settle every case:

**A rule is always a governed constraint.** Whatever values it relates, a declared rule is enforced by the runtime on every operation. If it constrains a single supplied value on its own slot, it is upheld at ingress; if it relates several values — the common case for a rule — it is upheld at the post-mutation sweep against the finished configuration (Section 3.2). A rule is never *not* governed.

**A rule may additionally serve as a compile-time premise.** When some computed value elsewhere carries a prove-or-reject obligation, a declared rule can supply the relational fact that discharges it. This is a second role the same rule plays, layered on top of its governance while its governance continues unchanged. The rule contributes its provable fact to the proof engine's knowledge to the extent that fact is decidable in context, and that premise is what lets a computed value's obligation discharge instead of reject ([spec §0.6](../../language/precept-language-spec.md), obligations #1–#3).

From these two facts a sharp statement follows: **a rule is never itself "proven."** What gets proven is always a *computed value's* freedom from fault and containment within its bounds. A rule may be the premise that discharges that proof, but the rule itself remains a governed constraint throughout, enforced at runtime like every other. "Proven" and "governed" are not competing labels a rule chooses between; the rule is governed, and it may help prove something else.

### 5.2 The flagship — one rule, both roles, no double standard

```precept
precept AccountLedger

field Deposits as money in 'USD' default '0.00 USD' nonnegative editable
field Withdrawals as money in 'USD' default '0.00 USD' nonnegative editable
field Balance as money in 'USD' default '0.00 USD' nonnegative

rule Deposits >= Withdrawals because "An account cannot withdraw more than it has deposited"

on Settle -> set Balance = Deposits - Withdrawals
```

`Deposits` and `Withdrawals` are independently-set external fields — supplied by direct edit. `Balance` is computed by the `Settle` hook. There are two distinct obligations here, and tracing each value settles both.

**Obligation A — Balance's bound.** `Balance` is a computed value with a `nonnegative` bound. The assignment `set Balance = Deposits - Withdrawals` produces a value that must be proven within that bound, or the definition is rejected. This is strictly prove-or-reject. It discharges only because the rule `Deposits >= Withdrawals` supplies the premise that makes the difference provably non-negative; remove the rule and Obligation A is unresolved and the definition rejected, naming the relation that would discharge it.

**Obligation B — the rule itself.** `rule Deposits >= Withdrawals` relates two independently-set external fields, so it is a governed constraint. Because it is a cross-field relationship, its enforcement point is the post-mutation sweep: a direct edit that raises `Withdrawals` past `Deposits` passes ingress (the edited value is within its own field's contract) and executes on the working copy, but the completed working copy violates the rule, the sweep discards it whole, and the operation returns `ConstraintsFailed` carrying the rule and its `because`. The invalid configuration never commits. That same rule additionally plays the premise role for Obligation A — one declared constraint doing both jobs at once.

The apparent double standard dissolves under per-field provenance. Consider the contrast the model is often challenged with. A `rule Balance >= MinBalance` over two independently-set editable fields is purely governed — both operands are supplied, so it is a cross-field rule the sweep enforces, with no proof involved. The flagship's `rule Deposits >= Withdrawals` over two independently-set editable fields is *equally* governed by the same reasoning — and it *additionally* serves as a premise, because a *separate* computed value, `Balance`, depends on the relation it states. The rule's own disposition is identical in both cases: governed, enforced at the sweep. The two cases differ only in whether some computed value elsewhere leans on the rule; the rule's own treatment is the same. Per-field provenance decides every value the same way every time, and the rule earns no different standard by helping prove `Balance`.

### 5.3 The same duality in a different domain

The pattern is not particular to bank balances. In [`samples/inventory-item.precept`](../../../samples/inventory-item.precept), the shrinkage flow shows the identical structure in quantities with units:

```precept
field QuantityOnHand as quantity of '{StockingUnit.dimension}' default '0 {StockingUnit}' nonnegative
…
event RecordShrinkage(Qty as quantity of '{StockingUnit.dimension}', Reason as string notempty)
on RecordShrinkage ensure RecordShrinkage.Qty <= QuantityOnHand because "Cannot write off {RecordShrinkage.Qty} when only {QuantityOnHand} is on hand — shrinkage cannot exceed physical stock"
…
from Listed on RecordShrinkage
    -> set QuantityOnHand = QuantityOnHand - RecordShrinkage.Qty
    …
```

`QuantityOnHand` after the write-off is a computed value carrying a `nonnegative` obligation (Obligation A): the subtraction must be proven not to drive stock below zero. The event ensure `RecordShrinkage.Qty <= QuantityOnHand` is a governed constraint on a supplied argument in the operation's context (Obligation B) — upheld by the runtime so the write-off can never exceed physical stock — and it is simultaneously the premise that discharges Obligation A. A governed constraint over supplied values, doubling as the premise that lets a computed value's bound be proven: the flagship's structure, in the units domain, drawn straight from a shipping sample. The classification model applies unchanged.

---

## 6. What the model deliberately excludes

The model has a sharp edge, and naming what sits just outside it is part of stating it precisely.

**There is no runtime disposition for an unprovable computed value.** When the compiler cannot prove a computed value stays within its constraints, the definition is rejected. The tempting third option — accept the definition and let a runtime check refuse the bad computed value when it arises — is excluded on principle. A computed value has no external door at which a runtime check could sit; it is produced inside the definition, mid-operation, with no ingress slot to govern. A runtime refusal of a computed value would satisfy the mechanical act of discarding a working copy but would abandon the thing that makes prevention meaningful for computed data: the guarantee that the value's safety was settled before any entity existed. That is why the phrase "an undecidable bound on a computed value falls to governance" describes a move the model does not contain. Decidability decides discharge versus rejection (Section 4.3); it never delivers a computed value to governance. The author's recourse to an unresolved obligation is to strengthen the definition — a bound on a source, a guard, a relational rule — so the obligation becomes provable.

**There is no author escape hatch.** No marker lets an author opt a computed site out of its proof obligation. Such a marker would reintroduce the bypassable, uncheckable path that governance-not-validation exists to eliminate, and it would let a definition compile with a fault-prone site the compiler never proved safe. Where the engine cannot derive a bound, the author states the missing fact as a rule the engine then uses, which keeps the guarantee intact and the reasoning legible.

Excluding both keeps the two regions clean: a computed value is proven or the definition is rejected; a supplied value is governed. Nothing straddles that line at runtime, and nothing opts out of it at compile time.

---

## 7. Where the model sits in the canon

This document is the classification model — how to determine, for any value or constraint, which mechanism enforces it. It draws on and defers to the canonical documents for grounding and for mechanism:

- **[`philosophy.md`](../../philosophy.md)** — the core commitments the model serves: prevention not detection, governance not validation, determinism, and honesty about the guarantee's shape. The Hybrid model is how those commitments are met across values of different origin.
- **[`compiler-and-runtime-design.md`](../../compiler-and-runtime-design.md)** — the mechanism. §1 and §1.1 state the one-net/two-regions framing this document routes by, and §1.2 walks the single connected story from a fault-prone site through compile-time proof and runtime governance. When you need to know *how* an obligation is stamped, discharged, or enforced, that document builds what this one classifies.
- **[`precept-language-spec.md`](../../language/precept-language-spec.md)** — the language-level contract. §0.6 is the proof engine design contract and its three-way verdict; §0.7 is the compile-time-and-runtime guarantee contract and the composition of the two mechanisms; §3A.4 is mutation atomicity and the two enforcement points of governance (ingress and the post-mutation sweep).
- **[`soundness-and-coverage.md`](../../compiler/soundness-and-coverage.md)** — the coverage sweep and the certificate format that make prove-or-reject sound: the exhaustive site sweep, the witness a violating verdict carries, and the legible certificate a discharged obligation emits.
- **[`runtime/result-types.md`](../../runtime/result-types.md)** — the outcome taxonomy, including the `ConstraintsFailed` verdict the post-mutation sweep returns when a governed constraint fails.

The division of labor to keep in mind: provenance is the axis, the two regions are prove-or-reject and governance, and the unit you classify is always a value and its obligations. Read a value back to its origin, apply the mechanism that origin dictates, and classify a rule through each value it touches. Every enforcement question a definition raises resolves through that one procedure.

### 7.1 Reference

| Value origin | Mechanism | When settled | Enforcement point | Refusal on failure |
|---|---|---|---|---|
| **Supplied** — construction input, event argument, direct edit of an `editable` field | Governance | Runtime, every operation | Ingress for a value's own-slot contract; post-mutation sweep for whole-configuration rules | Working copy discarded whole; operation returns `ConstraintsFailed` / an ingress refusal, entity unchanged |
| **Computed** — `<-` field, `set F = expr`, derived collection count | Prove-or-reject | Compile time, before any entity exists | Proof engine discharges each obligation against declared facts | Definition rejected — `ProvenViolating` with a witness, or `Unresolved` with the weakest precondition |

| A `rule` … | Its disposition |
|---|---|
| relating supplied values only | Governed — at the sweep if it spans several fields, at ingress if it constrains one entering value's own slot |
| relating supplied values, and also stating a fact a computed value's proof needs | Governed (unchanged), and additionally a compile-time premise that can discharge that computed value's obligation |
| mentioning a computed value's bound | The computed value carries a prove-or-reject obligation; the rule may still be governed over any supplied fields it also names |

A rule is never itself proven; a computed value is what gets proven, and a rule may be the premise. A supplied value is governed; a computed value is proven, or the definition is rejected. Provenance decides, one value at a time.
