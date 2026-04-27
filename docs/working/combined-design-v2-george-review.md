# George review — `combined-design-v2.md`

- **Overall verdict:** Strong merge direction and materially closer to the right architecture, but I would not lock it yet. One more Frank pass should be enough, provided the next revision hardens the semantic/runtime contracts instead of just polishing wording.

- **Strengths to preserve:**
  - The document keeps `CompilationResult` and `Precept` as genuinely different products with different consumers.
  - The `SyntaxTree` vs `TypedModel` split is now stated in the right philosophical terms: authored shape versus resolved meaning.
  - Proof is kept on the analysis side and lowering is kept on the execution side. That is the correct boundary.
  - The doc is honest about current implementation reality instead of pretending stubs are finished systems.
  - Runtime is no longer treated like an appendix; the merge gives lowering/evaluator/create/restore/fire/update real space.

- **Numbered blockers or corrections:**
  1. **`SyntaxTree` vs `TypedModel` is directionally right, but still not enforceable enough.** Section 5.3 says the typed layer must feel like a semantic database, but it does not yet specify the minimum semantic inventory that makes that true. The merged doc needs to say outright that `TypedModel` must carry declaration symbols, reference binding, normalized declarations, typed expressions/actions, and source-origin handles for semantic sites. Without that, anti-mirroring stays a slogan and LS features will still cheat back to syntax.
  2. **The LS consumption contract is still one level too vague for implementation.** Section 6 needs exact feature-to-artifact mapping, not just broad nouns. In particular: keyword/operator token classification comes from `TokenStream` + token metadata; identifier semantic tokens come from `TypedModel`; completions need `TypedModel` for scope plus `SyntaxTree` only for local parse context plus catalogs for candidate inventory; hover needs `TypedModel` plus catalog docs; go-to-definition needs typed symbol/reference data with declaration spans; preview needs lowered `Precept` only when lowering succeeds. `GraphResult` and `ProofModel` should be called out as diagnostic/explanation inputs, not default LS dependencies.
  3. **Runtime still does not have a contract as sharp as the compiler side.** The compiler stages now have a recognizable contract template; the lowered runtime model still reads more like an inventory. Section 5.8 needs to name the executable structures the runtime actually depends on: descriptor tables, slot layout, event-row dispatch, recomputation dependency indexes, access-mode indexes, constraint-plan indexes, and fault-site backstops. If runtime is truly co-equal, lowering cannot stay at the “plans and buckets” level.
  4. **Constraint-plan taxonomy is underspecified in a runtime-dangerous way.** “always / state / event” is not enough. The runtime API docs distinguish different anchor semantics (`in`, `to`, `from`, `on`) and different entry points (`Create`, `Restore`, `Fire`, `Update`, `Inspect*`) select different slices. The merged doc should not flatten that into generic “state-anchor” and “event-anchor” buckets or later runtime docs will drift immediately.
  5. **The doc needs a cleaner statement about what is permanent runtime contract versus provisional string-era surface.** Today the source still exposes string placeholders and outcomes like `UndefinedEvent`; the runtime API docs explicitly mark that as provisional pending descriptor-backed public APIs. The merged doc should preserve that truth instead of accidentally hard-locking string-lookup-era invalid-input behavior as the final runtime contract.

- **Revision directions Frank should apply:**
  - Add a short “minimum required `TypedModel` inventory” subsection under the split/type-checker material.
  - Replace the LS section with a stricter feature matrix that explicitly names `TokenStream`, `SyntaxTree`, `TypedModel`, `GraphResult`, `ProofModel`, catalogs, and `Precept`, and what each is allowed to do.
  - Expand lowering from “inventory” to “executable-model contract,” including operation-facing indexes and plan selection inputs.
  - Rename the constraint-plan discussion so anchor kinds stay explicit (`always`, `in`, `to`, `from`, `on`) instead of being collapsed into vague buckets.
  - Add one paragraph that separates **stable runtime contract** from **current provisional string placeholders** so later runtime docs do not inherit accidental finality.

- **Should one more Frank revision be enough?** Yes. I do not think this needs a fresh architecture round; it needs one disciplined revision that turns the remaining soft spots into explicit contracts.
