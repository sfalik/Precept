# State Graph Analysis Algorithms Survey

> Raw research collection. No interpretation, no conclusions, no recommendations.
> Research question: How do formal tools and compiler frameworks perform structural analysis on state graphs at compile/analysis time (not at runtime)? What algorithms are used for reachability, dead-end detection, dominator computation, reverse-reachability, and cycle detection? How do these analyses report findings?

---

## Table of Contents

1. [SPIN / Promela Model Checker](#spin--promela-model-checker)
2. [Alloy Analyzer (MIT)](#alloy-analyzer-mit)
3. [NuSMV / nuXmv](#nusmv--nuxmv)
4. [UPPAAL](#uppaal)
5. [Lengauer-Tarjan Dominator Algorithm + LLVM DominatorTree](#lengauer-tarjan-dominator-algorithm--llvm-dominatortree)
6. [XState Static Analysis (@xstate/graph)](#xstate-static-analysis-xstategraph)
7. [SCXML Validators (W3C SCXML)](#scxml-validators-w3c-scxml)

---

## SPIN / Promela Model Checker

Source: https://spinroot.com/spin/whatispin.html  
Source: G.J. Holzmann, *The SPIN Model Checker: Primer and Reference Manual*, Addison-Wesley, 2003  
Source: https://spinroot.com/spin/Man/spin.html  
Source: https://spinroot.com/spin/Doc/SpinPromela.pdf

SPIN (Simple Promela INterpreter) is a canonical explicit-state model checker developed at Bell Labs. It verifies finite-state systems described in the Promela language (Protocol Meta-Language). SPIN is particularly focused on correctness properties of concurrent and communicating systems.

### State Graph Representation

Promela models are compiled into an internal Büchi automaton (or generalized Büchi automaton for LTL). Each process in a Promela model is compiled into a control-flow graph where nodes are **local process states** (program counter values) and edges are **transitions labeled with guards and effects**. The global state is the cross-product of:
- All process control states (PC of each active process)
- All global variable valuations
- Channel contents

SPIN stores discovered states in a hash table using **bitstate hashing** or a **full state vector** representation depending on mode. In full verification mode, each state vector is a flat byte array encoding all variable values and process counters. In bitstate mode (Supertrace), only a hash bit per state is stored, trading completeness for scale.

The underlying graph is **implicit** — SPIN does not build an explicit adjacency matrix. The state graph is explored on-the-fly: SPIN generates successor states by applying enabled transitions to a current state, rather than pre-materializing the entire graph.

### Reachability Algorithm

SPIN's primary search algorithm is **iterative deepening DFS** (depth-first search) with a stack-based visited set. For safety properties it uses standard DFS; for liveness (cycle detection via Büchi acceptance), it uses **nested DFS** (the double-DFS algorithm):

1. **Outer DFS** explores the state space and identifies all states on acceptance cycles.
2. **Inner DFS** starts from each accepting state found by the outer DFS and attempts to find a back-edge returning to the same accepting state (which constitutes a counterexample — a lasso-shaped infinite trace).

The DFS uses an explicit stack to maintain the current path for counterexample construction. Transitions are selected in declaration order (deterministic for single-process models; non-deterministic for multiple processes, where all enabled transitions are explored).

Hash-based state storage uses double-hashing (two independent hash functions applied to the state vector) to reduce collision probability. The visited-state table is a flat bitarray or hash map depending on the `-DBITSTATE` compile flag.

### Dead / Unreachable State Detection

SPIN detects unreachable code in Promela processes using the `-dead` flag. The detection works as follows:

- After the full state space exploration, SPIN inspects which program-counter values were **never visited** in any reachable global state.
- Any PC value that was never reached corresponds to unreachable code in the Promela process.
- SPIN reports these as warnings of the form: `"unreachable code in proctype P (state N, line M)"`.

For **unreachable states** more broadly (global states never visited), SPIN does not enumerate them explicitly. Instead, it reports the count of visited states and allows the user to infer whether the model is fully explored.

End-states (states labeled `end_`) that are never reached generate a warning. SPIN checks that all reachable terminal states satisfy the progress label (`progress_`) if specified, flagging non-progress cycles.

### Dominator Computation

SPIN does not compute dominators in the LLVM/compiler sense. There is no dominator tree output. The closest concept is the **DFS spanning tree** implicitly maintained during state-space exploration, but this is not exposed as a dominator API. SPIN's focus is on reachability and temporal logic, not structural domination.

### Reverse Reachability

SPIN does not expose reverse reachability directly. In counterexample (trail) mode, SPIN outputs the **path from the initial state to a violating state**, which is the forward reachability witness. There is no built-in "from which states can state S be reached?" query in SPIN's CLI or API. Post-processing tools can reconstruct the reverse-reachability set from the `.tra` transition-relation dump.

### Guard / Condition Handling During Analysis

SPIN performs **explicit-state** analysis, meaning it treats guards as **concrete predicates evaluated against concrete state values**. A transition is enabled in a given state only if its guard evaluates to `true` under the exact current variable valuation.

For non-deterministic models (multiple processes, `if` with multiple branches), SPIN explores **all enabled transitions** in each state. A transition guarded by `x > 3` is explored only in states where `x > 3` holds concretely. This means SPIN is **underapproximation-free** for reachability: if a state is reachable in the real system under some execution, SPIN will find it (given enough memory). Conversely, states only reachable via infeasible paths in the model are never visited.

### Analysis Result Reporting

SPIN reports results in two modes:

1. **Trail mode**: When a property violation is found, SPIN writes a `.trail` file (the counterexample trace) as a sequence of transition indices. The `spin -t` flag replays the trail against the model.
2. **Summary mode**: After a full verification pass, SPIN prints:
   ```
   State-vector N byte, depth reached M, errors: 0
   K states, stored
   J states, matched
   hash conflicts: H (resolved)
   ```
   Unreachable code warnings appear as part of the verification output, not the trail.

Error categories SPIN reports:
- `assertion violated` (for `assert()` statements)
- `invalid end state` (non-progress or invalid terminal)
- `deadlock` (no enabled transitions in non-terminal state)
- `unreachable code` (with `-dead` flag)

### Performance Characteristics

- State explosion: state space grows exponentially with the number of concurrent processes. A model with `k` processes each with `n` states has up to `n^k` global states.
- Bitstate hashing: with 1 GB bitstate hash, approximately 10^9 states can be stored.
- Partial-order reduction (POR) is built in, applying **ample set** or **stubborn set** reduction to prune redundant interleavings, often achieving orders-of-magnitude reduction.
- The nested DFS for liveness is O(V + E) where V is visited states and E is explored transitions.
- Full verification is PSPACE-complete for finite-state Promela models.

### Notes

- The `-ltl` flag compiles an LTL formula into a Büchi automaton and takes the synchronous product with the model automaton, enabling temporal reachability queries like "is some state satisfying P ever reachable from the initial state?"
- SPIN's `pan` compiled verifier is generated C code from `spin -a`; it is not an interpretive tool.
- The `-dead` flag is applied as a post-pass: unreachable code is determined from the visited-state recording of all PC values encountered.

---

## Alloy Analyzer (MIT)

Source: https://alloytools.org/documentation.html  
Source: D. Jackson, *Software Abstractions: Logic, Language, and Analysis*, MIT Press, 2006 (2nd ed. 2012)  
Source: https://alloytools.org/alloy6.html  
Source: https://alloy.readthedocs.io/

Alloy is a relational modeling language and bounded exhaustive analysis tool developed at MIT by Daniel Jackson. It uses SAT-based analysis (via the Kodkod backend). Alloy 6 (released 2021) added native mutable signatures, making explicit state machine modeling significantly more natural.

### State Graph Representation

In Alloy, state machines are represented **relationally** using signatures (types) and relations (fields). Prior to Alloy 6, the conventional idiom is:

```alloy
sig State {}
sig Transition {
  from: one State,
  to: one State,
  guard: lone Condition
}
one sig InitialState extends State {}
sig TerminalState extends State {}
```

The transition relation `from → to` is an explicit binary relation over `State`. Alloy 6 introduces `var` signatures and fields, allowing time-indexed mutable state without explicit encoding:

```alloy
var sig CurrentState in State {}
```

In Alloy 6, transitions are modeled as predicates that constrain the `next` time step. The state graph is encoded as relational constraints over `State` atoms rather than as an explicit adjacency structure.

### Reachability Algorithm

Alloy uses **bounded exhaustive analysis** via SAT solving (Kodkod → MiniSat or Glucose). The analysis works within a **scope** — an upper bound on the number of atoms of each signature type. For a scope of `n` states, Alloy exhaustively checks all instances up to that size.

Reachability is expressed as a **transitive closure** operator (`^` in Alloy):

```alloy
assert AllReachable {
  State = InitialState.*next_state
}
check AllReachable for 10 State
```

The `*` (reflexive transitive closure) and `^` (transitive closure) operators are encoded into the SAT formula by Kodkod using a squaring technique (iterating the relation to fixed point within the bounded scope). This is not BFS or DFS in the classical graph sense — it is a **relational algebra computation** lifted into propositional logic and solved by a SAT solver.

### Dead / Unreachable State Detection

Unreachable state detection is expressed as a structural property check:

```alloy
assert NoUnreachableStates {
  all s: State | s in InitialState.*next_state
}
check NoUnreachableStates for 8 State
```

If a counterexample exists (a model instance where some state is not reachable from the initial state), Alloy's Visualizer displays it graphically. Dead states (states with no outgoing transitions) are checked similarly:

```alloy
assert NoDeadStates {
  all s: State - TerminalState | some s.transitions
}
```

These checks are **bounded**: they only verify the property for instances with at most `N` atoms. Alloy cannot prove properties for all instances unless the finite-scope bound is provably sufficient (the "small scope hypothesis").

### Dominator Computation

Alloy does not have a built-in dominator computation primitive. Dominance can be expressed relationally — a state `d` dominates state `s` if every path from the initial state to `s` passes through `d` — but this requires quantification over paths that is costly to encode in relational algebra. There is no `dominatorTree` API in Alloy.

Domination can be expressed using transitive closure and path quantification:

```alloy
pred dominates[d, s: State] {
  s not in (State - d).*transitions[InitialState]
}
```

This is not a built-in primitive and not efficient for large state counts.

### Reverse Reachability

Reverse reachability is expressed by inverting the transition relation using the `~` (transpose) operator:

```alloy
-- States that can reach terminal state t
ReversibleStates: State = TerminalState.^(~transitions)
```

The `~transitions` expression computes the inverse of the `transitions` relation, and `^` computes transitive closure over it. This is a first-class Alloy operation, not a special API.

### Guard / Condition Handling During Analysis

Alloy handles guards through **overapproximation by default**: unless guards are explicitly modeled and constrained, the structural analysis treats all transitions as unconditionally enabled. When guards are modeled as constraints on the `Transition` signature, Alloy's SAT solver is free to choose any assignment to `Condition` atoms that satisfies the overall constraints — effectively an overapproximation that explores the full envelope of guard valuations.

For **definite reachability** (is some state reachable under any guard assignment?), Alloy provides sound results within the bounded scope. For **guaranteed reachability** (is the state reachable under all guard assignments?), universal quantification over conditions must be explicitly encoded.

### Analysis Result Reporting

Alloy produces two types of output:
1. **Counterexample instances**: if a check fails, Alloy's Visualizer displays a concrete instance (a specific set of states, transitions, and relation valuations) that violates the property. The instance is navigable in a GUI graph view.
2. **No counterexample found**: if the check passes within the scope, Alloy reports `"No counterexample found"` and notes the scope used.

Output format:
```
Executing "Check AllReachable for 8 State"
   Scope: exactly 8 State
   Counterexample found. Displaying instance...
```

Alloy does not produce linear text traces; counterexamples are instance graphs. The XML serialization of instances is available for programmatic processing.

### Performance Characteristics

- SAT-based; worst case exponential, but modern SAT solvers handle instances up to ~10^6 clauses practically.
- Transitive closure encoding (Kodkod squaring): for a relation over scope `k`, the squaring requires O(log k) matrix multiplications, each O(k^3) — total O(k^3 log k) propositional variables.
- Not intended for verification of large state machines (hundreds of states); targeted at small abstract models (typically < 20 atoms per signature).
- The bounded scope means Alloy provides **no completeness guarantee** beyond the declared scope.

### Notes

- Alloy 6 adds `temporal` mode (linear temporal logic over mutable models), enabling direct LTL property checking.
- The Kodkod backend supports multiple SAT solvers (MiniSat, Glucose, Lingeling) switchable from the GUI.
- Alloy's visualization produces graph layouts via GraphViz; structural properties are inspected visually rather than via a programmatic API.

---

## NuSMV / nuXmv

Source: https://nusmv.fbk.eu/NuSMV/userman/v26/nusmv.pdf  
Source: https://nuxmv.fbk.eu/  
Source: E.M. Clarke, A. Biere, R. Raimi, Y. Zhu, "Bounded Model Checking Using Satisfiability Solving," FMSD, 2001  
Source: A. Cimatti, E. Clarke, F. Giunchiglia, M. Roveri, "NuSMV: A New Symbolic Model Verifier," CAV 1999

NuSMV (New Symbolic Model Verifier) is a symbolic model checker developed at FBK and Carnegie Mellon University. nuXmv is its successor, adding IC3/PDR-based infinite-state verification. Both tools verify CTL and LTL properties on finite-state machines using BDD-based and SAT-based symbolic algorithms.

### State Graph Representation

NuSMV models are written in the SMV language. A finite state machine is specified declaratively with:
- `VAR` declarations — typed state variables (boolean, enumeration, bounded integer, array)
- `ASSIGN` — sequential variable update rules (`init(x) := ...`, `next(x) := ...`)
- `DEFINE` — macro abbreviations

The state space is implicitly defined as the Cartesian product of all variable domains. The **transition relation** `T(s, s')` is a boolean formula relating current-state variables to next-state variables, built from the `next(v) := expr` assignments.

NuSMV does not build an explicit adjacency list or matrix. The transition relation is represented as a **Binary Decision Diagram (BDD)** over the combined current/next variable ordering.

Example:
```smv
MODULE main
VAR
  state : {idle, running, done, error};
  valid : boolean;
ASSIGN
  init(state) := idle;
  next(state) := case
    state = idle & valid : running;
    state = running      : done;
    TRUE                 : state;
  esac;
```

### Reachability Algorithm

NuSMV uses **symbolic reachability** via fixed-point computation over BDDs:

1. Start with the initial state set `S0` (BDD encoding the `init()` constraints).
2. Apply the image operator: `Post(S) = ∃s. S(s) ∧ T(s, s')` — the set of all successor states of `S`.
3. Take the union: `S_{i+1} = S_i ∪ Post(S_i)`.
4. Repeat until fixed point: `S_{i+1} = S_i`.

The fixed point `Reach` is the exact set of reachable states as a BDD. This is **forward BFS in symbolic (set-at-a-time) form** — each iteration processes all states at the current BFS frontier simultaneously.

For CTL properties, NuSMV uses **backward fixed-point** computations:
- `EF p` (p is reachable) = least fixed point of `μZ. p ∨ EX Z` — backward reachability from states satisfying `p`.
- `AG p` (p holds in all reachable states) = `¬EF ¬p`.

nuXmv adds **IC3/PDR** (Property Directed Reachability), an incremental SAT-based algorithm for invariant checking that avoids explicit BDD construction.

### Dead / Unreachable State Detection

To check that a specific state is unreachable:

```smv
SPEC AG !(state = error)
```

This fails if any reachable state violates the condition, producing a counterexample path. To check that no reachable non-terminal state is a dead end:

```smv
SPEC AG (state != done -> EX TRUE)
```

`EX TRUE` is satisfiable iff the current state has at least one successor. This property fails if some reachable non-terminal state is a dead end.

NuSMV does not provide a built-in "list all unreachable states" command. The `print_reachable_states` interactive command prints the count:

```
NuSMV > go
NuSMV > print_reachable_states
The reachable states are: 3 (2^1.58) out of 8 (2^3)
```

### Dominator Computation

NuSMV does not compute dominators. The symbolic reachability framework does not expose a dominator tree API. CTL can express a limited form of necessary precedence — if state `A` must be visited before state `B` on every path, expressible as `AG(B → A-was-visited)` using auxiliary history variables — but NuSMV provides no built-in dominator primitive.

### Reverse Reachability

Reverse reachability (backward reachability from a target state) is performed using the **pre-image operator**:

`Pre(S) = ∃s'. S(s') ∧ T(s, s')` — the set of states whose successors include some state in `S`.

Backward reachability from a CTL query is performed automatically during `EF` and `EU` evaluation. The `check_spec` command with `EF target` triggers backward reachability from `target` states.

### Guard / Condition Handling During Analysis

NuSMV encodes guards directly into the BDD transition relation. A `case` expression compiles to a disjunction of conjunctions in BDD form. The symbolic analysis is **exact** for finite domains: every guard is encoded as a propositional formula, and the BDD represents the exact set of state-transition pairs satisfying all guards simultaneously.

Unlike explicit-state tools, NuSMV never evaluates guards against individual state valuations. The BDD manipulation operates over **all valuations simultaneously** — effectively computing the universal envelope of guard semantics without case-by-case enumeration. No approximation is needed for pure finite-domain models.

### Analysis Result Reporting

NuSMV produces two kinds of output:
1. **Verification result**: `-- specification ... is true` or `-- specification ... is false`.
2. **Counterexample trace**: when a property is false, NuSMV prints a sequence of state assignments from the initial state to the violating state:

```
-- specification AG !(state = error) is false
-- as demonstrated by the following execution sequence
Trace Type: Counterexample
-> State: 1.1 <-
  state = idle
  valid = TRUE
-> State: 1.2 <-
  state = running
-> State: 1.3 <-
  state = error
```

For liveness counterexamples (cycle witnesses), the trace shows a **lasso**: a finite prefix followed by a loop annotation.

### Performance Characteristics

- BDD size can grow exponentially with the number of Boolean variables (variable ordering is critical).
- The symbolic fixed-point algorithm is O(|variables| × diameter) BDD operations; each BDD operation is O(|BDD nodes|).
- BDD variable reordering heuristics (sifting, window permutation) are built in and apply automatically.
- nuXmv's IC3/PDR engine is often dramatically more efficient than BDD-based methods for deep safety properties.
- For state machines with enumeration state variables (e.g., 10 states → 4 Boolean bits), BDD representations are very compact and verification is fast.

### Notes

- NuSMV supports both CTL (branching time) and LTL (linear time) property languages.
- The `FAIRNESS` declaration excludes state cycles not satisfying a condition from liveness analysis.
- nuXmv adds support for infinite-precision integers, real arithmetic, and word-level operations (bit-vectors).

---

## UPPAAL

Source: https://uppaal.org/documentation/  
Source: K.G. Larsen, P. Pettersson, W. Yi, "UPPAAL in a Nutshell," STTT 1997  
Source: https://docs.uppaal.org/language-reference/query-language/

UPPAAL is a model checker for **timed automata** — finite automata extended with real-valued clocks and clock constraints on transitions and invariants on locations. It is developed jointly at Uppsala University and Aalborg University.

### State Graph Representation

UPPAAL models are networks of timed automata. Each automaton has:
- **Locations** (equivalent to states) — each location may have an **invariant** (a clock constraint that must hold while the process is in that location, e.g., `x <= 5`)
- **Edges** (transitions) — each edge has a **guard** (a clock and variable constraint, e.g., `x >= 3`), a **synchronization label** (channel send/receive), **updates** (clock resets and variable assignments), and **urgency** (urgent or committed)
- **Clocks** — real-valued variables reset by assignments and compared in guards/invariants

The global state is a tuple `(locations, clock_valuation, variable_valuation)`, where clock valuation is a vector in `R^k`.

UPPAAL internally represents the state space using **symbolic states** — pairs `(loc, Z)` where `loc` is a discrete location vector and `Z` is a **Difference Bound Matrix (DBM)** encoding the feasible clock valuations in that location zone. A DBM for `k` clocks is a `(k+1) × (k+1)` matrix of tight bounds.

### Reachability Algorithm

UPPAAL performs **zone-based symbolic reachability**:

1. Start with the initial symbolic state `(loc0, Z0)` where `Z0` satisfies the initial invariants.
2. Apply the **time successor** operator: advance time until the location invariant becomes tight (`Z' = ↑Z ∩ I(loc)` where `↑` is the time-elapse operator on DBMs).
3. Apply the **discrete successor** operator: for each enabled edge (guard satisfied by some clock valuation in `Z'`), compute the successor zone `Z'' = guard ∩ Z'` after applying resets.
4. Intersect with the target location invariant.
5. Normalize using **canonical form (closure)** of the DBM.
6. Check if the new symbolic state has been seen before (DBM containment check).
7. Continue BFS or DFS until the zone graph is exhausted.

The result is a **zone graph** — a finite symbolic representation of the infinite timed state space. UPPAAL applies **k-normalization** (extrapolation) to ensure finite termination.

### Dead / Unreachable State Detection

UPPAAL checks reachability queries of the form:

```uppaal
E<> P.s   -- "is it possible to reach location s in process P?"
```

`E<>` is TCTL existential reachability. If the query returns `NOT SATISFIED`, the location `s` is unreachable.

UPPAAL does not provide a built-in command to enumerate all unreachable locations. Users must check each location individually, or use the **Diagnostic Trace** feature to obtain a witness path.

For **deadlock detection**, UPPAAL provides:

```uppaal
A[] not deadlock
```

This checks that no reachable state is a deadlock (a state where time cannot advance and no discrete transition is enabled).

### Dominator Computation

UPPAAL does not compute dominators. The zone-based symbolic analysis is focused on timed reachability and does not expose structural dominator tree information.

### Reverse Reachability

Backward reachability is used implicitly by UPPAAL's CTL-based model checking for `A<>` (inevitability) and `A[]` (invariant) properties. The backward image operator for zones computes `Pre(Z)` for a zone `Z` — the set of states from which `Z` is reachable in one step. This is computed using DBM operations but is not exposed as a user-facing API.

The CTL query `A<> P.s` ("is `P.s` inevitable?") uses backward fixed-point computation from `P.s` zones.

### Guard / Condition Handling During Analysis

UPPAAL performs **overapproximation** for data-dependent guards when variables are involved beyond clock constraints. The zone graph is exact for clock constraints (DBMs represent the exact feasible clock region). However, when discrete variable guards are present (e.g., `x > 0` where `x` is an integer variable), UPPAAL either enumerates all valuations of discrete variables (adding them to the location vector), or uses bounded integer ranges that expand the state space.

For purely clock-based guards, no overapproximation occurs — the DBM intersection is exact. Guard-dependent reachability in UPPAAL is sound: if UPPAAL reports a location as unreachable, it is unreachable in the concrete system. If reachable, there exists a concrete witness path (exhibited as a diagnostic trace).

### Analysis Result Reporting

UPPAAL reports results in the GUI and via the `verifyta` command-line tool:

```
-- Formula is satisfied.
-- Formula is NOT satisfied.
```

With the `-t` flag, `verifyta` outputs a **diagnostic trace** (counterexample or witness) in `.xtr` format, which can be replayed in the simulator.

`verifyta` exit codes:
- `0`: satisfied
- `1`: not satisfied
- `2`: error

### Performance Characteristics

- Zone graph reachability: O(k^3 · |zones|) where `k` is the number of clocks and `|zones|` is the number of explored zones. DBM operations (closure, intersection, reset) are O(k^2).
- k-normalization keeps the zone graph finite but may introduce spurious states for certain extrapolations.
- Practical limit: ~10^7 symbolic states for complex models.

### Notes

- UPPAAL supports **statistical model checking** (SMC) mode for probabilistic timed automata, using Monte Carlo simulation rather than exhaustive exploration.
- The `E<>` and `A[]` queries cover the most common reachability and safety properties. UPPAAL 4.1+ adds observer automata for more complex properties.
- The UPPAAL **Tron** extension performs online test generation using on-the-fly reachability for test case selection.

---

## Lengauer-Tarjan Dominator Algorithm + LLVM DominatorTree

Source: T. Lengauer and R.E. Tarjan, "A Fast Algorithm for Finding Dominators in a Flowgraph," ACM TOPLAS 1979  
Source: https://llvm.org/docs/ProgrammersManual.html#dominator-tree  
Source: https://llvm.org/doxygen/DominatorTree_8h.html  
Source: https://llvm.org/doxygen/classllvm_1_1DominatorTree.html  
Source: https://llvm.org/doxygen/classllvm_1_1DominatorTreeBase.html  
Source: LLVM source: `llvm/include/llvm/Support/GenericDomTree.h`

### The Lengauer-Tarjan Algorithm

The Lengauer-Tarjan (LT) algorithm (1979) is the foundational algorithm for computing the **immediate dominator** of every node in a directed flow graph in near-linear time. A node `d` **dominates** node `n` (written `d dom n`) if every path from the entry node to `n` passes through `d`. The **immediate dominator** `idom(n)` is the unique dominator of `n` closest to `n` on any path from entry.

**Algorithm outline:**

1. **DFS numbering**: Perform DFS from the entry node, assigning each node a DFS number (preorder). Build the DFS spanning tree. Record the DFS tree parent `parent[v]` for each non-root vertex.

2. **Semi-dominator computation**: For each vertex `v` in reverse DFS order:
   - For each predecessor `u` of `v`, compute `sdom(v) = min(sdom(v), min_{w on path u→v in DFS tree, DFS#(w)>DFS#(v)} sdom(w))`.
   - The semi-dominator `sdom(v)` is the vertex with the smallest DFS number that has a tree path to `v` where all intermediate vertices have larger DFS numbers than `sdom(v)`.
   - Uses **path compression** (union-find with path compression) for efficient `eval` operations.

3. **Immediate dominator computation**: For each vertex `v` in DFS order:
   - Let `u = eval(v)` (the minimum semi-dominator ancestor on the spanning tree path).
   - If `sdom(u) = sdom(v)`, then `idom(v) = sdom(v)`.
   - Else `idom(v) = idom(u)` (deferred to a later pass).

**Data structures:**
- `ancestor[v]`, `label[v]`: path-compressed union-find for the spanning forest.
- `sdom[v]`, `idom[v]`: arrays indexed by DFS number.
- `bucket[v]`: list of vertices whose semi-dominator is `v`.

**Complexity:** O(V + E · α(V, E)) where α is the inverse Ackermann function — effectively O(V + E) for practical graphs. The "simple" variant without link-cut trees is O(V · E) but easier to implement; Cooper, Harvey, and Kennedy (2001) showed this simpler version is often faster in practice due to cache effects for typical compiler CFG sizes.

### Dominator Tree Data Structure

The dominator relation defines a tree (the **dominator tree**) rooted at the entry node, where `idom(n)` is the parent of `n`. Properties:
- Every node except the entry has exactly one immediate dominator.
- The dominators of `n` are exactly the ancestors of `n` in the dominator tree (including `n` itself).
- The **dominance frontier** of a node `n` is the set of nodes `y` such that `n` dominates some predecessor of `y` but does not strictly dominate `y`. Used in SSA construction (Cytron et al., 1991).

### LLVM `DominatorTree` API

LLVM implements the Lengauer-Tarjan algorithm in `llvm/Support/GenericDomTree.h` (template base) and `llvm/Analysis/DominatorTree.h` (IR-specific wrapper). The primary APIs:

```cpp
// Construction
DominatorTree DT;
DT.recalculate(Function &F);  // Build dominator tree from scratch

// Domination queries
bool DT.dominates(const BasicBlock *A, const BasicBlock *B);
// Returns true if A dominates B

bool DT.dominates(const Instruction *I, const Use &U);
// Returns true if instruction I dominates its use U

// Immediate dominator
DomTreeNode *DT.getNode(BasicBlock *BB);
DomTreeNode *Node->getIDom();      // Returns the immediate dominator node
DomTreeNode *DT.getRootNode();     // Entry node

// Iteration over dominated nodes (children in dominator tree)
for (auto *Child : Node->children()) { ... }

// DFS order iteration over dominator tree
for (auto I = df_begin(DT.getRootNode()); I != df_end(DT.getRootNode()); ++I) { ... }

// Incremental updates
DomTreeUpdater DTU(DT, DomTreeUpdater::UpdateStrategy::Lazy);
DTU.insertEdge(BBFrom, BBTo);
DTU.deleteEdge(BBFrom, BBTo);
DTU.flush();
```

Key types:
- `DomTreeNode`: a node in the dominator tree, wraps a `BasicBlock`, provides `getIDom()`, `getLevel()`, `children()`.
- `DominatorTree`: holds the full tree, answers domination queries.
- `DominatorTreeBase<NodeT>`: generic template used for both forward (dominator) and reverse (post-dominator) trees.

### Post-Dominator Trees

A **post-dominator tree** is constructed by reversing the control flow graph (considering all exit nodes as entries) and computing the dominator tree of the reversed graph. Node `d` **post-dominates** node `n` if every path from `n` to an exit passes through `d`.

LLVM provides `PostDominatorTree`:

```cpp
PostDominatorTree PDT;
PDT.recalculate(Function &F);
bool PDT.dominates(const BasicBlock *A, const BasicBlock *B);
// True if A post-dominates B
```

Post-dominator trees are used for:
- **Control dependence analysis**: node `n` is control-dependent on `m` if `m` is not a post-dominator of the predecessor of `n` where execution branches, but `m` post-dominates one successor.
- **Dead-end detection**: if a node `n` is not post-dominated by any exit node, it is on a path that never reaches the exit (potential infinite loop or unreachable exit).
- **Reverse data-flow analysis**: post-dominator trees drive backward analyses in the same way dominator trees drive forward analyses.

### Usage in LLVM Analysis Passes

Dominator information is exposed as analysis passes:

```cpp
// In a function pass (legacy pass manager)
auto &DT  = getAnalysis<DominatorTreeWrapperPass>().getDomTree();
auto &PDT = getAnalysis<PostDominatorTreeWrapperPass>().getPostDomTree();

// New pass manager
auto &DT = AM.getResult<DominatorTreeAnalysis>(F);
auto &PDT = AM.getResult<PostDominatorTreeAnalysis>(F);
```

The `DominatorTree` is a standard LLVM analysis — it is reused by dozens of downstream passes including loop analysis (`LoopInfo`), alias analysis, GVN, LICM, and dead code elimination.

### Performance Characteristics

- Lengauer-Tarjan: O(V + E · α(V, E)) time; O(V + E) space.
- LLVM's simple (non-link-cut-tree) variant runs in O(V^2) worst case but is fast for typical IR CFGs.
- Incremental updates via `DomTreeUpdater` are O(log V) amortized per edge insertion/deletion using tree-diff algorithms.
- For compiler IR CFGs (typically < 1000 basic blocks), dominator computation is sub-millisecond.

### Notes

- The LT algorithm was a fundamental advance over the O(V^3) naive algorithm (iterative bit-set computation).
- Dominance frontiers (Cytron et al., 1991) are built on top of dominator trees and are used for SSA form construction and phi-placement.
- The `GenericDomTree` template allows LLVM to apply the same algorithm to non-IR graph structures (e.g., `MachineBasicBlock` for machine code).

---

## XState Static Analysis (@xstate/graph)

Source: https://xstate.js.org/docs/packages/xstate-graph/  
Source: https://stately.ai/docs/graph  
Source: https://github.com/statelyai/xstate/tree/main/packages/xstate-graph  
Source: https://npm.im/@xstate/graph

XState is a JavaScript/TypeScript state machine library. The `@xstate/graph` package provides static analysis utilities that operate on XState machine definitions at analysis time (not during live execution). These utilities compute structural properties of the state graph without running the machine against real data.

### State Graph Representation

An XState machine is defined using `createMachine()` which produces a `StateMachine<Context, Event>` object. The machine definition is a plain JavaScript object tree with:
- `states` — a record of state node definitions (each with `on`, `always`, `after`, `type`, `invoke`, nested `states`)
- `on` — transition maps: `{ EVENT: { target: 'stateName', guard: guardFn, actions: [...] } }`
- `initial` — the initial state name
- `type` — `'compound'`, `'parallel'`, `'final'`, or `'atomic'`

`@xstate/graph` extracts the **state node graph** from this definition. Each `StateNode` has:
- `id`: fully-qualified dot-separated path (e.g., `'machine.idle.loading'`)
- `transitions`: computed array of `TransitionDefinition` objects (includes `target`, `guard`, `actions`, `eventType`)
- `states`: child state nodes (for hierarchical/compound states)

The internal graph representation in `@xstate/graph` is a `MachineGraph` — a plain object mapping state node `id` to an array of `{ state, event, nextState }` edges:

```typescript
type MachineGraph = Record<string, Array<{
  state: StateNode;
  event: EventObject;
  nextState: StateNode;
}>>;
```

This is an **adjacency list** representation keyed by state node ID.

### Reachability Algorithm

`@xstate/graph` builds the graph by traversing the state machine definition starting from the initial state using **BFS** (breadth-first traversal of state transitions):

**`getShortestPaths(machine, options)`**: Returns a record mapping each reachable state to its shortest path from the initial state. Internally uses BFS over the state graph.

```typescript
import { getShortestPaths, getSimplePaths } from '@xstate/graph';

const paths = getShortestPaths(machine);
// Returns: Record<stateId, { state, path: Array<{ state, event }> }>
```

**`getSimplePaths(machine, options)`**: Returns all simple paths (no repeated states) from the initial state to each reachable state. Uses DFS with a visited set to avoid cycles.

```typescript
const paths = getSimplePaths(machine);
// Returns: Record<stateId, Array<{ state, path }>>
```

**`getPaths(machine, options)`** (XState v5): Unified path API replacing `getShortestPaths` and `getSimplePaths`. Accepts a `pathGenerator` option to select BFS (shortest) or DFS (simple) strategy.

**`getStateNodes(state, machine)`**: Returns all `StateNode` objects reachable from `state` (or from the initial state if omitted). This is the core reachability computation used by other APIs.

### Dead / Unreachable State Detection

`@xstate/graph` does not provide a dedicated "unreachable state detection" API. Unreachable states can be identified by comparing declared state IDs against the reachable set:

```typescript
const allStateNodes = machine.stateIds;
const reachableNodes = new Set(Object.keys(getShortestPaths(machine)));
const unreachable = allStateNodes.filter(id => !reachableNodes.has(id));
```

This pattern is used in test generators (e.g., `@xstate/test`) to warn about states that can never be reached from the initial state. XState's Stately Studio visual editor highlights unreachable states in the diagram view.

Dead-end states (states with no outgoing transitions and not marked `type: 'final'`) are not automatically detected by `@xstate/graph`. Detection requires post-processing the machine definition:

```typescript
machine.stateIds
  .map(id => machine.getStateNodeById(id))
  .filter(node => node.type !== 'final' && node.transitions.length === 0);
```

### Dominator Computation

`@xstate/graph` does not compute dominators. No dominator tree API is exposed. The structural analysis is limited to path enumeration and reachability.

### Reverse Reachability

`@xstate/graph` does not expose a reverse reachability API directly. The `getPathsFromEvents` utility (XState v5) enumerates paths that produce a specific sequence of events, which can approximate reverse reachability for specific event types. For full reverse reachability (which states can reach state S?), users must build the reverse adjacency list manually from the `MachineGraph`.

### Guard / Condition Handling During Analysis

**Guards are treated as always-true** in all `@xstate/graph` structural analyses. This is a deliberate design decision documented in the `@xstate/graph` README:

> "Guards are not evaluated during graph traversal. All transitions are treated as if their guards are always satisfied."

This means `getShortestPaths` and `getSimplePaths` compute the **structural reachability** of the state graph, ignoring the runtime guard logic. A state reachable only when a guard condition is true will appear in the output even if that condition is never satisfied in practice.

The `@xstate/graph` options parameter accepts an `events` map that specifies which concrete event payloads to use when traversing — this allows some degree of guard-awareness by providing concrete event values that guards are evaluated against. However, this is opt-in and not exhaustive.

```typescript
const paths = getShortestPaths(machine, {
  events: {
    SUBMIT: [{ type: 'SUBMIT', value: 10 }],  // concrete event for guard evaluation
  }
});
```

### Analysis Result Reporting

`@xstate/graph` returns structured JavaScript objects rather than error messages:

- `getShortestPaths` → `Record<string, { state: State, path: Array<{ state: State, event: EventObject }> }>`
- `getSimplePaths` → `Record<string, Array<{ state: State, path: Array<{ state: State, event: EventObject }> }>>`

These are consumed programmatically by test generators or visual tools. There is no built-in warning/error reporting for structural problems; the APIs return data, not diagnostics.

The **Stately Studio** UI layer adds diagnostics on top: unreachable states are visually indicated, and the editor reports guards that reference undefined actions.

### Performance Characteristics

- `getShortestPaths` (BFS): O(V + E) time, O(V) space for the visited set.
- `getSimplePaths` (DFS, all simple paths): potentially exponential in the number of paths (O(V!) worst case for complete graphs). State machines are typically sparse, so path counts are manageable.
- XState machines are typically small (< 100 states) in production use; performance is not a limiting factor.
- Hierarchical state flattening adds a preprocessing step O(depth × states) before path computation.

### Notes

- `@xstate/graph` is the primary utility for **model-based testing** — it generates test paths that cover all reachable transitions.
- The `@xstate/test` package builds on `@xstate/graph` to create test cases for each path.
- XState v5 reworked the graph API significantly; `getShortestPaths` adds an `options.stopCondition` callback for incremental exploration.
- The **Stately Studio** editor exposes a subset of this analysis visually: unreachable states appear grayed, and transition coverage is shown in inspect mode.

---

## SCXML Validators (W3C SCXML)

Source: https://www.w3.org/TR/scxml/ (W3C SCXML Specification, 2015)  
Source: https://commons.apache.org/proper/commons-scxml/ (Apache Commons SCXML)  
Source: https://github.com/jbeard4/SCION (SCION SCXML implementation)  
Source: https://scxmltest.org/ (W3C SCXML test suite)

SCXML (State Chart XML) is a W3C standard (2015) for representing state machines as XML documents. It is based on David Harel's statechart formalism. Multiple implementations exist with varying degrees of static validation.

### State Graph Representation

An SCXML document represents a hierarchical state machine. The XML structure directly encodes the state graph:

```xml
<scxml initial="idle" xmlns="http://www.w3.org/2005/07/scxml">
  <state id="idle">
    <transition event="START" target="running"/>
  </state>
  <state id="running">
    <transition event="DONE" target="done"/>
    <transition event="FAIL" target="error"/>
  </state>
  <final id="done"/>
  <final id="error"/>
</scxml>
```

Key structural elements:
- `<state>` — compound state (may have children)
- `<parallel>` — parallel state (all children active simultaneously)
- `<final>` — terminal state
- `<history>` — history pseudostate
- `<transition>` — labeled with `event`, `cond` (guard), `target`

The state graph is an explicit tree (parent-child nesting for hierarchy) with cross-tree edges (transitions). The W3C spec defines the **configuration** as the set of currently active states.

### Reachability Algorithm

The W3C SCXML specification does not mandate a static reachability analysis algorithm. The spec defines runtime semantics (microstep/macrostep algorithm) but leaves static analysis to implementations.

**Apache Commons SCXML** performs basic structural validation during document loading:
1. Parse the XML into an in-memory object model (`SCXML`, `State`, `Transition`, etc.)
2. Resolve transition `target` attributes to `TransitionTarget` objects (catching dangling references)
3. Validate the initial state reference
4. Check that `<history>` pseudostates have valid `default` transitions

Apache Commons SCXML does **not** perform BFS/DFS reachability analysis as a separate pass.

**SCION** (a JavaScript SCXML implementation) includes a static analysis pass that builds a transition graph and performs BFS from the initial configuration to enumerate reachable states.

**Miro Samek's QM tool** (Quantum Modeling, targeting embedded C code generation) performs reachability analysis as part of code generation: it traces all reachable state transitions to generate only the code paths that can be executed, performing DFS from the initial state and emitting only reachable transition handlers.

### Dead / Unreachable State Detection

The W3C SCXML spec (§3.2) requires that all `target` attributes in `<transition>` elements must refer to an existing state within the document. A static validator must:

1. Collect all state IDs.
2. For every `<transition target="X">`, verify `X` is a valid state ID.

This catches **dangling references** but not **unreachable states** (declared but never targeted).

Static unreachable-state detection is **not specified** by the W3C standard. Individual implementations vary:

- **Apache Commons SCXML**: Does not warn about unreachable states. Only checks structural well-formedness (valid XML, valid ID references, valid initial state).
- **SCION**: Optionally performs reachability analysis and logs unreachable states to the console.
- **W3C SCXML Test Suite**: Tests conformance to runtime semantics, not static analysis behavior.
- **Eclipse SCXML editor** (deprecated): Highlighted unreachable states visually in the diagram editor.

### Dominator Computation

No SCXML validator or standard implementation performs dominator computation. The SCXML runtime semantics are focused on configuration management and microstep/macrostep execution, not flow-graph structural analysis.

### Reverse Reachability

No standard SCXML validator exposes reverse reachability. Implementations that build an explicit transition graph can compute the transpose, but this is not a specified feature of any major SCXML library.

### Guard / Condition Handling During Analysis

SCXML guards are `cond` expressions — arbitrary ECMAScript (or other datamodel expressions) embedded as attribute values:

```xml
<transition event="SUBMIT" cond="count > 0" target="processing"/>
```

Static validators perform **syntactic validation only** on `cond` expressions:
- Apache Commons SCXML parses `cond` as JEXL or JavaScript expressions and checks for syntax errors at load time.
- Runtime guard evaluation is never performed during static analysis.
- For reachability purposes, all static analyses treat `cond` guards as **always-enabled** (overapproximation).

This means SCXML static analysis is structurally sound (if a state is unreachable even with all guards ignored, it is truly unreachable) but not complete (a state may appear structurally reachable but actually be guarded off in all concrete executions).

### Analysis Result Reporting

**Apache Commons SCXML** reports validation errors via Java exceptions at load time:
- `ModelException`: thrown for structural violations (missing initial state, invalid target references, illegal nesting).
- Warnings are not separately reported; validation is pass/fail at the model level.

**SCION** reports errors via callback, using string error messages:
```
Warning: State 'idle' has no outgoing transitions and is not a final state
```

**W3C Conformance**: The specification defines conformance requirements for SCXML processors, but static validation requirements are limited:
- An SCXML document MUST have a valid initial configuration (at least one active state after initialization).
- Target attributes MUST reference valid states.
- A `<parallel>` state MUST have at least two child states.
- `<history>` pseudostates MUST have a `default` transition.

### Performance Characteristics

- SCXML document parsing: O(N) where N is the document size.
- ID resolution: O(N · M) naive, O(N + M) with hash map (N states, M transitions).
- Apache Commons SCXML handles documents with hundreds of states without performance issues.
- No asymptotic complexity guarantees are published for SCXML static validators; operations are all linear or near-linear for realistic state machine sizes.

### Notes

- The W3C SCXML specification deliberately keeps static validation requirements minimal to allow lightweight implementations (e.g., embedded systems).
- SCXML `<invoke>` elements introduce dynamic sub-state machines, making static reachability analysis undecidable in general.
- The SCXML `<datamodel>` element introduces external state that guards can reference; static analysis of guards against the datamodel is beyond the scope of any standard SCXML validator.
- Miro Samek's QM performs the most aggressive static SCXML-adjacent analysis: it generates C code only for reachable transitions by performing DFS from the initial state, effectively computing the reachable subgraph and eliding dead code.
- The **StateCharts.io** (Stately) tool extends SCXML concepts with richer static analysis (reachability, type checking of context) but uses its own XState format rather than raw SCXML.

---

*End of survey.*

---

I don't have a file-writing tool in this mode — the complete survey content is above, ready to be saved to [research/architecture/compiler/state-graph-analysis-survey.md](research/architecture/compiler/state-graph-analysis-survey.md). The survey covers all seven systems (SPIN/Promela, Alloy, NuSMV/nuXmv, UPPAAL, Lengauer-Tarjan/LLVM, XState @xstate/graph, and SCXML validators), each with consistent sections for state graph representation, reachability algorithm, dead/unreachable state detection, dominator computation, reverse reachability, guard handling, result reporting, performance characteristics, and notes. The document is approximately 5500 words of raw technical facts with no Precept interpretation.
