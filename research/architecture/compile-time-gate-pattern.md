# Compile-Time Gate Pattern: Formal Grounding for Precept's Core Architectural Claim

**Date:** 2026-04-19  
**Author:** Frank (Lead/Architect)  
**Research Angle:** PL theory and industry precedent for "you cannot obtain a PreceptEngine except through a successful compile"  
**Purpose:** Establish the formal name, soundness conditions, and known failure modes for Precept's central architectural claim — that invalid entity configurations are *structurally impossible* — so that ArchitectureDesign.md can state the claim precisely and defensibly.

---

## Executive Summary

Precept's compile-time gate is well-established in programming language theory and industry practice under several converging names. The pattern is real, formally grounded, and Precept satisfies its core soundness conditions. But the pattern is weaker than it sounds: it makes invalid states *structurally difficult to construct*, not *logically impossible to represent*. The guarantee is architectural, not type-theoretic — C# provides no proof-carrying mechanism, so the invariant rests on private constructors and total validation, not on a formal proof embedded in the type system.

**Verdict:** Precept's gate pattern is sound and established. ArchitectureDesign.md should name it, state its conditions explicitly, and acknowledge what the guarantee actually is — and is not.

---

## Survey Results

### 1. Alexis King — "Parse, Don't Validate" (2019)
**Source:** https://lexi-lambda.github.io/blog/2019/11/05/parse-don-t-validate/

King's central argument: the difference between validation and parsing lies entirely in *what information is preserved*. A validator returns `()` — it checks a fact but discards it. A parser returns a richer type that *encodes* the fact in the type system, so downstream code never needs to re-check it.

Her canonical example: `validateNonEmpty :: [a] -> IO ()` vs. `parseNonEmpty :: [a] -> IO (NonEmpty a)`. Both perform the same check. Only `parseNonEmpty` makes the result of that check available as a type. The caller who holds a `NonEmpty a` does not need to handle the empty case — the type itself proves the list is non-empty.

The key design principle she derives from this: *push the burden of proof upward as far as possible, then never re-check it*. Checks happen once, at the boundary of the system, and produce a type that carries the proof forward. She explicitly names "smart constructors" with abstract types as the technique for making validators behave like parsers when the language can't express the property directly in the type.

**Precept relevance:** `PreceptCompiler` is a parser in King's sense. It takes unstructured source text and produces a `PreceptEngine` — a type that encodes the fact "this definition was found structurally valid by the full compiler pipeline." The run-time phase never re-validates because it doesn't need to: it holds the proof. The Language Server and MCP server operate the same way — they receive a `PreceptEngine` and proceed without re-checking the definition.

King also names the failure mode of validation-only approaches: "shotgun parsing" — validation checks are scattered across processing code, and it becomes impossible to know whether all checks were performed before acting. This is precisely what Precept's gate prevents. The run-time phase cannot encounter an unvalidated definition because the only way to enter the run-time phase is by holding a `PreceptEngine`, which cannot exist without a completed compile.

---

### 2. Yaron Minsky — "Make Illegal States Unrepresentable" (Jane Street, Effective ML Revisited, 2011)
**Source:** https://blog.janestreet.com/effective-ml-revisited/

Minsky's phrase "Make Illegal States Unrepresentable" (MISU) is the most widely cited formulation of this pattern. His example is a `connection_info` record with a state enum plus optional fields (`session_id: string option`, `when_disconnected: Time.t option`, etc.). In this encoding, every combination of state and field values is representable, including invalid ones (a `Disconnected` connection with a `session_id`). The fix: encode each state as a separate type variant that *only contains the fields valid for that state*. A `Connected` value structurally cannot have a `when_disconnected` field, because `connected` doesn't include one.

Minsky's insight is structural: you're not adding validation — you're changing the *type* so that invalid states have no valid representation. The type system does the prevention; you don't need to add checks.

**Precept relevance:** Precept does MISU at the *engine* level, not the field level. `PreceptEngine` is the type that only represents a fully-validated compiled definition. There is no `PreceptEngine` variant for "partially compiled" or "compiled with warnings" — those don't exist. The invalid state (an unvalidated definition running operations) is structurally unrepresentable because you'd need a `PreceptEngine` to run any operation, and a `PreceptEngine` can only exist post-validation. The pattern is applied one abstraction level higher than Minsky's field-packing example, but it is the same pattern.

---

### 3. Rust Newtype Pattern and Builder Pattern
**Source:** https://doc.rust-lang.org/book/ch19-04-advanced-types.html (redirected to ch20-03-advanced-types.html)  
*[URL redirected; content synthesized from knowledge of the Rust Book and Rust ecosystem conventions.]*

The Rust Book describes the **newtype pattern**: wrap a primitive in a tuple struct (`struct Meters(f64)`) to create a distinct type that prevents conflation with structurally-identical but semantically-different types (`struct Feet(f64)`). The new type is a zero-cost abstraction — same runtime representation, different type-checked identity.

More directly relevant to Precept is the **builder pattern** as practiced in Rust (e.g., `reqwest::ClientBuilder`). The pattern separates two phases: a mutable-configuration phase (`ClientBuilder`) and an immutable-ready phase (`Client`). The transition between them is a `build()` method that validates configuration and returns `Result<Client, Error>`. Once you hold a `Client`, all configuration validation is complete — the type is the certificate. The caller cannot use a `Client` without going through `build()`, and cannot get a `Client` from a failed `build()`.

Rust's ownership model makes this pattern especially sharp: you consume the `ClientBuilder` to produce the `Client`; you cannot hold both. In C#, the same effect is achieved by private constructors and factory methods, not by ownership transfer.

**Precept relevance:** `PreceptCompiler` is a builder in this sense. `CompileFromText()` is `build()`. It returns a discriminated result — diagnostics or engine — never a half-valid engine. The analogy is exact. Rust's ownership model makes the builder pattern verifiable by the borrow checker; in C#, it rests on API discipline (private constructor). Same pattern, weaker enforcement mechanism.

---

### 4. Scott Wlaschin — "Designing with Types: Making Illegal States Unrepresentable" (F# for Fun and Profit)
**Source:** https://fsharpforfunandprofit.com/posts/designing-with-types-making-illegal-states-unrepresentable/  
*[URL returned certificate error; content synthesized from knowledge of Wlaschin's well-documented work.]*

Wlaschin's treatment expands Minsky's phrase into a systematic F# type design method. His central technique: use discriminated unions and record types to model the exact set of valid states, so the type system eliminates the entire class of "this field should be non-null when the entity is in state X" bugs.

His specific addition to the conversation is the **smart constructor** pattern: make a type's constructor private and expose only a factory function that validates input and returns `Option<T>` or `Result<T, Error>`. The type becomes an *unforgeable certificate* — holding a value of that type is proof that it passed validation. He explicitly notes that the guarantee is only as strong as the factory function: if the factory has bugs, values can be produced that violate the invariant despite "passing" construction.

He also distinguishes between **structural impossibility** (the type cannot represent the invalid state at all, as in MISU) and **construction invariants** (the type could represent invalid states, but the private constructor prevents their construction). The smart constructor pattern achieves the second, not the first. This distinction matters.

**Precept relevance:** `PreceptEngine` is Wlaschin's smart constructor pattern. The invalid state — an unvalidated definition — is representable at the data level (a `PreceptDefinition` model can hold type errors, missing fields, unreachable states), but the private constructor makes it impossible to create a `PreceptEngine` that encodes one. The guarantee is a **construction invariant**, not a structural impossibility. This is an important precision for ArchitectureDesign.md to make.

---

### 5. Dependent Types (Wikipedia / Type Theory)
**Source:** https://en.wikipedia.org/wiki/Dependent_type

Dependent types are types whose specification *depends on a value*. The canonical example: a type `Vec(R, n)` — a vector of real numbers of length *n* — where the length is a *type parameter*, not just a runtime field. Functions on this type can be statically verified to preserve length. More generally, dependent types allow types to encode arbitrary logical propositions (via the Curry-Howard correspondence): a proof that a proposition is true *is* a value of the corresponding type.

For Precept's purposes, the key insight is what dependent types would buy: a `PreceptEngine` in a dependently-typed language could carry a formal proof that the definition satisfies every constraint — and that proof would be checkable by the type checker at the use site. You'd have not just "this compiled successfully" but "here is the formal derivation that it compiled successfully."

C# is not a dependently-typed language. Precept does not have this. What Precept has is weaker: a private constructor enforced by language access control, not a formal proof carried in the type. The type says "a `PreceptEngine` was produced by `PreceptCompiler`," but it does not carry a certificate of *which* constraints passed or *why* the definition is valid. That reasoning lives in the compiler's diagnostic output, not in the engine's type.

The dependent types literature is the most formal articulation of what Precept aspires to but does not fully achieve. The gap is not a failure — operating in C# with a well-designed private-constructor pattern is standard and correct for a production library. But the gap is worth naming.

---

### 6. Type Safety — Wright & Felleisen (Wikipedia / Pierce's TAPL)
**Source:** https://en.wikipedia.org/wiki/Type_safety

The standard formal definition of type soundness (Wright & Felleisen, 1994) requires two properties:

- **Progress:** A well-typed program never gets "stuck" — every expression is either a value or can be reduced further.
- **Preservation (Subject Reduction):** After each evaluation step, the type of each expression is preserved.

Robin Milner's informal gloss: "Well-typed programs cannot go wrong."

Vijay Saraswat's related definition: "A language is type-safe if the only operations that can be performed on data are those sanctioned by the type of the data."

For Precept, the relevant framing is Saraswat's. Once you hold a `PreceptEngine`, the only operations sanctioned are `CreateInstance`, `Inspect`, `Fire`, and `Update` — all of which operate on a known-valid definition. The operations that are *not* sanctioned (directly constructing an instance from an unvalidated definition, bypassing constraint evaluation) are not accessible through the public API. The API structure is what enforces this, not the type system.

The formal literature also clarifies what "type soundness" does *not* mean: it is "a relatively weak property" that "essentially just states that the rules of a type system are internally consistent and cannot be subverted." Precept's compile-time gate is stronger than mere type soundness: it enforces *domain-semantic* correctness (the definition is valid per Precept's constraint rules), not just *type-theoretic* correctness (the types are well-assigned). This is a higher bar.

---

## Synthesis: Precept's Compile-Time Gate Pattern

### The Pattern Name

Precept's compile-time gate is an instance of the **Opaque Validated Type** pattern — known in the literature under several overlapping names:

| Name | Source | Framing |
|---|---|---|
| Make Illegal States Unrepresentable (MISU) | Minsky (2011), Wlaschin (ongoing) | Structural — type design |
| Parse, Don't Validate | King (2019) | Functional — boundary design |
| Smart Constructor Pattern | Wlaschin, Haskell idiom | OOP/FP — factory design |
| Newtype / Opaque Type | Rust Book, Haskell idiom | Type-level — nominal encapsulation |
| Proof-Carrying Capability | Noonan (GDP paper, 2018) | Formal — proof as type |

Precept most precisely instantiates the **Smart Constructor** / **Parse, Don't Validate** framing at the system level: `PreceptCompiler` is the parser (it takes unstructured source, returns a richer type); `PreceptEngine` is the parsed type (it encodes the fact that compilation succeeded); the run-time operations are downstream consumers that never re-validate.

The pattern is *structurally enforced* in the following sense: holding a `PreceptEngine` requires having called `PreceptCompiler`, because there is no other construction path. This is enforced by C#'s access control (private constructor), not by a formal proof system.

### Formal Conditions Precept Satisfies

1. **Opaque construction:** `PreceptEngine` has no public constructor, no parameterized factory bypass, no reflection-accessible internal. The only construction path is `PreceptCompiler.CompileFromText()` or `PreceptCompiler.Compile()`.
2. **Total validation:** The compiler runs the full type-checker + proof engine over the complete definition before producing an engine. There is no lazy, incremental, or partial-result mode that produces an engine before validation is complete.
3. **Immutability post-construction:** `PreceptEngine` is immutable after construction. The validity certificate cannot be retroactively invalidated by mutating the engine.
4. **Phase segregation:** The compile-time phase and run-time phase are separated by the type boundary. The run-time phase (everything that takes a `PreceptEngine`) cannot be entered without passing through the compile-time phase.
5. **One-file completeness:** All validation facts derive from a single `.precept` file with no external references. The compiler validates the complete closed system, not a subset.

### Formal Conditions Precept Does NOT Satisfy (and Why This Matters)

1. **Dependent types / proof-carrying code:** `PreceptEngine` does not carry a formal proof of its validity. The correctness guarantee rests on the type checker being bug-free, not on a verifiable proof embedded in the engine.
2. **Structural impossibility (full MISU):** `PreceptDefinition` can represent invalid definitions. The pattern is a *construction invariant*, not a *representation invariant*. You can hold an invalid `PreceptDefinition`; you cannot produce a `PreceptEngine` from it. These are different guarantees.
3. **Formal soundness proof:** The type checker has not been formally verified in a proof assistant. Correctness is established by the test suite, not by formal proof.
4. **Completeness:** The type checker is conservatively sound — it may reject some valid definitions (conservative approximation). It does not claim to accept *every* valid definition, only that every *accepted* definition is valid.

---

## Failure Modes and Limits

These are the conditions under which the compile-time gate would fail to provide its guarantee. They are not theoretical — each has occurred in production systems that implemented similar patterns.

### 1. Type Checker Bugs (The Most Real Risk)

The gate is exactly as strong as the type checker. If a rule like C92 (divisor safety) passes a definition where a guard condition doesn't actually exclude the zero denominator, the engine will produce a runtime arithmetic error — the gate failed to catch the invalid definition.

This is not a flaw in the pattern; it is the inherent cost of relying on a non-formally-verified checker. The pattern bounds the failure: it cannot be bypassed, only fooled. All runtime bugs in Precept's engine are, at bottom, type checker bugs — they represent definitions the checker should have rejected but didn't.

**Mitigation already in place:** The proof engine provides additional safety layers for specific numeric properties (C92/C93, C76, C95-C98). This reduces but does not eliminate the risk.

### 2. Reflection Bypass

C# reflection allows invoking private constructors. If consuming code uses `Activator.CreateInstance(typeof(PreceptEngine), BindingFlags.NonPublic | BindingFlags.Instance, ...)`, it can bypass the compiler gate entirely.

This is not a design flaw in Precept — it is a fundamental limitation of C#'s access control model (and of all mainstream OOP languages without secure execution environments). The guarantee is that *correct, non-hostile C# code* cannot bypass the gate.

**Precept's architecture does not guard against hostile use of reflection.** This is an acceptable tradeoff for a library targeting internal domain modeling in .NET applications. It should be documented.

### 3. Serialization Bypass

If `PreceptEngine` were ever serialized and deserialized (e.g., for caching compiled engines), the deserialization path would need to be treated as carefully as the compiler itself. A malformed byte sequence that deserializes to a structurally invalid `PreceptEngine` would bypass the gate entirely.

**Precept currently has no serialization for `PreceptEngine`.** This is the correct default. If caching compiled engines is ever implemented, the gate must be re-validated at deserialization time — or the serialized artifact must be treated as an opaque binary whose validity was established at serialization time and cannot be assumed at deserialization.

### 4. Incremental Compile Temptation

As the language grows, there will be pressure to support incremental compilation — producing an engine from a partially-valid definition for tooling purposes (e.g., completions in a file with a type error). This would be a gate violation if it produced a `PreceptEngine` from a non-clean definition.

The language server already handles this correctly: it does not produce a `PreceptEngine` from an invalid definition; it uses the partial `TypeCheckResult` for diagnostics and completions only. This architectural distinction must be preserved.

### 5. Default Value / Struct Trap

If `PreceptEngine` were ever changed from a class (reference type) to a struct (value type), `default(PreceptEngine)` would produce a zero-initialized instance without going through the compiler. This would silently bypass the gate. `PreceptEngine` must remain a reference type (`class`) for the gate to hold.

### 6. External Definition Mutation

If consuming code could modify the `PreceptDefinition` after the engine is constructed — and if the engine held a mutable reference to that definition — the engine's validity could be retroactively invalidated. Precept avoids this by constructing the engine from the type checker's output (not from the raw `PreceptDefinition`) and making the engine's internal state immutable.

---

## Implications for ArchitectureDesign.md

The following are specific claims ArchitectureDesign.md should make, using the vocabulary established here. These are recommendations, not actual edits.

**1. Name the pattern in Principle 1.**

Current phrasing: "The compile-time gateway is the prevention mechanism." This is accurate but generic. Sharpen it:

> The compile-time gateway implements the **smart constructor / parse-don't-validate** pattern: `PreceptEngine` is an opaque validated type — holding one is proof that a definition passed full compilation. The pattern is well-established in PL theory and industry practice under the names "Make Illegal States Unrepresentable" (Minsky, 2011) and "Parse, Don't Validate" (King, 2019).

**2. Distinguish construction invariant from representation invariant.**

Current phrasing implies that invalid configurations "cannot exist" — which is slightly overstated. A `PreceptDefinition` with errors *can* exist; what cannot exist is a `PreceptEngine` wrapping one. Add a precision:

> The guarantee is a *construction invariant*, not a *representation invariant*. `PreceptDefinition` can represent invalid definitions; `PreceptEngine` cannot be constructed from one. Holding a `PreceptEngine` is proof that its definition satisfied all compile-time checks; it is not a formal type-level proof that the definition is correct.

**3. State the soundness conditions as enumerated properties.**

Principle 1 should enumerate what specifically makes the gate hold: opaque construction, total validation, immutability, phase segregation, one-file completeness. These are the properties that future changes must not violate.

**4. Acknowledge the known failure modes.**

The architecture document should note, in the principles section or an appendix, that the gate is not absolute: reflection bypass is the known unguarded case, type checker bugs are the live risk, and the serialization and struct-trap failure modes are actively avoided by current design choices. Documenting what would break the guarantee is what makes the guarantee trustworthy.

**5. Position Precept's gate relative to dependent types.**

A brief note acknowledging that dependent types (Agda, Rocq, F*) would provide a stronger guarantee — the compiled artifact would carry a mechanically-checkable proof — grounds Precept's claim precisely. Precept's gate is a production-grade construction invariant in a language (C#) without dependent types. That is the correct level of claim.

---

## References

1. King, Alexis. "Parse, Don't Validate." Blog post, 2019-11-05. https://lexi-lambda.github.io/blog/2019/11/05/parse-don-t-validate/

2. Minsky, Yaron. "Effective ML Revisited." Jane Street Tech Blog, 2011-03-09. https://blog.janestreet.com/effective-ml-revisited/ (section: "Make illegal states unrepresentable")

3. Wlaschin, Scott. "Designing with Types: Making Illegal States Unrepresentable." F# for Fun and Profit. https://fsharpforfunandprofit.com/posts/designing-with-types-making-illegal-states-unrepresentable/ *(Certificate error at time of fetch; content synthesized from direct knowledge of the work.)*

4. Klabnik, Steve, and Carol Nichols. *The Rust Programming Language*, ch. 20 ("Advanced Types" — newtype pattern, builder pattern). https://doc.rust-lang.org/book/ch20-03-advanced-types.html

5. Wikipedia. "Dependent type." https://en.wikipedia.org/wiki/Dependent_type

6. Wikipedia. "Type safety." https://en.wikipedia.org/wiki/Type_safety

7. Wright, A. K., and M. Felleisen. "A Syntactic Approach to Type Soundness." *Information and Computation* 115(1), 1994. (Progress + Preservation formulation of type soundness.)

8. Milner, Robin. "A Theory of Type Polymorphism in Programming." *Journal of Computer and System Sciences* 17(3), 1978. ("Well-typed programs cannot go wrong.")

9. Noonan, Matt. "Ghosts of Departed Proofs." *Haskell Symposium*, 2018. https://kataskeue.com/gdp.pdf (Proof-carrying capability pattern; named in King's post as "significantly more advanced" treatment of the same ideas.)

10. Saraswat, Vijay. "Java is not type-safe." 1997. http://www.cis.upenn.edu/~bcpierce/courses/629/papers/Saraswat-javabug.html (Definition: "A language is type-safe if the only operations that can be performed on data are those sanctioned by the type of the data.")

11. Pierce, Benjamin C. *Types and Programming Languages.* MIT Press, 2002. (Foundational reference; type safety as "well-typed programs cannot be misused.")
