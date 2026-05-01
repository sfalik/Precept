# Collection Iteration Research — Frank

**Date:** 2026-04-29
**Status:** Research complete, awaiting Shane's direction on whether to open a formal proposal

---

## Summary

Shane asked: what would basic loops over collections look like in Precept, the way CEL does it?

After reviewing CEL's macro system, the Precept spec (§0.4 "No loops", §0.1 Design Principles, expression grammar), philosophy.md, existing collection operations across samples, and comparable DSLs (OPA/Rego, SQL), my recommendation is:

**Quantifier predicates (`all`/`any`/`none`) — yes, they belong in Precept and are the natural next step.** They are not loops. They are bounded, deterministic, total predicates over declared collection fields. They strengthen constraint expressiveness without violating any execution model property.

**Projection/transformation (`map`/`filter`/`reduce`/`sum`) — not yet.** These produce new collections or scalar aggregates, which opens questions about intermediate types, proof obligations, and whether Precept needs computed-collection fields. The pressure isn't there yet.

---

## 1. How CEL Does It

CEL provides five iteration macros that expand to comprehension AST nodes at parse time — they are not runtime constructs:

| Macro | Semantics | Expansion |
|-------|-----------|-----------|
| `list.all(x, predicate)` | Universal: every element satisfies the predicate | Short-circuit false on first failure |
| `list.exists(x, predicate)` | Existential: at least one element satisfies | Short-circuit true on first match |
| `list.exists_one(x, predicate)` | Uniqueness: exactly one element satisfies | Must scan all |
| `list.filter(x, predicate)` | Selection: returns subset where predicate holds | Produces new list |
| `list.map(x, expression)` | Projection: transforms each element | Produces new list |

**Key design rationale from CEL:**
- **Macros, not functions.** CEL is deliberately non-Turing-complete. Macros expand at parse time into a primitive comprehension construct that the evaluator walks. No user-defined macros, no recursion, no unbounded computation.
- **No general iteration.** CEL does not have `for`, `while`, or `reduce`. The five macros cover the use cases that policy/constraint evaluation needs. The language designers judged that general iteration would undermine static analyzability and bounded evaluation guarantees.
- **The variable binding is scoped.** `x` in `list.all(x, x > 0)` is a macro-local binding, not a mutable variable. It's closer to a mathematical quantifier than a loop variable.

**What CEL excludes and why:**
- No `reduce`/`fold` — would require an accumulator, introducing sequential state.
- No nested comprehensions — keeps evaluation flat and predictable.
- No user-defined macros — prevents complexity accretion toward a general-purpose language.

---

## 2. The Pressure on Precept

Looking at the samples and the current collection surface, Precept today has:

**Collection types:** `set of T`, `queue of T`, `stack of T`
**Collection accessors:** `.count`, `.min`, `.max`, `.peek`
**Collection predicates:** `contains` (single-element membership)
**Collection mutations:** `add`, `remove`, `enqueue`, `dequeue`, `clear`

What's **missing** — real problems that samples already brush up against:

### A. Validating all elements of a collection

The InsuranceClaim sample has `MissingDocuments as set of string`. Today there's no way to express:

```
rule MissingDocuments all notempty because "Missing document names cannot be blank"
```

The constraint can only check `.count` or `contains` — it cannot assert a property about every member. The `notempty` inner-type modifier on `set of ~string` (where `~` marks the inner type) handles this one case at declaration time, but general element-level predicates are impossible.

### B. Computing derived values from collections

BuildingAccessBadgeRequest works around this by maintaining separate `LowestRequestedFloor` and `HighestRequestedFloor` fields that are manually synced via `set` actions. The `.min` and `.max` accessors exist, but there's no `sum` — and no way to express "total cost = sum of line item amounts" for a collection of structured values (which Precept doesn't yet have, but would need for collection iteration to matter most).

### C. Guards involving element-level properties

HiringPipeline uses `PendingInterviewers contains RecordInterviewFeedback.Interviewer` — which works because it's single-element membership. But a guard like "approve only if all interviewers have submitted feedback" requires the pattern `PendingInterviewers.count == 0`, which is a workaround. The intent is "no pending interviewers remain," not "the count is zero."

### D. The gap hierarchy

The pressure is ordered:
1. **Quantifier predicates over elements** — immediate, real, visible in current samples
2. **Aggregate functions over elements** (sum, average) — real but depends on collection-of-structured-values, which doesn't exist yet
3. **Projection/filtering** (map, filter) — only needed if you need to derive new collections, which is a much larger language question

---

## 3. Design Options

### Option A: Quantifier Keywords (Recommended)

**Approach:** Add `all`, `any`, `none` as bounded quantifier predicates on collection fields, using an element-binding syntax.

```precept
# Universal: every element satisfies
rule Items.all(item, item > 0)
    because "All line items must have positive amounts"

# Existential: at least one satisfies
from Submitted on Approve when Reviewers.any(r, r == Approve.ReviewerName)
    -> ...

# Negated universal: no element satisfies
rule Participants.none(p, p == "")
    because "Participant names cannot be blank"
```

**Grammar extension:**
```
CollectionQuantifier  :=  CollectionExpr "." ("all" | "any" | "none") "(" Identifier "," BoolExpr ")"
```

This produces a `QuantifierExpression` AST node — **not** a macro expansion. The element variable `item` is a scoped binding visible only inside the predicate.

**Philosophy filter assessment:**
- ✅ **Prevention:** Enables constraints that were previously inexpressible — strictly strengthens the governance surface.
- ✅ **Determinism:** Bounded iteration over a finite, declared collection field. Same data, same result. Always terminates.
- ✅ **Inspectability:** The engine can report which element(s) failed the predicate, with index/value attribution.
- ✅ **Keyword-anchored:** `all`, `any`, `none` are self-describing quantifier keywords. A domain expert reads `Items.all(item, item > 0)` as "all items are positive."
- ✅ **AI legibility:** Regular syntax, no nesting, no lambda abstraction. Pattern-matchable by any agent.
- ✅ **No loops introduced:** These are predicates, not iteration constructs. They produce a boolean, not a side effect. The AST is a quantifier node with a bound variable and a body expression — structurally identical to how the proof engine would reason about universals over finite sets.
- ✅ **Totality:** Empty collection + `all` → true (vacuous truth), `any` → false, `none` → true. No undefined behavior.
- ⚠️ **Proof engine impact:** The proof engine must reason about quantified expressions. For `all(item, item > 0)`, the inner-type constraints (e.g., `nonnegative` on a `set of number nonnegative`) may already prove the predicate — the proof engine should recognize this and not require redundant constraints.

**Main tradeoff:** Introduces a scoped variable binding (`item` in `all(item, predicate)`), which is new for Precept. Today the only variable-like references are field names and event argument dotted names. This is a modest complexity addition, but it's the minimum viable binding for quantification.

### Option B: Keyword-Prefix Quantifiers (No Binding Variable)

**Approach:** Use `all`/`any`/`none` as prefix keywords with a collection field and a predicate over the implicit "current element," referenced via a reserved word like `each` or `it`.

```precept
rule all MissingDocuments each notempty
    because "Missing document names cannot be blank"

rule any Items each > 100
    because "At least one item must exceed $100"

from UnderReview on Approve when none PendingInterviewers each == ""
    -> ...
```

**Philosophy filter assessment:**
- ✅ **Keyword-anchored:** `all`, `any`, `none` lead the statement.
- ✅ **Simpler binding:** `each` is a fixed reference — no user-chosen variable name.
- ❌ **Reads awkwardly:** `all MissingDocuments each notempty` doesn't read like natural English. The `each` keyword is a strange positional artifact.
- ❌ **Limits predicate complexity:** `each > 100` works for trivial comparisons, but `each.amount > 100 and each.status != "cancelled"` gets clumsy without a binding name.
- ❌ **Collision risk:** `all` is already `modify all` / `omit all` in state-scoped access mode declarations. Using it as an expression-level keyword requires disambiguation.

**Main tradeoff:** Simpler syntax at the cost of natural reading and extensibility. If Precept ever needs compound predicates over elements, this design paints you into a corner.

### Option C: Method-Style Aggregate Functions (Sum/Count/Any/All)

**Approach:** Extend the collection member access surface with aggregate methods that take predicate arguments.

```precept
# Aggregate boolean
rule Items.all(> 0) because "All items must be positive"

# Aggregate numeric
field Total as decimal -> Items.sum(.amount)

# Conditional count
rule Items.count(> 100) >= 3 because "At least 3 items must exceed $100"
```

**Philosophy filter assessment:**
- ✅ **Familiar:** LINQ-adjacent. Developers recognize the pattern.
- ❌ **Implicit element reference:** `.amount` and `> 0` have no explicit subject. What does `Items.all(> 0)` compare to? The element? Which property of the element?
- ❌ **Mixed semantics:** Some methods return boolean (`all`, `any`), some return numbers (`sum`, `count`), some return collections (`filter`). The member-access surface becomes overloaded.
- ❌ **Requires structured collection elements:** `Items.sum(.amount)` only works if `Items` is a collection of records with an `amount` field. Precept has `set of string`, `set of number` — not `set of {amount: decimal, status: string}`.
- ❌ **AI legibility drops:** The implicit subject makes pattern-matching harder for agents.

**Main tradeoff:** Looks familiar to developers but trades explicit readability for conciseness. Depends on features (structured collection elements) that don't exist yet.

---

## 4. My Recommendation

**Option A — quantifier keywords with explicit element binding** — is the right design.

Rationale:
1. **It's not a loop.** It's a predicate. The execution model property §0.4.1 says "no iteration constructs." A quantifier is a logical operation over a finite set — it produces a boolean, not a sequence. The distinction matters: `all(item, item > 0)` is no more a "loop" than `.count > 0` is. The compiler doesn't need fixpoint computation, widening, or any lattice infrastructure. It walks a finite declared collection and evaluates a pure predicate.

2. **The keywords are already reserved.** `All` and `Any` are in the lexer token table. `All` is currently used for `modify all` / `omit all`. `Any` is currently used for `in any` / `from any`. Both have available expression-level slots — the parser can disambiguate by position (expression context vs. declaration keyword).

3. **It fills the real gap.** The samples show the pressure clearly. Collection-level membership (`contains`) and aggregate properties (`.count`, `.min`, `.max`) don't cover element-level predicate constraints. Quantifiers close this gap.

4. **The proof engine benefits.** Universal quantifiers over collections with declared inner-type constraints are statically analyzable. If the field declares `set of number nonnegative`, then `Items.all(item, item >= 0)` is provably true from the type — the proof engine can fold it. This is exactly the kind of reasoning the proof system is designed for.

5. **It's the minimum viable step.** Quantifiers don't require new collection types, new field kinds, or computed collections. They work on the existing `set of T`, `queue of T`, `stack of T` surface with existing inner types.

**What I would NOT do right now:**
- No `map`, `filter`, or `reduce`. These produce values (collections or scalars), not predicates. They open type questions (what is the type of `Items.filter(...)`) and intermediate-value questions that are a separate design space.
- No `sum` or aggregate projection. This requires structured collection elements (collections of records), which is a much larger language feature. When that lands, aggregate functions should be designed alongside it.
- No nested quantifiers. `Items.all(item, item.parts.any(part, part > 0))` is tempting but introduces nested scoping that complicates the proof engine and readability. Start with flat quantifiers; extend later if the pressure appears.

---

## 5. Open Questions for Shane

1. **Is the §0.4.1 "No loops" property a hard constraint or a principle that admits bounded quantifiers?** I believe quantifiers are not loops — they're predicates over finite sets. But this is an identity question. If Shane reads §0.4.1 as excluding ALL element-level iteration including bounded quantifiers, that changes the design space.

2. **Should `none` be a keyword or should it be expressed as `not ... any`?** Adding `none` is a convenience — `not Items.any(item, item == "")` works equivalently. Three keywords is cleaner for readability; two plus negation is simpler for the grammar. I lean toward three.

3. **Element binding variable: user-named or fixed?** Option A uses `item` as a user-chosen name. An alternative is a fixed keyword like `it` or `each`. User-named is more flexible (especially if nested quantifiers ever land), but fixed is simpler and more keyword-anchored.

4. **What priority does this have relative to the proof engine and graph analyzer?** Quantifiers require proof-engine support for element-level reasoning. If the proof engine is next, this feature could be designed alongside it. If the proof engine is distant, quantifiers land as runtime-only evaluation (still useful, but without compile-time proof folding).

5. **Does this interact with the eventual structured-collection-element design?** If Precept ever supports `list of {name: string, amount: decimal}`, quantifiers over structured elements (`Items.all(item, item.amount > 0)`) need member access inside the binding. This is natural — the binding variable has the inner element type, and dotted access works normally — but it should be considered in the design.

---

## Precedent Summary

| Language/DSL | Iteration Surface | Design Choice | Alignment with Precept |
|-------------|-------------------|---------------|----------------------|
| CEL | `all`, `exists`, `exists_one`, `filter`, `map` as parse-time macros | Non-Turing-complete; macros not functions; no user-defined macros | High — same constraint-evaluation context, same bounded-evaluation philosophy |
| OPA/Rego | `some x in xs { pred }`, `all x in xs { pred }`, set comprehensions | Logic-programming with unification; more expressive but less readable for non-developers | Medium — expressiveness model is right, syntax model is wrong for Precept's audience |
| SQL | `EXISTS (subquery)`, `value op ANY (subquery)`, `value op ALL (subquery)` | Subquery-based; relational not collection-oriented | Low — wrong paradigm, but `ALL`/`ANY` as keywords validates the quantifier concept |

---

# Collection-Level Constraint Rules Research — Frank

**Date:** 2026-04-29
**Status:** Research complete, awaiting Shane's direction on which categories to advance to formal proposal
**Parallel track:** This complements `frank-loop-research.md` (quantifier predicates / iteration). This report focuses on constraint/validation rules, not computation or iteration.

---

## Summary

Shane asked: what collection-level rules could Precept support to express business requirements?

After surveying 7 external systems, building a taxonomy, and evaluating against Precept's philosophy and existing surface, my recommendation is:

**Precept already has a strong foundation.** The existing `mincount`/`maxcount`, `contains`, and `.count`/`.min`/`.max` cover cardinality and basic membership. The highest-value next step is extending the **field-constraint vocabulary** — adding `unique`, collection-level `notempty`, and cross-collection modifiers — before reaching for iteration-based rules. This covers ~70% of real-world collection constraints with zero new language constructs.

The remaining ~30% — element-shape predicates ("all items satisfy X"), aggregate-relational rules ("sum of items > threshold"), and ordering rules — require the quantifier predicates from the parallel loop research track. Those should land second.

---

## 1. Reference Survey

### FluentAssertions — Collection Assertion Vocabulary

| Category | Methods |
|----------|---------|
| Cardinality | `HaveCount(n)`, `HaveCountGreaterThan(n)`, `HaveCountLessThan(n)`, `BeEmpty()`, `NotBeEmpty()`, `ContainSingle()` |
| Membership | `Contain(item)`, `NotContain(item)`, `ContainInOrder(...)` |
| Uniqueness | `OnlyHaveUniqueItems()` |
| Element-shape | `OnlyContain(predicate)`, `AllSatisfy(assertion)`, `SatisfyRespectively(...)`, `AllBeOfType<T>()` |
| Ordering | `BeInAscendingOrder()`, `BeInDescendingOrder()` |
| Cross-collection | `BeSubsetOf(...)`, `BeSupersetOf(...)`, `IntersectWith(...)`, `BeEquivalentTo(...)`, `Equal(...)` |

**Mental model:** Fluent chaining on the collection as a whole. Each assertion is a named operation. Predicates are lambdas passed as arguments. The vocabulary is exhaustive — FA covers essentially every collection property a test might assert.

### Zod — Array Schema Validation

| Category | Methods |
|----------|---------|
| Cardinality | `.min(n)`, `.max(n)`, `.length(n)`, `.nonempty()` |
| Element-shape | Element schema (passed to `z.array(schema)`) |
| Custom | `.refine(predicate)` — escape hatch for uniqueness, cross-element checks, etc. |

**Mental model:** Schema composition. The element schema validates each item; the array methods validate the container. Anything beyond cardinality + element schema requires `.refine()` — an explicit escape hatch that acknowledges "the type system doesn't model this."

### Valibot — Array Validation Pipes

| Category | Methods |
|----------|---------|
| Cardinality | `minLength(n)`, `maxLength(n)` |
| Element-shape | `every(pipe)`, `some(pipe)` |
| Membership | `includes(value)`, `excludes(value)` |

**Mental model:** Pipe composition. Pipes are validation steps applied in sequence. `every` and `some` are explicit quantifiers. Cleaner than Zod's `.refine()` escape hatch — Valibot gives quantifiers first-class names.

### FluentValidation — Collection Validators

| Category | Methods |
|----------|---------|
| Element-level | `RuleForEach(x => x.Collection).SetValidator(...)`, `ChildRules(...)` |
| Collection-level | `RuleFor(x => x.Collection).Must(predicate)` |
| Cardinality | Via `Must(c => c.Count >= n)` |
| Uniqueness | Via `Must(c => c.Distinct().Count() == c.Count)` |

**Mental model:** Two distinct surfaces — element-level (`RuleForEach`) and collection-level (`RuleFor` + `Must`). FluentValidation doesn't have named collection assertions; everything beyond element validation goes through the generic `Must(predicate)` escape hatch. The vocabulary is thin on collection-specific operations.

### OPA/Rego — Collection Rules in Policy

| Category | Constructs |
|----------|------------|
| Element-shape | `every x in xs { predicate }`, `some x in xs` |
| Cardinality | `count(xs)` |
| Derived sets | Array/set comprehensions: `[x | x := arr[_]; x > 5]` |
| Cross-collection | Set operations: `&` (intersection), `|` (union), `-` (difference) |

**Mental model:** Logic programming. Rules are declarative statements. Comprehensions build derived collections. `every` and `some` are first-class quantifiers. Cross-collection operations use set algebra. The model is powerful but requires logic-programming literacy.

### Bean Validation (JSR-380) — Collection Annotations

| Category | Annotations |
|----------|-------------|
| Cardinality | `@Size(min=n, max=m)`, `@NotEmpty` |
| Element-cascading | `@Valid` (cascades validation to each element) |
| Nullability | `@NotNull` |

**Mental model:** Declarative annotations on fields. Collection constraints are limited to size and emptiness. Element-level validation is delegated via `@Valid` cascading to the element type's own annotations. No uniqueness, no ordering, no cross-collection — those require custom validators.

### SQL Constraints

| Category | Mechanisms |
|----------|------------|
| Cardinality | No direct collection cardinality (tables are collections; row counts are queries) |
| Uniqueness | `UNIQUE` constraint, `PRIMARY KEY` |
| Membership | `FOREIGN KEY` (value must exist in referenced table) |
| Element-shape | `CHECK` constraint on individual rows |
| Cross-table | Foreign keys, `CHECK` with subqueries (limited support) |

**Mental model:** Relational. Each row is independently constrained. Cross-row constraints use `UNIQUE`, `FOREIGN KEY`, or table-level `CHECK`. SQL's collection model is fundamentally different (tables are open collections, not declared fields), but `UNIQUE` as a declarative field-level keyword is directly relevant to Precept.

---

## 2. Taxonomy of Collection Rules

### Category 1: Cardinality
Rules about how many elements a collection has.

| Rule | Example systems |
|------|----------------|
| Non-empty | FA, Zod, Valibot, Bean Val |
| Exact count | FA |
| Min count | FA, Zod, Valibot, Bean Val |
| Max count | FA, Zod, Valibot, Bean Val |
| Single element | FA |

**Precept today:** `mincount N`, `maxcount N`, `.count` in expressions. **Gap:** No dedicated `notempty` for collections (must use `mincount 1`).

### Category 2: Membership
Rules about specific values being present or absent.

| Rule | Example systems |
|------|----------------|
| Contains value | FA, Valibot |
| Not contains value | FA, Valibot |
| Contains all of (subset) | FA |

**Precept today:** `contains` operator in expressions/guards. **Gap:** No `not contains` (must negate: `not X contains Y`). No multi-value containment.

### Category 3: Element-Shape (Quantified Predicates)
Rules about properties that all, some, or no elements satisfy.

| Rule | Example systems |
|------|----------------|
| All satisfy predicate | FA, Valibot, OPA, CEL |
| Any satisfies predicate | Valibot, OPA, CEL |
| None satisfies predicate | OPA (via negated `some`) |
| Exactly one satisfies | FA, CEL |
| Each element satisfies (cascading) | FV, Bean Val |

**Precept today:** Inner-type constraints (`set of number nonnegative`) cover the declaration-time case. **Gap:** No runtime predicate quantification. This is the domain of the parallel loop research — quantifier predicates (`all`/`any`/`none`).

### Category 4: Ordering
Rules about element arrangement.

| Rule | Example systems |
|------|----------------|
| Ascending order | FA |
| Descending order | FA |
| Monotone (no duplicates, ordered) | (implicit in ascending + unique) |

**Precept today:** Not applicable — `set` is unordered by definition; `queue` and `stack` have insertion-order semantics, not sort-order semantics. **Analysis:** Ordering constraints are only meaningful for ordered collection types (lists). Precept doesn't have a `list` type. If one is added, ordering constraints become relevant.

### Category 5: Cross-Collection
Rules relating two or more collections to each other.

| Rule | Example systems |
|------|----------------|
| Disjoint (no overlap) | OPA (set difference) |
| Subset | FA, OPA |
| Superset | FA |
| Intersection non-empty | FA |
| Same length | (custom in all) |
| Equivalence (same elements) | FA |

**Precept today:** No cross-collection operations. **Gap:** This is a real gap for business rules like "approved reviewers must be a subset of assigned reviewers" or "primary and backup contacts must be disjoint."

### Category 6: Aggregate-Relational
Rules relating a scalar aggregate of a collection to a threshold or another field.

| Rule | Example systems |
|------|----------------|
| Sum comparison | SQL (aggregates), custom in FV/FA |
| Min/max comparison | FA (via `BeInAscendingOrder`), SQL |
| Average comparison | SQL, custom in FV |
| Count-where comparison | OPA (comprehension + count) |

**Precept today:** `.min` and `.max` accessors on `set of T` (T orderable). `.count` on all collections. **Gap:** No `.sum`, no `.average`, no count-where. Sum is the most requested — "invoice line items must sum to the total" is a canonical business rule. But this depends on either (a) numeric collections with `.sum` accessor, or (b) structured collection elements with projection, which is a larger feature.

---

## 3. Business Requirements Mapping

### Cardinality
- "An order must have at least one line item" → `mincount 1` (exists today)
- "A review committee must have exactly 3 members" → `mincount 3 maxcount 3` (exists today)

### Membership
- "The list of approved currencies must include USD" → `rule ApprovedCurrencies contains "USD"` (exists today)
- "Blacklisted suppliers must not appear in the vendor list" → needs cross-collection, not today

### Element-Shape
- "All approvers must have distinct roles" → needs `unique` (proposed) + structured elements (future)
- "Every line item amount must be positive" → inner-type constraint `set of number positive` (exists at declaration), or quantifier `Items.all(item, item > 0)` (from loop research)

### Ordering
- "Milestone dates must be in chronological order" → needs ordered collection type + ordering constraint (future)

### Cross-Collection
- "Primary and backup contacts must be different people" → needs `disjoint` operator
- "Selected options must be a subset of available options" → needs `subset` operator

### Aggregate-Relational
- "Invoice line items must sum to the invoice total" → needs `.sum` accessor on numeric collections
- "Average review score must be at least 3.0" → needs `.sum` / `.count` or `.average` accessor

---

## 4. Precept Syntax Options

### Option A: Constraint-Keyword Extensions (Recommended First Step)

**Approach:** Extend the existing field-constraint vocabulary with new keywords that express collection-level properties declaratively, exactly as `mincount`/`maxcount`/`nonnegative`/`notempty` work for their respective types today.

```precept
# Uniqueness — elements must be distinct
field Approvers as set of string unique

# Collection notempty — at least one element required (alias for mincount 1)
field LineItems as set of number notempty

# Cross-collection subset
field SelectedOptions as set of string subset AvailableOptions

# Cross-collection disjoint
field PrimaryContacts as set of string disjoint BackupContacts
```

**What's new:** `unique`, collection-level `notempty`, `subset`, `disjoint` as field modifiers.

**Philosophy filter:**
- ✅ **Prevention:** Declarative, structural. The constraint is part of the field's type — every mutation is checked.
- ✅ **Determinism:** All new modifiers are decidable over finite sets.
- ✅ **Inspectability:** Each modifier is a named property the engine can report on.
- ✅ **Keyword-anchored:** Same position as existing modifiers. No new syntax forms.
- ✅ **AI legibility:** Flat, keyword-per-constraint. Trivially parseable.
- ✅ **No loops:** These are structural properties, not predicates over elements.
- ✅ **Mandatory rationale:** Constraint modifiers don't carry `because` — they are type-level facts. Consistent with existing `nonnegative`, `positive`, etc.
- ✅ **Proof engine:** `unique` on `set` is tautological (sets are inherently unique) — the compiler can warn. `unique` on `queue`/`stack` is meaningful. `subset`/`disjoint` are statically checkable when both collections are known.

**Covers from taxonomy:** Cardinality (complete), Membership (partial — `contains` exists), Cross-Collection (subset, disjoint), Uniqueness.

**Deliberately excludes:** Element-shape predicates (needs quantifiers), Ordering (needs ordered type), Aggregate-relational (needs `.sum`).

---

### Option B: Rule-Surface Quantifiers (Recommended Second Step)

**Approach:** Use quantifier predicates in `rule` and `ensure` expressions. This is the same design as Option A from the parallel loop research — included here for completeness of the constraint picture.

```precept
# All elements satisfy a predicate
rule Items.all(item, item > 0)
    because "All line items must have positive amounts"

# At least one element satisfies
rule Reviewers.any(reviewer, reviewer != "")
    because "At least one reviewer must be named"

# State-scoped element constraint
in Review ensure Scores.all(score, score >= 1 and score <= 5)
    because "All review scores must be between 1 and 5"

# Guard with quantifier
from Submitted on Approve when PendingApprovals.all(p, p != "")
    -> transition Approved
```

**Philosophy filter:** See the parallel loop research (`frank-loop-research.md`) for full assessment. Summary: ✅ on all principles. The key tradeoff is introducing a scoped variable binding, which is new for Precept but is the minimum viable form for quantification.

**Covers from taxonomy:** Element-Shape (complete), and enables some Aggregate-Relational rules when combined with collection accessors.

**Deliberately excludes:** Cross-collection (use Option A's keyword modifiers), Ordering (needs ordered type), Projection/computation (separate track).

---

### Option C: Dedicated `check` Blocks (Deferred)

**Approach:** A new construct that groups multiple collection-level assertions under a single collection reference, with a dedicated keyword.

```precept
check LineItems
    notempty because "Must have at least one line item"
    unique because "No duplicate line items"
    all(item, item > 0) because "All amounts must be positive"
    count <= 50 because "Maximum 50 line items"

check SelectedOptions
    subset AvailableOptions because "Selected options must be valid"
    disjoint ExcludedOptions because "Cannot select excluded options"
```

**Philosophy filter:**
- ✅ **Readability:** Groups related constraints visually. Reads well.
- ✅ **Mandatory rationale:** Each assertion carries `because`.
- ❌ **New construct kind.** Precept's constraint surfaces are `rule`, `ensure`, and field modifiers. `check` is a fourth surface. More language surface area = more to learn, more for the compiler to validate, more for tooling to support.
- ❌ **Redundant expressiveness.** Everything `check` can do is expressible via Option A (modifiers) + Option B (quantifiers in rules/ensures). The grouping is cosmetic, not semantic.
- ⚠️ **Inspection model.** Does a `check` block report as one constraint or many? If many, it's syntactic sugar over individual rules. If one, it changes the violation attribution model.

**Assessment:** This is a readability optimization, not a capability extension. Defer until (a) Options A and B are both shipped and (b) authors actually report that collection constraints are scattered and hard to read.

---

## 5. Intersection with the Proof Engine

### Statically Provable (compile-time)

| Rule | Proof mechanism |
|------|----------------|
| `unique` on `set of T` | Tautological — sets are unique by definition. Compiler warns. |
| `mincount N` / `maxcount N` vs initial state | Default is empty collection (count = 0). If `mincount 1` and no initial event populates the collection, the compiler can flag the violation against defaults. |
| `notempty` vs initial state | Same as `mincount 1` — provable against defaults. |
| `subset A B` when both are declared | If both collections have `choice`-typed elements with overlapping value sets, subset is provable. If element types differ, it's a type error. |
| `disjoint A B` when both are declared | Same reasoning as subset — provable when value domains are known. |
| `all(item, item >= 0)` when field declares `set of number nonnegative` | The inner-type constraint already guarantees the predicate. Proof engine recognizes this and reports the quantifier as tautological. |

### Runtime-Required (with compile-time diagnostic obligation)

| Rule | Why runtime | Required diagnostic |
|------|-------------|---------------------|
| `unique` on `queue of T` / `stack of T` | Elements are added at runtime via events; uniqueness depends on runtime data. | Obligation diagnostic: "uniqueness of `FieldName` depends on runtime data — the compiler cannot prove all insertions produce distinct values." |
| `all(item, item > threshold)` where `threshold` is a mutable field | The threshold can change; the predicate truth depends on runtime state. | Obligation diagnostic linking to the mutable dependency. |
| `subset A B` where B is modified by events | Subset relationship depends on which elements are added/removed at runtime. | Obligation diagnostic. |
| Aggregate-relational rules (`sum > X`) | Aggregate value depends on runtime collection contents. | Obligation diagnostic. |

### The No-Runtime-Faults Bridge

For every collection rule that requires runtime checking, the compiler must:

1. **Prove it's handled:** If the rule is a field modifier (`unique`, `subset`, `disjoint`), it's enforced on every mutation — same as `nonnegative` is enforced on every assignment. The engine checks after every `add`/`enqueue`/`push`/`remove`/`dequeue`/`pop` and rejects mutations that violate. No unproven fault.

2. **Emit obligation diagnostics for quantifier predicates:** If a `rule` or `ensure` uses a quantifier (`all`/`any`/`none`) and the compiler cannot prove the predicate from inner-type constraints alone, it emits a diagnostic: "quantifier constraint on `FieldName` requires runtime evaluation — ensure all code paths that modify the collection maintain the invariant." The constraint is still enforced at runtime (collect-all semantics), but the diagnostic makes the runtime dependency explicit.

3. **Prove empty-collection safety:** If a quantifier expression is used in a context where the collection might be empty, the compiler must reason about vacuous truth. `Items.all(item, item > 0)` on an empty collection is `true` (vacuous truth — mathematically correct). `Items.any(item, item > 0)` on an empty collection is `false`. If an `any` quantifier is in a `rule` position and the collection can be empty, the rule is falsifiable — the compiler should warn or require a `mincount 1` guard.

---

## 6. Recommendation

### Highest value/complexity ratio: Constraint-Keyword Extensions (Option A)

| Category | Approach | Value | Complexity | Priority |
|----------|----------|-------|------------|----------|
| Cardinality | Existing (`mincount`/`maxcount`) + `notempty` | High | Trivial | Now |
| Uniqueness | `unique` modifier | High | Low | Now |
| Cross-collection | `subset`, `disjoint` modifiers | Medium-High | Medium | Soon |
| Element-shape | Quantifier predicates (Option B, from loop research) | High | Medium-High | Second |
| Aggregate-relational | `.sum` accessor + quantifiers | Medium | High | Third |
| Ordering | Needs ordered collection type | Low | High | Deferred |

**Implementation order:**
1. **Collection `notempty`** — alias for `mincount 1`. Tiny change, high readability value. Many business rules say "must have at least one" and `notempty` reads better than `mincount 1`.
2. **`unique`** — on `queue` and `stack` (tautological on `set`, compiler warns). Enables "all values must be distinct" without quantifiers.
3. **Quantifier predicates** — from the parallel loop research. This unlocks element-shape rules.
4. **Cross-collection modifiers** (`subset`, `disjoint`) — after quantifiers, because the proof engine needs to reason about set relationships.
5. **`.sum` accessor** — when numeric collection aggregation becomes pressing. Probably alongside structured collection elements.

---

## 7. Open Questions for Shane

1. **Collection `notempty` — new keyword or reuse `notempty`?** Today `notempty` applies to `string` only. Extending it to collections is natural (`field Items as set of number notempty`), but it's a semantic widening of an existing keyword. Is that acceptable, or should it be a distinct keyword (e.g., `required`)?

2. **`unique` on sets — warn or silently accept?** Sets are inherently unique. If someone writes `field Tags as set of string unique`, should the compiler (a) warn "unique is redundant on set types," (b) silently accept it as a harmless declaration, or (c) reject it as an error? I lean toward (a) — warn but accept.

3. **Cross-collection modifiers — field modifiers or rule expressions?** `subset` and `disjoint` reference another field. As field modifiers (`field A as set of string subset B`), they're declarative but create a dependency ordering between field declarations. As rule expressions (`rule A subset B because "..."`), they're more flexible but lose the field-level constraint identity. Which surface fits better?

4. **Does this interact with the eventual `list` type?** If Precept adds an ordered collection type, ordering constraints (`ascending`, `descending`) become relevant. Should the taxonomy include them now for future-proofing, or defer entirely?

5. **Priority relative to the quantifier track?** The loop research recommends quantifier predicates. This research recommends constraint keywords. They're complementary. Should they be designed together in one proposal, or shipped as separate increments?

6. **Aggregate accessors — `.sum` and `.average` on numeric collections?** These are straightforward for `set of number` / `set of decimal`. Should they be part of this proposal or a separate one? They don't require quantifiers but do create proof obligations (e.g., `.sum` on an empty collection is 0 — is that always correct?).

---

# Readability Review: combined-design-v2.md (2026-07-17)

**Reviewer:** Elaine (UX Designer)
**Doc:** `docs/working/combined-design-v2.md`
**Verdict:** APPROVED-WITH-CONCERNS

## Top 3 Findings

1. **Parser section needs shape specificity.** The section explains the parser's *philosophy* (source-faithful, recovery-aware) but doesn't give an implementer enough to know what SyntaxTree nodes to define. Missing: error recovery node shape, concrete node inventory or shape sketch, and explicit contract for how malformed input is represented. A parser design doc author would need to invent these from scratch.

2. **Missing navigation guide.** A 486-line doc serving two audiences (human implementers and AI agents) needs a "How to read this document" paragraph after the status block. Three sentences: what §1–§3 cover (commitments and pipeline overview), what §4–§8 cover (per-stage contracts), what §9–§12 cover (runtime and integration). This is the single highest-ROI addition for both audiences.

3. **"How it serves the guarantee" paragraphs become formulaic.** Useful for Lexer through Graph Analyzer. By Proof Engine and Lowering, the pattern is predictable and the content is restating the opening sentence. Recommendation: fold the guarantee connection into the stage's opening paragraph for §8–§10 and drop the separate labeled paragraph.

## Genre Assessment

The rewrite succeeds. §1 opens with a problem statement and architectural commitment, not an inventory. Per-stage sections lead with design decisions. The philosophy-first framing is consistent throughout. This is a design document, not a reference manual.

## Decision

This doc is ready to serve as the architectural foundation for per-stage design docs (starting with the parser). The concerns above are improvements, not blockers — the parser concern is the most urgent because that's the immediate next use case.

---

# Design Review: combined-design-v2.md — Soundness, Completeness, Innovation

**Reviewer:** Frank (Lead Architect)
**Date:** 2026-06-03
**Document:** `docs/working/combined-design-v2.md`
**Context:** Only the Lexer is implemented. All other pipeline stages are stubs.

---

## VERDICT: APPROVED-WITH-CONCERNS

The document is architecturally sound and well-structured. It reads as a unified design explanation rooted in philosophy, not a defense of two separate systems. The pipeline is coherent, the artifact boundaries are clean, and the lowered executable model is concrete enough to implement against. The concerns below are real design gaps that will cost us if we hit them mid-implementation rather than addressing them now.

---

## Soundness Issues

1. **The proof strategy set is closed but its coverage boundary is unstated.** The doc lists four strategies (literal, modifier, guard-in-path, flow narrowing) and says "any obligation outside this set is unresolvable." But it never states what percentage of real-world proof obligations these four strategies can discharge. If most `ProofRequirement` instances in practice require cross-field reasoning (e.g., `ApprovedAmount <= RequestedAmount`), then the four strategies are a beautiful design that rejects most real programs. The doc should include a coverage analysis against the sample corpus — even an informal one — so implementers know whether the strategy set is right-sized or whether a fifth strategy (e.g., relational pair narrowing) is needed before v1.

2. **`Restore` bypasses access-mode but evaluates constraints — the interaction with computed fields is unspecified.** If persisted data includes stale computed-field values, does Restore recompute before constraint evaluation? The recomputation index is listed as a Restore input, but the evaluation order (recompute → validate vs. validate → recompute) is not specified. Getting this wrong means Restore either rejects valid persisted data or accepts invalid computed values.

3. **The `Create` without initial event path evaluates `always` + `in <initial>` — but default values may not satisfy `in <initial>` constraints.** The doc doesn't specify whether this is a compile-time guarantee (the proof engine should catch it) or a runtime domain outcome. The static-reasoning research (C3) says this is a known check, but the combined design doesn't thread it through the proof/fault chain. An author who writes `field X as number default 0` and `in Draft ensure X > 5` gets no compile-time warning in the current design — only a runtime `EventConstraintsFailed` on create. That violates the prevention promise.

4. **`ConstraintActivation` discriminant is described but not typed.** The doc says it "distinguishes whether a constraint binds to the current state, the source state, or the target state." But it doesn't specify whether this is an enum, a DU, or a tag on the descriptor. Given the catalog-driven architecture, this should be cataloged — it's language-surface knowledge that consumers need, not an implementation detail.

---

## Completeness Gaps

1. **No error recovery strategy for the parser.** The doc says `SyntaxTree` preserves "recovery shape for broken programs" and mentions "missing-node representation," but there is no recovery algorithm specified. Panic-mode? Synchronization tokens? The parser recovery strategy directly affects LS quality — a bad recovery model means completions and diagnostics degrade on every keystroke. This is a design decision, not an implementation detail, and it should be locked before the parser is built.

2. **No incremental compilation model.** The doc treats the pipeline as a single-shot transformation: source → tokens → tree → model → graph → proof → CompilationResult. But the language server needs incremental re-analysis on every keystroke. The doc should specify the invalidation boundary — does a keystroke re-lex the whole file? Re-parse? Re-typecheck? For a single-file DSL this may be "just re-run everything" and that's fine — but say so explicitly, with a size-ceiling argument for why that's acceptable (the 64KB source limit helps here).

3. **No serialization contract for `Version`.** The doc specifies `Restore` as the reconstitution path, but never specifies what the caller provides. What is the serialization shape of a `Version`? Is it `(stateName, fieldValues)`? `(stateDescriptor, slotArray)`? The host application needs a defined contract for what to persist and what to hand back to `Restore`. Without it, every host will invent its own serialization and we'll get impedance mismatches.

4. **No definition versioning or migration story.** When a `.precept` file changes (field added, state renamed, constraint tightened), what happens to persisted `Version` instances compiled against the old definition? `Restore` will reject them if they don't satisfy the new constraints. The doc should at least name this as a known gap and specify whether migration is in-scope or explicitly deferred.

5. **No observability hooks.** The doc specifies structured outcomes and inspections, but no tracing, logging, or metric emission points. For a production runtime, host applications need to observe: which events fired, which constraints failed, how long evaluation took, which proof strategies were used. These hooks shape the evaluator's internal architecture — bolting them on later means refactoring the evaluator.

---

## Innovation Opportunities

1. **The proof engine should guarantee initial-state satisfiability at compile time.** The research base (static-reasoning-expansion.md, C3/C4/C5) already describes per-field interval analysis. The combined design should commit to a concrete compile-time guarantee: *if default field values and initial-state constraints are both statically known, the proof engine verifies satisfiability and emits a diagnostic if no valid initial configuration exists.* This is unique among DSL runtimes — no validator, state machine library, or rules engine provides this. It's the proof engine's signature contribution and it's achievable with the bounded strategy set already designed.

2. **Precompute a "constraint influence map" during lowering for AI-native inspection.** Currently the inspection API tells you *what* constraints are active and *whether* they passed. It doesn't tell you *which fields drive which constraints* — the dependency graph exists in the `TypedModel` but is not lowered into an inspectable form. If lowering also produces a `ConstraintInfluenceMap` (constraint → contributing fields, with expression-text excerpts), then an AI agent can answer "why did this constraint fail?" and "which field change would fix it?" without reverse-engineering expressions. This is a structural differentiator for the MCP surface.

3. **The executable model should be a compiled decision table, not a tree walk.** The doc says lowering produces "lowered expression nodes and action plans" but doesn't specify the execution model. For Precept's small, closed expression language, the optimal model is a flat evaluation plan — precomputed slot references, operation opcodes, and result slots — not a recursive tree interpreter. Think of it as a register-based bytecode where "registers" are field slots. This makes evaluation predictable-time, cache-friendly, and trivially serializable for inspection. The doc should commit to "flat evaluation plan" as the executable model shape and explicitly reject tree-walking.

4. **Emit a machine-readable "contract digest" alongside `CompilationResult`.** A deterministic hash of the compiled definition's semantic content (fields, types, constraints, states, transitions — excluding whitespace and comments) would let host applications detect definition changes without diffing source text. Pair it with a structural diff API (`ContractDiff(old, new)` → added/removed/changed fields, states, constraints) and you have the foundation for the migration story (gap #4 above) and a production deployment safety net.

5. **The constraint evaluation matrix should surface "why not" explanations as structured data.** When `Fire` returns `Rejected` or `EventConstraintsFailed`, the outcome carries `ConstraintViolation` objects. But the doc doesn't specify whether violations carry *explanation depth* — just the failing expression text, or also the evaluated field values, the guard that scoped the constraint, and the specific sub-expression that failed. For AI legibility, violations should carry structured explanation: `{ constraint, expression, evaluatedValues: { field: value }, guardContext?, failingSubExpression? }`. This is cheap to compute during evaluation and transforms MCP from "it failed" to "it failed because X was 3 and the constraint requires X > 5."

---

## Right-Sizing Issues

1. **The `SyntaxTree` vs `TypedModel` anti-mirroring rules are over-specified for a doc at this level.** Four numbered rules about what `TypedModel` must not do, plus a seven-item "required inventory" — this is component-level design specification embedded in an architecture document. The architectural decision (they are separate artifacts with separate jobs) is correct and should stay. The implementation contract should move to a parser/type-checker-specific design doc that the implementer reads when building those stages.

2. **The five constraint-plan families and four activation indexes are correctly designed but could be simplified in the implementation.** The `from` and `to` families only activate during `Fire`. The `on` family only activates during `Fire`. The `in` family activates during `Update`, `Create`-without-event, and `Restore`. The `always` family activates everywhere. This means the evaluator really has two modes: "fire mode" (all five families) and "edit mode" (always + in). The doc could name these modes to simplify the mental model without losing the family distinction.

---

## Top 3 Recommended Changes Before This Doc Drives Per-Component Design

### 1. Add a proof coverage analysis against the sample corpus.

Run the four proof strategies against every `ProofRequirement` that would arise from the 20 sample files. Report how many obligations each strategy discharges and how many remain unresolvable. If coverage is below ~90%, design a fifth strategy before implementation begins. This is the highest-risk unknown in the document — the proof engine's value proposition depends on it.

### 2. Specify the parser error recovery strategy.

Lock one of: (a) panic-mode with synchronization at declaration keywords, (b) token-deletion/insertion with cost model, (c) "re-lex everything, re-parse everything" with the 64KB ceiling as the performance argument. The LS team cannot build completion/diagnostic features without knowing what the tree looks like on broken input.

### 3. Commit to a flat evaluation plan as the executable model.

Replace "lowered expression nodes and action plans" with a concrete specification: slot-addressed evaluation plans with operation opcodes, field-slot references, literal constants, and result slots. This prevents the implementation from defaulting to a recursive AST interpreter — which would be correct but would sacrifice the performance and inspectability properties that make Precept's runtime distinctive.

---

*This review is direct because the timing demands it. Addressing these three items now — before the parser, type checker, and evaluator are built — is nearly free. Addressing them after implementation begins is expensive. The architecture is sound. These are the gaps that would bite us.*

---

# Decision: Combined Design v2 Comprehensive Revision Pass

**By:** Frank
**Date:** 2026-07-17
**Status:** Applied

## Summary

Applied all team review feedback (Frank design review, George technical accuracy, Elaine readability) to `docs/working/combined-design-v2.md` in a single revision pass. Added Precept Innovations callouts to every major section. Added two new sections: §12 TextMate Grammar Generation and §13 MCP Integration.

## What Changed

### Review feedback applied (all three reviewers)
- Navigation guide ("How to read this document") after status block
- Parser: error recovery shape, node inventory, catalog-to-grammar mapping, anti-Roslyn guidance, ActionKind dual-use, parser/TypeChecker contract boundary
- TypeChecker: anti-pattern for per-construct check methods
- Proof engine: coverage boundary, flow narrowing clarification, initial-state satisfiability
- Compilation snapshot: no-incremental-compilation model, contract digest hash, definition versioning gap
- Lowering: fixed "catalogs not re-read" claim, descriptor shapes, flat evaluation plan, anti-pattern warnings, ConstraintActivation cataloging, Version serialization contract
- Runtime: Restore recomputation order, structured "why not" violations

### New content
- **Precept Innovations callouts** in every major section (§2–§14), 2–4 bullets each
- **§12 TextMate grammar generation** — catalog contributions table, anti-pattern, zero-drift guarantee
- **§13 MCP integration** — tool inventory, thin-wrapper principle, AI-first design, catalog-derived vocabulary

### Structural changes
- Former §12 (LS integration) renumbered to §14
- Doc grew from 486 to 694 lines
- Formulaic guarantee paragraphs folded into stage openings for §8–§10

## Decisions Locked
- Parser error recovery: construct-level panic mode with `MissingNode` + `SkippedTokens`
- Expression evaluation: flat slot-addressed evaluation plans, tree-walk explicitly rejected
- Incremental compilation: "re-run everything" is the intended model (64KB ceiling)
- Definition versioning: known gap, deferred beyond v1
- `ConstraintActivation`: should be cataloged (language-surface knowledge)

---

## Proposal Summary

Invert D3: make `write` the universal default for (field, state) pairs. Add a `readonly` modifier on field declarations to permanently lock fields from ever being written in any state. Eliminate root-level `write` declarations entirely.

---

## Question 1: Does inverting D3 weaken the conservative guarantee?

**Yes. Fundamentally.**

D3 as specified (§2.2 Access Mode, composition rule 1) states: "D3 is the universal per-pair baseline — undeclared (field, state) pairs default to `read`." The design principle behind this is explicit: "Authors declare only exceptions to readonly — `write` opens a field for editing in that state."

This is a **closed-world access model**. Nothing is writable unless explicitly opened. The omission failure mode is safe: if an author forgets to declare a `write`, the field is locked in that state. The author must take a deliberate action — writing the `write` keyword — to open the attack surface.

The proposal inverts this to an **open-world access model**. Everything is writable unless explicitly restricted. The omission failure mode is unsafe: if an author forgets to mark a field `readonly`, it is exposed in every state to direct mutation via `Update`.

This is the firewall-rule principle. Good security defaults to DENY; you add ALLOW exceptions. D3 defaults to DENY (read-only) and authors add ALLOW exceptions (`write`). The proposal defaults to ALLOW (writable) and authors add DENY exceptions (`readonly`). In a **governance** language — one whose entire identity is built on "invalid configurations are structurally impossible" (Principle 1: Prevention, not detection) — the conservative default is non-negotiable.

### Corpus evidence

The sample set confirms that the conservative default reflects real domain proportions:

- **Stateful precepts with zero write declarations:** `hiring-pipeline`, `loan-application` (except one guarded write), `apartment-rental-application`, `restaurant-waitlist`, `library-hold-request`, `travel-reimbursement`, `warranty-repair-request`. These precepts rely entirely on event-driven mutation. The D3 default silently protects all fields from direct editing. Under the proposal, all those fields would be writable by default — an enormous, invisible expansion of the attack surface.

- **Stateful precepts with 1–2 write declarations:** `crosswalk-signal` (1), `clinic-appointment-scheduling` (1), `building-access-badge-request` (1), `insurance-claim` (2), `maintenance-work-order` (1), `refund-request` (1), `subscription-cancellation-retention` (1), `event-registration` (2), `it-helpdesk-ticket` (1), `utility-outage-report` (1), `vehicle-service-appointment` (1). The typical pattern is opening 1–3 fields in 1–2 states. The remaining (field, state) pairs — the overwhelming majority — stay protected by D3.

- **Stateless precepts:** `fee-schedule` (3 of 5 writable), `payment-method` (2 of 6 writable), `computed-tax-net` (2 of 4 writable), `invoice-line-item` (4 of N writable). Even in stateless precepts, the typical pattern is that some fields are intentionally locked. `customer-profile` is the only sample using `write all`.

The verbosity cost of the current model is 1–2 lines per precept. The safety cost of the proposed model is an invisible, unbounded expansion of the mutation surface whenever an author omits a `readonly` marker.

### Principle citations

- **Principle 1 (Prevention, not detection):** The proposal turns field-level access control from structurally prevented to structurally permitted. An author who omits `readonly` on a field that should be locked has created a governance gap. Under D3, the same omission creates no gap.
- **Principle 4 (Full inspectability):** Auditability is stronger when the declared surface is the exception set (small, explicit) rather than the restriction set (requiring mental subtraction from a universal default). "What can a user directly edit here?" is answered by scanning for `write` keywords under D3. Under the proposal, the answer is "everything, minus what's marked `readonly`" — which requires reading every field declaration to check for the absence of a modifier.

---

## Question 2: Does `readonly` on a field cleanly complement or conflict with computed fields?

**It creates a semantic inconsistency.**

Computed fields (`field Tax as number -> Subtotal * TaxRate`) are already implicitly readonly. The spec enforces this structurally — `ComputedFieldNotWritable` is a type-checker diagnostic (§3.8). A computed field's readonly nature arises from its derivation: it has an expression, so it cannot be directly assigned. This is not a modifier; it is a structural consequence of the field's kind.

Under the proposal, the access defaults would be:

| Field kind | Proposed default | Actual access |
|---|---|---|
| Stored field (no `readonly`) | write | write |
| Stored field (with `readonly`) | write → overridden to read | read |
| Computed field | write (in theory) | read (structurally) |

The computed field's access mode would be inconsistent with the declared default. A stored field and a computed field would have different effective defaults despite the language claiming "write is the default." The author would need to understand that computed fields are a hidden exception to the stated default — undermining Principle 4 (inspectability) and Principle 5 (keyword-anchored readability).

Under D3, the picture is consistent:

| Field kind | D3 default | Actual access |
|---|---|---|
| Stored field (no `write`) | read | read |
| Stored field (with `write`) | read → overridden to write | write |
| Computed field | read | read |

All fields default to read. Computed fields are naturally aligned with the default. Stored fields that need to be writable are explicitly opened. There is no inconsistency to explain.

Adding `readonly` as a modifier also creates a redundancy question: should `readonly` on a computed field be a warning (redundant modifier), an error (modifier conflicts with structural readonly), or silently accepted? Each answer has downsides. Under D3, the question never arises — there is no `readonly` keyword, and computed fields simply match the default.

---

## Question 3: Does "write default, restrict per state" change the auditability story?

**Yes. It weakens it materially.**

In a stateful precept under D3, the audit question "which fields can a user directly edit in state S?" is answered by reading the `in S write` declarations. If there are none, the answer is "nothing — all mutation happens through events." This is a **closed-world audit**: the write declarations ARE the complete answer.

Under the proposal, the same question is answered by: "every field, minus those marked `readonly` on the field declaration, minus those restricted by `in S read` or `in S omit` declarations." This is an **open-world audit** requiring cross-referencing the field declarations (for `readonly` markers), the state-scoped access declarations (for per-state restrictions), and computing the difference. The mental model is subtraction from a universal set rather than enumeration of an explicit set.

For a governance language — one where the point is to make the access contract **explicit and visible** — the open-world model is the wrong posture. The current model's strength is that the write declarations positively assert what is open. The proposed model requires the reader to infer what is open from what is not restricted.

This matters especially for AI consumers. Precept's Principle 3 (deterministic semantics) and Principle 5 (keyword-anchored readability) are designed partly for AI legibility. A closed-world access model is easier for AI agents to reason about: "find all `write` declarations" is a simple, complete query. "Find all fields, subtract `readonly` fields, subtract per-state restrictions" is a compositional query with a higher error surface.

---

## Additional Concerns

### The `readonly` keyword itself is misaligned

`readonly` is a **programming-language concept** from C#, Java, Rust, TypeScript. It carries connotations of compile-time immutability, final binding, memory-model guarantees. Precept's access model is about **editability** — which fields can the host application directly mutate via the `Update` operation. These are different concepts. A field that is `read` in a given state is not immutable — events can still `set` it during transitions. It is merely not directly editable by the external caller. Introducing `readonly` would import programming-language semantics into a domain-configuration language, violating the philosophy's positioning of Precept as a language for domain experts and business analysts, not software developers (§ Who authors a precept in philosophy.md).

### Root-level `write` elimination is a false economy

The proposal motivates itself partly by eliminating root-level `write` declarations. But the current model already makes these declarations do useful work:

- `write BaseFee, DiscountPercent, MinimumCharge` in `fee-schedule` — the `write` keyword positively documents the author's intent. Reading it, you know immediately which fields are editable. The comment above it ("Only pricing levers are editable; TaxRate and CurrencyCode are locked") is restating what the `write` declaration already says.
- `write all` in `customer-profile` — a deliberate, visible assertion that everything is open. Under the proposal, this becomes the invisible default, and the author's deliberate intent vanishes from the surface.

The `write` keyword carries semantic weight as a positive assertion. Replacing it with the absence of `readonly` loses that signal.

---

## Verdict: **Reject**

The proposal inverts Precept's conservative access posture from closed-world (safe by default, explicitly opened) to open-world (exposed by default, explicitly restricted). This:

1. **Weakens the omission failure mode** from safe (field locked) to unsafe (field exposed).
2. **Creates an access-default inconsistency** between stored and computed fields.
3. **Degrades auditability** from positive enumeration to negative subtraction.
4. **Imports programming-language semantics** (`readonly`) into a domain-configuration language.
5. **Eliminates the positive-assertion value** of `write` declarations for marginal verbosity savings (1–2 lines per precept).

D3 is philosophically correct, empirically well-calibrated to real domain proportions, and consistent with the governance identity. It should not be inverted.

### What would need to change for reconsideration

If the underlying concern is verbosity in stateless precepts that happen to have mostly-writable fields, there are narrower solutions that preserve D3:

- A `write all` shorthand already exists and handles the fully-open case.
- If a `write all except F1, F2` syntax were needed, it could be evaluated without inverting the default. The exception list would still be a positive declaration against a positively-declared baseline.

Neither of these requires abandoning the conservative default. The proposal conflates "reduce boilerplate" with "invert the safety model." Only the former is a real problem; the latter is the wrong solution.

---

# George — Technical Review: combined-design-v2.md

**Date:** 2026-04-28
**Verdict:** APPROVED-WITH-CONCERNS

## Summary

The doc is architecturally sound, internally consistent with the catalog system, and faithful to the philosophy. The pipeline stages, artifact boundaries, lowering contract, and runtime operation surfaces are accurately described. The constraint evaluation matrix and activation indexes are precise and correct. The proof/fault chain is well-specified.

However, the doc has **critical implementation-readiness gaps for the Parser** (our immediate next step) and **Roslyn-bias risks** that would cause an implementer to default to conventional compiler patterns in at least four places. These must be patched before the parser design doc is written.

## Top 3 Recommended Changes Before Parser Design

1. **Add a concrete SyntaxTree node inventory.** The doc says what `SyntaxTree` is for but never enumerates its node types. An implementer has no guidance on whether to produce `PreceptSyntax` as a single flat root with child declarations, a Roslyn-style red/green tree, or something else. The Constructs catalog defines 11 `ConstructKind` values with typed slots — the doc should state explicitly that the parser produces one syntax node type per `ConstructKind`, with child nodes corresponding to `ConstructSlot` entries. This is the single most critical gap.

2. **Specify the parser/TypeChecker contract boundary.** The doc says the parser stamps `ConstructKind`, `ActionKind`, `OperatorKind`, `TypeKind` on `TypeRef` nodes, and `ModifierKind` — but doesn't say what the parser guarantees to the TypeChecker and what it doesn't. Does the parser guarantee structurally well-formed declarations (every required slot filled, or represented as a missing-node)? Does the parser guarantee that all identifiers in keyword positions actually resolved to catalog keywords? What can the TypeChecker assume without re-checking? This contract is the #1 source of bugs in multi-pass compilers when it's implicit.

3. **Add an explicit anti-Roslyn section for the parser.** The doc should state: (a) Precept's grammar is line-oriented and flat — there is no block nesting, no brace-delimited scopes, no expression statements; (b) error recovery is construct-level — if a line doesn't parse, emit a diagnostic and skip to the next newline-anchored declaration keyword; (c) the parser does NOT need a general-purpose recursive-descent expression parser for the full language — expressions only appear in specific slots (guards, action RHS, ensure clauses, computed fields, because clauses); (d) operator precedence comes from `Operators.GetMeta()`, not a hardcoded precedence table.

## Technical Accuracy Issues

1. **§5 Parser: "Catalog entry" claim is underspecified.** The doc says "The parser stamps syntax-level identities as soon as syntax alone can know them: construct kind, anchor keyword, action keyword, operator token, literal segment form." This is accurate in principle but dangerous in practice. The parser CAN stamp `ConstructKind` because construct dispatch is keyword-anchored (`field`, `state`, `event`, `rule`, `from`, `in`, `to`, `on`). The parser CAN stamp `OperatorKind` because operators are token-level. But `ActionKind` is ambiguous in syntax — `set` is both an action keyword (TokenCategory.Action) and a type keyword (TokenCategory.Type). The doc should note that `ActionKind` stamping requires the parser to use position context (after `->` = action; after `as`/`of` = type), and this disambiguation is a parser responsibility, not a catalog lookup.

2. **§6 TypeChecker: "projection, not in-place annotation" is correct but needs the mechanism.** The doc says `TypedModel` is a projection of `SyntaxTree`, which is architecturally right. But it doesn't say whether the TypeChecker walks the `SyntaxTree` nodes and produces parallel `TypedModel` nodes (Roslyn's approach), or whether it reads the `SyntaxTree` and populates semantic tables/inventories (more like a symbol-table-driven approach). For Precept's flat, declaration-oriented grammar, the latter is correct — the TypeChecker should build declaration registries and expression bindings, not a parallel tree. The doc should say this explicitly to prevent a Roslyn-pattern default.

3. **§8 ProofEngine: Strategy descriptions are accurate but "straightforward flow narrowing" is vague.** Literal proof, modifier proof, and guard-in-path proof are well-scoped. "Straightforward flow narrowing" could mean anything from SSA-based type narrowing to full dataflow analysis. In Precept's context, this should be scoped to: "if a guard clause in the same transition row establishes a constraint on a field, that constraint is available as evidence for proof obligations on expressions within that row's action chain." The doc should narrow this.

4. **§10 Lowering: "Catalogs are not re-read here" is a design assertion, not an implementation constraint.** Lowering receives `TypedModel` and `GraphResult` which already carry catalog-resolved identities. But the lowering step may still need to read catalog metadata for things like default-value computation, constraint text extraction, or fault-site descriptor construction. The doc should clarify: lowering reads catalog metadata transitively through already-resolved model identities, but does not perform fresh catalog lookups for classification purposes.

5. **Descriptor types are described by name but not by shape.** The doc references `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor` — and `ConstraintDescriptor` actually exists in code with a concrete shape. But the other four are named without any shape specification. The implementation action items (§Appendix A) correctly flag this, but the main doc body treats them as if they're defined. An implementer reading §10–§11 would assume these types exist.

## Implementation Readiness Gaps

### Parser (immediate next step)
- **No node inventory.** The doc never says what `PreceptSyntax Root` contains. Is it a list of declaration nodes? What are the node types? How do expression nodes nest? This is the gap that will cause the most re-derivation.
- **No error recovery strategy.** The doc says "recovery shape" and "missing-node representation" but doesn't specify the pattern. Does the parser use marker nodes for missing required slots? Does it use `Option<T>` for optional slots? Does it skip-to-newline on error?
- **No expression grammar specification.** Expressions appear in guards, action RHS values, ensure clauses, computed fields, if/then/else, and because clauses. The doc doesn't specify the expression grammar or how it relates to the Operators/Operations catalogs. The parser needs to know: what are the expression forms? (binary, unary, literal, field-ref, event-arg-ref, function-call, if-then-else, is-set/is-not-set, contains, member-access). This is a significant omission.
- **No statement about whether Precept's grammar is LL(1), LL(k), or requires backtracking.** Given the line-oriented, keyword-anchored design, it should be LL(1) with single-token lookahead in most positions, but this should be stated.
- **Catalog lookup timing for the parser is unclear.** The doc says the parser consults `Constructs`, `Tokens`, `Operators`, `Diagnostics`. It should specify: the parser uses `Tokens.Keywords` (already built into the lexer) for keyword recognition; it uses `Constructs.GetMeta()` for construct shape validation; it uses `Operators.GetMeta()` for precedence during expression parsing. When does each lookup happen?

### TypeChecker
- Adequate for a per-component design doc, but needs the "semantic tables, not parallel tree" clarification (issue #2 above).
- The typed action family (§6) is well-specified — this is a strong point.
- Missing: how does the TypeChecker handle multi-name declarations (`field a, b, c as number`)? Does it expand them into separate symbols? This matters for the TypedModel shape.

### GraphAnalyzer
- Adequate. The `GraphResult` facts list is concrete enough. The key risk is an implementer building a general graph library instead of the four specific fact categories listed.
- Missing: does the graph operate over `StateDescriptor` and `EventDescriptor` identities from the TypedModel, or does it build its own node identities? The doc says it consumes `TypedModel` — this should be explicit.

### ProofEngine
- The bounded strategy set is the right design. The proof/fault chain is well-specified.
- Missing: what does "unresolvable by the compiler" mean for the author? Is it always a hard error? Or can the author annotate intent? (I suspect hard error is correct for Precept's philosophy — but the doc should state it.)

### Evaluator/Runtime
- The constraint evaluation matrix is precise and correct. The operation-facing plan selection table is accurate.
- Missing: the doc never describes the expression evaluation model. Are lowered expressions interpreted (tree-walk)? Compiled to IL? Compiled to delegates? For Precept's scale, tree-walk interpretation is the right answer — but the doc should say so to prevent an implementer from building a JIT compiler.

## Roslyn-Bias Risks

1. **§5 Parser — An implementer will build a Roslyn-style recursive-descent parser with red/green trees.** The doc says nothing about Precept's grammar being flat and line-oriented. Roslyn's design is for a language with deep nesting (classes → methods → statements → expressions). Precept has NO nesting beyond expression-within-declaration. An implementer needs to know: one pass, keyword-dispatched, line-scoped declarations, with expression sub-parsing only in specific slots. The red/green tree pattern is massive overkill.

2. **§6 TypeChecker — An implementer will write per-construct-kind check methods.** The doc says the TypeChecker resolves `OperationKind`, `FunctionKind`, etc. from catalogs, but doesn't explicitly say: "the type checker should NOT have a `CheckFieldDeclaration()`, `CheckTransitionRow()`, etc. method per construct kind — it should have generic resolution passes that read construct metadata." In practice, some per-construct logic is unavoidable (field declarations and transition rows have genuinely different type-checking needs), but the doc should draw the line: catalog-resolvable checks are generic; construct-specific structural validation is the only place for per-kind methods.

3. **§8 ProofEngine — An implementer will reach for Z3 or an SMT solver.** The doc's "four strategies only, no general SMT solver" is the right call and IS stated. But the strategies are described abstractly enough that an implementer might still build a general obligation-discharge framework. The doc should add: "Each strategy is a simple predicate function, not a solver. Literal proof checks whether the operand is a compile-time constant. Modifier proof checks whether the field carries a relevant modifier. Guard-in-path proof checks whether the enclosing guard expression subsumes the obligation. Flow narrowing checks type state through the immediately enclosing control path."

4. **§10 Lowering — An implementer will serialize the TypedModel.** The doc says lowering "selectively transforms" — but an implementer's default is to map TypedModel types 1:1 to runtime types. The doc should state: the runtime model's shape is organized for execution, not for semantic analysis. Constraint plans are grouped by activation anchor, not by source declaration order. Action plans are grouped by transition row, not by field. The runtime model is a dispatch-optimized index, not a renamed analysis model.

5. **Expression parsing — An implementer will build a Pratt parser or ANTLR grammar.** Neither is stated as required or prohibited. For Precept's expression grammar (binary ops with catalog-defined precedence, unary negation/not, field refs, function calls, if/then/else, member access, is-set, contains), a simple precedence-climbing parser reading precedence from `Operators.GetMeta()` is the right tool. The doc should name this.

## Right-Sizing Issues

1. **The `TypedModel` inventory (§5) is comprehensive but may be over-specified for initial implementation.** "Dependency facts — computed-field dependencies, arg dependencies, referenced-field sets, and semantic edge data" is real and needed, but it's a second-pass concern. The initial TypedModel needs declaration symbols, reference bindings, and typed expressions. Dependency extraction can be a sub-pass within the TypeChecker or a separate lightweight analysis. The doc should distinguish "required for correctness" from "required for optimization."

2. **The proof engine's four strategies are right-sized for the current language.** No over-generalization detected. This is a strong point.

3. **The constraint activation indexes (§10) are correctly scoped.** Four indexes for five anchor families, with the activation discriminant — this is exactly what the evaluator needs, no more.

4. **The three-tier constraint query contract is well-designed but the doc should note that Tier 2 (ApplicableConstraints) is a runtime convenience, not an evaluation necessity.** The evaluator always uses the activation indexes directly. Tier 2 is for API consumers who want to know what constraints are relevant before firing an event.

---

### 2026-04-27: User directive — greenfield parser, no consumer concerns
**By:** Shane (via Copilot)
**What:** The parser is being written from scratch. The stub throws NotImplementedException. There are zero existing consumers of the parser output. Concerns about "breaking existing consumers" or "sequenced PR plans to protect consumers" are invalid in this context. Read the actual code before raising compatibility concerns.
**Why:** User correction — George raised a backward-compatibility concern that does not apply to a not-yet-implemented component.

### 2026-04-27: Design question — calculated field operator
**By:** Shane (via Copilot)
**What:** Would switching calculated fields from -> to <- help the catalog-driven parser design? Specifically: does having -> serve dual duty (transition arrow + computed field assignment) create token ambiguity that complicates slot catalog design? Would <- as a dedicated "computed from" operator simplify things?
**Why:** Shane raised this mid-design-iteration — fold into the parser design loop.

---

### Read the docs — language vision and language design
**By:** Shane (via Copilot)
**What:** Before producing any design output, ALL agents MUST read the relevant documentation — especially language vision and language design docs. Do not design in a vacuum. Do not assume. Read first.
**Why:** Agents have been designing without grounding in the existing language vision and language design documentation. This produces proposals that may contradict or ignore already-decided direction.
**Specific docs to read (minimum):**
- docs/philosophy.md
- docs/archive/language-design/precept-language-vision.md (archived)
- docs/language/precept-language-spec.md
- docs/language/catalog-system.md
- Any other docs/language/* files
- Sample files in samples/ to see actual syntax in use
**Rule:** If a design decision contradicts an existing doc, call that tension out explicitly rather than silently overriding it.

---

### Roslyn source generators for test generation
**By:** Shane (via Copilot)
**What:** Agents working on the catalog-driven parser design MUST consider Roslyn source generators as part of the solution — specifically for test generation. If the catalog describes constructs, slots, and grammar, a source generator could emit test scaffolding (or even test cases themselves) directly from catalog metadata, keeping tests in sync with language surface changes automatically.
**Why:** A truly catalog-driven design means the catalog is the single source of truth — test coverage should derive from it, not be hand-maintained alongside it. Source generators close that loop.
**Questions to address in design:**
- Could a Roslyn generator read ConstructCatalog/ConstructMeta and emit parser test stubs?
- Could it emit round-trip parse tests (text -> AST -> text) per construct automatically?
- How does this interact with the generic AST proposal — if AST nodes are generic/generated, do tests follow?
- What is the boundary between generated test scaffolding and hand-authored test logic?

---

# Calculated Field Arrow Direction: `<-` vs `->` Analysis

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-04-27
**Status:** VERDICT — REJECT
**Requested by:** Shane

---

## 1. Current State

### How `->` is used today

The `->` token (`TokenKind.Arrow`) serves **two distinct grammatical roles** in the Precept DSL:

**Role A — Computed field expression introducer:**
```
field Tax as number nonnegative -> Subtotal * TaxRate
field Net as number positive -> Subtotal - Tax
field LineTotal as number -> TaxableAmount + TaxAmount nonnegative
```

Grammar production (spec §2.2, line 576):
```
field Identifier ("," Identifier)* as TypeRef FieldModifier* ("->" Expr)?
```

The `->` appears at the end of a field declaration, after modifiers, to introduce a computed expression. The field is read-only by contract: no `set`, no `edit`, no `writable`. The arrow says "this field's value is derived from this expression."

**Role B — Action chain / outcome separator:**
```
from Draft on Submit
    -> set ApplicantName = Submit.Applicant
    -> set RequestedAmount = Submit.Amount
    -> transition Approved
```

Grammar productions (spec §2.2, lines 626–628, 656–658, 665–667):
```
from StateTarget on Identifier ("when" BoolExpr)?
    ("->" ActionStatement)*
    "->" Outcome
```

The `->` here introduces each step in a transition pipeline — actions and the final outcome. The parser loops consuming `-> ActionKeyword` pairs, breaking when the token after `->` is an outcome keyword.

**Role C — State action (entry/exit hook):**
```
to Active -> set LastLogin = now()
from Expired -> clear Cache
```

The `->` introduces the action chain after a state target in `to`/`from` scoped constructs.

### How the lexer handles it

The lexer is catalog-driven. `->` lives in the `Tokens` catalog as:

- **TokenKind:** `Arrow` (line 145 of `TokenKind.cs`)
- **Text:** `"->"` 
- **Category:** `TokenCategory.Structural` (not `Operator` — this is a deliberate classification; `Cat_Str`, line 328 of `Tokens.cs`)
- **TextMateScope:** `keyword.operator.arrow.precept`
- **SemanticTokenType:** `operator`

The lexer resolves `->` through the `TwoCharOperators` frozen dictionary (line 411 of `Tokens.cs`). The scan order is: try two-char operators first (`->` before `-`), then fall back to single-char operators (`-` as `Minus`). This is the maximal-munch guarantee documented in spec §1.5, line 209:

> `->` before `-`

The `TwoCharOperators` table is built generically from `Tokens.All` entries with length-2 text and `Operator` or `Structural` categories. The lexer's `TryScanOperator()` method (Lexer.cs lines 733–757) does a single `TwoCharOperatorStarters.Contains(c)` guard, then a `TwoCharOperators.TryGetValue((c, PeekNext), ...)` lookup. No special-case code for `->`.

### What complexity exists

**Disambiguation cost: zero at the lexer level.** The lexer emits `Arrow` tokens identically regardless of context. All disambiguation is in the parser:

- In a `field` declaration: `->` after modifiers means "computed expression follows."
- In a transition row: `->` at line start means "action or outcome follows."
- In a state action: `->` after state target means "action chain follows."

The parser knows which role `->` plays purely from its position in the grammar production — not from any lookahead past the arrow itself. The token after `->` tells the parser whether it's an action keyword (continue loop), outcome keyword (break loop), or expression start (computed field). This is straightforward recursive descent: the parser is already inside the right production when it encounters `->`.

**There is no ambiguity.** The two roles occupy different grammar productions that are entered through different leading tokens (`field` vs `from`/`to`/`on`). The parser never faces a choice between "is this `->` a computed expression arrow or an action chain arrow?" because by the time it sees `->`, it already knows which production it's in.

---

## 2. The `<-` Proposal

The proposal: use `<-` (left-pointing arrow) for computed field expressions instead of `->` (right-pointing arrow). The action chain would retain `->`.

### What would change

#### Lexer impact

A new token kind `LeftArrow` (or `ComputedArrow`) would be needed:

- **TokenKind.cs:** Add `LeftArrow` to the `Operators` section (line ~145).
- **Tokens.cs GetMeta:** Add a new entry: `TokenKind.LeftArrow => new(kind, "<-", Cat_Str, "Computed field expression", ...)`.
- **TwoCharOperators table:** Automatically picks up `('<', '-')` from the new catalog entry.

**Critical conflict: `<-` collides with `<` followed by `-` (unary negation).** The `TwoCharOperatorStarters` set already contains `<` (because of `<=`). Adding `<-` means the lexer would try to match `('<', '-')` as a `LeftArrow` token. But consider this existing legal expression:

```
rule Score < -5 because "..."
```

The lexer would see `<` followed by `-` and greedily consume `<-` as a `LeftArrow` token. This **breaks all existing programs** that use `< -` (less-than, followed by negative number or negation). The maximal-munch rule that currently works cleanly would now create a genuine ambiguity that requires context-sensitive lexing.

Possible mitigations:
1. **Require whitespace between `<` and `-` when they're separate tokens.** This changes the lexer from context-free to whitespace-sensitive in operator scanning. The current lexer has zero whitespace sensitivity in operator scanning — this would be a first.
2. **Don't use the maximal-munch table; add special-case code.** This breaks the catalog-driven scan model. Every other two-char operator goes through the generic `TwoCharOperators` lookup. `<-` would need a carve-out.
3. **Abandon `<-` as a two-char operator and parse it as two tokens (`<` + `-`).** This means the parser must compose two separate tokens into a computed-arrow concept. That's a regression in token-level clarity.

None of these are clean. The current system has **zero special-case code** in operator scanning.

#### Parser impact

If the lexer successfully emits `LeftArrow` tokens:

- Field declaration production changes from `("->" Expr)?` to `("<-" Expr)?`.
- The parser checks for `TokenKind.LeftArrow` instead of `TokenKind.Arrow` when parsing field tails.
- Action chains continue checking for `TokenKind.Arrow`.

The parse grammar change is trivial — a single token kind swap. No disambiguation change, because `->` and `<-` never competed in the same parse context anyway.

#### Type checker impact

None. The type checker operates on the AST, not tokens. Whether the tree says "computed expression" regardless of which arrow introduced it, the semantic analysis is identical.

#### Catalog impact

- `TokenKind.cs`: Add `LeftArrow` (1 enum member, update count).
- `Tokens.cs`: Add `GetMeta` entry for `LeftArrow`.
- The `TwoCharOperators` table auto-derives from `All`.
- The `Operators` catalog is **not affected** — `->` is not an expression-level operator (it's `Structural` category), and `<-` wouldn't be either.
- Spec §1.1 operator table: add `LeftArrow` / `<-` row.
- Spec §1.5 scan priority: add `<-` scan rule and document the `< -` conflict.

#### Grammar generation impact

The TextMate grammar generator reads catalog metadata and emits `tmLanguage.json`. A new `LeftArrow` token with `keyword.operator.arrow.precept` scope would be auto-picked up. Completions and hover would derive from the catalog entry. Impact is minimal — one new derived entry — **assuming the lexer conflict is solved.**

---

## 3. Compiler Simplification Assessment

### Does `<-` reduce parser lookahead requirements?

**No.** The current `->` requires zero lookahead to disambiguate between its two roles. The parser is already inside the correct production (field declaration vs. transition row) before encountering `->`. There is no lookahead cost to reduce.

### Does it eliminate any ambiguity that `->` currently creates?

**No.** There is no ambiguity to eliminate. `->` is unambiguous in both parse contexts because the contexts are entered through different leading tokens (`field` vs `from`/`to`/`on`). The parser never needs to decide "which kind of arrow is this?"

### Does it make the parse grammar more regular or less regular?

**Less regular.** Currently `->` is the universal "pipeline step" glyph — it means "what follows derives from / is produced by what precedes." Using `<-` for one role and `->` for the other introduces a directional split that the grammar must track. The parser now has two arrow token kinds where one sufficed.

More concretely: the scan-priority rule becomes harder. The current rule is clean — `->` before `-`. Adding `<-` requires `<-` before `<` and a resolution for the `<` + `-` collision. The scan priority table gains a new conflict pair.

### Does it affect operator overloading or multi-use disambiguation?

**Yes — negatively.** The `<` character is currently a starter for three tokens: `<=`, `<`, and (hypothetically) `<-`. The `-` character is currently a starter for two tokens: `->` and `-`. With `<-`, both characters become starters for three tokens each, and the `<-` vs `< -` collision is a genuine new ambiguity. The current system has **zero** collisions between its two-char operators and their single-char prefixes in isolation.

### Net assessment

**`<-` adds complexity.** It introduces a lexer conflict that doesn't exist today, requires either whitespace sensitivity or special-case scanning to resolve, and provides zero disambiguation benefit at the parser level because the parser never had an ambiguity to begin with.

---

## 4. Language Surface Assessment

### Does `<-` read better or worse than `->`?

Compare the two:

```

---

# Current (->)
field Tax as number nonnegative -> Subtotal * TaxRate
field Net as number positive -> Subtotal - Tax

---

# Proposed (<-)
field Tax as number nonnegative <- Subtotal * TaxRate
field Net as number positive <- Subtotal - Tax
```

Reading left to right: `field Tax as number nonnegative -> Subtotal * TaxRate` reads as "field Tax, a nonnegative number, **derived as** Subtotal times TaxRate." The arrow flows in the reading direction. The declaration on the left, the derivation on the right, and the arrow connects them in reading order.

With `<-`: "field Tax, a nonnegative number, **receives from** Subtotal times TaxRate." The arrow points backwards — from right to left, against reading direction. The semantic is "the expression on the right flows into the name on the left," which is assignment semantics (like Erlang's `X = 5` or R's `x <- 5`).

This is a philosophical misfit. Precept's `->` isn't assignment — it's derivation declaration. The field doesn't *receive* a value; the field *is defined as* the expression. The right-pointing arrow says "this is what you get" — a definition, not an imperative assignment. The left-pointing arrow imports imperative assignment semantics from general-purpose programming languages, which is precisely the direction Precept's philosophy says not to go.

### Philosophy filter

| Criterion | `->` | `<-` |
|-----------|------|------|
| Domain integrity vs. enforcement-later | Neutral | Neutral |
| Deterministic and inspectable | ✓ Both | ✓ Both |
| Keyword-anchored, flat statements | ✓ Preserved | ✓ Preserved |
| First-match routing / collect-all validation | N/A | N/A |
| AI legibility | ✓ Consistent arrow glyph across all pipeline contexts | ✗ Two arrows, need context to know which is which |
| Configuration vs. scripting | ✓ Declarative derivation feel | ✗ Assignment feel (imperative) |
| Power without hidden behavior | ✓ Same glyph for "this step produces" everywhere | ✗ Different glyph for same "produces" concept in field vs. transition |

**AI legibility specifically:** LLMs reading Precept files benefit from having one universal "produces" operator. A single arrow glyph in two grammatical contexts (field derivation and action pipeline) is less cognitive surface than two glyphs that must be mapped to their respective contexts. Models that have seen `->` in action chains will correctly predict it in computed fields. Introducing `<-` creates a second pattern to learn with no semantic payoff.

### Convention alignment

| Language/System | Arrow | Meaning |
|----------------|-------|---------|
| Haskell | `->` | Function type / pattern match result |
| Erlang | `->` | Clause body |
| R | `<-` | Assignment (imperative) |
| Elm | `->` | Function type / case branch result |
| OCaml | `->` | Pattern match / function type |
| PostgreSQL | `GENERATED ALWAYS AS (expr)` | Keyword-based, no arrow |
| Kotlin | `get() =` | Property getter |
| C# | `=>` | Expression body |

The `<-` convention is associated almost exclusively with **imperative assignment** (R, some reactive frameworks). The `->` convention is associated with **derivation, pattern matching, and "produces"** semantics. Precept's computed fields are derivations, not assignments. `->` is the correct convention.

---

## 5. Verdict: REJECT

### What's wrong with the proposal

1. **The compiler simplification claim is false.** There is no ambiguity in the current `->` usage that `<-` would resolve. The parser never needs to disambiguate between computed-field `->` and action-chain `->` because these are in different grammar productions entered through different leading tokens. The disambiguation cost is zero today.

2. **`<-` introduces a real lexer conflict.** The character pair `< -` currently and correctly scans as two separate tokens (`LessThan`, `Minus`). Adding `<-` as a two-char token breaks maximal-munch for expressions like `Score < -5`. Resolving this requires whitespace sensitivity or special-case scanning — both of which are regressions from the current zero-special-case operator scanner.

3. **`<-` carries wrong semantics.** The left-pointing arrow evokes assignment ("value flows into variable"), which is an imperative concept. Precept's computed fields are declarative derivations ("field is defined as expression"). The right-pointing arrow correctly conveys "this produces" / "this derives as." This is not a style preference — it's a semantic alignment question that affects how readers (human and AI) interpret the construct.

4. **It breaks the universal pipeline glyph.** `->` currently serves as a consistent "what follows is produced" glyph in both field derivations and action chains. Splitting this into two directional arrows doubles the visual vocabulary without adding expressiveness. The consistency of a single arrow glyph is a feature, not a coincidence — it was a deliberate design choice (spec §2.2, line 644: "The `->` arrow is deliberately overloaded to create a visual pipeline that reads top-to-bottom").

### Tradeoff accepted

We accept that `->` is a multi-use token (field derivation + action chain), which means the parser must know its context to interpret it. This is a trivially cheap disambiguation — the parser is already inside the correct production — and the readability benefit of a single universal "produces" glyph outweighs the theoretical elegance of one-token-one-meaning.

---

## 6. Alternatives Considered

### `=>` (fat arrow)

Used in C#, JavaScript, TypeScript for expression bodies. Would avoid the `< -` collision. However:
- Introduces a third arrow-like glyph alongside `->` and `=`.
- `=>` is strongly associated with lambda/function bodies in mainstream languages. Precept doesn't have lambdas. Importing the glyph without the semantics creates false familiarity.
- Doesn't improve over `->` in any dimension that matters.

**Verdict:** No advantage.

### `=` (plain equals)

```
field Tax as number nonnegative = Subtotal * TaxRate
```

This is how spreadsheets and some config languages work. Problems:
- `=` is already `TokenKind.Assign` — used in `set Balance = 0` action statements. Adding a second role creates a real ambiguity: in `field X as number = 5`, is `5` a computed expression or a default value? Defaults use `default 5`, so there's a syntactic distinction, but the visual similarity is a readability trap.
- `=` in an expression context evokes imperative assignment even more strongly than `<-`.

**Verdict:** Worse than the status quo.

### Keyword-based (`computed`, `derives`, `defined as`)

```
field Tax as number nonnegative computed Subtotal * TaxRate
field Tax as number nonnegative derives Subtotal * TaxRate
```

This follows the PostgreSQL `GENERATED ALWAYS AS` model. However:
- Precept's field declaration line is already dense: `field Name as Type Modifier* -> Expr`. Adding a multi-word keyword increases line width without adding clarity over `->`.
- A keyword would need to be reserved, increasing the keyword table and reducing the identifier namespace.
- The `->` glyph is compact and visually distinct — it immediately signals "derivation follows" without consuming identifier-like tokens.

**Verdict:** Inferior to `->` for Precept's compact line-oriented grammar.

### Status quo (`->`)

The right-pointing arrow:
- Has zero lexer conflicts.
- Requires zero parser disambiguation (context is always unambiguous from the enclosing production).
- Reads as "derives as" / "produces" — correct semantics for both computed fields and action chains.
- Is consistent with functional/DSL precedent for "this produces that."
- Is already implemented, documented, tested, and understood.

**Verdict:** The correct choice. No change needed.

---

## Source References

| Artifact | Location | Relevance |
|----------|----------|-----------|
| `TokenKind.Arrow` | `src/Precept/Language/TokenKind.cs:145` | Token enum definition |
| Arrow catalog entry | `src/Precept/Language/Tokens.cs:328–329` | `Cat_Str` category, `keyword.operator.arrow.precept` scope |
| `TwoCharOperators` table | `src/Precept/Language/Tokens.cs:411–415` | Generic two-char scan table derivation |
| `TryScanOperator()` | `src/Precept/Pipeline/Lexer.cs:733–757` | Maximal-munch scan — zero special cases |
| `LessThan` token | `src/Precept/Language/Tokens.cs:314` | `<` is single-char operator, `<` is also in `TwoCharOperatorStarters` via `<=` |
| `Minus` token | `src/Precept/Language/Tokens.cs:320–321` | `-` is single-char operator, `-` is also in `TwoCharOperatorStarters` via `->` |
| Field declaration grammar | `docs/language/precept-language-spec.md:576` | `("->" Expr)?` at end of field production |
| Transition row grammar | `docs/language/precept-language-spec.md:626–628` | `("->" ActionStatement)* "->" Outcome` |
| Deliberate overload rationale | `docs/language/precept-language-spec.md:644` | "deliberately overloaded to create a visual pipeline" |
| Scan order spec | `docs/language/precept-language-spec.md:209` | `->` before `-` in maximal-munch priority |
| Computed field research | `research/language/expressiveness/computed-fields.md` | Read-only derivation contract, precedent survey |
| `computed-tax-net.precept` | `samples/computed-tax-net.precept:10–11` | Canonical computed field usage |
| `invoice-line-item.precept` | `samples/invoice-line-item.precept:16–20` | Multi-step computed field chains |
| `travel-reimbursement.precept` | `samples/travel-reimbursement.precept:14` | Computed field with modifier interleaving |

---

# Design Session Round 1: Catalog-Driven Parser Full Vision

**By:** Frank
**Date:** 2026-04-27
**Status:** Round 1 complete — awaiting George's challenge (Round 2)

## What This Is

Round 1 of a 3-round design session requested by Shane. The prior analysis walked back Layer D (slot-driven productions) and rejected Layer C (disambiguation metadata). Shane explicitly rejected those walkbacks and asked for a full-vision design with no compromise.

## Key Decisions in This Round

1. **`DisambiguationEntry` replaces `LeadingToken` on `ConstructMeta`.** The single `LeadingToken` field cannot express constructs with multiple leading tokens (`StateEnsure` has 3, `AccessMode` has 2 with different disambiguation per entry). The new `Entries: ImmutableArray<DisambiguationEntry>` field carries per-leading-token disambiguation metadata.

2. **Generic disambiguation replaces 4 hand-written methods.** `ParseDisambiguated` handles the `when` guard uniformly, then matches disambiguation tokens from catalog metadata. Zero per-construct disambiguation code.

3. **Generic slot iteration drives all 11 constructs.** `ParseConstructSlots` reads `meta.Slots` and dispatches to slot parsers via a `FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>>`. No per-construct parse methods.

4. **Node factory dictionary instead of exhaustive switch.** Trades CS8509 compile-time enforcement for runtime testability via factory completeness tests. Flagged for George's challenge.

5. **Source generation rejected at current scale.** Design is generator-ready but 11 constructs don't justify the infrastructure investment.

## Design Artifact

`docs/working/catalog-parser-design-v1.md` — full design with C# sketches, catalog changes, migration path, and questions for George.

## What Round 2 Should Challenge

See `## For George` section in the design doc. Key areas: `Entries` replacing `LeadingToken` (breaking catalog change), `when` guard uniformity assumption, slot parser `SyntaxNode?` return type fragility, factory dictionary vs. switch, anchor/guard injection coupling, and clean-slate re-estimate.

---

# Decision: Catalog-Driven Parser Design — Round 3 Resolutions

**By:** Frank
**Date:** 2026-04-27
**Status:** Design decisions locked pending Shane review

## Context

Round 3 of the catalog-driven parser design collaboration. George (Round 2) found two bugs in the v1 design and flagged six decisions for Frank's disposition. Shane added a new question about language extensibility and generic AST options.

## Decisions Made

### George's Six Flagged Items

1. **F1 (LeadingTokenSlot): ACCEPTED.** `LeadingTokenSlot: ConstructSlotKind?` on `DisambiguationEntry` correctly handles the `write all` bug where the leading token doubles as slot content.

2. **F2 (BuildNode shape): GEORGE WINS — exhaustive switch.** CS8509 compile-time enforcement is the correct invariant shape for BuildNode. `_slotParsers` stays as dictionary (registry pattern). Split by purpose, not unified.

3. **F3 (ActionChain peek-before-consume): ACCEPTED.** Verified against all three outcome forms and the no-action case. Fix is correct and complete.

4. **F4 (Two-position when guard): ACCEPTED.** Both pre-disambiguation and post-EventTarget guard positions are valid Precept syntax. The generic disambiguator handles both uniformly. Spec must document both.

5. **F5 (DisambiguationTokens derivation): REJECTED.** Routing and grammar are separable concerns. Declare disambiguation tokens explicitly. No `IntroductionTokens` field on ConstructSlot.

6. **F6 (Migration PR sequence): ACCEPTED with bridge property.** Catalog shape change in PR 1 with `PrimaryLeadingToken` backward-compatible bridge. Parser work in subsequent PRs. Bridge removed when last consumer migrates.

### Extensibility Analysis Outcomes

- Generic AST (ConstructNode with Slots[]): **REJECTED.** Catastrophic type-safety loss for TypeChecker and Evaluator.
- AST-as-catalog-tree: **REJECTED.** Confuses syntax with semantics.
- Source-generated AST nodes: **DEFERRED.** Break-even at ~25-30 constructs. Design is generator-ready.
- Irreducible per-construct code identified: ConstructKind, GetMeta, AST node, BuildNode arm, TypeChecker rules, Evaluator semantics.

## Artifacts

- `docs/working/catalog-parser-design-v3.md` — supersedes v1 and v2 as the living design document.

---

## Summary

Audited all 32 files in `docs/` for references to field access modes, field modifiers, token vocabulary, grammar, diagnostics, and runtime enforcement. 7 files required updates; 25 required no change.

---

## Design Confirmed (Locked)

1. **Two-layer access mode model:**
   - Layer 1: field-level `writable` modifier sets baseline (`write` default across states)
   - Layer 2: `in <State> write|read|omit` overrides per-(field, state) pair
   - State-level ALWAYS wins over field-level for a specific pair
   - Fields without `writable` default to `read` in all states (D3 baseline preserved)

2. **Root-level `write <Field>` eliminated.** Use `writable` modifier on the field declaration instead.

3. **Root-level `write all` preserved** as sugar for stateless precepts — marks all non-computed fields writable.

4. **`writable` on computed field** → existing `ComputedFieldNotWritable` diagnostic fires.

5. **`writable` on event arg** → new `WritableOnEventArg` diagnostic (compile-time only; no runtime backstop path).

6. **`TokenKind.Writable`**: `Text = "writable"`, category `Declaration`, `ValidAfter = VA_FieldModifier`.

7. **`ModifierKind.Writable`**: `ModifierShape.Flag`, `FieldModifierMeta` subtype. Count: 14 → 15.

---

## Files Updated

| File | Changes |
|------|---------|
| `docs/language/precept-language-spec.md` | §1.1 token vocabulary, §1.2 keywords, §2.2 grammar/composition rules, §2.4 modifiers, §3.8 validation, §3.10 diagnostics |
| `docs/archive/language-design/precept-language-vision.md` | Editability form table, declaration keywords, Field Access Modes section, composition rules, parser/typechecker responsibilities (archived) |
| `docs/compiler/parser.md` | Flag modifiers list (added `writable`), dispatch note (write all only), AccessMode grammar node |
| `docs/compiler/type-checker.md` | Processing model — `writable` modifier validation and `WritableOnEventArg` |
| `docs/compiler/diagnostic-system.md` | `WritableOnEventArg` added to `DiagnosticCode` enum and exhaustive switch |
| `docs/language/catalog-system.md` | `Writable` in TokenKind enum, token count 90+ → 91+, `FieldModifierMeta` 14 → 15 members |
| `docs/runtime/evaluator.md` | Access-Mode Enforcement note updated — resolved mode from two-layer composition |

## Files Confirmed No Change

All `docs/working/` files (historical records — must not be updated per audit policy), `docs/philosophy.md`, lexer, graph-analyzer, proof-engine, literal-system, tooling-surface, precept-builder, fault-system, result-types, primitive-types, temporal-type-system, business-domain-types, extension, mcp (stub), language-server (stub), and all READMEs.

**Key rationale for result-types.md:** The runtime `FieldAccessMode { Read, Write }` enum represents the *resolved* per-(field, state) mode after both layers are applied. Correct as-is — `writable` is a compile-time declaration modifier; its resolution into runtime access mode happens in the Precept Builder.

---

## Open Questions / Escalations

None. All decisions locked and documented above.

---

# Frank — `writable` Field Modifier Review

**Date:** 2026-04-27  
**Branch:** `precept-architecture`  
**Commits reviewed:** 28535e4 (catalog + docs), 54672c8 (samples + tests)  
**Verdict:** BLOCKED

---

## Verdict: BLOCKED

One blocker. Three minor doc defects. Everything else is well-executed. Fix B1, M1, and M2 and this clears.

---

## B1 — `Constructs.AccessMode.LeadingToken` incorrectly changed to `TokenKind.In`
**Severity:** Blocker  
**File:** `src/Precept/Language/Constructs.cs`, line 107

`AccessMode.LeadingToken` was changed from `TokenKind.Write` to `TokenKind.In`. This is wrong and the impact is non-trivial: `Parser.cs` is a stub throwing `NotImplementedException`. The real parser has not been written yet. `parser.md` line 138 states: "The dispatch table is a direct map from `ConstructMeta.LeadingToken` to parse methods." A Parser.cs implementer following the catalog will build:

- `In → ParseAccessMode()` — **wrong**
- `In` at the top level must route to `ParseInScoped()` for preposition disambiguation (StateEnsure, AccessMode state-scoped form, StateAction all share `In` as leading token). Direct `In → ParseAccessMode()` would skip that disambiguation entirely.
- The `Write → ParseAccessMode()` path for `write all` (parser.md line 130) has no catalog entry to back it — it would be a dangling dispatch table entry with no corresponding `LeadingToken`.

The correct `LeadingToken` for `AccessMode` is `TokenKind.Write`. This is the one token that maps DIRECTLY to `ParseAccessMode()` at the top level. The state-scoped form (`in State write|read|omit`) enters `AccessMode` indirectly through `ParseInScoped()` — it is a secondary production routed by the preposition disambiguation method, not a first-class `LeadingToken` dispatch.

The `UsageExample = "in Draft write Amount"` and the description are accurate and should be kept. The `LeadingToken` field alone is wrong.

**Required fix:** `TokenKind.In` → `TokenKind.Write` in `Constructs.cs` `AccessMode` entry.

---

## M1 — Stale `edit` terminology in spec §1.1 token table
**Severity:** Minor  
**File:** `docs/language/precept-language-spec.md`, lines 47 and 111

Two entries in the §1.1 token vocabulary table carry v1 `edit` terminology:

- Line 47: `| In | in | State-scoped ensure/edit (in State ensure ...) |` — "edit" is a v1 keyword removed in v2 (§1.2 explicitly states this). Should reference write/read/omit.
- Line 111: `| All | all | Universal quantifier / edit all |` — "edit all" is the v1 form. The v2 form is `write all`.

§1.2 says "`edit` is not reserved in v2. `write` replaces `edit`." Having the token table say "edit all" is a direct contradiction in the same document.

**Required fix:**
- Line 47: `State-scoped ensure/edit (in State ensure ...)` → `State-scoped ensure/write/read/omit scope preposition`
- Line 111: `Universal quantifier / edit all` → `Universal quantifier / write all (stateless precepts), read all / omit all (state-scoped)`

---

## M2 — catalog-system.md field modifier count comment stale
**Severity:** Minor  
**File:** `docs/language/catalog-system.md`, line 740

The code sample comment reads `// ── Field modifiers (14) ─────────────────────────────────`. There are now 15 field modifiers. The summary table at line 716 correctly says 15. The `ModifierKind.cs` group comment correctly says `field (15)`. The inline doc comment is the only laggard.

**Required fix:** `(14)` → `(15)` on line 740.

---

## Good Findings

**G1 — Token group placement correct.** `TokenKind.Writable` lands in the Declaration group between `Optional` and `Because`. Consistent with `Optional` as a field-declaration modifier; Declaration group is the right category.

**G2 — `VA_FieldModifier` bidirectional setup correct.** `TokenKind.Writable` appears in the `VA_FieldModifier` array (other modifiers can precede `writable`) and `writable`'s own `ValidAfter: VA_FieldModifier` (other modifiers can follow `writable`). Position-agnostic within the modifier list. Samples confirm both orderings work: `positive writable`, `optional writable`.

**G3 — `FieldModifierMeta(AnyType)` split is architecturally correct.** `ApplicableTo = AnyType` (empty) is right because computed-field exclusion is a semantic rule (field has a `->` expression), not a type-compatibility rule. `ApplicableTo` encodes type-based restrictions; the computed-field restriction belongs to the type checker. The test comment explicitly documents this: "computed-field restriction is enforced by the type checker, not the modifier catalog." Clean separation.

**G4 — `WritableOnEventArg` diagnostic is correctly placed and specified.** Stage=Type, Severity=Error, Category=Structure, positioned after `CircularComputedField` and before `ConflictingAccessModes` in the enum. The message "The 'writable' modifier cannot appear on event argument '{0}'" is accurate. The fix hint "Remove 'writable' — event arguments are always read-only within the transition body" is precise and actionable.

**G5 — Sample migration is clean.** All 6 migrated samples (`computed-tax-net`, `fee-schedule`, `invoice-line-item`, `payment-method`, `sum-on-rhs-rule`, `transitive-ordering`) are stateless precepts. `writable` appears only on non-computed fields in every case. All 22 other samples are untouched. State-scoped `in State write` forms are preserved across the full sample set. `customer-profile.precept:write all` is correctly untouched. No stale `write <FieldName>` patterns remain.

**G6 — D3 guarantee correctly preserved.** Fields without `writable` default to `read`. The language spec §2.2 "D3 default" rule (composition rule #2) is accurate. The evaluator.md correctly describes the two-layer composition model being pre-resolved at Precept Builder time, with the evaluator reading descriptors, not re-computing access modes.

**G7 — Two-layer composition model documentation is a significant improvement.** The Layer 1 / Layer 2 model in §2.2 replaces the previous flat "read-by-default" description with a structured account of how field baselines and state overrides compose. The nine composition rules are complete, ordered correctly (baseline → D3 default → state override → guarded write → omit clear → set restriction → conflicts → computed → event arg), and accurate.

**G8 — All 1783 tests pass.** No regressions.

**G9 — Language spec completeness is thorough.** `writable` appears in §1.1 (Declaration keyword table), §1.2 (reserved keyword set), §2.2 (access mode grammar with full two-layer model), §2.4 (field modifiers table), §3.8 (modifier validation table), §3.10 (diagnostic table with `WritableOnEventArg`). No section missing.

**G10 — MCP gap is pre-existing, not introduced by this change.** `LanguageTool.cs` does not exist — MCP only has `PingTool.cs`. `writable` will flow through automatically when `LanguageTool` is implemented from the catalog. No gap introduced here.

---

## Minor Observation (Not a Blocker)

**`ComputedFieldNotWritable.RelatedCodes` does not include `WritableOnEventArg`.** Both diagnose misuse of `writable`. This is a weak cross-navigation gap in the LS diagnostic experience. Not a blocker — the two codes diagnose distinct surfaces (computed fields vs. event args) — but worth considering when the LS diagnostic UI ships.

---

## Fixes Required Before Re-Review

1. `src/Precept/Language/Constructs.cs` — `TokenKind.In` → `TokenKind.Write` on the `AccessMode` `LeadingToken` argument. Description and example may stay as-is.
2. `docs/language/precept-language-spec.md` line 47 — remove "edit", add write/read/omit.
3. `docs/language/precept-language-spec.md` line 111 — `edit all` → `write all`.
4. `docs/language/catalog-system.md` line 740 — `(14)` → `(15)`.

Items 2–4 are doc-only and may land in the same commit as item 1.

---

# Decision Note: Catalog-Driven Parser Design Round 2

**By:** George
**Date:** 2026-04-27
**Related doc:** `docs/working/catalog-parser-design-v2.md`
**Status:** Pending Frank's Round 3 response

---

## Context

Shane asked for the full vision of a catalog-driven parser with no compromise. Frank wrote Round 1
(`docs/working/catalog-parser-design-v1.md`). This note records George's Round 2 findings for
the decisions log.

---

## Findings Requiring Team Decision

### Finding G1 — Two Implementation Bugs in Frank's v1 (Both Are Real, Both Need Fixes)

**Bug 1 (ActionChain/Outcome boundary):** Frank's `ParseActionChain()` consumes `->` then breaks
when an outcome keyword follows, leaving the outcome keyword as the current token. `ParseOutcome()`
then expects `->` first and returns null. This fires on every `TransitionRow` with actions — the
`Outcome` required slot gets `ExpectedSlot` diagnostic on valid input.

Fix: `ParseActionChain()` must peek at `Peek(1)` before consuming `->` when an outcome keyword
follows. Leave `->` for `ParseOutcome()` to consume as its introduction token.

**Bug 2 (AccessMode `Write`-leading path):** `write all` dispatches with `Write` consumed as the
leading token. The generic slot iterator then calls `_slotParsers[AccessModeKeyword]()` but current
token is now the field target (`all`). The `Write` token is gone.

Fix: Add `LeadingTokenSlot: ConstructSlotKind?` to `DisambiguationEntry`. When set, the parser
injects a synthetic node from the already-consumed leading token into that slot rather than parsing
fresh.

**Disposition needed:** Frank should confirm both fixes are accepted before any implementation work
begins on the parser.

---

### Finding G2 — `DisambiguationEntry` Is a Breaking Catalog Shape Change

`ConstructMeta.LeadingToken` must become `Entries: ImmutableArray<DisambiguationEntry>`. This
breaks:
- LS completions that read `ConstructMeta.LeadingToken`
- MCP vocabulary output that uses it
- Any tests referencing it

Mitigation: add `PrimaryLeadingToken` computed property = `Entries[0].LeadingToken` as
backward-compat bridge. Consumers migrate to `Entries` incrementally.

**The "no migration" claim in Frank's v1 is correct for the parser stub, not for catalog consumers.**
The catalog PR must be sequenced before parser implementation begins.

---

### Finding G3 — Implicit Contract Must Become Explicit

The generic disambiguator leaves disambiguation tokens unconsumed. This only works if each
slot parser consumes its own introduction token as its first action. This is an invariant that
holds for all current slot parsers but is undocumented. If a future slot parser omits this,
the stream is corrupted silently.

Mitigation: document the contract in the `_slotParsers` dictionary code comment and in the
parser design doc. This is not a blocker, but it must be done before implementation.

---

### Finding G4 — Pre-Disambiguation `when` Guard Position in TransitionRow

The `when` guard can appear at two syntactic positions in TransitionRow:
1. Before the `on` disambiguation token (`from X when expr on Y → ...`)
2. After the event target (`from X on Y when expr → ...`, the standard form)

Frank's generic disambiguator handles both correctly: pre-disambiguation guards are injected
into slot[2] after disambiguation; post-EventTarget guards are parsed naturally during slot
iteration. Both produce the same `TransitionRowNode.Guard` field.

**Decision needed from Frank:** Is `from X when expr on Y → ...` (pre-disambiguation guard)
actually valid Precept syntax? The parser.md tables list `When` as a re-check option for all
preposition methods, but no samples demonstrate this form. If it is NOT valid, the disambiguator
should either skip the guard-consumption step or constrain it to constructs that declare
a `GuardClause` slot at the construct level.

---

## Revised Estimate

| Option | Scope | Estimate |
|--------|-------|---------|
| Option 1 (A+B+C+E only) | Catalog + vocabulary tables + disambiguation metadata + sync set | 1 week |
| Option 3 (A+B+D+E, Frank's full vision) | Full catalog-driven parser | **3–3.5 weeks** (was 2.5–3; +18h correctness hardening) |

The corrections do not change the architectural recommendation — they harden it. Option 3 is
still viable on a clean-slate build. The bugs are correctness issues that exist regardless of
which option is chosen once parser implementation begins.

---

## No Code Before These Are Resolved

Per George's charter: no implementation work until Frank's sign-off on the design. These bugs
are discovered in design review — the right time. The parser is still a stub. These are zero-cost
fixes at design time.

---

# Soup Nazi — Writable Coverage Review
**Date:** 2026-04-27  
**Reviewer:** Soup Nazi (Tester)  
**Scope:** `writable` field modifier — test coverage audit  
**Test run:** 1783 tests, 0 failed, 0 skipped ✅

---

## Verdict: BLOCKED

Two blockers. No soup until they are fixed.

---

## Blockers

### B1: `AccessMode.LeadingToken` change is untested

`Constructs.AccessMode` had its `LeadingToken` changed from `TokenKind.Write` to `TokenKind.In`. No test anywhere in `ConstructsTests.cs` asserts `LeadingToken` on any construct — the field is completely invisible to the test suite.

If someone reverts this to `Write` (or any other value), no test will catch it.

**Required fix:** Add a test to `ConstructsTests.cs`:

```csharp
[Theory]
[InlineData(ConstructKind.AccessMode,       TokenKind.In)]
[InlineData(ConstructKind.StateEnsure,      TokenKind.In)]
[InlineData(ConstructKind.StateAction,      TokenKind.To)]
[InlineData(ConstructKind.EventEnsure,      TokenKind.Ensure)]
[InlineData(ConstructKind.FieldDeclaration, TokenKind.Field)]
[InlineData(ConstructKind.StateDeclaration, TokenKind.State)]
[InlineData(ConstructKind.EventDeclaration, TokenKind.Event)]
public void LeadingToken_IsCorrect(ConstructKind kind, TokenKind expectedToken)
{
    Constructs.GetMeta(kind).LeadingToken.Should().Be(expectedToken,
        $"{kind} must begin with {expectedToken}");
}
```

`AccessMode → In` is the regression anchor for this change. The rest are bonus coverage.

---

### B2: `WritableOnEventArg` missing from `DiagnosticsTests.TypeCodes` static list

`TypeCodes` in `DiagnosticsTests.cs` (line 153) is a hardcoded TheoryData used by `TypeStageCodes_AllHaveTypeStage`. It includes `ComputedFieldNotWritable` but does **not** include `WritableOnEventArg`. The two diagnostic codes were added in the same implementation — one is present, one is absent.

The three dynamic `Create_*` theories (using `AllDiagnosticCodes()`) DO exercise `WritableOnEventArg` and would catch a missing `GetMeta` entry or factory crash. But `TypeStageCodes_AllHaveTypeStage` will not catch a future stage miscategorization for `WritableOnEventArg`.

**Required fix:** Add `DiagnosticCode.WritableOnEventArg` to the `TypeCodes` TheoryData in `DiagnosticsTests.cs`:

```csharp
// near ComputedFieldNotWritable
DiagnosticCode.WritableOnEventArg,
```

---

## Good Observations

**G1: ModifiersTests — 7 new/updated tests are correct.**  
Count invariants updated (28→29 total, 14→15 field, 25→26 structural), `Writable_AppliesToAnyType`, `Writable_IsStructuralFlag`, `Writable_TokenTextIsWritable`, and `FlagModifiers_HasValueIsFalse` theory updated — all well-formed. The "empty = any type" semantics are correctly documented in the assertion message.

**G2: Dynamic exhaustiveness net is solid.**  
`GetMeta_ReturnsForEveryModifierKind`, `All_ContainsEveryKindExactlyOnce`, and the three `Create_*` theories all use `Enum.GetValues<>()` — new entries are covered without code changes. `WritableOnEventArg` is not orphaned; it passes the Create factory tests today.

**G3: TokensTests uses dynamic count — no hardcoded token count to update.**  
`All_ContainsExactlyAsManyEntries_AsEnumValues` derives its expected count from the enum. `AllKeywords_HaveTextMateScope` and `AllKeywords_HaveSemanticTokenType` cover `TokenKind.Writable` automatically (it is `Cat_Decl`, which is included in both token-property checks). No action needed.

**G4: Sample files are clean.**  
All 6 migrated samples place `writable` only on non-computed fields:
- `computed-tax-net.precept`: Subtotal, TaxRate writable; Tax, Net (computed) — no `writable`. ✅  
- `fee-schedule.precept`: BaseFee, DiscountPercent, MinimumCharge writable; TaxRate, CurrencyCode locked. ✅  
- `invoice-line-item.precept`: Description, UnitPrice, Quantity, DiscountPercent writable; Subtotal through LineTotal (all computed) — no `writable`. ✅  
- `sum-on-rhs-rule.precept`: Total, Tax, Fee writable; Net (computed) — no `writable`. ✅  
- `transitive-ordering.precept`: High, Mid, Low writable; Spread (computed) — no `writable`. ✅  
- `payment-method.precept`: IsDefault, Nickname writable; no computed fields. ✅

**G5: Hover description coverage is implicit.**  
`GetMeta_ReturnsForEveryModifierKind` asserts `Description.NotBeNullOrEmpty` for every `ModifierKind` — `Writable` is covered without a dedicated test.

---

## Deferred Tests (Parser/TypeChecker not yet implemented)

These tests MUST exist before the implementation is marked complete. They are not optional.

| ID | Test | Trigger |
|----|------|---------|
| D1 | `field X as money writable` → zero diagnostics | Parser + TypeChecker |
| D2 | `field X as money` (no modifier) → field is read-only baseline | TypeChecker semantic model |
| D3 | Computed field + `writable` → `ComputedFieldNotWritable` | TypeChecker |
| D4 | Event arg + `writable` → `WritableOnEventArg` | TypeChecker |
| D5 | Field `writable` baseline + `in State write|read|omit` → correct composed mode | TypeChecker + evaluator |
| D6 | State-scoped `in State write Field` still works (regression) | TypeChecker |
| D7 | Stateless `write all` still works (regression) | TypeChecker |
| D8 | `writable` on computed field that also has `default` → both `ComputedFieldNotWritable` and `ComputedFieldWithDefault` fire | TypeChecker |

---

## Summary

Fix B1 and B2, then resubmit. The catalog-level work is solid. The sample migration is correct. The blocking gaps are small and surgical: one new `[Theory]` in `ConstructsTests.cs` and one line added to `DiagnosticsTests.TypeCodes`.

No soup until then.

---

## Verdict

**NEEDS MORE TESTS**

Two blockers. The catalog work is solid. The lexer surface is now covered. The gaps are surgical and documented below with exact fixes.

---

## Test Coverage Assessment

### What's covered

| Area | Test | Status |
|------|------|--------|
| `Writable_AppliesToAnyType` — `ApplicableTo.Should().BeEmpty()` | `ModifiersTests.cs` | ✅ |
| `Writable_IsStructuralFlag` — `Category == Structural`, `HasValue == false` | `ModifiersTests.cs` | ✅ |
| `Writable_TokenTextIsWritable` — `Token.Text == "writable"`, `Token.Kind == Writable` | `ModifiersTests.cs` | ✅ |
| `FlagModifiers_HasValueIsFalse` includes `Writable` | `ModifiersTests.cs` | ✅ |
| Count invariants updated: 29 total, 15 field, 26 structural | `ModifiersTests.cs` | ✅ |
| `GetMeta_ReturnsForEveryModifierKind` covers `Writable` via enum exhaustion | `ModifiersTests.cs` | ✅ |
| `AllFieldModifiers_AreStructural` covers `Writable` structurally | `ModifiersTests.cs` | ✅ |
| `TokenKind.Writable` in `Keywords`, TextMateScope, SemanticTokenType (via exhaustiveness) | `TokensTests.cs` | ✅ |
| `Create_*` factory theories cover `WritableOnEventArg` via `Enum.GetValues<DiagnosticCode>()` | `DiagnosticsTests.cs` | ✅ |
| `WritableOnEventArg` meta not-null, severity + stage returned by `Create` | `DiagnosticsTests.cs` | ✅ (via AllDiagnosticCodes dynamic) |
| Lexer emits `Writable` token after type keywords (all 5 surface cases) | `WritableSurfaceTests.cs` | ✅ (added during investigation) |
| `in Draft write Amount` emits `Write` not `Writable` (correct distinction) | `WritableSurfaceTests.cs` | ✅ |
| `write all` preserved — lexes as `Write + All` | `WritableSurfaceTests.cs` | ✅ |
| Root-level `write Amount` — lexer doesn't reject (Parser's job) | `WritableSurfaceTests.cs` | ✅ |

### What is NOT covered — blockers

See Findings section.

---

## Findings

### [GAP] WritableOnEventArg Missing from TypeCodes Stage Group

**Severity:** Major

**File:** `test/Precept.Tests/DiagnosticsTests.cs`, `TypeCodes` member data (~line 153)

**Finding:** `DiagnosticsTests.TypeCodes` is the hardcoded list used by `TypeStageCodes_AllHaveTypeStage`. It includes `DiagnosticCode.ComputedFieldNotWritable` (added in the same PR) but does **not** include `DiagnosticCode.WritableOnEventArg` (also added in the same PR). The two codes were introduced together; one made it into the stage-group list, one did not.

The three dynamic `Create_*` theories iterate `Enum.GetValues<DiagnosticCode>()` and DO exercise `WritableOnEventArg` — the `GetMeta` entry exists, the factory doesn't crash, and the severity/stage round-trips correctly via the generic path. But `TypeStageCodes_AllHaveTypeStage` will not catch a future miscategorization (e.g., accidentally setting `DiagnosticStage.Parse` instead of `DiagnosticStage.Type`).

Additionally, no severity spot-check exists for `WritableOnEventArg` the way one exists for `DivisionByZero_HasErrorSeverity`. This is a minor but real gap — it's `Severity.Error` and that contract should be pinned.

**Required action:**

1. Add `DiagnosticCode.WritableOnEventArg` to `TypeCodes` in `DiagnosticsTests.cs` between `CircularComputedField` and `ConflictingAccessModes`:

```csharp
// existing entries
DiagnosticCode.ComputedFieldNotWritable,
DiagnosticCode.ComputedFieldWithDefault,
DiagnosticCode.CircularComputedField,
DiagnosticCode.WritableOnEventArg,   // ← ADD THIS
DiagnosticCode.ConflictingAccessModes,
```

2. Add a severity spot-check:

```csharp
[Fact]
public void WritableOnEventArg_HasErrorSeverity()
{
    Diagnostics.GetMeta(DiagnosticCode.WritableOnEventArg).Severity.Should().Be(Severity.Error);
}
```

---

### [GAP] AccessMode.LeadingToken Change Has No Regression Test

**Severity:** Major

**File:** `test/Precept.Tests/ConstructsTests.cs`

**Finding:** `ConstructKind.AccessMode.LeadingToken` was changed from `TokenKind.Write` to `TokenKind.In` as part of this PR. This is a behavioral change to the catalog's public contract — `LeadingToken` drives LS completions, MCP vocabulary output, and semantic token classification. No test in `ConstructsTests.cs` asserts `LeadingToken` on any construct — the property is completely invisible to the test suite. A regression back to `TokenKind.Write` would not be caught by any test.

The `GetMeta_ReturnsForEveryConstructKind` exhaustiveness test checks `Kind`, `Name`, `Description`, and `UsageExample`. It does not check `LeadingToken`.

**Required action:** Add a `[Theory]` to `ConstructsTests.cs` pinning `LeadingToken` for key constructs. `AccessMode → In` is the regression anchor for this PR change; the others are bonus coverage that should also have been tested:

```csharp
[Theory]
[InlineData(ConstructKind.AccessMode,       TokenKind.In)]
[InlineData(ConstructKind.StateEnsure,      TokenKind.In)]
[InlineData(ConstructKind.StateAction,      TokenKind.To)]
[InlineData(ConstructKind.EventEnsure,      TokenKind.On)]
[InlineData(ConstructKind.FieldDeclaration, TokenKind.Field)]
[InlineData(ConstructKind.StateDeclaration, TokenKind.State)]
[InlineData(ConstructKind.EventDeclaration, TokenKind.Event)]
[InlineData(ConstructKind.TransitionRow,    TokenKind.From)]
[InlineData(ConstructKind.PreceptHeader,    TokenKind.Precept)]
public void LeadingToken_IsCorrect(ConstructKind kind, TokenKind expectedToken)
{
    Constructs.GetMeta(kind).LeadingToken.Should().Be(expectedToken,
        $"{kind} must begin with {expectedToken}");
}
```

---

### [CONFIRMED] ModifiersTests — 4 New Writable Tests Are Correct

**Severity:** N/A (confirmed)

**File:** `test/Precept.Tests/ModifiersTests.cs`

**Finding:** All 4 new Writable tests are well-formed and assert the right catalog properties:
- `Writable_AppliesToAnyType` — asserts empty `ApplicableTo` with the correct semantic comment ("empty = applies to all types; computed-field restriction is enforced by the type checker")
- `Writable_IsStructuralFlag` — asserts `Category == Structural` and `HasValue == false`
- `Writable_TokenTextIsWritable` — asserts `Token.Text == "writable"` and `Token.Kind == TokenKind.Writable`
- `FlagModifiers_HasValueIsFalse` updated to include `ModifierKind.Writable`

Count invariants (29 total / 15 field / 26 structural) are correct.

**Required action:** None.

---

### [CONFIRMED] TokensTests — TokenKind.Writable Covered by Exhaustiveness

**Severity:** N/A (confirmed)

**File:** `test/Precept.Tests/TokensTests.cs`

**Finding:** No direct spot-check test for `TokenKind.Writable` exists in `TokensTests.cs`. However, the existing exhaustiveness tests adequately cover it:
- `GetMeta_ReturnsWithoutThrowing_ForEveryTokenKind` — runs over every `TokenKind` including `Writable`
- `All_ContainsExactlyAsManyEntries_AsEnumValues` — count-invariant catches missing entries
- `AllKeywords_HaveTextMateScope` — `Writable` has `Cat_Decl` and non-null text, so it's included
- `AllKeywords_HaveSemanticTokenType` — same reason
- `Keywords_ContainsAllKeywordCategoryTokensWithNonNullText` — `Writable` will be in both `expectedKeys` and `Keywords.Keys`

The indirect coverage via `Writable_TokenTextIsWritable` in `ModifiersTests.cs` also pins the token text. No spot-check gap that needs to be filled.

**Required action:** None. Pre-existing pattern; `ValidAfter` membership is not tested for any token — that's a broader gap outside this PR's scope.

---

### [CONFIRMED] No Old `write Field` Syntax in Test Data

**Severity:** N/A (confirmed)

**File:** All `test/Precept.Tests/*.cs`

**Finding:** Grep for `write\s+\w` across all test files found zero matches in test data strings. The only occurrence is in a comment string in `ConstructsTests.cs` ("root-level write"). No regression from eliminated `write Field` syntax exists in the catalog test suite.

**Required action:** None. Note: the eliminated syntax is not rejected at lex time (lexer is context-free; `write Amount` emits `Write + Identifier` without error). Parser-level rejection must be tested once Parser is implemented.

---

### [CONFIRMED] MCP Regression — Lexer Correctly Handles writable

**Severity:** N/A (confirmed)

**Finding:** MCP server is live (`precept_ping` = ok). All lexer-surface probes pass:

| Probe | Result |
|-------|--------|
| `field Amount as money writable` | `Writable` token emitted after `MoneyType` ✅ |
| `field Amount as money` (no modifier) | No `Writable` token emitted ✅ |
| `write all` on stateless precept | `Write + All` tokens; no `Writable` ✅ |
| `in Draft write Amount` | `In + Write` tokens; `Writable` token absent (correct: `write` is the access-mode keyword, `writable` is the field modifier) ✅ |
| `write Amount` (eliminated syntax) | Lexes as `Write + Identifier`; no lex diagnostic. Rejection is Parser/TypeChecker work ✅ |

All compile paths uniformly throw `NotImplementedException` at `Parser.Parse()` — consistent with the known stub state.

**Required action:** None for current state.

---

### [CONFIRMED] WritableSurfaceTests.cs Created During Investigation

**Severity:** N/A (informational)

**File:** `test/Precept.Tests/WritableSurfaceTests.cs` (new, created during investigation)

**Finding:** 10 new tests were created during the MCP regression phase. They cover:
- 5 `*_LexesCorrectly` tests — verify token stream shapes for each writable surface case
- 5 `*_CompileThrowsNotImplemented` tests — anchor the current stub state

**Caution:** The `CompileThrowsNotImplemented` tests are asserting stub behavior. When Parser is implemented, they will turn red. That is correct and honest — they will be visible failures requiring update. They should NOT be deleted or skipped before the implementation lands; they should be converted to positive-case assertions at that time.

All 10 new tests pass. Total count is now 1793.

**Required action:** None. Keep the file. Update `*_CompileThrowsNotImplemented` tests when Parser is implemented.

---

## Deferred Tests (Parser/TypeChecker stubs — required before implementation is complete)

These tests MUST exist before the `writable` implementation is marked done. Red is acceptable. Skip is not.

| ID | Test | Gate |
|----|------|------|
| D1 | `field X as money writable` → compiles clean, zero diagnostics | Parser + TypeChecker |
| D2 | `field X as money` (no modifier) → field is read-only baseline | TypeChecker semantic model |
| D3 | Computed field + `writable` → `ComputedFieldNotWritable` diagnostic | TypeChecker |
| D4 | Event arg + `writable` → `WritableOnEventArg` diagnostic | TypeChecker |
| D5 | `writable` baseline + `in State write Field` override → correct composed access mode | TypeChecker + evaluator |
| D6 | `in State write Field` still works on non-writable field (regression) | TypeChecker |
| D7 | Stateless `write all` still works (regression) | TypeChecker |
| D8 | `writable` on computed field with `default` → both `ComputedFieldNotWritable` + `ComputedFieldWithDefault` fire | TypeChecker |

---

## Summary

The catalog-level work is solid. The `Writable` modifier entry in `Modifiers.cs` is correct and well-tested. The 1793 tests pass cleanly.

**Fix these two gaps, then resubmit:**

1. **`WritableOnEventArg` → add to `TypeCodes`** in `DiagnosticsTests.cs` + add `WritableOnEventArg_HasErrorSeverity` spot-check.
2. **`AccessMode.LeadingToken → In`** → add `LeadingToken_IsCorrect` theory to `ConstructsTests.cs`.

Both fixes are one-liners or near-one-liners. No soup until then.

---

# Decision: Access-mode shorthand grammar and AST split

**Date:** 2026-04-28
**By:** Shane (owner) with Frank and George follow-through
**Status:** Locked

## Decision

- Access declarations use `modify` plus the adjectives `readonly` or `editable`; structural exclusion uses `omit`.
- `modify` and `omit` share the same `FieldTarget` shapes: a single field, a comma-separated field list, or `all`.
- The locked grammar is:

```precept
in State modify Field readonly [when Guard]
in State modify Field editable [when Guard]
in State modify F1, F2, ... readonly|editable [when Guard]
in State modify all readonly|editable [when Guard]

in State omit Field
in State omit F1, F2, ...
in State omit all
```

- Guards are permitted only on `modify`; `omit` is never guardable.
- Syntax and catalog shapes stay split: `AccessModeDeclaration` and `OmitDeclaration` are separate AST node kinds, not a unified access-declaration node.

## Why

- Shane explicitly directed the team to preserve comma-separated field shorthand and the `all` shorthand in the new `modify` surface.
- He also extended that same shorthand to `omit`, because both verbs operate over the same `FieldTarget` domain.
- Separate AST nodes preserve the real semantic difference: `omit` changes structural presence, while `modify` declares an access level and optionally carries a guard.

## Follow-through

- Frank completed the live-doc sweep across the language spec, language vision, parser design, parser reference, catalog-system doc, runtime API doc, evaluator doc, and the design-round record so the published grammar is consistent everywhere.
- George's vocabulary-migration implementation remains the code baseline for `modify` / `readonly` / `editable` / `omit`; any earlier sample simplifications that split comma-separated targets are superseded by this shorthand-preservation directive.



---



---



### 2026-04-28 — v8 parser design document authored

**By:** Frank
**Status:** Complete
**Decision type:** Design document

**Decisions captured in v8:**
- OmitDeclaration is a separate construct from AccessMode with its own DisambiguationEntry `[new(TokenKind.In, [TokenKind.Omit])]`, separate AST node (2 slots only), and separate disambiguation routing.
- AccessMode DisambiguationEntry narrowed from `[Modify, Omit]` (v7) to `[Modify]` only (v8).
- FieldTargetNode is a discriminated union: abstract base + SingularFieldTarget, ListFieldTarget, AllFieldTarget sealed subtypes.
- ByLeadingToken[In] dispatches to 3 constructs (was 2 in v7): StateEnsure, AccessMode, OmitDeclaration.
- Total ConstructKinds is 12 (was 11 in v7). BuildNode switch has 12 arms.
- v7's `InScoped_RoutesToAccessMode_WhenOmitFollowsState` test was incorrect and is replaced by `InScoped_RoutesToOmitDeclaration_WhenOmitFollowsState`.
- Proposal C (when as StateAction disambiguation token) is DEFERRED — not incorporated without Shane's approval.

**Artifacts produced:**
- `docs/working/catalog-parser-design-v8.md` — primary design document, supersedes v7
- `docs/working/v8-design-session-notes.md` — change summary and decision verification matrix



---



---



v8 fixes applied per George's review: 4 targeted edits (omit guard diagnostic, stashed guard behavior, sync clarification, 2.1 split formalized). Verdict expected: APPROVED.



---



---



v8 approved after fix verification — proceed to Phase 2.

---

# George's Review of catalog-parser-design-v8.md

**By:** George (Runtime Dev)
**Date:** 2026-04-28
**Reviewing:** `docs/working/catalog-parser-design-v8.md`

---

## George's Review of catalog-parser-design-v8.md

**Verdict:** BLOCKED

---

### Check A — OmitDeclaration Split
**PASS**: All four items confirmed.
- `AccessMode` entry: `[new(TokenKind.In, [TokenKind.Modify])]` ✓ (§4 critical entries table, line 322)
- `OmitDeclaration` entry: `[new(TokenKind.In, [TokenKind.Omit])]` ✓ (§4 critical entries table, line 323)
- `ByLeadingToken[In]` theory uses `InlineData(TokenKind.In, 3)` ✓ (Slice 1.5 test spec)
- v7's wrong test `InScoped_RoutesToAccessMode_WhenOmitFollowsState` explicitly replaced with `InScoped_RoutesToOmitDeclaration_WhenOmitFollowsState` ✓ (§ Slice 4.4, with full corrected test body shown)

---

### Check B — omit guard prohibition
**FAIL**: Structural slot is correct but diagnostic test coverage is incomplete.

- `OmitDeclaration` has NO `GuardClause` slot: ✓ confirmed (`[SlotStateTarget, SlotFieldTarget]` — 2 slots, no guard; §1 Slot Sequences, §3 `OmitDeclarationNode`)
- Test for `in State omit Field when Guard` → diagnostic: **MISSING**

`ParseOmit_NeverHasGuard` (Slice 4.4) checks only that "result node has NO Guard property at all (structural impossibility)." This verifies the happy-path AST shape, but does NOT assert that parsing `in State omit Field when Guard` emits a diagnostic. Those are two different behaviors — one tests correct output, the other tests incorrect input detection.

**Additionally**, the stashed-guard + OmitDeclaration case is unaddressed: `in State when Guard omit Field` — the generic disambiguator (Slice 4.1 step 3) pre-consumes an optional `when` guard before peeking the disambiguation token. If it stashes a guard and routes to `OmitDeclaration`, Slice 4.2 step 4 says "Inject stashed guard into GuardClause slot index (if present)." There is no GuardClause slot in `OmitDeclaration`. The spec says "if present" which implies silent discard — but this is a permanently-locked language invariant. A stashed guard being silently discarded when routed to `OmitDeclaration` is a diagnostic gap that needs to be explicitly specified and tested.

**Two concrete fixes needed:**
1. Add `ParseOmit_WithGuard_PostField_EmitsDiagnostic` test spec: `in Closed omit Amount when Active` → diagnostic.
2. Specify behavior in Slice 4.2 when stashed guard cannot be injected because routed construct has no GuardClause slot. Either: (a) emit a named diagnostic, or (b) explicitly document "silent discard" as acceptable. Pick one and add a test.

---

### Check C — FieldTargetNode DU
**PASS**: Correct abstract base + 3 sealed subtypes, specified with full C# signatures in §3.
- `abstract record FieldTargetNode(SourceSpan Span) : SyntaxNode(Span)` ✓
- `sealed record SingularFieldTarget(SourceSpan Span, Token Name)` ✓
- `sealed record ListFieldTarget(SourceSpan Span, ImmutableArray<Token> Names)` ✓
- `sealed record AllFieldTarget(SourceSpan Span, Token AllToken)` ✓
- `ParseFieldTarget()` spec returns correct DU subtype based on token shape ✓ (Slice 4.3)

---

### Check D — comma-separated shorthand
**PASS**: All 9 grammar forms explicitly enumerated in §2. Test coverage:
- 6 `modify` forms tested individually (Singular×2, List×2, All×2 for readonly/editable) ✓
- 3 `omit` forms tested individually (Singular, List, All) ✓
- Comma-separated list tests: `ParseAccessMode_ListReadonly`, `ParseAccessMode_ListEditable`, `ParseOmit_List` ✓
- `all` tests: `ParseAccessMode_AllReadonly`, `ParseAccessMode_AllEditable`, `ParseOmit_All` ✓

---

### Check E — sync tokens
**PASS** (with implementation clarity concern): §1 correctly documents `modify` and `omit` as recovery anchors within `in`-scoped parse failures, and `ErrorSync_SyncSetIncludesModifyAndOmit` test is present.

However, the `SyncToNextDeclaration()` implementation shown in Slice 5.4 only loops on `Constructs.LeadingTokens`:

```csharp
if (Constructs.LeadingTokens.Contains(Current().Kind))
    return;
```

`modify` and `omit` are NOT in `LeadingTokens` — they are disambiguation tokens that appear AFTER `in`. The spec says they "serve as recovery anchors within `in`-scoped parse failures" but the shown sync loop has no path to check them. The implementation mechanism is not shown: is it a secondary check in the disambiguator itself? A supplementary token set passed to sync? This is implementable but under-specified. Frank should document the concrete mechanism or accept that this test verifies disambiguator-level recovery, not `SyncToNextDeclaration()`.

This doesn't block the design (it's a real limitation of the shown code snippet) but the test name `ErrorSync_SyncSetIncludesModifyAndOmit` will be misleading if modify/omit are handled by the disambiguator rather than by the sync function. **Recommend clarifying the mechanism in Slice 5.4.**

---

### Check F — Proposal C
**PASS**: §8 explicitly marks Proposal C (`when` as `StateAction` disambiguation token) as DEFERRED/OPEN, "explicitly NOT incorporated in v8." Shane has not approved it. ✓

---

### Slice Sizing

**Slice 1.4** (~140 lines): FITS. Full `GetMeta()` rewrite for 12 constructs is a big switch body but structurally uniform. No split needed — each case is roughly the same pattern. As long as the implementer keeps local slot-index constants per arm, this is manageable in one context window.

**Slice 2.1** (~220 lines): **SPLIT NEEDED.** 220 lines across 15+ files in a new directory with base types + DU + 12 concrete nodes is over the comfort threshold. Frank already proposed the right split:
- **2.1a** (~80 lines): Base types (`SyntaxNode`, `Declaration`, `Expression`, shared nodes), plus `FieldTargetNode` DU (abstract + 3 sealed subtypes). The DU needs to be visible before anything references it.
- **2.1b** (~140 lines): All 12 concrete `Declaration` subtypes.

This split respects the dependency: `AccessModeNode` and `OmitDeclarationNode` reference `FieldTargetNode`, so 2.1a must complete before 2.1b.

**Slice 3.1** (~100–150 lines): FITS. Pratt parser is the most intellectually dense piece, but 100–150 lines for the full implementation is realistic for a well-scoped method. The vocabulary dictionary is already wired in Slice 2.2. No split needed.

**No other slices are oversized** — Slice 3.2 is ~120 lines but spread across 9 simple parsing methods with uniform patterns (peek, consume, return). That's fine.

---

### Feasibility Issues

1. **Slice 4.2: stashed guard + OmitDeclaration (BLOCKING)**: The generic disambiguator pre-consumes a `when` guard before routing. For `OmitDeclaration`, there is no GuardClause slot to inject into. The spec says "if present" (silent discard) but this is a permanently-locked invariant violation that should produce a diagnostic. The behavior must be explicitly specified before implementation — a developer cannot make this decision unilaterally. This ties directly to Check B item 2.

2. **Slice 5.4: modify/omit recovery mechanism (implementation clarity)**: `SyncToNextDeclaration()` as written loops only on `Constructs.LeadingTokens`. The spec claims `modify` and `omit` serve as recovery anchors within `in`-scoped failures, but the implementation path is not shown. Is this a secondary check in `ParseInScopedConstruct()` itself? Or does `SyncToNextDeclaration()` accept supplementary tokens? Needs one more sentence of spec. Not blocking on its own, but the test `ErrorSync_SyncSetIncludesModifyAndOmit` will be hard to implement without it.

3. **Slice 2.1 split**: Frank's suggested 2.1a/2.1b split is correct but not formally part of the plan — it's described as a "consider" option. Given 220 lines, I'm recommending it be formalized. Not blocking but should be resolved before sprint starts.

---

### Test Spec Gaps

1. **(BLOCKING) `in State omit Field when Guard` → diagnostic not tested**: No Soup Nazi spec anywhere tests that a post-field `when` on an omit declaration emits a diagnostic. `ParseOmit_NeverHasGuard` only tests structural shape. Need: `ParseOmit_WithPostFieldGuard_EmitsDiagnostic`.

2. **(BLOCKING) `in State when Guard omit Field` → stashed guard + no injection slot**: The pre-field guard + omit routing path has no specified behavior and no test. Need: `ParseOmit_WithPreFieldGuard_EmitsDiagnosticAndDiscards` (or equivalent — but the behavior must be decided first per Feasibility Issue #1).

3. **(Minor) Slice 4.2 tests don't cover the OmitDeclaration injection path**: `GuardInjection_StashedGuard_LandsInCorrectSlot` only tests `StateEnsure`. A companion test covering OmitDeclaration (no injection slot path) would prevent silent discard from being accidentally removed.

4. **(Minor) Disambiguation routing tests are type-assertion based** ✓ (e.g., `BeOfType<OmitDeclarationNode>()`). No gap here — this is correct.

---

### BuildNode Completeness
**PASS**: 12 arms confirmed, with explicit `OmitDeclaration` arm shown:
```csharp
ConstructKind.OmitDeclaration => new OmitDeclarationNode(span,
    (StateTargetNode)slots[0]!, (FieldTargetNode)slots[1]!),
```
Total arm count is 12, matching total `ConstructKind` count. Wildcard `_` arm with `ArgumentOutOfRangeException` present ✓.

---

### Summary

v8 is substantially correct — the OmitDeclaration split is clean, the FieldTargetNode DU is architecturally sound, disambiguation entries are properly separated, and test coverage for all 9 grammar forms is solid. Two blocking gaps: (1) there is no diagnostic test for guard-attempted-on-omit (the invariant is stated but not verified at the parse-input level), and (2) the behavior when a pre-consumed `when` guard is stashed and then routed to OmitDeclaration (which has no GuardClause slot) is completely unspecified — this is an implementation corner case that a developer cannot resolve independently.

---

## Items Frank Must Fix Before Phase 2

1. **Add `ParseOmit_WithPostFieldGuard_EmitsDiagnostic` to Slice 4.4 Soup Nazi spec**: Parse `"in Closed omit Amount when Active"` → assert diagnostic emitted (specify the diagnostic code — either a new `OmitDoesNotSupportGuard` or reuse an existing code).

2. **Specify Slice 4.2 behavior when stashed guard + OmitDeclaration (no GuardClause slot)**: Document whether the stashed guard is (a) silently discarded with a diagnostic, or (b) silently discarded without a diagnostic. Then add a corresponding Soup Nazi test: `ParseOmit_WithPreFieldGuard_EmitsDiagnosticAndParses` or `ParseOmit_WithPreFieldGuard_DiscardsGuardSilently` (but option (a) is strongly preferred — this is an invariant violation).

3. **(Recommended, not strictly blocking) Clarify Slice 5.4 sync mechanism**: Add one sentence explaining HOW `modify` and `omit` serve as within-`in`-block recovery anchors. The shown `SyncToNextDeclaration()` loop doesn't include them; the actual mechanism (secondary check in the disambiguator? supplementary token set?) needs to be named.

4. **(Recommended) Formalize Slice 2.1a/2.1b split**: Change the "consider" language to a firm split. The 220-line single-slice is above threshold.



---



---



### 2026-04-28 — Phase 2 decisions audit complete

**By:** Frank
**Status:** Complete
**Decision type:** Audit

**Category of fixes:** Documentation dispatch tables and AST sections in `docs/compiler/parser.md` and `docs/language/precept-language-spec.md` still treated `OmitDeclaration` as part of the `AccessMode` construct. 9 targeted edits applied to align both files with the locked decisions: `OmitDeclaration` is a separate `ConstructKind` with its own disambiguation entry, AST node, and 2-slot sequence (no guard). Source catalog files were already correct.

**Audit artifact:** `docs/working/audit-decisions-notes.md`



---



---



### 2026-04-28T23:04:41Z: User directive — spike mode constraints
**By:** Shane (via Copilot)
**What:** While on the `precept-architecture` spike branch, no new branches and no PRs are to be created. All commits go directly to `precept-architecture`. Agents must never run `git checkout -b` or `gh pr create` during a spike session.
**Why:** User request — spike branches are exploratory; PRs and sub-branches add noise and process overhead that doesn't belong in a spike.

---

# Deep Re-Review: Catalog Extensibility CS8509 Enforcement

**Reviewer:** Frank (Lead/Architect)
**Date:** 2026-04-28
**Branch:** `feature/catalog-extensibility`
**Prior verdict:** APPROVED (frank-george-deviation-review.md)
**Revised verdict:** **BLOCKED** — 5 wildcard gaps defeat CS8509 enforcement

---

## Scope of re-review

The plan's central goal:

> When a developer adds a new `ConstructKind`, `ActionKind`, `ActionSyntaxShape`, or `RoutingFamily` to the catalog, the compiler must produce **CS8509 errors** at every location that needs updating. No silent gaps. No runtime-only throws.

I re-examined every switch expression in the implementation, not just the two originally reported deviations.

---

## What IS correct (confirmed)

| Switch | File:Line | CS8509 intact? | Notes |
|--------|-----------|----------------|-------|
| `BuildNode()` | Parser.cs:1315–1378 | ✅ | All 12 `ConstructKind` arms, no wildcard, `#pragma CS8524` only |
| `DisambiguateAndParse()` EventScoped | Parser.cs:239–252 | ✅ | All `ConstructKind` arms listed explicitly, no wildcard |
| `DisambiguateAndParse()` StateScoped | Parser.cs:272–286 | ✅ | All `ConstructKind` arms listed explicitly, no wildcard |
| `ParseActionStatement()` outer | Parser.cs:612–620 | ✅ | All 4 `ActionSyntaxShape` arms + explicit `None` throw, no wildcard |

These four switches achieve the plan's goal. Adding a new named enum member triggers CS8509.

---

## What is BROKEN — 5 gaps

### Gap 1–4: Inner `ActionKind` switches use `_ => throw`

**Plan requirement (Slice 5, explicitly):**

> Inner switch on `ActionKind` inside each shape handler (**no default**): fires when a new ActionKind is added with an existing shape but no node constructor.

The plan's code examples showed `// No default — CS8509 fires when new [shape] ActionKind added` on every inner switch.

**Actual implementation:**

| # | Method | File:Line | Pattern | CS8509? |
|---|--------|-----------|---------|---------|
| 1 | `ParseAssignValueStatement` | Parser.cs:631–635 | `_ => throw new InvalidOperationException(...)` | ❌ suppressed |
| 2 | `ParseCollectionValueStatement` | Parser.cs:644–651 | `_ => throw new InvalidOperationException(...)` | ❌ suppressed |
| 3 | `ParseCollectionIntoStatement` | Parser.cs:666–671 | `_ => throw new InvalidOperationException(...)` | ❌ suppressed |
| 4 | `ParseFieldOnlyStatement` | Parser.cs:679–683 | `_ => throw new InvalidOperationException(...)` | ❌ suppressed |

**Failure scenario:** Add `ActionKind.Increment` with `SyntaxShape = AssignValue` to the catalog. The outer `ActionSyntaxShape` switch routes it correctly to `ParseAssignValueStatement`. But the inner `ActionKind` switch's `_ =>` wildcard catches it — producing a runtime `InvalidOperationException` instead of a CS8509 compile error. The developer gets no compiler guidance.

**This directly defeats Gap 7 from the plan** ("ActionKind↔Statement node enforcement — Covered by gap 4 fix: ParseActionStatement CS8509 forces one arm per ActionKind").

**Fix:** Remove all four `_ => throw` arms. Add `#pragma warning disable CS8524` / `restore` around each inner switch (same pattern as BuildNode and the outer switch). Each inner switch already lists every `ActionKind` that belongs to its shape — the wildcard is purely redundant defensive code.

### Gap 5: `InvokeSlotParser` uses `_ => throw`

| # | Method | File:Line | Pattern | CS8509? |
|---|--------|-----------|---------|---------|
| 5 | `InvokeSlotParser` | Parser.cs:845–868 | `_ => throw new ArgumentOutOfRangeException(...)` | ❌ suppressed |

**The code comments and test are misleading:**

- Line 864: `// CS8509 enforcement: a new ConstructSlotKind member without an arm is a build error.`
- Line 866: `_ => throw` — **this wildcard makes the comment false.** `_ =>` covers all remaining patterns including named members. CS8509 does NOT fire.
- Test `InvokeSlotParser_SwitchIsExhaustive` (ParserInfrastructureTests.cs:90–97) checks the member **count** (17) — this is a fragile fallback, not CS8509 enforcement.

**Failure scenario:** Add `ConstructSlotKind.ConditionalBlock = 17`. The `_ =>` wildcard catches it at runtime. The count-based test catches it only if someone remembers to update the magic number. Neither is a CS8509 build error.

**Scope consideration:** `ConstructSlotKind` is not one of the four enums named in the plan's goal statement. However, the implementation itself claims CS8509 enforcement via code comments and a test named `InvokeSlotParser_SwitchIsExhaustive`. If the team chose to use test enforcement instead of CS8509 here, the comment and test name are misleading. Either way, this needs resolution.

**Fix:** Remove the `_ => throw` arm. Add `#pragma warning disable CS8524` / `restore` around the switch. All 17 `ConstructSlotKind` members already have arms. Update or remove the count-based test (it becomes redundant once CS8509 is the actual enforcement mechanism).

---

## How my prior review went wrong

In my original review, I caught myself mid-analysis:

> Wait — correction: `_ =>` in a switch expression **does** suppress CS8509 because it covers all remaining patterns including named members.

I then dismissed this because `InvokeSlotParser` operates on `ConstructSlotKind`, not the two deviating enums I was reviewing. This was correct but insufficient — I should have then asked: "Are there OTHER wildcards on enums that ARE in scope?" I never examined the four inner `ActionKind` switches. They are **squarely within Slice 5's scope** and the plan explicitly required no defaults. I failed to check whether the implementation matched the plan on those switches.

---

## Verdict: BLOCKED

**5 switches must be fixed before merge:**

1. `ParseAssignValueStatement` (Parser.cs:634) — remove `_ => throw`, add `#pragma CS8524`
2. `ParseCollectionValueStatement` (Parser.cs:650) — remove `_ => throw`, add `#pragma CS8524`
3. `ParseCollectionIntoStatement` (Parser.cs:670) — remove `_ => throw`, add `#pragma CS8524`
4. `ParseFieldOnlyStatement` (Parser.cs:682) — remove `_ => throw`, add `#pragma CS8524`
5. `InvokeSlotParser` (Parser.cs:866) — remove `_ => throw`, add `#pragma CS8524`, fix misleading comment

**Gaps 1–4 are plan violations** — Slice 5 explicitly required inner `ActionKind` switches with no default arm. The implementation has wildcards on all four.

**Gap 5 is a correctness/honesty issue** — the code claims CS8509 enforcement but the wildcard defeats it. Whether it's in formal plan scope or not, the misleading comment must be resolved.

After these fixes, every catalog enum switch in the parser will use the same pattern: explicit arms for all named members, `#pragma CS8524` to suppress unnamed-integer noise, no wildcard. CS8509 will fire on every new enum member. The plan's central goal will be achieved.

---

# Enum Deviation Review — `feature/catalog-extensibility`

**From:** Frank (Lead/Architect)  
**Date:** 2026-04-28  
**Re:** Two deviations reported by George post-implementation

---

## 1. `ActionSyntaxShape.None` — **UNDERMINES CS8509**

### What George did
Added `None = 0` to `ActionSyntaxShape` and added a corresponding `ActionSyntaxShape.None => throw new InvalidOperationException(...)` arm to the outer switch in `ParseActionStatement`.

### Why it matters
The plan required the outer `ActionSyntaxShape` switch and the inner `ActionKind` switches to have **no** default or wildcard arms — pure CS8509 enforcement. The `None` arm creates an escape hatch that defeats this.

Here is the exact failure mode:

1. Developer adds `ActionKind.Foo = 9`.
2. They forget to set a real `SyntaxShape` — either because they copy an incomplete stub or because `None = 0` is now a valid-looking named value that default-initializes silently.
3. The outer `ActionSyntaxShape` switch in `ParseActionStatement` hits `ActionSyntaxShape.None => throw`. That arm is satisfied. Execution terminates at runtime.
4. The inner per-shape `ActionKind` switches (`ParseAssignValueStatement`, `ParseCollectionValueStatement`, etc.) are **never reached**.
5. CS8509 fires at **none** of them — the new `ActionKind.Foo` slips the net entirely until runtime.

The plan's purpose was to make that slip **impossible at compile time**. George's `None` sentinel converts a compile-time hard error into a runtime exception. That is the wrong direction.

### Unreported companion deviation (B3)
While inspecting the inner switches, I found that George also used `_ => throw` wildcards in `ParseAssignValueStatement` and `ParseCollectionValueStatement`:

```csharp
// ParseAssignValueStatement — line ~632
ActionKind.Set => new SetStatement(span, field, value),
_ => throw new InvalidOperationException(...),

// ParseCollectionValueStatement — line ~643
ActionKind.Add => ..., ActionKind.Remove => ..., ActionKind.Enqueue => ..., ActionKind.Push => ...,
_ => throw new InvalidOperationException(...),
```

The `_` wildcard suppresses CS8509. A developer adding `ActionKind.Foo` with `SyntaxShape = AssignValue` would NOT get a compile-time error at `ParseAssignValueStatement` — it falls through to `_ => throw` silently at runtime. George did **not** report this deviation. It is the structural companion to the `None` problem and must be fixed at the same time.

(Note: George correctly used explicit wrong-family throw arms in the `DisambiguateAndParse` switches — lines 243–249 and 279–283 list every non-matching `ConstructKind` explicitly. The same discipline must be applied to the inner `ActionKind` switches.)

### Required fixes
- **B1:** Remove `None = 0` from `ActionSyntaxShape`. The enum must have exactly four members: `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly` — values 1 through 4, or leave C# auto-assign them starting from 1 if no explicit numbering is needed.
- **B2:** Remove the `ActionSyntaxShape.None => throw` arm from the `ParseActionStatement` outer switch. The switch must cover only the four real shapes with no wildcard.
- **B3:** Replace `_ => throw` wildcards in `ParseAssignValueStatement` and `ParseCollectionValueStatement` (and any other inner `ActionKind` switches that use a wildcard) with exhaustive explicit-arm patterns. For each shape-dispatch method, list every `ActionKind` that **does not** belong to that shape as an explicit `=> throw new InvalidOperationException(...)` arm. This is exactly how `DisambiguateAndParse` was implemented — the same pattern must apply here.

---

## 2. `RoutingFamily.None` — **SAFE**

### What George did
Added `None = 0` to `RoutingFamily`.

### Analysis
`RoutingFamily` is **never switched on** in `Parser.cs`. The parser routes constructs via `FindDisambiguatedConstruct` using token kinds from `DisambiguationEntry` — `RoutingFamily` is read from catalog metadata to guide routing logic but is not itself the discriminant of any switch expression in production code.

The test `Constructs_RoutingFamily_AllMembersHaveValue` correctly validates:

```csharp
meta.RoutingFamily.Should().NotBe((RoutingFamily)0,
    $"{kind} must have a non-default RoutingFamily");
```

This catches any `ConstructKind` that ships without a valid `RoutingFamily` — including any developer who accidentally leaves the field default-initialized. The enforcement is at test time, not compile time, but that is acceptable because `RoutingFamily` carries no CS8509-enforced switch in the parser.

`RoutingFamily.None` is a design smell — a sentinel for a non-value — and I would prefer it not exist. But it does not undermine CS8509 enforcement in production code. **No required fix before merge.** George may clean it up in a follow-on if desired.

---

## 3. `#pragma warning disable CS8524` — **SAFE**

### What George did
Added `#pragma warning disable CS8524` / `#pragma warning restore CS8524` pairs around four exhaustive switch expressions.

### Analysis
CS8524 and CS8509 are **distinct diagnostics**:

- **CS8509** fires when a **named** enum member is absent from a switch expression. This is the enforcement diagnostic for the catalog extensibility plan.
- **CS8524** fires when an **unnamed** raw integer value (e.g., `(ConstructKind)99`) could potentially reach a switch expression that does not have a catch-all arm.

A `#pragma disable CS8524` does **not** suppress CS8509. Both can be active simultaneously and independently.

All four pragmas are **tightly scoped** — each `disable` is immediately followed by a `restore` after the switch expression closes:

| Block | Lines | Switch |
|-------|-------|--------|
| 1 | 237–251 | EventScoped `FindDisambiguatedConstruct` switch |
| 2 | 270–285 | StateScoped `FindDisambiguatedConstruct` switch |
| 3 | 610–620 | `ActionSyntaxShape` switch in `ParseActionStatement` |
| 4 | 1313–1378 | `BuildNode` 12-arm `ConstructKind` switch |

No pragma spans a file boundary or suppresses anything beyond the intended switch. CS8509 is active at all four sites. George's explanation is technically correct.

---

## 4. Overall Verdict — **BLOCKED**

The branch does not merge until George resolves the following numbered items. **No exceptions.**

**B1:** Remove `None = 0` from `ActionSyntaxShape`. Enum must have exactly: `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly`.

**B2:** Remove the `ActionSyntaxShape.None => throw` arm from the outer switch in `ParseActionStatement`. Outer switch must be a clean 4-arm exhaustive switch, no sentinel handling.

**B3:** Replace `_ => throw` wildcard arms in the inner `ActionKind` switches (`ParseAssignValueStatement`, `ParseCollectionValueStatement`, and any other inner dispatch methods that switch on `ActionKind`) with exhaustive explicit-arm patterns — listing every `ActionKind` value that does not belong to that shape as a named throw arm. CS8509 must fire at these switches when a new `ActionKind` is added.

---

**`RoutingFamily.None` does not block merge.  
`#pragma disable CS8524` does not block merge.**

Fix B1–B3, push, and request re-review.



---



---



## Frank — Final Re-Review Verdict

**Date:** 2026-04-28  
**Branch:** feature/catalog-extensibility  
**Commit reviewed:** 5e5b2f958b041f199c7360c79feb49f6c7e02ba4

---

### Verdict: APPROVED

---

### Findings

Every blocking item from both prior review documents is closed.

**B1 — `ActionSyntaxShape.None = 0` removed:** ✅  
`ActionSyntaxShape` now has exactly 4 members with no explicit integer assignments:
```csharp
public enum ActionSyntaxShape { AssignValue, CollectionValue, CollectionInto, FieldOnly }
```
C# auto-assigns 0–3. No sentinel. Note: `AssignValue = 0` now occupies the zero slot formerly held by `None`. This means a default-initialized `ActionMeta.SyntaxShape` silently resolves to `AssignValue` rather than an obvious error sentinel. This is a minor structural note — it doesn't undermine CS8509 enforcement (all 8 catalog entries explicitly declare their `SyntaxShape`, and no route through the outer switch bypasses the inner switches), but it's worth recording: if a future `ActionMeta` entry is added with `SyntaxShape` accidentally omitted from the constructor, the `Enum.IsDefined` test will pass silently because `AssignValue = 0` is defined. **Not a blocker**, but the team should remain aware that `None = 0`'s safety-net role is not fully replicated by the current arrangement.

**B2 — `ActionSyntaxShape.None` arm removed from outer switch:** ✅  
`ParseActionStatement` outer switch is a clean 4-arm exhaustive switch:
```csharp
return meta.SyntaxShape switch
{
    ActionSyntaxShape.AssignValue     => ParseAssignValueStatement(meta),
    ActionSyntaxShape.CollectionValue => ParseCollectionValueStatement(meta),
    ActionSyntaxShape.CollectionInto  => ParseCollectionIntoStatement(meta),
    ActionSyntaxShape.FieldOnly       => ParseFieldOnlyStatement(meta),
};
```
`#pragma CS8524` tightly scoped. No wildcard. No sentinel arm.

**B3 — `ParseAssignValueStatement` inner switch:** ✅  
All 8 `ActionKind` members covered with explicit named arms. `Set` is the valid arm; `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop`, `Clear` each throw with identity-specific messages. No wildcard. `#pragma CS8524` tightly scoped.

**B4 — `ParseCollectionValueStatement` inner switch:** ✅  
All 8 `ActionKind` members covered. `Add`, `Remove`, `Enqueue`, `Push` are valid; `Set`, `Dequeue`, `Pop`, `Clear` throw. No wildcard. `#pragma CS8524` tightly scoped.

**B5 — `ParseCollectionIntoStatement` inner switch:** ✅  
All 8 `ActionKind` members covered. `Dequeue`, `Pop` are valid; `Set`, `Add`, `Remove`, `Enqueue`, `Push`, `Clear` throw. No wildcard. `#pragma CS8524` tightly scoped.

**B6 — `ParseFieldOnlyStatement` inner switch:** ✅  
All 8 `ActionKind` members covered. `Clear` is the valid arm; `Set`, `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop` throw. No wildcard. `#pragma CS8524` tightly scoped.

**B7 — `InvokeSlotParser`:** ✅  
`_ => throw` removed. All 17 `ConstructSlotKind` members have explicit named arms. `#pragma CS8524` tightly scoped. Comment updated to accurately state "CS8509 enforces named-value coverage here; #pragma CS8524 suppresses unnamed-integer noise" — no longer a false claim.

**Test fix — `Actions_ActionSyntaxShape_AllMembersHaveValue`:** ✅  
`NotBe((ActionSyntaxShape)0)` replaced with `Enum.IsDefined(meta.SyntaxShape).Should().BeTrue(...)`. This is the correct assertion now that no `None = 0` sentinel exists to distinguish "unset" from the first real member. The test guards against raw integer values outside the defined set.

**Actions.cs — 8 ActionMeta entries:** ✅  
All 8 `ActionKind` entries carry a non-zero, explicitly-set `SyntaxShape`: `Set → AssignValue`, `Add/Remove/Enqueue/Push → CollectionValue`, `Dequeue/Pop → CollectionInto`, `Clear → FieldOnly`. No entry is missing a shape declaration.

---

### CS8509 Enforcement Status

**Confirmed. The pattern achieves the stated goal.**

The enforcement chain is structurally sound:

1. A developer adds `ActionKind.Increment = 8` to the enum and declares `SyntaxShape = AssignValue` on the new `ActionMeta`.
2. The outer `ActionSyntaxShape` switch routes to `ParseAssignValueStatement`.
3. The inner `ActionKind` switch in `ParseAssignValueStatement` has no arm for `ActionKind.Increment`. **CS8509 fires. Build fails.**
4. The developer must add the arm before the branch compiles.

The same chain holds for all 4 inner switches depending on which `SyntaxShape` the new kind declares. CS8509 is active at all four sites because no wildcard suppresses it. `#pragma CS8524` suppresses only CS8524 (unnamed-integer noise) and has no effect on CS8509 — the two diagnostics are independent.

The only remaining caveat is the observation under B1: if a developer adds a new `ActionMeta` and omits `SyntaxShape` entirely (relying on default initialization), it silently routes to `AssignValue` (value 0) rather than producing an obvious test failure. This is a test-time gap, not a compile-time gap, and does not affect CS8509 enforcement. The `Enum.IsDefined` test will pass in that scenario. A future hardening option is to add explicit `= 1, 2, 3, 4` numbering so 0 becomes undefined, but that is not required for this merge.

**All 7 blocking items closed. No open findings. Branch is approved for merge.**

---

# Deviation Review: George's Catalog Extensibility Implementation

**Reviewer:** Frank (Lead/Architect)
**Date:** 2026-04-28
**Branch:** `feature/catalog-extensibility`
**Verdict:** APPROVED — both deviations are safe; CS8509 enforcement is intact.

---

## Deviation 1: `None = 0` on `RoutingFamily` and `ActionSyntaxShape`

### Finding: Safe — no CS8509 gap

**`RoutingFamily`** (Construct.cs:37–53):
- `None = 0` exists as a sentinel for default-initialization detection.
- **No switch expression in the codebase dispatches on `RoutingFamily` at all.** The parser routes by `ConstructKind` (via `DisambiguateAndParse` and `BuildNode`), not by `RoutingFamily`. `RoutingFamily` is a metadata property on `ConstructMeta` — it classifies constructs for documentation and routing-table grouping, but the actual dispatch switches are on `ConstructKind`.
- Since there is no `RoutingFamily` switch, `None` cannot act as a catch-all or mask missing arms. CS8509 enforcement operates entirely through the `ConstructKind` switches, which have no `None` member and no wildcard arms (except the `_ => throw` in `GetMeta` and `InvokeSlotParser`, which are defensive guards against unnamed integer casts, not semantic catch-alls).
- Every `ConstructMeta` in `Constructs.cs` assigns a non-`None` routing family (Header, Direct, StateScoped, or EventScoped). `None` is only reachable via `default(RoutingFamily)`.

**`ActionSyntaxShape`** (Action.cs:29–41):
- `None = 0` exists as the same sentinel pattern.
- The `ParseActionStatement` switch (Parser.cs:611–621) dispatches on `ActionSyntaxShape` and includes an **explicit `ActionSyntaxShape.None => throw` arm** — it does not fall through or act as a default. It hard-throws with a diagnostic message identifying the offending `ActionKind`.
- Every `ActionMeta` in `Actions.cs` assigns a non-`None` shape. `None` is only reachable via `default(ActionSyntaxShape)`.
- Adding a new `ActionSyntaxShape` member (e.g., `ConditionalValue`) would trigger CS8509 on the `ParseActionStatement` switch because the new member would have no arm. The `None` arm does not catch it.

**Conclusion:** `None = 0` sentinels are inert. They serve test/initialization detection and do not participate in any switch dispatch path that would mask a missing arm. CS8509 enforcement is fully intact for both enum families.

---

## Deviation 2: `#pragma warning disable CS8524`

### Finding: Safe — CS8524 suppression does not affect CS8509

**The two warnings are independent:**
- **CS8509** fires when a *named* enum member has no matching arm. This is the enforcement we depend on.
- **CS8524** fires when an *unnamed* integer value (e.g., `(ConstructKind)999`) has no matching arm. This is noise for our use case.

**Evidence from Parser.cs:**
- 4 pragma-scoped regions suppress CS8524 only: lines 238–252, 271–286, 611–621, 1314–1379.
- Each pragma is tightly scoped (`disable` immediately before the switch, `restore` immediately after).
- `TreatWarningsAsErrors=true` is set in `Precept.csproj` (line 7), meaning CS8509 fires as an **error**, not a warning. Suppressing CS8524 has zero effect on CS8509 — they are separate diagnostic IDs with separate suppression state.

**Verification:** The `BuildNode` switch (Parser.cs:1315–1378) has exactly 11 arms for exactly 11 `ConstructKind` members, with no wildcard. If a 12th `ConstructKind` is added, CS8509 fires as a build error. The CS8524 pragma does not intercept this.

**The `InvokeSlotParser` switch** (Parser.cs:845–868) uses the older `_ => throw` pattern for `ConstructSlotKind`, which is also fine — the wildcard catches only unnamed integer casts, and CS8509 still fires for missing named members because the switch is an expression (not a statement).

Wait — correction: `_ =>` in a switch expression **does** suppress CS8509 because it covers all remaining patterns including named members. However, `InvokeSlotParser` switches on `ConstructSlotKind`, not one of the two deviating enums. The four `#pragma` regions all cover switches that have **no wildcard arm** — they list every named member explicitly and suppress only the unnamed-integer CS8524 noise. This is exactly the correct pattern.

---

## Summary

| Deviation | Safe? | Reason |
|-----------|-------|--------|
| `None = 0` on `RoutingFamily` | ✅ | No switch dispatches on `RoutingFamily`; sentinel is metadata-only |
| `None = 0` on `ActionSyntaxShape` | ✅ | Explicit `None => throw` arm; does not mask new members |
| `#pragma disable CS8524` | ✅ | Independent from CS8509; tightly scoped; `TreatWarningsAsErrors` makes CS8509 a build error |

**The catalog extensibility contract is intact:** adding a new `ConstructKind` or `ActionKind` (or `ActionSyntaxShape` / `RoutingFamily`) member produces CS8509 build errors at every incomplete switch. George's deviations are structurally sound.

---

# PRECEPT0018 — Semantic Enum Zero-Slot Analyzer

**Author:** Frank (Code Reviewer)
**Date:** 2026-04-28
**Status:** Ready for implementation
**Implementer:** George

---

## 1. Feasibility

**Straightforward.** This is a textbook `SymbolAction` analyzer on `SymbolKind.NamedType` filtered to enums. The Roslyn `IFieldSymbol.ConstantValue` API gives direct access to the underlying integer value of each member. No control-flow analysis, no cross-compilation lookups, no semantic model gymnastics.

The existing `src/Precept.Analyzers/` project already targets `netstandard2.0` with `Microsoft.CodeAnalysis.CSharp 5.3.0` and is wired into `src/Precept/Precept.csproj` via `<ProjectReference OutputItemType="Analyzer">`. Infrastructure cost: zero.

**Diagnostic ID:** `PRECEPT0018`

---

## 2. The Rule

### What triggers a violation

An enum member resolves to integer value `0` AND does not meet any of the exemption criteria below.

Precisely: for every `enum` type declaration where the containing namespace starts with `Precept`, iterate its `IFieldSymbol` members with `HasConstantValue == true`. If any member's `ConstantValue` converts to `0L` (after widening to `long`), and no exemption applies, report `PRECEPT0018` on that member.

### Exemption criteria (in evaluation order)

| # | Condition | Rationale |
|---|-----------|-----------|
| E1 | The enum has `[System.Flags]` | Flags enums require `None = 0` by design. Standard C# pattern. |
| E2 | The member is named exactly `None` | Universal .NET sentinel convention. `None = 0` is structural — it means "no value assigned." |
| E3 | The member has `[AllowZeroDefault]` | Explicit opt-out for intentional semantic defaults where zero-init is correct by design. |

**That's it.** Three exemptions. No name allowlists for `Any`, `Normal`, `Default`, `Unknown`, etc. Those must use `[AllowZeroDefault]` — see § 3 for justification.

### Scope

- **Checked:** All enums in any namespace starting with `Precept` (covers `Precept.Language`, `Precept.Runtime`, `Precept.Pipeline`, etc.).
- **Not checked:** Test assemblies, third-party code, namespaces outside `Precept.*`.
- **Visibility:** All access levels — `public`, `internal`, `private`. The `LexerMode` enum is `private` and still needs protection. The silent-default risk is the same regardless of visibility.

### `[Flags]` enums

Auto-exempted entirely (E1). The analyzer skips them — it does not inspect individual members. Currently only `TypeTrait` is `[Flags]` in the codebase.

---

## 3. Opt-Out Mechanism

**Recommended:** `[AllowZeroDefault]` attribute on the member, with `[Flags]` and `None`-named auto-exemptions.

### Why not the alternatives

| Option | Verdict | Reason |
|--------|---------|--------|
| **Name allowlist** (`None`, `Unknown`, `Any`, `Normal`, `Default`, …) | ❌ Rejected | Brittle. Every new sentinel-like name requires a code change to the analyzer. `InState` and `Ensure` are not sentinel-sounding but sit at zero intentionally in some contexts. The allowlist either grows unbounded or misses cases. |
| **`[SemanticEnum]` on the enum** (opt-in) | ❌ Rejected | Inverts the safety default. Unannotated enums are unchecked — which means every new enum is unprotected until someone remembers to add the attribute. The whole point of this analyzer is to prevent *forgetting*. |
| **`[SuppressZeroDefault]` on the member** | ❌ Rejected | Semantically identical to `[AllowZeroDefault]` but with a confusing double-negative name. "Suppress the zero-default diagnostic" vs. "Allow zero as the default" — the latter reads as intent, the former reads as workaround. |
| **`[AllowZeroDefault]` on the member** | ✅ Selected | See below. |

### Why `[AllowZeroDefault]`

1. **Safe by default.** Every enum is checked. You must explicitly opt out — the dangerous path requires a conscious decision.
2. **Self-documenting.** The attribute at the declaration site says "yes, zero-init is intentional here" — future readers don't have to reconstruct why.
3. **No magic lists.** The only auto-exempted name is `None`, which is the universal .NET convention. Everything else requires explicit annotation.
4. **Minimal noise.** Only 3 existing enums need the attribute: `LexerMode.Normal`, `QualifierMatch.Any`, `PeriodDimension.Any`. That's 3 one-line annotations across the entire codebase.
5. **Consistent with project philosophy.** Precept's design principle is "make invalid states structurally impossible." An opt-out attribute is the structural version of that principle applied to the analyzer itself.

### Why `None` gets auto-exempted (not just the attribute)

`None = 0` is a .NET ecosystem convention with decades of usage. Requiring `[AllowZeroDefault]` on every `None` member would be pure ceremony — nobody has ever accidentally named a member `None` when they meant it to be semantically meaningful. The auto-exemption eliminates ~3 annotations (`RoutingFamily.None`, `GraphAnalysisKind.None`, `QualifierAxis.None`) with zero false-negative risk.

---

## 4. Placement

### Analyzer class

**File:** `src/Precept.Analyzers/Precept0018SemanticEnumZeroSlot.cs`

Lives alongside the existing 17 analyzers. No new project needed.

### Attribute class

**File:** `src/Precept/AllowZeroDefaultAttribute.cs`

```csharp
namespace Precept;

/// <summary>
/// Suppresses PRECEPT0018 for an enum member at value 0.
/// Apply this when zero-initialization is intentional — e.g., a "don't-care" default
/// or a documented initial state. The attribute signals that default(T) routing to
/// this member was a deliberate design choice, not an accident.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class AllowZeroDefaultAttribute : Attribute { }
```

**Why `AttributeTargets.Field`:** Enum members are fields in the CLR model. `AttributeTargets.Field` is the correct target. (There is no `AttributeTargets.EnumMember`.)

### No csproj changes needed

- `src/Precept.Analyzers/Precept.Analyzers.csproj` — no changes. The project already has the Roslyn reference and build configuration.
- `src/Precept/Precept.csproj` — no changes. The `<ProjectReference>` to Analyzers is already in place.
- `test/Precept.Analyzers.Tests/` — no changes to the test project file (it already references the analyzer project).

---

## 5. Diagnostic Output

### Message format

```
PRECEPT0018: Enum member '{0}' in '{1}' has value 0. Semantic enums must use explicit
1-based values so default(T) throws instead of silently routing. Assign '{0} = 1'
(and renumber subsequent members) or mark with [AllowZeroDefault] if zero-init is intentional.
```

**Substitutions:**
- `{0}` = member name (e.g., `AssignValue`)
- `{1}` = enum name (e.g., `ActionSyntaxShape`)

### Descriptor

```csharp
private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    title: "Enum member at value 0 in semantic enum",
    messageFormat: "Enum member '{0}' in '{1}' has value 0 — semantic enums must use explicit " +
                   "1-based values so default(T) throws instead of silently routing. " +
                   "Assign '{0} = 1' (and renumber subsequent members) or mark with [AllowZeroDefault] " +
                   "if zero-init is intentional.",
    category: "Precept.Language",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Every enum where all members are semantically meaningful must leave the zero " +
                 "slot unnamed. default(T) = (T)0 = unnamed = SwitchExpressionException rather " +
                 "than silent routing to an arbitrary first member. Enums with None = 0, " +
                 "[Flags] enums, and members marked [AllowZeroDefault] are exempt.");
```

**Severity: Error.** The existing project uses `TreatWarningsAsErrors`, so Warning would also block the build. But this is a correctness invariant — it deserves Error severity to match the other PRECEPT analyzers.

---

## 6. Implementation Guide

### File inventory

| File | Action | Description |
|------|--------|-------------|
| `src/Precept/AllowZeroDefaultAttribute.cs` | **Create** | The opt-out attribute |
| `src/Precept.Analyzers/Precept0018SemanticEnumZeroSlot.cs` | **Create** | The analyzer |
| `test/Precept.Analyzers.Tests/Precept0018Tests.cs` | **Create** | Test cases |

### Analyzer skeleton

```csharp
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0018 — Enum members at value 0 in Precept.* namespaces must be either:
/// (a) named "None" (structural sentinel), (b) in a [Flags] enum, or
/// (c) marked with [AllowZeroDefault]. All other zero-valued members are flagged
/// because default(T) silently routes to them.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0018SemanticEnumZeroSlot : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0018";

    private static readonly DiagnosticDescriptor Rule = new( /* see § 5 */ );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeEnum, SymbolKind.NamedType);
    }

    private static void AnalyzeEnum(SymbolAnalysisContext ctx)
    {
        var type = (INamedTypeSymbol)ctx.Symbol;

        // Only enums.
        if (type.TypeKind != TypeKind.Enum)
            return;

        // Scope: Precept.* namespaces only.
        if (!IsInPreceptNamespace(type))
            return;

        // E1: [Flags] enums are exempt.
        if (type.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "FlagsAttribute" &&
            a.AttributeClass.ContainingNamespace?.ToDisplayString() == "System"))
            return;

        // Check each member for value 0.
        foreach (var member in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (!member.HasConstantValue)
                continue;

            if (System.Convert.ToInt64(member.ConstantValue) != 0L)
                continue;

            // E2: Named "None" — structural sentinel.
            if (member.Name == "None")
                continue;

            // E3: [AllowZeroDefault] attribute.
            if (member.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "AllowZeroDefaultAttribute"))
                continue;

            // Violation.
            ctx.ReportDiagnostic(Diagnostic.Create(
                Rule,
                member.Locations.FirstOrDefault(),
                member.Name,
                type.Name));
        }
    }

    private static bool IsInPreceptNamespace(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace;
        while (ns != null && !ns.IsGlobalNamespace)
        {
            if (ns.Name == "Precept")
                return true;
            ns = ns.ContainingNamespace;
        }
        return false;
    }
}
```

### Key implementation notes

1. **`Convert.ToInt64`** handles all underlying enum types (`byte`, `short`, `int`, `long`, `uint`, etc.) safely.
2. **Namespace check walks up** the containment chain. `Precept.Language.TokenKind` → finds `Precept` at depth 2. `System.DayOfWeek` → never hits `Precept`.
3. **Attribute matching is by name**, not by type identity. The analyzer runs in `netstandard2.0` and doesn't reference `src/Precept/` — it cannot resolve the attribute's `INamedTypeSymbol` by reference. Name matching (`"AllowZeroDefaultAttribute"`) is the standard Roslyn analyzer pattern for this.
4. **`RegisterSymbolAction` on `SymbolKind.NamedType`** fires once per type declaration. This is more efficient than syntax-node analysis for enum-level checks.

---

## 7. Test Plan

All tests go in `test/Precept.Analyzers.Tests/Precept0018Tests.cs`. Use the existing `AnalyzerTestHelper.AnalyzeAsync<T>` pattern.

### True positives (MUST flag)

| # | Test name | Source | Expected |
|---|-----------|--------|----------|
| TP1 | `Implicit_zero_first_member_flags` | `namespace Precept.Language { public enum Foo { Bar, Baz } }` | 1 diagnostic on `Bar` |
| TP2 | `Explicit_zero_first_member_flags` | `namespace Precept.Language { public enum Foo { Bar = 0, Baz = 1 } }` | 1 diagnostic on `Bar` |
| TP3 | `Zero_not_first_member` | `namespace Precept.Language { public enum Foo { A = 1, B = 0, C = 2 } }` | 1 diagnostic on `B` |
| TP4 | `Private_enum_still_flagged` | `namespace Precept.Pipeline { class Outer { enum Inner { X, Y } } }` | 1 diagnostic on `X` |
| TP5 | `Internal_enum_still_flagged` | `namespace Precept.Runtime { internal enum Foo { Bar, Baz } }` | 1 diagnostic on `Bar` |
| TP6 | `Nested_precept_namespace` | `namespace Precept.Language.Nested { public enum Foo { Bar } }` | 1 diagnostic on `Bar` |

### True negatives (MUST NOT flag)

| # | Test name | Source | Expected |
|---|-----------|--------|----------|
| TN1 | `None_at_zero_exempt` | `namespace Precept.Language { public enum Foo { None = 0, Bar = 1 } }` | 0 diagnostics |
| TN2 | `Flags_enum_exempt` | `namespace Precept.Language { [System.Flags] public enum Foo { None = 0, A = 1, B = 2 } }` | 0 diagnostics |
| TN3 | `Flags_enum_with_non_None_zero` | `namespace Precept.Language { [System.Flags] public enum Foo { All = 0, A = 1 } }` | 0 diagnostics (entire `[Flags]` enum exempt) |
| TN4 | `AllowZeroDefault_attribute` | Source with `[AllowZeroDefault] Any = 0` | 0 diagnostics |
| TN5 | `One_based_enum_clean` | `namespace Precept.Language { public enum Foo { A = 1, B = 2, C = 3 } }` | 0 diagnostics |
| TN6 | `Non_precept_namespace_ignored` | `namespace SomeOtherLib { public enum Foo { Bar } }` | 0 diagnostics |
| TN7 | `No_zero_member_at_all` | `namespace Precept.Language { public enum Foo { A = 1, B = 2 } }` | 0 diagnostics |

### Edge cases

| # | Test name | Source | Expected |
|---|-----------|--------|----------|
| EC1 | `Multiple_zero_members` | `namespace Precept.Language { public enum Foo { A = 0, B = 0, C = 1 } }` | 2 diagnostics (both `A` and `B`) |
| EC2 | `None_plus_semantic_zero` | `namespace Precept.Language { public enum Foo { None = 0, Bad = 0, Good = 1 } }` | 1 diagnostic on `Bad` (only `None` exempted by name) |
| EC3 | `Empty_enum` | `namespace Precept.Language { public enum Foo { } }` | 0 diagnostics |
| EC4 | `Byte_underlying_type` | `namespace Precept.Language { public enum Foo : byte { A, B } }` | 1 diagnostic on `A` |
| EC5 | `Long_underlying_type` | `namespace Precept.Language { public enum Foo : long { A = 0L, B = 1L } }` | 1 diagnostic on `A` |

### Regression anchors (real Precept enums)

These don't use synthetic source — they document the expected analyzer behavior against the actual codebase once the fix wave completes. George should add these as comments in the test file or as a companion checklist, not as compilable tests (since they'd require the full Precept assembly):

| Enum | Expected | After fix wave |
|------|----------|----------------|
| `Severity` (1-based) | Clean | ✅ Already fixed |
| `DiagnosticStage` (1-based) | Clean | ✅ Already fixed |
| `TypeKind` (1-based) | Clean | ✅ Already fixed |
| `RoutingFamily.None = 0` | Clean (E2: name `None`) | ✅ Already correct |
| `TypeTrait` (`[Flags]`) | Clean (E1) | ✅ Already correct |
| `QualifierMatch.Any = 0` | Clean after `[AllowZeroDefault]` | Needs attribute |
| `PeriodDimension.Any = 0` | Clean after `[AllowZeroDefault]` | Needs attribute |
| `LexerMode.Normal = 0` | Clean after `[AllowZeroDefault]` | Needs attribute |
| `TokenKind.Precept = 0` | **Flagged** | Needs 1-based fix |
| `OperatorKind.Or = 0` | **Flagged** | Needs 1-based fix |

---

## 8. Rollout Sequence

The analyzer enforces at Error severity in a `TreatWarningsAsErrors` build. Enabling it before fixing the remaining ~23 at-risk enums will break the build. George should follow this sequence:

1. **Create the attribute** (`AllowZeroDefaultAttribute.cs`) — zero risk, additive.
2. **Create the analyzer** (`Precept0018SemanticEnumZeroSlot.cs`) — does not fire until build.
3. **Create the tests** (`Precept0018Tests.cs`) — validates analyzer logic in isolation.
4. **Add `[AllowZeroDefault]` to the 3 intentional-zero enums:**
   - `LexerMode.Normal`
   - `QualifierMatch.Any`
   - `PeriodDimension.Any`
5. **Fix the remaining ~20 catalog enums** to 1-based values (same pattern as the 6 already fixed). This is a separate PR or a later batch in the same PR — George's call based on scope.
6. **Build.** If clean, the analyzer is live.

**Step 5 is the big one.** The 6 already-fixed enums (`Severity`, `DiagnosticStage`, `ConstraintStatus`, `Prospect`, `FieldAccessMode`, `ActionSyntaxShape`, `TypeKind`) prove the pattern. The remaining ~20 follow the same mechanical transformation: assign `= 1` to the first member, explicit sequential values to the rest, update any hardcoded integer references in tests.

---

## 9. What This Does NOT Cover

- **Enum members with no explicit value that aren't at zero.** The analyzer only checks value `0`. It does not enforce "all members must have explicit values" — that's a style rule, not a correctness invariant.
- **Switch exhaustiveness.** PRECEPT0007 already covers `GetMeta` switch exhaustiveness. This analyzer is complementary — it protects the *enum declaration*, not the *switch consumption*.
- **Struct default-initialization.** If a struct has a semantic enum field, `default(Struct)` produces `(Enum)0`. This analyzer catches the enum-side problem; struct-level protection would be a separate analyzer.

---

## Appendix A: Enums That Will Need `[AllowZeroDefault]`

| Enum | Member | File | Justification |
|------|--------|------|---------------|
| `LexerMode` | `Normal` | `Pipeline/Lexer.cs` | Correct initial state — zero-init of the lexer starts in `Normal` mode by design. |
| `QualifierMatch` | `Any` | `Language/Operation.cs` | Documented default for ~95% of catalog entries. Zero-init means "matches any qualifier" — correct. |
| `PeriodDimension` | `Any` | `Language/ProofRequirement.cs` | Don't-care default. Zero-init means "any time dimension acceptable" — correct. |

## Appendix B: Enums That Will Need 1-Based Fix

These are the catalog enums where the first semantic member currently sits at implicit or explicit `0`. Each needs the same mechanical fix applied to the first 6:

`ActionKind`, `AnchorScope`, `AnchorTarget`, `Arity`, `Associativity`, `ConstructKind`, `ConstructSlotKind`, `ConstraintKind`, `DiagnosticCategory`, `DiagnosticCode`, `FaultCode`, `FaultSeverity`, `FunctionCategory`, `FunctionKind`, `ModifierCategory`, `ModifierKind`, `OperationKind`, `OperatorFamily`, `OperatorKind`, `ProofRequirementKind`, `TokenCategory`, `TokenKind`

**Count: ~22 enums.** All follow the same pattern — assign `= 1` to the first member and explicit sequential values to the rest.

---

*Frank — 2026-04-28. This document is implementation-ready. George: follow § 6 for file paths, § 7 for tests, § 8 for rollout order. No ambiguity should remain.*

---

# Spike Mode Is First-Class

**By:** Frank (Lead/Architect)
**Date:** 2026-04-28T23:09:18.057-04:00
**What:** Spike mode is now formally encoded across routing, ceremonies, wisdom, and `CONTRIBUTING.md`. Activation and exit intents are explicit, spike branches follow the `spike/{kebab-description}` convention, PR-demanding ceremonies are suppressed while `spike_mode: true`, and spike work exits through a structured closeout.
**Why:** Exploratory work is a real process mode, not an informal exception. If routing, ceremonies, durable wisdom, and contributor workflow docs do not encode spike mode together, the system incorrectly demands implementation gates, PRs, and branch churn during spikes.

## Encoded surfaces

- `.squad/routing.md` now recognizes spike activation and exit intents and suppresses PR-demanding auto-triggers during a spike.
- `.squad/ceremonies.md` now suppresses Design Review and PR Review when `spike_mode: true`, and defines manual Spike Kickoff / Spike Closeout ceremonies.
- `.squad/identity/wisdom.md` now records the spike branch naming convention and the rule that spike mode suppresses PR-dependent ceremonies.
- `CONTRIBUTING.md` now defines the contributor-facing spike workflow, including activation, branch rules, ceremony suppression, and deliberate closeout.

## Architectural decision

Spike mode is first-class. It must be activated deliberately, enforced consistently, and closed out explicitly. Exploratory work does not bypass process; it follows its own process.

---

# Zero-Slot Enum Audit — `src/Precept/`

**Reviewer:** Frank  
**Date:** 2026-04-28  
**Scope:** All `enum` declarations in `src/Precept/` audited for named semantic values at position 0  
**Trigger:** `ActionSyntaxShape` fix (explicit 1-based values); Shane asked "are there others?"

---

## Executive Summary

Shane's fix was correct and necessary. There are **four confirmed risks** sharing the same class of failure, one of which (`Severity.Info=0`) has the most dangerous silent-failure profile in the codebase today. Two more (`ConstraintStatus.Satisfied=0`, `Prospect.Certain=0`) are dormant time bombs that will matter the moment the evaluator and inspection engine are no longer stubs. The remaining enums are either intentionally sentinel-at-zero, protected by wildcard-throw arms, or unused in live switch dispatch.

---

## 1. Zero-Slot Risks Found

### RISK 1 — `Severity` · `Severity.cs` (inside `Diagnostic.cs`) · **HIGH**

```csharp
public enum Severity
{
    Info,     // = 0  ← RISK
    Warning,
    Error
}
```

**Where switched on:**  
`Compiler.cs:34` — `diagnostics.Any(d => d.Severity == Severity.Error)` sets `HasErrors`.  
Every diagnostic consumer in the language server and MCP tools compares against `Severity.Error` or `Severity.Warning`.

**Zero-default consequence:**  
`Diagnostic` is a `readonly record struct`. `default(Diagnostic)` gives `Severity = Info`. A bug in any new diagnostic factory path that passes `default` for severity (e.g., `new Diagnostic(default, stage, code, msg, span)`) silently downgrades all errors to informational. `Compiler.HasErrors` returns `false`. The pipeline passes. Invalid precepts compile clean. This is the most dangerous silent failure mode in the codebase — it bypasses the correctness gate entirely.

**Current protection:**  
`Diagnostics.Create(code, span)` is the sole factory and always derives severity from `DiagnosticMeta`. But the struct is publicly constructible without it.

**Recommended fix:**  
```csharp
public enum Severity
{
    Info    = 1,
    Warning = 2,
    Error   = 3,
}
```
`default(Severity) = (Severity)0` is unnamed. Any uninitialized diagnostic severity will throw `SwitchExpressionException` on first inspection — loud, not silent.

---

### RISK 2 — `DiagnosticStage` · `Diagnostic.cs` · **MEDIUM**

```csharp
public enum DiagnosticStage
{
    Lex,    // = 0  ← RISK
    Parse,
    Type,
    Graph,
    Proof
}
```

**Where switched on:**  
Stage is carried in every `Diagnostic` struct and in `DiagnosticMeta`. The language server and MCP vocabulary filter diagnostics by stage for display ordering and category attribution. A zero-default stage misattributes the diagnostic to `Lex` regardless of what pipeline stage produced it.

**Zero-default consequence:**  
Same struct-constructibility exposure as `Severity`. Less catastrophic (wrong attribution, not wrong severity), but still wrong: a type-check error silently appears as a lex error in tooling output.

**Recommended fix:**  
```csharp
public enum DiagnosticStage
{
    Lex   = 1,
    Parse = 2,
    Type  = 3,
    Graph = 4,
    Proof = 5,
}
```

**Note:** Fix `Severity` and `DiagnosticStage` together — they share the same struct and the same factory.

---

### RISK 3 — `ConstraintStatus` · `Inspection.cs` · **HIGH (dormant)**

```csharp
public enum ConstraintStatus { Satisfied, Violated, Unresolvable }
//                             ^= 0  ← RISK
```

**Where switched on:**  
`ConstraintResult.Status` is the per-constraint evaluation output returned by `InspectFire` and `InspectUpdate`. MCP tool `precept_inspect` surfaces this directly to callers. UI and agent consumers branch on `Satisfied` vs. `Violated` to decide whether constraints are blocking.

**Zero-default consequence:**  
`ConstraintResult` is a reference-type record, so accidental zero-construction is less likely than with a struct. The risk is in the evaluator implementation (currently `throw new NotImplementedException()`). When the evaluator is implemented, any code path that constructs a `ConstraintResult` without explicitly setting `Status` (possible in collection initializers, factory patterns, or test scaffolding) silently marks a **violated constraint as satisfied**. This is a direct correctness failure in the constraint enforcement model — the entire point of Precept.

**Recommended fix:**  
```csharp
public enum ConstraintStatus { Satisfied = 1, Violated = 2, Unresolvable = 3 }
```

Fix this **before** the evaluator is implemented, not after. Retrofitting is harder once there are construction sites.

---

### RISK 4 — `Prospect` · `Inspection.cs` · **HIGH (dormant)**

```csharp
public enum Prospect { Certain, Possible, Impossible }
//                     ^= 0  ← RISK
```

**Where switched on:**  
`RowInspection.Prospect` and `EventInspection.OverallProspect` are the first-match routing certainty signals returned from inspection. MCP `precept_inspect` uses these to tell callers which rows will fire, which might fire, and which cannot. A wrong `Certain` on an impossible row leads agents and UIs to present incorrect state-transition forecasts.

**Zero-default consequence:**  
Same analysis as `ConstraintStatus`. The evaluator is a stub. When implemented, an uninitialized `Prospect` field silently presents an impossible row as **certain to fire** — the most misleading possible output from inspection.

**Recommended fix:**  
```csharp
public enum Prospect { Certain = 1, Possible = 2, Impossible = 3 }
```

---

### RISK 5 — `FieldAccessMode` · `SharedTypes.cs` · **MEDIUM (dormant)**

```csharp
public enum FieldAccessMode { Read, Write }
//                            ^= 0  ← RISK
```

**Where switched on:**  
`FieldSnapshot.Mode` and `FieldAccessInfo.Mode` in runtime inspection. Callers use this to decide whether to render a field as editable in UIs or allow write operations in the runtime API.

**Zero-default consequence:**  
Zero-initialized mode = `Read`. A field that should be writable in the current state silently appears read-only. Write attempts are blocked without error. This is a subtle behavioral correctness failure — not a crash, not a thrown exception, just wrong behavior in the access model.

**Recommended fix:**  
```csharp
public enum FieldAccessMode { Read = 1, Write = 2 }
```

---

### RISK 6 — `TypeKind` · `TypeKind.cs` · **MEDIUM (dormant)**

```csharp
public enum TypeKind
{
    String,  // = 0  ← RISK
    Boolean,
    Integer,
    // ... 24 more
}
```

**Where switched on:**  
`Types.GetMeta(TypeKind kind)` has a `_ => throw ArgumentOutOfRangeException` wildcard — an **unnamed** zero would throw. But `String` is a **named** member at 0. An uninitialized `TypeKind` in the type checker (currently a stub) would silently treat any untyped expression node as `String`. Type compatibility checks, operation lookup, and accessor validation would all pass for `String` when the actual type is unknown.

**Current protection:**  
The TypeChecker is `throw new NotImplementedException()`. No live dispatch today.

**Recommended fix:**  
```csharp
public enum TypeKind
{
    String = 1,
    Boolean = 2,
    Integer = 3,
    // ...
}
```

Given the size of `TypeKind` (26 members) and the number of construction sites in catalog metadata, this is a larger change. Flag for implementation before TypeChecker is filled in.

---

## 2. Enums Assessed as Safe

| Enum | Reason |
|------|--------|
| `RoutingFamily` | `None=0` is an **explicit sentinel** — XML doc says "Not set — sentinel value for default-initialization detection." This is the right pattern. |
| `GraphAnalysisKind` | `None=0` is a structural sentinel. Used as the default for `EventModifierMeta.RequiredAnalysis` — "no analysis required." Correct usage. |
| `QualifierAxis` | `None=0` is a structural sentinel. Used as the default for `FixedReturnAccessor.ReturnsQualifier` — "no qualifier returned." Correct usage. |
| `TypeTrait` | `[Flags]` enum. `None=0` is the standard flags-enum sentinel pattern. Correct. |
| `PeriodDimension` | `Any=0` acts as a "don't care" default. `DimensionProofRequirement` uses it when any dimension is acceptable. Functionally sentinel. |
| `QualifierMatch` | `Any=0` documented explicitly as "Default for ~95% of entries." Used as a default parameter value in `BinaryOperationMeta`. Correct usage — this is the intentional safe default. |
| `LexerMode` (private) | `Normal=0` is the correct initial lexer state. The scanner struct is zero-initialized with `Normal` as default, which is intentional. Even without the explicit assignment in `Scanner()`, zero-init would be correct. |
| `FaultSeverity` | Single member `Fatal=0`. No dispatch ambiguity possible. |
| `ActionKind` | `Set=0`. All 4 parser shape-specific switches enumerate every `ActionKind` with explicit arms (either correct or `throw InvalidOperationException`). An unnamed 0 would throw; the named `Set=0` correctly routes to `SetStatement` in `ParseAssignValueStatement`. `Actions.GetMeta` has `_ => throw`. Mitigated by exhaustive per-shape arm coverage. |
| `ConstraintKind` | `Invariant=0`. `Constraints.GetMeta` has `_ => throw`. `ConstraintMeta` uses a DU subtype pattern — consumers pattern-match on `ConstraintMeta.Invariant` (the subtype), not on `ConstraintKind` directly. The DU acts as a second guard layer. |
| `ConstructKind` | `PreceptHeader=0`. `ParseDirectConstruct` has a `var k => throw` wildcard. `BuildNode` has CS8524 pragma (named exhaustive) — an unnamed 0 would throw `SwitchExpressionException`; `PreceptHeader` is never reached via the direct-parse path for a legitimate precept header. |
| `ConstructSlotKind` | `IdentifierList=0`. `InvokeSlotParser` has CS8524 pragma, no wildcard — unnamed 0 throws. But named `IdentifierList=0` would route to `ParseIdentifierList` if zero-initialized. Low risk: slots are only constructed in the catalog with explicit `ConstructSlotKind` values. |
| `DiagnosticCategory`, `DiagnosticCode`, `FaultCode` | Catalog enums used in `GetMeta` switches that all have `_ => throw`. Zero-default names would dispatch to first member, but all construction is factory-controlled. |
| `FunctionKind`, `FunctionCategory` | `GetMeta` has `_ => throw`. Factory-controlled. |
| `ModifierKind` | `Optional=0`. `Modifiers.GetMeta` has `_ => throw` (confirmed by pattern). Modifier metadata always built via catalog. |
| `OperationKind`, `OperatorKind` | `GetMeta` has `_ => throw`. Factory-controlled. |
| `ProofRequirementKind` | `Numeric=0`. `GetMeta` has `_ => throw`. |
| `TokenKind` | `Precept=0`. `Tokens.GetMeta` has (inferred) `_ => throw`. Tokens come from the lexer, never from zero-init. |
| `Arity`, `Associativity`, `OperatorFamily` | Used exclusively in catalog `OperatorMeta` records. Always explicitly set. Associativity: `Left=0` is the overwhelmingly common operator direction — a zero-default here would be correct for most operators. |
| `AnchorScope`, `AnchorTarget` | `InState=0`, `Ensure=0`. Used only in `AnchorModifierMeta` construction in the Modifiers catalog. All construction is explicit. |
| `ModifierCategory`, `TypeCategory` | Classification fields on metadata records. Always explicitly set in catalog construction. |
| `TokenCategory` | `Declaration=0`. Used in `TokenMeta.Categories` lists, always explicitly populated. |

---

## 3. Root Cause Analysis

**Why wasn't 1-based explicit values used from the start?**

Honest assessment: it was an oversight, not a considered tradeoff.

The evidence is the inconsistency. `RoutingFamily.None=0`, `GraphAnalysisKind.None=0`, and `QualifierAxis.None=0` all show that **someone was aware of the zero-default trap** and applied the sentinel pattern in those cases. But `Severity.Info=0`, `Prospect.Certain=0`, and `ConstraintStatus.Satisfied=0` show the protection wasn't applied consistently.

The pattern was:
1. **Applied correctly** when the developer had a structural "nothing here" concept that wanted to live at zero — the sentinel pattern was deliberate.
2. **Not applied** for semantic enums where every value is meaningful and there is no structural "nothing." Those defaulted to 0-based because C# defaults to 0-based.

`ActionSyntaxShape` was the acute case because:
- The enum was likely refactored from an earlier form that had a `None` sentinel
- When `None` was removed, the first real value inherited slot 0 without explicit reassignment
- The parser switch was exhaustive-named with CS8524 pragma (no wildcard throw), so a zero-default silently routed

The broader problem is that the 1-based-for-semantic-enums policy was never written down as a design rule. The `ActionSyntaxShape` fix established the pattern after the fact. The other risks above are the same class of bug waiting to manifest — most blocked by stubs today, all active risks once those stubs are filled in.

**Recommended policy going forward:**

> Every enum where ALL members are semantically meaningful (no structural "nothing" at 0) should use explicit 1-based integer values. The zero slot is unnamed. `default(Kind) = (Kind)0` throws or is structurally detectable.
> 
> Enums with an explicit `None = 0` sentinel are correct as-is — that IS the intended zero behavior.

This policy, if encoded in a Roslyn analyzer or code review checklist, would have caught every risk identified in this audit at definition time.

---

*Report written by Frank. Fix `Severity` + `DiagnosticStage` first — they are in a live struct with a correctness gate. Fix `ConstraintStatus`, `Prospect`, and `FieldAccessMode` before the evaluator is implemented. Flag `TypeKind` as a pre-TypeChecker task.*

---

# Decision: ActionSyntaxShape — Explicit 1-Based Integer Values

**Date:** 2026-04-28  
**Author:** George (Runtime Dev)  
**Branch:** `precept-architecture`  
**Commit:** `de2005a`

## Decision

`ActionSyntaxShape` members now carry explicit integer values starting at 1:

```csharp
public enum ActionSyntaxShape
{
    AssignValue     = 1,
    CollectionValue = 2,
    CollectionInto  = 3,
    FieldOnly       = 4,
}
```

`default(ActionSyntaxShape)` == `(ActionSyntaxShape)0` — an unnamed integer value with no named arm in any exhaustive switch.

## Why

The prior layout had `AssignValue = 0` (implicit). Any code that constructs an `ActionMeta` and leaves `SyntaxShape` as `default` silently routes through `ParseAssignValueStatement`. With `TreatWarningsAsErrors=true` and the existing `#pragma CS8524` pairs on each switch, the unnamed zero slot now causes a `SwitchExpressionException` at runtime instead of silent wrong-parser dispatch. The 1-based layout is self-enforcing structural protection at zero runtime cost.

## Alternatives Considered

- **Keep `None = 0` sentinel** — rejected because B1 removed it precisely to eliminate the dead "None" arm from the parser switch. Reinstating it defeats that cleanup.
- **Add a runtime guard in `ActionMeta` constructor** — redundant; the switch throws automatically without any extra code once zero is unnamed.
- **Leave values implicit** — leaves the silent zero-default trap in place; easy to trigger with a careless `ActionMeta(...)` call that omits the positional `SyntaxShape` argument in a future slice.

## Tradeoff Accepted

Serialized integer values for `ActionSyntaxShape` (if any external system ever persists them) shift by +1 relative to the old implicit layout. No current consumers serialize this enum, so the tradeoff is zero-cost in practice.

## Test Change

`Actions_ActionSyntaxShape_AllMembersHaveValue` (used `Enum.IsDefined`) was replaced by `Actions_ActionSyntaxShape_AllMembersAreNonZero` (asserts `(int)s > 0`). The new assertion directly enforces the 1-based invariant; the old assertion was weakened the moment `None=0` was removed, because `Enum.IsDefined(AssignValue)` is trivially true regardless of its integer value.

---

# George → Frank: B1–B7 Fixed — Re-review Requested

**From:** George (Runtime Dev)  
**Date:** 2026-04-28  
**Branch:** `feature/catalog-extensibility`  
**Commit:** `5e5b2f9`  
**Re:** Frank's BLOCKED verdicts in `frank-enum-deviation-review.md` and `frank-deep-deviation-review.md`

---

All 7 blocking issues are resolved. Build is clean (0 errors, 0 warnings, TreatWarningsAsErrors=true). All 2044 tests pass.

## What was changed

### B1 — `ActionSyntaxShape.None` removed (`src/Precept/Language/Action.cs`)

Removed `None = 0` and its XML doc comment. The enum now has exactly four members:

```csharp
public enum ActionSyntaxShape
{
    AssignValue,
    CollectionValue,
    CollectionInto,
    FieldOnly,
}
```

### B2 — Outer switch cleaned (`src/Precept/Pipeline/Parser.cs` — `ParseActionStatement`)

Removed the `ActionSyntaxShape.None => throw` arm. The outer switch now has exactly 4 arms with the `#pragma CS8524` pair intact.

### B3 — `ParseAssignValueStatement` inner switch

Replaced `_ => throw` with explicit named throw arms for every `ActionKind` not in the `AssignValue` shape: `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop`, `Clear`. Added `#pragma CS8524` pair.

### B4 — `ParseCollectionValueStatement` inner switch

Replaced `_ => throw` with explicit named throw arms for `Set`, `Dequeue`, `Pop`, `Clear`. Added `#pragma CS8524` pair.

### B5 — `ParseCollectionIntoStatement` inner switch

Replaced `_ => throw` with explicit named throw arms for `Set`, `Add`, `Remove`, `Enqueue`, `Push`, `Clear`. Added `#pragma CS8524` pair.

### B6 — `ParseFieldOnlyStatement` inner switch

Replaced `_ => throw` with explicit named throw arms for `Set`, `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop`. Added `#pragma CS8524` pair.

### B7 — `InvokeSlotParser` wildcard and comment (`Parser.cs`)

- Removed `_ => throw new ArgumentOutOfRangeException(...)` arm
- Added `#pragma warning disable CS8524` before the switch and `restore` after
- Replaced the misleading comment with: "CS8509 enforces named-value coverage here; #pragma CS8524 suppresses unnamed-integer noise."

### Companion test fix (`test/Precept.Tests/ActionsTests.cs`)

`Actions_ActionSyntaxShape_AllMembersHaveValue` was checking `NotBe((ActionSyntaxShape)0)`. With `None = 0` removed, `AssignValue` is now value 0, making that assertion a false positive. Updated to `Enum.IsDefined(meta.SyntaxShape)` which catches undefined integer values without falsely flagging `AssignValue`.

---

## Verification

```
dotnet build src/Precept/Precept.csproj  → succeeded, 0 errors, 0 warnings
dotnet test test/Precept.Tests/          → Passed! 2044/2044
```

Every catalog enum switch in the parser now follows the `BuildNode` gold standard:  
explicit arms for all named members · `#pragma CS8524` to suppress unnamed-integer noise · no wildcard · CS8509 active.

---

# Decision: Catalog Extensibility Implementation Complete (PR #138)

**By:** George (Runtime Dev)
**Date:** 2026-04-28
**Branch:** feature/catalog-extensibility
**PR:** #138

## Status

All 7 slices implemented and passing (2044 tests).

## What Was Done

### Slice 1 — ExpressionBoundaryTokens derived from catalog
`ExpressionBoundaryTokens` is now composed of `StructuralBoundaryTokens` (6 fixed tokens: When, Because, Arrow, Ensure, EndOfSource, NewLine) plus `Constructs.LeadingTokens` — no more hardcoded construct-leading tokens in the parser. Adding a new construct kind with a new leading token automatically extends the expression boundary.

### Slice 2 — BuildNode wildcard removed (CS8509 enforced)
The `_ => throw` wildcard arm was removed from `BuildNode`. The switch now has one arm per `ConstructKind` with no default. CS8509 fires when a new `ConstructKind` is added without a corresponding assembly arm. `#pragma warning disable CS8524` suppresses the unnamed-integer variant (correct: we only guard against new named members, not arbitrary integer casts).

### Slice 3 — RoutingFamily enum + ConstructMeta field
`RoutingFamily` enum added: `None=0` (sentinel), `Header`, `Direct`, `StateScoped`, `EventScoped`. Added as required positional parameter to `ConstructMeta`. All 12 catalog entries populated. Tests assert no entry has `None` routing family.

### Slice 3b — DisambiguateAndParse exhaustive switches
Both EventScoped and StateScoped switches in `DisambiguateAndParse` now have:
- Named arms for each matching construct
- `null =>` arm calling `EmitAmbiguityAndSync` (handles "not found" from lookup)
- Explicit wrong-family throw arms (all non-matching ConstructKind values listed explicitly — no wildcard)

This gives CS8509-style protection: adding a new ConstructKind forces updating both switches.

### Slice 4 — ActionSyntaxShape enum + ActionMeta field + Actions.ByTokenKind
`ActionSyntaxShape` enum added: `None=0` (sentinel), `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly`. Added as required positional parameter to `ActionMeta`. All 8 catalog entries populated. `Actions.ByTokenKind` (FrozenDictionary) added for O(1) token→meta lookup.

### Slice 5 — ParseActionStatement two-level CS8509 refactor
`ParseActionStatement` replaced with a shape-dispatch entry + 4 private helpers:
- Outer switch on `meta.SyntaxShape` (no default — CS8509 on new shapes)
- Inner switch per helper on `meta.Kind` (no default — CS8509 on new actions within each shape)
- Unknown token path uses `Actions.ByTokenKind.TryGetValue` instead of a `default:` case

### Slice 6 — AccessModeKeywords derived from catalog
`TokenMeta.IsAccessModeAdjective` bool flag added (optional, default false). `Readonly` and `Editable` tokens tagged. `Tokens.AccessModeKeywords` FrozenSet derived from tagged entries. `ParseAccessModeKeywordDirect` uses `Tokens.AccessModeKeywords.Contains(...)` instead of `is TokenKind.Readonly or TokenKind.Editable`.

## Implementation Deviations from Plan

1. **`None=0` sentinels** — Both `RoutingFamily` and `ActionSyntaxShape` got a `None=0` member not in the original plan spec. This is required for the "all members have non-default value" tests to work correctly (testing `.NotBe((RoutingFamily)0)` requires `0` to be the sentinel, not a valid value).

2. **`#pragma warning disable CS8524`** — The plan described "CS8509 fires here." The actual compiler warning for exhaustive named-enum switches without wildcard is CS8524 (unnamed values) when the switch handles all named values explicitly. CS8509 fires when named values are missed. Both concerns are addressed: CS8524 is suppressed (unnamed integer casts aren't our concern), and CS8509 will fire if a new named ConstructKind is added without an arm.

## Cross-Surface Impact

Parser internal only. No grammar, language server, MCP, or sample changes needed.

---

# Decision: All Semantic Zero-Slot Enums Use Explicit 1-Based Values

**Date:** 2026-04-28  
**Author:** George (Runtime Dev)  
**Status:** Implemented — commit `d300b26` on `precept-architecture`

---

## Decision

Every enum in `src/Precept/` where ALL members are semantically meaningful (no structural "nothing" at 0) must use explicit 1-based integer values. The zero slot stays unnamed. `default(Kind)` = `(Kind)0` = unnamed = throws `SwitchExpressionException` rather than silently routing to a wrong member.

This is a blanket structural invariant, not per-enum policy. Once established, new enums start at 1 by default.

---

## Enums Fixed

| Enum | File | Member count | Zero-slot risk |
|------|------|:---:|------|
| `Severity` | `Language/Diagnostic.cs` | 3 | `default(Diagnostic)` gives `Severity.Info`; `HasErrors` returns false on zero-constructed diagnostics |
| `DiagnosticStage` | `Language/Diagnostic.cs` | 5 | Zero-constructed diagnostics attributed to `Lex` stage |
| `ConstraintStatus` | `Runtime/Inspection.cs` | 3 | Zero-initialized result marks violated constraint as `Satisfied` — inverts Precept's core correctness guarantee |
| `Prospect` | `Runtime/Inspection.cs` | 3 | Zero-initialized prospect presents an impossible transition row as `Certain` to fire |
| `FieldAccessMode` | `Runtime/SharedTypes.cs` | 2 | Zero-initialized mode silently locks writable fields as `Read` |
| `TypeKind` | `Language/TypeKind.cs` | 26 | Zero-initialized kind silently treats unknown types as `String` |

---

## Pattern Applied

```csharp
// Before (unsafe — String = 0 is a valid member)
public enum TypeKind { String, Boolean, Integer, ... }

// After (safe — (TypeKind)0 is unnamed, throws SwitchExpressionException)
public enum TypeKind
{
    String  = 1,
    Boolean = 2,
    Integer = 3,
    ...
}
```

---

## Rationale

- **Silent wrong-branch routing is the failure mode.** When an enum member sits at 0 and a struct gets zero-initialized (field omitted, `default(T)`, array element, unset out-parameter), the switch routes silently to that member. The bug is invisible — no exception, no compiler warning, semantically wrong behavior.
- **The 1-based layout is free.** No behavioral change when all constructors set the field explicitly. The only observable difference is that `(T)0` now has no named arm — which is exactly the trip-wire we want.
- **`SwitchExpressionException` is the right signal.** It fires immediately at the misuse site with a clear message, not downstream after data has propagated. This is strictly better than a silent wrong-value path.
- **Consistency with `ActionSyntaxShape`.** That enum was made 1-based in the prior session (commit `de2005a`). Applying the same rule to all other enums closes the audit uniformly.

---

## Alternatives Rejected

- **Leave at 0, add runtime guards:** Adds noise at every consumer, doesn't eliminate the silent path.
- **Add `None = 0` sentinel:** The sentinel itself becomes a valid arm; switch exhaustiveness can mask it. The unnamed-slot approach is strictly stronger.
- **Per-enum risk assessment:** Inconsistent. The rule is simple enough to apply universally. Dormant enums (`ConstraintStatus`, `Prospect`, `FieldAccessMode`, `TypeKind`) are safer to fix now than when the evaluator ships and they're live.

---

## Impact on Tests

No tests used `(EnumName)0` or `default(EnumName)` — no test changes required. All 2044 tests passed without modification.

---

## Going Forward

New enums in `src/Precept/` where all members are semantically meaningful must start at 1. The canonical check: *"Is there a valid program state represented by integer 0 for this type?"* If no, start at 1.

---

# Decision: Map Access Syntax

**Date:** 2026-04-29  
**Author:** Frank (Lead Language Designer)  
**Status:** Advisory — pending owner decision  
**Context:** Shane asked whether `CoverageLimits.get(CheckCoverage.CoverageType)` is too C#-like or fits Precept's grammar.

---

## 1. Does `.get(key)` Fit Precept's Grammar?

**Short answer: It partially fits, but it's the wrong design choice.**

The spec *does* have a precedent for parameterized accessors: `.inZone(tz)` on `instant` and `datetime` (spec §3.3, temporal accessors). The parser already handles `MemberAccessExpression` + `(` → `MethodCallExpression` at binding power 80. So `.get(key)` is *parseable* — the machinery exists.

However, there's a critical distinction:

- **`.inZone(tz)` is a type-system accessor** — it's a fixed method name on a specific type, taking a single typed constant argument. It transforms one temporal type to another. It's a narrow, catalog-driven exception.
- **`.get(key)` is a general-purpose keyed lookup** — the argument is an arbitrary expression, not a type constant. It's closer to subscript access than to a type accessor.

The quantifier decision (`.all(item, pred)` → `each item in Items (pred)`) rejected multi-argument method-call syntax because it looked like C# and violated Precept's keyword-first declarative style. `.get(key)` is a single-argument form, so it's *less* of a violation — but it still introduces `object.verb(arg)` OOP method-call syntax into a language that otherwise uses `keyword Object Arg` action grammar everywhere:

```
-> set Field = Expr        # keyword Field = Expr
-> add Collection Value    # keyword Collection Value
-> remove Collection Value # keyword Collection Value
-> push Collection Value   # keyword Collection Value
```

Making map access the one place where `Object.verb(Arg)` appears in expression position is an unnecessary style break.

## 2. Candidate Evaluation

### A. `CoverageLimits.get(CheckCoverage.CoverageType)` — method call

- **Parse:** Unambiguous — spec already defines `MemberAccessExpression` + `(` → `MethodCallExpression`.
- **Style fit:** Poor. OOP method-call form in an otherwise keyword-driven language. The quantifier precedent rejected this pattern.
- **Contexts:** Works syntactically in rules, actions, and guards. Visually noisy.
- **Verdict:** Parseable but stylistically wrong. **Reject.**

### B. `CoverageLimits[CheckCoverage.CoverageType]` — subscript bracket

- **Parse:** Requires adding `[` as a left-denotation token at high binding power. Currently `[` only appears in null-denotation (list literals). Adding it as infix/postfix creates a new parse path.
- **Style fit:** Familiar from every C-family language, but Precept has no other postfix bracket notation. Brackets currently mean "list literal," not "index into."
- **Contexts:** Works everywhere, binds tightly. `rule CoverageLimits[CheckCoverage.CoverageType] >= 50000` reads cleanly.
- **Verdict:** Parseable with grammar extension. Visually compact. But introduces a new syntactic category (postfix brackets) that has no other use in the language. **Acceptable but not preferred.**

### C. `CoverageLimits at CheckCoverage.CoverageType` — keyword `at`

- **Parse:** `at` as an infix keyword operator (like `contains`) at binding power ~40. Left operand is map-typed expression, right operand is key expression.
- **Style fit:** Excellent. Reads like English: "coverage limits at this coverage type." Follows the same `Collection operator Key` pattern as `MissingDocuments contains ReceiveDocument.Name`.
- **Contexts:**
  - Rule: `rule CoverageLimits at CheckCoverage.CoverageType >= 50000` ✓
  - Action: `-> set CurrentLimit = CoverageLimits at CheckCoverage.CoverageType` ✓
  - Guard: `when CoverageLimits at CheckCoverage.CoverageType > MinCoverage` ✓
- **Precedence concern:** At BP 40 (same as `contains`), `CoverageLimits at CheckCoverage.CoverageType >= 50000` parses as `(CoverageLimits at CheckCoverage.CoverageType) >= 50000` — correct, because `.` binds tighter (80) and comparison binds looser (30).
- **Lexer:** `at` is a new keyword but has no collision risk — it's not used anywhere in the current grammar.
- **Verdict:** **Recommended.** Clean English reading, follows `contains` precedent exactly, no grammar conflicts, works in all three contexts.

### D. `CoverageLimits of CheckCoverage.CoverageType` — reuse `of`

- **Parse:** `of` is already heavily overloaded: `set of T`, `queue of T`, `map of K to V`, `choice of ...`. Using it as an infix expression operator creates parser ambiguity in type positions.
- **Style fit:** Reads okay in isolation but "limits of coverage type" reverses the natural English meaning (the type is the key, not the possessor).
- **Verdict:** **Reject.** Too overloaded, semantically backwards.

### E. `CoverageLimits for CheckCoverage.CoverageType` — keyword `for`

- **Parse:** `for` is not currently a keyword. Could work as infix operator.
- **Style fit:** "Coverage limits for this coverage type" reads naturally. However, `for` strongly implies iteration in most programming contexts, which conflicts with Precept's no-iteration philosophy.
- **Verdict:** **Acceptable but risky.** The iteration connotation is a liability.

### F. `lookup CoverageLimits CheckCoverage.CoverageType` — keyword-first

- **Parse:** Keyword-first form matching action grammar (`keyword Collection Key`).
- **Style fit:** Matches action patterns but map access is an *expression*, not an action. Keyword-first forms in Precept are statements/actions (`set`, `add`, `remove`, `push`, `transition`). Expressions use infix/postfix notation. A keyword-first expression would be a new grammatical category.
- **Contexts:** Awkward in rules: `rule lookup CoverageLimits CheckCoverage.CoverageType >= 50000` — where does the `lookup` end and the `>=` begin? Requires parenthesization: `rule (lookup CoverageLimits CheckCoverage.CoverageType) >= 50000`.
- **Verdict:** **Reject.** Expression-position keyword-first form creates ambiguity and requires parens.

### G. `CoverageLimits.get CheckCoverage.CoverageType` — accessor without parens

- **Parse:** `.get` would parse as a `MemberAccessExpression` yielding a value, then `CheckCoverage.CoverageType` is a separate expression with no connecting operator. The parser would see two adjacent expressions with no infix operator between them — a parse error.
- **Style fit:** Looks like a shell command, not a Precept expression.
- **Verdict:** **Reject.** Unparseable without grammar surgery.

## 3. Recommendation

**Use `at` as the map access operator.**

```
CoverageLimits at CheckCoverage.CoverageType
```

### Rationale

1. **Follows the `contains` precedent exactly.** `contains` is an infix keyword operator that takes a collection on the left and a value on the right. `at` is the same pattern: map on the left, key on the right.

2. **No method calls in expression position.** The quantifier decision established that Precept prefers keyword forms over method-call syntax. `at` is a keyword, not a method.

3. **`.inZone(tz)` is the exception, not the rule.** That accessor exists because timezone conversion is a type-system operation (it changes the return type). Map lookup is a data operation — it should use the language's standard expression vocabulary, not the type accessor escape hatch.

4. **Reads as English.** "Coverage limits at this coverage type" is immediately clear to business stakeholders.

5. **Proof engine integration is clean.** `containskey` guards the `at` operator the same way `.count > 0` guards `.peek` — the proof engine tracks key-presence from `when CoverageLimits containskey K` and permits `CoverageLimits at K` in the guarded scope.

### Usage in all three contexts

```precept
# Rule expression
rule CoverageLimits at CheckCoverage.CoverageType >= 50000
    because "Each coverage type must meet minimum threshold"

# Action body
from Active on CheckCoverage when CoverageLimits containskey CheckCoverage.CoverageType
    -> set CurrentLimit = CoverageLimits at CheckCoverage.CoverageType
    -> no transition

# Event guard
from Active on AdjustCoverage
    when CoverageLimits containskey AdjustCoverage.CoverageType
    and CoverageLimits at AdjustCoverage.CoverageType > MinCoverage
    -> ...
```

### Implementation sketch

- New lexer keyword: `at` → `TokenKind.At`
- New left-denotation entry: `At` at binding power 40 → `MapAccessExpression(left, ParseExpression(40))`
- Type checker: left must be `map of K to V`, right must be assignable to `K`, result type is `V`
- Proof engine: `at` requires a `containskey` guard in scope for the same map and key expression (new proof obligation: `KeyPresence`)

---

# Advisory: Map Access Keyword — `at` vs `for`

**Author:** Frank (Lead Language Designer)
**Date:** 2026-04-29
**Status:** Advisory (no file changes)
**Context:** Shane asked whether `for` should replace the previously recommended `at` as the map access infix keyword.

---

## 1. Existing Uses in Precept

### `for` — Not a reserved keyword

`for` does **not** appear in the v2 reserved keyword set (§1.2 of `precept-language-spec.md`). It is an ordinary identifier today. It appears in sample files only inside comments and string literals — never as syntax. Adding `for` as a keyword would be a net-new reservation.

### `at` — Not a reserved keyword

`at` also does **not** appear in the v2 reserved keyword set. Like `for`, it appears in sample files only inside comments and string literals (e.g., `"capped at half"`, `"at least one floor"`). Adding `at` would also be a net-new reservation.

**Tie.** Neither keyword carries existing syntactic baggage. Both are fresh reservations.

---

## 2. Cognitive Load — What Does English Prime You For?

This is where the two diverge meaningfully.

### `for` — "intended for / on behalf of / filtered by"

In English, `for` implies **purpose, benefit, or selection**: "the limit *for* this coverage type," "the fee *for* this transaction." It reads like a natural-language lookup: "get me the value meant for this key."

In programming, `for` is universally associated with **iteration** — `for` loops, `for-each`, `for...in`, `for...of`. Every mainstream language (C, Java, Python, JavaScript, Go, Rust, Swift, Kotlin) uses `for` as an iteration keyword. Precept has no loops by design (§0.4.1: "No loops"), but adopting `for` as a map access keyword creates a false association for anyone who has ever written code. It will feel like it should iterate. This is a real cognitive cost.

### `at` — "located at / indexed at"

In English, `at` implies **location or position**: "the value *at* this key," "the element *at* index 3." It reads as spatial lookup — which is exactly what map access is.

In programming, `at` is used for **element access** — C++ `vector.at(i)`, Java `List.get()` (conceptually "at index"), Python's `__getitem__`. No mainstream language uses `at` for iteration. The association is clean: `at` means "retrieve the thing located here."

**Advantage: `at`.** The word `for` carries iteration baggage from every programming language. `at` carries lookup/access semantics with no conflicting association.

---

## 3. Readability in All Three Contexts

### Rule expression

```precept
rule CoverageLimits for CheckCoverage.CoverageType >= 50000
rule CoverageLimits at CheckCoverage.CoverageType >= 50000
```

**`for`** reads: "the coverage limits *for* the coverage type must be ≥ 50,000." Natural English — this is `for`'s strongest context. A business analyst would write it this way.

**`at`** reads: "the coverage limits *at* the coverage type must be ≥ 50,000." Slightly more technical, but still clear. "At" signals lookup, which is accurate.

**Edge: `for`** — in isolation, the `for` reading is slightly more natural in this context.

### Action body

```precept
-> set SomeField to CoverageLimits for CheckCoverage.CoverageType
-> set SomeField to CoverageLimits at CheckCoverage.CoverageType
```

**`for`** reads: "set SomeField *to* CoverageLimits *for* the coverage type." The `to` ... `for` pairing is natural but introduces a subtle risk: the prepositions `to` and `for` now both appear in the same statement with different structural roles. `to` is the assignment target connector; `for` is the map access operator. A reader must parse two prepositions doing unrelated jobs.

**`at`** reads: "set SomeField *to* CoverageLimits *at* the coverage type." The `to` ... `at` pairing is clearer — `to` means "assign into," `at` means "look up from." Different prepositions, different roles, no confusion.

**Edge: `at`** — avoids the `to`/`for` preposition collision in action context.

### Event guard

```precept
when CoverageLimits for CheckCoverage.CoverageType > MinCoverage
when CoverageLimits at CheckCoverage.CoverageType > MinCoverage
```

**`for`** reads: "when the coverage limits *for* the coverage type exceed the minimum." Natural, clear.

**`at`** reads: "when the coverage limits *at* the coverage type exceed the minimum." Clear, slightly more technical.

**Edge: `for`** — marginal.

### Composite reading

Across all three contexts, `for` wins on pure English fluency in 2 of 3, but `at` wins on **structural clarity** — it is never ambiguous about what role it plays. The action-body `to`/`for` collision is a real readability problem, not a theoretical one. Precept uses `to` as a core keyword (state-gate ensures, `map of K to V`). Adding `for` as another overloaded preposition increases the preposition density in action statements.

**Overall edge: `at`.** The structural clarity advantage outweighs the marginal English fluency advantage of `for`.

---

## 4. Parse Ambiguity

### `for`

No **grammatical** ambiguity — `for` is not reserved, so the parser can adopt it as a new infix operator without collision. However, if Precept ever introduces bounded quantifiers (currently proposed in `collection-types.md` §Proposed Extensions — `each`, `any`, `no`), future language evolution could face pressure to use `for` in a quantifier context (e.g., `each item in Items for ...`). Reserving `for` now for map access would block that path.

### `at`

No grammatical ambiguity. `at` has no plausible future use in Precept's design space beyond element access. It is a dead-end keyword in the best sense — it does exactly one thing and nothing else will ever want it.

**Advantage: `at`.** Smaller future-reservation footprint.

---

## 5. Precedent — Infix Operator Pattern

Precept's existing infix keyword operator is `contains`:

```precept
MissingDocuments contains ReceiveDocument.Name
```

Pattern: `Collection <infix-keyword> Operand`

Both candidates follow this pattern:

```precept
CoverageLimits at CheckCoverage.CoverageType       -- map access
CoverageLimits for CheckCoverage.CoverageType       -- map access
MissingDocuments contains ReceiveDocument.Name       -- membership
```

`contains` is a **verb** — it describes what the collection does with the operand. `at` is a **preposition** — it describes where in the collection to look. `for` is also a preposition but with a purpose/selection flavor.

The infix slot accepts both verbs and prepositions (precedent: `contains` is a verb, `is` is a verb in `is set`/`is not set`). Neither `at` nor `for` breaks the pattern.

**Tie.** Both fit the infix precedent.

---

## 6. Verdict

**I recommend `at`.** The original recommendation stands.

`for` is not bad — it reads beautifully in rule context and guard context. But it has three disadvantages that `at` does not:

1. **Iteration baggage.** Every programmer alive associates `for` with loops. Precept has no loops. Using `for` for map access creates a false cognitive prime that `at` avoids entirely.

2. **Preposition collision with `to`.** In action bodies (`-> set F to M for K`), `to` and `for` are both prepositions doing different structural jobs in the same statement. `at` creates no such collision — `to` assigns, `at` looks up.

3. **Future reservation cost.** `for` is a high-value English word that could plausibly serve quantifier syntax or other future constructs. `at` has no competing future claim.

The marginal English fluency advantage of `for` in rule and guard contexts does not outweigh these structural disadvantages. `at` reads clearly in all three contexts, parses unambiguously, carries the right programming-language association (element access), and leaves `for` available for future language evolution.

**Recommendation: `at` confirmed.**

```precept
-- Canonical map access syntax
CoverageLimits at CheckCoverage.CoverageType
```

---

# Decision: Map Access Keyword — `for` vs `at`

**By:** Frank (Lead Language Designer)
**Date:** 2026-04-29
**Status:** Advisory — owner decision needed
**Context:** Follow-up to frank-16 (`at` recommendation). Shane asks whether `for` is better.

---

## 1. Keyword Inventory Check

Neither `for` nor `at` is in Precept's v2/v3 reserved keyword set. Both are fresh — no existing meaning to collide with.

## 2. English Reading Test

| Form | Reading |
|------|---------|
| `CoverageLimits at CheckCoverage.CoverageType` | "coverage limits *at* this coverage type" — spatial/indexing, like looking up a slot |
| `CoverageLimits for CheckCoverage.CoverageType` | "coverage limits *for* this coverage type" — relational, like asking "which limit applies to this type?" |

`for` reads more naturally in business-rule English. Domain experts say "the limit **for** auto coverage" not "the limit **at** auto coverage." `at` carries a programmer connotation (array-at-index) that `for` avoids.

## 3. Grammar Ambiguity Test

### Rule context
```precept
rule CoverageLimits for CheckCoverage.CoverageType >= 50000
    because "Minimum coverage per type"
```
Parses cleanly: `CoverageLimits for CheckCoverage.CoverageType` is the LHS expression, `>=` begins the comparison. No ambiguity.

### Action context
```precept
-> set SomeField = CoverageLimits for CheckCoverage.CoverageType
```
Parses cleanly: `=` left-delimits the RHS expression; `for` is an infix operator binding map-to-key within the expression.

### Guard context
```precept
when CoverageLimits for CheckCoverage.CoverageType > MinCoverage
```
Parses cleanly: `for` binds tighter than `>`, producing `(CoverageLimits for CheckCoverage.CoverageType) > MinCoverage`.

### Loop/iteration confusion risk
Precept has **no loops** (§0.4.1 — "No iteration constructs"). There is no `for` loop, no `for-each`, no iteration keyword anywhere in the grammar. The quantifier syntax uses `each`/`any`/`no` + `in`, not `for`. Reserving `for` as a map-access keyword creates no collision with any existing or planned construct. A reader familiar with C-family languages might initially expect `for` to mean iteration — but Precept's keyword-anchored design principle (§0.1.5) means statement kind is identified by the *opening* keyword, and `for` would never appear at statement-opening position. It only appears as an infix operator within an expression.

## 4. Precedent Check

`for` does not appear anywhere in Precept's grammar, keyword tables, sample files (checked all 28), or collection-types doc as a language keyword. The only occurrences of "for" in `.precept` files are inside string literals and comments. Zero tension.

## 5. Verdict: `for` is better than `at`

**Recommendation: adopt `for` over `at`.**

Reasons:
1. **Domain-natural phrasing.** Business users say "the limit *for* this type," not "the limit *at* this type." `for` matches how domain experts talk about keyed lookups.
2. **No grammar ambiguity.** `for` parses unambiguously in all three usage contexts (rule, action, guard). Binding precedence is straightforward — tighter than comparison operators, same tier as `.` member access.
3. **No existing keyword collision.** `for` is unused in Precept. No iteration constructs exist or are planned that would conflict.
4. **Avoids programmer connotation.** `at` suggests array indexing. `for` suggests relational lookup — which is what map access semantically *is* in a business-rule language.

The only counterargument for `at` is familiarity for developers who know `dict.at(key)` from C++ or similar. But Precept optimizes for domain readability (§0.1.5), not programmer familiarity.

---

# Decision Record: `map` Access Keyword — Working Syntax `for`, Decision Open

**By:** Frank  
**Date:** 2026-04-29  
**Status:** Open — pending owner sign-off

## Summary

`docs/language/collection-types.md` (§ Candidate 6: `map of K to V`) now documents the map key-access syntax using the infix keyword `for`:

```
CoverageLimits for CheckCoverage.CoverageType
```

This replaces the prior `.get(key)` method-call notation that appeared in the code example, proof-engine implications prose, and action surface bullet.

## What Changed

- Code block in the map candidate now uses `F for key` infix form.
- Proof engine implications updated: `F for key` requires a `containskey` guard.
- Action surface bullet now lists `for` (infix key access) instead of `.get(key)`.
- Multimap rejection section updated to reference `for` consistently.
- An **open-question callout** was added immediately after the grammar fit block, making it explicit that `for` is the working spelling but is **not finalized**. Both `for` and `at` are under consideration.

## Decision Status

**Not locked.** Shane is leaning toward `for` but has not committed. The keyword requires explicit owner sign-off before it can advance to a formal proposal or be implemented in the runtime, lexer, or parser.

## Why `for` is the current lean

`CoverageLimits for CheckCoverage.CoverageType` reads as natural English — "the coverage limits *for* that coverage type." It aligns with the business-analyst-readability filter. `at` is also plausible (`CoverageLimits at CheckCoverage.CoverageType`) and has precedent in index-access idioms across several languages, but `for` is more semantically precise in a key-value lookup context.

## Next Step

Owner decision required. When Shane locks the keyword, update the open-question callout to a resolved note and advance `map` to a formal GitHub issue.


---

### 2026-04-29: Quantifier predicate syntax — approved form

**By:** Shane (via Frank design advisory)

**Decision:** Quantifier predicates use keyword-first, English-readable form:
`quantifier binding in Collection (predicate)`
Keywords: `each` (universal), `any` (existential), `no` (negated existential).

**Rejected:** `.all(item, pred)` method-call form — violates three surface conventions
(method calls with args, binding variables, commas in expressions).

**Rationale:** `each item in Items (item > 0)` reads as English declaration.
`.all(item, item > 0)` reads as C# method invocation. Precept is keyword-anchored;
every construct opens with a keyword. Named binding over fixed `it` — avoids
nesting ambiguity. Three keywords over two+negation — `no` is more natural than
`not any` in business rule prose.

**Lexer impact:** `each` and `no` need new lexer entries. `all` reserved keyword
superseded by `each` — `each` is grammatically correct where `all` would be broken.


---

### 2026-04-29: Quantifier negated existential — `no` confirmed over `none`

**By:** Frank (design advisory, owner evaluation)

**Status:** Analysis complete — pending owner confirmation to lock

**Question:** Should the negated existential quantifier be `no` or `none`?

```precept
# Under consideration:
rule no a in Amounts (a < 0) because "No negative amounts permitted"
rule none a in Amounts (a < 0) because "No negative amounts permitted"
```

---

**Decision: `no` is the correct keyword. `none` is rejected.**

---

### 1. English Readability

`no binding in Collection` is natural English. `no` is a determiner — the same
word class as `each` and `any` in the quantifier family. "No item in Items" reads
as a direct English statement: "no item in this collection satisfies the predicate."

`none binding in Collection` is not grammatical English. `none` is a pronoun,
not a determiner. English speakers say "none of the items" — the pronoun requires
"of the" to connect to a noun. "None item" is a usage error in standard English.
A business rules author reading `none amount in Amounts (...)` would stumble.

With a descriptive binding name: `no amount in Amounts (amount < 0)` reads
as spoken English with essentially zero friction. `none amount in Amounts (...)` does not.

### 2. Grammar Symmetry

`each` / `any` / `no` are all English determiners. The three words form a
grammatically consistent set — same word class, same position before a bare noun.

`each` / `any` / `none` breaks the family. `none` is a pronoun. The word class
shifts mid-family, and the syntactic role of the keyword in the expression changes
subtly but incorrectly. This is the kind of inconsistency that erodes readability
over time without authors being able to identify exactly why something feels wrong.

### 3. Keyword Collision Assessment

`no` is already a registered keyword in the Precept lexer (Token: `No`, Text: `no`,
Context: "Prefix for `no transition`"). It appears in the spec under Keywords: Outcomes.

The alleged ambiguity with `-> no transition` is **not a real parsing problem**:

- `no transition` — `No` token followed by `Transition` keyword
- `no a in Amounts (...)` — `No` token followed by an `Identifier`

After the lexer emits `No`, the parser needs exactly one additional token of lookahead:
- Next token is `Transition` → outcome production
- Next token is an identifier → quantifier production

These productions are in entirely different positions in the grammar (action block
outcome vs. rule/guard predicate context), making the disambiguation trivially
unambiguous. There is no formal grammar ambiguity.

The practical implication: the `No` token already exists. Adding the quantifier
use case means `No` serves two grammatical roles in different productions. The spec
keyword table's context field for `No` will need to be expanded to document both
roles when the quantifier proposal advances to implementation. This is a
documentation task, not a parser task.

`none` avoids this dual-role situation entirely — it is currently unreserved. But
avoiding a dual-role for an already-overloaded token is not a strong enough reason
to choose a word that fails the English readability test. The dual-role is harmless.

### 4. Precedent

**Alloy** (the canonical formal specification language for set/relation predicates):
uses `no` as the negated existential quantifier: `no x: S | pred`. This is the most
directly relevant precedent — Alloy's quantifier surface (`all`, `some`, `no`) is
exactly the design space Precept is operating in.

**SQL**: `NOT EXISTS (SELECT ...)` — too verbose to be relevant to keyword choice.

**OCL**: No standalone negated existential keyword; negation is `not collection->exists(x | pred)`.

**LINQ**: No `.None()` in the standard library. Third-party extensions exist but are
non-canonical.

**Natural language logic**: "For no x in S, P(x)" — uses `no` as the quantifier.
"None of the x in S satisfies P" — uses `none` but requires "of the" structure.

Alloy's precedent is dispositive. It faced the identical design choice and chose `no`.

### 5. Spec Impact

The quantifier keyword table in `precept-language-spec.md` currently reads:

| Token | Text | Context |
|-------|------|---------|
| `All` | `all` | Universal quantifier / `modify all` / `omit all` (state-scoped) |
| `Any` | `any` | State wildcard (`in any`, `from any`) |

When the quantifier proposal advances, this table requires:
- `All` → retired or reassigned (already superseded by `each` per prior decision)
- `Any` → context column expanded to include "Existential quantifier binding"
- `Each` → new entry, universal quantifier binding
- `No` (existing entry under Outcomes) → context column expanded to include
  "Negated existential quantifier binding"

The Outcomes table `No | no | Prefix for no transition` row becomes:
`No | no | Prefix for no transition; negated existential quantifier`

---

**Verdict:** `no` stays. The prior decision (frank-quantifier-syntax.md) was correct.
The `no`/`none` question has a clear answer: `no` is the determiner, `none` is the pronoun,
and determiners are the right word class for this syntactic position. `each`/`any`/`no`
is a coherent grammatical family. Alloy confirms the precedent. The `no transition`
collision is non-issue parsing. No change to the approved syntax.

---

# Decision: `ordered` as Collection Modifier vs `sortedset` as Named Type

**Date:** 2026-04-29  
**Author:** Frank (Lead/Architect)  
**Status:** Pending owner sign-off  
**Verdict:** `sortedset` as a named type is correct. `ordered` stays scoped to `choice(...)` inner types. Do not introduce a collection-level `ordered` modifier.

---

## Question

Shane proposed reusing the existing `ordered` keyword at the collection level:

```precept
# Instead of:
field ApprovalLevels as sortedset of string

# Shane's proposal:
field ApprovalLevels as set of string ordered
```

The rationale: `ordered` already exists as a modifier on `choice(...)` inner types. Why introduce a new type name when the modifier approach reuses existing vocabulary?

---

## Analysis

### 1. What `ordered` currently means on `choice(...)`

`ordered` on `choice(...)` is a **modifier on the inner element type**. It declares declaration-position rank on the values of that choice type — `"low" < "medium" < "high"` by their index in the `choice(...)` list. This enables ordinal comparison operators (`<`, `>`, `<=`, `>=`) and makes `.min`/`.max` valid on collections whose inner type is `choice(...) ordered`.

The precise distinction: `ordered` governs **element comparability** (can these values be compared?), not **collection storage order** (in what sequence does the collection store its elements?). The `ordered` modifier answers the type-checker question "is this type orderable?" — it does not answer the runtime question "how are elements laid out in memory or iteration order?"

`set of choice("low", "medium", "high") ordered` is an unordered collection whose element type happens to support ordinal comparison. The set itself is still unordered — elements are stored with no guaranteed iteration sequence. Only the *type* carries rank.

### 2. What `set of T ordered` would mean

If `ordered` were allowed as a collection-level modifier, there are two distinct interpretations with no syntactic way to distinguish between them:

**Interpretation A — Sorted storage:** Elements maintained in sorted order by the inner type's comparer at all times. Equivalent to `SortedSet<T>` (C#), `TreeSet` (Java). Iteration yields ascending order.

**Interpretation B — Stable insertion order:** Elements maintained in the order they were added. Equivalent to `LinkedHashSet<T>` (Java), Python's `dict` post-3.7. Iteration yields insertion order.

Both are legitimately called "ordered" in common language. A business analyst reading `set of string ordered` cannot determine which semantics applies without consulting documentation. `sortedset of T` is unambiguous — the type name carries the full semantics before any constraints are read.

### 3. Semantic collision: the grammar is already occupied

`ordered` is already valid in the inner-type position of a collection declaration:

```precept
field RiskLevels as set of choice("low", "medium", "high") ordered
```

Here, `ordered` attaches to `choice(...)`. If collection-level `ordered` is introduced at the trailing position, this line becomes ambiguous:

- Is `ordered` still on the inner type (current meaning)?
- Has it migrated to the collection level (new meaning)?

The parser must decide. There is no single-token lookahead that resolves which of the two attachment sites an `ordered` at the end of the declaration belongs to.

And the double-modifier scenario:

```precept
field RiskLevels as set of choice("low", "medium", "high") ordered ordered
```

This is the unavoidable consequence: to have *both* semantics simultaneously, an author would need two `ordered` tokens. The grammar rule distinguishing "first `ordered` belongs to `choice(...)`, second belongs to the collection" is arbitrary and invisible to anyone reading the declaration. This is a grammar landmine.

The current spec is explicit: `ordered` is valid only on `choice` and is a type error on all non-choice types, including collections (`field Tags as set of string ordered` is invalid). Collection-level `ordered` would require dismantling that constraint and replacing it with positional disambiguation logic — adding complexity to gain nothing.

### 4. Grammar fit

Precept's style is keyword-anchored flat statements. The type name at the start of a declaration names the thing completely. Modifiers after the type name are **constraints** — properties of the declared type, not alternatives to its identity.

Compare the parse roles:

```precept
field Tags as set of string notempty              # set of string IS the type; notempty IS a constraint
field Tags as set of string ordered               # if ordered changes storage: the modifier IS the type
```

In the second form, `ordered` is not constraining `set of string` — it is redefining it. That violates the flat-statement style where the constraint position is for validation rules, not semantic variants.

`sortedset of T` follows the same pattern as all other collection types: the keyword names the thing, `of T` specifies the element type, and trailing tokens are constraints:

```precept
field ApprovalLevels as sortedset of choice("team-lead", "director", "vp", "cfo") ordered notempty
```

Here `sortedset` names the storage semantics, `choice(...) ordered` names the inner type with rank, `notempty` is a constraint. Every token is in the right position.

### 5. Precedent

Static type systems are unanimous: named types, not modifiers, for collections with different storage semantics.

| Language | Unordered unique set | Sorted unique set | Approach |
|---|---|---|---|
| C# | `HashSet<T>` | `SortedSet<T>` | Named type |
| Java | `HashSet<T>` | `TreeSet<T>` | Named type |
| Python | `set` | `SortedList` (sortedcontainers) | Named type |
| Kotlin | `hashSetOf()` | `sortedSetOf()` | Named function |
| Scala | `Set` | `SortedSet` | Named type |

The only domains that use modifier-for-ordering are query languages (SQL `ORDER BY`, LINQ `.OrderBy()`) — and those apply ordering at *query time* to a result set, not at *declaration time* to a stored collection. Precept is a declaration language, not a query language. That precedent does not transfer.

Python's `OrderedDict` is the canonical example of this choice made correctly: Python did not introduce `dict ordered`. It introduced a named type. The same reasoning applies here.

### 6. Extensibility

If `ordered` becomes a collection-level modifier, examine what it means across the full collection surface:

| Collection | Effect of `ordered` modifier | Problem |
|---|---|---|
| `queue of T` | Queues are already insertion-ordered. `ordered` → sorted? That changes FIFO to sorted-dequeue, breaking the queue contract. | Semantic collision with existing semantics. |
| `stack of T` | Stacks are LIFO. `ordered` → sorted? That changes push/pop semantics fundamentally. | Same collision. |
| `priorityqueue of T priority P` | Already has `ascending`/`descending` direction modifiers. `ordered` → redundant? conflicting? | Vocabulary noise. |
| `deque of T` | Double-ended. Sorted deque means neither push-front nor push-back preserves the invariant unambiguously. | Ambiguous interaction. |

The modifier does not generalize. It creates noise or ambiguity on every other collection type. `sortedset` is a clean, bounded addition with no surface area bleed. Its semantics are fully contained by the name and do not conflict with any existing modifier.

---

## Verdict

**`sortedset` as a named type is correct. `ordered` must stay scoped to `choice(...)` inner types.**

The reasons are conclusive and mutually reinforcing:

1. **Semantic precision.** `ordered` on `choice(...)` modifies element-type comparability. `sortedset` specifies collection storage semantics. These are different concepts. One keyword cannot carry both meanings without ambiguity.

2. **Grammar collision.** `ordered` is already valid — and already meaningful — in the inner-type position of collection declarations. Allowing it at the collection level too creates a genuine parsing ambiguity. The double-`ordered` scenario (`set of choice(...) ordered ordered`) is a grammar landmine with no clean resolution.

3. **Type-declaration style.** In Precept's flat-statement model, the type name carries the storage semantics. Trailing modifiers are constraints. Using a trailing modifier to silently change what type you get violates this invariance.

4. **Precedent.** Every major static type system uses named types for collections with different storage semantics. The modifier approach is a query-time pattern that does not transfer to declaration-time type design.

5. **Non-generalizable.** Collection-level `ordered` creates noise or semantic collision on `queue`, `stack`, `priorityqueue`, and `deque`. `sortedset` has no such bleed.

The `ordered` modifier has one meaning, one valid application site, and zero ambiguity today. The correct response to Shane's question is: those two `ordered` keywords are doing different things and belong at different semantic levels. Conflating them would be an architectural regression.

**Recommendation:** Keep `sortedset of T` in the candidate section at Medium priority. Keep `ordered` scoped to `choice(...)`. Do not introduce a collection-level `ordered` modifier now or in the future without a new design review.

---

# Decision: `sorted` as Collection-Level Modifier vs `sortedset` as Named Type

**Date:** 2026-04-29  
**Author:** Frank (Lead/Architect)  
**Status:** Pending owner sign-off  
**Verdict:** `sortedset` as a named type remains correct. `sorted` as a collection-level modifier is rejected. The grammar collision is gone — but the deeper objections survive the keyword swap.

---

## Question

Shane read the previous rejection of `ordered` as a collection-level modifier and pushed back — not on the grammar collision specifically, but on the principle. His counter-proposal:

```precept
field Tags as set of string sorted
field RiskLevels as set of choice("low", "medium", "high") ordered sorted
field Priorities as set of integer sorted
```

Constraints:
- `sorted` only applies where it makes semantic sense (`set of T` — not `queue`, `stack`, `priorityqueue`)
- `sorted` is only valid when inner type `T` is comparable (numeric, `string`, `~string`, `choice(...) ordered`)
- The principle: Precept is modifier-heavy and should stay consistent with that

This is not the same as the prior `ordered` proposal. It deserves an honest separate analysis.

---

## Analysis

### 1. Does `sorted` eliminate the grammar collision?

**Yes. Completely.**

`sorted` is a new keyword with no existing use anywhere in the Precept grammar. It does not appear in the inner-type position, the constraint position, or anywhere else. The declaration:

```precept
field RiskLevels as set of choice("low", "medium", "high") ordered sorted
```

is unambiguous: `ordered` attaches to `choice(...)` (inner-type modifier enabling ordinal comparison), `sorted` attaches to the collection (collection-level modifier). There is no positional ambiguity, no double-keyword nightmare, no parser disambiguation crisis.

The grammar collision was the easiest argument against `ordered`. It no longer applies to `sorted`. I acknowledge this clearly. Shane's keyword swap resolves the specific syntactic problem from the prior analysis.

### 2. Does the type constraint hold up?

**Yes, the enforcement is clean.**

The existing "orderable" predicate — already used to gate `.min`/`.max` validity — applies directly:

| Inner type | `sorted` valid? | Reason |
|---|---|---|
| `integer`, `decimal`, `number` | ✓ | Numeric — naturally orderable |
| `string`, `~string` | ✓ | String ordering — deterministic (`OrdinalIgnoreCase` for `~string`) |
| `choice(...) ordered` | ✓ | Declaration-position rank enables ordinal comparison |
| `boolean` | ✗ | Not orderable — Precept defines no comparison relation on boolean |
| `choice(...)` (without `ordered`) | ✗ | Unordered choice — no comparison relation, no natural sort key |

These are clean type errors. The type checker can enforce them by reusing the existing `IsOrderable(T)` predicate already gating `.min`/`.max`.

One subtlety: `set of choice("low", "medium", "high") sorted` (without `ordered` on the inner type) is a type error because `choice(...)` without `ordered` is not orderable. If the author wants sorted-set semantics, they need both modifiers: `set of choice(...) ordered sorted`. This is not a bug — it's the right rule. The coupling between `ordered` on the inner type and `sorted` on the collection is the same coupling that exists with `sortedset of choice(...) ordered`.

**Verdict on constraint enforcement: clean and implementable.**

### 3. Does `sorted` read as a modifier or as a type?

**It reads like a type — not a modifier. That's the problem.**

Precept's modifier vocabulary falls into three semantic categories:

| Category | Examples | What they do |
|---|---|---|
| Validation constraints | `notempty`, `nonnegative`, `positive`, `min`, `max`, `mincount`, `maxcount`, `minlength`, `maxlength` | Define what values are allowed; proof engine checks them |
| Structural/access declarations | `optional`, `writable` | Declare field presence / mutability baseline |
| Type-enrichment | `ordered` (on `choice(...)`) | Add a comparison relation to the inner type; enable operations that weren't valid before |

`sorted` fits none of these.

- It does not define what values are valid — a `set of integer sorted` and a `set of integer` accept exactly the same values.
- It does not govern field presence or mutability.
- It does not add a comparison relation to the inner type — the inner type's orderability exists independently.

What `sorted` does: it changes the **backing data structure** and **iteration behavior** of the collection. A `set of string sorted` uses a tree-backed implementation that maintains sorted order at all times. Iteration yields ascending order. `.min`/`.max` are always safe (when `notempty` is also present). A `set of string` uses a hash-backed implementation. Iteration yields arbitrary order.

These are **different things**, not the same thing with a constraint applied. `sorted` is doing type discrimination disguised as a modifier.

Compare the semantics carefully:

```precept
field Tags as set of string notempty maxcount 10
# → still a hash-backed set; notempty and maxcount are constraints on values
# → if you remove those modifiers, you still have the same underlying type

field Tags as set of string sorted
# → tree-backed set; sorted iteration; safe .min/.max
# → if you remove `sorted`, you have a fundamentally different type
```

`sorted` is not orthogonal to `set of string`. It selects a different implementation variant. The modifier is the type, which is exactly the failure mode my previous analysis described — and it applies here just as much as it did for `ordered`.

### 4. Precept's modifier philosophy — is `sorted` a category break?

**Yes. And it's more subtle than it first appears.**

The closest existing analog is `ordered` on `choice(...)`. That modifier adds comparison capability to an element type — it enables `<`/`>` and `.min`/`.max`. One could argue: doesn't `sorted` do something similar at the collection level — it enables safe `.min`/`.max` on the collection?

The analogy breaks at a critical point:

- `ordered` on `choice(...)` declares a **semantic property** of the element type: these values have a rank. The runtime doesn't need to change anything — values are still stored the same way; the type system now knows they can be compared.
- `sorted` on `set of T` directs a **runtime implementation choice**: use a different data structure. The type carries the same values either way; the difference is visible in iteration order and in how the backing structure maintains its invariant.

The first is a semantic declaration. The second is a storage directive. Precept's modifier vocabulary has never had a storage directive before. Introducing `sorted` would mean the modifier grammar now has two incompatible semantic categories: "what values are valid / how values are accessed" and "how the runtime stores the data." That is a design tax. Future proposals will ask why they can't use `persistent`, `indexed`, or `deduplicate` as similar storage directives — and the answer "because we only accepted one of these" is an arbitrary stop-rule.

### 5. Grammar position

The grammar position is **unambiguous and clean**. I have no objection here.

`sorted` appears in the trailing modifier position after the type reference:

```
FieldDecl  :=  field Identifier as CollectionType Modifier*
```

It coexists cleanly with other modifiers:

```precept
field Tags as set of string sorted notempty maxcount 10
```

Parse order for `set of choice(...) ordered sorted notempty`:
1. `set` — collection kind
2. `of choice("low", "medium", "high")` — inner type
3. `ordered` — inner-type modifier (attaches to `choice(...)`)
4. `sorted` — collection-level modifier (attaches to `set of T`)
5. `notempty` — validation constraint on the collection

Each step is unambiguous at LL(1). Grammar production is straightforward. The "only on `set`" restriction is type-checker enforcement, not grammar restriction — the parser admits it; the checker rejects it on `queue` and `stack` with `InvalidModifierForType`.

**No grammar objection to `sorted`. Position is clean.**

### 6. The named-type counterargument

`sortedset of T` is unambiguous, self-documenting, and follows a unanimous pattern.

Every major type system that distinguishes hash-backed from tree-backed containers uses **named types**:

| Language | Unordered unique set | Sorted unique set |
|---|---|---|
| C# | `HashSet<T>` | `SortedSet<T>` |
| Java | `HashSet<T>` | `TreeSet<T>` |
| Python | `set` | `SortedList` (sortedcontainers) |
| Kotlin | `hashSetOf()` | `sortedSetOf()` |
| Scala | `Set` | `SortedSet` |
| F# | `Set<'T>` (sorted by default) | distinct from `HashSet<T>` |

This is not an accident. It reflects a genuine insight: sorted-ness is a type-level property, not an attribute of a value. `SortedSet<T>` and `HashSet<T>` have different performance contracts, different iteration semantics, and different structural invariants. Type names carry that contract upfront. Modifiers don't.

Shane's modifier-consistency principle is valid — Precept IS modifier-heavy, and that's right. But the principle applies when an attribute is **orthogonal to type identity**. `notempty` is orthogonal — a `set of string` and a `set of string notempty` are the same type with a constraint applied. `sorted` is not orthogonal — `set of string sorted` and `set of string` are different implementations.

The principle applies when you're qualifying; it doesn't apply when you're selecting a variant.

Furthermore: if `sorted` ships as a modifier, `sortedset` as a named type becomes redundant syntax. The vocabulary is permanently split. If `sortedset` ships as a named type, there's nothing to reconcile — it stands alone. The named-type path is more forward-compatible.

### 7. The "only on set" restriction reveals type-level semantics

This is the structural argument that settles the question.

Shane explicitly restricts `sorted` to `set` only — not `queue`, `stack`, `priorityqueue`. That restriction is correct: `queue of T sorted` would mean sorted-dequeue, which breaks the FIFO contract entirely; `stack of T sorted` would break LIFO; these interactions are incoherent.

But notice what this restriction means: **`sorted` is a property that discriminates between collection kinds**. It is valid on `set`, invalid on `queue` and `stack`, because it defines a different behavioral category. That is what type names do. Modifiers that can only validly apply to one collection kind — and whose application changes the behavioral contract in ways that make it incompatible with other collection kinds — are type discriminators in disguise.

`notempty` (proposed), `mincount`, `maxcount` — these apply uniformly across all three collection kinds without semantic weirdness. `sorted` cannot. That asymmetry is not a modifier attribute; it's a type property.

---

## Verdict

**`sortedset` as a named type remains correct. `sorted` as a collection-level modifier is rejected.**

The grammar collision is gone — I said `ordered` had a real collision; `sorted` does not. That objection is fully resolved.

The remaining objections are not merely grammar preferences. They reflect structural arguments about what modifiers are and what type names are:

1. **`sorted` is a storage directive, not a validation constraint.** All Precept modifiers either constrain values, govern access, or enrich type semantics. `sorted` directs the runtime to use a different backing data structure. That is a different semantic register — one Precept has never placed in the modifier position.

2. **The "only on `set`" restriction is a type discriminator, not a modifier attribute.** A modifier that is valid on exactly one collection kind and whose semantics are incompatible with all other collection kinds is expressing type-level information. It belongs in a type name.

3. **`sortedset` is self-documenting and unambiguous.** An author reading `field ApprovalLevels as sortedset of choice(...) ordered notempty` immediately knows: sorted unique collection, ordered-choice inner type, non-empty constraint. An author reading `field ApprovalLevels as set of choice(...) ordered sorted notempty` must know what `sorted` means on a `set` — that it changes backing structure, that it affects iteration order, that it makes `.min`/`.max` safe. That is hidden complexity.

4. **Shannon's modifier-heavy principle is valid in general, but doesn't apply here.** Precept is right to be modifier-heavy where modifiers express orthogonal attributes. Sorted-ness is not orthogonal to set identity — it selects a variant. The principle applies when you're constraining; not when you're choosing an implementation.

5. **Precedent is unanimous.** Every static type system that solved this problem chose named types. The modifier approach is a query-time pattern; it does not transfer to declaration-time type design.

**Recommendation:** Keep `sortedset of T` in the §Proposed Additional Types section at Medium priority. Keep `sorted` out of the modifier vocabulary. If Shane disagrees with this verdict after reviewing the category-break and type-discriminator arguments, schedule a design review — but do not accept `sorted` as a modifier on the basis of modifier-consistency alone.

---

# Decision: Priorityqueue Design — Five Open Questions Resolved

**Date:** 2026-04-29
**Author:** Frank (Lead Language Designer)
**Scope:** `docs/language/collection-types.md` — `priorityqueue` subsection in § Proposed Additional Types
**Trigger:** Shane's answers to five open questions from frank-14

---

## Resolved Questions

### 1. Priority Direction

**Decision:** Direction is specifiable at both declaration and operation sites. Declaration sets the default; dequeue can override.

- Declaration: `field F as priorityqueue of T priority P descending`
- Operation override: `-> dequeue F ascending into Target priority PTarget`
- Default is `ascending` (lowest value dequeued first) when no modifier is specified.
- Modifier follows its target in both positions, consistent with Precept's modifier-follows-target grammar.

### 2. Peek Behavior

**Decision:** `.peek` returns the element value (type `T`); `.peekpriority` returns the priority value (type `P`).

Two separate scalar accessors rather than a compound return. This is consistent with Precept's flat accessor model — every existing accessor returns a single scalar. Both require `.count > 0` emptiness guard.

### 3. Priority-Type Constraints (Quantifier Predicates)

**Decision:** Quantifier predicates (`each`/`any`/`no`) work on the priority axis via a two-field binding.

When iterating a `priorityqueue`, the binding variable exposes `.value` (element, type `T`) and `.priority` (priority, type `P`). Example: `no claim in ClaimQueue (claim.priority < 2)`.

This is a meaningful design precedent: single-type collections produce bare scalar bindings; two-type-parameter collections produce two-field projection bindings. The pattern generalizes to `map` (`.key`/`.value`).

### 4. Dequeue Priority Capture

**Decision:** Approved syntax: `-> dequeue F into Target priority PriorityTarget`.

`PriorityTarget` receives the dequeued item's priority value, typed as the declared priority type. The `priority` capture clause is optional — omitting it discards the priority value.

### 5. Grammar Alignment with `map`

**Decision:** Generalized two-type-parameter grammar using role-connector keywords.

```
TwoParamCollectionType :=
    priorityqueue of ScalarType priority ScalarType TypeQualifier?
  | map of ScalarType to ScalarType TypeQualifier?
```

`priority` and `to` are role-connector keywords introducing the secondary type parameter. `of` introduces the primary type. This is the canonical pattern for all two-type-parameter collection types in Precept.

---

## New Open Questions Surfaced

1. **Quantifier binding shape inference.** The type checker must distinguish bare-scalar bindings (single-type collections) from two-field projection bindings (two-type-parameter collections). Should this be implicit (inferred from collection type) or explicit?

2. **`ascending`/`descending` keyword scope.** These may also apply to `sortedset` iteration or future ordering features. Reserve broadly or scope to `priorityqueue`?

---

## Artifacts Updated

- `docs/language/collection-types.md` — full priorityqueue subsection rewritten with all five decisions integrated
- Priority Summary table and Recommended Rollout updated
- Comparison table updated to `priorityqueue of T priority P`


---

## Decision: `priorityqueue` requires explicit priority type in field declaration

**Date:** 2026-04-29
**Author:** Frank (Lead Language Designer)
**Status:** Advisory — pending owner decision
**Scope:** `priorityqueue of T` proposal in `docs/language/collection-types.md`

### Context

The current `priorityqueue` proposal declares only the element type (`priorityqueue of string`). The priority value passed to `enqueue ... priority Expr` has no declared type in the field declaration. Shane identified this gap.

### Decision Recommendation

The field declaration must include an explicit priority type parameter:

```
priorityqueue of T priority P
```

Concrete example:

```precept
field ClaimQueue as priorityqueue of string priority integer
```

### Rationale

1. **No-runtime-faults principle (spec §0.1 items 7, 10, 11).** If the priority type is inferred from the first `enqueue` site, the compiler cannot verify type consistency across multiple enqueue sites without first resolving all action chains — and cannot verify orderability at declaration time. Explicit declaration gives the type checker a proof anchor at registration pass time, before any expression is checked.

2. **Prevention, not detection.** Precept's identity is that invalid configurations are structurally impossible. An undeclared priority type means the compiler discovers mismatches reactively (detection) rather than preventing them structurally (prevention). The declaration is the contract.

3. **Precedent: existing collection grammar.** The shipped `CollectionType := (set | queue | stack) of ScalarType` uses a single type parameter. `priorityqueue` is the first collection kind requiring two type parameters. The `map of K to V` proposal (high priority) also requires two type parameters with a different connector (`to`). The `priority` keyword naturally mirrors this: `of` introduces the element type, `priority` introduces the priority type — each connector names the role of its type parameter.

4. **Orderability proof obligation.** The priority type must be orderable (the queue must know which element has highest priority). Explicit declaration lets the compiler enforce this at the field declaration, not at first usage. If `P` is `choice(...)`, the `ordered` modifier is required — same pattern as `set of choice(...) ordered`.

5. **Inference weakness.** Inference from first usage creates fragile proof surface: reordering event declarations could change which `enqueue` site the compiler sees first, and error messages would point to usage sites rather than the declaration. Explicit declaration is the Precept way.

### Corrected Grammar

```
CollectionType  :=  (set | queue | stack) of ScalarType TypeQualifier?
                 |  priorityqueue of ScalarType priority ScalarType TypeQualifier?
```

The second `ScalarType` (after `priority`) must be orderable — numeric types, `date`, `time`, `instant`, `duration`, or `choice(...) ordered`.

### Open Questions Surfaced

1. **Priority direction:** Is highest-value-first or lowest-value-first the default? Should there be a `descending`/`ascending` modifier? (e.g., `priorityqueue of string priority integer descending`)
2. **Priority accessor:** Should `.peek` return just the element, or a pair of (element, priority)? Is there a `.peekpriority` accessor?
3. **Priority constraints:** Can field modifiers apply to the priority type? (e.g., `priorityqueue of string priority integer min 1 max 5`) — and if so, where do they attach syntactically?
4. **Dequeue semantics:** Does `dequeue ClaimQueue into CurrentClaim` discard the priority, or is there a way to capture it? (e.g., `dequeue ClaimQueue into CurrentClaim priority CurrentPriority`)
5. **Grammar alignment with `map`:** Both `priorityqueue of T priority P` and `map of K to V` are two-type-parameter collections. Should the grammar production be generalized, or kept as separate productions?

---

# Collection Surface Re-evaluation: Three Challenges Answered

**Date:** 2026-04-29  
**Author:** Frank (Lead/Architect)  
**Status:** Pending owner sign-off  
**Scope:** Response to Shane's three challenges following the `sorted`-modifier rejection and `bag`/`list` analysis

---

## Challenge 1: "Enriches element-type semantics" vs "enriches collection-type semantics"

### Shane's challenge

You accepted `ordered` on `choice(...)` because it "enriches element-type semantics." But `sorted` on `set of T` enriches the collection's semantics — it declares "this collection maintains sort order." Both change runtime behavior. Why is one a legitimate enrichment and the other a storage directive?

### Assessment: the distinction is real, but my prior explanation was imprecise

The prior explanation correctly identified the distinction but stated it in a form that is vulnerable to this challenge. Here is the precise formulation.

**`ordered` on `choice(...)`** — what does it actually do?

Before `ordered`, the values `"low"`, `"medium"`, `"high"` have no comparison relation. The expression `"low" < "medium"` is a type error. The comparison relation does **not exist** in the type system until `ordered` is applied. The modifier creates something that was not previously there. This is semantic enrichment at the type level — the type system gains new knowledge about the values' relationships.

**`sorted` on `set of T` where T is already orderable (e.g., `integer`)**  — what does it actually do?

Before `sorted`, `set of integer` already knows how to compare values. The expression `1 < 2` is valid. `.min`/`.max` are already available (with emptiness guards). The comparison relation already exists. `sorted` does not add any capability to the type system — it changes how the collection stores and iterates its elements. This is a storage and behavioral directive — the runtime uses a different data structure.

**The precise test:** Does the modifier add a capability to the type system that was not previously present? Or does it use an already-present capability to direct runtime behavior?

- `ordered` on `choice(...)` — ADDS a capability (comparison). Before `ordered`, you cannot compare choice values. After `ordered`, you can.
- `sorted` on `set of T` — USES an already-present capability to change storage. Before `sorted`, you can already compare `integer` values. After `sorted`, you still compare them the same way — the collection just stores them in a tree instead of a hash.

This is not a thin distinction. It maps cleanly onto: "does the modifier change what the type system knows?" vs "does the modifier change what the runtime does with knowledge the type system already has?"

**Verdict on Challenge 1:** The distinction between semantic enrichment and storage directive is real and defensible. My prior reasoning was correct; the explanation needed sharpening. No revision to the `sortedset` verdict is required.

---

## Challenge 2: Is `set` the only collection that could be `sorted`?

### Shane's challenge

My structural kill-shot was: "a modifier valid on exactly one collection kind is a type discriminator, not a modifier." Shane points out that `sorted` could apply to both `set` and `list`:

- `set of string sorted` → sorted set (unique + sorted)
- `list of string sorted` → sorted list (duplicates + sorted)

If `sorted` applies coherently to both, the kill-shot fails.

### Honest evaluation: the kill-shot as stated was too strong

Shane is right. `sorted` DOES generalize to `list`. If `list` ships, `list of string sorted` is a coherent concept: a sorted bag — maintains elements in sorted order, allows duplicates. Python's `sortedcontainers.SortedList` is exactly this. The concept exists and is useful.

The "only valid on exactly one collection kind" test was a good heuristic but not the correct test. I stated it too strongly. Correcting the record.

### The correct test — and why the verdict still holds

The correct test is not "how many collection kinds does it apply to?" The correct test is:

**Does the modifier change the operation surface of the target type — or does it merely constrain the values the type accepts?**

Modifiers that constrain values are genuine modifiers:
- `notempty` on `set` — same operations, now requires ≥1 element. Doesn't remove or add operations.
- `mincount N` on `queue` — same operations, now requires ≥N elements. Doesn't remove or add operations.

Modifiers that change the operation surface are type discriminators:
- `sorted` on `list` — `list of string sorted` cannot support `insert at index` or `remove-at`. Position in a sorted list is determined by value, not by the author. The arbitrary-position mutation that is `list`'s defining territory over `log`/`queue`/`stack` is gone. `sorted` does not constrain the values `list` accepts — it REMOVES the operations that make `list` what it is.
- `sorted` on `set` — `set of string sorted` vs `set of string` are different behavioral contracts. Iteration order changes. The backing structure changes (tree vs hash). `.min`/`.max` become O(1) vs O(n). These differences are visible to the author and the proof engine in ways that go beyond value constraints.

When removing a modifier gets you a fundamentally different type — not just an unconstrained version of the same type — the modifier is doing type-selection, not qualification.

Compare:
- Remove `notempty` from `set of string notempty` → `set of string`. Same type, more permissive.
- Remove `sorted` from `set of string sorted` → `set of string`. Different storage contract, different iteration semantics, different `.min`/`.max` performance. Not the same type with a constraint removed — a different type.
- Remove `sorted` from `list of string sorted` → `list of string`. Operations reappear (`insert at index`, `remove-at`). The sorted list is not a constrained list — it's a different operational contract with fewer capabilities.

**Revised kill-shot:** A modifier that changes the operation surface or behavioral contract of the target type — rather than constraining the values it accepts — is a type discriminator, not a modifier. `sorted` fails this test on both `set` and `list`.

### Does the `sortedset` verdict need revision?

No. The conclusion is unchanged. The underlying argument is stronger on the revised formulation because:

1. `sorted` on `set` changes the behavioral contract (storage, iteration, performance). Named type is correct.
2. `sorted` on `list` removes capabilities from the type. If both apply, the modifier is doing different things to different types — which means it carries type-level information the modifier position cannot express cleanly.
3. The collection type system still needs `sortedset` as a named type. The "sorted list = sorted bag" concept would need its own named type too (a type that's currently out of scope).

**Verdict on Challenge 2:** The kill-shot as stated was too strong. The revised formulation — "changes the operation surface rather than constraining values" — is the correct test. The verdict (reject `sorted` as a modifier; `sortedset` as a named type is correct) stands under the revised test.

---

## Challenge 3a: Is `bag` a good business word?

### Shane's challenge

Business analysts don't say "bag." They might loosely say "list." `bag` is computer science jargon (multiset). What do business users actually call an unordered collection with multiples?

### Assessment

This deserves a direct answer. Let me work through the alternatives honestly:

| Word | Assessment |
|---|---|
| `multiset` | Mathematically precise. More jargon than `bag`, not less. Computer scientists know it; analysts don't. |
| `tally` | Captures the "counting" angle well. But suggests only a count aggregate, not a collection of items. `tally of string` reads like a counter, not a collection. |
| `inventory` | Domain-specific. Perfect in retail; wrong in claims processing, project management, or finance. |
| `quantities` | Plural noun, grammatically awkward as a type keyword. |
| `counter` | Python's name for this concept. Reads like a device ("a counter on the wall") not a collection type. |
| `list` | Already designated for a different type (ordered mutable sequence). Reusing it would create permanent ambiguity between "ordered sequence" and "frequency-tracking collection." |
| `bag` | 1 syllable. CS-technical but short. Established usage: Perl's `Bag`, Guava's `Multiset`/`Bag`, SQL's `MULTISET`, Python's `Counter`, `sortedcontainers.SortedList`. |

The business case for `bag`: a business analyst working with order quantities, claim frequencies, or inventory counts will learn `bag` in a single sentence of documentation — "bag = an unordered collection that tracks how many of each thing you have." The word is short, distinct, and learnable.

The more important point: the reason `bag` struggles in business vocabulary is not that it's unfamiliar — it's that business domains don't often name this concept at all. They write `items: 3x SKU-001, 1x SKU-007` in a spreadsheet, not "a bag of items." The CONCEPT is ubiquitous even if the word isn't. When the doc says "bag of T is the right type when you need to track quantities or frequencies — how many of X you have," the concept clicks immediately.

The alternatives that would satisfy "a business analyst uses this word naturally" are all domain-specific (`inventory`, `tally`) and would be just as unfamiliar to analysts in adjacent domains. `bag` is the least wrong option.

**One real risk:** authors may reach for `list` when they mean `bag`, because "list" is how business people speak casually about any collection. The documentation and diagnostics need to be explicit: `list of T` = ordered sequence, `bag of T` = quantity-tracking collection.

**Verdict on Challenge 3a:** `bag` is the correct keyword. The alternatives are worse. The documentation must do the work of explaining what `bag` means in business terms on first use. No verdict change.

---

## Challenge 3b: Does `set = bag with unique` suggest a unified design?

### Shane's proposal

If `set` is mathematically a bag with uniqueness enforced, should the design reflect that? Proposed unified model:

- `bag of T` — unordered, duplicates
- `bag of T unique` — unordered, no duplicates (= current `set`)
- `bag of T sorted` — sorted, duplicates
- `bag of T unique sorted` — sorted, no duplicates (= current `sortedset`)

### Evaluation

The mathematical relationship (set = bag with uniqueness) is real. The question is whether it should manifest in the language surface.

**Arguments the unified model gets right:**

1. It is internally consistent — four combinations from two modifiers.
2. It acknowledges the mathematical family.
3. It would make `sorted` legitimate if it weren't for the operation-surface argument above.

**Arguments the unified model gets wrong:**

**1. `set` is the shipped canonical type. `bag` is a proposed new type.** You do not redesign a shipped, documented, tested, and philosophically grounded type as a derived form of something that doesn't exist yet. The direction of derivation is wrong. `set` predates `bag` in the design. Making `set` a modifier-qualified variant of `bag` creates an awkward retrofitting problem with the existing surface.

**2. The common case requires a modifier; the uncommon case is the base.** In the domain scenarios where collections appear in `.precept` files, `set`-style membership tracking (tags, categories, allowed values, assigned interviewers) is FAR more common than `bag`-style quantity tracking (order line items, claim frequencies). Requiring business analysts to write `bag of string unique` for the common case when `set of string` already exists is backward ergonomics. Precept's surface should make the common case as clean as possible.

**3. `bag`'s primary value is `.countof(element)`, which has no natural role on `set`.** The defining differentiator of `bag` over `set` is the ability to ask "how many of X do I have?" — `.countof(element)`. On a `bag of string unique`, every element is present 0 or 1 times. The `.countof(element)` accessor returns only 0 or 1. It's not wrong, but it's not useful. The author just wants `contains`. This is a design smell: the primary accessor of the base type is degenerate on the most common variant.

**4. `unique` on `bag` changes the operation surface.** A `bag of T unique` can no longer hold duplicates — the `add` operation's behavior changes (from "increment count" to "no-op if present"). This is the same operation-surface test from Challenge 2. `unique` is not just constraining values; it's changing what `add` does. That's a type-level distinction.

**5. The mathematical insight doesn't transfer to language design.** `integer` and `natural number` are related mathematically (naturals = integers with `nonnegative`), but Precept doesn't have `integer nonnegative` as its primary vocabulary for non-negative integers — it uses the `nonnegative` modifier as a constraint on an already-natural type. Similarly, the modifier-heavy design works for adding constraints on top of a stable type, not for deriving one canonical type from another. `set` and `bag` are different enough in their design intent and primary accessor surface to warrant distinct names.

**Verdict on Challenge 3b:** The unified-bag model is rejected. `set` and `bag` are distinct named types. They have different primary use cases, different action emphasis, and different accessor contracts. The mathematical relationship is noted but does not imply shared vocabulary roots. `set` remains the canonical keyword for unique-membership collections. `bag` is evaluated as a new High-priority candidate for quantity-tracking collections.

---

## The "base types vs semantic variants" framing

Shane's framing proposal: `set`, `bag`, `list` as three base types; `queue`, `stack`, `priorityqueue`, `log`, `map` as semantic variants.

### Assessment: partially right, but misrepresents Precept's design philosophy

The framing captures a real mathematical relationship: `queue`, `stack`, `log`, and `priorityqueue` are all ordered sequences — mathematically, they could be described as `list` with access restrictions. The mathematical structure is there.

But in Precept, the TYPE NAME IS THE CONTRACT. `queue of T` does not mean "list of T with FIFO access restriction" — it means FIFO processing is the designed intent, and the type enforces it structurally. An author who declares `field AgentQueue as queue of string` is communicating a domain decision, not selecting an implementation. The type name is the declaration. The operational contract is the identity.

Calling `queue`, `stack`, `log`, and `priorityqueue` "semantic variants" of `list` would:

1. Suggest they could be replaced by `list` + modifiers — which is exactly the `sorted` modifier problem in reverse. `queue` is NOT a `list` with a FIFO modifier. The FIFO constraint is not orthogonal to the type — it IS the type.
2. Undervalue the business clarity of named types. When a domain expert sees `field ServiceQueue as queue of string`, they don't need to read modifiers to understand FIFO semantics. The name carries the contract immediately.
3. Create a precedent for "why can't I write `list of T fifo`?" — which is a grammar expansion we explicitly don't want.

A more accurate taxonomy for Precept, organized by design intent:

| Family | Types | Organizing principle |
|---|---|---|
| Membership collections | `set`, `bag` | What things you have, with or without frequency |
| Sequenced access | `queue`, `stack`, `log`, `priorityqueue` | The ACCESS PATTERN is the defining constraint |
| Sorted membership | `sortedset` | Like `set`, but tree-backed with sorted iteration |
| Indexed mutable sequence | `list` (low priority candidate) | Ordered with arbitrary positional access/mutation |
| Associative | `map` | Key-value structure, lookup by key |

In this taxonomy, there are no "base types" — every type IS its contract. `queue` is not a variant of `list`; `bag` is not a variant of `set`. Each name is a complete statement of the collection's behavioral identity.

**Verdict on framing:** The `set`/`bag`/`list` as base types framing is partially right (it correctly clusters the unordered membership types together) but wrong in classifying `queue`, `stack`, `log`, and `priorityqueue` as variants. In Precept, sequenced-access types are domain contracts, not constrained lists.

---

## Summary of Verdict Changes

| Question | Prior verdict | Revised verdict |
|---|---|---|
| Semantic enrichment vs storage directive | Correct but imprecisely stated | Same conclusion, sharper formulation: `ordered` ADDS a comparison capability that didn't exist; `sorted` USES existing comparison capability to change storage |
| Kill-shot on `sorted` modifier | "Only valid on one kind" — overstated | Corrected to: "changes the operation surface rather than constraining values" — verdict unchanged |
| Can `sorted` apply to `list`? | Not addressed | Yes — `sorted list` is coherent as sorted bag. My prior formulation was too strong. But revised kill-shot holds. |
| `bag` vocabulary | Acceptable | Still acceptable — alternatives are all worse; documentation must explain business-facing meaning |
| Unified bag model (`set = bag unique`) | Not previously evaluated | Rejected. Wrong direction of derivation, wrong accessor ergonomics, operation-surface change. |
| `set`/`bag`/`list` as base types | Not previously evaluated | Partially right; wrong to treat `queue`/`stack`/`log`/`priorityqueue` as variants rather than domain contracts |

**Net result:** Two prior arguments needed sharpening (the semantic enrichment distinction and the kill-shot formulation). One argument was too strong as stated (`sorted` valid on exactly one kind). The verdicts — `sortedset` as named type, `sorted` as modifier rejected, `bag` acceptable vocabulary, unified-bag model rejected — all stand.

---

# `list of T` — Oversight Acknowledgment and Full Evaluation

**By:** Frank  
**Date:** 2026-04-29  
**Status:** Decision — Low priority; add to doc as evaluated candidate (not reject, not high)

---

## Was It an Oversight?

Yes. Plainly.

The `§ Proposed Additional Types` section evaluated sorted sets, multisets, double-ended queues, priority queues, append-only logs, ring buffers, bounded collections, and multimaps. What it never evaluated — not as a candidate, not as an explicit reject — was the most fundamental indexed collection type in every general-purpose ecosystem: the ordered, duplicates-allowed, index-accessible sequence. `List<T>` in .NET. `ArrayList` in Java. `list` in Python. `Vec<T>` in Rust.

This is confirmed by the comparison table. Python's `list` appears in two rows — once as the LIFO stack surrogate, once as the "immutable" append-only stand-in — but never in its primary role as a general-purpose ordered sequence with random access. There is no row for "Ordered sequence with random access" in the table at all. It was mapped piecemeal into other roles and the base concept was never evaluated on its own.

There is no principled reason it was skipped. The research started from the question "what collection types exist across ecosystems?" and somehow the most universal one ended up absorbed into other rows. This is a gap in the research, not a deliberate design decision.

---

## Evaluation: `list of T`

### What It Is

An ordered sequence of elements, allowing duplicates, with stable insertion-order positions. Elements can be appended, inserted at an arbitrary index, removed at an arbitrary index, and read by index.

Differentiators from existing types:
- `queue of T` — ordered + duplicates, but FIFO-only (no random access, remove only from front)
- `stack of T` — ordered + duplicates, but LIFO-only (no random access, remove only from top)
- `log of T` — ordered + duplicates, but append-only (no removal at all, positions stable)
- `set of T` — unordered + no duplicates

`list of T` is the only type that combines: ordered insertion, preserved positions, duplicates, arbitrary insertion, AND arbitrary positional removal.

### Business Scenario

The genuine use case for `list` (and only `list`) is an ordered sequence where arbitrary removal is permitted:

```precept
field ApprovalChain as list of string

from Draft on AddReviewer
    -> append ApprovalChain AddReviewer.ReviewerName
    -> no transition

from Draft on RecuseReviewer when ApprovalChain contains RecuseReviewer.ReviewerName
    -> remove ApprovalChain RecuseReviewer.ReviewerName
    -> no transition

rule ApprovalChain.count >= 2
    reason "At least two reviewers required"
```

This specific pattern — ordered list of named reviewers, with recusal-style removal — cannot be modeled by `queue` (which destroys order through dequeue semantics), `stack` (LIFO only), or `log` (no removal). The scenario is real but not widespread.

Most business-rule scenarios that feel like they need `list` resolve cleanly into one of the more constrained types when you examine the actual invariant:
- "Ordered history that accumulates" → `log`
- "Items processed in order" → `queue`
- "Items processed in reverse" → `stack`
- "Membership tracking" → `set`

### Proof Engine Implications

**`.at(N)` — index access.** A new proof obligation category: index-bounds safety. The proof engine must verify:
- `N >= 0` (trivially discharged when N is a literal)
- `N < F.count` (requires a guard when N is a variable)

This is tractable. The pattern follows the same model as emptiness obligations on `.peek`/`.first`/`.last`, extended to a two-sided bounds check. Authors must supply:

```precept
from State on AccessItem when Items.count > EventArgs.Index
    -> set Result = Items.at(EventArgs.Index)
```

**`insert(index, element)` and `remove-at(index)` — positional mutation.** This is the hard problem. When elements are removed or inserted at arbitrary positions, all subsequent positions shift. A proof established for "element at index 3 is X" is invalidated by a subsequent `remove-at(1)`. The proof engine has no stable positional invariant to reason from — unlike `log` where positions are monotonically stable (append-only means index 3 stays index 3 forever), a mutable list's positions are dynamic.

This does not make the type impossible, but it means the proof engine effectively cannot offer anything beyond the basic index-bounds check on each individual access. Any attempt to reason about "what value is at position N after this sequence of mutations" requires tracking full mutation history — which is not how Precept's proof engine is designed to work.

**`append` (safe case).** If the action surface is restricted to append + remove-from-back (LIFO) or append + remove-from-front (FIFO), the proof surface simplifies. But that's just `stack` or `queue`. The incremental value over those types is specifically the arbitrary-position access and mutation, which is the proof-hard part.

### Grammar Fit

```
field F as list of T
```

Reuses the existing `of` connector and all existing constraints (`mincount`, `maxcount`, `notempty`, `optional`). Clean grammar fit — no new keywords at declaration time.

### Action Surface

| Action | Syntax | Behavior |
|--------|--------|----------|
| `append` | `append F Expr` | Add element to end. Always safe. |
| `insert` | `insert F at Expr Expr` | Insert element at index. Proof: index in bounds. |
| `remove-at` | `remove-at F Expr` | Remove element at index. Proof: index in bounds. |
| `remove` | `remove F Expr` | Remove first occurrence of value. No-op if absent. |
| `clear` | `clear F` | Remove all elements. Always safe. |

| Accessor | Returns | Proof |
|----------|---------|-------|
| `.count` | `integer` | None |
| `.at(N)` | `T` | `N >= 0 and N < F.count` |
| `.first` | `T` | `.count > 0` |
| `.last` | `T` | `.count > 0` |

### Relation to `bag` and `log`

**`list` vs `bag`:** Orthogonal. `bag` tracks element frequencies without positional structure — `bag.countof("X")` is meaningful, position is not. `list` tracks positional structure without frequency tracking — `list.at(2)` is meaningful, duplicate counts are not first-class. Neither type subsumes the other; they address different business-rule dimensions.

**`list` vs `log`:** Related but distinct by design intent. `log` is ordered + append-only — positions are stable because nothing is ever removed. `list` is ordered + mutable — positions shift when elements are inserted or removed. The critical observation: **the most valuable part of `list` — ordered insertion + positional read access — is already in `log`'s candidate write-up** (`log` already includes `.at(N)`, `.first`, `.last`). The incremental territory `list` adds over `log` is exactly the thing `log` prohibits by design: arbitrary removal.

`list` does not render `log` redundant. `log` is the right type when the business rule is "immutable record" (audit trail, compliance history). `list` would be the right type when the business rule is "ordered sequence where elements can be retracted." These are genuinely different domain semantics — but `log`'s constraint is a feature, not a limitation.

### Does `list` Render Either `bag` or `log` Redundant?

No. Each serves a distinct purpose:
- `bag` — counting occurrences, quantity tracking (how many of X)
- `log` — immutable ordered history (what happened, in order, permanently)
- `list` — mutable ordered sequence (ordered positions, arbitrary changes)

### Priority Assessment

**Low.** Here is the reasoning:

1. **The read-access half of the value proposition collapses into `log`.** If the business need is "ordered sequence with positional read," `log.at(N)` already addresses it in the `log` candidate. `list` adds arbitrary removal on top of that, which is a different (and weaker) semantic commitment.

2. **The business scenarios that genuinely require `list` over `log`/`queue`/`stack` are narrow.** The "ordered sequence with recusal removal" pattern exists, but it's not the pervasive need that `bag` (quantity tracking) and `log` (audit compliance) are. Most ordered-sequence business rules have a cleaner semantic fit with append-only or FIFO/LIFO access.

3. **The proof engine cost of arbitrary positional mutation is real.** `log`'s append-only invariant gives the proof engine stable positions to reason from. `list`'s arbitrary mutation does not. The proof engine provides index-bounds checks on individual accesses, but it cannot verify positional invariants across mutations. This is not a fatal flaw, but it is a genuine cost that needs to be accepted consciously.

4. **Evaluate after `log` ships.** If `.at(N)` on a `log` satisfies the real use cases that appear in practice, `list` may not need to exist as a separate type. If real-world `.precept` files reveal a concrete need for ordered-with-removal that `log`/`queue`/`stack` genuinely cannot serve, the case for `list` strengthens.

This is not a reject — ring buffer was rejected on philosophy grounds (silent eviction violates "nothing is hidden"). `list` has no philosophy violation. It is simply low-priority relative to its proof-engine complexity cost and narrow incremental territory over `log`.

---

## Decision

1. **Acknowledge the oversight.** `list of T` was not evaluated in the original research pass. This is documented here as a gap, not a deliberate decision.

2. **Add `list of T` to `collection-types.md` as a Low priority candidate**, evaluated with this write-up. It belongs in the priority table between `deque` (low) and the rejects.

3. **Defer to after `log` ships.** Real-world usage will reveal whether `.at(N)` on `log` satisfies the positional-read use cases. Only then evaluate whether a mutable `list` addresses a genuine gap.

4. **Comparison table gap.** A "Ordered sequence with random access" row is missing from the `§ Comparison With Other Collection Systems` table. This should be added: `.NET List<T>`, Java `ArrayList`, Python `list` (primary role), Rust `Vec<T>`, SQL (none), CEL (none), Haskell (none) → Precept proposed `list of T` (low pri).

---

## Action Items

- [ ] Add `### Candidate 7: list of T` to `docs/language/collection-types.md` with this evaluation
- [ ] Add "Ordered sequence with random access" row to the comparison table
- [ ] Update Priority Summary table to include `list of T` at Low priority (below `deque`)
- [ ] Note in `log of T` candidate: the positional read overlap with `list` is deliberate — `log` covers the common case (immutable ordered history + read-by-index) and `list` is evaluated separately for the mutable-sequence use case

---

# Decision: `sortedset of T` — Value Assessment

**Date:** 2026-04-29  
**Author:** Frank (Lead/Architect)  
**Status:** Pending owner sign-off  
**Verdict:** Reject. `set of T notempty` covers every legitimate use case. No Precept construct can observe sorted iteration order. The benefit claimed for `sortedset` is owned entirely by `notempty`, not by sorted storage.

---

## The Challenge

Shane's challenge:

> "Why would iteration order matter? There are no loops in Precept."

The context: quantifiers (`each`, `any`, `no`) are boolean predicates — they don't accumulate, fold, or produce ordered sequences. Rules and guards consume `.count`, `.min`, `.max`, `contains`, and quantifiers. The question is whether `sortedset` adds anything that `set` with equivalent constraints does not.

---

## Analysis

### 1. Is there any Precept construct where iteration order affects correctness?

**No.**

Every operation in Precept's current and near-future surface is order-independent:

| Operation | Order sensitivity |
|---|---|
| `each F (predicate)` | Boolean predicate — result is true or false regardless of iteration order |
| `any F (predicate)` | Same — short-circuits on first match, but the result is the same regardless |
| `no F (predicate)` | Same — the boolean result is iteration-order-independent |
| `.count` | Cardinality — order-independent |
| `.min` | Finds the minimum value — the minimum is the minimum regardless of storage order |
| `.max` | Finds the maximum value — same |
| `contains` | Membership test — order-independent |
| `add` / `remove` / `clear` | Mutations — the author never observes sort order during mutation |

There is no Precept construct that has access to "the third element" or "the element at position N." There is no folding. No sequence consumption. No accumulation where order affects the result.

**Sorted iteration order is structurally unobservable in Precept's surface today and in every near-future proposal.**

---

### 2. The `.min`/`.max` argument

The existing candidate description says:

> "A `sortedset` with a `notempty` constraint makes these always-safe."

This framing is wrong about which ingredient provides the safety. The `notempty` is doing all the work. Consider these two declarations:

```precept
field ApprovalLevels as sortedset of choice("team-lead", "director", "vp", "cfo") ordered notempty
# .max is always safe

field ApprovalLevels as set of choice("team-lead", "director", "vp", "cfo") ordered notempty
# .max is also always safe — identical proof obligation
```

The proof engine discharges the `UnguardedCollectionAccess` obligation by checking whether `notempty` (or `mincount 1`) is declared on the field. It does not inspect whether the backing storage is a tree or a hash. `set of T notempty` satisfies the `.min`/`.max` proof obligation identically to `sortedset of T notempty`.

The performance difference — O(1) access to min/max on a tree-backed sorted set vs O(n) linear scan on a hash-backed set — is real but irrelevant in a DSL governing business contract entities. Collections in `.precept` files contain tags, categories, approval levels, and fee-schedule keys — not high-frequency streaming data. The O(n) scan over a set of 3–50 elements is not a performance problem anyone in this domain cares about.

**The `.min`/`.max` safety argument is wholly owned by `notempty`. `sortedset` contributes nothing to the proof obligation or its discharge.**

---

### 3. Declaration intent without observable behavior

The residual argument for `sortedset` is declaration intent — the type name signals that sort order is semantically meaningful to the author.

But if no Precept construct can observe the sort order — no loop, no folding, no ordered sequence consumer, no index access — then the author is declaring intent *about something that cannot be verified or consumed by the language*. That is not intent; it is noise.

Worse: it is a false signal. A reader seeing `field ApprovalLevels as sortedset of choice(...) ordered` infers that sort order matters to some computation in the precept. If the only consumers are `.min`, `.max`, and quantifiers — all order-independent — the type declaration has misled the reader.

A type's declaration should reflect a behavioral contract that is visible in the language. `set` declares: unordered, unique membership. `sortedset` would declare: sorted, unique membership — where "sorted" is semantically invisible to every operation the language exposes. That is a type with no observable type boundary. It is indistinguishable from `set` at the language surface.

**Intent without observable behavior is noise, not signal. A type that cannot be distinguished from another type by any language construct is not a type — it is an implementation detail wearing a type costume.**

---

### 4. Performance

Tree-backed sorted set: O(log n) insert. Hash-backed set: O(1) average insert.

In a DSL governing business integrity contracts — loan applications, insurance claims, policy records — collections are small. Approval levels, required reviewers, permitted fee categories. The O(log n) vs O(1) distinction is noise at those sizes. There is no domain where this tradeoff matters to anyone writing a `.precept` file.

The only scenario where the tree structure pays its cost is high-frequency insertion into large collections — exactly the workload Precept is not designed for. The performance argument cuts against `sortedset`, not for it.

---

### 5. Could a future construct observe sorted order?

Theoretically: if Precept ever adds ordered-iteration constructs — folding, sequence-consuming aggregations, index access by rank — `sortedset` would become observable. But:

1. Such constructs conflict directly with Precept's "no loops" design principle.
2. Every near-future proposal described in the language research (`deque`, indexed access) is either low-priority or explicitly out of scope.
3. "Keep this type because a future construct might use it" could justify any type. The bar for speculative future use is not met here — especially when the speculative construct contradicts the language's own philosophy.

If Precept ever adds ordered-iteration constructs, the design review at that time will include the question "do we need a sorted variant of set to support this?" That is the correct moment to introduce `sortedset`. Not now, when it is semantically invisible to everything in the language.

---

## Verdict

**Reject `sortedset of T`.**

| Claim | Assessment |
|---|---|
| No Precept construct observes iteration order | Confirmed. Quantifiers, `.min`/`.max`, `contains`, `.count` — all order-independent. |
| `.min`/`.max` safety | Owned by `notempty`, not sorted storage. `set of T notempty` is proof-identical. |
| Declaration intent | Vacuous when nothing in the language can observe the declared property. |
| Performance | Irrelevant. Business-rule collections are small. O(log n) vs O(1) is noise. |
| Speculative future use | Does not meet the bar. Speculative constructs that contradict the language's philosophy are not a valid design anchor. |

`set of T` with the appropriate `notempty` / `mincount` constraint covers every real use case that was claimed for `sortedset`. `sortedset` adds tree-backed storage with O(log n) inserts, carries a type name that signals a property no consumer can observe, and delivers nothing the existing surface cannot already express.

**Recommendation:** Move `sortedset of T` from Candidate 1 in the Proposed Additional Types section to a `### Rejected:` subsection, alongside `ringbuffer`, `bounded collection`, and `multimap`. Update the Priority Summary table. Remove from the Rollout plan. Note the rejection in the comparison table. Remove the Open Question about `ascending`/`descending` keyword reuse for `sortedset`.

**Impact on collection taxonomy:** The "Sorted membership" family described in prior decisions becomes an empty family. The correct collection taxonomy for Precept's surface does not include a sorted-membership category. If a future ordered-iteration construct arrives and makes sorted order observable, `sortedset` can be re-evaluated at that time with an actual consuming construct to justify it.

---

# ScalarType Extension — Temporal and Business-Domain Inner Types

**Author:** Frank (Lead/Architect)
**Date:** 2026-04-29
**Status:** Verdict delivered — pending owner sign-off before doc update

---

## The Question

`collection-types.md` line 238 says:

> "Collections of temporal or business-domain types are not currently supported — the inner type must be a primitive."

The current grammar:

```
ScalarType := string | ~string | integer | decimal | number | boolean | choice(...) ordered?
```

Shane is asking why. This document delivers the answer.

---

## 1. Temporal Types — Inventory and Ordering

Eight types from `temporal-type-system.md`:

| Type | Backing | `<`/`>` | Equality | Notes |
|------|---------|---------|----------|-------|
| `date` | `NodaTime.LocalDate` | **Yes** | Calendar date | Total ordering. `'2026-01-01' < '2026-06-01'` is unambiguous. |
| `time` | `NodaTime.LocalTime` | **Yes** | Time of day | Total ordering. No timezone — comparison is unambiguous. |
| `instant` | `NodaTime.Instant` | **Yes** | UTC nanosecond | Total ordering. UTC by definition — no ambiguity. |
| `duration` | `NodaTime.Duration` | **Yes** | Nanosecond magnitude | Total ordering. |
| `datetime` | `NodaTime.LocalDateTime` | **Yes** | Calendar date+time | Total ordering. Timezone-free — comparison is unambiguous. |
| `period` | `NodaTime.Period` | **No** | **Structural only** | `'1 month' != '30 days'` — structurally different components. NodaTime deliberately omits `IComparable<Period>` because month length varies. `.min`/`.max` unavailable. `contains` works (structural match). |
| `timezone` | `NodaTime.DateTimeZone` | **No** | IANA identifier | Identity type. `==`/`!=` only. Analogous to `boolean` in this respect — `set of boolean` is valid, `boolean.min` is a type error. |
| `zoneddatetime` | `NodaTime.ZonedDateTime` | **No** | Instant + local + zone compound | NodaTime deliberately omits `IComparable<ZonedDateTime>` — ordering semantics are ambiguous (by instant? by wall clock?). Doc says: compare via `.instant` or `.datetime` accessor instead. `==`/`!=` compare all three components. Valid equality for set deduplication, but niche. |

**Summary:** Five temporal types are fully orderable (`date`, `time`, `instant`, `duration`, `datetime`). Three are equality-only (`period`, `timezone`, `zoneddatetime`) — for the same principled NodaTime reason in each case, not a Precept restriction.

---

## 2. Business-Domain Types — Inventory and Ordering

Seven types from `business-domain-types.md`:

| Type | Backing | `<`/`>` | Equality | Notes |
|------|---------|---------|----------|-------|
| `money` | `decimal` + ISO 4217 currency | **Yes** (same currency) | Same currency | `'100 USD' < '200 USD'` is valid. Cross-currency `<` is a type error — same rule as cross-currency `+`. |
| `currency` | ISO 4217 code (string) | **No** | Code equality | Identity type. Currency codes have no natural ordering. `'USD' < 'EUR'` has no business meaning. |
| `quantity` | `decimal` + UCUM unit | **Yes** (same dimension, auto-converts) | Same dimension | `'5 kg' < '10 kg'` is valid. `'5 kg' < '10 lbs'` auto-converts (D8). Cross-dimension `<` is a type error. |
| `unitofmeasure` | UCUM unit identifier | **No** | Unit identifier | Identity type. `'kg' < 'lbs'` has no business meaning. |
| `dimension` | Dimension category (string) | **No** | Category equality | Identity type. `'mass' < 'length'` has no business meaning. |
| `price` | `decimal` + currency + unit | **Yes** (same currency + unit) | Same currency + unit | `'4.17 USD/each' < '5.00 USD/each'` is valid. Cross-currency or cross-unit `<` is a type error. |
| `exchangerate` | `decimal` + currency pair | **No** | Same currency pair | Doc explicitly notes: "Exchange rates have no meaningful ordering outside their time context." `==`/`!=` require same currency pair. Limited utility in collections but not structurally wrong. |

**Summary:** Three business-domain types are orderable (`money`, `quantity`, `price`). Four are equality-only (`currency`, `unitofmeasure`, `dimension`, `exchangerate`).

---

## 3. Is the Restriction Principled?

**No. It is not principled. It is incidental.**

Evidence:

**A. The doc says so.** Line 238 says "not currently supported" — explicitly "not yet," not "by design." Open Question 8 in the same doc says directly: "There is no principled reason to exclude temporal types... from collection inner types. This appears to be an incremental build artifact (collections and typed scalars landed at different times) rather than an intentional restriction."

**B. Every type satisfies the collection inner type requirements:**
1. Single value — not a collection type itself. ✓ All satisfy this.
2. Well-defined equality semantics. ✓ Every type has `==`/`!=`.
3. No nested collections. ✓ None are collections.

**C. The non-orderable cases are already handled by existing precedent.** `boolean` is in ScalarType. `set of boolean` is valid. `.min`/`.max` on `set of boolean` is a type error because `boolean` is not orderable. The same `TypeTrait.Orderable` check that governs this behavior applies to `period`, `timezone`, `zoneddatetime`, `currency`, `unitofmeasure`, `dimension`, and `exchangerate`. The machinery exists. Nothing new is needed to handle non-orderable inner types.

**D. The `in`/`of` qualifier question is grammar design, not a semantic barrier.** `set of money in 'USD'` needs parser disambiguation — does `in 'USD'` bind to `money` (inner type qualifier) or to the collection declaration? This is solvable by the same disambiguation pattern used for `set of choice(...) ordered` — `ordered` attaches to `choice(...)`, and `in '<currency>'` would attach to `money`. This is a grammar design task, not a reason to exclude the types.

---

## 4. What Would `set of date` Look Like?

Real business scenarios that collections of temporal types already demand:

**`set of date`:**
```precept
field BlackoutDates as set of date
field ValidFilingDates as set of date
field ContractMilestones as set of date
```

- `BlackoutDates contains '2026-07-04'` — is today a blackout day?
- `ValidFilingDates.min` — earliest valid filing date (`.count > 0` guard required, just like `set of integer`)
- `ValidFilingDates.max` — latest valid filing date
- `each d in BlackoutDates (d > Today)` — quantifier over a set of dates — entirely natural

The day-counter simulation machinery noted in the temporal doc (60+ lines of boilerplate across 3 samples) is a direct consequence of not having `date` as an inner type. Authors are using `set of integer` to hold day offsets. This is exactly the problem `set of date` solves.

---

## 5. What Would `set of money` Look Like?

Real business scenarios that collections of monetary types demand:

**`set of money in 'USD'`:**
```precept
field ApprovedFeeAmounts as set of money in 'USD'
field DisallowedPaymentThresholds as set of money in 'USD'
field AcceptedBidAmounts as set of money in 'USD'
```

- `ApprovedFeeAmounts contains '150 USD'` — is this fee on the approved list?
- `ApprovedFeeAmounts.min` — the minimum approved fee (guarded)
- `ApprovedFeeAmounts.max` — the maximum approved fee (guarded)

**Open `set of money` (no currency qualifier):** A set mixing `'100 USD'` and `'85 EUR'` is problematic — what does `.min` return? This is analogous to the cross-dimension `quantity` problem. The design answer is: `.min`/`.max` require a proven-same-currency proof, or the field must be declared `as set of money in 'USD'`. This is not a blocker — it is a type-checker constraint, the same kind of constraint that applies to `money + money` (cross-currency is a type error).

---

## 6. Incidental or Principled — Verdict

**Incidental.** The restriction came from the order of implementation: `set`/`queue`/`stack` shipped against the primitive type vocabulary. Temporal and business-domain types were designed later. The `ScalarType` grammar was never updated when the new types arrived. No design review ever said "temporal types must not be collection inner types." No architecture note captures a reason. The Open Questions section in the same doc explicitly identifies this as an artifact.

The proof: if you try to construct a principled reason for excluding `date` from `set of date`, you can't. Equality is deterministic. Ordering is total. `contains` is safe. `.min`/`.max` are safe with a count guard. There is no argument.

---

## 7. The `~string` Question

Does `~string` have a parallel for temporal or business-domain types? **No, and this is the expected and correct answer.**

`~string` addresses a single concern: case-insensitive string comparison. It is a modifier that changes the comparer from `StringComparer.Ordinal` to `StringComparer.OrdinalIgnoreCase`.

Temporal types have no concept of case. `'2026-01-01'` is `'2026-01-01'` — there is no upper/lower case in a date. Business-domain types are similarly immune: ISO 4217 enforces uppercase for currency codes (`USD`, not `usd`), UCUM enforces lowercase for units (`kg`, not `KG`), and all comparisons are already case-normalized by the parsing layer before they enter the type system. The `~` modifier is not applicable, not analogous, and not needed for any of these types.

---

## 8. Verdict and Updated Grammar

**Expand `ScalarType` to include all Precept scalar types.** The restriction is an incremental build artifact with no principled basis.

**Proposed expanded grammar:**

```
CollectionType    :=  (set | queue | stack) of ScalarType

ScalarType        :=  PrimitiveType | TemporalType | BusinessDomainType

PrimitiveType     :=  string
                   |  ~string
                   |  integer
                   |  decimal
                   |  number
                   |  boolean
                   |  choice(...) ordered?

TemporalType      :=  date
                   |  time
                   |  instant
                   |  duration
                   |  period (PeriodQualifier)?
                   |  timezone
                   |  zoneddatetime
                   |  datetime

PeriodQualifier   :=  in '<unit>'
                   |  of ('date' | 'time' | 'datetime')

BusinessDomainType :=  money (in '<currency>')?
                    |  currency
                    |  quantity ( (in '<unit>') | (of '<dimension>') )?
                    |  unitofmeasure
                    |  dimension
                    |  price (PriceQualifier)?
                    |  exchangerate

PriceQualifier    :=  in '<currency>/<unit>'
                   |  in '<currency>' of '<dimension>'
                   |  in '<currency>'
                   |  in '<unit>'
                   |  of '<dimension>'
```

**Ordering and accessor availability — the full table:**

| Inner type | Orderable (`.min`/`.max`) | Equality (`contains`) | Notes |
|------------|--------------------------|----------------------|-------|
| `date` | ✓ | ✓ | |
| `time` | ✓ | ✓ | |
| `instant` | ✓ | ✓ | |
| `duration` | ✓ | ✓ | |
| `datetime` | ✓ | ✓ | |
| `period` | ✗ | ✓ (structural) | `.min`/`.max` are type errors. `'1 month' != '30 days'` in a set. Needs documentation. |
| `timezone` | ✗ | ✓ | `.min`/`.max` are type errors. Same pattern as `set of boolean`. |
| `zoneddatetime` | ✗ | ✓ (compound: instant + local + zone) | Niche but valid. `.min`/`.max` type errors. |
| `money in 'USD'` | ✓ | ✓ | Same-currency required for ordering. |
| `money` (open) | ✗ (mixed currencies) | ✓ (same currency match) | `.min`/`.max` are type errors without proven same-currency. Needs documented restriction. |
| `currency` | ✗ | ✓ | `.min`/`.max` are type errors. Identity type. |
| `quantity in 'kg'` | ✓ | ✓ | Same-dimension with auto-convert for comparison. |
| `quantity of 'mass'` | ✓ (commensurable) | ✓ | Same-dimension rule applies. |
| `quantity` (open) | ✗ | ✓ (same unit match) | `.min`/`.max` type errors without proven dimension. |
| `unitofmeasure` | ✗ | ✓ | Identity type. `.min`/`.max` type errors. |
| `dimension` | ✗ | ✓ | Identity type. `.min`/`.max` type errors. |
| `price in 'USD/each'` | ✓ | ✓ | Same currency+unit required. |
| `price` (open) | ✗ | ✓ (same currency+unit match) | `.min`/`.max` type errors without proven currency+unit. |
| `exchangerate` | ✗ | ✓ (same pair) | `.min`/`.max` type errors. Limited utility but not wrong. |

**Types I would exclude — none.** There is no type in either family that fails the three collection inner type requirements (single value, equality semantics, not a collection). Even the equality-only types are valid as inner types — they just can't use `.min`/`.max`, which is already the case for `boolean`.

**Grammar complexity note.** `in`/`of` qualification in inner type position requires parser disambiguation. `set of money in 'USD'` must parse as `set of (money in 'USD')`, not as something ambiguous. The same disambiguation pattern used for `set of choice(...) ordered` applies: after consuming the inner type keyword (`money`, `quantity`, `price`, `period`), check for `in` or `of` and consume the qualifier as part of the inner type spec. This is a grammar design task, not a semantic barrier. Owner sign-off needed on grammar before doc update.

---

## Action Required

1. **Shane: approve semantic verdict.** The restriction should be removed. `ScalarType` should expand to include all Precept scalar types. No principled reason for the exclusion exists.

2. **Grammar design for `in`/`of` in inner type position.** Before the doc is updated, the parser disambiguation strategy for `set of money in 'USD'` needs to be locked. This is mechanical but needs an explicit decision.

3. **Documentation notes for edge cases.** When the doc is updated, three cases need explicit callouts:
   - `period` in a set: structural equality means `'1 month' != '30 days'`
   - `zoneddatetime` in a set: compound equality (instant + local + zone) — two `zoneddatetime` values representing the same moment in different timezones are different elements
   - Open `money`/`quantity`/`price` in a set: `.min`/`.max` require a same-currency/same-unit proof; use qualified inner types (`money in 'USD'`) for cleaner semantics

4. **One stale reference to resolve.** Open Question 8 in collection-types.md mentions `percentage` as a business-domain type. No such type appears in `business-domain-types.md`. Either `percentage` was planned and not designed yet, or the reference is stale. Shane should clarify.

**Do not update the doc until Shane approves.**

---

# `choice(...)` Design Analysis

**By:** Frank, Lead Architect  
**Date:** 2026-04-29  
**Status:** Consolidated — key decisions locked by owner 2026-04-29

**Owner decision (2026-04-29):** `set ChoiceField = stringVariable` → **TypeMismatch**. Choice is a sealed type. String variables cannot be assigned to choice fields. Only choice-typed sources (with subset-compatible value sets) and compile-time string literals in the declared set are valid.

---

## Owner Note (2026-04-29, added during fresh assessment)

Shane: *"I guess the real value of choice is showing the user a drop-down."*

This may be the load-bearing insight. `string + rule` already provides prevention. What `choice` uniquely adds — over and above a rule — is a **UI rendering signal**: "render this field as a dropdown, not a free-text input." Tooling (language server, form generators, admin UIs) reads the choice set to offer a closed list of options. The type system enforcement may be secondary to the presentation metadata.

This reframes the whole design question: is `choice` a *type*, a *constraint*, or a *rendering hint*? The answer determines the right model.

---

## Orientation: What the Gap Actually Is

Before evaluating options, let's be precise about the failure. The problem is not that `choice` lacks non-string support. The problem is a broken identity rule: **choice types have no identity beyond the field that declares them.** Each declaration is its own isolated universe. You cannot reference it, match against it, or route through it. The dynamic arg case is the most acute symptom, but the root cause is the absence of any structural type identity for choice.

Every option below is really an answer to the question: *what is the identity of a choice type?*

---

## Option A: Structural Equivalence — Keep String-Only, Add Type Identity

**What this means precisely:** Two `choice(...)` declarations are the same type if their string value sets are equivalent. For **unordered** choice: set equality (order-independent — `choice("a","b")` ≡ `choice("b","a")`). For **ordered** choice: sequence equality (order defines rank — `choice("low","medium","high")` ≡ `choice("low","medium","high")` but ≢ `choice("medium","low","high")`). The declaration sequence is the ordinal rank definition; two ordered-choice types with different sequences have different rank semantics and are incompatible for ordinal comparison.

**Grammar impact:** Zero. This is a purely type-checker change. The AST is unchanged. The type identity check changes from reference equality ("same declaration node") to value equality ("same set or sequence of string literals").

**Behavior changes:**
- An event arg `Level as choice("low","medium","high")` is now structurally compatible with `field Priority as choice("low","medium","high")`. Assignment `set Priority = SetPriority.Level` becomes valid.
- The runtime validates incoming arg values against the declared choice set at fire time — membership enforcement, not just type-system enforcement.
- Ordinal comparison between two structurally equivalent ordered choice values (`SetPriority.Level > Priority`) becomes valid when both carry the same ordered sequence. The compile-time check is: identical sequence → same rank definition → comparison is well-defined.
- Ordinal comparison between two ordered choices with the same *members* but different *sequences* remains a type error — the rank definitions conflict.

**Concrete syntax (Option A):**

```precept
field Priority as choice("low", "medium", "high") ordered default "low"

event SetPriority(Level as choice("low", "medium", "high") ordered)

from Active on SetPriority
    -> set Priority = SetPriority.Level   # valid: structurally equivalent types
    -> no transition

# Comparison also valid — identical sequence, same rank
from Active on SetPriority when SetPriority.Level > Priority
    -> set Priority = SetPriority.Level
    -> no transition
```

```precept
# Subset arg — different choice set → still a type error
event Promote(Target as choice("medium", "high") ordered)  # ≢ choice("low","medium","high") — different set
-> set Priority = Promote.Target   # TypeMismatch
```

**Pros:**
- Closes the dynamic arg gap completely with zero grammar changes
- Backward compatible — no existing `.precept` files break
- Conceptually honest: types defined by structure, not by declaration node
- Implementation scope is localized to the type checker's choice equality rule

**Cons:**
- Repeating the entire set in the event arg declaration is verbose for large choice sets (6+ options)
- Does not address non-string values
- Ordinal rank edge case: if you have `choice("low","medium","high") ordered` (field) and `choice("low","medium","high")` (unordered arg), assignment is valid (same set, demoting ordering is safe widening), but ordinal comparison is a type error (arg has no rank). The type checker must track whether `ordered` is in play for comparison resolution, not just for assignment.

**Philosophy fit:** Strong. Structural typing for closed-set literals is the principled model. The closed type vocabulary (§0.4, Property 4) is preserved — `choice(...)` remains a built-in type constructor, not a user-defined type. No new language concepts introduced.

**Assessment:** This should happen regardless of what else is decided. It is the minimum viable fix and has no downsides at the language level. Every other option builds on top of it.

---

## Option B: Typed Choice — `choice of T`

**What this means:** The `choice` type constructor accepts an optional element-type qualifier, consistent with the collection type pattern (`set of T`, `queue of T`). `choice of string(...)` is the explicit form of the current `choice(...)`. `choice of integer(...)` and `choice of decimal(...)` extend to non-string types.

**Grammar:**

```
ChoiceType := choice ("of" ChoiceElementType)? "(" ChoiceValueExpr ("," ChoiceValueExpr)* ")"
ChoiceElementType := string | integer | decimal | number
ChoiceValueExpr   := StringExpr | IntegerLiteral | DecimalLiteral | ...
```

The `of` keyword here is the same connector used in `set of T` — it specifies the element type. `choice("a","b")` and `choice of string("a","b")` are the same type (backward-compatible default).

**Concrete syntax (Option B):**

```precept
# String choice (current behavior preserved)
field Status as choice("draft", "active", "closed") default "draft"

# Integer choice — explicit codes
field ErrorCode as choice of integer(0, 404, 500) default 0

# Decimal choice — fixed rate tier
field TaxRate as choice of decimal(0.0, 0.05, 0.10, 0.20) default 0.0

# Event arg with typed choice
event SetErrorCode(Code as choice of integer(0, 404, 500))

from Active on SetErrorCode
    -> set ErrorCode = SetErrorCode.Code
    -> no transition
```

**Ordinal rank for integer/decimal choice:**

The natural expectation when you see `choice of integer(3, 1, 2) ordered` is ambiguous — is the rank numeric order or declaration order? Decision: **Keep declaration-order rank universally.** The asymmetry of "integer choice uses numeric order" is a special case that adds complexity for minimal gain. A business analyst writing `choice of integer(404, 500, 200)` who wants 200 < 404 < 500 will be surprised either way unless behavior is explicitly documented. The simpler contract: **the order you declare is the order of rank.** This rule is identical for string and numeric choice. List values in the rank you want.

**The `ordered` modifier on integer choice:** `ordered` is the *capability gate* — it enables ordinal operators on the field, regardless of underlying type. Without `ordered`, even a `choice of integer` field is equality-only. This is important: it distinguishes "this field has an ordered meaning" from "the underlying integers can be compared." You may declare `choice of integer(500, 404, 200)` where you specifically DON'T want numeric ordering exposed — just constraint on valid values, not rank.

**Pros:**
- Principled generalization, consistent with `set of T` grammar
- Closes real use cases: error codes, numeric priority levels, fixed rate tiers, integer status codes
- Default backward-compatible — existing `choice(...)` is unchanged
- Structural equivalence (Option A) carries over naturally to typed choice

**Cons:**
- Grammar complexity: parser must handle `choice of T (values)` vs `choice (values)`, requiring one-token lookahead after `choice`
- Type checker must handle non-string choice values in assignment, comparison, and validation
- The `of` keyword is already used in two other contexts (collection inner type connector, and `of 'DimensionFamily'` qualifier). Adding `choice of T` is a third use — unambiguous by context, but the catalog must document this
- Existing `TypeMismatch` diagnostic for non-string choice values becomes version-gated

**Philosophy fit:** Solid. The collection types established the `of T` pattern as the element-type idiom. Extending to choice is consistent. The closed type vocabulary is maintained — `choice of integer` is a built-in parameterized type, not a user-defined type.

**Assessment:** Right direction, but a separate proposal from Option A. Do Option A first; bring Option B as a follow-on once A is stable.

---

## Option C: Named Choice Types — `type Priority = choice(...)`

**What this means:** A new top-level declaration form introduces named aliases for choice sets. Fields and event args reference the name.

```precept
type Priority = choice("low", "medium", "high") ordered
field Priority as Priority default "low"
event SetPriority(Level as Priority)
```

**The conflict with the closed type vocabulary principle:**

The language spec (§0.4, Property 4) explicitly states: *"Closed type vocabulary. The language has a fixed set of types. No user-defined types, no parametric polymorphism, no open type hierarchies. This makes exhaustive type checking possible over a finite, fully-known vocabulary."*

Named types require:
- A new top-level declaration scope (symbol table entries for type names)
- A scoping model (file-local only? namespace-scoped? importable?)
- Reference resolution at every field/arg declaration site
- Rules about forward declarations and ordering
- For cross-file use: an import or reference mechanism

This is a substantial language expansion. It changes the language's identity model from "types are structural, closed, built-in" to "types can be named and declared."

**Where Option C is strongest:** It is the cleanest solution to the reuse problem. A 10-value choice set used in 8 fields and 3 event args requires repeating all 10 values 11 times under Option A. Option C eliminates that entirely.

**Assessment:** Option C is not wrong as a design direction — it's wrong as an *immediate* decision. It requires a separate, dedicated proposal that explicitly revisits §0.4, Property 4 and defines the scope rules. Log it as a future proposal. Do not conflate it with the current cluster.

---

## Option D: Choice Is a Read-Only Set — Conceptual Unification

**The semantic argument for separation:**

| Concept | What the field HOLDS | Cardinality |
|---|---|---|
| `set of string` | A **collection of string values** — the field IS the set | Zero to N values |
| `choice("a","b","c")` | A **single string value** constrained to a domain | Exactly one value |

A `set of string` field answers: *"what values does this entity currently hold?"*  
A `choice("a","b","c")` field answers: *"what is this entity's current status/category/mode?"*

`Tags contains "urgent"` is a runtime test — the answer changes as tags are added/removed.  
`Status contains "active"` would mean... what? Either: "is the current value of Status equal to 'active'?" (that's `Status == "active"`, not `contains`) or "is 'active' a declared member of the Status type?" (always compile-time knowable for literals).

There is no runtime use case for `contains` on a scalar choice field.

**Where the observation has genuine merit:** `set of choice(...)` already works and is correct. The collection-types doc notes `set of choice("low","medium","high") ordered` — a field holding a *set* of priority values with `.min`/`.max` by rank. This is the correct composition. Do not collapse choice into set.

**Assessment:** Reject. Choice is NOT a read-only set. Do not add `contains` to scalar choice fields. The scalar vs. collection distinction is fundamental.

---

## Cross-Cutting: The `''` Notation Question

**What `''` means today:** Typed constants (`'...'`) are for values that carry a unit or dimension qualifier — `'1 USD'`, `'5 km'`, `'3 months'`. The `''` delimiter signals: "this value's meaning requires a qualifier."

**For `choice of integer(1, 2, 3)` — should values be `'1'`, `'2'`, `'3'` or `1`, `2`, `3`?**

**Bare integer literals. Definitively.**

1. `'1'` in typed-constant context means "a typed constant whose text is `1`" — which requires the type checker to resolve the numeric type from the surrounding `choice of integer` context, exactly as it already does for bare `1`. The `''` adds zero information.
2. The `''` notation carries the mental model of "unit-qualified value." An integer with no unit is not a typed constant in that sense. Writing `choice of integer('1', '2', '3')` imports the wrong mental model.
3. Consistency: `field MaxCount as integer min 1 max 100 default 5` — all integer literals are bare. No reason choice values should differ.
4. The business analyst audience (§2 of philosophy.md) writes numbers as numbers.

**The `''` notation is wrong for numeric choice values.** Use bare literals.

---

## Recommendation Summary

### Lock now — Option A

**Structural equivalence** in the type checker. Two `choice(...)` types are equal if:
- **Unordered:** their string value sets are equal (order-independent)
- **Ordered:** their string value sequences are equal (different sequences → different types → ordinal comparison is a type error)

No grammar changes. Zero breaking changes. Closes the dynamic arg gap.

### Propose next — Option B

`choice of T` as a follow-on proposal. Bare literals. Declaration-order rank universally. `ordered` remains the capability gate for all typed choice.

### Defer — Option C

Named types. Requires a dedicated proposal explicitly revisiting §0.4 Property 4 and defining scope rules.

### Reject — Option D

`contains` on scalar choice fields. The semantic difference between a scalar constrained value and a collection is fundamental.

---

## Decision Table

| Question | Answer |
|---|---|
| Is choice a read-only set? | No. Choice is a scalar type with a constrained value domain. |
| Should `contains` work on a choice field? | No. `contains` tests membership in a collection. A choice field is not a collection. |
| Should `choice of integer` use bare literals or `''`? | Bare literals. `''` is for unit/dimension-qualified typed constants. |
| Should `ordered` for integer choice use numeric rank or declaration rank? | Declaration rank, consistent with string choice. |
| Should `ordered` still be required for integer choice ordinal operators? | Yes. `ordered` is the intent signal. |
| What closes the dynamic arg gap today? | Option A: structural equivalence in the type checker. No grammar changes needed. |
| What is the right long-term model for non-string choice values? | Option B: `choice of T`, following the established `set of T` pattern. |
| Should named types be introduced to solve the reuse problem? | Not in this cluster. Requires a dedicated proposal addressing §0.4 Property 4. |

---

## Addendum: The `''` Notation Question (Revisited)

**Date:** 2026-04-29  
**Occasion:** Shane's UX disambiguation argument — not addressed in the original analysis.

---

### The argument I didn't address

My prior analysis dismissed `''` for numeric choice values on the grounds that it imports the wrong mental model (unit-qualified constants) and adds zero information. That's correct for the `choice of integer` case. But Shane's argument is different, and it applies to the **untyped string case** I didn't question:

> `choice("normal", "high")` — the `""` implies these are strings. Users will expect `set Status = "normal"` to work like any string assignment. But it doesn't: choice enforces membership. The `""` sets the wrong expectation about what the field accepts.

> `choice('normal', 'high')` — the `''` signals "typed literal, type determined by context." That communicates the value is *constrained* — not a free string you can substitute at will.

This is a UX framing argument about the **declaration site**, not the assignment site. It deserves a direct response.

---

### Is the `''` signal coherent here?

The spec (§1.3) defines the two delimiter forms with precise, non-overlapping contracts:

- `"..."` — **always produces `string`**. The type is fixed by the delimiter.
- `'...'` — produces **non-primitive values**. The lexer treats content opaquely; the type checker resolves the specific type from context.

The content validation table (§3.3) enumerates every valid context for `'...'`: `date`, `time`, `instant`, `datetime`, `money`, `quantity`, `period`, `duration`, `timezone`, etc. Every row is a non-primitive type. There is no `choice` row, and there cannot be one without adding a new resolution path.

Here is why that matters: a choice value like `'normal'` is not a non-primitive value. At the runtime level, a choice field holds a **string**. The choice constraint is a membership rule applied to a string — it does not change the underlying type. Using `'normal'` in a choice declaration would require the type checker to resolve a typed constant to... `string`, as constrained by choice membership. That breaks the invariant that `'...'` → non-primitive. The delimiter would be lying about the type.

Shane's reading of `''` as "typed literal, context-determines-type" is correct — but "context-determines-type" in the spec means *the type is non-primitive and the lexer can't determine it*. It does not mean "free-text literal that the surrounding construct will validate." Choice values don't meet that contract. They're strings, and the spec says `""` is the string delimiter.

---

### Why the delimiter isn't the right lever for this UX problem

The UX concern is real. Users seeing `choice("draft", "active", "closed")` may reasonably believe the field accepts any string. But the lever Shane is pulling — the declaration-site delimiter — can't fix the assignment-site behavior.

Consider what happens after adopting `''`:

```precept
field Status as choice('draft', 'active', 'closed') default 'draft'
```

Users will *still* write:

```precept
set Status = "active"   # membership-validated — this is still valid
set Status = "bogus"    # MembershipViolation — this is still an error
```

The assignment surface uses `""` regardless of what the declaration uses, because `set Status = "active"` reads a string literal off the field type, not the declaration delimiter. The `''` at declaration time doesn't change what users write at assignment time. The UX confusion doesn't disappear — it just moves: now users wonder why the declaration uses `'...'` but their assignments use `"..."`.

Worse: `''` at the assignment site — `set Status = 'active'` — should be a type error, because `'active'` is a typed constant in a context that expects a string-type field value. A user who infers from the declaration that choice values use `''` will write this and be confused by the error.

---

### What would actually address the UX concern

The underlying concern is: **users don't see the membership constraint from the call site.** The right fix is tooling:

1. **Language server hover:** Hovering on `set Status = "bogus"` shows "Status is a choice field — valid values: draft, active, closed."
2. **Diagnostic message quality:** `MembershipViolation` should name the valid set in the error, not just say "not a member."
3. **Completion:** The language server offers choice members as completions when editing an assignment to a choice field.

These address the actual failure mode (user doesn't know the constraint exists) without corrupting the delimiter contract.

---

### Verdict

**Keep `""` for choice values in the untyped `choice(...)` case.** Definitively.

The UX argument is valid as a problem statement, but `''` is the wrong solution. It contradicts the spec invariant (`'...'` → non-primitive), it doesn't fix the assignment-site behavior, and it introduces new confusion when users try to use `''` in assignments. The right fixes are diagnostic quality and language server completions.

The decision table entry stands:

| Should choice declaration values use `''` instead of `""`? | No. `'...'` is the non-primitive literal form. Choice values are strings. The membership constraint is enforced by the type checker, not signaled by the delimiter. |

---

### Does the answer change under Option B (`choice of T`)?

No — it sharpens the existing answer.

For `choice of string("draft", "active")`: values are strings → `""`. Unchanged.

For `choice of integer(0, 404, 500)` or `choice of decimal(0.05, 0.10)`: values are numeric literals → bare. The `''` argument doesn't arise, because there's no temptation to use `""` for integers, and `''` would again claim non-primitive semantics for plain numbers.

Option B doesn't create a case where `''` becomes appropriate for choice values. If anything, the explicit `choice of string(...)` form makes the "these are strings" expectation *more* correct, not less.

---

## Fresh Assessment — Outside the Box

**Date:** 2026-04-30  
**Occasion:** Shane's challenge to re-examine the premise — not just refine the existing options.

---

### Start here: the spec is hiding an ambiguity that's driving the entire tension

The addendum said: "a choice value is a string constrained by a rule." That phrase is the problem. Not wrong, exactly, but dangerously imprecise — it leaves open a question the spec never answers:

**Can a `string`-typed value be assigned to a choice field at runtime, with membership validated then?**

If the answer is **yes** — even conditionally — then `choice` is a constrained string, and the question about whether it's "different from `string + rule`" collapses. If the answer is **no**, then `choice` is a sealed vocabulary type that justifies itself through structural prevention.

The spec doesn't say. The grammar defines `ChoiceType` as `choice "(" StringExpr ("," StringExpr)* ")"`, which signals the underlying type is string, but says nothing about assignment compatibility at the variable (non-literal) level. The operator table shows `choice ≡ choice` for comparisons — choice does not compare directly against `string`. But assignment rules aren't stated with the same precision.

This ambiguity is the root of every downstream tension in this discussion. Before we can answer any of the other questions, we have to decide: **what is a choice value?**

---

### The prevention test — the one question that settles the design

Here is the sharp test. Does this compile?

```precept
field Status as string writable
field Priority as choice("Low","Medium","High") default "Low"

# Can I do this?
set Priority = Status
```

If yes: `choice` is a constrained string. Runtime enforcement. Detection dressed up as a type.  
If no: `choice` is a sealed vocabulary type. Compile-time blocked. Prevention.

`choice` only justifies its existence over `string + rule` if the answer is **no.**

With `string + rule`, `set Priority = Status` compiles and runs; the rule fires afterward. The invalid value never persists in a committed entity — Precept's atomicity guarantee covers that. But there's a runtime evaluation fault path: you can attempt `set Priority = "bogus"` and it *tries* before it fails. With sealed `choice`, `set Priority = Status` is a type error that the compiler catches before any instance exists. That is a qualitatively different guarantee.

The prevention principle in `docs/philosophy.md` is precise: "Invalid entity configurations cannot exist. They are structurally prevented before any change is committed." A runtime membership check satisfies the first half. Only a compile-time type block satisfies both halves. **The load-bearing element of `choice`'s existence is compile-time blocking of runtime string values entering choice fields.** Everything else is secondary.

---

### Framing 1: Choice as a sealed vocabulary type — not a constrained string

This is the sharpest version of the current design, made fully explicit.

A `choice(...)` field holds a **vocabulary value**. Not a string. The runtime stores it as a string, but that's an implementation detail — the language surface treats it as an opaque scalar type. Implications:

- `set Priority = "Low"` — **valid.** String literal `"Low"` is known at compile time; the compiler verifies it's a member.
- `set Priority = SomeStringField` — **TYPE ERROR.** A `string` value is not a vocabulary value. The compiler cannot verify membership at compile time.
- `set Priority = SetPriority.Level` where `Level as choice("Low","Medium","High")` — **valid.** Structural equivalence; same vocabulary.
- `Priority == "Low"` — **valid.** Literal comparison: the compiler verifies the literal is a member.
- `Priority == SomeStringField` — **TYPE ERROR.** Runtime string comparison would bypass type safety.

This model makes `choice` genuinely different from `string + rule`:

| | `string + rule in(...)` | `choice(...)` sealed |
|---|---|---|
| `set F = "bogus"` | Compiles, rule rejects at runtime | Compile error: `MembershipViolation` |
| `set F = StringVariable` | Compiles, rule rejects if invalid | Compile error: `StringAssignedToChoice` |
| `set F = ChoiceArg` | No such arg — arg is `string` | Valid if structurally compatible |
| Prevention guarantee | Structural (atomicity) | Structural (type system) |
| Compile-time diagnosis | None at assignment | Yes — both cases |

The cost: this is stricter than whatever the current implementation does. It blocks any `.precept` that assigns a string variable to a choice field. That is the right call. Those files have a hidden detection dependency they don't know they have.

**This is the design `choice` should commit to. Not as a new feature — as a clarification of what it already is, made unambiguous in the spec.**

---

### Framing 2: Named vocabulary shorthand — shared sets without user-defined types

The ergonomic problem that keeps surfacing: a 6-value choice set repeated across 4 fields and 2 event args is maddening to maintain. Named vocabulary sets solve that without touching the type system.

```precept
vocabulary Priority = ("Low", "Medium", "High", "Critical") ordered

field Priority as Priority default "Low"
field EscalationTarget as Priority default "High"
event SetPriority(Level as Priority)
event Escalate(Target as Priority)

from Active on SetPriority
    -> set Priority = SetPriority.Level
    -> no transition
```

The critical design decision: `vocabulary` introduces a **named shorthand**, not a type. `field Priority as Priority` expands at parse time to `field Priority as choice("Low","Medium","High","Critical") ordered`. The actual type in the type checker is still `choice(...)`. No user-defined type exists. §0.4 Property 4 is preserved — the closed type vocabulary is untouched.

This is macro expansion, not a type system extension. No symbol table entry, no reference resolution, no scoping rules beyond file-local. The implementation is a pre-pass that substitutes the vocabulary reference with the inline form before the type checker sees it.

What this is NOT: Option C. Option C introduces a named type with type identity. A `vocabulary` entry has no type identity — two fields using the same vocabulary entry are both `choice(...)` with identical sets, structurally equivalent (Option A). The name is purely for authoring convenience.

**Assessment:** Worth a dedicated proposal. Not this cluster — but not far off either. The prevention guarantee is unaffected because the resolved type is still sealed `choice(...)`.

---

### Framing 3: The "field-level `in` constraint" — the honest degenerate case

What if we make the constrained-string reading fully explicit in the syntax and accept its implications?

```precept
field Priority as string in("Low","Medium","High") ordered default "Low"
```

`in(...)` as a field-level structural constraint — not a separate rule, part of the type annotation. The compiler enforces membership on literals. The type is `string`. Variable assignment still compiles — because it's `string`.

This framing deserves naming because it's the design we slide into if we fail to commit to the sealed vocabulary model. It's what `choice` degrades to if we allow `set Priority = SomeStringField` without blocking it. The type becomes `string` with decoration. The prevention guarantee partially evaporates.

I'm naming it not to recommend it but to establish it as the failure mode. If someone argues "choice should be more permissive about string assignment," this is where that argument leads. The question to ask them: "do you want `field Priority as string in(...)` instead?" If the answer is yes, write that and call it what it is. If the answer is no — then seal the type.

**Assessment:** Do not implement. Name it so we know what we're avoiding.

---

### Cross-language survey — what it actually teaches

**TypeScript string literal unions** (`"Low" | "Medium" | "High"`): structurally typed, string variable → union type is a compile-time error without explicit narrowing. This is precisely the sealed vocabulary model. TypeScript gets right what the current Precept spec leaves ambiguous. The right mental model: `choice(...)` is Precept's string literal union. Act like it.

**Rust enums**: nominal, not structural. `Priority::Low` is not a string. Clean for type safety; wrong ergonomic model for a business-analyst DSL. A domain expert will write `"Low"` and ask why it doesn't work.

**SQL `CHECK` constraints**: detection, not prevention. The DB attempts the insert and rejects it. This is `string + rule` territory. The comparison illustrates exactly why Precept's stronger guarantee matters.

**Kotlin sealed classes**: same nominal tradeoff as Rust. Exhaustive pattern matching is a great property, but the construction overhead is wrong for the audience.

**The survey verdict:** TypeScript string literal unions are the correct analogue. Same strings, same structural typing, compile-time prevention. The only thing TypeScript gets right that Precept hasn't fully committed to: the blocking of string-variable-to-union assignment. Commit to it.

---

### The subset-subtype model — is it sound under the sealed vocabulary model?

Shane's proposal: `arg Priority as choice("Low","Medium")` should be valid for `field Priority as choice("Low","Medium","High")` because the subset is provably safe.

Under the sealed vocabulary model: **yes, this is sound.** The compiler can verify at compile time that every value the arg can hold is a member of the field's declared set. No runtime check needed. Prevention holds.

What this requires: a subtype relation on choice types — `choice(A) <: choice(B)` iff `A ⊆ B`. For ordered choice: `A` must be an order-preserving subset of `B` (same relative rank) to support ordinal comparison across the assignment. This is a natural extension of structural equivalence (Option A) and doesn't require grammar changes.

The direction matters: arg→field assignment uses the subtype relation (narrower is assignable to wider). Field→arg assignment does not (the field might hold a value the arg can't represent). The type checker enforces this asymmetry.

---

### Direct answer to Shane's question

**"At this point, is it different than `field Priority as string` + `rule Priority in('Low','Medium','High')`?"**

Yes — IF and only IF the spec commits to two things:

1. **Compile-time literal checking**: `set Priority = "bogus"` is a compile error, not a runtime failure.
2. **String variable blocking**: `set Priority = SomeStringField` is a type error. No runtime path exists for a non-member value to reach the field through the type system.

Without both, `choice` is a better-surfaced `string + rule` — useful ergonomically, but not qualitatively different in prevention terms. The prevention principle in `docs/philosophy.md` requires (2) in particular. A runtime path where an arbitrary string value can flow into a choice field is detection dressed up as a type.

**The current spec doesn't commit to (2).** That's the design gap. It's fixable without changing the grammar.

---

### Concrete recommendation

**Commit to the sealed vocabulary model. Now — as a spec clarification, not a new feature.**

1. **Add an explicit assignment rule to the spec:** A `string` value is not assignment-compatible with a `choice` field. The only compatible assignment sources are: (a) a string literal that is a declared member, verified at compile time; (b) a value from a structurally compatible or subset choice type.

2. **Add a diagnostic entry:** `StringAssignedToChoice` — emitted when a `string`-typed expression is assigned to a `choice` field. Severity: Error.

3. **Lock Option A (structural equivalence)** as the arg supply mechanism. Extend it to include the subset-subtype relation: `choice(A)` is assignable to `choice(B)` iff `A ⊆ B`.

4. **File the named vocabulary shorthand** as a separate follow-on proposal — not a type system change, a macro expansion for authoring ergonomics.

5. **Update the operator table and assignment compatibility tables** in the spec to reflect the sealed model explicitly. This is the single biggest documentation gap.

The thing I want to say plainly: **the current design is incomplete, not wrong.** The mechanism is correct. The abstraction is correct. But without the spec commitment to blocking string variable assignment, `choice` is partially prevention and partially detection, and we've been arguing about the wrong things as a result. Seal the type, add the diagnostic, document the rule. The rest of the design — structural equivalence, subset subtyping, named vocabulary shorthand as future work — follows cleanly from that one commitment.

```precept
# What the sealed vocabulary model looks like in practice

field Priority as choice("Low","Medium","High","Critical") default "Low"
field Notes as string writable

# These compile:
set Priority = "High"                                # literal — verified at compile time
set Priority = EscalationEvent.Level                 # Level as choice("Low","Medium","High","Critical") — same set
set Priority = EscalationEvent.Target                # Target as choice("High","Critical") — subset, valid

# These are type errors:
set Priority = Notes                                 # StringAssignedToChoice — blocked
set Priority = "Urgent"                              # MembershipViolation — not in declared set
set Priority = EscalationEvent.Urgency               # Urgency as choice("Routine","Urgent") — incompatible sets
```

The language surface doesn't change. The prevention guarantee becomes honest.

---

## Consolidated Design Update — 2026-04-29

**Date:** 2026-04-29  
**Occasion:** Owner design session synthesis — Shane's consolidated input on five open questions.

---

### 1. The Load-Bearing Insight: Choice = Dropdown Signal + Closed-Set Constraint

Shane's formulation is precise and should be the canonical justification: *"I guess the real value of choice is showing the user a drop-down."*

This is the JSON Schema `enum` model. A `choice` field simultaneously does two things:

1. **Constrains the value domain** — membership enforcement at the type-system level.
2. **Signals the presentation contract** — "render this field as a dropdown, not a free-text input." Language server, form generators, and admin UIs read the choice set to offer a closed list of options.

The second property is what justifies `choice` over `string + rule`. Both enforce membership. Only `choice` communicates "the valid values are enumerable and knowable at design time, not just runtime." That's the metadata consumers need to render a dropdown, generate an OpenAPI enum, or populate a UI picker.

**This framing does not invalidate the prevention test.** It adds a second justification. The dropdown contract still requires the type to be sealed — an open `string` value flowing into a `choice` field would corrupt the dropdown semantics (the field would hold values the dropdown doesn't offer). The prevention argument and the dropdown argument converge on the same architectural conclusion: seal the type.

**What changes in the spec:** The justification for `choice` must include the UI rendering signal explicitly. "Choice enforces membership" is half the answer. "Choice enables dropdown rendering" is the other half. Both properties must appear in the spec's introduction of the `choice` type.

---

### 2. The Subset Subtype Model — Replacing Exact-Match Structural Equivalence

My original Option A defined structural equivalence as exact-match: two choice types are the same iff their sets are identical. Shane's consolidated input replaces this with **subset subtype assignment**: `choice(A) <: choice(B)` iff `A ⊆ B`.

**The new rule:**

An event arg of type `choice(A)` is valid as the source for assignment to a field of type `choice(B)` iff every member of `A` is a member of `B`. The compiler verifies this statically — no runtime check needed. Prevention holds: every value the arg can carry is a guaranteed member of the field's declared set.

```precept
field Priority as choice("Low", "Medium", "High") default "Low"

# Valid — {"Low","Medium"} ⊆ {"Low","Medium","High"}
event Triage(Level as choice("Low", "Medium"))
from Active on Triage
    -> set Priority = Triage.Level    # OK: every Level value is in Priority's set

# Valid — {"High"} ⊆ {"Low","Medium","High"}
event MarkUrgent(Level as choice("High"))
from Active on MarkUrgent
    -> set Priority = MarkUrgent.Level  # OK

# TYPE ERROR — "Critical" ∉ {"Low","Medium","High"}
event Escalate(Level as choice("Medium", "Critical"))
from Active on Escalate
    -> set Priority = Escalate.Level   # TypeMismatch: "Critical" not in field's declared set

# TYPE ERROR — superset: field may hold "Low" which isn't in arg's set
event StrictEscalate(Level as choice("Medium", "High", "Critical"))
from Active on StrictEscalate
    -> set Priority = StrictEscalate.Level   # TypeMismatch: superset is not a subtype
```

**Direction constraint:** The subtype relation is arg→field only. A field can supply a value to a *wider* arg (subtype widens to supertype), not to a *narrower* one. The type checker enforces this asymmetry.

**What this replaces:** The exact-match requirement from Option A. Exact match was sound but too restrictive — it forced event arg declarations to repeat the entire field set verbatim. Subset subtype is the TypeScript string literal union model: `"Low" | "Medium"` is assignable to `"Low" | "Medium" | "High"` because the narrower union is a subtype of the wider. Precise, statically verified, well-precedented.

**What this does NOT change:** The prevention test remains open. Subset subtype governs choice→choice assignment. Whether string→choice assignment compiles is addressed in §6.

---

### 3. Ordered Choice + Subset: The Order-Preserving Subsequence Rule

For ordinal comparison (`<`, `>`, `<=`, `>=`, `.min`, `.max`) between two choice values, it is not sufficient that the arg's set is a subset of the field's set. The ranks must be consistent.

**The rule:** An ordered `choice(A)` arg supports ordinal comparison with an ordered `choice(B)` field iff `A` is an **order-preserving subsequence** of `B` — that is, the relative order of members in `A` matches their relative order in `B`.

```precept
field Priority as choice("Low", "Medium", "High") ordered default "Low"
# Declared ranks: Low=1, Medium=2, High=3
```

| Arg type | Subsequence check | Ordinal comparison valid? |
|---|---|---|
| `choice("Low", "Medium") ordered` | Low=1 < Medium=2 in both ✓ | Yes |
| `choice("Medium", "High") ordered` | Medium=2 < High=3 in both ✓ | Yes |
| `choice("Low", "High") ordered` | Low=1 < High=3 in both ✓ (non-contiguous is fine) | Yes |
| `choice("Medium", "Low") ordered` | Medium=1 in arg, Medium=2 in field — rank conflict ✗ | TypeMismatch |
| `choice("Low", "Medium")` (unordered) | Arg has no rank | Assignment valid; ordinal comparison TypeMismatch |

**Assignment vs. comparison:** The subtype relation (§2) governs assignment. The order-preserving subsequence rule governs ordinal comparison. A choice arg can be assignable to a field (subset ✓) but still not support ordinal comparison (rank conflict). Both checks are static.

```precept
# Concrete example — assignment valid, ordinal comparison valid
event Triage(Level as choice("Low", "Medium") ordered)
from Active on Triage when Triage.Level < Priority
    -> set Priority = Triage.Level    # both valid: subset ✓, order-preserving ✓

# Assignment valid, ordinal comparison TypeMismatch (reversed order)
event InvertedTriage(Level as choice("Medium", "Low") ordered)
from Active on InvertedTriage when InvertedTriage.Level < Priority  # TypeMismatch: rank conflict
    -> set Priority = InvertedTriage.Level   # this line would be valid if above weren't a type error
```

**Declaration order universally — for all types.** String choice and all typed choice (`integer`, `decimal`, `number`) use declaration order as the rank definition. There are no special cases for natural numeric order. The author controls rank by how they write the list. If natural numeric order is desired, list values numerically — and they agree by construction.

```precept
# Declaration order = rank order for all types
field ErrorCode as choice of integer(200, 404, 500) ordered default 200
# Ranks: 200=1, 404=2, 500=3 — author listed ascending; natural and declared order agree

field SeverityCode as choice of integer(500, 404, 200) ordered default 500
# Ranks: 500=1, 404=2, 200=3 — deliberately reversed; the author controls this
# 500 < 404 < 200 by declared rank
```

**Spec impact:** The operator table row at line 1127 currently reads `choice (ordered, same set)`. Under the subset subtype model, the constraint is no longer same-set — it is order-preserving subsequence. That row's right-operand description must be updated to reflect this. The exact-match framing was only accurate under Option A's original equivalence rule.

---

### 4. `choice of T` — Locked Grammar for All Primitives

**Locked.** `choice of T` extends to all primitive types: `string`, `integer`, `decimal`, `number`, `boolean`.

**Grammar:**

```
ChoiceType        := choice ("of" ChoiceElementType)? "(" ChoiceValueExpr ("," ChoiceValueExpr)* ")"
ChoiceElementType := string | integer | decimal | number | boolean
ChoiceValueExpr   := StringLiteral | IntegerLiteral | DecimalLiteral | NumberLiteral | BooleanLiteral
```

`choice("a","b")` = `choice of string("a","b")` — backward-compatible default. All existing `.precept` files are unchanged.

**Concrete syntax for all types:**

```precept
# String choice — unchanged
field Status as choice("draft", "active", "closed") default "draft"

# Integer choice — HTTP status codes, numeric tiers
field ErrorCode as choice of integer(200, 404, 500) ordered default 200

# Decimal choice — exact rate tiers (base-10 precision required)
field TaxRate as choice of decimal(0.00, 0.05, 0.10, 0.20) default 0.00

# Number choice — threshold picker (IEEE 754 precision acceptable for this domain)
field AlertThreshold as choice of number(1500, 2500, 5000) default 2500

# Boolean choice — full domain declared explicitly
field IsActive as choice of boolean(true, false) default true

# Event arg using typed choice — subset subtype applies
event SetCode(Code as choice of integer(200, 404))
from Active on SetCode
    -> set ErrorCode = SetCode.Code   # valid: {"200","404"} ⊆ {"200","404","500"}
```

**Bare literals for non-string values.** `choice of integer('1', '2', '3')` is a type error — `''` is the non-primitive literal form (spec §1.3). Integers are primitives. Numeric choice values are written as plain numeric literals, consistent with all other numeric usage in the language (`field MaxCount as integer min 1 max 100 default 5`).

**Type isolation — `decimal` and `number` remain incompatible.** No implicit `decimal → number` widening exists in any context (spec §3.2). `choice of decimal(0.05, 0.10)` and `choice of number(0.05, 0.10)` are distinct types. Whether precision is base-10 exact or IEEE 754 approximate is a meaningful decision; the `of T` qualifier makes that intent explicit.

```precept
field ExactRate as choice of decimal(0.05, 0.10) default 0.05
field ApproxThreshold as choice of number(0.05, 0.10) default 0.05

event SetRate(Rate as choice of number(0.05, 0.10))
from Active on SetRate
    -> set ExactRate = SetRate.Rate   # TypeMismatch: decimal ≠ number, no implicit widening
```

**`ordered` on `choice of boolean`:** `boolean` has no meaningful declared order — `true < false` vs `false < true` is undefined semantics with no business analogue. `ordered` must be disallowed on `choice of boolean`. The grammar permits it; the type checker rejects it with a dedicated diagnostic (e.g., `OrderedChoiceOnBoolean`).

**Mixed literal unions:** Off the table. `choice("low", 1, true)` is invalid. All members of a choice literal list must share a single element type. Type mismatch within the list is a parse-time or type-checker error.

---

### 5. String `<`/`>` vs. Ordered Choice `<`/`>` — Two Distinct Operator Behaviors

**Confirmed from spec line 1126:** `< > <= >=` work on `string` fields with **lexicographic ordering**.

**The problem this creates for business values:** Lexicographic ordering is almost always wrong for severity, tier, or priority fields.

```
# Lexicographic — "High" < "Low" < "Medium"
# A comparison Priority > "Low" returns true when Priority == "High" — backwards
```

**The ordered choice correction:** `choice("Low","Medium","High") ordered` declares explicit semantic rank — `Low=1, Medium=2, High=3`. The `<`/`>` operators on this field use declared rank, not lexicographic order. The author's intent is captured in the declaration.

**Two operator behaviors on two distinct types — not an inconsistency:**

| Field type | `<`/`>` behavior | Correct for business ranking? |
|---|---|---|
| `string` | Lexicographic (spec §4, line 1126) | Usually no — alphabetic accident |
| `choice(...) ordered` | Declared rank (spec §4, line 1127) | Always yes — author-defined |
| `choice(...)` (unordered) | Not permitted | N/A — type error at compile time |

This is appropriate semantics per type. `string` has `<`/`>` because strings have a universal total order (lexicographic). This is correct wherever lexicographic order is meaningful — alphabetic lists, code points, identifiers. `ordered choice` has `<`/`>` because the author has explicitly declared a semantic rank. The `ordered` keyword is the signal that ordinal operators should use declared rank, overriding any default.

The operator table's two separate rows (lines 1126 and 1127) correctly capture this as distinct behaviors — they coexist without conflict because the left-operand types are distinct.

**Practical implication for tooling:** A `string` field used for severity or priority levels should be migrated to `choice(...) ordered`. Using `<`/`>` on a plain `string` field for business rankings compiles and runs but produces wrong results. A language server warning when `<`/`>` is used on a `string` field in guard/rule context — suggesting `choice ordered` — would be a useful quality-of-life improvement, though this is tooling work, not a type system change.

---

### 6. The Prevention Test — Open Question Requiring Owner Sign-Off

**Status: OPEN.**

The question introduced in my Fresh Assessment and carried forward explicitly because it has not been answered:

```precept
field Notes as string writable
field Priority as choice("Low", "Medium", "High") default "Low"

set Priority = Notes   # TypeMismatch or valid?
```

**If TypeMismatch (sealed vocabulary model):**
- `choice` is structurally different from `string + rule`
- Only string literals verified at compile time and structurally compatible choice values are assignment-compatible sources
- Prevention is complete: no runtime path for an arbitrary string to enter a choice field through the type system
- The TypeScript string literal union analogue — what the Fresh Assessment named as the correct model

**If valid (constrained string model):**
- `choice` is a `string` field with membership enforcement at runtime
- The language allows attempting the assignment; the runtime rejects invalid values via atomicity
- This is Framing 3 from the Fresh Assessment — field-level `in` constraint — the named failure mode

**My recommendation is unchanged:** Seal the type. `set Priority = someStringVariable` should be a `TypeMismatch` diagnostic (Error). The dropdown framing reinforces this — if a `choice` field's value set is knowable at design time for UI rendering, the type must be sealed or the rendering contract is meaningless (the field could hold values the dropdown doesn't offer).

**What owner sign-off resolves:**
1. Whether `StringAssignedToChoice` is added as an Error-severity diagnostic
2. Whether any existing `.precept` files assign string variables to choice fields and would need migration
3. Whether comparison of a choice field against a `string` variable (`Priority == someStringField`) is also a type error, or only assignment is blocked

This question is independent of the subset subtype model (§2), which governs choice→choice assignment. The open question is specifically: does `string` narrow to `choice` at all, under any circumstances?

---

### 7. Decision Table

| Question | Answer | Status |
|---|---|---|
| What is `choice`'s primary justification over `string + rule`? | Dropdown signal: finite valid values knowable at design time, renderable as UI picker. Membership enforcement is convergent, not the primary differentiator. | **Locked** |
| What is the arg→field assignment rule for choice? | Subset subtype: `choice(A) <: choice(B)` iff `A ⊆ B`. Compiler verifies statically. | **Locked** |
| What is the rule for ordinal comparison across two ordered choice values? | Order-preserving subsequence: arg's declared sequence must be rank-consistent with field's declared sequence. | **Locked** |
| Does `choice` support non-string element types? | Yes. `choice of T` for `string`, `integer`, `decimal`, `number`, `boolean`. | **Locked** |
| What literals do non-string choice values use? | Bare literals. `choice of integer(0, 404, 500)`. The `''` form is a type error. | **Locked** |
| What governs rank for all choice types? | Declaration order, universally — string AND numeric. No special case for natural numeric order. | **Locked** |
| Is `ordered` required to enable `<`/`>` on choice fields? | Yes. `ordered` is the capability gate for all element types. | **Locked** |
| Is `ordered` valid on `choice of boolean`? | No. `boolean` has no meaningful declared ordering. Type checker rejects with diagnostic. | **Locked** |
| Does `decimal` widen to `number` in choice assignment or comparison? | No. `decimal` does not implicitly widen to `number` in any context (spec §3.2). `choice of decimal` and `choice of number` are distinct types. | **Locked** |
| Are mixed literal unions (`choice("low", 1, true)`) valid? | No. All members must share a single element type. | **Locked** |
| What is the distinction between string `<`/`>` and ordered choice `<`/`>`? | String: lexicographic (spec line 1126). Ordered choice: declared rank (spec line 1127). Two distinct operator behaviors on two distinct types — not an inconsistency. | **Locked** |
| Does `set Priority = someStringVariable` compile? | **OPEN.** Recommendation: TypeMismatch (sealed vocabulary). Requires owner sign-off. | **Open** |
| Should Option C (named types) be introduced? | Not in this cluster. Requires separate proposal explicitly addressing §0.4 Property 4. | Deferred |
| Should `vocabulary` shorthand be proposed as authoring ergonomics? | Worth a dedicated follow-on proposal. Macro expansion at parse time; no type system change. Preserves §0.4 Property 4. | Deferred |

---

# Decision Record: Choice Field Diagnostic Messages

**By:** Elaine  
**Date:** 2026-04-29  
**Status:** Pending owner sign-off

---

## What This Covers

Five distinct diagnostics for violations of a `choice` field's type contract. `TypeMismatch` was rejected as the error code for these — it is too generic to be actionable. Every one of these violations has a different cause and a different fix. The messages should reflect that.

These diagnostics assume the locked design decisions from Frank's choice analysis (2026-04-29):
- `choice` is a sealed type — only choice-typed sources and compile-time literals in the declared set are assignment-compatible
- Subset subtype: `choice(A) <: choice(B)` iff `A ⊆ B`
- `choice of T` is valid for `string`, `integer`, `decimal`, `number`, `boolean`
- `ordered` is the capability gate for ordinal comparison (`<`, `>`, `<=`, `>=`) on all element types
- Order-preserving subsequence is required for ordinal comparison across two ordered choice values

**Audience context:** The primary author of a `.precept` file is a business analyst or domain expert. Messages must use plain language — no type-theory terminology. The message should tell the user what to do, not just what went wrong.

**Valid-values display rule:** When the field's declared set has ≤5 members, include all values in the primary message. When >5 members, show the first 3 and the remaining count (e.g., `"Low", "Medium", "High", and 2 more`). The hover detail always shows the full set regardless of size.

---

## Category 1 — Non-choice type assigned to choice field

**Fires when:** A value that is not choice-typed is assigned to a choice field. Covers string variables, integer variables, decimal variables, booleans, collections, money, dates — any non-choice source.

**Examples that trigger this:**
```precept
field Priority as choice("Low", "Medium", "High") default "Low"
field Notes as string writable

set Priority = Notes           # NonChoiceAssignedToChoice
set Priority = SomeIntegerVar  # NonChoiceAssignedToChoice
set Priority = true            # NonChoiceAssignedToChoice
```

---

### Diagnostic: `NonChoiceAssignedToChoice`

**Severity:** Error

**Message template:**
```
'{0}' is a {1} value and cannot be assigned to '{2}' — valid values are {3}
```

| Placeholder | Binds to | Example |
|---|---|---|
| `{0}` | Source expression text | `Notes` |
| `{1}` | Source type name | `string` |
| `{2}` | Target field name | `Priority` |
| `{3}` | Formatted valid values (field's declared set, truncated if >5 members) | `"Low", "Medium", "High"` |

**Rendered example:**
> `'Notes' is a string value and cannot be assigned to 'Priority' — valid values are "Low", "Medium", "High"`

**Secondary / hover detail:**
> `Priority` is a choice field — it only accepts specific values from its declared set: `"Low"`, `"Medium"`, `"High"`. The variable `Notes` is a `string` and could hold any text, including values not in this set. The compiler cannot verify at build time that `Notes` will only ever contain a valid value.
>
> To fix this:
> - Assign a literal value directly: `set Priority = "High"`
> - Or declare the source as a compatible choice type: `Notes as choice("Low", "Medium", "High")`

**Quick-fix hint:** `Assign a literal value, or change the source declaration to a compatible choice type`

---

## Category 2 — Choice literal not in field's set

**Fires when:** A compile-time literal (string or numeric) is used in an assignment to a choice field, but the literal is not a member of the field's declared set. The value is known at compile time — this is a definitive membership failure.

**Examples that trigger this:**
```precept
field Priority as choice("Low", "Medium", "High") default "Low"
field ErrorCode as choice of integer(0, 404, 500)

set Priority = "Critical"     # ChoiceLiteralNotInSet — "Critical" is not in the set
set ErrorCode = 999            # ChoiceLiteralNotInSet — 999 is not in the set
```

---

### Diagnostic: `ChoiceLiteralNotInSet`

**Severity:** Error

**Message template:**
```
{0} is not a valid value for '{1}' — valid values are {2}
```

| Placeholder | Binds to | Example |
|---|---|---|
| `{0}` | The rejected literal, as written in source | `"Critical"` or `999` |
| `{1}` | Target field name | `Priority` |
| `{2}` | Formatted valid values (field's declared set, truncated if >5 members) | `"Low", "Medium", "High"` |

**Rendered example (string choice):**
> `"Critical" is not a valid value for 'Priority' — valid values are "Low", "Medium", "High"`

**Rendered example (integer choice):**
> `999 is not a valid value for 'ErrorCode' — valid values are 0, 404, 500`

**Secondary / hover detail (string example):**
> `Priority` accepts: `"Low"`, `"Medium"`, `"High"`. The value `"Critical"` is not in this set.
>
> To fix this:
> - Use one of the declared values: `set Priority = "High"`
> - Or add `"Critical"` to the field declaration: `field Priority as choice("Low", "Medium", "High", "Critical")`

**Quick-fix hint:** `Replace the literal with a declared value, or add it to the field's declared set`

---

## Category 3 — Choice arg values outside field's set

**Fires when:** A choice-typed arg or field is assigned to a choice field, but the source's value set is not a subset of the target field's declared set. One or more values in the source could flow into the field that the field does not accept.

The element types are compatible (both `string`, or both `integer`, etc.) — this is a membership violation, not an element type conflict (that is Category 4).

**Examples that trigger this:**
```precept
field Priority as choice("Low", "Medium", "High") default "Low"

# "Critical" is not in {"Low","Medium","High"}
event Escalate(Level as choice("Low", "Medium", "Critical"))
from Active on Escalate
    -> set Priority = Escalate.Level   # ChoiceArgOutsideFieldSet

# "Critical" and "Urgent" both outside the field's set
event Triage(Level as choice("Low", "Medium", "High", "Critical", "Urgent"))
from Active on Triage
    -> set Priority = Triage.Level     # ChoiceArgOutsideFieldSet
```

---

### Diagnostic: `ChoiceArgOutsideFieldSet`

**Severity:** Error

**Message template:**
```
'{0}' includes values not in '{1}': {2}. Valid values are {3}
```

| Placeholder | Binds to | Example |
|---|---|---|
| `{0}` | Source arg or field reference | `Escalate.Level` |
| `{1}` | Target field name | `Priority` |
| `{2}` | All values from source that are not in the target set (comma-separated) | `"Critical"` or `"Critical", "Urgent"` |
| `{3}` | Formatted valid values (target field's declared set, truncated if >5 members) | `"Low", "Medium", "High"` |

Note: `{2}` shows **all** out-of-set values, not just the first. Business analysts need the complete list to decide whether to narrow the arg or expand the field.

**Rendered example (single out-of-set value):**
> `'Escalate.Level' includes values not in 'Priority': "Critical". Valid values are "Low", "Medium", "High"`

**Rendered example (multiple out-of-set values):**
> `'Triage.Level' includes values not in 'Priority': "Critical", "Urgent". Valid values are "Low", "Medium", "High"`

**Secondary / hover detail (single value example):**
> `Priority` accepts: `"Low"`, `"Medium"`, `"High"`. The arg `Escalate.Level` is declared as `choice("Low", "Medium", "Critical")`, which includes `"Critical"` — a value `Priority` does not accept. If `Escalate.Level` were assigned as-is, a `"Critical"` value could enter `Priority`'s field, violating the declared contract.
>
> To fix this:
> - Narrow the arg to values `Priority` accepts: `Level as choice("Low", "Medium")`
> - Or add the value to the field: `field Priority as choice("Low", "Medium", "High", "Critical")`

**Quick-fix hint:** `Remove out-of-set values from the arg declaration, or add them to the field's declared set`

---

## Category 4 — Choice element type mismatch

**Fires when:** Source and target are both choice-typed, but their element types differ. `choice of integer` is a fundamentally different type from `choice of string` — no implicit conversion exists, and there is no value overlap.

**Examples that trigger this:**
```precept
field Status as choice("Draft", "Active", "Closed")    # choice of string

event SetCode(Code as choice of integer(1, 2, 3))
from Active on SetCode
    -> set Status = SetCode.Code    # ChoiceElementTypeMismatch

field ErrorCode as choice of integer(0, 404, 500)
field Level as choice of decimal(0.5, 1.0, 2.5)
set ErrorCode = Level               # ChoiceElementTypeMismatch
```

---

### Diagnostic: `ChoiceElementTypeMismatch`

**Severity:** Error

**Message template:**
```
'{0}' is a choice of {1} and cannot be assigned to '{2}' — '{2}' holds choice of {3} values
```

| Placeholder | Binds to | Example |
|---|---|---|
| `{0}` | Source expression text | `SetCode.Code` |
| `{1}` | Source element type | `integer` |
| `{2}` | Target field name | `Status` |
| `{3}` | Target element type | `string` |

**Rendered example:**
> `'SetCode.Code' is a choice of integer and cannot be assigned to 'Status' — 'Status' holds choice of string values`

**Secondary / hover detail:**
> `Status` is declared as `choice("Draft", "Active", "Closed")` — a choice of string values. `SetCode.Code` is declared as `choice of integer(1, 2, 3)`. These choice types carry fundamentally different kinds of values and have no compatible assignment path.
>
> To fix this:
> - Declare the arg with the correct element type: `Code as choice("Draft", "Active", "Closed")`
> - Or use a separate field for the numeric code alongside the string-choice field

**Quick-fix hint:** `Change the arg type to match the field's element type`

---

## Category 5 — Ordered rank conflict

**Fires when:** Ordinal comparison (`<`, `>`, `<=`, `>=`) is attempted between two `ordered` choice values whose declared sequences assign different ranks to one or more shared values. The subset relation may still hold (assignment can be valid), but ordinal comparison requires rank consistency — and the sequences conflict.

Rank is determined by position in the `choice(...)` declaration. Two ordered choice types have a rank conflict when they share a value but place it at a different relative position.

**Examples that trigger this:**
```precept
field Priority as choice("Low", "Medium", "High") ordered default "Low"
# Declared ranks: Low=1, Medium=2, High=3

# Rank conflict: in this arg, Medium=1 and Low=2 — inverted from the field
event Escalate(Level as choice("Medium", "Low") ordered)
from Active when Escalate.Level > Priority  # ChoiceRankConflict — "Medium" and "Low" have inverted ranks
    -> set Priority = Escalate.Level
```

`choice("Medium", "Low") ordered` declares Medium=rank1, Low=rank2. `choice("Low", "Medium", "High") ordered` declares Low=rank1, Medium=rank2. The shared values `"Low"` and `"Medium"` have inverted relative ranks. `Escalate.Level > Priority` would mean opposite things depending on which sequence is used — so comparison is disallowed.

Note: assignment (`set Priority = Escalate.Level`) is separately governed by the subset subtype rule. It may still be valid (subset check passes: `{"Medium","Low"} ⊆ {"Low","Medium","High"}`). `ChoiceRankConflict` fires only on the comparison — not on the assignment.

---

### Diagnostic: `ChoiceRankConflict`

**Severity:** Error

**Message template:**
```
'{0}' and '{1}' cannot be compared — their declared rank sequences conflict at {2}
```

| Placeholder | Binds to | Example |
|---|---|---|
| `{0}` | Left operand name | `Escalate.Level` |
| `{1}` | Right operand name | `Priority` |
| `{2}` | First conflicting value found (quoted) | `"Medium"` |

**Rendered example:**
> `'Escalate.Level' and 'Priority' cannot be compared — their declared rank sequences conflict at "Medium"`

**Secondary / hover detail:**
> `Priority` is declared as `choice("Low", "Medium", "High") ordered` — `"Low"` is rank 1, `"Medium"` is rank 2, `"High"` is rank 3. `Escalate.Level` is declared as `choice("Medium", "Low") ordered` — `"Medium"` is rank 1, `"Low"` is rank 2.
>
> The relative order of `"Medium"` and `"Low"` is inverted between the two declarations. Using `>` or `<` here would give opposite results depending on which ordering applies — so the comparison is not allowed.
>
> To fix this, align the arg's declaration with the field's ordering:
> ```precept
> event Escalate(Level as choice("Low", "Medium") ordered)
> ```
> With this declaration, `"Low"` is rank 1 and `"Medium"` is rank 2 in both types — comparison is well-defined.

**Quick-fix hint:** `Reorder the arg's declared values to match the field's rank sequence`

---

## Design Notes

### Why five separate diagnostics instead of one

The five categories require different fixes:

| Category | Source of problem | Fix |
|---|---|---|
| Non-choice type | Source is not choice-typed | Change source declaration to a compatible choice type |
| Literal not in set | The specific value doesn't exist in the field | Change the literal, or add the value to the field's declared set |
| Arg values outside set | Source choice type has extra values the field won't accept | Narrow the arg's declared set, or expand the field's declared set |
| Element type mismatch | Choice types carry different underlying value kinds | Align element types across source and target |
| Rank conflict | Ordering sequences are incompatible for ordinal comparison | Reorder the arg's declaration to match the field's rank sequence |

A single `TypeMismatch` diagnostic cannot surface this guidance. The fix for Category 1 ("declare your arg as choice") is wrong advice for Category 2 ("change the literal value"). Separate diagnostics give the right fix for each failure mode.

### Valid-values display thresholds (implementation note)

All messages that show the field's declared set follow the same rule:
- **≤5 members:** Show all values inline: `"Low", "Medium", "High"`
- **>5 members:** Show the first 3 and a count: `"Low", "Medium", "High", and 2 more`
- **Hover detail:** Always shows the full set regardless of size

For `choice of integer` and `choice of decimal` values, no string quotes are used: `0, 404, 500`.

### Category 5 fires on comparison only

`ChoiceRankConflict` fires only on ordinal comparison operators (`<`, `>`, `<=`, `>=`). Assignment is separately governed by the subset subtype rule — `ChoiceArgOutsideFieldSet` covers membership violations on assignment. An arg can be assignment-compatible with a field (its values are a subset) but still trigger `ChoiceRankConflict` when comparison is attempted with conflicting rank sequences. The two checks are independent.

---

## Summary Table

| Diagnostic | Severity | Cat. | Message Template | Fires on |
|---|---|---|---|---|
| `NonChoiceAssignedToChoice` | Error | 1 | `'{0}' is a {1} value and cannot be assigned to '{2}' — valid values are {3}` | Non-choice type (string, integer, boolean, etc.) assigned to a choice field |
| `ChoiceLiteralNotInSet` | Error | 2 | `{0} is not a valid value for '{1}' — valid values are {2}` | Compile-time literal not in the field's declared set |
| `ChoiceArgOutsideFieldSet` | Error | 3 | `'{0}' includes values not in '{1}': {2}. Valid values are {3}` | Choice-typed source has values outside the target field's declared set |
| `ChoiceElementTypeMismatch` | Error | 4 | `'{0}' is a choice of {1} and cannot be assigned to '{2}' — '{2}' holds choice of {3} values` | Source and target are both choice-typed but with different element types |
| `ChoiceRankConflict` | Error | 5 | `'{0}' and '{1}' cannot be compared — their declared rank sequences conflict at {2}` | Ordinal comparison between two ordered choice values with conflicting rank sequences |

---

# Design Consultation: `lookup` and `queue by P` Language Surface

**By:** Elaine  
**Date:** 2026-07-17  
**Status:** Pending owner sign-off

---

## Issue 1 — `containskey` and `removekey`: Drop the `-key` suffix

**Verdict: Replace both with `contains` and `remove`.**

These compound words read like .NET API method names, not natural language. A business author writing a coverage rule does not think "containskey" — they think "contains." The `-key` suffix is defensive disambiguation that the type checker can handle implicitly: a `lookup of K to V` only has keys as membership targets. There are no "value-side" membership or removal operations. The type system knows from context that the argument is a key.

This aligns lookup directly with `set`:

| Operation | `set of T` | `lookup of K to V` (current) | `lookup of K to V` (proposed) |
|---|---|---|---|
| Membership | `F contains X` | `F containskey X` | `F contains X` |
| Removal | `remove F X` | `removekey F X` | `remove F X` |

**Before:**
```precept
when CoverageLimits containskey CheckCoverage.CoverageType
    -> set CurrentLimit = CoverageLimits for CheckCoverage.CoverageType

-> removekey CoverageLimits SomeCoverageType
```

**After:**
```precept
when CoverageLimits contains CheckCoverage.CoverageType
    -> set CurrentLimit = CoverageLimits for CheckCoverage.CoverageType

-> remove CoverageLimits SomeCoverageType
```

"When CoverageLimits contains this coverage type" is clean English. "Remove CoverageLimits SomeCoverageType" is identical in surface form to `remove MissingDocuments ReceiveDocument.Name` already in the sample canon.

**There is no ambiguity to resolve.** A lookup does not support value-side membership tests or removal — only key-side. The type checker knows the type; the keyword does not need to repeat that knowledge. This is the same principle that lets `set.contains` work without `set.containsvalue`.

---

## Issue 2 — Asymmetry diagnosis: the `by`/`priority` naming fork

**The specific source:** Within the priority queue type, `by` and `priority` both refer to the same concept (the ordering dimension), but they appear as different words depending on syntactic context. An author learns two words for one idea:

| Context | Word used |
|---|---|
| Declaration: `queue of string by integer descending` | `by` |
| Enqueue: `enqueue ClaimQueue X by FileClaim.Severity` | `by` |
| Dequeue capture: `dequeue ClaimQueue into X priority Y` | `priority` |
| Accessor: `ClaimQueue.priority` | `priority` |
| Quantifier member: `each claim in ClaimQueue (claim.priority …)` | `priority` |

The fork is: `by` for input operations (declaration, enqueue), `priority` for output operations (dequeue capture, accessor, quantifier). An author who just wrote `enqueue ClaimQueue "ABC" by severity` and then writes `dequeue ClaimQueue into X` will reach for `by` to capture the ordering value — because that's the word they used a line above. Encountering `priority` there introduces a naming seam.

**Is it resolvable?** Partially. There are two directions:

**Option A — unify on `by` at the dequeue site (recommended):**
```precept
-> dequeue ClaimQueue into CurrentClaim by CurrentSeverity
```
Enqueue and dequeue both use `by` as their role connector. The accessor (`.priority`) and quantifier member (`.priority`) remain noun-form, which is grammatically appropriate there and doesn't read strangely.

**Option B — unify on `priority` everywhere:**
```precept
-> enqueue ClaimQueue X priority FileClaim.Severity
```
This was the original design before `by` was introduced. Shane explicitly likes `by` at declaration. Revisiting it requires reopening a locked decision.

**Recommendation: Option A.** Change the dequeue capture keyword from `priority` to `by`. This makes `by` the connective at both enqueue and dequeue action sites, consistent with the declaration. The accessor and quantifier member names (`priority`) stay as nouns.

Updated table after both fixes:

| Operation | Proposed surface |
|---|---|
| Declare | `field F as queue of string by integer descending` |
| Enqueue | `enqueue F Expr by Priority` |
| Dequeue (with capture) | `dequeue F into X by Y` |
| Dequeue (value only) | `dequeue F into X` |
| Accessor | `F.peek` / `F.priority` |
| Quantifier member | `x.priority` / `x.value` |

The remaining fork (`by` at action sites, `priority` as noun) is grammatically natural and is not a usability problem. "Enqueue *by* severity" and "the `.priority` of the front element" are different grammatical roles for the same underlying concept — the first is a preposition introducing a role, the second is a noun naming a property. This is not a seam.

**What the asymmetry is not:** The intentional differences between lookup and priority queue — different verbs, different access patterns, different membership semantics — are not the source of the feeling. Those differences exist because the types are genuinely different. The `by`/`priority` fork is the specific site of the problem because it exists *within a single type*.

---

## Issue 3 — Quantifier variable shape difference (flagged, not blocking)

This was not raised but is a genuine learnable-but-surprising edge. In a regular `queue of T`, iterating with `each x in Q` binds `x` to the raw element value (type T). In a `queue of T by P`, iterating with `each x in Q` binds `x` to a pair with `.value` (type T) and `.priority` (type P).

Same iteration syntax — different variable shape. An author who is familiar with regular queue iteration and writes:

```precept
rule each claim in ClaimQueue (claim.length > 5)   # ← intuitive but wrong
```

will get a type error because `claim` is a pair, not a string. The correct form is:

```precept
rule each claim in ClaimQueue (claim.value.length > 5)
```

**This is not a blocking design flaw.** The declaration `queue of T by P` is a visible signal that this is a pair type, and the error message can be teachable ("ClaimQueue elements have a `.value` and a `.priority` — use `claim.value` to access the string"). But it should be called out as a learner friction point worth addressing in diagnostics and documentation, not just through type errors.

---

## Summary

| Issue | Recommendation | Status |
|---|---|---|
| `containskey` | Replace with `contains` | Proposed |
| `removekey` | Replace with `remove` | Proposed |
| `by`/`priority` fork | Change dequeue capture from `priority` to `by` | Proposed |
| Quantifier variable shape | No design change; address in diagnostics and docs | Flagged |

---

# Design Decision: `priorityqueue` Backing Structure and Tiebreak Guarantee

**By:** Frank + George  
**Date:** 2026-04-29  
**Status:** Pending owner sign-off

---

## Decision

The `priorityqueue of T priority P` type uses `SortedDictionary<TPriority, Queue<TElement>>` as its backing structure, with two mandatory modifications. The language spec guarantees **stable (insertion-order) tiebreaking** for elements with equal priority.

---

## Why .NET `PriorityQueue<T,P>` Cannot Be the Direct Backing Type

`System.Collections.Generic.PriorityQueue<TElement, TPriority>` explicitly documents:

> *"The order in which elements of equal priority are dequeued is not specified."*

"Unspecified" = non-deterministic. Non-determinism breaks Precept's inspectability guarantee — the proof engine cannot reason about which element `.peek` returns when there are ties. `PriorityQueue<T,P>` is ruled out as the direct backing type.

---

## Approved Structure

`SortedDictionary<TPriority, Queue<TElement>>` with a thin wrapper class.

Each distinct priority value maps to a FIFO `Queue<TElement>`. Elements at equal priority dequeue in insertion order. The operations:

| Operation | Complexity | Notes |
|---|---|---|
| `enqueue` | O(log k) | k = distinct priority values |
| `dequeue` | O(log k) | Min/max key + Queue.Dequeue |
| `.peek` / `.peekpriority` | O(log k) | First/Last key + Queue.Peek |
| `.count` | O(1) | Separately maintained counter |
| Quantifier iteration | O(n) | All elements across all buckets |

---

## Mandatory Modifications

### 1. Separate element counter

The wrapper maintains a dedicated `int _count`, incremented on `enqueue` and decremented on `dequeue`/`clear`. Do not sum bucket sizes for `.count`. The `.count > 0` guard is evaluated on every `.peek`, `.peekpriority`, and `dequeue` — O(1) is required.

### 2. Declaration-derived comparers (correctness requirement)

**Never use `Comparer<T>.Default` for ordered `choice of T` priority types.**

`choice of string("normal", "high") ordered` has declaration-position rank: `"normal"` = 0, `"high"` = 1. Natural string ordering gives `"high" < "normal"` alphabetically — the inverse of intent. Using the default comparer would silently dequeue `"normal"` first when `"high"` should be first.

The Precept builder must select the comparer strategy at build time based on the priority field's `TypeKind`:

- `TypeKind.Integer`, `TypeKind.Decimal`, `TypeKind.Number` — `Comparer<T>.Default`
- `TypeKind.Choice` with `ordered` modifier — declaration-position rank comparer, built from choice member list as a `FrozenDictionary<string, int>` rank map
- `TypeKind.Choice` without `ordered` — compile error (`P` must satisfy `TypeTrait.Orderable`)

The rank comparer is constructed once at build time, never at evaluation time.

---

## Language Spec Guarantee

The spec states: **"When multiple elements share the same priority, they are dequeued in the order they were enqueued (insertion-order tiebreaking)."**

The bucket model is the implementation. The spec exposes the contract only.

**`.peek` and `.peekpriority`** always reflect the field's *declared direction*. The per-operation direction override at the dequeue site (e.g., `dequeue F ascending into X priority Y`) selects which end of the sorted structure to dequeue from for that operation only — it does not reorder the queue and does not affect what `.peek` returns. This is a known limitation: there is no `.peekascending`/`.peekdescending` accessor pair.

**`.count`** returns total elements across all priority groups. Quantifiers iterate all elements across all groups — not just the front group.

---

## Alternatives Rejected

| Alternative | Why rejected |
|---|---|
| `System.Collections.Generic.PriorityQueue<T,P>` | Non-deterministic tiebreaking — violates inspectability |
| Min-heap augmented with insertion counter | Harder to inspect, harder to serialize, no performance gain for typical k-small business-domain usage |
| `ImmutableSortedDictionary` | Allocation overhead unjustified; Version immutability is achieved by copy-on-write at the operation boundary |
| `SortedList<TPriority, Queue<TElement>>` | Viable optimization for k ≤ ~20 — may be used as a hidden implementation choice after profiling, but not a design decision |

---

# Technical Review: Elaine's `lookup`/`queue` Surface Proposals

**By:** Frank  
**Date:** 2025-07-17  
**Status:** Recommendations delivered — pending owner sign-off

---

## Proposal 1 — Replace `containskey` with `contains`

**Verdict: APPROVED.**

No grammar ambiguity. `contains` is an infix expression operator at precedence 40 (spec §2.1). It parses as `ContainsExpression(left, ParseExpression(40))`. The left operand is resolved to a field type by the type checker, not the parser. Extending the type checker's `contains` validation table from `{set, queue, stack}` to `{set, queue, stack, lookup}` is a pure type-checker change. The parser sees `Expr contains Expr` regardless of whether the left side is a set or a lookup.

If someone passes a `V`-typed expression to `F contains Expr` on a `lookup of K to V`, the type checker fires `TypeMismatch` — the expected type is `K`, the actual type is `V`. The diagnostic message should say "contains on lookup tests key membership; expected type K, got V." This is clean — no new diagnostic code needed, just a message template specialization.

The `-key` suffix is purely cosmetic disambiguation. No parser production, no proof obligation, no evaluator branch depends on the distinction between `contains` and `containskey`. The type checker already knows the collection kind from the field's declared type. The suffix duplicates information the type system already has.

---

## Proposal 2 — Replace `removekey` with `remove`

**Verdict: APPROVED.**

Parser: no changes required. The `ActionStatement` grammar is already `remove Identifier Expr`. The parser emits the same AST node regardless of whether the field is `set of T` or `lookup of K to V`. Type checker resolves the field type and validates that the expression matches `T` (for set) or `K` (for lookup). This is a type-checker-only extension.

Proof obligation: confirmed identical to `set`. `remove` on `set` is no-op-if-absent — no guard required, no emptiness proof needed. `removekey` on `lookup` has the same semantics (spec: "removekey requires no guard — no-op if absent, like remove on set"). Unifying the keyword preserves this guarantee. No new proof obligation category.

The `-key` suffix is not load-bearing anywhere. No pipeline stage, no evaluator branch, no proof rule depends on it. It exists only because the original `collection-types.md` design mirrored .NET's `Dictionary.ContainsKey`/`Dictionary.Remove` API naming. That's API naming leaking into a DSL surface — exactly what Precept's language design is supposed to prevent.

---

## Proposal 3 — Use `by` at the dequeue-capture site

**Verdict: APPROVED WITH MODIFICATION.**

### Analysis of filter-condition ambiguity

The concern I raised previously: `dequeue ClaimQueue into CurrentClaim by CurrentSeverity` could be misread as "dequeue the item BY this severity" (a filter/selection condition) rather than "dequeue and capture the severity INTO this field."

Is this a real parsing ambiguity? **No.** The parser grammar for dequeue is:

```
dequeue Identifier (into Identifier (by Identifier)?)?
```

There is no conditional-dequeue production. The parser has no `by` + expression continuation that would create a grammatical fork. The `by` keyword in this position is unambiguously a capture binding — the parser cannot misparse it.

Is it a reader-misparse risk? **Mildly.** A business author encountering `dequeue F into X by Y` for the first time might momentarily wonder whether `by Y` means "select by Y" or "capture Y." But this is a first-encounter learning cost, not an ongoing ambiguity. Once learned, the pattern is stable.

### Weighing the arguments

**Elaine's consistency argument** (spec Principle 5 — keyword-anchored readability): The `by` keyword appears at declaration (`queue of T by P`), at enqueue (`enqueue F Expr by Priority`), and now at dequeue (`dequeue F into X by Y`). The same keyword, the same role (introducing the priority axis), in all three action contexts. An author who writes `enqueue F X by P` one line above will instinctively reach for `by` at dequeue. Encountering `priority` there is a vocabulary seam — two words for one concept within the same type.

**My filter-reading concern**: Theoretical. No grammar production creates ambiguity. No current or planned Precept feature introduces conditional dequeue. If conditional dequeue were ever needed, it would use `when` (the language's universal guard keyword), not `by`. The `by` keyword is already claimed for priority-axis role connection — overloading it for a future filter condition would itself be the design error.

**Verdict:** Elaine's consistency argument is stronger. Principle 5 says "statement kind is identified by its opening keyword sequence" — and within that, vocabulary consistency across the lifecycle of a single type is the natural corollary. `by` at declaration, `by` at enqueue, `by` at dequeue. The fork was unjustified.

### The modification

The accessor (`.priority`) and quantifier binding (`.priority`) remain as nouns. This is correct and Elaine explicitly preserves it. `by` is a preposition introducing a role at action sites. `.priority` is a noun naming a property at access sites. Different grammatical roles, same underlying concept. No seam.

---

## Summary Table

| Proposal | Verdict | Conditions |
|---|---|---|
| `contains` replaces `containskey` | **Approved** | Type checker emits `TypeMismatch` if `V`-typed arg supplied; diagnostic message should name the key/value distinction |
| `remove` replaces `removekey` | **Approved** | No-op-if-absent semantics preserved; no new proof obligation |
| `by` replaces `priority` at dequeue-capture | **Approved** | Accessor (`.priority`) and quantifier binding (`.priority`) retain noun form |

---

## Implementation Notes

All three changes are type-checker-only and catalog-metadata updates. No parser grammar changes. No new AST node types. The `Actions` catalog entry for `remove` gains `lookup` in its applicable-types metadata. The `Operations` catalog entry for `contains` gains `lookup` in its valid-lhs-types list. The dequeue action grammar already supports an optional trailing identifier — the keyword text changes from `priority` to `by`.

The `containskey` and `removekey` tokens can be removed from the lexer's keyword table entirely (they are not yet implemented — this is pre-implementation design). The `priority` keyword at action sites is similarly pre-implementation.

---

# Design Advisory: `priorityqueue` Syntax Alternatives

**By:** Frank  
**Date:** 2026-04-29  
**Status:** Pending owner decision

---

## Context

Shane found `priorityqueue of T priority P` verbose. This advisory surveys four alternative syntax forms and makes a recommendation. The decision is not yet locked.

Current design (baseline for comparison):

```precept
field ClaimQueue as priorityqueue of string priority integer descending

-> enqueue ClaimQueue FileClaim.ClaimId priority FileClaim.Severity
-> dequeue ClaimQueue into CurrentClaim priority CurrentPriority
```

---

## Grounding Constraint

The `priority` connector at action sites is not decoration — it is the parser's discriminator between a plain `enqueue F val` (queue) and a two-arg `enqueue F val priority p` (priorityqueue). Dropping it without a replacement forces type-driven grammar (the parser must look up `F`'s declared type to know if the second arg is valid), which violates Precept's principle that statement kind is identified by its opening keyword sequence.

Any option that drops the connector entirely is off the table. Replace it — don't remove it.

---

## Option 1 — `ranked of T by P dir` + `at` connector (Recommended)

```precept
field ClaimQueue as ranked of string by integer descending

-> enqueue ClaimQueue FileClaim.ClaimId at FileClaim.Severity
-> dequeue ClaimQueue into CurrentClaim at CurrentPriority
```

**Gains:** `ranked` is shorter and reads naturally ("a ranked queue of strings, by integer, descending"). `by` is a natural English role connector — consistent with `map of K to V` pattern. `at` is symmetric and brief at both action sites.

**Costs:** 1 new reserved keyword (`ranked`), 2 new contextual keywords (`by`, `at`).

**Open risk:** If `list of T` eventually uses `at` for positional insert (`insert F at N Expr`), there is a contextual collision. Fallback: use `with` instead of `at` — "enqueue ClaimId *with* severity 5" — which has no collision risk and reads naturally.

---

## Option 2 — `ranked of T by P dir`, action sites unchanged

```precept
field ClaimQueue as ranked of string by integer descending

-> enqueue ClaimQueue FileClaim.ClaimId priority FileClaim.Severity
-> dequeue ClaimQueue into CurrentClaim priority CurrentPriority
```

**Gains:** Only the declaration is shortened. Zero change to the action surface — no regression risk. `ranked by` is still a natural read.

**Costs:** 1 new reserved keyword (`ranked`), 1 new contextual keyword (`by`). `priority` still appears at every enqueue and dequeue — partial relief only.

---

## Option 3 — Fold into `queue`: `queue of T by P dir`

```precept
field ClaimQueue as queue of string by integer descending

-> enqueue ClaimQueue FileClaim.ClaimId by FileClaim.Severity
-> dequeue ClaimQueue into CurrentClaim by CurrentPriority
```

**Gains:** Zero new reserved keywords — only `by` as a new contextual keyword.

**Costs:** FIFO and priority-ordered are fundamentally different ordering contracts, not a modifier on the same type. An author who misses the `by` clause will misunderstand which element `.peek` returns. The proof engine needs different dequeue-order reasoning for both — a hidden type discriminator under one keyword. Documentation forks. Does not scale: would imply `stack of T by P` also becomes valid.

**Verdict:** Not recommended. The semantic split is a real user-experience problem.

---

## Option 4 — Positional args (drop connectors entirely)

```precept
-> enqueue ClaimQueue FileClaim.ClaimId FileClaim.Severity   # no connector
-> dequeue ClaimQueue into CurrentClaim CurrentPriority       # no connector
```

**Gains:** Maximum brevity.

**Costs:** Type-driven grammar — the parser cannot distinguish `enqueue F A` (queue) from `enqueue F A B` (priorityqueue) without type lookup. `dequeue F into A B` — genuine parsing ambiguity. Readability regression: `-> enqueue ClaimQueue "ABC123" 5` gives the reader no signal that `5` is a priority. Violates Precept's "keyword-anchored readability" principle.

**Verdict:** Off the table. Grammar regression is a hard blocker.

---

## Recommendation

**Go with Option 1** (`ranked of T by P` + `at` connector) unless the `at` collision with future `list of T` is a confirmed blocker, in which case substitute `with` for `at`:

```precept
field ClaimQueue as ranked of string by integer descending

-> enqueue ClaimQueue FileClaim.ClaimId with FileClaim.Severity
-> dequeue ClaimQueue into CurrentClaim with CurrentPriority
```

"Enqueue ClaimId *with* severity 5" and "dequeue into X *with* captured priority" both parse cleanly as English. `with` is not currently in the Precept lexer, so there is no collision risk anywhere.

**If only the declaration keyword is the pain point** (Option 2), the action surface can remain unchanged. This is the minimal-risk path.

---

## Decision Needed

Owner to pick one:

1. `ranked of T by P dir` + `at` connector (Option 1)
2. `ranked of T by P dir` + `with` connector (Option 1 variant)
3. `ranked of T by P dir`, keep `priority` at action sites (Option 2)
4. Keep current `priorityqueue of T priority P` design as-is
5. Other direction

---

# Frank — whitespace-insensitivity docs sync

Date: 2026-04-30
Requested by: Shane

## Decision

The language docs now treat whitespace-insensitivity as an explicit language guarantee, not an implementation accident.

## Locked points

1. "Line-oriented" means keyword-anchored structure, not newline-delimited syntax.
2. Declarations may span multiple lines freely; whitespace, including newlines, is cosmetic within a declaration.
3. Inside transitions, `from` starts a new row and `->` starts a new pipeline step; those keywords, not newlines, are the structural separators.
4. Parser docs must describe the whitespace fix as a trivia-filter architecture: `Parser.Parse()` strips `NewLine` and `Comment` before `ParseSession`, while `Compilation.Tokens` retains the full token stream for tooling consumers.
5. Qualifier parsing is catalog-driven. `Types.QualifierShape` is the source of truth, with ambiguous-preposition handling derived from construct metadata rather than heuristics.
6. Type-ref docs must model multi-qualifier forms directly (`Qualifiers`, not `Qualifier`) for both scalar and collection inner types.
7. Collection docs should demonstrate readable two-line declarations where long `queue of T by P` forms benefit from line wrapping, and should show qualified inner types as valid collection element forms.

## Why

This is the enduring design intent of Precept's keyword-led surface:

- AI-safe authoring: no layout state required to recover structure.
- No layout traps: reformatting, copy-paste, and code generation do not change meaning.
- Tooling-friendly parsing: statement kind and boundaries come from keywords, not indentation analysis.
- Human readability: long compound declarations can wrap across lines without becoming fragile.

---

# WSI Implementation Decisions (Slices 2–5)

**Date:** 2026-04-30
**Author:** George (Runtime Dev)

---

## Decision 1: SkipTrivia() removal is safe — parse stream is already trivia-free

**What:** Removed all 7 remaining `SkipTrivia()` call sites from `ParseStateAction`, `ParseTransitionRow`, and `ParseEventHandlerWithGuardCheck`. The method definition had already been deleted in the partial Slice 2 work.

**Why:** With Slice 1's pre-parse filter in place (`tokens.Where(t => t.Kind is not TokenKind.NewLine and not TokenKind.Comment)`), no trivia ever enters the ParseSession token array. Calling `SkipTrivia()` was a no-op before the filter and a compile error after the definition was removed.

**Implication:** Direct `new ParseSession(...)` construction (e.g., in `ExpressionParserTests.cs`) bypasses the filter. Those tests don't inject NewLine tokens so they are unaffected — but callers must be aware that ParseSession itself makes no trivia guarantees.

---

## Decision 2: `NewLine` removed from `StructuralBoundaryTokens`; comment is the authority

**What:** Removed `TokenKind.NewLine` from `StructuralBoundaryTokens`. Added a comment block explaining the belt-and-suspenders layering: (1) pre-parse filter is primary; (2) the Pratt loop terminates at NewLine via the `!OperatorPrecedence` fallthrough as secondary; (3) `StructuralBoundaryTokens` must not add redundant entries for tokens that never arrive.

**Why:** Keeping `NewLine` in the boundary set was harmless with the filter in place but was misleading documentation — it implied NewLine could arrive. Removing it makes the set self-consistent: every member is a real structural token that can arrive during parsing.

---

## Decision 3: `VA_DeclStart` cleared; advisory metadata updated

**What:** `VA_DeclStart` in `Tokens.cs` changed from `[TokenKind.NewLine]` to `[]`. Comment updated to reflect that declaration keywords are keyword-anchored (not newline-following), and that this array is advisory completion metadata, not a parse constraint.

**Why:** The old comment ("Declaration-starting keywords appear after newlines") was accurate for the old grammar but became false documentation once the pre-parse filter was added. Advisory metadata that contradicts the grammar model misleads tooling consumers.

---

## Decision 4: `TypeQualifierNode? Qualifier` → `ImmutableArray<TypeQualifierNode> Qualifiers`

**What:** Both `ScalarTypeRefNode` and `CollectionTypeRefNode` now carry `ImmutableArray<TypeQualifierNode> Qualifiers` (empty array for no qualifiers) instead of a nullable single-qualifier.

**Why:** The `exchangerate` type requires two qualifiers (`in 'USD' to 'EUR'`). A single nullable field cannot model this. The array is the structurally correct representation; empty is the no-qualifier case.

**TypeChecker stub:** The TypeChecker is a stub so no downstream semantic code consumed `.Qualifier`. This provided safe migration ground, but the audit was done anyway — no call sites referenced `.Qualifier` outside of Parser.cs itself.

---

## Decision 5: `AmbiguousQualifierPrepositions` derived from catalog at class init

**What:** A new `FrozenDictionary<TokenKind, FrozenSet<TokenKind>> AmbiguousQualifierPrepositions` static field maps `In → {Modify, Omit, Ensure}` and `To → {Arrow, Ensure}`, derived entirely from `Constructs.ByLeadingToken` and `DisambiguationEntry.DisambiguationTokens`.

**Why:** These are the construct-leading tokens that share a preposition with type qualifiers. If hardcoded, they would drift when new constructs are added. Being catalog-derived, the disambiguation automatically updates when `Constructs.cs` does.

---

## Decision 6: `QualifierShape` gating is load-bearing before `TryPeekQualifierKeyword()`

**What:** `ParseTypeRef` checks `typeMeta.QualifierShape is not null` (from catalog) before entering the qualifier while-loop. `TryPeekQualifierKeyword()` is never called for types that don't accept qualifiers.

**Why:** Without this gate, the parser would attempt qualifier disambiguation after `as string` and could incorrectly consume `in StateName` as a qualifier. The catalog is the authority on which types accept qualifiers — the gate makes this explicit and prevents greedy over-parsing.

---

## Decision 7: `TypeTrait.ChoiceElement` trait drives `ChoiceElementTypeKeywords`

**What:** Added `ChoiceElement = 1 << 2` to the `TypeTrait` flags enum. Applied to `String`, `Boolean`, `Integer`, `Decimal`, `Number`. Replaced the hardcoded `ChoiceElementTypeKeywords` FrozenSet with a catalog-derived one: `Types.ByToken.Where(kvp => kvp.Value.Traits.HasFlag(TypeTrait.ChoiceElement)).Select(kvp => kvp.Key).ToFrozenSet()`.

**Why:** The old hardcoded set was a parallel copy of catalog knowledge. Adding a future type that should be a valid choice element would require two edits. With the trait, one edit to `Types.GetMeta()` is sufficient. Zero behavior change — the 5 types are the same.

---

## Build & Test Status

- `dotnet build src/Precept/Precept.csproj` — green, 0 errors, 0 warnings
- `dotnet test` — 2080 Precept.Tests + 230 Precept.Analyzers.Tests, all pass

---

# Owner Decision: choice of T — Explicit Element Type Required

**By:** Shane (owner)
**Date:** 2026-04-29
**Status:** Locked

## Decision

choice(...) without an explicit of T element type qualifier is a compile error.
Every choice declaration must explicitly name its element type:

`precept
field Status as choice of string("draft", "active", "closed") default "draft"
field Priority as choice of string("Low", "Medium", "High") ordered default "Low"
field ErrorCode as choice of integer(0, 404, 500) default 0
field TaxRate as choice of decimal(0.0, 0.05, 0.10, 0.20) default 0.0
`

There is no implicit default to string. The shorthand choice("a","b","c") is removed.

## Rationale

- Consistency: all choice declarations have the same shape regardless of element type
- Clarity: the type is always visible — no implicit defaulting
- Reinforces the sealed-type mental model: the author explicitly declares a typed vocabulary
- No ambiguity for readers or tooling

## Diagnostic

A new diagnostic is needed: choice(...) without of T → error pointing the author to add the element type.

---

# WSI Test Coverage: Findings and Coverage Gaps

**Date:** 2026-04-30
**Author:** Soup Nazi (Tester)
**Scope:** Parser Whitespace-Insensitivity Slices 1–5 test coverage

---

## Summary

27 new tests added to `test/Precept.Tests/ParserTests.cs` covering the 9 required WSI categories. All 27 pass. Total suite: 2107 passing, 0 failing.

MCP regression (`precept_compile`) could not be run — that tool is not yet implemented (only `precept_ping` exists in `tools/Precept.Mcp/Tools/`).

---

## Gaps and Findings

### GAP-1: `ParseAtom()` does not handle `TypedConstant` tokens

**Category:** Parser correctness / qualifier expressions  
**Severity:** Low (by design, but undocumented)

Single-quoted strings (`'USD'`) lex as `TypedConstant` (TokenKind 116–119). `ParseAtom()` in `Parser.cs` handles `StringLiteral` but not `TypedConstant`. Qualifier values written with single quotes produce an `ExpectedToken` diagnostic; the parse result is structurally broken.

**Impact:** Any user writing `field Rate as money in 'USD'` gets a diagnostic. Must use `"USD"` (double-quoted).

**Recommendation:** Either:
- Extend `ParseAtom()` to handle `TypedConstant` where a string value is expected, or
- Document explicitly in the language spec / error message that qualifier values must be double-quoted string literals, not typed constants.

---

### GAP-2: `StateEnsure` with `when` guard clause not implemented

**Category:** Parser completeness  
**Severity:** High (blocks sample files)

Grammar form `in State ensure Condition when Guard because "msg"` is present in `insurance-claim.precept` and `loan-application.precept`. The parser terminates the condition expression at `when`, then `Expect(Because)` fails. Both sample files produce parser diagnostics on `StateEnsure` blocks with guards.

**Impact:** Two of the three primary sample files have parse errors. `hiring-pipeline.precept` is the only sample that parses cleanly.

**Recommendation:** Track as a first-class parser gap. Add `when` branch to `StateEnsure` parsing to recognize and attach the guard clause.

---

### GAP-3: `is set` / `is not set` membership expressions may be incomplete

**Category:** Parser completeness  
**Severity:** Medium

`insurance-claim.precept` contains `field.expression is set` / `field.expression is not set` expressions. The parser may not recognize `is` as a postfix or infix operator for null/set-membership checks. These appear to contribute diagnostics in the sample file parse.

**Recommendation:** Confirm whether `is set` is in the language spec. If specified, add `Is` to `OperatorPrecedence` or handle it as a postfix sentinel in `ParseAtom()`/`ParseExpression()`.

---

### GAP-4: `TypeChecker.Check()` is not implemented

**Category:** Test infrastructure / pipeline completeness  
**Severity:** Medium (blocks `Compiler.Compile()` in tests)

`TypeChecker.Check()` at `src/Precept/Pipeline/TypeChecker.cs` line 26 throws `NotImplementedException`. Any test using `Compiler.Compile()` throws. Tests must use `Lexer.Lex()` + `Parser.Parse()` directly, which bypasses type checking entirely.

**Impact:** All existing parser tests — including the 27 new WSI tests — can only assert structural correctness. Type-mismatch, unresolved-reference, and other type-level correctness properties cannot be tested until TypeChecker is implemented.

**Recommendation:** Implement TypeChecker or at minimum provide a stub that returns cleanly. The existing test suite at 2107 tests has zero type-checking coverage by definition.

---

### GAP-5: MCP `precept_compile` not implemented

**Category:** MCP tool coverage  
**Severity:** Medium (blocks regression protocol)

The project's canonical regression protocol requires 4 rounds of `precept_compile` to verify WSI behavior end-to-end. `tools/Precept.Mcp/Tools/` contains only `PingTool.cs`. None of the 5 MCP tools described in the custom instructions exist yet.

**Impact:** The MCP regression gate defined in the WSI test charter cannot be executed. The 4 regression inputs from this session should be preserved as smoke tests when `precept_compile` ships:

1. Multi-line field with default → zero errors
2. `field Rate as exchangerate in "USD" to "EUR"` → qualifiers parsed correctly, zero errors
3. `field Rates as list of exchangerate in "USD" to "EUR"` → collection element qualifiers parsed, zero errors
4. `in Draft modify Status to "active"` → `in Draft` parsed as state context, NOT as qualifier

---

## Tests Added

| Test | Category | Status |
|------|----------|--------|
| `WSI_Slice1_NewlinesBetweenFields_Parsed` | Multi-line whitespace | ✅ Pass |
| `WSI_Slice1_MultiLineFieldWithDefault_Parsed` | Multi-line whitespace | ✅ Pass |
| `WSI_Slice1_MultiLineFieldWithConstraint_Parsed` | Multi-line whitespace | ✅ Pass |
| `WSI_Slice1_CommentOnSameLineAsDecl_Filtered` | Comment filtering | ✅ Pass |
| `WSI_Slice1_CommentBetweenFields_Filtered` | Comment filtering | ✅ Pass |
| `WSI_Slice1_CommentAtEndOfBlock_Filtered` | Comment filtering | ✅ Pass |
| `WSI_Qualifier_ExchangeRate_TwoQualifiers` | Multi-qualifier parsing | ✅ Pass |
| `WSI_Qualifier_Price_TwoQualifiers` | Multi-qualifier parsing | ✅ Pass |
| `WSI_Qualifier_MultilineWithComments_Parsed` | Multi-qualifier parsing | ✅ Pass |
| `WSI_Qualifier_InKeyword_IsQualifierWhenFollowedByCurrency` | Qualifier disambiguation | ✅ Pass |
| `WSI_Qualifier_InDraftModify_IsNotQualifier` | Qualifier disambiguation | ✅ Pass |
| `WSI_Qualifier_InWithAmbiguousVerb_TreatedAsBoundary` | Qualifier disambiguation | ✅ Pass |
| `WSI_CollectionQualifier_SetOfMoney_SingleElementQualifier` | Collection qualifiers | ✅ Pass |
| `WSI_CollectionQualifier_ListOfExchangeRate_TwoElementQualifiers` | Collection qualifiers | ✅ Pass |
| `WSI_CollectionQualifier_SetOfPrice_TwoElementQualifiers` | Collection qualifiers | ✅ Pass |
| `WSI_Negative_PureStringField_NoQualifiers` | Negative cases | ✅ Pass |
| `WSI_Negative_InStateModify_NoQualifiersOnField` | Negative cases | ✅ Pass |
| `WSI_Negative_FieldWithOnlyWhitespace_ParsesCleanly` | Negative cases | ✅ Pass |
| `WSI_TokenStream_NewlineTokens_PresentInOriginalStream` | Token stream regression | ✅ Pass |
| `WSI_TokenStream_CommentTokens_PresentInOriginalStream` | Token stream regression | ✅ Pass |
| `WSI_TokenStream_ParseSession_SeesFilteredTokens` | Token stream regression | ✅ Pass |
| `WSI_Integration_SampleFile_ParsesWithNoErrors` (hiring-pipeline only) | Integration sample files | ✅ Pass |
| `WSI_Integration_InsuranceClaim_ParsesStructurally` | Integration sample files | ✅ Pass (known diagnostics) |
| `WSI_Integration_LoanApplication_ParsesStructurally` | Integration sample files | ✅ Pass (known diagnostics) |
| `WSI_ChoiceElement_CatalogRegression_ExactlyFiveTypes` | ChoiceElementTypeKeywords catalog | ✅ Pass |
| `WSI_ChoiceElement_CatalogRegression_OnlyPrimitiveTypes` | ChoiceElementTypeKeywords catalog | ✅ Pass |
| `WSI_ChoiceElement_CatalogRegression_ContainsExpectedKinds` | ChoiceElementTypeKeywords catalog | ✅ Pass |

---

# Frank gap analysis inbox

Date: 2026-05-01
Requested by: Shane

## GAP-1 — single-quoted qualifier values

- Spec intent is clear: single quotes are typed constants, double quotes are plain strings.
- The spec's qualifier examples explicitly use typed constants: `field Amount as money in 'USD'`, `field Distance as quantity of 'length'`.
- Qualifier values should remain typed/domain values, not labels. Future checking should validate them against currency/unit/dimension registries rather than treat them as arbitrary text.
- Current parser behavior is therefore a bug, not an intentional restriction. `ParseAtom()` needs typed-constant support in ordinary expression positions used by type qualifiers.

## GAP-2 — `ensure ... when ...`

- Guarded ensures are already part of language intent, but the spec is internally inconsistent on placement.
- Grammar sections use post-condition form (`ensure BoolExpr ("when" BoolExpr)?`), while one prose/example section still shows pre-`ensure` guard placement.
- Samples use post-condition form consistently: `in Approved ensure DecisionNote is set when FraudFlag because ...`.
- Semantic intent: the guard makes the ensure conditional, like guarded rules. It does **not** behave like a transition-row availability guard.
- Recommendation: parser fix plus spec cleanup to one canonical form, with post-condition `ensure Condition when Guard because ...` as the preferred surface.

## GAP-3 — `is set` / `is not set`

- The spec already treats `is set` / `is not set` as a real expression operator and ties it to the removal of `null`.
- The operator means presence/absence of an optional value, not collection membership.
- Samples show both field and optional event-argument usage (`DecisionNote is set`, `Approve.Note is set`), so spec wording should broaden from "optional field" to optional references/slots.
- Recommendation: implement as catalog-owned presence operators (`IsSet` / `IsNotSet`, or equivalent metadata-backed representation), then align parser/type checker/spec wording.

## Overall

- All three gaps are expression-surface drift between spec/samples and parser reality.
- GAP-1 and GAP-3 share the same immediate parser root: the Pratt parser's atom/led coverage does not match the spec's literal/operator surface.
- GAP-2 is a grammar-shape drift: AST and samples expect guarded ensures, but parser recognition only handles the older/stale placement.
- Fixing these gaps should unblock the two broken sample files, and also expose one adjacent issue: guarded `on ... ensure ... when ...` uses the same broken ensure shape and should be fixed in the same pass.

---

# George gap analysis inbox — parser implementation

Date: 2026-05-01
Requested by: Shane

---

## GAP-1 — Single-quoted strings (`'USD'`) lex as `TypedConstant`, not `StringLiteral`

### Lexer facts

The lexer emits four distinct token kinds for single-quoted literals:

| Kind | Value | Description |
|------|-------|-------------|
| `TypedConstant = 116` | text | Simple form `'USD'` — no interpolation |
| `TypedConstantStart = 117` | text | First segment of `'prefix {expr}...'` |
| `TypedConstantMiddle = 118` | text | Middle segment between interpolation holes |
| `TypedConstantEnd = 119` | text | Final segment of an interpolated typed constant |

These are structurally parallel to `StringLiteral/StringStart/StringMiddle/StringEnd`. The lexer enters `LexerMode.TypedConstant` on `'`, produces `TypedConstant` on the closing `'` (no interpolation), or the `Start/End` pair when `{...}` holes are present. The lexer is complete — the gap is entirely in the parser.

### ParseAtom() gap

`ParseAtom()` handles: `NumberLiteral`, `StringLiteral`, `True`, `False`, `StringStart`, `Identifier`, `Not`, `Minus`, `LeftParen`, `If`. No case exists for `TypedConstant` (116) or `TypedConstantStart` (117). Any call to `ParseExpression(0)` that encounters a single-quoted value falls to the `default:` arm and emits `ExpectedToken "expression"`.

### Where qualifier values enter ParseAtom

`ParseTypeRef()` (line 1123) is the only qualifier-value path. When the catalog says a type has a `QualifierShape`, it enters a while-loop that calls `ParseExpression(0)` for each qualifier value. That route hits `ParseAtom()`. The qualifier keyword itself is consumed by `Advance()` before `ParseExpression(0)` is called, so the first token `ParseAtom()` sees is the value — a `TypedConstant` token.

### Minimal fix

Two new case arms in `ParseAtom()`:

```csharp
case TokenKind.TypedConstant:
    return new LiteralExpression(current.Span, Advance());

case TokenKind.TypedConstantStart:
    return ParseInterpolatedTypedConstant();
```

Plus a `ParseInterpolatedTypedConstant()` method that mirrors `ParseInterpolatedString()` exactly, substituting `TypedConstantMiddle`/`TypedConstantEnd` for `StringMiddle`/`StringEnd`. The `InterpolatedStringExpression` AST node can be reused — it wraps `ImmutableArray<InterpolationPart>` with no string-vs-typed-constant discrimination at the AST level. (Type checking later supplies that context.)

This is a four-line change to the switch plus a ~25-line parse method.

### Risks

1. **ParseChoiceValue** (line 1233): directly checks `cur.Kind == TokenKind.StringLiteral` for string-typed choice options — `TypedConstant` will still fail there. This is a separate but related gap not in scope for this task.
2. **Type checking**: `UnresolvedTypedConstant (52)` and `InvalidTypedConstantContent (53)` diagnostics exist and are ready. The TypeChecker is a stub, so no immediate downstream breakage. When TypeChecker is implemented it will need to validate TypedConstant values against the qualifier's expected type (currency code, unit, dimension, etc.).
3. **Interpolated typed constants**: The interpolated path (`TypedConstantStart`) raises the question of whether interpolation is valid in qualifier position — e.g., `money in 'US{country}'`. This needs Frank's spec sign-off. The simple non-interpolated form (`TypedConstant`) is unambiguous and can be fixed immediately.

### Downstream consumers to update

TypeChecker: stub (no change needed now). Evaluator: stub (no change needed now). No runtime impact.

### Effort: **Small**

### Dependency on Frank

**Yes** — narrowly: is an interpolated TypedConstant (`TypedConstantStart`) valid in qualifier position? For the simple non-interpolated form this is a clear bug fix. Frank should confirm whether interpolated qualifier values are in scope for the same pass or deferred.

---

## GAP-2 — `StateEnsure` with `when` guard not implemented

### Current ParseStateEnsure implementation

```csharp
// Parser.cs line 418
private StateEnsureNode ParseStateEnsure(SourceSpan start, Token preposition, StateTargetNode anchor, Expression? stashedGuard)
{
    Advance(); // consume 'ensure'
    var condition = ParseExpression(0);
    var because = Expect(TokenKind.Because);   // ← fails when 'when' is next
    var message = ParseExpression(0);
    return new StateEnsureNode(..., stashedGuard, condition, message);
}
```

### Why `when` causes failure

`TokenKind.When` is in `StructuralBoundaryTokens` (line 95):

```csharp
private static readonly FrozenSet<TokenKind> StructuralBoundaryTokens = new[]
{
    TokenKind.When, TokenKind.Because, TokenKind.Arrow, TokenKind.Ensure,
    TokenKind.EndOfSource,
}.ToFrozenSet();
```

So `ParseExpression(0)` inside `ParseStateEnsure` terminates at `when` (correctly, per the boundary set). Then `Expect(TokenKind.Because)` fires on the `when` token → emits `ExpectedToken "because"`.

### Guard semantics: stashed vs inline

The dispatch loop calls `TryParseStashedGuard()` before dispatching. A "stashed guard" handles the pre-`ensure` form: `in State when Guard ensure Condition because Msg`. The `stashedGuard` parameter carries that guard in.

The failing form is **post-condition inline guard**: `in State ensure Condition when Guard because Msg`. These are equivalent semantically (the guard makes the ensure conditional), but the stashed path is already wired and the inline path is not.

### AST node: already designed for Guard

`StateEnsureNode` (SyntaxNodes/StateEnsureNode.cs):

```csharp
public sealed record StateEnsureNode(
    SourceSpan Span,
    Token Preposition,
    StateTargetNode State,
    Expression? Guard,       // ← exists; used for stashed guard
    Expression Condition,
    Expression Message) : Declaration(Span);
```

No new AST node is needed.

### Minimal fix

Mirror exactly what `ParseAccessMode` does (lines 387–391):

```csharp
private StateEnsureNode ParseStateEnsure(SourceSpan start, Token preposition, StateTargetNode anchor, Expression? stashedGuard)
{
    Advance(); // consume 'ensure'
    var condition = ParseExpression(0);

    // Post-condition inline guard: accept either stashed or inline, not both
    Expression? guard = stashedGuard;
    if (guard is null && Current().Kind == TokenKind.When)
    {
        Advance(); // consume 'when'
        guard = ParseExpression(0);
    }

    var because = Expect(TokenKind.Because);
    var message = ParseExpression(0);

    return new StateEnsureNode(
        SourceSpan.Covering(start, message.Span),
        preposition, anchor, guard, condition, message);
}
```

This is a five-line addition to an existing method. No new infrastructure.

### Adjacent gap: EventEnsure

`ParseEventEnsure()` (line 543) has the **identical** missing inline-guard handling. `insurance-claim.precept` line 35 demonstrates it: `on Submit ensure Submit.Amount <= 100000 when Submit.RequiresPoliceReport because "..."`. The `EventEnsureNode` also has `Expression? Guard`. The same fix applies to both methods — they should be updated in the same pass.

### Risks

1. **Dual guard ambiguity**: If someone writes `in State when A ensure Condition when B because Msg`, both stashed and inline guards are present. The `guard is null` check handles this gracefully (stashed takes precedence, inline is silently ignored). A dedicated diagnostic for the dual-guard case would be cleaner but is not required for the minimal fix.
2. **GAP-3 interaction**: The canonical sample `in Approved ensure DecisionNote is set when FraudFlag because ...` has BOTH GAP-2 and GAP-3 active simultaneously. Fixing GAP-2 alone does not make that sample parse cleanly — `is set` must also be resolved. The two fixes are independent but should land together for the sample to validate end-to-end.
3. **Spec consistency**: Frank's analysis notes a spec ambiguity between pre-`ensure` and post-condition guard placement. Implementation should follow the post-condition form (as samples show), and the spec cleanup should confirm the canonical form.

### Downstream consumers

TypeChecker: stub. Evaluator: stub. No impact.

### Effort: **Small**

### Dependency on Frank

**Yes** — confirm that the post-condition `ensure Condition when Guard because Msg` form is the canonical surface, and whether the pre-`ensure` stashed form is also supported or should be deprecated. Also confirm EventEnsure should get the same fix in the same pass.

---

## GAP-3 — `is set` / `is not set` membership expressions

### Current `is` handling in the parser

`Is` has `TokenKind.Is = 42`. It appears in `Tokens.cs` (Cat_Mem, "Multi-token operator prefix (is set, is not set)"), in `TokenKind.cs`, and is referenced in `SyntaxReference.cs` and `Modifiers.cs` documentation. But:

- `OperatorKind` has no `IsSet` or `IsNotSet` member.
- `Operators.cs` has no entry for `Is`.
- `OperatorPrecedence` (derived from `Operators.All`, binary only) has no entry for `TokenKind.Is`.
- `TokenKind.Is` is referenced **nowhere** in `Parser.cs`.

The Pratt loop exits immediately on `is` after the left operand (`!OperatorPrecedence.TryGetValue(current.Kind, out var opInfo)` → `break`). There is no partial implementation — `is` as an operator is entirely absent from the parser.

### Exact sample usage

From `insurance-claim.precept` line 28:
```
in Approved ensure DecisionNote is set when FraudFlag because "..."
```

From `loan-application.precept` line 62:
```
-> set DecisionNote = if Approve.Note is set then Approve.Note else ...
```

From `library-book-checkout.precept`:
```
in Available ensure BorrowerId is not set because "..."
in CheckedOut ensure BorrowerId is set because "..."
```

`is set` is a boolean presence check on an optional field. `is not set` is its negation. Neither uses `is null` — the samples exclusively use `is set`/`is not set`. The Tokens catalog description also only lists these two forms.

### Implementation gap

`is set` is a **two-token postfix operator**: left operand + `is` + `set`. `is not set` is a **three-token postfix operator**: left operand + `is` + `not` + `set`. This makes it non-trivial for the current Pratt architecture:

- `OperatorPrecedence` is a `FrozenDictionary<TokenKind, (Precedence, RightAssociative)>` keyed on a single token kind. `Is` can be added as an entry to get the Pratt loop to recognize the start of the operator, but the loop body must then handle multi-token lookahead to distinguish `is set` vs `is not set`.
- The existing `UnaryExpression` node holds a single `Token Operator` — it cannot represent a two- or three-token operator sequence.

### What a catalog-driven implementation looks like

**Step 1 — OperatorKind:**
Add `IsSet = 19` and `IsNotSet = 20` to `OperatorKind`. These are postfix unary operators.

**Step 2 — Operators.cs:**
Add two entries with `Arity.Unary` (postfix), `OperatorFamily.Membership`, `Precedence: 40` (same as `Contains`), backed by `Tokens.GetMeta(TokenKind.Is)` as the leading token. The `OperatorMeta` record may need a `SecondToken` field, or a `MultiTokenSuffix string[]` field, for the trailing `set`/`not set` text. This is a Frank/architecture decision on how multi-token operators live in the catalog.

**Step 3 — AST node:**
A new `IsSetExpression` (or `PresenceCheckExpression`) node:
```csharp
public sealed record IsSetExpression(
    SourceSpan Span,
    Expression Operand,
    bool IsSet) : Expression(Span);
```
`UnaryExpression` cannot be used because it only holds one operator token.

**Step 4 — Pratt loop:**
Add `TokenKind.Is` to `OperatorPrecedence` (precedence 40, non-associative). In the Pratt loop, handle `Is` specially: consume `Is` token, peek at next token:
- If `Set` → consume it, produce `IsSetExpression(left, IsSet: true)`
- If `Not` → consume it, expect `Set`, produce `IsSetExpression(left, IsSet: false)`
- Otherwise → diagnostic

**Step 5 — Type checker (when unblocked):**
`DiagnosticCode.IsSetOnNonOptional = 49` already exists with the correct message. The type checker should use it when `IsSetExpression.Operand` resolves to a non-optional type.

### Risks

1. **`not` keyword collision**: `is not set` uses the `Not` keyword between `is` and `set`. `Not` also exists as a standalone prefix operator. Inside the `Is` Pratt branch, consuming `Not` is unambiguous (you're already past `Is`), but the grammar comment must be explicit.
2. **Precedence of `not` inside `is not set`**: This is NOT the prefix `not` operator being applied — it is structural syntax within the `is not set` form. The Pratt loop must consume `not` as a keyword token, not re-enter `ParseExpression` recursively.
3. **Catalog shape for multi-token operators**: This is the first case where an operator spans more than one token. If the catalog's `OperatorMeta.Token` field only accommodates one token, a shape extension is needed. This is a non-trivial catalog design decision.
4. **Type-checker interaction**: `IsSetExpression` must only be valid on `optional` fields. The `IsSetOnNonOptional` diagnostic is at the type stage. The parser should accept `is set` on any left operand; the type checker enforces the optional constraint.

### Effort: **Medium**

This requires: Frank decision on catalog shape for multi-token operators, a new AST node, OperatorKind additions, non-trivial Pratt loop extension, and a new diagnostic path in the type checker (when unblocked). None of the individual pieces are large, but together they touch four layers (catalog, AST, parser, type stage).

### Dependency on Frank

**Yes** — specifically:
- Should `IsSet`/`IsNotSet` be `OperatorKind` members? If yes, does `OperatorMeta` need a multi-token suffix field?
- What is the precedence level relative to `Contains` and comparison operators?
- Is `is null` / `is not null` also in scope, or exclusively `is set`/`is not set`?
- AST: reuse `UnaryExpression` with a compound operator token, or new `IsSetExpression` node?

---

## Overall assessment

### Implementation order recommendation

1. **GAP-1 first** — independent, minimal risk, unblocks any precept using typed-constant qualifier values. Simple non-interpolated form only; defer interpolated qualifier constants if Frank wants a separate decision.
2. **GAP-3 second** — requires Frank's catalog-shape decision first. Once that's locked, implementation is medium scope but self-contained.
3. **GAP-2 third** — depends on `is set` (GAP-3) being parseable before the canonical insurance-claim sample validates end-to-end. But the GAP-2 parser fix itself is independent of GAP-3 and can land first — it just won't clear the sample until GAP-3 also lands.

Pragmatic sequencing: **GAP-2 immediately** (five lines, zero design risk), **GAP-1 immediately** (simple form), **GAP-3 after Frank design sign-off**.

### Shared infrastructure

- **GAP-2 and EventEnsure**: These two fixes share the identical pattern and must land in the same commit. `ParseStateEnsure` and `ParseEventEnsure` are parallel implementations that drifted.
- **GAP-1 and GAP-3**: Both require `ParseAtom()` changes. If landed in the same pass, a single test sweep covers both.
- **GAP-3 catalog work**: The multi-token operator shape decision (if Frank requires a catalog extension) could also affect future operators with compound syntax. That design decision should be scoped and resolved before touching `OperatorMeta`.
