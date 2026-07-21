# Fault family — the full coordinate space, with a disposition on every cell

**Status**: Draft — 2026-07-21. Companion artifact to `disposition-map.json` in the same folder, which
carries all 9,292 cells individually. This file is the readable account; the JSON is the artifact that
proves totality.

## What was crossed

Three axes, none of them invented here:

| Axis | Values | Source |
|---|---|---|
| Proof-requirement site | 101 | `sites.json` — every catalog-declared `ProofRequirement`, from `Operations.cs` (74), `Types.cs` (18), `Actions.cs` (7), `Functions.cs` (2) |
| Evaluation-site category | 23 | `eval-sites.json` — every expression position the grammar admits, argued off the top-level dispatch table rather than off a sample sweep |
| Type family | 4 | `docs/language/README.md` — primitive, temporal, business-domain, collection |

101 × 23 × 4 = **9,292 cells**. Every one of them appears in `disposition-map.json` with exactly one
disposition. None is absent, and no cell carries a generic refusal.

## The counts

| Disposition | Cells | Share |
|---|---|---|
| empty (pruned, with derivation) | 7,047 | 75.8% |
| defined | 992 | 10.7% |
| open | 760 | 8.2% |
| deferred | 441 | 4.7% |
| conflicted | 52 | 0.6% |

## The pruned regions, and why each is pruned

Five derivations account for all 7,047 empty cells. Each is an argument, not an assertion.

**Type-family mismatch — 6,693 cells.** A catalog site declares the types its entry ranges over. A cell
whose type-family coordinate is not the family of any of those declared types cannot be reached by any
program: there is no expression that mints `divisor must be non-zero` for `integer / integer` at a
collection-typed value, because neither operand slot admits a collection type. This one derivation prunes
roughly three quarters of the product on its own, which is the expected shape — most sites live in one
family, a minority (a money numerator with a decimal divisor, for instance) live in two.

**The choice value production — 113 cells.** `ChoiceValueExpr` admits only a string literal, a number
literal, `true` or `false`; no operator, member access, or function call can appear, and negative numeric
literals reach the position through parser constant folding, which likewise yields a literal. So no
catalog-declared safety precondition has any operation to attach to. The category is dispositioned rather
than dropped precisely because the grammar calls it an expression position.

**Collection actions outside action positions — 72 cells.** A collection action is an `ActionStatement`,
and the grammar admits an `ActionStatement` only after `->` in a transition row, a stateless hook, a
construction row, or a state hook. An `insert at` requirement therefore cannot arise inside a rule
condition or a field default.

**`dequeue` / `pop` outside the operand-free category — 63 cells.** These carry an `ActionMeta`
requirement whose subject is the collection field and which has no authored operand at all. They occur at
exactly one evaluation-site category, the one the enumeration created for them.

**No expression in the operand-free category — 106 cells.** The converse: the operand-free
action-precondition category holds no authored expression, so no operator, function or accessor site can
occur inside it.

## The deferred region — 441 cells, 13 sites, one owner ruling

The owner ruled temporal arithmetic out of scope for this slice, pending the unmade number-model
decision. `docs/language/primitive-types.md:688` states that the temporal arm rides with the integer
overflow deferral; `docs/language/temporal-type-system.md:624` restates it from the temporal side.

Thirteen catalog sites are deferred under it: the four period-dimension checks on `date ± period` and
`time ± period`, the three duration divisions, the four money- and quantity-over-period/duration
divisions, and the two price-times-period/duration qualifier chains.

**Read this boundary carefully — it is wider than the canon text it cites.** The cited deferral covers
representable-range overflow only. The applied reading here is "any catalog site whose declaring entry is
an arithmetic operation with a temporal-typed operand or result", which also sweeps in the
period-dimension and divide-by-zero requirements on temporal operands. Those have nothing to do with the
number model. The wider reading was applied because the instruction named temporal arithmetic as a whole,
and the alternative — deciding the boundary here — would have been inventing an answer. It is listed
below as a missing rule.

**Arithmetic overflow prunes nothing.** No catalog site in `sites.json` declares an overflow requirement;
the requirement-kind histogram has no overflow member. HEAD nevertheless emits `NumericOverflow` today
(observed on `samples/Test2.precept`). So overflow faults are minted by the engine without a
catalog-declared `ProofRequirement` behind them, which means the site axis — derived from the catalogs —
structurally cannot contain them. That is recorded below as a missing rule, and it matters beyond this
slice: the matrix's completeness claim for the fault family is only as wide as the catalog it enumerates.

## The open region — 760 cells, one missing rule doing almost all the work

Thirty-seven sites are open, and 36 of them are open for the same reason.

**No premise class supplies type-qualifier facts — 756 cells across 36 sites.** The requirement kinds
`QualifierCompatibility` (32 sites), `QualifierChain` (3 remaining after the temporal deferral), and
`DimensionalProduct` (1) are discharged by facts about a value's currency, unit, or dimension. Those
facts live in a type reference — `money in 'USD'`, `quantity of 'kg'` — not in a modifier. The matrix's
four premise classes are field modifiers, argument constraints, the handler's guard, and pre-state
constraints, and a type qualifier is none of them. `ModifierMeta.ProofSatisfactions`, which the matrix
points at as the catalog anchor for what discharges what, carries `ProofSatisfaction.Numeric` entries
only (`src/Precept/Language/Modifiers.cs:73`). So the obligation side of these cells is derivable — the
catalog declares the precondition — but the discharge contract cannot be written, and a defined cell
requires one. They are open, not defined, and not empty.

**No premise class supplies collection-contents facts — 4 cells, 1 site.** `append ... by` declares that
the ordering key must not already exist. Discharging that needs a fact about which keys the collection
currently holds. All four premise classes are declaration- or guard-scoped; none ranges over collection
contents. Adjacent to the standing open question of whether quantified constraints are proof surface at
all (`docs/Working/obligation-discharge-matrix-2026-07-19.md:312`), but not the same question.

## The conflicted region — 52 cells, one category

Every defined-shaped cell at `stateless-hook-action-operand` is conflicted.
`docs/language/precept-language-spec.md § Stateless event hook` states that handlers do not support
`when` guards. `samples/Test2.precept:4` writes one, and HEAD both accepts it and consumes it as a
premise — the measured run printed `[guard normal-form-equal to WP]` for that row. A defined cell must
state its applicable premise-class set, and whether class (c) exists at this category turns on which
source is right. The obligation side is settled; only the class set is disputed, so these 52 cells become
defined the moment the owner rules. Both sources are cited on every cell.

## The defined region — 992 cells in 19 authorable groups

The 992 defined cells factor cleanly into eight discharge stories crossed with seven premise settings.
The groups below follow that factoring: each shares one worked setting, and each authors a number of
representative cells rather than one cell per product coordinate, because within a story many
coordinates mint obligations that unify with the same schema and therefore sit in the same cell under the
matrix's own identity criterion.

| Group | Cells | Coordinates covered | What it is |
|---|---|---|---|
| 1 | 10 | 10 | Division and remainder by zero, primitive lanes, transition-row write |
| 2 | 10 | 20 | Same, at a construction row and a state hook |
| 3 | 10 | 50 | Same, inside the five guard positions |
| 4 | 10 | 50 | Same, in constraint conditions and computed fields |
| 5 | 10 | 50 | Same, in declaration-position value expressions |
| 6 | 8 | 20 | Same, in a reject message and a `because` rationale |
| 7 | 11 | 15 | Division by zero on money/quantity/price/rate, transition-row write |
| 8 | 8 | 30 | Same, at a construction row and a state hook |
| 9 | 10 | 75 | Same, inside guards |
| 10 | 12 | 105 | Same, in constraint conditions and messages |
| 11 | 10 | 75 | Same, in declaration positions |
| 12 | 12 | 40 | `sqrt` and `pow` preconditions across all six premise settings |
| 13 | 15 | 45 | Reading a possibly-empty collection, at write sites |
| 14 | 10 | 75 | Same, inside guards |
| 15 | 12 | 105 | Same, in constraint conditions and messages |
| 16 | 10 | 75 | Same, in declaration positions |
| 17 | 12 | 60 | Indexed access out of bounds |
| 18 | 12 | 12 | Collection actions with a safety precondition |
| 19 | 12 | 80 | Comparing choice values never declared `ordered` |

204 authored cells covering all 992 defined coordinates. Full setting and authoring notes per group are
in `groups.json` and in the `groups` block of `disposition-map.json`.

### The hazard every authoring agent must be told

Slice 2 verified a false proof at HEAD: a business rule used as a premise can discharge a fault
obligation that nothing establishes or preserves. `rule PlannedQuantity > 0` discharges a divide-by-zero
via the `CompositionalConstraint` strategy while a different handler sets the field to zero, and the file
compiles with zero diagnostics (`authored-expressiveness-gaps.md`, Part 3, Defect B). Consequently: in
any group whose region includes an evaluation-site category where premise class (d) is available, a
witness whose discharge cites a `rule` must record `builtStatus` as unverified with that reason named. A
clean compile is not confirmation there. Guard-based and modifier-based discharges are unaffected.

Groups whose region is entirely free of class (d) — where clean-compile confirmation is admissible — are
5, 11, 16, and 19. Group 4 is mixed and is flagged as such. Every other group carries the hazard note.

### The other thing to tell them: ten positions mint nothing at HEAD

The evaluation-site enumeration measured, at commit `e1a14d91`, ten expression positions that raise no
fault obligation at all: all five guard positions, both interpolation positions, and three of the five
declaration positions. Base witnesses at those positions must be recorded model-provable /
built-unresolved. A clean compile there is the site failing to mint, not the program being accepted. Two
further positions — `type-qualifier-expression` and `collection-inner-type-modifier-value-expression` —
were not measured at all and must be recorded as unmeasured rather than inferred from the scalar case.

## Missing rules — what the matrix cannot currently answer

These are the cells whose answers are not derivable from what the matrix says today. They are the most
valuable output of this pass.

1. **No premise class covers type-qualifier facts.** 36 sites, 756 cells, all open. The matrix's four
   premise classes have no member that a currency, unit, or dimension qualifier instantiates, and
   `ProofSatisfactions` carries numeric satisfactions only. Either a fifth class, or an extension of
   class (a) beyond modifiers to declaration attributes generally, or a ruling that these requirements
   are type-checking rather than proof surface.

2. **No premise class covers collection-contents facts.** The `append by` uniqueness requirement needs to
   know which keys a collection already holds. Nothing in the premise vocabulary ranges over contents.

3. **The temporal-arithmetic exclusion has no written boundary.** The canon deferral covers representable
   range overflow. Whether it also covers period-dimension checks and divide-by-zero on a temporal
   divisor is unwritten. Thirteen sites were deferred under the wider reading; under the narrower reading
   nine of them would be defined.

4. **Overflow faults have no catalog site.** HEAD emits `NumericOverflow`, but no `ProofRequirement` in
   any catalog declares an overflow precondition. The fault family's site axis is derived from the
   catalogs, so overflow is structurally outside it — which means the matrix's exhaustiveness claim for
   the fault family currently excludes a fault the engine actually raises. This needs either a catalog
   entry or a written statement that overflow is minted by a different mechanism.

5. **Two-sided index-bounds discharge is not covered by any written decision procedure.** Proving
   `0 <= i < count` consumes both an interval on the index and a cardinality fact about the collection.
   The matrix's only written fault decision procedure (Family 4) computes one operand's interval from its
   modifiers and in-scope guard facts. Whether the two-premise combination is licensed, and by which
   derivation, is unstated. Group 17 is authored against this gap and must report back rather than fill
   it.

6. **Site identity versus evaluation occasion is unsettled.** This pass treats an evaluation site as a
   syntactic position, following the enumeration. The alternative — one site per occasion, so a rule
   condition evaluated at an editable-field ingress is a different site from the same condition at a
   handler checkpoint — would change premise availability materially, because ingress evaluation is a
   discharge mechanism the matrix rules on separately. Nothing in the matrix settles the cross-occasion
   case. Every cell in this map inherits the syntactic reading; a ruling the other way re-opens all 992
   defined cells.

7. **Self-discharge inside a guard is unstated in two shapes.** Whether an earlier conjunct of a guard is
   a premise for a later one (`when Go.Parts != 0 and Total / Go.Parts > 1.0`), and whether the branches
   of `if C then A else B` make `C` a premise inside them. The second affects every category, since
   conditional expressions appear in all of them.

8. **A conditional rule's `when` and a constraint's failure context are outside the premise vocabulary.**
   `rule X / Y > 1 when Y != 0` is the spelling an author reaches for, and the matrix's class (c) is the
   *handler's* guard, which a rule activation guard is not. Likewise the `because` message renders only
   when the constraint is false — a fact no class carries.

## Open questions carried but not answered

- Whether the residency fact (the entity is in state S) is a premise, and under which class. The matrix
  records this as open; it lands on four evaluation-site categories and rides as an open dependency on
  groups 2, 4, 10, and 15.
- Whether quantified constraints are proof surface at all. Rides on the quantifier-predicate cells in
  groups 4, 10, and 15.
- Whether the broader reading of class (d) — "other constraints hold in this configuration" — applies at
  a rule condition or a computed field. Deliberately not adopted here; it is exactly the reading the Part
  3 defect shows going wrong.
