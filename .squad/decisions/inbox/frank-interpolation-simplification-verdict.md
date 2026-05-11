# Frank-24 — Interpolation Simplification Verdict

**From:** Frank  
**To:** Shane  
**Date:** 2026-05-11  
**Re:** Shane's challenge — drop per-type grammar tables and per-slot type checks; accept any hole expression, stitch text, validate the combined result as a typed constant

---

## Verdict: APPROVED — with one structural guardrail retained

The simplification is architecturally sound. I'm reversing my own position from frank-23, and I'll explain exactly why.

---

## What the Simplification Actually Means

Under the simplified approach:

1. **Parser** (Slice 1): Unchanged. Parse `{expr}` holes into expression nodes inside the interpolated typed constant. This is pure structure — no semantics.

2. **Type checker** (Slice 2): For each hole, resolve the expression and record its type — but do NOT classify it into a "slot" or match against a type-grammar table. Instead:
   - Resolve all hole expressions normally.
   - Substitute each hole's **text representation** into the literal string at its position.
   - Pass the combined string through the **existing** `TypedConstantValidation` pipeline for the target type — the same validation that already runs on static typed constants like `'100 USD'`.
   - If validation passes, the interpolated constant is valid.
   - If validation fails, emit a diagnostic — the same content-validation diagnostic that a malformed static literal would produce.

3. **What gets validated at compile time:** The structure of each hole expression (is it a well-typed expression?). The structure of the overall literal (are text segments and holes properly lexed?). For holes whose values are **statically known** (literal integers, literal strings, references to fields of known types with `in` constraints), the combined string can be fully validated at compile time.

4. **What gets deferred:** Holes whose values are not statically known (field references without `in` constraints, computed expressions) produce a combined string that cannot be fully validated until runtime. This is already the semantic model for interpolated typed constants — they are the language's **expansion joint** for runtime-deferred construction.

---

## What the Simplification Gains

1. **Eliminates ~250 lines of type-grammar machinery.** The per-type valid-form tables (8 patterns for price, 8 for exchangerate, 7 for duration/period compound forms, etc.), the segment classification taxonomy, the matching algorithm, the slot identity enum, and the per-slot compatibility tables — all gone. The existing `TypedConstantValidation` pipeline does the work.

2. **Zero cognitive load for future types.** Adding a new typed constant type (or a new valid form for an existing type) requires only updating the existing content validator. No second grammar table to maintain, no slot compatibility matrix to extend.

3. **The `string` exception disappears as a special case.** Under the grammar-table approach, `string` required explicit handling in every slot's compatibility table (and a rationale block explaining why). Under the simplified approach, a `string` hole substitutes its runtime value into the combined text, which either validates or doesn't. The special case doesn't exist because there's no slot-type taxonomy to make exceptions to.

4. **Completions (Slice 3) simplify.** The completion handler doesn't need to replicate the type-grammar matching to determine which slot the cursor is in. It offers all fields/args; the compile loop catches mismatches.

---

## What the Simplification Loses

**One thing: early structural rejection of malformed interpolated forms when hole values are not statically known.**

### The Concrete Example

```precept
field Weight as integer
field Unit as unitofmeasure

# Under the grammar-table approach:
set Measurement = '1 {Weight} kg'    # ← COMPILE ERROR: structural form mismatch
                                      #   No quantity pattern has [num, hole, unit]

# Under the simplified approach:
set Measurement = '1 {Weight} kg'    # ← At compile time, Weight is integer, value unknown.
                                      #   Combined text: '1 ??? kg' — cannot validate statically.
                                      #   At runtime: '1 5 kg' — fails content validation.
                                      #   The error is caught, but at runtime, not compile time.
```

This is the case where position-awareness catches an error the simplified approach misses. The grammar table knows that no valid quantity form has `T(num) H T(unit)` — the form itself is wrong regardless of what the hole contains. The simplified approach can only discover this when it has a concrete value to substitute.

### Why This Loss Is Acceptable

Here's where I changed my mind. Consider what we're actually protecting against:

1. **`'1 {Weight} kg'` is a pathological form.** The author has already provided a static magnitude (`1`) and a static unit (`kg`). The hole is structurally nonsensical — it sits between two components that together already constitute a complete quantity. This is not a subtle bug that a reasonable author would write and be surprised by. It's a typo or a misunderstanding of the syntax.

2. **The error is still caught.** It fails at runtime content validation, not silently. Precept's philosophy says "make invalid configurations structurally impossible" — and the invalid configuration (a malformed quantity) IS impossible. It's rejected. The question is only *when*: compile time vs. runtime. For interpolated typed constants specifically, runtime rejection was always part of the contract — `string` holes, dynamically-valued fields, and computed expressions all defer to runtime.

3. **The philosophy's structural-impossibility guarantee was designed for the entity's data configurations — the (state, field-values) pairs.** It was not designed to mean "every possible diagnostic fires at compile time regardless of cost." Interpolation is inherently a runtime construction mechanism. The compile-time guarantee is: if we can prove it wrong statically, we do. The simplified approach preserves that — it validates the combined string whenever all components are statically known.

4. **The cost of the grammar tables is not proportional to the benefit.** The tables protect against a narrow class of structural errors (malformed segment arrangements) that are (a) unlikely, (b) caught at runtime anyway, and (c) require ~250 lines of new type-checker infrastructure plus ongoing maintenance as the type system evolves.

---

## The `string` Exception

Under the simplified approach, `string` is not an exception at all. A `string` hole substitutes a runtime value into the text. If the combined text validates, it's valid. If it doesn't, it's a runtime error. This is exactly the same semantics as a `string` in a static typed constant position — there is no special case to reason about.

Under the grammar-table approach, `string` was a universal escape hatch explicitly coded into every slot's compatibility table. The simplification eliminates the need for that machinery entirely.

---

## The Structural Prohibition — `'1 {x} kg'`

Under the simplified approach, `'1 {x} kg'` is not rejected at parse or type-check time if `x`'s value is unknown. It is rejected at runtime when the combined text fails content validation.

Is this acceptable? Yes — **with one guardrail.**

### The Guardrail: Static Substitution When Possible

When a hole expression resolves to a **compile-time-known value** (a literal, a field with a `default` and no mutations, etc.), the type checker SHOULD substitute that value and run content validation immediately. This is not the grammar-table approach — it's the existing validation pipeline applied opportunistically.

Example:
```precept
field Weight as integer default 5
set Measurement = '1 {Weight} kg'   # Combined: '1 5 kg' — content validation FAILS at compile time
```

This opportunistic early validation catches the same errors the grammar tables would catch, but only when values are known — which is exactly the right boundary. When values aren't known, we can't validate content anyway, grammar tables or not (we can only validate *form*).

The guardrail is: **the type checker substitutes statically-known values and validates the combined text at compile time.** This is a natural extension of the existing content-validation pipeline, not new machinery.

---

## Recommendation

**Revise the interpolation plan as follows:**

1. **Drop the per-type valid-form grammar tables entirely.** No pattern tables, no segment classification taxonomy, no slot identity enum, no per-slot compatibility matrices.

2. **Drop diagnostic codes 120 and 122.** Code 120 (`InvalidInterpolatedTypedConstantForm`) and 122 (`InterpolatedTypedConstantHoleTypeMismatch`) are products of the grammar-table approach. They don't exist under the simplified model. Content-validation failures use the same diagnostics as static typed constants.

3. **Keep diagnostic code 121** (`InterpolationNotSupportedForType`). The prohibition on formatted temporal types (`date`, `time`, `instant`, etc.) is a categorical decision, not a form-matching decision. It stays.

4. **Slice 2 redesign:** `ResolveInterpolatedTypedConstant()` becomes:
   - Resolve the target type from context.
   - If target type is a formatted temporal type → emit 121, return error.
   - Resolve all hole expressions.
   - For holes with statically-known values, substitute into the text.
   - If all holes are statically resolved, run full content validation on the combined string. Emit the standard content-validation diagnostic on failure.
   - If any holes are dynamically valued, record the expression for runtime validation. No compile-time content error (cannot validate what you don't know).
   - Construct the typed node with resolved hole expressions.

5. **Slice 3 simplification:** Completions inside holes offer all fields/args in scope. No slot-position filtering needed. The compile loop is the type-checking gate, not the completion provider.

6. **The `string` exception section is deleted** from the plan. There is no exception — `string` is just another expression type whose runtime value gets substituted.

---

## What This Does NOT Change

- **Slice 1 (Parser):** Unchanged. Parse holes into expression nodes.
- **Slice 4 (Semantic Tokens):** Unchanged. Walk hole expressions for token classification.
- **Slice 5 (Docs/MCP):** Scope reduced (fewer diagnostic codes to document).
- **Formatted temporal prohibition:** Unchanged. `date`/`time`/`instant`/`datetime`/`zoneddatetime`/`timezone` still prohibited from interpolation.
- **The philosophy guarantee:** Invalid configurations remain structurally impossible. The question was only *when* the form is validated, not *whether*.

---

## Tradeoff Accepted

We accept that a narrow class of structurally malformed interpolated literals (like `'1 {x} kg'` where `x` is dynamically valued) will be caught at runtime instead of compile time. This tradeoff is acceptable because:

1. The forms are pathological and unlikely in practice.
2. The error is always caught — never silent.
3. When values are statically known, the error IS caught at compile time via content validation.
4. The implementation cost of catching these at compile time (~250 lines of grammar-table infrastructure) is disproportionate to the benefit.
5. Interpolation is inherently a runtime construction mechanism — the simplified approach aligns with that identity.
