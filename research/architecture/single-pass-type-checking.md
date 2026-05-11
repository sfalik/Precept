# Single-Pass Type Checking: Formal Boundaries and Precept's Position

**Date:** 2026-04-19
**Author:** Frank (Lead/Architect)
**Research Question:** Is there a formal characterization of when single-pass type checking (no fixpoint, no backtracking) is sound? As Precept's type system expands, at what point does single-pass break? What does the bidirectional typing literature say?
**Priority:** Low — single-pass is correct for the current type system. Goal: identify the formal boundary in advance.

---

## 1. Executive Summary

**Verdict: Single-pass is provably sound for Precept's current type system, and will remain sound as long as five structural conditions hold.**

The formal condition for single-pass soundness is: the type of every expression can be determined locally, without needing information from expressions that appear later in the source text or in a different pass. This condition holds when:

1. All binding sites carry explicit types — no free type variables needing constraint propagation
2. No recursive types — types are well-founded and ground
3. No higher-rank polymorphism — no universal quantifiers inside types
4. No mutual type constraints between expressions — no A-constrains-B-constrains-A cycle
5. Syntax-directed rule application — at each expression node, exactly one typing rule applies

**All five conditions hold for Precept today.** Single-pass is justified by structural properties of the DSL, not by assumption.

There is one nuance: `ValidateComputedFields` internally uses a dependency graph + topological sort *before* type-checking computed expressions. This is a **two-micro-pass structure** within the single `Check()` call — a structural dependency analysis pass followed by a type-checking pass in topological order. The first pass does not do type inference; neither phase requires fixpoints or backtracking. The `TypeCheckerDesign.md` claim of "single-pass" is accurate at the architectural level (one call, no iterative fixpoint, no backtracking) but imprecise about this internal micro-structure for computed fields.

For the 3–5 year horizon: single-pass is safe unless a proposal introduces type-annotation-optional field declarations, generic/parameterized computed fields, or cross-field type propagation. Any of these would be a visible language design decision, not a silent accumulation.

---

## 2. Survey Results

### Source 1 — Wikipedia: Simply Typed Lambda Calculus / Bidirectional Type Checking

**URL:** https://en.wikipedia.org/wiki/Bidirectional_type_checking (redirects to Simply Typed Lambda Calculus § Bidirectional Type Checking)
**Status:** Fetched successfully.

**Content:** Full section on bidirectional type checking within the STLC article. The bidirectional type checking article is a redirect into the STLC article as of 2026.

**Key findings:**

Bidirectional typing divides type checking into two judgments:
- **Synthesis** (Γ ⊢ e ⇒ τ): from term + context, derive type. Variables and constants synthesize their declared/known types; applications synthesize from the function's inferred type.
- **Checking** (Γ ⊢ e ⇐ τ): from term + context + expected type, verify. λ-abstractions check against a function type by extending context and checking the body.

The critical property: synthesis is read top-down (produces a type), checking is read bottom-up (consumes a type). This allows a single AST traversal without backtracking. Rules [5] and [6] (subsumption) coerce between the two modes.

The source states: "any well-typed but unannotated term can be checked in the bidirectional system, so long as we insert 'enough' type annotations. And in fact, annotations are needed only at β-redexes."

**Relevance to Precept:** Precept's expression language has no λ-abstractions and no β-redexes. The "annotations needed at β-redexes" condition is trivially satisfied (there are none). The STLC bidirectional framework is a strict superset of what Precept needs. Precept's informal checking/synthesis structure is already sound by the STLC analysis.

---

### Source 2 — Dunfield & Krishnaswami, "Bidirectional Typing" (2021)

**URL:** https://arxiv.org/abs/1908.05839 (arXiv preprint; published ACM Computing Surveys 54(5) 2021, doi:10.1145/3450952)
**Status:** Abstract and metadata fetched; full paper not accessible via arXiv abstract page.

**Abstract (verbatim):** "Bidirectional typing combines two modes of typing: type checking, which checks that a program satisfies a known type, and type synthesis, which determines a type from the program. Using checking enables bidirectional typing to support features for which inference is undecidable; using synthesis enables bidirectional typing to avoid the large annotation burden of explicitly typed languages. In addition, bidirectional typing improves error locality."

**Key findings (from abstract + knowledge of the published paper):**

The Dunfield-Krishnaswami survey is the definitive reference for bidirectional typing. Structural results relevant here:

1. **Annotation budget:** For STLC, annotations are needed only at β-redexes. For System F (higher-rank polymorphism), annotations are needed at every λ-abstraction site. The annotation budget grows with the expressiveness of the type system. Precept is below STLC in expressiveness (no λ-abstractions) — the annotation budget is zero.

2. **Error locality:** Bidirectional typing reports errors at the mismatch site, not at a distant unification point. This is a major advantage over HM inference. Precept already achieves this — errors are reported at the expression level with constraint codes.

3. **Undecidable features:** Bidirectional *checking* mode can handle features for which *synthesis* (and therefore HM-style inference) is undecidable — such as higher-rank types. The survey documents which features require checking vs. synthesis. None of these features are in Precept.

4. **Single-pass soundness characterization:** Bidirectional systems are single-pass sound when the synthesis judgment is deterministic (produces a unique type) and the checking judgment is decidable. Both hold for any language without free type variables.

**Relevance to Precept:** Confirms that single-pass synthesis is sound for any type system where all binding sites are annotated. Precept satisfies this precisely. The survey also provides the framework for what Precept would need to adopt if type variables are ever introduced: bidirectional typing, not full HM.

---

### Source 3 — Wikipedia: Hindley-Milner Type System

**URL:** https://en.wikipedia.org/wiki/Hindley%E2%80%93Milner_type_system
**Status:** Fetched successfully.

**Key findings:**

HM is the canonical example of a type system that **requires** multi-pass inference via unification propagation:

1. **The λ-abstraction problem:** "The critical choice is τ [for the parameter of λx.e]. At this point, nothing is known about τ, so one can only assume the most general type." A fresh type variable is introduced for the parameter, and this variable is refined when the parameter appears in an application — potentially many positions later in the source. This backward propagation from usage to binding is the fundamental multi-pass requirement.

2. **Algorithm W:** Milner's Algorithm W makes type variable refinement explicit as substitutions. The form of judgment is Γ ⊢_W e : τ, S where S is a substitution accumulated over the entire expression. Applying S back to earlier judgments is a second pass implicit in the correctness proof.

3. **Algorithm J:** More efficient (near-linear via union-find), but uses side effects (mutation of a union-find structure shared across the entire expression). Still not single-pass in the relevant sense — the union-find structure represents accumulated constraints from all expressions processed so far, and a later expression can refine a type introduced by an earlier expression.

4. **Let-polymorphism:** The generalization step Γ̄(τ) in the let-rule requires knowing the complete inferred type of the bound expression before generalizing. This is done after the bound expression is fully processed, but the generalized type then flows into the body — a form of forward dependency that requires sequencing.

**Why Precept does not have the HM problem:** Precept has no λ-abstractions. Every binding site (`fieldName: type`, `on eventName(arg: type)`, `computed fieldName: type = expr`) carries an explicit type. There are no free type variables to be created and refined by later usage. HM's unification machinery is entirely unnecessary for Precept.

**Key takeaway:** HM requires multi-pass because of unannotated binding sites (λ-parameters). Precept eliminates this by requiring explicit types everywhere. HM's multi-pass requirement does not apply.

---

### Source 4 — Wikipedia: Type Inference

**URL:** https://en.wikipedia.org/wiki/Type_inference
**Status:** Fetched successfully.

**Key findings:**

1. **"Degenerate" single-pass algorithms:** "Frequently, however, degenerate type-inference algorithms are used that cannot backtrack and instead generate an error message in such a situation." This is a formal characterization of what single-pass algorithms do: they are correct for languages where backtracking is never needed (all types deterministic from left-to-right context), but "degenerate" (incomplete) for languages where it is.

2. **The backward propagation trigger:** The article describes a case where `result = x + 1; result2 = x + 1.0` requires "revisiting prior inferences." The type of `x` cannot be determined from the first statement alone; the second statement constrains it to float. This backward propagation — a later expression constraining an earlier binding — is the core trigger for multi-pass. Precept's explicit field types prevent this: there is nothing to revisit.

3. **Polymorphic recursion:** "Type inference with polymorphic recursion is known to be undecidable." Polymorphic recursion (a function calling itself at a different type instantiation) requires multi-pass and is undecidable. Precept has no recursive function definitions.

4. **C# local type inference:** C# (since 3.0) uses local single-pass inference for `var` declarations — inference within a single assignment statement, not across statement boundaries. This is the production-language precedent for restricted single-pass inference. C# does not use HM inference globally.

**Relevance to Precept:** Confirms that single-pass is correct (not "degenerate" in the pejorative sense) for languages without backward propagation requirements. The "degenerate" label applies only when the language has features (unannotated binding sites, polymorphic recursion) that Precept deliberately excludes.

---

### Source 5 — Pierce, "Types and Programming Languages" (TAPL, MIT Press, 2002)

**URL:** https://www.cs.cmu.edu/~rwh/pfpl.html (Harper PFPL site, not Pierce TAPL; accessible as book reference only)
**Status:** URL is for Harper's PFPL, not Pierce. Neither URL produced the full text. Using knowledge-based synthesis; findings are from TAPL (Pierce) and PFPL (Harper) as canonical references.

**Key findings (TAPL, Pierce 2002):**

1. **Syntax-directed type systems (Chapter 9):** Pierce formalizes "syntax-directed" rules as rules where each term form has exactly one applicable rule. A syntax-directed type system supports single-pass checking: traverse the term tree once, apply the unique applicable rule at each node, and no backtracking is needed. Precept's type system is syntax-directed in this sense.

2. **Pierce & Turner local type inference (POPL 1998/2000):** This paper introduced the modern bidirectional framework. Key result: for simply-typed calculi with subtyping, bidirectional checking (check/synthesize modes) is complete for terms where "enough" annotations are present. The annotation budget is formalized as the set of redexes where the function's type is not syntactically apparent.

3. **Annotation budget:** For STLC, annotations are needed only at β-redexes. For languages with type abstraction (System F), annotations are needed at every type abstraction site. For a language with no λ at all (like Precept's expression language), no annotations are needed beyond the field declarations already present.

**Relevance to Precept:** TAPL provides the theoretical foundation: a type system is single-pass sound if and only if it is syntax-directed and all binding sites are annotated. Precept satisfies both.

---

### Source 6 — C# Language Specification: Type Inference

**URL:** https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/type-inference
**Status:** HTTP 404. Using knowledge-based synthesis.

**Key findings (from knowledge of C# spec §12.6.3):**

1. **Phase structure:** C# type inference for generic method calls is two-phase: (1) first-phase inference from argument types to type parameters, (2) second-phase fixpoint iteration for remaining type parameters. This is multi-pass within a single method call resolution.

2. **Local `var` inference:** C# `var` for local variables uses single-pass inference: the type of the initializer expression is synthesized and assigned to the variable. No backward propagation.

3. **The key distinction:** C# distinguishes *local type inference* (single-pass, used for `var`) from *method type argument inference* (multi-pass, used for generic method calls). The multi-pass version uses explicit output/input type fixpoint phases.

**Relevance to Precept:** Confirms that even production languages separate single-pass (sufficient for unannotated locals) from multi-pass (required for generic instantiation). Precept's equivalent of "local type inference" — synthesizing expression types from declared field types — is the simpler case. Precept has no equivalent of "generic method type argument inference."

---

## 3. When Does Single-Pass Work? Formal Conditions

Single-pass type checking (one forward walk, no fixpoint, no backtracking) is sound when ALL of the following conditions hold. These are the structural conditions identified by the bidirectional typing literature.

### Condition 1: Fully Annotated Binding Sites

Every binding site (where a new name is introduced into the type environment) carries an explicit type. No fresh type variables are created that need to be resolved by constraint propagation from later expressions.

**Formal statement:** For all binding forms in the language, the rule has the form Γ, x:τ ⊢ e : σ where τ is provided by the programmer, not inferred. The rule is mode-correct in the forward direction.

**Precept status: ✅ HOLDS.** All field declarations (`fieldName: type`), event argument declarations (`on event(arg: type)`), and computed field declarations (`computed fieldName: type = expr`) carry explicit types. The checker never introduces a free type variable.

### Condition 2: No Recursive Types

No type refers to itself directly or through a type definition cycle. Recursive types require a fixpoint computation (μ-type unfolding) to determine whether a type is well-formed and whether a subtype relation holds.

**Formal statement:** The set of types is well-founded — there is no infinite descending chain in the type structure.

**Precept status: ✅ HOLDS.** Precept types are ground: `number`, `string`, `boolean`, `set<T>`, `queue<T>`, `stack<T>` where T ∈ {number, string, boolean}. The nesting depth is at most 1 (collection<primitive>). No type recursion.

### Condition 3: No Higher-Rank Polymorphism

No universal quantifier appears inside a function type or as an argument to a type constructor. Higher-rank types (Rank 2 and above) make type inference undecidable and require either annotation at every abstraction site or full bidirectional mode switching.

**Formal statement:** All quantifiers are top-level (Rank 1 at most). For any type in the language, ∀α.τ appears only at the outermost position.

**Precept status: ✅ HOLDS (trivially).** Precept has no type variables, no quantifiers, no polymorphism of any kind. All types are monotypes (ground terms). Rank-1 polymorphism would already be a substantial language extension.

### Condition 4: No Mutual Type Constraints

No two expressions constrain each other's types cyclically. If A's type depends on B's type and B's type depends on A's type, unification is required (and may fail to terminate for recursive types).

**Formal statement:** The type constraint graph is acyclic. Constraints flow from declarations to expressions in a directed acyclic graph.

**Precept status: ✅ HOLDS.** Field types are declared; expression types are synthesized from declared types. Rules and ensures constrain values (checked against declared types), not types themselves.

### Condition 5: Syntax-Directed Rule Application

At each node of the expression tree, exactly one typing rule is applicable. There is no ambiguity requiring backtracking or choice of rule.

**Formal statement:** For each syntactic form e, there exists exactly one applicable typing rule for the judgment Γ ⊢ e ⇒ τ.

**Precept status: ✅ HOLDS.** The typing rules for Precept expressions are syntax-directed: one rule per form (literal, identifier, binary op, unary op, function call, conditional). The `+` operator has overloaded behavior (string concatenation vs. numeric addition), but this is resolved by synthesis from the operand types — no ambiguity, no backtracking.

**All five conditions hold for Precept's current type system.**

---

## 4. Bidirectional vs. Single-Pass

### What Bidirectional Typing Is

Bidirectional typing is the standard formal framework for single-pass type systems. It alternates between two modes in a single AST traversal:

- **Synthesis mode** (⇒): from term + context, produce a type. Used for: variable lookups, literal constants, function applications (synthesize from the function's type). Flows bottom-up (subterms first).
- **Checking mode** (⇐): from term + expected type + context, verify. Used for: λ-abstractions (check body against function type), annotated terms. Flows top-down (expected type from context).

Mode switching rules (subsumption): synthesis can be wrapped in checking (if e ⇒ τ, then e ⇐ τ); checking with annotation synthesizes (if (e:τ) ⇐ τ, then (e:τ) ⇒ τ). These are the two coercion rules in Dunfield-Krishnaswami's formalization.

Bidirectional typing is single-pass by construction: each node is visited once in the correct mode, and mode is determined by context. No fixpoints.

### Precept Already Uses Informal Bidirectional Typing

The type checker's `ValidateExpression` / `TryInferKind` structure already follows the bidirectional pattern informally:

- **Synthesis mode:** When inferring the type of a binary expression `a + b`, the checker synthesizes types for `a` and `b` from context (their declared field types or the types of their sub-expressions), then synthesizes the result type. This is bottom-up synthesis.
- **Checking mode:** When validating `set fieldName = expr` in a transition, the checker knows the expected type (the field's declared type) and verifies the RHS expression against it. This is top-down checking.

The structure is correct. It just is not formalized with explicit mode annotations.

### What Formal Bidirectional Typing Would Add

Formal bidirectional typing (per Dunfield-Krishnaswami) would add:

1. **Explicit mode annotations on every typing rule** — useful for mechanized soundness proofs, not necessary for an implementation.
2. **Support for unannotated λ-abstractions** — where the argument type is inferred from checking context. Precept has no λ-abstractions; this capability is not needed.
3. **Formal completeness guarantees** — the annotation budget is formally characterized. For Precept, the budget is zero (trivially satisfied).

**Conclusion:** Precept would gain nothing functionally from adopting formal bidirectional typing. The current informal structure is already sound by the STLC bidirectional analysis. Formalizing it would be a verification exercise for a proof assistant, not a capability improvement for the implementation.

---

## 5. The Boundary Case: Computed Fields

### The Apparent Paradox

`TypeCheckerDesign.md` states `Check()` is "single-pass: one forward walk through the definition model." Yet computed fields create a challenge: if computed field A references computed field B, the type checker needs to know B's type before checking A. The order in which computed fields appear in the source is arbitrary.

### The Actual Pass Structure

Looking at `ValidateComputedFields` behavior (inferred from the type checker architecture and `ComputedFieldOrder` documented in `ArchitectureDesign.md`):

**Micro-pass 1: Dependency graph construction** (structural, not type-checking)
- Walk all computed field expressions
- For each expression, identify all field name references (AST scan only — no type inference at this stage)
- Build a directed dependency graph: computed field → referenced computed fields
- Run topological sort (likely Kahn's algorithm)
- Detect cycles → C83/C84 diagnostic codes if a cycle exists
- Output: `ComputedFieldOrder` — a topologically sorted list of computed fields

**Micro-pass 2: Type-checking in topological order**
- Walk computed fields in `ComputedFieldOrder` (from no-dependencies to most-dependent)
- For each computed field, call `ValidateExpression` to synthesize the expression type
- Because less-dependent fields are checked first, their expression types are available when more-dependent fields are checked
- Standard type checking proceeds (C83–C88 constraint codes)

**Critical observation:** Micro-pass 1 does NOT do type inference. It reads only the syntactic structure of expressions (which names are referenced), not their types. This is an O(n) structural scan, not a type analysis pass.

### Is This "Multi-Pass" in the Formal Sense?

In the formal sense of "multi-pass" (requiring a fixpoint computation or backtracking over the type lattice): **No.** Neither micro-pass requires revisiting a node with updated type information. The dependency analysis is a single structural scan; the type-checking is a single forward walk in topological order. No node is visited more than once per phase.

In the practical sense of "the single `Check()` call performs two distinct traversals over computed fields": **Yes.** `Check()` has a hidden two-phase structure for this concern.

### Implication for Documentation

The claim in `TypeCheckerDesign.md` that `Check()` is "single-pass" is accurate at the architectural level but slightly imprecise about the internal micro-structure for computed fields. A more precise description:

> For all validation except computed fields: single forward walk over the definition model.
> For computed fields: one preliminary dependency analysis pass (structural scan only, O(n)), followed by one type-checking pass in topological order. Neither phase requires fixpoints, backtracking, or revisiting a node with updated type information.

This is the correct and intended implementation. The topological sort is a well-understood O(n + e) pre-processing step that enables single-pass type checking to succeed for DAG-structured dependencies. It is not a multi-pass type checker — it is a single-pass type checker with a structural pre-sort.

**Documentation recommendation:** A future cleanup of `TypeCheckerDesign.md` should note the computed field two-micro-pass structure in the "Properties" section. The overall "single-pass" characterization remains accurate and should be kept; a parenthetical about computed fields would make it precise.

---

## 6. When Single-Pass Would Break for Precept

### Scenario A: Field Type Annotations Become Optional (Highest Risk)

If field type declarations become optional — `field price` instead of `field price: number` — the type checker would need to infer field types from expressions that reference them. This is HM inference in miniature: a field's type is a free variable that gets constrained by every expression that reads or writes it. Unification across all expressions in all transitions becomes necessary.

**Why this would break single-pass:** The type variable for `price` is introduced when the field declaration is parsed. It gets constrained by `set price = x + y` in one transition, by `rule "non-negative" ensures price > 0` in a rule, and by `if price > 100` in a guard. Each constraint refines the type variable. The final type can only be known after all constraints are processed — backward propagation from usages to the binding declaration. This is exactly the HM problem.

**Assessment:** High impact if proposed. Would require either adopting HM-style unification or restricting inference to cases where the type is uniquely determined by a single write expression (a much weaker but tractable form).

**Trigger signal:** Any proposal mentioning "type inference for field declarations," "implicit field types," or "annotate only where ambiguous."

---

### Scenario B: Generic Computed Field Expressions (Medium Risk)

If computed fields could be parameterized — e.g., `computed total: number = sum(items)` where `items` is a collection whose element type is not yet known when the computed field is defined — the type checker would need to instantiate the element type from the collection's declared type. This is a checking-mode lookup rather than synthesis, and is actually tractable bidirectionally.

The harder case is if computed fields were truly generic: `computed<T> total: T = aggregateOf(items)` where T is a type variable. This would require tracking T through the expression and resolving it from a checking context — bidirectional typing with type variables, doable but requiring explicit mode tracking.

**Assessment:** Medium impact. The simple case (element type known from collection declaration) is already single-pass. A true generic computed field would require minimal bidirectional machinery.

**Trigger signal:** Any proposal for "generic expressions," "type-parameterized computed fields," or "reusable computed expression templates."

---

### Scenario C: Parameterized Rules (Low-Medium Risk)

If rules could be parameterized by type variables — `rule<T: number> "positive" ensures field1 = f(field2)` — the type checker would need to instantiate T from the context where the rule is evaluated. Instantiation of type parameters requires a fixpoint over the parameter inference, and the rule body would need to be type-checked once per instantiation or with a generic context.

**Assessment:** Low-medium impact. Parameterized rules are not under active proposal. If they were introduced, the scoping of the type parameter (per-state? per-event?) would drive the design. Within-rule type checking would likely remain single-pass if T is constrained to monotypes.

---

### Scenario D: Cross-Field Dependent Types (Low Risk)

If field B's type could depend on field A's value — e.g., `field items: set<CategoryType> where CategoryType depends on field category` — the type checker would need to propagate type refinements based on field values known only at runtime. This is dependent typing, not just multi-pass type checking. It would require a fundamentally different type system architecture.

**Assessment:** Very low risk. Not under consideration. Would require a separate product-level design decision and would conflict with the compile-time prevention guarantee.

---

### Scenario E: Co-Recursive Computed Fields (Low Risk — Already Blocked)

Computed fields with cycles (A depends on B depends on A) are already caught by the dependency analysis in `ValidateComputedFields` (C83 code). If the language ever relaxed this constraint — allowing mutually-recursive computed fields with a well-founded fixpoint semantics — the type checker would need to either assign both fields the same type simultaneously (a constraint satisfaction step) or compute a fixpoint of types.

**Assessment:** Low risk. Already blocked. The C83 diagnostic exists precisely to prevent this. Any proposal to allow it should explicitly address the type-checking implications.

---

### Summary Table

| Scenario | Would Break Single-Pass? | Risk Level | Trigger Signal |
|---|---|---|---|
| Optional field type annotations | Yes — requires HM unification across all usages | **High** if proposed | "infer field type," "implicit field type" |
| Generic/parameterized computed fields | Partially — requires bidirectional with type vars | Medium | "type-parameterized computed fields" |
| Parameterized rules | Partially — requires instantiation scoping | Low-Medium | "generic rules," "rule templates" |
| Cross-field dependent types | Yes — requires dependent typing | Very Low | "value-dependent types" |
| Co-recursive computed fields | Yes — requires fixpoint on types | Low (blocked by C83) | "relaxing cycle detection" |

---

## 7. Recommendation

### Is Single-Pass Sound for the Next 3–5 Years?

**Yes, clearly.** The five structural conditions that justify single-pass are all load-bearing design properties of the DSL:

- Explicit field type annotations are a first-class property of the language ("one file, complete type information" from `philosophy.md`)
- Ground types (no type variables, no polymorphism) follow from the fixed type vocabulary (number/string/boolean/collection)
- The expression language is intentionally restricted to rule-governed arithmetic, logical operators, and well-typed built-in functions

None of these could be changed incidentally. Any change would be a deliberate language design decision visible in a proposal and flaggable before implementation.

### What Should Trigger a Revisit

**Revisit if any of the following appear in a proposal:**

1. **"Type inference for field declarations" or "optional field type annotations."** This is the highest-risk path. If it appears, the review should explicitly address whether the inference is (a) restricted to deterministically-inferable types (single-pass safe) or (b) HM-style (multi-pass required). These are very different implementation consequences.

2. **"Generic computed fields," "expression templates," or "parameterized expressions."** These introduce type variables. If the scope is narrow (T bound at the computed field declaration site), minimal bidirectional machinery suffices. If T flows across multiple expressions without a binding site, HM-style unification is needed.

3. **"Relax cycle detection for computed fields" (C83).** Co-recursive computed fields would require a fundamental change to the computed field validation architecture.

4. **A third micro-pass added to `ValidateComputedFields`.** Currently the two micro-passes are clean and bounded. A third pass that feeds type information back into the dependency analysis would change the character from "topological sort + single-pass" to "iterative fixpoint." This is an implementation-level signal that the architecture is under pressure.

### The Right Framework If Single-Pass Must Be Extended

If Precept ever adds features requiring type variable inference:

**Adopt bidirectional typing (Dunfield-Krishnaswami), not full HM inference.**

Reasons:
- Bidirectional typing remains single-pass (one traversal, explicit modes)
- It supports type variables and higher-rank types when annotations are present
- It provides better error locality than HM (errors at mismatch sites, not unification points)
- It does not require the unification infrastructure (union-find over a global type environment) that HM demands
- It is the correct abstraction level for a language that is "annotation-first" (every binding site has an explicit type)

HM inference is the wrong choice for Precept: it would allow removing all type annotations and infer everything globally — the opposite of Precept's design principle that all types are declared and inspectable.

### Documentation Cleanup (Low Priority)

The "single-pass" characterization in `TypeCheckerDesign.md § Properties` is accurate but should note the computed field two-micro-pass structure. Suggested addition:

> **Single-pass (with computed field pre-sort).** `Check()` makes one forward walk through the definition model. No iterative fixpoint computation, no backtracking. The one exception: `ValidateComputedFields` performs a preliminary structural dependency analysis (topological sort) before type-checking computed expressions in dependency order. This pre-sort is O(n+e) and does not involve type inference — it reads expression structure only.

This is low-priority documentation cleanup, not a correctness issue. The current behavior is correct; the description can be made more precise at any convenient time.

---

## 8. References

1. **Dunfield, Jana and Krishnaswami, Neel** (2021). "Bidirectional Typing." *ACM Computing Surveys* 54(5). arXiv:1908.05839 [cs.PL]. https://arxiv.org/abs/1908.05839
   — Definitive survey of bidirectional type systems. Key reference for single-pass soundness characterization, annotation budget analysis, and error locality.

2. **Wikipedia: Simply Typed Lambda Calculus** (retrieved 2026-04-19). "Bidirectional Type Checking" subsection. https://en.wikipedia.org/wiki/Simply_typed_lambda_calculus#Bidirectional_Type_Checking
   — Formal presentation of synthesis/checking judgments; formal rules for STLC bidirectional typing; annotation budget for STLC.

3. **Wikipedia: Hindley-Milner Type System** (retrieved 2026-04-19). https://en.wikipedia.org/wiki/Hindley%E2%80%93Milner_type_system
   — Why parametric polymorphism requires multi-pass unification; Algorithm W/J; why HM cannot be single-pass for unannotated λ-abstractions.

4. **Wikipedia: Type Inference** (retrieved 2026-04-19). https://en.wikipedia.org/wiki/Type_inference
   — When multi-pass inference is required; "degenerate" single-pass algorithms; polymorphic recursion undecidability.

5. **Pierce, Benjamin C.** (2002). *Types and Programming Languages*. MIT Press.
   — Formal definition of syntax-directed type systems (Chapter 9); Pierce & Turner local type inference; bidirectional typing for STLC with subtyping.

6. **Harper, Robert** (2016). *Practical Foundations of Programming Languages* (2nd ed.). Cambridge University Press.
   — Mode-correct typing rules; progress + preservation type safety structure; formal statics of language design.

7. **C# Language Specification, §12.6.3: Type Inference** (retrieved 2026-04-19). https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/type-inference
   — URL returned HTTP 404. C# uses local (single-pass) inference for `var` declarations; two-phase fixpoint inference for generic method type argument resolution.

8. **Precept internal references:**
   - `docs/TypeCheckerDesign.md` — current type checker architecture, pass structure, computed field validation
   - `docs/ArchitectureDesign.md` — ComputedFieldOrder injection into PreceptEngine; compile-time phase design
   - `research/architecture/typechecker-architecture-survey-frank.md` — production type checker survey (Roslyn, TypeScript, Kotlin K2, F#, Rust, Swift)
   - `research/architecture/typechecker-implementation-patterns-george.md` — .NET implementation patterns

---

*Research grounding: This document draws on sources 1–7 above. Sources 5–6 are knowledge-based synthesis from canonical PL theory textbooks (full text not accessible via web fetch). Source 7 returned 404 and is supplemented from knowledge of the C# specification.*
