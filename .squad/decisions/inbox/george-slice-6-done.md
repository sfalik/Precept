### 2026-05-07: Slice 6 Complete
**By:** George (for Soup Nazi)
**Commit:** fe358ef
**Structural checks implemented:** IsSet/IsNotSet expression resolution, choice domain validation (empty + duplicate), computed-field cycle detection (DFS), forward-reference belt-and-suspenders on ComputedDeps.
**DiagnosticCodes used:** IsSetOnNonOptional (49), EmptyChoice (46), DuplicateChoiceValue (45), CircularComputedField (40), DefaultForwardReference (54). No new codes — all pre-existing.
**Reachability algorithm:** N/A — reachability is GraphAnalyzer's responsibility per §14. Slice 6 per §13 is IsSet/IsNotSet + computed deps + choice validation + forward-ref belt-and-suspenders.
**Cycle detection:** Three-color DFS on ComputedDeps adjacency graph. O(n) construction + O(n) traversal. Currently a no-op because ComputedDeps is empty until computed expression resolution is wired.
**Choice validation:** During PopulateFields, when type is ChoiceTypeReference: empty domain → EmptyChoice, duplicate values → DuplicateChoiceValue. Uses HashSet for O(n) duplicate detection.
**IsSet/IsNotSet:** ResolvePostfixOp validates operand is an optional field or optional arg. Non-optional → IsSetOnNonOptional. Non-field/arg operand → IsSetOnNonOptional.
**Forward-ref belt-and-suspenders:** Post-hoc validation that ComputedDeps entries don't reference fields at or after their own declaration index. Redundant with D8 enforcement in ResolveIdentifier.
**Edge cases for Soup Nazi:** Stateless precepts (no states) pass through — no structural checks depend on state presence. Single-state precepts pass. Precepts with no transitions pass. ComputedDeps being empty causes cycle detection and forward-ref check to be no-ops (correct — no computed expressions resolved yet). PostfixOp test updated from stub assertion to IsSetOnNonOptional assertion.
**Test delta:** 3177 passing (up from 3170 baseline). 19 pre-existing TypeCheckerTransitionTests failures from Slice 5 revert — not introduced by this slice.
