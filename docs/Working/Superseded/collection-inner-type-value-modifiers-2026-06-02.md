> **SUPERSEDED 2026-06-11** — replaced by [`compiler-readiness-plan-2026-06-11.md`](../compiler-readiness-plan-2026-06-11.md). Retained for history; all still-valid obligations were mined into that plan (see its `-appendices/working-docs-triage.md`). Do **not** treat as current strategy.

# Design — Collection inner-type value modifiers

| Property | Value |
|---|---|
| Status | **Locked 2026-06-02** *(owner-authorized after adversarial `precept-reviewer` pass — verdict lock-ready, catalog+code reuse confirmed against source)* |
| Kind | Language-surface design (new modifier position) |
| Phase target | Relational-rules-and-bounds plan, Slice 3 (collection element-type bounds) |
| Comparable-systems research status | partial — inline survey (no standalone `research/language/` study existed; gap noted below) |
| Supersedes / superseded by | — |
| Owner-locked decisions inherited | Two core decisions pre-aligned with owner this session (declared-modifier syntax; full scalar parity) — recorded, four-legged, not re-litigated |

**Sources consulted (per decision):**

- `docs/philosophy.md` (full) — core commitments; Philosophy Alignment matrix.
- `docs/language/precept-language-spec.md` — §0.1 (11 principles), §0.4 (execution-model properties), §0.6 (proof-engine design contract), §0.7 (compile-time/runtime guarantee contract + Composition), §2.3 (`CollectionType`/`CollectionInnerType` grammar, 1099-1106), §2.4 (constraint modifiers are rule shorthand), §3.5 (scope rules + quantifier binding scope), the modifier↔type applicability table (1658-1687).
- `docs/language/collection-types.md` (full, 1532 lines) — inner type system, the `Rejected: Bounded Collection (capacity modifier)` decision (1424-1442), Constraint Catalog (748-772), the "Element-level constraints → use a quantifier" callout (772), `~string`/qualified inner-type precedent for inner-type-carried metadata.
- `docs/compiler/proof-engine.md` — Strategy 9 (Length Containment, 1691-1705), Strategy 10 (Count Containment, 1707-1711), the member-access length-interval row (1702).
- `src/Precept/Pipeline/ProofEngine.Lengths.cs` — `StringLengthIntervalOf`, `MemberAccessStringLengthInterval` (the dead-but-sound element-bound hook at 217-223), `TryLengthContainmentProof`.
- `src/Precept/Pipeline/ProofEngine.Intervals.cs` — `IntervalOfNarrowed` (numeric reader; no member-access arm today → element numeric reads are Unbounded).
- `src/Precept/Pipeline/SemanticIndex.cs` — `TypedElementType` DU (`TypedScalarElement`/`TypedChoiceElement`/`TypedQualifiedElement`, 349-372); `TypedField` length/numeric/count carrier fields (396-406).
- `src/Precept/Pipeline/TypeChecker.cs` — `BuildTypedElementType` (122-151); the inner-qualifier extraction path.
- `docs/Working/bugs.md` — BUG-019 (residual gap 1), BUG-017/018/020 (same obligation-creation family).
- `docs/Working/relational-rules-and-bounds-plan-2026-06-02.md` — Slice 3 framing + consultation record.
- `samples/it-helpdesk-ticket.precept`, `samples/restaurant-waitlist.precept`, `samples/utility-outage-report.precept` — the three blocked corpus samples.
- Comparable systems (verbatim excerpts in § Language Design Grounding): JSON Schema `items` + `minItems`/`maxItems`; CUE `[...T]`; protovalidate `repeated.items` vs `repeated.min_items`; W3C XML Schema `simpleType` restriction facets.

---

## Pre-design consultation evidence (gate audit)

**Tier classification: Tier 2 → effectively heavier (a near-miss spec conflict that resolved as "different axis").** Surfaced 2026-06-02 this session, before any agent spawn or design write.

The canonical area checked is `collection-types.md`. Inner types there already carry *type-qualifiers* (`~string`, `money in 'USD'`, `quantity of 'length'`, `choice of T(...) ordered`) — metadata that restricts the element's denomination/comparison — but **no value modifiers** (`maxlength`, `min`/`max`, `notempty`, …). The Constraint Catalog (collection-types.md:748-772) lists only `notempty`/`mincount`/`maxcount`/`optional`/`default` as collection constraints, and the closing callout states:

> "**Element-level constraints:** `notempty`, `mincount`, and `maxcount` constrain the collection as a whole. To constrain individual elements (e.g., 'all items must be positive' or 'no items may be empty'), use a quantifier predicate." (collection-types.md:772)

That callout is about **rule-level** element predication (`rule each x in C (x > 0)`), not about an element value *type bound* that the proof engine can read at a read-site. The two are complementary: a quantifier is a runtime governance rule over current elements; an inner-type modifier is a static *type contract* the proof engine reads when an element flows out via `.peek`/`.first` into a capped field. The feature here is the second.

**The near-miss: the `capacity`-modifier rejection.** `collection-types.md:1424-1442` rejects a `capacity N` modifier:

> "**Rejected: Bounded Collection (`capacity` modifier).** … These two would be equivalent: `field Tags as set of string capacity 10` / `field Tags as set of string maxcount 10` … **Reject.** `maxcount N` already provides this capability. A `capacity` synonym adds language surface cost with zero capability gain. Precept's philosophy favors a small, precise surface (§0.4.1) — adding a second spelling for the same constraint violates that principle."

This was checked verbatim and is **a different axis**. `capacity` was rejected because it is a redundant *spelling for cardinality* (`maxcount` — how many elements the collection holds). The present proposal constrains *each element's value* (how long / how large / whether-empty each item is) — a capability `maxcount`, `mincount`, and `notempty` cannot express at all. The rejection's own bar — "small, precise surface (§0.4.1); reject zero-capability-gain synonyms" — is the bar this proposal must clear, and it clears it: there is no existing spelling for a per-element value bound. (JSON Schema, CUE, protovalidate, and XSD all treat element-value constraints and array-cardinality constraints as two separate axes — see § Language Design Grounding.)

**Owner alignment reached (this session):** the declared inner-type value-modifier shape (`queue of string maxlength 200`) was chosen over inferring element bounds from write-sites, and full scalar parity was chosen over length-only. Both are recorded below as Decision 1 and Decision 2 and are **not reopened** in this pass.

**Stakes:** pre-release, solo-dev, no external `.precept` authors yet. The two core decisions are therefore **high stakes** (they shape an author-visible type surface and the proof contract) but **not irreversible** — reversal would not break shipped external precepts. Falsifiers below are external-author-visible.

---

## Goal

A collection's scalar inner type may carry the same value-constraint modifiers it could carry as a field; those bounds govern every element at ingress, must be proven at write-sites, and are carried as the element's value interval at read-sites — so a value read out of a collection (`.peek`/`.first`/`.last`/`.min`/`.max`, or a quantifier binding) can be proven into a same-or-wider-bounded destination field. *(Testable: the three blocked samples compile clean; an over-bound write-site rejects; a read-site into a same-or-wider-bounded field discharges with no per-access guard for the bound.)*

---

## Scope

**In scope**

- Grammar: `CollectionInnerType` may carry a trailing value-modifier list, after any existing type-qualifier (e.g. `set of money in 'USD' nonnegative`, `queue of string maxlength 200`, `set of integer min 0 max 100`, `set of string notempty`).
- Full scalar parity: any value modifier legal on the inner scalar **type as a field** is legal on the inner type — `maxlength`/`minlength` (string), `min`/`max`/`nonnegative`/`positive`/`nonzero` (numeric), `notempty` (string), `maxplaces` (decimal), unit/dimension-qualified bounds on `money`/`quantity`/`price`.
- Per-element typing validation: `InvalidModifierForType` (PRE0033) fires per inner-type element when the modifier is illegal for that scalar type.
- Element governance at ingress (runtime): every element entering via `add`/`enqueue`/`push`/`insert`/`put`/`append`, and every element of a `default [...]` literal, governed against the inner-type bound; out-of-bound element ⇒ working copy discarded.
- Write-site proof obligation (compile-time): a mutation whose source value cannot be proven within the inner-type bound rejects (reusing scalar containment machinery — numeric interval / string-length interval — per element).
- Read-site reach: element-returning accessors and quantifier bindings carry the inner type's declared bound as the value interval.

**Out of scope (this design)**

- Two-axis element bounds on the *key/ordering* parameter (`queue of T by P`'s `P`, `lookup of K to V`'s `K`/`V`) — the same mechanism generalizes, but this design formalizes the primary inner type `T`. The grammar slot is reserved; the per-parameter rollout is a follow-on. *(Deferred, not rejected.)*
- New collection cardinality surface (`mincount`/`maxcount`/`notempty` on the collection itself are unchanged).
- Bag per-element count interactions beyond ingress governance (the multiset count axis is orthogonal).

**Deferred**

- Inner-type bound *inference* from the closed set of write-sites — explicitly the rejected alternative in Decision 1; recorded so it is not re-proposed.

---

## Philosophy Alignment

The 11 principles of `precept-language-spec.md §0.1`, every row filled.

| # | Principle | How this design upholds it |
|---|---|---|
| 1 | Prevention, not detection | Element governance at ingress (§Semantic Rule 2) discards the working copy on any out-of-bound element; the invalid collection-with-an-oversized-element never persists. Not a post-hoc element scan. |
| 2 | One file, complete rules | The element bound lives in the field's type declaration in the same `.precept` file. No external element validator, no parallel constraint list. |
| 3 | Deterministic semantics | The bound is a static literal (or a field-reference bound resolved like any modifier value). Same definition + same element = same accept/reject. No solver, no probabilistic element check. |
| 4 | Full inspectability | The element bound is part of the declared type and surfaces through hover/diagnostics exactly as a scalar field modifier does; a read-site proof carries the element bound as attribution (§Architecture — Tooling). The contrast with the *inferred* alternative (Decision 1) is precisely that an inferred bound would be non-inspectable. |
| 5 | Keyword-anchored readability | Reuses existing modifier keywords (`maxlength`, `min`, `max`, `notempty`, …) in a position that reads left-to-right: `queue of string maxlength 200`. No new symbol, no expression-first form. The dense compound wraps freely (Principle 5 multi-line allowance). |
| 6 | Explicit domain meaning over primitive convenience | An element with real domain identity keeps it: `set of money in 'USD' nonnegative` pins currency *and* sign on each element. The modifier composes with the existing type-qualifier, not in place of it. |
| 7 | Compile-time-first static checking | Write-sites prove-or-reject against the element bound (§Semantic Rule 3, §0.7 no deferral). A mutation whose source cannot be proven within the bound rejects with a message naming the missing source bound. |
| 8 | Approximation honesty | No approximation introduced. A `set of number min 0` carries `number`'s IEEE-754 approximation exactly as a `number` field does; the bound check on an exact `decimal`/`integer`/`string`/length is exact. The element bound does not present an approximate element as exact. |
| 9 | Mandatory rationale | The element modifier is constraint-modifier shorthand (§2.4) — it desugars to a per-element rule with a **generated** rationale (e.g. "each queue element must be at most 200 characters"), satisfying the mandatory-reason requirement without an authored `because`, identical to the scalar-field case. |
| 10 | Totality | The read-site reach (§Semantic Rule 4) is what makes a previously-unbounded element read bounded, closing the BUG-019 residual gap: a string read via `.peek` into a `maxlength` field now has a static length interval, so the length-containment obligation discharges instead of being unprovable. No new fault path is introduced. |
| 11 | Static completeness | Every element entering carries its bound (governance) and every fault-prone read of an element is proven against that carried bound (compile-time) — the §0.7 Composition pattern, applied per element. No element flows unbounded into a capped destination; no element-read fault class is left unlinked to a diagnostic. |

**Stateless-first-class + Domain-expert-primary-author companion.** The feature is lifecycle-agnostic: a stateless data-only precept (e.g. an address record with `field PhoneNumbers as set of string maxlength 20`) governs its element bounds exactly as a stateful one does — element governance runs at every ingress regardless of whether states exist. For the domain-expert author (`philosophy.md § Who authors a precept`, spec §0.8), the surface reads in domain vocabulary: "an agent queue of names, each at most 200 characters" is `queue of string maxlength 200` — the same `maxlength 200` they already write on a scalar field, now on the element type. No new concept to learn; the modifier they know simply attaches one position inward.

---

## Language Design Grounding

### The general field — per-element constraints are a recognized, distinct axis

Every mainstream schema/constraint/validation system that has both *array cardinality* and *element typing* keeps them as **two separate axes**: a constraint on how many elements, and a (sub-)constraint on what each element is. Precept already has the first axis (`mincount`/`maxcount`/`notempty`); it is missing the second. The convergence across four independent systems is the strongest precedent that the element-value axis is a real, non-redundant capability — not a second spelling of cardinality (which is exactly the distinction the `capacity`-modifier rejection turned on).

**JSON Schema** — the sharpest precedent, because it makes the cardinality-vs-element split explicit and canonical. Element constraints go in `items` (a sub-schema each element must satisfy); array length goes in `minItems`/`maxItems`:

> "For this kind of array, set the `items` keyword to a single schema that will be used to validate all of the items in the array."
> ```json
> { "type": "array", "items": { "type": "number" }}
> ```
> "The length of the array can be specified using the `minItems` and `maxItems` keywords. The value of each keyword must be a non-negative number."
> ```json
> { "type": "array", "minItems": 2, "maxItems": 3}
> ```
> — *json-schema.org, Understanding JSON Schema → Arrays (accessed 2026-06-02).*

An element sub-schema can itself carry `maxLength`/`minimum`/`maximum` — exactly Precept's element value modifiers. The mapping is direct: Precept `maxcount`/`mincount` ≙ JSON Schema `maxItems`/`minItems` (cardinality); Precept inner-type `maxlength`/`min`/`max` ≙ JSON Schema `items: { maxLength / minimum / maximum }` (element value).

**CUE** — list element constraints via `[...T]`, where `T` can be any constraint (a type unified with bounds):

> "CUE uses the `[...T]` syntax to constrain repeated elements in open lists." Examples: `[...int]` (all elements are ints); `[1, 2, 3, ...int]` (predefined elements then a type constraint).
> — *cuelang.org, Tour → Types → Lists (accessed 2026-06-02).*

In CUE the element constraint is the *unification of a type with bounds* (`[...int & >=0 & <=100]`, `[...string & strings.MaxRunes(200)]`). Precept's inner-type modifier list is the same idea expressed in keyword form: `set of integer min 0 max 100`, `queue of string maxlength 200`. CUE places the bound on the element type; Precept does too.

**protovalidate (Buf) / protoc-gen-validate** — `repeated` field rules split element constraints (`items`) from count constraints (`min_items`/`max_items`):

> "RepeatedRules describe the rules applied to repeated values." `min_items`: "value must contain at least N items"; `max_items`: count cap; `items`: "Specifies the constraints to be applied to each item in the field."
> ```proto
> message MyRepeated {
>   // value must contain at least 2 items
>   repeated string value = 1 [(buf.validate.field).repeated.min_items = 2];
> }
> ```
> — *protovalidate.com / buf.build, Repeated rules reference (accessed 2026-06-02).*

`items` carries element-level rules (e.g. an element `string.max_len`), strictly distinct from `min_items`/`max_items`. This is the same maxcount-vs-element-bound split Precept needs.

**W3C XML Schema** — the *element-type-carries-the-constraint* precedent: a `simpleType` restriction derives a constrained type by applying facets, and that derived type is then used as element/list-item content:

> "A datatype is said to be derived by restriction from another datatype when values for zero or more constraining facets are specified that serve to constrain its value space and/or its lexical space to a subset of those of its base type." A derived type "can then serve as the datatype for element content" — e.g. `<restriction base='string'><maxLength value='50'/></restriction>`.
> — *w3.org, XML Schema Part 2: Datatypes (accessed 2026-06-02).*

XSD shows the precedent that a *bounded scalar type* is a first-class thing that an aggregate's items take as their type — which is precisely what `string maxlength 200` becomes in inner-type position.

**Where Precept deliberately diverges.** In JSON Schema, protovalidate, and XSD the element constraint is a **validation annotation** — checked at a validation pass, bypassable by any path that does not run the validator. Precept's element bound is a **governed runtime contract plus a compile-time prove-or-reject obligation**: it is enforced on every ingress (governance, §0.7), and any write-site whose source cannot be *proven* within the bound is **rejected at compile time** (Principle 7/10/11, no deferral). The read-site reach (carrying the bound back out as a value interval) is also a divergence — none of the comparators feeds an element's declared bound back into a *downstream assignment's* static safety proof; Precept does, because totality (Principle 10) requires the element read be provable into a capped destination.

### Precept-internal grounding

- The inner type **already carries metadata** — `~string` (case-insensitivity), `money in 'USD'` (currency), `quantity of 'length'` (dimension), `choice of T(...) ordered` (ordering). The `TypedElementType` DU (`SemanticIndex.cs:349-372`) exists specifically so inner-type metadata is carried structurally. Adding value-bound metadata to the element type is symmetric with that established pattern, not a new kind of thing.
- Constraint modifiers are **rule shorthand** (spec §2.4): `field Qty as number min 5` ≙ `rule Qty >= 5`. An inner-type `min 5` is the per-element analogue — `set of integer min 5` ≙ "every element ≥ 5" — and participates in proof identically (spec §2.4: "participates in compile-time proof identically to the equivalent rule").
- The proof engine **already has the read hook**: `ProofEngine.Lengths.cs:MemberAccessStringLengthInterval` (217-223) is a deliberately-placed dead-but-sound stub that returns unbounded today and is documented as "the single sound place to read [element-type length] once they are [expressible]." This design makes that hook live.

---

## Audience and Teachability

### Worked example (plausible domain — IT helpdesk agent queue)

A support desk keeps a queue of available agents by name; agent names are capped at 200 characters (a system field-length limit). When a ticket needs assignment, the front agent is read into the assigned-agent field, which carries the same cap.

```precept
field AgentQueue as queue of string maxlength 200
field AssignedAgent as string optional maxlength 200
field LastQueuedAgent as string optional maxlength 200

event RegisterAgent(AgentName as string notempty maxlength 200)

from New, Assigned on RegisterAgent
    -> enqueue AgentQueue RegisterAgent.AgentName     # write-site: AgentName carries maxlength 200 ⇒ provable into the element bound
    -> no transition

from New on Assign when AgentQueue.count > 0
    -> set LastQueuedAgent = AgentQueue.peek          # read-site: .peek carries the element's maxlength 200 ⇒ discharges into LastQueuedAgent (maxlength 200)
    -> set AssignedAgent = AgentQueue.peek
    -> dequeue AgentQueue
    -> transition Assigned
```

Before this feature, `AgentQueue as queue of string` gave `.peek` an *unbounded* length, so `set LastQueuedAgent = AgentQueue.peek` could not be proven into the `maxlength 200` field — a `LengthBoundViolation` (the BUG-019 residual gap). With `queue of string maxlength 200`, the element read carries `(0, 200)` and discharges. *(This is the literal shape of `samples/it-helpdesk-ticket.precept`.)*

### A real PRE-coded error for a likely misuse

A misuse the author will hit is putting a string-only modifier on a numeric inner type (or vice versa). The existing per-type validation fires per element:

```
set of integer maxlength 5
        ^^^^^^^ ^^^^^^^^^
PRE0033 (InvalidModifierForType): 'maxlength' applies to string values, but the
elements of this set are integer. Use 'min'/'max' to bound an integer element,
or 'maxcount' to limit how many elements the set holds.
```

The message names the wrong axis explicitly (length vs numeric vs cardinality), steering the author to the right one — domain vocabulary, not compiler internals (spec §0.8).

### ≤10-minute teaching path

1. *(2 min)* You already write `field Name as string maxlength 200` — a value bound on a field.
2. *(2 min)* A collection wraps an inner type: `set of string`, `queue of string`. The inner type is "what each element is."
3. *(2 min)* Put the same bound on the inner type: `queue of string maxlength 200` — "each element is a string of at most 200 characters." Cardinality (`maxcount 10` — how many) is a separate, unchanged thing.
4. *(2 min)* Anything you put in (`enqueue`, `add`, `default [...]`) is checked against the element bound, just like a field edit.
5. *(2 min)* Anything you read out (`.peek`, `.first`, `each x in C`) carries that bound — so reading an element into a same-or-wider-capped field just works.

No new keywords; the only new idea is *position* (the modifier sits on the inner type).

---

## Semantic Rules

Notation: `T` is the inner scalar type; `m` an inner-type value modifier; `Γ ⊢ e : τ` is the type judgment; `⟦e⟧ⁱ` denotes the static value interval (numeric `IntervalOf` or string-length `StringLengthIntervalOf`) of `e`.

### Rule 1 — Typing (element-modifier legality)

A value modifier `m` is legal on inner type `T` **iff** `m` is legal on `T` as a field type, per the existing scalar modifier↔type compatibility table (spec §2.4 table, lines 1658-1687). No new compatibility axis is introduced; the table is reused unchanged with the element type substituted for the field type.

```
  modifierAppliesTo(m, T)                         (existing §2.4 table)
─────────────────────────────────────────────────────────────────────  [InnerMod-Ok]
  Γ ⊢ (collection of T  m …)  inner-type-wellformed

  ¬ modifierAppliesTo(m, T)
─────────────────────────────────────────────────  [InnerMod-Bad]
  emit PRE0033 InvalidModifierForType at the element-modifier span
```

Worked: `set of integer maxlength 5` → `modifierAppliesTo(maxlength, integer)` is false → PRE0033 per element. `queue of string maxlength 200` → true → well-formed. Modifier-*value* validation (`minlength > maxlength`, negative, duplicate) reuses the existing checks (spec table 1678-1687), now keyed on the element modifier set.

### Rule 2 — Element governance at ingress (runtime, §0.7 governance)

For every operation that introduces an element `v` into a collection field `F` whose inner type declares bound `m` — `add F v`, `enqueue F v`, `push F v`, `insert F v at N`, `put F k = v` (value axis), `append F v`, and each element `vᵢ` of a `default [v₁ … vₙ]` literal — `v` is governed against `m` at the moment it enters, **operation-blind**, on the working copy:

```
  enter(v, F)     bound(F.inner) = m     ¬ satisfies(v, m)
──────────────────────────────────────────────────────────  [Elem-Gov-Reject]
  working copy discarded  (§3A.4 — invalid configuration never persists)
```

This is prevention (Principle 1): the collection-containing-an-out-of-bound-element configuration is never committed, exactly as a scalar field edit that violates its modifier discards the working copy. Governance is independent of whether any downstream operation reads the element.

**The governed element-introducing action set is derived from the catalog, not hand-listed.** The set above is illustrative; the build derives it from `ActionMeta` (the actions whose effect grows the collection / establishes an element value), so value-introducing variants — including `enqueue … by` (`EnqueueBy`) and `append … by` (`AppendBy`), whose *value* axis introduces an element while their *key/ordering* axis is out of scope — are covered without a separately-maintained list. (Catalog-driven, per CLAUDE.md: derive, don't duplicate.)

### Rule 3 — Write-site proof obligation (compile-time, §0.7 no deferral)

Symmetric to the scalar containment obligation, lifted to the element. A mutation `op F src` introducing `src` into `F` (inner bound `m`) carries a per-element containment obligation; it discharges iff `src`'s static interval is provably within `m`:

```
  op F src     bound(F.inner) = m     ⟦src⟧ⁱ ⊑ band(m)
─────────────────────────────────────────────────────────  [Write-Discharge]
  obligation discharged

  op F src     bound(F.inner) = m     ⟦src⟧ⁱ ⋢ band(m)   (violating OR unprovable)
──────────────────────────────────────────────────────────────────────────────  [Write-Reject]
  emit LengthBoundViolation / OutOfRange (per modifier kind) — §0.7 no deferral
```

`⊑`/`band` reuse the scalar machinery: string-length via `TryLengthContainmentProof` (the element band is the inner `minlength`/`maxlength`), numeric via the interval-containment path (the element band is inner `min`/`max`/`nonnegative`). `enqueue Q with x` into `queue of string maxlength 200` discharges iff `x` carries `maxlength ≤ 200` — the §0.7 Composition pattern, per element. (The helpdesk sample's `enqueue AgentQueue RegisterAgent.AgentName` discharges because `AgentName` is declared `maxlength 200`.)

### Rule 4 — Read-site reach (the gap closer)

An element-returning read of `F` (inner bound `m`) carries `m` as the read value's static interval, so the read can be proven into a downstream destination. The reads are: accessors `.peek` (queue/stack/`queue of T by P`), `.first`/`.last`/`.at(N)` (log/list), `.min`/`.max` (orderable set); and a quantifier binding `each x in F` / `any x in F` / `no x in F`, where `x` carries `m`.

```
  Γ ⊢ F : collection of (T m)     read ∈ {.peek,.first,.last,.at(N),.min,.max}
──────────────────────────────────────────────────────────────────────────────  [Read-Reach]
  ⟦F.read⟧ⁱ  =  band(m)

  Γ ⊢ F : collection of (T m)     bind x in F
─────────────────────────────────────────────────  [Bind-Reach]
  ⟦x⟧ⁱ  =  band(m)     (x carries the element bound inside the predicate)
```

**Plug-in points (concrete).**

- *String-length:* `ProofEngine.Lengths.cs:MemberAccessStringLengthInterval` (currently returns `(0, null)` unbounded, lines 217-223) reads the receiver field's `TypedElementType` element-length bounds and returns `band(maxlength/minlength)` instead. The surrounding `StringLengthIntervalOf` member-access arm (lines 121-122) already routes element reads here. So `set F = C.peek` discharges via the *existing* `TryLengthContainmentProof` against `F`'s declared `maxlength`, with the element interval now non-unbounded.
- *Numeric:* `ProofEngine.Intervals.cs:IntervalOfNarrowed` gains a `TypedMemberAccess` arm (none today → element numeric reads are `Unbounded`) that, for an element-returning accessor on a field whose `TypedElementType` carries `min`/`max`/`nonnegative`, returns `NumericInterval[band(m)]`. This feeds the existing OutOfRange/assignment-range path unchanged.
- *Quantifier binding:* the binding variable's interval is seeded from `band(m)` in the predicate's narrowing context (spec §3.5 quantifier binding scope already gives `x` the inner type; this adds the inner *bound* to `x`'s interval).

### Rule 5 — Soundness preservation (Principles 7/10/11)

- **Principle 7 (compile-time-first).** Write-sites prove-or-reject (Rule 3); the engine never compiles an element write it cannot prove within the bound. No new "guess" path.
- **Principle 10 (totality).** The only behavioral change to existing programs is that a previously-*unprovable* element read becomes *provable* (Rule 4) — it can only turn an emitted obligation into a discharged one, never the reverse, because `band(m)` is always a *subset* of the unbounded interval the engine assumed before. An element read still never yields an undefined value: the bound narrows, it does not widen. No fault path is created.
- **Principle 11 (static completeness).** Composition holds per element: governance (Rule 2) makes the bound true of every element at ingress; the compiler (Rules 3, 4) proves every element write within the bound and every element read carries it. No element flows un-governed; no fault-prone element read is left unproven. The new modifier position introduces no element that escapes governance and no read that escapes proof.

**Soundness guard on Rule 4.** The read-reach is sound *only because* Rule 2 governs ingress: an element can carry `band(m)` on the way out precisely because no element violating `m` could ever have entered. If element governance did not run at ingress, Rule 4 would be unsound (it would claim a bound the element might not satisfy). The two rules are a matched pair — this is the §0.7 Composition contract instantiated at the element level.

---

## Architecture Grounding

### Layer placement

| Layer | Change |
|---|---|
| **Catalog** | No new modifier keywords (reuse `Modifiers`). The modifier↔type compatibility table (already catalog-driven) is consulted with the element type as subject — no parallel list. The grammar generator picks up the new inner-modifier position from `Constructs`/`Tokens` (no hand-edit of `tmLanguage.json`). |
| **Parser** | `ParseTypeRef`/collection-inner parsing accepts a trailing modifier list on `CollectionInnerType`, after the existing optional type-qualifier. Disambiguation: the modifier list starts with a known modifier keyword; the collection-level constraint list (`mincount`/`maxcount`/`notempty`/`default`) is parsed at the field-modifier position as today — binding is `(set of (T m…)) <collection-modifiers>`. |
| **Type checker** | `BuildTypedElementType` (`TypeChecker.cs:122-151`) extracts the element modifier set and per-element-validates it (Rule 1, PRE0033). The element bounds are stored on the element type (new carrier — see below). |
| **Proof engine** | Write-site obligation generation per element (Rule 3, reusing length/numeric containment); read-site reach (Rule 4) in `ProofEngine.Lengths.cs` (live the existing hook) and `ProofEngine.Intervals.cs` (new member-access arm). |

### Carrier shape (DU, not nullable-flattening)

Per CLAUDE.md ("use discriminated unions for varying shapes; don't paper over shape differences with nullable fields"), the element bound attaches to the `TypedElementType` DU. Two viable shapes — **resolved in Supporting Decision 3** below: extend the existing subtypes with a `DeclaredValueBounds` companion record (length/numeric/flag bounds), so `TypedScalarElement`/`TypedQualifiedElement`/`TypedChoiceElement` each optionally carry it, rather than introducing a fourth subtype that would fork the qualifier/choice axes. The bound carrier mirrors the `TypedField` length/numeric fields (`DeclaredMinLength`/`DeclaredMaxLength`/`DeclaredMin`/`DeclaredMax`, `SemanticIndex.cs:396-406`).

### Cross-component propagation

- **Runtime.** Element governance at ingress (Rule 2) is a new per-element constraint check in the mutation path (`add`/`enqueue`/`push`/`insert`/`put`/`append` and `default [...]` materialization). It reuses the scalar constraint-evaluation already run on field edits — the element bound desugars to a per-element rule (§2.4), evaluated against the working copy; failure discards the working copy (§3A.4). *Explicit: this is the one component that must enforce at runtime; everything else is compile-time.*
- **Tooling (language server).** Hover on the field shows the element bound as part of the type (`queue of string maxlength 200`); read-site proof attribution names the element bound as the interval source (Principle 4 inspectability). Semantic tokens treat the inner modifier identically to a field modifier (catalog-derived). *(`docs/tooling/language-server.md` touch.)*
- **MCP.** The compile-result projection and catalog formatters surface the element bound where they surface field modifiers; the type DTO for a collection field carries the element bound. Thin wrappers only — the logic stays in core (`CatalogFormatters.cs`, `CompileToolDtos.cs` verified for the field/modifier projection shape). *(`docs/tooling/mcp.md` touch.)*

### Breaking changes

None for existing precepts: a collection inner type **without** a modifier behaves exactly as today (unbounded element). The change is purely additive at the grammar and proof layers. The only behavioral delta is that a read of a *newly-bounded* element becomes provable where it was previously an emitted obligation — strictly fewer diagnostics, never more, on unchanged source.

### External architectural precedent (excerpt)

The "bounded scalar type as a first-class thing that an aggregate's items take as their type" is the XSD restriction model:

> "A datatype is said to be derived by restriction from another datatype when values for zero or more constraining facets are specified that serve to constrain its value space … These derived types can then serve as the datatype for element content."
> — *w3.org, XML Schema Part 2: Datatypes (accessed 2026-06-02).*

Precept's `TypedElementType`-carries-bounds is the same architecture: the element type is a constrained scalar, and the collection's items take it as their type.

---

## Inventory of what will be built

| Artifact | File(s) | Note |
|---|---|---|
| Grammar: inner-type modifier list | `src/Precept/Pipeline/Parser.Types.cs` (collection-inner parsing); spec §2.3 grammar (1099-1106) | `CollectionInnerType := ScalarType TypeQualifier? ValueModifier*` (and `ChoiceType ValueModifier*`) |
| Element bound carrier on the element type | `src/Precept/Pipeline/SemanticIndex.cs` (`TypedElementType` DU + `DeclaredValueBounds` companion) | DU extension, not nullable-flatten |
| Type-checker: per-element modifier validation | `src/Precept/Pipeline/TypeChecker.cs` (`BuildTypedElementType`); `TypeChecker.Validation.Modifiers.cs` | Rule 1 / PRE0033 per element; reuse §2.4 compatibility table + value checks |
| Proof: write-site obligation per element | `src/Precept/Pipeline/ProofEngine.*` (obligation generation at `add`/`enqueue`/`push`/`insert`/`put`/`append`/`default`) | Rule 3; reuse length + numeric containment |
| Proof: read-site reach (string-length) | `src/Precept/Pipeline/ProofEngine.Lengths.cs:MemberAccessStringLengthInterval` | make the dead hook (217-223) live |
| Proof: read-site reach (numeric) | `src/Precept/Pipeline/ProofEngine.Intervals.cs:IntervalOfNarrowed` | new `TypedMemberAccess` arm |
| Proof: read-site reach (quantifier binding) | quantifier narrowing context | seed `x`'s interval from `band(m)` |
| Runtime: element governance at ingress | runtime mutation path (`add`/`enqueue`/…/`default` materialization) | Rule 2; reuse scalar constraint evaluation |
| Diagnostics | reuse PRE0033 (InvalidModifierForType), PRE0135 (LengthBoundViolation), OutOfRange/PRE0079; no new code unless write-site element rejection needs a distinct code | confirm during build; if a new code is added → diagnostic-system.md |
| Test stubs | `test/Precept.Tests/` | see Acceptance criteria — typing-reject, write-reject, read-discharge, three-sample-clean, governance-runtime |
| Sample fixes | `samples/it-helpdesk-ticket.precept`, `samples/restaurant-waitlist.precept`, `samples/utility-outage-report.precept` | add the element `maxlength` that unblocks `.peek`/accessor reads |

---

## Decisions

### Decision 1 — Syntax: declared inner-type value modifier *(high stakes; owner-aligned; not reopened)*

The element bound is **declared** on the inner type (`queue of string maxlength 200`), not **inferred** from the set of write-sites.

- **Rationale.** An explicit declared bound is a governed, inspectable type contract — symmetric with how the inner type already carries type-qualifiers (`~string`, `money in 'USD'`, `ordered`). It is the value the proof engine reads at read-sites and the contract governance enforces at ingress; both need a single declared source of truth on the type.
- **Alternatives considered and rejected.** *(a) Infer the element bound from the closed set of write-sites* (take the join of all `enqueue`/`add` source bounds as the element bound). Rejected: the bound would be implicit and non-inspectable (violates Principle 4), and it carries a soundness side-condition — it is sound only if the write-set is provably closed (no host-injected element, no path that adds an unbounded value), which §0.7's "boundary of the guarantee" (host-injected/hydrated state is not re-proven) cannot guarantee. *(b) Length-only via a dedicated `elemmaxlength` keyword.* Rejected by Decision 2 (parity). *(c) A quantifier rule only* (`rule each x in C (x.length <= 200)`). Insufficient: a rule is runtime governance over current elements but does **not** carry a bound back to a read-site, so it cannot close the totality gap (a `.peek` would still be unbounded) — this is why the existing "use a quantifier" callout does not solve BUG-019.
- **Precedent.** JSON Schema `items` sub-schema, CUE `[...T]`, protovalidate `repeated.items`, XSD `simpleType` restriction — all express the element constraint **declaratively on the element type**, none infer it from usage sites (§Language Design Grounding, verbatim).
- **Tradeoff accepted.** The author must write the bound explicitly even when every write-site already carries it (mild redundancy at the declaration). Accepted because explicitness/inspectability is the whole point, and the write-site proof (Rule 3) then *confirms* the sources match the declared bound rather than silently deriving it.
- **Counter-evidence.** Some systems do flow element constraints from usage (type inference in CUE unifies usage with declaration). But CUE still *records* the unified constraint on the type; it does not leave it implicit-and-unreadable, so this is not true inference-without-declaration.
- **Reversibility.** Reversible pre-release — no external precept depends on it. Reversing to inference later would be a breaking proof-semantics change, but no shipped artifact breaks today.
- **Blast radius.** Grammar + type checker + proof read/write + runtime ingress + tooling/MCP projection. Additive; no existing-precept behavior change.

### Decision 2 — Scope: full scalar parity *(high stakes; owner-aligned; not reopened)*

The inner type may carry **any** value modifier its scalar type could carry as a field (`maxlength`/`minlength`, `min`/`max`, `nonnegative`/`positive`/`nonzero`, `notempty`, `maxplaces`, qualified bounds), not length-only.

- **Rationale.** Consistency and expressiveness: the inner type *is* a scalar type, and a scalar type's modifier set is already defined (§2.4 table). Restricting to length-only would create a second, smaller modifier vocabulary for the same types — a special case the author must learn and the catalog must encode separately.
- **Alternatives considered and rejected.** *Length-only.* Rejected: it solves the immediate BUG-019 string case but leaves the symmetric numeric case (`set of integer min 0 max 100` reading `.min`/`.max` into a bounded field) unaddressed, and it fragments the modifier model. The marginal cost of parity is low because the compatibility table and containment machinery already exist for all scalar modifiers.
- **Precedent.** JSON Schema item sub-schemas carry the full keyword set (`maxLength`, `minimum`, `maximum`, …); CUE element constraints are arbitrary type-unifications; protovalidate `items` carries the full per-type rule set. None of the comparators restricts element constraints to a sub-vocabulary of what the scalar type supports.
- **Tradeoff accepted.** Larger immediate surface than length-only — every scalar modifier must be validated/governed/proven in element position from day one (more test cells). Accepted because the machinery is shared and the alternative is a fragmented model.
- **Counter-evidence.** A minimal first cut (length-only) would ship the three blocked samples faster. But it would harden a length-only model that Decision 2 would then have to widen — re-work, and a temporary inconsistency in the modifier surface.
- **Reversibility.** Narrowing later (parity → length-only) would remove author-visible surface — higher reversal cost than Decision 1, but still pre-release and no external precept depends on numeric element bounds yet.
- **Blast radius.** The full §2.4 modifier set in element position; the per-element validation, governance, write-proof, and read-reach must each handle numeric, length, flag, and qualified bounds.

### Supporting Decision 3 — `notempty` in inner-type position means per-element non-empty, distinct from collection `notempty`/`mincount` *(medium stakes — surfaced by this pass)*

> **Retargeted by `modifier-name-axis-overlap-2026-06-03.md` (Locked 2026-06-03).** The collection-coexistence half of this decision is **reversed** under owner authorization (Direction A): `notempty` is now `StringOnly` — it has no collection meaning, so there is no longer a collection-level `notempty` to coexist with; "the collection has ≥1 element" is `mincount 1` only. The element half is **resolved**: per-element `notempty` is achieved by the retarget plus generic satisfaction-shape binding in `BuildElementValueBounds` (no per-modifier arm), not by positional overloading of a both-axes modifier. The text below is retained as the original framing; read it through that retarget.

`set of string notempty` means **each element is a non-empty string** (per-element `string` `notempty`), which is *different* from `set of string` with a collection-level `notempty`/`mincount 1` (the collection has ≥1 element).

- **Rationale.** `notempty` is overloaded by position: on a *string field* it means "the string has content"; on a *collection field* it means "the collection has ≥1 element" (spec §2.4: "String is non-empty; collection contains at least one element"). In inner-type position the subject is the element's scalar type, so `notempty` takes its **scalar** meaning (string content). The position disambiguates — exactly as `ordered` on an inner `choice` (valid) differs from `ordered` as a collection field modifier (invalid).
- **Both can coexist:** `field Tags as set of string notempty mincount 1` — the inner `notempty` means "no element is the empty string," the collection `mincount 1` means "at least one tag." They are different axes (element value vs cardinality), and the parser binding `(set of (string notempty)) mincount 1` keeps them lexically separate. *(This must be teachable: the worked-example teaching path step 3 already separates element bound from cardinality.)*
- **Alternatives considered and rejected.** *Reject inner `notempty` as ambiguous and force `minlength 1`.* Rejected: `minlength 1` is exactly what scalar `notempty` desugars to, and disallowing `notempty` in element position would make the inner modifier set a *proper subset* of the scalar set — contradicting Decision 2 (parity). The position already disambiguates.
- **Precedent.** JSON Schema expresses the same two axes separately — `items: { minLength: 1 }` (each element non-empty) vs `minItems: 1` (array non-empty). The two never collide because they live in different keywords; Precept's positional split is the same separation.
- **Tradeoff accepted.** `notempty` carries two meanings keyed by position — a minor teaching wrinkle. Accepted because it is the *same* overloading the language already ships for `notempty` (field-string vs field-collection) and for `ordered` (inner-choice vs field).
- **Counter-evidence.** Positional overloading of a keyword can confuse; but the alternative (forbidding `notempty` inward) breaks parity and the existing precedent is identical.
- **Reversibility / blast radius.** Reversible; localized to the `notempty` validation arm and one teaching-path sentence.

### Supporting Decision 4 — Field-reference element bounds inherit BUG-020's resolution, not this design's *(low stakes — boundary call)*

An inner-type modifier whose value is a *field reference* (`queue of string maxlength SomeField`) is governed by the same modifier-value binding/enforcement work as scalar field-reference bounds (BUG-020), not separately designed here.

- **Rationale.** The element modifier *is* constraint-modifier shorthand (§2.4); a field-reference value in element position has the identical "resolve, bind, enforce" obligation as `field X as number min Floor`. BUG-020 (field-reference modifier silently accepted, never bound) is the live tracking item for that; element position inherits whatever resolution BUG-020 lands.
- **Tradeoff accepted.** This design does not independently specify field-reference element bounds; if BUG-020's resolution lands first, element position composes with it; if not, literal element bounds ship first and field-reference element bounds follow. Accepted to avoid duplicating an in-flight decision.
- **Alternatives / Precedent / Reversibility.** Alternative: specify it here — rejected as duplicating BUG-020. Precedent: §2.4 already routes modifier-value scope identically for fields and (by the shorthand equivalence) elements. Reversible; no blast radius beyond deferring to BUG-020.

---

## Acceptance criteria (test-shaped)

1. **Three blocked samples compile clean.** `it-helpdesk-ticket`, `restaurant-waitlist`, `utility-outage-report` — after adding the element `maxlength` on the relevant queue/list inner type — `precept_compile` returns zero diagnostics, where today they emit `LengthBoundViolation` on the `.peek`/accessor read.
2. **Typing reject.** `field S as set of integer maxlength 5` emits PRE0033 (InvalidModifierForType) at the `maxlength` span; `field S as set of string min 0` likewise.
3. **Write-site over-bound rejects.** `field Q as queue of string maxlength 5` + `enqueue Q E.Name` where `E.Name` is `string` (unbounded) or `maxlength 10` emits `LengthBoundViolation` at the write-site (no deferral); the control `E.Name maxlength 5` compiles clean.
4. **Read-site discharges.** `field Q as queue of string maxlength 200` + `field Dst as string maxlength 200` + `when Q.count > 0 -> set Dst = Q.peek` compiles clean; narrowing `Dst` to `maxlength 100` re-emits `LengthBoundViolation` (the read carries `(0,200)`, not within `100`).
5. **Numeric parity read.** `field S as set of integer min 0 max 100` + `field Dst as integer min 0 max 100` + `when S.count > 0 -> set Dst = S.max` compiles clean; an over-narrow `Dst` re-emits OutOfRange.
6. **Quantifier binding carries the bound.** `field S as set of integer min 0 max 100` — inside `rule no x in S (x > 100)` the binding `x` carries `[0,100]`, and a proof depending on `x`'s upper bound discharges.
7. **Runtime governance.** An ingress of an out-of-bound element (e.g. `enqueue` a 250-char value into `maxlength 200` via an event arg that itself permits it — exercising the runtime path) is rejected at runtime (working copy discarded), with the element-bound rule's generated rationale surfaced. *(Runtime gate — exercised when the runtime path is testable.)*
8. **No regression.** A collection inner type with no modifier behaves exactly as before (element read unbounded); corpus stays green.

---

## Dependencies

- **BUG-020 resolution** (field-reference modifier binding) — only for *field-reference* element bounds (Supporting Decision 4). Literal element bounds (the three blocked samples) do **not** depend on BUG-020.
- The shared scalar containment machinery (`TryLengthContainmentProof`, numeric interval containment) — already shipped (BUG-019 fix landed 2026-06-02).
- The `TypedElementType` DU (`SemanticIndex.cs`) — already shipped (carries `~string`/qualified/choice metadata; this extends it).

---

## Doc-update enumeration (per CLAUDE.md routing)

| Doc | Update |
|---|---|
| `docs/language/precept-language-spec.md` | §2.3 grammar — restructure the **collection-level** production so the value-modifier list attaches to the inner type alongside the existing qualifier (today `TypeQualifier?` binds at the collection level, `set of CollectionInnerType TypeQualifier?`; the edit must reconcile both onto the inner type, e.g. `set of (ScalarType TypeQualifier? ValueModifier*)`, so `set of money in 'USD' nonnegative` binds both to the element); §2.4 note that modifiers may sit in inner-type position; the modifier↔type table note that the table is consulted per-element; remove the §0.1-area "residual gap: collection element-type length modifiers are not yet expressible" sentence (~line 249) once shipped. |
| `docs/language/collection-types.md` | Inner Type System — new "Element value modifiers" subsection; Constraint Catalog — distinguish element value bounds from collection cardinality; update the "Element-level constraints → use a quantifier" callout (772) to add the inner-type-modifier path; cross-reference the `capacity`-rejection (different axis). |
| `docs/compiler/proof-engine.md` | Strategy 9 member-access row (1702) — element-type length bounds now read (no longer "always unbounded"); the numeric interval member-access arm; write-site element obligation. |
| `docs/compiler/type-checker.md` | `BuildTypedElementType` element-modifier extraction + per-element PRE0033 validation; accessor read-reach typing. |
| `docs/compiler/diagnostic-system.md` | Only if a new write-site element-rejection code is introduced (else PRE0033/PRE0135/OutOfRange reused — confirm at build). |
| `docs/tooling/language-server.md` | Hover/semantic-token/attribution surfacing of the element bound. |
| `docs/tooling/mcp.md` | Type/modifier projection of the element bound (CatalogFormatters / CompileToolDtos). |
| `docs/Working/bugs.md` | Close BUG-019 residual gap 1; move BUG-019 to Fixed once the three samples are green. |

---

## Falsifiers (external-author-visible)

A claim is wrong if any of these is observed by an external author:

1. **A collection inner type carrying a value modifier the scalar type rejects compiles clean** (e.g. `set of integer maxlength 5` produces no PRE0033) — falsifies Rule 1 / Decision 2 parity.
2. **A `.peek`/`.first`/`.min` read of a bounded element into a same-bound field still emits `LengthBoundViolation`/OutOfRange** — falsifies the read-site reach (Rule 4), the feature's reason to exist.
3. **An over-bound write-site compiles clean** (e.g. `enqueue Q E.Name` with `E.Name` unbounded into `queue of string maxlength 5` produces no diagnostic) — falsifies Rule 3 / Principle 7 (deferral).
4. **An out-of-bound element persists at runtime** (an element longer than the inner `maxlength` survives an ingress and is observable on a later read) — falsifies Rule 2 / Principle 1 (governance).
5. ~~**`set of string notempty` is interpreted as collection `mincount 1`** (an empty string element is admitted while an empty collection is rejected) — falsifies Supporting Decision 3.~~ **Risk removed by `modifier-name-axis-overlap-2026-06-03.md` (Locked 2026-06-03):** `notempty` is now `StringOnly` with no collection meaning, so `set of string notempty` can *only* mean the per-element bind — there is no longer a collection-`notempty` reading to be confused with. Collection cardinality is `mincount 1`.

---

## Resolved at lock (deferred scope / build-confirm — no open question blocks the lock)

1. **Two-axis element bounds on `P`/`K`/`V`** (`queue of T by P`, `lookup of K to V`). The mechanism generalizes (each parameter is a scalar inner type), but this design formalizes only the primary `T`. **Resolved: deferred** — a follow-on design; the grammar slot is reserved and it is explicitly out of this design's Scope/acceptance. No sample needs it.
2. **Write-site rejection diagnostic code.** **Resolved: reuse** `LengthBoundViolation`/`OutOfRange` (the element write-site is a value-into-a-bound containment, the same shape as a scalar field assignment). A distinct element-specific code is not introduced; `diagnostic-system.md` is touched only if the build surfaces a concrete need for a more precise message (a build-confirm, not an open design choice).
3. **`default [...]` element governance vs proof.** **Resolved: folds into the existing default-against-rules check** (spec §0.6 item 11) — a `default` list literal's elements are literals, statically provable against the element bound at compile time (Rule 3 discharges trivially), with ingress governance (Rule 2) as the runtime backstop. Build verifies the element-literal check rides the existing default-check path; no new mechanism.

## Build sequencing & reuse principles (carried into Slice 3 planning)

- **String-length first.** Land the string-length read-reach (fills the dead `MemberAccessStringLengthInterval` hook) — unblocks all three samples immediately — then the numeric / quantifier / qualified half as a fast follow. De-risks the one watch-item (money/quantity normalization, the least-tested parity cell) into the second step where it cannot block the corpus.
- **Share the obligation generator, don't fork it.** The per-element write-site obligation construction reuses the scalar set-action requirement construction (`Actions.cs`), parameterized on bound-source (field bound vs element bound) — not a parallel generator. (Code-side of the catalog-reuse bar.)
- **Catalog + code reuse (verified at lock):** typing reuses `ValidateValueModifiers`/`Modifiers.ApplicableTo` with the element `TypeKind` as subject; provers reuse `TryLengthContainmentProof` + the numeric interval-containment path; the bound carries on the `TypedElementType` DU using `TypedField`'s bound vocabulary; diagnostics reuse PRE0033/PRE0135/OutOfRange. The genuinely-new code is parser-position + element-modifier extraction + three small read-reach plug-ins.

---

*Anything I was unsure about, surfaced for the owner:* (a) whether the read-site reach should also seed the numeric `IntervalOfNarrowed` member-access arm in *this* slice or defer the numeric half — I designed for full parity (Decision 2), but if the owner wants the smallest gap-closing cut, the string-length half alone unblocks all three samples and the numeric half could follow; (b) whether Supporting Decision 4 (field-reference element bounds → BUG-020) should instead be folded in here rather than deferred — I deferred to avoid duplicating an in-flight decision, but the owner may prefer to specify it together.

**Build watch-item (from adversarial review — non-blocking).** The least-tested cell of the full-parity surface is a **`money`/`quantity` element bound read via `.min`/`.max` into a qualified destination**: the numeric read-reach feeds `IntervalOfNarrowed`, which carries non-trivial currency/unit normalization (incl. a "dynamic denominator → Unbounded" path, `Intervals.cs:44-61`). The three blocked samples are string-only and won't exercise it. Watch: if Acceptance criterion 5 (numeric parity read) or a `money`/`quantity` element-read test surfaces an interval that doesn't normalize against the destination's unit/currency, that is the signal to take the **string-length-first split** (unblock the three samples with the length half, land the numeric/qualified half as a follow-on) rather than force the qualified cell. Aligns with Falsifier 2.
