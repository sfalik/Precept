# Coverage: spec-semantics-proof

Region: `docs/language/precept-language-spec.md` lines 1847-2169 — §3A Language Semantics + §4 Graph Analyzer + §5 Proof Engine + Open Questions + Cross-References.

**Rollup:** blocks total 52 / covered 0 / added 24 / no-behavior 28. Cells: draft-in-region 0, added 51, total 51.

**Draft-cell note:** All 15 draft cells in `spec.draft.json` cite lines 1580-1670 (temporal/quantity/currency/percentage surface), which fall OUTSIDE this region. None match a block here, so this unit contributes 0 draft cells and 51 net-new added cells. Those draft cells belong to the unit that owns 1580-1670; not dropped, just not in scope for this region.

**Disposition rationale:** §3A.2 (outcome verdict taxonomy), §3A.3 (subject attribution), §3A.4 (mutation atomicity), §3A.6 (inspection), the collect-all/first-match and guards-are-routing prose, restoration outcomes, and the construction-time constraint-composition ordering are all runtime evaluation semantics with no compile-time accept/reject verdict to exercise — marked NO-BEHAVIOR. §4 and §5 headings are status pointers. Structural checks (dead-end/sink, unreachable, initial-state reachability) are named only in the CC#26 compiler note at line 2113; cells are anchored there. Diagnostic codes for structural/construction checks drawn from spec prose + backstop `spec-coverage-audit.md` (PRE0119 StructuralSinkState, PRE0125 AlwaysRejecting, PRE0115).

## §3A.1 Constraint Semantics

| block | kind | disposition | cells / reason |
|---|---|---|---|
| §3A heading + intro (1847) | section | NO-BEHAVIOR | semantic-model framing prose |
| §3A.1 heading + intro (1851) | section | NO-BEHAVIOR | "distinguishes categories of truth" framing |
| #### Rules prose (1855-1868) | section | ADDED | rule-references-event-arg-rejected |
| Rules code example (1861) | example | ADDED | rule-global-accept, rule-guarded-accept |
| #### Ensures prose + anchors (1870-1887) | section | ADDED | ensure-to-state-accept, ensure-from-state-accept |
| Ensures code example (1876) | example | ADDED | ensure-in-state-accept, ensure-guarded-accept, ensure-on-event-accept |
| #### Rejections (1889-1893) | section | ADDED | reject-row-accept (remainder = rejection-vs-constraint-failure semantics, runtime) |
| #### Guards (1895-1897) | section | NO-BEHAVIOR | runtime routing semantics (guards select, no violation) |
| #### Collect-all vs first-match (1899-1906) | section | NO-BEHAVIOR | runtime evaluation-order semantics |

## §3A.2 Outcomes and Semantic Verdicts

| block | kind | disposition | cells / reason |
|---|---|---|---|
| §3A.2 heading + outcome-types list (1908-1928) | section | NO-BEHAVIOR | runtime verdict taxonomy; no compile check |
| Construction outcomes table (1934-1942) | table | NO-BEHAVIOR | runtime outcome meanings; structural "Transitioned impossible" covered by construction-transition-excluded-rejected |
| Restoration outcomes (1944-1946) | section | NO-BEHAVIOR | runtime restore-time semantics |

## §3A.3 – §3A.4

| block | kind | disposition | cells / reason |
|---|---|---|---|
| §3A.3 Constraint Violation Subject Attribution (1948-1963) | section | NO-BEHAVIOR | runtime attribution model |
| §3A.4 Mutation Atomicity (1965-1971) | section | NO-BEHAVIOR | runtime working-copy / all-or-nothing semantics |

## §3A.5 Entity Construction

| block | kind | disposition | cells / reason |
|---|---|---|---|
| §3A.5 intro (1973-1975) | section | NO-BEHAVIOR | motivation prose |
| Event decl `initial` modifier prose (1977-1983) | section | ADDED | two-initial-events-rejected, initial-modifier-parameterless-accept |
| Event decl code example (1979-1981) | example | ADDED | initial-modifier-accept |
| Construction row syntax prose (1985-1987) | section | NO-BEHAVIOR | restates "must use `on` not `from`"; testable form = initial-event-in-transition-row-rejected (Fire-once block) |
| Construction row code examples (1989-2007) | example | ADDED | construction-row-on-event-accept, construction-row-guarded-accept, construction-row-reject-path-accept |
| Construction-rows-NOT-transition-rows table (2011-2019) | table | ADDED | construction-transition-excluded-rejected, construction-no-transition-excluded-rejected, transition-row-transition-accept |
| Row classification prose (2019) | section | NO-BEHAVIOR | type-checker-internal classification (how compiler decides) |
| Construction Idioms heading + prose (2021-2023) | section | NO-BEHAVIOR | domain-choice motivation |
| Construction Idioms table (2025-2028) | table | ADDED | pattern-a-constructor-existential-accept, pattern-b-free-construction-accept |
| Pattern B prose (2030) | section | ADDED | pattern-b-required-birth-absent-field-rejected |
| Construction semantics prose + list (2032-2040) | section | ADDED | no-initial-event-all-fields-defaulted-accept |
| Construction-time constraint composition (2042-2050) | section | NO-BEHAVIOR | runtime constraint-composition ordering |
| Compiler enforcement list (2052-2056) | section | ADDED | required-fields-need-initial-event-rejected, initial-event-missing-assignments-rejected, uninitialized-field-self-read-rejected, uninitialized-cross-field-read-rejected |
| Design rationale (2058) | section | NO-BEHAVIOR | rationale |
| Why `on Event` not `from` (2060) | section | NO-BEHAVIOR | rationale |
| Why `transition` excluded (2062) | section | NO-BEHAVIOR | rationale; behavior covered by construction-transition-excluded-rejected |
| Why same `EventRow` construct (2064) | section | NO-BEHAVIOR | rationale |
| Why `initial` not renamed (2066) | section | NO-BEHAVIOR | rationale |
| Why guards allowed (2068) | section | NO-BEHAVIOR | rationale; accept covered by construction-row-guarded-accept |
| Why non-initial events can't use `on Event` in stateful (2070-2076) | section | ADDED | bare-on-event-non-initial-stateful-rejected |
| Cross-language precedent prose (2078) | section | NO-BEHAVIOR | precedent narrative |
| Cross-language precedent table (2080-2089) | table | NO-BEHAVIOR | precedent survey |
| Precept's contribution (2091) | section | NO-BEHAVIOR | comparison narrative |
| "Construction must always be possible" (2093) | section | ADDED | always-rejecting-initial-event-rejected |
| Fire-once guarantee heading + list (2095-2101) | section | ADDED | initial-event-in-transition-row-rejected (runtime UndefinedEvent / AvailableEvents portions = runtime) |
| Stateless Precepts prose + list (2103-2111) | section | ADDED | stateless-precept-no-states-accept, stateless-with-initial-event-accept |
| Compiler note blockquote — CC#26 (2113) | example | ADDED | stateless-exempt-reachability-accept, stateless-exempt-dead-end-accept, stateless-exempt-unreachable-accept, dead-end-state-rejected, unreachable-state-rejected, state-unreachable-from-initial-rejected |

## §3A.6 Inspection

| block | kind | disposition | cells / reason |
|---|---|---|---|
| §3A.6 Inspection as First-Class Operation (2115-2121) | section | NO-BEHAVIOR | runtime inspection guarantee (answer matches execution) |

## §4 Graph Analyzer

| block | kind | disposition | cells / reason |
|---|---|---|---|
| §4 heading + Status pointer (2125-2129) | section | NO-BEHAVIOR | status pointer to graph-analyzer.md; structural checks enumerated at CC#26 (2113) |

## §5 Proof Engine

| block | kind | disposition | cells / reason |
|---|---|---|---|
| §5 heading + Status pointer (2131-2133) | section | NO-BEHAVIOR | status pointer |
| Proof engine intro prose (2135) | section | ADDED | initial-state-satisfiability-pre0115-rejected |
| PRE0115 construction exemption (2137) | section | ADDED | pre0115-construction-exemption-accept |
| Proved / conservative prose (2139) | section | ADDED | unprovable-qualifier-fact-rejected |
| Implemented qualifier-oriented checks list (2141-2146) | section | ADDED | proof-nonzero-obligation-rejected, proof-nonnegative-obligation-rejected, proof-currency-axis-compat-rejected, proof-currency-conversion-resolution-accept, proof-unit-compat-rejected, proof-dimension-compat-rejected, proof-temporal-dimension-compat-rejected, proof-declaration-based-fact-accept |
| Interval containment prose (2147) | section | ADDED | proof-cross-unit-interval-containment-accept, proof-counting-unit-mismatch-not-proved-rejected |
| Unprovable obligation prose (2149) | section | ADDED | unprovable-obligation-surfaces-diagnostic-rejected |

## Open Questions / Cross-References

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Open Questions / Implementation Notes (2153-2155) | section | NO-BEHAVIOR | "TBD" placeholder |
| Cross-References heading + table (2159-2170) | section | NO-BEHAVIOR | doc-pointer table, no normative rule |
