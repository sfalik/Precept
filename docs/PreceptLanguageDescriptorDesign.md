# PreceptLanguage Descriptor Design

## Problem

The Precept DSL grammar — keywords, statement forms, type system, operators, expression scopes, and semantic constraints — exists only as implicit knowledge spread across:

- `PreceptParser.cs` (regex patterns + imperative logic)
- `PreceptExpressionParser.cs` (operator precedence + token kinds)
- `PreceptCompiler` / `PreceptEngine` (compile-time and runtime constraint enforcement)
- `SmSemanticTokensHandler.cs` (keyword token arrays)
- `SmDslAnalyzer.cs` (completion keyword lists)
- `precept.tmLanguage.json` (TextMate regex alternations)
- `docs/DesignNotes.md § DSL Syntax Contract` (prose)
- `README.md § DSL Syntax Reference` (prose)

Each grammar change requires updating all of these independently. The sync checklist in `copilot-instructions.md` keeps growing because each new consumer adds another manual obligation. No consumer can derive the grammar programmatically.

## Solution

A static class `PreceptLanguage` in `src/Precept/` that formally declares every element of the language as typed records. This becomes the **single canonical source** for language metadata. All consumers reference it or are validated against it.

## Location

```
src/Precept/PreceptLanguage.cs
```

No new project. Lives in the core library alongside the parser and runtime.

## Data Model

### Keywords

Grouped by syntactic role — not a flat list.

```csharp
public static readonly string[] ControlKeywords =
    ["precept", "state", "initial", "from", "on", "if", "else", "when", "any"];

public static readonly string[] ActionKeywords =
    ["transition", "set", "reject", "rule", "reason", "no",
     "add", "remove", "enqueue", "dequeue", "push", "pop", "clear", "into",
     "above", "below"];

public static readonly string[] TypeKeywords =
    ["string", "number", "boolean", "null"];

public static readonly string[] LiteralKeywords =
    ["true", "false", "null"];

public static readonly string[] CollectionTypeKeywords =
    ["set", "queue", "stack"];
```

### Scalar Types

```csharp
public sealed record ScalarTypeDef(string Name, bool SupportsNullable, string ClrType);
```

| Name | Nullable | CLR Type |
|---|---|---|
| `string` | yes | `System.String` |
| `number` | yes | `System.Double` |
| `boolean` | yes | `System.Boolean` |
| `null` | no | — |

### Collection Types

```csharp
public sealed record CollectionTypeDef(
    string Keyword,
    string Description,
    string[] AllowedInnerTypes,
    string[] MutationVerbs,
    string[] AccessorProperties);
```

| Keyword | Inner Types | Mutations | Accessors |
|---|---|---|---|
| `set<T>` | `string`, `number`, `boolean` | `add`, `remove`, `clear` | `.count`, `.min`, `.max`, `contains` |
| `queue<T>` | `string`, `number`, `boolean` | `enqueue`, `dequeue [into]`, `clear` | `.count`, `.min`, `.max`, `.peek`, `contains` |
| `stack<T>` | `string`, `number`, `boolean` | `push`, `pop [into]`, `clear` | `.count`, `.min`, `.max`, `.peek`, `contains` |

### Operators

```csharp
public sealed record OperatorDef(string Symbol, int Precedence, OperatorArity Arity, string Description);

public enum OperatorArity { Unary, Binary }
```

Precedence (highest number binds tightest):

| Prec | Operators | Description |
|---|---|---|
| 1 | `\|\|` | Logical OR |
| 2 | `&&` | Logical AND |
| 3 | `==`, `!=` | Equality |
| 4 | `>`, `<`, `>=`, `<=`, `contains` | Comparison / membership |
| 5 | `+`, `-` | Addition, subtraction |
| 6 | `*`, `/`, `%` | Multiplication, division, modulo |
| 7 | `!`, `-` (unary) | Logical NOT, negation |

### Constructs

Each distinct statement form the parser accepts:

```csharp
public sealed record ConstructDef(
    string Form,
    string Description,
    string Context,
    string? Example);
```

| Form | Context | Description |
|---|---|---|
| `precept <Name>` | top-level | Top-level declaration. Exactly one per file. |
| `state <Name> [initial]` | top-level | State declaration. Exactly one must be `initial`. |
| `rule <BoolExpr> "<Reason>"` | under state | State entry rule. References any data field. |
| `event <Name>` | top-level | Event declaration with optional indented args and rules. |
| `<Type>[?] <Name> [= <Literal>]` | under event | Event argument declaration. |
| `rule <BoolExpr> "<Reason>"` | under event | Event rule. May only reference that event's args. |
| `<Type>[?] <Name> [= <Literal>]` | top-level | Scalar data field declaration. |
| `rule <BoolExpr> "<Reason>"` | under field | Field rule. May only reference the declaring field. |
| `<set\|queue\|stack><T> <Name>` | top-level | Collection field declaration. Always starts empty. |
| `rule <BoolExpr> "<Reason>"` | under collection | Collection rule. May only reference the declaring collection. |
| `rule <BoolExpr> "<Reason>"` | top-level | Top-level rule. References any data field declared above. |
| `from <State\|any> on <Event> [when <Guard>]` | top-level | Transition block header. |
| `if <Guard>` | from-on body | Guarded branch. Must end with `transition` or `no transition`. |
| `else if <Guard>` | from-on body | Additional guarded branch. Must follow `if` or `else if`. |
| `else` | from-on body | Fallback branch. Required after `if`/`else if` chain. |
| `set <Field> = <Expr>` | branch body | Data assignment. Executes on fire in declaration order. |
| `transition <State>` | branch body | State transition outcome. |
| `no transition` | branch body | Accept event without changing state. |
| `reject [reason "<Message>"]` | else/unguarded body | Reject event. Only valid in `else` or unguarded position. |
| `add <SetField> <Expr>` | branch body | Add element to set. |
| `remove <SetField> <Expr>` | branch body | Remove element from set. |
| `enqueue <QueueField> <Expr>` | branch body | Enqueue element. |
| `dequeue <QueueField> [into <Field>]` | branch body | Dequeue element; optionally store in scalar field. |
| `push <StackField> <Expr>` | branch body | Push element onto stack. |
| `pop <StackField> [into <Field>]` | branch body | Pop element from stack; optionally store in scalar field. |
| `clear <CollectionField>` | branch body | Remove all elements from collection. |

### Expression Scopes

Which identifiers are valid in which expression positions:

```csharp
public sealed record ExpressionScopeDef(
    string Position,
    string[] AllowedIdentifiers,
    string Description);
```

| Position | Allowed Identifiers |
|---|---|
| Guard (`if`/`else if`) | All data fields, `EventName.ArgName`, collection accessors |
| `when` predicate | All data fields, `EventName.ArgName`, collection accessors |
| `set` RHS | All data fields (read-your-writes), `EventName.ArgName`, collection accessors |
| Field rule | Declaring field only (+ its dotted properties) |
| Event rule | That event's args only (bare `ArgName` or `EventName.ArgName`) |
| State rule | Any data field, collection accessors |
| Top-level rule | Any data field declared above the rule, collection accessors |

### Semantic Constraints

Every constraint the parser, compiler, and runtime enforce — grouped by enforcement phase:

```csharp
public sealed record ConstraintDef(string Phase, string Description);

public enum ConstraintPhase { Parse, Compile, Runtime }
```

#### Parse-Time Constraints

1. Exactly one `precept` declaration per file.
2. Exactly one `state` must include `initial`.
3. No duplicate state names.
4. No duplicate event names.
5. No duplicate field names (scalar or collection, across both).
6. No duplicate event argument names within an event.
7. Non-nullable scalar fields must declare a default value.
8. `set` is only valid inside a `from ... on ...` branch body.
9. `reason` is only valid on `reject`.
10. `if`/`else if` branches must end with `transition <State>` or `no transition` — not `reject`.
11. `else` may end with `transition`, `reject`, or `no transition`.
12. After an `if`/`else if` chain, a fallback must use `else`; a bare block-level outcome after a chain is a parse error.
13. A second `if` within the same `from ... on ...` block (without `else if`) is a parse error.
14. `else if` requires a preceding `if`. `else` requires a preceding `if`.
15. `else` may not be duplicated within a chain.
16. No statements after an outcome (`transition`/`reject`/`no transition`) in a `from ... on ...` block.
17. Each `(state, event)` pair may be handled in exactly one `from ... on ...` block.
18. Field rules may only reference their owning field.
19. Event rules may only reference that event's declared arguments.
20. Top-level rules may only reference fields declared above the rule (no forward references).
21. `states ...` and `events ...` plural declarations are rejected with guidance.
22. Inline `transition A -> B on E` form is rejected with guidance.
23. At least one `state` must be declared.
24. Collection fields do not support nullable inner types or nesting.

#### Compile-Time Constraints

25. A field's default value must satisfy its own field rules.
26. Collection rules must pass at creation time (collection starts empty: `count = 0`).
27. Top-level rules must pass against all fields' default values.
28. Initial state entry rules must pass against default data.
29. Event rules with all-defaulted/nullable args must pass against those default/null values.
30. A literal `set` assignment (constant RHS) must not violate the target field's rules or top-level rules.

#### Runtime Constraints

31. Event rules are evaluated before guard evaluation (Stage 1).
32. Event rules are evaluated against an args-only context — data fields with the same name as an event arg cannot shadow the arg value.
33. Guard predicates (`if`/`else if`) are evaluated after event rules pass (Stage 2).
34. `when` predicate is evaluated before any branch predicates. If `false`, the entire block is `NotApplicable`.
35. `set` assignments execute in declaration order with read-your-writes semantics (Stage 3).
36. Field rules and top-level rules are checked after all `set` assignments and collection mutations commit (Stage 4).
37. State rules are checked only on state transitions, including self-transitions (Stage 5).
38. `no transition` does NOT trigger state rules.
39. Any rule violation at any stage → full rollback → `Rejected` result.
40. `dequeue` from empty queue → `Rejected`.
41. `pop` from empty stack → `Rejected`.
42. `when` false → `NotApplicable` (distinct from `Rejected` — no mutation, no reason emitted, another block for the same event may still handle it).
43. Non-nullable event args without a default are required — caller must supply them.
44. Non-nullable event args with a default use the default when omitted.
45. Nullable event args default to `null` when omitted (or to their declared default).

### Identifier Naming

```csharp
public static readonly string IdentifierPattern = "[A-Za-z_][A-Za-z0-9_]*";
```

All user-defined names (precept name, states, events, fields, args) must match this pattern.

### Literal Values

```csharp
public sealed record LiteralFormDef(string Type, string Syntax, string[] Examples);
```

| Type | Syntax | Examples |
|---|---|---|
| String | `"double-quoted text"` | `"hello"`, `""` |
| Number | Integer or decimal, optional leading `-` | `0`, `42`, `3.14`, `-1` |
| Boolean | `true` or `false` | `true`, `false` |
| Null | `null` | `null` |

Literals are valid as field defaults, event arg defaults, and in expressions.

### Multi-State `from` Syntax

The `from` header accepts three forms:

| Form | Meaning |
|---|---|
| `from State on Event` | Single source state |
| `from StateA, StateB on Event` | Comma-separated list of source states |
| `from any on Event` | All declared states |

All three forms expand to per-state transition entries internally. The `(state, event)` uniqueness constraint applies after expansion.

### Dotted Accessor Forms

```csharp
public sealed record AccessorDef(string BaseType, string Member, string ReturnType, string Description);
```

| Base | `.member` | Returns | Description |
|---|---|---|---|
| Collection | `.count` | `number` | Number of elements |
| Collection | `.min` | `T` | Minimum element (error if empty) |
| Collection | `.max` | `T` | Maximum element (error if empty) |
| `queue<T>` / `stack<T>` | `.peek` | `T` | Front/top element without removing (error if empty) |
| Collection | `contains <Expr>` | `boolean` | Membership test (infix operator, not dotted) |
| Event arg | `EventName.ArgName` | arg type | Prefixed arg reference — unambiguous when a data field shares the arg name |

### Outcome Kinds

```csharp
public sealed record OutcomeKindDef(string Name, string Description, bool InstanceMutated);
```

| Kind | Description | Instance mutated? |
|---|---|---|
| `Accepted` | Event handled, state changed | Yes |
| `AcceptedInPlace` | Event handled via `no transition`, data may change but state stays | Yes |
| `Rejected` | Event matched but blocked (guard reject, rule violation, empty collection op) | No (full rollback) |
| `NotDefined` | No `from ... on ...` block handles this event in the current state | No |
| `NotApplicable` | A `when` predicate evaluated to `false` | No |

### Fire Pipeline Stages

The execution order when `engine.Fire(instance, event, args)` is called:

```
Stage 1: Event argument validation + event rules
         → Rejected if args invalid or event rule violated
Stage 2: Transition resolution (when predicate → guard evaluation)
         → NotApplicable if 'when' is false
         → NotDefined if no matching block
         → Rejected if all guards fail and outcome is reject
Stage 3: Set assignments + collection mutations (declaration order, read-your-writes)
         → Rejected if assignment fails or empty-collection operation
Stage 4: Field rules + top-level rules (checked against post-mutation data)
         → Rejected with full rollback if any rule violated
Stage 5: State rules (only on state transition, including self-transition)
         → Rejected with full rollback if any rule violated
```

All stages are sequential. Any failure at any stage stops execution and returns the instance unchanged.

### Instance Data Shape

```csharp
public sealed record InstanceShapeDef(string Field, string Type, string Description);
```

| Field | Type | Description |
|---|---|---|
| `WorkflowName` | `string` | Name from `precept <Name>` declaration |
| `CurrentState` | `string` | Active state name |
| `LastEvent` | `string?` | Most recently fired event (null on fresh instance) |
| `UpdatedAt` | `DateTimeOffset` | Timestamp of last mutation |
| `InstanceData` | `Dictionary<string, object?>` | All scalar field values + collection backing data |

### Comments

```
# Full-line comment
state Idle initial  # Inline comment
```

`#` outside a double-quoted string literal starts a comment; everything from `#` to end of line is ignored.

### Indentation Semantics

The parser uses indentation (any whitespace deeper than the parent line) to associate child declarations with their parent:

- Rules indented under `state` → state rules
- Args and rules indented under `event` → event args and event rules
- Rules indented under a field → field rules
- `if`/`else`/`set`/`transition`/mutations indented under `from ... on ...` → transition body

Indentation depth is flexible (no fixed tab width), but child lines must have strictly greater indentation than their parent header.

## Consumers

### MCP Server (`precept_language` tool)

`return JsonSerializer.Serialize(PreceptLanguage.Descriptor)` — trivial implementation. Returns the full language reference as structured JSON in a single call.

### Language Server

- `SmSemanticTokensHandler.KeywordTokens` → references `PreceptLanguage.ControlKeywords` + `PreceptLanguage.ActionKeywords` + etc.
- `SmDslAnalyzer.KeywordItems` → built from `PreceptLanguage.AllKeywords`.

### Validation Tests

- A test reads `precept.tmLanguage.json`, extracts keyword alternations from regex patterns, and asserts they match `PreceptLanguage`.
- A test scans `README.md § DSL Syntax Reference` and `DesignNotes.md § DSL Syntax Contract` for keyword/operator/type coverage against `PreceptLanguage`.

### Parser

The parser's hand-written regexes remain the parsing implementation. `PreceptLanguage` does not replace or generate them. The parser continues to be the behavioral source of truth; `PreceptLanguage` is the metadata source of truth.

## Consumer Boundaries

**Active consumers** (reference `PreceptLanguage` at runtime):
- Language server completions (`SmDslAnalyzer.KeywordItems`)
- Language server semantic tokens (`SmSemanticTokensHandler.KeywordTokens`)
- MCP tool (`precept_language` serializes it directly)

**Passive consumers** (validated against `PreceptLanguage` by tests, but don't reference it):
- `PreceptParser.cs` — regex patterns inherently encode keyword knowledge; cannot be parameterized
- `PreceptExpressionParser.cs` — operator precedence is structural, not data-driven
- `precept.tmLanguage.json` — consumed by VS Code's TextMate engine at extension load, not by C# code
- `README.md` / `DesignNotes.md` — prose; validated for coverage by test assertions

## What PreceptLanguage Is Not

- Not a parser generator input. The parser stays hand-written.
- Not a runtime schema. `DslWorkflowModel` describes a specific precept's structure; `PreceptLanguage` describes the language itself.
- Not documentation. It's structured data that documentation, tools, and tests consume.

## Maintenance Rule

When any parser, compiler, or runtime constraint changes, `PreceptLanguage.cs` must be updated in the same pass. Add this to the sync checklist in `copilot-instructions.md`.
