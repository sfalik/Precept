# Dry-Run / Preview / Inspect API Survey

**Research question:** What systems let you ask "if I do X, what would happen?" without actually committing the change? How is this API designed, what does it return, and how does it differ from the real operation?

**Scope:** Raw external research. No interpretation, no conclusions, no recommendations for Precept.

**Date collected:** 2026-04-20

---

## Table of Contents

1. [Terraform `plan`](#1-terraform-plan)
2. [Kubernetes Dry-Run](#2-kubernetes-dry-run)
3. [Temporal.io Update Validators](#3-temporalio-update-validators)
4. [XState `machine.transition()` and `transition()`](#4-xstate-machinetransition-and-transition)
5. [Pulumi `preview`](#5-pulumi-preview)
6. [Database Transactions as Dry-Run (`BEGIN` / `ROLLBACK`)](#6-database-transactions-as-dry-run-begin--rollback)
7. [Git `--dry-run` and `--no-commit`](#7-git---dry-run-and---no-commit)
8. [OPA / Rego Partial Evaluation](#8-opa--rego-partial-evaluation)
9. [AWS Step Functions](#9-aws-step-functions)
10. [Dafny / Formal Verification as Preview](#10-dafny--formal-verification-as-preview)
11. [Type Checkers as Preview](#11-type-checkers-as-preview)

---

## 1. Terraform `plan`

### 1.1 API Signature

`terraform plan` is a CLI subcommand. It does not have a REST API in the local case — it is invoked as a subprocess.

```
terraform plan [options]
```

Key options relevant to preview semantics:

| Option | Effect |
|--------|--------|
| (no option) | Creates a *speculative plan* — not saved, not executable |
| `-out=FILE` | Saves the plan to a binary file that `terraform apply FILE` can later execute |
| `-json` | Emits machine-readable JSON UI output to stdout |
| `-destroy` | Plans a destroy operation |
| `-refresh-only` | Plans only to reconcile state with remote objects, no configuration changes |
| `-target=ADDRESS` | Restricts planning to a specific resource address |
| `-replace=ADDRESS` | Forces a replace action for a resource that would otherwise be unchanged |
| `-detailed-exitcode` | Returns exit code 2 if there are changes, 0 if no changes, 1 if error |

**Source:** https://developer.hashicorp.com/terraform/cli/commands/plan

---

### 1.2 What `plan` Returns vs. What `apply` Returns

`terraform plan` without `-out` returns:

- **Human-readable diff summary** to stdout/stderr showing:
  - Resources to create (`+`)
  - Resources to destroy (`-`)
  - Resources to update in-place (`~`)
  - Resources to destroy and recreate (`-/+`)
  - Resources to replace (`+/-`)
  - Resources with no changes (omitted by default)
- **Counts**: "Plan: X to add, Y to change, Z to destroy."
- **Exit code**: standard 0/1, or 0/1/2 with `-detailed-exitcode`

`terraform plan -out=tfplan` additionally returns:

- A **binary plan file** (opaque format, not a standard JSON file). This file contains:
  - The full configuration
  - All planned changes with before/after resource states
  - All input variable values (potentially including sensitive values in cleartext)
  - The planning options used

The binary plan file can be converted to JSON with:

```
terraform show -json tfplan
```

The JSON output format contains (from Terraform's internal JSON output format documentation):

```json
{
  "format_version": "1.0",
  "variables": { ... },
  "planned_values": {
    "root_module": {
      "resources": [
        {
          "address": "aws_instance.example",
          "type": "aws_instance",
          "name": "example",
          "values": { ... }
        }
      ]
    }
  },
  "resource_changes": [
    {
      "address": "aws_instance.example",
      "type": "aws_instance",
      "name": "example",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "ami": "ami-0c55b159cbfafe1f0",
          "instance_type": "t2.micro"
        },
        "after_unknown": {
          "id": true,
          "public_ip": true
        },
        "before_sensitive": {},
        "after_sensitive": {}
      }
    }
  ],
  "configuration": { ... },
  "prior_state": { ... }
}
```

Key fields in `resource_changes[].change`:

- `actions` — array of strings: `["no-op"]`, `["create"]`, `["read"]`, `["update"]`, `["delete"]`, `["delete","create"]`, `["create","delete"]`
- `before` — the current state of the resource (null for creates)
- `after` — the planned state after apply (null for destroys)
- `after_unknown` — map of fields whose values are unknown until apply (computed values)
- `before_sensitive` / `after_sensitive` — which fields contain sensitive values

`terraform apply` runs all the same logic as plan but also:
- Locks the state file
- Executes the actual provider API calls
- Writes the resulting state back to the state file
- Returns the final resource states (not planned states)

---

### 1.3 What `plan` Cannot Tell You

- **Computed field values**: Fields marked `(known after apply)` are unknown during plan. The `after_unknown` map identifies these. Example: AWS assigns an instance's public IP only after creation — `plan` shows `(known after apply)` for `public_ip`.
- **Provider-side race conditions**: Another process may modify the resource between plan and apply. The plan snapshot is stale as soon as it is produced.
- **Partial apply failures**: If `apply` fails partway through a multi-resource operation, `plan` cannot predict which partial state will result.
- **Drift since refresh**: If `-refresh=false` is used, the plan is based on cached state and may not reflect actual infrastructure.
- **Cross-plan dependencies**: The plan file has no notion of another apply running concurrently against the same state.

---

### 1.4 How It Is Triggered

- **Same binary, different subcommand** from `apply`. Both share planning options.
- `plan` without `-out`: speculative, produces no artifact.
- `plan -out=FILE`: creates a plan artifact that `apply FILE` can consume.
- In **Terraform Cloud / Enterprise**: Plans are triggered via API as part of a "Run" object. The structured run API exposes a plan phase with status transitions.

Terraform Cloud Run API (abbreviated):

```
POST /api/v2/runs
{
  "data": {
    "type": "runs",
    "attributes": {
      "is-destroy": false,
      "plan-only": true    // <- creates a speculative plan that cannot be applied
    },
    "relationships": { ... }
  }
}
```

A `plan-only: true` run in Terraform Cloud is explicitly a preview. It runs through the plan phase and produces structured plan output but the "Confirm & Apply" button is disabled.

---

### 1.5 Consistency Guarantees

- `plan` reads the current remote state (by default) using provider APIs.
- The plan represents a snapshot at the time of the `plan` invocation.
- State is locked during apply (not during plan in many backends).
- **Between plan and apply, another apply may run and invalidate the plan.** Terraform will detect some of these conflicts (e.g., resource state drift), but not all.
- Terraform documentation explicitly states: "you should always re-check the final non-speculative plan before applying to make sure that it still matches your intent."

---

### 1.6 Cacheability / Reusability

- A saved plan file (`-out=FILE`) is reusable exactly once: `terraform apply FILE` consumes it.
- After apply, the plan file is stale.
- In Terraform Cloud, each run has its own plan artifact. Plans are not shared across runs.
- Plans cannot be applied against a different workspace or state than they were generated from.

---

### 1.7 Failure Modes Unique to `plan` vs. `apply`

**Unique to `plan`**:
- **Provider authentication errors during refresh**: plan reads actual infrastructure, so access errors surface during planning.
- **State lock contention**: if another `apply` holds the lock, `plan` may block or fail.
- **False positives on "no changes needed"**: if `-refresh=false`, plan may incorrectly report no changes when drift has occurred.

**Unique to `apply`**:
- **Provider API failures**: the actual resource creation/update/deletion call fails.
- **Partial apply state**: if apply fails mid-run, some resources are created/modified and others are not.
- **Timeout failures**: provider operations have timeouts; plan does not execute operations.

---

## 2. Kubernetes Dry-Run

### 2.1 API Signature

Kubernetes dry-run is a query parameter on mutating HTTP requests. It applies to HTTP verbs `POST`, `PUT`, `PATCH`, and `DELETE`.

```
?dryRun=All
```

The `dryRun` parameter is an enum string with two accepted values:

- **[no value set]** — allow side effects (standard non-dry-run behavior). Can be expressed as `?dryRun` or `?dryRun&pretty=true`.
- **`All`** — every stage runs normally except the final storage stage. No side effects are committed.

**Example dry-run request:**

```http
POST /api/v1/namespaces/test/pods?dryRun=All
Content-Type: application/json
Accept: application/json

{ ... pod spec ... }
```

The response looks identical to a non-dry-run response — the API server returns the final object that *would have been* persisted, or an error if the request could not be fulfilled.

**Source:** https://kubernetes.io/docs/reference/using-api/api-concepts/#dry-run

---

### 2.2 `kubectl apply --dry-run=client` vs. `--dry-run=server`

`kubectl apply` exposes two dry-run modes:

**`--dry-run=client`**:
- Entirely client-side.
- Does not send the request to the API server at all.
- kubectl prints the object that *would* be submitted.
- No admission controllers run. No server-side defaulting. No webhook validation.
- Use case: quickly see what kubectl would send, without any network round-trip.

**`--dry-run=server`**:
- Sends the request to the API server with `?dryRun=All`.
- The full admission pipeline runs.
- Server-side defaulting is applied.
- Validating and mutating admission webhooks are called (if they declare `sideEffects: None` or `sideEffects: NoneOnDryRun`).
- The server returns what would be stored.
- No object is written to etcd.

---

### 2.3 What Server-Side Dry-Run Returns

The response body for a dry-run POST is the full Kubernetes object that *would* be stored, with some generated fields populated differently:

**Generated fields that differ in dry-run:**

| Field | Dry-run behavior |
|-------|-----------------|
| `metadata.name` (when `generateName` is set) | Has a unique random name — but a *different* random name than will be used at actual creation |
| `metadata.creationTimestamp` | Records the time of the dry-run request, not the actual creation time |
| `metadata.deletionTimestamp` | Reflects the time of the dry-run request |
| `metadata.uid` | A randomly generated non-deterministic UID; will be different at actual creation |
| `metadata.resourceVersion` | Reflects persisted version tracking — will differ |
| Any fields set by mutating admission controllers | Applied during dry-run, so reflected in the response |
| `spec.clusterIP`, `spec.ports` for `Service` resources | Assigned by kube-apiserver; dry-run may assign different values |

**Source:** https://kubernetes.io/docs/reference/using-api/api-concepts/#generated-values

---

### 2.4 The Admission Webhook Pipeline in Dry-Run

When `?dryRun=All` is set:

1. Mutating admission webhooks run.
2. Validating admission webhooks run.
3. Schema validation runs.
4. The object is **not** written to etcd.

Webhooks receive the full `AdmissionReview` request object:

```json
{
  "apiVersion": "admission.k8s.io/v1",
  "kind": "AdmissionReview",
  "request": {
    "uid": "705ab4f5-...",
    "kind": { "group": "...", "version": "v1", "kind": "Pod" },
    "resource": { "group": "", "version": "v1", "resource": "pods" },
    "operation": "CREATE",
    "dryRun": true,
    "object": { ... },
    "oldObject": null,
    ...
  }
}
```

The `dryRun: true` field in the request payload signals to webhooks that this is a dry-run. Webhooks that have side effects (e.g., creating external resources) **must** suppress those side effects when `dryRun` is true.

A webhook declares whether it can handle dry-run via its `sideEffects` field:

```yaml
apiVersion: admissionregistration.k8s.io/v1
kind: ValidatingWebhookConfiguration
webhooks:
  - name: my-webhook.example.com
    sideEffects: NoneOnDryRun   # OR: None
```

- `sideEffects: None` — the webhook never has side effects; it is always called in dry-run.
- `sideEffects: NoneOnDryRun` — the webhook suppresses side effects when `dryRun: true`; it is called in dry-run.
- `sideEffects: Some` or `sideEffects: Unknown` — the webhook is **skipped** in dry-run. The dry-run request will fail if any webhook it would normally invoke has `sideEffects: Some/Unknown` and is not excluded.

Webhook responses for dry-run:

```json
{
  "apiVersion": "admission.k8s.io/v1",
  "kind": "AdmissionReview",
  "response": {
    "uid": "<value from request.uid>",
    "allowed": false,
    "status": {
      "code": 403,
      "message": "Dry-run rejected: resource quota exceeded"
    }
  }
}
```

**Source:** https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/#side-effects

---

### 2.5 Authorization for Dry-Run

Authorization is identical for dry-run and non-dry-run requests. RBAC rules apply equally. A dry-run `PATCH` to a `Deployment` requires the same `patch` verb on `deployments` as a real patch.

---

### 2.6 What Dry-Run Cannot Tell You

- **Actual etcd write contention**: Dry-run does not acquire storage locks or version-check against etcd. A dry-run success does not guarantee the actual write will succeed (e.g., if another writer changes the object between dry-run and actual apply).
- **Controller side-effects**: Kubernetes controllers (Deployment controller, ReplicaSet controller) react to resource creation by creating further resources. Dry-run of a Deployment creation does not simulate the Deployment controller creating ReplicaSets.
- **Webhook side effects (suppressed)**: If a webhook would normally perform an external side effect (e.g., creating a DNS record), that does not happen in dry-run. The dry-run result reflects the webhook's behavior only in terms of admission decisions, not downstream effects.
- **Exact generated field values**: `uid`, `resourceVersion`, `creationTimestamp`, and `generateName`-derived names will differ between dry-run and actual creation.

---

### 2.7 Consistency Guarantees

- Server-side dry-run reads current object state from the API server (which reads from etcd via the watch cache or directly). The state observed during dry-run is a consistent snapshot at the time of the request.
- No state is written; therefore, the dry-run result is not linearizable with concurrent writes.
- Multiple concurrent dry-run requests against the same object are independent and non-conflicting with each other and with real requests.

---

### 2.8 Cacheability / Reusability

- Dry-run results are **not cacheable** in any formal sense. The API server does not cache them.
- Each dry-run request re-executes the full admission pipeline against the current cluster state.
- The result is valid only at the moment of the request.

---

### 2.9 Failure Modes Unique to Dry-Run

- **Webhook exclusion**: If a webhook does not declare `sideEffects: None` or `sideEffects: NoneOnDryRun`, the dry-run request is rejected with an error rather than calling the webhook. This is a fail-closed behavior: it's better to refuse the dry-run than to accidentally trigger a side effect.
- **Webhook inconsistency**: A webhook that suppresses side effects in dry-run may make different decisions than it would with full access to external systems. The dry-run result reflects the webhook's "side-effect-suppressed" code path.

---

## 3. Temporal.io Update Validators

### 3.1 Architecture: Signals, Queries, and Updates

Temporal Workflows can receive three kinds of messages:

| Type | Direction | Blocking | Mutates state | Returns value |
|------|-----------|----------|--------------|---------------|
| Query | Read | No | No | Yes |
| Signal | Write | No (fire-and-forget) | Yes | No |
| Update | Read/Write | Yes | Yes (if accepted) | Yes |

An **Update** is a synchronous write request. The sender blocks until the Worker acknowledges it.

**Source:** https://docs.temporal.io/develop/go/message-passing

---

### 3.2 The Validator Function

An Update handler can optionally declare a **validator function** that runs *before* the handler body. The validator is the closest Temporal gets to a "what would happen if I do this?" API.

In Go:

```go
err = workflow.SetUpdateHandlerWithOptions(
    ctx,
    "set-language",
    func(ctx workflow.Context, newLanguage Language) (Language, error) {
        // 👉 This is the handler. It CAN mutate workflow state.
        var previousLanguage Language
        previousLanguage, language = language, newLanguage
        return previousLanguage, nil
    },
    workflow.UpdateHandlerOptions{
        Validator: func(ctx workflow.Context, newLanguage Language) error {
            if _, ok := greeting[newLanguage]; !ok {
                // 👉 In a validator, you return an error to reject the Update.
                return fmt.Errorf("%s unsupported language", newLanguage)
            }
            return nil
        },
    },
)
```

The validator:
- Receives the same argument types as the handler.
- Returns a single value of type `error`.
- If it returns a non-nil error or panics, the Update is **rejected** before being written to history.
- If it returns nil, the Update is **accepted** and the handler runs.

---

### 3.3 What the Validator Sees vs. What the Handler Sees

The validator sees:
- The current workflow state (readable closure variables, context, etc.)
- The Update's arguments
- The `workflow.Context`

The validator does **not** see:
- Any pending state mutations from prior Update handlers that have been accepted but not yet completed
- Any buffered signals not yet processed
- Workflow state changes that will result from the handler itself

The validator runs **synchronously on the workflow goroutine**, so it does have access to the workflow's current state — but it cannot block (no `workflow.Await`, no Activities, no Child Workflows).

From the Temporal documentation:
> "Use validators to reject an Update before it is written to History. Validators are always optional."

---

### 3.4 How Rejection Differs from Handler Failure

**Validator rejection:**
- The Update is rejected **before** `WorkflowExecutionUpdateAccepted` is written to History.
- The caller receives an "Update failed" error.
- No history event is written.
- The workflow state is **unchanged**.

**Handler failure:**
- The Update has already been accepted (`WorkflowExecutionUpdateAccepted` is in History).
- The handler ran and returned an error.
- `WorkflowExecutionUpdateCompleted` is written to History with the failure.
- The workflow state **may have been partially mutated** before the error.

This is a critical distinction: the validator is the pre-commit gate, the handler is post-commit execution.

---

### 3.5 The `WaitForStage` Parameter

When sending an Update from a client, the caller specifies how far to wait:

```go
updateHandle, err := temporalClient.UpdateWorkflow(
    ctxWithTimeout,
    client.UpdateWorkflowOptions{
        WorkflowID:  we.GetID(),
        RunID:       we.GetRunID(),
        UpdateName:  "set-language",
        WaitForStage: client.WorkflowUpdateStageAccepted,
        // OR: client.WorkflowUpdateStageCompleted
        Args: []interface{}{message.Chinese},
    },
)
```

- `WorkflowUpdateStageAccepted`: The client blocks until the validator has run and accepted the Update. The handler may still be running.
- `WorkflowUpdateStageCompleted`: The client blocks until the handler has fully completed and returned a result.

`WaitForStage: Accepted` is effectively a "did the validator pass?" check — the client can confirm acceptance without waiting for the full handler execution.

---

### 3.6 What Validators Cannot Tell You

- **Handler execution results**: The validator does not know what the handler will return or whether the handler will fail.
- **Handler side effects**: The validator does not know what Activities the handler will execute or what their results will be.
- **Race with concurrent Updates**: If two Updates are sent concurrently, the validator for the second runs after the first is accepted but the handler state changes from the first may not yet be applied when the second validator runs.

---

### 3.7 How It Is Triggered

- **Same endpoint, different lifecycle stage.** The Update is sent via `UpdateWorkflow`. The validator is called automatically by the Temporal Worker before the handler. There is no separate "validate-only" endpoint.
- The validator is not a separate RPC call — it is part of the Update's processing pipeline on the Worker.

---

### 3.8 Consistency Guarantees

- The validator runs on the workflow goroutine, observing the same consistent workflow state as the handler.
- If the validator passes, there is no guarantee that the handler will succeed — the handler may still fail for reasons the validator cannot check (e.g., an Activity it invokes fails).
- If the validator fails, the workflow state is exactly as it was before the Update request was received.

---

### 3.9 Cacheability / Reusability

- Validator results are not cached. Each Update request runs the validator fresh.
- An accepted Update (passed validator) is durable — if the Worker crashes before the handler completes, the Update is replayed from History. The validator is **not** replayed on replay — only the handler is.

---

## 4. XState `machine.transition()` and `transition()`

### 4.1 Overview

XState (v4 and v5) provides pure functions for computing state transitions without executing side effects. These are the primary "what would happen" APIs in XState.

**Sources:**
- https://stately.ai/docs/machines (XState v5)
- https://xstate.js.org/docs/guides/states.html (XState v4)

---

### 4.2 XState v4: `machine.transition(state, event)`

In XState v4, `machine.transition()` is a pure function on the machine object:

```javascript
const lightMachine = createMachine({
  id: 'light',
  initial: 'green',
  states: {
    green: { on: { TIMER: 'yellow' } },
    yellow: { on: { TIMER: 'red' } },
    red: { on: { TIMER: 'green' } }
  }
});

console.log(lightMachine.initialState);
// State {
//   value: 'green',
//   actions: [],
//   context: undefined,
//   // ...
// }

const nextState = lightMachine.transition('yellow', { type: 'TIMER' });
// State {
//   value: { red: 'walk' },
//   actions: [],
//   context: undefined,
//   // ...
// }
```

The returned `State` object contains:

| Field | Type | Description |
|-------|------|-------------|
| `value` | string or object | The next state value (e.g., `'green'` or `{red: 'walk'}`) |
| `context` | any | The updated context after the transition |
| `event` | object | The event object that triggered this state |
| `actions` | array | **Actions that would be executed** — listed but NOT executed |
| `activities` | object | Map of activity IDs to `true` (started) or `false` (stopped) |
| `history` | State | The previous State instance |
| `meta` | object | Static meta data from state nodes |
| `done` | boolean | Whether this is a final state |
| `changed` | boolean or undefined | Whether the state changed from the previous state |

The **`actions` array** is the core "what would happen" payload: it lists every action that would be dispatched during this transition, but `machine.transition()` does not execute them. Actions remain pure descriptions.

---

### 4.3 `state.can(event)` — Predicate Check

XState v4 (4.25.0+) provides a predicate method:

```javascript
const inactiveState = machine.initialState;

inactiveState.can({ type: 'TOGGLE' }); // true  — this event causes a state change
inactiveState.can({ type: 'DO_SOMETHING' }); // false — no matching transition
```

`state.can(event)` returns `true` if sending the event would cause a state change (state value change, new actions, or context change). Internally it calls `machine.transition()` and checks whether `state.changed === true`.

**Important:** Guard functions are executed during `state.can()`. If guards have side effects, they will fire.

---

### 4.4 `state.nextEvents` — Available Events

```javascript
const { initialState } = lightMachine;

console.log(initialState.nextEvents);
// => ['TIMER', 'EMERGENCY']
```

`state.nextEvents` is a list of event types that have registered transitions from the current state. It is useful for determining which events can be taken and representing potential events in UI (e.g., enabling/disabling buttons).

---

### 4.5 XState v5: `transition()` Function

In XState v5, the recommendation moved away from `machine.transition()` toward standalone functions:

```javascript
import { createMachine, initialTransition, transition } from 'xstate';

const machine = createMachine({
  initial: 'pending',
  states: {
    pending: {
      on: { start: { target: 'started' } }
    },
    started: {
      entry: 'doSomething'
    }
  }
});

const [initialState, initialActions] = initialTransition(machine);
console.log(initialState.value); // logs 'pending'
console.log(initialActions);     // logs []

const [nextState, actions] = transition(machine, initialState, {
  type: 'start'
});
console.log(nextState.value);    // logs 'started'
console.log(actions);            // logs [{ type: 'doSomething', … }]
```

`transition(machine, state, event)` returns a tuple `[nextState, actions]`:

- `nextState` — the new `State` snapshot after the transition
- `actions` — the array of action objects that *would* execute; they are not invoked

Both `initialTransition` and `transition` are **pure functions with no side effects**.

---

### 4.6 `getNextTransitions(state)` — Transition Inspection (v5.26.0+)

```javascript
import { createMachine, createActor, getNextTransitions } from 'xstate';

const actor = createActor(machine).start();
const transitions = getNextTransitions(actor.getSnapshot());

console.log(transitions.map(t => t.eventType));
// logs ['start', 'reset']
```

Each transition object has:

| Property | Description |
|----------|-------------|
| `eventType` | The event type string |
| `target` | The state node that the transition targets |
| `source` | The state node where the transition originates |
| `actions` | Actions that would execute during the transition |
| `reenter` | Whether the transition is reentrant |
| `guard` | The guard condition (not evaluated, just described) |

---

### 4.7 `createActor(machine)` vs. `machine.transition()`

| | `createActor(machine)` | `transition(machine, state, event)` |
|--|------------------------|-------------------------------------|
| **Purpose** | Running actor instance | Pure state computation |
| **Side effects** | Actions are executed | Actions are returned, not executed |
| **State persistence** | Maintains state across events | Stateless — caller provides state each time |
| **Services/activities** | Spawned and managed | Not started |
| **Use case** | Production application state | Testing, inspection, prediction |

---

### 4.8 What `transition()` Cannot Tell You

- **Action results**: If an action invokes an external service or has a side effect, `transition()` cannot tell you what that side effect produces.
- **Guard evaluation with external state**: Guards are re-evaluated with the provided context and event. If a guard reads from external state, `transition()` uses whatever was in the context at call time.
- **Invoked service behavior**: `transition()` does not start invoked services (Promises, observables, machines). The `invoke` configuration is described in the returned state but not executed.
- **Delayed transitions**: Delayed (`after`) transitions are not triggered by `transition()`; they require a running actor with access to real timers.

---

### 4.9 How It Is Triggered

- `machine.transition()` (v4) and `transition()` (v5) are synchronous function calls. No network, no events, no subscriptions.
- They are intentionally the simplest possible API: pure function, state in, state out.

---

### 4.10 Consistency Guarantees

- Both functions are **deterministic** given the same inputs.
- They observe only the context and state values passed in; they do not read external state.
- No race conditions are possible — they are synchronous and pure.

---

### 4.11 Cacheability / Reusability

- Results are fully cacheable — the output is a function of the machine definition, the current state, and the event.
- The returned `State` object is immutable and JSON-serializable.
- XState v4 explicitly notes: "`state.history` will not retain its history in order to prevent memory leaks."

---

## 5. Pulumi `preview`

### 5.1 API Signature

`pulumi preview` is a CLI subcommand:

```
pulumi preview [url] [flags]
```

Key options:

| Option | Effect |
|--------|--------|
| `-j` / `--json` | Serialize preview diffs, operations, and output as JSON; set `PULUMI_ENABLE_STREAMING_JSON_PREVIEW` to stream JSON events instead |
| `--diff` | Display operations as rich diffs showing the overall change |
| `--expect-no-changes` | Return an error if any changes are proposed (useful in CI to assert a stack is up-to-date) |
| `--save-plan FILE` | [PREVIEW] Save the proposed operations to a plan file |
| `--show-sames` | Show resources that are unchanged alongside those that will change |
| `--show-replacement-steps` | Show detailed resource replacement create/delete steps instead of a combined step |
| `--show-reads` | Show resources being read in, alongside those being managed |
| `--target URN` | Restrict preview to a specific resource URN |
| `--replace URN` | Force replacement of the specified resource |

**Source:** https://www.pulumi.com/docs/iac/cli/commands/pulumi_preview/

---

### 5.2 What `preview` Returns vs. What `up` Returns

`pulumi preview` returns:

- **Human-readable diff summary** to stdout:
  - Resources to create (`+`)
  - Resources to destroy (`-`)
  - Resources to update in-place (`~`)
  - Resources to replace (`-` then `+`, or `+/-`)
  - Resources with no changes (`=`, only with `--show-sames`)
- **Property-level diffs**: For updates, Pulumi shows which specific properties changed, with old and new values
- **Summary counts**: "Previewing update (dev): X resources to create, Y to update, Z to delete"

With `--json`, Pulumi emits structured JSON. The structure (from Pulumi's event streaming API) includes per-resource events:

```json
{
  "type": "resourcePreEvent",
  "resourcePreEvent": {
    "metadata": {
      "op": "update",
      "urn": "urn:pulumi:dev::mystack::aws:s3/bucket:Bucket::my-bucket",
      "type": "aws:s3/bucket:Bucket",
      "old": {
        "type": "aws:s3/bucket:Bucket",
        "inputs": { "bucketName": "old-name" },
        "outputs": { "id": "old-name", "arn": "arn:aws:s3:::old-name" }
      },
      "new": {
        "type": "aws:s3/bucket:Bucket",
        "inputs": { "bucketName": "new-name" }
      },
      "diffs": ["bucketName"],
      "detailedDiff": {
        "bucketName": {
          "kind": "update",
          "inputDiff": true
        }
      }
    }
  }
}
```

`pulumi up` (the apply operation) additionally:
- Creates/updates/deletes the actual cloud resources via provider API calls
- Writes updated state to the Pulumi state backend (S3, Pulumi Cloud, etc.)
- Returns the final resource outputs (not planned outputs)
- Computed outputs that were unknown during preview are resolved to real values

---

### 5.3 Resource Operation Types

Pulumi's `preview` classifies each resource's operation as one of:

| Operation | Symbol | Meaning |
|-----------|--------|---------|
| `create` | `+` | Resource does not exist; will be created |
| `delete` | `-` | Resource exists; will be deleted |
| `update` | `~` | Resource exists; some properties will change in-place |
| `replace` | `-/+` or `+/-` | Resource must be deleted and recreated |
| `read` | (no symbol) | Data source read; no mutation |
| `same` | `=` | Resource unchanged (shown only with `--show-sames`) |
| `import` | special | Resource will be imported from existing infrastructure |

---

### 5.4 The `detailedDiff` Structure

For update and replace operations, Pulumi provides a `detailedDiff` map at the property level:

```json
"detailedDiff": {
  "tags[\"Environment\"]": {
    "kind": "update",      // OR: "add", "delete", "update", "updateReplace"
    "inputDiff": true
  }
}
```

`kind` values:
- `add` — property is being added
- `delete` — property is being deleted
- `update` — property is being changed in-place
- `updateReplace` — property change requires resource replacement

---

### 5.5 The `--save-plan` Option

Pulumi has an experimental `--save-plan FILE` option:

```
pulumi preview --save-plan plan.json
pulumi up --plan plan.json
```

When a plan is saved, `pulumi up --plan` will refuse to make any changes not described in the plan file. This provides a stronger guarantee: the apply is constrained to exactly the changes that were previewed.

This differs from Terraform's plan model slightly: Pulumi's plan is a constraint on the apply operation, not an executable artifact. Pulumi still re-runs the program during `up`; the plan file prevents deviation.

---

### 5.6 What `preview` Cannot Tell You

- **Unknown (output) values**: Like Terraform, Pulumi cannot know the values of cloud-provider-assigned properties (IDs, ARNs, IPs) until apply. These appear as `<computed>` or the output shows only inputs.
- **Provider-level side effects**: Some resources trigger cascading changes in the cloud provider (e.g., changing a VPC's CIDR block may affect subnets). `preview` shows Pulumi-tracked resource changes only, not provider-side cascades.
- **Cross-stack reference effects**: If stack A outputs a value that stack B reads, previewing stack B shows what stack A currently outputs — not what it will output after stack A is updated.
- **Policy pack evaluation**: `--policy-pack` runs CrossGuard policies during preview, but policy decisions are advisory unless the policy pack enforces mandatory rules.

---

### 5.7 How It Is Triggered

- **Separate subcommand** from `up`. Same underlying engine, different terminal operation.
- Pulumi Cloud supports remote previews via the Deployment API (experimental `--remote` flag).

---

### 5.8 Consistency Guarantees

- `preview` reads current state from the state backend.
- Provider refresh is optional; by default, Pulumi trusts its cached state rather than re-querying every cloud resource. Use `-r`/`--refresh` to force re-reading from the cloud provider.
- The plan is valid at the moment it is computed. Concurrent changes to cloud resources between preview and apply will not be detected unless refresh is used.

---

### 5.9 Cacheability / Reusability

- Without `--save-plan`, the preview is ephemeral — output only, no artifact.
- With `--save-plan FILE`, the plan file constrains a subsequent `pulumi up --plan FILE`.
- The plan file is not generally reusable across different states (if state changes, the plan may reference stale resource configurations).

---

## 6. Database Transactions as Dry-Run (`BEGIN` / `ROLLBACK`)

### 6.1 The Pattern

SQL databases support transactional semantics that can be exploited as a dry-run mechanism:

1. Open a transaction (`BEGIN`)
2. Execute the mutations (INSERT, UPDATE, DELETE)
3. Run queries to observe the resulting state within the transaction
4. Roll back (`ROLLBACK`)

Because the transaction was rolled back, no changes are committed to the database.

---

### 6.2 PostgreSQL Example

```sql
BEGIN;

-- Execute the "dry-run" mutations
UPDATE accounts
SET balance = balance - 100
WHERE id = 42;

-- Read the result of what would happen
SELECT balance, (balance >= 0) AS would_succeed
FROM accounts
WHERE id = 42;

-- If balance is negative, we know the operation would violate a constraint
-- Roll back — nothing is committed
ROLLBACK;
```

---

### 6.3 What This Approach Returns

Within a transaction, reads are consistent with the transaction's writes:
- `SELECT` after `UPDATE` within the same transaction reflects the updated data.
- Other transactions (with the default isolation level of `READ COMMITTED` in PostgreSQL) do **not** see the uncommitted data.
- Constraints are evaluated at statement time (or deferred to commit, depending on the constraint definition).

The caller sees:
- The result of queries *as if* the mutations were committed
- Constraint violation errors (if any) **before** rollback
- Trigger execution results (if triggers are defined)

---

### 6.4 SAVEPOINT for Nested Dry-Runs

PostgreSQL and other databases support `SAVEPOINT` for nested rollback points:

```sql
BEGIN;
  UPDATE orders SET status = 'shipped' WHERE id = 99;

  SAVEPOINT check_point;
    UPDATE inventory SET quantity = quantity - 1 WHERE product_id = 5;
    SELECT quantity FROM inventory WHERE product_id = 5;
    -- If quantity < 0, the nested operation would fail
  ROLLBACK TO SAVEPOINT check_point;
  -- inventory change is undone, orders change is still pending

ROLLBACK;
-- Everything undone
```

`ROLLBACK TO SAVEPOINT` undoes changes back to the savepoint but keeps the outer transaction alive. This is equivalent to a nested dry-run within a larger transaction.

---

### 6.5 Isolation Levels and Their Effect on Dry-Run Accuracy

| Isolation Level | What the dry-run sees |
|-----------------|----------------------|
| `READ UNCOMMITTED` | Other transactions' uncommitted data (dirty reads) — rarely used in PostgreSQL |
| `READ COMMITTED` (default PostgreSQL) | Committed data as of each statement; own uncommitted changes visible |
| `REPEATABLE READ` | Consistent snapshot from transaction start; own uncommitted changes visible |
| `SERIALIZABLE` | Full serializability; any conflict with concurrent transactions causes abort |

For dry-run purposes, `REPEATABLE READ` or `SERIALIZABLE` provides the most consistent snapshot — the dry-run sees a stable view of the database for the duration of the "preview."

---

### 6.6 What This Approach Cannot Tell You

- **Trigger side effects**: Database triggers fire during the transaction. If a trigger invokes `pg_notify` or calls an external function (via `plpython`, `plperl`, etc.), those side effects may not be suppressible inside a rolled-back transaction. `NOTIFY` events queued during a transaction are **discarded on `ROLLBACK`** in PostgreSQL, but triggers that write to external systems (via dblink, foreign data wrappers) will have already executed.
- **Sequence values**: `nextval()` on a sequence increments the sequence **even within a rolled-back transaction**, because sequences are non-transactional by design. A dry-run that calls `nextval()` consumes a sequence value.
- **Deferred constraints**: Constraints deferred to commit time (`DEFERRABLE INITIALLY DEFERRED`) are only evaluated at `COMMIT`, not during the transaction. A `ROLLBACK` will not reveal deferred constraint violations.
- **External system state**: The dry-run cannot simulate changes in external systems that the database might trigger (e.g., foreign key checks against FDW tables, application-level side effects).
- **Long-running lock contention**: An open transaction holds locks. A long "preview" transaction may block concurrent writers.

---

### 6.7 How It Is Triggered

- Standard SQL: `BEGIN` / `SAVEPOINT` / `ROLLBACK`.
- No special flag or API — the caller constructs the transaction manually.
- Application code must explicitly implement the rollback; there is no built-in "dry-run mode."

---

### 6.8 Consistency Guarantees

- Within the transaction, reads are consistent with the transaction's own writes (read-your-writes).
- Other transactions do not observe the dry-run's intermediate state (assuming at least `READ COMMITTED` isolation).
- The database guarantees atomicity: `ROLLBACK` is guaranteed to undo all changes from the transaction.

---

### 6.9 Cacheability / Reusability

- Dry-run results are not cacheable. The transaction must be re-executed for each preview.
- Rolled-back transactions leave no persistent artifact.

---

## 7. Git `--dry-run` and `--no-commit`

### 7.1 `git push --dry-run`

**Synopsis** (from git-push documentation):

```
git push [-n | --dry-run] [<repository> [<refspec>...]]
```

`-n` or `--dry-run`: "Do everything except actually send the updates."

**What it does:**
- Performs all local computations (packs objects, computes which refs to update)
- Contacts the remote and negotiates the object graph
- Does **not** transmit the actual data
- Prints the output as if the push had happened

**What it returns** (tabular output, same format as a real push):

```
To github.com:user/repo.git
   abc1234..def5678  main -> main
```

Each line: `<flag> <summary> <from> -> <to>`

- `(space)` — would be a successful fast-forward
- `+` — would be a successful forced update
- `-` — would be a successfully deleted ref
- `*` — would be a new ref
- `!` — would be rejected or failed
- `=` — would be up to date (no push needed)

**Source:** https://git-scm.com/docs/git-push

---

### 7.2 What `push --dry-run` Cannot Tell You

- **Remote hook rejections**: The remote's `pre-receive` and `update` hooks are **not** called during dry-run. Dry-run only simulates the transport layer; server-side hooks are not executed.
- **Authentication**: The dry-run does contact the remote server for negotiation. Authentication failures will surface, but authorization via hooks will not.
- **Exact ref update order**: If pushing multiple refs atomically (`--atomic`), the dry-run does not test atomicity.

---

### 7.3 `git merge --no-commit`

```
git merge --no-commit [<commit>...]
```

From git-merge documentation:
> "With `--no-commit` perform the merge and stop just before creating a merge commit, to give the user a chance to inspect and further tweak the merge result before committing."

**What it does:**
- Performs the full merge computation (3-way merge, conflict resolution)
- Updates the **working tree** and **index** as if the merge were committed
- Does **not** create the merge commit
- Sets `MERGE_HEAD` to point to the merged branch head
- The user can inspect the result, make further changes, then `git commit` or `git merge --abort`

**What it returns:**
- Modified files in the working tree (the merged result)
- Conflict markers in files where the merge cannot auto-resolve
- Exit code 0 if merge succeeded (no conflicts), non-zero if there are conflicts
- `AUTO_MERGE` ref pointing to the working tree state including conflict markers (when using `ort` strategy)

This is a **stateful** dry-run: the working tree is modified. It is not fully non-destructive — the working tree and index are changed. `git merge --abort` is needed to return to the pre-merge state.

**Source:** https://git-scm.com/docs/git-merge

---

### 7.4 `git merge --squash`

A related pattern:

```
git merge --squash <branch>
```

> "Produce the working tree and index state as if a real merge happened (except for the merge information), but do not actually make a commit, move the HEAD, or record `$GIT_DIR/MERGE_HEAD`."

This combines changes from the branch into the working tree and index but does not record the merge history. The next `git commit` creates a single commit rather than a merge commit. This is not precisely a dry-run (it does modify the working tree) but it creates a pre-commit "preview" state.

---

### 7.5 Consistency Guarantees

- `git push --dry-run` operates against the current local state. If the local repository has stale remote-tracking refs, the dry-run may misjudge fast-forward eligibility.
- `git merge --no-commit` operates against the current working tree. If the working tree is dirty, behavior may be unexpected (git warns: "Running git merge with non-trivial uncommitted changes is discouraged").

---

## 8. OPA / Rego Partial Evaluation

### 8.1 Overview

Open Policy Agent (OPA) is a general-purpose policy engine. Rego is its declarative query language. OPA's partial evaluation feature allows pre-compiling a policy with some inputs known and others left as variables, producing a *residual policy* — a simplified policy that can be evaluated later when the remaining inputs become available.

While not strictly a "dry-run" API, partial evaluation answers: "Given what I know right now, what would this policy require or produce for any possible value of the unknown inputs?"

---

### 8.2 Full Evaluation vs. Partial Evaluation

**Full evaluation:**

```go
r := rego.New(
    rego.Query("data.example.allow"),
    rego.Module("example.rego", module),
    rego.Input(map[string]interface{}{
        "user": "alice",
        "action": "read",
        "resource": "/api/data",
    }),
)
rs, err := r.Eval(ctx)
// Returns: [{Expressions: [{Value: true}]}]
```

Full evaluation requires all inputs. Returns the concrete result (true/false, data, etc.).

**Partial evaluation:**

```go
r := rego.New(
    rego.Query("data.example.allow"),
    rego.Module("example.rego", module),
    // Only some inputs are bound:
    rego.Input(map[string]interface{}{
        "user": "alice",
    }),
)
pr, err := r.PartialEval(ctx)
// Returns a PartialResult containing a residual policy
```

The `PartialResult` (pre-compiled residual) can then be evaluated multiple times with different values for the unbound inputs:

```go
r2 := pr.Rego(
    rego.Input(map[string]interface{}{
        "action": "read",
        "resource": "/api/data",
    }),
)
rs, err := r2.Eval(ctx)
```

This two-phase approach is much faster than re-evaluating the full policy each time, because the portions of the policy that depend only on the known inputs are pre-computed.

---

### 8.3 What the Residual Policy Contains

The residual policy is a set of partial rules that capture the "unresolved" parts of the original policy. For example, given:

```rego
allow {
    input.user == "alice"
    input.action == "read"
}
```

If `input.user == "alice"` is known to be true, the residual policy is:

```rego
allow {
    input.action == "read"
}
```

The residual policy:
- Is itself valid Rego
- Can be serialized and stored
- Contains only the conditions that depend on the unknown inputs
- Has `true` conditions eliminated and `false` conditions replaced by an unsatisfiable rule

---

### 8.4 How Partial Evaluation Differs from Full Evaluation

| Aspect | Full Evaluation | Partial Evaluation |
|--------|-----------------|--------------------|
| Inputs required | All inputs | Subset of inputs |
| Output | Concrete values | Residual policy |
| Cost | Proportional to policy complexity | Higher first-time cost; much cheaper for repeated evaluations |
| When to use | Single evaluation | Same policy, many different inputs |
| Unknown inputs | Not allowed | Represented as variables in the residual |

---

### 8.5 Partial Evaluation as a Preview Mechanism

Partial evaluation can be used to pre-check policy consequences before having all inputs:

- A system knows some context (e.g., the requesting user's role) but not the full request.
- Partial evaluation produces a residual showing what conditions must hold in the final request for the policy to allow it.
- This is a form of "if you send request X, the policy will evaluate to: [residual conditions]."

This is **not** a real-operation dry-run — it evaluates the policy logic, not the real system. It is closer to symbolic execution or theorem proving.

---

### 8.6 How It Is Triggered

In the OPA Go library:

```go
r := rego.New(
    rego.Query("..."),
    rego.Module("...", "..."),
    rego.Input(partialInputs),
)
pr, err := r.PartialEval(ctx)  // Returns PartialResult
```

In the OPA REST API:

```http
POST /v1/compile
{
  "query": "data.example.allow == true",
  "input": { "user": "alice" },
  "unknowns": ["input.action", "input.resource"]
}
```

The `unknowns` field lists the input paths that are not yet bound.

The response contains the residual policy as a set of partial rules in JSON.

---

### 8.7 What Partial Evaluation Cannot Tell You

- **The concrete result for unknown inputs**: By definition, partial evaluation cannot tell you the final answer for inputs not yet provided.
- **Runtime behavior of external data calls**: OPA can call external data sources (`http.send`, etc.) during policy evaluation. Partial evaluation may not execute these if they depend on unknown inputs.
- **Whether the full evaluation will succeed**: Even if partial evaluation produces a residual, the residual might still fail for certain input values.

---

## 9. AWS Step Functions

### 9.1 Overview

AWS Step Functions is a serverless workflow orchestration service. It executes state machines defined in Amazon States Language (ASL). Unlike Terraform or Kubernetes, Step Functions does **not** have a true "what would happen?" API — every execution is a committed real execution.

---

### 9.2 What Step Functions Offers Instead

**`GetExecutionHistory` API:**

Step Functions records every state transition as an event in the execution history. After an execution (which is committed), you can inspect what happened:

```json
{
  "events": [
    {
      "timestamp": "...",
      "type": "ExecutionStarted",
      "id": 1,
      "executionStartedEventDetails": {
        "input": "{ \"key\": \"value\" }",
        "inputDetails": { "included": true, "truncated": false },
        "roleArn": "arn:aws:iam::..."
      }
    },
    {
      "timestamp": "...",
      "type": "TaskStateEntered",
      "id": 2,
      "stateEnteredEventDetails": {
        "name": "HelloWorld",
        "input": "{ \"key\": \"value\" }",
        "inputDetails": { "included": true, "truncated": false }
      }
    }
    // ...
  ]
}
```

This is **retrospective inspection**, not pre-execution preview.

---

### 9.3 State Machine Validation Without Execution

AWS Step Functions provides a `ValidateStateMachineDefinition` API:

```http
POST /stateMachines/validate

{
  "definition": "{ ... ASL JSON ... }",
  "severity": "ERROR"
}
```

This checks the ASL definition for structural validity:
- JSON syntax errors
- Invalid state names or references
- Missing required fields
- Circular references

It does **not** execute the state machine or check whether the Lambda functions exist, IAM permissions are correct, or runtime behavior is valid.

---

### 9.4 Express Workflows and Test State API

Step Functions has a `TestState` API that can execute a **single state** in isolation:

```http
POST /stateMachines/testState

{
  "definition": "{ ... single state JSON ... }",
  "roleArn": "arn:aws:iam::...",
  "input": "{ \"key\": \"value\" }",
  "inspectionLevel": "TRACE"
}
```

`inspectionLevel` values:
- `INFO` — minimal output
- DEBUG — includes state input/output
- `TRACE` — includes all HTTP requests/responses made by HTTP Task states

This is closer to a dry-run for individual states: it executes the state's logic (including calling Lambda, HTTP endpoints, etc.) but does not affect the state machine's persistent state or emit to CloudWatch (by default).

**Limitations:**
- Only one state at a time — cannot test the full workflow flow.
- States that modify external resources still modify them. `TestState` is not fully sandboxed.
- Not available for all state types.

---

### 9.5 No True Workflow-Level Dry-Run

Step Functions has no equivalent to Terraform `plan` or Kubernetes dry-run that would simulate an entire execution path without committing changes. The design reflects the service's execution model: each execution is independent, committed to DynamoDB, and billable.

**Why no dry-run exists at the workflow level:**
- State machine executions involve arbitrary Lambda code, external API calls, and SNS/SQS operations. Simulating all these without side effects would require sandboxing every integrated service.
- The execution history is the audit trail; without a real execution, there is no history to inspect.

---

### 9.6 Approximate Dry-Run Pattern

Teams simulate dry-run for Step Functions by:
1. Running executions in a separate AWS account or a "sandbox" environment.
2. Using Step Functions Local (a downloadable simulator) which mocks Lambda and other AWS service integrations.
3. Using the AWS Step Functions Data Flow Simulator in the console, which processes input/output transformations (JSONPath, intrinsic functions) without executing integrations.

Step Functions Local (`StepFunctionsLocal`):
- Runs locally (Docker or JAR)
- Simulates state transitions without actual AWS API calls
- Supports mocking Lambda responses via a mock configuration file

```json
// Mock configuration
{
  "StateMachines": {
    "MyStateMachine": {
      "TestCases": {
        "HappyPath": {
          "MyLambdaState": {
            "Return": { "result": "ok" }
          }
        }
      }
    }
  }
}
```

---

## 10. Dafny / Formal Verification as Preview

### 10.1 Overview

Dafny is a verification-aware programming language. Programs are annotated with preconditions, postconditions, loop invariants, and class invariants. The Dafny verifier (built on Z3) proves at compile time that these specifications hold for all possible inputs — no runtime execution needed.

This is the ultimate "what would happen?" tool: instead of simulating one execution, Dafny proves what will happen for **all possible** executions.

---

### 10.2 Verification as Preview

A Dafny method with specifications:

```dafny
method Withdraw(balance: int, amount: int) returns (newBalance: int)
  requires balance >= 0
  requires amount >= 0
  requires amount <= balance   // Pre-condition: sufficient funds
  ensures newBalance == balance - amount
  ensures newBalance >= 0      // Post-condition: no overdraft
{
  newBalance := balance - amount;
}
```

The Dafny verifier proves:
- Given any `balance >= 0` and `amount >= 0` with `amount <= balance`, the method always terminates.
- The return value `newBalance` will always equal `balance - amount`.
- The return value will always be `>= 0`.

No execution occurs. The proof is entirely symbolic.

---

### 10.3 State Machine Invariants

Dafny can verify state machine transitions maintain invariants across all states:

```dafny
class StateMachine {
  var state: string
  var balance: int

  predicate Invariant()
    reads this
  {
    balance >= 0 &&
    (state == "Open" || state == "Closed" || state == "Locked")
  }

  method Transition(event: string)
    requires Invariant()
    modifies this
    ensures Invariant()
  {
    // transition logic
  }
}
```

The `requires Invariant()` + `ensures Invariant()` contract means: "If the invariant held before calling `Transition`, it will hold after." Dafny proves this statically.

This is the strongest possible form of "what would happen?" — it is a proof that any execution will maintain the invariant, not a simulation of any single execution.

---

### 10.4 What Dafny Verification Tells You

- Whether a method can terminate (termination metrics).
- Whether postconditions are always satisfied given preconditions.
- Whether invariants are maintained across state transitions.
- Whether array/index access is always in-bounds.
- Whether arithmetic operations can overflow (with bounded integers).

---

### 10.5 What Dafny Verification Cannot Tell You

- **Runtime behavior with external state**: Dafny reasons about the specified model, not about I/O, network calls, or external system interactions.
- **Performance**: Dafny proves correctness, not performance characteristics.
- **Underspecified behavior**: If postconditions are weak (or absent), Dafny cannot prove anything meaningful about unspecified behavior.
- **Non-determinism outside the specification**: If real-world execution includes nondeterminism not modeled in the specs, verification results may not match runtime behavior.

---

### 10.6 How It Is Triggered

Dafny verification runs as part of the build/compile step:

```
dafny verify myprogram.dfy
```

There is no "runtime" to speak of. The verification is a compile-time proof. No code is executed.

The Dafny Language Server (LSP) provides real-time verification in editors, showing proof results as the developer types.

---

### 10.7 Consistency Guarantees

- Dafny proofs are sound: if Dafny reports verification success, the specified properties hold for **all** inputs satisfying the preconditions.
- Dafny proofs are complete relative to the axioms of the underlying logic (modulo Z3 timeouts and incompleteness).
- Verification is entirely static — it does not depend on any runtime state.

---

## 11. Type Checkers as Preview

### 11.1 TypeScript `tsc --noEmit`

**API signature:**

```
tsc --noEmit [--project tsconfig.json]
```

`--noEmit` instructs the TypeScript compiler to type-check all files in the project but produce no output files (no `.js`, no `.d.ts`, no source maps).

This is a structural dry-run of the compilation process:
- All type errors are reported.
- All imports are resolved.
- All type-checking rules are applied.
- No files are written.

**What it returns:**
- Type errors with file paths, line numbers, and error codes to stderr
- Exit code 0 if no errors, non-zero if there are errors

**What it cannot tell you:**
- Runtime errors (type system is unsound in certain edge cases; runtime coercions may succeed or fail regardless of type annotation)
- Errors that only appear with specific input data (type checking is static)
- Exact performance characteristics of the emitted code

**How it is triggered:**
- Same binary as the real compile (`tsc`), different flag.
- Commonly used in CI pipelines as a check step before the actual build.

---

### 11.2 Roslyn Analyzers (`dotnet build` without publish)

Roslyn (the C# compiler platform) runs analyzers during compilation. Analyzers are components that inspect the syntax tree, semantic model, or compilation as a whole, and emit diagnostics.

The core analyzer pattern:

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MyAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            ctx => {
                // Inspect the AST node
                // Emit a diagnostic if something is wrong
                ctx.ReportDiagnostic(Diagnostic.Create(...));
            },
            SyntaxKind.InvocationExpression
        );
    }
}
```

Analyzers run:
- Without executing the code.
- During `dotnet build` (which includes compilation).
- During IDE background analysis (in Visual Studio, Rider, VS Code with OmniSharp/C# Dev Kit).
- As part of `dotnet format` or `dotnet analyzers`.

There is no direct CLI equivalent of `tsc --noEmit` for Roslyn, but:

```
dotnet build --no-incremental
```

runs a full build (including analyzers) and reports all diagnostics. To check-only without producing binaries, one approach is to build with `ErrorsAsWarnings` or use:

```
dotnet build /p:RunAnalyzersDuringBuild=true /p:GenerateFullPaths=true
```

Roslyn analyzers see the **semantic model** of the code — resolved symbols, type information, control flow graphs — but they run entirely statically without executing any user code.

**What analyzers cannot tell you:**
- Runtime behavior specific to particular input data.
- Performance characteristics.
- Behavior of external systems (databases, APIs) that the code interacts with.

---

### 11.3 The Broader Pattern: Static Analysis as "Preview"

Type checkers and static analyzers represent a distinct category of preview:

| Characteristic | Type checker preview | Runtime dry-run |
|----------------|---------------------|-----------------|
| When it runs | Compile time | At runtime (or simulated runtime) |
| What it checks | Type compatibility, static invariants | Dynamic behavior |
| State required | None (or just the code itself) | Current system state |
| Coverage | All code paths simultaneously | One execution path per run |
| False positives | Possible (type system approximations) | Rare (executes real code) |
| False negatives | Possible (unsoundness, dynamic typing) | Rare (executes real code) |

---

### 11.4 `dotnet run --dry-run` (Does Not Exist)

There is no `dotnet run --dry-run` equivalent in the .NET ecosystem. The closest equivalent is a combination of:
1. `dotnet build` (check compilability)
2. Running analyzers (check static invariants)
3. Unit tests (check runtime behavior for specific inputs)

This fragmentation is one of the key differences between compile-time and runtime preview: type systems can check all code simultaneously at compile time, but they cannot simulate specific runtime scenarios.

---

### 11.5 How Type Checker "Preview" Is Triggered

- `tsc --noEmit`: Same binary, `--noEmit` flag
- `eslint --no-fix`: Lint check without applying fixes
- `pylint`, `mypy`, `pyright`: Run as separate tools, no execution
- `cargo check` (Rust): Type checks and borrows-checks without producing binaries
- `go vet`: Static analysis without building or running
- Roslyn analyzers: Run during `dotnet build` or IDE analysis

All of these are **separate operations** from the real build/run — they are distinct commands or modes.

---

## Cross-System Patterns

### Triggering Mechanism

| System | Mechanism | Same endpoint? |
|--------|-----------|----------------|
| Terraform | Separate subcommand (`plan` vs `apply`) | No |
| Kubernetes | Query parameter (`?dryRun=All`) on same endpoint | Yes |
| Temporal | Same Update handler, separate lifecycle stage (validator) | Yes (same message) |
| XState | Different function (`transition()` vs actor `send()`) | No |
| Pulumi | Separate subcommand (`preview` vs `up`) | No |
| SQL transactions | Application-controlled (`BEGIN`/`ROLLBACK`) | N/A |
| Git push | Flag (`--dry-run`) on same command | Yes (same command) |
| Git merge | Flag (`--no-commit`) on same command | Yes (same command) |
| OPA | Different API (`/v1/compile` vs `/v1/data`) | No |
| Step Functions | No equivalent | N/A |
| Dafny | Compiler flag / separate tool | N/A |
| TypeScript | Compiler flag (`--noEmit`) | Yes (same binary) |

### What "Preview" Returns That "Real" Doesn't

| System | Extra info in preview |
|--------|-----------------------|
| Terraform | Before/after diffs, `after_unknown` map, complete resource change list |
| Kubernetes | Object as would be stored (without committing) |
| Temporal | Validator rejection before history write |
| XState | `actions` array (what would execute), `changed` flag |
| Pulumi | `detailedDiff` map, property-level diff kinds |
| SQL | Query results inside a transaction that will be rolled back |
| Git push | Would-be ref update statuses |
| Git merge | Working tree merged result (pre-commit) |
| OPA | Residual policy (simplified for unknown inputs) |
| Dafny | Proof of correctness across all inputs |
| TypeScript | Type errors without emitted files |

---

*End of survey. Sources cited inline per section.*
