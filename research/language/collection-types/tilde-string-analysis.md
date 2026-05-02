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
