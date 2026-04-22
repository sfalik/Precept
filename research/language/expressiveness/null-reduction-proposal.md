# Null-Reduction and Field Access Modes

Canonical proposal covering two related language changes: null vocabulary reduction and per-state field access modes. Both features share a design goal — making the DSL more precise about when fields exist and what operations are valid on them.

## Summary and Motivation

Precept's current null vocabulary is overloaded. `nullable` is the only way to express "this field may not have a value," but nullable means different things in different contexts: a field awaiting its first value, a field intentionally cleared, a field that doesn't apply in the current state. The DSL has no way to distinguish these cases, which forces authors to encode structural absence as null values and then write guards to check for those nulls.

Meanwhile, the DSL lacks vocabulary for per-state field access modes. Every field is globally present and the only access control is `edit` (editable) vs. nothing (readonly). There is no way to declare that a field is structurally absent from a state, or to distinguish between readonly and editable fields in a state-specific way.

These two gaps compound. Authors write `nullable` to handle fields that don't exist yet, then write guards like `when X != null` to check for presence, then use `set X = null` to "remove" a value — all because the language lacks vocabulary for presence (`optional`), presence testing (`is set` / `is not set`), value removal (`clear`), and structural absence (`omit`).

This proposal introduces:

1. **`optional`** — replaces `nullable` as the field modifier for "this field may not have a value"
2. **`is set` / `is not set`** — presence operators for guards and expressions
3. **`clear`** — action keyword to remove a field's value (set it to its type's empty state)
4. **`omit` / `read` / `write`** — per-state field access mode verbs
5. **`write` replaces `edit`** — hard break for consistency with the `read`/`write` pair

### Why one proposal for two features

The null vocabulary and field access modes are coupled. `optional` fields need `is set` / `is not set` for guards. `omit` needs `clear` semantics (value reset on state entry). `write` replaces `edit` because the access mode verb triple must be internally consistent. Shipping one without the other would leave the language in an inconsistent intermediate state.

---

## Part 1: Null Vocabulary Reduction

### 1.1 `optional` replaces `nullable`

**Current syntax:**
```precept
field ClaimantName as string nullable
```

**Proposed syntax:**
```precept
field ClaimantName as string optional
```

`optional` is a direct keyword replacement for `nullable`. Same semantics: the field may hold no value. Same position in the grammar: after the type reference, before other field modifiers.

#### Locked Decision: Hard break — `nullable` removed

- **Why:** `nullable` is implementation jargon from C#/SQL. `optional` communicates intent to domain experts: "this field doesn't always have a value." Every DSL design principle prioritizes readability for non-programmers (Principle #2).
- **Alternatives rejected:**
  - Keep `nullable` as alias alongside `optional` — two keywords for the same concept fragments codebases and confuses AI generation (Principle #12). One keyword, one meaning.
  - Punctuation-based optionality (`string?`) — violates the explicit keyword design goal stated in `PreceptLanguageDesign.md` Goals section ("use `nullable` rather than punctuation-based null markers"). `optional` maintains keyword-based clarity.
- **Precedent:** TypeScript uses `?` (punctuation), but every keyword-dominant DSL (GraphQL `!` for required, Protobuf `optional`, OpenAPI `required: false`) uses a word. Precept sits keyword-dominant on the spectrum (§ Keyword vs Symbol Design Framework). `optional` is the natural keyword for that position.
- **Tradeoff accepted:** Hard break requires updating all 25 sample files and all existing precept definitions. This is a one-time migration cost accepted because the language is pre-1.0 and the keyword change improves every future reading of every precept.

### 1.2 `is set` / `is not set` — Presence operators

**Current pattern (workaround):**
```precept
from UnderReview on Approve when DecisionNote != null -> ...
invariant MarketingOptIn == false or Email != null because "Marketing opt-in requires an email"
```

**Proposed syntax:**
```precept
from UnderReview on Approve when DecisionNote is set -> ...
invariant MarketingOptIn == false or Email is set because "Marketing opt-in requires an email"
```

`is set` and `is not set` are boolean presence operators that test whether an `optional` field currently holds a value. They replace the `!= null` / `== null` comparisons that authors currently write.

#### Semantic rules

1. `X is set` evaluates to `true` if `X` holds a non-default value; `false` otherwise.
2. `X is not set` is the logical negation of `X is set`.
3. Both operators are valid in any boolean expression context: guards (`when`), invariants, asserts, `if...then...else` conditions.
4. Compile-time error if the target field is not `optional` — a non-optional field is always set by definition.
5. `is set` / `is not set` compose with `and`, `or`, `not` like any boolean expression.

#### Locked Decision: `is set` / `is not set` as keyword operators

- **Why:** `!= null` is a comparison against a language-level null literal. `is set` is a semantic test — "does this field have a value?" The semantic test communicates intent; the null comparison communicates implementation. Principle #2 (English-ish): `when Email is set` reads as a business rule; `when Email != null` reads as code.
- **Alternatives rejected:**
  - `has value` / `has no value` — more verbose, `has` is not an existing keyword, `value` is semantically overloaded (every field has a value; the question is whether it's the default/empty value). `is set` is tighter.
  - `exists` / `not exists` — SQL-flavored, implies row-level existence rather than field-level presence. Wrong conceptual model for a field within an entity.
  - Keep `!= null` as the only form — forces domain experts to reason about null as a value, which contradicts the goal of removing null from the DSL vocabulary.
- **Precedent:** SQL `IS NULL` / `IS NOT NULL` (keyword-based presence test on nullable columns). Kotlin `?.let {}` / `?: run {}` (structural presence branching). GraphQL introspection distinguishes `null` (absent) from value. The keyword-based presence test is the proven pattern for declarative languages.
- **Tradeoff accepted:** Two new multi-word operators (`is set`, `is not set`) expand the keyword surface. Accepted because they replace the `null` literal comparison pattern and make the language more self-documenting.

### 1.3 `clear` — Action keyword for value removal

**Current pattern (workaround):**
```precept
from RetentionReview on AcceptOffer -> set CancellationReason = null -> transition Active
```

**Proposed syntax:**
```precept
from RetentionReview on AcceptOffer -> clear CancellationReason -> transition Active
```

`clear` resets an `optional` field to its empty/default state. For scalar optional fields, this means the field becomes "not set." For collections, `clear` already exists and empties the collection.

#### Semantic rules

1. `clear X` in an action chain sets field X to its declared default (or null for optional fields with no explicit default).
2. Compile-time error if X is not `optional` and has no declared default — clearing a non-optional field with no default would leave it in an undefined state.
3. `clear` is valid in transition action chains (`-> clear X ->`) and state entry/exit actions.
4. `clear` on collections retains existing semantics (empty the collection).

#### Locked Decision: `clear` as a dedicated action keyword

- **Why:** `set X = null` treats null as a value in the expression language. `clear X` treats removal as an action — parallel to `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`. The action vocabulary is verbs; `clear` is a verb. Assigning null is a value operation that keeps null in the expression language; clearing is a mutation that removes it. Principle #13 (keywords for domain): clearing a field is a domain action, not a mathematical operation.
- **Alternatives rejected:**
  - `unset X` — imperative/shell connotation (`unset` in bash). `clear` is more neutral and already exists for collections.
  - `reset X` — implies returning to a prior state, not removing a value. A field that was never set has nothing to "reset" to.
  - `remove X` — already used for collection element removal (`remove MissingDocuments DocName`). Overloading `remove` for scalar fields creates ambiguity.
  - Keep `set X = null` — keeps `null` as a value in the expression language, which this proposal eliminates.
- **Precedent:** `clear` is already a Precept keyword for collections. Extending it to scalar optional fields is a natural generalization. Redis `DEL`, SQL `SET col = NULL` (but Precept avoids null in expressions), Salesforce field clearing via API.
- **Tradeoff accepted:** `clear` on an optional field and `clear` on a collection are semantically different (field → default vs. collection → empty). Accepted because the verb is the same English concept ("remove the contents") and the operand type disambiguates.

### 1.4 `null` removed from expression language

#### Locked Decision: No `null` literal, no `none` keyword

- **Why:** `null` as a value creates three problems: (1) authors use `null` in comparisons when they mean "not set" — semantic mismatch, (2) `null` in `set` when they mean "clear" — action/value confusion, (3) null propagation in arithmetic is a common source of bugs in every language that has it. Removing `null` from the expression language forces authors to use the semantically correct constructs: `is set` for testing, `clear` for removal, `optional` for declaration. The DSL becomes a closed vocabulary for field presence.
- **Alternatives rejected:**
  - Keep `null` alongside `is set`/`clear` — two ways to do everything, inconsistent codebases, AI generation confusion.
  - Replace `null` with `none` — same problem with a different spelling. The issue isn't the keyword; it's having a null-like value in expressions at all.
  - `null` only in `default null` position — still keeps null in the grammar for one purpose. Cleaner to use `optional` (implies default is empty) and `default` for explicit non-null defaults.
- **Precedent:** Rust has no null (uses `Option<T>`). Swift has `nil` but wraps it in `Optional`. Kotlin's null safety makes null a type-level concern, not a value-level one. The trend in modern language design is to push null out of the value space and into the type/declaration space. Precept follows this trend.
- **Tradeoff accepted:** Existing precepts using `nullable default null` need migration. `default null` becomes unnecessary — `optional` without a `default` clause implies the field starts empty. Migration is mechanical and one-time.

---

## Part 2: Per-State Field Access Modes

### 2.1 Verb triple: `omit` / `read` / `write`

Three verb keywords declare the access mode for a field in a specific state:

| Verb | Meaning | Update API | Fire pipeline (`set`) |
|------|---------|------------|----------------------|
| `omit` | Field structurally absent from the state's data shape | Not accessible | Blocked (compile error) |
| `read` | Field present and readonly | Read only | Allowed |
| `write` | Field present and editable | Read + write | Allowed |

**Syntax:**
```
in <StateList> <VerbGroup>+
VerbGroup := (omit | read | write) <FieldList>
```

Multiple verb groups chain on a single `in <State>` statement:

```precept
in New omit AssignedAgent, ResolutionNote write Priority
```

This declares: in New, AssignedAgent and ResolutionNote are structurally absent; Priority is editable.

#### Guarded write

```precept
in UnderReview when not FraudFlag write AdjusterName
```

Guards are valid only on `write` — conditionally granting editability. When the guard fails, the field falls back to D3 baseline (`read`).

#### Locked Decision: `omit`/`read`/`write` verb triple

- **Why:** Three explicit modes cover the full access spectrum (absent/readonly/editable) without implicit defaults that require documentation to understand. All three are verbs — consistent with the existing keyword grammar. The `read`/`write` pair is the universal data-access pair. `omit` communicates structural absence ("this field is not part of this state's shape"), which is more accurate than `hide` (implies the field exists but isn't shown) or `absent` (adjective, not a verb).
- **Alternatives rejected:**
  - `define X absent/readonly/editable` — 4 new keywords, adjective category expansion unprecedented in the language, named-predicate collision with issue #8's `define`.
  - `view`/`edit` twin verbs (implicit absent) — Shane wanted all 3 modes explicit. Also, Shane rejected `view` ("that's a UI context").
  - `omit`/`view`/`edit` — Shane chose `write` over `edit` for consistency with `read` (read/write pair).
  - Verbless adjective modes (`readonly X`, `editable X`, `absent X`) — adjective-as-verb unprecedented in the language. No DSL precedent.
  - `field` reuse (`in S field X`) — Plaid-inspired but creates semantic overload between field declaration and mode declaration contexts.
  - `present`/`edit` — `present` doesn't communicate readonly mode.
  - `allow read`/`allow edit` — permission-framing misfit; Precept's modes are structural declarations, not authorization responses.
  - `hide`/`view`/`edit` — `hide` implies the field exists but is hidden; `omit` is structurally accurate.
- **Precedent:** 30-system survey across PLT/DSL, workflow, and platform/CMS systems confirmed: verb-as-mode is universal in successful systems (Salesforce View/Edit, ServiceNow Read Only, Dynamics 365 Show/Hide/Enable/Disable, OpenAPI readOnly/writeOnly). No successful system uses a generic verb + trailing mode qualifier. Plaid (typestate-oriented per-state field sets) and Rust typestate pattern (per-state-type structs) are the closest structural precedents. TypeScript discriminated unions are the closest mainstream analogy.
- **Tradeoff accepted:** Three new keywords (`omit`, `read`, `write`) is the highest keyword cost among the alternatives. Accepted because each keyword names a distinct, self-documenting mode, and the 30-system precedent survey confirms verb-as-mode is the pattern that works.

### 2.2 D3: Per-pair baseline

**D3 (refined):** For any (field, state) pair without an explicit access mode declaration, the mode is `read` (present + readonly via Update API).

D3 operates at the **(field, state) pair** level, not the field level. A declaration for one pair does not affect other pairs involving the same field. D3 never "turns off."

**What this means in practice:**

```precept
# Only one declaration needed — D3 handles the other 6 states
in Draft omit CandidateName, RoleName, RecruiterName, FeedbackCount, OfferAmount, FinalNote
```

CandidateName is `omit` in Draft and `read` (D3) in Screening, InterviewLoop, Decision, OfferExtended, Hired, and Rejected. No further declarations needed.

#### Locked Decision: Per-pair D3, not per-field lifecycle scoping

- **Why:** Per-field lifecycle scoping (Model A) forces complete coverage — once any state declares a mode for field X, every state must declare X's mode. In a 7-state hiring pipeline with 6 fields omitted in Draft, Model A requires 11 declarations. Per-pair D3 (Model B) requires 1. The ceremony explosion under Model A is exactly the "massive repetition" pain that Precept's minimal-ceremony principle (Principle #3) exists to prevent.
- **Alternatives rejected:**
  - **Model A (field-level D3 cutoff):** Once any state declares a mode for field X, D3 stops for X entirely. Every state must declare X's mode. Rejected: 11 lines vs 1 line for the hiring pipeline. Set arithmetic for "what exists here?" is cognitively harder than set union. Introduces a new "lifecycle-scoped" concept that doesn't exist in the current language.
  - **Root-level baseline + state overrides:** `read CandidateName` at root level, then `in Draft omit CandidateName` per state. Rejected: redundant with D3. Two ways to express the same semantics, violating the single-expression principle. Creates a question about whether root-level and D3 interact differently.
  - **Separate presence + editability declarations:** Two parallel declaration systems for presence (`visible in`/`omit in`) and editability (`write`/`edit`). Rejected: the three-verb model unifies presence and editability into one declaration surface with no expressiveness loss.
- **Precedent:** No surveyed system has Precept's exact three-mode model. Closest analogs: TypeScript discriminated unions (per-state field presence via type narrowing — massive adoption validates developer comprehension), Plaid (typestate-oriented per-state field sets — proves the concept is implementable and type-checkable), Rust typestate pattern (per-state-type structs — production-proven in mainstream ecosystem). All use implicit defaults for undeclared fields rather than requiring complete specification.
- **Tradeoff accepted:** D3 means a field's mode may not be explicitly written anywhere — you "know" it's `read` because there's no declaration. This is a slight locality-of-reference tension (Principle #4). Mitigated by tooling: the language server shows effective mode on hover; the inspector surfaces the full access mode matrix.

### 2.3 `write` replaces `edit`

`write` replaces `edit` everywhere in the language. This is a hard break.

**Current syntax:**
```precept
in UnderReview edit FraudFlag
in UnderReview when not FraudFlag edit AdjusterName
edit all
```

**Proposed syntax:**
```precept
in UnderReview write FraudFlag
in UnderReview when not FraudFlag write AdjusterName
write all
```

#### Locked Decision: `write` over `edit`

- **Why:** The access mode verb triple must be internally consistent. `read`/`edit` is an asymmetric pair — `read` names the data operation, `edit` names the user action. `read`/`write` is the universal data-access pair used across every layer of computing (file systems, databases, APIs, memory). The consistency aids AI discoverability (Principle #12) and reduces the learning curve for developers encountering the DSL.
- **Alternatives rejected:**
  - Keep `edit` — creates `omit`/`read`/`edit` where two verbs name data operations and one names a user action. The asymmetry is a teaching burden.
  - `omit`/`view`/`edit` — same `view` rejection from Shane.
  - `omit`/`view`/`write` — same `view` rejection.
- **Precedent:** POSIX `r`/`w`/`x`. SQL `SELECT`/`INSERT`/`UPDATE`/`DELETE`. REST `GET`/`PUT`/`POST`. HTTP `read`/`write` semantics. OpenAPI `readOnly`/`writeOnly`. Every data-access model uses read/write as the fundamental pair.
- **Tradeoff accepted:** Hard break requires updating all sample files, all documentation, and all existing precepts. `edit` is used in 17 sample files. One-time cost, pre-1.0.

### 2.4 `read` and `omit` are stateful-only verbs

`read` and `omit` are valid only after `in <State>`. They have no meaning at root level because:

- `read` at root level is redundant with D3 — every field is already `read` by default.
- `omit` at root level is incoherent — a globally omitted field is a field you didn't declare.

Root-level `write` remains valid for stateless precepts (`write all` replaces `edit all`).

### 2.5 Chained verb groups

Multiple verb groups can appear on a single `in <State>` line:

```precept
in New omit AssignedAgent, ResolutionNote write Priority
```

This is syntactic sugar equivalent to:
```precept
in New omit AssignedAgent, ResolutionNote
in New write Priority
```

The verb keywords (`omit`, `read`, `write`) are unambiguous delimiters. LL(1) parsing — after reading a field list, the next token is either another verb keyword (new group), `in`/`from`/`to`/etc. (new statement), or EOF.

---

## Composition Rules

The seven composition rules formalize how access mode declarations interact.

### Rule 1: D3 is the universal per-pair baseline

For any (field, state) pair without an explicit access mode declaration, the mode is `read`.

### Rule 2: Explicit declarations override D3 for the specific pair only

| Declaration | Effect |
|---|---|
| `in S omit F` | F is `omit` in S (structurally absent, value cleared on entry) |
| `in S read F` | F is `read` in S (same as D3 — explicit for documentation) |
| `in S write F` | F is `write` in S (present + editable via Update) |
| `in S when G write F` | F is `write` in S when G passes; `read` (D3) when G fails |

### Rule 3: Guarded `write` is the only guarded access mode

- `in S when G write F` — conditionally editable ✓
- `in S when G read F` — semantically meaningless ("conditionally readable" is not a useful mode) ✗
- `in S when G omit F` — semantically incoherent ("conditionally absent" creates dynamic data shapes that break structural typing) ✗

### Rule 4: `omit` clears on state entry

When an entity transitions into state S, every field `omit`ted in S has its value reset to the field's declared default. This applies to:
- Initial creation (initial state)
- Normal transitions
- Cycle re-entry (e.g., Reopen → New)
- Does NOT apply to `no transition` (the entity doesn't "enter" its current state)

### Rule 5: `set` validation against target state

Any `set` targeting a field `omit`ted in the effective target state is a compile-time error.

| Row type | Effective target state |
|---|---|
| `-> transition X` | X |
| `-> no transition` | Current (source) state |

### Rule 6: Contradiction detection

Same (field, state) pair with conflicting modes = compile error:
- `in S omit F` + `in S read F` → error (absent AND present)
- `in S omit F` + `in S write F` → error (absent AND writable)
- `in S read F` + `in S write F` → error (unconditional readonly AND unconditional writable)

Note: `in S read F` + `in S when G write F` is NOT a contradiction — guarded `write` conditionally upgrades from the `read` base.

### Rule 7: Root-level `write` for stateless precepts only

`write all`, `write Field1, Field2`, `write Field1 when Guard` are valid only in stateless precepts. Root-level `read` and `omit` are not valid syntax.

---

## Before/After Examples

### Example 1: `optional` replaces `nullable`

**Before:**
```precept
field ClaimantName as string nullable
field DecisionNote as string nullable default null
```

**After:**
```precept
field ClaimantName as string optional
field DecisionNote as string optional
```

`optional` without a `default` clause implies the field starts empty. `default null` is no longer needed or valid.

### Example 2: `is set` / `is not set` in guards

**Before:**
```precept
in Approved assert DecisionNote != null when FraudFlag because "Fraud-flagged approvals require a decision note"
from UnderReview on Approve when DecisionNote != null and CreditScore >= 680 -> ...
```

**After:**
```precept
in Approved assert DecisionNote is set when FraudFlag because "Fraud-flagged approvals require a decision note"
from UnderReview on Approve when DecisionNote is set and CreditScore >= 680 -> ...
```

### Example 3: `clear` action in a transition

**Before:**
```precept
from RetentionReview on AcceptOffer
  -> set SaveOfferAccepted = true
  -> set CancellationReason = null
  -> transition Active
```

**After:**
```precept
from RetentionReview on AcceptOffer
  -> set SaveOfferAccepted = true
  -> clear CancellationReason
  -> transition Active
```

### Example 4: Helpdesk ticket with `omit`/`read`/`write` (cycle scenario)

The helpdesk ticket has a Reopen cycle (Resolved → New). `omit` controls which fields are cleared on re-entry; D3 preserves the rest.

```precept
precept ItHelpdeskTicket

field TicketTitle as string optional
field Severity as number default 3
field AssignedAgent as string optional
field ResolutionNote as string optional
field ReopenCount as integer default 0
field Priority as choice("Low","Medium","High","Critical") default "Low"

state New initial
state Assigned
state WaitingOnCustomer
state Resolved
state Closed

# Access modes — only exceptions to D3 need declaration
in New omit AssignedAgent, ResolutionNote write Priority

# After Reopen → New:
#   AssignedAgent → cleared (omit)
#   ResolutionNote → cleared (omit)
#   TicketTitle → preserved (D3 read)
#   Severity → preserved (D3 read)
#   ReopenCount → preserved (D3 read), incremented by Reopen event
#   Priority → preserved and editable (write)
```

**Effective access mode matrix:**

| Field | New | Assigned | WaitingOnCustomer | Resolved | Closed |
|-------|-----|----------|-------------------|----------|--------|
| TicketTitle | `read` (D3) | `read` (D3) | `read` (D3) | `read` (D3) | `read` (D3) |
| Severity | `read` (D3) | `read` (D3) | `read` (D3) | `read` (D3) | `read` (D3) |
| AssignedAgent | **`omit`** | `read` (D3) | `read` (D3) | `read` (D3) | `read` (D3) |
| ResolutionNote | **`omit`** | `read` (D3) | `read` (D3) | `read` (D3) | `read` (D3) |
| ReopenCount | `read` (D3) | `read` (D3) | `read` (D3) | `read` (D3) | `read` (D3) |
| Priority | **`write`** | `read` (D3) | `read` (D3) | `read` (D3) | `read` (D3) |

Two declarations. D3 fills the remaining 28 cells.

### Example 5: Insurance claim with guarded `write`

```precept
precept InsuranceClaim

field ClaimantName as string optional
field ClaimAmount as decimal default 0 maxplaces 2
field AdjusterName as string optional
field DecisionNote as string optional
field FraudFlag as boolean default false

state Draft initial
state Submitted
state UnderReview
state Approved
state Denied
state Paid

# In Draft, claim data hasn't been submitted yet
in Draft omit ClaimantName, ClaimAmount, AdjusterName, DecisionNote

# Unconditional write for FraudFlag; guarded write for AdjusterName
in UnderReview write FraudFlag
in UnderReview when not FraudFlag write AdjusterName
```

When `FraudFlag` is true, AdjusterName is `read` (D3 baseline — guard fails, falls back). When `FraudFlag` is false, AdjusterName is `write` (guard passes). This is additive composition — guarded `write` conditionally upgrades from the D3 baseline without conflict.

### Example 6: Hiring pipeline — D3 efficiency (1 line vs 11)

```precept
precept HiringPipeline

field CandidateName as string optional
field RoleName as string optional
field RecruiterName as string optional
field FeedbackCount as integer default 0 nonnegative
field OfferAmount as number default 0 nonnegative
field FinalNote as string optional maxlength 500

state Draft initial
state Screening
state InterviewLoop
state Decision
state OfferExtended
state Hired
state Rejected

# ONE declaration — D3 handles the other 6 states for all 6 fields
in Draft omit CandidateName, RoleName, RecruiterName, FeedbackCount, OfferAmount, FinalNote
```

Under per-field lifecycle scoping (Model A — rejected), the same semantics would require:

```precept
# Model A: 11 lines for the same result
in Draft omit CandidateName, RoleName, RecruiterName, FeedbackCount, OfferAmount, FinalNote
in Screening read CandidateName, RoleName, RecruiterName
in Screening omit FeedbackCount, OfferAmount, FinalNote
in InterviewLoop read CandidateName, RoleName, RecruiterName
in InterviewLoop omit OfferAmount, FinalNote
in Decision read CandidateName, RoleName, RecruiterName, FeedbackCount
in Decision omit OfferAmount, FinalNote
in OfferExtended read CandidateName, RoleName, RecruiterName, FeedbackCount, OfferAmount
in OfferExtended omit FinalNote
in Hired read CandidateName, RoleName, RecruiterName, FeedbackCount, OfferAmount
in Rejected read CandidateName, RoleName, RecruiterName, FinalNote
```

D3-always (per-pair) eliminates the deep-linear repetition problem entirely.

### Example 7: Stateless precept with `write all`

```precept
precept CustomerProfile

field Name as string optional
field Email as string optional
field Phone as string optional
field PreferredContactMethod as choice("email","phone","sms") default "email"
field MarketingOptIn as boolean default false

invariant MarketingOptIn == false or Email is set because "Marketing opt-in requires an email address"

# All fields editable — write replaces edit
write all
```

No `read` or `omit` at root level — they have no meaning without states. `write all` is the sole root-level access mode declaration.

---

## Teachable Error Messages

| Code | Condition | Message |
|------|-----------|---------|
| C90 | `set` targets a field `omit`ted in the effective target state | `"Field '{field}' is omit in state '{target}'; set has no effect because the value is cleared on entry"` |
| C91 | Contradictory access mode declarations for same (field, state) pair | `"Field '{field}' has conflicting access modes in state '{state}': '{mode1}' and '{mode2}'"` |
| C92 | `is set` / `is not set` applied to non-optional field | `"Field '{field}' is not optional — it is always set"` |
| C93 | `clear` applied to non-optional field without default | `"Cannot clear field '{field}' — it is not optional and has no default value"` |
| C94 | `read` or `omit` used at root level (outside `in <State>`) | `"'{verb}' is only valid after 'in <State>' — use 'write' for stateless precepts"` |
| C95 | `nullable` keyword used (removed) | `"'nullable' has been replaced by 'optional'"` |
| C96 | `null` literal used in expression | `"'null' is not valid in expressions — use 'is set' / 'is not set' for presence checks, 'clear' to remove a value"` |
| C97 | `edit` keyword used (removed) | `"'edit' has been replaced by 'write'"` |
| C98 | Guarded `read` or `omit` attempted | `"Guards are only valid on 'write' declarations — '{verb}' cannot be conditional"` |
| C99 | Root-level `write` in a stateful precept | `"Root-level 'write' is only valid in stateless precepts — use 'in <State> write' instead"` |

All error messages follow the Precept diagnostic convention: state the problem, state the reason, and suggest the fix.

---

## Semantic Rules

### Null vocabulary

1. `optional` replaces `nullable` in all field and event-arg declarations.
2. `optional` without a `default` clause implies the field starts empty (no value).
3. `optional` with a `default` clause starts the field with the specified value; the field can be cleared later.
4. `is set` / `is not set` are boolean operators valid in any expression context.
5. `is set` / `is not set` are compile-time restricted to `optional` fields.
6. `null` is removed from the expression language. No null literal, no null comparisons.
7. `clear` resets an `optional` field to empty (no value) or a non-optional field to its declared default.
8. `clear` on a collection empties it (existing semantics preserved).

### Field access modes

9. Every (field, state) pair has an effective access mode: `omit`, `read`, or `write`.
10. Undeclared (field, state) pairs default to `read` (D3).
11. `omit` means the field is structurally absent — value cleared to default on state entry.
12. `read` means the field is present and readonly via Update API.
13. `write` means the field is present and editable via Update API.
14. `set` in transitions is blocked only by `omit` (compile error). `read` and `write` do not restrict `set`.
15. Guarded `write` conditionally upgrades from D3 baseline. Guard fails → `read`.
16. `read` and `omit` are valid only in `in <State>` declarations.
17. Root-level `write` is valid only in stateless precepts.
18. `omit` clearing applies to initial creation, normal transitions, and cycle re-entry. Does not apply to `no transition`.

---

## Tooling Surface

### Language server

- **Completions:** `optional` offered after type reference (replaces `nullable`). `is set` / `is not set` offered in guard/expression contexts for optional fields. `clear` offered in action chains for optional fields and collections. `omit`/`read`/`write` offered after `in <State>` in declaration context.
- **Hover:** Shows effective access mode for any field in any state (computed from declarations + D3). Shows `optional` status on field hover.
- **Diagnostics:** All new diagnostic codes (C90–C99) surfaced in the Problems panel. Migration diagnostics (C95–C97) guide authors from old to new syntax.
- **Semantic tokens:** `optional` as type modifier. `is set` / `is not set` as operators. `clear` as action keyword. `omit`/`read`/`write` as action keywords.

### TextMate grammar

- `optional` added to field modifier patterns.
- `is set` / `is not set` added to operator patterns (multi-word — `is` + `set`/`not set`).
- `clear` added to action keyword alternation.
- `omit`, `read`, `write` added to action keyword alternation.
- `nullable`, `edit` removed from keyword patterns (replaced by migration diagnostics).

### MCP tools

- **`precept_language`:** Updated keyword lists — new keywords added, removed keywords noted.
- **`precept_compile`:** Field DTOs include `isOptional: boolean` (replaces `isNullable`). Access mode declarations surfaced in compilation output.
- **`precept_inspect`:** Returns `accessModes` map per state — `{ "FieldName": "read" | "write" | "omit" }`. Shows effective mode including D3 defaults.
- **`precept_update`:** Respects `write`-only fields. Rejects updates to `read` or `omit` fields.
- **`precept_fire`:** Validates `set` targets against effective target state access modes. Returns `clear` results.

### Preview / diagram

- State diagram nodes show per-state field shapes (which fields exist in each state).
- `omit`ted fields visually absent from state nodes.
- `write` fields distinguished from `read` fields (e.g., pencil icon or editable indicator).

---

## Dependencies and Related Issues

| Issue | Relationship |
|-------|-------------|
| #14 — `when` guards on declarations | Prerequisite for guarded `write`. Must ship first or simultaneously. |
| #9 — `if...then...else` expressions | `is set` / `is not set` should work in `if` conditions. Parallel or after. |
| #17 — Computed fields | Computed fields interact with access modes: a computed field is inherently `read` (never `write`). `omit` on a computed field means it's not computed in that state. |
| #8 — Named predicates | No interaction — named predicates are boolean expressions, not field modes. |
| #16 — Built-in functions | No direct interaction. `clear` is an action keyword, not a function. |
| #65 — Event action hooks | `clear` may appear in event hooks. Access modes apply to hook targets same as transitions. |

---

## Explicit Exclusions / Out of Scope

1. **No `none` keyword.** Some languages use `none` as a null-like value. Precept eliminates the concept entirely — `optional` + `is set` + `clear` replace every use case without a null-like value in the expression language.
2. **No backward compatibility shim.** `nullable` → `optional` and `edit` → `write` are hard breaks. No deprecation period, no dual-keyword support. Pre-1.0 language.
3. **No `null` in `default` clauses.** `optional` without `default` is the idiomatic form for "starts empty." `default null` is removed.
4. **No conditional `read` or `omit`.** Guards apply to `write` only. Presence and absence are static, structural properties.
5. **No root-level `read` or `omit`.** D3 is the root-level baseline. Explicit root-level `read` is redundant. Root-level `omit` is incoherent.
6. **No `omit` on `no transition` clearing.** `no transition` does not trigger `omit` clearing — the entity doesn't re-enter its current state.
7. **No access mode inheritance across states.** Each (field, state) pair is independent. No cascade, no override chains.
8. **No `visible` keyword.** Earlier proposal iterations used `visible in`/`visible after` for field presence. Eliminated — the verb triple (`omit`/`read`/`write`) unifies presence and editability.

---

## Implementation Scope

### T-shirt sizes by layer

| Layer | Size | Notes |
|-------|------|-------|
| Parser | **L** | New keywords (`optional`, `is set`, `is not set`, `clear`, `omit`, `read`, `write`). Remove `nullable`, `null` literal, `edit`. New grammar productions for access mode declarations with chained verb groups. |
| Type checker | **L** | `is set`/`is not set` restricted to optional fields. `clear` target validation. Access mode D3 resolution. `set`-into-`omit` validation. Contradiction detection. Cross-surface validation (root-level `write` only stateless). |
| Expression evaluator | **M** | `is set`/`is not set` evaluation. `clear` action execution. `null` literal removal. |
| Runtime engine | **M** | `omit` clearing on state entry. Access mode enforcement in Update API. `write` permission checking. |
| Language server | **L** | Completions for all new keywords in correct contexts. Hover showing effective access modes. All new diagnostics. Migration diagnostics for removed keywords. |
| TextMate grammar | **M** | New keywords in correct patterns. Removed keywords from patterns. Multi-word operator patterns for `is set`/`is not set`. |
| MCP tools | **M** | DTO updates (isOptional, accessModes). Inspect output expansion. Update/Fire validation updates. |
| Sample files | **L** | All 25 samples updated: `nullable` → `optional`, `edit` → `write`, `== null`/`!= null` → `is set`/`is not set`, `set X = null` → `clear X`, add access mode declarations where domain-appropriate. |
| Documentation | **L** | `PreceptLanguageDesign.md`, `RuntimeApiDesign.md`, `EditableFieldsDesign.md`, `McpServerDesign.md`, `README.md` all updated. |
| Tests | **XL** | New test coverage for every new keyword, operator, diagnostic, and semantic rule. Migration diagnostic tests. Access mode composition tests. D3 baseline tests. Guarded write tests. `omit` clearing tests. |

### Suggested implementation order

1. Parser: `optional` keyword + `is set`/`is not set` operators + `clear` action (null vocabulary)
2. Parser: `omit`/`read`/`write` keywords + chained verb group grammar + `edit` removal
3. Type checker: null vocabulary diagnostics (C92, C93, C95, C96)
4. Type checker: access mode diagnostics (C90, C91, C94, C97, C98, C99)
5. Expression evaluator: `is set`/`is not set` evaluation + `clear` execution
6. Runtime engine: `omit` clearing on state entry + Update API access mode enforcement
7. Language server: completions, hover, semantic tokens
8. TextMate grammar
9. MCP tools
10. Sample files
11. Documentation (final slice, same PR)

---

## Acceptance Criteria

### Null vocabulary

- AC-1: `optional` parses as a field modifier in all positions where `nullable` was valid (fields, event args).
- AC-2: `nullable` produces diagnostic C95 with migration message.
- AC-3: `is set` evaluates to `true` for optional fields with a value, `false` for empty optional fields.
- AC-4: `is not set` is the logical negation of `is set`.
- AC-5: `is set` / `is not set` on non-optional fields produces diagnostic C92.
- AC-6: `is set` / `is not set` works in guards (`when`), invariants, state asserts, event asserts, and `if...then...else` conditions.
- AC-7: `clear X` resets an optional field to empty.
- AC-8: `clear X` resets a non-optional field with a declared default to that default.
- AC-9: `clear X` on a non-optional field without default produces diagnostic C93.
- AC-10: `clear` on collections empties the collection (existing semantics preserved).
- AC-11: `null` literal in any expression context produces diagnostic C96.
- AC-12: `default null` is no longer valid syntax.
- AC-13: `optional` without `default` starts the field empty.

### Field access modes

- AC-14: `in S omit F` makes field F structurally absent in state S.
- AC-15: `in S read F` makes field F present and readonly in state S.
- AC-16: `in S write F` makes field F present and editable in state S.
- AC-17: Undeclared (field, state) pairs have effective mode `read` (D3).
- AC-18: `omit` clears field to default on state entry (initial, transition, cycle re-entry).
- AC-19: `omit` does NOT clear on `no transition`.
- AC-20: `set` targeting a field `omit`ted in the target state produces diagnostic C90.
- AC-21: Contradictory declarations for same (field, state) pair produce diagnostic C91.
- AC-22: Guarded `write` conditionally upgrades from D3 baseline; guard fails → `read`.
- AC-23: Guarded `read` or `omit` produces diagnostic C98.
- AC-24: `read` or `omit` at root level produces diagnostic C94.
- AC-25: `write` at root level in a stateful precept produces diagnostic C99.
- AC-26: Chained verb groups parse correctly: `in S omit A, B write C, D read E`.
- AC-27: `edit` produces diagnostic C97 with migration message.
- AC-28: `write all` / `write F1, F2` works in stateless precepts (replaces `edit all` / `edit F1, F2`).
- AC-29: `precept_inspect` returns `accessModes` map showing effective mode per field.
- AC-30: `precept_update` rejects edits to non-`write` fields.

### Cross-cutting

- AC-31: All 25 sample files updated to new syntax.
- AC-32: `PreceptLanguageDesign.md` updated with new grammar, semantic rules, and keyword tables.
- AC-33: TextMate grammar highlights all new keywords in correct scopes.
- AC-34: Language server completions offer new keywords in correct contexts.
- AC-35: All new diagnostics have teachable error messages following project conventions.

---

## Research and Rationale Links

| Document | Content |
|----------|---------|
| `.squad/decisions/inbox/coordinator-field-access-mode-decided.md` | Locked decision record: `omit`/`read`/`write` verb triple, D3 refinement, all alternatives considered |
| `.squad/decisions/inbox/frank-pressure-test-analysis.md` | 5 design questions answered, 6 scenarios validated, 7 composition rules formalized |
| `.squad/decisions/inbox/frank-creative-syntax-exploration.md` | 5 creative syntax ideas explored, `view`/`edit` recommended (later renamed to `read`/`write`) |
| `.squad/decisions/inbox/frank-three-axis-access-modes.md` | Three declaration sites compared, cycle problem discovered, field-level recommended |
| `.squad/decisions/inbox/frank-field-access-modes.md` | 7 syntax alternatives ranked, split-concern (presence + editability) analysis |
| `.squad/decisions/inbox/frank-define-syntax-analysis.md` | `define` verb + mode qualifiers analysis, keyword budget comparison |
| `.squad/decisions/inbox/frank-in-to-from-access-modes.md` | 7 ideas using existing prepositions, `view`/`edit` twin verbs recommended |
| `research/language/expressiveness/field-access-mode-precedents.md` | 10-system PLT/DSL precedent survey (Alloy, TLA+, SCXML, Plaid, Rust typestate, TypeScript, XState, Drools, Cedar, Rego) |
| `research/language/expressiveness/null-elimination-research.md` | External survey of null-free languages, DSLs, databases, and form systems |
| `research/language/expressiveness/nullable-computed-fields-research.md` | Computed field propagation analysis for nullable/optional interaction |
