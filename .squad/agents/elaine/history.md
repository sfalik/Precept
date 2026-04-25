## Core Context

- Owns UX/design across README form, brand-spec presentation, semantic visual surfaces, and preview/mockup direction.
- Keeps visual decisions aligned with runtime semantics: Emerald/Amber/Rose are semantic runtime signals; Gold is a restrained brand accent.
- Historical summary (pre-2026-04-13): led README layout/hero passes, semantic-visual-system refinements, preview-concept explorations, and verdict-modifier UX analysis.

## Learnings

- README form work is mostly hierarchy, line economy, and plaintext resilience.
- Preview concepts must be validated against realistic sample complexity, not only small demo precepts.
- Verdict modifiers are strongest as subtle authored-intent cues layered beneath runtime outcomes.
- Diagnostic messages must pass the domain-author test, not just the developer test. "Typed constant," "interpolation expression," "member accessor," and "provably unsatisfiable" all fail. The audience thinks in fields, values, states, conditions — not tokens, types, and satisfiability.
- `InvalidCharacter` is the single most structurally broken diagnostic in the lexer: it covers three completely different problems (invalid source char, unrecognized escape, lone `}` in a text value) with one undifferentiated message. Each needs its own code and fix-oriented message.
- The lone `}` case is the highest-probability first-contact failure for domain authors using interpolation in text values. It has zero instructional value in the current message.

## Recent Updates

### 2026-04-11 — Verdict modifiers UX perspective
- Recommended badge-level authored verdict cues, not full-surface fills, to preserve clarity and avoid false confidence.
- Identified state verdicts as novel differentiator territory for Precept tooling and storytelling.

### 2026-04-18 — Proof engine design review (PR #108)
- Reviewed ProofEngineDesign.md and PR body Commit 14 (hover integration) for UX spec verification.
- Verdict: CHANGES_NEEDED — architecture is right, presentation layer needs finishing.
- 3 non-negotiable hover positions are correctly captured and architecturally enforced (no interval notation, no compiler internals, "why" attribution required).
- Key UX issues found:
  - `ToNaturalLanguage()` mapping table incomplete — only 4 shapes specified, need full coverage including `[N,N]`, `(-∞,N]`, `(N,M)`, etc.
  - C94–C98 diagnostic message templates use raw interval notation — violates position #1. Must use the same `ToNaturalLanguage()` formatter.
  - ConstraintViolationDesign.md stale for C92/C93, missing C94–C98 entirely.
  - `ProofAssessment.Evidence` as bare `string?` needs a formatting contract to prevent inconsistent hover attribution.
- Missing specs identified: expression hover triggers, multi-source attribution formatting (cap at 3), partial-proof display guidance, hover for rule/guard declarations themselves.
- What's right: shared assessment model, truth-based C92/C93 split, proven-violation-only C94 policy, silence on Unknown, no path for internal type names to reach hover.
- Full review in `.squad/decisions/inbox/elaine-proof-engine-review.md`.

### 2026-04-24 — docs.next/ full design review (document UX & navigability lens)
- Reviewed all 4 READMEs (top-level, compiler/, language/, runtime/) and 3 pipeline docs (lexer, parser, type-checker) for usability, structural consistency, and information architecture.
- Verdict: **APPROVED** — the navigation layer is well-built and the structural alignment across pipeline docs is strong.
- Key strengths: README tables are consistent, reading orders match real learning paths, cross-references are dense and accurate, all referenced files exist (no dead-end navigation), AI agent navigability is excellent (list_dir + README → right doc in one hop).
- Structural consistency across lexer/parser/type-checker is now very close — same section skeleton, same heading hierarchy, same pattern of Design Principles → Architecture → domain sections → Error Recovery → Consumer Contracts → Deliberate Exclusions → Cross-References → Source Files.
- Nits: type-checker Overview has a mild structural redundancy (public surface described twice), and the top-level README doesn't mention the runtime/ folder's reading-order dependency on compiler docs.
- The folder structure (compiler/, language/, runtime/) is intuitive and maps well to consumer mental models.
- No blockers found. All navigation paths terminate at real files. The type-checker doc is implementable as a standalone blueprint.

### 2026-04-24 — G3 operator diagnostic message evaluation

**Context:** TypeChecker emits `TypeMismatch` when `OperatorTable.ResolveBinary` returns null for incompatible operand types. Frank flagged the message framing as unnatural for binary operator errors.

**Current message (via `TypeMismatch` template):**
- Template: `"Expected a {0} value here, but got '{1}'"`
- For `decimal * number`: `"Expected a decimal value here, but got '* on number'"`
- The "expected/found" frame assumes one side is the ground truth and the other is the violator. That assumption holds for assignment-style errors (rule condition must be boolean) but breaks for binary operators where *neither operand is wrong individually* — the *combination* is wrong.

**Evaluation — two audiences:**

1. **DSL authors in VS Code squiggles:**
   - Current: "Expected a decimal value here, but got '* on number'" — confusing. The author didn't "expect decimal." They wrote `D * N` and don't understand why the pair is rejected. The message privileges the left operand as "expected" arbitrarily.
   - Frank's form: "'decimal' and 'number' are incompatible for '*'" — directly names both operands and the operation. The author knows exactly what's wrong: these two types can't be multiplied.

2. **AI agents via MCP `precept_compile`:**
   - Current: Parseable but semantically misleading. An agent seeing "Expected decimal, got * on number" might try to fix the right operand to be decimal, which doesn't solve the cross-lane problem (decimal × decimal still requires explicit bridging awareness).
   - Frank's form: Unambiguous. An agent can extract left type, right type, and operator from a predictable structure and reason about the fix (bridge function).

**Recommendation: New diagnostic code `OperatorTypeMismatch`.**

Rationale for a new code rather than reusing `TypeMismatch`:
- The failure mode is structurally different. `TypeMismatch` is "this position expects type X, you gave type Y" — a single-target error. Operator type errors are "these two types are incompatible for this operation" — a *relational* error between two operands.
- The `FaultCode.TypeMismatch` → `DiagnosticCode.TypeMismatch` chain in the runtime covers general type mismatches. Operator type mismatches at runtime are the *same* fault (wrong types for an operator), so `FaultCode.TypeMismatch` should map to *both* `DiagnosticCode.TypeMismatch` and `DiagnosticCode.OperatorTypeMismatch` via the `[StaticallyPreventable]` attribute. However, `[StaticallyPreventable]` currently takes a single `DiagnosticCode`. This is a design constraint worth flagging — the attribute may need to accept multiple codes, or `FaultCode.TypeMismatch` may need splitting.
- The message template takes different parameters (left type, right type, operator) vs. (expected type, found type). Sharing a template forces one of the two call sites to abuse the parameter semantics.
- Separating the codes makes MCP `precept_language` enumeration more precise — agents see "operator type mismatch" as a distinct rule, which it is.

**Recommended message template:**

```
"'{0}' and '{1}' are incompatible for '{2}'"
```

Parameters: `{0}` = left type display name, `{1}` = right type display name, `{2}` = operator symbol.

Examples:
- `decimal * number` → `"'decimal' and 'number' are incompatible for '*'"`
- `boolean + boolean` → `"'boolean' and 'boolean' are incompatible for '+'"`
- `string - integer` → `"'string' and 'integer' are incompatible for '-'"`

**Teachable enhancement (future):** For the specific `decimal × number` cross-lane case, the type-checker design doc says the message should suggest bridge functions. The template above is the base; an enhanced version for numeric cross-lane specifically could append: `" — use approximate() to convert decimal to number, or round() to convert number to decimal"`. This could be a second diagnostic code (`NumericLaneCrossing`) or conditional message construction within `OperatorTypeMismatch`. Recommend deferring the teachable suffix to the implementation PR — the base template is the priority.

**Consistency assessment:**
- The `CrossCurrencyArithmetic` diagnostic already uses a relational framing: `"Cannot combine '{0}' ({1}) with '{2}' ({3}) — different currencies"`. This confirms the project already has precedent for "these two things are incompatible" over "expected X, got Y" for binary operation errors.
- The recommended template follows the same pattern but is shorter — appropriate for the more common base-type mismatch case.

**Ripple effects of adding `OperatorTypeMismatch`:**
- `DiagnosticCode` enum: add member
- `Diagnostics.GetMeta`: add switch arm (enforced by exhaustiveness)
- `TypeChecker.CheckBinaryExpression`: change `DiagnosticCode.TypeMismatch` → `DiagnosticCode.OperatorTypeMismatch` with new args
- `FaultCode.TypeMismatch` `[StaticallyPreventable]` linkage: needs design decision (see above)
- Catalog tests: add test for new code
- `diagnostic-system.md`: add to registry listing
- Existing tests: update `Check_BinaryArithmetic_DecimalTimesNumber_EmitsTypeMismatch` and `Check_BinaryArithmetic_BooleanPlusBoolean_EmitsTypeMismatch` to assert on new code

**Verdict:** Recommend the change. The current framing actively misleads both audiences. The fix is clean, follows existing project precedent (`CrossCurrencyArithmetic`), and the ripple is well-scoped.
