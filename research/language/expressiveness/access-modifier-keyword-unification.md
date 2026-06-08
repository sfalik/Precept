---
status: Cited
authored: 2026-05-26
author: Frank (Lead/Architect)
topic: Cross-position access-modifier keyword precedent — does one keyword span both field-declaration and per-instance/per-method modify positions in production languages?
external-engagement: strong
sources-fetched:
  - https://www.typescriptlang.org/docs/handbook/2/objects.html (accessed 2026-05-26)
  - https://kotlinlang.org/docs/properties.html (accessed 2026-05-26)
  - https://kotlinlang.org/docs/basic-syntax.html (accessed 2026-05-26)
  - https://doc.rust-lang.org/book/ch05-01-defining-structs.html (accessed 2026-05-26)
  - https://doc.rust-lang.org/reference/items/structs.html (accessed 2026-05-26)
  - https://doc.rust-lang.org/reference/patterns.html (accessed 2026-05-26)
  - https://raw.githubusercontent.com/swiftlang/swift-book/main/TSPL.docc/LanguageGuide/TheBasics.md (accessed 2026-05-26)
  - https://raw.githubusercontent.com/swiftlang/swift-book/main/TSPL.docc/LanguageGuide/Properties.md (accessed 2026-05-26)
  - https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly (accessed 2026-05-26)
  - https://docs.oracle.com/javase/specs/jls/se21/html/jls-8.html (accessed 2026-05-26)
  - https://docs.oracle.com/javase/specs/jls/se21/html/jls-14.html (accessed 2026-05-26)
  - https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/ (accessed 2026-05-26)
  - https://docs.scala-lang.org/tour/classes.html (accessed 2026-05-26)
  - https://docs.scala-lang.org/tour/basics.html (accessed 2026-05-26)
consumed-by: docs/Working/field-never-set-diagnostic.md § Decision 5 (keyword unification, `Stakes: irreversible`)
---

# Access-Modifier Keyword Unification — Cross-Position Precedent Survey

> One-sentence framing: a Stage-1 comparator survey of 8 production languages testing whether the same access-modifier / mutability keyword is used at both type-declaration (field) position and per-instance / per-method modify position, to ground the F-LANG-GRAPH-04 design's keyword-unification precedent leg.

## Background

The Precept design at [`docs/Working/field-never-set-diagnostic.md`](../../../docs/Working/field-never-set-diagnostic.md) (F-LANG-GRAPH-04) was demoted from `Locked` to `Externally-Grounded` during a Phase 8 retrofit because its **Decision 5** precedent leg — unifying the `writable` (field-declaration) and `editable` (per-state `modify`) keywords into a single `editable` Access Modifier — cited four comparable languages, two of which (Rust, SQL) the retrofit flagged as precision issues. The design now requires a survey-grounded precedent leg with verbatim excerpts and stable identifiers before it can re-Lock for Phase 5 implementation.

The decision is `Stakes: irreversible`: once `writable` is removed from the language surface and consumer `.precept` files write `editable`, restoring `writable` would break every consumer. Per the `/design` skill's irreversible-decision discipline, the precedent leg must rest on more than two verified comparators.

**Research question (singular, falsifiable):** Does the same access-modifier / mutability keyword appear at both (a) field / property / member declaration position AND (b) some other per-instance, per-method, per-pattern, or per-binding modify position — in production languages?

If the answer is "yes in most surveyed languages," the cross-position unification pattern is established prior art, and Decision 5's claim is defensible on broader grounds than the original v2 stated. If the answer is "rarely or never," the design's framing collapses and the keyword unification needs a different defense (e.g., catalog-category-cleanup-only, not precedent-grounded).

## Methodology

- **Research question.** Does one keyword span both (a) field declaration AND (b) per-instance-use / per-method / per-binding mutability marking, in production languages?
- **Search strategy.** Official language documentation (specs / handbooks / reference manuals) for 8 production languages chosen to cover the major ML-family / OO-family / systems-family / web-family quadrants: TypeScript, Kotlin, Rust, Swift, C#, Java, F#, Scala. Each fetched via WebFetch on 2026-05-26 from the canonical documentation URL. For Swift, where the published docs.swift.org site renders as an SPA returning empty HTML to fetch tools, I substituted the canonical raw markdown source at github.com/swiftlang/swift-book — same authoritative content, fetch-friendly format.
- **Inclusion criteria.** A language is in scope if (1) it's a mainstream production language with stable public documentation, (2) it has a keyword expressing read-only-ness, immutability, or mutability that appears at a field/property declaration position. SQL was deliberately excluded from this survey — it was flagged in the Phase 8 retrofit as a conceptual analogy (privilege grant DDL is separate from table-creation DDL) rather than a same-keyword-at-multiple-positions syntactic precedent, so it doesn't speak to the research question.
- **Exclusion criteria.** Excluded: SQL (no syntactic same-keyword precedent), dynamic languages without field declarations (Python, Ruby, JavaScript), and academic / niche languages (Haskell `let`, OCaml `mutable`, ML variants) — narrower scope keeps the survey verifiable.
- **Source-grade declaration.** All 14 fetched sources are **Primary** — official language documentation hosted on the language vendor's documentation domain (Microsoft Learn for C# / F#, Oracle docs for the JLS, kotlinlang.org for Kotlin, doc.rust-lang.org for Rust, swift.org / swift-book repository for Swift, typescriptlang.org for TypeScript, docs.scala-lang.org for Scala). No Secondary (vendor blog) or Tertiary (forum / knowledge-from-training) sources were used.
- **Time bounds.** All fetches performed 2026-05-26. Source URLs are stable canonical paths (not preview URLs); citations record the access date.
- **Verbatim discipline.** Every per-language assessment below carries a direct quote from the fetched source, presented as a markdown blockquote with URL and access date. Bare-prose claims without an excerpt are not used.

## Findings — comparator table

| # | Language | Keyword | Positions where the keyword appears | Same-keyword-at-multiple-positions? | Verdict |
|---|---|---|---|---|---|
| 1 | TypeScript | `readonly` | Interface property, class property, index signature, array shorthand (`readonly T[]`), tuple type | Yes — 5 declaration positions | **Strong** |
| 2 | Kotlin | `val` / `var` | Class property, top-level property, local variable, primary-constructor parameter | Yes — 4 declaration positions | **Strong** |
| 3 | Rust | `mut` | `let mut` binding, function parameter, method receiver (`&mut self`), reference type (`&mut T`), pattern (`mut?`) — **NOT struct field** | No at struct-field position; yes at non-field positions | **Exception** |
| 4 | Swift | `let` / `var` | Local constant/variable, stored property (struct / class) | Yes — 2 declaration positions | **Strong** |
| 5 | C# | `readonly` | Field declaration, `readonly struct`, struct instance member, `ref readonly` return, `ref readonly` parameter | Yes — 5 declaration positions | **Strong** |
| 6 | Java | `final` | Field declaration, local variable declaration (plus parameters, methods, classes per the broader `final` use) | Yes — 2+ declaration positions | **Strong** |
| 7 | F# | `let` + `mutable` | `let` binds a value; `mutable` is a separate keyword that flags reassignable values (record field or local) | Partial — keyword **pair**, not single shared keyword | **Partial** |
| 8 | Scala | `val` / `var` | Class member, primary-constructor parameter, top-level value, local value | Yes — 4 declaration positions | **Strong** |

**Net:** 6 strong / 1 partial / 1 exception across 8 surveyed languages.

## Per-comparator detail

### 1. TypeScript — `readonly` [Primary]

> "Properties can also be marked as `readonly` for TypeScript."
>
> ```typescript
> interface SomeType {
>   readonly prop: string;
> }
> ```
>
> "While it won't change any behavior at runtime, a property marked as `readonly` can't be written to during type-checking."
>
> "The `ReadonlyArray` is a special type that describes arrays that shouldn't be changed."
>
> "Just as TypeScript provides a shorthand syntax for `Array<Type>` with `Type[]`, it also provides a shorthand syntax for `ReadonlyArray<Type>` with `readonly Type[]`."
>
> "Finally, you can make index signatures `readonly` in order to prevent assignment to their indices:"
>
> ```typescript
> interface ReadonlyStringArray {
>   readonly [index: number]: string;
> }
> ```
>
> "One final note about tuple types - tuple types have `readonly` variants, and can be specified by sticking a `readonly` modifier in front of them - just like with array shorthand syntax."
>
> — *TypeScript Handbook § Objects > Readonly Properties* (https://www.typescriptlang.org/docs/handbook/2/objects.html, accessed 2026-05-26)

**Assessment.** Five distinct positions for the same `readonly` keyword: interface property declarations, class property declarations, index signatures, the `readonly T[]` array shorthand, and the `readonly [T, U]` tuple form. **Strong precedent for cross-position keyword unification.**

### 2. Kotlin — `val` / `var` [Primary]

> "Properties can be mutable (`var`) or read-only (`val`)."
>
> — *Kotlin Reference § Properties* (https://kotlinlang.org/docs/properties.html, accessed 2026-05-26)

> "Use the `val` keyword to declare variables that are assigned a value only once. These are immutable, read-only local variables that can't be reassigned a different value after initialization:"
>
> ```kotlin
> fun main() {
>     val x: Int = 5
>     println(x)
> }
> ```
>
> "Use the `var` keyword to declare variables that can be reassigned. These are mutable variables, and you can change their values after initialization:"
>
> "You can declare variables at the top level:"
>
> ```kotlin
> val PI = 3.14
> var x = 0
> ```
>
> — *Kotlin Reference § Basic syntax* (https://kotlinlang.org/docs/basic-syntax.html, accessed 2026-05-26)

**Assessment.** The same `val`/`var` keyword pair declares class properties, primary-constructor parameters (`class Rectangle(val height: Double, val length: Double)`), top-level properties, and local variables — at least four positions. **Strong precedent.**

### 3. Rust — `mut` [Primary]

Rust is the documented exception. The Rust Book and the Rust Reference grammar both confirm that `mut` does NOT appear at struct field declarations.

> "Note that the entire instance must be mutable; Rust doesn't allow us to mark only certain fields as mutable."
>
> — *The Rust Programming Language, Chapter 5.1 — Defining and Instantiating Structs* (https://doc.rust-lang.org/book/ch05-01-defining-structs.html, accessed 2026-05-26)

The grammar for struct fields in the Rust Reference contains no `mut` slot — only attributes, visibility, identifier, and type:

> ```
> StructField → OuterAttribute* Visibility? IDENTIFIER : Type
> ```
>
> — *Rust Reference § Items > Structs* (https://doc.rust-lang.org/reference/items/structs.html, accessed 2026-05-26)

`mut` instead lives at identifier-pattern position:

> ```
> IdentifierPattern → ref? mut? IDENTIFIER ( @ PatternNoTopAlt )?
> ```
>
> — *Rust Reference § Patterns* (https://doc.rust-lang.org/reference/patterns.html, accessed 2026-05-26)

**Assessment.** Rust uses the same `mut` keyword at pattern bindings (`let mut x = 5`), function parameters (`fn f(mut x: T)`), method receivers (`fn f(&mut self)`), and reference types (`&mut T`) — but **not at struct field declarations**. Field mutability follows the binding mutability of the struct instance, not a per-field modifier. **Exception to the cross-position-at-field-declaration pattern, with the explicit "Rust doesn't allow us to mark only certain fields as mutable" sentence confirming the design's precision-issue flag.** The narrower true statement — "Rust uses one `mut` keyword across non-field positions" — does not satisfy the cross-position-including-field claim Decision 5 originally made.

### 4. Swift — `let` / `var` [Primary]

> "You declare constants with the `let` keyword and variables with the `var` keyword."
>
> ```swift
> let maximumNumberOfLoginAttempts = 10
> var currentLoginAttempt = 0
> ```
>
> — *The Swift Programming Language § The Basics > Constants and Variables* (https://raw.githubusercontent.com/swiftlang/swift-book/main/TSPL.docc/LanguageGuide/TheBasics.md, accessed 2026-05-26)

> "Stored properties can be either *variable stored properties* (introduced by the `var` keyword) or *constant stored properties* (introduced by the `let` keyword)."
>
> ```swift
> struct FixedLengthRange {
>     var firstValue: Int
>     let length: Int
> }
> ```
>
> — *The Swift Programming Language § Properties > Stored Properties* (https://raw.githubusercontent.com/swiftlang/swift-book/main/TSPL.docc/LanguageGuide/Properties.md, accessed 2026-05-26)

**Assessment.** The same `let`/`var` keyword pair declares both local constants/variables and stored properties on structs/classes. The Swift book is explicit that the property keywords are the same as the binding keywords ("introduced by the `var` keyword" / "introduced by the `let` keyword"). **Strong precedent for cross-position unification.**

### 5. C# — `readonly` [Primary]

The C# language reference enumerates five contexts for the `readonly` keyword:

> "Use the `readonly` keyword as a modifier in five contexts:
>
> - In a field declaration, `readonly` means you can only assign the field during the declaration or in a constructor in the same class. […]
> - In a `readonly struct` type definition, `readonly` means the structure type is immutable. […]
> - In an instance member declaration within a structure type, `readonly` means an instance member doesn't modify the state of the structure. […]
> - In a `ref readonly` method return, the `readonly` modifier indicates that the method returns a reference and writes aren't allowed to that reference.
> - To declare a `ref readonly` parameter to a method."
>
> — *C# Language Reference — readonly keyword* (https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly, accessed 2026-05-26)

**Assessment.** The Microsoft Learn reference explicitly enumerates five contexts for a single keyword, including the field-declaration context that Decision 5 needs. This is the **single strongest cross-position-keyword precedent in the surveyed corpus** — the docs themselves frame the multi-position usage as the keyword's defining feature, not an incidental side effect.

### 6. Java — `final` [Primary]

> "A field can be declared `final` (§4.12.4). Both class and instance variables (`static` and non-`static` fields) may be declared `final`."
>
> Grammar from § 8.3.1 (Field Modifiers):
>
> ```
> FieldModifier: (one of) Annotation public protected private static final transient volatile
> ```
>
> — *Java Language Specification SE 21 § 8.3.1.2 — final Fields* (https://docs.oracle.com/javase/specs/jls/se21/html/jls-8.html, accessed 2026-05-26)

> "If the optional keyword `final` appears at the start of the declaration, the variable being declared is a final variable (§4.12.4)."
>
> Grammar from § 14.4 (Local Variable Declarations):
>
> ```
> VariableModifier: Annotation final
> ```
>
> — *Java Language Specification SE 21 § 14.4.1* (https://docs.oracle.com/javase/specs/jls/se21/html/jls-14.html, accessed 2026-05-26)

**Assessment.** The JLS grammar productions show `final` listed explicitly as both a `FieldModifier` (§ 8.3.1) and a `VariableModifier` (§ 14.4), with both sections cross-referencing § 4.12.4 (final Variables) as the unified semantic definition. The same `final` keyword also appears on parameters, methods, and classes (not separately quoted here as the two excerpts above already establish multi-position usage at the field + local-variable positions Decision 5 cares about). **Strong precedent.**

### 7. F# — `let` + `mutable` [Primary]

> "The `let` keyword binds a value, as in the following examples:
>
> ```fsharp
> let a = 1
> let b = 100u
> let str = \"text\"
> ```"
>
> "You can use the keyword `mutable` to specify a variable that can be changed. Mutable variables in F# should generally have a limited scope, either as a field of a type or as a local value. Mutable variables with a limited scope are easier to control and are less likely to be modified in incorrect ways."
>
> ```fsharp
> let mutable x = 1
> x <- x + 1
> ```
>
> — *F# Language Reference — Values* (https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/, accessed 2026-05-26)

**Assessment.** F# is the partial case. The verbatim sentence — *"Mutable variables in F# should generally have a limited scope, either as a field of a type or as a local value"* — explicitly confirms that `mutable` is the *same* keyword at both record-field position and local-binding position. That satisfies the cross-position-keyword claim. But the F# pattern is a **keyword pair** (`let mutable …` — two keywords combined) rather than a single shared keyword like TypeScript's `readonly` or C#'s `readonly`. The F# precedent supports the unification *direction* but is structurally less clean than the single-keyword cases.

### 8. Scala — `val` / `var` [Primary]

> "Primary constructor parameters with `val` and `var` are public."
>
> ```scala
> class Point(val x: Int, val y: Int)
> ```
>
> "Parameters without `val` or `var` are private values, visible only within the class."
>
> — *Scala Tour — Classes* (https://docs.scala-lang.org/tour/classes.html, accessed 2026-05-26)

> "You can name the results of expressions using the `val` keyword"
>
> ```scala
> val x = 1 + 1
> ```
>
> "Variables are like values, except you can re-assign them. You can define a variable with the `var` keyword."
>
> ```scala
> var x = 1 + 1
> x = 3 // This compiles because \"x\" is declared with the \"var\" keyword.
> ```
>
> — *Scala Tour — Basics* (https://docs.scala-lang.org/tour/basics.html, accessed 2026-05-26)

**Assessment.** The same `val`/`var` keyword pair declares (a) class members, (b) primary-constructor parameters with public visibility, (c) top-level / local values. **Strong precedent for cross-position keyword unification.**

## Findings — does the precedent hold?

The cross-position-keyword pattern holds in **6 of 8** surveyed languages (TypeScript, Kotlin, Swift, C#, Java, Scala), is **partial in 1 of 8** (F# — keyword pair `let mutable`, not single shared keyword), and is **explicitly absent in 1 of 8** at the field-declaration position specifically (Rust — `mut` is binding-level, not per-field; the Rust Book is explicit on this).

| Verdict | Count | Languages |
|---|---|---|
| Strong cross-position precedent | 6 | TypeScript, Kotlin, Swift, C#, Java, Scala |
| Partial (keyword pair, not single keyword) | 1 | F# |
| Exception at field-declaration position | 1 | Rust |

The dominant industry pattern is keyword unification across field-declaration and other-position modes. Rust is the documented and well-known exception — and the exception is *itself* informative, because the Rust Book explicitly states *"Rust doesn't allow us to mark only certain fields as mutable"*. The Rust pattern is "binding-level mutability inherits to fields," which is a deliberate design choice, not an oversight. F# represents a third design choice — a keyword pair where one keyword (`let`) introduces the binding and a second keyword (`mutable`) qualifies it — applied uniformly across positions.

**The survey supports the F-LANG-GRAPH-04 Decision 5 unification claim, on broader grounds than the original v2 statement.** The accurate framing is: "6 of 8 surveyed production languages share the unified-keyword pattern; the dominant industry default is one keyword across field-declaration and other positions; Rust is the documented exception (binding-level inheritance); F# uses a keyword pair."

## Recommendation for Precept

Decision 5's keyword-unification claim is **supported** by surveyed precedent on broader grounds than the original v2 stated. Recommended re-framing for the design's precedent leg when it re-Locks:

- **Keep**: TypeScript `readonly` (5 positions), Kotlin `val`/`var` (4 positions) — both already cited, now with verbatim excerpts and stable identifiers.
- **Add**: Swift `let`/`var` (local + stored property), C# `readonly` (5 contexts including field — strongest single-keyword example in the survey), Java `final` (field + local + parameter + method + class), Scala `val`/`var` (class member + ctor param + top-level + local). These four were absent from v2 and each is a Primary-source-grade strong precedent.
- **Re-frame Rust**: drop the original "Rust `mut` appears at field declarations and in patterns/bindings" framing (which the Phase 8 retrofit correctly flagged as inaccurate). Re-frame as: *"Rust is the documented exception — the Rust Book states explicitly that fields cannot be individually marked mutable; mutability is inherited from the binding (`let mut foo`). The exception is a deliberate design choice (binding-level inheritance), not an oversight."* This is more honest than removing Rust entirely and more useful for readers who know Rust.
- **Remove SQL**: as the Phase 8 retrofit flagged, SQL `GRANT UPDATE` is a privilege-grant DDL conceptual analogy, not a same-keyword-at-multiple-positions syntactic precedent. It doesn't answer the cross-position-keyword research question and was excluded from this survey.
- **Cite F# as a partial precedent**: the keyword-pair pattern (`let mutable`) is structurally different from a single shared keyword, but the F# docs explicitly confirm that `mutable` applies at both field-of-a-type and local-value positions — the cross-position-keyword direction holds, just via a two-keyword construction.

After this re-framing, Decision 5's precedent leg rests on **6 verified Primary-source strong precedents + 1 partial precedent + 1 honestly-described exception**, instead of the original "every comparable language" overstatement.

## What would change this conclusion

Three observation-shapes would force re-investigation:

1. **Counter-evidence from an additional mainstream surveyed comparator.** If a survey of 4+ additional production languages (Go, C++, Dart, Ruby, Python, OCaml, Haskell, …) shows 3+ of them following Rust's binding-level-inheritance pattern rather than the field-declaration pattern, the "6 of 8" majority claim weakens to "majority among a curated 8-language set, minority in a broader sample." Strong response: re-frame the precedent as "industry pattern in OO/web-family languages; less consistent in systems-family languages" — narrower claim, still defensible.

2. **A position-specific aliases-under-the-hood discovery.** If Kotlin's `val` at class-property position were shown to compile to a structurally different runtime construct than `val` at local-variable position (e.g., property syntactic sugar generating a Java field + getter, vs local being a plain local) such that they share only the surface keyword but not the semantics, the "same keyword across positions" claim becomes a surface-level coincidence rather than a deep design unification. Strong response: distinguish "surface keyword unification" (which Precept proposes) from "semantic unification" (which is a separate claim Decision 5 isn't making).

3. **Empirical UX evidence that `editable` confuses domain experts.** If F3 in the design's Falsifiers list (`editable` reads as "currently being edited" instead of "the caller has write capability") is observed in domain-expert testing of the sample corpus, the keyword *choice* (not the unification *direction*) was wrong. Strong response: revert Decision 5's specific keyword choice — keep the unification direction (single keyword), but pick a different keyword. This survey grounds the *direction*, not the specific lexeme.

## Threats to Validity

- **Selection bias on 8 comparators.** The 8 languages were chosen for OO/ML/systems/web-family quadrant coverage, not by enumerative survey of all production languages with field declarations. A broader survey could shift the majority count. Honest declaration: this is a curated set, not a comprehensive enumeration. The 8-language slate matches the original Decision 5 v2 framing extended with Phase 8's additions — it tests the design's specific precedent claim, not the universal "all languages" claim.
- **Swift docs.swift.org SPA-rendered; substituted source.** The published Swift documentation site at docs.swift.org renders content via client-side JavaScript and returned empty HTML to WebFetch. Substituted the canonical raw markdown from `github.com/swiftlang/swift-book/main/TSPL.docc/LanguageGuide/*.md` — the same source the published site renders, in fetch-friendly format. The substitution is upgrade, not downgrade — same authoritative authorship, more directly verifiable. No load-bearing claim rests on inaccessible material.
- **F# constructor-parameter and inherited-`mutable` positions not separately confirmed.** The Microsoft Learn F# Values page confirms `mutable` at "field of a type" and "local value" positions but does not separately confirm class-property positions in detail. The F# verdict (Partial) is conservative and would not flip even if more positions were confirmed — the partial-verdict reason is structural (keyword pair, not single keyword), not position-count.
- **No empirical UX evidence in this survey.** This is a precedent / prior-art survey, not a UX study. It establishes that other languages use the cross-position pattern; it doesn't establish that Precept's specific lexeme choice (`editable`) is the right choice. F3 in the design's Falsifiers list covers the UX question; this survey doesn't answer it.
- **Recency.** All sources fetched 2026-05-26. The TypeScript handbook, Kotlin reference, Rust Book, Swift Book, C# reference, JLS SE 21, F# reference, and Scala tour are all stable canonical paths that update via documented language-version releases (not silently). Citation excerpts are reproducible by re-fetching the same URLs. The C# reference page metadata records `ms.date: 2026-01-22` (last edit) and the document references C# version history — recent enough that the five-context enumeration reflects current C# (through C# 12).
- **Knowledge-from-training-data not used.** Every load-bearing claim above carries a verbatim excerpt from a Primary source fetched 2026-05-26. No Tertiary substitutions.

## Open Questions

These surfaced during the survey but do not bear on Decision 5's re-Lock:

1. **C# `ref readonly` parameter position (5th context).** The Microsoft Learn page enumerates `ref readonly` parameter as a fifth `readonly` context, sub-bulleted under the `ref readonly` return context. This is a recent addition to the language. Whether Precept's eventual `editable` modifier has a parallel parameter-position equivalent is unresolved and out of scope.
2. **Kotlin / Scala primary-constructor-parameter as a Precept analog.** Both Kotlin and Scala allow `val`/`var` directly on primary-constructor parameters (`class Point(val x: Int, val y: Int)`), unifying the parameter and member-field declaration positions. Whether Precept should have an analogous "transition-payload field as access-modifier-bearing declaration" is an open language-design question, separately scoped.
3. **Rust's binding-inheritance pattern as an alternative model.** Rust's "the entire instance must be mutable" pattern is a different design choice than per-field marking — could Precept model field write-capability as instance-level (e.g., a precept declares itself `mutable` and all `editable` fields inherit from that)? This is a separate design question; it doesn't affect Decision 5 (which already proposes per-field unification).
4. **F# `let mutable` as a counter-argument to single-keyword unification.** Does F#'s keyword-pair pattern argue *against* single-keyword unification — i.e., is "two keywords combined" a better model than "one keyword across positions"? The argument has some surface appeal (each keyword does one job) but adds verbosity; not pursued further in this survey since 6 of the 8 surveyed languages reject the keyword-pair pattern.

## Sources

All sources Primary; all fetched 2026-05-26.

| # | Title | Author / Org | Stable identifier (URL) | Access date |
|---|---|---|---|---|
| 1 | TypeScript Handbook — Objects > Readonly Properties | Microsoft / TypeScript team | https://www.typescriptlang.org/docs/handbook/2/objects.html | 2026-05-26 |
| 2 | Kotlin Reference — Properties | JetBrains | https://kotlinlang.org/docs/properties.html | 2026-05-26 |
| 3 | Kotlin Reference — Basic syntax | JetBrains | https://kotlinlang.org/docs/basic-syntax.html | 2026-05-26 |
| 4 | The Rust Programming Language — Chapter 5.1 (Defining Structs) | Rust Foundation | https://doc.rust-lang.org/book/ch05-01-defining-structs.html | 2026-05-26 |
| 5 | The Rust Reference — Items > Structs | Rust Foundation | https://doc.rust-lang.org/reference/items/structs.html | 2026-05-26 |
| 6 | The Rust Reference — Patterns | Rust Foundation | https://doc.rust-lang.org/reference/patterns.html | 2026-05-26 |
| 7 | The Swift Programming Language — The Basics (source) | Apple / Swift project | https://raw.githubusercontent.com/swiftlang/swift-book/main/TSPL.docc/LanguageGuide/TheBasics.md | 2026-05-26 |
| 8 | The Swift Programming Language — Properties (source) | Apple / Swift project | https://raw.githubusercontent.com/swiftlang/swift-book/main/TSPL.docc/LanguageGuide/Properties.md | 2026-05-26 |
| 9 | C# Language Reference — `readonly` keyword | Microsoft | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly | 2026-05-26 |
| 10 | Java Language Specification SE 21 — § 8.3.1.2 (final Fields) | Oracle | https://docs.oracle.com/javase/specs/jls/se21/html/jls-8.html | 2026-05-26 |
| 11 | Java Language Specification SE 21 — § 14.4 (Local Variable Declarations) | Oracle | https://docs.oracle.com/javase/specs/jls/se21/html/jls-14.html | 2026-05-26 |
| 12 | F# Language Reference — Values | Microsoft | https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/ | 2026-05-26 |
| 13 | Scala Tour — Classes | Scala Center / EPFL | https://docs.scala-lang.org/tour/classes.html | 2026-05-26 |
| 14 | Scala Tour — Basics | Scala Center / EPFL | https://docs.scala-lang.org/tour/basics.html | 2026-05-26 |
| 15 | (declared unfetchable — not used) JLS SE 21 § 4.12.4 (final Variables) | Oracle | https://docs.oracle.com/javase/specs/jls/se21/html/jls-4.html#jls-4.12.4 | 2026-05-26 — § 14.4 + § 8.3.1.2 cite this section but neither was substituted from this URL; load-bearing material was sourced directly from § 8.3.1.2 and § 14.4 instead |
| 16 | (deliberately excluded from survey scope) SQL standard / `GRANT UPDATE` | ISO / ANSI | — | — |
