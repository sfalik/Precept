# State Machine Runtime API Survey

> Raw research collection. No interpretation, no conclusions for Precept, no recommendations.
> The question: what operations do systems expose for interacting with a running state machine instance, and what do those operations return?

> **Survey dimension note:** The "Direct Field Update Without Transition" dimension was included because the target system under design supports direct field mutation outside the event/transition mechanism. Most surveyed systems do not offer this operation. The dimension is included to document that gap honestly — not to validate or invalidate the design choice.

---

## Table of Contents

1. [Temporal.io](#temporalio)
2. [XState v5](#xstate-v5)
3. [W3C SCXML](#w3c-scxml)
4. [Erlang/OTP gen_statem](#erlangOTP-gen_statem)
5. [Akka Typed Behaviors](#akka-typed-behaviors)
6. [Redux / Redux Toolkit](#redux--redux-toolkit)
7. [Workflow Engines: Cadence, Apache Airflow, AWS Step Functions](#workflow-engines)

---

## Temporal.io

Source: https://docs.temporal.io/develop/typescript/message-passing  
Source: https://docs.temporal.io/encyclopedia/workflow-message-passing

Temporal models long-running workflows as durable state machines. Each workflow execution is a running instance with its own history log. External callers interact with a running workflow via three message types: **Signals**, **Queries**, and **Updates**.

### Instance Identity

A workflow instance is identified by a `workflowId` string (user-supplied or auto-generated at start time). The caller obtains a `WorkflowHandle<T>` from the client:

```typescript
// Starting a workflow — returns a WorkflowHandle
const handle = await client.workflow.start(myWorkflow, {
  workflowId: 'order-123',
  taskQueue: 'main',
});

// Getting a handle to an already-running workflow
const handle = client.workflow.getHandle('order-123');
```

`WorkflowHandle<T>` is the primary gateway for all message operations on a specific instance. The `workflowId` is the stable, user-meaningful identity; the `runId` (UUID) identifies a specific run within a potential chain of continues-as-new.

### Defining Signal, Query, and Update on the Workflow Side

TypeScript SDK uses typed "definition" objects created before the handler is registered:

```typescript
import { defineQuery, defineSignal, defineUpdate } from '@temporalio/workflow';

// Query: no-arg, returns string
const getStatusQuery = defineQuery<string>('getStatus');

// Signal: takes a string argument, returns void
const approveSignal = defineSignal<[reason: string]>('approve');

// Update: takes a number, returns boolean
const adjustAmountUpdate = defineUpdate<boolean, [number]>('adjustAmount');
```

Inside the workflow function, handlers are registered with `setHandler`:

```typescript
import { setHandler } from '@temporalio/workflow';

export async function myWorkflow(): Promise<void> {
  let status = 'pending';
  let amount = 100;

  setHandler(getStatusQuery, () => status);            // sync, returns value
  setHandler(approveSignal, (reason: string) => {     // async allowed; return ignored
    status = 'approved';
  });
  setHandler(adjustAmountUpdate,
    async (delta: number) => {                        // async handler allowed
      amount += delta;
      return amount > 0;
    },
    {
      validator: (delta: number) => {                 // sync; throws to reject before history
        if (delta === 0) throw new Error('Delta cannot be zero');
      },
    }
  );

  await condition(() => status === 'approved');
}
```

### The "Fire" Operation: Signal

`WorkflowHandle.signal(def, args?)` — asynchronous write, no return value.

```typescript
// TypeScript client
await handle.signal(approveSignal, 'finance team approval');
// → Promise<void>; resolves when the server has accepted the signal into the history
```

- Return type: `Promise<void>`. The promise resolves when the Temporal server has durably persisted the `WorkflowExecutionSignaled` history event.
- The signal handler on the workflow side may not have executed yet when the caller's promise resolves.
- No way to distinguish "handler not registered" vs "handler ran without error". Signal delivery is guaranteed if the workflow is still running; dropped silently if the workflow has already completed.
- Signals cannot return a value to the caller by design.
- History impact: adds a `WorkflowExecutionSignaled` event.

### The "Query" Operation

`WorkflowHandle.query(def, args?)` — synchronous read, no mutation, no history entry.

```typescript
const status: string = await handle.query(getStatusQuery);
// → Promise<ReturnType>; string in this example
```

- Return type: `Promise<ReturnType>` where `ReturnType` is the first generic argument of `defineQuery<ReturnType, [Args]>()`.
- The query handler **must be synchronous** (`setHandler` for a query cannot accept an async handler).
- Queries do **not** add events to workflow history.
- Queries are answered in a consistent point-in-time snapshot of the workflow state.
- If the workflow is not running (completed, failed, terminated), queries may still be served against the last known state depending on persistence configuration.
- Error on failure: the `Promise` rejects; specific error type is `WorkflowQueryFailedError`.

### The "Update" Operation

`WorkflowHandle.executeUpdate(def, options?)` — synchronous write that can return a value. Two-phase: validate then execute.

```typescript
// Blocking variant — waits for handler completion
const result: boolean = await handle.executeUpdate(adjustAmountUpdate, { args: [50] });
// → Promise<ReturnType>

// Non-blocking variant — waits only for server acceptance (post-validator)
const updateHandle: WorkflowUpdateHandle<boolean> = await handle.startUpdate(
  adjustAmountUpdate,
  { args: [50], waitForStage: WorkflowUpdateStage.ACCEPTED }
);
// Later:
const result: boolean = await updateHandle.result();
```

Full type signatures from the SDK:

```typescript
// executeUpdate
executeUpdate<Ret, Args extends any[]>(
  def: UpdateDefinition<Ret, Args> | string,
  options: WorkflowUpdateOptions & { args: Args }
): Promise<Ret>;

// startUpdate
startUpdate<Ret, Args extends any[]>(
  def: UpdateDefinition<Ret, Args> | string,
  options: WorkflowUpdateOptions & { args: Args; waitForStage: WorkflowUpdateStage }
): Promise<WorkflowUpdateHandle<Ret>>;

// WorkflowUpdateHandle
interface WorkflowUpdateHandle<T> {
  updateId: string;
  workflowId: string;
  result(): Promise<T>;
}
```

### Update Lifecycle and Error Discrimination

An Update passes through two stages:

1. **Validation** — the `validator` function (if provided) runs synchronously in the workflow. If it throws, the Update is **rejected** before being written to history. The client receives a `WorkflowUpdateFailedError`.
2. **Execution** — if accepted, a `WorkflowExecutionUpdateAccepted` event is written to history, then the async handler runs. If the handler throws, a `WorkflowExecutionUpdateCompleted` event is written with failure data and the client receives a `WorkflowUpdateFailedError`.

Both validator rejection and handler failure surface as the same error type (`WorkflowUpdateFailedError`). The caller **cannot distinguish** them by exception type alone; the error message contains the rejection reason.

```typescript
import { WorkflowUpdateFailedError } from '@temporalio/client';

try {
  await handle.executeUpdate(adjustAmountUpdate, { args: [0] });
} catch (e) {
  if (e instanceof WorkflowUpdateFailedError) {
    // Could be validator rejection OR handler failure
    console.log(e.message);
  }
}
```

History events:
- Validator rejection: no history entry is written for the rejected update.
- Handler failure: `WorkflowExecutionUpdateAccepted` + `WorkflowExecutionUpdateCompleted` (with failure) are written.

### "What Would Happen" (Simulation)

No built-in "dry run" or "simulate" operation on the runtime API. The Temporal SDK has no equivalent of XState's pure `transition()`. The closest approximation is:
- Use a Query to read current state and apply transition logic locally (client-side simulation).
- Use the Update validator as a pre-flight check that does not commit if it throws.
- Use Temporal's testing utilities (`TestWorkflowEnvironment`) for offline simulation in test code.

### "Direct Field Update Without Transition"

No direct field mutation API. The data model is entirely internal to the workflow function's local variables. External callers can only influence state through Signals and Updates (which the workflow handles via registered handlers). There is no "bypass transition" write operation.

### Return Type: Typed vs Stringly Typed

Fully typed in TypeScript. `defineQuery<Ret, Args>`, `defineSignal<Args>`, `defineUpdate<Ret, Args>` capture types at definition time. The client-side API is generic and the TypeScript compiler enforces argument and return types.

The underlying wire protocol uses Temporal's `Payloads` (serialized bytes + metadata), but the TypeScript SDK provides codec abstraction so callers work with typed values.

### Comparison Table

| Operation | Return         | History Entry | Can Reject | Return Value |
|-----------|----------------|---------------|------------|--------------|
| Query     | `Promise<T>`   | None          | No         | Yes (`T`)    |
| Signal    | `Promise<void>`| `ExecutionSignaled` | No  | None         |
| Update    | `Promise<T>`   | `Accepted` + `Completed` | Yes (validator) | Yes (`T`) |

---

## XState v5

Source: https://stately.ai/docs/actors  
Source: https://stately.ai/docs/machines  
Source: https://stately.ai/docs/states  
Source: https://stately.ai/docs/inspection  
XState v5.x (latest as of research)

XState v5 models state machines as "actors" within an actor system. The primary runtime unit is an **actor** created from a machine definition.

### Instance Identity

Actors are identified by their position in the actor system tree. The root actor is created directly; child actors are spawned inside parent actor logic.

```typescript
import { createActor } from 'xstate';

const actor = createActor(machine);
// actor has no stable user-facing string ID by default
// Child actors can be given IDs during spawn: context.spawn(childMachine, { id: 'child-1' })
```

There is no registry of running actors in the same way Temporal has `workflowId`. Identity is maintained by holding a reference to the `Actor` object or an `ActorRef` (opaque reference that can be sent across actor boundaries within the same process).

### Creating and Starting an Actor

```typescript
import { createActor } from 'xstate';

const actor = createActor(machine, {
  input: { /* initial input */ },
  inspect: (inspectionEvent) => { /* optional inspection hook */ },
});

actor.start(); // required before sending events or reading state
```

### The "Fire" Operation: `actor.send()`

```typescript
actor.send({ type: 'APPROVE', reason: 'finance' });
// → void; returns immediately
```

- Return type: `void`. No return value, no acknowledgment.
- The event is processed synchronously within the same microtask if the actor is active.
- After `send()` returns, `actor.getSnapshot()` reflects the post-transition state (in XState v5, processing is synchronous for finite state machines).
- If the event causes no transition (no matching transition or guard fails), the snapshot is unchanged.
- There is no error thrown for an unhandled event — the actor simply stays in its current state.

### The "Inspect/Query" Operation: `actor.getSnapshot()`

```typescript
const snapshot = actor.getSnapshot();
```

Return type is the `Snapshot` of the actor type. For machine actors:

```typescript
interface MachineSnapshot<Context, Event, Children, Tag, Output, Guard, Actor, Delay, StateValue> {
  value: StateValue;                          // current state name or nested object
  context: Context;                           // extended state
  status: 'active' | 'done' | 'error' | 'stopped';
  output: Output | undefined;                 // set when status === 'done'
  error: unknown | undefined;                 // set when status === 'error'
  tags: Set<string>;
  children: Record<string, ActorRef<any>>;    // spawned/invoked actors
  matches(value: StateValue): boolean;
  hasTag(tag: string): boolean;
  can(event: Event): boolean;
  getMeta(): Record<string, any>;
  toJSON(): object;
}
```

Key properties on the snapshot object:
- `snapshot.value` — string for flat machines, nested object for hierarchical machines (e.g. `{ form: 'invalid' }`), object with multiple keys for parallel machines.
- `snapshot.context` — current extended state (immutable; do not mutate).
- `snapshot.status` — lifecycle status; `'active'` means still running.
- `snapshot.output` — defined only when `status === 'done'`.
- `snapshot.matches('stateName')` — returns `true` if the current state value is the given value or a subset of it.
- `snapshot.can({ type: 'EVENT' })` — returns `true` if sending this event would cause a state change; **evaluates guards as a side-effect-free check**.
- `snapshot.hasTag('tag')` — returns `true` if any active state node has the given tag.

`getSnapshot()` is synchronous and non-mutating. It reads from the actor's internal state without dispatching any events.

### Subscribing to State Changes

```typescript
const subscription = actor.subscribe((snapshot) => {
  // called every time the actor emits a new snapshot
  console.log(snapshot.value);
});

// Later:
subscription.unsubscribe();
```

Return type: `{ unsubscribe(): void }` — a standard observable subscription.

### "What Would Happen" Without Committing: Pure Transition Functions

XState v5 provides pure transition utilities that compute next state without running side effects or modifying actor state.

```typescript
import { transition, getNextSnapshot, getNextTransitions } from 'xstate';

// Since XState v5.19.0: pure transition
// Returns [nextSnapshot, actionsToExecute]
const [nextState, actions] = transition(machine, currentSnapshot, event);

// getNextSnapshot (older API, still works)
const nextSnapshot = getNextSnapshot(machine, currentSnapshot, event);

// getNextTransitions — what transitions are available from this snapshot
// Returns an array of transition objects (v5.26.0+)
const transitions = getNextTransitions(snapshot);
// Each entry: { eventType, target, source, actions, reenter, guard }
```

`transition()` and `getNextSnapshot()` are **pure functions** — they do not execute actions, run entry/exit handlers, or mutate any actor state. They return what the state machine's declarative logic dictates.

`getNextTransitions(snapshot)` returns the set of enabled transitions without firing any event, allowing callers to enumerate what events are currently valid.

### "Direct Field Update Without Transition"

No direct field mutation API exposed to external callers. Context (extended state) can only change via `assign` actions triggered by transitions. There is no equivalent to a "bypass transition" write.

However, the `createActor` options accept a `snapshot` parameter to restore a previously serialized state (useful for persistence/hydration, not for live mutation):

```typescript
const persistedSnapshot = JSON.stringify(actor.getSnapshot());
const restoredActor = createActor(machine, { snapshot: JSON.parse(persistedSnapshot) });
```

### The Inspect API

XState v5 provides an inspection hook for observing all internal events without interfering with the actor:

```typescript
const actor = createActor(machine, {
  inspect: (inspectionEvent) => {
    switch (inspectionEvent.type) {
      case '@xstate.actor':    // an actor was created
      case '@xstate.event':    // an event was sent to an actor
      case '@xstate.snapshot': // an actor emitted a new snapshot
      case '@xstate.microstep': // intermediate step during a compound transition
    }
  }
});
```

The `inspect` callback receives typed `InspectionEvent` objects. This is a side-channel observation mechanism — it does not pause or alter execution.

### Return Type: Typed vs Stringly Typed

XState v5 is fully TypeScript-typed. Machine definitions use generic type parameters for `Context`, `Event`, `State`, etc. The snapshot type, action types, and guard types are inferred. The `send()` event argument is typed to the union of all event types declared in the machine.

String-typed aspects: event `type` fields are strings by convention (e.g. `{ type: 'APPROVE' }`), but TypeScript narrows them to the declared union.

### Can the Caller Distinguish Guard Failures?

`snapshot.can(event)` returns `true` or `false` — there is **no distinction** between "no transition defined for this event" and "a transition is defined but its guard returned false". Both result in `false`. The distinction is not surfaced in the public API.

After `actor.send(event)`, if nothing changed, the snapshot is identical to the pre-send snapshot. There is no error thrown, no rejection signal.

### Waiting for a Condition

```typescript
import { waitFor } from 'xstate';

const doneSnapshot = await waitFor(
  actor,
  (snapshot) => snapshot.status === 'done',
  { timeout: 5000 } // optional
);
```

Returns `Promise<Snapshot>`. Rejects with a timeout error if the predicate isn't satisfied within the timeout.

---

## W3C SCXML

Source: https://www.w3.org/TR/scxml/ (W3C Recommendation, 1 September 2015)

SCXML is a specification-level standard. It defines an XML document format and an execution model (algorithm), not a programming API. Implementations (e.g., Apache Commons SCXML, PySCXML, SCION, etc.) expose their own APIs; this section covers what the SCXML specification mandates at the execution model level.

### Instance Identity

The specification mandates a `_sessionid` system variable bound at session initialization time. The format is platform-specific (type `NMTOKEN`). The session ID is accessible inside the SCXML document via the `_sessionid` data model variable, but the mechanism for an external caller to obtain and reference a session ID is implementation-defined.

```xml
<!-- Inside SCXML document, _sessionid is always available -->
<send target="#_scxml_{sessionId}" event="someEvent"/>
```

External callers send events to sessions via **Event I/O Processors** (e.g., the SCXML Event I/O Processor or the Basic HTTP Event I/O Processor). The addressing is URI-based; session identity is reflected in the URI scheme of the I/O processor's `location` field in `_ioprocessors`.

### Encapsulation Principle

The SCXML specification explicitly states:

> "An SCXML processor is a pure event processor. The only way to get data into an SCXML state machine is to send external events to it. The only way to get data out is to receive events from it."

This is a fundamental design principle: **there is no query or inspect operation** in the SCXML execution model. External entities can only write (via events); they cannot read the state configuration or data model directly.

### The "Fire" Operation: `<send>` (External) and `<raise>` (Internal)

External events are delivered via Event I/O Processors. The SCXML spec defines:

```xml
<!-- Send event to another SCXML session -->
<send target="http://sessions.example.com/session/abc" 
      type="http://www.w3.org/TR/scxml/#SCXMLEventProcessor"
      event="approve"
      namelist="reason amount">
  <!-- optional delay -->
</send>

<!-- Raise internal event (within the same session) -->
<raise event="internalEventName"/>
```

`<send>` attributes:
- `event` — the event name string.
- `target` — URI of the destination.
- `type` — transport mechanism URI (e.g., SCXML Event I/O Processor, Basic HTTP).
- `delay` / `delayexpr` — deferred delivery time (CSS2 duration format).
- `namelist` — space-separated list of data model location names to send as payload.
- `id` / `idlocation` — for cancellation via `<cancel sendid="..."/>`.

Return from `<send>`: The element returns immediately; delivery is asynchronous. The SCXML processor may generate `error.communication` if delivery fails. There is no return value or acknowledgment.

### The "Inspect" Operation

**No standardized external inspection API.** The SCXML spec provides no `getState()`, `query()`, or similar operation accessible from outside the session.

Inside the SCXML document, the `In(stateId)` predicate can be used in `cond` attributes to branch based on the current configuration:

```xml
<transition cond="In('closed')" event="open" target="open"/>
```

`In(stateId)` returns `true` if the state with the given `id` is currently in the active configuration.

The `_event` system variable is bound to the current event being processed (accessible during transition execution, `<onentry>`, `<onexit>`). It has fields: `name`, `type`, `sendid`, `origin`, `origintype`, `invokeid`, `data`.

System variables accessible inside the document:
- `_sessionid` — the session's unique ID (string).
- `_name` — the value of the `name` attribute on `<scxml>`.
- `_event` — the current event being processed.
- `_ioprocessors` — map of available Event I/O Processors with their `location` fields.
- `_x` — root for platform-specific variables.

### Execution Model: Event Queues

The SCXML algorithm maintains two queues:

1. **Internal event queue** — events raised by `<raise>` and `<send target="_internal">`. Processed before external events. Drained to empty before each macrostep completes.
2. **External event queue** — events from external sources (other sessions, I/O processors). A blocking read at the end of each macrostep.

Execution follows **run-to-completion** semantics: once processing of an external event begins (a macrostep), no new external events are processed until all internal events have been drained and all eventless transitions have been exhausted. This means the state machine cannot be in an intermediate state from an external observer's perspective.

**Microstep**: execution of one set of enabled transitions.  
**Macrostep**: a complete sequence of microsteps, ending when the internal queue is empty and no eventless transitions are enabled.

### "What Would Happen" Simulation

No standardized operation. The spec defines no dry-run or simulation API. Implementations may offer non-normative testing utilities.

### "Direct Field Update Without Transition"

The `<assign>` element modifies the data model, but it can only appear inside executable content blocks (transitions, `<onentry>`, `<onexit>`, `<finalize>`, `<script>`). It is not externally accessible.

The spec notes:

> "Note that this specification does not define any way to modify the data model except by `<assign>`, `<finalize>`, and possibly platform-specific elements of executable content. In particular, no means is defined for external entities to modify the data model."

### Transition Selection Algorithm (Guard Evaluation)

A transition `T` is enabled by event `E` in atomic state `S` if:
1. `T`'s source state is `S` or an ancestor of `S`
2. `T` matches `E`'s name (prefix matching on dot-separated tokens)
3. `T` has no `cond` attribute **or** its `cond` evaluates to `true`

If no transition matches (either the event type doesn't match, or all guards fail), the event is silently discarded. There is no error raised and no way to distinguish "no transition for this event" from "transition exists but guard failed" from the outside.

### Return Types

The SCXML spec is data-model-agnostic. It supports the Null data model, ECMAScript data model, and XPath data model. Event data types are platform-specific. The `_event.data` field type depends on the data model in use.

No typed API surface in the traditional sense. All communication is string-typed event names + untyped payloads.

### Error Events

SCXML processors signal errors as internal events with names beginning with `error.`:
- `error.communication` — failure communicating with external entity.
- `error.execution` — expression evaluation or execution failure.
- `error.platform` — platform/application-specific error.

These events are placed on the internal event queue and are not surfaced to external callers directly.

---

## Erlang/OTP gen_statem

Source: https://www.erlang.org/doc/apps/stdlib/gen_statem.html (OTP 28.4.2)

`gen_statem` is the Erlang/OTP behavior for implementing generic state machines. It is the modern replacement for `gen_fsm`. Every running `gen_statem` process is a state machine instance.

### Instance Identity

A `gen_statem` process is identified by its Erlang PID (`pid()`) or a registered name:

```erlang
% PID-based reference
ServerRef :: pid()

% Named registration forms
ServerRef :: {local, Name :: atom()}
           | {global, GlobalName :: term()}
           | {via, RegMod :: module(), Name :: term()}
```

The `ServerRef` type union:

```erlang
-type server_ref() ::
    pid()
    | (LocalName :: atom())
    | {Name :: atom(), Node :: atom()}
    | {global, GlobalName :: term()}
    | {via, RegMod :: module(), ViaName :: term()}.
```

### Callback Modes

`gen_statem` supports two callback modes, selected by implementing the `callback_mode/0` callback:

**`state_functions` mode**: State must be an `atom()`. One function per state:
```erlang
Module:StateName(EventType, EventContent, Data) -> event_handler_result(state_name())
```

**`handle_event_function` mode**: State may be any `term()`. Single handler:
```erlang
Module:handle_event(EventType, EventContent, CurrentState, Data) -> event_handler_result(state())
```

The `state()` type:
```erlang
-type state() :: state_name() | term(). % atom in state_functions mode; any term in handle_event_function
-type state_name() :: atom().
-type data() :: term().
```

### The "Fire" Operations

**Synchronous fire (call):**
```erlang
gen_statem:call(ServerRef, Request) -> Reply
gen_statem:call(ServerRef, Request, Timeout) -> Reply
% Timeout :: timeout() | {clean_timeout, T} | {dirty_timeout, T}
% Return: Reply :: term()
% Delivers: {call, From} event type to state callback
% Blocks: yes, blocks calling process until reply or timeout
% On timeout: calling process exits with {timeout, {gen_statem, call, [ServerRef, Request, Timeout]}}
```

**Asynchronous fire (cast):**
```erlang
gen_statem:cast(ServerRef, Msg) -> ok
% Return: ok (always, immediately)
% Delivers: cast event type to state callback
% Blocks: no
```

**Asynchronous call (send_request + receive):**
```erlang
gen_statem:send_request(ServerRef, Request) -> ReqId :: request_id()
% Sends request without blocking; returns an opaque request ID

gen_statem:wait_response(ReqId, WaitTime) ->
    {reply, Reply} | {error, {Reason, ServerRef}} | timeout
% WaitTime may be 'infinity'

gen_statem:receive_response(ReqId, Timeout) ->
    {reply, Reply} | {error, {Reason, ServerRef}} | timeout
% Like wait_response but stops waiting at timeout (abandons without error)

gen_statem:check_response(Msg, ReqId) ->
    {reply, Reply} | {error, {Reason, ServerRef}} | no_reply
% Non-blocking check; used in selective receive patterns
```

The `request_id()` type is opaque.

### The "Inspect" Operation: `sys:get_state/1`

`sys:get_state/1` retrieves the current state and data without dispatching any event:

```erlang
sys:get_state(ServerRef) -> {CurrentState, Data}
sys:get_state(ServerRef, Timeout) -> {CurrentState, Data}

% Return: tuple of {state(), data()}
% Does NOT place any event in the state machine's event queue
% Uses Erlang's sys debug infrastructure (not part of the gen_statem protocol)
```

Also:
```erlang
sys:get_status(ServerRef) -> Status
% Returns internal OTP process status including state, data, and debug info
% Format: {status, Pid, {module, Module}, Items}
```

Note: `sys:get_state` is available for all `gen_*` behaviors. It is a maintenance/introspection operation, not part of the business API.

### State Callback Return Types

State callbacks return action instructions to the `gen_statem` engine:

```erlang
% Full result type
-type event_handler_result(StateType) ::
    {next_state, NextState :: StateType, NewData :: data()}
  | {next_state, NextState :: StateType, NewData :: data(), Actions}
  | {keep_state, NewData :: data()}
  | {keep_state, NewData :: data(), Actions}
  | keep_state_and_data
  | {keep_state_and_data, Actions}
  | {repeat_state, NewData :: data()}
  | {repeat_state, NewData :: data(), Actions}
  | repeat_state_and_data
  | {repeat_state_and_data, Actions}
  | {stop, Reason :: term()}
  | {stop, Reason :: term(), NewData :: data()}
  | {stop_and_reply, Reason :: term(), Replies}
  | {stop_and_reply, Reason :: term(), Replies, NewData :: data()}

-type action() ::
    postpone
  | {postpone, Postpone :: boolean()}
  | hibernate
  | {hibernate, Hibernate :: boolean()}
  | {next_event, EventType :: event_type(), EventContent :: term()}
  | {change_callback_mode, NewCallbackMode :: callback_mode()}
  | enter_action()
  | reply_action()
  | timeout_action()

-type reply_action() :: {reply, From :: from(), Reply :: term()}
```

The `{reply, From, Reply}` action can be returned from any state callback (not just the one that received the `{call, From}` event). This means replies to synchronous calls can be deferred to a different state.

### Sending a Reply to a Synchronous Caller

Replies from `gen_statem:call/2,3` can be sent in two ways:

1. **In the return value** of the state callback:
   ```erlang
   {keep_state, NewData, [{reply, From, ReplyValue}]}
   ```

2. **Via `gen_statem:reply/2` at any point** (allows deferred reply):
   ```erlang
   gen_statem:reply(From, Reply) -> ok
   gen_statem:reply([{From, Reply}]) -> ok
   ```

### Event Types

```erlang
-type event_type() ::
    external_event_type()
  | timeout_event_type()
  | internal

-type external_event_type() ::
    {call, From :: from()}
  | cast
  | info

-type timeout_event_type() ::
    timeout
  | {timeout, Name :: term()}
  | state_timeout

% Timeout types:
% event_timeout  — reset by any event (including internal); single unnamed timer
% state_timeout  — reset on state change; fires if machine stays in same state
% generic_timeout — named timer {timeout, Name}; not reset by events
```

### "What Would Happen" Simulation

No built-in pure simulation API. `gen_statem` is a live-running OTP process; there is no equivalent of XState's pure `transition()`. To simulate, one would need to call the callback module's function directly (bypassing the process framework), which is possible since they are ordinary Erlang functions but is not a supported API pattern.

### "Direct Field Update Without Transition"

`sys:replace_state/2` can forcibly replace the state and data:

```erlang
sys:replace_state(ServerRef, ReplaceFun) -> NewState
sys:replace_state(ServerRef, ReplaceFun, Timeout) -> NewState
% ReplaceFun :: fun(State :: term()) -> NewState :: term()
% For gen_statem, State is {CurrentStateName, Data}
```

This is a maintenance/debugging facility, not a production API. It bypasses transition logic entirely, fires no event handlers, and should not be used in production code.

### Return Type: Typed vs Stringly Typed

Erlang is dynamically typed. There is no compile-time enforcement on the type of `Reply`. Types are documented via `-spec` attributes and Dialyzer analysis, but are not enforced at runtime. `Reply :: term()` is the most specific contract.

### Guard vs No-Transition Discrimination

From the caller's perspective:
- If a `call` is dispatched and the state callback matches it (any clause matches the event type), a reply is expected.
- If the state callback does not handle the event (falls through or uses `{keep_state_and_data, []}` with no reply), the calling process will block until timeout.
- The `gen_statem` behavior does **not** automatically distinguish "guard failed" from "no clause matched" — both result in the event being passed through without a reply (for `cast`) or blocking indefinitely (for `call`).
- The `postpone` action reschedules an event to be tried again after the next state change. This is the mechanism for deferring events the current state cannot handle.

---

## Akka Typed Behaviors

Source: https://doc.akka.io/libraries/akka-core/current/typed/interaction-patterns.html  
Akka Core v2.10.17 (Scala/Java)

Akka Typed models actors using `Behavior[T]` — a strongly typed handler function that processes messages of type `T`. The "state machine" pattern in Akka Typed is expressed as recursive `Behavior[T]` values, where transitions are represented by returning a new `Behavior` from the message handler.

### Instance Identity

Actors are referenced via `ActorRef[T]`. This is an opaque, typed handle to a running actor. `ActorRef[T]` can be passed around, serialized (with Akka serialization), and stored.

```scala
// ActorRef is the handle to an actor instance
val actorRef: ActorRef[MyProtocol.Command] = ???

// ActorSystem is itself the root actor
val system: ActorSystem[RootCommand] = ActorSystem(rootBehavior, "MySystem")

// child actors are spawned from ActorContext
val childRef: ActorRef[ChildCommand] = context.spawn(childBehavior, "child-name")
// or anonymously:
val anonRef: ActorRef[ChildCommand] = context.spawnAnonymous(childBehavior)
```

There is no global registry by default. Actor identity = the `ActorRef` handle itself. Cluster Sharding adds entity identity via `EntityTypeKey` + `entityId` string.

### Message Protocol: `Behavior[T]`

The fundamental pattern:

```scala
sealed trait Command
case class Approve(reason: String) extends Command
case class GetStatus(replyTo: ActorRef[Status]) extends Command
case class Status(current: String)

object MyActor {
  def apply(): Behavior[Command] =
    pending()

  private def pending(): Behavior[Command] =
    Behaviors.receiveMessage[Command] {
      case Approve(reason) =>
        // transition: return a new Behavior representing the "approved" state
        approved(reason)
      case GetStatus(replyTo) =>
        replyTo ! Status("pending")
        Behaviors.same // stay in current state
    }

  private def approved(reason: String): Behavior[Command] =
    Behaviors.receiveMessage[Command] {
      case GetStatus(replyTo) =>
        replyTo ! Status(s"approved: $reason")
        Behaviors.same
      case Approve(_) =>
        Behaviors.same // already approved; no-op
    }
}
```

State is implicit in which `Behavior` value is currently active. There is no `currentState` property on the actor or its reference.

### The "Fire" Operation: `tell` / `!`

```scala
actorRef ! MyCommand("data")    // Scala
actorRef.tell(MyCommand("data")) // Java
// Return: Unit (Scala) / void (Java); returns immediately
```

`tell` is asynchronous fire-and-forget. The message is placed in the actor's mailbox. There is no acknowledgment, no return value, and no guarantee of processing order relative to other operations in the calling thread.

### The "Request-Response" Pattern (Synchronous Equivalent)

Akka Typed has no synchronous `call` primitive on `ActorRef`. The reply must be embedded in the message protocol as an `ActorRef[Response]` field:

```scala
case class GetStatus(replyTo: ActorRef[Status]) extends Command
// The caller provides its own ActorRef as the replyTo destination
```

From **outside the actor system**, `ask` returns a `Future[Response]`:

```scala
import akka.actor.typed.scaladsl.AskPattern._
import akka.util.Timeout
import scala.concurrent.duration._

implicit val timeout: Timeout = 3.seconds
implicit val system: ActorSystem[_] = ???

val result: Future[Status] = actorRef.ask(ref => GetStatus(ref))
// → Future[Status]; completed with the reply, or failed with TimeoutException
```

Full signature:
```scala
// On ActorRef (via extension method from AskPattern)
def ask[Res](
  replyTo: ActorRef[Res] => T
)(implicit timeout: Timeout, scheduler: Scheduler): Future[Res]
```

From **within an actor**, `context.ask` adapts the response:

```scala
context.ask(otherActor, GetStatus.apply) {
  case Success(Status(s)) => WrappedStatus(s)
  case Failure(_)         => WrappedStatus("unknown")
}
// context.ask is a fire-and-collect pattern; no blocking; response arrives as a message
```

### Generic Status Reply: `StatusReply[T]`

For operations that can succeed or fail with a validation error:

```scala
import akka.pattern.StatusReply

case class OpenDoor(replyTo: ActorRef[StatusReply[String]]) extends Command

// Inside the actor:
replyTo ! StatusReply.Success("Door opened")
// or:
replyTo ! StatusReply.Error("Door locked")

// From outside, using askWithStatus:
val result: Future[String] = actorRef.askWithStatus(ref => OpenDoor(ref))
// StatusReply.Success(v) → Future succeeds with v
// StatusReply.Error(msg) → Future fails with StatusReply.ErrorMessage(msg)
```

This pattern allows the caller to distinguish a business-logic rejection from a timeout or actor crash.

### The "Inspect/Query" Operation

There is **no built-in `getState()` or query operation** in Akka Typed. All state inspection must go through the actor's own message protocol. The actor decides what state it exposes by handling specific query messages.

There is no equivalent to `sys:get_state/1` in standard Akka Typed (that is an OTP-specific mechanism). Akka Classic had `actor.underlyingActor` for test access, but this pattern does not exist in Typed.

For testing, `ActorTestKit` provides:
```scala
val probe: TestProbe[Status] = testKit.createTestProbe[Status]()
actorRef ! GetStatus(probe.ref)
val reply: Status = probe.receiveMessage() // blocking receive in test code
```

### "What Would Happen" Simulation

No built-in pure simulation API. `Behaviors.receiveMessage` is a live handler function, not a pure transition function. To simulate, one would need to call the behavior factory functions directly in test code, which is possible but not a first-class API.

The `BehaviorTestKit` in Akka Typed tests allows synchronous execution of actor logic for testing purposes:

```scala
import akka.actor.testkit.typed.scaladsl.BehaviorTestKit

val testKit = BehaviorTestKit(MyActor())
testKit.run(Approve("test"))
// state after the message can be inspected via testKit.currentBehavior
```

But `BehaviorTestKit` is test-only and cannot be used on live actors.

### "Direct Field Update Without Transition"

No mechanism. Actor state is encapsulated in the behavior's closure. External mutation is explicitly prevented by the actor model's encapsulation guarantee.

### Return Type: Typed vs Stringly Typed

Strongly typed at the Scala/Java level. `ActorRef[T]` is parameterized by the exact message type `T` — only messages of type `T` can be sent. Response types are encoded in the message protocol (e.g., `replyTo: ActorRef[Response]`). The compiler enforces protocol adherence.

### Guard vs No-Transition Discrimination

Akka Typed's `Behaviors.receiveMessage` / `Behaviors.receive` are pattern-match handlers. If no case matches a message, the behavior should return `Behaviors.unhandled` to signal that the message was not processed. The system then applies supervision strategies (default: log and continue). The caller receives **no feedback** about whether the message was handled.

For ask patterns, failure to reply within the timeout surfaces as a `TimeoutException` on the `Future`. But the caller cannot distinguish "actor is in a state that doesn't handle this message" from "actor crashed" from "actor is slow".

---

## Redux / Redux Toolkit

Source: https://redux-toolkit.js.org/api/createSlice  
Redux v5.x, Redux Toolkit v2.x

Redux is a predictable state container for JavaScript applications. It models application state as a single immutable object tree and state transitions as pure reducer functions: `(state, action) → state`. Running state machines are not typically represented as actor instances; the "running instance" is the Redux store, and state machine state lives within the store's state tree.

### Instance Identity

There is typically a single Redux `store` per application. If multiple state machine instances are needed, they are represented as keyed objects within the state tree, e.g.:

```typescript
// State tree with multiple machine instances
interface AppState {
  orders: Record<string, OrderState>; // keyed by orderId
}
```

The "running instance" concept is approximated by keys in the state tree, not by live actor references.

### Creating a Slice

`createSlice` defines a "slice" of state — the state subtree, reducer logic, and action creators for a feature domain:

```typescript
import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';

interface CounterState { value: number }
const initialState: CounterState = { value: 0 };

const counterSlice = createSlice({
  name: 'counter',                  // used as action type prefix
  initialState,
  reducers: {
    increment(state) {
      state.value++;                // Immer-powered "mutation" of draft state
    },
    incrementByAmount(state, action: PayloadAction<number>) {
      state.value += action.payload;
    },
  },
  selectors: {
    selectValue: (state) => state.value,
  },
});

export const { increment, incrementByAmount } = counterSlice.actions;
export default counterSlice.reducer;
```

`createSlice` return type:
```typescript
{
  name: string;
  reducer: ReducerFunction;              // suitable for combineReducers
  actions: Record<string, ActionCreator>;
  caseReducers: Record<string, CaseReducer>;
  getInitialState: () => State;
  reducerPath: string;
  selectSlice: Selector;
  selectors: Record<string, Selector>;
  getSelectors: (selectState: (root: RootState) => State) => Record<string, Selector>;
  injectInto: (injectable: Injectable, config?: InjectConfig) => InjectedSlice;
}
```

### The "Fire" Operation: `store.dispatch(action)`

```typescript
store.dispatch(counterSlice.actions.increment());
store.dispatch(counterSlice.actions.incrementByAmount(5));
// → dispatched action object (same as input) is returned
// → return type: the action object itself (for middleware chaining)
```

`dispatch` signature:
```typescript
dispatch<T extends Action>(action: T): T
// With thunk middleware:
dispatch<R>(thunk: ThunkAction<R, State, Extra, AnyAction>): R
```

- `dispatch()` is **synchronous** for plain actions. The reducer runs synchronously; `getState()` reflects the new state immediately after `dispatch()` returns.
- `dispatch()` returns the dispatched action (or the thunk's return value if the action is a thunk).
- No error is thrown for "unhandled action" — reducers are expected to return the current state unchanged for unknown action types.

### The "Inspect/Query" Operation: `store.getState()`

```typescript
const state: RootState = store.getState();
// → the current state tree; synchronous; does not trigger any reducers
```

`getState()` is a pure read with no side effects. Return type is the root state type `RootState`.

Selectors (from `createSlice.selectors` or standalone) are pure functions over state:

```typescript
const { selectValue } = counterSlice.selectors;
const value: number = selectValue(store.getState());
// → number; pure function; no dispatch
```

`reselect`-based memoized selectors (`createSelector`) only recompute when their inputs change.

### Subscribing to State Changes

```typescript
const unsubscribe = store.subscribe(() => {
  const state = store.getState();
  // called synchronously after every dispatch
});
unsubscribe(); // remove listener
```

Return type: `() => void` (the unsubscribe function).

### "What Would Happen" Simulation

Since reducers are **pure functions**, the "what would happen" operation is trivially available:

```typescript
const nextState = counterSlice.reducer(currentState, counterSlice.actions.incrementByAmount(5));
// → State; the state that would result from the action; does not touch the store
```

Or via the `caseReducers` field:
```typescript
const nextState = counterSlice.caseReducers.incrementByAmount(currentState, { payload: 5, type: '' });
```

This is a first-class capability in Redux because reducers are pure functions. The store is not involved. This is the most complete "simulate without committing" API of any system surveyed.

### "Direct Field Update Without Transition"

Redux provides `store.dispatch(someAction)` as the only write path. There is no bypass. However, because reducers are pure, any `(state, action) → state` function can be used to compute a new state — the question is only whether it gets dispatched to the store.

For testing or offline computation, state can be computed by calling the reducer directly without dispatching to the store.

### Async Operations: `createAsyncThunk`

For async state machines (e.g., loading states), `createAsyncThunk` generates three action types:

```typescript
import { createAsyncThunk } from '@reduxjs/toolkit';

const fetchUser = createAsyncThunk(
  'user/fetch',
  async (userId: string, thunkApi) => {
    const response = await fetch(`/api/users/${userId}`);
    return response.json() as Promise<User>;
  }
);

// Generated action types:
// 'user/fetch/pending'
// 'user/fetch/fulfilled'
// 'user/fetch/rejected'
```

`dispatch(fetchUser('123'))` returns a `Promise` that resolves with the dispatched result action. The thunk middleware intercepts and executes the async function.

### Return Type: Typed vs Stringly Typed

Fully typed with TypeScript. `PayloadAction<T>` carries a typed payload. Selectors return typed values. Action creators are typed: `ActionCreatorWithPayload<number>`, `ActionCreatorWithoutPayload`, etc.

Action `type` strings are plain strings (e.g. `'counter/increment'`), which is a stringly-typed aspect — they are not opaque symbols. But `createSlice` generates them automatically and the TypeScript types narrow them appropriately.

### Guard vs No-Transition Discrimination

Redux reducers have no "guard" concept in the gen_statem sense. The reducer either handles the action (returns a different state) or does not (returns the same state). The caller cannot distinguish:
- "action type not recognized"
- "action type recognized but guard condition in reducer false"
- "action processed, no-op"

All three return the original state unchanged. The caller receives no error signal.

---

## Workflow Engines

Three workflow orchestration systems that use state machine concepts: Cadence, Apache Airflow, and AWS Step Functions.

### Cadence

Source: Uber Cadence (https://cadenceworkflow.io/) — the precursor to Temporal. Temporal is a fork of Cadence; their APIs are nearly identical.

Cadence exposes the same three message types: **signals** (async write), **queries** (sync read), and (in older versions) no update equivalent. Temporal's Update type was introduced after the Cadence/Temporal fork.

**Go SDK (Cadence) — core types:**

```go
// Workflow client (external caller)
type Client interface {
    // Execute workflow
    ExecuteWorkflow(ctx context.Context, options StartWorkflowOptions, workflow interface{}, args ...interface{}) (WorkflowRun, error)

    // Get handle to running workflow
    GetWorkflow(ctx context.Context, workflowID string, runID string) WorkflowRun

    // Signal: async write, returns error only for transport failure
    SignalWorkflow(ctx context.Context, workflowID string, runID string, signalName string, arg interface{}) error

    // Query: sync read, returns (result, error)
    QueryWorkflow(ctx context.Context, workflowID string, runID string, queryType string, args ...interface{}) (encoded.Value, error)
}

// Inside the workflow:
cadence.SetQueryHandler(ctx, "getStatus", func() (string, error) {
    return currentStatus, nil
})
```

`QueryWorkflow` returns `(encoded.Value, error)` — a decoded value that must be populated into a typed variable:
```go
var result string
val, err := client.QueryWorkflow(ctx, wfID, "", "getStatus")
if err == nil {
    val.Get(&result)
}
```

Cadence instance identity: `(workflowID string, runID string)` pair. `workflowID` is user-supplied; `runID` is auto-generated per run.

---

### Apache Airflow

Source: https://airflow.apache.org/docs/apache-airflow/stable/stable-rest-api-ref.html

Apache Airflow orchestrates DAG (Directed Acyclic Graph) pipelines. The "state machine" in Airflow is a **DAG Run** — an instance of a DAG with a specific `dag_run_id`. Each task within the DAG Run has its own state.

DAG Run states: `queued`, `running`, `success`, `failed`, `skipped`.  
Task Instance states: `none`, `removed`, `scheduled`, `queued`, `running`, `success`, `failed`, `upstream_failed`, `skipped`, `restarting`, `deferred`, `sensing`.

**Instance identity:** `(dag_id: string, dag_run_id: string)`. Also `task_id` for individual task instances.

**REST API operations:**

```http
# Fire (trigger a DAG run)
POST /api/v1/dags/{dag_id}/dagRuns
Body: { "dag_run_id": "my-run-001", "conf": {...}, "logical_date": "2024-01-01T00:00:00Z" }
Response: DagRun object

# Inspect (get DAG Run state)
GET /api/v1/dags/{dag_id}/dagRuns/{dag_run_id}
Response: {
  "dag_run_id": "my-run-001",
  "dag_id": "my_dag",
  "state": "running",  // "queued" | "running" | "success" | "failed"
  "start_date": "2024-01-01T00:01:00Z",
  "end_date": null,
  "conf": {...},
  ...
}

# Get task instance states
GET /api/v1/dags/{dag_id}/dagRuns/{dag_run_id}/taskInstances
Response: { "task_instances": [{ "task_id": "...", "state": "running", ... }] }

# Update task state directly (force state change without executing transition)
PATCH /api/v1/dags/{dag_id}/dagRuns/{dag_run_id}/taskInstances/{task_id}
Body: { "dry_run": false, "new_state": "success", "include_downstream": false }
```

Airflow does support `dry_run: true` in some patch operations to preview what would be affected.

There is no equivalent to signal/query/update semantics. DAG Runs can be paused, resumed, cleared (re-run tasks), and marked as success/failed via the API. All operations are JSON over HTTP; no type safety.

---

### AWS Step Functions

Source: https://docs.aws.amazon.com/step-functions/latest/apireference/  
AWS Step Functions Express and Standard workflows.

Step Functions defines state machines as Amazon States Language (ASL) JSON documents. Each execution is a running instance.

**Instance identity:** `executionArn` — an Amazon Resource Name (ARN) string, e.g.:
```
arn:aws:states:us-east-1:123456789012:execution:MyStateMachine:my-execution-001
```

Also: `stateMachineArn` (identifies the definition) + `name` (user-supplied execution name).

**Core API operations (AWS SDK / REST):**

```typescript
// Start an execution (fire)
const response = await sfn.startExecution({
  stateMachineArn: 'arn:aws:states:...:stateMachine:MyMachine',
  name: 'my-execution-001',       // optional; idempotency key within 90 days
  input: JSON.stringify({ orderId: '123' })
}).promise();
// Returns: { executionArn: string, startDate: Date }

// Inspect: describe current execution
const desc = await sfn.describeExecution({
  executionArn: 'arn:aws:states:...:execution:MyMachine:my-execution-001'
}).promise();
// Returns:
// {
//   executionArn: string,
//   stateMachineArn: string,
//   name: string,
//   status: 'RUNNING' | 'SUCCEEDED' | 'FAILED' | 'TIMED_OUT' | 'ABORTED',
//   startDate: Date,
//   stopDate?: Date,
//   input: string,     // original input JSON string
//   output?: string,   // result JSON string; set when status === 'SUCCEEDED'
//   error?: string,    // error name; set when status is terminal failure
//   cause?: string,    // error cause description
// }

// Inspect: get full event history
const history = await sfn.getExecutionHistory({
  executionArn: '...',
  maxResults: 1000,
  reverseOrder: false,
  nextToken?: string  // pagination
}).promise();
// Returns: { events: HistoryEvent[], nextToken?: string }
// HistoryEvent: { id, previousEventId, timestamp, type, ...typeSpecificFields }
// types: ExecutionStarted, TaskStateEntered, TaskStateExited, WaitStateEntered,
//        ChoiceStateEntered, SucceedStateEntered, FailStateEntered, ExecutionSucceeded, etc.
```

**Sending input to a running execution (callback pattern):**

Step Functions supports a "wait for task token" pattern. The execution pauses at a task state, emits a task token, and waits for an external caller to complete or fail it:

```typescript
// Resume a paused execution with task token
await sfn.sendTaskSuccess({
  taskToken: '...',     // opaque token emitted when state machine reached the Wait state
  output: JSON.stringify({ approved: true })
}).promise();
// Returns: {} (empty on success)

await sfn.sendTaskFailure({
  taskToken: '...',
  error: 'ApprovalDenied',
  cause: 'Finance rejected the request'
}).promise();
// Returns: {}

// Heartbeat to prevent timeout
await sfn.sendTaskHeartbeat({ taskToken: '...' }).promise();
```

This is the closest Step Functions has to a "signal" — it is event-driven via task tokens rather than named signals.

**No "what would happen" simulation.** Step Functions does not expose a dry-run or simulation API for running executions. The Step Functions Local developer tool allows offline simulation for testing, but this is not a runtime API.

**No "direct field update without transition."** The execution is opaque; input data flows between states via the `InputPath` / `OutputPath` / `ResultPath` / `Parameters` mapping in the ASL definition. External callers cannot bypass the state machine and directly write fields.

**Return types:** Stringly typed. `input`, `output` are JSON strings. `status` is a string enum. `error` and `cause` are plain strings. The AWS SDK provides TypeScript types for the request/response shapes, but the state machine's own data is untyped JSON.

**Guard vs No-Transition Discrimination:**
Step Functions Choice states select transitions based on data conditions. If no branch matches and there is no `Default` branch, the execution fails with `States.NoChoiceMatched`. This error is exposed in `describeExecution` as `error: "States.NoChoiceMatched"`. The caller can distinguish "no matching choice" (this specific error code) from other failures, making Step Functions slightly better than most systems surveyed in this regard.

---

*This document is raw research. Compiled from official documentation, W3C specifications, and Erlang OTP reference manuals. No conclusions about design choices or applicability are drawn here.*
