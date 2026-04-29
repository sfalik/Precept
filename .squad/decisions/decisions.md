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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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



---

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
