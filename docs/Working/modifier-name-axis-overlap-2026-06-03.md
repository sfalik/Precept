---
status: Locked 2026-06-03
phase-target: Relational-rules-and-bounds plan (collection element-type bounds slice) — TBD precise row
comparable-systems-research-status: strong — cites `research/language/collection-element-aggregate-scope-syntax.md`, a Stage-1 survey of six schema/constraint systems with verbatim excerpts and mirrored sources
sources-consulted:
  - research/language/collection-element-aggregate-scope-syntax.md: comparator survey (vocabulary-asymmetry finding; protovalidate min_len/min_items; JSON Schema/CUE/XSD/Ada/SQL-DOMAIN); the "only notempty collides; watch the catalog for a second both-axes modifier" falsifier
  - docs/Working/collection-inner-type-value-modifiers-2026-06-02.md: LOCKED element-modifiers design; Supporting Decision 3 (per-element notempty awaits a disambiguator); Falsifier 5 (set of string notempty mis-read as mincount 1); ElementPositionValueTokens routing rule
  - src/Precept/Language/Modifiers.cs: the notempty entry (StringAndCollectionTypes, L122-139); StringOnly group (L37); ElementPositionValueTokens derivation (L373-378); notempty's two ProofSatisfactions (length + count accessors, L126-136)
  - src/Precept/Pipeline/ProofEngine.Strategies.cs: SatisfactionCovers (L293-361) — the access-safety discharge path; mincount's DeclarationValue bound resolves to null (L319-322) so mincount does NOT discharge count>0 today; notempty's count>0 satisfaction DOES (L332-333)
  - src/Precept/Language/Actions.cs: the .peek/.first/dequeue/pop access obligation is NumericProofRequirement(count, GreaterThan, 0) (L111-113, 135-137, 207-211, 250-252)
  - docs/language/collection-types.md: Constraint Catalog (notempty "equivalent to mincount 1; statically discharges .min/.max/.peek/.peekby/.first/.last"); the Element-level-constraints callout
  - docs/language/precept-language-spec.md: §0.1 (11 principles, esp. P5 keyword-anchored readability L100, P4 inspectability); §2.3 inner-type value modifiers (L1121 — notempty "left at field-modifier position … awaits a disambiguator"); §2.4 modifier↔type table (L1669) + notempty-on-collections discharge note (L1679) + notempty+optional mutex note (L1681); §2.4 constraint-modifier-shorthand (L1133)
  - src/Precept.Analyzers/Precept0011ModifiersCrossRef.cs: the catalog-introspecting analyzer pattern (reads GetMeta(ModifierKind) switch arms via CatalogAnalysisHelpers; per-arm + cross-arm checks)
  - src/Precept.Analyzers/Precept0027DiagnosticEmissionCoverage.cs: sibling analyzer (RegisterCompilationAction, descriptor shape, error severity) the new no-overlap analyzer mirrors
  - src/Precept.Analyzers/CatalogAnalysisHelpers.cs: GetNamedArgument / EnumerateCollectionElements / ResolveEnumFieldName helpers the new analyzer reuses to read ApplicableTo TypeTarget arrays
  - samples/shopping-cart.precept: the SOLE collection-level notempty in the corpus — the event Create parameter CatalogItems (L94, set of string notempty), NOT the field Catalog (L23, no notempty, only contains-read); migration target
---

# Removing the collection-vs-element modifier-name axis overlap (`notempty` → string-only)

## Goal

When done, no value modifier's `ApplicableTo` spans both a collection kind and a scalar kind, the build fails if anyone reintroduces such a modifier, `set of string notempty` cleanly routes to the element ("each element is a non-empty string") because the per-`ModifierKind` switch that binds element bounds is replaced by generic satisfaction-shape binding, and "the collection has ≥1 element" is written `mincount 1` — which discharges `.first`/`.peek`/`.last` access safety exactly as `notempty` did. *(Testable: `set of string notempty` rejects an empty-string element; a collection field declared `mincount 1` lets `.peek` compile with no `count > 0` guard; a synthetic both-axes modifier fails the analyzer; the corpus is green after the one-line shopping-cart migration.)*

## Scope

- **In scope**
  - Retarget the `notempty` catalog entry from `StringAndCollectionTypes` to `StringOnly` in `Modifiers.cs` (drop its collection applicability and its `count`-accessor `ProofSatisfaction`).
  - A new Roslyn analyzer (sibling of `Precept0027`/`Precept0028`, in `src/Precept.Analyzers/`) that fails the build if any `ValueModifierMeta.ApplicableTo` contains both a collection `TypeKind` and a scalar `TypeKind`.
  - Wire `mincount 1` (and `mincount N`, N ≥ 1) to discharge the collection emptiness access-safety obligation (`count > 0`) that `notempty` discharged — a required build item, because the proof engine today discharges that obligation only from `notempty`, not from `mincount` (see § Semantic Rules, Rule 3 + the grounding finding).
  - Genericize collection-element bound binding: replace the per-`ModifierKind` `switch` in `BuildElementValueBounds` (`TypeChecker.cs:206-228`) with a loop over `Modifiers.GetMeta(kind).ProofSatisfactions`, dispatching on the satisfaction DU shape — a required build item, because that switch has no `notempty` arm today (it hardcodes `NotEmpty: false`), so routing `notempty` inward type-checks but produces no bound; it is also itself the `*Kind`-enum-dispatch anti-pattern (see § Semantic Rules, Rule 2 + the grounding finding).
  - Migrate the one collection-level `notempty` in the corpus (`samples/shopping-cart.precept:94`) to `mincount 1`.
  - Doc-sync: `precept-language-spec.md` §2.4 table + notes, `collection-types.md` Constraint Catalog + callouts + the per-kind "discharged by `notempty`" lines + the `sortedset`-rejection rationale, the element-modifiers Working design's Supporting Decision 3 / Falsifier 5. D3 locked element-`notempty` and collection-`notempty` *coexisting*; this design **reverses the collection-coexistence half** (owner-authorized via Direction A — "drop `notempty` for collections") while *resolving* D3's element-`notempty` half.

- **Out of scope**
  - The broader element-vs-collection *explicit-scope-syntax* question (research Candidates 2/3/5: `each` marker, grouping parens, named element types). This design takes the research's Candidate-1/Candidate-4 path (close `notempty`, lean on vocabulary asymmetry) for the **one** overlap; it does not introduce any new scope marker.
  - Any change to `mincount`/`maxcount`/`optional`/`default` semantics beyond wiring `mincount`'s emptiness discharge.
  - Two-axis element bounds on `queue of T by P` / `lookup of K to V` parameters (already deferred by the element-modifiers design).

- **Deferred to future**
  - If a *second* modifier ever needs both a scalar and a collection target (the research falsifier), the analyzer forces that to be a deliberate, surfaced decision rather than a silent reintroduction — at which point the explicit-scope-syntax question (research Candidate 2/3) reopens.

## Philosophy Alignment

| # | Principle | Affected? | How served (cite) | Tension | Tradeoff |
|---|---|---|---|---|---|
| 1 | Prevention not detection | Y | The analyzer makes a both-axes modifier a *build-time impossibility* (`Precept0027`-style `RegisterCompilationAction`, error severity), not a thing caught in code review — prevention applied to the catalog itself. | N/A | N/A |
| 2 | One file, complete rules | N | Per-`.precept` rule completeness is unchanged; the change is in the C# catalog and proof engine. | N/A | N/A |
| 3 | Determinism | N | No solver, no new evaluation path; the discharge wiring is a static interval comparison. | N/A | N/A |
| 4 | Full inspectability | Y | After retarget, the inner-type routing (`ElementPositionValueTokens`) makes `set of string notempty` *visibly* a per-element bind (hover shows it on the element), and `mincount 1` *names* the cardinality axis — both scopes are inspectable from the type, not inferred (spec P4, L11). | The collection author loses the word `notempty`; must learn `mincount 1` names the same thing. | Accept: one fewer overloaded keyword is *more* inspectable, not less (spec §2.4 already says `notempty` *is* `mincount 1` on a collection). |
| 5 | Keyword-anchored readability | Y | This is the **motivation** (research § Implications, §0.8). The reader's axis question answers itself: content-words (`notempty`/`minlength`/`maxlength`) scope the element; count-words (`mincount`/`maxcount`) scope the collection — the "vocabulary asymmetry" the survey found (spec P5, L100; research Candidate 4). | Author who reaches for `notempty` on a collection intending cardinality now silently gets the per-element reading (no diagnostic — owner decision); they learn `mincount 1` from docs/hover. | Accept: a one-time redirect to `mincount 1` is cheaper than a permanently-overloaded keyword whose scope a reader must infer from type knowledge. |
| 6 | Explicit domain meaning over primitive convenience | N | No type-identity change; `notempty` stays a string flag, `mincount` stays a cardinality bound. | N/A | N/A |
| 7 | Compile-time-first static checking | Y | The `mincount 1` discharge wiring keeps the `.peek`/`.first` count-safety obligation *prove-or-reject* — it must not regress to "deferred" when `notempty` is removed from collections (spec P7, L17). | N/A | Accept: this is the load-bearing build item — without it, dropping collection-`notempty` would silently re-open a totality hole. |
| 8 | Honesty about approximation | N | No approximation introduced or removed. | N/A | N/A |
| 9 | Mandatory rationale | N | Both `notempty` and `mincount` are constraint-modifier shorthand with generated rationales (spec §2.4, L1133); unchanged. | N/A | N/A |
| 10 | Static semantic checking | Y | `set of string notempty` now type-checks the element (`InvalidModifierForType` if the element isn't a string), routed by the *existing* `ElementPositionValueTokens` rule and bound by generic satisfaction-shape binding (the per-`ModifierKind` switch in `BuildElementValueBounds` replaced, not extended) — spec P10. | N/A | N/A |
| 11 | Static completeness | Y | The `mincount 1` discharge wiring (Rule 3) preserves the property that every `.peek`/`.first`/`.last` access on a guaranteed-non-empty collection is statically proven safe — no well-typed program faults on empty access (spec P11, L25; §0.7). | N/A | Accept: completeness is preserved *only because* the discharge moves to `mincount`; this is a required, not optional, part of the build. |

**Companion commitments.** *Stateless-first-class*: unaffected — `notempty` on a string field/arg and `mincount` on a collection field both work identically in a stateless data-only precept. *Domain-expert-primary-author*: the change *helps* the domain expert — the survey's whole concern is that a reader cannot tell `notempty`'s scope on a collection; removing the overlap means the only `notempty` they ever read is the unambiguous string one, and cardinality always reads as `mincount`/`maxcount` (a count word). The one-time cost is learning `mincount 1` for "has at least one element," which the spec already documents as `notempty`'s equivalent.

## Language Design Grounding

### General field

The element-vs-aggregate axis is, across every comparable system, made explicit by **structural separation, not applicability inference** (research § Findings). Precept is the outlier: it writes element and collection modifiers as bare trailing tokens on one line, separated only by which type each modifier applies to. The research's central finding is that Precept *partly designed around* this — its vocabulary already separates most axes (`maxlength` element vs `maxcount` collection), so the only genuine collision is `notempty`, the **sole** modifier valid on both a scalar `string` and a collection.

The decisive precedent is **protovalidate's vocabulary asymmetry**, which the research grades as the directly-applicable comparator:

> "Use `items` to apply validation rules to each element in a repeated field" … `min_len` (element string length) vs `min_items` (aggregate count).
> — research § protovalidate, citing *protovalidate — Standard rules* (accessed 2026-06-03)

protovalidate never collides because element and aggregate constraints live in distinct keyword namespaces (`min_len` vs `min_items`). The research's Candidate 4 ("vocabulary asymmetry") generalizes this: *count-words scope the collection; value-words scope each element.* Precept already has this for every modifier **except** `notempty`. Removing `notempty` from collections makes the asymmetry total — the design's whole move.

**JSON Schema** and **CUE** corroborate that the two axes are independently expressible without a shared keyword:

> "The length of the array can be specified using the `minItems` and `maxItems` keywords." (element rules live under `items`)
> — research § JSON Schema, citing *Understanding JSON Schema — Arrays*, Draft 2020-12 (accessed 2026-06-03)

In JSON Schema "each element non-empty" is `items: { minLength: 1 }` and "array non-empty" is `minItems: 1` — two keywords, never one. Precept's positional split after this change is the same separation: per-element `notempty` (element string) vs `mincount 1` (collection cardinality).

**What Precept takes / diverges.** Precept takes the vocabulary-asymmetry consensus (each axis self-names). It diverges from JSON Schema/CUE/XSD by *not* introducing a structural container (`items`, `[...]`, a named type) for the element — it relies on inner-type *position* plus the now-total vocabulary asymmetry, because (a) the only collision is being removed, and (b) a structural marker is new surface the research judged unwarranted for a one-modifier problem (research Conclusions, "Candidate 1/4 is the cheapest structurally-complete fix").

### Precept-specific application

- **Spec §2.3 (L1121)** already records the consequence of the overlap: the element-modifier design "left [`notempty`] at field-modifier position … per-element `notempty` awaits a disambiguator." Retargeting to `StringOnly` makes `notempty` scalar-only, so the *existing* `ElementPositionValueTokens` derivation (`Modifiers.cs:373-378` — "type-restricted AND applies to no collection type") routes it to the element with **no parser change** (the derived token set just widens). Routing alone does not produce a bound, though: `BuildElementValueBounds` (`TypeChecker.cs:206-228`) is a per-`ModifierKind` switch with no `notempty` arm (it hardcodes `NotEmpty: false`), so the required build item is to **genericize that switch into satisfaction-shape binding** (Rule 2) — after which `notempty` binds for free with no per-modifier arm. The disambiguator the spec was waiting for is "remove the collection meaning."
- **Spec §0.1 P5 (L100), §0.8** are the motivation (keyword-anchored, domain-expert-read-top-to-bottom).
- **No locked *spec* rejection is overridden; one locked *working-design* half is reversed under owner authorization.** Spec §2.4 and `collection-types.md` *describe* `notempty`-on-collections as current behavior; they do not lock it as a rejected-alternative or "no-X" decision (grep recorded under Decision 1's Sources). The element-modifiers Working design's Supporting Decision 3 (`collection-inner-type-value-modifiers-2026-06-02.md:371-381`) *locked* that element-`notempty` and collection-`notempty` **coexist**. This design **reverses the collection-coexistence half** of D3 — owner-authorized this session via Direction A ("drop `notempty` for collections") — and **resolves** its element-`notempty` half (the per-element bind D3 anticipated). So D3 is part-reversed (collection), part-resolved (element), not a clean resolve.

## Audience and Teachability

### Worked example (plausible domain — order intake)

```precept
# A cart must carry at least one catalog item, and each item id is a non-empty code.
field CatalogItems as set of string notempty mincount 1
field PromoCodes  as set of string maxlength 32   # each code at most 32 chars
field NextItem    as string optional

from Building on PickFirst when CatalogItems.count > 0
    -> set NextItem = CatalogItems.min   # access safe: count > 0 guard discharges
    -> no transition
```

`set of string notempty mincount 1` reads cleanly in two axes: `notempty` (a value word) scopes **each element** — every item id is a non-empty string — and `mincount 1` (a count word) scopes the **collection** — at least one item. Before this change, `set of string notempty` was ambiguous and the language sidestepped it by refusing to route `notempty` inward at all; now it routes by the same rule as `maxlength`.

### What happens on the likely confusion (no diagnostic — by owner decision)

A collection author reaches for the old spelling intending cardinality:

```
field Tags as set of string notempty
```

This does **not** error. `notempty` is scalar-only after the retarget, so it routes to the element exactly like `maxlength` would, and `set of string notempty` means *each element is a non-empty string*. An author who meant "the set has at least one element" silently gets the per-element reading instead. **The owner decided (2026-06-03) to accept this silently — no advisory diagnostic** (see § Resolved questions): a "did you mean `mincount 1`?" heuristic would false-positive on legitimate per-element `notempty`, and the per-element meaning is itself sound and useful. The teaching path and docs carry the `mincount 1` form; Falsifier 2 watches for the meaning-inversion actually biting in practice.

PRE0033 (`InvalidModifierForType`) still fires for a genuine type mismatch — `notempty` on a non-string element (`field Tags as set of integer notempty`) or on a collection element that has no string to bind to — because `notempty`'s `ApplicableTo` is now `[String]`:

```
field Tags as set of integer notempty
                             ^^^^^^^^
PRE0033 (InvalidModifierForType): 'notempty' applies to 'string', not to the
element type 'integer'.
```

### ≤10-minute teaching path

1. *(2 min)* `notempty` is a **value** word — it means "this string has content." It belongs to a string, never to a collection-as-a-whole. (`collection-types.md § Constraint Catalog`, post-update.)
2. *(2 min)* "The collection has at least one element" is a **count** word: `mincount 1`. (Same catalog row.)
3. *(2 min)* Count words (`mincount`/`maxcount`) scope the collection; value words (`notempty`/`minlength`/`maxlength`/`min`/`max`) scope each element. (spec §2.4 per-element note.)
4. *(2 min)* So `set of string notempty mincount 1` = "at least one element, each a non-empty string." (Worked example above.)
5. *(2 min)* `mincount 1` (and `notempty` historically) lets you read `.first`/`.peek`/`.min` with no `count > 0` guard — the collection is provably non-empty. (spec §2.4 discharge note.)

## Semantic Rules

Notation: `m` a value modifier; `ApplicableTo(m)` its `TypeTarget[]`; the collection access-safety obligation for `.peek`/`.first`/`.last`/`.min`/`.max`/`dequeue`/`pop` is `NumericProofRequirement(SelfSubject(count), GreaterThan, 0)` (`Actions.cs:111-113`).

### Rule 1 — `notempty` typing (retarget)

```
  ApplicableTo(notempty) = StringOnly = [String]      (after retarget)
─────────────────────────────────────────────────────────────────────  [Notempty-Scalar]
  notempty legal on bound type T  iff  T = String

  notempty binds to type T  ∧  T ≠ String   (e.g. set of integer notempty —
                                              element type is integer)
──────────────────────────────────────────────────────────────────────────  [Notempty-Type-Mismatch-Bad]
  emit InvalidModifierForType (PRE0033) at the notempty span
```

This reuses the *existing* §2.4 compatibility check with the narrowed `ApplicableTo`; no new typing axis. Note the interaction with Rule 2: when an inner type is present (`set of <T> notempty`), `notempty` *routes to the element* and is type-checked against the element type `T` — so `set of string notempty` is **clean** (element is string) and `set of integer notempty` errors (element is integer). PRE0033 therefore fires on a genuine *type* mismatch, not on the cardinality-confusion case (which is accepted silently — see § Resolved questions). Modifier-value and mutex checks (`notempty`+`optional` ⇒ `ConflictingModifiers`/C120, spec §2.4 L1681) are unchanged.

### Rule 2 — Inner-type routing + generic element-bound binding (the required build item)

The parser side routes correctly already: `notempty` enters `ElementPositionValueTokens` for free once it is scalar-only.

```
  notempty ∈ ElementPositionValueTokens
    ⟺ ApplicableTo(notempty) ≠ ∅  ∧  ApplicableTo(notempty) ∩ CollectionKinds = ∅
    ⟺ ApplicableTo(notempty) = [String]                          (after retarget — TRUE)
──────────────────────────────────────────────────────────────────────────────────  [Notempty-Routes-To-Element]
  parser consumes `notempty` after the inner type into the element-modifier list
```

**But routing inward is not sufficient to produce a bound.** `BuildElementValueBounds` (`TypeChecker.cs:206-228`) — the function that turns a routed element modifier into a `DeclaredValueBounds` — is a `switch (modifier.Kind)` with arms for `minlength`/`maxlength`/`min`/`max`/flags and **no `notempty` arm**; it hardcodes `NotEmpty: false` (L231). So a routed `notempty` type-checks but contributes no bound — the element would not actually be constrained non-empty. That switch is also the CLAUDE.md `*Kind`-enum-dispatch anti-pattern (one arm per member "because the language says so").

The fix is **fully generic catalog-driven binding** (a catalog-sufficiency probe confirmed no catalog enrichment is needed — `notempty`'s existing `ProofSatisfactions` already carry the shapes): replace the `switch (modifier.Kind)` with a loop over `Modifiers.GetMeta(kind).ProofSatisfactions`, dispatching on the **satisfaction DU shape** `(Projection, Comparison, Bound)` — allowed DU-subtype dispatch, not enum-identity dispatch:

```
  for each s : ProofSatisfaction.Numeric in GetMeta(modifier.Kind).ProofSatisfactions
    match (s.Projection, s.Comparison, s.Bound):
      Accessor("length") , >  , Constant(0)              → NotEmpty = true   (length-non-empty)
      Accessor("count")  , >  , Constant(0)              → NotEmpty = true   (count-non-empty)
      Accessor("length") , .. , DeclarationValue         → minLength | maxLength via TryReadLengthLiteral
      SelfValue          , .. , DeclarationValue         → declaredMin | declaredMax via TryGetComparableModifierValue
      SelfValue          , ≥/> , Constant                → NumericFlags.Add(modifier.Kind)
──────────────────────────────────────────────────────────────────────────────────  [Element-Bound-From-Satisfactions]
  `set of string notempty`  ≡  element bound with NotEmpty = true  ≡  `minlength 1` per element
```

This **exactly mirrors the existing precedent** `FlagLowerBoundFromMeta` (`ProofEngine.Intervals.cs:253-267`), which already projects modifier meaning generically off `ProofSatisfactions` with no `Kind` switch. Valued bounds keep reusing `TryReadLengthLiteral` (length-valued: `minlength`/`maxlength`) and `TryGetComparableModifierValue` (numeric-valued: `min`/`max` — preserving the UCUM `Declared`/`Normalized` split). The behaviour for every existing element modifier is identical to the current switch (guarded by the shipped Phase-1/2 element-bound tests staying green); the only *new* effect is `notempty` setting `NotEmpty = true` for free — the proof engine then folds it to `minlength ≥ 1` (`ProofEngine.Analysis.cs:452-453`). `maxplaces`/`optional`/`default`/`ordered` contribute nothing naturally (they carry no Numeric satisfaction of these shapes), so genericizing introduces no spurious bounds.

`set of string notempty` therefore means **each element is a non-empty string**, identical to `set of string minlength 1` — because `notempty` on a `string` desugars to `minlength 1` (spec §2.4 shorthand, L1133; `ProofEngine.Lengths.cs:237` `notempty ⇒ length ≥ 1`). This is the per-element `notempty` the element-modifiers design's Supporting Decision 3 sought — achieved by catalog-derived routing **plus** generic satisfaction-shape binding, with the per-`ModifierKind` switch removed rather than extended with one more arm.

### Rule 3 — `mincount 1` discharges the emptiness access obligation (the required build item)

The access obligation is `count > 0`. Today it is discharged (Strategy "ProofSatisfactions walk", `ProofEngine.Strategies.cs:186-197`) by walking the field's modifiers and matching a `ProofSatisfaction.Numeric` whose projection is the `count` accessor:

- `notempty` carries `count > 0` via a `Constant(0)` bound (`Modifiers.cs:132-135`). `SatisfactionCovers` matches `(GreaterThan, GreaterThan) when boundValue (0) >= threshold (0)` (`Strategies.cs:332-333`) ⇒ **discharges**.
- `mincount` carries `count >= DeclarationValue` (`Modifiers.cs:207-217`). `SatisfactionCovers` resolves `NumericBoundSource.DeclarationValue` to `null` (`Strategies.cs:319` — "conservative — cannot compare without runtime value") ⇒ `boundValue is null` ⇒ returns `false` ⇒ **does NOT discharge today.**

So removing collection-`notempty` *without further work would re-open the empty-access totality hole* for authors who switch to `mincount 1`. The build must therefore make `mincount N` (with a statically-known `N ≥ 1`) discharge `count > 0`:

```
  field F declares mincount N     N ≥ 1 (statically resolved literal magnitude)
─────────────────────────────────────────────────────────────────────────────  [Mincount-Discharges-Emptiness]
  obligation  F.count > 0  on a .peek/.first/.last/.min/.max read of F  →  discharged
```

The plug-in point: resolve `mincount`'s declared magnitude (already on `TypedField.DeclaredMinCount`, an `int?` populated at `TypeChecker.cs:709`) and compare against the requirement threshold, mirroring the `notempty` arm. This stays catalog-mediated (the discharge reads the declared bound; no `ModifierKind` switch). Scope this to a **statically-literal `N`**: discharge `count > 0` when `DeclaredMinCount >= 1`, keeping the conservative `DeclarationValue → null` default (`Strategies.cs:319`) for non-literal cases so nothing is over-discharged. *(Equivalently, `mincount`'s `ProofSatisfaction` could gain a resolvable bound for the literal-`N` case; the design fixes the capability, the build picks the cleaner of the two shapes.)*

### Rule 4 — The analyzer invariant (build-time)

```
  m : ValueModifierMeta     ApplicableTo(m) ∩ CollectionKinds ≠ ∅
                            ∧  ApplicableTo(m) ∩ ScalarKinds ≠ ∅
─────────────────────────────────────────────────────────────────────  [Both-Axes-Bad]
  emit PRECEPT00NN (error) at the GetMeta arm — build fails
```

`CollectionKinds` = the `CollectionTypes` set (`Modifiers.cs:40-45`); `ScalarKinds` = every non-collection `TypeKind`. This makes the "only one axis per modifier" property a structural guarantee, so the routing rule (Rule 2) can never again face a both-axes modifier.

**Note — why the missing `notempty` arm went uncaught, and the follow-up.** The existing `Precept0019 PipelineCoverageExhaustiveness` analyzer (opt-in via `[HandlesCatalogExhaustively(typeof(T))]`) is the mechanism that *would* have flagged the missing `notempty` arm in `BuildElementValueBounds` — but the element-bound binding switch was never enrolled (today only `ExpressionFormKind`/`OutcomeArgumentKind`/`ActionSyntaxShape` are enrolled, on the Parser). Genericizing `BuildElementValueBounds` (Rule 2) obviates the question for this site — there is no switch left to enroll. The broader gap (other surviving consumer-side modifier-`Kind` switches that should either be enrolled in `Precept0019` or genericized, or be caught by a "no consumer-side `*Kind` dispatch" analyzer) is a **follow-up catalog-discipline sweep, not this slice**. Decision 2 (the no-overlap analyzer) is unaffected and stays as scoped.

### Rule 5 — Soundness preservation (Principles 7, 10, 11)

- **P7 / P10 / P11.** The only soundness-relevant change is moving the emptiness discharge from `notempty` to `mincount 1` (Rule 3). After the build item lands, *the set of programs that prove `count > 0` is unchanged* (anything that wrote collection-`notempty` rewrites to `mincount 1`, which now discharges identically). No `.peek`/`.first` access becomes unprovable that was provable before; no fault path is created. The retarget itself can only *narrow* `notempty`'s legal positions (P10: strictly fewer accepted programs on the collection axis, never a new fault). The analyzer (Rule 4) adds a build-time check; it touches no runtime evaluation.
- **Soundness guard on Rule 3.** This is the matched pair: collection-`notempty` removal (Rule 1) is sound *only because* `mincount 1` discharge (Rule 3) ships in the same change. Shipping Rule 1 without Rule 3 would regress P11 (an empty-access fault would become reachable for the migrated samples). They are not separable.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Two layers:
1. *Catalog* — the `notempty` `ApplicableTo` retarget and the dropped `count` `ProofSatisfaction` live in `Modifiers.cs` (`GetMeta`). This is correct per catalog-first: `ApplicableTo` is the single source the type checker, `ElementPositionValueTokens`, LS, and MCP all derive from. Changing the catalog entry propagates everywhere automatically — no parallel list edit.
2. *Build-tooling (Roslyn analyzer)* — the no-overlap invariant belongs in `src/Precept.Analyzers/` alongside the other catalog-cross-ref analyzers (`Precept0011`, `Precept0027`), **not** in pipeline code, because it is a *meta-property of the catalog source*, checked at C# compile time against the `GetMeta(ModifierKind)` switch — exactly the surface `Precept0011ModifiersCrossRef` already analyzes. Placing it in the pipeline would mean checking a static catalog invariant at `.precept`-compile time, which is the wrong layer (the invariant is about the catalog's own shape, knowable without any `.precept` input).

The `mincount` discharge wiring (Rule 3) is *pipeline* (`ProofEngine.Strategies.cs`) because it is a proof-discharge capability, not catalog metadata — though it reads catalog/`TypedField` declared bounds.

**Cross-component propagation.**
- *Runtime (parser, type checker, evaluator, diagnostics):* Parser — `ElementPositionValueTokens` now includes `notempty` token, so the inner-type parser consumes it after the inner type (no parser code change; the derived set widens). Type checker — `BuildElementValueBounds` genericized (Rule 2); `notempty` after an inner type now routes to and validates the element (`InvalidModifierForType` only when the element type isn't `string`, e.g. `set of integer notempty`); `set of string notempty` binds per-element and is clean (was never routed before). `notempty` no longer carries any collection meaning. Proof engine — `mincount 1` discharge added (Rule 3); `notempty`'s `count` `ProofSatisfaction` removed (no longer relevant to any collection). Diagnostics — reuse PRE0033 (`InvalidModifierForType`) for a genuine element-type mismatch (`set of integer notempty`); no new diagnostic and no advisory (owner decision — accept the cardinality-confusion case silently).
- *Tooling (syntax highlighting, completions, hover, semantic tokens):* Completions/hover for `notempty` on a collection field no longer offered/valid (catalog-derived — automatic). Hover on `set of string notempty` shows it as an element bound. Semantic tokens unchanged (still a modifier token). `docs/tooling/language-server.md` touch only if hover copy hardcodes the old "string or collection" description.
- *MCP (vocabulary, DTOs, tool output):* `CatalogFormatters` surfaces `notempty`'s `ApplicableTo` (now string-only) — automatic from the catalog. No DTO shape change. `docs/tooling/mcp.md` touch only if a formatter hardcodes the applicability text.

**Breaking changes.** Yes — author-visible semantic flip: `set of string notempty` changes meaning from *collection cardinality (`mincount 1`)* to *per-element bind (each element non-empty)* — a **silent meaning change**, not a rejection (the owner accepted the silent case; no diagnostic). `notempty` on a *non-string* element (`set of integer notempty`) flips from accepted-as-cardinality to *rejected* (`InvalidModifierForType`). This is the high-stakes part. Pre-release, solo-dev, one corpus occurrence — so reversal-cost is bounded (Decision 1 blast radius). No external `.precept` authors depend on it. The new analyzer diagnostic ID (`PRECEPT00NN`) is a new build-diagnostic code (Precept-internal, not `.precept`-author-visible).

### External architectural precedent

The invariant-enforcing analyzer mirrors Precept's own established pattern and Roslyn's analyzer-SDK separation. Roslyn separates *analysis* (compilation-end checks over the symbol graph) from *the compiler proper*:

> "An analyzer that requires analyzing the entire compilation … can register a `RegisterCompilationAction` … invoked once at compilation end."
> — Roslyn analyzer SDK pattern, as already instantiated in `Precept0027DiagnosticEmissionCoverage.cs:48` (`context.RegisterCompilationAction(AnalyzeCompilation)`).

Precept already uses this exact shape for catalog invariants (`Precept0027` for diagnostic-emission coverage; `Precept0011` for modifier mutex/subsumes symmetry). The new analyzer takes the same architecture — read the catalog's `GetMeta` switch arms via `CatalogAnalysisHelpers`, assert a cross-arm/per-arm property, error on violation. **Divergence:** `Precept0011` checks *relational* properties (mutex symmetry, subsumes acyclicity); the new one checks a *per-arm shape* property (`ApplicableTo` axis-disjointness) — a simpler per-arm check, closest to `Precept0011`'s self-reference checks (`CheckMutexSelfRef`). No novel architecture; a new instance of the existing catalog-invariant-analyzer family.

## Inventory of what will be built

| Artifact | File(s) | Note |
|---|---|---|
| Retarget `notempty` `ApplicableTo` | `src/Precept/Language/Modifiers.cs:122-139` | `StringAndCollectionTypes` → `StringOnly`; drop the `Accessor("count") > 0` `ProofSatisfaction` (L132-135), keep the `Accessor("length") > 0` one; update the `HoverDescription` to string-only wording. |
| Genericize element-bound binding | `src/Precept/Pipeline/TypeChecker.cs:206-228` (`BuildElementValueBounds`) | Rule 2: replace the per-`ModifierKind` `switch` with a loop over `ProofSatisfactions`, dispatching on the satisfaction DU shape (mirror `FlagLowerBoundFromMeta`, `ProofEngine.Intervals.cs:253-267`); reuse `TryReadLengthLiteral`/`TryGetComparableModifierValue`; set `NotEmpty=true` on `Accessor("length"\|"count") > Constant(0)`; append to `NumericFlags` on `SelfValue + Constant`. **Modifies already-shipped code** (the Phase-1/2 element-binding switch); must be behavior-preserving for existing element modifiers (`maxlength`/`min`/`max`/flags bind identically — guarded by the shipped Phase-1/2 element-bound tests staying green) while adding `notempty`. |
| `mincount 1` emptiness discharge | `src/Precept/Pipeline/ProofEngine.Strategies.cs` (the modifier-satisfaction walk ~L186-197, or `SatisfactionCovers` ~L316) | Rule 3: resolve `DeclaredMinCount` (literal `N ≥ 1`) and discharge `count > 0`. Catalog/`TypedField`-mediated; no `ModifierKind` switch. |
| New no-overlap analyzer | `src/Precept.Analyzers/Precept00NNModifierAxisDisjoint.cs` (next free ID after 0028) | Rule 4: per-arm check on `GetMeta(ModifierKind)`; reuse `CatalogAnalysisHelpers.GetNamedArgument("ApplicableTo")` + `EnumerateCollectionElements` + a `TypeKind`-name → collection/scalar classifier; mirror `Precept0027` descriptor + `RegisterCompilationAction`/`RegisterOperationAction(SwitchExpression)` shape. Error severity. |
| Analyzer test stubs | `test/Precept.Analyzers.Tests/` | (a) a synthetic both-axes `ValueModifierMeta` fixture triggers the diagnostic; (b) the real catalog (string-only `notempty`, collection-only `mincount`) is clean. |
| Corpus migration | `samples/shopping-cart.precept:94` | The sole collection-`notempty` is the **`event Create` parameter** `CatalogItems as set of string notempty` (L90-94), **not** a field. (The field `Catalog`, L23, has no `notempty`; it is assigned `set Catalog = Create.CatalogItems` (L119) and used only via `Catalog contains …` (L124, L131) — never `.peek`/`.first`/`.min`-read, so no element-access discharge depends on it.) Rewrite the parameter to `set of string notempty mincount 1` (each id non-empty + ≥1 element) **or** `set of string mincount 1` if cardinality-only was the intent — confirm intent at build. No element-access dependency exists either way. |
| Compiler test stubs | `test/Precept.Tests/` | `set of string notempty` rejects an empty-string element (binds per-element); `set of integer notempty` emits PRE0033 (element-type mismatch); `set of string notempty` does NOT error (silent per-element bind — no advisory); `mincount 1` field lets `.peek` compile with no guard; `mincount 0` does NOT discharge `count > 0`. |
| Doc-sync | spec §2.3/§2.4, `collection-types.md`, element-modifiers Working design | See Doc-update enumeration. |

## Decisions

### Decision 1 — Retarget `notempty` from `StringAndCollectionTypes` to `StringOnly`

**Stakes**: high *(author-visible semantic flip on an existing modifier; pre-release, solo-dev, one corpus use ⇒ high, not irreversible)*

- **Rationale.** `notempty` is the *sole* both-axes modifier (`Modifiers.cs:47-53` vs every other element-routable modifier being scalar-only). Removing its collection target (a) eliminates the one genuine element-vs-collection ambiguity the research found, (b) makes the vocabulary asymmetry total (count-words → collection, value-words → element), and (c) — because `ElementPositionValueTokens` keys on "scalar-only" — *automatically* routes `set of string notempty` to the element, which, once `BuildElementValueBounds` is genericized (Rule 2: the per-`ModifierKind` switch replaced by satisfaction-shape binding), delivers the per-element `notempty` the element-modifiers design wanted with no per-modifier arm. Collection cardinality stays fully expressible: `mincount 1` already means exactly "≥1 element" (spec §2.4 L1679: `notempty` *is* `mincount 1` on a collection). Includes the required `mincount 1` discharge wiring (Rule 3) so no totality regresses.
- **Tradeoff accepted.** A collection author who reaches for `notempty` intending cardinality now silently gets the per-element reading (no diagnostic — owner decision) and must learn `mincount 1`; `notempty` carries the slight teaching cost of "value word, element only," and the silent meaning-change is a real risk the design accepts (watched by Falsifier 2a). Accepted because a permanently-overloaded keyword whose scope a reader infers from type knowledge is the exact §0.8 friction the survey identified, and a "did you mean" advisory would false-positive on legitimate per-element `notempty`.
- **Alternatives considered.** *(a) Keep the overlap, permanently ban `notempty` in inner-type position* (research Candidate 1, status quo): rejected — it leaves `notempty` overloaded on collection fields (the readability friction persists) and *forgoes* the free per-element `notempty`. *(b) Introduce an explicit `each`/parens scope marker* (research Candidates 2/3): rejected for this scope — new language surface the research judged unwarranted for a one-modifier problem ("Candidate 1/4 is the cheapest structurally-complete fix"; surface cost vs §0.4). *(c) Retarget but skip the `mincount 1` discharge wiring*: rejected — re-opens the empty-access totality hole (Rule 3 grounding); not separable from this decision. *(d) Retarget + route inward but add a `notempty` arm to the `BuildElementValueBounds` switch*: rejected — adds one more arm to the very `*Kind`-enum-dispatch anti-pattern this design otherwise removes; generic satisfaction-shape binding (Rule 2) is the catalog-first fix and makes "no new code for a new modifier" literally true. Without the genericization, routing `notempty` inward produces no element bound at all (`NotEmpty` stays hardcoded `false`), so it is not separable either.
- **Precedent.** protovalidate `min_len` (element) vs `min_items` (aggregate) — distinct vocabulary per axis, never colliding (research § protovalidate); JSON Schema `items:{minLength:1}` vs `minItems:1` (research § JSON Schema). Both are the vocabulary-asymmetry family this completes.
- **Sources consulted for this decision.**
  - `src/Precept/Language/Modifiers.cs:122-139` — `ModifierKind.Notempty => new ValueModifierMeta(... StringAndCollectionTypes ...)` with two `ProofSatisfaction.Numeric` (length accessor `>0`, count accessor `>0`); `:373-378` `ElementPositionValueTokens` = "type-restricted AND applies to no collection type."
  - `docs/language/precept-language-spec.md:1121` — "the catalog-derived routed set is exactly the scalar value modifiers that apply to no collection type; `notempty` is therefore left at field-modifier position … per-element `notempty` awaits a disambiguator." `:1679` — "On collection fields, `notempty` is equivalent to `mincount 1`." `:1669` — the modifier↔type table row.
  - **Spec-first settlement check (guard 17):** `grep -in "notempty" docs/language/precept-language-spec.md docs/language/collection-types.md` returns *descriptions* of current behavior (the §2.4 table row, the equivalence note) and the §2.3 statement that per-element `notempty` "awaits a disambiguator" — but **no locked `## Alternatives rejected` entry, no Decision block, no "no X" statement** locking `notempty`'s collection axis. The element-modifiers Working design's Supporting Decision 3 explicitly *anticipated* resolving it. So this is a behavior change to an existing modifier the owner has authorized this session — **not** an override of a locked rejection (would be Tier-3); the §2.3 "awaits a disambiguator" line is the spec inviting exactly this resolution.
  - `research/.../collection-element-aggregate-scope-syntax.md` § Candidate 1 & 4, Conclusions — "exactly one modifier (`notempty`) genuinely collides, and it is redundant both ways."
  - `samples/shopping-cart.precept:90-94,119,124` — the one collection `notempty` is the `event Create` *parameter* `CatalogItems` (L94), not the field; the field `Catalog` (L23) is assigned from it (L119) and only `contains`-read (L124), never accessor-read.
- **Strongest counter-evidence.** The research's "tentative lean" notes the readability friction is *real under §0.8* and that Candidate 2 (`each`) wins "if the design pass concludes the friction warrants visible surface." A reviewer could argue removing `notempty` from collections doesn't *fully* solve the axis-visibility problem for the *other* element modifiers (`maxlength` still binds silently to the element). Response: true, but this design's scope is the *overlap* (the one ambiguous spelling), not the broader visibility question — which it explicitly leaves open (research Candidates 2/3, out of scope). Removing the overlap is strictly necessary for *any* of those candidates and sufficient to close the only genuine ambiguity. *(Looked in: the research doc's Conclusions/What-would-change, the element-modifiers Working design's Decision 3/Falsifier 5, spec §2.3/§2.4.)*
- **Reversibility.** `Easy` (pre-release) — reversal is re-adding the collection targets + the `count` `ProofSatisfaction` to one catalog arm and re-migrating one sample; no external precept depends on it. Effectively-irreversible only post-external-ship, which has not happened.
- **Blast radius.** Catalogs: `Modifiers.cs` (one arm). Type checker: `BuildElementValueBounds` switch → generic satisfaction-shape binding (Rule 2; behavior-preserving for existing element modifiers, guarded by the shipped Phase-1/2 element-bound tests). Proof engine: one discharge arm (Rule 3). Docs: spec §2.3/§2.4, `collection-types.md` Constraint Catalog + callouts + per-kind discharge lines + `sortedset`-rejection rationale, element-modifiers Working design Decision 3/Falsifier 5. Samples: `shopping-cart.precept` (1 line). Tests: the new compiler stubs. External consumers: none (pre-release).

### Decision 2 — Add a Roslyn analyzer asserting no `ValueModifierMeta.ApplicableTo` spans both a collection and a scalar kind

**Stakes**: medium *(Precept-internal build diagnostic; new analyzer + new diagnostic ID, but no `.precept`-author-visible surface; reversible by deleting the analyzer)*

- **Rationale.** Decision 1 removes the *current* overlap; the analyzer prevents *reintroducing* one. The research's explicit falsifier — "If a second modifier joins the both-scalar-and-collection overlap set … the 'only one collides' premise weakens … Watch the catalog: any new modifier with both a scalar-type target and a collection target in `Modifiers.cs` reopens this." — is exactly the regression the analyzer makes a build-time impossibility. It turns a human "watch the catalog" obligation into a structural guarantee, consistent with Precept's prevention-not-detection commitment applied to its own catalog.
- **Tradeoff accepted.** A future deliberate both-axes modifier (if one is ever justified) must be unblocked by either an allow-list entry or an explicit analyzer change — friction on a deliberate decision. Accepted: that friction is the point (it forces the explicit-scope-syntax question, research Candidate 2/3, to be reopened rather than silently bypassed).
- **Alternatives considered.** *(a) No analyzer, rely on the doc/research "watch the catalog" note*: rejected — a human watch-item is exactly what the research falsifier warns rots; detection not prevention. *(b) A pipeline-stage runtime check*: rejected — wrong layer (the invariant is about catalog source shape, knowable at C# compile time with no `.precept` input; belongs with `Precept0011`/`Precept0027`).
- **Precedent.** `Precept0011ModifiersCrossRef.cs` (catalog cross-arm invariants over the `GetMeta(ModifierKind)` switch — mutex symmetry, subsumes acyclicity, self-reference) and `Precept0027DiagnosticEmissionCoverage.cs` (`RegisterCompilationAction`, error severity, catalog-coverage gate). Roslyn analyzer-SDK separation of analysis from the compiler proper.
- **Sources consulted for this decision.**
  - `src/Precept.Analyzers/Precept0011ModifiersCrossRef.cs:30-126` — `[DiagnosticAnalyzer]`, `RegisterOperationAction(SwitchExpression)`, `TryGetCatalogSwitchKind(... "ModifierKind")`, per-arm + cross-arm checks via `CatalogAnalysisHelpers`.
  - `src/Precept.Analyzers/Precept0027DiagnosticEmissionCoverage.cs:9-48` — descriptor shape, `customTags: CompilationEnd`, `RegisterCompilationAction`, error severity.
  - `src/Precept/Language/Modifiers.cs:40-53` — `CollectionTypes` and `StringAndCollectionTypes` arrays (the kind sets the analyzer classifies against).
  - `research/.../collection-element-aggregate-scope-syntax.md` § What would change this conclusion — the "watch the catalog for a second both-axes modifier" falsifier.
  - **Spec-first settlement check (guard 17):** the analyzer is a Precept-internal build invariant; grepped `precept-language-spec.md`/`collection-types.md` for any prior decision on catalog modifier-axis disjointness — the spec is silent (this is an internal catalog-shape guarantee, not language surface). Legitimate new internal-tooling decision.

## Falsifiers

External-author-visible (Decision 1 flips an existing modifier's behavior). A claim here is wrong if any is observed post-ship:

1. **`set of string notempty` admits an empty-string element** (e.g. an `add`/`default ""` element survives) — falsifies Rule 2: the genericized `BuildElementValueBounds` should set `NotEmpty=true` from `notempty`'s satisfaction shape (folded to `minlength 1` per element). If empties survive, the satisfaction-shape binding is not setting the bound (e.g. the `Accessor("length") > Constant(0)` shape isn't matched).
2. **`set of string notempty` is still interpreted as collection cardinality** (`mincount 1`) rather than as a per-element bind — falsifies Decision 1 / Rule 1 (the retarget didn't take; `notempty` is still being read as a collection-level modifier).
2a. **Meaning-inversion bites in practice** (the no-advisory revisit trigger): authors keep writing `notempty` on a collection *intending* cardinality and silently get the per-element reading — observed as recurring bug reports / corpus mistakes where `set of T notempty` was meant as "≥1 element." This is the watch that would reopen the owner's accept-silent decision (and the advisory question) — it does not falsify a built claim, but it is the documented revisit signal.
3. **A `mincount 1` collection field requires a `count > 0` guard to `.peek`/`.first`** (the access obligation does not discharge from `mincount 1`) — falsifies Rule 3, the required build item; means dropping collection-`notempty` re-opened a totality hole.
4. **A synthetic both-axes `ValueModifierMeta` (a scalar + a collection `TypeKind` in `ApplicableTo`) compiles the analyzer project clean** — falsifies Decision 2 / Rule 4.
5. **A domain expert, after the ≤10-min path, cannot state that `mincount 1` (not `notempty`) is "the collection has at least one element"** — falsifies the Audience teachability claim; the vocabulary-asymmetry framing isn't landing.

## Acceptance criteria (test-shaped)

1. **Per-element `notempty` rejects empty elements (via generic binding).** After `BuildElementValueBounds` is genericized (the per-`ModifierKind` switch replaced by satisfaction-shape binding), `field T as set of string notempty` + an ingress/`default` of `""` → element-bound rejection (the routed `notempty` sets `NotEmpty=true`, folded to `minlength ≥ 1`; compile-time write-proof or runtime governance per the element-modifiers design); `"x"` is clean. Regression guard: the shipped Phase-1/2 element-bound tests (`maxlength`/`min`/`max`/flags) stay green — the genericization is behavior-preserving for them.
2. **`notempty` no longer means collection cardinality.** `field T as set of string notempty` no longer means "≥1 element"; it binds per-element (each element non-empty), and `mincount 1` is the cardinality form. The parser routes `notempty` to the element; an author wanting cardinality who writes `notempty` gets the per-element reading silently — **no diagnostic, no advisory** (owner decision). `set of integer notempty` errors PRE0033 (element-type mismatch).
3. **`mincount 1` discharges access safety.** `field Q as queue of string mincount 1` + `-> set X = Q.peek` (no `when Q.count > 0`) compiles clean; `mincount 0` (or no count modifier) re-emits the `count > 0` access obligation.
4. **Analyzer fails the build on a both-axes modifier.** A test fixture `ValueModifierMeta` with `ApplicableTo` = `[String, Set]` produces `PRECEPT00NN` (error); the real catalog produces none.
5. **Corpus green after migration.** `samples/shopping-cart.precept` migrated; full `dotnet test` corpus + `precept_compile` on all samples returns the pre-change diagnostic set (zero new).
6. **`.first`/`.peek` still discharge under `mincount 1`** for every accessor the spec lists (`.min`/`.max`/`.peek`/`.peekby`/`.first`/`.last`) on the kinds that surface them.

## Dependencies

- **Upstream.** The locked element-modifiers design (`collection-inner-type-value-modifiers-2026-06-02.md`) — its inner-type routing + per-element governance/proof is what makes `set of string notempty` meaningful as an element bound. Owner authorization (this session) for the `notempty` semantic flip.
- **Downstream.** Retargets the element-modifiers design's Supporting Decision 3: **reverses** its collection-`notempty`-coexistence half (owner-authorized) and **resolves** its element-`notempty` half (per-element `notempty` now achieved). Removes its Falsifier 5 risk. Unblocks any future explicit-scope-syntax design (research Candidate 2/3) from having to special-case `notempty`.

## Doc-update enumeration (per CLAUDE.md routing)

| Doc | Update |
|---|---|
| `docs/language/precept-language-spec.md` | §2.4 table (L1669): `notempty` Applicable-to → `string` only; move the collection-non-empty meaning to the `mincount`/`maxcount` row guidance. §2.4 notes: rewrite the "`notempty` on collections" discharge note (L1679) → "`mincount 1` discharges `.min`/`.max`/`.peek`/`.peekby`/`.first`/`.last`/`.at`"; keep `notempty`+`optional` mutex (L1681) for the string case. §2.3 (L1121): update the parenthetical — `notempty` is now scalar-only and *routes to the element*; the "awaits a disambiguator" sentence is resolved. §2.4 flag-modifier row (L1148): `notempty` = "String is non-empty" (drop the collection clause). §631 (the collection-emptiness / non-empty-guarantee statement): reword "discharged by `notempty`" → "discharged by `mincount 1` / a non-empty guarantee." The §0.4 vocabulary list (L595) is unchanged (the keyword still exists). |
| `docs/language/collection-types.md` | Constraint Catalog: remove `notempty` row (or mark it "not a collection modifier — use `mincount 1`"); ensure `mincount N` row notes it discharges access safety for `N ≥ 1`. The "Element value modifiers"/inner-type section: note `set of string notempty` = per-element non-empty. The "Element-level constraints" callout: drop `notempty` from the cardinality list. **The ~10 per-kind "discharged by `notempty`" lines** (e.g. L334-335, L353, L426, L775, L1051-1052, L1066, L1144): reword each to "discharged by `mincount 1` / a non-empty guarantee." **The `sortedset`-rejection rationale** (~L1492, L1539 — "owned by `notempty` alone" / "`set of T notempty` is proof-identical to `sortedset of T notempty`"): reword off `notempty`-as-cardinality to `mincount 1` / a non-empty guarantee (the rejection rationale must not lean on a collection-`notempty` that no longer exists). The "Scalar constraints do not apply to collections" note: `notempty` is now in the scalar-only set. *(Line numbers are pointers — verify against the file at execution; reword by content, not blind offset.)* |
| `docs/compiler/proof-engine.md` | Count-containment / emptiness-discharge strategy: `mincount N` (N ≥ 1) discharges the `count > 0` access obligation (was `notempty`-only). |
| `docs/compiler/type-checker.md` | `notempty` per-type validation now string-only; `BuildElementValueBounds` genericized (per-`ModifierKind` switch → satisfaction-shape binding, so `notempty` binds per-element); `notempty` on a non-string element → `InvalidModifierForType`. |
| `docs/tooling/language-server.md` | Only if hover copy hardcodes `notempty`'s old "string or collection" description. |
| `docs/tooling/mcp.md` | Only if a catalog formatter hardcodes `notempty` applicability text (else automatic from the catalog). |
| `docs/Working/collection-inner-type-value-modifiers-2026-06-02.md` | Supporting Decision 3 (L371-381) — note it is **retargeted**: the collection-`notempty`-coexistence half is *reversed* (owner-authorized, Direction A), the element-`notempty` half is *resolved* (per-element `notempty` achieved via the retarget + generic satisfaction-shape binding, not a per-modifier special case); Falsifier 5 — note the risk is removed (collection `notempty` no longer a valid spelling). *(Working doc; cross-link, not canonical.)* |
| `src/Precept.Analyzers/` README or analyzer index (if one exists) | Register the new `PRECEPT00NN` in any analyzer inventory. |

## Operational dimensions

- **Security** — N/A. No source-text-ingestion surface change; the analyzer reads C# catalog source at build, not untrusted `.precept` input. The parser change is a derived-set widening (`ElementPositionValueTokens`), no new input shape.
- **Observability** — Addressed. A genuine element-type mismatch (`set of integer notempty`) surfaces as PRE0033; the cardinality-confusion case is accepted silently (no diagnostic, by owner decision — the `mincount 1` form is taught via docs/hover, not a diagnostic). The `mincount 1` access discharge surfaces through the same proof-attribution path as `notempty` did (hover/diagnostics show the discharging modifier — spec P4); the discharging-modifier attribution must name `mincount` where it previously named `notempty`.
- **Evolvability** — N/A. No dependency on an external standard (NodaTime/ICU/UCUM/ISO 4217/TZDB).

## Resolved questions

1. **"Author wrote `notempty` meaning cardinality" — resolved: accept silent, NO advisory.** After the retarget + generic binding, `notempty` after an inner type *legally* binds to the element (Rule 2), so `field T as set of string notempty` does **not** error — it means "each element non-empty." A collection author who *meant* "≥1 element" gets a per-element bind, not a diagnostic. **Owner decision (2026-06-03): accept the silent per-element binding; no advisory diagnostic is built.** Rationale: silent per-element binding is the correct, sound behavior, consistent with how every other element modifier binds; a "did you mean `mincount 1`?" heuristic would risk false positives on legitimate per-element `notempty`. The revisit trigger if this call ages badly is the meaning-inversion watch in § Falsifiers (item 2a) — but no advisory is in scope here.
