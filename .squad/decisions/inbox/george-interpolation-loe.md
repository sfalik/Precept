# George — Interpolation LOE Review

**Date:** 2026-05-11T14:55:38.348-04:00  
**Plan reviewed:** `docs/Working/interpolation-plan.md` (frank-23)  
**Scope note:** Estimates include tests and the small core/tooling walker updates the plan does not list explicitly but the feature needs to work end-to-end.

| Slice | What | New LOC | Modified LOC | Deleted LOC | Total Delta | Complexity |
|-------|------|---------|--------------|-------------|-------------|------------|
| 1 | Parser + AST/catalog + parser tests | 78 | 26 | 14 | 104 | Medium — the parse loop itself is mechanical, but the new expression form ripples into catalog metadata and regression tests. |
| 2 | Type checker + diagnostics + core walkers/tests | 345 | 86 | 38 | 431 | Very High — this is the real cost center: grammar tables, text classification, three diagnostics, typed-node plumbing, and every core expression walker must stay coherent. |
| 3 | Completions (`CompletionHandler.cs`) + LS tests | 118 | 52 | 0 | 170 | High — hole-aware completion has to rediscover slot identity from segmented tokens and stay aligned with Slice 2’s acceptance rules. |
| 4 | Semantic tokens + LS traversal fixes/tests | 32 | 24 | 0 | 56 | Medium — the handler change is small, but the same expression-tree walk is duplicated across several language-server helpers. |
| 5 | Spec/docs sync (no expected MCP protocol work) | 20 | 14 | 0 | 34 | Low — mostly spec and diagnostic-catalog wording once the runtime behavior is settled. |
| **Total** |  | **593** | **202** | **52** | **795** |  |

## Risks / complexity hotspots

1. **Compound temporal scope is larger than the slice text admits.** `docs/Working/interpolation-plan.md:171-173` says `duration`/`period` compounds generalize to N components, but the Slice 2 design at `401-455`/`542` assumes a small finite matcher. `ResolveInterpolatedTypedConstant()` will stay simple only if scope is narrowed to the enumerated 2-component patterns.
2. **Name binding is currently missing from the plan.** `src/Precept/Pipeline/NameBinder.cs:384-387` and `642-649` only recurse into `InterpolatedStringExpression`; once the parser emits `InterpolatedTypedConstantExpression`, hole identifiers will not bind unless binder traversal is updated too.
3. **The new classifier can drift from existing typed-constant validation.** Today `ResolveTypedConstant()` delegates to `TypedConstantValidation.Validate` in `src/Precept/Pipeline/TypeChecker.Expressions.cs:250-256`; the new segment classifier described at `docs/Working/interpolation-plan.md:425-439` re-implements numeric/unit/currency recognition in a second place.
4. **Slice 3 duplicates runtime grammar knowledge inside the language server.** `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs:80-112`, `438-653`, and `728-760` are built around raw typed-constant token text; hole completions will need new segmented-hole detection plus slot matching, and the plan currently has that logic living separately from the checker.
5. **Slice 4 undercounts the traversal ripple.** `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs:644-651` is only one walker; `SemanticExpressionLocator.cs:341-348`, `SlotContext.cs:996-1004`, `TypedConstantCollector.cs:260-268`, and `SyntaxSelectionBuilder.cs:239-247` all special-case interpolated strings and will need parallel `TypedInterpolatedTypedConstant` support to avoid tooling blind spots.

## Overall feasibility verdict

**feasible-with-caveats**

The parser and documentation slices are straightforward, but the plan materially underestimates Slice 2 and slightly underestimates the language-server ripple. If Shane either narrows compound temporal support to the explicitly enumerated forms or approves a shared runtime matcher that Slice 3 can reuse, this is a solid implementation target; without that, the biggest risk is checker/completion drift rather than raw parser difficulty.
