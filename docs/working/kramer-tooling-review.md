# Type Checker Design — Tooling/Language Server Consumer Review

**Reviewer:** Kramer (Tooling Dev)
**Document:** `docs/compiler/type-checker.md` (817 lines)
**Date:** 2026-05-02
**Perspective:** Language server and VS Code extension consumer of `SemanticIndex`

---

## GOOD FOR TOOLING

### 1. Back-pointers on every typed record — go-to-definition solved

Every `TypedField`, `TypedState`, `TypedEvent`, `TypedArg`, `TypedTransitionRow`, `TypedRule`, `TypedEnsure`, etc. carries a `Syntax` back-pointer to its originating AST node. Since AST nodes carry `SourceSpan` (which already includes line/column in LSP-compatible format), go-to-definition is a direct pointer chase — no span-correlation table needed.

### 2. Typed expressions carry `ResultType` — hover is trivial

`TypedExpression` subtypes all carry `ResultType: TypeKind`, and `TypedFieldRef`/`TypedArgRef` carry the resolved name. For hover, I can walk the typed expression tree, find the subexpression whose `Syntax.Span` contains the cursor, and show its `ResultType`. This is exactly what the LS needs — no syntax-only guessing.

### 3. Symbol tables with O(1) lookup — completions are fast

`FieldsByName`, `StatesByName`, `EventsByName` as `FrozenDictionary` secondary indexes give me instant lookup for "does this identifier resolve?" and the ordered arrays (`Fields`, `States`, `Events`) give me natural completion lists in declaration order. This is the right shape for context-aware completions.

### 4. Diagnostics carry SourceSpan — diagnostics pipeline unchanged

The existing `Diagnostic` record struct already carries `SourceSpan` with `StartLine/StartColumn/EndLine/EndColumn`. The `SemanticIndex.Diagnostics` will just flow into the same aggregation path that the LS already uses from `Compilation.Diagnostics`. No new mapping needed.

### 5. Error recovery policy — partial results keep LS functional

The "always produce partial results" policy with `TypedErrorExpression` replacing failed sub-expressions means the LS gets a populated `SemanticIndex` even when the user is mid-edit. This is critical for interactive editing — I can still offer hover, go-to-def, and completions on the parts that did resolve while showing diagnostics on the parts that didn't.

### 6. Array-primary storage preserves declaration order

The design uses `ImmutableArray<TypedField>` primary with derived `FrozenDictionary` secondary. This means completions show fields in source order (user expectation) while lookups remain O(1). Perfect.

### 7. TypedExpression DU subtypes — semantic token classification

`TypedFieldRef`, `TypedArgRef`, `TypedLiteral`, `TypedFunctionCall`, `TypedMemberAccess` etc. give me fine-grained classification for semantic tokens. A `TypedFieldRef` → `variable.field`, a `TypedArgRef` → `parameter`, a `TypedFunctionCall` → `function`. This is much richer than the current token-category-only approach.

### 8. Qualifier and modifier information — rich hover content

`TypedField.Qualifier`, `TypedField.Modifiers`, `TypedField.ImpliedModifiers` give me everything I need for rich hover tooltips ("money(USD), required, writable"). The resolved metadata is all there — no catalog re-lookup needed at hover time.

---

## GAPS

### GAP-1: No position-indexed lookup for "what's at this cursor?" — MEDIUM

**Problem:** The LS constantly needs to answer: "given a source position (line, column), what semantic entity is here?" The `SemanticIndex` is organized by semantic role (fields, states, events, transitions), not by source position. To find what's under the cursor, I'd need to:
1. Walk all typed records
2. Check each `Syntax` back-pointer's span
3. For expressions within those records, recursively walk the typed expression tree

For hover and go-to-definition, this linear scan over all declarations + recursive expression walk is O(n) per keystroke.

**Impact:** With Precept's 64KB file ceiling and flat grammar, this is probably fast enough in practice (dozens of declarations, not thousands). But it's worth noting the design doesn't provide a position-indexed structure.

**Recommendation:** Not a blocker — the LS can build a local position index on demand from the `SemanticIndex` after each compile. Document this as an acceptable LS-side concern, not a `SemanticIndex` shape change.

### GAP-2: No cross-reference index (field usages, event references) — LOW

**Problem:** "Find all references" and "where is this field modified?" require scanning all `TypedTransitionRow` actions, all `TypedRule` conditions, all `TypedEnsure` conditions, etc. The `ConstraintFieldRefs` dependency fact helps for constraints, but there's no general "FieldX is referenced at these locations" index.

**Impact:** The LS can build this at analysis time from the existing data — walk all typed expressions, collect `TypedFieldRef`/`TypedArgRef` instances with their spans. The `SemanticIndex` has enough information; it just doesn't pre-compute this particular view.

**Recommendation:** Accept as LS-side computation. If "find references" becomes latency-sensitive, a `FrozenDictionary<string, ImmutableArray<SourceSpan>> FieldReferences` could be added later. Not a design-time blocker.

### GAP-3: Scope context at arbitrary positions — MEDIUM

**Problem:** For completions, the LS needs to know "what's in scope at position P?" — specifically:
- Which event args are available (inside a transition row)
- Which quantifier bindings are active (inside a `for` predicate)
- What the expected type is (for typed constant completion)

The `CheckContext` captures this during type-checking (via `CurrentEventArgs`, `CurrentFieldIndex`), but it's discarded after the check pass. The `SemanticIndex` output doesn't encode "at position X, event args Y are in scope."

**Impact:** Without scope-at-position data, the LS must re-derive scope from the syntax tree structure: "Am I inside a transition row? If so, which event? What are its args?" This is the same syntax-walking that the anti-mirroring rules discourage for semantic features.

**Recommendation:** Consider adding a lightweight scope map to the `SemanticIndex`:
```csharp
// Optional: maps source ranges to available scopes
ImmutableArray<ScopeRegion> ScopeRegions
// where:
record ScopeRegion(SourceSpan Range, string? EventName, ImmutableArray<TypedArg> AvailableArgs);
```
This would let completions look up scope by position without re-deriving from syntax. However, the LS can also derive scope from the typed transition rows (each `TypedTransitionRow` knows its `EventName` and the event's args are in `EventsByName`). Not a hard blocker.

### GAP-4: Expression-level type information for non-leaf nodes — LOW

**Problem:** For hover on a complex expression like `Amount * Quantity`, I want to show the result type of the full binary expression. The `TypedBinaryOp` carries `ResultType`, so this works. But for hover on just `Amount` within that expression, I need to find the `TypedFieldRef` leaf.

The design handles this correctly via the recursive DU structure — every sub-expression is a `TypedExpression` with its own `ResultType` and `Syntax` span. The LS just needs to find the deepest expression whose span contains the cursor.

**Status:** Not actually a gap — the design handles this. Noting it here to confirm that nested expression hover is supported.

### GAP-5: No "edit declaration" index for stateless precepts — LOW

**Problem:** Stateless precepts use `edit all` or `edit Field1, Field2` declarations. These aren't in the current `SemanticIndex` shape spec. They'd need a typed record (something like `TypedEditDeclaration`) to support hover/completions on edit declaration fields.

**Status:** Likely addressed in the broader design when edit declarations are formalized as a normalized declaration type. Flag for verification.

### GAP-6: TypedExpression `Syntax` back-pointer is typed as `Expression` — MINOR

**Problem:** The `TypedExpression` base carries `Expression Syntax` — the abstract AST expression node. For hover position lookup, I need the `SourceSpan`, which is on `Expression.Span` (or however spans are attached to AST nodes). This should work as long as all `Expression` subtypes carry their span.

**Status:** Verify that all `Expression` AST nodes carry `SourceSpan`. If they do (which they should per the parser design), this is fine.

---

## RECOMMENDATIONS

### R1: Document the LS consumption pattern explicitly

Add a short section to the type-checker doc (or as a cross-reference to the compiler-and-runtime-design.md §15) describing the canonical LS consumption patterns:

```
For hover: Find deepest TypedExpression whose Syntax.Span contains cursor → show ResultType + catalog docs
For go-to-def: Find TypedFieldRef/TypedArgRef at cursor → navigate to declaration's Syntax.Span
For semantic tokens: Walk typed expressions → map DU subtype to token type
For completions: Use SemanticIndex symbol tables + scope derivation from containing TypedTransitionRow
```

This prevents each future LS developer from reinventing the access pattern.

### R2: Confirm TypedExpression provides span coverage for the full expression

The LS needs every `TypedExpression.Syntax` to carry a `SourceSpan` that covers the full syntactic extent of that expression (including parentheses, operator tokens, etc.). If `Expression` nodes in the AST carry only the "interesting" span (e.g., just the operator token), hover will miss sub-expressions.

**Action:** Verify that `Expression.Span` in the parser design covers the full syntactic extent. This is probably already correct, but worth confirming given hover depends on it.

### R3: Add TypedTransitionRow.EventArgs convenience accessor

The `TypedTransitionRow` has `EventName`, and I can look up `EventsByName[row.EventName].Args` to get the args in scope for that row. But since this is the #1 LS scope-derivation pattern, consider adding:

```csharp
// On TypedTransitionRow:
ImmutableArray<TypedArg> ResolvedArgs  // convenience: the event's args pre-resolved
```

This avoids the LS needing to do a `EventsByName` lookup for every transition row during completion scope analysis.

### R4: Consider a TypedEditDeclaration record

For stateless precepts, the `edit all` / `edit Field1, Field2` declarations need a typed representation to support hover (show which fields are editable), completions (suggest field names), and go-to-definition (navigate to the field declaration from the edit reference).

### R5: Incremental compilation — not a design-time concern, but flag for later

The current LS recompiles on every change. The type checker adds a new pipeline stage. With Precept's scale constraints (64KB ceiling, flat grammar, ~100 declarations max), the full pipeline should remain fast enough for interactive editing. The lexer + parser + type checker + graph + proof pipeline running in <50ms on a typical precept file means no incremental strategy is needed now.

**But:** If the type checker's expression resolution proves slower than expected on large files with many computed fields and complex guard expressions, the first optimization should be skipping type-checking for declarations whose syntax hasn't changed (checking the AST node identity via reference equality on the `SyntaxTree.Declarations` array). Flag this as a performance escape hatch, not a design change.

### R6: Preview webview — SemanticIndex gives richer data

The state diagram preview currently works from `SyntaxTree` + `StateGraph`. With the `SemanticIndex`, the preview gains:
- **Resolved guard types** — can show "boolean" badge on guards that type-check correctly, "error" on those that don't
- **Typed action summaries** — "set Amount = EventArg.Price * EventArg.Quantity" with type annotations
- **Constraint anchoring** — `TypedEnsure.AnchorState` / `TypedEnsure.AnchorEvent` lets the preview attach constraint badges to specific states/events in the diagram

No design changes needed — the preview just gets richer data to render.

### R7: Anti-mirroring and the LS — confirm the boundary

The governing doc (`compiler-and-runtime-design.md §6`) explicitly says:
> "Semantic LS features must not walk syntax. Hover, go-to-definition, semantic tokens, and semantic completions must be satisfiable from SemanticIndex bindings plus back-pointers to originating syntax nodes."

This means the LS uses `.Syntax` back-pointers for:
- **Position lookup:** "What span does this declaration cover?" (for go-to-def target)
- **Position matching:** "Does this expression's span contain my cursor?" (for hover/semantic tokens)

Both are pure position operations — the LS never needs to interpret the syntax structure of the back-pointed node. It only needs `SourceSpan` from it. This is consistent with the anti-mirroring intent.

**One exception to confirm:** Completions currently derive scope from syntax context ("am I inside an `in State` block?"). Once the type checker runs, the LS should derive scope from `SemanticIndex` typed records (e.g., the containing `TypedTransitionRow`'s `EventName`) rather than walking the parse tree. This transition needs to be planned as the checker lands.

---

## SUMMARY VERDICT

The `SemanticIndex` design is **well-shaped for tooling consumption.** The combination of typed DU expressions with `ResultType`, back-pointers with `SourceSpan`, ordered symbol tables with frozen lookup indexes, and partial-result error recovery gives the LS everything it needs for hover, go-to-definition, semantic tokens, and diagnostics without any design-level gaps.

The two medium-priority items (position-indexed lookup and scope-at-position) are both derivable from the existing shape at LS analysis time — they don't require `SemanticIndex` changes, just LS-side index construction. The convenience accessor on `TypedTransitionRow` (R3) would save repeated lookups but isn't blocking.

**No design changes required.** The LS can consume this as specified.
