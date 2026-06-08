---
status: Active — horizon groundwork
authored: 2026-06-03
author: research (lifecycle-1)
topic: How schema/constraint/type systems make "constrains each element" syntactically distinct from "constrains the aggregate", and candidate directions for Precept's collection inner-type value modifiers
external-engagement: strong
---

# Collection element-vs-aggregate constraint scope: making the axis visible

> When a value modifier is written after a collection's inner type — `queue of string maxlength 200` — does it constrain *each element* or *the collection as a whole*? Today Precept resolves this implicitly, by modifier applicability. This survey asks how comparable systems make that axis explicit, and lays out candidate directions for Precept neutrally — it does not lock a syntax.

## Background

Precept ships per-element value modifiers on collection inner types today: `queue of string maxlength 200` caps each element's length; `set of integer min 0 max 100` bounds each element's value (see [`collection-types.md § Element value modifiers`](../../docs/language/collection-types.md#element-value-modifiers) and [`precept-language-spec.md` §2.3/§2.4](../../docs/language/precept-language-spec.md)). The element-vs-collection axis is resolved **implicitly by applicability**:

- A modifier that is **scalar-only** (`maxlength`, `minlength`, `min`, `max`, `maxplaces`, `nonnegative`, `positive`, `nonzero`) binds to the **element** — it cannot mean anything about a collection.
- A modifier that **applies to the collection** (`maxcount`, `mincount`) binds to the **collection**.

The routing is catalog-derived, not hand-maintained: `Modifiers.ElementPositionValueTokens` selects exactly the value modifiers that are type-restricted *and* apply to no collection type, so they are scalar-only and "cannot collide with a collection-level modifier" ([`src/Precept/Language/Modifiers.cs`](../../src/Precept/Language/Modifiers.cs)).

**The owner's concern (the research question).** This rule is *invisible to a reader/author*. To know that `maxlength 200` in `queue of string maxlength 200` constrains each *element* and not the queue, the reader must already know that `maxlength` is a string-only modifier. There is no syntactic signal on the axis. And one modifier genuinely collides: `notempty` is the **sole** modifier valid on both a scalar `string` and a collection (`Modifiers.cs` gives it `StringAndCollectionTypes`; every other element-routable modifier is scalar-only). For `notempty`, placement after the inner type is genuinely ambiguous — `set of string notempty` could mean "the set is non-empty" or "each string is non-empty." Precept currently sidesteps this by **not routing `notempty` to element position at all** (`collection-types.md § Element value modifiers` notes it "awaits a disambiguator; use `minlength 1` for a per-element non-empty string").

Two poles are live, and this survey pre-favors neither:

- **(A) The implicit-by-applicability rule may be fine.** Only `notempty` collides, and it is redundant both ways (`mincount 1` for the collection, `minlength 1` for the element). It could simply be disallowed in element position permanently, closing the one ambiguity — and the implicit rule stands.
- **(B) An explicit syntax that visibly scopes a modifier to element vs. collection may be worth the surface cost.**

This is **horizon groundwork**: the owner flagged the clarity concern and authorized the survey to ground a future `/design`. It does not authorize a design; the consuming design pass is the gate for any syntax decision.

## Methodology

- **Research question.** How do schema / constraint / type systems make "this constraint applies to each element" syntactically distinct from "this constraint applies to the aggregate (count/length/uniqueness)"? What is the *visual signal* — nesting, a separate keyword namespace, a delimiter, position, or an explicit "each/item" marker? And given that consensus, what candidate directions exist for Precept, weighed against its principles?
- **Search strategy.** (1) Spec-first grep of the canonical Precept docs — `collection-types.md`, `precept-language-spec.md` §§0.1/0.4/0.8/2.3/2.4, `philosophy.md` — and the `Modifiers.cs` catalog, to confirm the current rule and the exact overlap set. (2) Comparator survey via official documentation of six systems chosen to span the design space: JSON Schema (nesting), CUE (in-bracket), protovalidate (keyword namespace), XSD (separate derived type), Ada (separate component subtype in the type grammar), SQL/PostgreSQL DOMAIN (named separate type lifted onto an array column). Excerpts fetched 2026-06-03 and mirrored to [`research/references/collection-element-scope/source-excerpts.md`](../references/collection-element-scope/source-excerpts.md).
- **Inclusion / exclusion.** Included: systems with a *declarative* element-vs-aggregate distinction expressible in their schema/type surface. Excluded: imperative validation libraries where the distinction is "write a loop over the elements" (no syntactic axis to survey); approximate/probabilistic collection types (out of scope per `collection-types.md § Approximation Stance`); nested-collection cases (Precept forbids collections of collections, so multi-level nesting precedent is not load-bearing here).
- **Source-grade mix.** Primary: JSON Schema docs, CUE tour, W3C XSD Primer, Ada Reference Manual, PostgreSQL docs. Secondary: protovalidate (Buf vendor docs). Tertiary: one PostgreSQL mailing-list example for the `array_length` aggregate idiom (flagged inline). Internal: Precept catalog/spec reads (`purely-internal` for the Precept-state half; the comparator half is `strong`).
- **Time bounds.** Comparator fetches 2026-06-03. Precept reads against the `spike/Precept-V2-Radical` working tree on the same date.

### Spec-first check (Step 1b)

The canonical docs **describe** the implicit-by-applicability rule and **flag** the `notempty` gap as open — they do **not lock** an answer to "should the axis be explicit." `collection-types.md § Element value modifiers` states the rule and parenthetically defers `notempty` ("awaits a disambiguator"); `precept-language-spec.md` §2.4 carries a per-element consultation note. No locked Decision block or `## Alternatives rejected` entry settles the element-vs-collection *syntax* question. The closest locked decision is the `capacity`-modifier rejection (`collection-types.md § Rejected: Bounded Collection`), which `collection-types.md` explicitly distinguishes ("does not apply here: there is no existing spelling for a per-element value bound"). So the question is genuinely open — the survey is not re-litigating a lock.

## Findings

### The comparator axis: six mechanisms, one consensus

Every surveyed system makes the element-vs-aggregate axis **explicit and structural** — none resolves it by "infer from which keyword applies to which type." The *mechanism* varies along one dimension: how far the element constraint is **physically separated** from the aggregate constraint.

| System | Element constraint | Aggregate (count/length/uniqueness) constraint | Visual signal for the axis | Grade |
|---|---|---|---|---|
| **JSON Schema** (2020-12) | nested under `items: { … }` | top-level `minItems` / `maxItems` / `uniqueItems` | **Nesting** — element rules live inside the `items` sub-schema; aggregate rules are siblings of `type: array` | Primary |
| **CUE** | inside the brackets: `[...int]`, `[...>0]`, `[...T & c]` | open/closed list shape (`...` presence) | **Delimiter / position** — element constraint is literally between `[` and `]` | Primary |
| **protovalidate** | `repeated.items.<type> = { … }` | `repeated.min_items` / `max_items` / `unique` | **Keyword namespace** — `.items.` segment vs sibling `.min_items` on the same `repeated` rule | Secondary |
| **XSD** | a derived `simpleType` (`restriction` + facets) used as the element type | `minOccurs` / `maxOccurs` on the element particle | **Separate named/derived type** — facets attach to the type, occurrence to the particle | Primary |
| **Ada** | the **component subtype** in `array (index) of component_definition` | the **index constraint** (index range = bounds = count) | **Separate slot in the type grammar** — component subtype vs index range are different productions | Primary |
| **SQL / PostgreSQL** | a named `DOMAIN` (`CHECK (VALUE …)`) used as the array element type | `array_length(value, …)` in a CHECK on the array | **Named separate type** — element rule is lifted into a reusable domain | Primary (domain) / Tertiary (length idiom) |

#### JSON Schema — nesting (Primary)

Element constraints are nested inside `items`; aggregate constraints are top-level keywords:

> "The length of the array can be specified using the `minItems` and `maxItems` keywords."
> — *Understanding JSON Schema — Arrays*, Draft 2020-12 (accessed 2026-06-03)

`items: { "type": "number", "maximum": 100 }` bounds each element; `maxItems: 5` bounds the count. The reader sees the nesting depth: inside `items` ⇒ element; beside `type: array` ⇒ aggregate. **No applicability inference needed** — `maximum` placed at top level would bound nothing meaningful, but the reader is never asked to reason about that; the nesting tells them.

#### CUE — in-bracket position (Primary)

> "Open lists may contain some predefined elements, followed by `...` and an optional value that constrains any elements that follow."
> — *CUE Tour — Lists* (accessed 2026-06-03)

`[...>0]` constrains every element to be positive; the constraint is *physically inside* the list brackets. Cardinality is a different mechanism entirely (closed vs open list). The bracket is the axis signal.

#### protovalidate — keyword namespace (Secondary)

> "Use `items` to apply validation rules to each element in a repeated field"
> — *protovalidate — Standard rules* (accessed 2026-06-03)

```protobuf
repeated string tags = 2 [(buf.validate.field).repeated.items.string = { min_len: 1, max_len: 50 }];
repeated string members = 1 [(buf.validate.field).repeated.min_items = 1];
```

Note `min_len`/`max_len` live under `.items.string` (element) while `min_items` is a sibling on `repeated` (aggregate). The naming even disambiguates the near-homonyms: `min_len` (element string length) vs `min_items` (aggregate count). The `.items.` path segment is the axis signal.

#### XSD — separate derived type (Primary)

> "We use the restriction element to indicate the existing (base) type, and to identify the 'facets' that constrain the range of values."
> "The maximum number of times an element may appear is determined by the value of a maxOccurs attribute in its declaration."
> — *XML Schema Part 0: Primer* (accessed 2026-06-03)

Facets (`maxLength`, `minInclusive`) attach to a `simpleType`; occurrence (`minOccurs`/`maxOccurs`) attaches to the element particle. The element value rule and the cardinality rule are on *different XML constructs entirely*. The axis is signalled by which construct carries the constraint.

#### Ada — separate slot in the type grammar (Primary)

> "All components of an array have the same subtype."
> — *Ada Reference Manual, 3.6 Array Types* (accessed 2026-06-03)

`array (Index_Range) of Component_Subtype` — the component subtype (with its own constraint, e.g. `Natural` or a range-constrained subtype) occupies a *distinct grammar slot* from the index range (which bounds count). This is the closest structural analogue to Precept's `<collection> of <inner-type> <modifier>` shape: Ada keeps the per-element type and the cardinality in separate, unambiguous positions of the same one-line type expression.

#### SQL / PostgreSQL — named separate type (Primary)

> "It should use the key word VALUE to refer to the value being tested."
> "The underlying data type of the domain … can include array specifiers."
> — *PostgreSQL 18 — CREATE DOMAIN* (accessed 2026-06-03)

The idiom: `CREATE DOMAIN positive_int AS int CHECK (VALUE > 0)`, then a column `scores positive_int[]`. The per-element rule is lifted into a *named* type; the array column references it. Aggregate length checks use `array_length(value, …)` in a separate CHECK (mailing-list idiom, Tertiary). Same family as XSD: name the element type separately, then aggregate over it.

### What the consensus is — and what it is not

**The consensus is that the axis is made explicit by structural separation, not by applicability inference.** Across all six, the reader never has to know "which type does this keyword apply to" to determine scope — the *position* (nesting / brackets / grammar slot), the *keyword namespace* (`.items.`), or a *separate named type* tells them. Three sub-families:

1. **Position/nesting** (JSON Schema, CUE) — element constraint is physically inside an element-scoped container.
2. **Keyword namespace** (protovalidate) — an `items`/`element` segment in the path.
3. **Separate type** (XSD, Ada, SQL DOMAIN) — the element type is named/derived independently, then the collection is built over it.

**What the consensus is *not*:** none of these systems faces Precept's *exact* problem, because none writes the element modifier as a bare trailing token on the same line as the aggregate modifier with no delimiter. Precept's `queue of string maxlength 200 maxcount 50` (if both were written) is a flat token stream where only applicability separates `maxlength` (element) from `maxcount` (aggregate). The comparators have *already* separated the two structurally, so the ambiguity Precept has cannot arise in them. This is the core finding: **Precept's implicit rule is an outlier; the field's answer is structural separation.** Whether that outlier is a *problem* depends on Precept's own principles, addressed next.

## Implications for Precept

The comparator evidence says the axis is *normally* explicit. But Precept's principles cut both ways, so the evidence does not by itself decide:

- **Keyword-anchored readability (§0.1 Principle 5, §0.8).** Precept is explicitly keyword-anchored and optimizes for a domain expert reading top-to-bottom in domain vocabulary, *not* for developer terseness. An axis the reader must *infer from type knowledge* is in tension with "the author reads top-to-bottom in domain vocabulary" — the reader of `queue of string maxlength 200` cannot tell the scope from vocabulary alone. This leans toward (B). **But** §0.8 also warns that designs are judged by whether they *serve the domain expert*, and an extra scoping keyword/marker is more surface to learn.
- **Small, precise surface (§0.4, and the `capacity` / multimap / sortedset rejections in `collection-types.md`).** Precept has a documented pattern of *rejecting* redundant surface. An explicit element/collection marker is new surface; the bar is "does it earn its keep." This leans toward (A) if the only real collision (`notempty`) can be closed cheaply.
- **The inner type already carries attached metadata.** `~string`, `money in 'USD'`, `quantity of 'length'`, `choice of T(...) ordered` all bind metadata to the *inner* type, and the parser already binds them to the inner scalar (`collection-types.md`: "When `in 'CurrencyCode'` … appears after the inner type … it binds to the inner type, not the collection"). There is established precedent that *trailing tokens after the inner type bind to the inner type*. This is relevant to every candidate: a per-element modifier binding to the inner type is *consistent* with how qualifiers already bind — which actually *supports* the implicit rule's mental model ("everything after the inner type decorates the inner type"), and reframes the collection-level modifiers (`mincount`/`maxcount`/`notempty`) as the *exceptions* that reach back out to the collection.

That reframing is the crux: under the current grammar, **most trailing tokens decorate the inner type; a few reach back to the collection**. The question is whether the few that reach back are visible enough.

## Candidate directions for Precept

Five candidates, neutrally. Each notes its disambiguation mechanism, comparator precedent, principle tradeoffs, and whether it forces migrating the shipped `queue of string maxlength 200` syntax (a real cost — solo/pre-release, but samples and any authored precepts would churn).

### Candidate 1 — Do-nothing-structural: keep implicit-by-applicability, permanently disallow `notempty` in element position

**Sketch.** No grammar change. `queue of string maxlength 200` stays. `notempty` is never element-routable; `set of string notempty` always means the *collection* is non-empty. Authors who want per-element non-empty write `minlength 1`. The applicability rule (catalog-derived `ElementPositionValueTokens`) remains the disambiguator.

**Disambiguation mechanism.** Modifier applicability (scalar-only ⇒ element; collection-applicable ⇒ collection). The one genuine collision is removed by fiat.

**Comparator precedent.** None of the six does this — but the collision-removal *is* well-grounded: `notempty` on a string element is exactly `minlength 1`, and on a collection exactly `mincount 1`, so disallowing the ambiguous spelling loses no expressiveness (both meanings remain reachable through an unambiguous spelling). protovalidate's naming (`min_len` vs `min_items`) is the precedent for *relying on distinct vocabulary per axis* — Precept already has distinct vocabulary (`maxlength` vs `maxcount`), it just lacks it for `notempty`.

**Principle tradeoffs.** Smallest surface (Principle: small/precise) — wins decisively here. The cost is the readability tension: scope is still invisible to a reader who does not know the modifier vocabulary (§0.8 friction). Mitigations short of new syntax: LS hover already can state "constrains each element"; a diagnostic could *name the scope* in its message; documentation can teach the rule. Whether those suffice is the open judgment.

**Forces migration?** No. This is the only candidate with zero migration cost.

### Candidate 2 — Explicit per-element keyword marker (`each` / `element`)

**Sketch (illustrative, not locked).** A scope marker before the element modifier: `queue of string each maxlength 200`, or `queue of (string maxlength 200)` style grouping (see Candidate 4). E.g. `set of integer each min 0 max 100`. Collection modifiers stay bare: `set of integer each min 0 max 100 maxcount 50`.

**Disambiguation mechanism.** Explicit marker word. `each`-prefixed ⇒ element; bare ⇒ collection.

**Comparator precedent.** protovalidate's `.items.` namespace; conceptually the "explicit each/item marker" family. Precept *already reserves `each`* as a quantifier keyword (`rule each x in C (…)` — `collection-types.md § Quantifier Predicates`), so the word is in the language and already means "ranges over every element" — strong internal consistency. That dual use is also a risk (lexer/parser disambiguation of `each` in type position vs predicate position).

**Principle tradeoffs.** Strongly serves §0.8 readability — `each maxlength 200` reads in domain vocabulary as "each element, at most 200." Reuses an existing keyword (no new vocabulary to learn if the author knows quantifiers). Costs: new surface in type position; parser must disambiguate `each` (precedent exists — `set` is already dual-use as type-vs-action, per `collection-types.md § set disambiguation`). Resolves `notempty` cleanly: `log of string each notempty` (element) vs `log of string notempty` (collection).

**Forces migration?** Yes — `queue of string maxlength 200` would become `queue of string each maxlength 200` (or the marker could be *optional* with the implicit rule as fallback, avoiding migration but reintroducing the invisibility for unmarked cases — a hybrid worth weighing in design).

### Candidate 3 — Grouping delimiter on the inner type (`queue of (string maxlength 200)`)

**Sketch (illustrative).** Parenthesize the inner type with its element modifiers: `queue of (string maxlength 200)`, `set of (integer min 0 max 100)`. Anything outside the parens decorates the collection: `set of (integer min 0) maxcount 50`.

**Disambiguation mechanism.** Delimiter (parentheses) — inside ⇒ element; outside ⇒ collection. Directly mirrors CUE's "inside the brackets" and JSON Schema's nesting.

**Comparator precedent.** Strongest precedent of any candidate — CUE (`[...>0]`), JSON Schema (`items: {…}`), and the general "element constraint is physically contained" family. Also internally consistent: Precept already uses `(…)` for `choice of string("low","high")` value lists and quantifier predicates.

**Principle tradeoffs.** Unambiguous and well-precedented. Cost: parentheses are *symbol-heavy*, which §0.8 Principle 1 flags as a deliberate exclusion ("Symbol-heavy or expression-first grammars are deliberate exclusions"). A domain expert reading `set of (integer min 0 max 100)` meets a delimiter, not a keyword — mild friction against the keyword-anchored ideal. Resolves `notempty`: `log of (string notempty)` (element) vs `log of string notempty` (collection).

**Forces migration?** Yes — every shipped element-modifier declaration gains parens. (Could be made optional/back-compatible, but optionality reintroduces the unmarked-invisibility case.)

### Candidate 4 — Collection-side explicit keyword instead (mark the *aggregate*, leave element bare)

**Sketch (illustrative).** Invert: element modifiers stay bare (decorating the inner type, consistent with `~string`/`money in 'USD'` precedent), and the *collection-level* count modifiers get a visible anchor that reads as collection-scoped. Precept already half-does this — `maxcount`/`mincount` literally contain "count," naming the aggregate. Extend by ensuring *every* collection-scoped modifier carries an aggregate-naming form, and disallow the bare-ambiguous `notempty` (route the collection meaning to `mincount 1` or a `notempty` that is *only* collection-scoped, with element non-empty via `minlength 1`).

**Disambiguation mechanism.** Vocabulary asymmetry — aggregate modifiers self-name as aggregate (`maxcount`, `mincount`); element modifiers self-name by being scalar-type vocabulary (`maxlength`, `min`). This is Candidate 1 plus a *naming discipline* making the asymmetry a deliberate, documented design rule rather than an accident of applicability.

**Comparator precedent.** protovalidate (`min_len` element vs `min_items` aggregate — the vocabulary itself encodes the axis). XSD facet-name vs `maxOccurs`. This is the "distinct vocabulary per axis" family.

**Principle tradeoffs.** Near-zero new surface (mostly a documented framing + the `notempty` fix). Serves §0.8 better than Candidate 1 *if* the vocabulary asymmetry is taught as a rule ("count-words scope the collection; value-words scope each element"). Weaker than Candidates 2/3 for a reader who does not yet know the vocabulary — the signal is in the *word choice*, not a structural marker. Resolves `notempty` the same way as Candidate 1 (it is the word that lacks an axis-encoding form).

**Forces migration?** No, beyond the `notempty` element-position closure (which is not yet shipped, so no migration).

### Candidate 5 — Element type as a named separate declaration (XSD / SQL-DOMAIN style)

**Sketch (illustrative).** Allow naming a constrained scalar type once, then using it as an inner type: a hypothetical `type ShortString as string maxlength 200` then `queue of ShortString`. Element constraints live on the named type; the collection declaration is clean.

**Disambiguation mechanism.** Separate named type — the element constraint is not on the collection line at all.

**Comparator precedent.** Strongest external precedent (XSD `simpleType`, SQL `DOMAIN`, Ada component subtype). The dominant "enterprise schema" answer.

**Principle tradeoffs.** Removes the axis ambiguity entirely (element rules are elsewhere) and adds reuse. But it is a **large new language surface** — user-defined named scalar types do not exist in Precept today (`collection-types.md`: "There are no user-defined collection types"; the scalar surface is likewise catalog-fixed). This is a much bigger feature than the axis-clarity problem warrants on its own, and would need its own design/research thread. It also moves the constraint *away* from the point of use, which can hurt the one-file-complete-rules readability for a domain expert scanning a single field. Likely out of scope for *this* problem, included for completeness as the "full separation" pole.

**Forces migration?** No (additive), but it is the heaviest lift and arguably solves a different problem.

### Candidate comparison

| Candidate | Disambiguation | Closest precedent | New surface | Migrates shipped `maxlength 200`? | §0.8 readability | Resolves `notempty` |
|---|---|---|---|---|---|---|
| 1 Do-nothing-structural | applicability + ban `notempty` element | protovalidate vocabulary | none | No | weakest | by ban |
| 2 `each` marker | explicit keyword | protovalidate `.items.` / "each" family | one (reused keyword) | Yes (or optional) | strongest | `each notempty` |
| 3 grouping `( )` | delimiter | CUE / JSON Schema nesting | symbol (mild §0.1-1 tension) | Yes (or optional) | strong but symbol | `(string notempty)` |
| 4 vocabulary asymmetry | self-naming words | protovalidate `min_len`/`min_items` | near-none + framing | No | medium | by ban + framing |
| 5 named element type | separate type | XSD / SQL DOMAIN / Ada | large (new feature) | No (additive) | high but off-site | element rule elsewhere |

## Conclusions

This research is **horizon groundwork** feeding a future `/design`; it proposes **no locked conclusion**. The four-leg rationale is deferred to the consuming design pass. One **clearly-flagged tentative lean**, offered as a starting frame, not a decision:

- **Tentative lean (flagged, not locked):** The comparator consensus (explicit structural separation) is strong, but it is *answering a problem Precept partly designed around* — Precept's vocabulary already separates most axes (`maxlength` vs `maxcount`), and the inner-type-decoration precedent (`~string`, `money in 'USD'`) gives "trailing tokens decorate the inner type" a coherent mental model. The *acute* problem is narrow: exactly one modifier (`notempty`) genuinely collides, and it is redundant both ways. That makes **Candidate 1 / Candidate 4** (close `notempty`, lean on vocabulary asymmetry, invest in LS-hover/diagnostic scope-naming) the cheapest *structurally-complete* fix — the ambiguity goes away. The *residual* concern is pure readability for a reader who does not know the vocabulary, which is real under §0.8 but is the kind of friction tooling (hover, diagnostics) and docs can carry without new grammar. **Candidate 2 (`each`)** is the strongest *if the design pass concludes the readability friction warrants visible surface* — it reuses an existing keyword and reads in domain vocabulary. The honest tension: (A)-family wins on small-surface, (B)/Candidate-2 wins on read-without-prior-knowledge. The design pass should decide which principle dominates *for this specific axis*, with the owner — not this survey.

## What would change this conclusion

- **If authored precepts / samples turn out to use element modifiers heavily and ambiguously** (e.g., real confusion observed about whether `maxlength` scopes the element), the readability cost rises and the lean shifts toward Candidate 2/3.
- **If a second modifier joins the both-scalar-and-collection overlap set** (today only `notempty`), the "only one collides" premise behind Candidate 1/4 weakens, and an explicit marker becomes more justified. (Watch the catalog: any new modifier with both a scalar-type target and a collection target in `Modifiers.cs` reopens this.)
- **If a domain expert in usability testing cannot correctly state the scope** of `queue of string maxlength 200` after brief exposure, the "vocabulary + tooling carries it" assumption (Candidate 1/4) is falsified and the structural-marker candidates strengthen.
- **If user-defined named scalar types get designed for an unrelated reason**, Candidate 5 becomes nearly free and changes the calculus.

## Open Questions

- Is the `each` keyword's dual role (quantifier predicate vs type-position element marker) cleanly disambiguable in the parser, and does the reuse *help* (one concept) or *confuse* (two grammar contexts)? — a design/parser question.
- Could a *hybrid* (implicit rule stays the default; an *optional* explicit marker available for emphasis) get the best of both — zero migration, opt-in clarity — or does optionality just preserve the invisibility for the unmarked majority? Weigh in design.
- Does the LS-hover / diagnostic "name the scope" mitigation actually reach the domain-expert author, or only the developer reading in an IDE? (Bears on whether tooling can carry the readability cost under §0.8.)
- For `lookup of K to V`, where do element modifiers even attach — key, value, or both? The survey focused on single-inner-type collections; two-parameter kinds (`lookup`, `queue of T by P`) multiply the axis question and were not resolved here.

## Sources

- **Understanding JSON Schema — Arrays** (JSON Schema project). Draft 2020-12. <https://json-schema.org/understanding-json-schema/reference/array>. Primary. Accessed 2026-06-03. Mirrored: `research/references/collection-element-scope/source-excerpts.md`.
- **CUE Tour — Lists** (CUE project). <https://cuelang.org/docs/tour/types/lists/>. Primary. Accessed 2026-06-03. Mirrored.
- **protovalidate — Standard rules** (Buf). <https://protovalidate.com/schemas/standard-rules/>. Secondary (vendor). Accessed 2026-06-03. Mirrored.
- **XML Schema Part 0: Primer** (W3C Recommendation). <https://www.w3.org/TR/xmlschema-0/>. Primary. Accessed 2026-06-03. Mirrored.
- **Ada Reference Manual, 3.6 Array Types** (ISO/IEC 8652; community mirror). <https://ada-lang.io/docs/arm/AA-3/AA-3.6/>. Primary. Accessed 2026-06-03. Mirrored.
- **PostgreSQL 18 — CREATE DOMAIN** and **§8.15 Arrays** (PostgreSQL Global Development Group). <https://www.postgresql.org/docs/current/sql-createdomain.html>, <https://www.postgresql.org/docs/current/arrays.html>. Primary. Accessed 2026-06-03. Mirrored.
- **PostgreSQL pgsql-bugs list** (array_length-in-domain idiom). <https://www.postgresql.org/message-id/4AFC5BBC.90202@phlo.org>. Tertiary (mailing list). Accessed 2026-06-03.

### Internal sources (Precept canonical)

- `docs/language/collection-types.md` — § Element value modifiers, § Constraint Catalog, § Inner Type System, § set disambiguation, § Quantifier Predicates, § Rejected: Bounded Collection.
- `docs/language/precept-language-spec.md` — §0.1 Principle 5 (keyword-anchored readability), §0.4, §0.8 (authoring audience), §2.3 (CollectionType grammar), §2.4 (modifier↔type table + per-element note).
- `docs/philosophy.md` — § Who authors a precept (domain-expert primary author).
- `src/Precept/Language/Modifiers.cs` — `notempty` → `StringAndCollectionTypes` (sole overlap); `ElementPositionValueTokens` derivation (scalar-only ⇒ element-routable).

## Threats to Validity

- **Domain mismatch (the central threat).** None of the six comparators writes the element modifier as a *bare trailing token on the collection's own line* the way Precept does (`queue of string maxlength 200`). They all separated the axis structurally *before* the ambiguity could arise, so they are evidence for "how to make the axis explicit," not direct evidence that Precept's specific flat-token form is *confusing in practice*. The "is it actually a problem" half rests on §0.8 principle reasoning, not on observed user confusion — flagged as the weakest link, addressed in the falsifiers.
- **Selection bias.** Six comparators were chosen to span the mechanism space (nesting / delimiter / namespace / separate-type), not enumerated exhaustively. Other systems (Zod `.array()` element schema vs `.min()` length; TypeScript tuple-length vs element type; OpenAPI = JSON Schema) were judged to land in the same three families and were not separately surveyed. If a system uses a *seventh* mechanism, this would be missed.
- **Source grade.** One aggregate idiom (PostgreSQL `array_length`-in-domain) rests on a mailing-list post (Tertiary); it is illustrative, not load-bearing — the load-bearing SQL claim (DOMAIN as named per-value type) is Primary.
- **Recency.** All comparator docs fetched 2026-06-03; JSON Schema (2020-12) and XSD are stable standards; CUE and protovalidate are evolving and may have changed syntax since fetch. Re-verify before any design lock.
- **Internal-read currency.** Precept reads are against the `spike/Precept-V2-Radical` branch; `Modifiers.cs` `ElementPositionValueTokens` and the `notempty` overlap were read directly, so the "only `notempty` collides" claim is grounded in current source, not inference.
