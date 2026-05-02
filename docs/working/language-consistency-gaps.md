# Language Consistency Gaps

Pre-TypeChecker audit — exhaustive consistency check of language docs, catalogs, and lexer/parser implementation.

**Scope:** `docs/language/`, `src/Precept/Language/` catalogs, `src/Precept/Pipeline/Lexer.cs`, `src/Precept/Pipeline/Parser*.cs`  
**Out of scope:** TypeChecker, runtime evaluator, graph analyzer, LSP, MCP

**Fix policy:**
- Obvious gap → agent rubber-ducks, applies fix, status = **Fixed**
- Non-obvious gap → full analysis written, status = **Unresolved** (owner resolves on second pass)

**Audit status: CLOSED — 42 gaps total, 42 Fixed, 0 Unresolved (GAP-035, GAP-040, GAP-042 resolved 2026-05-02)**

---

## Summary Table

| ID | Title | Category | Status | Iter |
|----|-------|----------|--------|------|
| GAP-001 | `reason` keyword used instead of `because` in collection-types.md | Doc-Spec | Fixed | 1 |
| GAP-002 | Named-rule syntax in business-domain-types.md not in spec grammar | Doc-Spec | Fixed | 1 |
| GAP-003 | `notempty` applicability: spec §Constraint Catalog vs §3.8 conflict | Doc-Spec | Fixed | 1 |
| GAP-004 | `sqrt` signature: spec says `(numeric)` but primitive-types.md says `(number)` | Doc-Spec | Fixed | 1 |
| GAP-005 | `BindingShadowsField` severity: Warning (§3.5 + collection-types) vs Error (§3.10) | Doc-Spec | Fixed | 1 |
| GAP-006 | Spec §1.1 and §1.2 missing 14 new collection-type tokens | Doc-Catalog | Fixed | 2 |
| GAP-007 | Spec §2.1 precedence missing `for` infix; §2.2 ActionStatement missing 6 new actions | Doc-Catalog | Fixed | 2 |
| GAP-008 | Spec §2.3 CollectionType grammar missing 6 new collection type forms | Doc-Catalog | Fixed | 2 |
| GAP-009 | Spec §3.6 member access table missing new collection type accessors and `for` infix | Doc-Catalog | Fixed | 2 |
| GAP-010 | Spec §3.8 action validation table missing 7 new collection action forms | Doc-Catalog | Fixed | 2 |
| GAP-011 | `FunctionKind.Now` ("now()") absent from temporal-type-system.md | Doc-Catalog | Fixed | 3 |
| GAP-012 | Spec §2.7 Parser Diagnostics table missing 4 parse-stage codes | Doc-Catalog | Fixed | 4 |
| GAP-013 | `AmbiguousTypedConstant` message mismatch: spec §3.10 vs catalog | Doc-Catalog | Fixed | 4 |
| GAP-014 | `EventHandlerInStatefulPrecept` message mismatch: spec §3.10 vs catalog | Doc-Catalog | Fixed | 4 |
| GAP-015 | Three choice parse-stage codes placed in spec §3.10 (Type Checker section) | Doc-Catalog | Fixed | 4 |
| GAP-016 | Spec §3.10 incomplete — ~30 catalog codes undocumented in Diagnostic Catalog | Doc-Catalog | Fixed | 4 |
| GAP-017 | `Arrow` (`->`) categorized as `Structural` in `Tokens.cs` but spec §1.1 places it in the Operators table | Catalog-Impl | Fixed | 5 |
| GAP-018 | Spec §1.1 `NumberLiteral` row description omits exponent notation documented in §1.3 and implemented in the lexer | Doc-Spec | Fixed | 5 |
| GAP-019 | `UnexpectedKeyword` (11) and `InvalidCallTarget` (12) listed in spec §2.7 as active parser diagnostics but never emitted by the parser | Doc-Impl | Fixed | 6/7 |
| GAP-020 | `contains` associativity: spec §2.1 says `left`, `Operators.cs` enforces `NonAssociative`; left-denotation binding was ParseExpression(40) instead of ParseExpression(41) | Doc-Catalog | Fixed | 6 |
| GAP-025 | GAP-003 incomplete — `Modifiers.cs` `Notempty` applicability still `StringOnly`; spec §3.8 requires string + 8 collection types | Doc-Catalog | Fixed | 7 |
| GAP-026 | `Modifiers.cs` `CollectionTypes` array stale — `mincount`/`maxcount` applicability missing 6 new TypeKind members | Doc-Catalog | Fixed | 7 |
| GAP-027 | `Tokens.cs` `Notempty` description reads "String constraint: non-empty" but spec §1.1 says "String or collection constraint: non-empty" | Doc-Catalog | Fixed | 7 |
| GAP-028 | `Functions.cs` `sqrt` has `Integer` and `Decimal` overloads but spec §3.7 explicitly says integer/decimal inputs are type errors | Doc-Catalog | Fixed | 7 |
| GAP-029 | `IsOutcomeAhead()` hardcodes `{Transition, No, Reject}` instead of deriving from `TokenCategory.Outcome` | Catalog-Impl | Fixed | 8 |
| GAP-030 | `ParseAtom` hardcodes `case TokenKind.Min: case TokenKind.Max:` for keyword-as-function-name instead of deriving from `Functions.ByName` ∩ `Tokens.Keywords` | Catalog-Impl | Fixed | 8 |
| GAP-031 | Unary/postfix binding powers hardcoded in `Parser.Expressions.cs` (`not`→25, negate→65, `is set`→60) instead of reading from `Operators.ByToken`/`ByTokenSequence` | Catalog-Impl | Fixed | 8 |
| GAP-032 | `Functions.cs` `pow(integer, integer)` overload missing `ProofRequirement` for `exp >= 0`; spec §0.6 item 4 explicitly lists this alongside `sqrt` as a non-negative proof obligation | Doc-Catalog | Fixed | 8 |
| GAP-033 | `ModifierKind.cs` `Notempty` XML doc comment reads "Flag: string is non-empty" — stale after GAP-025 expanded applicability to string + 8 collection types (analogous to GAP-027 in Tokens.cs) | Doc-Catalog | Fixed | 8 |
| GAP-034 | `ParseTypeRef` hardcodes `is TokenKind.Set or TokenKind.QueueType or …` (6-kind collection test) instead of deriving from `Types.All.Where(Collection && not Lookup)` | Catalog-Impl | Fixed | 9 |
| GAP-035 | `ParseChoiceValue` hardcodes `is TokenKind.IntegerType or TokenKind.DecimalType or TokenKind.NumberType` for "numeric choice type" — no catalog property captures this semantic | Catalog-Impl | Fixed | 9 |
| GAP-036 | Spec §3.8 `clear F` action validation table omits `optional` fields; §1.2 explicitly names `clear` as the removal mechanism for optional fields | Doc-Doc | Fixed | 9 |
| GAP-037 | `Modifiers.cs` `Writable` hover description references retired `write` verb: "in State write Field" should be "in State modify Field editable/readonly" | Doc-Catalog | Fixed | 9 |
| GAP-038 | Spec §2.1 left-denotation table for `For` shows `ParseExpression(40)` but left-associative at level 40 requires `ParseExpression(41)`; implementation is correct | Doc-Impl | Fixed | 9 |
| GAP-039 | Spec §3.8 `append F Expr` and `enqueue F Expr` rows include `log of T by P` and `queue of T by P` as valid targets, but catalog excludes these — explicit key form required | Doc-Catalog | Fixed | 9 |
| GAP-040 | `Types.cs` `BagAccessors.countof` has `ParameterType: TypeKind.Integer` but spec says `countof(E)` takes element `E` of type `T` (the bag's element type), not integer | Doc-Catalog | Fixed | 9 |
| GAP-041 | Spec §3.8 quantifier predicate validation table names `TypeMismatch` for non-boolean predicate but dedicated `QuantifierPredicateNotBoolean` (code 106) exists in both catalog files | Doc-Catalog | Fixed | 9 |
| GAP-042 | Parser `ParseActionStatement` dispatch has dead branches for `CollectionValueBy`, `CollectionIntoBy`, and `RemoveAtIndex`; variant actions are excluded from `Actions.ByTokenKind` so these shapes never reach dispatch | Catalog-Impl | Fixed | 9 |
| GAP-021 | `is set`/`is not set` associativity: spec §2.1 says `left`, catalog says `NonAssociative (postfix)` | Doc-Impl | Fixed | 6 |
| GAP-022 | Spec §2.1 null-denotation table names `StringLiteralExpression` — a node that does not exist; implementation uses `LiteralExpression` | Doc-Impl | Fixed | 6 |
| GAP-023 | Spec §2.1 precedence table has `is` row (60) appearing before `+/-` row (50); two rows at level 60 without ordering explanation | Doc-Spec | Fixed | 6 |
| GAP-024 | `bag of T`, `list of T`, and `log of T` forms lack `TypeQualifier?` in spec §2.3 grammar but the parser accepts element-type qualifiers on all collection types | Doc-Impl | Fixed | 6 |

---

## Gap Entries

---

## GAP-001: `reason` keyword used instead of `because` in collection-types.md

**Status:** Fixed  
**Category:** Doc-Spec  
**Location:** `docs/language/collection-types.md` lines 899, 1147, 1154, 1259  
**Found in iteration:** 1

**Description:**  
Four rule examples in `collection-types.md` use `reason "..."` as the clause introducing the rule rationale:

```precept
rule CartItems.countof("hazmat-item") <= 3
    reason "No more than 3 hazardous items per order"
```

The spec unambiguously mandates the `because` keyword. Principle 9 (§0.1) states:
> "Every constraint carries a mandatory reason. … the `because` clause is syntactically required on every rule and ensure."

The spec grammar (§2.2 `RuleDeclaration`) is `rule BoolExpr because StringExpr`. The `catalog-system.md` ConstructSlotKind enum names the slot `BecauseClause` — not `ReasonClause`. Every other example in every other doc (primitive-types.md, temporal-type-system.md, business-domain-types.md, all samples) uses `because`. The four occurrences in collection-types.md are isolated errors.

**Rubber-Duck Analysis:**  
The word "reason" is semantically synonymous with "because" in plain English, which explains the typo. The spec is crystal-clear: `because` is the keyword. There is zero ambiguity. The fix is safe and surgical — a mechanical word substitution in four code fence blocks. No semantic change; no risk.

**Resolution:**  
Applied fix in `docs/language/collection-types.md` — changed `reason` to `because` at lines 899, 1147, 1154, 1259.

---

## GAP-002: Named-rule syntax in business-domain-types.md not in spec grammar

**Status:** Fixed  
**Category:** Doc-Spec  
**Location:** `docs/language/business-domain-types.md` lines 636–651  
**Found in iteration:** 1

**Description:**  
Two example blocks used `rule Name\n  BoolExpr` (named-rule form, no `because`) — not valid per spec grammar `rule BoolExpr ("when" BoolExpr)? because StringExpr`.

**Rubber-Duck Analysis:**  
`because` is syntactically mandatory per spec Principle 9 and the `RuleDeclaration` grammar. The named-rule form (`rule Identifier`) does not exist in the spec. The examples were informal documentation labels that leaked into code fences.

**Owner judgment:** _"because is mandatory, this is an error in the doc."_ — Named rules are not a planned syntax feature. The examples should conform to spec grammar.

**Resolution:**  
Fixed per owner judgment. Both examples rewritten to valid `rule BoolExpr because StringExpr` form:
- Pattern 2: `rule Measurement.dimension == ExpectedDimension` + `because "Measurement dimension must match the expected dimension"`
- Pattern 3: `rule Input.dimension == Output.dimension` + `because "Input and output must have the same dimension"`

```precept
rule MeasurementDimensionConsistency
  Measurement.dimension == ExpectedDimension
```

```precept
rule InputOutputDimensionMatch
  Input.dimension == Output.dimension
```

The spec's `RuleDeclaration` grammar (§2.2) is:

```
rule BoolExpr because StringExpr
```

This grammar has no identifier slot for a rule name. The spec's `ConstructSlotKind` enum (catalog-system.md) lists `BecauseClause` as a slot but has no `RuleName` slot. Principle 9 mandates `because` as syntactically required. Both examples violate two distinct spec invariants: (1) using an unrecognized name-form, and (2) omitting the mandatory `because` clause.

**Rubber-Duck Analysis:**  
Two interpretations are possible:

1. **Error in the doc**: The author wrote these as documentation labels, intending to show conceptual examples. The actual syntax should be `rule Measurement.dimension == ExpectedDimension because "Measurement must match expected dimension"`. The "name" is just a heading-like comment that leaked into the code fence.

2. **Planned language feature**: Named rules may be a design intent for the business-domain types proposal — not yet written into the spec, but anticipated. In this interpretation, the doc is ahead of the spec.

Because business-domain-types.md is a proposal document ("Implementation state: Proposal — not yet implemented"), interpretation 2 is plausible. However, the spec wins per the fix policy, and the spec does not define named rules. Fixing these examples by removing the name and adding `because` clauses would be safe for interpretation 1 but would erase a design intent if interpretation 2 is correct.

**Resolution:**  
Needs owner judgment. Owner should confirm: (a) are named rules a planned syntax feature for business-domain types? If no, fix the examples to match spec grammar. If yes, document the named-rule grammar in the spec first, then update these examples to match.

---

## GAP-003: `notempty` applicability: spec §Constraint Catalog vs §3.8 conflict

**Status:** Fixed  
**Category:** Doc-Spec  
**Location:**  
- `docs/language/precept-language-spec.md` §Constraint Catalog (narrow) vs §3.8 Modifier Validation (broad)  
- `docs/language/primitive-types.md` §Constraint Catalog (matched narrow)  
- `docs/language/collection-types.md` §Constraint Catalog (matched broad)  
- `docs/language/catalog-system.md` §Modifiers (matched narrow)  
**Found in iteration:** 1

**Description:**  
The spec's Constraint Catalog section stated `notempty | string only` while spec §3.8 listed `string` + 8 collection kinds. The spec contradicted itself; downstream docs split on the same fault line.

**Rubber-Duck Analysis:**  
The Constraint Catalog section was written when only primitive types existed. §3.8 was extended when collection types were designed. §3.8 is the authoritative, more recent statement.

**Owner judgment:** _"yes, notempty is valid on collections"_

**Resolution:**  
Fixed per owner judgment. Four locations updated:
- `precept-language-spec.md` §2.4 token description: `String constraint: non-empty` → `String or collection constraint: non-empty`
- `precept-language-spec.md` §Constraint Catalog flag description: added collection meaning `equivalent to mincount 1`
- `primitive-types.md` §Constraint Catalog `notempty` row: scope extended to list collection types with `mincount 1` equivalence note and cross-ref to collection-types.md
- `catalog-system.md` §Field modifier applicability: `notempty applies to String` → lists all 8 applicable types including collection kinds

**Description:**  
The spec's Constraint Catalog section states:

```
| `notempty` | `string` only | `.length > 0` |
```

But the spec's §3.8 Modifier Validation table states `notempty` applies to:

> `string`, `set`, `queue`, `stack`, `log`, `log of T by P`, `bag`, `list`, `queue of T by P`

The spec contradicts itself: the Constraint Catalog says string-only; §3.8 says string + 8 collection kinds.

Downstream docs split on this fault line:

| Doc | `notempty` scope |
|-----|-----------------|
| `primitive-types.md` Constraint Catalog | `string` only — matches spec §Constraint Catalog |
| `collection-types.md` Constraint Catalog | `set`, `queue`, `stack`, `log`, `bag`, `list`, `queue of T by P` — matches spec §3.8 |
| `catalog-system.md` Modifiers section | `String` only — matches spec §Constraint Catalog |

The `collection-types.md` account is narrower than §3.8 in one direction: it does not list `lookup of K to V` as a target for `notempty`, which is consistent with both docs (lookup is never listed as `notempty`-eligible anywhere).

**Rubber-Duck Analysis:**  
The spec Constraint Catalog is almost certainly the stale version. It was written when only primitive types existed; §3.8 was extended when collection types were added. The §3.8 entry is the authoritative, more detailed statement.

The fix path is:
1. Update the spec's Constraint Catalog section to match §3.8 (add the collection types to the `notempty` row).
2. Update `primitive-types.md` Constraint Catalog to note that `notempty` applies to `string` among primitive types (unchanged in scope) but acknowledge the collection extension via a cross-reference.
3. Update `catalog-system.md` Modifiers text to list `Set`, `Queue`, `Stack` (and future collection types) alongside `String` in `notempty`'s `ApplicableTo`.

However, the spec is ground truth per fix policy — it should not be edited without owner confirmation of the intent. The correct fix in the type docs depends on which spec interpretation wins.

**Resolution:**  
Needs owner judgment. Owner should confirm that §3.8 is the authoritative `notempty` scope (not the Constraint Catalog section), then update: (1) spec §Constraint Catalog row for `notempty`, (2) `catalog-system.md` Modifiers text, and optionally (3) `primitive-types.md` scope clarification.

---

## GAP-004: `sqrt` signature: spec says `(numeric)` but primitive-types.md says `(number)`

**Status:** Fixed  
**Category:** Doc-Spec  
**Location:**  
- `docs/language/precept-language-spec.md` §3.7 Built-in Function Catalog  
- `docs/language/primitive-types.md` §Built-in Functions  
**Found in iteration:** 1

**Description:**  
Spec §3.7 had `sqrt(value) | (numeric) → number` in the Signature column while its own Constraints column said "Number-lane only." `primitive-types.md` correctly wrote `(number) → number`. The spec's Signature and Constraints columns contradicted each other.

**Rubber-Duck Analysis:**  
`Math.Sqrt` in .NET takes `double`. `decimal` has no `Math.Sqrt` overload and doesn't implement `IFloatingPoint<T>`. `integer` would need lossy widening to `double`. The `(numeric)` shorthand in the Signature column was stale early-draft text not updated when the "Number-lane only" constraint was added.

**Owner judgment:** _"the intent was to limit sqrt to number only"_ — confirmed by .NET: `Math.Sqrt` is `double`-only; no native `decimal` sqrt path exists.

**Resolution:**  
Fixed. Spec §3.7 `sqrt` Signature column updated from `(numeric) → number` to `(number) → number`. Constraints column expanded to explain why `decimal` and `integer` are type errors (no .NET overload; use `approximate(value)` to convert). `primitive-types.md` was already correct — no change needed there.

**Description:**  
The spec §3.7 function catalog entry for `sqrt`:

```
| `sqrt(value)` | (numeric) → number | number | Number-lane only; proof engine checks non-negativity |
```

The formal Signature column says `(numeric)`. In the spec's type system, `numeric` is a supertype union covering `integer`, `decimal`, and `number`. If taken literally, `sqrt(42)` and `sqrt(3.14)` would be valid calls on integer and decimal inputs.

But the Constraints column immediately says **"Number-lane only"**. This means only `number`-typed arguments are accepted. `sqrt(decimal)` and `sqrt(integer)` are compile errors.

`primitive-types.md` §Built-in Functions writes the signature as `(number) → number`, which is unambiguous and consistent with the "Number-lane only" constraint:

```
| `sqrt(value)` | `(number) → number` | `number` | **Number-lane only.** `sqrt(decimal)` is a type error — use `sqrt(approximate(value))`. Proof engine checks non-negativity. |
```

**Rubber-Duck Analysis:**  
The spec §3.7 Signature column uses `(numeric)` as a shorthand meaning "a numeric type" but then restricts via the Constraints column. This is an internal ambiguity in the spec's own format: the formal signature and the constraint note say different things.

`primitive-types.md` resolves the ambiguity in favor of precision: `(number) → number`. This is correct — the "Number-lane only" invariant means only `number`-typed values reach `sqrt`. The spec's `(numeric)` in the Signature column appears to be an early-draft shorthand that was not updated when the "Number-lane only" constraint was added.

The risk of fixing `primitive-types.md` to say `(numeric)` (to match spec literally) would introduce confusion: the type doc would then say `sqrt` accepts `decimal`, which contradicts the "Number-lane only" text in both docs. The risk of fixing the spec to say `(number)` is that someone might interpret it as a breaking change. In practice it is not a change — it's a clarification.

**Resolution:**  
Needs owner judgment. Preferred fix: update the spec §3.7 `sqrt` Signature column from `(numeric)` to `(number)` to match the Constraints column and `primitive-types.md`. No behavior change — just aligning the formal signature with the stated constraint.

---

## GAP-005: `BindingShadowsField` severity: Warning (§3.5 + collection-types) vs Error (§3.10)

**Status:** Fixed  
**Category:** Doc-Spec  
**Location:**  
- `docs/language/precept-language-spec.md` §3.5 (Scope Rules) line 1147 — was Warning  
- `docs/language/precept-language-spec.md` §3.10 Diagnostic Catalog code 103 — Error (already correct)  
- `docs/language/collection-types.md` §Quantifiers Shadowing row line 771 — was Warning  
**Found in iteration:** 1

**Description:**  
Spec §3.5 and `collection-types.md` said Warning; spec §3.10 Diagnostic Catalog said Error. The spec contradicted itself.

**Rubber-Duck Analysis:**  
A shadowed field is invisible inside the predicate — a structural trap. Precept's philosophy is prevention over detection. Warning was inconsistent with the §3.10 catalog entry and with the broader language philosophy.

**Owner judgment:** _"this should be an error"_ — shadowing a field name with a binding variable is prevented, not merely warned about. §3.10 Error entry was correct; §3.5 text and collection-types.md were stale.

**Resolution:**  
Fixed. Two locations updated to Error:
- `precept-language-spec.md` §3.5 Scope Rules shadowing row: "Warning: `BindingShadowsField`" → "Error: `BindingShadowsField` — rename the binding to avoid confusion"
- `collection-types.md` §Quantifiers Shadowing row: same update
- §3.10 Diagnostic Catalog (code 103) was already correct — no change needed.

**Description:**  
The spec §3.5 (Scope Rules) describes binding variable shadowing:

> If a field with the same name exists at global scope, the binding variable shadows it inside the predicate. **Warning:** `BindingShadowsField`.

But the spec §3.10 Diagnostic Catalog lists `BindingShadowsField` as:

```
| BindingShadowsField (103) | Error | "Binding variable '{0}' shadows a field with the same name — rename the binding to avoid confusion." |
```

`collection-types.md` §Quantifiers Shadowing row:

```
| Shadowing | If a field with the same name exists at global scope, the binding variable shadows it inside the predicate. Warning emitted: BindingShadowsField. |
```

`collection-types.md` aligns with §3.5 (Warning) and conflicts with §3.10 (Error). The spec itself is inconsistent.

**Rubber-Duck Analysis:**  
This is a classic two-location update that missed a sync. §3.5 was likely written first with "Warning" as the intended severity — the author wanted the diagnostic to be advisory, not a hard stop. §3.10 was filled in later and the severity was incorrectly set to Error. Or vice versa: §3.10 was promoted to Error during a review and §3.5 text wasn't updated.

The semantic question: should a binding variable that shadows a field be a Warning or an Error? 

Arguments for Warning: Shadowing is legal and sometimes intentional. The binding variable has well-defined semantics (it is always the collection element, never the shadowed field). A Warning informs authors without blocking compilation.

Arguments for Error: The spec principle 1 (prevention, not detection) suggests that surprising behavior (shadowing causing the field to be inaccessible inside the predicate) should be prevented. An Error forces the author to rename, eliminating the ambiguity.

Neither argument is clearly wrong. The correct severity is an owner decision, not an agent-resolvable mechanical fix. `collection-types.md` should be updated once the spec's intent is clarified.

**Resolution:**  
Needs owner judgment. Owner should confirm the intended severity, update the spec to use one consistent severity in both §3.5 and §3.10, then update `collection-types.md` to match.

---

## GAP-006: Spec §1.1 and §1.2 missing 14 new collection-type tokens

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `docs/language/precept-language-spec.md` §1.1 (token vocabulary tables) and §1.2 (reserved keyword list)  
**Found in iteration:** 2

**Description:**  
`TokenKind.cs` and `Tokens.cs` define 14 tokens introduced with the new collection types (`spike/Precept-V2`). None appear in the spec's §1.1 keyword-category tables, and none are in the §1.2 reserved keyword list:

| TokenKind member | Keyword text | Catalog category | Catalog description |
|---|---|---|---|
| `BagType` | `bag` | Type | Bag collection type |
| `ListType` | `list` | Type | List collection type |
| `LogType` | `log` | Type | Log collection type |
| `LookupType` | `lookup` | Type | Lookup collection type |
| `By` | `by` | Preposition | Ordering key preposition |
| `At` | `at` | Preposition | Index position preposition |
| `Ascending` | `ascending` | Declaration | Ascending sort order |
| `Descending` | `descending` | Declaration | Descending sort order |
| `Append` | `append` | Action | Log/list append action |
| `Insert` | `insert` | Action | List insert action |
| `Put` | `put` | Action | Lookup put action |
| `For` | `for` | Preposition | Lookup key access infix operator |
| `Countof` | `countof` | Constraint | Bag element count accessor |
| `Peekby` | `peekby` | Constraint | Priority queue ordering-key peek accessor |

The spec §1.2 reserved keyword list (lines 434–449) is missing all 14. The v2-additions note at line 451 lists `each` as a v2 addition but does not mention any of the 14 above. These tokens ARE fully documented in `collection-types.md §Token vocabulary` (lines 559–586) and are correctly present in the C# catalog.

**Rubber-Duck Analysis:**  
The spec §1.1 and §1.2 were written to cover the original token vocabulary and were not updated when the new collection types were added to the catalog. The collection-types.md document has its own "Token vocabulary" section which serves as a de-facto supplement to the spec's §1.1 tables. The split documentation is workable but inconsistent with the spec's stated role as the authoritative token vocabulary table.

The fix is mechanical but wide: add new rows to the appropriate §1.1 category tables (Keywords: Types for BagType/ListType/LogType/LookupType; Keywords: Prepositions for By/At/For; Keywords: Declaration for Ascending/Descending; Keywords: Actions for Append/Insert/Put; and a new member-name category or footnote for Countof/Peekby), then add all 14 to the §1.2 keyword list with a "v2 additions" note. The content is fully known from the catalog and collection-types.md — no design judgment is needed for the content itself, only for the placement and formatting conventions within the existing spec structure.

**Resolution:**  
Fixed in iteration 3. **Owner judgment:** _"a miss when collections were added"_ — spec §1.1 and §1.2 were not back-ported when collection types were implemented. Mechanical update. Added:
- `Ascending`, `Descending` to Keywords: Declaration table
- `By`, `At`, `For` to Keywords: Prepositions table
- `Append`, `Insert`, `Put` to Keywords: Actions table
- `Countof`, `Peekby` to Keywords: Constraints table
- `BagType`, `ListType`, `LogType`, `LookupType` to Keywords: Types table
- All 14 added to §1.2 reserved keyword block; v3-additions note expanded to list them explicitly.

---

## GAP-007: Spec §2.1 expression precedence and §2.2 ActionStatement grammar not updated for new collection features

**Status:** Fixed  
**Category:** Doc-Catalog
**Location:**  
- `docs/language/precept-language-spec.md` §2.1 left-denotation table (approx. line 716)  
- `docs/language/precept-language-spec.md` §2.2 `ActionStatement` grammar (approx. line 804)  
**Found in iteration:** 2

**Description:**  
**§2.1 — `for` infix operator absent from expression precedence:**  
The spec §2.1 left-denotation table lists: `Or`, `And`, comparison operators, `Contains`, `Is`, arithmetic operators, `.` (member access), `(` (function call). The `for` infix operator — used for lookup key access (`LookupField for KeyExpr`) — does not appear anywhere in the §2.1 precedence table or left-denotation production table. `TokenKind.For` (128, "for") is in the catalog with category `Preposition` and description "Lookup key access infix operator". `collection-types.md` documents it at line 580 and the Accessor Summary at line 864: `| F for K | ✗|✗|✗|✗|✗|✗|✗|✓ | V | F contains K |`.

**§2.2 — ActionStatement grammar missing 6 action forms:**  
The spec §2.2 `ActionStatement` production (lines 804–811) lists only the original 8 actions:
```
ActionStatement  :=  set Identifier "=" Expr
                  |  add Identifier Expr
                  |  remove Identifier Expr
                  |  enqueue Identifier Expr
                  |  dequeue Identifier ("into" Identifier)?
                  |  push Identifier Expr
                  |  pop Identifier ("into" Identifier)?
                  |  clear Identifier
```
Missing from this production are 6 actions documented in `collection-types.md §Action Summary` (lines 826–847) and in the catalog (`TokenKind.Append`, `TokenKind.Insert`, `TokenKind.Put`):

| Action | For | Example |
|---|---|---|
| `append F Expr` | `log of T`, `list of T` | `append AuditTrail entry` |
| `append F Expr by P` | `log of T by P` | `append ComplianceLog entry by timestamp` |
| `enqueue F Expr by P` | `queue of T by P` | `enqueue ClaimQueue item by priority` |
| `insert F Expr at N` | `list of T` | `insert ApprovalChain name at 0` |
| `remove F at N` | `list of T` (by index) | `remove ApprovalChain at 0` |
| `put F K = V` | `lookup of K to V` | `put CoverageLimits "fire" = 500000` |

All six action tokens are in the catalog. `collection-types.md §Action Summary` is the authoritative reference.

**Rubber-Duck Analysis:**  
The spec §2.1 and §2.2 were written before the new collection types were designed. The collection-types.md document was added as the canonical reference for the new collection surface, but the corresponding spec sections were not updated. The spec's §3 (type checker) was partially updated (modifier validation, contains, notempty tables all reference the new types) but §2 (parser grammar) was not.

For `for`: its precedence level needs to be between `.` (80) and `contains` (40) — it reads as `F for K` where F is a lookup field reference and K is a key expression, so it should be higher than `contains`. The exact precedence is a design decision.

For the action grammar: the productions are straightforward additions. `append`, `insert at`, `put`, `enqueue by`, `remove at` syntax is documented in collection-types.md with full behavioral detail.

**Resolution:**  
Fixed in iteration 3. **Owner judgment:** _"a miss when collections were added"_ — spec §2.1 and §2.2 were not back-ported when collection types were implemented. Mechanical update.
- §2.1 precedence table: added `for` at precedence 40 (same as `contains`), left-associative; added `For` row to left-denotation table with `BinaryExpression(LookupAccess, ...)` production.
- §2.2 ActionStatement grammar: added 7 new action forms: `remove F at N`, `enqueue F Expr by Expr`, `dequeue F (into G)? (by H)?`, `append F Expr`, `append F Expr by Expr`, `insert F Expr at N`, `put F K = V`.

---

## GAP-008: Spec §2.3 CollectionType grammar missing 6 new collection type forms

**Status:** Fixed  
**Category:** Doc-Catalog
**Location:** `docs/language/precept-language-spec.md` §2.3 (Type References), approximately line 919  
**Found in iteration:** 2

**Description:**  
The spec §2.3 `TypeRef` production defines `CollectionType` as:

```
CollectionType  :=  (set | queue | stack) of ScalarType TypeQualifier?
```

This grammar only covers the original three collection types. The catalog has 6 additional TypeKind members for collection types — each with a corresponding token and documentation in `collection-types.md §Overview`:

| TypeKind member | Grammar form | Tokens involved |
|---|---|---|
| `Log` | `log of ScalarType` | LogType, Of |
| `LogBy` | `log of ScalarType by ScalarType` | LogType, Of, By |
| `Bag` | `bag of ScalarType` | BagType, Of |
| `List` | `list of ScalarType` | ListType, Of |
| `QueueBy` | `queue of ScalarType by ScalarType DirectionModifier?` | QueueType, Of, By, Ascending/Descending |
| `Lookup` | `lookup of ScalarType to ScalarType` | LookupType, Of, To |

The `collection-types.md §Overview` (lines 34–42) has the correct grammar:
```
CollectionType  :=  (set | queue | stack) of ScalarType
                |   bag of ScalarType
                |   list of ScalarType
                |   log of ScalarType
                |   log of ScalarType by ScalarType
                |   queue of ScalarType by ScalarType DirectionModifier?
                |   lookup of ScalarType to ScalarType
DirectionModifier := ascending | descending
```

The `ScalarType` production in §2.3 is also stale: it excludes `bag`, `list`, `log`, `lookup` from the collection type alternatives (they belong in `CollectionType`, not `ScalarType`, but the CollectionType production needs them).

**Rubber-Duck Analysis:**  
The spec §2.3 was written when only `set`, `queue`, and `stack` existed. `collection-types.md §Overview` was added as the canonical grammar reference for the new types. The spec §2.3 was not updated. `TypeKind.cs` has Log (27), LogBy (28), Bag (29), List (30), QueueBy (31), Lookup (32) — all with complete metadata in `Types.cs`. The grammar content is known and correct in `collection-types.md`.

**Resolution:**  
Fixed in iteration 3. **Owner judgment:** _"a miss when collections were added"_ — spec §2.3 `CollectionType` production was not back-ported when collection types were implemented. Mechanical update.Replaced the single-line production with a 7-alternative grammar covering all TypeKind members (`set/queue/stack`, `bag`, `list`, `log`, `log by P`, `queue by P DirectionModifier?`, `lookup of K to V`) and added the `DirectionModifier` production.

---

## GAP-009: Spec §3.6 member access table missing new collection type accessors; `for` infix accessor absent

**Status:** Fixed  
**Category:** Doc-Catalog
**Location:** `docs/language/precept-language-spec.md` §3.6, "Collection and core accessors" table (approximately line 1280)  
**Found in iteration:** 2

**Description:**  
The spec §3.6 "Collection and core accessors" table lists only:

```
| set of T      | count, min, max  |
| queue of T    | count, peek      |
| stack of T    | count, peek      |
| string        | length           |
```

The catalog (`Types.cs`) defines accessors for all new collection types. These are documented in `collection-types.md §Accessor Summary` (lines 851–865) but absent from the spec:

| Collection type | Accessors | Returns | Proof requirement |
|---|---|---|---|
| `log of T` | `.first`, `.last` | `T` | `.count > 0` |
| `log of T` | `.at(N)` | `T` | `N >= 0 and N < F.count` |
| `log of T by P` | `.first`, `.last` | `T` | `.count > 0` |
| `log of T by P` | `.at(N)` | `T` | `N >= 0 and N < F.count` |
| `bag of T` | `.countof(E)` | `integer` | None |
| `list of T` | `.first`, `.last` | `T` | `.count > 0` |
| `list of T` | `.at(N)` | `T` | `N >= 0 and N < F.count` |
| `queue of T by P` | `.peek` | `T` | `.count > 0` |
| `queue of T by P` | `.peekby` | `P` | `.count > 0` |
| `lookup of K to V` | `F for K` (infix) | `V` | `F contains K` |

`TokenKind.Countof` (137) and `TokenKind.Peekby` (138) are catalog entries with `IsValidAsMemberName: true`; both are documented in `collection-types.md §Accessor Summary`. The `for` infix accessor (`F for K`) uses `TokenKind.For` (136); it appears in `collection-types.md` §Emptiness Safety (line 643) and §Accessor Summary (line 864) but is absent from the spec §3.6 member access section entirely — not just missing from the table but missing from the surrounding prose too.

**Rubber-Duck Analysis:**  
The spec §3.6 "Member access" section was written before the new collection types existed. The `contains` operator table (lines 1239–1253) and the `notempty` modifier table (lines 1412–1419) were updated when new types were added, but the member access table was not. This is a partial update — §3.6 needs a symmetric update to the member access table.

The `for` infix accessor is especially notable: unlike `.count`, `.peek`, etc., which use dot notation, `F for K` is an infix binary syntax. Its omission from the spec is both a §3.6 table gap and a §2.1 expression precedence gap (covered in GAP-007).

**Resolution:**  
Fixed in iteration 3. **Owner judgment:** _"a miss when collections were added"_ — spec §3.6 member access table was not back-ported when collection types were implemented. Mechanical update.Expanded the table from 4 rows to 27 rows, adding:
- `queue of T by P` accessors: `count`, `peek`, `peekby` (returns P)
- `bag of T` accessors: `count`, `countof(E)` (returns integer)
- `list of T` accessors: `count`, `first`, `last`, `at(N)`
- `log of T` accessors: `count`, `first`, `last`, `at(N)`
- `log of T by P` accessors: `count`, `first`, `last`, `at(N)`
- `lookup of K to V` accessor: `count`
- Added a blockquote note documenting the `F for K` infix operator (lookup key access, not a dot-member).
- Table extended to 4 columns (Object type, Member, Result type, Proof).

---

## GAP-010: Spec §3.8 action statement validation table missing new collection actions

**Status:** Fixed  
**Category:** Doc-Catalog
**Location:** `docs/language/precept-language-spec.md` §3.8, "Action statement validation" table (approximately line 1434)  
**Found in iteration:** 2

**Description:**  
The spec §3.8 "Action statement validation" table (lines 1434–1446) lists only:

```
| set F = Expr     | Any scalar         | Assignable to field type |
| add F Expr       | set of T           | T                        |
| remove F Expr    | set of T           | T                        |
| enqueue F Expr   | queue of T         | T                        |
| dequeue F (into G)? | queue of T      | —                        |
| push F Expr      | stack of T         | T                        |
| pop F (into G)?  | stack of T         | —                        |
| clear F          | Any collection     | —                        |
```

The catalog has `TokenKind.Append` (132), `TokenKind.Insert` (133), `TokenKind.Put` (134) — all with `Cat_Act` category. The `collection-types.md §Action Summary` (lines 826–847) documents 7 additional action forms that are absent from the spec §3.8 table:

| Action | Field type required | Value type | Proof |
|---|---|---|---|
| `append F Expr` | `log of T` or `list of T` | `T` | None |
| `append F Expr by P` | `log of T by P` | `T`; `P` = ordering type | `not (F contains P)` |
| `enqueue F Expr by P` | `queue of T by P` | `T`; `P` = ordering type | None |
| `dequeue F (into G)? (by H)?` | `queue of T by P` | — | `.count > 0` |
| `insert F Expr at N` | `list of T` | `T`; `N` = integer | index-bounds |
| `remove F at N` | `list of T` (by index) | — | index-bounds |
| `put F K = V` | `lookup of K to V` | `K`; `V` = value type | None |

Additionally, `remove F Expr` on a `bag of T` decrements the element count (not removes-if-present like set), but the spec table only documents `remove F Expr` as a `set of T` operation. This is a coverage gap — `remove` also applies to `bag` with different semantics, and to `list of T` (first-occurrence removal).

**Rubber-Duck Analysis:**  
The spec §3.8 action validation table was written for the original three collection types. When new collection types were added, the table was not extended. The `contains` table and modifier validation table in §3.8 were updated for the new types, so this is a partial update. `collection-types.md §Action Summary` and the per-type action tables are the authoritative reference for the new action semantics.

**Resolution:**  
Fixed in iteration 3. **Owner judgment:** _"a miss when collections were added"_ — spec §3.8 action validation table was not back-ported when collection types were implemented. Mechanical update.Updated existing rows and added new rows:
- `add`: now documents `bag of T` as well as `set of T`
- `remove F Expr`: now documents `set of T`, `bag of T`, `list of T` (first-occurrence), and `lookup of K to V` (remove by key)
- `remove F at N` (new): `list of T`, removes by zero-based index
- `enqueue`: updated to cover both `queue of T` and `queue of T by P`
- `enqueue F Expr by Expr` (new): `queue of T by P`, explicit ordering key
- `dequeue`: updated with optional `by H` for priority-queue targeted dequeue
- `clear`: corrected — not valid on `log of T`, `log of T by P`, or `lookup of K to V` (was "Any collection")
- `append F Expr` (new): `log of T`, `log of T by P`, `list of T`
- `append F Expr by Expr` (new): `log of T by P`, explicit ordering key
- `insert F Expr at N` (new): `list of T`, zero-based index insert
- `put F K = V` (new): `lookup of K to V`, upsert

---

## GAP-011: `FunctionKind.Now` absent from temporal-type-system.md

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `docs/language/temporal-type-system.md`  
**Found in iteration:** 3

**Description:**  
`FunctionKind.Now` (name: `"now"`, returns `instant`) is defined in `Functions.cs` and documented in `precept-language-spec.md` §3.7 as:

```
| now()        | ()          | instant  | Current UTC instant — time zone independent; use .inZone(tz) to convert |
```

However, `now()` did not appear anywhere in `temporal-type-system.md`. A grep across the file returns zero matches for "now". The doc mentions `abs()`, `min()`, and `trim()` as freestanding functions for temporal types (Decision #20) but omits `now()` entirely.

This means the only canonical prose reference for the `now()` function was the spec §3.7 function table, with no coverage in the temporal type system doc that describes `instant` in depth.

**Rubber-Duck Analysis:**  
`now()` is the primary way to obtain an `instant` value at runtime. It is likely to be heavily used in `when` guards and action expressions. The temporal type system doc has a rich `instant` section covering `.inZone(tz)`, arithmetic, ISO 8601 format, etc. — but no mention of how to get an `instant` value at all (other than typed constants). The absence is notable because the doc is the natural landing point for developers looking up instant semantics.

The doc could either: (a) add a brief "Obtaining an instant" section that references `now()`, or (b) add `now()` to the freestanding function table it already has. The simplest correct fix is (a): a `**Runtime construction — now():**` paragraph right after the Single-quoted literal description in the `instant` type section, plus updating the Decision #20 freestanding-functions mention.

**Resolution:**  
Fixed. **Owner judgment:** _"now should get documented properly in the temporal type system doc with rationale as per the other types and features."_

Initial iteration 4 fix added a minimal callout. Updated to a full `#### now() — current UTC instant` subsection within `### instant`, covering: why freestanding (Decision #20), UTC-always rationale, determinism implications (Decision #9), four usage pattern examples, type rules table, and a new Locked Decision #21 documenting the no-timezone-overload choice with alternatives rejected.

---

<!-- Iteration 1 complete: 5 gaps found, 1 fixed (GAP-001), 4 unresolved (GAP-002, GAP-003, GAP-004, GAP-005). Docs audited: collection-types.md, primitive-types.md, business-domain-types.md, temporal-type-system.md, catalog-system.md against precept-language-spec.md. -->

<!-- Iteration 2 complete: 5 gaps found (GAP-006 through GAP-010), all Unresolved. Cross-reference: TokenKind.cs (138 members), TypeKind.cs (32 members), Tokens.cs, Types.cs against precept-language-spec.md §1.1, §1.2, §2.1, §2.2, §2.3, §3.6, §3.8. All new collection-type tokens/types are correctly documented in collection-types.md but absent from the corresponding spec §1 and §2 sections. Spec §3 was partially updated (contains/notempty tables correct; member access and action validation tables stale). No C# files modified. -->

<!-- Iteration 3 complete: GAP-006 through GAP-010 fixed (all mechanical back-ports of collection-type surface to spec §1.1, §1.2, §2.1, §2.2, §2.3, §3.6, §3.8). 1 new gap filed (GAP-011): FunctionKind.Now absent from temporal-type-system.md — Unresolved, needs owner judgment on placement. Audit scope: all FunctionKind, ActionKind, OperatorKind members cross-referenced against all 5 language docs (precept-language-spec.md, collection-types.md, primitive-types.md, temporal-type-system.md, business-domain-types.md). Zero name/spelling mismatches found. No C# files modified. -->

---

## GAP-012: Spec §2.7 Parser Diagnostics table missing 4 parse-stage codes

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `docs/language/precept-language-spec.md` §2.7 Parser Diagnostics  
**Found in iteration:** 4

**Description:**  
Four parse-stage codes (`OmitDoesNotSupportGuard`, `EventHandlerDoesNotSupportGuard`, `PreEventGuardNotAllowed`, `ExpectedOutcome`) were absent from spec §2.7. The table was written when only 4 generic parser errors existed.

**Rubber-Duck Analysis:**  
All four are `DiagnosticStage.Parse` in the catalog. §2.7 should be a complete catalog of parse diagnostics. Mechanical addition.

**Owner judgment:** _§2.7 should be a complete parse-diagnostic catalog — adding all 4 missing codes._

**Resolution:**  
Fixed. Added all 4 codes to the §2.7 table with conditions, severity, and exact message templates from `Diagnostics.cs`.

**Description:**  
`DiagnosticCode.cs` defines 8 parse-stage codes (codes 9–16). The spec §2.7 Parser Diagnostics table lists only 4 of them:

| In spec §2.7 | Not in spec §2.7 |
|---|---|
| `ExpectedToken` (9) | `OmitDoesNotSupportGuard` (13) |
| `NonAssociativeComparison` (10) | `EventHandlerDoesNotSupportGuard` (14) |
| `UnexpectedKeyword` (11) | `PreEventGuardNotAllowed` (15) |
| `InvalidCallTarget` (12) | `ExpectedOutcome` (16) |

The four missing codes all fire from the transition-row and access-mode parsers for specific structural violations, with these catalog entries in `Diagnostics.cs`:
- `OmitDoesNotSupportGuard`: "'omit' is an unconditional structural exclusion — 'when' guards are not allowed"
- `EventHandlerDoesNotSupportGuard`: "Event handlers ('on Event -> action') do not support 'when' guards..."
- `PreEventGuardNotAllowed`: "A 'when' guard before the event target is not supported on transition rows — place the guard after 'on Event'"
- `ExpectedOutcome`: "Expected a transition outcome ('-> transition State', '-> no transition', or '-> reject Message') but none was found"

The spec §2.7 table was written when only the four "generic" parser errors existed and was not updated when these more specific parse-stage errors were added.

**Rubber-Duck Analysis:**  
The spec §2.7 is clearly the stale side — these four codes are fully implemented in the catalog and are parse-stage by design (they fire when the parser's transition-row and access-mode grammar cannot proceed, not during type checking). Adding them to §2.7 is a mechanical documentation update. The message templates and descriptions are known from the catalog. No design decisions are required for the content.

The risk of fixing is low — this is adding information, not changing anything. However, whether the spec §2.7 table should be comprehensive or just representative is an owner call.

**Resolution:**  
Needs owner judgment. Owner should confirm that §2.7 should be a complete catalog of parse diagnostics (and add the 4 missing codes), or document it as representative with a cross-reference to the full catalog.

---

## GAP-013: `AmbiguousTypedConstant` message mismatch between spec §3.10 and catalog

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `docs/language/precept-language-spec.md` §3.10 Diagnostic Catalog "New codes" table  
**Found in iteration:** 4

**Description:**  
The spec §3.10 "New codes" table had:
```
| `AmbiguousTypedConstant` | Error | "'{0}' could be a {1} or {2} — add context to disambiguate" | Multi-member family, no context to narrow |
```

The `Diagnostics.cs` catalog has:
```csharp
DiagnosticCode.AmbiguousTypedConstant => new(..., "Typed constant '{0}' is ambiguous between {1} and {2}", ...)
```

The parameter count is the same (3 parameters: {0}, {1}, {2}) but the message text differs structurally:
- Spec: `"'{0}' could be a {1} or {2} — add context to disambiguate"` — the `{0}` token is presented as a value and the type alternatives are followed by a directive.
- Catalog: `"Typed constant '{0}' is ambiguous between {1} and {2}"` — leads with the token kind ("Typed constant"), states ambiguity directly, no directive.

**Rubber-Duck Analysis:**  
The catalog message is more consistent with Precept's diagnostic style (factual, concise, no imperative directive — directions go in `FixHint`). The spec message has an inline directive ("add context to disambiguate") that the catalog separates out. The catalog is the implementation ground truth.

**Resolution:**  
Fixed. Spec §3.10 `AmbiguousTypedConstant` message template updated to match the catalog:  
`"'{0}' could be a {1} or {2} — add context to disambiguate"` → `"Typed constant '{0}' is ambiguous between {1} and {2}"`

---

## GAP-014: `EventHandlerInStatefulPrecept` message mismatch between spec §3.10 and catalog

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `docs/language/precept-language-spec.md` §3.10 Diagnostic Catalog "New codes" table  
**Found in iteration:** 4

**Description:**  
The spec §3.10 "New codes" table had:
```
| `EventHandlerInStatefulPrecept` | Error | "Event handlers cannot appear in a precept with state declarations" | `on Event ->` mixed with `state` declarations |
```

The `Diagnostics.cs` catalog has:
```csharp
DiagnosticCode.EventHandlerInStatefulPrecept => new(..., "Event handler '{0}' is not valid in a stateful precept", ...)
```

The parameter count differs:
- Spec: 0 parameters — names the class of constructs ("Event handlers") generically.
- Catalog: 1 parameter {0} — names the specific handler (`'{0}'`) that triggered the error.

**Rubber-Duck Analysis:**  
The catalog message is strictly more informative — it names which event handler is invalid, giving the author an exact location. The spec message is generic and was likely written before the message was implemented with a parameter. The catalog is the implementation ground truth.

**Resolution:**  
Fixed. Spec §3.10 `EventHandlerInStatefulPrecept` message template updated to match the catalog:  
`"Event handlers cannot appear in a precept with state declarations"` → `"Event handler '{0}' is not valid in a stateful precept"`

---

## GAP-015: Three choice parse-stage codes placed in spec §3.10 (Type Checker section)

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `docs/language/precept-language-spec.md` §2.7 and §3.10  
**Found in iteration:** 4

**Description:**  
`EmptyChoice` (46), `ChoiceMissingElementType` (90), `ChoiceElementTypeMismatch` (88) were in spec §3.10 but are `DiagnosticStage.Parse` in the catalog.

**Rubber-Duck Analysis:**  
The catalog (Parse stage) is correct — all three are detected during `ParseTypeRef()` with no type-checker context needed. §3.10 placement was misleading.

**Owner judgment:** _Parse stage is correct; choice syntax errors belong in §2.7._

**Resolution:**  
Fixed. Added all three codes to the §2.7 table. Replaced the three full rows in §3.10 with a single cross-reference row: `"Parse-stage — see §2.7 Parser Diagnostics"`.

**Description:**  
Three diagnostic codes are in the spec §3.10 "New codes" table (which sits under §3 "Type Checker") but are labeled `DiagnosticStage.Parse` in `Diagnostics.cs`:

| Code | Catalog stage | Spec §3.10 placement |
|------|--------------|---------------------|
| `EmptyChoice` (46) | `DiagnosticStage.Parse` | §3.10 "New codes" (under §3 Type Checker) |
| `ChoiceMissingElementType` (90) | `DiagnosticStage.Parse` | §3.10 "New codes" (under §3 Type Checker) |
| `ChoiceElementTypeMismatch` (88) | `DiagnosticStage.Parse` | §3.10 "New codes" (under §3 Type Checker) |

These are choice-type syntax errors that the parser can detect during `ParseTypeRef()`:
- `EmptyChoice`: `choice of T()` with empty arg list — the `(` immediately closes.
- `ChoiceMissingElementType`: `choice(...)` without `of T` — missing `of` token.
- `ChoiceElementTypeMismatch`: `choice of integer("hello")` — literal kind doesn't match declared element type at parse time.

**Rubber-Duck Analysis:**  
The catalog (Parse stage) is probably correct: these are detected at parse time because the choice type grammar (`§2.3 ChoiceType`) can validate element count and literal kind without type-checker context. The parser has all necessary information — no symbol table or expression context is needed. The spec §3.10 placement under Type Checker is misleading.

However, there's a counter-argument: `ChoiceElementTypeMismatch` (wrong literal kind for declared element type) requires the declared element type (`of T`) to be present, which the parser reads during the same TypeRef parse. This is technically parser-phase knowledge, not type-checker knowledge. So Parse stage seems correct.

The fix would be to move these three codes to a "Choice syntax diagnostics" note in the spec §2.7 Parser Diagnostics table (matching their actual stage), and remove them from §3.10. This requires a design decision about whether these should be parse-phase or type-phase.

**Resolution:**  
Needs owner judgment. Owner should confirm: (a) are these parser-phase diagnostics that belong in §2.7 rather than §3.10? If yes, move their entries from §3.10 to §2.7 in the spec. If the owner determines they should remain type-checker diagnostics (stage change from Parse → Type in the catalog), update `Diagnostics.cs` instead. No C# files modified here.

---

## GAP-016: Spec §3.10 Diagnostic Catalog incomplete — ~30 catalog codes undocumented

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `docs/language/precept-language-spec.md` §3.10 Diagnostic Catalog  
**Found in iteration:** 4  
**Owner judgment:** _"Can we just make the catalog the source of truth?"_ — Yes. `Diagnostics.cs` + `DiagnosticCode.cs` are the canonical source; spec §3.10 should be a schema pointer and group reference, not a duplicate of the catalog.

**Description:**  
The spec §3.10 "Diagnostic Catalog" documented only a subset of the 106 `DiagnosticCode` members, and all duplicated data that already lives in `Diagnostics.cs`.

**Rubber-Duck Analysis:**  
An initial fix (by `fix-gap-016` agent) added nine `####` subsections with individual code rows sourced from `Diagnostics.cs`. On review, owner identified that this approach — duplicating message templates and severities into a doc — contradicts the metadata-driven architecture principle: catalogs are the source of truth; docs describe schema and rationale, not duplicate catalog data.

**Resolution:**  
Rewrote `§3.10` as a thin pointer:
- **Canonical sources** block linking to `DiagnosticCode.cs` and `Diagnostics.cs`
- **Cross-reference** to `docs/compiler/diagnostic-system.md` for schema and design rationale
- **Diagnostic groups table** — ordinal ranges and subsystem doc cross-refs (no individual code rows)
- **Design notes** — parse-stage choice codes (why ordinals 46/88/90 are `DiagnosticStage.Parse`), code-66 reassignment note for `~string`
- Added XML doc comment on `DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals` (ordinal 66) recording the reassignment history directly in the source

All individual code rows removed from the spec. `Diagnostics.cs` is the single source of truth.

**Description:**  
The spec §3.10 "Diagnostic Catalog" documented only a subset of the 106 `DiagnosticCode` members. The following groups had NO entry in spec §3.10:

| Group | Codes | Count | Where documented (if anywhere) |
|-------|-------|-------|-------------------------------|
| `MutuallyExclusiveQualifiers` | 23 | 1 | §3.8 prose (business-domain types) |
| `InvalidTypedConstantContent` | 53 | 1 | None found in spec |
| Temporal type codes | 55–62 (8 codes) | 8 | `temporal-type-system.md` §Teachable error messages |
| Collection safety: UnguardedAccess/Mutation, NonOrderable | 63–65 | 3 | `collection-types.md` |
| Business-domain codes | 67–77 (11 codes) | 11 | `business-domain-types.md` |
| Runtime/value safety | 78–79 | 2 | None found in spec |
| Graph codes | 80–81 | 2 | Not yet — graph analyzer not implemented |
| Proof codes | 82–84 | 3 | Not yet — proof engine not implemented |
| Lifecycle validation | 93–94 | 2 | Spec §3A.5 prose (no table entry) |

**Resolution:**  
Added nine new subsections to the end of §3.10 in `docs/language/precept-language-spec.md`:
- **Qualifier type diagnostics** — `MutuallyExclusiveQualifiers` (23)
- **Typed-constant content diagnostics** — `InvalidTypedConstantContent` (53)
- **Temporal type diagnostics** — codes 55–62 with cross-reference to `temporal-type-system.md`
- **Unguarded collection access and mutation diagnostics** — codes 63–65 with cross-reference to `collection-types.md`
- **Business-domain type diagnostics** — codes 67–77 with cross-reference to `business-domain-types.md`
- **Runtime / value-safety diagnostics** — `NumericOverflow` (78), `OutOfRange` (79)
- **Graph analyzer diagnostics** — codes 80–81 with note that these are emitted in a later pipeline stage
- **Proof engine diagnostics** — codes 82–84 with note that these are emitted in a later pipeline stage
- **Lifecycle-validation diagnostics** — `RequiredFieldsNeedInitialEvent` (93), `InitialEventMissingAssignments` (94)

All message templates sourced from `src/Precept/Language/Diagnostics.cs` (ground truth).

---

<!-- Iteration 4 complete: GAP-011 fixed (now() added to temporal-type-system.md instant section and Decision #20). 5 new gaps filed: GAP-012 (Unresolved — spec §2.7 missing 4 parse-stage codes), GAP-013 (Fixed — AmbiguousTypedConstant message mismatch in §3.10), GAP-014 (Fixed — EventHandlerInStatefulPrecept message mismatch in §3.10), GAP-015 (Unresolved — 3 choice codes labeled Parse-stage in catalog but placed in §3.10 Type Checker section), GAP-016 (Unresolved — spec §3.10 missing ~30 codes across temporal/business-domain/graph/proof/lifecycle groups; 3 codes entirely undocumented). Audit scope: all 106 DiagnosticCode members cross-referenced against spec §1.8, §2.7, §3.10; all 13 ExpressionFormKind members cross-referenced against spec §2.1 (no gaps). No C# files modified. -->

---

## GAP-017: `Arrow` (`->`) categorized as `Structural` in `Tokens.cs` but spec §1.1 places it in the Operators table

**Status:** Fixed  
**Category:** Catalog-Impl  
**Location:** `src/Precept/Language/Tokens.cs` (Arrow entry); `docs/language/precept-language-spec.md` §1.1 Operators table  
**Found in iteration:** 5

**Description:**  
`TokenKind.Arrow` (`->`) was tagged `Cat_Str` (Structural — same category as `EndOfSource`, `NewLine`, `Comment`) but three independent sources say it is an operator: spec §1.1 Operators table, `TokenKind.cs` `// ── Operators ──` section comment, and its own `TextMateScope`/`SemanticTokenType` metadata. The `TwoCharOperators` filter had a compensating `or Structural` clause specifically because of this miscategorization.

**Rubber-Duck Analysis:**  
`Cat_Str` for `Arrow` was a bug. The `or TokenCategory.Structural` workaround in `TwoCharOperators` was the tell. Fixing `Arrow` to `Cat_Op` and removing the workaround makes all four sources consistent with zero behavior change to the lexer.

**Resolution:**  
Changed `Cat_Str` → `Cat_Op` for `TokenKind.Arrow` in `Tokens.cs`. Simplified `TwoCharOperators` filter from `c is TokenCategory.Operator or TokenCategory.Structural` to `c is TokenCategory.Operator` — the `or Structural` clause was the workaround and is no longer needed. Updated the `TwoCharOperators` doc comment to remove the `Structural` reference. No behavior change; `Arrow` was already correctly scanned as a two-char operator.  
**Note (iteration 7):** The C# changes described above were written in iteration 5 but not applied at that time (see iteration 5 completion comment: "no C# edits"). Verified and applied in iteration 7.

---

## GAP-018: Spec §1.1 `NumberLiteral` description omits exponent notation

**Status:** Fixed  
**Category:** Doc-Spec  
**Location:** `docs/language/precept-language-spec.md` §1.1 Literals table (line 403)  
**Found in iteration:** 5

**Description:**  
The spec §1.1 Literals table row for `NumberLiteral` reads:
```
| `NumberLiteral` | Digit sequence, optionally with one `.` followed by more digits |
```

This description is incomplete. The spec's own §1.3 grammar says:
```
NumberLiteral  :=  '-'? Digits ('.' Digits)? (('e' | 'E') ('+' | '-')? Digits)?
```

And the spec §1.3 prose explicitly states: "**Exponent notation and numeric types:** Exponent notation (`e`/`E`) is only valid for the `number` type. … The lexer accepts all forms; the type checker enforces the restriction."

The lexer (`ScanNumber` in `Lexer.cs`, lines 700–733) implements the full three-part grammar: integer digits, optional decimal part, optional exponent part. The `NumberLiteral` token produced by the lexer for `1.5e-3` is a single token, not multiple. The §1.1 summary description misrepresents the token by omitting the exponent part, creating an internal spec inconsistency between §1.1 and §1.3.

**Rubber-Duck Analysis:**  
The §1.1 Literals table was written as a quick-reference summary. The exponent form was likely added later (in §1.3 with the number-type design) and the §1.1 summary was not updated. Since §1.3 is the authoritative grammar section and the description in §1.1 is a plain-English summary, the correct fix is to extend the §1.1 summary description to mention the exponent form with a cross-reference to §1.3. The risk is zero — it is adding information to a summary that already references §1.3.

**Resolution:**  
Fixed. Updated `docs/language/precept-language-spec.md` §1.1 Literals table `NumberLiteral` row:  
`"Digit sequence, optionally with one `.` followed by more digits"` →  
`"Digit sequence; optionally followed by a decimal part (`.` + more digits) and/or an exponent part (`e`/`E`, optional sign, digits) — see §1.3 for the full grammar"`

---

---

## GAP-029: `IsOutcomeAhead()` hardcodes outcome token set instead of deriving from `TokenCategory.Outcome`

**Status:** Fixed  
**Category:** Catalog-Impl  
**Location:** `src/Precept/Pipeline/Parser.cs` lines ~420–425  
**Found in iteration:** 8

**Description:**  
`IsOutcomeAhead()` hard-checks exactly three `TokenKind` values:

```csharp
private bool IsOutcomeAhead()
{
    var next = Peek(1);
    return next.Kind is TokenKind.Transition or TokenKind.No or TokenKind.Reject;
}
```

The catalog already encodes these as `TokenCategory.Outcome` (`Cat_Out`) in `Tokens.cs`. These three tokens are the complete and only outcome tokens today — so the hardcoded set and the catalog-derived set are identical. But the hardcoding violates the catalog-derivation rule: if a new outcome token is added to the catalog, `IsOutcomeAhead()` silently fails to track it.

**Catalog reference:** `Tokens.All` / `TokenCategory.Outcome`

**Fix:**  
Add a catalog-derived static set to `Parser.cs`:

```csharp
private static readonly FrozenSet<TokenKind> OutcomeTokens =
    Tokens.All.Where(t => t.Categories.Contains(TokenCategory.Outcome))
              .Select(t => t.Kind).ToFrozenSet();
```

Update `IsOutcomeAhead()` to:

```csharp
private bool IsOutcomeAhead() => OutcomeTokens.Contains(Peek(1).Kind);
```

---

## GAP-030: `ParseAtom` hardcodes `TokenKind.Min`/`TokenKind.Max` as function-call-capable keyword tokens

**Status:** Fixed  
**Category:** Catalog-Impl  
**Location:** `src/Precept/Pipeline/Parser.Expressions.cs` lines ~178–202  
**Found in iteration:** 8

**Description:**  
`ParseAtom` has these explicit cases for keywords that are also function names:

```csharp
case TokenKind.Identifier:
// GAP-C fix: min/max are keywords but can also appear as function names
// in expression position: min(a, b) or max(a, b).
case TokenKind.Min:
case TokenKind.Max:
```

`min` and `max` are constraint keywords (`TokenKind.Min`/`TokenKind.Max`, `Cat_Cns`) that the lexer emits when it sees those words. They are simultaneously function names in `Functions.ByName`. The catalog contains both facts; the parser can derive the intersection. If a future function is added whose name collides with a keyword token (e.g., a hypothetical `now` becoming a DSL-recognized keyword), the parser would not handle it without a manual code change.

`IsValidAsMemberName` (which drives `KeywordsValidAsMemberName`) covers the `.min`/`.max` member-access case — not the `min(a, b)` function-call case. There is no catalog-derived set today for "keyword tokens that are also callable as functions."

**Catalog reference:** `Functions.ByName` ∩ `Tokens.Keywords`

**Fix:**  
Add a catalog-derived set to `Parser.cs`:

```csharp
/// <summary>
/// Keyword tokens whose text also appears as a function name in <see cref="Functions.ByName"/>.
/// Catalog-derived. Drives the function-call atom branch in ParseAtom.
/// </summary>
internal static readonly FrozenSet<TokenKind> KeywordsUsableAsFunctionNames =
    Functions.All
        .Where(f => Tokens.Keywords.ContainsKey(f.Name))
        .Select(f => Tokens.Keywords[f.Name])
        .ToFrozenSet();
```

Restructure `ParseAtom` to check `KeywordsUsableAsFunctionNames.Contains(current.Kind)` rather than hard-matching `TokenKind.Min` / `TokenKind.Max`. The cleanest approach is to check the set before the switch statement and fall through to the identifier/call branch.

---

## GAP-031: Unary and postfix operator binding powers hardcoded instead of catalog-derived

**Status:** Fixed  
**Category:** Catalog-Impl  
**Location:** `src/Precept/Pipeline/Parser.Expressions.cs` lines ~61, ~207, ~215  
**Found in iteration:** 8

**Description:**  
Three operator binding powers are hardcoded numeric literals in the expression parser, even though `Operators.All` already carries the canonical values:

| Location | Hardcoded value | Catalog source | Catalog value |
|----------|----------------|----------------|---------------|
| `ParseExpression` line ~61 (`is set` postfix guard) | `60` | `Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set)!.Precedence` | `60` |
| `ParseAtom` line ~207 (`not` unary binding power) | `25` | `Operators.ByToken[(TokenKind.Not, Arity.Unary)].Precedence` | `25` |
| `ParseAtom` line ~215 (`negate` unary binding power) | `65` | `Operators.ByToken[(TokenKind.Minus, Arity.Unary)].Precedence` | `65` |

All three values currently match the catalog. The risk is catalog drift: if an operator's precedence is adjusted in `Operators.cs`, the parser won't track it. The `is set` case has a comment acknowledging the catalog value but still uses a literal. The `not`/`negate` cases have no such acknowledgment.

Note: member-access (`80`) and method-call (`90`) binding powers are NOT in the operator catalog (dot and parenthesis are grammar-structural, not `Operators.All` members) — those are legitimate hardcoding.

**Catalog reference:** `Operators.ByToken[(TokenKind, Arity.Unary)]`, `Operators.ByTokenSequence`

**Fix:**  
Replace the three numeric literals with catalog lookups, captured as `private static readonly int` constants at the top of the `ParseSession` or the outer `Parser` class to avoid per-call overhead:

```csharp
// In Parser.cs static fields (outer class — ref struct cannot own statics):
private static readonly int PrecedenceIsSet   = (int)Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set)!.Precedence;
private static readonly int PrecedenceNot     = Operators.ByToken[(TokenKind.Not,   Arity.Unary)].Precedence;
private static readonly int PrecedenceNegate  = Operators.ByToken[(TokenKind.Minus, Arity.Unary)].Precedence;
```

Then replace `60`, `25`, `65` with `PrecedenceIsSet`, `PrecedenceNot`, `PrecedenceNegate` in `Parser.Expressions.cs`.

---

## GAP-032: `Functions.cs` `pow(integer, integer)` missing `ProofRequirement` for `exp >= 0`

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `src/Precept/Language/Functions.cs` (Pow entry, lines ~162–172); `docs/language/precept-language-spec.md` §0.6 item 4  
**Found in iteration:** 8

**Description:**  
Spec §0.6 item 4 explicitly lists `pow(integer, integer)` exponents as a non-negative proof obligation, paired with `sqrt` inputs:

> **Non-negative proof obligations** such as `sqrt` inputs and `pow(integer, integer)` exponents. The compiler requires a provable non-negative path — via `nonnegative` constraint, a rule, an ensure, or a guard — before accepting the expression.

The `sqrt` entry in `Functions.cs` correctly carries a `NumericProofRequirement`:

```csharp
FunctionKind.Sqrt => new(kind, "sqrt", ...,
[
    new([PSqrtNumber], TypeKind.Number,
        ProofRequirements:
        [
            new NumericProofRequirement(new ParamSubject(PSqrtNumber), OperatorKind.GreaterThanOrEqual, 0m,
                "Argument must be non-negative"),
        ]),
],
```

The `pow` entry has NO `ProofRequirements` on any overload:

```csharp
FunctionKind.Pow => new(kind, "pow", ...,
[
    new([new(TypeKind.Integer, "base"), new(TypeKind.Integer, "exp")], TypeKind.Integer),
    new([new(TypeKind.Decimal, "base"), new(TypeKind.Integer, "exp")], TypeKind.Decimal),
    new([new(TypeKind.Number,  "base"), new(TypeKind.Integer, "exp")], TypeKind.Number),
],
```

The integer-base overload specifically requires a proof obligation: `pow(-3, -2)` would produce `1/9`, which cannot be represented as an integer. Negative exponents of integer bases are mathematically undefined in integer arithmetic.

**Rubber-Duck Analysis:**  
The spec is explicit and unambiguous. The `sqrt` entry is the reference pattern — the proof engine reads `ProofRequirements` from the catalog to enforce non-negativity at compile time. The `pow` entry is missing this on the `Integer^Integer` overload. The Decimal and Number overloads produce fractional results with negative exponents (which are representable), but the spec singles out `pow(integer, integer)` — so only the Integer overload needs the proof obligation.

**Fix:**  
Add `ProofRequirements` to the `Integer^Integer` overload:

```csharp
new([new(TypeKind.Integer, "base"), new(TypeKind.Integer, "exp")], TypeKind.Integer,
    ProofRequirements:
    [
        new NumericProofRequirement(new ParamSubject(new(TypeKind.Integer, "exp")),
            OperatorKind.GreaterThanOrEqual, 0m,
            "Exponent must be non-negative for integer pow"),
    ]),
```

To avoid inline construction, define a named param constant `PPowExp = new(TypeKind.Integer, "exp")` at the top of the Numeric section (analogous to `PSqrtNumber`) and reference it in both the overload signature and the `NumericProofRequirement`.

---

## GAP-033: `ModifierKind.cs` `Notempty` XML doc comment stale after GAP-025 fix

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `src/Precept/Language/ModifierKind.cs` line 22  
**Found in iteration:** 8  
**Severity:** Low (dev-facing XML comment only; no runtime impact)

**Description:**  
The `Notempty` enum member in `ModifierKind.cs` carries this XML doc comment:

```csharp
/// <summary>Flag: string is non-empty.</summary>
Notempty    =  6,
```

GAP-025 (resolved in iteration 7) expanded `Notempty.ApplicableTo` from `StringOnly` to `StringAndCollectionTypes` — a named array covering `String` + 8 collection types. GAP-027 updated the description in `Tokens.cs` from "String constraint: non-empty" to "String or collection constraint: non-empty." The analogous update was not applied to `ModifierKind.cs`.

This is the same pattern as GAP-027 (Tokens.cs description), resolved in iteration 7 — but in the enum file rather than the tokens catalog. The XML doc comment is the only surface affected; the C# catalog metadata (`ApplicableTo`) is already correct.

**Fix:**  
Update line 22 of `ModifierKind.cs`:

```csharp
// before
/// <summary>Flag: string is non-empty.</summary>

// after
/// <summary>Flag: string or collection is non-empty.</summary>
```

<!-- Iteration 8 complete: George's runtime/parser implementation audit. TypeChecker stub — no violations. Evaluator stub (all NotImplementedException) — no violations. Lexer fully catalog-driven — no violations. Parser: 3 new Catalog-Impl gaps found (GAP-029 IsOutcomeAhead hardcodes outcome token set; GAP-030 ParseAtom hardcodes min/max as function-call keywords; GAP-031 unary/postfix binding powers 25/65/60 hardcoded). Prior gap fixes confirmed: GAP-025 (Notempty.ApplicableTo → StringAndCollectionTypes ✅), GAP-026 (CollectionTypes 9 members ✅), GAP-028 (sqrt Number-only ✅). Frank's 6-dimension catalog/spec audit: 2 additional gaps found (GAP-032 pow missing ProofRequirement; GAP-033 ModifierKind.cs Notempty comment stale). Status: 33 gaps total, 29 Fixed, 4 Unresolved. GAP-030 fixed: KeywordsUsableAsFunctionNames derived from Functions.All ∩ Tokens.Keywords replaces hardcoded case TokenKind.Min:/Max: arms. -->

<!-- Iteration 5 complete: 2 gaps filed — GAP-017 (Unresolved — Arrow categorized as Cat_Str in Tokens.cs but spec §1.1, TokenKind.cs grouping, TextMateScope, and SemanticTokenType all say operator; TwoCharOperators filter has a redundant `or Structural` clause to compensate; no C# edits), GAP-018 (Fixed — spec §1.1 NumberLiteral row description omitted exponent notation documented in §1.3 and implemented in the lexer). Audit scope: all 138 TokenKind members, full Lexer.cs (ScanWord, ScanNumber, ScanStringContent, ScanTypedConstantContent, ScanComment, EmitNewLine, TryScanOperator, TryScanPunctuation), Tokens.Keywords/TwoCharOperators/SingleCharOperators/PunctuationChars derived tables, spec §1.1 and §1.3. All 138 TokenKind members accounted for in the lexer; zero unused members found; SetType correctly parser-synthesized and excluded from keyword lookup; ~string correctly scans as Tilde + StringType; ~= and !~ correctly scan as single compound tokens; -> correctly scans as Arrow (maximal munch via TwoCharOperators); case-sensitivity confirmed (FrozenDictionary ordinal); IsValidAsMemberName tokens (min, max, countof, peekby) lexed as keywords, parser handles dual-use. No C# files modified. -->

---

## GAP-019: `UnexpectedKeyword` (11) and `InvalidCallTarget` (12) never emitted by the parser

**Status:** Fixed  
**Category:** Doc-Impl  
**Location:** `docs/language/precept-language-spec.md` §2.7 Parser Diagnostics table; `src/Precept/Language/DiagnosticCode.cs` lines 18–19  
**Found in iteration:** 6

**Description:**  
Spec §2.7 lists both `UnexpectedKeyword` and `InvalidCallTarget` in the Parser Diagnostics table as valid, active parse-stage diagnostics:

- `UnexpectedKeyword` (11): `"'{0}' cannot appear inside a {1}"` — for "Unrecognized keyword in declaration position"
- `InvalidCallTarget` (12): `"Only built-in functions can be called this way — '{0}' is not a function name"` — for "Non-callable expression followed by `(`"

But `DiagnosticCode.cs` has these comments on both codes:
```csharp
UnexpectedKeyword = 11, // reserved — not currently emitted by the parser
InvalidCallTarget = 12, // reserved — not currently emitted by the parser
```

Searching all three parser files (`Parser.cs`, `Parser.Declarations.cs`, `Parser.Expressions.cs`) finds zero calls to `Diagnostics.Create(DiagnosticCode.UnexpectedKeyword, ...)` or `Diagnostics.Create(DiagnosticCode.InvalidCallTarget, ...)`.

For `InvalidCallTarget` specifically: spec §2.1 left-denotation table says "`(` (LeftParen): … else → diagnostic". In the implementation, when `left` is neither a `MemberAccessExpression` nor an `IdentifierExpression` (e.g. `42(x)`, `(A + B)(x)`), the parser silently `break`s without consuming `(` or emitting any diagnostic, leaving `(x)` as unparsed tokens that cause confusing downstream errors.

**Rubber-Duck Analysis:**  
The "reserved" comments indicate these diagnostics were planned but deferred. The spec §2.7 placement implies they are active, creating a false expectation for tooling consumers. For `InvalidCallTarget`: the case is reachable — `42(args)` or `(A + B)(args)` reach the infix `LeftParen` branch with a non-MemberAccess left operand; the parser currently `break`s silently (the comment "unreachable: identifiers resolve as FunctionCall in ParseAtom" is inaccurate — non-identifier left operands do reach this branch). For `UnexpectedKeyword`: would fire when a declaration keyword appears in a context where it is not a valid construct leader. Fixing `InvalidCallTarget` requires a one-line C# change (add the diagnostic before the `break`). Fixing `UnexpectedKeyword` is harder — requires defining what "unexpected keyword in declaration position" means for each construct context.

**Resolution:**  
Fixed. Implemented by George (iteration 7). The `DiagnosticCode.cs` "reserved" comments were removed on both codes. Two emission sites added to the parser:

1. **`InvalidCallTarget` (12)** — emitted in `Parser.Expressions.cs` at the infix `LeftParen` branch when `left` is not a `MemberAccessExpression` (e.g. `42(args)`, `(A + B)(args)`). The `DescribeCallTarget(left)` helper supplies the `{0}` substitution.

2. **`UnexpectedKeyword` (11)** — emitted in `ParseAtom` default fallback when `AllKeywordKinds.Contains(current.Kind)` is true. `AllKeywordKinds` is catalog-derived (`Tokens.Keywords.Values.ToFrozenSet()`) and correctly covers all keyword categories while excluding structural, punctuation, operator, and identifier tokens.

Diagnostic message templates in `Diagnostics.cs` match spec §2.7 exactly.

---

## GAP-020: `contains` associativity — spec §2.1 says `left`, `Operators.cs` enforces `NonAssociative`

**Status:** Fixed  
**Category:** Doc-Impl  
**Location:** `docs/language/precept-language-spec.md` §2.1 Expression Precedence table and Null-denotation table; `src/Precept/Language/Operators.cs` (`OperatorKind.Contains` entry)  
**Found in iteration:** 6

**Description:**  
Spec §2.1 precedence table listed `contains` with `left` associativity:
```
| 40 | `contains` | collection membership | left |
```

`Operators.cs` declares it `Associativity.NonAssociative`:
```csharp
OperatorKind.Contains => new SingleTokenOp(..., Arity.Binary, Associativity.NonAssociative, Precedence: 40, ...)
```

Because `contains` is in `OperatorPrecedence` with `NonAssociative`, the parser's chaining check (`if (meta?.Associativity == Associativity.NonAssociative) { ... }`) WILL emit `NonAssociativeComparison` for `A contains B contains C`. The spec said `left` (chaining allowed via left-fold); the implementation rejects chaining. Additionally, the `NonAssociativeComparison` error message ("use 'and' to combine comparisons") is inappropriate for a membership operator.

A secondary inconsistency: the spec left-denotation table showed `BinaryExpression(Contains, ParseExpression(40))`. For a non-associative operator at level 40, the spec convention (matching comparisons at level 30 → `ParseExpression(31)`) calls for `ParseExpression(41)` — using P+1 as the explicit non-associative signal.

**Rubber-Duck Analysis:**  
The catalog's `NonAssociative` is semantically correct: `A contains B` returns `boolean`, so `(A contains B) contains C` is a type error (boolean is not a collection). Rejecting it at parse level with a targeted message is better UX than a downstream type error. The spec table had the wrong associativity label, and the left-denotation table had the wrong right-binding power for a non-associative operator. Both are pure doc fixes — no C# change needed.

**Resolution:**  
Fixed. Applied to `docs/language/precept-language-spec.md`:
1. §2.1 Precedence table: `contains` associativity changed `left` → `non-associative`.
2. §2.1 Non-associative note paragraph: updated to mention `contains` alongside comparisons; added postfix-vs-binary-same-level explanation.
3. §2.1 Left-denotation table: `Contains` row right-binding changed `ParseExpression(40)` → `ParseExpression(41)` (P+1, matching the non-associative convention used for comparisons).

---

## GAP-021: `is set`/`is not set` associativity — spec §2.1 says `left`, catalog says `NonAssociative` (postfix)

**Status:** Fixed  
**Category:** Doc-Impl  
**Location:** `docs/language/precept-language-spec.md` §2.1 Expression Precedence table; `src/Precept/Language/Operators.cs` (`OperatorKind.IsSet`, `OperatorKind.IsNotSet` entries)  
**Found in iteration:** 6

**Description:**  
Spec §2.1 precedence table listed `is (is set / is not set)` with `left` associativity:
```
| 60 | `is` (`is set` / `is not set`) | presence test | left |
```

`Operators.cs` declares both `IsSet` and `IsNotSet` as `Arity.Postfix, Associativity.NonAssociative`. Calling a postfix operator "left" associative is semantically ambiguous — postfix operators consume only a left operand and return a value; they have no right operand, so "left-/right-associativity" does not apply. The `NonAssociative` label in the catalog correctly reflects that chaining (`A is set is not set`) is meaningless (the result is `boolean`, not a field).

**Rubber-Duck Analysis:**  
This is a metadata accuracy issue rather than a behavioral one. The binary-operator chaining check in the parser only fires for `Arity.Binary` operators (`OperatorPrecedence` is built from `Arity.Binary` entries). The `IsSet`/`IsNotSet` postfix operators are `Arity.Postfix`, so they are NOT in `OperatorPrecedence` and the NonAssociative annotation has no behavioral effect. The parser handles `is` as a special infix case before the general `OperatorPrecedence` lookup. The spec saying `left` for a postfix operator is simply wrong terminology — the correct label is `non-associative (postfix)` or `n/a`.

**Resolution:**  
Fixed. Updated `docs/language/precept-language-spec.md` §2.1 Precedence table: `is set`/`is not set` row associativity changed from `left` to `non-associative (postfix)`. The role column was also updated from `presence test` to `presence test (postfix)` for clarity.

---

## GAP-022: Spec §2.1 null-denotation table names `StringLiteralExpression` — a node that does not exist

**Status:** Fixed  
**Category:** Doc-Impl  
**Location:** `docs/language/precept-language-spec.md` §2.1 Null-denotation table; `src/Precept/Pipeline/SyntaxNodes/Expressions/LiteralExpression.cs`  
**Found in iteration:** 6

**Description:**  
Spec §2.1 null-denotation table row:
```
| `StringLiteral` | `StringLiteralExpression` |
```

But no `StringLiteralExpression` class exists in the SyntaxNodes. The parser creates `LiteralExpression` for `StringLiteral` tokens:
```csharp
case TokenKind.StringLiteral:
    return new LiteralExpression(current.Span, Advance());
```

`LiteralExpression` (doc comment: "A literal value: integer, decimal, string, boolean.") handles all four primitive literal token kinds uniformly. No `StringLiteralExpression.cs` file exists in `src/Precept/Pipeline/SyntaxNodes/Expressions/`. The spec's non-existent class name is inconsistent with the parallel rows for `NumberLiteral` and `True`/`False`, which both correctly say `LiteralExpression`.

Note: `TypedConstant` correctly maps to `TypedConstantExpression` (a distinct class, because typed constants are context-opaque). Plain string literals are NOT opaque — they are immediate `string` values — so reusing `LiteralExpression` is correct.

**Rubber-Duck Analysis:**  
This appears to be a transcription error in the null-denotation table. The author likely intended `LiteralExpression` (matching all other primitive literal rows) but used the pattern from a language like Roslyn where string literals have a distinct AST node. The fix is purely cosmetic — changing one entry in the spec table.

**Resolution:**  
Fixed. Updated `docs/language/precept-language-spec.md` §2.1 Null-denotation table: `StringLiteral → StringLiteralExpression` → `StringLiteral → LiteralExpression`.

---

## GAP-023: Spec §2.1 precedence table has `is` row (60) appearing before `+/-` row (50); two rows at level 60

**Status:** Fixed  
**Category:** Doc-Spec  
**Location:** `docs/language/precept-language-spec.md` §2.1 Expression Precedence table  
**Found in iteration:** 6

**Description:**  
Spec §2.1 states "Tighter binding = higher number", implying rows should be in ascending precedence order. But the table had:
```
| 40 | `contains` | ...         |
| 40 | `for`      | ...         |
| 60 | `is` ...   | ...  left   |   ← precedence 60 at row 7
| 50 | `+` `-`    | ...         |   ← precedence 50 at row 8
| 60 | `*` `/` `%`| ...         |   ← precedence 60 at row 9
```

Row 7 (precedence 60) appeared before row 8 (precedence 50), violating ascending order. Two rows both had precedence 60 (`is` and `*/÷/%`), creating a duplicate level without explanation of how a postfix operator and binary operators at the same level interact.

**Rubber-Duck Analysis:**  
The `is` row was inserted between `contains`/`for` (40) and `+/-` (50) in an earlier editing pass and the table was never re-sorted. The two-rows-at-60 situation is correct in the implementation: `is set` (postfix) has binding power 60 and `*/÷/%` (binary) also have binding power 60. The Pratt `nextMinPrec = 61` when parsing a multiplicative right operand means the postfix `is` check (`minPrecedence > 60` breaks) cannot enter that right-parse context, so `A * B is set` parses as `(A * B) is set`. The table just needs reordering and a note explaining the postfix interaction.

**Resolution:**  
Fixed. Applied to `docs/language/precept-language-spec.md` §2.1 Precedence table:
1. Moved `is` row to after `*/÷/%` row (both at 60 — now in a natural reading order).
2. Fixed `is` associativity (see GAP-021 above — changed to `non-associative (postfix)`).
3. Updated the non-associative note paragraph to explain the postfix-vs-binary same-level precedence interaction (see GAP-020 fix above).

---

## GAP-024: `bag of T`, `list of T`, and `log of T` forms lack `TypeQualifier?` in spec §2.3 grammar but the parser accepts element-type qualifiers on all collection types

**Status:** Fixed  
**Category:** Doc-Impl  
**Location:** `docs/language/precept-language-spec.md` §2.3 CollectionType grammar; `src/Precept/Pipeline/Parser.Declarations.cs` `ParseTypeRef()` (collection-type branch)  
**Found in iteration:** 6

**Resolution:** Spec §2.3 updated to add `TypeQualifier?` to all four collection forms (`bag of ScalarType TypeQualifier?`, `list of ScalarType TypeQualifier?`, `log of ScalarType TypeQualifier?`, `log of ScalarType by ScalarType TypeQualifier?`). Parser was already correct — this was a spec notation gap. The qualifier constrains the element type, not the container, so it is orthogonal to all collection kinds. Owner (Shane) signed off 2026-05-02.

**Description:**  
Spec §2.3 CollectionType grammar explicitly includes `TypeQualifier?` only on `(set | queue | stack)` but omits it from the other forms:
```
CollectionType  :=  (set | queue | stack) of ScalarType TypeQualifier?
                |   bag of ScalarType
                |   list of ScalarType
                |   log of ScalarType
                |   log of ScalarType by ScalarType
                ...
```

The parser's `ParseTypeRef()` calls `TryPeekQualifierKeyword()` after parsing the element type for ALL six collection kinds (`set`, `queue`, `stack`, `bag`, `list`, `log`). The check is element-type-driven: it reads the element type's `QualifierShape` from the catalog and proceeds if non-null. So `bag of money in 'USD'`, `list of quantity of 'length'`, `log of money in 'USD'`, and `log of money in 'USD' by string` all parse without error, even though the spec grammar excludes `TypeQualifier?` from these forms.

**Rubber-Duck Analysis:**  
Two valid interpretations:

1. **Spec omission** — `bag`, `list`, and `log` should also accept element-type qualifiers; the grammar omission was an authoring oversight. The same logic that makes `set of money in 'USD'` useful applies equally to `bag of money in 'USD'`. Fix: add `TypeQualifier?` to these forms in the spec grammar.

2. **Parser over-permissive** — the spec intentionally restricts qualifiers to `set`, `queue`, `stack` (perhaps `bag`/`list`/`log` are v3 additions where qualifier semantics were not fully reviewed). Fix: restrict the parser to call `TryPeekQualifierKeyword()` only when `collectionToken.Kind is TokenKind.Set or TokenKind.QueueType or TokenKind.StackType`.

The parser's catalog-driven approach (element-type-driven regardless of collection kind) is architecturally cleaner and avoids a hardcoded list of collection kinds that accept qualifiers. The spec restriction may be an oversight.

**Resolution:**  
Unresolved. Owner should decide: (a) update spec §2.3 grammar to add `TypeQualifier?` to `bag of ScalarType TypeQualifier?`, `list of ScalarType TypeQualifier?`, `log of ScalarType TypeQualifier?`, and `log of ScalarType by ScalarType TypeQualifier?` (accepting the parser as authoritative); or (b) restrict the parser to only invoke `TryPeekQualifierKeyword()` for `set`, `queue`, and `stack` element types.

---

<!-- Iteration 6 complete: 6 gaps filed — GAP-019 (Unresolved — `UnexpectedKeyword` (11) and `InvalidCallTarget` (12) listed in spec §2.7 as active parser diagnostics but DiagnosticCode.cs comments both as "reserved — not currently emitted"; parser silently breaks on non-callable `(` instead of emitting InvalidCallTarget), GAP-020 (Fixed — `contains` associativity: spec said `left` but catalog is `NonAssociative`; also fixed left-denotation right-binding from ParseExpression(40) to ParseExpression(41); updated non-associative note paragraph), GAP-021 (Fixed — `is set`/`is not set` associativity: spec said `left` for postfix operators; catalog and impl use NonAssociative; updated spec to say `non-associative (postfix)`), GAP-022 (Fixed — spec §2.1 null-denotation table named non-existent `StringLiteralExpression`; parser creates `LiteralExpression`; fixed spec table), GAP-023 (Fixed — spec §2.1 precedence table had `is` row (60) appearing before `+/-` row (50) out of order; two rows at level 60 with no interaction note; reordered table and added postfix explanation), GAP-024 (Unresolved — `bag of T`, `list of T`, `log of T` in spec §2.3 grammar lack TypeQualifier? but parser accepts qualifiers on all collection types via catalog-driven TryPeekQualifierKeyword()). Audit scope: full Parser.cs, Parser.Declarations.cs, Parser.Expressions.cs; spec §2.1–§2.7; Operators.cs catalog; Modifiers.cs catalog; DiagnosticCode.cs; SyntaxNodes/Expressions/ file inventory. 4 Fixed (doc-only), 2 Unresolved (need C# or owner judgment). No C# files modified. -->

---

## GAP-025: GAP-003 incomplete — `Modifiers.cs` `Notempty` applicability still `StringOnly`

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `src/Precept/Language/Modifiers.cs` lines 70–74; `docs/language/precept-language-spec.md` §3.8 (line 1470)  
**Found in iteration:** 7

**Description:**  
GAP-003 updated four documentation locations to extend `notempty` from string-only to string + 8 collection types. However, `Modifiers.cs` itself was not updated. The catalog still reads:

```csharp
ModifierKind.Notempty => new(kind, "notempty",
    ApplicableTo: StringOnly,  // StringOnly = [TypeKind.String]
    ...),
```

Spec §3.8 (line 1470) is now authoritative:
> `notempty` applies to: `string`, `set`, `queue`, `stack`, `log`, `log of T by P`, `bag`, `list`, `queue of T by P`

This is 9 types total (1 scalar + 8 collection kinds). `Lookup` is explicitly excluded from `notempty` applicability (lookup entries are always present; "non-empty" is not meaningful).

**Rubber-Duck Analysis:**  
The GAP-003 author updated the spec and three supporting docs but did not update `Modifiers.cs`. The catalog is the machine-readable source that the TypeChecker will consume to enforce modifier applicability. A `notempty` on a `bag` field should be valid per the spec but the catalog currently marks it invalid. This is a TypeChecker-correctness issue. However, fixing it requires confirming the 9-type list is final (owner review recommended before TypeChecker work begins) and ensuring the `CollectionTypes` array is consistent (see GAP-026).

**Resolution:**  
Fixed (iter 7, Frank). Defined `StringAndCollectionTypes = [TypeKind.String, TypeKind.Set, TypeKind.Queue, TypeKind.Stack, TypeKind.Log, TypeKind.LogBy, TypeKind.Bag, TypeKind.List, TypeKind.QueueBy]` in `Modifiers.cs`. Updated `Notempty.ApplicableTo` to reference the new array. `Lookup` excluded per owner decision — lookup entries are defined at design time and cannot be empty at runtime. Tests updated: `StringModifiers_ApplyToStringOnly` theory no longer includes `Notempty`; new `Notempty_AppliesToStringAndCollectionTypes` fact verifies the 9-type set and the Lookup exclusion explicitly.

---

## GAP-026: `Modifiers.cs` `CollectionTypes` array stale — `mincount`/`maxcount` missing 6 new TypeKind members

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `src/Precept/Language/Modifiers.cs` lines 23–26; `docs/language/precept-language-spec.md` §3.8 (line 1473)  
**Found in iteration:** 7

**Description:**  
`Modifiers.cs` defines:

```csharp
private static readonly TypeKind[] CollectionTypes = [TypeKind.Set, TypeKind.Queue, TypeKind.Stack];
```

This array drives `ApplicableTo` for `ModifierKind.Mincount` and `ModifierKind.Maxcount`. Spec §3.8 (line 1473) says `mincount`/`maxcount` applies to:

> `set`, `queue`, `stack`, `log`, `log of T by P`, `bag`, `list`, `queue of T by P`, `lookup of K to V`

Six TypeKind members are missing: `Log` (27), `LogBy` (28), `Bag` (29), `List` (30), `QueueBy` (31), `Lookup` (32). Note that `Lookup` IS included in `mincount`/`maxcount` (unlike `notempty` where it is excluded) — this is intentional per the spec.

**Rubber-Duck Analysis:**  
`CollectionTypes` was defined when only the original three collection types existed. The six new collection types added later were not added to this array. This is a pure catalog omission with no ambiguity — the spec explicitly lists all nine types. The fix is a mechanical array extension. TypeChecker implications are straightforward (mincount/maxcount on a `bag` field will currently be rejected; it should be allowed). Owner may want to apply this fix alongside GAP-025 for consistency.

**Resolution:**  
Fixed (iter 7, Frank). Extended `CollectionTypes` from 3 to 9 members: `[TypeKind.Set, TypeKind.Queue, TypeKind.Stack, TypeKind.Log, TypeKind.LogBy, TypeKind.Bag, TypeKind.List, TypeKind.QueueBy, TypeKind.Lookup]`. `Lookup` IS included per owner decision — constraining how many lookup entries exist at runtime is meaningful for `mincount`/`maxcount`. No other modifier references `CollectionTypes`. Tests updated: `CollectionModifiers_ApplyToSetQueueStack` renamed to `CollectionModifiers_ApplyToAllNineCollectionTypes` and now verifies all 9 members.

---

## GAP-027: `Tokens.cs` `Notempty` description reads "String constraint: non-empty" but spec §1.1 says "String or collection constraint: non-empty"

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `src/Precept/Language/Tokens.cs` line 225; `docs/language/precept-language-spec.md` §1.1 Keywords: Constraints table  
**Found in iteration:** 7

**Description:**  
Spec §1.1 Keywords: Constraints table entry for `notempty`:
> `String or collection constraint: non-empty`

`Tokens.cs` catalog entry (line 225):
```csharp
TokenKind.Notempty => new(kind, "notempty", Cat_Cns, "String constraint: non-empty", ...)
```

The description field omits the collection applicability. This is the token description used by tooling consumers (hover text, MCP `precept_language` output, completions).

**Rubber-Duck Analysis:**  
The description was written before GAP-003 extended `notempty` to collections. When GAP-003 was resolved it updated 4 doc locations but missed this catalog description field. The fix is a one-word change with zero behavior impact.

**Resolution:**  
Fixed. Updated `Tokens.cs` line 225: `"String constraint: non-empty"` → `"String or collection constraint: non-empty"`.

---

## GAP-028: `Functions.cs` `sqrt` has `Integer` and `Decimal` overloads but spec §3.7 says integer/decimal inputs are type errors

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `src/Precept/Language/Functions.cs` lines 176–201; `docs/language/precept-language-spec.md` §3.7 (line 1391)  
**Found in iteration:** 7

**Description:**  
Spec §3.7 function catalog row for `sqrt`:
> `sqrt(value) | (number) → number | number | Number-lane only; decimal and integer inputs are type errors (no .NET Math.Sqrt overload for decimal; use approximate(value) to convert first)`

`Functions.cs` defines three overloads for `FunctionKind.Sqrt`:
```csharp
new([PSqrtInteger], TypeKind.Number, ...),  // integer → Number
new([PSqrtDecimal], TypeKind.Number, ...),  // decimal → Number
new([PSqrtNumber],  TypeKind.Number, ...),  // number  → Number
```

The integer and decimal overloads contradict the spec's explicit "type errors" claim. If the TypeChecker uses these overloads, `sqrt(integer_field)` and `sqrt(decimal_field)` would succeed, silently accepting inputs the spec says are invalid.

**Rubber-Duck Analysis:**  
GAP-004 updated the spec §3.7 `sqrt` row to say "Number-lane only; decimal and integer inputs are type errors." But `Functions.cs` was not updated at that time — the same pattern as GAP-003 (doc fix without catalog fix). Two valid interpretations: (a) the Integer/Decimal overloads are catalog bugs — they should be removed to match the spec; (b) the spec is aspirational and the implementation intentionally widens `sqrt` to all numeric types with no conversion requirement. This is an owner judgment call: the spec reasoning ("no .NET Math.Sqrt for decimal; use approximate() first") suggests the restriction is deliberate. If upheld, remove the Integer and Decimal overloads from `Functions.cs`.

**Resolution:**  
Fixed (iter 7, Frank). Removed the `Integer→Number` and `Decimal→Number` overloads from `FunctionKind.Sqrt` in `Functions.cs`. Only the `Number→Number` overload remains, matching spec §3.7 exactly. The `PSqrtInteger` and `PSqrtDecimal` static fields were also removed as they became unreferenced. Tests updated: `NumericFunction_OverloadCount(Sqrt, 3)` → `(Sqrt, 1)`; `Sqrt_AllOverloads_HaveNonNegativeRequirement` trimmed to `[InlineData(0)]` only; `Sqrt_RequirementCount_IsOnePerOverload` updated to assert 1 overload. Total overload count invariant updated: 49 → 47.

---

<!-- Iteration 7 complete: Final pre-TypeChecker language consistency audit pass. Phase A verification — confirmed 22 of 24 prior gaps are genuinely fixed; identified 2 false-Fixed entries: GAP-017 (C# changes written in iter 5 but never applied — applied now in iter 7) and GAP-003 (docs updated but Modifiers.cs not updated — filed as new GAP-025). 4 new gaps filed: GAP-025 (Unresolved → Fixed in iter 7 by Frank — GAP-003 incomplete; Modifiers.cs Notempty ApplicableTo updated from StringOnly to StringAndCollectionTypes; spec §3.8 9-type set applied, Lookup excluded), GAP-026 (Unresolved → Fixed in iter 7 by Frank — Modifiers.cs CollectionTypes extended from 3 to 9 members; Lookup included for mincount/maxcount), GAP-027 (Fixed — Tokens.cs Notempty description updated), GAP-028 (Unresolved → Fixed in iter 7 by Frank — Functions.cs sqrt Integer/Decimal overloads removed per spec §3.7; sqrt is Number-lane only). C# changes applied this iteration: Tokens.cs Arrow Cat_Str → Cat_Op, TwoCharOperators filter simplified, Notempty description updated, Modifiers.cs CollectionTypes extended + StringAndCollectionTypes added + Notempty.ApplicableTo updated, Functions.cs sqrt Integer/Decimal overloads + PSqrtInteger/PSqrtDecimal fields removed. Tests updated: ModifiersTests.cs (StringModifiers_ApplyToStringOnly theory, CollectionModifiers renamed+extended, new Notempty_AppliesToStringAndCollectionTypes fact), FunctionsTests.cs (OverloadCount Sqrt 3→1, Total_OverloadCount 49→47, Sqrt_AllOverloads_HaveNonNegativeRequirement trimmed to overload 0 only, Sqrt_RequirementCount_IsOnePerOverload asserts 1 overload). Final state: 28 gaps total, 28 Fixed, 0 Unresolved. Pre-TypeChecker language consistency audit CLOSED. -->





---

## GAP-034: `ParseTypeRef` hardcodes 6-token collection-type test instead of deriving from `Types.All`

**Status:** Fixed  
**Category:** Catalog-Impl  
**Location:** `src/Precept/Pipeline/Parser.Declarations.cs` `ParseTypeRef()`, line ~995  
**Found in iteration:** 9

**Description:**  
`ParseTypeRef` dispatches to the collection-type parsing branch using a hardcoded token-kind disjunction:

```csharp
if (current.Kind is TokenKind.Set or TokenKind.QueueType or TokenKind.StackType
                 or TokenKind.BagType or TokenKind.ListType or TokenKind.LogType)
```

The six token kinds listed are exactly the non-Lookup collection-type leaders — the set of all `TypeMeta` entries where `Category == TypeCategory.Collection` and the token kind is not `TokenKind.LookupType`. That invariant is already encoded in `Types.All`; the parser needs only to derive it. If a new collection type is added to the catalog, this `if` guard must be updated manually, or the new type will fall through to an error/fallback.

**Rubber-Duck Analysis:**  
`SimpleCollectionTypeLeaders` is a natural derived set — it's exactly `Types.All` filtered by `Category == TypeCategory.Collection && Token.Kind != TokenKind.LookupType`. The catalog encodes this relationship already. The Lookup exclusion is correct because `lookup of K to V` uses a two-param grammar handled by the dedicated `if (current.Kind == TokenKind.LookupType)` branch first. A new collection type added to `Types.cs` will automatically appear in `SimpleCollectionTypeLeaders` without any parser change. Fix is mechanical: define the `FrozenSet` and replace the `is` disjunction with `.Contains()`.

**Resolution:**  
Fixed. Added `SimpleCollectionTypeLeaders` static `FrozenSet<TokenKind>` to `Parser.cs` (lines 164–176), derived from `Types.All` filtered by `Category == TypeCategory.Collection && Token.Kind != TokenKind.LookupType`. Updated `ParseTypeRef()` in `Parser.Declarations.cs` (line ~997) to use `SimpleCollectionTypeLeaders.Contains(current.Kind)` instead of the hardcoded 6-kind `is` disjunction.

---

## GAP-035: `ParseChoiceValue` hardcodes numeric type check with no catalog property

**Status:** Fixed  
**Category:** Catalog-Impl  
**Location:** `src/Precept/Pipeline/Parser.Declarations.cs` `ParseChoiceValue()`, line ~1148  
**Found in iteration:** 9

**Description:**  
`ParseChoiceValue` determines how to parse a choice literal value based on the element type. For the numeric case (integer, decimal, number), it allows an optional leading minus sign followed by a number literal. The check is:

```csharp
if (elemToken.Kind is TokenKind.IntegerType or TokenKind.DecimalType or TokenKind.NumberType)
```

This is a hardcoded three-member disjunction that encodes "is this a numeric choice element type?" There is no `TypeTrait.Numeric` or equivalent catalog property that captures this semantic. If a new numeric type were added to the catalog, this `if` guard would require a manual update, violating the metadata-driven architecture principle.

**Rubber-Duck Analysis:**  
The root issue is that `TypeTrait` has no `Numeric` flag. `TypeTrait.ChoiceElement` marks which types can appear as choice element types at all, but does not distinguish numeric from non-numeric within that subset. Two valid approaches:

1. **Add `TypeTrait.Numeric` to the catalog** — mark Integer, Decimal, Number with this flag; let `ParseChoiceValue` derive a `FrozenSet<TokenKind> NumericChoiceElementTypes` from `Types.ByToken.Where(Traits.HasFlag(TypeTrait.Numeric))`. This is the metadata-driven approach and keeps the parser consistent with the catalog-first design.

2. **Accept the hardcode as a permanent documented exception** — the numeric type set is small and stable; adding a new numeric primitive is a foundational language change warranting manual review of all consumers. The three-way `is` disjunction is self-documenting and stable.

Owner judgment required on whether `TypeTrait.Numeric` is worth introducing as a general catalog trait.

**Resolution:**  
Unresolved. Owner should decide: (a) add `TypeTrait.Numeric` to the `TypeTrait` enum, mark Integer/Decimal/Number types with it, and derive `NumericChoiceElementTypes` in `Parser.cs` for use in `ParseChoiceValue`; or (b) accept the three-way `is` disjunction as a stable, documented hardcode with a comment citing the design rationale.

**Frank's Analysis:**  
**Decision: Option 2 — add `ChoiceLiteralTokens: IReadOnlyList<TokenKind>?` to `TypeMeta`.** Not the trait flag; not a documented exception.

George's analysis described one hardcoded check but the method has two: the numeric-path branch guard (`IntegerType | DecimalType | NumberType`) AND the `elemToken.Kind switch` over String/Boolean below it. `TypeTrait.NumericLiteral` eliminates only the first — the second switch stays, leaving the method half-catalog, half-identity. Only `ChoiceLiteralTokens` eliminates both.

`ChoiceLiteralTokens` is a nullable `IReadOnlyList<TokenKind>` on `TypeMeta`, null for all non-choice-element types (zero churn), populated for the five valid element types: `String → [StringLiteral]`, `Boolean → [True, False]`, `Integer/Decimal/Number → [NumberLiteral]`. The parser derives both the signed-prefix path and the `isValid` check from this field — no `elemToken.Kind is ...` identity anywhere in the method.

`TypeTrait.ChoiceElement` is unchanged: it remains the validation gate for `ChoiceElementTypeKeywords`. `ChoiceLiteralTokens` fills the parse-time dispatch gap. Full decision with implementation brief in `.squad/decisions/inbox/frank-gap035-decision.md`.

---

<!-- Iteration 9 complete: 9 gaps filed — GAP-034(Fixed — ParseTypeRef hardcoded 6-kind collection test replaced with catalog-derived SimpleCollectionTypeLeaders FrozenSet), GAP-035 (Unresolved — ParseChoiceValue hardcodes numeric type check; TypeTrait.Numeric would be needed for a catalog-driven fix), GAP-036 (Fixed — spec §3.8 clear F row extended to include optional fields per §1.2), GAP-037 (Fixed — Modifiers.cs Writable hover updated from retired 'write' verb to v3 'modify ... editable/readonly' syntax), GAP-038 (Fixed — spec §2.1 For left-denotation corrected from ParseExpression(40) to ParseExpression(41) for left-associativity), GAP-039 (Unresolved — spec §3.8 append/enqueue tables claim log-by/queue-by accept simple form but catalog requires explicit 'by'; need owner decision), GAP-040 (Unresolved — BagAccessors.countof has ParameterType: TypeKind.Integer but should be element type T; TypeAccessor design limitation), GAP-041 (Fixed — spec §3.8 quantifier predicate table updated from TypeMismatch to QuantifierPredicateNotBoolean), GAP-042 (Unresolved — parser dispatch has dead branches for CollectionValueBy/CollectionIntoBy/RemoveAtIndex; variant actions excluded from ByTokenKind). C# changes: Modifiers.cs Writable hover description updated. Spec changes: §2.1 For ParseExpression value, §3.8 clear F optional row, §3.8 quantifier predicate diagnostic name. Final state: 42 gaps total, 38 Fixed, 4 Unresolved. -->

---

## GAP-036: Spec §3.8 `clear F` action validation table omits `optional` fields

**Status:** Fixed  
**Category:** Doc-Doc  
**Location:** `docs/language/precept-language-spec.md` §3.8 (line ~1505); `src/Precept/Language/Actions.cs` `ClearApplicable`  
**Found in iteration:** 9

**Description:**  
Spec §3.8 action validation table, `clear F` row:

> `| clear F | set of T, queue of T, stack of T, bag of T, list of T, queue of T by P | — | Not valid on log of T, log of T by P, or lookup of K to V (v3) |`

The "field type required" column lists only collection types. `optional T` fields are missing.

However, spec §1.2 explicitly states:
> "The `null` literal is removed entirely — `optional` fields use `is set`/`is not set` for presence testing and **`clear` for value removal**."

The `Actions.cs` catalog correctly includes `ModifiedTypeTarget(null, [ModifierKind.Optional])` in `ClearApplicable` — meaning `clear F` applies to any optional field regardless of its underlying type. The §3.8 action table is inconsistent with §1.2 and with the catalog.

**Rubber-Duck Analysis:**  
Section §1.2 is authoritative: `clear` on optional fields is the designed mechanism for removing a value, replacing the banned `null` literal. The catalog correctly implements this. The §3.8 action table was written before the optional-field applicability was added to the catalog, or was never updated to reflect §1.2. The fix is a targeted row extension — adding `optional T` to the "field type required" column of the `clear F` row with a note about semantics.

**Resolution:**  
Fixed. Extended the `clear F` row in spec §3.8 action validation table to include `optional T` in the field type required column, with a parenthetical explaining the semantics (resets to "not set").

---

## GAP-037: `Modifiers.cs` `Writable` hover description references retired `write` verb

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `src/Precept/Language/Modifiers.cs` line 138  
**Found in iteration:** 9

**Description:**  
`Modifiers.cs` `ModifierKind.Writable` hover description (line 138):

```csharp
HoverDescription: "The field is directly editable. Without this modifier, the field is read-only by default. Use 'in State write Field' to override per state."
```

The phrase "in State write Field" describes the v2 access mode syntax. In v3, `write` is no longer a reserved keyword (spec §1.2: "The access mode verbs `write`/`read` are removed entirely in favor of the `modify` verb + adjective pattern. `write` and `read` are no longer reserved — they are ordinary identifiers in v3."). The correct v3 syntax is `in State modify F editable` or `in State modify F readonly`.

**Rubber-Duck Analysis:**  
This is a stale doc string in the catalog that was written when `write` was still the access mode verb (v2) and was never updated when the verb was retired in v3. The hover description appears in language server tooltips, completions, and MCP `precept_language` output. Anyone reading the hover would see the old v2 syntax and attempt to use `in State write Field`, which would fail because `write` is no longer a keyword. The fix is a one-sentence update with zero behavior impact.

**Resolution:**  
Fixed. Updated `Modifiers.cs` line 138 hover description: replaced "Use 'in State write Field'" with "Use 'in State modify Field editable/readonly'".

---

## GAP-038: Spec §2.1 `For` left-denotation shows `ParseExpression(40)` but left-associativity requires `ParseExpression(41)`

**Status:** Fixed  
**Category:** Doc-Impl  
**Location:** `docs/language/precept-language-spec.md` §2.1 left-denotation table, `For` row (line ~738)  
**Found in iteration:** 9

**Description:**  
Spec §2.1 left-denotation table:

> `| For | BinaryExpression(LookupAccess, ParseExpression(40)) — lookup field access … |`

The precedence table (same section) shows `for` at level 40 with `left` associativity. For a left-associative binary operator at level `P`, the standard Pratt parser encoding requires the right-operand to be parsed with `ParseExpression(P + 1)` = `ParseExpression(41)` — this prevents the right-hand side from absorbing another `for` at the same level, producing left-to-right grouping.

With `ParseExpression(40)`, a second `for` (level 40) would satisfy `40 >= 40` and be consumed right-associatively, giving `A for (B for C)`. With `ParseExpression(41)`, the second `for` fails `40 >= 41` and is left unconsumed, producing `(A for B) for C`.

The implementation in `Parser.Expressions.cs` correctly uses `nextMinPrec = Precedence + 1 = 41` for non-right-associative operators (via `int nextMinPrec = opInfo.RightAssociative ? opInfo.Precedence : opInfo.Precedence + 1`). The spec entry is a typo — it should read `ParseExpression(41)`, consistent with `Contains` which is at the same level and correctly shows `ParseExpression(41)`.

**Rubber-Duck Analysis:**  
Typo in the spec. `Contains` (also level 40, non-associative) correctly shows `ParseExpression(41)`. The `For` row was likely written by analogy but the `+1` was accidentally dropped. No behavior impact — the implementation is correct. Doc-only fix.

**Resolution:**  
Fixed. Updated spec §2.1 left-denotation table `For` row from `ParseExpression(40)` to `ParseExpression(41)`.

---

## GAP-039: Spec §3.8 `append`/`enqueue` action tables include by-type targets that catalog excludes

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `docs/language/precept-language-spec.md` §3.8 (lines ~1500–1507); `src/Precept/Language/Actions.cs` `ActionKind.Append`, `ActionKind.Enqueue`  
**Found in iteration:** 9

**Description:**  
Spec §3.8 action validation table:

> `| append F Expr | log of T, log of T by P, list of T | T | … |`
> `| enqueue F Expr | queue of T, queue of T by P | T | … |`

Spec also adds: "without `by`, ordering key is derived from the element (v3)" for `enqueue`.

However, the `Actions.cs` catalog:
- `ActionKind.Append.ApplicableTo` = `[TypeKind.Log, TypeKind.List]` — excludes `TypeKind.LogBy`
- `ActionKind.Enqueue.ApplicableTo` = `[TypeKind.Queue]` — excludes `TypeKind.QueueBy`

For `log of T by P` and `queue of T by P`, the explicit-key forms (`append F Expr by Expr`, `enqueue F Expr by Expr`) are the catalog-defined mechanism. The spec's "ordering key derived from element" note implies automatic key derivation, but there is no mechanism for this in the current implementation.

**Rubber-Duck Analysis:**  
Two interpretations:

1. **Spec over-documents an unimplemented feature** — the "ordering key derived from element" note describes a capability that was planned but not implemented. For a `log of T by P`, the key type `P` may differ from the element type `T` (e.g., `log of string by instant`), making automatic derivation impossible in general. The catalog correctly excludes the by-types from simple append/enqueue. Fix: remove `log of T by P` from the `append F Expr` row and `queue of T by P` from the `enqueue F Expr` row in the spec.

2. **Catalog is missing a feature** — simple `append F Expr` on a `log of T by P` field is valid when `T == P`, with the element itself as the ordering key. The catalog should add `TypeKind.LogBy` to `Append.ApplicableTo` and `TypeKind.QueueBy` to `Enqueue.ApplicableTo`. This requires a TypeChecker check that `T == P` for the simple form.

The catalog's `AppendBy` hover explicitly says "Requires 'when not (F contains P)' guard" — implying the key is always user-supplied, not derived. This supports interpretation 1.

**Resolution:**  
Fixed. Owner decision: the key is always required for keyed collections — automatic key derivation is not implemented and not planned. Removed `log of T by P` from the `append F Expr` valid-types column and `queue of T by P` from the `enqueue F Expr` valid-types column in spec §3.8. Corrected the `enqueue F Expr by Expr` note to remove the "ordering key derived from element" claim. Catalog (`Actions.cs`) was already correct and unchanged.

**Keyed-form confirmation (post-fix):**  
Owner confirmed: `append F Expr by Expr` and `enqueue F Expr by Expr` DO support `log of T by P` and `queue of T by P` respectively — the explicit key is always required. Verified that spec §3.8 keyed-form rows are correct:
- `append F Expr by Expr` → valid target `log of T by P` ✅
- `enqueue F Expr by Expr` → valid target `queue of T by P` ✅

No spec or catalog changes were needed for the keyed forms — they were already accurate after the original fix pass.

---

## GAP-040: `BagAccessors.countof` has `ParameterType: TypeKind.Integer` but parameter should be element type `T`

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `src/Precept/Language/Types.cs` line ~153; `docs/language/precept-language-spec.md` §3.6 member access table  
**Found in iteration:** 9

**Description:**  
`Types.cs` `BagAccessors` (line ~153):

```csharp
new TypeAccessor("countof", "Count of a specific element (0 if absent)", ParameterType: TypeKind.Integer),
```

Spec §3.6 member access table:

> `| bag of T | countof(E) | integer | returns how many times E appears in the bag (v3) |`

The parameter `E` in `countof(E)` is a bag element of type `T` — the bag's own element type. Using `ParameterType: TypeKind.Integer` is incorrect: for a `bag of string`, `countof(E)` accepts a `string` argument, not an integer. The return type is `integer`, but the parameter type is `T`.

By contrast, `at(N)` accessors (e.g., in `ListAccessors`) correctly use `ParameterType: TypeKind.Integer` because position `N` is always an integer index. The `countof` entry appears to be a copy-paste of the `at` accessor's `ParameterType` field.

**Frank's Design Decision (2026-05-02 — approved by Shane):**  
Introduce a new `ElementParameterAccessor : TypeAccessor` sealed record — the subtype IS the metadata, mirroring how `FixedReturnAccessor` works for the return-type axis.

```csharp
public sealed record ElementParameterAccessor(
    string    Name,
    string    Description,
    TypeTrait RequiredTraits = TypeTrait.None,
    ProofRequirement[]? ProofRequirements = null
) : TypeAccessor(Name, Description, null, RequiredTraits, ProofRequirements);
```

`BagAccessors.countof` becomes `new ElementParameterAccessor("countof", "Count of a specific element (0 if absent)")`.

Type checker pattern match:
```csharp
accessor switch
{
    ElementParameterAccessor => resolveAgainstElementType,
    _ when accessor.ParameterType is { } fixedType => resolveAgainstFixedType,
    _ => noParameter,
}
```

Option 1 (`ParameterIsElementType: bool` flag) was rejected: creates illegal state when combined with `ParameterType`. Option 2 (`TypeKind.Element` sentinel) was rejected: poisons the `TypeKind` catalog enum with a non-type meta-level instruction. Audit confirmed only `countof` is wrong — `at()` (integer index) and `inZone()` (timezone) are correctly typed.

**Resolution:**  
Fixed (2026-05-02). Added `ElementParameterAccessor : TypeAccessor` sealed record to `src/Precept/Language/Type.cs` after `FixedReturnAccessor`. Changed `BagAccessors.countof` in `src/Precept/Language/Types.cs` from `new TypeAccessor(... ParameterType: TypeKind.Integer)` to `new ElementParameterAccessor(...)`. No MCP serialization changes required — `tools/Precept.Mcp/Tools/` contains only `PingTool.cs`. Added tests: `Bag_CountofAccessor_IsElementParameterAccessor` (verifies subtype and null `ParameterType`) and `SequentialCollectionAccessors_AtAccessor_HasIntegerParameterType` (regression: at() still integer-typed).

---

## GAP-041: Spec §3.8 quantifier predicate table names `TypeMismatch` for non-boolean predicate but dedicated code 106 exists

**Status:** Fixed  
**Category:** Doc-Catalog  
**Location:** `docs/language/precept-language-spec.md` §3.8 (line ~1455); `src/Precept/Language/DiagnosticCode.cs` (line ~106); `src/Precept/Language/Diagnostics.cs` (line ~360)  
**Found in iteration:** 9

**Description:**  
Spec §3.8 quantifier predicate validation table:

> `| Predicate must be boolean | BoolExpr in quantifier resolves to non-boolean type | TypeMismatch |`

However, `DiagnosticCode.cs` defines:

```csharp
QuantifierPredicateNotBoolean = 106,
```

And `Diagnostics.cs` provides a full metadata entry for this code:

```csharp
DiagnosticCode.QuantifierPredicateNotBoolean => new(
    nameof(DiagnosticCode.QuantifierPredicateNotBoolean),
    DiagnosticStage.Type, Severity.Error,
    "Quantifier predicate must be a boolean expression, but this resolves to {0}",
    DiagnosticCategory.TypeSystem),
```

Code 106 is a dedicated diagnostic for exactly this case — it has a specific message template that mentions the resolved type. If the type checker emits code 106 for non-boolean predicates, the spec table is wrong to name `TypeMismatch` (code 18). `TypeMismatch` is a general-purpose diagnostic; code 106 is precise.

**Rubber-Duck Analysis:**  
The dedicated `QuantifierPredicateNotBoolean` (106) code supersedes the general `TypeMismatch` for this case. The spec table was written using `TypeMismatch` as a placeholder or from an earlier design where the dedicated code didn't exist. The fix is mechanical: update the spec table cell from `TypeMismatch` to `QuantifierPredicateNotBoolean`. This does not require reading the TypeChecker to verify — the existence of a dedicated code with an appropriate message is sufficient evidence that it is (or is intended to be) the diagnostic emitted for this case.

**Resolution:**  
Fixed. Updated spec §3.8 quantifier predicate validation table: replaced `TypeMismatch` with `QuantifierPredicateNotBoolean` in the "Predicate must be boolean" row.

---

## GAP-042: Parser `ParseActionStatement` dispatch has dead branches for variant-action SyntaxShapes

**Status:** Fixed  
**Category:** Catalog-Impl  
**Location:** `src/Precept/Pipeline/Parser.Declarations.cs` `ParseActionStatement()` SyntaxShape dispatch; private methods `ParseCollectionValueByStatement`, `ParseCollectionIntoByStatement`, `ParseRemoveAtIndexStatement`  
**Found in iteration:** 9

**Description:**  
`ParseActionStatement()` dispatches based on `meta.SyntaxShape`, where `meta` comes from `Actions.ByTokenKind`. `Actions.ByTokenKind` excludes variant actions (those where `PrimaryActionKind != null`):

```csharp
public static FrozenDictionary<TokenKind, ActionMeta> ByTokenKind { get; } =
    All.Where(m => m.PrimaryActionKind == null)
       .ToFrozenDictionary(m => m.Token.Kind);
```

Three `SyntaxShape` values are exclusively used by variant actions:
- `CollectionValueBy` — only `AppendBy` (primary: Append) and `EnqueueBy` (primary: Enqueue)
- `CollectionIntoBy` — only `DequeueBy` (primary: Dequeue)
- `RemoveAtIndex` — only `RemoveAt` (primary: Remove)

Since variant actions are never in `ByTokenKind`, these three SyntaxShape branches in the dispatch switch are dead code. The corresponding private methods `ParseCollectionValueByStatement`, `ParseCollectionIntoByStatement`, and `ParseRemoveAtIndexStatement` are also dead — they can never be called through `ParseActionStatement`.

The actual handling of the `by`/`at` forms is implemented inline within the primary action parsers:
- `ParseCollectionValueStatement`: after parsing value, checks `Current().Kind == TokenKind.By` for append/enqueue; checks `Current().Kind == TokenKind.At` for remove
- `ParseCollectionIntoStatement`: after parsing `into G?`, checks `Current().Kind == TokenKind.By` for dequeue

**Rubber-Duck Analysis:**  
The inline approach is the correct design for this parser: `by`/`at` disambiguation happens post-value, after the primary action and its mandatory operand have been consumed. The dedicated `ParseCollectionValueByStatement` etc. were created as infrastructure but superseded by the inline dispatch. Two options:

1. **Remove the dead code** — remove `ParseCollectionValueByStatement`, `ParseCollectionIntoByStatement`, `ParseRemoveAtIndexStatement`, and their dead dispatch branches. The inline approach is correct and sufficient. This eliminates confusion.

2. **Leave as documented infrastructure** — the dispatch switch documents the full SyntaxShape taxonomy for future consumers (e.g., a tool that reads `ActionMeta.SyntaxShape` to understand the grammar). The dead code serves as forward compatibility scaffolding.

Note: `ParseInsertAtStatement` and `ParsePutKeyValueStatement` are NOT dead — `Insert` and `Put` have `PrimaryActionKind == null` and are dispatched via `ByTokenKind`.

**Resolution:**  
Fixed (2026-05-02). Shane's decision: delete the dead code entirely. Removed the three dead dispatch arms (`CollectionValueBy`, `RemoveAtIndex`, `CollectionIntoBy`) from `ParseActionStatement`'s switch and deleted `ParseCollectionValueByStatement`, `ParseRemoveAtIndexStatement`, and `ParseCollectionIntoByStatement` methods. Replaced the now-incomplete named-value switch with a discard arm `_ => throw new InvalidOperationException(...)` that documents the unreachability, removing the need for `#pragma warning disable CS8524`. Confirmed `ParseInsertAtStatement` and `ParsePutKeyValueStatement` are untouched. All 2713 tests pass — no behavior change.

`LookupType` is correctly excluded: `lookup of K to V` uses a different multi-keyword syntax (handled by the preceding `if (current.Kind == TokenKind.LookupType)` branch at lines ~965–992).

**Rubber-Duck Analysis:**  
The catalog already encodes collection membership via `TypeCategory.Collection`. The derivation is:
```csharp
Types.All
    .Where(t => t.Category == TypeCategory.Collection
             && t.Token is not null
             && t.Token.Kind != TokenKind.LookupType)
    .Select(t => t.Token!.Kind)
    .ToFrozenSet()
```
This produces exactly `{Set, QueueType, StackType, BagType, ListType, LogType}` today — a safe substitution. New collection types will automatically appear in the set when their catalog entries carry `TypeCategory.Collection`.

Note on `TokenKind.Set` vs `TokenKind.SetType`: the lexer emits `TokenKind.Set` (the keyword token) for the word "set". `TokenKind.SetType` is a parser-synthesized alias present in `Types.ByToken` but never emitted by the lexer. `TypeKind.Set.Token.Kind` returns `TokenKind.Set`, so the derived set correctly uses the lexer-emitted token kind. ✅

**Fix:**  
Added `SimpleCollectionTypeLeaders` static field to `Parser.cs` (outer class):

```csharp
/// <summary>
/// Token kinds that lead a "standard" collection type reference using <c>X of T</c> syntax.
/// Excludes <see cref="TokenKind.LookupType"/> — lookup uses <c>lookup of K to V</c> and is
/// handled by a dedicated path in <c>ParseTypeRef</c>. Derived from
/// <see cref="Types.All"/> filtered by <see cref="TypeCategory.Collection"/> — never hardcoded.
/// </summary>
internal static readonly FrozenSet<TokenKind> SimpleCollectionTypeLeaders =
    Types.All
        .Where(t => t.Category == TypeCategory.Collection
                 && t.Token is not null
                 && t.Token.Kind != TokenKind.LookupType)
        .Select(t => t.Token!.Kind)
        .ToFrozenSet();
```

Replaced the hardcoded `is TokenKind.Set or TokenKind.QueueType or ...` condition in `ParseTypeRef` with:

```csharp
if (SimpleCollectionTypeLeaders.Contains(current.Kind))
```

---

## GAP-035: `ParseChoiceValue` hardcodes `IntegerType | DecimalType | NumberType` for "numeric choice type"

**Status:** Unresolved  
**Category:** Catalog-Impl  
**Location:** `src/Precept/Pipeline/Parser.Declarations.cs` `ParseChoiceValue()`, line ~1147  
**Found in iteration:** 9

**Description:**  
`ParseChoiceValue` checks whether a choice element's declared type requires a `NumberLiteral` input (as opposed to a `StringLiteral` or `True`/`False`) using a hardcoded disjunction:

```csharp
if (elemToken.Kind is TokenKind.IntegerType or TokenKind.DecimalType or TokenKind.NumberType)
{
    // optional leading minus + NumberLiteral; fold signed literal
    ...
}
```

This check means "the value for this choice element is expressed as a numeric literal (possibly signed)." No `TypeTrait`, `TypeCategory`, or other catalog property currently expresses this semantic. The closest proxy — `TypeTrait.Orderable & TypeCategory.Scalar` — identifies the same three types today, but is semantically wrong: it means "this type supports ordinal comparison" rather than "this type's literal representation is a number."

**Rubber-Duck Analysis:**  
The parser is making a domain decision: "For which types is the choice literal a `NumberLiteral` token (possibly prefixed by `-`)?" The answer — `integer`, `decimal`, `number` — is language-level knowledge that belongs in the catalog. Four possible resolutions:

1. **New `TypeTrait.NumericLiteral` flag** — add a trait to `TypeMeta` that marks types whose choice-position literal is a number. Semantically precise. Requires a catalog change and adds a new trait concept.
2. **`LiteralTokenKind` property on `TypeMeta`** — each type declares which `TokenKind` it expects in literal/choice position (e.g., `NumberLiteral`, `StringLiteral`, `True`/`False`). Most general; allows full dispatch from the catalog. Significant catalog API change.
3. **`TypeCategory.Numeric` (new category or repurpose)** — rename/add a category specifically for numeric types. Workable but requires designing the category taxonomy more carefully.
4. **Leave as-is** — the three-type set is stable; no new numeric types are planned. Acceptable as a documented exception to the catalog-derivation rule.

The current proxy `TypeTrait.Orderable & TypeCategory.Scalar` would work today (only Integer/Decimal/Number are both Orderable and Scalar), but using it as a proxy for "accepts NumberLiteral" would break if a future type (e.g., a `ratio` type) is Orderable+Scalar but uses a different literal form, or if `string` gains orderable semantics.

**Ownership:** Requires a catalog design decision before a fix can be applied. No C# changes made this iteration.

**Frank's Analysis:**  
**Decision: Option 2 (refined) — add `ChoiceLiteralTokens: IReadOnlyList<TokenKind>?` to `TypeMeta`.** Not `TypeTrait.NumericLiteral`; not a documented exception.

This method has **two** hardcoded dispatches, not one. The first is the numeric-path guard (`IntegerType | DecimalType | NumberType`). The second is the `elemToken.Kind switch` over String and Boolean below it (lines 1176–1181). `TypeTrait.NumericLiteral` eliminates only the first; the second switch stays, leaving the method half-catalog, half-identity. Only `ChoiceLiteralTokens` resolves both simultaneously.

`ChoiceLiteralTokens` is a nullable `IReadOnlyList<TokenKind>?` on `TypeMeta` (null default — zero churn on the ~25 non-choice types). Populated for all five `ChoiceElement` types: `String → [StringLiteral]`, `Boolean → [True, False]`, `Integer/Decimal/Number → [NumberLiteral]`. The signed-prefix path becomes `literalTokens.Contains(TokenKind.NumberLiteral)`. The `isValid` switch becomes `literalTokens?.Contains(cur.Kind) == true`. No `elemToken.Kind is ...` identity anywhere in the method.

`TypeTrait.ChoiceElement` is unchanged — it remains the validation gate for `ChoiceElementTypeKeywords`. `ChoiceLiteralTokens` fills the parse-time dispatch gap. These serve different pipeline stages and both belong in the catalog.

Option 3 (documented exception) fails the architecture test: the catalog-system doc explicitly states that "consumers hardcoding per-member knowledge that should be metadata" is the smell, and a parser switch on enum identity to apply per-member parse behavior is exactly that smell. The "stable small set" argument misapplies the documented-exception bar.

Full decision and implementation brief in `.squad/decisions/inbox/frank-gap035-decision.md`.

**Resolution:**  
Fixed (2026-05-02). Added `ChoiceLiteralTokens: IReadOnlyList<TokenKind>?` (default null) to `TypeMeta` in `src/Precept/Language/Type.cs`. Populated on all 5 choice-element types in `src/Precept/Language/Types.cs`: `String → [StringLiteral]`, `Boolean → [True, False]`, `Integer/Decimal/Number → [NumberLiteral]`. Refactored `ParseChoiceValue` in `Parser.Declarations.cs` to look up `ChoiceLiteralTokens` via `Types.ByToken` — both the signed-numeric guard (`literalTokens?.Contains(TokenKind.NumberLiteral) == true`) and the `isValid` check (`literalTokens?.Contains(cur.Kind) == true`) are now catalog-derived. No `elemToken.Kind is ...` identity anywhere in the method. Added 9 tests: `NumericChoiceElementTypes_HaveNumberLiteralInChoiceLiteralTokens`, `StringChoiceElementType_HasStringLiteralInChoiceLiteralTokens`, `BooleanChoiceElementType_HasTrueAndFalseInChoiceLiteralTokens`, `NonChoiceElementTypes_HaveNullChoiceLiteralTokens`, `AllChoiceElementTypes_HaveNonNullChoiceLiteralTokens`, plus 5 parser round-trip tests for all element types including signed literals and error diagnostics.

<!-- Iteration 9 complete: George's Catalog-Impl audit of lexer + parser.Lexer fully catalog-driven — no violations. Prior fixes verified: GAP-029 (IsOutcomeAhead uses OutcomeKeywords.Contains ✅), GAP-030 (KeywordsUsableAsFunctionNames derived from Functions.All ∩ Tokens.Keywords ✅), GAP-031 (binding powers read from Operators.ByToken/ByTokenSequence ✅), GAP-032 (PPowIntExp ProofRequirement on integer pow overload ✅), GAP-033 (ModifierKind.cs Notempty comment updated ✅). New gaps: GAP-034 (Fixed — ParseTypeRef hardcoded 6-token collection-type guard → SimpleCollectionTypeLeaders derived from Types.All(Collection, not Lookup)), GAP-035 (Unresolved — ParseChoiceValue hardcodes IntegerType|DecimalType|NumberType for numeric choice literal; no catalog property encodes "accepts NumberLiteral"; requires owner decision on TypeTrait.NumericLiteral or LiteralTokenKind). Status: 35 gaps total, 34 Fixed, 1 Unresolved. -->

## Schema Reference

Each gap uses this template:

```
## GAP-NNN: <short title>
**Status:** Open | Fixed | Unresolved
**Category:** Doc-Doc | Doc-Spec | Doc-Catalog | Catalog-Impl | Doc-Impl
**Location:** <file> §<section> (line N)
**Found in iteration:** N
**Description:** <what the inconsistency or gap is — be specific, quote the conflicting text>
**Rubber-Duck Analysis:** <always present — agent reasoning: what causes this, what the correct answer is, what risk there is in fixing it>
**Resolution:** <if Fixed: what was changed and where; if Unresolved: why it needs owner judgment>
```

**Category definitions:**
- `Doc-Doc` — two language docs contradict each other
- `Doc-Spec` — a type doc contradicts `precept-language-spec.md` (spec wins)
- `Doc-Catalog` — a doc names a token/type/action/operator/function/code not in the C# catalog, or vice versa
- `Catalog-Impl` — a catalog entry is unused/unreachable in the lexer or parser
- `Doc-Impl` — documented syntax is not implemented (or implementation diverges from docs)
