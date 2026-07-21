<!--
GENERATED FILE — do not hand-edit.
Source: fault-1-division-primitive-transition-write.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault group 1 — division and remainder by zero, primitive numeric lanes, transition-row write

Family id: fault-1-division-primitive-transition-write
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic validity argument
- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match validity argument
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: respellability is measured against the corpus, not asserted
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- Every cell in this file resolves to the same verdict; fault-1/integer-divide-integer carries a live-verified concrete band member (`when not (Divide.Parts == 0)`) and its licensed respelling; the other nine cells inherit it structurally rather than re-deriving it.
- This verdict is model-derived only. Per the ratification protocol's stated limits (matrix § Ratification protocol), agreement with the sample corpus is necessary, not sufficient, and the corpus has not been run against this family in this pass.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 respellability is analogous to Witness Family 1's — every sound band member's business intent respells into a licensed guard or arg-bound form
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: a family verdict ratifies only when it agrees with the classified sample corpus; not yet run for this family

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## fault-1/integer-divide-integer — IntegerDivideInteger — integer / integer — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:119 — catalog: IntegerDivideInteger — integer / integer declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger — integer / integer |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) (field modifiers) is not exercised: this witness design reads the divisor directly off the triggering event's own argument, so there is no field in the divisor position for a field modifier to attach to. A field-governed-divisor variant is not authored in this file. Premise (d) (pre-state constraints) is listed as available at the transition-row-action-operand evaluation-site category in general, but this cell's applicable-class set omits it: the matrix's § Validity arguments (docs/Working/obligation-discharge-matrix-2026-07-19.md:200-217) states seven named arguments, and none of them argues that an explicit `rule` construct, consumed as premise (d) via the CompositionalConstraint strategy, truth-preservingly discharges a fault precondition — 'Inductive hypothesis plus sign monotonicity' is stated for a constraint-preservation WP (Witness Family 1 Base B), not a fault obligation, and no other named argument covers this derivation either. Per the Rule-validity gate (matrix § Rule validity, :190-198: 'no cell ratifies whose derivation cites an argument-less rule'), this cell cannot record a (d)-based discharge contract entry without inventing an argument the matrix does not carry. This is recorded as a missing rule, not resolved here. It is not merely a hypothetical gap: reproduced locally at commit e1a14d91 (scratch, not committed to the repo) — a precept declaring `rule Parts != 0`, dividing by `Parts` in one handler and setting `Parts = 0` in a separate handler in the same state, compiles with zero diagnostics; the second handler's own rule-preservation WP evaluates to false (visible in the WP calculator's rule trace) but nothing turns that into a compile error, and the division is accepted regardless. This is the identical defect docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B, documents (`rule PlannedQuantity > 0` consumed as a premise for `100.0 / PlannedQuantity` while a separate handler sets `PlannedQuantity = 0`) — reproduced independently here with this cell's own field names rather than cited secondhand. Per the false-proof hazard, any witness that cited this derivation would have to record builtStatus as unverified rather than proven-today — this cell avoids the question by not authoring the witness at all, since the matrix gives no derivation to attribute the (would-be) acceptance to.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:119 — catalog: IntegerDivideInteger — integer / integer — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as integer default 0
field PerPart as integer default 0

state Active initial

event Create(StartTotal as integer) initial
event Divide(Parts as integer)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set PerPart = Total / Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as integer nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as integer nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as integer nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: when not (Divide.Parts == 0)
- Its licensed respelling: when Divide.Parts != 0

- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14 adopted rules; none rewrites `not (a == b)` to `a != b` — the closest rules are N7 (double negation) and N14 (negated ordering comparisons, which is stated for <=/</>=/>, not ==/!=)

- Live-verified band member: compiled at commit e1a14d91 with `from Active on Divide when not (Divide.Parts == 0) -> set PerPart = Total / Divide.Parts -> no transition` — HEAD still rejects with DivisionByZero, confirming this spelling is not recognized as normal-form-equal to the direct comparison under the rules the calculator implements today. Sound (the guard is logically equivalent to Parts != 0), but no adopted normalization rule (N1-N14) rewrites equality-negation to the direct comparison, so it sits outside the stated normal form — a genuine sound-but-unprovable band member, not a built-vs-model gap.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.
- Strategy names (GuardInPath, DeclarationAttribute) are read from src/Precept/Pipeline/ProofLedger.cs:100-114 and confirmed as the dispatch order for this obligation kind in src/Precept/Pipeline/ProofEngine.cs:1068-1075 (TryDeclarationAttributeProof before TryGuardInPathProof) and src/Precept/Pipeline/ProofEngine.Strategies.cs:85,:713 — this is a source-inspection claim, not something tools/Precept.MatrixTools's CLI prints for fault obligations (it prints diagnostics and rule WPs only, see the wp note above), so the accept/reject outcomes are live-verified but the specific strategy label is verified by reading the dispatcher, not by an observed printout.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:119 — catalog: IntegerDivideInteger — integer / integer — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## fault-1/integer-modulo-integer — IntegerModuloInteger — integer % integer — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:128 — catalog: IntegerModuloInteger — integer % integer declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerModuloInteger — integer % integer |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) is not applicable for the same reason as fault-1/integer-divide-integer (no field sits in the divisor position). Premise (d) is available at this evaluation-site category per its general survey but is not part of this cell's discharge contract, for the same reason recorded in full on fault-1/integer-divide-integer: the matrix's § Validity arguments states no argument for a rule-premised (CompositionalConstraint) discharge of a fault obligation. Recorded as a missing rule, not resolved here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:128 — catalog: IntegerModuloInteger — integer % integer — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as integer default 0
field Remainder as integer default 0

state Active initial

event Create(StartTotal as integer) initial
event Divide(Parts as integer)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set Remainder = Total % Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as integer nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as integer nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as integer nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The same band member illustrated on fault-1/integer-divide-integer (`when not (Divide.Parts == 0)`) applies structurally to every lane in this file; not re-verified live per cell.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:128 — catalog: IntegerModuloInteger — integer % integer — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## fault-1/decimal-divide-decimal — DecimalDivideDecimal — decimal / decimal — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:150 — catalog: DecimalDivideDecimal — decimal / decimal declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal — decimal / decimal |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) is not applicable for the same reason as fault-1/integer-divide-integer (no field sits in the divisor position). Premise (d) is available at this evaluation-site category per its general survey but is not part of this cell's discharge contract, for the same reason recorded in full on fault-1/integer-divide-integer: the matrix's § Validity arguments states no argument for a rule-premised (CompositionalConstraint) discharge of a fault obligation. Recorded as a missing rule, not resolved here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:150 — catalog: DecimalDivideDecimal — decimal / decimal — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as decimal default 0.0
field PerPart as decimal default 0.0

state Active initial

event Create(StartTotal as decimal) initial
event Divide(Parts as decimal)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set PerPart = Total / Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as decimal nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as decimal nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as decimal nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The same band member illustrated on fault-1/integer-divide-integer (`when not (Divide.Parts == 0)`) applies structurally to every lane in this file; not re-verified live per cell.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:150 — catalog: DecimalDivideDecimal — decimal / decimal — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## fault-1/decimal-modulo-decimal — DecimalModuloDecimal — decimal % decimal — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:159 — catalog: DecimalModuloDecimal — decimal % decimal declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalModuloDecimal — decimal % decimal |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) is not applicable for the same reason as fault-1/integer-divide-integer (no field sits in the divisor position). Premise (d) is available at this evaluation-site category per its general survey but is not part of this cell's discharge contract, for the same reason recorded in full on fault-1/integer-divide-integer: the matrix's § Validity arguments states no argument for a rule-premised (CompositionalConstraint) discharge of a fault obligation. Recorded as a missing rule, not resolved here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:159 — catalog: DecimalModuloDecimal — decimal % decimal — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as decimal default 0.0
field Remainder as decimal default 0.0

state Active initial

event Create(StartTotal as decimal) initial
event Divide(Parts as decimal)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set Remainder = Total % Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as decimal nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as decimal nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as decimal nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The same band member illustrated on fault-1/integer-divide-integer (`when not (Divide.Parts == 0)`) applies structurally to every lane in this file; not re-verified live per cell.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:159 — catalog: DecimalModuloDecimal — decimal % decimal — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## fault-1/number-divide-number — NumberDivideNumber — number / number — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:181 — catalog: NumberDivideNumber — number / number declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | NumberDivideNumber — number / number |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) is not applicable for the same reason as fault-1/integer-divide-integer (no field sits in the divisor position). Premise (d) is available at this evaluation-site category per its general survey but is not part of this cell's discharge contract, for the same reason recorded in full on fault-1/integer-divide-integer: the matrix's § Validity arguments states no argument for a rule-premised (CompositionalConstraint) discharge of a fault obligation. Recorded as a missing rule, not resolved here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:181 — catalog: NumberDivideNumber — number / number — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

- This cell's divisor arg is declared `number` (IEEE double). The Arg-bound interval arithmetic argument (matrix:206) states its own open dependency here verbatim: 'the argument covers the exact lanes (integer, decimal) only... extending the rule [to number] requires an outward-rounding side condition on the computed bound, not yet stated.' That dependency is stated for a *computed* (summed) bound; this cell's obligation needs no arithmetic on the arg at all (the declared value is compared directly to zero, no addition or rounding intervenes), so the specific rounding failure mode the argument names does not obviously reach this cell. This is not resolved here: the matrix's stated argument does not say whether the exact-lanes restriction is about summation specifically or about the `number` lane's declared-bound comparison generally, and this cell inherits the weaker, unresolved citation rather than a citation this pass wrote from scratch.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as number default 0.0
field PerPart as number default 0.0

state Active initial

event Create(StartTotal as number) initial
event Divide(Parts as number)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set PerPart = Total / Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as number nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as number nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as number nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The same band member illustrated on fault-1/integer-divide-integer (`when not (Divide.Parts == 0)`) applies structurally to every lane in this file; not re-verified live per cell.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:181 — catalog: NumberDivideNumber — number / number — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## fault-1/number-modulo-number — NumberModuloNumber — number % number — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:190 — catalog: NumberModuloNumber — number % number declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | NumberModuloNumber — number % number |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) is not applicable for the same reason as fault-1/integer-divide-integer (no field sits in the divisor position). Premise (d) is available at this evaluation-site category per its general survey but is not part of this cell's discharge contract, for the same reason recorded in full on fault-1/integer-divide-integer: the matrix's § Validity arguments states no argument for a rule-premised (CompositionalConstraint) discharge of a fault obligation. Recorded as a missing rule, not resolved here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:190 — catalog: NumberModuloNumber — number % number — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

- This cell's divisor arg is declared `number` (IEEE double). The Arg-bound interval arithmetic argument (matrix:206) states its own open dependency here verbatim: 'the argument covers the exact lanes (integer, decimal) only... extending the rule [to number] requires an outward-rounding side condition on the computed bound, not yet stated.' That dependency is stated for a *computed* (summed) bound; this cell's obligation needs no arithmetic on the arg at all (the declared value is compared directly to zero, no addition or rounding intervenes), so the specific rounding failure mode the argument names does not obviously reach this cell. This is not resolved here: the matrix's stated argument does not say whether the exact-lanes restriction is about summation specifically or about the `number` lane's declared-bound comparison generally, and this cell inherits the weaker, unresolved citation rather than a citation this pass wrote from scratch.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as number default 0.0
field Remainder as number default 0.0

state Active initial

event Create(StartTotal as number) initial
event Divide(Parts as number)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set Remainder = Total % Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as number nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as number nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as number nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The same band member illustrated on fault-1/integer-divide-integer (`when not (Divide.Parts == 0)`) applies structurally to every lane in this file; not re-verified live per cell.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:190 — catalog: NumberModuloNumber — number % number — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## fault-1/integer-divide-decimal — IntegerDivideDecimal — integer / decimal — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:212 — catalog: IntegerDivideDecimal — integer / decimal declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideDecimal — integer / decimal |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) is not applicable for the same reason as fault-1/integer-divide-integer (no field sits in the divisor position). Premise (d) is available at this evaluation-site category per its general survey but is not part of this cell's discharge contract, for the same reason recorded in full on fault-1/integer-divide-integer: the matrix's § Validity arguments states no argument for a rule-premised (CompositionalConstraint) discharge of a fault obligation. Recorded as a missing rule, not resolved here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:212 — catalog: IntegerDivideDecimal — integer / decimal — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as decimal default 0.0
field PerPart as decimal default 0.0

state Active initial

event Create(StartTotal as decimal) initial
event Divide(Parts as integer)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set PerPart = Total / Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as integer nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as integer nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as integer nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The same band member illustrated on fault-1/integer-divide-integer (`when not (Divide.Parts == 0)`) applies structurally to every lane in this file; not re-verified live per cell.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:212 — catalog: IntegerDivideDecimal — integer / decimal — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## fault-1/integer-modulo-decimal — IntegerModuloDecimal — integer % decimal — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:221 — catalog: IntegerModuloDecimal — integer % decimal declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerModuloDecimal — integer % decimal |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) is not applicable for the same reason as fault-1/integer-divide-integer (no field sits in the divisor position). Premise (d) is available at this evaluation-site category per its general survey but is not part of this cell's discharge contract, for the same reason recorded in full on fault-1/integer-divide-integer: the matrix's § Validity arguments states no argument for a rule-premised (CompositionalConstraint) discharge of a fault obligation. Recorded as a missing rule, not resolved here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:221 — catalog: IntegerModuloDecimal — integer % decimal — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as decimal default 0.0
field Remainder as decimal default 0.0

state Active initial

event Create(StartTotal as decimal) initial
event Divide(Parts as integer)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set Remainder = Total % Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as integer nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as integer nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as integer nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The same band member illustrated on fault-1/integer-divide-integer (`when not (Divide.Parts == 0)`) applies structurally to every lane in this file; not re-verified live per cell.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:221 — catalog: IntegerModuloDecimal — integer % decimal — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## fault-1/integer-divide-number — IntegerDivideNumber — integer / number — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:243 — catalog: IntegerDivideNumber — integer / number declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideNumber — integer / number |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) is not applicable for the same reason as fault-1/integer-divide-integer (no field sits in the divisor position). Premise (d) is available at this evaluation-site category per its general survey but is not part of this cell's discharge contract, for the same reason recorded in full on fault-1/integer-divide-integer: the matrix's § Validity arguments states no argument for a rule-premised (CompositionalConstraint) discharge of a fault obligation. Recorded as a missing rule, not resolved here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:243 — catalog: IntegerDivideNumber — integer / number — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

- This cell's divisor arg is declared `number` (IEEE double). The Arg-bound interval arithmetic argument (matrix:206) states its own open dependency here verbatim: 'the argument covers the exact lanes (integer, decimal) only... extending the rule [to number] requires an outward-rounding side condition on the computed bound, not yet stated.' That dependency is stated for a *computed* (summed) bound; this cell's obligation needs no arithmetic on the arg at all (the declared value is compared directly to zero, no addition or rounding intervenes), so the specific rounding failure mode the argument names does not obviously reach this cell. This is not resolved here: the matrix's stated argument does not say whether the exact-lanes restriction is about summation specifically or about the `number` lane's declared-bound comparison generally, and this cell inherits the weaker, unresolved citation rather than a citation this pass wrote from scratch.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as integer default 0
field PerPart as number default 0.0

state Active initial

event Create(StartTotal as integer) initial
event Divide(Parts as number)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set PerPart = Total / Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as number nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as number nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as number nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The same band member illustrated on fault-1/integer-divide-integer (`when not (Divide.Parts == 0)`) applies structurally to every lane in this file; not re-verified live per cell.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:243 — catalog: IntegerDivideNumber — integer / number — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## fault-1/integer-modulo-number — IntegerModuloNumber — integer % number — divisor from the triggering event's own argument

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the group's precedent)
- src/Precept/Language/Operations.cs:252 — catalog: IntegerModuloNumber — integer % number declares the divisor-non-zero Numeric ProofRequirement

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerModuloNumber — integer % number |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divide.Parts != 0

Weakest precondition: Divide.Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the divisor operand at this catalog site — instantiated here as the triggering event's own declared argument |
| 0 | the catalog-declared zero threshold for this operation's divisor-safety condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14; no rule rewrites `not (a == b)` to `a != b`, so an equality-negation guard is not normal-form-equal to the direct comparison

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly off the triggering event's own argument with no intervening computation (spec:164, :170 purity), so premise (c) is available via the row's guard over that same argument (guards select before the action runs, spec:1897) and premise (b) is available via governance enforcing the argument's declared modifier before the division reads it (spec:268). Premise (a) is not applicable for the same reason as fault-1/integer-divide-integer (no field sits in the divisor position). Premise (d) is available at this evaluation-site category per its general survey but is not part of this cell's discharge contract, for the same reason recorded in full on fault-1/integer-divide-integer: the matrix's § Validity arguments states no argument for a rule-premised (CompositionalConstraint) discharge of a fault obligation. Recorded as a missing rule, not resolved here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (`Divide.Parts != 0`) against the row's guard conjuncts (a finite set). No match rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match — a row's actions execute only if its guard evaluated true, and the action sees the same pre-state and args the guard read
- docs/language/precept-language-spec.md:1897 — spec: guards select — a row's actions run only if its guard evaluated true
- docs/language/precept-language-spec.md:164 — spec: each assignment in a row sees the state left by preceding assignments; here there are none before the read

**Entry 2 — (b)**

- Derivation: governance enforces the event arg's declared bound before any computation reads it; a `nonzero` (or `positive`) bound on the divisor arg excludes zero from its interval -> arg-bound discharge
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor arg's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on every value entering as an event arg, before any computation derives from it; the same argument is stated for a summed bound, this cell's use is the single-operand degenerate case of the same governance-before-computation reasoning
- docs/language/precept-language-spec.md:268 — spec: governance enforced at runtime on every value entering as an event argument, before any computation derives from it
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same `Months as integer positive` shape, here specialized to `nonzero`
- src/Precept/Language/Operations.cs:252 — catalog: IntegerModuloNumber — integer % number — declares the Numeric divisor-non-zero ProofRequirement this cell instantiates

- This cell's divisor arg is declared `number` (IEEE double). The Arg-bound interval arithmetic argument (matrix:206) states its own open dependency here verbatim: 'the argument covers the exact lanes (integer, decimal) only... extending the rule [to number] requires an outward-rounding side condition on the computed bound, not yet stated.' That dependency is stated for a *computed* (summed) bound; this cell's obligation needs no arithmetic on the arg at all (the declared value is compared directly to zero, no addition or rounding intervenes), so the specific rounding failure mode the argument names does not obviously reach this cell. This is not resolved here: the matrix's stated argument does not say whether the exact-lanes restriction is about summation specifically or about the `number` lane's declared-bound comparison generally, and this cell inherits the weaker, unresolved citation rather than a citation this pass wrote from scratch.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The Vocabulary states this schema for a summed multi-arg bound (Family 1 Base A); this cell's obligation is a single-operand exclusion (divisor != 0), so the 'interval combination' degenerates to a single declared interval excluding zero rather than a sum of two bounds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept W

field Total as integer default 0
field Remainder as number default 0.0

state Active initial

event Create(StartTotal as integer) initial
event Divide(Parts as number)

on Create
    -> set Total = Create.StartTotal

from Active on Divide
    -> set Remainder = Total % Divide.Parts
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Divide.Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Divide` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-nonzero* — arg-modifier

- Addition: Parts as number nonzero
- Premise classes: (b)
- Derivation: governance enforces `Parts != 0` on every value entering as the Divide event's arg, before the division reads it -> arg-bound discharge
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Divide.Parts` becomes `Parts as number nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Divide.Parts >= 0 | 0 >= 0 is true, so the guard admits Parts == 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-nonzero | Parts as number nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The same band member illustrated on fault-1/integer-divide-integer (`when not (Divide.Parts == 0)`) applies structurally to every lane in this file; not re-verified live per cell.

**What the sources leave unstated or ambiguous here**

- The obligation is the catalog-declared safety precondition at this evaluation site, not a constraint WP through a write plan — the fault family mints one obligation per evaluation site directly (matrix § The minting rule, :186; § Per-family case shapes, :154), so no backward substitution applies here.
- wp.canonicalKey is omitted: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) computes constraint-family WPs only. Every witness in this file was compiled through the calculator's CLI and it printed "no computable obligations (no rules and no desugaring modifiers in scope)" for every one of them, including the base programs that DO carry a fault diagnostic — confirming the calculator has no method for fault-family safety preconditions at all, not merely a gap on this file's specific shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — the group's stated precedent for this discharge shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: Per-family case shapes — fault family: the catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: The minting rule — fault minting: each evaluation site in a plan mints its own fault obligations
- src/Precept/Language/Operations.cs:252 — catalog: IntegerModuloNumber — integer % number — declares the divisor Numeric ProofRequirement
- docs/compiler/proof-engine.md:515 — code: Catalog-Driven Obligation Instantiation — the type checker stamps ProofRequirements onto typed nodes; the proof engine reads them, does not compute them
- docs/compiler/soundness-and-coverage.md § Coverage by fault mode — code: the DivisionByZero FaultCode this obligation's rejection diagnostic carries

## Fields with no rendering rule

Data the generator has no rendering rule for, surfaced verbatim rather than dropped. Each is either a schema addition the generator has not caught up with, or a stray field.

| Where | Field | Value |
|---|---|---|
| cells[fault-1/integer-divide-integer].respellability | overrideVerdict | "yes" |

