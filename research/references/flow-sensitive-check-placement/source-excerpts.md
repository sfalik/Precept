# Mirrored excerpts — flow-sensitive check placement

Snapshot of load-bearing verbatim excerpts for `../../architecture/compiler/flow-sensitive-check-placement-survey.md`.
Mirrored to defend against URL rot. Fetch dates: 2026-05-29.

---

## Rust — rustc-dev-guide, MIR borrow check

Source: <https://rustc-dev-guide.rust-lang.org/borrow-check.html> (accessed 2026-05-29; Primary — official compiler dev guide)

> "The borrow checker operates on the MIR."

> "We then do a second type check across the MIR" (followed by region inference and subsequent analyses).

> "The MIR is far less complex than the HIR; the radical desugaring helps prevent bugs in the borrow checker." and "using the MIR enables non-lexical lifetimes, which are regions derived from the control-flow graph."

> "Finally, we do a second walk over the MIR, looking at the actions it does and reporting errors. For example, if we see a statement like `*a + 1`, then we would check that the variable `a` is initialized and that it is not mutably borrowed, as either of those would require an error to be reported."

---

## Kotlin — JetBrains/kotlin FIR basics

Source: <https://github.com/JetBrains/kotlin/blob/master/docs/fir/fir-basics.md> (accessed 2026-05-29; Primary — official compiler docs)

FIR phases (verbatim list): RAW_FIR, IMPORTS, COMPILER_REQUIRED_ANNOTATIONS, COMPANION_GENERATION, SUPER_TYPES, SEALED_CLASS_INHERITORS, TYPES, STATUS, EXPECT_ACTUAL_MATCHING, CONTRACTS, IMPLICIT_TYPES_BODY_RESOLVE, CONSTANT_EVALUATION, ANNOTATION_ARGUMENTS, BODY_RESOLVE, CHECKERS, FIR2IR.

> "At this point, all FIR tree is already resolved, and it's time to check it and report diagnostics for the user. Note that it's allowed to report diagnostics only in this phase." (CHECKERS phase)

> "If some diagnostic can be detected only during resolution (e.g, error that type of argument does not match with the expected type of parameter) then information about such errors is saved right inside the FIR tree and converted to proper diagnostic only on the CHECKERS stage."

---

## TypeScript — Handbook, Narrowing

Source: <https://www.typescriptlang.org/docs/handbook/2/narrowing.html> (accessed 2026-05-29; Primary — official language handbook)

> "TypeScript follows possible paths of execution that our programs can take to analyze the most specific possible type of a value at a given position. It looks at these special checks (called type guards) and assignments, and the process of refining types to more specific types than declared is called narrowing."

> "This analysis of code based on reachability is called control flow analysis, and TypeScript uses this flow analysis to narrow types as it encounters type guards and assignments. When a variable is analyzed, control flow can split off and re-merge over and over again, and that variable can be observed to have a different type at each point."

> "As we mentioned earlier, when we assign to any variable, TypeScript looks at the right side of the assignment and narrows the left side appropriately."

---

## Roslyn — Nullable Reference Types

Source: <https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-reference-types.md> (accessed 2026-05-29; Primary — official compiler feature spec)

> "Flow analysis is used to infer the nullability of variables within executable code. The inferred nullability of a variable is independent of the variable's declared nullability."

> "The warning is a W warning when assigning `?` to `!` and the target is a local."

Source: <https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Nullability%20Public%20API%20Design%20Notes.md> (accessed 2026-05-29; Primary — compiler design notes)

> "To fully determine nullability, we must bind the entire method and run nullability analysis up to the point requested by the caller, unlike today where individual statements can get away with being bound by themselves."

Implementation note: the nullable flow analysis is implemented by `NullableWalker` under `src/Compilers/CSharp/Portable/FlowAnalysis/` — a walk over bound nodes, structurally a flow-analysis pass distinct from the binder.

---

## Occurrence typing — Tobin-Hochstadt & Felleisen

Source: Sam Tobin-Hochstadt and Matthias Felleisen, "Logical Types for Untyped Languages," Proceedings of the 15th ACM SIGPLAN International Conference on Functional Programming (ICFP '10), pp. 117–128. DOI: 10.1145/1863543.1863561. Author copy: <https://www2.ccs.neu.edu/racket/pubs/icfp10-thf.pdf> (accessed 2026-05-29; Primary — peer-reviewed paper).

Abstract framing (per ACM DL record, accessed 2026-05-29):

> "occurrence typing" is "a type discipline for exploiting the use of data-type predicates in the test expression of conditionals" — refinement is layered onto the base type system; the type of each variable occurrence is determined by the predicates that flow-dominate it.
