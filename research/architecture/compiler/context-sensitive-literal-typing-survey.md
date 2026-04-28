# Context-Sensitive Literal Type Resolution Survey

> Raw research collection. No interpretation, no conclusions, no recommendations.
> Research question: How do compilers resolve the type of numeric literals from expression context? What happens when context is insufficient or ambiguous? How is bidirectional type checking formalized for this purpose?

---

## Haskell — Numeric Literal Polymorphism (GHC)

Source: GHC User's Guide — Type Defaulting Rules https://downloads.haskell.org/ghc/latest/docs/users_guide/ghci.html#type-defaulting-in-ghci; Haskell 2010 Language Report §6.4.1 Numeric Literals; GHC source `compiler/GHC/Tc/Gen/Lit.hs`; Haskell Wiki "Defaulting rules" https://wiki.haskell.org/Defaulting_rules; Vytiniotis et al., "OutsideIn(X): Modular type inference with local assumptions," JFP 2011.

### Literal Pre-Resolution Representation

In the Haskell Report, integer literals are desugared into a call to `fromInteger`:

```haskell
42  ≡  fromInteger 42
```

where `fromInteger :: Num a => Integer -> a`. The literal `42` therefore has the polymorphic type `Num a => a` — it is not a monomorphic `Int` or `Integer` at the parse stage. Similarly, floating-point literals desugar to `fromRational`:

```haskell
3.14  ≡  fromRational (314 % 100)
```

with type `Fractional a => a`.

Internally in GHC's type checker (`GHC.Tc.Gen.Lit`), the literal generates a fresh inference variable (a *unification variable* in GHC terminology, spelled `alpha`) with a type class constraint:

- Integer literal `42` → variable `alpha` with constraint `Num alpha`
- Float literal `3.14` → variable `alpha` with constraint `Fractional alpha`

These constraints are collected and solved by the constraint solver in `GHC.Tc.Solver`.

### Context Sources for Type Narrowing

GHC collects constraints from the usage site of the literal. Context that narrows the inference variable includes:

1. **Type annotations** — `(42 :: Int)` immediately unifies `alpha ~ Int`.
2. **Function argument types** — `f 42` where `f :: Int -> Bool` unifies `alpha ~ Int`.
3. **Binary operator partner** — `42 + (x :: Int)` propagates from the type of `x` through the `(+) :: Num a => a -> a -> a` constraint.
4. **Pattern-match scrutinee context** — if the result of an expression is matched in a context that demands a specific type, the constraint flows inward.
5. **Return type annotation on the enclosing binding** — `g :: Int; g = 42` pushes `Int` to the RHS literal.

GHC's type inference is *Hindley-Milner with type classes* (Wadler-Blott). The constraint `Num alpha` is discharged by finding an instance `Num T` for the resolved `T`. The literal's type is whatever `alpha` is unified to after constraint solving.

### Defaulting Rules (When Context is Insufficient)

When no usage site provides enough information to determine the type of a numeric literal, GHC applies **type defaulting**. The Haskell 2010 standard defines a `default` declaration:

```haskell
default (Integer, Double)
```

This means: for any ambiguous type variable `v` such that:
1. `v` appears only in constraints of the form `C v` (all single-parameter class constraints),
2. at least one constraint is a numeric class (`Num`, `Integral`, `Fractional`, `Floating`, `Real`, `RealFrac`, `RealFloat`),
3. all other constraints are standard numeric or `Show`/`Eq`/`Ord`,

…GHC attempts to substitute each type in the `default` list in order until a substitution satisfies all constraints. For `Num v`, it tries `Integer` first, then `Double`.

GHCi (the interactive REPL) has a relaxed defaulting rule that additionally tries `()` and `(a, b)` types to make interactive sessions more fluid.

The **monomorphism restriction** (Haskell 2010 §4.5.5) also forces defaulting: a binding without an explicit type signature and without function arguments is not generalized. Instead, the type variable is defaulted. GHC warns about this with `-Wmonomorphism-restriction`.

### Ambiguity / Error Reporting

If no type in the `default` list satisfies all constraints on an ambiguous variable, GHC reports:

```
Ambiguous type variable 'a0' arising from the literal '42'
prevents the constraint '(Num a0)' from being solved.
Probable fix: use a type annotation to specify what 'a0' should be.
```

The message names the unresolved type variable, identifies the literal that introduced it, and suggests a type annotation. GHC also reports which constraints are unsatisfied.

When a `Fractional` constraint conflicts with a type that is only `Num` (e.g., `let x = 3.14; x :: Int`), GHC reports:

```
No instance for (Fractional Int) arising from the literal '3.14'
```

### Bidirectional Algorithm Structure

GHC's type checking is not purely bidirectional in the Pierce-Turner sense. It uses **Algorithm W** extended with type class constraints (the Damas-Hindley-Milner algorithm). The direction of information flow is:

- **Synthesis (inference) mode**: most expressions synthesize their type bottom-up; the literal `42` synthesizes `Num alpha => alpha` (a constrained polymorphic type).
- **Checking mode** (added in GHC's OutsideIn(X) system, Vytiniotis et al. 2011): when GHC encounters a **pushed-in type** (from a type annotation or a known function argument type), it uses that as the *expected type* and checks the literal against it. This corresponds to the Check rule in bidirectional systems.

GHC's OutsideIn(X) constraint-based type inference propagates expected types through `tcExpr` calls that carry an `ExpType` argument. When `ExpType` is `Check tau`, the literal type variable is immediately unified with `tau`.

### Out-of-Range Literal Handling

The Haskell 2010 specification does not define overflow behavior for `fromInteger` — each `Num` instance defines its own behavior. However, GHC's `Int` instance wraps silently (two's complement), while `Integer` has arbitrary precision and never overflows.

GHC does **not** check at compile time whether a literal value fits the target type. `let x = 9999999999 :: Int` compiles without warning even on 32-bit systems. At runtime, the value wraps. There is no static range check.

Exception: third-party libraries like `safe` provide range-checked numeric construction, but this is not built into the core literal resolution mechanism.

### Literal Type Error Diagnostics

GHC error messages for literal type problems include:

- `No instance for (Fractional T)` when a float literal is used where T is not Fractional
- `No instance for (Num T)` when an integer literal is used where T lacks a Num instance
- `Ambiguous type variable 'a' arising from literal …` when context is insufficient and defaulting fails

Each message includes the source span of the literal and names the constraint that could not be resolved.

### Notes

- The `OverloadedLists` extension applies analogous polymorphism to list literals.
- The `RebindableSyntax` extension allows `fromInteger` and `fromRational` to be rebound to custom implementations, enabling domain-specific numeric literal handling.
- GHC's `ExtendedDefaultRules` pragma (enabled by default in GHCi) adds more candidates to the defaulting list.

---

## Kotlin — Integer Literal Type Inference

Source: Kotlin Language Specification §16.9 "Integer Literals"; Kotlin Specification §11.1 "Type Inference and Type Checking"; JetBrains Kotlin Spec https://kotlinlang.org/spec/expressions.html#integer-literals; Kotlin compiler source `compiler/frontend/src/org/jetbrains/kotlin/resolve/calls/results/`

### Literal Pre-Resolution Representation

In the Kotlin specification, integer literals without a suffix (`42`, `-7`) have a special compile-time type called the **integer literal type** (ILT). The ILT is not a concrete runtime type; it is a compile-time artifact that records:

1. The literal's integer value.
2. The set of integer types the value is compatible with based on its numeric range.

Specifically, the Kotlin spec defines that the integer literal `42` is compatible with `Byte`, `Short`, `Int`, and `Long` if its value fits within each type's range. The ILT is a subtype of all compatible concrete types simultaneously — a form of *ad-hoc subtyping* that exists only during type inference.

The compiler represents this internally as a `PseudoClassDescriptor` for the integer literal type family. When the type inference engine encounters a literal, it creates an ILT node that defers resolution until the expected type is known.

### Context Sources for Type Narrowing

Kotlin uses an **expected type** mechanism where the compiler passes a "type expectation" downward into expressions:

1. **Variable declaration with explicit type** — `val x: Long = 42` passes `Long` as the expected type to the RHS; the ILT is resolved as `Long` if `42` fits.
2. **Function parameter type** — `fun f(x: Short) {}; f(42)` passes `Short` as the expected type.
3. **Assignment to a typed property** — `var y: Byte = 0; y = 42` resolves `42` as `Byte`.
4. **Return type of containing function** — `fun g(): Int = 42` resolves `42` as `Int`.
5. **Arithmetic with typed operands** — when one side of a binary expression is a concrete integer type, Kotlin resolves the other operand to match (subject to standard numeric promotion rules).

If no expected type is available, the ILT resolves to **`Int`** by default (see Defaulting Rules below).

### Defaulting Rules (When Context is Insufficient)

When the Kotlin type inference engine cannot determine an expected type for an integer literal, it applies the **integer literal default**: the literal resolves to `Int`, provided its value fits in the `Int` range (−2,147,483,648 to 2,147,483,647).

If the literal value exceeds `Int` range but fits in `Long`, the default becomes `Long`. This is an automatic promotion based solely on value magnitude, not context.

Floating-point literals without a suffix always default to `Double`. The `Float` type requires either an explicit annotation or the `f`/`F` suffix.

There is no defaulting ambiguity in Kotlin — the rules produce a deterministic default for every literal without context. The ILT resolves exactly one concrete type.

### Ambiguity / Error Reporting

Kotlin does not produce ambiguity errors for numeric literals — the defaulting rules are exhaustive. The error scenario instead is **out-of-range**: if the expected type is `Byte` and the literal value is `200`, the compiler reports:

```
The integer literal does not conform to the expected type Byte
```

This is a type mismatch diagnostic, not an ambiguity. It appears on the literal token.

A second error scenario: if the literal has an explicit suffix that conflicts with the expected type (e.g., `val x: Int = 42L`), the compiler reports a type mismatch between `Long` (from the suffix) and `Int` (from the declaration).

### Bidirectional Algorithm Structure

Kotlin's type inference is described as a **constraint-based** system with bidirectional information flow:

- **Top-down (checking mode)**: the expected type is propagated from the declaration or function parameter into the subexpression. This is captured in the spec as "type expectations" passed through the inference context.
- **Bottom-up (synthesis mode)**: when no expectation is available, the expression synthesizes its type from its structure. For literals, this uses the defaulting rules.

The spec describes a `TypeInferenceContext` that carries the current `ExpectedType`. For literals, the algorithm is roughly:

```
tcExpr(ctx, IntegerLiteral(v)):
  if ctx.expectedType ≠ None:
    if v fits ctx.expectedType:
      resolve literal as ctx.expectedType
    else:
      error: value out of range for expectedType
  else:
    resolve literal as default(v)  // Int or Long by magnitude
```

The Kotlin compiler (K2) uses a more elaborate constraint inference system ("FIR" — Front-end Intermediate Representation), but the observable behavior for literal resolution matches this simplified model.

### Out-of-Range Literal Handling

Kotlin performs **static range checking** for numeric literals against the resolved type. If the resolved type is `Byte` and the literal value is 200, the compiler emits a compile-time error. There is no silent truncation or wrapping.

For the `Long` suffix (`L`), values up to 2^63−1 are accepted. Values exceeding `Long` range are a compile-time error (`The value is out of range`).

Hexadecimal and binary literals follow the same rules with the same range constraints.

### Literal Type Error Diagnostics

Kotlin error codes related to literal resolution:

- `INTEGER_LITERAL_OUT_OF_RANGE` — the value exceeds the resolved type's range
- `FLOAT_LITERAL_OUT_OF_RANGE` — similarly for float
- `TYPE_MISMATCH` — the ILT's resolved type doesn't match the expected type (e.g., expected `Short`, value 200 which exceeds Short's range)
- `CONSTANT_EXPECTED_TYPE_MISMATCH` — the literal type from a suffix conflicts with the expected type

Diagnostics appear on the literal token and include both the expected type and the actual resolved ILT type.

### Notes

- Long literals require the `L` suffix to express values exceeding `Int` max even in an unambiguous context. Without `L`, a value like `3000000000` is out-of-range for `Int` and causes an error, even if the expected type is `Long`.
- Unsigned integer literals (`42u`, `42uL`) use a separate `UInt`/`ULong` ILT family.
- Kotlin's ILT mechanism is an explicit feature of the language specification, not an implementation detail. The spec defines ILT as a distinct kind of type alongside class types and type parameters.

---

## Swift — ExpressibleByIntegerLiteral / ExpressibleByFloatLiteral

Source: Swift Language Reference §"Numeric Literals" https://docs.swift.org/swift-book/documentation/the-swift-programming-language/lexicalstructure/#Numeric-Literals; Swift Language Reference §"Literal Expressions"; Swift compiler source `lib/Sema/CSGen.cpp`, `lib/Sema/TypeCheckExpr.cpp`, `lib/Sema/CSSimplify.cpp`

### Literal Pre-Resolution Representation

Swift uses **literal protocols** as the abstraction layer for literal type resolution. An integer literal in Swift is not simply `Int` — it is an expression that is resolved to any type `T` that conforms to `ExpressibleByIntegerLiteral`. The protocol is:

```swift
public protocol ExpressibleByIntegerLiteral {
    associatedtype IntegerLiteralType: _ExpressibleByBuiltinIntegerLiteral
    init(integerLiteral value: IntegerLiteralType)
}
```

Before type resolution, the literal expression has no concrete type — it is a *literal expression node* in the AST. The constraint system then constrains the node's type to any type conforming to `ExpressibleByIntegerLiteral`.

Internally, Swift's constraint system (`lib/Sema/CSGen.cpp`) generates a *literal constraint*:

```
LiteralConstraint(literalKind: .Integer, type: T, conformsTo: ExpressibleByIntegerLiteral)
```

This constraint is held open until the rest of type checking provides enough information to determine `T`.

Standard library types conforming to `ExpressibleByIntegerLiteral` include: `Int`, `Int8`, `Int16`, `Int32`, `Int64`, `UInt`, `UInt8`, …, `Float`, `Double`, `Float80`, `Decimal`.

### Context Sources for Type Narrowing

The Swift constraint system is a **constraint propagation solver**. Context that narrows the literal type includes:

1. **Type annotation** — `let x: Double = 42` adds a constraint `T == Double`.
2. **Function argument type** — `func f(_ x: Float) {}; f(42)` adds `T == Float`.
3. **Return type** — `func g() -> UInt8 { return 42 }` adds `T == UInt8`.
4. **Operator overload resolution** — when a literal is an operand to an operator, Swift uses overload resolution to narrow the literal type to match the operator's concrete argument type.
5. **Type coercion** — `42 as Double` forces `T == Double`.
6. **Generic context** — `func h<T: SignedInteger>(_ x: T) {}; h(42)` constrains the literal to `SignedInteger`-conforming types, which Swift resolves using the default fallback.

### Defaulting Rules (When Context is Insufficient)

Swift defines **default literal types** for each literal protocol:

| Protocol | Default Type |
|---|---|
| `ExpressibleByIntegerLiteral` | `Int` |
| `ExpressibleByFloatLiteral` | `Double` |
| `ExpressibleByBooleanLiteral` | `Bool` |
| `ExpressibleByStringLiteral` | `String` |
| `ExpressibleByNilLiteral` | no default (always requires context) |

When the constraint solver cannot uniquely determine `T` from the usage context, it substitutes the default type. This is implemented in `lib/Sema/CSSimplify.cpp` as a final *literal default resolution pass* that runs after the main solving phase.

The default is a fallback of last resort: if any context narrows the type, the default is not used.

### Ambiguity / Error Reporting

If the literal type remains underdetermined after all context and the default:

- If two equally-scored solutions exist (e.g., conflicting constraints both satisfiable): the solver picks the lowest-scoring solution or reports an ambiguity. For literals this is rare because the default provides a unique fallback.
- If the default `Int` does not satisfy the constraints (e.g., the context requires a type that is `ExpressibleByFloatLiteral` but not `ExpressibleByIntegerLiteral`), the solver reports a type error.

Typical error messages:

```
error: cannot convert value of type 'Int' to specified type 'Float'
```

or, when the type variable cannot be resolved:

```
error: expression type 'T' is ambiguous without more context
```

### Bidirectional Algorithm Structure

Swift's type checker is explicitly described as using **bidirectional constraint propagation**. The constraint system in `lib/Sema/ConstraintSystem.h` and `CSGen.cpp` works as follows:

1. **Generation phase**: walk the AST and generate type variables and constraints for each expression node. Literal expressions generate a type variable with a literal conformance constraint.
2. **Solving phase**: the constraint solver applies simplification rules (unification, conformance checking, overload resolution) iteratively.
3. **Commitment phase**: once the solver reaches a unique solution, it binds each type variable to a concrete type.

This is not a traditional Pierce-Turner bidirectional system but a *constraint-based* system. The constraint system can propagate types both top-down (from annotations, expected types) and bottom-up (from subexpressions). For literals, the protocol conformance constraint acts as a lower bound, and context constraints act as upper bounds.

Swift does not explicitly separate "checking mode" and "synthesis mode" in its implementation. Instead, the constraint solver treats expected types as equality constraints on type variables, achieving a similar effect.

### Out-of-Range Literal Handling

Swift checks literal values at compile time for integer types:

```swift
let x: Int8 = 200  // error: integer literal '200' overflows when stored into 'Int8'
```

The error is emitted during the constraint solving phase, after the literal type is resolved to `Int8`, by checking the literal's value against the type's range. For floating-point literals truncated to `Float`, Swift may produce a precision warning but not an error unless the value is out of range.

```swift
let x: UInt = -1  // error: negative integer literal cannot be converted to unsigned type 'UInt'
```

### Literal Type Error Diagnostics

Swift compiler diagnostics for literal type resolution:

- `cannot convert value of type 'Int' to specified type 'T'` — when the default resolution `Int` conflicts with the required type
- `integer literal 'N' overflows when stored into 'T'` — range check failure
- `negative integer literal cannot be converted to unsigned type 'T'`
- `expression type 'T' is ambiguous without more context` — when the type variable cannot be resolved

Diagnostics are attached to the literal token and include the literal's value and the target type.

### Notes

- The `_ExpressibleByBuiltinIntegerLiteral` protocol is internal to the Swift standard library and represents the connection from user-space protocols to the compiler's built-in literal support.
- Custom types can conform to `ExpressibleByIntegerLiteral` by implementing `init(integerLiteral:)`, making the literal resolution mechanism extensible.
- Swift's solver uses a *scoring* mechanism to prefer "better" solutions when multiple options exist. A solution that requires fewer coercions scores better.

---

## Rust — Integer Literal Inference via Unification

Source: Rust Reference §"Numeric Literals" https://doc.rust-lang.org/reference/expressions/literal-expr.html; Rust Reference §"Type Inference" https://doc.rust-lang.org/reference/types/inferred.html; Rust compiler source `compiler/rustc_typeck/src/check/expr.rs`; RFC 0012 "Integer type inference"

### Literal Pre-Resolution Representation

In Rust, an integer literal without a suffix (e.g., `42`) has an **integer inference variable** as its type during type checking. This is written `{integer}` in Rust's internal type representation and in user-facing error messages. It represents a type variable constrained to be one of the integer types: `i8`, `i16`, `i32`, `i64`, `i128`, `isize`, `u8`, `u16`, `u32`, `u64`, `u128`, `usize`.

Similarly, a floating-point literal without a suffix has type `{float}`, constrained to `f32` or `f64`.

These inference variables are not type class constraints (there is no `Num` type class in Rust). They are instead **monomorphic inference variables** tracked in the type inference engine (`rustc_infer::infer::InferCtxt`). The variable is created with an *integer kind* marker that restricts unification partners to integer types only.

### Context Sources for Type Narrowing

Rust's type inference is **local** (it does not do Hindley-Milner global inference across function boundaries). Type narrowing sources within a function body:

1. **Variable binding annotation** — `let x: u32 = 42;` immediately unifies the literal's `{integer}` with `u32`.
2. **Function argument type** — `fn f(x: u64) {}; f(42)` unifies at the call site.
3. **Arithmetic expression with typed operand** — `let x: i32 = 0; let y = x + 42;` unifies `42` as `i32` through the `Add<i32>` constraint.
4. **Method call receiver** — `let x = 42; x.count_ones()` unifies `x` to the type that has the `count_ones` method.
5. **Assignment to typed variable** — `let mut x: i16 = 0; x = 42;` unifies `42` as `i16`.
6. **Return type** — `fn g() -> u8 { 42 }` unifies `42` as `u8`.

Rust's inference propagates through the function body. It does **not** propagate across function boundaries without type annotations.

### Defaulting Rules (When Context is Insufficient)

If after all local constraints are collected the `{integer}` variable remains free, Rust applies a **fallback rule**: the `{integer}` type is resolved as `i32`. The `{float}` fallback is `f64`.

This fallback is applied as a final step before code generation, not during type inference. The effect is that code like:

```rust
fn main() {
    let x = 42;  // resolves to i32
}
```

compiles without annotation. The fallback is documented in the Rust Reference and is considered a stable language guarantee.

### Ambiguity / Error Reporting

In practice, Rust's `{integer}` and `{float}` variables are rarely left ambiguous because the fallback resolves them. Ambiguity errors arise when:

1. **Conflicting constraints** — a literal is used in two contexts that require different concrete types.
2. **Unsatisfied trait bounds** — the resolved type does not implement a required trait.

When Rust cannot resolve a literal type (rare, in generic contexts where fallback cannot apply), it reports:

```
error[E0282]: type annotations needed
  --> src/main.rs:2:9
   |
2  |     let x = 42;
   |         ^ consider giving `x` a type
```

The `E0282` error code specifically means "type annotations needed." The message includes the source location and suggests adding a type annotation.

### Bidirectional Algorithm Structure

Rust's type inference is **constraint-based with a separate obligation system** (Chalk, the Rust trait solver). It is not a pure bidirectional system. The relevant structure is:

1. **Constraint collection**: the type checker visits expressions and generates equality constraints (`T == u32`), subtype constraints, and trait obligations (`T: Copy`). Literal expressions contribute `T = {integer}` or `T = {float}` constraints.
2. **Unification**: the inference engine (`InferCtxt`) solves equality constraints using a union-find structure, similar to Algorithm W.
3. **Trait solving**: after unification, the trait solver (Chalk / the legacy `fulfill` system) checks that all trait obligations are satisfied.
4. **Fallback**: after solving, any remaining free `{integer}` or `{float}` variables are replaced with their defaults.

The directed information flow (top-down vs. bottom-up) is implicit: expected types from annotations propagate as equality constraints, effectively implementing a checking mode without an explicit mode parameter.

### Out-of-Range Literal Handling

Rust performs **compile-time range checks** for numeric literals once the type is resolved:

```rust
let x: u8 = 256;
```

The error message:

```
error: literal out of range for `u8`
  --> src/main.rs:2:17
   |
2  |     let x: u8 = 256;
   |                 ^^^ this value is out of range
   |
   = note: the literal `256` does not fit into the type `u8` whose range is `0..=255`
```

The note includes the valid range. For literal expressions specifically, this is a compile-time error regardless of build mode.

Negative literals for unsigned types:

```rust
let x: u8 = -1;
// error[E0600]: cannot apply unary operator `-` to type `u8`
```

### Literal Type Error Diagnostics

Rust error codes for literal type problems:

- `E0282` — type annotations needed (type could not be inferred)
- `E0308` — mismatched types (when literal type doesn't match expected)
- `E0600` — cannot apply unary operator to unsigned type
- Lint `overflowing_literals` — triggered when a literal overflows its resolved type (hard error in current Rust, not merely a lint)

Diagnostics always include the source span, the expected/actual type, and the valid range when applicable.

### Notes

- The `{integer}` and `{float}` notation appears verbatim in Rust compiler error messages, giving users a hint that the type is not yet resolved.
- Rust's type inference is intentionally **local** (function-scoped) by design, unlike Haskell's global type inference. This means more annotations are required at function boundaries.
- Integer suffix syntax (`42u32`, `3.14f32`) bypasses inference entirely and sets the type at parse time.

---

## Ada — Universal Numeric Types

Source: Ada Reference Manual (ARM) 2022 §3.5.4 "Integer Types"; ARM §3.5.6 "Real Types"; ARM §4.2 "Literals"; ARM §8.6 "The Context of Overload Resolution"; ARM §3.4.1 "Universal Types"; John Barnes, "Programming in Ada 2012," Chapter 15.

### Literal Pre-Resolution Representation

Ada uses the concept of **universal types** for unresolved numeric literals. There are exactly two universal numeric types:

- **`universal_integer`** — the type of all integer literals. It is a mathematical integer type with no defined range limit.
- **`universal_real`** — the type of all real (floating-point) literals. It is an exact rational number type.

These are not real Ada types that programs can declare variables of — they exist only at compile time. A literal expression `42` has type `universal_integer`, and `3.14` has type `universal_real`, until the surrounding context resolves the concrete type.

ARM §3.4.1: "Universal types are predefined types that can be used in contexts where no specific type has been specified."

### Context Sources for Type Narrowing

Ada's overload resolution rules (ARM §8.6) govern how `universal_integer` and `universal_real` are resolved to concrete types:

1. **Object declaration with explicit type** — `X : Integer := 42;` resolves the literal to `Integer`.
2. **Named number** — `Max : constant := 100;` assigns `universal_integer` to `Max` permanently (named numbers retain universal type).
3. **Function/procedure parameter** — `Put_Line(Integer'Image(42));` resolves from the parameter type.
4. **Qualified expression** — `Integer'(42)` explicitly asserts the type.
5. **Subtype constraint** — `subtype Small is Integer range 1 .. 10; X : Small := 5;` resolves `5` as `Integer` (the base type of `Small`).
6. **Array index** — the index type of the array constrains the literal.

Ada has no type inference in the Haskell sense. Overload resolution is performed on complete expressions, not by propagating constraints.

### Defaulting Rules (When Context is Insufficient)

Ada does not have Haskell-style defaulting. Instead, `universal_integer` and `universal_real` participate in **implicit conversions**:

1. A `universal_integer` value is implicitly convertible to any integer type when the context demands it.
2. A `universal_real` value is implicitly convertible to any real type.

These implicit conversions apply only when the resolution is unambiguous. If the context provides exactly one expected integer type, the literal is silently converted.

**Named numbers** are a special case: a constant declared with `:= expression` and no explicit type retains `universal_integer` or `universal_real` permanently. These constants participate in compile-time constant folding and never require a concrete type.

### Ambiguity / Error Reporting

Ada's overload resolution can fail with ambiguity. The ARM specifies that an expression is ambiguous if more than one interpretation is legal. For numeric literals, this typically arises in:

1. **Overloaded subprogram calls** — `f(42)` where `f` is overloaded for both `Integer` and `Long_Integer` with no disambiguation.
2. **Missing context** — expression statements that are not assignments and provide no type context.

ARM §8.6 ¶26: "If the context does not provide a unique interpretation, the construct is illegal."

Ada compilers report:

```
ambiguous expression (cannot determine type)
```

The fix is a qualified expression: `Integer'(42)` or an explicit type conversion `Integer(42)`.

### Bidirectional Algorithm Structure

Ada uses **overload resolution** (ARM §8.6) rather than Hindley-Milner or bidirectional type checking. Overload resolution operates in two passes:

1. **Bottom-up**: each expression node is annotated with the set of possible interpretations (possible types). For a literal `42`, this set initially contains all integer types visible in scope (via `universal_integer`'s implicit conversion potential).
2. **Top-down**: the context narrows the interpretation set to exactly one. If after top-down narrowing more than one interpretation remains, it is an error. If zero remain, it is also an error.

This is structurally similar to bidirectional checking but predates the formalization: Ada's overload resolution was designed in the 1970s–80s. The ARM calls this "two-pass overload resolution."

### Out-of-Range Literal Handling

Ada requires that literal values fit the resolved type at compile time. The ARM §4.9 defines "static expressions." A static numeric literal that exceeds the range of its resolved type is a **compile-time error** (Constraint_Error is raised in static expression evaluation, which is a legality rule violation):

```ada
X : Integer range 1 .. 10 := 42;
-- compile-time error: value out of range (42 not in 1..10)
```

- For subtypes with explicit ranges: range check is done at compile time.
- For base types (`Integer`, `Long_Integer`): the range is implementation-defined (in practice, 32-bit on most compilers).

Named numbers avoid range issues entirely because they retain universal type.

### Literal Type Error Diagnostics

GNAT (GCC Ada) compiler messages for literal type errors:

- `warning: value not in range of type "T"` (with Constraint_Error at elaboration for static violations)
- `error: ambiguous expression` — multiple interpretations
- `error: no match for "T" in context of expression` — no valid interpretation

GNAT uses `-gnatl` for full listing and `-gnato` to control overflow checking mode. Static violations are compile-time errors; dynamic violations are runtime `Constraint_Error`.

### Notes

- Ada's `universal_integer` predates all modern type inference systems and represents a design where mathematical-concept types exist separately from machine-implementation types.
- The `Numeric_Error` exception was present in Ada 83 and merged with `Constraint_Error` in Ada 95.
- Ada's named numbers provide a zero-cost abstraction: `Max : constant := 100;` used in expressions is inlined as `universal_integer(100)` at each use site, folded at compile time.

---

## Bidirectional Type Checking — Pierce & Turner 2000 (Local Type Inference)

Source: Benjamin C. Pierce and David N. Turner, "Local Type Inference," *ACM Transactions on Programming Languages and Systems* (TOPLAS), 22(1):1–44, January 2000. doi:10.1145/345099.345100; Jana Dunfield and Neel Krishnaswami, "Bidirectional Typing," *ACM Computing Surveys* 54(5):98, 2021; Jana Dunfield and Frank Pfenning, "Tridirectional Typechecking," POPL 2004.

### Literal Pre-Resolution Representation

Pierce and Turner's local type inference paper (2000) does not specifically address numeric literals as a distinct construct. The paper addresses *unannotated lambda abstractions* and *unannotated function arguments*. However, the bidirectional framework they introduce is the theoretical foundation that later work applies to literal types.

In the bidirectional framework, every expression is typed in one of two modes:

- **Synthesis mode** (`e ⇒ T`): the expression `e` synthesizes (produces) a type `T` without external guidance.
- **Checking mode** (`e ⇐ T`): the expression `e` is checked against an externally-provided expected type `T`.

For a numeric literal in a pure bidirectional system, the treatment is:

- In **synthesis mode**: the literal must produce a type on its own. This requires either a default type or an error.
- In **checking mode**: the externally-provided type `T` is pushed into the literal. The literal is checked to ensure its value is consistent with `T`.

### Context Sources for Type Narrowing

In the Pierce-Turner framework, context is provided by the **expected type** that flows into checking mode. The algorithm decides which mode to use based on the syntactic position:

| Syntactic position | Mode |
|---|---|
| Argument to a known function | Checking (expected type = parameter type) |
| RHS of a typed variable declaration | Checking (expected type = declared type) |
| Return expression with known return type | Checking (expected type = return type) |
| Argument to an unknown/polymorphic function | Synthesis |
| Expression in isolation | Synthesis |
| Sub-expression of a type-annotated expression | Checking |

The key rule for *application* (function call) in Pierce-Turner is:

```
Γ ⊢ e_f ⇒ T₁ → T₂    Γ ⊢ e_arg ⇐ T₁
─────────────────────────────────────────── [App]
Γ ⊢ e_f(e_arg) ⇒ T₂
```

Here `e_arg` is checked against the known parameter type `T₁`, which is the "pushing down" of context into sub-expressions.

### Defaulting Rules (When Context is Insufficient)

Pierce and Turner's paper does not define defaulting for numeric literals. In their framework, if a literal appears in synthesis mode and has no obvious type, the system requires either a default type (language policy) or produces a type error.

Dunfield & Krishnaswami ("Bidirectional Typing," 2021) note that literals in synthesis mode require a **default type** or the system must treat the literal as an error. The choice of default is language-policy, not part of the core bidirectional algorithm.

### Ambiguity / Error Reporting

In the bidirectional framework, there is no inherent ambiguity for a literal in checking mode — the expected type is given. Ambiguity arises only in synthesis mode when the literal's type cannot be determined. The formal synthesis rule:

```
[Lit-Synth]
v ∈ Literals, default(v) = T
─────────────────────────────
Γ ⊢ v ⇒ T
```

If `default(v)` is undefined and there is no expected type, the system reports a type error (no applicable rule).

The complementary checking rule is:

```
[Lit-Check]
v ∈ Literals, fits(v, T) = true
────────────────────────────────
Γ ⊢ v ⇐ T
```

If `fits(v, T) = false` (value out of range), the rule does not apply and the expression fails to type-check.

### Bidirectional Algorithm Structure

Pierce and Turner's local type inference algorithm has three components:

1. **Propagation**: expected types are propagated downward from type annotations and function signatures into sub-expressions.
2. **Recovery**: when propagation fails (no expected type available), synthesis mode is used.
3. **Unification is local**: unlike Hindley-Milner, unification is performed only within a single expression tree, not globally across a function body.

The core insight for literal typing: the **Sub** (subsumption) rule allows a synthesized type to be used where a supertype is expected:

```
Γ ⊢ e ⇒ S    S <: T
──────────────────────── [Sub]
Γ ⊢ e ⇐ T
```

For numeric literals in a subtyping system (e.g., `Int <: Long` or integer literal types as subtypes of all integer types), this rule allows a literal synthesized as the most specific type to pass checking against a wider type.

Dunfield & Krishnaswami's 2021 survey identifies four key properties of bidirectional systems:

1. Type annotations are required at introduction forms (lambdas, let-bindings) when in synthesis mode.
2. Elimination forms (function application, projection) propagate types.
3. The subsumption rule mediates between checking and synthesis.
4. Bidirectional systems are *sound* by construction if the mode assignment is correct.

### Out-of-Range Literal Handling

The `fits(v, T)` predicate in `[Lit-Check]` encodes range checking. If the literal value `v` does not fit in type `T`, the check rule fails. This is a semantic constraint, not a syntactic one. Formally:

```
fits(42, Int8) = (42 ∈ [-128, 127]) = true
fits(200, Int8) = (200 ∈ [-128, 127]) = false
```

When `[Lit-Check]` fails due to `fits`, the typechecker reports a range error at the literal's source position.

### Literal Type Error Diagnostics

Pierce and Turner's paper does not specify diagnostic format (it is a theoretical framework). The framework implies that a failed check produces a type error at the expression node where checking failed. Implemented bidirectional checkers typically report:

- Mode-of-failure: the literal failed checking against the expected type
- The literal's value
- The expected type
- The reason: either type mismatch (wrong kind of type) or range error (value out of range)

### Notes

- Pierce and Turner's 2000 paper focuses on *local* type inference: inference that is purely local to a subexpression tree, as opposed to global unification. This locality is what makes bidirectional systems predictable and error-message-friendly.
- Dunfield & Pfenning (POPL 2004) extended the framework to *tridirectional* typing, adding a third "continuation" mode for sequencing contexts.
- The system described in Pierce-Turner is *not* complete (cannot infer all types that Hindley-Milner can), but it is *decidable* and *predictable* — the programmer always knows whether a type annotation is needed.
- Dunfield's 2021 survey "Bidirectional Typing" provides a comprehensive comparison of how different languages implement the checking/synthesis distinction.

---

## TypeScript — Numeric Literal Types and Widening

Source: TypeScript Handbook §"Literal Types" https://www.typescriptlang.org/docs/handbook/2/everyday-types.html#literal-types; TypeScript GitHub microsoft/TypeScript issue #10676 "Literal type widening"; TypeScript spec (archived) §3.10; TypeScript 3.4 release notes §"Const contexts and literal types"; TypeScript compiler source `src/compiler/checker.ts`.

### Literal Pre-Resolution Representation

TypeScript's type system distinguishes between:

- **Literal types**: the type `42` (the singleton type containing only the value 42), written `42` in TypeScript type syntax.
- **Widened types**: the type `number` (the general numeric type).

A numeric literal expression `42` in TypeScript initially has the **literal type** `42`. Whether this type is preserved or **widened** to `number` depends on the context of the binding.

This is fundamentally different from Haskell, Kotlin, Swift, and Rust: TypeScript's literal types are not about resolving which machine integer type to use — they are about the *precision of type-level information* carried through the program.

### Context Sources for Type Narrowing

TypeScript's literal type resolution involves two opposite processes: **widening** (losing precision) and **narrowing** (gaining precision). For numeric literals:

**Widening triggers:**

1. **`let` declaration without annotation** — `let x = 42` gives `x` the type `number`. TypeScript widens because `let` bindings are mutable, so the literal type `42` would be misleadingly precise.
2. **Function return without annotation** — `function f() { return 42; }` infers `number` return type.
3. **Object property without `as const`** — `const obj = { x: 42 }` gives `obj.x` the type `number` (not `42`), because properties are mutable.

**Narrowing / literal type preservation triggers:**

1. **`const` declaration without annotation** — `const x = 42` gives `x` the type `42` (the literal type). `const` is immutable, so the literal type is safe and precise.
2. **Explicit literal type annotation** — `let x: 42 = 42` preserves `42` as the type.
3. **`as const` assertion** — `const obj = { x: 42 } as const` gives `obj.x` the type `42`.
4. **Union type context** — `type T = 1 | 2 | 3; let x: T = 1;` preserves `1` as the member of the union.
5. **Expected type is a literal type** — `function f(x: 42) {}; f(42)` passes because `42` (the literal) is assignable to `42` (the type).

### Defaulting Rules (When Context is Insufficient)

TypeScript's "defaulting" for numeric literals is the widening rule:

- `let` binding → widens to `number`
- `const` binding → stays as literal type `42`

This is deterministic and based on mutability semantics, not value range. TypeScript does not distinguish `Int`, `Long`, etc. — all numeric values share the `number` type (IEEE 754 double).

The widening behavior is specified in the TypeScript specification and described in the TypeScript 3.4 release notes under "const assertions." Before TypeScript 3.4, `as const` did not exist and there was no way to preserve literal types in object literals without explicit annotations.

### Ambiguity / Error Reporting

TypeScript numeric literal type errors are assignment-compatibility errors:

1. **Literal type vs. number** — `let x: 42 = 43;` errors: `Type '43' is not assignable to type '42'`.
2. **Union mismatch** — `type T = 1 | 2; let x: T = 3;` errors: `Type '3' is not assignable to type 'T'`.
3. **Overflow**: TypeScript does not check for IEEE 754 overflow for literals — `9e999` evaluates to `Infinity` silently.

There are no "ambiguity" errors for numeric literals in TypeScript. The widening/narrowing rules are deterministic.

### Bidirectional Algorithm Structure

TypeScript's checker (`src/compiler/checker.ts`) uses a form of bidirectional type checking informally described in the TypeScript design documentation:

- **Contextual typing** (TypeScript's term for checking mode): when a variable has a known type (from an annotation, inference from a prior assignment, or an expected type from a function signature), TypeScript uses that as the *contextual type* and checks the expression against it.
- **Type inference** (synthesis mode): when no contextual type is available, TypeScript infers the expression's type bottom-up.

For literal expressions specifically:

```
tcExpr(node: LiteralExpression, contextualType?: Type):
  if contextualType is a union type containing literalType(node.value):
    return literalType(node.value)  // preserve as literal
  if contextualType is 'number':
    return 'number'  // widen immediately
  if node is in const context:
    return literalType(node.value)  // preserve literal type
  else:
    return widenLiteralType(node.value)  // widen to 'number'
```

The `widenLiteralType` operation replaces the singleton type `42` with the base type `number`.

TypeScript's "contextual typing" is TypeScript's name for what Pierce and Turner call "checking mode" — the expected type is pushed down from annotations and function signatures.

### Out-of-Range Literal Handling

TypeScript does not perform integer range checking because TypeScript has no integer types — all numeric values are IEEE 754 doubles. The range of exact integer representation is approximately ±2^53, defined by `Number.MAX_SAFE_INTEGER`. TypeScript does **not** warn when a literal exceeds this range:

```typescript
const x: number = 9007199254740993;  // 2^53 + 1, not exactly representable — no error
```

TypeScript 4.x added a lint rule (`@typescript-eslint/no-loss-of-precision`) in the associated `typescript-eslint` package, but this is not part of the TypeScript compiler itself.

For the literal type `42`, there is no "out of range" concept — the type `42` is simply the value `42` treated as a type, and TypeScript has no integer bounds.

### Literal Type Error Diagnostics

TypeScript error codes for literal type issues:

- `TS2322` — `Type 'X' is not assignable to type 'Y'` — the most common literal type error
- `TS2345` — `Argument of type 'X' is not assignable to parameter of type 'Y'`

TypeScript error messages include both the inferred/actual type and the expected type, making literal type mismatches easy to diagnose:

```
Type '43' is not assignable to type '42'.
```

### Notes

- TypeScript's approach to literal types is orthogonal to the other systems surveyed: it is about *type-level precision* (singleton types vs. union types vs. base types), not about *machine integer type selection* (`u32` vs `i64`).
- The TypeScript compiler's widening behavior is called "freshness" in some parts of the codebase: a "fresh" literal type `42` widens to `number` when assigned to a mutable binding; a "stale" or "frozen" literal type never widens.
- Template literal types (TypeScript 4.1+) extend the same literal-type mechanism to strings, enabling type-safe string interpolation at the type level.
- The `satisfies` operator (TypeScript 4.9+) allows checking against a contextual type without widening the variable's declared type, providing more fine-grained control over when widening occurs.

---

## Cross-System Comparison Summary

> This section summarizes raw observable behaviors only. No recommendations.

| System | Pre-resolution representation | Default type (int) | Default type (float) | Static range check | Mode structure |
|---|---|---|---|---|---|
| Haskell | `Num a => a` (constrained type variable) | `Integer` (via defaulting) | `Double` | No (instance-defined) | HM + OutsideIn(X) constraints |
| Kotlin | Integer Literal Type (ILT), ad-hoc subtype | `Int` (or `Long` if value > Int.MAX) | `Double` | Yes, compile-time error | Expected-type propagation |
| Swift | Type variable + conformance constraint | `Int` | `Double` | Yes, compile-time error | Constraint propagation solver |
| Rust | `{integer}` inference variable | `i32` | `f64` | Yes, compile-time error | Local unification + fallback |
| Ada | `universal_integer` / `universal_real` | N/A (implicit conversion) | N/A | Yes, compile-time error | Two-pass overload resolution |
| Pierce-Turner | Literal node, no pre-type | Language policy | Language policy | Via `fits(v, T)` predicate | Checking / Synthesis modes |
| TypeScript | Literal type `42` (singleton) or `number` | `number` (widen in `let`) | same | No (no integer types) | Contextual typing |
```

---
