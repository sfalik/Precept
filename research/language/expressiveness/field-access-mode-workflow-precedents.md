# Per-State Field Access Patterns — Workflow & State Machine Precedents

**Date:** 2026-05-15
**Author:** George (Runtime Dev)
**Context:** Precept candidate syntax `in <StateList> define <FieldList> <mode>` where mode ∈ {absent, readonly, editable}

## Research Question

How do established workflow engines and state machine frameworks handle per-state data properties — specifically, whether field presence, read/write access, or editability varies by state? Does any system provide declarative per-state field access modes comparable to Precept's proposed syntax?

---

## 1. XState v5

**Category:** JavaScript/TypeScript state machine library
**Data model:** Single `context` object global to the machine

XState v5 uses `setup({ types: { context: { ... } } })` to declare a single TypeScript type for the entire machine's context. There is no per-state context type — all states share one flat context shape. Context is updated imperatively via `assign()` actions on transitions. Typegen (which in v4 could narrow event types per state) is explicitly not supported in v5.

**Per-state field access:** None. All context properties are globally available and mutable from any state via `assign()`. Guards can check context values but cannot structurally prevent a field from being accessed or modified in a given state.

**Relevance to Precept:**
- XState proves that the dominant JS state machine library treats all data as globally visible
- No concept of `absent` (field doesn't exist in this state) or `readonly` (field exists but can't be written)

---

## 2. Temporal.io

**Category:** Durable workflow orchestration platform
**Data model:** Workflow state = class instance fields (Java/TS) or local variables (Python)

Temporal workflows store state in regular programming language variables within the workflow function/class. All signal handlers, query handlers, and update handlers see the full workflow state. Query handlers provide read-only external access; signal handlers provide write access; update handlers provide both — but these are external API boundaries, not per-state internal constraints.

**Per-state field access:** None. Temporal doesn't have a notion of discrete states in the Harel/SCXML sense — workflows are procedural code with `await` points. All workflow state is visible everywhere within the workflow function.

**Relevance to Precept:**
- Demonstrates that workflow platforms conflate "all data available everywhere" as a simplification
- No structural field access restrictions at any point in the workflow lifecycle

---

## 3. AWS Step Functions

**Category:** Cloud-native workflow orchestration (JSON-based)
**Data model:** JSON payload passed between states via InputPath/OutputPath/ResultPath/Parameters/ResultSelector

Step Functions filter and transform a JSON payload at each state boundary. Each state receives a shaped subset of the execution's JSON via `InputPath` and `Parameters`, processes it, and emits a shaped output via `OutputPath`, `ResultSelector`, and `ResultPath`. This means each state can see only a **subset** of the overall data.

**Per-state field access:** Partial — via I/O filtering. Each state's `InputPath` and `Parameters` determine which JSON fields are visible to the state's task logic, and `OutputPath`/`ResultPath` determine what gets written back. However:
- This is **data plumbing**, not declarative access modes
- There is no `readonly` concept — a state either receives a field or doesn't
- There is no `absent` declaration — fields are simply not routed
- The filtering is imperative JSON path expressions, not declarative per-field modes
- Fields not included in `InputPath` are still present in the execution state; they're just not forwarded

**Relevance to Precept:**
- Closest cloud workflow system to per-state data shaping
- But the mechanism is I/O transformation at state boundaries, not declarative field-level access modes
- Precept's `in <StateList> define <FieldList> <mode>` is structurally different: it declares what IS TRUE about fields in a state, not what data to route

---

## 4. Spring State Machine

**Category:** Java enterprise state machine framework
**Data model:** Extended State = global `Map<Object, Object>` via `getExtendedState().getVariables()`

Spring SM maintains "extended state" as a single global map accessible from any state via `StateContext`. Guards evaluate conditions against extended state using `Guard<S,E>.evaluate(StateContext)` or SpEL expressions like `guardExpression("extendedState.variables.get('myvar')")`. Session scoping exists but scopes the entire state machine instance, not individual states.

**Per-state field access:** None. All extended state variables are globally accessible from any state handler, guard, or action. No mechanism to restrict variable visibility or mutability by state.

**Relevance to Precept:**
- Demonstrates the "global bag of variables" pattern common in enterprise state machines
- Guards can CHECK values but cannot PREVENT access or mutation structurally

---

## 5. Erlang/OTP gen_statem

**Category:** Erlang behavior module for state machines
**Data model:** Single `Data` term passed through all state callbacks

gen_statem offers two callback modes: `state_functions` (one function per state, state must be atom) and `handle_event_function` (single handler, state can be any term). In both modes, a single `Data` term (typically a map or record) is threaded through all state callbacks as the last argument. Callbacks return `{next_state, NextState, NewData}`.

**Per-state field access:** None. Every state callback receives the complete `Data` term. The language trusts programmer discipline — there's no structural mechanism to declare that a field is absent, readonly, or editable in a particular state. Pattern matching in state function heads can destructure data, but this is runtime validation, not declarative access control.

**Relevance to Precept:**
- Erlang's immutable-by-default data gives natural "readonly" semantics for data not explicitly updated
- But this is a property of the language, not of the state machine framework
- No per-state field declarations

---

## 6. Camunda / BPMN

**Category:** Business process management engine (BPMN 2.0)
**Data model:** Process variables with hierarchical execution-tree scoping

Camunda variables are scoped to execution tree nodes: process instance → child execution → task. Variables on a parent scope are visible in child scopes unless shadowed by a local variable (`setVariableLocal()`). Input/output mapping per activity (`<camunda:inputOutput>`) can create local variables from parent-scope data and optionally write results back.

**Per-state field access:** Closest to per-state scoping among process engines, via:
- **Input mapping:** `<camunda:inputParameter>` creates task-local variables from process data
- **Output mapping:** `<camunda:outputParameter>` writes task-local results back to parent scope
- **Local variables:** `setVariableLocal()` creates variables visible only within the current execution scope

However:
- Scoping is tied to the execution tree hierarchy, not to named states
- There is no `readonly` mode — a task either has a local copy or accesses the parent's variable
- There is no `absent` mode — variables simply aren't mapped
- The scoping is implicit in the execution hierarchy, not declared per state

**Relevance to Precept:**
- Demonstrates that process engines can achieve scope isolation via I/O mapping
- But the mechanism is operational (map data in/out at activity boundaries) rather than declarative (declare field modes per state)
- Precept's model is more direct: `in Submitted define applicantName readonly` vs. Camunda's `<camunda:inputParameter name="applicantName">${applicantName}</camunda:inputParameter>`

---

## 7. Windows Workflow Foundation (WF)

**Category:** .NET workflow framework (legacy, part of .NET Framework)
**Data model:** `Variable<T>` scoped per activity; `InArgument<T>` / `OutArgument<T>` / `InOutArgument<T>` for data flow

WF has the most structured data scoping of any system studied:
- **Variables** are declared on activities and scoped to that activity's lifetime: "The lifetime of a variable at runtime is equal to the lifetime of the activity that declares it. When an activity completes, its variables are cleaned up."
- **VariableModifiers.ReadOnly** can make a variable read-only: `new Variable<string> { Modifiers = VariableModifiers.ReadOnly }`
- **Arguments** define typed, directional data flow: `InArgument<T>` (read), `OutArgument<T>` (write), `InOutArgument<T>` (both)

**Per-state field access:** Significant but not per-state in the Precept sense:
- Variables ARE scoped to activities (which are analogous to states in a workflow)
- ReadOnly modifier EXISTS but applies to the variable's entire lifetime, not conditionally by state
- Argument directionality (In/Out/InOut) provides access-mode semantics at activity boundaries
- However, WF activities compose hierarchically (Sequence contains Assign, etc.) — it's not a flat state-to-state model

**Relevance to Precept:**
- WF is the only system that provides BOTH per-activity variable scoping AND a read-only modifier
- `InArgument/OutArgument/InOutArgument` is conceptually similar to field access modes
- But WF's model is hierarchical activity composition, not state machine lifecycle semantics
- Precept's per-state field modes are more directly expressible and apply across the entity lifecycle

---

## 8. SMACH (ROS)

**Category:** Python state machine library for robotic systems (Robot Operating System)
**Data model:** UserData with declared `input_keys`, `output_keys`, and `io_keys` per state

SMACH is the **closest precedent** to Precept's per-state field access model:

```python
class MyState(smach.State):
    def __init__(self):
        smach.State.__init__(self,
            outcomes=['succeeded'],
            input_keys=['field_a'],      # read-only access
            output_keys=['field_b'],     # write-only access
            io_keys=['field_c']          # read-write access
        )
```

Each state explicitly declares:
- `input_keys` — fields the state MAY READ (analogous to `readonly`)
- `output_keys` — fields the state MAY WRITE (analogous to `editable` with write-only semantics)
- `io_keys` — fields the state may both read and write (analogous to `editable`)

The SMACH container validates these declarations at construction time, checking that input keys are satisfied by the container's available data and that output keys are consumed by subsequent states.

**Per-state field access:** Yes — the strongest precedent found:
- States declare which data keys they read from and write to
- Containers validate data flow between states at construction time
- Access is enforced: a state cannot read a key it didn't declare as `input_keys` or `io_keys`

**Limitations vs. Precept:**
- No `absent` concept — a field is either accessible or not declared
- No lifecycle-wide field mode declarations — each state declares independently
- Key remapping allows container-level aliasing, adding indirection
- No type system for the userdata values — keys are strings, values are untyped

**Relevance to Precept:**
- SMACH proves that per-state data access declarations are a viable design pattern with real-world use
- The input/output/io trichotomy maps loosely to Precept's readonly/editable modes
- But SMACH's model is procedural (userdata is a dict passed between execute() calls) while Precept's is declarative and lifecycle-scoped

---

## 9. Unity Animator

**Category:** Game engine animation state machine
**Data model:** Global parameters (Int, Float, Bool, Trigger) shared across all states

Unity's Mecanim Animator Controller uses global parameters that all states and transitions reference. Parameters are set/read via `Animator.SetFloat()`, `Animator.SetBool()`, etc. `StateMachineBehaviour` scripts can be attached to individual states and receive callbacks (`OnStateEnter`, `OnStateUpdate`, `OnStateExit`) but access the same global parameter set.

**Per-state field access:** None. All parameters are global. StateMachineBehaviours can have their own member fields, but these are per-behaviour instance data, not per-state access restrictions on shared parameters.

**Relevance to Precept:**
- Game engine state machines optimize for simplicity and performance, not data governance
- Confirms that real-time state machines use global shared state

---

## 10. Akka FSM (Classic)

**Category:** Scala/Java actor-based state machine
**Data model:** Single `Data` type parameter shared across all state handlers

Akka FSM is parameterized as `FSM[State, Data]` where `Data` is a single type for the entire machine. State handlers pattern-match on `Event(msg, data)` and return transitions via `goto(State).using(newData)` or `stay().using(newData)`.

```scala
sealed trait Data
case object Uninitialized extends Data
final case class Todo(target: ActorRef, queue: Seq[Any]) extends Data
```

**Per-state field access:** None structurally. Different states can pattern-match on different `Data` subtypes (e.g., `Uninitialized` vs. `Todo`), providing a form of state-dependent data shape — but this is runtime pattern matching, not declarative field access modes. All handlers receive the full `Data` value.

**Relevance to Precept:**
- Akka's `Data` subtyping is the closest any mainstream actor framework gets to state-dependent data shapes
- But it's opt-in via sealed trait hierarchies and enforced only at pattern match time
- No concept of readonly vs. editable

---

## 11. SCXML (W3C Standard)

**Category:** W3C standard for state machine notation (XML)
**Data model:** `<datamodel>` with `<data>` elements; ECMAScript or XPath expression language

SCXML allows `<datamodel>` elements as children of `<state>`, meaning data can be **declared** inside a state. With `binding="late"`, these data elements are initialized only when the state is first entered. However, the ECMAScript data model specification (B.2.2) explicitly states: "The Processor must place all variables in a single global ECMAScript scope. Specifically, the SCXML Processor must allow any data element to be accessed from any state."

**Per-state field access:** Paradoxically, SCXML has state-local data DECLARATIONS but globally scoped ACCESS:
- Data can be declared (syntactically) inside a state
- Late binding delays initialization until state entry
- But once initialized, the variable is globally accessible from any state
- No read-only or absent modes
- The `In()` predicate allows guards to check current state, but doesn't restrict data access

**Relevance to Precept:**
- SCXML demonstrates the closest standardized approach to state-scoped data declaration
- But the W3C deliberately chose global scope for simplicity
- Precept's model goes further by making the access mode (absent/readonly/editable) part of the declaration, not just the initialization timing

---

## Synthesis

### Dominant Industry Pattern: Global Data, Imperative Guards

Across all 11 systems studied, **no mainstream system provides declarative per-state field access modes** comparable to Precept's `in <StateList> define <FieldList> <mode>` syntax.

The dominant pattern is:
1. **Single shared data store** — one context/data/extended-state object accessible from all states
2. **Imperative guards** — runtime checks that validate data conditions before transitions
3. **Programmer discipline** — trusting developers to only access appropriate data in each state

### Systems with Partial Precedent

| System | What it provides | Gap vs. Precept |
|--------|-----------------|-----------------|
| **SMACH (ROS)** | `input_keys` / `output_keys` / `io_keys` per state | No `absent` mode; no lifecycle-wide declarations; untyped |
| **Windows WF** | Activity-scoped `Variable<T>` with `ReadOnly` modifier; `InArgument`/`OutArgument`/`InOutArgument` | Hierarchical composition model, not flat state lifecycle |
| **AWS Step Functions** | `InputPath`/`OutputPath` filters per state | I/O plumbing, not declarative access modes |
| **Camunda/BPMN** | Variable scopes with input/output mapping | Execution-tree scoping, not state-based |
| **SCXML** | `<datamodel>` inside `<state>` with late binding | Global scope at runtime despite state-local declaration |
| **Akka FSM** | `Data` sealed trait subtypes per state (via pattern matching) | Runtime pattern matching, not declarative; no readonly/editable |

### Key Findings

1. **SMACH is the strongest precedent.** Its `input_keys`/`output_keys`/`io_keys` model explicitly declares per-state data access. However, it operates at a different abstraction level (robotic task sequencing) and lacks the lifecycle semantics and type safety Precept provides.

2. **WF's `VariableModifiers.ReadOnly` and argument directionality** prove that data access mode concepts exist in production workflow systems, but WF applies them at the activity-composition level, not across a state lifecycle.

3. **No system combines all three of Precept's proposed modes.** `absent` (field structurally does not exist in this state) is particularly novel — most systems only distinguish between "available" and "not routed."

4. **Precept's declarative, lifecycle-scoped approach is genuinely novel.** The syntax `in <StateList> define <FieldList> <mode>` operates at a different semantic level than any existing system: it declares what is TRUE about the entity's data shape in a given state, rather than imperatively filtering, routing, or guarding data at transition boundaries.

### Implication for Language Design

The absence of direct precedent is both an opportunity and a risk:
- **Opportunity:** Precept occupies a unique position — no existing tool provides this capability, meaning it solves a problem that teams currently address with ad-hoc imperative logic
- **Risk:** The absence of precedent means the design cannot lean on established patterns; the UX, error messages, and mental model must be crafted carefully since users won't have prior experience with this paradigm
