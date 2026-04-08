# Conditional Logic Strategy

Cross-cutting research and decision rationale for Precept's conditional logic surface: conditional expressions (#9), inline guarded fallback (#12), and conditional invariants (#14).

## Background

In early March 2026, Precept migrated to a Superpower-based parser. The old design used nested `DslClause` blocks (if/else/else-if) within transition rows, requiring indentation-sensitive parsing and complex block-end detection. The redesign eliminated nested branching to preserve flat, keyword-anchored statements and deterministic parsing. The replacement: multiple transition rows per (state, event) pair, each with an optional `when` guard, first-match evaluation.

## The Central Boundary

**The teaching model: `when` is a structural guard (conditionally applies declarations), `if...then...else` is a value expression (selects between two values).**

- `when` operates at the *row level*: if the guard is false, the entire row is skipped, unmatchable for that event.
- `if...then...else` operates at the *value level*: selects one of two values based on a boolean condition. Both branches type-check; both are expressions.

This distinction is the foundation for all three conditional logic decisions.

## Decision 1: Conditional Expressions (#9)

**Selected: `if <condition> then <value> else <value>`**

Journey:
- **Ternary `? :`** — rejected. Principle 3 (minimal ceremony, no colons) and Principle 9 (tooling-friendly). Colons require disambiguation from type syntax.
- **`choose...when...otherwise`** — considered. Zero false-affordance pedagogically, but lost the pairing with `if` that makes conditional expressions teachable alongside conditionals in other languages.
- **`if...then...otherwise`** — verbose. "Otherwise" is 9 characters where "else" is 4.
- **`if...then...else`** selected. The false affordance of `if` (its potential to suggest imperative branching) is managed through compiler error messages, not keyword avoidance. "if...then...else is a value expression. For conditional row selection, use `when` on the row header."

Both branches required (total expression); no partial if-then. Type-check both; both must resolve to the same type.

## Decision 2: Inline Guarded Fallback (#12)

**Decision: Closed. Declined.**

The proposal was to allow `else reject "<reason>"` inside a single row body, enabling inline fallback without a second row.

**Why declined:** The semantic dual-meaning of `else`. In value context (inside computed fields or expression assignments), `else` selects a value. In action context (row outcome), `else` would route to rejection. This overloads `else` with two incompatible meanings in adjacent positions, violating Principle 1 (teachability via pairing with `if` — but now paired with both "then" and "reject"?).

The two-row pattern (`from State on Event when Guard -> ... -> outcome` / `from State on Event -> reject ...`) is honest, not duplicative. It's explicit, first-match clear, and doesn't introduce syntactic ambiguity. For inline fallback within a single row body, use `if...then...else` inside the expression (set, computed field, etc.).

## Decision 3: Conditional Invariants (#14)

### The `else true` Dead End

Proposal: `invariant if X then Y else true` — only enforce the invariant when X holds.

Why rejected: Invariants that sometimes don't apply aren't invariants. "Always true, except here" is a contradiction in terms. Every other constraint system (FluentValidation, DMN, JSON Schema post-Draft 7, OCL, Alloy) uses implication (X → Y), guards (when X: assert Y), or separate positive rows — never conditional invariants with false-case fallthrough.

### The `implies` Dead End

Proposal: `invariant X implies Y` — formal logic notation.

Why rejected: Formal logic `→` is powerful for math but the wrong register for business DSLs. The negated case (`not X implies Y` ≡ `X or Y`) creates double negatives and shifts burden to the reader. FluentValidation, Cedar, DMN all avoid formal implication in surface syntax.

### The Negation Problem (Root Cause)

The real issue: constraints with `!IsPremium` define the default as the negation of the exception. Most systems avoid this entirely.

- **FluentValidation**: `When(x => x.IsPremium)` / `Unless(x => x.IsPremium)` — always positive conditions.
- **DMN**: Rows are positive; the decision table structure handles alternatives.
- **JSON Schema**: `if/then/else` added in Draft 7 specifically because negation was poor UX.
- **Cedar**: `when` guards are positively framed.

Root insight: Boolean fields for categorical data are the anti-pattern, not the syntax. A "premium" flag is a category, not a boolean. Issue #25 proposes choice types (`choice of Premium | Standard`) to eliminate this class of negation entirely.

### Resolution: `when` Guards on Invariants (with `when not` for negation)

**Approved syntax:**

```precept
invariant Amount > 0 when IsPremium because "..."
invariant Amount > 100 when not IsPremium because "..."
```

`when` is the structural guard (consistent with transition row guards). Negative conditions use `when not` — there is no separate `unless` keyword. **New keyword: `not`** (replacing `!`). Replaces `!` for fluent readability: `when not IsPremium` reads better than `when !IsPremium`.

**Why not `unless`?** The precedent survey (7-to-3 against) showed most comparable systems avoid a dedicated negation keyword. `unless` breaks down on compound conditions (De Morgan confusion with `unless A and B`), and Precept's one-canonical-form principle means `when not` is the single unambiguous way to express negative guards.

### Modeling Guidance

Boolean fields for categorical distinctions should use choice types (#25). The compiler emits an informational diagnostic: "Boolean field used for categorical distinction; consider a `choice` type to eliminate negation."

## Precedent Survey

| System | Conditional Constraint Pattern | Notes |
|---|---|---|
| FluentValidation | `When(condition)` / `Unless(condition)` | Flagship `When` pattern. `Unless` rarely used in practice. |
| Drools | `when` in rule head | Structural guard. Flat rows. |
| Cedar (AWS) | `when` guards on policy | Positive framing. |
| DMN | Positive rows only | No negation. Alternatives via row selection. |
| JSON Schema | `if/then/else` (Draft 7+) | Added because negation was poor UX. |
| Zod | `.refine().when()` | Guard-based activation. |
| OCL | `implies` operator | Formal register. Not fluent for business DSLs. |
| Alloy | `=>` implication, `not` keyword | Formal logic. `not` keyword preferred over `!`. |
| CSS | Separate positive rules | Designers avoid negation; it's syntactically heavy. |
| SQL CHECK | Independent constraints | No conditional relationship. Each standalone. |

*Full captures in `references/conditional-invariant-survey.md`.*

## Consistency Audit (April 6, 2026)

Full audit of the `when`/`if` split across the existing language surface.

### Current constructs — all consistent

| Construct | Keyword | Role | Consistent? |
|-----------|---------|------|-------------|
| Transition row guards | `when` | Structural guard (skip row if false) | ✓ |
| State entry/exit gates | `to`/`from` + `assert` | Implicit guard (reject transition) | ✓ |
| State-scoped invariants | `in` + `assert` | Implicit guard (scoped to state) | ✓ |
| Event argument validation | `on` + `assert` | Implicit guard (reject if invalid) | ✓ |
| Field editability | `in State edit` | Static declaration | ✓ |
| Field defaults | `default Value` | Static value | ✓ |
| Rules | `rule Expr` | Unconditional data integrity | ✓ |
| Set assignments | `set Field = Expr` | Value expression | ✓ |

**Key finding:** `when` and `assert` are correctly distinguished — `when` *skips* a row (soft gate), `assert` *rejects* the transition (hard gate). Different semantics, appropriate different keywords.

### Cross-system validation

| System | Guard Pattern | Value Pattern | Precept Alignment |
|--------|---------------|---------------|-------------------|
| XState | `guard(context)` on transitions | Conditional actions | ✓ `when` mirrors guards |
| Statecharts (Harel) | `event[guard]` on transitions | In-state activities | ✓ `when` on event + state |
| SCXML | `<transition cond="...">` | `<if>/<elseif>/<else>` executable content | ✓ Multi-row replaces `<if>` |
| DMN | Input entries (guards) | Output expressions (value selection) | ✓ Guards → `when`, outputs → `set` RHS |

### Opportunities identified

1. **Conditional edit eligibility** — `in Draft when Editor != null edit Content`. Consistent with `when` as structural guard on declarations. **Approved for #14 scope.**
2. **Conditional defaults** — `default if IsPremium then 0 else 10`. Low priority, rare use case.
3. **Rules with `when`** — `rule Balance >= 0 when State == "Active"`. Better handled by `in State assert` which already exists.

### Design doc cleanup needed

The design doc (around line 226) contains a `when` vs `if` comparison table showing `if`/`else if`/`else` as if it's a current feature. This is the removed March 2026 construct. Should be moved to a rationale section at implementation time.

## Related Issues

| Issue | Decision | Status |
|---|---|---|
| #9 | `if...then...else` value expression | Open — proposal revised |
| #12 | Declined — semantic dual-meaning of `else` | Closed |
| #14 | `when` guards on invariants and edit declarations | Open — proposal revised, `unless` dropped |
| #25 | Choice type eliminates boolean negation at modeling level | Open |
| #31 | Replace all symbolic logical operators with keywords (`and`, `or`, `not`) | Open — expanded from `not`-only to full logical operator migration |
| #8 | Named rules — future composability for grouped constraints | Open |

## Keyword vs Symbol Decision (April 6, 2026)

The `not` keyword decision expanded into a broader design question: where should Precept draw the line between keywords and symbols across the entire language?

Research across the keyword-symbol spectrum (APL through COBOL), cognitive readability studies, DSL design literature (Fowler), and a full inventory of Precept's 47 keywords / 26 symbols led to a settled framework now captured in `docs/PreceptLanguageDesign.md` § Keyword vs Symbol Design Framework (Locked):

- **Keywords** for structure, domain concepts, and logical operators (`and`, `or`, `not`)
- **Symbols** for math/comparison (`+`, `-`, `==`, `!=`) and the one structural exception `->` (universal state machine notation, defended by Principle #11)

#31 was expanded from `not`-only to cover all three logical operators (`&&` → `and`, `||` → `or`, `!` → `not`). `!=` stays as a symbol — unary negation and binary comparison are different operator families, and every keyword-for-logic system (SQL, Python, Alloy, DMN) retains `!=` without issue.
