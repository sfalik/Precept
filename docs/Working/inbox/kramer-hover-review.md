BLOCKED

G1: `rule` hover is low-cost. `TypedRule` already carries the typed condition plus authored `because` text (stored as `Message`), and `ProofLedger.Obligations` keyed by `ConstraintContext(RuleIdentity)` can drive the status badge.

G2: `ensure` hover is also low-cost. `TypedEnsure` has anchor state/event, typed boolean condition, `Message`, and the proof side can be grouped by `EnsureIdentity`. `ProofLedger.ConstraintInfluence` can cheaply fill the `Protects:` line.

G3: Construct-level hover is mechanically feasible. The manifest/semantic layer preserves `Syntax` or `RowSpan` for fields, states, events, rules, ensures, access declarations, and transition rows, and the existing construct-selection pattern in the language server can be reused for a construct-first resolver.

B1: The design's description-first lead line is not currently feasible for most examples. The compiler preserves authored rationale for `rule`/`ensure` (`because`) and `reject` rows (`RejectReason`), but not for `field`, `state`, `event`, `access`, normal transition rows, or qualifier clauses. In `inventory-item.precept`, those explanations live in comments; comments are lexed but not attached to constructs, so the LS has no durable way to recover them at hover time.

B2: The V2 examples over-claim runtime metadata for today's hover path. `HoverHandler` receives only the current `Compilation` snapshot (tokens, manifest, symbols, semantics, graph, proof, diagnostics). It does not have an executable model, runtime descriptors, or inspect/fire/update projections, so lines that imply concrete runtime metadata (final write maps, ordered runtime mutation summaries, direct fire/update surfaces) must either be compile-time-derived/generalized or moved out of V1.

N1: `field` hover is medium overall. Type/nullability/qualifiers are low (`TypedField.ResolvedType`, `Presence`, `DeclaredQualifiers`). Direct-edit state lists are medium via `SemanticIndex.AccessModes` traversal. `Governed by:` is medium via `ProofLedger.ConstraintInfluence`. Event-driven mutation reach is high if the design wants it prominently.

N2: `state` hover is medium. Incoming/outgoing transitions and reachability are available from `StateGraph.Edges`/`ReachableStates`; writable fields can be derived from `AccessModes`; active-ensure counts can be derived by grouping `EnsuresByState` with proof obligations. `terminal reachable` is derivable, but only indirectly through dead-end analysis rather than a ready-made per-state flag.

N3: `event` hover is medium. Arg types/qualifiers are already on `TypedEvent.Args`. `Can fire from:` is available from `GraphEvent.HandledInStates`. `Typical effects:` requires summarizing actions across `TransitionRows` and `EventHandlers`, which is traversal/formatting work, not a ready projection.

N4: `transition row` hover is medium/high. Guard, action order, outcome, reject reason, source/target reachability, and row-scoped proof obligations all exist today. The expensive part is the prose proof-gap line: unresolved obligations are present, but natural text like `compound-unit + currency arithmetic in shipment cost path` is not precomputed and would need diagnostic-driven formatting or a new summarizer.

N5: `access` hover is medium. `TypedAccessMode` has the raw declaration, and same-write-set/locked-state summaries can be derived by scanning other access declarations. Be careful with guarded access rules: the final state x field write map is not materialized anywhere today.

N6: `reject` hover is medium. `RejectReason` is available and `state unchanged / no mutations commit` matches the language semantics, but `Selected when no earlier row guard matches` requires ordered row analysis for the same `(state,event)` pair (plus wildcard-row handling); there is no existing projection for that.

N7: `qualifier` hover is medium/high. Parsed qualifier spans exist in `QualifiedTypeReference.Qualifiers`, and resolved qualifier meaning exists in `DeclaredQualifierMeta`, so axis/type/check information is available. The hard part is the `applied to X, Y` line: that requires a repo-wide scan for equivalent qualifier templates because there is no qualifier-usage index.

N8: VS Code hover markdown is fine with the current design language: bold headings, blockquotes, code spans, inline emoji, and short bullet-style lines render well. Avoid tables, HTML, and deep nested lists if the design grows; hover width makes those degrade fast.

N9: Cheap bonus: rule/ensure hovers can cheaply add `Referenced fields:` / `Referenced args:` from `ProofLedger.ConstraintInfluence`, and proof-status detail can cheaply include the winning `ProofStrategy` when everything is proved.

N10: One implementation-note correction: the design says `SemanticIndex` has semantic subjects. It doesn't, in practice, for these constructs right now; `TypedRule.SemanticSubjects` and `TypedEnsure.SemanticSubjects` are currently empty, so the implementation should plan around `ConstraintInfluenceEntry` instead.