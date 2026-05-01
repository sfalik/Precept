# Precept Language Specification (v2)

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Incremental — grows as each compiler stage is designed and implemented |
| Implementation state | §1 Lexer complete; §2 Parser complete; §3 Type Checker complete; §3A Language Semantics complete; §0 Preamble complete; §4–5 stubs (pre-implementation contracts in §0.5–0.6) |
| Grounding | `docs/PreceptLanguageDesign.md` (v1 spec); vision archived at `docs/archive/language-design/precept-language-vision.md` |
| Clean room rule | References v1 grammar and keyword inventory; does not import v1 implementation details |

---

## 0. Preamble

This section captures the language's foundational identity: the design principles, conceptual model, governance philosophy, execution model properties, and pre-implementation contracts for pipeline stages not yet built. It is authoritative — any redesign that contradicts material here has departed from Precept's identity.

### 0.1 Design Principles

The language must preserve these principles. They are non-negotiable — any redesign that violates one has departed from Precept's identity.

1. **Prevention, not detection.** Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist. They are structurally prevented before any change is committed, not caught after the fact. This is the defining commitment: the contract prevents them. A compiler that validates on request is doing detection; a compiler that makes invalid configurations structurally impossible is doing prevention.

2. **One file, complete rules.** Every field, rule, ensure, and transition lives in the `.precept` definition. There is no scattered logic across service layers, validators, event handlers, or ORM interceptors. All proof facts, all type information, all constraint scope, all routing — everything needed to understand and enforce the contract — derives from a single file. No imports, no cross-file references, no external rule injection, no ambient configuration.

3. **Deterministic semantics.** Same definition, same data, same outcome — always. No non-deterministic solvers, no timing-dependent analysis, no culture-dependent operations, no stochastic reasoning. This is what makes the engine trustworthy as a business rules host and auditable as an AI agent tool.

4. **Full inspectability.** The language must make the engine's reasoning fully exposable. At any point, you can preview every possible action and its outcome without executing anything. The engine exposes the complete reasoning: conditions evaluated, branches taken, constraints applied. You see not only what would happen, but why. Nothing is hidden. Inspectability extends to proof reasoning — proven ranges, source attribution, and what the engine could not prove must all be surfaceable through diagnostics, hover, and tooling.

5. **Keyword-anchored readability.** Structure is explicit and line-oriented, but "line-oriented" means declarations are anchored by keywords, not terminated by newlines. Statement kind is identified by its opening keyword sequence. A declaration may span multiple lines freely; whitespace — including newlines — is cosmetic within the declaration. Inside transitions, `from` starts a new transition row and each `->` starts the next pipeline step; those keywords are the structural boundaries, not layout. This keeps the language AI-safe for authoring, avoids layout traps during reformatting/copy-paste/code generation, keeps tooling boundary detection keyword-driven instead of layout-driven, and lets dense compound declarations such as `queue of T by choice of string(...) ordered` wrap across lines for human readability without changing meaning.

6. **Explicit domain meaning over primitive convenience.** When a value has real domain identity (money, date, quantity, currency), the language should name it as a distinct type with its own operator rules and compile-time enforcement. Primitive types are the storage mechanism; domain types are the meaning mechanism.

7. **Compile-time-first static checking.** The compiler proves what it can, rejects what it can prove invalid, and does not guess. Compile-time structural checking catches unreachable states, type mismatches, constraint contradictions, division by zero, overflow, empty collection access, and more — before runtime, before any entity instance exists. A precept that compiles without diagnostics has no unproven evaluation faults. This is where the "contract" metaphor becomes literal: the compile step validates the definition's structural soundness before any instance exists.

8. **Approximation honesty.** The language does not present approximation as exactness. If a value or operation is inherently approximate, that fact must be explicit in the contract. Exact-value lanes remain exact. Silent approximation inside an exact-looking path weakens the user's ability to reason about outcomes. The line between exact and approximate behavior must be visible in the type system and the language surface.

9. **Mandatory rationale.** Every constraint carries a mandatory reason. The engine requires not just the rule, but its rationale. This is a language requirement, not a convention — the `because` clause is syntactically required on every rule and ensure.

10. **Totality.** Every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`. The evaluation surface has no undefined behavior. For any expression that *could* fault at runtime — division by zero, overflow, empty collection access — the compiler must either prove safety or emit a diagnostic requiring the author to supply constraints that make safety provable. A precept that compiles without diagnostics has no unproven arithmetic or access faults. Runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable.

11. **Static completeness.** If a precept compiles without diagnostics, it does not fault at runtime. The compiler catches all type errors, proves all arithmetic safety obligations, and verifies all access preconditions at compile time. Every fault class that the evaluator can produce — type mismatch, division by zero, overflow, empty collection access, constraint range impossibility — is linked to a compiler diagnostic that prevents it. Runtime fault checks exist only as defensive redundancy, never as the primary enforcement mechanism. This is the bridge between the compiler and the evaluator: the compiler's job is to make every evaluator error path unreachable.

### 0.2 Language Model

A precept defines a governed business entity.

The entity may be:

1. Stateful: lifecycle positions, transitions, state-scoped rules, and event routing.
2. Stateless: data integrity, editability, and event-driven mutation without a state machine.

The governing concepts are:

1. Fields: stored or computed data.
2. Rules: global data truth.
3. Ensures: contextual truth tied to state or event context.
4. Events: typed triggers.
5. Actions: mutations.
6. Transitions: movement truth.
7. Editability: direct mutation permissions.
8. Modifiers: declaration-attached structural, semantic, or severity intent.

The language protects configurations, not isolated values. In a stateful precept, a configuration is current state plus current field data. In a stateless precept, it is the current field data alone.

### 0.3 Governance, Not Validation

This distinction is fundamental to the language's identity and governs how every constraint surface works.

**Validation** checks data at a moment in time, when called. A validator runs when you invoke it. Code paths that don't call the validator bypass the rules. There is always a window where invalid state can exist.

**Governance** declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract. The language makes certain configurations structurally impossible — not checked, not caught, but prevented.

This distinction shapes the language at every level:

1. Rules are not assertions called at a checkpoint. They are declarations that hold structurally on every operation where they apply.
2. Guarded rules do not weaken the guarantee. They make it precise — the rule applies exactly where the domain says it should, and the engine ensures it cannot be bypassed.
3. State is not a passive label. It is an active rule-activator. An entity in `Approved` has different data requirements than the same entity in `Draft` — the state defines what must be true about the data there.
4. Mutations are atomic. All mutations execute on a working copy. Constraints are evaluated against the working copy. If all constraints pass, the working copy is promoted. If any constraint fails, the working copy is discarded. There is no window where a partially-committed mutation with a violated rule exists.
5. An invalid definition cannot produce an engine. The compile-time gate is a structural boundary — not a convention, not a best practice. If the definition has errors, no engine exists to run.

The full guarantee is about configurations: the pair of (current lifecycle position, current field values) for stateful entities, or simply current field values for stateless entities. Invalid configurations are structurally impossible. A valid entity is simply one where every constraint holds for its current configuration.

### 0.4 Execution Model Properties

The following properties of Precept's execution model are language design choices, not implementation accidents. They are what make tractable compile-time reasoning possible, and any future compiler must preserve them.

1. **No loops.** The language has no iteration constructs. Expression trees are finite and acyclic. This eliminates the need for fixpoint computation and widening operators.

2. **No control-flow branches.** A transition row is a flat sequence: evaluate a guard, execute assignments left-to-right, check rules and ensures. There are no `if` statements that split execution into paths that later reconverge. Conditional *expressions* (`if/then/else`) produce a single value — both branches are type-checked, exactly one is evaluated — but they do not create control-flow divergence.

3. **No reconverging flow.** Because there are no loops or branches, there is no join point where two different states must be merged. Each assignment in a row sees the state left by all preceding assignments. This makes sequential flow analysis a linear walk, not a dataflow graph.

4. **Closed type vocabulary.** The language has a fixed set of types. No user-defined types, no parametric polymorphism, no open type hierarchies. This makes exhaustive type checking possible over a finite, fully-known vocabulary.

5. **Finite state space.** States, events, fields, and rules are declared statically. Every transition row, state action, and rule can be enumerated exhaustively. No symbolic execution over unbounded domains is needed.

6. **Expression purity.** Expressions cannot mutate entity state, trigger side effects, or observe anything outside their evaluation context (current field values and event arguments). This is a language semantic, not a caller convention. It is what makes inspection safe — you can always ask what an expression would produce without affecting anything.

7. **No separate compilation.** Each `.precept` file is self-contained. No imports, no cross-file references. The compiler processes the entire definition in one pass. All proof facts derive from the single file.

These properties are the reason the language can support tractable compile-time proofs. Standard interval arithmetic, bounded relational closure, and single-pass validation are directly applicable without the lattice infrastructure that general-purpose analyzers require. The absence of widening is a feature: in general-purpose analyzers, widening is the primary source of precision loss.

### 0.5 Graph Analyzer Design Contract

> **Pre-implementation contract.** This section captures the language's requirements for
> the graph analyzer. §4 is a stub pending implementation. These requirements define the
> contract the implementation must satisfy.

The compiler must build and reason over the full state transition graph at compile time. The graph is constructed from declared states, events, and transition rows. This is a first-class language requirement, not an optional optimization.

The graph analysis surface must support at least these reasoning capabilities:

1. **BFS/DFS reachability from initial.** Required to detect unreachable states (C48) and to define the reachable state set that other modifiers reason over. `initial` provides the root.
2. **Terminal state identification.** States with no outgoing transition rows. Required to anchor path analysis and to validate `terminal` modifier declarations.
3. **Dead-end state detection.** Non-terminal states where all outgoing rows reject or produce no-transition. These have transition machinery that never succeeds — likely authoring mistakes (C50).
4. **Incoming/outgoing edge analysis.** Per-state: which events fire into this state, which events fire out. Required for `guarded` (all incoming transitions have guards), `entry` (event fires only from initial), `isolated` (event fires from exactly one state), `universal` (event fires from every reachable non-terminal state).
5. **Dominator analysis.** Required for `required`/`milestone` — the modifier asserts that all initial→terminal paths must visit this state. Dominator analysis (O(V+E) via Lengauer-Tarjan) determines whether a state is on every such path.
6. **Reverse-reachability.** Required for `irreversible` (no path from this state back to any ancestor state in the initial→forward ordering) and `sealed after <State>` (no mutation after the named state is entered — requires reachability analysis from the named state forward).
7. **Row-partition analysis.** Required for `writeonce` (field set at most once across all reachable transition rows) and `sealed after` (no row reachable after the named state assigns to the field).
8. **Outcome-type analysis.** Per (state, event) pair: do all rows produce `transition`? `no transition`? All `reject`? Required for `advancing` (every success is a state transition), `settling` (every success is no-transition), `completing` (transitions only to terminal states), `absorbing` (event handlers never transition out), and for existing diagnostics like C51 (reject-only pairs) and C52 (events that never succeed).

**Overapproximation rule.** Structural graph analysis treats all edges as traversable regardless of `when` guards — it overapproximates reachability. This is sound: structural guarantees cannot account for guard-dependent path selection because guard satisfaction depends on runtime data. A modifier that claims "all paths visit this state" means all *structurally declared* paths, not all guard-satisfiable paths. This is the correct tradeoff for compile-time analysis.

**Interaction with existing diagnostics.** The graph analysis that modifiers require is an extension of the analysis the compiler already performs for C48 (unreachable states), C49 (orphaned events), C50 (dead-end states), C51 (reject-only pairs), and C52 (events that never succeed). Modifiers do not replace these diagnostics — they make them stronger by adding author-declared intent that the compiler can cross-check against the graph structure.

### 0.6 Proof Engine Design Contract

> **Pre-implementation contract.** This section captures the language's requirements for
> the proof engine. §5 is a stub pending implementation. These requirements define the
> contract the implementation must satisfy.

#### Proof-system responsibilities

The proof layer must be able to support the language's proof-bearing claims. That includes:

1. **Numeric interval reasoning.** Field constraints, rules, and guards contribute provable numeric ranges. The proof system tracks these ranges through assignment chains.
2. **Relational reasoning** over numeric expressions involving multiple fields.
3. **Divisor safety.** Two-tier: proven-zero divisors are hard errors; divisors with no compile-time nonzero proof are obligation diagnostics requiring the author to supply a constraint (e.g., `nonzero`, `positive`, a rule, or a guard).
4. **Non-negative proof obligations** such as `sqrt` inputs and `pow(integer, integer)` exponents. The compiler requires a provable non-negative path — via `nonnegative` constraint, a rule, an ensure, or a guard — before accepting the expression.
5. **Assignment range impossibility.** An assignment expression provably outside the target field's constraint range is a compile-time error.
6. **Contradictory rule detection.** When two rules' ranges are provably incompatible — no value can satisfy both simultaneously — the compiler reports the contradiction.
7. **Vacuous rule detection.** When a rule is provably always true given field constraints, the compiler reports it as tautological.
8. **Dead guard detection.** A guard provably always false means the row or block can never execute.
9. **Tautological guard detection.** A guard provably always true means the `when` clause has no effect.
10. **Compile-time rule enforcement against defaults.** Rules and initial-state ensures are checked against default field values at compile time. A definition where default values violate a declared rule is rejected before any instance exists.
11. **Sharpening of reachability and routing diagnostics** from proven-dead guards.
12. **Structured proof attribution** suitable for hover, diagnostics, and agent consumption — every proven range carries the constraints and rules that contributed to it.

#### Proof philosophy

The proof layer is governed by these requirements, which are language-level commitments, not implementation preferences:

1. **Soundness over completeness.** The proof layer must never claim an expression is safe when it is not. Every proof path must return a provably correct result or conservatively decline. False negatives (missed proofs) cause author friction — the author must supply additional constraints. False positives (wrong "safe" claims) cause runtime failures. The language always chooses the safe direction.

2. **Proven violations only.** The language reports what is definitively broken, not what might be broken. Flagging possible violations turns the compiler into a nag that trains authors to ignore warnings. Flagging only proven violations makes it a trusted guide — when it speaks, it is right.

3. **Opaque solvers are rejected on principle.** The language's proof reasoning must be legible — to authors, to tooling, and to AI agents. Proof witnesses must be structured data, not opaque solver traces. If the compiler cannot prove safety, it says so explicitly — the author is never confronted with an unexplainable verdict. This is why SMT/Z3 solvers are excluded even when they could prove more: opaque proof witnesses violate the inspectability commitment.

4. **One file, complete proof facts.** All proof facts derive from the `.precept` definition. No external oracle, no hidden configuration, no side channel. The proof engine's knowledge boundary is the file boundary.

5. **Truth-based diagnostic classification.** Proof outcomes are classified into three categories: *proved dangerous* (the compiler can demonstrate a violation), *proved safe* (the compiler can demonstrate correctness), and *unresolved* (the compiler cannot determine either). These categories map to distinct author actions: fix a proven violation, rely on proven safety, or supply additional constraints to help the compiler. Diagnostics are classified by proof outcome, not by syntax shape.

6. **Proof attribution is required, not optional.** Every proven range must carry its source attribution — the field constraints, rules, and guards that contributed. Authors must see what the engine proved, what it could not prove, and why. Proof results flow as structured data, not parsed prose — tooling and agents consume the proof model directly, never by parsing diagnostic message text.

7. **Sequential proof flow.** Actions in a chain are sequenced — each subsequent action sees the proof state left by all preceding actions. When a field is reassigned, prior proof facts about that field are invalidated before the new assignment's facts are stored. This is a language semantic that ensures proof reasoning tracks the actual mutation sequence.

---

## 1. Lexer

The lexer transforms source text into a flat token stream. It produces `TokenStream` containing an `ImmutableArray<Token>` and an `ImmutableArray<Diagnostic>` for lexer-level errors (unterminated strings, unrecognized characters).

The lexer enforces a hard source-size ceiling of **65,536 characters (64 KB)** as a **security guardrail**. The limit exists to bound lexer work and memory usage on adversarial input; it is not a language expressiveness rule.

### 1.1 Token Vocabulary

Every token the lexer can produce. Organized by category to match the `TokenKind` enum.

#### Keywords: Declaration

| Token | Text | Context |
|-------|------|---------|
| `Precept` | `precept` | Precept header declaration |
| `Field` | `field` | Field declaration |
| `State` | `state` | State declaration |
| `Event` | `event` | Event declaration |
| `Rule` | `rule` | Named rule / invariant declaration |
| `Ensure` | `ensure` | State/event assertion keyword |
| `As` | `as` | Type annotation (`field X as number`) |
| `Default` | `default` | Default value modifier |
| `Optional` | `optional` | Field optionality modifier (v2) |
| `Writable` | `writable` | Field writable-baseline modifier — marks a non-computed field as directly editable by default across all states (v2) |
| `Because` | `because` | Reason clause |
| `Initial` | `initial` | Initial state marker |

#### Keywords: Prepositions

| Token | Text | Context |
|-------|------|---------|
| `In` | `in` | State-scoped scope preposition (`in State ensure ...`, `in State modify|omit ...`) |
| `To` | `to` | Entry-gate ensure (`to State ensure ...`) |
| `From` | `from` | Exit-gate ensure or transition source (`from State ...`) |
| `On` | `on` | Event trigger (`on Event ensure ...`, `from State on Event ...`) |
| `Of` | `of` | Collection inner type (`set of string`) |
| `Into` | `into` | Dequeue/pop target (`dequeue Queue into Field`) |

#### Keywords: Control

| Token | Text | Context |
|-------|------|---------|
| `When` | `when` | Guard clause |
| `If` | `if` | Conditional expression |
| `Then` | `then` | Conditional expression |
| `Else` | `else` | Conditional expression |

#### Keywords: Actions

| Token | Text | Context |
|-------|------|---------|
| `Set` | `set` | Field assignment action / set collection type (dual-use) |
| `Add` | `add` | Set add action |
| `Remove` | `remove` | Set remove action |
| `Enqueue` | `enqueue` | Queue enqueue action |
| `Dequeue` | `dequeue` | Queue dequeue action |
| `Push` | `push` | Stack push action |
| `Pop` | `pop` | Stack pop action |
| `Clear` | `clear` | Collection clear action |

#### Keywords: Outcomes

| Token | Text | Context |
|-------|------|---------|
| `Transition` | `transition` | State transition outcome |
| `No` | `no` | Prefix for `no transition` |
| `Reject` | `reject` | Rejection outcome |

#### Keywords: Access Modes (v2 → v3)

| Token | Text | Context |
|-------|------|---------|
| `Modify` | `modify` | State-scoped access mode verb (`in <State> modify <Field> readonly\|editable …`) |
| `Readonly` | `readonly` | Access mode adjective — constrain to read-only (`in <State> modify <Field> readonly …`) |
| `Editable` | `editable` | Access mode adjective — declare editable (`in <State> modify <Field> editable …`) |
| `Omit` | `omit` | State-scoped structural exclusion (`in <State> omit …`) |

#### Keywords: Logical Operators

| Token | Text | Context |
|-------|------|---------|
| `And` | `and` | Logical conjunction |
| `Or` | `or` | Logical disjunction |
| `Not` | `not` | Logical negation |

#### Keywords: Membership

| Token | Text | Context |
|-------|------|---------|
| `Contains` | `contains` | Collection membership test |
| `Is` | `is` | Multi-token operator prefix (`is set`, `is not set`) |

#### Keywords: Quantifiers / Modifiers

| Token | Text | Context |
|-------|------|---------|
| `All` | `all` | Universal quantifier / `modify all` / `omit all` (state-scoped) |
| `Any` | `any` | State wildcard (`in any`, `from any`) |

#### Keywords: State Modifiers (v2)

| Token | Text | Context |
|-------|------|---------|
| `Terminal` | `terminal` | Structural: no outgoing transitions |
| `Required` | `required` | Structural: all initial→terminal paths visit this state (dominator) |
| `Irreversible` | `irreversible` | Structural: no path back to any ancestor state |
| `Success` | `success` | Semantic: marks a success outcome state |
| `Warning` | `warning` | Semantic: marks a warning outcome state |
| `Error` | `error` | Semantic: marks an error outcome state |

#### Keywords: Constraints

| Token | Text | Context |
|-------|------|---------|
| `Nonnegative` | `nonnegative` | Number/integer constraint: value >= 0 |
| `Positive` | `positive` | Number/integer constraint: value > 0 |
| `Nonzero` | `nonzero` | Number/integer constraint: value != 0 |
| `Notempty` | `notempty` | String constraint: non-empty |
| `Min` | `min` | Numeric minimum constraint / built-in function (dual-use) |
| `Max` | `max` | Numeric maximum constraint / built-in function (dual-use) |
| `Minlength` | `minlength` | String minimum length constraint |
| `Maxlength` | `maxlength` | String maximum length constraint |
| `Mincount` | `mincount` | Collection minimum count constraint |
| `Maxcount` | `maxcount` | Collection maximum count constraint |
| `Maxplaces` | `maxplaces` | Decimal maximum decimal places constraint |
| `Ordered` | `ordered` | Choice ordinal comparison constraint |

#### Keywords: Types

| Token | Text | Context |
|-------|------|---------|
| `StringType` | `string` | Scalar type |
| `BooleanType` | `boolean` | Scalar type |
| `IntegerType` | `integer` | Scalar type (v2: explicit integer, separate from number) |
| `DecimalType` | `decimal` | Scalar type (v2: exact base-10) |
| `NumberType` | `number` | Scalar type (general numeric) |
| `ChoiceType` | `choice` | Enumerated value set type — requires explicit element type (`choice of T(...)`) |
| `SetType` | `set` | Set collection type (dual-use with action keyword) |
| `QueueType` | `queue` | Queue collection type |
| `StackType` | `stack` | Stack collection type |

#### Keywords: Temporal Types (v2)

| Token | Text | Context |
|-------|------|---------|
| `DateType` | `date` | Temporal: calendar date |
| `TimeType` | `time` | Temporal: time of day |
| `InstantType` | `instant` | Temporal: UTC point in time |
| `DurationType` | `duration` | Temporal: elapsed time quantity |
| `PeriodType` | `period` | Temporal: calendar quantity |
| `TimezoneType` | `timezone` | Temporal: timezone identity |
| `ZonedDateTimeType` | `zoneddatetime` | Temporal: date+time+timezone |
| `DateTimeType` | `datetime` | Temporal: local date+time |

#### Keywords: Business-Domain Types (v2)

| Token | Text | Context |
|-------|------|---------|
| `MoneyType` | `money` | Business: monetary amount |
| `CurrencyType` | `currency` | Business: currency identity |
| `QuantityType` | `quantity` | Business: measured quantity |
| `UnitOfMeasureType` | `unitofmeasure` | Business: unit identity |
| `DimensionType` | `dimension` | Business: dimension family identity |
| `PriceType` | `price` | Business: compound money/quantity rate |
| `ExchangeRateType` | `exchangerate` | Business: compound currency/currency rate |

#### Keywords: Literals

| Token | Text | Context |
|-------|------|---------|
| `True` | `true` | Boolean literal |
| `False` | `false` | Boolean literal |

#### Operators

| Token | Text | Precedence note |
|-------|------|-----------------|
| `DoubleEquals` | `==` | Comparison |
| `NotEquals` | `!=` | Comparison |
| `GreaterThanOrEqual` | `>=` | Comparison (scanned before `>`) |
| `LessThanOrEqual` | `<=` | Comparison (scanned before `<`) |
| `GreaterThan` | `>` | Comparison |
| `LessThan` | `<` | Comparison |
| `Assign` | `=` | Assignment (scanned after `==`) |
| `Plus` | `+` | Arithmetic |
| `Minus` | `-` | Arithmetic / unary negation |
| `Star` | `*` | Arithmetic |
| `Slash` | `/` | Arithmetic |
| `Percent` | `%` | Arithmetic (modulo) |
| `Arrow` | `->` | Action chain / outcome separator |
| `CaseInsensitiveEquals` | `~=` | Case-insensitive comparison (string-only) |
| `CaseInsensitiveNotEquals` | `!~` | Case-insensitive not-equals (string-only) |
| `Tilde` | `~` | Case-insensitive collection inner type prefix (`set of ~string`) |

**Scan order for operators:** Multi-character operators must be attempted before their single-character prefixes: `!~` before `!=` before `!` (if ever reintroduced), `~=` before `~`, `->` before `-`, `==` before `=`, `>=` before `>`, `<=` before `<`. A standalone `~` is only valid immediately before `string` in a collection inner type position — elsewhere it is a lexer error.

#### Punctuation

| Token | Text |
|-------|------|
| `Dot` | `.` |
| `Comma` | `,` |
| `LeftParen` | `(` |
| `RightParen` | `)` |
| `LeftBracket` | `[` |
| `RightBracket` | `]` |

#### Literals

| Token | Produced when |
|-------|---------------|
| `NumberLiteral` | Digit sequence, optionally with one `.` followed by more digits |
| `StringLiteral` | `"..."` with no `{` interpolation (emitted as a single token) |
| `StringStart` | `"...{` — text before the first interpolation opening |
| `StringMiddle` | `}...{` — text between interpolation segments |
| `StringEnd` | `}..."` — text after the last interpolation closing |
| `TypedConstant` | `'...'` with no `{` interpolation (emitted as a single token) |
| `TypedConstantStart` | `'...{` — typed constant before first interpolation |
| `TypedConstantMiddle` | `}...{` — typed constant between interpolation segments |
| `TypedConstantEnd` | `}...'` — typed constant after last interpolation |

**`Token.Text` contract for quoted literals:** `Text` contains the semantic content — delimiters stripped, escape sequences resolved. `StringLiteral` for `"hello"` → `Text = "hello"`. `StringStart` for `"Hello {` → `Text = "Hello "`. `StringEnd` for `} world"` → `Text = " world"`. `TypedConstant` for `'2026-04-23'` → `Text = "2026-04-23"`. A zero-length segment (e.g. `"{Name}"` where nothing precedes the first `{`) produces an empty `Text` — the token is still emitted.

**`Token.Offset` and `Token.Length` contract for quoted literals:** `Offset` and `Length` span the full raw source range including opening and closing delimiters. `StringLiteral` for `"hello"` has `Length = 7` (both quotes included). `StringStart` for `"Hello {` spans through the `{`. This allows tools to highlight or replace the exact source text without having to re-infer delimiter positions. `Text` (content-only) and `Offset`/`Length` (raw-source span) are complementary.

See [§1.3 Literal Syntax](#13-literal-syntax) for full rules. See `docs/compiler/literal-system.md` for the complete literal system design.

#### Identifiers

| Token | Produced when |
|-------|---------------|
| `Identifier` | A word matching the identifier grammar that is not a reserved keyword |

```
Identifier  :=  Letter (Letter | Digit | '_')*
Letter      :=  [a-zA-Z]
Digit       :=  [0-9]
```

Identifiers are case-sensitive. Leading underscores are not permitted. Examples: `Balance`, `applicantName`, `Phase1`, `line_item_total`.

#### Structure

| Token | Produced when |
|-------|---------------|
| `Comment` | `#` through end of line |
| `NewLine` | Line terminator (LF, CRLF, or CR) |
| `EndOfSource` | Sentinel appended after all source text is consumed |

### 1.2 Reserved Keywords

Keywords are **strictly lowercase**. Identifiers are case-sensitive: `From` is a valid identifier, `from` is a reserved keyword.

The complete v2 reserved keyword set:

```
precept  field  as  default  optional  writable  rule  because
state  initial  terminal  required  irreversible  event  ensure
success  warning  error
in  to  from  on  when  any  all  of
set  add  remove  enqueue  dequeue  push  pop  clear  into
transition  no  reject
omit  modify  readonly  editable
string  number  boolean  integer  decimal  choice  maxplaces  ordered
date  time  instant  duration  period  timezone  zoneddatetime  datetime
money  currency  quantity  unitofmeasure  dimension  price  exchangerate
true  false
and  or  not  contains  is
if  then  else
nonnegative  positive  nonzero  notempty
min  max  minlength  maxlength  mincount  maxcount
```

**v2 additions** (not in v1): `optional`, `writable`, `omit`, `clear`, `nonzero`, `is`, `integer`, `decimal`, `choice`, `maxplaces`, `ordered`, `terminal`, `required`, `irreversible`, `success`, `warning`, `error`, `date`, `time`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, `datetime`, `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`.

**v3 additions** (not in v2): `modify`, `readonly`, `editable`. The access mode verbs `write`/`read` are removed entirely in favor of the `modify` verb + adjective pattern. `write` and `read` are no longer reserved — they are ordinary identifiers in v3.

**v3 removals:** `write` and `read` are not reserved in v3. The `modify` verb + `readonly`/`editable` adjective pattern replaces them. Both are ordinary identifiers in v3; no special parser recognition is needed.

**v1 removals:** `nullable`, `null`, and `edit` are not reserved in v2. `optional` replaces `nullable`. `modify` replaces `edit`. The `null` literal is removed entirely — `optional` fields use `is set`/`is not set` for presence testing and `clear` for value removal. All three are ordinary identifiers in v2; no special parser recognition is needed.

### 1.3 Literal Syntax

The language has two quoted literal forms with distinct roles. Double-quoted strings (`"..."`) always produce `string`. Single-quoted typed constants (`'...'`) produce non-primitive values — the lexer treats their content opaquely and the type checker determines the specific type (see [§3.3 Context-Sensitive Type Resolution](#33-context-sensitive-type-resolution)). There is no constructor-call syntax for non-primitive types (e.g., no `date(...)` or `period(...)`) — typed constants are the sole mechanism for constructing non-primitive literal values. Both quoted forms support `{expr}` interpolation. Numeric and boolean literals remain bare tokens.

#### Numeric literals

A numeric literal is a sequence of decimal digits, optionally preceded by a unary minus, and optionally followed by a decimal part and/or an exponent part. The parser constant-folds `-` followed by a `NumberLiteral` into a single signed literal value — negative numbers are first-class literals, not runtime negation. No leading `+`. No underscores or grouping separators.

```
NumberLiteral  :=  '-'? Digits ('.' Digits)? (('e' | 'E') ('+' | '-')? Digits)?
Digits         :=  [0-9]+
```

Examples: `0`, `42`, `-1`, `3.14`, `-3.14`, `0.5`, `1.5e2`, `-1e5`, `1e-5`, `3.0E+10`

The lexer produces a single `NumberLiteral` token for all numeric forms. The type checker determines the specific numeric type based on context (see [§3.3 Context-Sensitive Type Resolution](#33-context-sensitive-type-resolution)).

**Exponent notation and numeric types:** Exponent notation (`e`/`E`) is only valid for the `number` type. It is a type error to use exponent notation in a context that requires `integer` or `decimal` — `integer` is whole numbers only, and `decimal` is exact base-10 representation where exponent form would be semantically misleading. The lexer accepts all forms; the type checker enforces the restriction.

#### String literals (`"..."`)

String literals are delimited by double quotes. They always produce `string` type values.

**Without interpolation:** `"hello world"` → single `StringLiteral` token.

**With interpolation:** The lexer decomposes the string into segments at `{` and `}` boundaries:

```
"Hello {Name}, your balance is {Balance}"
```
→ `StringStart("Hello ")`, `Identifier(Name)`, `StringMiddle(", your balance is ")`, `Identifier(Balance)`, `StringEnd("")`

Interpolation is always-on — `{` inside a string always opens an interpolation expression. To include a literal `{`, escape it as `{{`. To include a literal `}`, escape it as `}}`.

**Empty interpolation:** `"{}"` is lexically valid. The lexer emits `StringStart("")`, then immediately sees `}` and emits `StringEnd("")` with no expression tokens between them. The parser rejects empty interpolation as a syntax error (expected expression). Zero-length `Text` on `StringStart`/`StringEnd` is normal and expected — the lexer always emits the boundary token even when the content is empty.

**Escape sequences in strings:** `\"` (double quote), `\\` (backslash), `\n` (newline), `\t` (tab), `{{` (literal brace), `}}` (literal brace).

#### Typed constants (`'...'`)

Typed constants are delimited by single quotes. The type is determined by expression context (not by content) — the lexer treats the content opaquely.

**Without interpolation:** `'2026-04-23'` → single `TypedConstant` token.

**With interpolation:** Same decomposition rules as strings, producing `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd` tokens.

**Escape sequences:** `\'` (single quote), `\\` (backslash), `{{` (literal brace), `}}` (literal brace).

**Content words are not keywords.** Words that appear inside typed constant content — such as `days`, `hours`, `minutes`, `seconds`, `months`, `years`, `weeks`, `USD`, `kg` — are not language keywords. They are validated by the type checker against the context-determined type, not by the lexer. This keeps the reserved keyword set stable as new types are added.

#### List literals

List literals are delimited by `[` and `]` with comma-separated scalar values. The lexer produces individual punctuation and value tokens — list structure is assembled by the parser.

```
[1, 2, 3]       → LeftBracket, NumberLiteral, Comma, NumberLiteral, Comma, NumberLiteral, RightBracket
["a", "b"]       → LeftBracket, StringLiteral, Comma, StringLiteral, RightBracket
[]               → LeftBracket, RightBracket
```

### 1.4 Comments and Whitespace

Precept source has no indentation significance. Indentation is purely cosmetic — the lexer treats leading spaces and tabs identically to inter-token whitespace.

**Comments:** `#` begins a comment that extends to the end of the line. Comments can appear standalone on a line or after code on the same line. The lexer emits a `Comment` token (the parser may discard or preserve it).

```
# This is a standalone comment
field Balance as number  # This is an inline comment
```

**Whitespace:** Spaces and tabs between tokens are consumed silently (no token emitted). They serve only as delimiters between keyword/identifier tokens.

**Newlines:** Line terminators (LF `\n`, CRLF `\r\n`, CR `\r`) produce `NewLine` tokens, but they are trivia to the parser rather than structural boundaries. Statement kind is determined entirely by the opening keyword sequence, not by line position or termination. Declarations may span multiple lines freely. Within transitions, `from` introduces a new transition row and each `->` introduces a new pipeline step; those keywords are the only structural separators inside the transition surface.

### 1.5 Operator and Punctuation Scanning

Operators and punctuation are scanned after attempting keyword/identifier matches. Multi-character operators are tried before their single-character prefixes to avoid false matches.

**Scan priority (highest first):**

1. `->` (Arrow)
2. `==` (DoubleEquals)
3. `!=` (NotEquals)
4. `>=` (GreaterThanOrEqual)
5. `<=` (LessThanOrEqual)
6. `=` (Assign)
7. `>` (GreaterThan)
8. `<` (LessThan)
9. `+`, `-`, `*`, `/`, `%` (Arithmetic)
10. `.`, `,`, `(`, `)`, `[`, `]` (Punctuation)

### 1.6 Dual-Use Token Disambiguation

Three tokens serve double duty. The lexer emits a single token kind for each; the parser disambiguates by syntactic context.

#### `set` — Collection Type and Action Keyword

| Preceding context | Interpretation | Example |
|-------------------|---------------|---------|
| After `as` or `of` (type position) | `SetType` | `field Tags as set of string` |
| After `->` (action position) | `Set` (action) | `-> set Balance = 0` |

A third use exists: `set` as an adjective in the presence operators `is set` / `is not set` (for `optional` fields). This is not a lexer disambiguation concern — the lexer emits separate `Is`, `Not`, and `Set` tokens, and the parser composes the multi-token operator.

**Lexer strategy (locked):** The lexer always emits `TokenKind.Set` for the word `set`. `TokenKind.SetType` is never produced by the lexer — it is a parser-synthesized token kind used in the AST to represent `set` in a type position. The `Tokens.Keywords` dictionary maps `"set"` to `TokenKind.Set` only. The parser reinterprets the `Set` token as `SetType` when the preceding token is `As` or `Of`.

#### `min` / `max` — Constraint Keyword and Built-in Function

| Following context | Interpretation | Example |
|-------------------|---------------|---------|
| Followed by a number literal (constraint zone) | Constraint | `field Score as number min 0 max 100` |
| Followed by `(` (expression context) | Function call | `set Amount = min(Requested, Available)` |

The `(` disambiguates: constraint keywords are never followed by `(`, function calls always are.

#### `in` / `of` — Preposition and Type Qualifier

`in` and `of` each serve two roles: routing/scoping prepositions and type-position qualifiers.

| Context | Role | Example |
|---------|------|---------|
| After a state name (`in Draft ensure ...`) | Routing preposition | `in Draft ensure Amount > 0` |
| After a domain type in a field declaration | Type qualifier | `field Amount as money in 'USD'` |
| After `set of`, `queue of`, `stack of` | Collection inner type | `field Tags as set of string`, `field Labels as set of ~string` |
| After a domain type in a field declaration | Dimension family qualifier | `field Distance as quantity of 'length'` |

Type qualifiers narrow the value domain of the field — they are part of the type annotation, not a declaration modifier. `in '<unit>'` pins to a specific unit or currency. `of '<family>'` constrains to a dimension or component family. A field may use `in` or `of`, never both — with one exception: `price` allows `in` (currency-only) combined with `of` (denominator dimension), because price has two independent axes. When `in` specifies a compound `'currency/unit'` value, `of` is rejected. The preceding token (always a type keyword or collection keyword) makes the type-qualifier role unambiguous at LL(1).

The lexer uses a mode stack to handle nested interpolation in string and typed-constant literals. This ensures `{expr}` inside a literal correctly lexes the expression tokens and then returns to the literal context.

**Modes:**

| Mode | Active when |
|------|-------------|
| `Normal` | Default mode — scanning keywords, identifiers, operators, literals |
| `String` | Inside a `"..."` literal, scanning text and looking for `{` or `"` |
| `TypedConstant` | Inside a `'...'` literal, scanning text and looking for `{` or `'` |
| `Interpolation` | Inside `{...}` within a literal, scanning expression tokens and looking for `}` |

**Transitions:**

| From | On | To | Emits |
|------|----|----|-------|
| Normal | `"` | String | (begins string scanning) |
| String | `{` | Interpolation (push) | `StringStart` or `StringMiddle` |
| String | `"` | Normal | `StringEnd` or `StringLiteral` |
| Normal | `'` | TypedConstant | (begins typed constant scanning) |
| TypedConstant | `{` | Interpolation (push) | `TypedConstantStart` or `TypedConstantMiddle` |
| TypedConstant | `'` | Normal | `TypedConstantEnd` or `TypedConstant` |
| Interpolation | `}` | (pop to String or TypedConstant) | (resumes enclosing literal scanning) |
| Interpolation | `"` | String (push) | (nested string inside interpolation) |
| Interpolation | `'` | TypedConstant (push) | (nested typed constant inside interpolation) |

Nesting is fully supported: a string interpolation expression can contain a typed constant, and vice versa. The mode stack has a maximum depth of **8**. If a push would exceed this limit, the lexer emits an `UnterminatedInterpolation` diagnostic and resumes using the recovery rule for unterminated interpolations. Realistic nesting depth is 3 or fewer; the limit exists to prevent unbounded stack growth on adversarial input.

### 1.8 Lexer Diagnostics

The lexer emits diagnostics for malformed input. These are collected alongside tokens in the `TokenStream`.

The `InputTooLarge` diagnostic is different from the other entries in this table: it is a security failure, not a syntax failure. Once the source crosses the 65,536-character ceiling, lexing aborts immediately and returns only the `EndOfSource` sentinel so downstream stages never process the hostile input.

| Condition | Diagnostic Code | Severity | Description |
|-----------|-----------------|----------|-------------|
| Input too large | `InputTooLarge` | Error | Source exceeds 65536 characters (64 KB), which is the lexer security limit; lexing is aborted and the token stream contains only the `EndOfSource` sentinel |
| Unterminated string literal | `UnterminatedStringLiteral` | Error | `"hello` with no closing `"` before end of line/source |
| Unterminated typed constant | `UnterminatedTypedConstant` | Error | `'2026-01-01` with no closing `'` before end of line/source |
| Unterminated interpolation | `UnterminatedInterpolation` | Error | `"hello {Name` with no closing `}` before end of line |
| Unrecognized character | `InvalidCharacter` | Error | Character that is not part of any valid token |
| Unrecognized string escape | `UnrecognizedStringEscape` | Error | `\X` inside `"..."` where X is not `"`, `\`, `n`, or `t` |
| Unrecognized typed constant escape | `UnrecognizedTypedConstantEscape` | Error | `\X` inside `'...'` where X is not `'` or `\` (note: `\n` and `\t` are also invalid here) |
| Unescaped `}` in literal | `UnescapedBraceInLiteral` | Error | A lone `}` inside a string or typed constant that was not doubled (`}}`) |

The lexer continues scanning after diagnostics to maximize token recovery for downstream error reporting.

#### Recovery rules

Each error condition has a defined recovery boundary:

| Condition | Recovery boundary | Rationale |
|-----------|-------------------|-----------|
| Unterminated string literal | Scan to end of current line; resume in `Normal` mode on the next line | Prevents the rest of the file from being consumed as string content |
| Unterminated typed constant | Scan to end of current line; resume in `Normal` mode on the next line | Same as above for `'...'` literals |
| Unterminated interpolation | Scan forward for a `}` at depth 0; if none found before end of current line, resume in the enclosing literal mode at the next line | Recovers the enclosing literal context where possible |
| Unrecognized character | Skip the single character; resume scanning at the next character | Minimal disruption — one bad character should not invalidate surrounding tokens |
| Unrecognized string escape | Skip the `\` and the following character; continue in `String` mode | Preserves surrounding content; the bad sequence is omitted from `Text` |
| Unrecognized typed constant escape | Skip the `\` and the following character; continue in `TypedConstant` mode | Same as above |
| Unescaped `}` in literal | Preserve the `}` as literal content in the token's `Text`; continue scanning | Recovers the character so surrounding content remains intact |

In all cases the invalid source span still produces a diagnostic with the correct `SourceSpan`. Post-recovery tokens are emitted normally so the parser and downstream stages can report additional errors.

---

## 2. Parser

The parser transforms the flat `TokenStream` into a `SyntaxTree` — an abstract syntax tree representing the semantic structure of the precept definition. The parser is a hand-written recursive descent parser with a Pratt expression parser for operator precedence. It produces an AST (not a CST) — comments and whitespace are consumed silently.

```
TokenStream  →  Parser.Parse  →  SyntaxTree
```

The public surface is a static class `Parser` with a single method:

```csharp
public static SyntaxTree Parse(TokenStream tokens)
```

The parser always runs to end-of-source. On malformed input it emits diagnostics and inserts `IsMissing` nodes or skips to sync points, ensuring downstream stages receive a structurally coherent tree.

### 2.1 Expression Precedence

| Precedence | Token(s) | Role | Associativity |
|:----------:|----------|------|:-------------:|
| 10 | `or` | logical disjunction | left |
| 20 | `and` | logical conjunction | left |
| 25 (prefix) | `not` | logical negation | right (prefix) |
| 30 | `==` `!=` `~=` `!~` `<` `>` `<=` `>=` | comparison | non-associative |
| 40 | `contains` | collection membership | left |
| 40 | `is` (`is set` / `is not set`) | presence test | left |
| 50 | `+` `-` (infix) | additive arithmetic | left |
| 60 | `*` `/` `%` | multiplicative arithmetic | left |
| 65 (prefix) | `-` (unary) | negation | right (prefix) |
| 80 | `.` | member access | left |
| 80 | `(` (postfix) | function/method call | left |

**Non-associative comparisons:** `A == B == C` is a parse error. The parser detects when the left operand is already a comparison expression and emits a `NonAssociativeComparison` diagnostic. (The right-binding power of 31 prevents right-associativity; the explicit left-operand check prevents left-associative chaining.)

*Implementation note:* The expression parser uses Pratt parsing (top-down operator precedence). `ParseExpression(int minBp)` parses a complete expression, stopping when it encounters a token whose left-binding power is ≤ `minBp`.

#### Null-denotation (atoms and prefix)

| Token | Production |
|-------|------------|
| `Identifier` | `IdentifierExpression` |
| `NumberLiteral` | `NumberLiteralExpression` |
| `True` / `False` | `BooleanLiteralExpression` |
| `StringLiteral` | `StringLiteralExpression` |
| `StringStart` | `InterpolatedStringExpression` (reassembly loop) |
| `TypedConstant` | `TypedConstantExpression` |
| `TypedConstantStart` | `InterpolatedTypedConstantExpression` (reassembly loop) |
| `LeftBracket` | `ListLiteralExpression` |
| `LeftParen` | `ParenthesizedExpression` |
| `Not` | `UnaryExpression(Not, ParseExpression(25))` |
| `Minus` | `UnaryExpression(Negate, ParseExpression(65))` |
| `If` | `ConditionalExpression` (`if` Expr `then` Expr `else` Expr) |
| _other_ | missing `IdentifierExpression` + diagnostic |

#### Left-denotation (infix and postfix)

| Token | Production |
|-------|------------|
| `Or` | `BinaryExpression(Or, ParseExpression(10))` |
| `And` | `BinaryExpression(And, ParseExpression(20))` |
| `==` `!=` `~=` `!~` `<` `>` `<=` `>=` | `BinaryExpression(op, ParseExpression(31))` |
| `Contains` | `ContainsExpression(left, ParseExpression(40))` |
| `Is` | `IsSetExpression` — consumes optional `Not`, then `Set` |
| `+` `-` (infix) | `BinaryExpression(op, ParseExpression(50))` |
| `*` `/` `%` | `BinaryExpression(op, ParseExpression(60))` |
| `.` (Dot) | `MemberAccessExpression(left, Identifier)` |
| `(` (LeftParen) | If `left` is `MemberAccessExpression` → `MethodCallExpression`; if `IdentifierExpression` → `CallExpression`; else → diagnostic |

### 2.2 Declaration Grammar

After the `precept <Name>` header, the parser enters a loop that dispatches on the current non-trivia token to select a declaration production.

#### Top-level dispatch

| Leading token | Production |
|---------------|-----------|
| `field` | `FieldDeclaration` |
| `state` | `StateDeclaration` |
| `event` | `EventDeclaration` |
| `rule` | `RuleDeclaration` |
| `in` | `StateEnsureDeclaration`, `AccessModeDeclaration`, or `OmitDeclaration` |
| `to` | `StateEnsureDeclaration` or `StateActionDeclaration` |
| `from` | `TransitionRowDeclaration`, `StateEnsureDeclaration`, or `StateActionDeclaration` |
| `on` | `EventEnsureDeclaration` or `EventHandlerDeclaration` |
| `EndOfSource` | exit loop |
| _anything else_ | diagnostic + sync-point resync |

#### `field` declaration

```
field Identifier ("," Identifier)* as TypeRef FieldModifier* ("->" Expr)?
```

Multi-name shorthand: `field A, B, C as string` declares three fields of the same type. Modifiers appear before the computed expression arrow. The `->` introduces a computed expression.

#### `state` declaration

```
state StateEntry ("," StateEntry)*
StateEntry  :=  Identifier ("initial")? StateModifier*
StateModifier  :=  terminal | required | irreversible | success | warning | error
```

#### `event` declaration

```
event Identifier ("," Identifier)* ("(" ArgList ")")? ("initial")?
ArgList  :=  ArgDecl ("," ArgDecl)*
ArgDecl  :=  Identifier as TypeRef FieldModifier*
```

Event arguments use parenthesized syntax. The `initial` keyword follows the argument list.

#### `rule` declaration

```
rule BoolExpr ("when" BoolExpr)? because StringExpr
```

The optional `when` guard scopes the rule to states where the guard is true.

#### `in` / `to` / `from` dispatch

These preposition keywords parse a state target, then look ahead to select the production:

| Preposition | Following verb | Production |
|-------------|---------------|-----------|
| `in` | `ensure` | state ensure (scoped to `in`) |
| `in` | `modify` | access mode (state-scoped) |
| `in` | `omit` | omit declaration (state-scoped structural exclusion) |
| `to` | `ensure` | state ensure (scoped to `to`) |
| `to` | `->` | state action (entry hook) |
| `from` | `on` | transition row |
| `from` | `ensure` | state ensure (scoped to `from`) |
| `from` | `->` | state action (exit hook) |

All three support an optional `when` guard between the state target and the verb (except `from ... on`, where the guard is inside the transition row after the event name).

#### Transition row

```
from StateTarget on Identifier ("when" BoolExpr)?
("->" ActionStatement)*
"->" Outcome

ActionStatement  :=  set Identifier "=" Expr
                  |  add Identifier Expr
                  |  remove Identifier Expr
                  |  enqueue Identifier Expr
                  |  dequeue Identifier ("into" Identifier)?
                  |  push Identifier Expr
                  |  pop Identifier ("into" Identifier)?
                  |  clear Identifier

Outcome  :=  transition Identifier
          |  no transition
          |  reject StringExpr
```

Each action and the outcome are introduced by `->`. The `->` arrow is deliberately overloaded to create a visual pipeline that reads top-to-bottom: each step in a transition — guard, actions, outcome — flows through the same arrow. The parser loops consuming `->` followed by an action keyword, and breaks out when the token after `->` is an outcome keyword.

#### State/event ensure

```
(in|to|from) StateTarget ensure BoolExpr ("when" BoolExpr)? because StringExpr
on Identifier ensure BoolExpr ("when" BoolExpr)? because StringExpr
```

#### Stateless event hook

```
on Identifier
("->" ActionStatement)*
```

Event hooks without a `when`/`ensure` continuation are parsed as stateless event hooks with an arrow-prefixed action chain.

#### State action

```
(to|from) StateTarget ("when" BoolExpr)?
("->" ActionStatement)*
```

State actions support an optional `when` guard between the state target and the action chain. The guard is passed through to the AST node.

#### Access mode and omit declaration

Two separate constructs govern per-state field access: `AccessMode` (modify + adjective + optional guard) and `OmitDeclaration` (omit, no guard). They share the `in` preposition but are distinct `ConstructKind`s with different slot sequences.

```
FieldTarget  :=  identifier ("," identifier)* | all

── AccessMode: modify (field present, access level declared) ───────────────────
in StateTarget modify Field readonly ("when" BoolExpr)?              ← singular access constraint
in StateTarget modify Field editable ("when" BoolExpr)?              ← singular access upgrade
in StateTarget modify Field { "," Field }* readonly ("when" BoolExpr)?  ← comma-separated shorthand
in StateTarget modify Field { "," Field }* editable ("when" BoolExpr)?  ← comma-separated shorthand
in StateTarget modify all readonly ("when" BoolExpr)?                ← state-scoped all
in StateTarget modify all editable ("when" BoolExpr)?                ← state-scoped all

── OmitDeclaration: omit (field structurally absent — no guard) ────────────────
in StateTarget omit Field                                            ← singular structural exclusion
in StateTarget omit Field { "," Field }*                             ← comma-separated shorthand
in StateTarget omit all                                              ← state-scoped all (no fields visible)
```

**Two verbs, two roles:**
- `modify` = constraint verb (field present, access mode applied). Takes a field target and an access mode adjective (`readonly` or `editable`). Parallel to `omit` — both are verbs, different semantics.
- `omit` = exclusion verb (field absent from state entirely). Takes a field target only — no adjective, no guard.

**Two-layer access mode composition model:**

- **Layer 1 — field-level baseline (`writable` modifier):** `writable` on a field declaration sets that field's baseline access mode to writable across all states. Fields without `writable` default to read-only.
- **Layer 2 — state-level override (`in <State> modify|omit`):** State-scoped declarations override the field's baseline for a specific (field, state) pair only. State-level always wins over the field-level baseline.
- **Undeclared (field, state) pairs** use the field's baseline: read-only for fields without `writable`, editable for fields with `writable`.

Root-level access mode declarations are **not valid syntax** — use the `writable` modifier on the field declaration for field-level mutability. All access mode overrides are state-scoped.

State-scoped access modes (`in StateTarget`) use `modify` for constraint declarations and `omit` for structural exclusion. The field target is either `all` or a comma-separated list of field names.

**Composition rules:**
1. **Field baseline** — `writable` modifier on a field declaration sets the field's default to editable across all states.
2. **D3 default** — fields without `writable` default to read-only for every (field, state) pair unless overridden by a state-scoped declaration.
3. **State-level override always wins** — an explicit `in <State> modify|omit` declaration overrides the field's baseline for that (field, state) pair only.
4a. **`readonly` and `editable` are the only guarded access modes** — guarded `editable` upgrades a read-only baseline to editable when the guard holds; guarded `readonly` downgrades a writable baseline to read-only when the guard holds; in both cases the field is always structurally present. `omit` cannot be guarded because conditional structural presence breaks static per-state field maps.
4b. **Guarded `readonly` requires a `writable` baseline** — a guarded `readonly` on a field without `writable` is a compile error (`RedundantAccessMode`); both branches would otherwise resolve to read-only, making the guard vacuous.
4c. **Unguarded declarations must change the effective mode** — `in <State> modify F editable` where `F` carries `writable` (editable is already the baseline) and `in <State> modify F readonly` where `F` lacks `writable` (read-only is the D3 default) are both compile errors (`RedundantAccessMode`). A declaration that resolves to the same mode the field already falls back to changes nothing — it is dead code. This mirrors the `RedundantModifier` pattern: declarations that have no effect are refused, not merely warned about. **`omit` is exempt** — it operates on structural presence rather than mutability and always changes the effective shape of the state, so it can never be redundant on the mutability axis. **`all` forms (`in <State> modify all readonly`) are also exempt** — a broadcast declaration's effective change depends on the current field population; applying redundancy checks to bulk forms would make valid declarations brittle as fields are added or removed.
5. **`omit` clears on state entry** — field value resets to default on any transition into an `omit` state (including self-transitions); does NOT apply to `no transition`.
6. **`set` targeting an `omit` field in the target state** is a compile error; `readonly`/`editable` do not restrict `set`.
7. **Conflicting modes** on the same (field, state) pair is a compile error.
8. **`writable` on a computed field** is a compile error (`ComputedFieldNotWritable`).
9. **`writable` on an event argument** is a compile error (`WritableOnEventArg`).

### 2.3 Type References

```
TypeRef  :=  ScalarType TypeQualifier?
          |  CollectionType
          |  ChoiceType

ScalarType  :=  string | number | integer | decimal | boolean
             |  date | time | instant | duration | period
             |  timezone | zoneddatetime | datetime
             |  money | currency | quantity | unitofmeasure
             |  dimension | price | exchangerate

CollectionType  :=  (set | queue | stack) of ScalarType TypeQualifier?
ChoiceType        :=  choice "of" ChoiceElementType "(" ChoiceValueExpr ("," ChoiceValueExpr)* ")"
ChoiceElementType :=  string | integer | decimal | number | boolean
ChoiceValueExpr   :=  StringLiteral | NumberLiteral | BooleanLiteral
TypeQualifier   :=  (in | of) Expr
```

Type qualifiers narrow the value domain: `in '<unit>'` pins to a specific unit or currency, `of '<family>'` constrains to a dimension family. A field may use `in` or `of`, not both.

**`ChoiceType` delimiter note:** The `(...)` enclosing choice values is a type-level constraint parameter — those values define the allowed domain and are part of the type itself, not a value being assigned. This is intentionally distinct from the `[...]` list literal syntax used in `default` clauses, which is a value expression. Using `(...)` here signals type parameterization; using `[...]` would create a visual collision in compound forms like `set of choice of string(...) ... default [...]` where both delimiters would appear in the same declaration for different purposes.

**`set` disambiguation:** The lexer always emits `TokenKind.Set`. In `ParseTypeRef()`, when followed by `of`, the parser treats it as the collection type. Outside type position, `set` is the action keyword.

### 2.4 Field Modifiers

Field modifiers appear after the type reference and before any computed expression.

| Modifier | Syntax | Category |
|----------|--------|----------|
| `optional` | flag | Field is nullable; use `is set`/`is not set` for presence |
| `writable` | flag | Field baseline is directly editable across all states (unless overridden per-state); invalid on computed fields and event args |
| `ordered` | flag | Choice field supports ordinal comparison |
| `nonnegative` | flag | Value ≥ 0 |
| `positive` | flag | Value > 0 |
| `nonzero` | flag | Value ≠ 0 |
| `notempty` | flag | String is non-empty |
| `default` _Expr_ | value | Default value |
| `min` _Expr_ | value | Minimum value |
| `max` _Expr_ | value | Maximum value |
| `minlength` _Expr_ | value | Minimum string length |
| `maxlength` _Expr_ | value | Maximum string length |
| `mincount` _Expr_ | value | Minimum collection count |
| `maxcount` _Expr_ | value | Maximum collection count |
| `maxplaces` _Expr_ | value | Maximum decimal places |

### 2.5 Interpolation Reassembly

The parser reassembles interpolated literals from the segmented token stream the lexer produced. Both `ParseInterpolatedString()` and `ParseInterpolatedTypedConstant()` use the same loop:

1. Consume `Start` token → `TextSegment`
2. `ParseExpression(0)` → `ExpressionSegment`
3. If `Middle` → `TextSegment`, go to step 2
4. If `End` → `TextSegment`, done

`ParseExpression(0)` terminates naturally at `StringMiddle`/`StringEnd`/`TypedConstantMiddle`/`TypedConstantEnd` because these token kinds have no binding power in the expression parser. This is the depth-unaware reassembly property: because `}` always ends an interpolation hole and has no meaning in the expression grammar, the parser stops naturally without tracking nesting depth.

### 2.6 Error Recovery

The parser uses two complementary mechanisms:

#### Missing-node insertion

When an expected token is absent, the parser emits a diagnostic and creates a synthetic token with `IsMissing = true` and a zero-length span at the current position. The resulting AST node is structurally complete. Used for: missing identifiers, missing keywords (`as`, `because`, `ensure`), missing expression atoms.

#### Sync-point resync

When the parser is structurally lost at the top level, it scans forward for a sync token:

| Sync token | Keyword |
|------------|---------|
| `Precept` | `precept` |
| `Field` | `field` |
| `State` | `state` |
| `Event` | `event` |
| `Rule` | `rule` |
| `From` | `from` |
| `To` | `to` |
| `In` | `in` |
| `On` | `on` |

These are unambiguous top-level declaration starters. Continuation tokens (`when`, `->`, `set`, `transition`, `ensure`, `because`) are never sync points — they appear mid-production and would cause the parser to skip valid content.

### 2.7 Parser Diagnostics

| Condition | Diagnostic Code | Severity | Description |
|-----------|-----------------|----------|-------------|
| Expected token not found | `ExpectedToken` | Error | "Expected {0} here, but found '{1}'" |
| Unrecognized keyword in declaration position | `UnexpectedKeyword` | Error | "'{0}' cannot appear inside a {1}" |
| Chained comparison (`A == B == C`) | `NonAssociativeComparison` | Error | "Comparisons like == and < cannot be chained — {0}" |
| Non-callable expression followed by `(` | `InvalidCallTarget` | Error | "Only built-in functions can be called this way — '{0}' is not a function name" |

---

## 3. Type Checker

The type checker transforms a `SyntaxTree` into a `SemanticIndex` — a flat collection of symbol tables, resolved declarations, and diagnostics. It always produces a result, even on broken input (the pipeline's resilient contract).

```
SyntaxTree  →  TypeChecker.Check  →  SemanticIndex
```

The public surface is a static method:

```csharp
public static SemanticIndex Check(SyntaxTree tree)
```

### 3.1 Processing Model

The type checker makes two passes over the declaration list:

1. **Registration pass.** Walk all declarations, build symbol tables: field names → types, state names, event names → argument types. No expression checking. This pass ensures all names are available regardless of declaration order.
2. **Checking pass.** Walk all declarations again, resolve expressions, validate types, emit diagnostics.

Declaration order does not matter. A rule at line 5 can reference a field declared at line 50. A transition row can reference states declared after it.

### 3.2 Type Widening Rules

Implicit widening is lossless and one-directional. Only two implicit widenings exist:

```
integer  →  decimal     (implicit — lossless)
integer  →  number      (implicit — lossless, direct)
```

- `integer` widens to `decimal` — every integer is exactly representable in base-10.
- `integer` widens to `number` — every integer within safe integer range is exactly representable as an IEEE 754 double. This is a direct widening, not transitive through `decimal`.
- `decimal` does **NOT** implicitly widen to `number` in any context. The conversion is lossy — IEEE 754 cannot exactly represent all base-10 values (`0.1 + 0.2 ≠ 0.3`). Use `approximate(decimalValue)` to explicitly convert `decimal → number`, or `round(numberValue, places)` to convert `number → decimal`. See §3.7 for bridge function signatures.
- No implicit narrowing. A `number` value cannot be assigned to an `integer` or `decimal` field without an explicit bridge function (`round`, `floor`, `ceil`, `truncate`).

**Widening applies in these contexts:**

| Context | Example |
|---------|---------|
| Assignment | `set IntegerField = ...` where the RHS is `integer` and the field is `decimal` |
| Binary operators | `IntegerField + DecimalField` — `integer` widens to `decimal`, result is `decimal` |
| Function arguments | `min(IntegerExpr, DecimalExpr)` — `integer` widens to `decimal` |
| Default values | `field X as decimal default 42` — `42` widens to `decimal` |
| Comparison | `IntegerField > DecimalField` — `integer` widens, comparison is valid |

For the complete conversion map — including the context-by-context matrix, bridge function catalog, and rationale for why comparisons also require bridging — see [Primitive Types · Numeric Lane Rules](primitive-types.md#numeric-lane-rules).

### 3.3 Context-Sensitive Type Resolution

Multiple literal forms produce tokens whose specific type cannot be determined at lex time. The type checker resolves these uniformly using expression context: field type, assignment target, binary operator peer, function argument position, constraint value position, default value position.

**Uniform rule:** If no context is available for any context-dependent literal, the type checker emits a diagnostic and assigns `ErrorType`. No implicit fallback.

#### Numeric literals

A `NumberLiteral` token does not carry an inherent numeric lane. Context determines the type.

| Literal form | Valid target types | Resolution rule |
|---|---|---|
| Whole number (`42`) | `integer`, `decimal`, `number` | Context determines. If target is `integer`, resolves as `integer`. If `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. |
| Fractional (`3.14`) | `decimal`, `number` | If target is `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. Type error if target is `integer`. |
| Exponent (`1.5e2`) | `number` only | Always `number`. Type error if target is `integer` or `decimal`. |

No literal suffix syntax exists — context is the sole resolution mechanism.

#### Typed constants

A `TypedConstant` token's content is opaque to the lexer. The type checker resolves the type using context-born resolution — the same model as numeric literals:

1. **Context determines the type.** The expression context (field type, operator peer, function signature, comparison operand) propagates an expected type inward. This is the same top-down inference that resolves `42` to `integer`, `decimal`, or `number`.
2. **Content is validated against the expected type.** The content is parsed and validated as a value of the context-determined type. Invalid content is a compile error.
3. **No context → compile error.** A typed constant in a position with no type expectation is a compile error.

**Content validation table** — given context-determined type, valid content patterns:

| Expected type | Valid content | Examples |
|---|---|---|
| `date` | `YYYY-MM-DD` | `'2026-04-15'` |
| `time` | `HH:MM:SS` or `HH:MM` | `'14:30:00'` |
| `instant` | ISO 8601 with `T`, trailing `Z` | `'2026-04-15T14:30:00Z'` |
| `datetime` | ISO 8601 with `T`, no zone | `'2026-04-15T14:30:00'` |
| `zoneddatetime` | ISO 8601 with `T`, `[Zone]` bracket | `'2026-04-15T14:30:00[America/New_York]'` |
| `timezone` | `Word/Word` IANA identifier | `'America/New_York'` |
| `duration` | `<integer> <temporal-unit>` (with optional `+`) | `'72 hours'` |
| `period` | `<integer> <temporal-unit>` (with optional `+`) | `'30 days'`, `'2 years + 6 months'` |
| `money` | `<number> <ISO-4217-code>` | `'100 USD'` |
| `quantity` | `<number> <unit-name>` | `'5 kg'` |
| `price` | `<number> <currency>/<unit>` | `'4.17 USD/each'` |
| `exchangerate` | `<number> <currency>/<currency>` | `'1.08 USD/EUR'` |
| `currency` | `<ISO-4217-code>` (3-letter) | `'USD'` |
| `unitofmeasure` | Unit name | `'kg'` |
| `dimension` | Dimension name (UCUM registry) | `'mass'` |

### 3.4 Name Resolution

All names are registered in the first pass. The checking pass validates every reference against the symbol tables.

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Duplicate field name | Two `field` declarations declare the same name | `DuplicateFieldName` |
| Duplicate state name | Two state entries have the same name | `DuplicateStateName` |
| Duplicate event name | Two `event` declarations share a name | `DuplicateEventName` |
| Duplicate event arg | Two args in the same event have the same name | `DuplicateArgName` |
| Undeclared field reference | `IdentifierExpression` in expression context does not match a field name (or in-scope event arg) | `UndeclaredField` |
| Undeclared state reference | State name in `from`/`to`/`in` target or `transition` outcome does not match a declared state | `UndeclaredState` |
| Undeclared event reference | Event name in `from ... on`, `on` ensure, or event handler does not match a declared event | `UndeclaredEvent` |
| Multiple initial states | More than one state entry has `initial` | `MultipleInitialStates` |
| No initial state | Stateful precept (has states) but none is marked `initial` | `NoInitialState` |

### 3.5 Scope Rules

Precept has a small, well-defined scope model. There are no nested scopes, no imports, no modules.

#### Global scope

Fields, states, and events are all declared at the top level. They are visible everywhere in the precept body. Order of declaration does not matter (see §3.1 — registration pass).

#### Expression scope

| Context | What's in scope |
|---------|----------------|
| Rule condition / guard | All field names |
| Ensure condition / guard | All field names |
| Transition row guard | All field names + current event's args (via `EventName.ArgName`) |
| Transition row actions (RHS of `set`, value of `add`/`enqueue`/`push`) | All field names + current event's args |
| State action guard / actions | All field names |
| Event handler actions | All field names + current event's args |
| Default value expression | Field names declared **before** this field (no self-reference, no forward reference) |
| Computed expression (`field X as T -> Expr`) | All field names except those that would form a dependency cycle (no self-reference, no mutual cycles) |
| Modifier value expressions (`min N`, `max N`, etc.) | Only literal values — no field references |

#### Event arg access

Event args are accessed via dotted notation: `EventName.ArgName`. The type checker resolves this by:

1. Checking if the object of a `MemberAccessExpression` is an `IdentifierExpression` that matches a declared event name.
2. If so, the member is resolved against the event's arg declarations.
3. Event arg access is only valid in contexts where an event is in scope (transition rows, event ensures, event handlers).

### 3.6 Expression Typing Rules

#### Binary operators

**Core scalar operators:**

| Operator | Left type | Right type | Result type | Widening? |
|----------|-----------|------------|-------------|-----------|
| `+` `-` `*` `/` `%` | numeric | numeric | common numeric type | Yes — widen to common |
| `+` | `string` | `string` | `string` | No (concatenation) |
| `==` `!=` | any T | same T | `boolean` | Yes — `integer` widens to `decimal` or `number`; `decimal` vs `number` is a type error (see §3.2) |
| `~=` `!~` | `string` | `string` | `boolean` | No — case-insensitive ordinal comparison (`OrdinalIgnoreCase`); type error on non-string operands |
| `<` `>` `<=` `>=` | numeric | numeric | `boolean` | Yes — `integer` widens to `decimal` or `number`; `decimal` vs `number` is a type error (see §3.2) |
| `<` `>` `<=` `>=` | `string` | `string` | `boolean` | No (lexicographic) |
| `<` `>` `<=` `>=` | `choice of T` (ordered) | `choice of T` (ordered, same element type, order-preserving subsequence) | `boolean` | No (declaration-position rank) |
| `and` `or` | `boolean` | `boolean` | `boolean` | No |

**Common numeric type:** When two numeric operands have different lanes, the result is the wider type: `integer op decimal` → `decimal`; `integer op number` → `number`. However, `decimal op number` is a **type error** — the author must use an explicit bridge function (`approximate(decimalValue)` to convert to `number`, or `round(numberValue, places)` to convert to `decimal`). There is no implicit `decimal → number` widening in any context — the conversion is lossy. See [Primitive Types · Numeric Lane Rules](primitive-types.md#numeric-lane-rules) for the complete conversion map and §3.7 for bridge function signatures.

**Temporal operators** — see the [temporal type system](temporal-type-system.md#semantic-rules) for the full per-type operator matrix. Summary:

| Left | Op | Right | Result | Notes |
|------|----|-------|--------|-------|
| `date` | `±` | `period of 'date'` | `date` | Unconstrained period → `UnqualifiedPeriodArithmetic`. |
| `date` | `-` | `date` | `period` | Calendar distance. |
| `date` | `+` | `time` | `datetime` | Composition. Commutative. |
| `time` | `±` | `period of 'time'` | `time` | Unconstrained period → `UnqualifiedPeriodArithmetic`. |
| `time` | `±` | `duration` | `time` | Sub-day bridging. Wraps at midnight. |
| `time` | `-` | `time` | `period` | |
| `instant` | `-` | `instant` | `duration` | |
| `instant` | `±` | `duration` | `instant` | |
| `datetime` | `±` | `period` | `datetime` | Accepts all period components. |
| `datetime` | `-` | `datetime` | `period` | |
| `duration` | `±` | `duration` | `duration` | |
| `duration` | `*` `/` | `integer` or `number` | `duration` | Scaling. `decimal` is a type error. Commutative for `*`. |
| `duration` | `/` | `duration` | `number` | Ratio. |
| `period` | `±` | `period` | `period` | |
| `zoneddatetime` | `±` | `duration` | `zoneddatetime` | Timeline arithmetic. |
| `zoneddatetime` | `-` | `zoneddatetime` | `duration` | |

**Temporal comparison:** `date`, `time`, `instant`, `duration`, `datetime` support all comparison operators. `period`, `timezone`, and `zoneddatetime` support only `==`/`!=` — ordering operators are type errors. Cross-type temporal comparison is always a type error.

**Business-domain operators** — see the [business-domain types](business-domain-types.md) for the full per-type operator matrix with cancellation rules. Summary:

| Left | Op | Right | Result | Notes |
|------|----|-------|--------|-------|
| `money` | `±` | `money` | `money` | Same currency required. |
| `money` | `*` `/` | `decimal` | `money` | Commutative for `*`. `number` is a type error. |
| `money` | `/` | `money` (same curr.) | `decimal` | Dimensionless ratio. |
| `money` | `/` | `money` (diff. curr.) | `exchangerate` | |
| `money` | `/` | `quantity` / `period` / `duration` | `price` | Price derivation. |
| `quantity` | `±` | `quantity` | `quantity` | Same dimension required. |
| `quantity` | `*` `/` | `decimal` | `quantity` | Commutative for `*`. |
| `quantity` | `/` | `quantity` (same dim.) | `decimal` | |
| `quantity` | `/` | `quantity` (diff. dim.) | `quantity` (compound) | |
| `price` | `*` | `quantity` / `period` / `duration` | `money` | Dimensional cancellation. Commutative. |
| `price` | `*` `/` | `decimal` | `price` | Commutative for `*`. |
| `price` | `±` | `price` | `price` | Same currency and unit required. |
| `exchangerate` | `*` | `money` | `money` | Currency conversion. Commutative. |
| `exchangerate` | `*` `/` | `decimal` | `exchangerate` | Commutative for `*`. |

**Business-domain comparison:** `money`, `quantity`, `price` support all comparison operators (same currency/dimension/unit required). `exchangerate`, `currency`, `unitofmeasure`, `dimension` support only `==`/`!=` — ordering operators are type errors.

#### Unary operators

| Operator | Operand type | Result type |
|----------|-------------|-------------|
| `not` | `boolean` | `boolean` |
| `-` (negate) | numeric | same numeric type |
| `-` (negate) | `duration` | `duration` |
| `-` (negate) | `money` | `money` (preserves currency) |
| `-` (negate) | `quantity` | `quantity` (preserves unit/dimension) |
| `-` (negate) | `price` | `price` (preserves currency/unit) |

#### `contains`

| Collection type | Value type | Result |
|-----------------|-----------|--------|
| `set of T` | `T` (or widens to `T`) | `boolean` |
| `queue of T` | `T` | `boolean` |
| `stack of T` | `T` | `boolean` |
| non-collection | — | type error |

#### `is set` / `is not set`

| Operand | Valid? | Result |
|---------|--------|--------|
| `optional` field | Yes | `boolean` |
| Non-optional field | Type error — field always has a value | — |

#### Conditional (`if ... then ... else ...`)

The `then` and `else` branches must have compatible types (same type, or one widens to the other). The result type is the common type.

#### Member access (`.`)

**Collection and core accessors:**

| Object type | Member | Result type |
|-------------|--------|-------------|
| `set of T` | `count` | `integer` |
| `set of T` (T orderable) | `min` | `T` |
| `set of T` (T orderable) | `max` | `T` |
| `queue of T` | `count` | `integer` |
| `queue of T` | `peek` | `T` |
| `stack of T` | `count` | `integer` |
| `stack of T` | `peek` | `T` |
| `string` | `length` | `integer` |
| Event arg reference (`EventName.ArgName`) | — | arg's declared type |

**Temporal accessors** — see the [temporal type system](temporal-type-system.md) for the full per-type accessor tables. Summary: `date` has `.year`, `.month`, `.day`, `.dayOfWeek` → `integer`. `time` has `.hour`, `.minute`, `.second` → `integer`. `instant` has only `.inZone(tz)` → `zoneddatetime` (no skip-level accessors). `duration` has `.totalDays`, `.totalHours`, `.totalMinutes`, `.totalSeconds` → `number`. `period` has `.years`, `.months`, `.weeks`, `.days`, `.hours`, `.minutes`, `.seconds` → `integer`; `.hasDateComponent`, `.hasTimeComponent` → `boolean`; `.basis` → `string`; `.dimension` → `dimension`. `zoneddatetime` has `.instant`, `.timezone`, `.datetime`, `.date`, `.time` and integer component accessors. `datetime` has `.date`, `.time`, `.inZone(tz)`, and integer component accessors.

**Business-domain accessors** — see the [business-domain types](business-domain-types.md#accessors-per-type) for the full accessor table. Summary: `money` has `.amount` → `decimal`, `.currency` → `currency`. `quantity` has `.amount` → `decimal`, `.unit` → `unitofmeasure`, `.dimension` → `dimension`. `price` has `.amount` → `decimal`, `.currency` → `currency`, `.unit` → `unitofmeasure`, `.dimension` → `dimension`. `exchangerate` has `.amount` → `decimal`, `.from`/`.to` → `currency`. `unitofmeasure` has `.dimension` → `dimension`. `period` also has `.basis` → `string` and `.dimension` → `dimension` for its `in`/`of` qualification system.

| _other_ | — | `InvalidMemberAccess` diagnostic |

#### Function calls

See §3.7 for the complete built-in function catalog.

#### Parenthesized expressions

Type is the type of the inner expression. Transparent.

#### String interpolation

Each `{expr}` inside `"..."` is type-checked independently. Any scalar type is coercible to string. Collections are a type error inside string interpolation.

#### Typed constant interpolation

Each `{expr}` inside `'...'` is type-checked independently. After interpolation expressions are typed, the full content is validated against the context-determined type as described in §3.3.

### 3.7 Built-in Function Catalog

Functions are validated against a closed catalog. There are no user-defined functions, no registration mechanism, no extension point.

| Function | Signature | Return type | Constraints |
|----------|-----------|-------------|-------------|
| `min(a, b)` | `(numeric, numeric) → numeric` | Common numeric type of args | — |
| `max(a, b)` | `(numeric, numeric) → numeric` | Common numeric type of args | — |
| `abs(value)` | `(numeric) → numeric` | Same numeric type as input | — |
| `clamp(value, lo, hi)` | `(numeric, numeric, numeric) → numeric` | Common numeric type | — |
| `floor(value)` | `(decimal\|number) → integer` | `integer` | — |
| `ceil(value)` | `(decimal\|number) → integer` | `integer` | — |
| `truncate(value)` | `(decimal\|number) → integer` | `integer` | — |
| `round(value)` | `(decimal\|number) → integer` | `integer` | Banker's rounding |
| `round(value, places)` | `(numeric, integer) → decimal` | `decimal` | `places` must be non-negative integer; **explicit bridge: number→decimal** |
| `approximate(value)` | `(decimal) → number` | `number` | **Explicit bridge: decimal→number**; makes precision loss visible |
| `pow(base, exp)` | `(numeric, integer) → numeric` | Same numeric type as `base` | `exp` must be non-negative for integer lane |
| `sqrt(value)` | `(numeric) → number` | `number` | Number-lane only; proof engine checks non-negativity |
| `trim(value)` | `(string) → string` | `string` | — |
| `startsWith(s, prefix)` | `(string, string) → boolean` | `boolean` | Case-sensitive prefix test |
| `endsWith(s, suffix)` | `(string, string) → boolean` | `boolean` | Case-sensitive suffix test |
| `toLower(s)` | `(string) → string` | `string` | Lowercase (invariant culture) |
| `toUpper(s)` | `(string) → string` | `string` | Uppercase (invariant culture) |
| `left(s, n)` | `(string, integer) → string` | `string` | Leftmost N code units (clamped to string length) |
| `right(s, n)` | `(string, integer) → string` | `string` | Rightmost N code units (clamped to string length) |
| `mid(s, start, length)` | `(string, integer, integer) → string` | `string` | 1-indexed substring (clamped); `start` and `length` must be positive `integer` |
| `now()` | `() → instant` | `instant` | — |

**Lane bridge functions.** Two functions are the sole explicit bridges between numeric lanes: `approximate(decimal) → number` and `round(value, places) → decimal`. The rounding family (`floor`, `ceil`, `truncate`, `round` with no places) provide `decimal|number → integer`. No other mechanism crosses lane boundaries — `decimal * NumberField` without `approximate()` is a type error (see type-checker.md §4.2a).

**Function validation checks:**

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Unknown function name | `foo(...)` where `foo` is not in the catalog | `UndeclaredFunction` |
| Wrong arity | `min(a)` or `min(a, b, c)` | `FunctionArityMismatch` |
| Arg type mismatch | `min("a", "b")` — strings to numeric function | `TypeMismatch` |
| Arg constraint violation | `round(x, -1)` — negative places | `FunctionArgConstraintViolation` |

### 3.8 Semantic Checks

#### Type compatibility

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Assignment type mismatch | `set Field = Expr` where `Expr`'s type is not assignable to `Field`'s type (after widening) | `TypeMismatch` |
| Guard not boolean | `when Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| Rule condition not boolean | `rule Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| Ensure condition not boolean | `ensure Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| Message not string | `because Expr` or `reject Expr` where `Expr` is not `string` | `TypeMismatch` |
| Binary operator type error | Operator applied to incompatible types (e.g., `string + boolean`) | `TypeMismatch` |
| Comparison on unordered choice | `<` / `>` / `<=` / `>=` on a `choice` field without the `ordered` modifier | `TypeMismatch` |
| Conditional branch mismatch | `if ... then A else B` where A and B have no common type | `TypeMismatch` |
| Default value type mismatch | `default Expr` where `Expr`'s type is incompatible with the field type | `TypeMismatch` |
| Collection element type mismatch | `add Field Expr` where `Expr`'s type doesn't match the collection's element type | `TypeMismatch` |
| Numeric literal incompatible | Fractional literal in `integer` context, or exponent literal in `integer`/`decimal` context | `TypeMismatch` |

#### Modifier validation

Modifiers are constraints on field/arg values. The type checker validates applicability:

| Modifier | Applicable to | Error when applied to |
|----------|---------------|----------------------|
| `writable` | any non-computed field type (field declarations only) | computed fields (`ComputedFieldNotWritable`); event arguments (`WritableOnEventArg`) |
| `nonnegative` | `integer`, `decimal`, `number` | `string`, `boolean`, `choice`, collections, temporal, domain |
| `positive` | `integer`, `decimal`, `number` | (same as above) |
| `nonzero` | `integer`, `decimal`, `number` | (same as above) |
| `notempty` | `string` | `number`, `integer`, `decimal`, `boolean`, `choice`, collections |
| `min` / `max` | `integer`, `decimal`, `number` | `string`, `boolean`, collections |
| `minlength` / `maxlength` | `string` | `number`, `integer`, `decimal`, `boolean`, collections |
| `mincount` / `maxcount` | `set`, `queue`, `stack` | scalars |
| `maxplaces` | `decimal` | `integer`, `number`, `string`, `boolean`, collections |
| `ordered` | `choice` | all non-choice types |
| `optional` | any field type | — (always valid) |

**Modifier value validation:**

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| `min` > `max` | `min` value exceeds `max` value on the same field | `InvalidModifierBounds` |
| `minlength` > `maxlength` | `minlength` exceeds `maxlength` | `InvalidModifierBounds` |
| `mincount` > `maxcount` | `mincount` exceeds `maxcount` | `InvalidModifierBounds` |
| Negative count/length/places | `minlength`/`maxlength`/`mincount`/`maxcount`/`maxplaces` is negative | `InvalidModifierValue` |
| `maxplaces` not integer | Decimal places must be a whole number | `InvalidModifierValue` |
| Duplicate modifier | Same modifier applied twice to one field | `DuplicateModifier` |
| Redundant modifier | `nonnegative` and `positive` on the same field (`positive` subsumes `nonnegative`) | `RedundantModifier` (warning) |

#### Action statement validation

| Action | Field type required | Value type required | Additional checks |
|--------|--------------------|--------------------|-------------------|
| `set F = Expr` | Any scalar | Assignable to field type | Field must not be computed |
| `add F Expr` | `set of T` | `T` | — |
| `remove F Expr` | `set of T` | `T` | — |
| `enqueue F Expr` | `queue of T` | `T` | — |
| `dequeue F (into G)?` | `queue of T` | — | If `into G`, `G` must be type `T`. Requires emptiness proof (`UnguardedCollectionMutation`) |
| `push F Expr` | `stack of T` | `T` | — |
| `pop F (into G)?` | `stack of T` | — | If `into G`, `G` must be type `T`. Requires emptiness proof (`UnguardedCollectionMutation`) |
| `clear F` | Any collection | — | — |

Type errors: applying a set operation to a non-set field, a queue operation to a non-queue field, etc.

#### Access mode validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Field not declared | Access mode names a field that doesn't exist | `UndeclaredField` |
| State not declared | Access mode scoped to a state that doesn't exist | `UndeclaredState` |
| Computed field in editable mode | A computed field is listed in a `modify ... editable` access mode declaration | `ComputedFieldNotWritable` |
| `writable` on computed field | A computed field carries the `writable` modifier | `ComputedFieldNotWritable` |
| `writable` on event arg | An event argument carries the `writable` modifier | `WritableOnEventArg` |
| Conflicting access modes | Same field has both `modify` and `omit` in the same state | `ConflictingAccessModes` |
| Redundant access mode (unguarded) | `in <State> modify F editable` where `F` has `writable` (baseline already editable), or `in <State> modify F readonly` where `F` lacks `writable` (baseline already read-only); named-field forms only | `RedundantAccessMode` (error) |
| Redundant access mode (guarded) | `in <State> modify F readonly when Guard` where `F` lacks `writable` — guard-true branch = read-only, guard-false branch = read-only (D3 baseline); the guard changes nothing | `RedundantAccessMode` (error) |

#### Computed field validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Self-reference | Computed expression references its own field | `CircularComputedField` |
| Transitive cycle | Computed fields form a dependency cycle (A→B→A, or A→B→C→A, etc.) | `CircularComputedField` |
| Expression type mismatch | Computed expression type doesn't match field type | `TypeMismatch` |
| Computed with default | Field has both `->` and `default` | `ComputedFieldWithDefault` |
| Computed as write target | `set` action targets a computed field | `ComputedFieldNotWritable` |

#### Choice type validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Missing element type | `choice("a", "b")` — no `of T` | `ChoiceMissingElementType` |
| Empty choice | `choice of string()` — no values | `EmptyChoice` |
| Duplicate choice value | `choice of string("a", "a")` | `DuplicateChoiceValue` |
| Wrong literal kind | `choice of integer("not-a-number")` | `ChoiceElementTypeMismatch` |
| Non-choice assigned to choice | `set Priority = someStringVar` | `NonChoiceAssignedToChoice` |
| Choice literal not in set | `set Priority = "Unknown"` where `"Unknown"` not declared | `ChoiceLiteralNotInSet` |
| Choice arg outside field set | Arg `choice("Low")` supplied to `choice("Low","Med","High")` field — values outside | `ChoiceArgOutsideFieldSet` |
| Element type mismatch | `choice of integer` arg to `choice of string` field | `ChoiceElementTypeMismatch` |
| Rank conflict | `choice("Med","Low")` arg to `choice("Low","Med","High")` field — order not preserved | `ChoiceRankConflict` |

**v1 limits:** Negative numeric literals (e.g., `choice of integer(-1, 0, 1)`) are supported via parser constant-folding. Typed choice nested inside a collection element type (e.g., `set of choice of string(...)`) is not supported in v1 — the inner type must be a simple scalar.

#### List literal validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Element type mismatch | List element type doesn't match collection element type | `TypeMismatch` |
| List in non-default position | List literal used outside a `default` clause | `ListLiteralOutsideDefault` |
| Empty list as default | Valid — empty collection | — |

#### Transition outcome validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Undeclared target state | `transition StateName` where `StateName` is not declared | `UndeclaredState` |
| Reject message not string | `reject Expr` where `Expr` is not string | `TypeMismatch` |

#### Stateless/stateful cross-validation

A precept that contains both `EventHandlerDeclaration` nodes (`on Event -> actions`) and any `state` declarations is an error. In a stateful precept, event handlers are redundant with `from any on Event -> no transition` followed by rules. Mixing the two creates ambiguity about execution order.

A stateless precept (no states, no `from`, no transitions) that uses only event hooks is valid.

### 3.9 Error Recovery

#### ErrorType propagation

When the type checker encounters an unresolvable expression (missing node, undeclared name, type error in a sub-expression), it assigns `ErrorType`. `ErrorType` is compatible with every other type for the purpose of further checking — it suppresses all downstream type errors that would cascade from the original failure.

**Rules:**

1. Any operation involving `ErrorType` produces `ErrorType`.
2. `ErrorType` satisfies any type constraint — no further diagnostics are emitted for expressions that already carry `ErrorType`.
3. `ErrorType` never appears in a valid program. It only exists in the presence of other diagnostics.

#### Handling `IsMissing` AST nodes

| Node category | Recovery behavior |
|---|---|
| Declaration with `IsMissing` name | Skip — do not add to symbol table. Parser already emitted a diagnostic. |
| Expression with `IsMissing` | Assign `ErrorType`. No diagnostic emitted (parser already reported it). |
| TypeRef with `IsMissing` | Resolve to `ErrorType`. Fields with error types still appear in the symbol table but their type is `ErrorType`. |
| Guard with `IsMissing` subexpression | Guard is assigned `ErrorType`. The transition row is still processed — other checks continue. |
| Missing state/event name tokens | Skip the containing declaration. |

#### One diagnostic per root cause

The type checker emits diagnostics for root causes only. When `ErrorType` is flowing through an expression tree, the type checker stays silent. The first diagnostic emitted for a given expression chain is the root cause; all subsequent type mismatches involving `ErrorType` are symptoms.

### 3.10 Diagnostic Catalog

#### Existing codes (already in `DiagnosticCode.cs`)

| Code | Severity | Message template | Fires when |
|------|----------|------------------|------------|
| `UndeclaredField` | Error | "Field '{0}' is not declared" | Identifier in expression context doesn't match any field |
| `TypeMismatch` | Error | "Expected a {0} value here, but got '{1}'" | Type incompatibility in any expression context |
| `NullInNonNullableContext` | Error | "'{0}' requires a value and cannot be empty here" | Optional field used where value is required |
| `InvalidMemberAccess` | Error | "'.{0}' is not available on {1} fields" | Dot access on unsupported type |
| `FunctionArityMismatch` | Error | "'{0}' takes {1} inputs, but {2} were provided" | Wrong number of function arguments |
| `FunctionArgConstraintViolation` | Error | "Value {0} for '{1}' is not valid: {2}" | Function arg violates constraint |

#### New codes

| Code | Severity | Message template | Fires when |
|------|----------|------------------|------------|
| `DuplicateFieldName` | Error | "Field '{0}' is already declared" | Two field declarations with same name |
| `DuplicateStateName` | Error | "State '{0}' is already declared" | Duplicate state entry |
| `DuplicateEventName` | Error | "Event '{0}' is already declared" | Duplicate event declaration |
| `DuplicateArgName` | Error | "Argument '{0}' is already declared on event '{1}'" | Duplicate arg in same event |
| `UndeclaredState` | Error | "State '{0}' is not declared" | Reference to non-existent state |
| `UndeclaredEvent` | Error | "Event '{0}' is not declared" | Reference to non-existent event |
| `UndeclaredFunction` | Error | "'{0}' is not a recognized function" | Unknown function name in call |
| `MultipleInitialStates` | Error | "Only one state can be marked 'initial' — '{0}' and '{1}' both are" | Two or more initial states |
| `NoInitialState` | Error | "This precept has states but none is marked 'initial'" | Stateful precept without initial |
| `InvalidModifierForType` | Error | "The '{0}' constraint does not apply to {1} fields" | Modifier on inapplicable type |
| `InvalidModifierBounds` | Error | "{0} ({1}) cannot exceed {2} ({3})" | min > max, minlength > maxlength, etc. |
| `InvalidModifierValue` | Error | "The value for '{0}' must be {1}" | Negative count/length, non-integer maxplaces |
| `DuplicateModifier` | Error | "The '{0}' constraint is already applied to this field" | Same modifier twice |
| `RedundantModifier` | Warning | "'{0}' is unnecessary — '{1}' already implies it" | nonnegative + positive |
| `ComputedFieldNotWritable` | Error | "Field '{0}' is computed and cannot be assigned" | `set` targeting computed field, `modify ... editable` access mode on computed field, or `writable` modifier on computed field |
| `ComputedFieldWithDefault` | Error | "Field '{0}' is computed and cannot have a default value" | Both `->` and `default` |
| `CircularComputedField` | Error | "Computed field '{0}' has a circular dependency: {1}" | Self-reference or transitive cycle in computed field dependency graph |
| `ConflictingAccessModes` | Error | "Field '{0}' has conflicting access modes in state '{1}'" | modify + omit same field same state |
| `RedundantAccessMode` | Error | "The '{0}' access mode for field '{1}' in state '{2}' is redundant — the effective mode is already '{0}'" | `in S modify F editable` where F has `writable`; `in S modify F readonly` where F lacks `writable`; `in S modify F readonly when Guard` where F lacks `writable` |
| `WritableOnEventArg` | Error | "The 'writable' modifier cannot appear on event argument '{0}'" | `writable` on an event arg declaration |
| `ListLiteralOutsideDefault` | Error | "List values can only appear in default clauses" | `[...]` outside default position |
| `DuplicateChoiceValue` | Error | "Choice value '{0}' is duplicated" | Repeated value in choice set |
| `EmptyChoice` | Error | "A choice type must have at least one value" | `choice of T()` with no args |
| `ChoiceMissingElementType` | Error | "A choice type requires an explicit element type — use 'choice of string(...)', 'choice of integer(...)', etc." | `choice(...)` without `of T` |
| `ChoiceElementTypeMismatch` | Error | "Expected a {0} literal — this choice is declared as 'choice of {0}'" | Literal kind doesn't match declared element type, or `choice of integer` arg to `choice of string` field |
| `NonChoiceAssignedToChoice` | Error | "Field '{0}' is a choice type — only choice-compatible values can be assigned to it" | Non-choice source assigned to choice field |
| `ChoiceLiteralNotInSet` | Error | "'{0}' is not a declared value of '{1}'" | Literal not in field's declared set |
| `ChoiceArgOutsideFieldSet` | Error | "Argument choice includes '{0}', which is not in field '{1}'" | Arg choice has values outside field's set |
| `ChoiceRankConflict` | Error | "The order of values in this argument conflicts with the declared order of '{0}'" | Arg choice sequence is not an order-preserving subsequence of field's sequence |
| `CollectionOperationOnScalar` | Error | "'{0}' is a {1} operation, but '{2}' is not a {1}" | add/remove on non-set, etc. |
| `ScalarOperationOnCollection` | Error | "'{0}' cannot be used with collection field '{1}'" | set = on collection field |
| `IsSetOnNonOptional` | Error | "'{0}' always has a value — 'is set' only works on optional fields" | is set / is not set on required field |
| `EventArgOutOfScope` | Error | "Event '{0}' arguments are not accessible here" | Event.Arg access outside transition/ensure/stateless event |
| `InvalidInterpolationCoercion` | Error | "A {0} value cannot appear inside a text interpolation" | Collection in `{...}` inside string |
| `UnresolvedTypedConstant` | Error | "Cannot determine the type of '{0}' — the content does not match any known value pattern" | Typed constant shape doesn't match any family |
| `AmbiguousTypedConstant` | Error | "'{0}' could be a {1} or {2} — add context to disambiguate" | Multi-member family, no context to narrow |
| `DefaultForwardReference` | Error | "Default value for '{0}' cannot reference '{1}', which is declared later" | Field default references later field |
| `EventHandlerInStatefulPrecept` | Error | "Event handlers cannot appear in a precept with state declarations" | `on Event ->` mixed with `state` declarations |

---

## 3A. Language Semantics

This section defines the semantic model that sits between mechanical type checking (§3) and structural graph analysis (§4). It specifies the meaning of constraint surfaces, outcome verdicts, mutation guarantees, entity construction, and inspection — the concepts that give the language its governance identity beyond syntax and types.

### 3A.1 Constraint Semantics

The language distinguishes categories of truth.

#### Rules

`rule` expresses global data truth — constraints that hold after every mutation.

Rules support optional guards:

```precept
rule Score >= 680 because "Credit score too low"
rule DownPayment >= RequestedAmount * 0.20 when LoanType == "conventional" because "Conventional loans require 20% down"
```

A guarded rule applies only when its guard is true. This is conditional constraint scoping — the rule is precise about where the domain says it should apply — not a weakening of the guarantee. The engine ensures the rule holds in every configuration where its guard is satisfied.

Rules operate in field scope. They cannot reference event arguments — this ensures reusability across all events.

#### Ensures

`ensure` expresses contextual truth — constraints scoped to a specific state or event.

Ensures also support optional guards:

```precept
in Review ensure Reviewers.count >= 2 because "Review requires at least two reviewers"
in Open when Escalated ensure Priority >= 3 because "Escalated tickets must be high priority"
on Submit when Submit.Type == "payment" ensure Submit.Amount > 0 because "Payment amounts must be positive"
```

The surface includes these anchors:

1. `in <State> ensure ...` — residency truth. The constraint holds for every operation while the entity is in this state.
2. `to <State> ensure ...` — entry truth. The constraint is checked on any transition into this state.
3. `from <State> ensure ...` — exit truth. The constraint is checked on any transition out of this state.
4. `on <Event> ensure ...` — event-argument truth. The constraint validates the caller-provided event arguments.

#### Rejections

`reject` is authored prohibition, not failed data truth. This is a designed prohibition in the definition, not a constraint violation — it means the author deliberately forbade this outcome.

The distinction between rejection and constraint failure is semantically significant. A rejection says "the definition explicitly disallows this"; a constraint failure says "the data would violate a declared truth." They require different responses from callers and different diagnostic framing.

#### Guards

`when` guards are routing logic, not constraints. They do not produce violations. A guard that evaluates to false simply means the row does not match — the runtime moves to the next row. Only the row that actually fires (or an explicit `reject` fallback) produces outcomes. This is a fundamental language semantic: guards select; constraints enforce.

#### Collect-all vs first-match

The language makes a semantic distinction between:

1. **Validation surfaces,** which are collect-all. Rules and ensures are evaluated exhaustively — every applicable constraint is checked, and all violations are reported. The caller receives the complete set of failures, not just the first one encountered.
2. **Routing surfaces,** which are first-match and order-sensitive. Transition rows are evaluated in declaration order — the first matching guard wins, and remaining rows are not evaluated.

This distinction is a language design choice, not an optimization. Validation must be exhaustive because partial feedback is useless to callers — reporting only the first violation forces trial-and-error correction. Routing must be first-match because transition rows are authored with priority ordering — evaluating all rows would create ambiguity about which outcome applies.

### 3A.2 Outcomes and Semantic Verdicts

The language defines a stable semantic verdict space. Every operation that can mutate an entity produces one of a fixed set of outcomes. These outcome types are semantically distinct — not just diagnostic convenience — and callers must be able to discriminate between them.

The outcome types are:

1. **Successful transition.** Event fired; state changed; entity updated.
2. **Successful no-transition event.** Event fired; in-place mutations committed; no state change. This is a deliberate design allowing in-place data changes to be event-driven without triggering entry/exit actions.
3. **Explicit rejection.** An authored `reject` row matched — a designed prohibition, not a data constraint failure.
4. **Constraint failure.** Mutations would violate a rule or ensure; rolled back.
5. **Unmatched routed event.** Transition rows exist for the event but all guards failed — an instance data condition.
6. **Undefined event surface.** No transition rows defined for this event in the current state — a definition gap.
7. **Successful direct update.** Field write committed.
8. **Access mode failure.** Patch targets a field not editable in the current state — either `readonly` (read-only) or `omit` (structurally absent).
9. **Invalid input failure.** Patch is structurally malformed.

**Why the distinctions matter:**

- **Rejection vs constraint failure.** Rejection is an authored decision; constraint failure is a data truth violation. They require different responses from callers and different diagnostic framing.
- **Unmatched vs undefined.** Undefined means no routing surface exists (a definition gap the author should address); unmatched means routing exists but the current data does not satisfy any guard (an instance data condition the caller can address).
- **Transition vs no-transition.** Both are successes. No-transition events execute mutations without state change — a meaningful event-driven pattern, not a degenerate case.

#### Construction outcomes

Entity construction via the initial event produces the same outcome space. All event outcomes are valid at construction: `Transitioned` (construction-time routing to a different state), `Applied` (stayed in initial state), `Rejected` (business rejection at intake), `ConstraintsFailed` (data truth violation at intake), `Unmatched` (guarded initial rows, none matched). `UndefinedEvent` cannot occur — the compiler guarantees the initial event exists. The caller uses the same pattern matching for construction that they use for every event.

#### Restoration outcomes

Entity restoration from persisted data produces a distinct outcome space: successful restoration (data valid, constraints passed), constraint failure (persisted data violates current definition's rules/ensures), or invalid input (structural mismatch — undefined state, unknown fields, type mismatch). Restoring an entity in an invalid state is not allowed — the governance guarantee applies from the moment an entity is loaded, not just when it is mutated. Future migration logic runs before constraint evaluation, transforming persisted data to conform to the current definition.

### 3A.3 Constraint Violation Subject Attribution

When a constraint is violated, the language supports semantic subject attribution — not just the violation message, but what the violation is about.

Every constraint has both **semantic subjects** (the fields or args the constraint references) and a **scope** (why this constraint exists — which rule, which state ensure, which event ensure).

The four constraint kinds have distinct attribution:

1. **Event ensures** target event arguments plus the event scope. The user provided those args.
2. **Rules** target directly referenced fields plus the definition scope. The runtime does not reverse-map through mutations — if the author wants arg-level feedback, they write an event ensure.
3. **State ensures** target directly referenced fields plus the state scope (with anchor: in, to, or from).
4. **Transition rejections** target the event as a whole — this is an authored routing rule, not a data constraint.

Computed fields referenced in constraints are also targets, with transitive expansion to the concrete stored fields they depend on.

This attribution model is a language-level requirement because it flows from how the language distinguishes constraint scopes. The consumer decides rendering; the language provides the semantic structure.

### 3A.4 Mutation Atomicity

All mutations execute on a working copy. Constraints are evaluated against the working copy after all mutations complete. If every constraint passes, the working copy is promoted to become the entity's committed state. If any constraint fails, the working copy is discarded and the entity's state is unchanged. An invalid configuration never exists, even transiently. There is no window between mutation and constraint checking where a partially-committed state with violated rules can be observed.

This guarantee applies to all mutation surfaces: event-driven transitions, stateless event hooks, direct field updates, and state entry/exit actions. The working copy semantics are uniform — every path through the engine that can modify entity data uses the same all-or-nothing promotion/discard model.

### 3A.5 Entity Construction

Construction is modeled as an **initial event** — the precept's constructor. This solves the fundamental problem that entities with required fields (non-optional, no default) cannot be constructed parameterlessly: the author would be forced to either invent nonsense defaults or make things optional that should not be.

```precept
event Create(ApplicantName as string, Amount as currency in USD, CreditScore as integer) initial
```

The `initial` modifier on an event designates it as the construction event. The runtime's `Create(args)` operation fires this event atomically as part of entity creation:

1. Build a hollow version (defaults applied, initial state set, omitted fields structurally absent).
2. Fire the initial event with the caller's args through the standard pipeline — same guards, same mutations, same ensures, same constraint checking as any other event.
3. Return the outcome — same verdict space the caller uses for every event (see §3A.2).

If the precept does not declare an initial event, `Create()` is parameterless and always succeeds (the compiler guarantees all fields have defaults or are optional — enforced by `RequiredFieldsNeedInitialEvent` / `InitialEventMissingAssignments`).

**Construction-time constraint composition.** When the initial event fires, constraints compose naturally with no new language surface:

1. **Arg ensures** (`on Create ensure ...`) — pre-assignment validation of caller-provided args.
2. **Field constraints** (rules, field-level ensures) — post-assignment truth.
3. **Global rules** (`rule ...`) — always evaluated.
4. **Entry ensures** (`to <InitialState> ensure ...`) — construction-specific truth. These are the same entry ensures that fire on any transition into the initial state, but at construction time they serve as the intake invariant — what must be true about data when the entity first exists.
5. **Residency ensures** (`in <InitialState> ensure ...`) — while-in-state truth.

No special "construction constraint" form is needed. `to <InitialState> ensure` is the natural construction-time rule: it fires when the entity enters the initial state, which is exactly what construction does.

**Compiler enforcement:**
- **`RequiredFieldsNeedInitialEvent`:** Precept has required fields (non-optional, no default) but does not declare an initial event — construction cannot produce a valid initial version.
- **`InitialEventMissingAssignments`:** Initial event does not assign all required fields that lack defaults — post-construction state may violate constraints.

**Design rationale:** Construction goes through the full event pipeline because entities must satisfy their constraints from the moment they exist. A parameterless construction path cannot enforce business invariants at intake. By modeling construction as an event, the language reuses all existing machinery — guards can discriminate construction routing, ensures validate args, `reject` can refuse intake, and the caller uses the same pattern matching they use for every event.

### 3A.6 Inspection as a First-Class Operation

Inspection is not a reporting layer — it is a fundamental language operation. It has the same depth as event execution: guard evaluation, exit actions, mutations, entry actions, computed field recomputation, and constraint evaluation — all executed on a working copy without committing.

The answer to "what would happen?" is always available, from any state, for any event, and is honest. The inspection result matches what execution would produce for the same inputs. Inspectability is what makes the governance contract trustworthy — you can always ask, and the language guarantees the answer matches what execution would do.

This is not merely a tooling convenience. It is a language-level guarantee that flows from the execution model's properties: expression purity (§0.4 property 6 — expressions cannot mutate state), deterministic semantics (§0.1 principle 3 — same inputs produce same outputs), and working copy isolation (§3A.4 — mutations execute on a copy). These properties together make inspection safe and honest by construction, not by convention.

---

## 4. Graph Analyzer

> **Status:** Stub — to be written when the graph analyzer is designed and implemented.

---

## 5. Proof Engine

> **Status:** Stub — to be written when the proof engine is designed and implemented.

---

## Open Questions / Implementation Notes

_TBD — open questions will be captured here as later pipeline stages are designed._

---

## Cross-References

| Document | Relationship |
|---|---|
| [Compiler and Runtime Design](../compiler-and-runtime-design.md) | Pipeline architecture; stage contracts |
| [Catalog System](catalog-system.md) | Machine-readable language definition that feeds all pipeline stages |
| [Lexer](../compiler/lexer.md) | §1 implementation detail |
| [Parser](../compiler/parser.md) | §2 implementation detail |
| [Type Checker](../compiler/type-checker.md) | §3 implementation detail |
| [Graph Analyzer](../compiler/graph-analyzer.md) | §4 implementation detail |
| [Proof Engine](../compiler/proof-engine.md) | §5 implementation detail |
| [Language Vision](../archive/language-design/precept-language-vision.md) | Archived target language surface — this spec tracks what's implemented |
