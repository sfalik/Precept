# Keyword Clarity Audit

Research file for a systematic evaluation of every keyword in the Precept DSL against learnability, disambiguity, and the read-aloud test. Grounded in the 11 design principles from `docs/PreceptLanguageDesign.md § Design Philosophy`.

**Status:** Proposed — inventory complete, evaluation not yet started.

## Motivation

Two questions surfaced during backlog grooming (April 2026):

1. **`invariant` vs `assert`** — both sound like "this must be true." The temporal distinction (always-on data truth vs. movement-scoped check) is the defining semantic difference, but the keywords don't teach this distinction on first read.
2. **`when` overloading** — `when` serves as both a declaration guard ("skip this rule if false") and a transition filter ("fall through to the next row if false"). Same keyword, different evaluation semantics.

These are specific instances of a broader question: **does each keyword teach its own meaning?** This audit proposes a systematic evaluation to answer that.

## Scope

Every keyword in the current DSL vocabulary, organized by category:

### Control keywords (6)
| Keyword | Role(s) | Overloaded? | Read-aloud clarity |
|---------|---------|-------------|-------------------|
| `state` | State declaration | No | ✅ Clear |
| `in` | State scope ("while in state X") | No | ✅ Clear |
| `to` | Target state scope ("on entering state X") | No | ✅ Clear |
| `from` | Source state ("leaving state X") | No | ✅ Clear |
| `on` | Event trigger ("when event X fires") | No | ✅ Clear |
| `when` | Declaration guard; transition row filter | **Yes — 2 roles** | ⚠️ See analysis below |

### Declaration keywords (6)
| Keyword | Role(s) | Overloaded? | Read-aloud clarity |
|---------|---------|-------------|-------------------|
| `precept` | Precept header declaration | No | ✅ Clear (domain-specific) |
| `field` | Field declaration | No | ✅ Clear |
| `invariant` | Always-on data constraint | No | ⚠️ See analysis below |
| `event` | Event declaration | No | ✅ Clear |
| `assert` | Movement-scoped constraint (state/event) | No | ⚠️ See analysis below |
| `edit` | Edit block declaration | No | ✅ Clear |

### Action keywords (8)
| Keyword | Role(s) | Overloaded? | Read-aloud clarity |
|---------|---------|-------------|-------------------|
| `set` | Assign a value; also type keyword (`set<T>`) | **Yes — 2 roles** | ⚠️ Action vs. type |
| `add` | Add to collection | No | ✅ Clear |
| `remove` | Remove from collection | No | ✅ Clear |
| `enqueue` | Enqueue to queue | No | ✅ Clear |
| `dequeue` | Dequeue from queue | No | ✅ Clear |
| `push` | Push to stack | No | ✅ Clear |
| `pop` | Pop from stack | No | ✅ Clear |
| `clear` | Clear a collection | No | ✅ Clear |

### Grammar keywords (11)
| Keyword | Role(s) | Overloaded? | Read-aloud clarity |
|---------|---------|-------------|-------------------|
| `as` | Type annotation (`field X as string`) | No | ✅ Natural |
| `nullable` | Nullable modifier | No | ✅ Clear |
| `default` | Default value | No | ✅ Clear |
| `because` | Reason sentinel for invariants/asserts | No | ✅ Excellent — self-documenting |
| `initial` | Initial state marker | No | ✅ Clear |
| `with` | Event argument declaration | No | ✅ Natural |
| `any` | Quantifier ("any of X") | No | ✅ Clear |
| `all` | Quantifier ("all of X") | No | ✅ Clear |
| `of` | Quantifier link ("any of") | No | ✅ Natural |
| `into` | Collection target (`add X into Y`) | No | ✅ Natural |
| `rule` | Named rule declaration | No | ✅ Clear |

### Outcome keywords (3)
| Keyword | Role(s) | Overloaded? | Read-aloud clarity |
|---------|---------|-------------|-------------------|
| `transition` | State transition outcome | No | ✅ Clear |
| `no` | Negation for "no transition" | No | ✅ Clear in context |
| `reject` | Rejection outcome | No | ✅ Clear |

### Type keywords (6)
| Keyword | Role(s) | Overloaded? | Read-aloud clarity |
|---------|---------|-------------|-------------------|
| `string` | String type | No | ✅ Universal |
| `number` | Numeric type | No | ✅ Universal |
| `boolean` | Boolean type | No | ✅ Universal |
| `set` | Set collection type; also action keyword | **Yes — 2 roles** | ⚠️ Disambiguated by position |
| `queue` | Queue collection type | No | ✅ Clear |
| `stack` | Stack collection type | No | ✅ Clear |

### Logical operators (3)
| Keyword | Role(s) | Overloaded? | Read-aloud clarity |
|---------|---------|-------------|-------------------|
| `and` | Logical AND | No | ✅ Natural |
| `or` | Logical OR | No | ✅ Natural |
| `not` | Logical NOT | No | ✅ Natural |

### Domain operators (1)
| Keyword | Role(s) | Overloaded? | Read-aloud clarity |
|---------|---------|-------------|-------------------|
| `contains` | Collection membership test | No | ✅ Natural |

---

## Identified Clarity Issues

### Issue 1: `invariant` / `assert` — data truth vs. movement truth

**Problem:** Both keywords sound like "this must be true." The temporal distinction (always-true data constraint vs. checked-at-a-moment movement constraint) is the critical semantic difference but is not evident from the keywords alone.

**Current usage:**
```
invariant Name != "" because "Name required"              // Always-on
in Approved assert Amount > 0 because "Must be positive"  // Checked on state entry
on Submit assert Fee > 0 because "Fee required"           // Checked on event fire
```

**Contender pairs:**

| Data truth | Movement truth | Pro | Con |
|-----------|---------------|-----|-----|
| `invariant` / `assert` (status quo) | Both CS-correct terms | Precise | Distinction requires explanation |
| `invariant` / `guard` | `guard` implies gatekeeping | `when` already means "guard" in Precept |
| `invariant` / `require` | `require` reads as precondition | `require` doesn't inherently signal movement-scoped |
| `always` / `assert` | Maximally self-documenting temporal split | `always` is an adverb, unusual as a declaration keyword |
| `constraint` / `assert` | `constraint` implies permanent structural rule | `constraint` is generic |

**Evaluation criteria to apply:**
- Read-aloud test with non-programmer audience
- Comparison to DSL precedent (Cedar, Alloy, DMN, Drools)
- Impact on existing samples (25 files), parser, grammar, completions, MCP tools, docs

### Issue 2: `when` — dual role as declaration guard and transition filter

**Problem:** Same keyword, different evaluation semantics.

| Position | Meaning | On false |
|----------|---------|----------|
| `invariant ... when <expr>` | Declaration guard — conditionally activate this rule | Skipped silently (no error) |
| `from X on Y when <expr>` | Transition row filter — match this route | Fall through to next row |

**Current usage:**
```
invariant Amount > 0 when IsActive because "Active items need amount"  // Guard
from Open on Submit when Amount > 100                                  // Filter
    transition to Review
```

**Contender splits:**

| Declaration guard | Transition filter | Precedent |
|-------------------|-------------------|-----------|
| `when` / `when` (status quo) | Positionally disambiguated | Works — but requires learning |
| `when` / `where` | SQL intuition: WHERE = filter rows | Familiar split |
| `if` / `when` | `if` for conditional activation | `if` overloaded in expression positions |
| `when` / `given` | `given` as precondition framing | Less commonly recognized |

### Issue 3: `set` — action keyword and type keyword

**Problem:** `set` is both: `set X = value` (assign action) and `set<string>` (set collection type).

**Mitigation:** Positionally disambiguated — `set` at statement start is an action, `set<T>` after `as` is a type. The parser handles this cleanly. Worth noting but likely low-priority given the disambiguation is robust.

---

## Evaluation Framework

For each identified issue, the audit should evaluate:

1. **Read-aloud test** — Can a business analyst read the keyword in context and understand its role without documentation?
2. **Write-from-intent test** — Given a business rule in English, does the developer reach for the correct keyword on the first try?
3. **Precedent survey** — How do comparable DSLs handle the same semantic distinction?
4. **Overload tax** — How much additional learning does the current keyword impose vs. alternatives?
5. **Migration cost** — How many files, tests, and tools change if the keyword is renamed? (Quantify from the current codebase.)
6. **Principle alignment** — Which design principle(s) does the current vs. proposed keyword best serve?

## Precedent targets

| DSL/Language | Relevant features | Why |
|---|---|---|
| **Cedar** (AWS authorization) | `permit`/`forbid`, `when`/`unless` | Policy DSL with constraint semantics |
| **Alloy** | `fact`, `assert`, `pred` | Formal modeling with invariant vs. assertion distinction |
| **DMN** (Decision Model & Notation) | Decision tables, FEEL expressions | Business-audience DSL with constraint rules |
| **Drools** | `when`/`then`, `rule` | Production-rule system with guard semantics |
| **SQL** | `WHERE`, `HAVING`, `CHECK`, `CONSTRAINT` | Universal reference for filtering vs. constraining |
| **XState** | `guard`, `cond`, `always` | State machine library with guard concepts |
| **OCL** (Object Constraint Language) | `inv`, `pre`, `post` | UML constraint language with lifecycle-scoped constraints |
| **TLA+** | `INVARIANT`, `PROPERTY` | Formal spec language with temporal properties |

## Out of scope

- Operator symbols (`==`, `!=`, `->`, etc.) — these are mathematically standard and not candidates for keyword-ification.
- Proposed future keywords from #86 (modifier taxonomy) — those are additive, not renames.
- Built-in function names — those are API surface, not grammar keywords.

## Dependencies

- Builds on: current DSL vocabulary from `precept_language` MCP tool
- Related: #86 (modifier taxonomy), #31 (logical keyword migration — completed)
- Informs: potential future keyword rename proposals

## Next steps

1. **Precedent survey** — Research how Cedar, Alloy, DMN, Drools, OCL, and XState handle the invariant/assert distinction and guard/filter overloading.
2. **Sample audit** — Count usage of each flagged keyword across all 25 samples. Quantify migration cost.
3. **Read-aloud sessions** — Test current and proposed keywords with an LLM-as-naive-reader to proxy non-programmer comprehension.
4. **Lock or defer** — For each issue, decide: rename (breaking change), alias (compatibility), or accept (status quo with better docs).
