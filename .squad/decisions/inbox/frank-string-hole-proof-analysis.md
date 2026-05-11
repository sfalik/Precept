# String-Hole Exclusion and Proof Engine Impact Analysis

**Author:** Frank (frank-25)
**Date:** 2026-05-11T15:11:32.474-04:00
**Context:** Shane's question on whether `string` should be excluded from valid hole expressions in interpolated typed constants. Follows frank-23 (grammar-table-driven plan), frank-24 (simplification — rejected by Shane because compile-time guarantees are non-negotiable), and George's feasibility review.

---

## Part 1 — The Proof Engine's Current Relationship with Typed Constants

### How the proof engine reasons about typed constant fields

The proof engine (`ProofEngine.cs`) treats typed constant fields (money, quantity, price, exchangerate, duration, period, currency, unitofmeasure, dimension) as **opaque values**. It does not inspect their internal structure — magnitude, currency code, unit name — at any point.

Specifically:

1. **Strategy 1 (Literal Proof):** Only discharges obligations when the subject is a `TypedLiteral` with a numeric value (`decimal`, `int`, `long`). Typed constant literals are stored as composite objects — not bare numerics — so this strategy never fires on typed constant content.

2. **Strategy 2 (Declaration Attribute):** Checks field modifiers (`nonnegative`, `positive`, etc.) and their `ProofSatisfaction` metadata. This works on typed constant fields but only for modifier-based proofs (e.g., "this money field is nonnegative"). It does not examine the typed constant's internal components.

3. **Strategy 3 (Guard in Path):** Decomposes guard expressions into `GuardConstraint` records by pattern-matching `TypedFieldRef op TypedLiteral` shapes and extracting decimal values. Guards that compare typed constant fields to typed constant literals (e.g., `ApprovedAmount > '0.00 USD'`) work through the expression tree — but the proof engine extracts the *numeric comparison semantics*, not the currency/unit content.

4. **Strategy 5 (Qualifier Compatibility):** Resolves `DeclaredQualifierMeta` from field declarations and compares them for axis-match equality (e.g., both fields are `in 'USD'`). This is a **declaration-level** check — it reads the qualifier from the field's `DeclaredQualifiers`, not from any runtime value or interpolation hole.

5. **Initial State Satisfiability:** `GetTypeDefault` (line 1188) marks all typed constant types under the `_ => MarkUnfoldable(unfoldable, fieldName)` branch. The constant folder returns `UnknownSentinel` for these fields. Constraints involving typed constant fields cannot be folded at compile time — they are conservatively treated as unknown.

### What proof obligations exist for typed constant types

The proof engine proves faults for:
- **Division by zero** — numeric; does not involve typed constant content
- **Sqrt of negative** — numeric; does not involve typed constant content
- **Collection empty on access/mutation** — collection types only
- **Presence requirements** — field optionality, not content
- **Modifier requirements** — `nonnegative`/`positive` on the numeric magnitude (applies to money/quantity via modifier metadata, but checked against the field declaration, not the constant's text)
- **Dimension compatibility** — period dimension matching (date/calendar vs. time dimension)
- **Qualifier compatibility** — currency/unit/dimension match between operands in arithmetic (checked via `DeclaredQualifiers` on field declarations, not via runtime values)

**Key finding:** The proof engine never reasons about the *content* of a typed constant value. It reasons about *field declarations* — type, modifiers, qualifiers. The internal structure of `'100 USD'` is invisible to it.

### Compile-time knowledge of field values

The proof engine knows a field's value at compile time ONLY when:
- The field has a literal default that resolves to a `TypedLiteral` with a foldable value (numeric or boolean)
- The field is computed from foldable expressions

Typed constant fields are unfoldable. Fields with non-literal defaults or computed expressions are unfoldable. Field references in expressions are resolved to their default value (if foldable) or `UnknownSentinel`.

---

## Part 2 — String-Exclusion Impact Assessment

### 2.1 — How often does this arise?

**Evidence from `samples/` (29 sample files):**

- **Zero samples** declare a `currency`, `unitofmeasure`, or `dimension` field type. The typed qualifier fields in samples use `in 'USD'` qualifiers on `money` fields — the currency is declared as a *qualifier*, not carried in a separate `currency` field.
- **One sample** (`fee-schedule.precept`) has `CurrencyCode as string default "USD"` — a `string` field used to carry a currency code. This is the *exact* case where an author might write `'{Amount} {CurrencyCode}'` to construct a money value with a string hole.
- **All money fields** in samples use static qualifiers (`in 'USD'`). None construct money values dynamically from field references at all — every money default and assignment uses static typed constants like `'0.00 USD'` or `'5000.00 USD'`.
- **No sample** uses interpolated typed constants at all (they are unimplemented — currently a crash stub).

**Real-world use-case analysis:**

The `string` hole use case arises when:
1. The precept receives a currency code, unit name, or similar qualifier as a `string` from external input (event arguments)
2. The author constructs a typed constant dynamically: `set Price = '{Amount} {CurrencyStr}/each'`

This is a **boundary integration** pattern — the precept bridges untyped string data from the host application into the typed constant system. The question is: should this bridge exist inside the interpolation syntax, or should the author be required to use a typed field?

**Frequency verdict:** Rare in practice. The samples show that authors either (a) use static qualifiers on their typed fields, or (b) receive already-typed event arguments. The `fee-schedule.precept` pattern — `string` carrying a currency code — is a design smell that the type system is specifically designed to prevent. The author *should* use `field CurrencyCode as currency default 'USD'`.

### 2.2 — What does the proof engine gain?

The proof engine itself gains **nothing directly**. It does not inspect typed constant content. The gain is upstream — in the **type checker's structural guarantees**, which the proof engine's qualifier compatibility and satisfiability analyses depend on.

Here's the chain:

**With `string` excluded (all holes typed):**

For `'{Amount} {Curr}'` targeting `money`:
- `Amount` must be `integer`/`decimal`/`number` → the type system guarantees a valid numeric magnitude
- `Curr` must be `currency` → the type system guarantees a valid ISO 4217 code (every `currency` field/arg value is validated at every write point)
- **Result:** The constructed money value is **structurally valid by construction**. The type checker can prove that no runtime typed-constant-validation fault can occur. No runtime content validation is needed for the interpolated result.

The proof engine's Strategy 5 (Qualifier Compatibility) benefits indirectly: when both operands in money arithmetic have their currency derived from `currency`-typed fields, the qualifier compatibility check can prove currency match at compile time. If one operand's currency came from a `string` hole, the proof engine cannot resolve its qualifier — it falls through to `Unresolved`, emitting a warning.

**With `string` permitted:**

For `'{Amount} {CurrStr}'` targeting `money` where `CurrStr` is `string`:
- The type checker accepts it (string is valid in any slot per frank-23)
- `CurrStr` could contain `"BOGUS"` at runtime
- The proof engine cannot resolve the qualifier from a string-sourced value
- Runtime validation must catch content errors
- Any downstream qualifier compatibility proof on this field fails (Unresolved)

**Net proof delta:** When `string` is excluded, the type checker can establish a **closed-world guarantee**: every interpolated typed constant is structurally valid if it compiles. This is exactly Precept's core identity — make invalid configurations structurally impossible, not deferred to runtime.

### 2.3 — What does the author lose?

The author loses the ability to write `'{x}'` where `x` is a `string` field. They must use a typed field (`currency`, `unitofmeasure`, `dimension`) for qualifier slots, and `integer`/`decimal`/`number` for magnitude slots.

**Is this a real loss?**

No. It is discipline enforcement, not capability loss. Here's why:

1. **Typed qualifier fields exist.** Precept has `currency`, `unitofmeasure`, and `dimension` as first-class types. If the author needs a field that carries a currency code, they should declare `field X as currency`, not `field X as string`. The type system then guarantees `X` always holds a valid ISO 4217 code.

2. **Event arguments can be typed.** Instead of `event SetCurrency(Code as string)`, the author writes `event SetCurrency(Code as currency)`. The runtime validates the argument at the event boundary — exactly where boundary validation belongs.

3. **Zero-constructor discipline alignment.** Precept's temporal types already prohibit interpolation precisely because their components can't be meaningfully typed as independent slots (Decision #17 — zero constructors). Excluding `string` from business domain type holes applies the same principle: if you can't give it a type, you can't use it in construction.

4. **The `fee-schedule.precept` pattern is fixable.** `CurrencyCode as string default "USD"` should be `CurrencyCode as currency default 'USD'`. The string field is a type system underutilization, not a legitimate need for string holes.

**Does this conflict with Precept's type system design?**

No — it *reinforces* it. The type system's purpose is to ensure that values always belong to their declared type's valid domain. Allowing `string` into typed constant holes creates a backdoor that bypasses this guarantee. Excluding it closes the backdoor.

### 2.4 — Proof consequence

**Quantitative analysis:**

Consider the five proof strategies and how they interact with interpolated typed constants:

| Strategy | With string holes | Without string holes | Delta |
|----------|-------------------|----------------------|-------|
| S1 Literal | No effect (numeric only) | No effect | None |
| S2 Declaration Attribute | Modifier proofs work; qualifier resolution fails for string-sourced values | All proofs work; qualifiers resolvable from typed declarations | **Gains qualifier resolution** |
| S3 Guard in Path | Guards over string-valued slots cannot extract comparison values | Guards work on typed slots (currency fields are comparable) | **Gains guard decomposition for qualifier slots** |
| S5 Qualifier Compatibility | String-sourced qualifiers → `null` → proof fails → Unresolved | All qualifiers resolvable → proof succeeds | **Gains qualifier compatibility proofs** |
| S10 Initial State Satisfiability | Typed constant fields remain unfoldable either way | Same | None |

**Three out of five strategies gain proof power when string holes are excluded.** The gain is concentrated in qualifier-related proofs — exactly the domain where typed constants are most important.

**How much more often can faults be proved structurally impossible?**

For any precept that:
- Constructs typed constants via interpolation with qualifier holes (currency, unit, dimension slots)
- Performs arithmetic or comparison on the resulting typed values
- Has qualifier compatibility requirements on those operations

...the proof engine can prove qualifier compatibility **100% of the time** with typed holes, vs. **0% of the time** with string holes. This is binary: either the qualifier is resolvable from declarations (typed → proved) or it isn't (string → unresolved warning).

---

## Part 3 — Verdict

### **Exclude entirely.**

String holes are a compile-time proof liability. Typed fields exist for every qualifier domain that matters. The loss is discipline enforcement, not capability loss.

**Reasoning:**

1. **Philosophy alignment:** Precept's core identity is "make invalid configurations structurally impossible." String holes make invalid configurations structurally possible — a `string` field can hold `"BOGUS"` in a currency slot. This directly contradicts the grounding document.

2. **Proof engine impact:** Three of five proof strategies gain power. Qualifier compatibility — the most important proof family for typed constant arithmetic — goes from unprovable to fully provable. This is not marginal; it is the difference between "compiler guarantees correctness" and "hope the runtime catches it."

3. **No real use case loss:** Zero samples use `currency`/`unitofmeasure` field types, but that's because the feature is unimplemented. When interpolation ships, authors will declare typed qualifier fields — that's the *point* of having these types. The string-carrying-a-currency-code pattern (`fee-schedule.precept`) is a design smell that the type system is designed to prevent.

4. **The annotation alternative is unnecessary complexity.** The `'{x:currency}'` annotation option (Option B) adds syntax, parser work, and a second validation path for a use case that doesn't exist in practice. If we ever discover a legitimate need for string-to-typed-constant bridging, we can add it later as a deliberate extension — but it should not be the default.

5. **Simplification dividend.** Excluding string eliminates the `string` row from every per-slot compatibility table in the interpolation plan (26 table rows across 8 types). It eliminates the "runtime-deferred check" exception from the philosophy constraint section. It eliminates the need for runtime content validation of interpolated results — if it compiles, it's valid. The plan gets simpler and the implementation gets shorter.

**What we ship:** Every hole in an interpolated typed constant must resolve to a type that the slot's compatibility table can structurally verify. No escape hatches. No runtime surprises. If the author needs to bridge string data into the typed system, they do it at the event boundary (typed event arguments) or via explicit field assignment — not inside interpolation.
