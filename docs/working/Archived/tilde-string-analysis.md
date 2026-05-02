# ~string: Syntax and Scope Analysis

**Author:** Frank (Senior Language Designer)
**Branch:** `spike/Precept-V2`
**Date:** 2026-05-21
**Related docs:** `docs/language/primitive-types.md` · `docs/language/collection-types.md` · `docs/language/precept-language-spec.md` · `docs/philosophy.md` · `research/language/expressiveness/case-insensitive-comparison-survey.md` · `research/language/expressiveness/case-insensitive-implementation-survey.md`

---

## Summary

`~string` as a collection inner type is the correct and coherent syntax given that `~=` and `!~` are locked: the `~` sigil is now committed as the case-insensitivity marker across the Precept language surface, and `~string` is its direct, zero-cost extension into the type system. Any keyword-based alternative would fracture the case-insensitivity vocabulary — authors would need to learn `~` for operators and an unrelated keyword for types, with no bridge between them.

Expanding `~string` to scalar fields is not recommended at this time. Auto-promotion of `==` semantics on a `~string` field violates Precept's explicitness commitment and undermines the `~=` operator's clarity. Pure annotation without enforcement is inert and inconsistent with how the language's type system works. An enforcement model — where `~string` on a scalar field requires `~=` for equality comparisons and makes `==` a compile error — is intellectually coherent and preserves explicitness, but lacks the empirical usage evidence that would motivate adding this complexity now. The `~=` operator handles the scalar comparison case explicitly; the collection type handles the container case. The remaining gap is addressed by convention, not language features, until usage evidence says otherwise.

---

## Locked Design — Scalar ~string

**Status:** Locked. All review gaps resolved by owner (Shane). This section is the canonical implementation reference; the analysis sections below are supporting rationale. Item 4 of the original `## Locked decisions` section ("`~string` is collection-only") is superseded by this section.

---

### What is valid

```precept
field Email  as ~string            # valid — scalar field type
field Code   as ~string notempty   # valid — constraints compose normally
event Login(Email as ~string)      # valid — event arg declaration
field Labels as set of ~string     # valid — collection inner type (unchanged)
```

`~string` is valid as a scalar field type and as a collection inner type. Storage is always case-preserving — the `~` modifier affects comparison and membership semantics only, never the stored representation.

---

### Core enforcement rules (all three ship together)

**Rule 1 — Equality:** `==` or `!=` on any `~string` field (either operand position) is a compile error.

| Wrong | Correct |
|-------|---------|
| `Email == "admin@example.com"` | `Email ~= "admin@example.com"` |
| `Email != someValue` | `Email !~ someValue` |

- `CaseInsensitiveFieldRequiresTildeEquals` (for `==`), `CaseInsensitiveFieldRequiresTildeNotEquals` (for `!=`)
- Fires when `~string` appears in either operand position
- Message: *`'Email' is declared ~string (case-insensitive). Use ~= instead of == to avoid treating 'admin@example.com' and 'Admin@example.com' as different values.`*

**Rule 2 — CS collection contains CI value:** `collection of string contains ~string field` is a compile error.

- `CaseInsensitiveValueInCaseSensitiveContains`
- Fires when: collection inner/key type is `string` (not `~string`) AND the right operand is a `~string` field
- Applies to all collection kinds (`set`, `queue`, `stack`, `log`, `bag`, `list`) and `lookup` key access
- Inverse direction (`collection of ~string contains string value`) is fine — no diagnostic
- Message: *`'Email' is ~string but 'Roles' is set of string (case-sensitive). A value like 'Admin' stored as 'admin' would not be found. Either change 'Roles' to set of ~string, or use a quantifier to test membership explicitly.`*

**Rule 3 — String prefix/suffix functions:** `startsWith(~string field, ...)` or `endsWith(~string field, ...)` is a compile error.

| Wrong | Correct |
|-------|---------|
| `startsWith(Email, "admin@")` | `~startsWith(Email, "admin@")` |
| `endsWith(Email, ".com")` | `~endsWith(Email, ".com")` |

- `CaseInsensitiveFieldRequiresTildeStartsWith`, `CaseInsensitiveFieldRequiresTildeEndsWith`
- Messages:
  - *`'Email' is declared ~string (case-insensitive). Use ~startsWith instead of startsWith to avoid treating 'admin@...' and 'Admin@...' as different prefixes.`*
  - *`'Email' is declared ~string (case-insensitive). Use ~endsWith instead of endsWith to avoid treating '.com' and '.COM' as different suffixes.`*

---

### New operators

| Operator | Syntax | Semantics | Valid when |
|----------|--------|-----------|-----------|
| `~startsWith` | `~startsWith(field, prefix)` | `OrdinalIgnoreCase` prefix test | First arg is `~string` |
| `~endsWith` | `~endsWith(field, suffix)` | `OrdinalIgnoreCase` suffix test | First arg is `~string` |

Using `~startsWith`/`~endsWith` on a plain `string` field is a type error. Using `startsWith`/`endsWith` on a `~string` field is `CaseInsensitiveFieldRequiresTildeStartsWith`/`CaseInsensitiveFieldRequiresTildeEndsWith`.

---

### String functions unaffected

`trim`, `left`, `right`, `mid`, `toLower`, `toUpper` — CI semantics do not apply. These functions are always ordinal. No enforcement check.

---

### Ordering operators — documented asymmetry, no enforcement

`<`/`>`/`<=`/`>=` on a `~string` field use ordinal, case-sensitive lexicographic ordering — identical to `<`/`>` on a plain `string` field. No compile error; no `~<`/`~>` operators will be added. The `~` modifier declares CI equality intent only. Document explicitly; behavior is not hidden.

---

### Type unification (if/then/else)

| Branches | Result type | Rationale |
|---------|------------|-----------|
| `~string` + `~string` | `~string` | |
| `~string` + `string` | `~string` | CI preserved — selection, not transformation; `~=` on a CS value is a harmless no-op |
| `string` + `string` | `string` | |

---

### Event arg declarations

`event Foo(Email as ~string)` is valid. The author is declaring CI comparison intent for how those args are to be used. The CI obligation applies at every comparison site that receives the arg.

---

### Excluded: `choice of ~string`

`~string` is **not** a valid `ChoiceElementType`. `choice of ~string("draft", "active")` is a type error. Storage invariant conflict: `choice` guarantees the stored value IS the declared canonical string; `~string` never normalizes storage. These contracts are irreconcilable without new surface. Boundary normalization (`toLower(event.Arg)` before assigning to a choice field) is the correct pattern.

---

### Implementation model

**No new `TypeKind`.** `TypeKind.String` with `CaseInsensitive = true` flag on the type reference node. `ScalarTypeRefNode` must gain a `CaseInsensitive` property (currently only `CollectionTypeRefNode` has it).

**Parser:** Additive change to `ParseTypeRef()` — new `~` path for scalar field declarations. Existing collection inner type path is unchanged. `CaseInsensitiveStringOnNonCollection` (code 66) is retired when this ships — it was the guard preventing scalar `~string`. Parser emits `ExpectedToken` for standalone `~` outside valid positions (unchanged).

**Type checker:** Three enforcement rules above. The CI flag must be carried per field reference in `SemanticIndex`. All checks fire at the comparison site, not the declaration site.

**Runtime:** `lookup of ~string to V` must be constructed with `ImmutableDictionary.Create(StringComparer.OrdinalIgnoreCase)`. This is the only runtime change — all other CI semantics are compile-time enforcement.

**Diagnostic catalog:** Retire `CaseInsensitiveStringOnNonCollection` (code 66). Add: `CaseInsensitiveFieldRequiresTildeEquals`, `CaseInsensitiveFieldRequiresTildeNotEquals`, `CaseInsensitiveValueInCaseSensitiveContains`, `CaseInsensitiveFieldRequiresTildeStartsWith`, `CaseInsensitiveFieldRequiresTildeEndsWith`.

---

## Question A: Is ~string the best syntax?

### Symbol coherence

The `~` symbol in Precept now carries a single, consistent meaning: **case-insensitivity**. It appears in three positions:

| Surface | Form | Meaning |
|---|---|---|
| Comparison operator | `~=` | Ordinal, case-insensitive equality (`OrdinalIgnoreCase`) |
| Comparison operator | `!~` | Ordinal, case-insensitive not-equals |
| Collection inner type | `~string` | Selects `OrdinalIgnoreCase` comparer for the collection |

The conceptual model is a **prefix modifier**: `~` modifies whatever immediately follows it. In the operator domain, `~=` is "case-insensitive equals" and `!~` is "case-insensitive not-equals." In the type domain, `~string` is "case-insensitive string." The modifier's payload is identical across all three positions: "ignore case in comparisons."

There is a syntactic surface difference worth acknowledging: in `~=`, the `~` modifies the operator's comparison semantics; in `~string`, the `~` modifies the type's identity. These are different syntactic roles. But the reader's mental model is the same — "~ means case-insensitive" — and that's the only model they need to carry. A domain expert who learns `~=` will correctly read `set of ~string` as "a set of case-insensitive strings" without additional instruction.

The mathematical tradition of `~` as "approximation" or "equivalence under relaxation" maps naturally here: in Precept, the relaxation is precisely "ignore case." The operator choice was contested in the research phase (see `case-insensitive-comparison-survey.md`, which recommended against `~=` due to Lua confusability and cascade pressure), but the owner committed to it — and that commitment is now an asset for the symbol's coherence in the type system. The `~` sigil carries its meaning precisely because the operator surface established it.

**PostgreSQL precedent worth noting:** PostgreSQL uses `~` and `~*` (tilde-star) for case-sensitive and case-insensitive regex matching respectively. That system establishes tilde as a comparison-mode modifier. Precept uses it differently (tilde before `=` rather than after the operator), but the general reading of "tilde = case-insensitive variant" has prior art in the broader tooling ecosystem.

### Alternatives evaluated

#### `~string` (current)

```precept
field Labels as set of ~string
field Tags   as set of ~string
```

- **Symbol cost:** Reuses the already-committed `Tilde` token; no new symbols introduced.
- **Grammar cost:** Zero. `~` before `string` in collection inner type position is already handled by the existing `Tilde` token and the parser rule for `ScalarType`.
- **Readability for domain experts:** Dense but consistent. Once `~=` is understood, `~string` requires no new concept or vocabulary.
- **Discoverability:** Authors who know the `~=` operator have a clear bridge to `~string`. Completions and hover can reinforce the connection.
- **Cascade coherence:** If the `~` family ever expands (e.g., CI `contains` operator, CI `startsWith`), `~string` and the operator set remain one unified vocabulary.
- **Verdict:** Correct.

#### `string(ci)` — parameterized type

```precept
field Labels as set of string(ci)
```

- **Readability:** Poor. `(ci)` reads as a function call to a domain expert. "String-ci" has no natural English meaning.
- **Grammar cost:** High. Introduces type parameterization syntax that Precept doesn't use elsewhere. Worse: `choice` already uses `(...)` for value enumeration — `choice of string("a", "b", "c")`. Adding `string(ci)` creates a collision between "value enumeration arguments" (for `choice`) and "type modifier arguments" (for `string(ci)`). Same syntax, two incompatible semantics.
- **Symbol coherence:** None. Completely disconnected from `~=`/`!~`. Authors who know the CI operators have no reason to look for `string(ci)`.
- **Verdict:** Rejected on two independent grounds — grammar collision and zero coherence with the operator vocabulary.

#### `string ignorecase` — post-positional modifier keyword

```precept
field Labels as set of string ignorecase
```

- **Readability:** High. Reads naturally as English; domain experts can parse it without instruction.
- **Grammar cost:** Low. `ignorecase` would be a keyword after `string` in a type position. There is precedent in the language: `ordered` already modifies `choice of T(...)` post-positionally.
- **Symbol coherence:** None. Completely disconnected from `~=`/`!~`. An author familiar with `~=` has no reason to guess that the type system uses `ignorecase` instead of `~`.
- **Vocabulary fracture:** The language would have two incompatible ways to express case-insensitivity: `~` (symbol-based) for scalar comparisons, and `ignorecase` (keyword-based) for types. Authors learning the language would encounter two separate concepts that express the same domain idea — and nothing in either surface would point to the other. This is the most damaging cost: not the grammar, but the conceptual split.
- **Verdict:** Rejected. High readability in isolation, but creates a vocabulary fracture that undermines the `~` commitment. The two surfaces must speak the same language.

#### `ci string` — prefix keyword

```precept
field Labels as set of ci string
```

- **Readability:** Poor. `ci` is a programming abbreviation, not a natural English word. Domain experts don't recognize it.
- **Symbol coherence:** None.
- **Verdict:** Rejected. Worse than `string ignorecase` on all dimensions.

#### `nocase string` — prefix keyword variant

```precept
field Labels as set of nocase string
```

- **Readability:** Medium. "nocase" appears in PostgreSQL collation names and some tool documentation, but it's not common English.
- **Symbol coherence:** None.
- **Verdict:** Rejected.

#### `string~` — suffix tilde (hypothetical)

```precept
field Labels as set of string~
```

- Post-fix modifier pattern not established in Precept. Reads oddly; misses the clean "reads like a type modifier" quality of `~string`. The tilde after the word looks like a trailing character rather than a prefix that semantically modifies the type.
- **Verdict:** Rejected.

### Recommendation

**`~string` is the right syntax, definitively.** The `~=` and `!~` operators locked the `~` symbol as the case-insensitivity sigil. `~string` is the direct extension of that sigil into the type system — same symbol, same concept, same mental model, zero new vocabulary.

The only legitimate critique is discoverability: a domain expert who has not yet encountered `~=` might not know what `~` means. But this critique applies equally to any alternative — `string ignorecase` is no more discoverable than `~string` to a reader who hasn't read the documentation. Discoverability is a tooling problem (completions, hover, error messages), not a syntax problem. The syntax problem is coherence, and `~string` has it.

Grammar cost: zero. Cognitive load for anyone who knows `~=`: zero. Coherence with the operator vocabulary: complete. `~string` stays.

---

## Question B: Scalar field scope

### The gap

Authors writing guards and rules on scalar string fields must use `~=` for case-insensitive comparisons. The field declaration carries no signal that case-insensitivity is the semantically correct interpretation for that field. This creates three concrete friction points:

**1. Discipline burden.** Every comparison on a semantically case-insensitive field must explicitly use `~=`. A single `==` where `~=` was intended is a silent semantic bug — the rule compiles, fires, and produces the wrong answer for mixed-case input. The compiler cannot catch it because it doesn't know the field is semantically CI.

**2. Documentation absence.** `field Email as string` communicates nothing about intended comparison semantics. Whether `Email == "admin@example.com"` is correct or a bug depends entirely on out-of-band knowledge about the domain.

**3. Collection asymmetry.** `field Emails as set of ~string` gives the collection correct membership semantics automatically — "Admin@Example.COM" and "admin@example.com" are the same element. The scalar field `field Email as string` gets none of that — the case-insensitivity intent lives only in the author's head and in comparison expressions scattered through the file.

This gap is most visible for identifier-like fields: email addresses, coupon codes, product SKUs, ISO country codes, department codes. These are semantically case-insensitive in their domain (an email address comparison is always CI; a coupon code lookup is always CI), but the type system treats them identically to fields where case matters (e.g., a display name, a file path on a case-sensitive filesystem).

The gap is real. The question is whether language features are the right response.

### Semantics if expanded

Four distinct semantic options exist if `field Email as ~string` were made valid:

#### Option 1: Auto-promotion — `==` on `~string` behaves like `~=`

```precept
field Email as ~string

# Under this option, these two expressions would be semantically identical:
Email == "admin@example.com"   # silently OrdinalIgnoreCase
Email ~= "admin@example.com"   # explicitly OrdinalIgnoreCase
```

`==` on a `~string` field is silently case-insensitive. The stored value is case-preserving; only comparison semantics change.

**Against — explicitness violation.** Identical surface syntax (`==`) produces different behavior depending on the declared type of the left operand. The reader of an expression must know both the operator and the field's declared type to understand the comparison's semantics. This is exactly the SQL collation model — and its failure mode is well-documented: comparisons look case-sensitive but are not, and the type declaration that controls the behavior is not visible at the comparison site.

Precept's `~=` operator was specifically designed to make case-insensitive comparisons *explicit at the point of use*. Auto-promotion undermines this commitment at its root.

**Against — `~=` becomes ambiguous.** If `==` on `~string` is already CI, what does `~=` mean on that same field? Two possibilities: it means the same thing (and is redundant), or it is a type error (and authors must un-learn the explicit operator). Neither is acceptable.

**Against — language principle.** From the language spec §0.1 Principle 6: "Explicit domain meaning over primitive convenience." Auto-promotion is implicit convenience precisely where the language demands explicit meaning.

**Verdict:** Rejected.

#### Option 2: Pure annotation — `~string` documents intent, does not change `==` semantics

```precept
field Email as ~string

# Authors still required to write:
rule EmailMatchesAdmin
    Email ~= "admin@example.com"
because "..."
```

`~string` is a declaration of domain intent. Comparison semantics are unchanged. `~=` is still required everywhere.

**Against — inert annotation.** The type annotation has no behavioral consequence. The discipline burden is not reduced — the compiler cannot tell the difference between "I know this field is CI and deliberately used `~=`" and "I accidentally used `==` on a field that should have been compared with `~=`."

**Against — inconsistent with how Precept's type system works.** Every type declaration in Precept carries behavioral meaning: `integer` restricts to whole numbers, `notempty` requires non-empty content, `optional` triggers presence-guard obligations. An annotation that changes nothing about compilation or runtime behavior is not a Precept type feature — it's a comment in angle brackets.

**Verdict:** Rejected. Pure annotation without enforcement is inert; Precept doesn't do inert type annotations.

#### Option 3: Enforcement — `~string` requires `~=`, makes `==` a compile error on that field

```precept
field Email as ~string

# Compile error: CaseInsensitiveFieldRequiresTildeEquals
Email == "admin@example.com"

# Required:
Email ~= "admin@example.com"   # correct — explicit CI comparison

# Assignment is unchanged:
set Email = SubmitForm.Email   # fine — storage is case-preserving
```

`~string` is a **type-level constraint**: it declares the field is semantically case-insensitive and requires every equality comparison to use `~=` or `!~`. Using `==` or `!=` on a `~string` field is a `CaseInsensitiveFieldRequiresTildeEquals` compile error.

**In favor — preserves explicitness.** Every comparison remains visibly `~=`, not secretly `==`. The `~=` operator retains its role as the complete, unambiguous signal of case-insensitive comparison. The difference from Option 1: the type system *requires* explicit syntax, rather than replacing it with implicit syntax.

**In favor — adds compiler enforcement.** The type checker catches "accidentally used `==` on a CI field" at compile time. This is the value proposition: the declaration enables a proof obligation — you cannot silently use the wrong comparison operator.

**Analogy to `optional`.** `optional` fields require `is set` guards before certain accesses; the type carries a proof obligation. `~string` on a scalar field would require `~=`/`!~` for equality comparisons; the type carries a comparison obligation. The pattern is the same: the type declares semantic intent, and the compiler enforces consistent usage.

**In favor — coherent with `~=`.** The explicit `~=` operator is not undermined; it is elevated. `~string` fields make the use of `~=` *mandatory*, not optional. Authors learning the language get a clear message: "`~string` means the engine will require you to use `~=` when comparing this field."

**Against — novel proof obligation class.** "Operator restriction by type" is new ground for Precept's type system. Every existing proof obligation is about *presence* or *range* (is the value set? is it in bounds?), not about *which operator to use*. This is a meaningful design boundary to cross, and it should be crossed with data, not theory.

**Against — no usage evidence yet.** Whether accidentally using `==` on semantically-CI scalar fields is a frequent, real bug class in practice is unknown. Without empirical evidence from real precept authoring, this complexity addition is speculative.

**Verdict:** Intellectually the most coherent option if scalar `~string` is to be added. But not warranted by current usage evidence. Mark as "revisit when we have real authoring data."

#### Option 4: Keep the current restriction

```precept
field Email as string   # always

rule EmailMatchesAdmin
    Email ~= "admin@example.com"   # always explicit
because "..."
```

No change to semantics, type system, parser, or compiler. The gap remains.

**In favor — consistent with philosophy.** Every comparison is explicit. `~=` is visible at every use site. There is no hidden behavior anywhere. The discipline burden is real but so is its benefit: reading any guard or rule tells you immediately whether the comparison is CS or CI.

**In favor — collection coverage.** For the container case, `set of ~string` already provides correct membership semantics. The scalar case is covered by `~=` discipline. The gap is real but bounded.

**Against — documentation absence persists.** The field declaration communicates nothing about intended comparison semantics. This is a philosophical loss: the contract is incomplete without this intent.

**Verdict:** Correct for now. The gap is real, the discipline-based solution works, and no available option improves on it without tradeoffs that aren't yet justified by usage evidence.

### Coherence with ~= operator

The `~=` and `!~` operators exist to make case-insensitive comparisons **explicit at the point of use**. This is the critical design commitment: the reader of an expression knows exactly whether a comparison is case-sensitive or case-insensitive by looking at the operator token. No field declaration lookup required.

Auto-promotion (Option 1) breaks this commitment: `Email == "admin"` could be CI or CS depending on the field type. The reader must know both the operator AND the field's declared type to understand comparison semantics. The operator is no longer a complete signal.

The enforcement option (Option 3) preserves and strengthens this commitment. You still write `~=` for CI comparisons — the operator is still the complete, explicit signal. The type declaration adds compiler enforcement of *consistent* `~=` usage, but it doesn't hide the operator or make `==` implicitly CI. The operator remains the visible, unambiguous syntax.

The distinction is:
- **Auto-promotion:** type declaration makes `==` *mean* something different (hidden, implicit)
- **Enforcement:** type declaration makes `~=` *required* (explicit, enforced)

Precept's philosophy permits enforcement — it's the whole product — and prohibits hidden behavior. Only Option 3 is coherent with that combination.

### Alternatives

| Option | Auto-promotes `==` | Compile-time enforcement | Consistent with `~=` philosophy | Verdict |
|---|---|---|---|---|
| Current (restriction) | No | N/A — feature absent | ✓ | **Keep for now** |
| Auto-promotion | Yes | No (silent) | ✗ | Rejected |
| Pure annotation | No | No | ✓ | Rejected (inert) |
| Enforcement (`==` → error on `~string` fields) | No | Yes | ✓ | Future candidate |

### Recommendation

**Keep the current restriction.** `field Name as ~string` remains a compile-time error (`CaseInsensitiveStringOnNonCollection`).

The rationale is compact:

1. **Explicitness is non-negotiable.** Auto-promotion produces type-dependent operator semantics — the same `==` expression means different things depending on the field's declared type. This is exactly what `~=` exists to prevent.

2. **Inert annotations are not a Precept pattern.** If the declaration does nothing behavioral, it's a comment, not a type feature.

3. **The enforcement option is coherent but premature.** If empirical usage shows that accidentally using `==` on semantically-CI scalar fields is a frequent, real bug class, Option 3 is the correct answer. The design is clear; the motivation is not yet established.

4. **The gap is partially covered.** `set of ~string` handles the container case. `~=` handles the scalar comparison case explicitly. The declaration-documentation gap is real but addressed by convention until the evidence base for Option 3 exists.

---

**Future direction marker — if Option 3 is revisited:**

The exact semantics if scalar `~string` is implemented with enforcement:

- `field Email as ~string` is valid as a scalar field type declaration.
- `Email == expr` and `Email != expr` on a `~string` field are `CaseInsensitiveFieldRequiresTildeEquals` and `CaseInsensitiveFieldRequiresTildeNotEquals` compile errors respectively.
- `Email ~= expr` and `Email !~ expr` are the required forms — always explicit, always visible.
- Assignment (`set Email = expr`), storage, `.length`, string interpolation, and all non-comparison operations are unchanged. Storage is always case-preserving.
- Type compatibility: `string` values are assignable to `~string` fields and vice versa — the type modifier affects comparison obligations, not storage format. This means `~string` is structurally `string` with an additional constraint layer, not a distinct storage type.
- In rules and ensures: a `~string` field compared with `==` in any expression position is a compile error. The rule author sees: "this field requires `~=` for equality comparisons."
- The diagnostic text should teach: "Field `Email` is declared as `~string` (case-insensitive). Use `~=` instead of `==` to compare it."

---

## Locked decisions

The following are established and should not be re-litigated in derived proposals:

1. **`~=` (case-insensitive equals) is locked.** `string ~= string` is the canonical form for ordinal, case-insensitive string equality. `OrdinalIgnoreCase` semantics.

2. **`!~` (case-insensitive not-equals) is locked.** `string !~ string` is the canonical form for CI inequality. These two operators together form the complete CI equality surface.

3. **`~` is the case-insensitivity sigil for Precept.** It is not to be replaced with a keyword in any future CI type or operator feature (`ignorecase`, `ci`, `nocase` are all off the table). Future features in the case-insensitivity space must start from `~`.

4. **`~string` is collection-only in the current implementation.** `field Name as ~string` is a compile-time error (`CaseInsensitiveStringOnNonCollection`). Question B's conclusion is to keep this restriction until empirical usage evidence motivates the enforcement option.

5. **`~string` is valid for all four collection kinds.** `set of ~string`, `queue of ~string`, `stack of ~string`, `log of ~string` are all valid. The `OrdinalIgnoreCase` comparer governs `contains` semantics consistently across all of them.

6. **Storage is always case-preserving.** `~string` — whether as a collection inner type or a future scalar field type — affects comparison and membership semantics, never the stored value. A stored `"Admin@Example.COM"` is retrieved as `"Admin@Example.COM"` regardless of how comparisons on it are evaluated.

7. **The `~` token is a lexer-level primitive.** `CaseInsensitiveEquals: ~=`, `CaseInsensitiveNotEquals: !~`, and `Tilde: ~` are three distinct tokens. Scan order ensures `~=` is attempted before `~`, so `x ~= y` never tokenizes as `x ~ = y`. A standalone `~` outside a collection inner type position is a lexer error — this rule must be preserved even if scalar `~string` is added.

---

## Composition Analysis

**Author:** Frank (Senior Language Designer)
**Branch:** `spike/Precept-V2`
**Date:** 2026-05-28
**Trigger:** Owner is revisiting the deferral of scalar `~string` and considering locking in Option 3 (enforcement). This section is the full composition audit that was missing from the prior analysis — every surface the type must touch if it ships.

---

### Summary

Scalar `~string` under the enforcement model (Option 3) is compositionally sound on most surfaces: assignment is bidirectional with no storage change, non-comparison operations are unaffected, and the `~=`/`!~` requirement maps cleanly onto the existing diagnostic infrastructure. The one genuine crack in the enforcement model is the **mixed-collection case**: a `~string` scalar value flowing into a `contains` test against a `set of string` (or vice versa) uses the collection's comparer, not the value's type — and the collection's comparer is case-sensitive. The enforcement model as described catches `==`/`!=` on a `~string` field directly; it does not catch a `~string` value being fed to a case-sensitive `contains`. This is the most dangerous composition gap. The ordering operator gap (`<`, `>`) is real but lower severity — authors are already on ordinal ordering for string comparisons everywhere, so the inconsistency is expected and bounded. The `lookup of ~string to V` key type introduces a novel runtime constraint (the dictionary must use `OrdinalIgnoreCase` as its key comparer) that has no precedent in the current collection backing type model. None of these gaps are blockers — each has a defined resolution — but collectively they mean that locking in scalar `~string` today commits the type checker, the diagnostic catalog, the backing type for `lookup`, and three language specification sections simultaneously.

---

### Assignment compatibility

**`string` → `~string` field (`set Email = event.StringArg`):** Fine. Storage is case-preserving regardless of the field's type modifier. The assignment copies the raw UTF-16 value; no normalization, no folding. The type checker permits this because `~string` is `string` with an added comparison obligation — it is not a narrower storage type. This is the intended model. The analogy: assigning to a `notempty` field does not require the assigned value to be provably non-empty at the assignment site (that is a constraint violation, caught separately) — the type still accepts assignment from a plain non-empty `string`.

**`~string` → `string` field (`set Alias = Email`):** Fine. A `~string` value is structurally a `string` at the storage level. Assigning it to a `string`-declared field drops the comparison obligation. The receiving field has no CI requirement; that is the author's responsibility. This direction is asymmetric with respect to obligation: the CI intent is lost at the destination, but there is no way to propagate it without making every `string` field aware of how its value originated — which would be a whole-program analysis, not a local type check.

**`~string` → `~string` field:** Fine and the cleanest case. Both sides have the same comparison obligation. No friction anywhere.

**String literal → `~string` field (`set Email = "admin@example.com"`):** Fine. A string literal is `string`; assignment to `~string` follows the `string → ~string` rule above. No constraint check needed at the assignment site.

**`~string` field as event argument (where the event arg is declared `string`):** Fine by the same asymmetric assignability rule. The value is passed as a `string`; the CI obligation does not follow the value through the call boundary. This is the same loss-of-intent that occurs in any assignment from `~string` to `string`, and the resolution is the same: it is the author's responsibility at the receiving side.

**`~string` field as source in `default` initialization:** Fine. `field Email as ~string default "admin@example.com"` — the default is a string literal assigned to a `~string` field, identical to the literal-assignment case above.

**Summary:** Assignment compatibility is simple because `~string` is not a different storage representation — it is `string` with a comparison obligation. The type checker must permit all assignment directions (both `string → ~string` and `~string → string`) without error, and add the comparison obligation check only at the comparison site, not the assignment site.

---

### Comparison operators

Every analysis here assumes the enforcement model: `==`/`!=` on a `~string` operand are compile errors; `~=`/`!~` are the required forms.

**`~string == string` (error):** The left operand is a `~string` field. `==` is prohibited. Error: `CaseInsensitiveFieldRequiresTildeEquals`. Teaching message: "Field `Email` is declared `~string` (case-insensitive). Use `~=` instead of `==` to compare it."

**`string == ~string` (error — right operand):** The enforcement check must also cover the right-operand position. An author could write `"admin@example.com" == Email` and the error should be symmetric. The diagnostic is the same: `CaseInsensitiveFieldRequiresTildeEquals`. The type checker must detect `~string` in either operand position of `==`/`!=`.

**`~string == ~string` (error — both CI):** Both sides require `~=`. Same error. There is no reason to special-case this — if one CI field is compared to another CI field using `==`, the result would still be case-sensitive, which is the wrong semantic.

**`~string ~= string` (correct, fine):** This is the intended expression. The `~=` operator is always `OrdinalIgnoreCase` regardless of operand type — it does not need the type to be `~string`. The `~string` obligation is satisfied.

**`~string ~= ~string` (correct, fine):** Both sides are CI-declared. The comparison is CI. Semantically ideal. Fine.

**`string ~= ~string` (fine — `~=` always CI regardless):** The `~=` operator applies `OrdinalIgnoreCase` regardless of whether the operands are `~string` or `string`. An author comparing a plain `string` to a `~string` field with `~=` is doing the right thing. No error. This is an important symmetry property: `~=` is defined on `(string, string)` operands (see `OperationKind.StringCaseInsensitiveEqualsString`), and `~string` values are `string` at the operation level. The enforcement check is not on the operator — it is on the field declaration when `==` is used.

**`~string != string` (error):** `!=` is the CS inequality operator. Same enforcement: `CaseInsensitiveFieldRequiresTildeNotEquals`.

**`~string !~ string` (correct):** The CI not-equals operator. Satisfies the obligation. Fine.

**`~string < string`, `~string > string`, `~string <= string`, `~string >= string` — ordering operators:**

**Revised — see `### Ordering operators — revised assessment` below.** The prior claim that ordering operators are not available on `string` was incorrect. `<`, `>`, `<=`, `>=` ARE valid on `string` — they perform ordinal, case-sensitive lexicographic comparison. This was confirmed as the correct language spec behavior; `primitive-types.md` has been corrected accordingly. See the revised assessment section for the full analysis of what this means for `~string` and the enforcement model.

**Conclusion:** The enforcement model for comparison operators is complete for the equality surface. No ordering operator clause is needed (the revised assessment explains why). The only specification requirement is that the `~string` check fires on both operand positions of `==` and `!=`, not only on the left.

---

### Contains operator — the dangerous case

This is the critical composition tension. The `contains` operator's semantics are determined by the **collection's** inner type, not the value being tested. If the collection is `set of string`, `contains` is case-sensitive — full stop, regardless of what the right operand's type is. The type checker today only checks that the value's type is compatible with the collection's inner type (both are `string`), and `string` is assignment-compatible with `~string` and vice versa.

**`set of string contains ~string value` — the dangerous case:**

```precept
field AdminEmails as set of string   # ordinal key comparer
field Email       as ~string

when AdminEmails contains Email      # compiles — but this is a CS contains of a CI value!
```

The semantics: `AdminEmails` uses `string` equality internally. `contains` iterates the set using `OrdinalIgnoreCase` only if the inner type is `~string`. Since the inner type is `string`, the comparer is ordinal case-sensitive. The `~string` type of `Email` is invisible to the `contains` operator — the collection decides the comparison mode.

**Impact:** `"Admin@Example.COM"` will not match `"admin@example.com"` in the set. The bug is silent — the code compiles, the logic is wrong.

**Should the compiler detect this?** Yes. This is exactly the class of silent semantic bug that the enforcement model is designed to catch. The mismatch is structurally detectable: the left operand is a collection with a CS inner type, and the right operand is a `~string` field. The intended semantics (CI comparison) are declared on the value; the actual semantics (CS comparison) are imposed by the collection. These are detectably incompatible.

**Proposed diagnostic:** `CaseInsensitiveValueInCaseSensitiveContains` (warning or error — see below). Teaching message: "Field `Email` is declared `~string` (case-insensitive), but `AdminEmails` is a `set of string` (case-sensitive). The `contains` test will use case-sensitive comparison — `\"admin@example.com\"` and `\"Admin@Example.COM\"` are treated as different values. Either declare `AdminEmails as set of ~string` or use an explicit `~=` in a quantifier predicate."

**Severity:** Error. This is not "stylistically inconsistent" — it is a provably wrong semantic that the author almost certainly did not intend. The enforcement model's job is to make this structurally impossible, not just warned about.

---

**`set of ~string contains string value` — OrdinalIgnoreCase contains, plain string value:**

```precept
field Tags   as set of ~string
field Prefix as string

when Tags contains Prefix
```

The collection uses `OrdinalIgnoreCase`. The value being tested is a plain `string`. This is fine — no mismatch. The `contains` semantics are CI because the collection declared them so. The value's type does not need to be `~string` for the operation to be correct. No diagnostic needed.

---

**`set of ~string contains ~string value` — fully consistent:**

The canonical case. Both collection and value agree on CI semantics. Fine.

---

**`queue of string contains ~string value` — same as set of string case:**

The `contains` operator on `queue of string` is case-sensitive. Same dangerous mismatch. Same diagnostic applies: `CaseInsensitiveValueInCaseSensitiveContains`.

---

**`queue of ~string contains ~string value` — fully consistent:** Fine.

---

**`log of ~string contains ~string value` — fully consistent:** Fine.

---

**`log of string contains ~string value`:** Same dangerous case — the log's comparer is CS. Same diagnostic.

---

**`bag of string contains ~string value`:** Same — the bag uses the inner type's equality for `contains`. Same diagnostic.

---

**`bag of ~string contains ~string value`:** Fine.

---

**`list of string contains ~string value`:** Same dangerous case. Same diagnostic.

---

**`lookup of string to V contains ~string key` — case-sensitive key lookup with CI key:**

```precept
field CoverageLimits as lookup of string to decimal
field CoverageType   as ~string

when CoverageLimits contains CoverageType   # CS key lookup with a CI key
```

The `contains` on a `lookup of string to V` checks key membership using the dictionary's key equality — which is case-sensitive when the key type is `string`. A `~string` field value being used as the lookup key will miss case-variant entries. Same dangerous mismatch, same diagnostic family. However, the diagnostic message should be tailored for the lookup case: "Field `CoverageType` is declared `~string`, but `CoverageLimits` is a `lookup of string to decimal` (case-sensitive keys). Key lookup will use case-sensitive comparison."

This also applies to `F for K` (value access): if `CoverageType` is `~string` and the lookup key type is `string`, both the `contains` guard and the `for` access should trigger the diagnostic.

---

**`lookup of ~string to V contains string key` — CI key lookup with plain string key:**

The `lookup of ~string to V` uses `OrdinalIgnoreCase` for key equality (see below). Passing a plain `string` as the lookup key is fine — the comparer is determined by the collection, not the key argument's type. No diagnostic needed.

---

**Cross-collection diagnostic rule (complete):**

The type checker must check `contains` (and `lookup for K`) expressions for the following mismatch:
- Left operand is a collection with a `string` (not `~string`) inner type (or key type for `lookup`)
- Right operand is a `~string` field (or expression resolving to a `~string` field)
→ Emit `CaseInsensitiveValueInCaseSensitiveContains` (Error)

The inverse direction (CI collection, plain string value) is fine and requires no diagnostic.

---

### `~string` as a lookup key type

**Declaration:**

```precept
field CoverageLimits as lookup of ~string to decimal
```

This declares a lookup whose key space uses `OrdinalIgnoreCase` equality. The language semantics must be:
- `put CoverageLimits "MEDICAL" = 100` followed by `put CoverageLimits "medical" = 200` → the second `put` **overwrites** the first, because `"MEDICAL"` and `"medical"` are the same key under `OrdinalIgnoreCase`. The result is `CoverageLimits for "Medical"` returns `200` (or whatever the last `put` wrote). This is correct and desirable — it mirrors how `set of ~string` handles deduplication.
- `CoverageLimits contains "Medical"` returns `true` if any case-variant of `"medical"` has been `put`.

---

## Frank's Final Design Review
**Date:** 2026-05-01

Read in full: `primitive-types.md`, `collection-types.md`, `precept-language-spec.md`. Cross-checked grammar productions, operator tables, type unification rules, enforcement rule coverage, and cross-feature interactions. The design is well-reasoned and the three-rule enforcement model is internally sound. I found no blocking issues. Three important gaps and four minor ones are documented below — all are spec coverage deficits that could cause an implementation to diverge from the intended behavior if the author follows only one document.

---

### Issues Found

**1. Spec §3.6 `contains` operator table is missing the CI enforcement rule**
**Location:** `precept-language-spec.md` § 3.6 — `contains`
**Severity:** Important

The spec's `contains` type-rules table shows `set of T | T → boolean`, `queue of T | T → boolean`, `stack of T | T → boolean`. There is no row, footnote, or reference documenting that `collection of string contains ~string` is a compile error (`CaseInsensitiveValueInCaseSensitiveContains`). This enforcement rule lives only in `primitive-types.md §~string`. An implementer working from the spec alone would write a `contains` type-check that passes when it should fail.

**Recommended fix:** Add a row or enforcement note to the `contains` table in §3.6: "If the collection's inner/key type is `string` and the right operand is `~string`, emit `CaseInsensitiveValueInCaseSensitiveContains`." Cross-reference `primitive-types.md §~string` Enforcement Rule 3.

---

**2. Spec §3.6 `==`/`!=` operator row doesn't carve out `~string`**
**Location:** `precept-language-spec.md` § 3.6 — Binary operators, `==`/`!=` row
**Severity:** Important

The `==`/`!=` row says "any T | same T → boolean". This implies `~string == ~string` and `~string == string` are valid expressions — but both are compile errors (`CaseInsensitiveFieldRequiresTildeEquals`). The enforcement rule is documented in `primitive-types.md` but absent from the operator table in §3.6. The `~=`/`!~` row correctly documents those as the required forms for `~string`, but without an explicit carve-out on the `==`/`!=` row, an implementer would allow the wrong operators.

**Recommended fix:** Add a note to the `==`/`!=` row: "Exception: if either operand is `~string`, `==`/`!=` are compile errors — use `~=`/`!~` instead (`CaseInsensitiveFieldRequiresTildeEquals` / `CaseInsensitiveFieldRequiresTildeNotEquals`)."

---

**3. Spec §3.6 `+` operator: `string + ~string` combination is unspecified**
**Location:** `precept-language-spec.md` § 3.6 — Binary operators, `+` rows for string
**Severity:** Important

The operator table documents three string concatenation cases: `string + string → string`, `~string + ~string → string`, `~string + string → string`. The case `string + ~string` is missing. This is directly author-reachable: `"Hello, " + EmailField` where `EmailField` is `~string` produces `string + ~string`. A strict match against the table finds no row — an implementer who table-drives the type check would emit a spurious type error on ordinary concatenation code.

The correct result is `string` (same logic as `~string + string` — concatenation is transformation, CI qualifier does not survive). But the spec must document it.

**Recommended fix:** Add a fourth row: `` `+` | `string` | `~string` | `string` | No (concatenation — CI qualifier not preserved) ``. The table is currently asymmetric on left/right string-type combinations.

---

**4. `primitive-types.md §~string` enforcement rule uses "field" where it should say "expression"**
**Location:** `primitive-types.md` — `~string` Enforcement rules, Rule 1
**Severity:** Minor

Rule 1 says "`==` or `!=` on any `~string` **field** (either operand position) is a compile error." The word "field" is too narrow. The enforcement fires based on the *type* of the expression, not whether it's a bare field reference. An if/then/else expression whose result type is `~string` (e.g., `if Cond then EmailField else OtherEmail`) must also trigger CI enforcement at downstream comparison sites — the CI qualifier propagated through the conditional. Event args are separately called out as covered ("CI obligation applies at every comparison site that receives the arg"), but they're also not "fields."

If an implementer reads "field" literally and implements the check as "field reference with CI flag = true" rather than "expression with type CaseInsensitive = true," they'd miss CI enforcement on if/then/else results.

**Recommended fix:** Change "on any `~string` field (either operand position)" to "on any `~string`-typed expression (either operand position)." This aligns with the implementation model described later in the same section: CI is a property of the `ScalarTypeRefNode`, not of the field declaration site.

---

**5. `collection-types.md §Membership Operator` CI note omits `log of ~string`**
**Location:** `collection-types.md` — Membership Operator section, "Case sensitivity" note
**Severity:** Minor

The case-sensitivity note at the end of the section reads: "`contains` on `set of string` is case-sensitive (ordinal). `contains` on `set of ~string` is case-insensitive. `contains` on `queue of ~string` and `stack of ~string` is also case-insensitive." The note omits `log of ~string`, even though the `~string` inner type subsection two paragraphs above explicitly states it is valid for `log` and uses `OrdinalIgnoreCase`. A reader scanning only the case-sensitivity note would conclude `log of ~string` has no CI behavior.

**Recommended fix:** Extend the note: "…`contains` on `queue of ~string`, `stack of ~string`, and `log of ~string` is also case-insensitive."

---

**6. Spec §2.1 places type checker diagnostics in the parser section**
**Location:** `precept-language-spec.md` § 2.1 — Null-denotation table, `Tilde` row
**Severity:** Minor

The `Tilde` null-denotation entry includes this parenthetical: "compile error otherwise (`CaseInsensitiveFieldRequiresTildeStartsWith` / `CaseInsensitiveFieldRequiresTildeEndsWith` if `startsWith`/`endsWith` was used without `~`)". Those diagnostics fire when `startsWith(~stringField, ...)` is called — that is a type checker check at a regular function call site, not a parser behavior. The parser section is describing what the parser does when it sees `~startsWith`; the enforcement for when it *doesn't* see `~` belongs in §3.6.

**Recommended fix:** Remove the cross-diagnostic parenthetical from §2.1. Add a dedicated row in §3.6 (function call type rules): "Calling `startsWith`/`endsWith` with a `~string` first argument emits `CaseInsensitiveFieldRequiresTildeStartsWith`/`CaseInsensitiveFieldRequiresTildeEndsWith`."

---

**7. `collection-types.md §Quantifier Predicates` lexer note misstates status of `no`**
**Location:** `collection-types.md` — Quantifier Predicates, "Lexer note"
**Severity:** Minor

The note says "`each` and `no` require new lexer entries." `no` is already a reserved keyword in the spec (§1.2: listed under outcomes, "Prefix for `no transition`"). It does not need a new lexer entry — it needs parser disambiguation (the same pattern as `set` in type vs. action position, and `min`/`max` in constraint vs. function position). `each` genuinely does require a new lexer entry. The incorrect claim about `no` could mislead the implementer into thinking there's a lexer conflict to resolve when there isn't one.

**Recommended fix:** Change the note to: "`any` is already a reserved keyword. `each` requires a new lexer entry. `no` is already reserved (for `no transition`) and requires only parser disambiguation in the quantifier expression position — same dual-use pattern as `set` and `min`/`max`."

---

### Sign-off conditions

Issues 1–3 should be resolved before implementation begins — all three would produce silent behavioral divergence from the design intent in a spec-driven implementation. Issues 4–7 are documentation clarifications that should land in the same edit pass as the implementation plan but do not block design approval.

**Backing type implication — the novel constraint:**

The current `lookup of K to V` backing type is `ImmutableDictionary<K, V>` (see collection-types.md). The default `ImmutableDictionary` uses the type's default equality comparer — for `string`, that is `StringComparer.Ordinal`. To support `lookup of ~string to V`, the dictionary must be created with `StringComparer.OrdinalIgnoreCase` as its key comparer.

This is not automatically handled. `ImmutableDictionary<string, V>` with the default comparer will not give `OrdinalIgnoreCase` semantics even if the declared key type is `~string`. The runtime evaluator must detect the `~string` key type and create `ImmutableDictionary.Create(StringComparer.OrdinalIgnoreCase)` instead of the default.

This is the **only place in the entire type system where a runtime backing type must be parameterized differently based on the `~string` flag.** It is a contained change — one call site in the evaluator's collection factory method — but it is a real implementation requirement that does not exist anywhere else in the current codebase.

The type checker must also ensure that `~string` is not valid as the value type `V` of a lookup — there is no meaningful CI semantic for values, only for keys. `lookup of string to ~string` should be a type error: `CaseInsensitiveStringOnNonCollectionKey`. (A value in a lookup is just stored and retrieved; comparison semantics on the value are irrelevant for lookup purposes.)

Wait — actually this needs more thought. If someone declares `lookup of string to ~string`, that means the values are `~string`-typed. From the scalar enforcement perspective, when a value is retrieved via `CoverageLimits for someKey`, the returned type would be `~string`, and subsequent comparisons on it would require `~=`. Is this valid? It's logically coherent under the enforcement model. But it's also strange: values in a lookup are not searched over using a comparer — they're just returned. The `~string` value type on a lookup would only matter if you assigned the retrieved value to a field. At that point it would follow the normal `~string → string` or `~string → ~string` assignment rules. **Conclusion: `lookup of string to ~string` is fine under the enforcement model — the type propagates normally.** No need to prohibit it.

For the key type position specifically: `lookup of ~string to V` is valid and meaningful. The CI flag on the key type changes the comparer. All other collection types that could have `~string` as an inner type already handle this at the comparer level (e.g., `set of ~string`). `lookup of ~string to V` is a direct extension of the same pattern.

**`countof` accessor:** `lookup` does not have `.countof` — that's `bag`. No issue here.

**Summary for lookup:**
- `lookup of ~string to V` is valid, meaningful, and requires `OrdinalIgnoreCase` key comparer at runtime.
- `put` with case-variant keys overwrites, which is correct and expected.
- The runtime evaluator needs one new code path to select `OrdinalIgnoreCase` when the key type is `~string`.
- This is the only implementation surface where the `~string` flag affects a backing type selection.

---

### Rules and ensures

**`rule Email == "admin@example.com"` where Email is `~string` — error at rule site:**

Yes. The enforcement model applies everywhere `==` appears on a `~string` field — in guards, in rules, in ensures, in conditional expressions. A rule using `==` on a `~string` field is the same error as any other `==` on a `~string` field. Teaching message: "Rule expression uses `==` on `~string` field `Email`. Use `~=` for case-insensitive comparison."

**`ensures Email ~= SubmitForm.Email` — fine:**

Correct usage. The ensure uses `~=`; the obligation is satisfied.

**`rule no a in Addresses (a == Email)` where Email is `~string` and Addresses is `list of string`:**

Two issues compound here. The quantifier binding `a` has type `string` (from `list of string`). `Email` is `~string`. The `==` operator is used. This triggers:
1. `CaseInsensitiveFieldRequiresTildeEquals` — `Email` is `~string` compared with `==`.
2. Potentially also `CaseInsensitiveValueInCaseSensitiveContains` is not directly triggered here (this is `==` not `contains`), but the `==` error is.

If rewritten as `a ~= Email`, both issues resolve: `~=` satisfies the `~string` comparison obligation, and `~=` is `OrdinalIgnoreCase` regardless of whether `a` is `string` or `~string`.

**`ensures Email == ""` — empty-check special case:**

This is the subtle one. An empty string check (`Email == ""`) is conceptually not a CI comparison — there is only one empty string, and it is both ordinal-equal and CI-equal to `""`. The author could argue: "I'm checking for emptiness, not comparing a business value. `==` should be fine here."

The enforcement model as described does not special-case this. `Email == ""` where `Email` is `~string` would be a `CaseInsensitiveFieldRequiresTildeEquals` error. The correct form is `Email ~= ""`, which works — `"" ~= ""` is true under any string comparer.

**Should empty-string be special-cased?** No. The reason:
1. Correctness: `"" ~= ""` is always true, so the correct form compiles and works.
2. Complexity: adding an "except empty string literal" carve-out to the enforcement rule creates a leaky abstraction — what about `Email ~= SomeOtherEmptyField` where the other field might be empty? The special case cannot be generalized without whole-value-range analysis.
3. Alternatives: the `notempty` constraint is the idiomatic Precept way to express non-emptiness. `field Email as ~string notempty` makes the emptiness constraint structural — no `== ""` check needed at all.
4. Teaching opportunity: the error message can note that `~=` also works for empty checks: "Use `~=` for all equality comparisons on `~string` fields — including empty-string checks: `Email ~= \"\"` is equivalent."

**`ensures Email == ""` conclusion:** Error, same as any other `==` on `~string`. Not special-cased. The teaching message should mention the `notempty` constraint and the `~=` alternative.

---

### String operations

**`.length` on `~string` field:** Unaffected. `.length` returns `integer` (UTF-16 code unit count). There is no CI vs. CS distinction in a length measurement. Fine.

**String interpolation `"Hello {Email}"` where Email is `~string`:** Fine. The interpolation coerces the field value to its string representation. There is no comparison in interpolation; the `~string` constraint is irrelevant. The stored value is case-preserving — `"Hello Admin@Example.COM"` is what you get.

**`+` concatenation (`Email + "@domain.com"`):** Fine. Concatenation does not involve comparison semantics. The result type is `string` (not `~string` — concatenation always produces a plain `string`). This is correct: the result of concatenating a `~string` and a literal has no inherent CI obligation. An author who wants the result to be CI-compared must use `~=` explicitly.

**All non-comparison operations:** Length, interpolation, concatenation — all fine and unaffected. The `~string` modifier is purely a comparison-obligation marker; it does not affect any non-comparison operation.

---

### `notempty` and `optional` interactions

**`field Email as ~string notempty`:**

Valid. `notempty` on a scalar `string`/`~string` field means the stored value must be non-empty (`""` is rejected). This constraint is purely about the stored value's length — it has no interaction with CI comparison semantics. The `notempty` check is: `value.Length > 0`. No case sensitivity involved.

`notempty` + `~string` is a natural and useful combination. An email field that is both non-empty and CI-compared is a common requirement.

**`field Email as ~string optional`:**

Valid. `optional` means the field may be unset (null in backing storage). The `is set` / `is not set` guards are unaffected by CI semantics — presence testing has nothing to do with comparison. Authors must guard with `Email is set` before comparing `Email ~= someValue`, exactly as they would with any `optional` field.

There is one subtle interaction: `Email is set and Email ~= "admin@example.com"` — this is the correct pattern. The `is set` guard does not satisfy the `~=` requirement; the `~=` is still explicitly required. No issue — the guards are orthogonal obligations.

**`Email is set` guard — unaffected by CI:**

`is set` tests presence, not value. CI semantics are irrelevant. Fine.

**`notempty` as an alternative to `== ""`:**

As noted in the Rules section, `field Email as ~string notempty` structurally prevents empty strings without requiring any `== ""` or `~= ""` check. This is the idiomatic Precept pattern — prefer a structural constraint over a repeated guard expression. The enforcement model does not need to special-case empty-string comparisons because `notempty` is the better tool.

---

### Type compatibility model

**Is `~string` a subtype of `string`, a supertype, or a sibling with bidirectional assignability?**

Neither subtype nor supertype in the traditional sense. `~string` is `string` with an additional comparison obligation. The correct model is:

> `~string` is `TypeKind.String` with a boolean flag `CaseInsensitive = true`. It is not a distinct `TypeKind` member.

This is exactly how the current `CollectionTypeRefNode` represents it — `CaseInsensitive = false` (default) for `set of string`, `CaseInsensitive = true` for `set of ~string`. The flag is a property of the type reference at the declaration site, not a new kind.

For scalar fields, the parallel representation would be `ScalarTypeRefNode` with a `CaseInsensitive = true` flag when the parser sees `field Email as ~string`. The type checker resolves this to `TypeKind.String` with CI = true.

**Assignability rules:**

- `~string` is assignable to `string`: always (drop the CI obligation; the storage is compatible).
- `string` is assignable to `~string`: always (gain the CI obligation; the caller must use `~=` at comparison sites).
- `~string` is assignable to `~string`: always.

These are the only assignability rules needed. There is no covariance or contravariance complexity — both types have the same backing representation.

**Type unification in binary expressions:**

When the type checker unifies `~string` and `string` in a binary expression (e.g., `Email ~= someStringField`):
- The operator `~=` accepts `(string, string)` → `boolean`. Both `~string` and `string` are `TypeKind.String` — the unification succeeds.
- The operator `==` accepts `(string, string)` → `boolean`. Same unification — but the enforcement check fires separately: if either operand is `~string`-typed, `==` is an error.

The type unification itself is simple and orthogonal to the enforcement check. The enforcement check is a post-unification semantic rule: "if the resolved operation is `==` or `!=` AND any operand has `CaseInsensitive = true`, emit `CaseInsensitiveFieldRequiresTildeEquals`."

**Does the type checker need a new type node?**

No new `TypeKind` enum member is needed. `TypeKind.String` remains the storage type. The `CaseInsensitive` flag lives on the type reference node (already established in `CollectionTypeRefNode`; would be extended to `ScalarTypeRefNode`). The semantic index must propagate this flag from the field declaration to the expression type at every use site.

The key implementation question is: does `SemanticIndex` (the type-checked representation) carry the CI flag per field, or is it reconstructed from the syntax tree on demand? Given that the semantic index is the output of the type checker and the input to all downstream tools, it must carry the CI flag per field reference. The simplest representation: the field metadata in `SemanticIndex` carries a `bool CaseInsensitive` alongside the `TypeKind`.

---

### Diagnostics inventory

Every new diagnostic required if scalar `~string` is locked in under the enforcement model:

| Code | Severity | Stage | Trigger | Teaching message |
|------|----------|-------|---------|-----------------|
| `CaseInsensitiveFieldRequiresTildeEquals` | Error | Type | `==` operator where either operand is a `~string` field | "Field `{0}` is declared `~string` (case-insensitive). Use `~=` instead of `==` to compare it — `==` is case-sensitive." |
| `CaseInsensitiveFieldRequiresTildeNotEquals` | Error | Type | `!=` operator where either operand is a `~string` field | "Field `{0}` is declared `~string` (case-insensitive). Use `!~` instead of `!=` to compare it — `!=` is case-sensitive." |
| `CaseInsensitiveValueInCaseSensitiveContains` | Error | Type | `contains` (or `lookup for K`) where the collection/lookup has a `string` (not `~string`) inner/key type and the right operand is a `~string` field | "Field `{0}` is declared `~string`, but `{1}` uses case-sensitive `string` for its {2} type. The `contains` test will use case-sensitive comparison — declare `{1} as {3} of ~string` for case-insensitive membership, or restructure the comparison." |

The `CaseInsensitiveStringOnNonCollection` diagnostic already exists (code 66) and currently fires when `field Name as ~string` appears as a scalar field declaration. **This diagnostic must be removed or suppressed** when scalar `~string` is unlocked — it is the exact guard that currently prevents scalar `~string` from compiling. Its removal is a breaking change to the diagnostic set; anything checking for code 66 in tests or tooling must be updated.

Three diagnostics total are net-new. The existing `CaseInsensitiveStringOnNonCollection` is removed (or retired with a `Removed` marker in the catalog).

**Fix hints for `CaseInsensitiveValueInCaseSensitiveContains`:**
- If the collection is under the author's control: "Change the field declaration to `set of ~string` for case-insensitive membership."
- If the value is the variable: "Remove the `~string` modifier from field `{0}` if case-sensitive comparison is intended, or restructure using a quantifier with `~=`."

---

### What locking in now means

#### Language spec (`docs/language/precept-language-spec.md`)

- **§1.1 Operators table:** The `Tilde` token description currently says "only valid immediately before `string` in a collection inner type position." This must be amended to: "valid as the case-insensitive modifier in a collection inner type position (`set of ~string`) **and** as a scalar field type modifier (`field Name as ~string`)."
- **§1.2 Reserved keywords:** No change needed — `~` is already a token, not a keyword.
- **§3 Type Checker (when implemented):** The type checker spec must document the `~string` enforcement rule: `==`/`!=` on any `~string` field is a compile error; `~=`/`!~` are required. This rule must also cover the `contains`/`for` cross-collection mismatch.

#### Primitive types doc (`docs/language/primitive-types.md`)

- **`~string` section (currently "collection-only"):** This section says explicitly: "`~string` is not a standalone field type — it is only valid as the inner type of a collection." This must be rewritten to describe the scalar field use case: `field Email as ~string` is now valid; it carries a comparison obligation requiring `~=`/`!~`.
- The operator table for `string` must note that on a `~string`-declared field, `==` and `!=` are compile errors and `~=`/`!~` are required.
- The `CaseInsensitiveStringOnNonCollection` diagnostic in the type errors section must be removed and replaced with the new enforcement diagnostics.

#### Collection types doc (`docs/language/collection-types.md`)

- **Inner type system section:** The sentence "**`~string` is collection-only.** `field Name as ~string` is a compile-time error: `CaseInsensitiveStringOnNonCollection`" must be removed or amended to describe the new scalar field behavior.
- **Membership operator section:** The `contains` type rules table must add a row documenting the `CaseInsensitiveValueInCaseSensitiveContains` mismatch diagnostic for `set of string contains ~string value`.
- **`lookup of K to V` section:** Must note that `lookup of ~string to V` selects `OrdinalIgnoreCase` as the key comparer, and `put` with a case-variant key overwrites the existing entry.
- **`~string` in queue, stack, and log:** Existing coverage is correct; no change needed for those cases.

#### Type checker implementation (`src/Precept/Pipeline/TypeChecker.cs`)

The type checker is currently a stub (Phase 3 — `throw new NotImplementedException()`). When implemented, it must:

1. **Parse/resolve scalar `~string`:** `ScalarTypeRefNode` must accept `CaseInsensitive = true` when the parser sees `field X as ~string`. The parser currently emits `CaseInsensitiveStringOnNonCollection` for this case via `DiagnosticCode.CaseInsensitiveStringOnNonCollection`. That check must be removed.

2. **Semantic index field entry:** The field metadata must carry `bool CaseInsensitive`. Every reference to a `~string` field in expression nodes must be annotated with this flag by the type checker.

3. **Binary expression enforcement:** In the `BinaryOperation` expression form check, after resolving the operation (which will succeed — both types are `TypeKind.String`), add: if `op == == || op == !=` AND (left is CI OR right is CI), emit the appropriate `CaseInsensitiveFieldRequiresTildeEquals`/`CaseInsensitiveFieldRequiresTildeNotEquals` diagnostic.

4. **`contains` / `for` cross-check:** In the `contains` expression check, after resolving the collection's inner/key type: if the collection's inner/key type is `string` (not CI) AND the value argument is `~string` (CI), emit `CaseInsensitiveValueInCaseSensitiveContains`.

5. **Lookup `for` cross-check:** Same logic as `contains` but for the `F for K` key access expression.

#### Diagnostics catalog (`src/Precept/Language/DiagnosticCode.cs` and `Diagnostics.cs`)

- **Remove (retire):** `CaseInsensitiveStringOnNonCollection` (code 66). Mark as `Removed` in the catalog with a description of when it was retired.
- **Add:** `CaseInsensitiveFieldRequiresTildeEquals`
- **Add:** `CaseInsensitiveFieldRequiresTildeNotEquals`
- **Add:** `CaseInsensitiveValueInCaseSensitiveContains`

#### Runtime evaluator (`src/Precept/Runtime/Evaluator.cs`)

One implementation change: when creating a `lookup of ~string to V` collection, the evaluator must use `ImmutableDictionary.Create(StringComparer.OrdinalIgnoreCase)` instead of the default constructor. This is the only runtime change required — everything else is compile-time enforcement that prevents reaching invalid runtime states.

#### Parser (`src/Precept/Pipeline/Parser.Declarations.cs`)

The parser currently sets `caseInsensitive = true` only inside the collection inner type path (after `of`). The `CaseInsensitiveStringOnNonCollection` diagnostic is emitted when `~string` appears outside that path. The parser change is: remove the out-of-collection-context error for `~string`, and instead parse `field X as ~string` as a valid scalar field declaration with `CaseInsensitive = true` on the resulting `ScalarTypeRefNode`. The enforcement moves to the type checker.

#### Implementation scope assessment

- **Parser change:** Small. One guard removed, one path extended to produce a CI-flagged `ScalarTypeRefNode`.
- **Diagnostic catalog change:** Small. Three additions, one retirement.
- **Type checker change:** Medium. Two new enforcement rules (equality operator check, contains cross-check). The type checker is not yet implemented (stub), so these are design-time additions to the implementation contract — they go into `precept-language-spec.md §3` and will be implemented in Phase 3.
- **Evaluator change:** Small. One new dictionary construction path for `lookup of ~string to V`.
- **Documentation change:** Medium. Three doc files need substantive edits. The primitive-types section is the most critical (rewrites the `~string` section from scratch).

**Overall: Medium.** Not trivial — touches parser, type checker contract, runtime, diagnostics catalog, and three documentation files — but no architectural changes, no new grammar constructs, no new AST node types. Every change is an extension of an existing pattern, not a new pattern.

---

### Recommendation

**The enforcement model is not complete as described.** The mixed-collection case is a genuine enforcement gap: a `~string` value flowing into a `contains` test against a case-sensitive collection compiles without error and silently produces case-sensitive membership semantics. This is the exact class of bug the feature is meant to prevent.

**The minimum coherent set of enforcement rules (not over-engineered, not under-specified):**

1. **Equality operator rule:** `==` and `!=` on any `~string` field (either operand position) are compile errors. Required forms: `~=` and `!~`. This is the rule described in the prior analysis.

2. **Contains cross-collection rule:** `contains` (and `lookup for K`) where the collection has a `string` (not `~string`) inner/key type and the value argument is a `~string` field is a compile error. This is the new rule the prior analysis did not cover.

These two rules together close the enforcement model. Every other composition surface either (a) has no comparison semantics (assignment, `.length`, interpolation, concatenation) and is therefore outside the enforcement scope, or (b) already behaves correctly without enforcement (CI collection with plain string value — the comparer is determined by the collection).

**What is deliberately left outside the enforcement model:**

- Ordering operators (`<`, `>`, `<=`, `>=`) — valid on `string` (ordinal/CS lexicographic); `~string` does not change their semantics; no enforcement check needed. See `### Ordering operators — revised assessment` below for the full corrected analysis.
- `dequeue into`, `pop into`, `dequeue into by` — these produce a typed value from a collection; the type of the output is the collection's inner type. If the collection is `queue of ~string`, the output value will be `~string`-typed; if `queue of string`, it will be `string`-typed. The enforcement model handles comparisons on those output values through rule 1 above.
- Event argument type boundaries — if a `~string` field is passed as a `string`-typed event argument, the CI obligation is not propagated across that boundary. This is the accepted tradeoff documented in the assignment compatibility section.

**Is locking in now the right call?**

The prior analysis deferred due to lack of empirical usage evidence. The design is clear; the implementation scope is medium. If the owner is willing to commit to the full two-rule enforcement model (including the contains cross-collection diagnostic), locking in now is defensible. The enforcement model is coherent, the implementation scope is bounded, and the documentation changes are identified.

If the owner locks in scalar `~string` but omits the contains cross-collection rule, the feature ships with a known enforcement gap that will catch authors by surprise in exactly the scenario described in this analysis. That is the worse outcome: the feature signals safety without providing it. Either ship the full two-rule model or don't ship scalar `~string` yet.

**Minimum condition for locking in:** Both rules (equality enforcement + contains cross-check) must be in scope for the same implementation slice. They are not separable — rule 1 without rule 2 is misleading.

---

### Ordering operators — revised assessment

**Prior claim (incorrect):** "Ordering operators (`<`, `>`) — not applicable to `string` at all; no gap."

**Corrected state:** Ordering operators (`<`, `>`, `<=`, `>=`) are valid on `string`. They perform ordinal, case-sensitive lexicographic comparison (code-point order on UTF-16). This is the correct language spec behavior — confirmed via `docs/language/precept-language-spec.md` and now reflected in `docs/language/primitive-types.md`, which previously carried the incorrect claim. The prior analysis in this file relied on that documentation error.

**Actual semantics:** `string < string → boolean` (ordinal, CS, lexicographic). `"A" < "a"` is `true`; `"Z" < "a"` is `true` because uppercase code points are numerically lower than lowercase ones in Unicode. This is deterministic and has no locale-sensitive behavior.

---

#### What this means for `~string`

`Email < "m"` where `Email` is declared `~string` is valid, and the comparison uses ordinal, case-sensitive lexicographic ordering. This creates a surface asymmetry: equality comparisons on a `~string` field must use `~=`/`!~` (CI), but ordering comparisons always use ordinal CS semantics. There is no `~<` operator, and the locked decision from `§ Case-Insensitive Comparison: Design rationale` confirms none will be added.

Three options were evaluated:

| Option | Description | Verdict |
|--------|-------------|---------|
| **Allow unchanged** | `<`/`>` on `~string` valid, ordinal/CS semantics, documented explicitly | **Correct** |
| Compile error | `<`/`>` on `~string` is an error with no available remedy | Rejected |
| Warning | Allow but warn that ordering is CS on a CI-declared field | Rejected |

**Recommendation: allow unchanged.** The argument:

1. **No CI ordering variant exists, and none will be added.** The enforcement model is about substituting a correct form for a wrong one: `==` → `~=` (there exists a CI equality operator). For ordering operators, no CI counterpart exists. A compile error on `~string <` without a remedy traps authors who have a legitimate need to range-check a CI-declared field (`CouponCode < "M"` to bucket by alphabetic range, `Email > "a"` to filter records). Making the only available operator an error — with no `~<` in sight — is not enforcement; it is obstruction.

2. **`~string` declares CI equality semantics, not CI semantics for all operations.** The enforcement model targets the equality surface because that is where CS/CI is a meaningful, author-visible choice (`==` vs `~=`). Ordering on strings is always ordinal — there is no "CI version of less-than" with different results an author might intend. The `~` modifier signals "compare membership and equality case-insensitively." It does not signal "forbid every operation that does not have a CI counterpart."

3. **The inconsistency is real but not dangerous.** `Email < "m"` on a `~string` field produces a correct, deterministic result. The semantics are the same as `<` on any `string`. The `~string` modifier adds no ambiguity to the ordering operators — it only matters where `==` vs `~=` creates a meaningful behavioral distinction.

4. **Precept's no-hidden-behavior commitment is satisfied by explicit documentation.** The behavior is not hidden if the docs state it plainly. The requirement is that `<`/`>` on a `~string` field is documented as ordinal/CS, not suppressed silently.

---

#### What the enforcement model needs

**No implementation change.** The enforcement model already covers:
- Rule 1: `==`/`!=` on any `~string` field → error (require `~=`/`!~`)
- Rule 2: `contains`/`for` mismatch between CI value and CS collection → error

Ordering operators require no new rule. The type checker treats `<`/`>` on `~string` fields identically to `<`/`>` on `string` fields — the CI flag is irrelevant to ordering operations.

---

#### What the documentation needs

1. **`docs/language/primitive-types.md` (`~string` section):** Add a note that `<`/`>` on a `~string` field uses ordinal (CS) semantics — the `~` modifier applies only to equality operators, not to ordering. *(This companion documentation update is the only action item.)*

2. **This analysis (done):** The prior claim "ordering operators not applicable to `string` at all — no gap" is corrected. The new accurate statement: "Ordering operators are valid on `string` (ordinal/CS lexicographic); `~string` does not alter their semantics; no enforcement check needed; document explicitly."

---

#### Updated enforcement model clause

> **Ordering operators (`<`, `>`, `<=`, `>=`) on `~string` fields:** Valid. Semantics are ordinal, case-sensitive lexicographic ordering — identical to ordering on a plain `string` field. The `~` modifier does not affect ordering semantics. No enforcement check. Authors must understand that CI equality semantics do not extend to ordering; the docs must state this explicitly.

---

## startsWith / endsWith on ~string

**Question:** When `startsWith(field, prefix)` or `endsWith(field, suffix)` is called where `field` is declared `~string`, should the function auto-promote to `OrdinalIgnoreCase`, silently stay case-sensitive, or require an explicit `~startsWith`/`~endsWith` operator?

### The framework

Two prior decisions establish the principled rule:

- **Equality enforcement:** `==`/`!=` on a `~string` field is a compile error — the author must use `~=`/`!~`. Reason: an explicit CI alternative exists.
- **CI collection promotion:** `set of ~string contains value` silently uses `OrdinalIgnoreCase`. Reason: no `~contains` operator exists; the declared type drives the comparer.

The rule the two decisions jointly imply: **explicit CI form exists → enforce; no explicit CI form exists → the type's declared comparer is authoritative.**

All three options below turn on whether `~startsWith`/`~endsWith` exist.

---

### Option A — Auto-promote

`startsWith(~string field, prefix)` automatically uses `OrdinalIgnoreCase`. No new operators or tokens.

**Analysis:** Falls on the promotion branch of the existing rule. Direct precedent: `set of ~string contains value`. No new language surface. The type checker inspects the `CaseInsensitive` flag on the first argument's resolved type and emits an `OrdinalIgnoreCase` evaluation; the field declaration is the only signal needed.

**Downside:** CI behavior is invisible at the call site. Readers of `startsWith(Email, "admin@")` must know `Email` is `~string` to understand the semantics. This is accepted throughout the collection model but worth naming.

---

### Option B — Allow with asymmetry

`startsWith`/`endsWith` always use `Ordinal` even on `~string` fields. Document the gap.

**Analysis:** Least principled option. `~string` protects equality but leaves prefix/suffix matching in the pre-`~string` world — forcing authors back to `toLower(Email) startsWith toLower(prefix)`, exactly the mechanism-over-intent idiom `~=` was designed to eliminate. The `~string` declaration would be an incomplete guarantee. Given that domain-suffix, scheme, and role-prefix checks are canonical `startsWith`/`endsWith` uses, the "rare enough to ignore" argument doesn't hold. **Not recommended.**

---

### Option C — Explicit `~startsWith` / `~endsWith` operators

Add `~startsWith` and `~endsWith` as new operator tokens. By the existing rule ("explicit CI form exists → enforce"), `startsWith`/`endsWith` on a `~string` field becomes a compile error: `CaseInsensitiveFieldRequiresTildeStartsWith` / `CaseInsensitiveFieldRequiresTildeEndsWith`. Mirrors the `==` → `~=` relationship exactly.

**Analysis:** Maximally consistent with the equality enforcement model. Every string comparison operation on a `~string` field — equality, prefix, suffix — requires an explicit CI form at the call site. Nothing implicit.

**Cost:** Two new operator tokens, two new diagnostic codes, catalog and grammar updates. Not large, but not zero. Authors must learn `~startsWith`/`~endsWith` exist and use them everywhere they apply a prefix/suffix test to a `~string` field.

**Upside:** The enforcement model is complete and uniform. There is no "sometimes explicit, sometimes promoted" split to explain or qualify.

---

### Both A and C are principled

Option A is principled because no explicit CI form exists → the declared type's comparer is authoritative.  
Option C is principled because it *creates* that CI form, which then activates the enforcement branch.  
The difference is a surface/enforcement tradeoff, not a correctness tradeoff. Option B breaks both branches of the rule.

---

### Recommendation

**Option A**, unless the team wants full call-site explicitness for all scalar `~string` comparisons. Auto-promotion has zero surface cost and is consistent with the CI collection precedent. Option C is the stronger consistency story — every `~string` comparison is explicit — but it grows the operator surface and requires authors to adopt two new tokens.

**Key question for Shane:** Do you want `startsWith` on a `~string` field to be a compile error requiring `~startsWith` (Option C — call-site explicitness everywhere, uniform enforcement), or is silent promotion acceptable here as it is for CI collections (Option A — no new operators, declared type drives behavior)?

---

## if/then/else type unification with ~string

**Author:** Frank
**Date:** 2026-07-14
**Trigger:** George's Gap 4 — the prior composition analysis did not state the unification rule. One of the two options below must be locked before scalar `~string` ships.

---

### The question

When one branch of `if/then/else` is `~string` and the other is `string`, what is the result type?

```
field Email as ~string

if IsVip then Email else "guest@example.com"
```

---

### Option 1 — CI dropped (`~string` + `string` → `string`)

As soon as one branch is a plain `string`, the CI flag is dropped and the result is `string`. Analogy: concatenation — `Email + "@domain.com"` → `string`, not `~string`. The CI qualifier does not survive any operation that produces a new or combined value.

**Consequence:** The expression above has type `string`. Comparing it with `==` compiles without error. But when `IsVip = true` and `Email = "ADMIN@EXAMPLE.COM"`, a subsequent `== "admin@example.com"` silently returns `false`. The CI protection was lost at the branch site — triggered by the most natural authoring choice (a plain string fallback). This is exactly the class of silent mismatch the enforcement model was designed to eliminate.

---

### Option 2 — CI preserved (`~string` + `string` → `~string`)

If either branch is `~string`, the result is `~string`. Analogy: `optional` unification — `if cond then SomeField else null` → `optional`. A qualifier carried by any branch propagates to the result because that branch might win.

**Consequence:** The expression above has type `~string`. Comparing it with `==` is a `CaseInsensitiveFieldRequiresTildeEquals` error — the author is forced to `~=`. When the `~string` branch wins, this is exactly correct. When the CS literal branch wins, `~=` is over-cautious but always correct: `OrdinalIgnoreCase` on a value that is already in canonical case is a harmless no-op.

---

### The decisive distinction: selection vs. transformation

Concatenation transforms a value — the result is a new string with no lineage claim from its operands. `if/then/else` **selects** a value: when the `~string` branch wins, the result *is* `Email`, CI semantics intact. Dropping the CI qualifier at the branch site silently discards a protection that is still in force at the value's origin.

The collection precedent supports this direction. `lookup of ~string to V` does not ask whether a given key happened to come from a CI field — the declared comparer applies uniformly. Analogously, a `~string`-typed branch taints the result because the CI path might be taken.

---

### Recommendation: Option 2 — CI preserved

George recommended Option 1 without deep argument. The safety analysis reverses it. The enforcement model's purpose is to make silent case-sensitive comparisons on CI-declared values structurally impossible. Option 1 silently reintroduces exactly that bug — triggered by the most ordinary authoring pattern (a string literal fallback). Option 2 forces `~=` in all cases, which is always correct and never harmful. The asymmetry in authoring cost is negligible; the asymmetry in correctness is not.

**Lock in:** `~string` + `~string` → `~string`; `~string` + `string` → `~string`; `string` + `string` → `string`.

---

## choice of ~string

**Question:** Should `choice of ~string("draft", "active")` be valid — meaning membership validation and equality use `OrdinalIgnoreCase`?

**For (Shane's inclination):** If `~string` is valid in scalar field position, excluding it from `choice of T` looks compositionally inconsistent. Authors ingesting external data (event args, API payloads) that may arrive in mixed case would benefit from declared-CI membership semantics rather than normalizing at every ingestion site.

**Against:**

*1. The storage invariant breaks.* `choice` works because the stored value IS the declared canonical string — that identity is what makes downstream equality, serialization, and ordered rank reliable. `~string` never normalizes storage: `"DRAFT"` stays `"DRAFT"`. Assign `"DRAFT"` to `choice of ~string("draft"`)`: does the runtime store `"DRAFT"` or normalize to `"draft"`? If it stores `"DRAFT"`, ordinal comparisons against `"draft"` will fail silently. If it normalizes, that contradicts the `~string` storage model, which has no normalization concept anywhere in Precept today. This isn't a design question that can be answered "later" — it determines the entire backing type contract.

*2. The enforcement model can't compose.* Scalar `~string` works by requiring `~=` on equality and diagnosing `==` as an error. But `choice` doesn't have `~=` — it uses `==` only. For `choice of ~string`, the engine must either silently promote `==` to CI (invisible, contradicts every enforcement principle established for scalar `~string`) or make `==` on a CI choice an error pointing toward an operator that doesn't exist for the choice type. Neither is coherent without new surface.

*3. George's control argument holds, and the workaround is trivial.* For literals embedded in guards and rules, the author controls both sides — CI is pure noise. For dynamic values, `set Status = toLower(event.StatusArg)` normalizes at the ingestion boundary in one line, leaving the choice field ordinal-clean downstream. This is already idiomatic Precept.

**Recommendation: Exclude.** The storage invariant break is structurally blocking — this is not a syntax question but a type coherence question with no clean resolution inside the existing model. Add one sentence to the spec: `~string` is not a valid `ChoiceElementType`. CI membership on choice fields is a boundary-normalization problem, not a field-declaration problem.

