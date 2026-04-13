# Milestone Modifier — External Research + Feasibility Study

**Research date:** 2026-04-12
**Author:** Frank (Lead/Architect & Language Designer)
**Requested by:** Shane — "super valuable" assessment during modifier taxonomy review
**Scope:** Deep feasibility analysis for a single modifier candidate: `milestone` (mandatory state visitation)
**Reference:** `research/language/expressiveness/structural-lifecycle-modifiers.md` (initial survey), `research/language/expressiveness/modifier-taxonomy-proposal.md` (taxonomy context)

---

## Part 1: External Research

### 1.1 BPMN 2.0 — Mandatory Task Completion

**Source:** Camunda BPMN documentation (https://docs.camunda.io/docs/components/modeler/bpmn/bpmn-coverage/, https://docs.camunda.io/docs/components/modeler/bpmn/parallel-gateways/)

BPMN enforces mandatory task visitation through **structural mechanisms**, not annotations:

- **Parallel gateways (AND-gateways):** When a parallel gateway splits the flow, *all* outgoing paths must complete before a joining parallel gateway can proceed. This is the closest BPMN analog to `milestone` — it structurally guarantees that every branch (and every task on that branch) is visited. However, it applies to *concurrent* branches, not sequential path choices.

- **No optional-bypass on sequence flows:** In a BPMN process, if a task sits on the only path between two gateways, it is structurally mandatory — the process *must* execute it. BPMN doesn't need a "required" annotation because the graph topology itself enforces path visitation.

- **Exclusive gateways (XOR):** When an exclusive gateway creates alternative paths, BPMN *cannot* guarantee that a specific task on one branch will be visited. A domain expert who wants "every claim must be reviewed" cannot express this as a structural property in BPMN if the review sits on only one branch of an exclusive gateway.

**Key insight for Precept:** BPMN proves that **graph topology is sufficient** to enforce mandatory visitation — when the topology guarantees it. The gap emerges exactly where Precept's `milestone` would be valuable: when exclusive-choice branching creates paths that bypass a state, and the business rule says "you shouldn't be able to bypass it."

### 1.2 TLA+ / Model Checking — Liveness Properties

**Source:** learntla.com/core/temporal-logic.html (Hillel Wayne)

TLA+ expresses mandatory state visitation as a **liveness property** using the temporal `<>` ("eventually") operator:

```tla+
EventuallyReviewed == <>(\E s \in States: s = "UnderReview")
```

- `<>P` means "P is true in at least one state of every behavior" — i.e., every possible execution must pass through P.
- `[]P` means "P is always true" (safety property — equivalent to invariants).
- `<>[]P` means "P is eventually always true" (convergence).
- `P ~> Q` means "if P is true, Q is eventually true" (leads-to, the temporal version of implication).

**Critical distinction:** TLA+ checks liveness properties via **model checking** — exhaustive exploration of the state space. This is computationally expensive. TLC (the TLA+ model checker) uses depth-first search with cycle detection. Liveness checking is significantly slower than safety checking because it must explore all possible behaviors, not just reachable states.

**Key insight for Precept:** The TLA+ `<>` operator is the *semantic* equivalent of what `milestone` would express. But TLA+ verifies it via runtime exhaustive model checking, not compile-time graph analysis. Precept's static approach (dominator analysis) is a *sound overapproximation* of TLA+'s exhaustive check — it catches structural violations cheaply, at the cost of potential false positives on guard-dependent paths.

### 1.3 Alloy — Transitive Closure and Path Constraints

**Source:** alloytools.org/tutorials/online/frame-RC-1.html

Alloy expresses path constraints using **transitive closure** on relations and **linear ordering** on state atoms:

```alloy
// "Every trace must visit a state where the entity is UnderReview"
fact { all s: State | s = first implies eventually_reaches_review[s] }
```

Alloy's `util/ordering[State]` module imposes a total ordering on states, providing `first`, `last`, and `next` functions. The River Crossing puzzle tutorial demonstrates the pattern: define a state machine with transitions, then ask Alloy to find traces satisfying specific path constraints.

For milestone-like properties, Alloy would express:
- `run { some s: State | s.status = UnderReview }` — find a trace visiting UnderReview
- The negation (no such trace exists) proves a structural bypass

Alloy uses **SAT-bounded model checking** — it explores all instances up to a given scope. Like TLA+, this is runtime analysis, not compile-time graph analysis.

**Key insight for Precept:** Alloy confirms that "all paths visit X" is a well-understood formal property. Both Alloy and TLA+ express it as a search/verification problem. Precept's graph-structural approach (dominator analysis) is a lighter-weight alternative that trades precision for speed — the right trade for an authoring-time diagnostic.

### 1.4 Petri Nets / Workflow Nets — Soundness Properties

**Source:** https://en.wikipedia.org/wiki/Petri_net (Workflow nets section)

Workflow nets (WF-nets), a Petri net subclass for modeling business processes (van der Aalst, 1998), define **soundness** as a key property:

> A WF-net is *sound* if: (1) for every marking reachable from the initial marking, it is possible to reach the final marking; and (2) **for each transition, there is a reachable marking in which the transition is enabled** (i.e., every transition can potentially fire).

Condition (2) is directly relevant — it says every task in the workflow must be *potentially reachable*. This is a weaker property than `milestone` (reachability ≠ mandatory visitation), but it uses the same graph-analysis foundation.

The soundness verification involves:
- **Reachability analysis** from initial marking to all transitions
- **Reverse reachability** from all transitions to final marking
- **Liveness checking** — L1-live (potentially fireable) through L4-live (live from any reachable marking)

A place (state) that must be visited on every path from source to sink is a **dominator** in the workflow net's reachability graph — the same concept Precept would use.

**Key insight for Precept:** The Petri net / workflow net community has established that "mandatory visitation" is a fundamental soundness property of business processes. The graph-theoretic tools for verifying it (reachability, dominator analysis) are well-understood and computationally tractable for finite graphs.

### 1.5 Compliance/Audit Systems — Structural Enforcement

Regulatory compliance frameworks frequently require mandatory checkpoint visitation:

- **SOX (Sarbanes-Oxley) Section 404:** Financial processes must include "management review" and "independent audit" checkpoints. Bypass of these steps is a compliance violation. SOX doesn't specify *how* systems enforce this — it simply requires evidence that the steps occurred.

- **HIPAA Audit Controls (§164.312(b)):** Healthcare workflows involving PHI (Protected Health Information) must pass through authorization checkpoints. Access review is not optional.

- **Financial regulatory workflows (KYC/AML):** "Know Your Customer" processes require identity verification, sanctions screening, and risk assessment — all mandatory steps before account opening. Skipping any creates regulatory exposure.

- **Four-eyes principle:** Many financial and healthcare processes require that certain decisions pass through two independent reviewers. This is fundamentally a "must visit" constraint — the entity must transit through both reviewer states.

**Key insight for Precept:** Compliance frameworks are the *primary real-world demand driver* for `milestone`. Every compliance officer thinks in terms of "this step cannot be skipped." Today, they enforce it with manual audits, post-hoc detection, and runtime checks. A declarative `milestone` modifier that provides compile-time verification would be a genuinely novel capability — prevention rather than detection, which is Precept's core value proposition.

### 1.6 Graph Theory — Dominator Analysis

**Source:** https://en.wikipedia.org/wiki/Dominator_(graph_theory)

**Definition:** In a directed graph with entry node $n_0$, node $d$ **dominates** node $n$ if every path from $n_0$ to $n$ must go through $d$. This is written $d \text{ dom } n$.

**Key concepts:**
- **Strict dominance:** $d$ strictly dominates $n$ if $d$ dominates $n$ and $d \neq n$.
- **Immediate dominator (idom):** The unique node that strictly dominates $n$ but does not strictly dominate any other strict dominator of $n$.
- **Dominator tree:** A tree where each node's parent is its immediate dominator. The entry node is the root.
- **Postdominance:** Node $z$ postdominates node $n$ if all paths from $n$ to the *exit* node must pass through $z$.

**For `milestone` verification:** A state $S$ is a valid milestone if $S$ **postdominates** the initial state with respect to all terminal states. Equivalently: $S$ dominates every terminal state in the forward graph. Equivalently: every path from `initial` to any `terminal` state passes through $S$.

**Algorithms:**

1. **Naïve iterative algorithm** — Compute $\text{Dom}(n)$ using the dataflow equations:

$$\text{Dom}(n_0) = \{n_0\}$$
$$\text{Dom}(n) = \{n\} \cup \bigcap_{p \in \text{preds}(n)} \text{Dom}(p)$$

Iterate until convergence. Complexity: $O(n^2)$ in the number of nodes.

2. **Lengauer-Tarjan algorithm (1979)** — Near-linear: $O(n \cdot \alpha(n))$ where $\alpha$ is the inverse Ackermann function. In practice, essentially $O(n)$.

3. **Cooper-Harvey-Kennedy algorithm (2001)** — Simpler implementation, slightly worse worst-case, but equally fast in practice for typical graph sizes.

**Practical complexity for Precept:** Precept graphs are tiny — typically 4–12 states. Even the naïve $O(n^2)$ algorithm would complete in microseconds. The Cooper-Harvey-Kennedy algorithm would be the pragmatic choice: simple to implement, well-documented, and handles Precept's graph sizes trivially.

**Compiler precedent:** Dominator analysis is a foundational algorithm in compilers. It's used for:
- **SSA (Static Single Assignment) form:** φ-function placement requires dominance frontiers.
- **Loop detection:** Natural loops are identified via back edges to dominator nodes.
- **Dead code elimination:** Unreachable code lives outside the dominator tree.

This means dominator analysis is a textbook algorithm with robust, well-tested implementations available as references.

### 1.7 XState v5 — Tags and Path Coverage

**Source:** https://stately.ai/docs/tags

XState v5 provides `tags` — string annotations on state nodes for grouping:

```typescript
const machine = createMachine({
  states: {
    loadingUser: { tags: ['pending'] },
    success: { tags: ['resolved'] },
    error: { tags: ['rejected'] }
  }
});
actor.getSnapshot().hasTag('pending'); // runtime query
```

Tags are **purely semantic annotations** — they carry no enforcement. There is no mechanism in XState to declare "every execution must visit a state tagged `required`." XState's test utilities provide path coverage tools (listing all possible paths through a machine), but these are testing aids, not structural guarantees.

**Key insight for Precept:** XState's gap here is exactly what `milestone` would fill. XState can tag states with meaning but cannot enforce path obligations. Precept could do both — the modifier carries semantic meaning (this state matters) AND structural enforcement (the compiler proves it's mandatory).

### 1.8 AWS Step Functions — Choice States

AWS Step Functions uses `Choice` states for conditional branching and `Parallel` states for concurrent execution. Like BPMN:
- `Parallel` states guarantee all branches complete (structurally mandatory).
- `Choice` states create alternative paths with no mechanism to require that a specific state appears on all branches.

Step Functions provides no annotation or mechanism for declaring "this state must be visited" across alternative paths. The AWS documentation (docs.aws.amazon.com) focuses on error handling, retries, and timeouts — not path obligation semantics.

### 1.9 Temporal.io — Workflow Saga Patterns

**Source:** https://docs.temporal.io/workflows, https://docs.temporal.io/encyclopedia/detecting-activity-failures

Temporal.io workflows are imperative code (Go, Java, TypeScript, Python), not declarative graph definitions. "Mandatory steps" are enforced by program flow control:

```typescript
async function processLoan(input: LoanInput) {
  await verifyDocuments(input);  // Can't skip — code flow requires it
  await underwriteLoan(input);    // Same — sequential execution
  await approveLoan(input);
}
```

Temporal's equivalent of `milestone` is simply "the function call is in the code path." The challenge Temporal addresses is *durability* (what happens if the worker crashes mid-execution), not *path obligation* (ensuring certain steps happen). Heartbeats and timeouts detect activity failures but don't enforce path visitation.

Temporal's saga pattern handles compensation (undoing steps on failure) but doesn't declare mandatory checkpoints.

**Key insight for Precept:** Temporal solves a different problem. Imperative workflow engines enforce mandatory steps through code structure (if it's called, it runs). Precept's declarative model creates the gap that `milestone` fills — because the *same state graph* can have multiple paths through it, and the compiler needs a way to know which states must appear on all paths.

### 1.10 Process Mining — Conformance Checking

**Source:** https://en.wikipedia.org/wiki/Conformance_checking (Carmona et al., 2018; van der Aalst, 2016)

Process mining's conformance checking detects when real process executions deviate from a normative model:

- **Token-based replay:** Replay each trace against a Petri net model, counting produced/consumed/missing/remaining tokens. Missing tokens indicate skipped activities.
- **Trace alignment:** Find the closest model trace to each observed trace, identifying deviations.
- **Fitness metric:** $\frac{1}{2}(1 - \frac{m}{c}) + \frac{1}{2}(1 - \frac{r}{p})$ where $m$ = missing tokens, $c$ = consumed tokens, $r$ = remaining tokens, $p$ = produced tokens.

**Direct relevance:** Conformance checking detects *post-hoc* that a required step was skipped. A `milestone` modifier in Precept would prevent this **before deployment** — the compiler would reject a precept definition where the "required step" can be structurally bypassed.

This is Precept's philosophy in action: **prevention, not detection**. Process mining detects compliance violations after they happen. Precept's `milestone` would make them structurally impossible.

**Key insight for Precept:** Process mining validates that `milestone` solves a real problem — process instances DO skip required steps, and organizations spend significant resources detecting and remediating these violations. A compile-time guarantee is strictly superior to post-hoc detection.

---

## Part 2: Feasibility Study for Precept

### 2a. The Dominator Analysis Approach

#### How Dominator Analysis Works for Milestone Verification

Given a Precept with states $S$, initial state $s_0$, terminal states $T \subseteq S$, and a transition graph $G$ derived from declared transition rows:

**Step 1: Build the adjacency graph.** For each transition row `from A on Event -> transition B`, add edge $A \to B$. Ignore `no transition` and `reject` outcomes. This is already done in `PreceptAnalysis.cs` — the `graph` dictionary maps each state to its set of transition targets.

**Step 2: Compute dominators.** Using the iterative dataflow algorithm (small graph sizes make this appropriate):

```
Dom(s_0) = {s_0}
for each state n ≠ s_0:
    Dom(n) = {n} ∪ ⋂{Dom(p) : p ∈ predecessors(n)}
iterate until fixed point
```

**Step 3: Check milestone property.** A state $M$ is a valid milestone if and only if $M \in \text{Dom}(t)$ for every terminal state $t \in T$.

Equivalently: $M$ appears in the intersection of the dominator sets of all terminal states:

$$\text{MilestoneSet} = \bigcap_{t \in T} \text{Dom}(t) \setminus \{s_0\}$$

Any state in MilestoneSet is a structural milestone — every path from initial to any terminal must pass through it.

**Step 4: Verify declared milestones.** For each state marked `milestone`, check membership in MilestoneSet. If not a member, emit a diagnostic.

#### Computational Complexity

For Precept's graph sizes (typically 4–12 states, rarely exceeding 25):
- **Naïve iterative algorithm:** $O(n^2)$ per iteration, $O(n)$ iterations worst case → $O(n^3)$ total. For $n = 25$: ~15,625 operations. Negligible.
- **Memory:** $O(n^2)$ for dominator sets stored as bit vectors. For $n = 25$: ~78 bytes.
- **In practice:** The algorithm converges in 2–3 iterations for acyclic graphs, which most Precept state graphs are. Total time: sub-microsecond.

This is not a performance concern. The BFS reachability analysis already in `PreceptAnalysis.cs` is the same order of complexity.

#### Where It Lives in the Pipeline

The dominator analysis should live in `PreceptAnalysis.cs`, immediately after the existing reachability analysis (BFS from initial state). The data structures are already available:

- **`graph`** — `Dictionary<string, HashSet<string>>`: adjacency list for forward edges
- **`reachable`** — `HashSet<string>`: BFS-computed reachable states
- **`terminalStates`** — `string[]`: states with no outgoing transitions
- **`allStateNames`** — `string[]`: ordered state list

The dominator analysis needs one additional data structure: **reverse adjacency** (predecessors for each state). This is easily computed from the existing `graph` in a single pass.

The analysis result record `AnalysisResult` would gain a new field: `IReadOnlyList<string> MilestoneViolations` (states marked `milestone` that fail the dominator check).

### 2b. The Guard Problem

#### The Structural Overapproximation

Precept's static analysis treats the transition graph as a **non-deterministic** graph: every edge declared in a transition row is considered traversable, regardless of guards. This is a deliberate design choice: guards are data-dependent expressions that cannot be evaluated at compile time without knowing runtime field values.

This means the analyzer sees all paths as possible, even if some are guarded by conditions that, in practice, always force the entity through the milestone state.

#### Concrete Example: Insurance Claim with Guard-Dependent Bypass

Consider a hypothetical modification of the insurance-claim sample where an "express" path bypasses UnderReview:

```precept
state Draft initial
state Submitted
state UnderReview       # Candidate milestone
state Approved
state Denied
state Paid

# Normal path: Draft → Submitted → UnderReview → Approved/Denied → Paid
from Draft on Submit -> transition Submitted
from Submitted on AssignAdjuster -> transition UnderReview
from UnderReview on Approve when ... -> transition Approved
from UnderReview on Deny -> transition Denied

# Express path (hypothetical): Submitted → Approved directly, with guard
from Submitted on ExpressApprove when ClaimAmount < 100 -> transition Approved

from Approved on PayClaim -> transition Paid
```

If `UnderReview` is marked `milestone`, the dominator analysis would report a **violation** because the path `Draft → Submitted → Approved → Paid` exists structurally (via `ExpressApprove`). The analyzer doesn't know that this path only activates for small claims — it sees the edge and reports the bypass.

This is a **true positive in graph terms** (the bypass path exists) but a **false positive in domain terms** (the business intended this guard-controlled bypass as acceptable).

#### Survey of Guard-Dependent Paths in Sample Files

I analyzed the 24 sample files for guard-dependent bypasses that would create false positives:

| Sample | States | Terminal candidates | Guard-dependent bypasses | False positive risk |
|--------|--------|---------------------|--------------------------|---------------------|
| insurance-claim | 6 | Denied, Paid | Approve has guard on documents — but path to Denied is unguarded | Low — UnderReview dominates both Denied and Paid |
| hiring-pipeline | 7 | Hired, Rejected | RejectCandidate is unguarded from Screening, InterviewLoop, Decision — creates direct paths to Rejected bypassing InterviewLoop/Decision | Medium — InterviewLoop is NOT a dominator for Rejected |
| loan-application | 5 | Funded, Declined | Approve has guard — but Decline is unguarded from UnderReview | Low — UnderReview dominates both Funded and Declined |
| travel-reimbursement | 5 | Paid, Rejected | Submit has guard on lodging — reject path from Submitted is unguarded | Low — Submitted dominates all terminals |

**Finding:** Most samples have **linear or near-linear** state graphs where the question of domination is trivial — each state dominates all subsequent states. The false positive risk primarily arises in graphs with:
1. Multiple terminal states (e.g., both "success" and "failure" endpoints)
2. Different paths to success vs. failure terminals
3. Guard-protected paths on only some branches

**In the real sample corpus:** False positives would be rare because most samples have a dominant linear spine with deviations only for failure paths. The critical scenario is when the *business* considers a guarded bypass acceptable but the analyzer flags it.

#### Mitigations

**Mitigation 1: Accept the overapproximation as conservative.** The dominator analysis is sound — it never misses a real structural bypass. False positives mean the author intended a guarded bypass. This is the simplest approach and aligns with Principle #8 ("never guess — if the checker can't prove, it assumes satisfiable"). Here the analysis is conservatively strict: if a bypass *structurally exists*, the author must acknowledge it.

**Mitigation 2: Suppress when all bypass paths are guarded.** If every path that bypasses the milestone state has at least one guarded transition row on it, the diagnostic could be downgraded to a warning or suppressed. This requires walking the bypass paths and checking for guard presence — more complex analysis, and "guarded" doesn't mean "infeasible."

**Mitigation 3: Author annotation to acknowledge exceptions.** Allow the author to annotate bypass paths as intentional. This is the most precise mitigation but requires new language surface. Deferred — out of scope for the initial implementation.

**Recommendation:** Start with Mitigation 1. The conservative approach is consistent with Precept's philosophy. If real-world usage shows common false positive patterns, Mitigation 2 can be added later as a refinement.

### 2c. Interaction with `terminal`

#### Does `milestone` Require `terminal`?

Strictly speaking, dominator analysis requires a set of "target" nodes. For `milestone`, the targets are terminal states — states where the entity's lifecycle ends. Without knowing which states are terminal, the analysis would have to assume *every* state is a potential terminal, making the milestone check trivially fail for any non-universal dominator.

**Three approaches:**

1. **Require `terminal` modifier (strongest).** `milestone` is only valid if at least one state is marked `terminal`. The compiler errors if `milestone` is used without `terminal` states. This makes the dependency explicit and the analysis precise.

2. **Infer terminal states from structure (current behavior).** `PreceptAnalysis.cs` already detects terminal states: states with no outgoing transition rows. Use these inferred terminals as the target set. This works without `terminal` modifier adoption but relies on accurate inference.

3. **Accept either (flexible).** Use explicit `terminal` markers if present; fall back to inferred terminals. A diagnostic warns if `milestone` is used with no terminal states (explicit or inferred).

**Recommendation:** Approach 3 (flexible). In the current codebase, `terminal` is a candidate modifier not yet implemented. `milestone` should work with inferred terminals today and benefit from explicit `terminal` markers when they ship. The dependency is soft, not hard.

#### How the Analysis Changes

| Terminal source | Analysis behavior |
|-----------------|-------------------|
| Explicit `terminal` markers | Dominator analysis targets exactly the marked states. Precise. |
| Inferred terminals (no outgoing rows) | Same analysis, using inferred set. Correct for most samples. |
| No terminals found (cycles/infinite lifecycles) | `milestone` check is skipped. New diagnostic: "Cannot verify milestone — no terminal states found." |

#### Precepts Without Terminal States

Some precepts model cyclic lifecycles (e.g., a traffic light that rotates indefinitely, a subscription that renews forever). These have no terminal states — every state has outgoing transitions.

For these precepts, `milestone` is **semantically incoherent** — "every path from initial to terminal" is vacuously true when there are no terminals. The compiler should either:
- Reject `milestone` on cyclic precepts with a clear diagnostic ("Milestone requires at least one terminal state")
- Accept it vacuously (no diagnostic, no verification)

**Recommendation:** Reject with diagnostic. A vacuously true modifier is worse than useless — it misleads the author into thinking the property is being enforced.

### 2d. The Naming Question

Four candidates for the modifier keyword:

| Syntax | Reading | Analysis |
|--------|---------|----------|
| `state UnderReview required` | "The state UnderReview is required" | **Generic.** "Required" is overloaded in software (required fields, required args). Precept already uses `required` conceptually for event args (`notempty`, positional args). Risk of confusion. |
| `state UnderReview milestone` | "The state UnderReview is a milestone" | **Domain-natural.** Project managers and compliance officers use "milestone" to mean "checkpoint that must be reached." No overlap with existing Precept keywords. |
| `state UnderReview checkpoint` | "The state UnderReview is a checkpoint" | **Clear but narrower.** "Checkpoint" implies a pause-and-verify step. Domain experts may interpret it as "save progress here" rather than "must visit here." |
| `state UnderReview mandatory` | "The state UnderReview is mandatory" | **Too broad.** Sounds like the state must always be the current state, not that it must appear on every path. |

**Principle #2 analysis (English-ish):** "Milestone" reads most naturally in a sentence: *"UnderReview is a milestone in the claims process."* This matches how domain experts already describe mandatory checkpoints in project plans, compliance documentation, and process maps.

**Principle #13 analysis (keywords for domain):** "Milestone" is a domain concept first, a technical concept second. Domain experts use it without training. "Required" is a programmer's word. "Checkpoint" is a database/game concept (save state). "Mandatory" is bureaucratic.

**Recommendation:** `milestone`. It reads naturally, carries the right semantic weight, avoids collision with existing Precept vocabulary, and matches how compliance/process professionals already describe the concept.

**Syntax:** `state UnderReview milestone` — same position as `initial`, same modifier slot.

### 2e. Diagnostic Design

#### Diagnostic Code

The analysis diagnostic range is C48–C53. The type checker uses C56–C59. Available codes: **C54** and **C55** are unallocated in the analysis range. Use **C54** for the milestone violation diagnostic.

(Note: if `terminal` ships first and takes C54, use C55 or the next available.)

#### Diagnostic Message Template

```
C54: State '{State}' is marked milestone but can be bypassed.
     Path: {Initial} → {BypassPath} → {Terminal} (does not visit {State}).
```

Example for the hypothetical express-approve insurance claim:

```
C54: State 'UnderReview' is marked milestone but can be bypassed.
     Path: Draft → Submitted → Approved → Paid (does not visit UnderReview).
```

#### Information to Include

1. **The milestone state** — which state failed the check
2. **A witness path** — one concrete path from initial to terminal that demonstrates the bypass
3. **The terminal state reached** — which terminal the bypass path leads to

**Severity:** Error (not warning). A declared milestone that can be bypassed is a structural authoring mistake, same as C8 (duplicate initial). The author declared intent; the structure contradicts it.

#### Actionability

The author has three clear remediation options:
1. **Remove the bypass path** (add/modify transition rows to force the entity through the milestone state)
2. **Remove the `milestone` modifier** (acknowledge the bypass is intentional)
3. **Add guards that structurally prevent the bypass** (though guards don't eliminate the structural path — see Section 2b)

The witness path in the diagnostic tells the author exactly *where* the bypass exists, making it highly actionable.

### 2f. Concrete Sample File Analysis

#### Insurance Claim

```
State graph:
  Draft (initial) → Submitted → UnderReview → Approved → Paid
                                             → Denied
```

**Candidate milestone:** `UnderReview` — every claim should be reviewed before approval or denial.

**Dominator analysis:**
- Dom(Draft) = {Draft}
- Dom(Submitted) = {Draft, Submitted}
- Dom(UnderReview) = {Draft, Submitted, UnderReview}
- Dom(Approved) = {Draft, Submitted, UnderReview, Approved}
- Dom(Denied) = {Draft, Submitted, UnderReview, Denied}
- Dom(Paid) = {Draft, Submitted, UnderReview, Approved, Paid}

Terminal states (inferred): Denied, Paid.
MilestoneSet = Dom(Denied) ∩ Dom(Paid) = {Draft, Submitted, UnderReview}.

**Result:** `UnderReview` IS in the MilestoneSet. ✅ **The modifier would be correctly verified.**

**False positives:** None. Every path from Draft to either Denied or Paid passes through UnderReview. The existing sample has no bypass paths.

**Diagnostic output:** None (no violation).

#### Hiring Pipeline

```
State graph:
  Draft (initial) → Screening → InterviewLoop → Decision → OfferExtended → Hired
                    Screening → Rejected
                    InterviewLoop → Rejected
                    Decision → Rejected
```

**Candidate milestones:**
- `InterviewLoop` — every candidate should go through interviews
- `Decision` — every candidate should reach a decision point

**Dominator analysis:**
- Dom(Draft) = {Draft}
- Dom(Screening) = {Draft, Screening}
- Dom(InterviewLoop) = {Draft, Screening, InterviewLoop}
- Dom(Decision) = {Draft, Screening, InterviewLoop, Decision}
- Dom(OfferExtended) = {Draft, Screening, InterviewLoop, Decision, OfferExtended}
- Dom(Hired) = {Draft, Screening, InterviewLoop, Decision, OfferExtended, Hired}
- Dom(Rejected) = {Draft, Screening, Rejected} ← KEY: only Draft and Screening dominate Rejected

Terminal states: Hired, Rejected.
MilestoneSet = Dom(Hired) ∩ Dom(Rejected) = {Draft, Screening}.

**Result:** `InterviewLoop` is NOT in the MilestoneSet. ❌ `Decision` is NOT in the MilestoneSet. ❌

This is **correct behavior!** A candidate can be rejected from Screening, InterviewLoop, or Decision — the `RejectCandidate` event is available from all three states, creating direct paths to Rejected that bypass later states. InterviewLoop and Decision are NOT structural milestones.

**Diagnostic output for `state InterviewLoop milestone`:**
```
C54: State 'InterviewLoop' is marked milestone but can be bypassed.
     Path: Draft → Screening → Rejected (does not visit InterviewLoop).
```

This diagnostic is accurate and actionable. The author would need to decide: either remove `milestone` (acknowledging that screening rejections bypass the interview loop), or restructure the flow so that screening rejections still pass through the interview loop.

#### Loan Application

```
State graph:
  Draft (initial) → UnderReview → Approved → Funded
                                → Declined
```

**Candidate milestone:** `UnderReview` — every loan must be reviewed.

**Dominator analysis:**
- Dom(Funded) = {Draft, UnderReview, Approved, Funded}
- Dom(Declined) = {Draft, UnderReview, Declined}

Terminal states: Funded, Declined.
MilestoneSet = Dom(Funded) ∩ Dom(Declined) = {Draft, UnderReview}.

**Result:** `UnderReview` IS in the MilestoneSet. ✅ **Correctly verified.**

**False positives:** None. The `Approve` guard (document verification, credit score, affordability) creates a guarded transition, but the *unguarded* `Decline` path also goes through UnderReview, so no bypass exists.

#### Travel Reimbursement

```
State graph:
  Draft (initial) → Submitted → Approved → Paid
                               → Rejected
```

**Candidate milestone:** `Submitted` — every reimbursement must be submitted before action.

**Dominator analysis:**
- Dom(Paid) = {Draft, Submitted, Approved, Paid}
- Dom(Rejected) = {Draft, Submitted, Rejected}

Terminal states: Paid, Rejected.
MilestoneSet = Dom(Paid) ∩ Dom(Rejected) = {Draft, Submitted}.

**Result:** `Submitted` IS in the MilestoneSet. ✅ **Correctly verified.**

This is a near-linear graph where dominator analysis is trivially correct.

### 2g. Implementation Complexity Assessment

#### Lines of Code Estimate

| Component | Estimate | Notes |
|-----------|----------|-------|
| Reverse adjacency builder | ~15 LOC | One pass over existing `graph` |
| Dominator set computation | ~35 LOC | Iterative dataflow, using `HashSet<string>` per node |
| MilestoneSet intersection | ~10 LOC | Intersect dominator sets of terminal states |
| Milestone violation check | ~15 LOC | For each milestone-marked state, check membership |
| Diagnostic emission | ~15 LOC | Witness path computation (optional BFS) + diagnostic construction |
| **Total dominator analysis** | **~90 LOC** | |
| Parser: `milestone` keyword recognition | ~10 LOC | Add to modifier parsing in `PreceptParser.cs` |
| Type checker: modifier validation | ~15 LOC | `milestone` only valid on state declarations |
| `PreceptState` model: modifier field | ~5 LOC | Add `bool IsMilestone` or modifier enum |
| Diagnostic catalog entry (C54) | ~5 LOC | |
| Tests (unit + integration) | ~150–200 LOC | Dominator correctness, sample-based verification, false positive scenarios |
| **Total implementation** | **~290–340 LOC** | |

#### Integration Points

1. **`PreceptParser.cs`** — Recognize `milestone` keyword after state name (same position as `initial`). Add to modifier parsing.
2. **`PreceptState` record** — Add `IsMilestone` property or extend modifier enum.
3. **`PreceptAnalysis.cs`** — Add dominator analysis after BFS reachability. Add `MilestoneViolations` to `AnalysisResult`.
4. **`PreceptTypeChecker.cs`** — Validate that `milestone` only appears on state declarations, not on initial state (initial is trivially a milestone).
5. **`DiagnosticCatalog.cs`** — Add C54 entry.
6. **`PreceptTokenMeta.cs`** — Add `Milestone` token with `[TokenCategory(TokenCategoryKind.Keyword)]`.
7. **`precept.tmLanguage.json`** — Add `milestone` to `controlKeywords` alternation.
8. **`PreceptAnalyzer.cs`** — Add `milestone` to completion items after state names.
9. **MCP `CompileTool.cs`** — Milestone info included in state output (automatic if model changes).

#### Test Coverage Needed

| Test category | Count | Description |
|---------------|-------|-------------|
| Parser recognition | 3–4 | `milestone` parsed, invalid positions rejected, duplicate modifier rejected |
| Dominator correctness | 5–6 | Linear graph, branching graph, cycle, multiple terminals, no terminals |
| Sample-based verification | 4 | Insurance, hiring, loan, travel — verify expected milestone validity |
| False positive scenarios | 3–4 | Guard-dependent bypasses, `from any` shortcuts, reject-from-anywhere patterns |
| Diagnostic format | 2–3 | Correct code, message, witness path |
| Edge cases | 3–4 | Milestone on initial state (trivially valid), milestone with no terminals, stateless precept with milestone |

#### Risk Factors

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Guard-dependent false positives cause user confusion | Medium | Medium | Conservative approach (Section 2b), clear diagnostic with witness path |
| Interaction with future `terminal` modifier | Low | Low | Flexible approach (Section 2c) already handles both explicit and inferred terminals |
| Grammar ambiguity with multiple modifiers (`state X initial milestone`) | Low | Low | Same modifier-slot parsing as `initial` — modifiers are order-independent after state name |
| `from any` creates unexpected bypass paths | Medium | Low | `from any` expands to all states, which may create unseen edges. Document clearly. |

---

## Part 3: Open Questions

The following design questions require Shane's input before `milestone` could move to implementation:

### Q1. Ship Order — Does `terminal` Ship First?

The dominator analysis works with inferred terminals today, but it's semantically cleaner with explicit `terminal` markers. Should `terminal` ship first (creating the foundation), or can `milestone` ship independently using inferred terminals?

**Trade-off:** Shipping `milestone` first demonstrates higher domain value sooner, but couples it to an inference that may not match author intent. Shipping `terminal` first is foundational but lower-impact.

### Q2. Diagnostic Severity — Error or Warning?

Section 2e recommends error severity (matching C8 for `initial`). But milestone violations are subtler than missing-initial — the author may have intended the bypass. Should the diagnostic be:
- **Error** (hard block, same as structural modifiers) — stronger guarantee, forces the author to resolve
- **Warning** (advisory) — softer, allows intentional bypasses without removing the modifier

### Q3. Multiple Milestones — Ordering Constraint?

If an author marks both `UnderReview` and `Decision` as milestones, should the compiler verify that they're visited in *that order*, or only that each is independently visited on every path? Order-independent checking is simpler and matches the dominator model. Order-dependent checking requires path enumeration, which is more complex.

### Q4. Milestone on Terminal States — Allowed?

Marking a terminal state as `milestone` is vacuously true (every path to a terminal visits the terminal). Should this be:
- Silently accepted (true, harmless)
- Warned (redundant — terminal is already mandatory at its own endpoint)
- Rejected (modifier adds no value)

### Q5. `from any` Interaction

`from any on Event -> transition State` creates edges from *every* state to the target, including potential bypass edges. Should the dominator analysis have special handling for `from any`, or treat it as the expanded edge set? Current recommendation: no special handling (expand and analyze), but the witness path in the diagnostic should make `from any`-generated bypasses visible.

### Q6. Philosophy Gate

Does `milestone` require a philosophy update? It introduces a new *kind* of structural guarantee (mandatory visitation) beyond the existing guarantees (prevention, determinism, inspectability). The argument for "no gate needed": milestone is prevention (structurally preventing bypass). The argument for gate: it's a novel verification property, not previously articulated. Flag for Shane's consideration.

---

## Summary

`milestone` is a feasible, high-value modifier with strong external precedent and straightforward implementation (~290–340 LOC). The core algorithm (dominator analysis) is a textbook graph algorithm with near-linear complexity, well-suited to Precept's tiny state graphs. The primary risk is guard-dependent false positives, which are manageable with a conservative diagnostic approach. External research confirms that mandatory checkpoint visitation is a real domain need poorly served by existing state machine tools — making this a genuine differentiator for Precept.
