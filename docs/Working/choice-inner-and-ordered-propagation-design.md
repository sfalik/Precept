---
status: Locked 2026-05-26 (owner ratifications: D-1 single source of truth — `Ordered` moves to `ChoiceTypeReference.Ordered`; `ModifierKind.Ordered` removed from field's modifier list; `ProofEngine.Strategies.cs:58` TryDeclarationAttributeProof extended to consult type-ref Ordered for choice-typed params (~10 LOC). D-2 structural record name = `TypedElementType` (DU base, `TypedScalarElement` / `TypedChoiceElement(bool Ordered)` / `TypedQualifiedElement(...)`) lives in `SemanticIndex.cs` near other `Typed*` records. D-3 confirmed structural carrier path. D-4 PULLED BUG-012 INTO PHASE 4 (one slice with COLL-02/03, no transition window in solo-dev context). D-6 (unordered choice-element collections) sanctioned: encourage as sample-corpus pattern; one canonical sample exercises it.)
feature-gates: F-LANG-COLL-02, F-LANG-COLL-03
related-bugs: BUG-012
target-phase: Phase 4 (collection completeness) — with explicit dependency on Phase 5 for the BUG-012 fix
---

# Choice inner types in collections + ordered-choice trait propagation

## 1. Goal

Make `(set|list|bag|queue|stack|log) of choice of T(...) [ordered]` and `lookup of K to choice of T(...) [ordered]` first-class, parseable collection types. When a collection's element type is an ordered choice, the `ordered` declaration must flow through collection element accessors (`.first`, `.last`, `.at(N)`, `.min`, `.max`, `.peek`, `.peekby`) so that the result type carries the ordering capability — i.e. `Tiers.first <= "Medium"` type-checks with the same correctness story as a bare `Tier <= "Medium"` against an ordered-choice field.

The two feature gates are tightly coupled: shipping F-LANG-COLL-02 (parser) without F-LANG-COLL-03 (trait propagation) produces a documented type that users cannot exercise — every ordered comparison on an accessor result emits `PRE0112 UnprovedModifierRequirement`. They are designed as a single language slice for that reason, even though the parser change and the accessor-result-type change are physically separable.

## 2. Scope

**In scope.**

- Parser: `ParseInnerTypeReference` accepts `choice` as an inner-type lead and dispatches to `ParseChoiceType`. This applies symmetrically to (a) `set|list|bag|queue|stack|log of choice of T(...)`, (b) `log of T by choice of K(...)` and `queue of T by choice of K(...)` ordering-key position (the spec already documents this latter form at `collection-types.md:1070`), and (c) `lookup of choice of K(...) to V` and `lookup of K to choice of V(...)`.
- AST: `CollectionTypeReference.ElementType` and `CollectionTypeReference.KeyType` already accept any `ParsedTypeReference`, so a `ChoiceTypeReference` lives there directly. No new DU subtype is needed.
- Ordered-on-inner-choice carrier: introduce an `Ordered` flag (or equivalent) on `ChoiceTypeReference` so the inner type can record `ordered` *as a type-level modifier on the choice*, not as a field-level modifier. Today `ordered` is field-attached via `Modifiers.cs:71-75` with `ChoiceOnly` applicability — that mechanism stays for bare ordered-choice fields (back-compat), but the parser additionally recognises a trailing `ordered` keyword inside `ParseChoiceType` and records it on the `ChoiceTypeReference`.
- TypeChecker / SemanticIndex: `TypedField.ElementType` and `TypedField.KeyType` are bare `TypeKind` today (`SemanticIndex.cs:307-308`). When the element/key type is `Choice`, we need to carry forward (i) the choice domain, (ii) the element-of-choice type (`integer` / `string` / `decimal` / `number` / `boolean`), and (iii) whether the choice is ordered. Two structural shapes are evaluated in Decision D-2; the chosen one becomes the implementation contract.
- Accessor return-type resolution: `ResolveAccessorReturnType` (`TypeChecker.Expressions.Callables.cs:1067-1083`) returns a bare `TypeKind` today. The base `TypeAccessor` arm (`.first`, `.last`, `.at(N)`, `.min`, `.max`, `.peek`) returns the collection's element type — when that element is an ordered choice, the resulting `TypedMemberAccess` must carry enough metadata that downstream operator proof obligations can see the `Ordered` modifier without walking back to the collection field declaration.
- PRE0104 (`RequiredTraitViolation`) check at `TypeChecker.Expressions.Callables.cs:952-967`: extend so that `.min`/`.max` on a collection of `Choice` element type checks `Ordered` presence at the element level (today the check looks up `Types.GetMeta(elementType.Value).Traits`, which for `TypeKind.Choice` cannot encode "ordered or not" — the metadata is per-kind, not per-instance).
- Diagnostics: review `CollectionInnerTypeError` (PRE0105) wording so the existing `set of choice of ...` rejection message is replaced by a positive parse path, and so the *new* failure mode — `set of choice of T(...)` *without* `ordered`, then calling `.min`/`.max` — emits `RequiredTraitViolation` with a clear "this choice needs `ordered` to compare" message rather than the current generic "trait violation."
- Docs: `collection-types.md:83` ScalarType note ("simple scalar… or `~string`") is updated to permit `choice of T(...)`; the `collection-types.md:489` v1-limit clause is deleted; `collection-types.md:146` (which already documents `RiskLevels.min`/`.max` semantics) becomes accurate rather than aspirational; `precept-language-spec.md:1707` v1-limit clause is deleted; `precept-language-spec.md:1071-1077` `CollectionType` grammar is updated to refer to a `ScalarOrChoiceType` non-terminal (or equivalent).
- Tests: positive parse tests for every collection-kind × choice-inner-type combination; positive `.min`/`.max` test on an ordered-choice element; negative `.min`/`.max` test on an unordered-choice element (still emits `RequiredTraitViolation`); positive ordinal comparison `Coll.first <= "Medium"` once F-LANG-COLL-03 lands — *if* BUG-012 has not yet shipped, the comparison test will need to use field-vs-field rather than field-vs-literal (literal-side ordered inference is BUG-012's territory).
- Samples: add at least one canonical sample using `set of choice of T(...) ordered` once the feature ships (Wave 3); a candidate is the `it-helpdesk-ticket.precept` Priority-tier rollup or `medical-prior-auth.precept`'s `UrgencyLevel` history.

**Out of scope (deferred).**

- Nested collections of collections (`list of list of T`). Independent v1 limit; not unblocked by this work.
- Qualified inner types (`set of money in 'USD'`, `set of quantity of 'length'`). Tracked separately as **F-LANG-COLL-06**. The plumbing this design adds for "non-trivial inner type" partially overlaps with what F-LANG-COLL-06 needs (passing a `ParsedTypeReference` through, carrying inner-type metadata to the proof engine), so the two designs should be implementation-cross-referenced — but the language-surface decisions are independent.
- Choice-domain *subset* relationships (`set of choice of string("A","B")` assigned to a field declared as `set of choice of string("A","B","C")`). Choice equivalence/subsetting rules are inherited from the existing scalar-choice path; this design does not change them.
- BUG-012's ordered-literal proof gap. This design produces the *type-level* propagation; BUG-012 fixes the *literal-side* inference inside the proof engine. The two ship in different phases (this in Phase 4, BUG-012 in Phase 5) — see § 6 Dependencies and § 10 Phase Targeting.

**Explicitly NOT changing.**

- The field-level `ordered` modifier semantics (`Modifiers.cs:71-75`) for bare `choice of T(...) ordered` fields stay exactly as they are. We're additively recognising `ordered` in a new lexical position (inside the inner-type slot of a collection); the old position is untouched.
- The bare `ChoiceElement` trait gate. No change to which element types may appear inside a choice (still the five scalars in `ChoiceLiteralTokens`).

## 3. Inventory of what will be built

### Parser (`src/Precept/Pipeline/Parser.Types.cs`)

- **`ParseInnerTypeReference` (lines 238-265)** — add a leading-token check for `TokenKind.Choice` *before* the simple-type lookup; on match, call into `ParseChoiceType` and return the resulting `ChoiceTypeReference`. The `ParseChoiceType` method itself stays as-is structurally but gains a trailing-`ordered` consumption (see next bullet).
- **`ParseChoiceType` (lines 86-144)** — after the closing `)`, if `Peek().Kind == TokenKind.Ordered`, consume it and set the new `Ordered` flag on `ChoiceTypeReference`. Currently this only happens at the field-modifier scope; pulling it into `ParseChoiceType` makes the modifier scope-uniform: a choice carries `ordered` wherever it's spelled, including in collection inner-type slots.
- **`ParseCollectionType` (lines 146-232)** — no signature change. The existing `ElementType = ParseInnerTypeReference()` and `keyType = ParseInnerTypeReference()` call sites now transparently accept choices because `ParseInnerTypeReference` does.
- **Field-modifier path** — when a field is declared as `field F as choice of T(...) ordered`, the `ordered` token is now ambiguous between "trailing on the choice type" (consumed by `ParseChoiceType`) and "field-level modifier" (consumed by `ParseFieldModifierNodes`). The natural disambiguation: `ParseChoiceType` already consumes everything up through the closing `)`; whichever scope sees the `ordered` token first wins. Choose: **`ParseChoiceType` consumes the `ordered` token** even at field scope. Downstream effect — the field's `Modifiers` list no longer contains `Ordered` for ordered-choice fields; instead the *type reference* carries it. Existing field-scope code that asks "is this field ordered?" (proof engine, accessors) must migrate to consult the type reference. This is a structural simplification, not a regression — it makes the `ordered` semantic uniform across field-scope and collection-inner-scope.

### AST (`src/Precept/Pipeline/ParsedTypeReference.cs`)

- **`ChoiceTypeReference`** — add `bool Ordered` to the record. Default `false`. The parser sets it from the trailing-`ordered` consumption above.

### TypeChecker (`src/Precept/Pipeline/TypeChecker.cs`)

- **`ResolveTypeKind` (lines 90-103)** — already returns `(Type, ElementType, KeyType)` as bare `TypeKind`. The tuple is insufficient to carry choice-inner metadata. Two paths:
  - **D-2 Option A:** Replace `TypeKind?` with a small structural record `ElementTypeRef(TypeKind Kind, ChoiceMeta? Choice)` where `ChoiceMeta` carries `(ImmutableArray<string> Domain, TypeKind ElementOfChoice, bool Ordered)`. Threaded through `TypedField.ElementType` and `TypedField.KeyType`.
  - **D-2 Option B:** Keep the bare `TypeKind` tuple; store the choice element metadata on `TypedField` directly as `Dictionary<string, ChoiceMeta> ElementChoiceMetadata` on `CheckContext`, with the field name as key (mirrors `CIElementCollections`). Accessor resolution looks up the field, then the metadata.
  - **Recommend Option A** — it makes the metadata flow through `TypedExpression` correctly (necessary for nested accessors and for projections through quantifiers); Option B can only answer "is this field's element ordered?" via a field-name lookup, which fails for list-literal collections and for accessor chains. See Decision D-2.

### TypedExpression carrier (`src/Precept/Pipeline/SemanticIndex.cs`)

- **`TypedMemberAccess` (lines 75-81)** — today carries `(ResultType, Object, ResolvedAccessor, ProofRequirements, Span)`. The accessor result type is a bare `TypeKind`. For ordered-choice propagation, the result needs to carry whatever choice metadata Option A produces. Two sub-options:
  - **D-3 Option A:** Add `ChoiceMeta? ChoiceMetadata` slot to `TypedMemberAccess` (and to other `TypedExpression` subtypes that can return a choice — `TypedFieldRef`, `TypedConditional`, `TypedQuantifier` binding sites if quantifier elements are choices).
  - **D-3 Option B:** Lift the propagation to obligation-generation: when the proof engine sees a comparison whose receiver is a `TypedMemberAccess` returning `TypeKind.Choice`, walk back to the receiver's collection field, consult its element metadata, and use it to discharge `ModifierRequirement(Ordered)`. No new slot on `TypedMemberAccess`.
  - **Recommend Option A** — Option B repeats the BUG-012 mistake one level up. The proof engine should NOT be walking backward through arbitrary expressions to recover declaration attributes; that's exactly the strategy `DeclarationAttribute` already does for field-vs-field, and it cannot generalise to `(foo.first + 1).last` style chains. Carrying the metadata on the typed expression is the right architectural seam. See Decision D-3.

### Accessor return-type resolution (`src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs`)

- **`ResolveAccessorReturnType` (lines 1067-1083)** — signature changes from returning `TypeKind` to returning `(TypeKind, ChoiceMeta?)`. The base-accessor arm (which returns the element type) extracts choice metadata from the receiver via `GetElementType`, which itself now returns the richer `ElementTypeRef`. `FixedReturnAccessor` and `ElementParameterAccessor` arms return `(f.Returns, null)` and `(TypeKind.Integer, null)` respectively.
- **PRE0104 check (lines 952-967)** — when `accessor.RequiredTraits` includes `Orderable` AND the element type is `Choice`, instead of consulting `Types.GetMeta(Choice).Traits` (which can't encode per-instance ordered-ness), consult the element-level `ChoiceMeta.Ordered`. If false, emit `RequiredTraitViolation` with a sharper message — e.g., `"choice of {ElementOfChoice}(...) is not 'ordered'; add 'ordered' to enable {.min/.max}"`.

### Proof engine (`src/Precept/Pipeline/ProofEngine.Strategies.cs`)

- **`TryDeclarationAttributeProof` Modifier arm (lines 52-59)** — when the obligation subject is a `TypedMemberAccess` whose `ChoiceMetadata.Ordered` is true, satisfy `ModifierRequirement(Ordered)` from the metadata instead of (or in addition to) walking the receiver's field declaration. This is the symmetric fix for accessor results — it's not the same fix as BUG-012 (literal-side inference), but the architecture overlaps. See § 6.

### Docs

- `docs/language/collection-types.md` — strike line 83 ScalarType caveat; strike line 489 v1-limit; line 146 prose stays as-is (it was already aspirational/forward); the table-of-supported-element-types at line 60 inherits the change.
- `docs/language/precept-language-spec.md` — strike line 1707 v1-limit; update `CollectionType` grammar lines 1071-1077 to reference `ScalarOrChoiceType` (or inline-expand to add a `| choice of …` alternative); update `ChoiceType` grammar at line 1079 to allow trailing `ordered` (it already does at field scope; clarify the type-scope analogue).
- `docs/Working/bugs.md` — annotate BUG-012 with a cross-link to this design noting that the accessor-result propagation here is upstream of the literal-side inference there.
- `docs/Working/compiler-readiness-plan-2026-05-24.md` lines 689-690 — flip F-LANG-COLL-02 and F-LANG-COLL-03 from "find/diagnostic" framing to "ship the feature" framing.

### Tests

- `test/Precept.Tests/Parser/ParserTypeTests.cs` (or similar) — parse each of the seven collection kinds (set, queue, stack, bag, list, log, lookup) × `of choice of {string,integer,boolean,decimal,number}(...) [ordered]`. ~35 positive tests.
- `test/Precept.Tests/TypeChecker/CollectionInnerChoiceTests.cs` — `.min`/`.max` on `set of choice of T(...) ordered` types succeeds; `.min`/`.max` on `set of choice of T(...)` (no ordered) emits `RequiredTraitViolation`; `.first`/`.last` on `list of choice of T(...) ordered` returns ordered-choice type (proof obligations for ordinal comparison discharge).
- `test/Precept.Tests/ProofEngine/OrderedChoiceAccessorTests.cs` — `Tiers.first <= Threshold.first` discharges (field-vs-field ordered-choice via accessor). Note: `Tiers.first <= "Medium"` will still fail until BUG-012 ships; add an `[Trait("Phase", "5")]` skip-or-xfail marker with a TODO referencing BUG-012.

## 4. Decisions

### D-1. Ship F-LANG-COLL-02 (choice inner in collections) as a feature

**Stakes.** The doc corpus (`collection-types.md:146`, `precept-language-spec.md:1079`) already documents `set of choice of T(...) ordered` as if it works. Two of the most idiomatic ordinal-rollup use cases (severity tiers, urgency levels) cannot currently express the natural shape. The alternative — telling authors to use bare `set of string` and reconstruct ordering via lookup maps — is a known anti-pattern that's already cited in BUG-012's workaround.

**Rationale.** The semantic story is closed. `choice of T(...)` is already a fully resolved type; collections already accept arbitrary inner-type structure (`CollectionTypeReference.ElementType` is `ParsedTypeReference`, not `SimpleTypeReference`). The parser change is small (lift one `if` block in `ParseInnerTypeReference`); the AST is already shaped for it. The hard work is downstream — accessor propagation (D-3) — and that work is *required regardless* of whether we ship D-1, because the prose at `collection-types.md:146` will keep being a documentation lie until accessors propagate. Shipping D-1 also unlocks the language's intended idiom for tier/rank histories (medical-prior-auth UrgencyLevel timeline, IT helpdesk Severity history).

**Alternatives.**

- **Reject permanently** ("collections never carry parameterised inner types beyond what scalars give you"). Rejected — it contradicts `lookup of K to choice of V(...)` which is already documented and `queue of T by choice of K(...) ordered` which is documented at `collection-types.md:1070-1090`. Both must work for the existing samples to make sense. We're not introducing the capability; we're closing parser gaps to make the existing capability uniformly available.
- **Defer to v2** (ship F-LANG-COLL-02 alone, ship F-LANG-COLL-03 in a later language version). Rejected — without F-LANG-COLL-03, every author who reaches for the feature will hit `PRE0112` on the first `.min`/`.max` call. The MCP server will return diagnostics that look like compiler bugs. Shipping the parse without the trait flow makes the feature actively worse than not shipping it.
- **Defer everything to F-LANG-COLL-06** (qualified inner types). Rejected — that work has its own scope and is itself multi-day. Bundling adds risk; the two feature gates are architecturally orthogonal (qualifiers vs. parameterised choice).

**Precedent.**

- C#: `List<TEnum>` where `TEnum : Enum, IComparable<TEnum>` is the closest analogue — collections of an ordered enum sort by enum's declared `IComparable` semantics; `List.Min`/`Max` work without surprise.
- F#: `Set<Card>` where `type Card = Two | Three | … | Ace` — ordered by declaration; `Set.minElement`/`maxElement` are total when the set is non-empty.
- Swift: `Array<Severity>` where `enum Severity: Int, Comparable` — same pattern; ordinal comparison on collection elements is idiomatic.
- Internal: the existing field-level `choice of T(...) ordered` does exactly this for scalars; we're extending the same idea one level into collections.

**Tradeoff accepted.** The TypeChecker plumbing gets thicker (Option A in D-2 introduces a small structural record where there was previously a `TypeKind`). The maintenance cost is the cost of accurate per-instance type metadata — same cost we're already paying for currency/unit qualifier metadata on `TypedField.DeclaredQualifiers`. Accept it.

**Sources.** `samples/medical-prior-auth.precept:33`, `samples/it-helpdesk-ticket.precept` (refactor cited in BUG-012), `docs/language/collection-types.md:146,489`, `docs/language/precept-language-spec.md:1079,1707`, `docs/Working/bugs.md:54-82`.

### D-2. ElementType/KeyType representation — structural ref vs. side-table

**Stakes.** This is the load-bearing structural decision. Choosing wrong here either (a) blocks accessor propagation cleanly (Option B) or (b) pays a refactor cost on every TypedField call site (Option A). The choice cascades into ten-plus files.

**Rationale (for Option A — structural `ElementTypeRef`).** Choice metadata must flow through accessor results to satisfy F-LANG-COLL-03. Accessor results are `TypedExpression` instances, not field references — they cannot be looked up in a `CheckContext` side-table by name. Once we admit that *some* `TypedExpression` needs to carry choice metadata, the cleanest seam is to make the metadata travel with the expression type wherever choices appear. The structural carrier is the right architectural location; the side-table is a special case that works for fields and fails for everything else.

**Alternatives.**

- **Option B (side-table on `CheckContext`).** Mirrors `CIElementCollections`. Cheaper to land. Fails for: list-literal collections (no field name), accessor chains, quantifier bindings whose binding type is `Choice`, conditional expressions returning a choice. Rejected.
- **Option C (encode ordered-ness on `TypeKind` via a synthetic `ChoiceOrdered` kind distinct from `Choice`).** Tempting but wrong: `TypeKind` is a closed catalog enum, and "ordered" is not a different *kind* of type — it's per-instance metadata on an instance of the choice type. Encoding it on the kind violates the catalog rule against per-member dispatch. Rejected on language-design grounds.

**Precedent.** `TypedField.DeclaredQualifiers` already pays this same architectural cost — `money in 'USD'` is represented structurally, not via a `MoneyUsd` synthetic kind. Currency metadata travels with the type wherever the type goes. Choice metadata follows the same pattern.

**Tradeoff accepted.** ~6-10 file edits cascading from the `TypedField.ElementType` shape change. Mitigation: do this in a vertical slice with the type-checker tests green at every step; keep `ChoiceMeta` a `sealed record` so the migration is mechanical.

**Sources.** `src/Precept/Pipeline/SemanticIndex.cs:307-308`, `src/Precept/Language/Types.cs:265-266` (existing precedent: per-type `ChoiceLiteralTokens` lifted out of the type-checker into the catalog).

### D-3. Accessor return-type carrier — on `TypedExpression` vs. lifted to proof engine

**Stakes.** Determines whether the obligation-generation site or the type-checking site is responsible for knowing that "this expression's value can be ordinally compared." Wrong here means BUG-012-shaped bugs cascade across accessor chains.

**Rationale (for Option A — slot on `TypedMemberAccess` and friends).** Type-checking is the right phase to record type-shape information; the proof engine should consume, not derive, type metadata. Option B requires the proof engine to walk backward through expression trees to recover declaration attributes — a strategy that does NOT generalise (cf. BUG-012 root-cause analysis at `docs/Working/bugs.md:78`). Option A is local: every typed expression knows its full type identity, and the proof engine reads it.

**Alternatives.**

- **Option B (lifted to proof engine — declaration walk).** Rejected — see § 6 dependency analysis below. This is *literally* what BUG-012 documents as the wrong strategy.
- **Option C (compute on demand via a `TypedExpression.GetChoiceMetadata()` method on the base).** Equivalent to A in semantics; the implementation difference is method vs. slot. Slot is cheaper at point of use (no traversal); method is cheaper at point of construction (no extra parameter on every typed-expression record). Recommend slot because typed expressions are constructed once and consulted many times; precedent: `TypedField.DeclaredQualifiers` is a slot, not a method.

**Precedent.** `TypedField.DeclaredQualifiers` (slot), `TypedMemberAccess.ProofRequirements` (slot), `TypedTypedConstant.DeclaredQualifiers` (slot). Pattern is established.

**Tradeoff accepted.** Slot count on `TypedMemberAccess` grows by one. Acceptable — five slots → six is well below the per-record sanity threshold.

**Sources.** `src/Precept/Pipeline/SemanticIndex.cs:75-81`, `docs/Working/bugs.md:78` (BUG-012 root-cause analysis).

### D-4. Relationship to BUG-012 — ship in different phases or co-ship?

**Stakes.** Co-shipping would couple Phase 4 (collections) to Phase 5 (proof engine extensions). Separating creates a transient window where F-LANG-COLL-03 ships, accessor-result propagation works for field-vs-field comparisons, but accessor-result-vs-literal still fails until BUG-012 ships.

**Rationale (for: ship F-LANG-COLL-02/03 in Phase 4, BUG-012 in Phase 5 — separated).** F-LANG-COLL-02/03 is a language-surface feature; it belongs with the rest of Phase 4's collection-completeness work. BUG-012 is a proof-engine bug — it has the same shape as BUG-001 (rule body narrowing) and BUG-004 (event ensure narrowing), both of which were sized as small/medium proof-engine fixes. The bugs.md root-cause analysis already names the fix shape: add a typed-literal strategy to the proof engine that infers `Ordered` from the operand's expected type. That's a Phase 5 satisfiability-extensions concern (Phase 5's goal already covers literal-side inference patterns), and it's complementary to — not blocking on — F-LANG-COLL-02/03.

The transient window is small and well-defined: between Phase 4 ship and Phase 5 ship, `set of choice of T(...) ordered` users can do `Coll.first <= OtherColl.first` (field-vs-field via accessor) and `Coll.first <= OrderedChoiceField` (accessor-vs-field) but NOT `Coll.first <= "Medium"` (accessor-vs-literal). The exact same restriction is the documented BUG-012 reality today; the window doesn't make anything worse, it just makes the literal-side restriction visible at one more comparison site. Tests should mark literal-side comparisons as `[Trait("Phase","5")]` xfail.

**Alternatives.**

- **Co-ship (move BUG-012 to Phase 4).** Rejected — Phase 4 is already L-sized (~1-1.5 weeks per the plan). Adding a proof-engine fix that lives architecturally in `ProofEngine.Strategies.cs` would conflate two architectural seams that the phase structure deliberately keeps separate.
- **Defer F-LANG-COLL-02/03 to Phase 5.** Rejected — it leaves Phase 4 with a documented collection feature (per `collection-types.md:146`) unshipped, undermining the Phase 4 goal of "every documented capability of the 9 collection types works as specified."

**Precedent.** BUG-001 was scoped to a single proof-engine strategy fix and shipped in Phase 3, not in the phase that originally exposed it (Phase 2 sample authoring). BUG-004 follows the same pattern. BUG-012 is structurally the same shape.

**Tradeoff accepted.** Authors who reach for `Coll.first <= "Medium"` between Phase 4 and Phase 5 will hit `PRE0112` with a confusing message (now pointing at a more elaborate expression). Mitigation: F-LANG-COLL-03 ships with an updated `UnprovedModifierRequirement` message that mentions BUG-012 by reference until the bug closes (or include in `collection-types.md` a brief "literal-side ordered comparisons are landing in Phase 5" footnote that disappears with BUG-012's fix).

**Sources.** `docs/Working/bugs.md:54-82` (BUG-012), `docs/Working/compiler-readiness-plan-2026-05-24.md:706-720` (Phase 5 scope).

## 5. Acceptance criteria

A vertical slice is considered complete only when ALL of the following hold:

1. Each of the seven collection kinds with each of the five `ChoiceElement` element types parses without `MissingTypeReference` or `ExpectedToken` diagnostics. (Positive parse tests, ~35 cases.)
2. `field F as set of choice of string("Low","Medium","High") ordered` produces a `TypedField` whose `ElementType` records `(TypeKind.Choice, ChoiceMeta(Domain=["Low","Medium","High"], ElementOfChoice=String, Ordered=true))`. (TypeChecker integration test.)
3. `F.min` and `F.max` on an ordered-choice element type type-check cleanly; on an unordered-choice element type emit `RequiredTraitViolation` with a message naming `ordered`. (Positive + negative tests.)
4. `field G as set of choice of string("L","M","H") ordered` field-vs-field comparison through accessors (`F.first <= G.first` where both have the same ordered choice element shape) discharges the `Ordered` modifier obligation without diagnostic. (Proof-engine integration test.)
5. The `set of choice of T(...) ordered` example block at `collection-types.md:148-156` compiles when run through `precept_compile`. (Sample regression test.)
6. `docs/Working/bugs.md` BUG-012 entry is cross-linked back to this design and to a Phase 5 plan item; the entry's "fix complexity" is reaffirmed as small-to-medium per the original root-cause analysis.
7. No new diagnostic codes allocated. All gates use existing `RequiredTraitViolation` (PRE0104), `UnprovedModifierRequirement` (PRE0112), and `CollectionInnerTypeError` (PRE0105) where applicable.
8. `samples/` sweep: any sample that currently uses a `set of string`-plus-cascade workaround for what is naturally `set of choice of T(...) ordered` is *not* refactored in this slice — sample post-fix cleanup is a separate doc-touch in the same PR but a distinct commit, mirroring the BUG-005 post-fix-cleanup pattern.

## 6. Dependencies

- **Upstream**: Phase 3 (proof engine narrowing fixes for BUG-001/BUG-004 family) must be complete — confirmed at `docs/Working/compiler-readiness-plan-2026-05-24.md:673`. F-LANG-COLL-02/03 does not directly depend on those fixes, but Phase 4 sequencing depends on Phase 3 closure.
- **Internal to Phase 4**: F-LANG-COLL-06 (qualified inner types) and this design share the "non-trivial inner-type metadata flows through accessors" plumbing. If F-LANG-COLL-06 ships first, the `ElementTypeRef` shape from D-2 must accommodate qualifier metadata too (recommend pre-design alignment: `ElementTypeRef` becomes a small DU with `ScalarElement`, `ChoiceElement`, `QualifiedElement` variants, and F-LANG-COLL-06 adds the third). If this design ships first, F-LANG-COLL-06 inherits the `ElementTypeRef` shape and extends it.
- **Downstream**: BUG-012 (Phase 5). The acceptance criteria here explicitly carve out literal-side comparisons as Phase 5's responsibility. F-LANG-COLL-03's metadata carrier on `TypedMemberAccess` is *consumed* by BUG-012's fix — when BUG-012's typed-literal strategy lands in the proof engine, it consults the same `ChoiceMeta` slot this design adds.

## 7. Doc-update enumeration

Per `CLAUDE.md` § Documentation Sync (routing table):

| Update | File |
|---|---|
| Language surface — new construct | `docs/language/collection-types.md:60,83,146,489` |
| Language surface — grammar production | `docs/language/precept-language-spec.md:1071-1077,1079,1707` |
| Catalog change — new field on `ChoiceTypeReference` | `src/Precept/Pipeline/ParsedTypeReference.cs:60-65` (record signature changes; doc cross-link from `collection-types.md` "scalar inner type" prose) |
| Diagnostic re-targeting | `docs/compiler/diagnostic-system.md` — confirm `PRE0104` / `PRE0105` examples updated; no new codes |
| Pipeline stage — parser | `docs/compiler/parser.md` § Implementation State — note inner-type slot now accepts `ChoiceTypeReference` |
| Pipeline stage — type checker | `docs/compiler/type-checker.md` § Implementation State — note element-type carrier is now structural |
| Pipeline stage — proof engine | `docs/compiler/proof-engine.md` § Implementation State — note `DeclarationAttribute` Modifier arm reads `TypedMemberAccess.ChoiceMetadata` |
| Bug map | `docs/Working/bugs.md` — add cross-link from BUG-012 |
| Phase plan tracking | `docs/Working/compiler-readiness-plan-2026-05-24.md:689-690` — reframe F-LANG-COLL-02/03 from "diagnostic/verification" to "feature ship" |

## 8. Open questions

Items the owner must ratify before this design locks:

1. **Does the field-modifier path's `Ordered` token migrate fully into `ChoiceTypeReference.Ordered`, or do we maintain both representations during a transition window?** (See § 3 "Field-modifier path" bullet.) Recommendation: full migration, single source of truth on the type ref. Risk: any consumer that today looks for `ModifierKind.Ordered` in `TypedField.Modifiers` must migrate. Audit needed: `grep -rn "ModifierKind.Ordered"` to enumerate consumers.
2. **The exact name of the new structural record from D-2 (`ElementTypeRef`, `InnerTypeRef`, `TypedInner`, …) and whether it lives in `SemanticIndex.cs` or its own file.** Style decision; doesn't affect semantics.
3. **Should `set of choice of T(...)` (no `ordered`) be a sample-corpus pattern, or do we only sanction the `ordered` variant?** The unordered variant is parseable; `.min`/`.max` rejects; `.first`/`.last`/`.at`/`contains` work. There's a real use case (a `set` of allowed categories with no ordinal semantics) but it overlaps significantly with `set of string`. Owner decision: encourage, tolerate, or discourage?
4. **The transition-window diagnostic message for literal-side ordered comparisons** (between F-LANG-COLL-03 ship and BUG-012 ship). Should `UnprovedModifierRequirement` on a literal-side comparison gain a tracking reference (`see BUG-012` in the message text), or stay silent on the cause?

## 9. Falsifiers

This design is wrong if any of:

- **A sample-corpus survey reveals zero current or planned uses of `set of choice of T(...)` outside the two cited examples** (medical-prior-auth, IT helpdesk). If author demand is purely speculative, defer the whole thing. Mitigation: the documentation already commits to the feature at `collection-types.md:146` — even a documentation correction (rather than a feature ship) would be a real-cost choice; this design is justified even at low demand because the doc lie costs more than the implementation does.
- **The structural carrier in D-2 introduces a measurable parse/check perf regression** (> 5% on the existing 6108-test suite). Unlikely (it's a sealed-record reshape, not a hot-path change) but worth measuring on the first vertical slice. Mitigation: if measured, fall back to D-2 Option B for the side-table path and accept the architectural debt for non-field receivers.
- **F-LANG-COLL-06's emerging shape requires a fundamentally different `ElementTypeRef` than what D-2 settles.** Mitigation already noted in § 6 — coordinate pre-design with whichever feature ships first.
- **BUG-012's fix turns out to require the literal-side proof strategy to ALSO consult `TypedMemberAccess.ChoiceMetadata`** in a way that requires propagating the metadata further than this design accommodates (e.g., through arithmetic on choices, which doesn't exist today but might if `choice of integer` ever gets a `+1` operator). The design is wrong to the extent it commits to a metadata shape that doesn't accommodate that future expansion. Mitigation: the `ChoiceMeta` shape is small and additive; keep it open for extension.

## 10. Phase-target rationale

**Phase 4 (collection completeness)** is the canonical home. The phase's stated goal — "every documented capability of the 9 collection types works as specified" — directly indicts the `collection-types.md:146` lie. F-LANG-COLL-02/03 are already listed in the Phase 4 backlog at `compiler-readiness-plan-2026-05-24.md:689-690`, but the phrasing there is too narrow ("targeted diagnostic" / "verification") — this design reframes them as feature-ship items. Sequencing within Phase 4 should be: F-LANG-COLL-06 (qualified inner types) first OR this design first, with a brief coordination step on the shared `ElementTypeRef` shape; the other follows.

**Why not Phase 5.** Phase 5 is proof-engine-shaped (satisfiability extensions, contradiction detection, dead-code). F-LANG-COLL-02 is parser-shaped and F-LANG-COLL-03 is type-checker-shaped. Putting them in Phase 5 would mix language-surface work into a phase explicitly scoped to proof-engine internals. BUG-012, by contrast, IS proof-engine-shaped, so it stays Phase 5.

**Why not earlier.** Phase 2 was "stop the bleeding" (sample-corpus stabilisation). Phase 3 was the F-LANG-BIZ proof-engine extensions. Neither phase was sized to accommodate a language-surface addition.

**Effort estimate.** Within Phase 4's L envelope, F-LANG-COLL-02/03 is sized as M — comparable to F-LANG-COLL-07 (queue of T by P two-field quantifier binding) which is the next-most-architecturally-load-bearing item in the phase. Concrete drivers: D-2's structural reshape cascades to ~6-10 files; the parser change itself is ~30 LOC; tests are ~50 cases.

---

**Status**: Draft 2026-05-26 — pending owner review.

**Resolution path**: owner addresses the four Open Questions in § 8, ratifies (or amends) Decisions D-1 through D-4, and either marks "Locked YYYY-MM-DD" inline or returns specific items for revision. On lock, this doc moves into the Phase 4 execution stream and the doc-touch obligations in § 7 become part of the implementation PR's acceptance.
