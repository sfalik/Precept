# Coverage inventory — spec-parser (§2 Parser)

**Region:** `docs/language/precept-language-spec.md:813-1214`

**Rollup:** blocks total 38 · covered 0 · added 29 · no-behavior 9 — cells draft-reused 0 · added 286 · total 286

**Draft reuse note:** The 15 draft cells in `spec.draft.json` all cite lines outside this region (512, 1580, 1613, 1627, 1667-1672 — the type/temporal/percentage sections). None exercise a §2 parser behavior, so 0 were reused. All 286 cells here are net-new ADDs. This matches the unit guidance ("Expect heavy ADD").

## Block inventory

| block (heading / table / example + line) | kind | disposition | cell block · count / no-behavior reason |
|---|---|---|---|
| §2 Parser intro (813) | section | NO-BEHAVIOR | implementation-internal description (recursive-descent + Pratt, flat manifest) — no guaranteed behavior |
| pipeline diagram `TokenStream → Parser.Parse → ConstructManifest` (817) | example | NO-BEHAVIOR | illustrative data-flow diagram |
| `Parser.Parse` C# signature (823) | example | NO-BEHAVIOR | implementation-internal public API surface |
| "always runs to end-of-source" prose (827) | section | NO-BEHAVIOR | error-recovery posture; testable form lives in §2.6 (recovery cells) |
| §2.1 Expression Precedence + precedence table (829-844) | table | ADDED | `2.1-precedence-table` · 18 |
| Non-associative operators prose (846) | section | ADDED | `2.1-non-associative` · 11 |
| Pratt implementation note (848) | section | NO-BEHAVIOR | implementation-internal (`ParseExpression(int minBp)` mechanics) |
| Structural separators (`->`, `<-`) prose (850) | section | ADDED | `2.1-structural-separators` · 3 |
| Null-denotation heading + table (852-870) | table | ADDED | `2.1-null-denotation` · 23 |
| Left-denotation heading + table (872-885) | table | ADDED | `2.1-left-denotation` · 16 |
| §2.2 Declaration Grammar intro (887-889) | section | NO-BEHAVIOR | dispatch-loop intro prose; behaviors enumerated in the dispatch table below |
| Top-level dispatch table (891-904) | table | ADDED | `2.2-top-level-dispatch` · 9 |
| `field` declaration grammar + prose (906-912) | example | ADDED | `2.2-field-declaration` · 10 |
| `state` declaration grammar (914-920) | example | ADDED | `2.2-state-declaration` · 11 |
| `event` declaration grammar + prose (922-930) | example | ADDED | `2.2-event-declaration` · 8 |
| `rule` declaration grammar + prose (932-938) | example | ADDED | `2.2-rule-declaration` · 6 |
| `in`/`to`/`from` dispatch table + StateTarget grammar + prose (940-960) | table | ADDED | `2.2-preposition-dispatch` · 12 |
| Transition row grammar + ActionStatement/Outcome + prose (962-989) | example | ADDED | `2.2-transition-row` · 26 |
| State/event ensure grammar + prose (991-998) | example | ADDED | `2.2-state-event-ensure` · 7 |
| Stateless event hook grammar + prose (1000-1007) | example | ADDED | `2.2-stateless-event-hook` · 3 |
| State action grammar + prose (1009-1016) | example | ADDED | `2.2-state-action` · 4 |
| Quantifier expression grammar + prose (1018-1028) | example | ADDED | `2.2-quantifier-grammar` · 7 |
| Access mode + omit grammar + composition rules 1-9 / D130-D143 (1030-1087) | example | ADDED | `2.2-access-mode-omit` · 23 |
| §2.3 Type References grammar fence (1088-1117) | example | ADDED | `2.3-typeref-grammar` · 44 (scalars, collections, choice, empty/missing-elem/mismatch) |
| `choice of ~string` blockquote note (1119) | section | ADDED | `2.3-cistring-note` (cistring/choice-element) |
| Type-qualifier `in`/`of`/`to` prose (1121) | section | ADDED | `2.3-typeref-grammar` (qual/in, qual/of, qual/to, qual/in-and-of-both) |
| Value modifiers in inner-type position prose (1123) | section | ADDED | `2.3-inner-value-modifier-compat` · 2 + `2.3-typeref-grammar` coll/inner-* |
| `~string` in ScalarType prose (1125) | section | ADDED | `2.3-cistring-note` (cistring/field, cistring/event-arg, cistring/nonstring-tilde) |
| ChoiceType `(...)` vs `[...]` delimiter note (1127) | section | NO-BEHAVIOR | rationale for delimiter choice; no distinct parse behavior beyond the choice cells |
| `set` disambiguation prose (1129) | section | ADDED | `2.3-set-disambiguation` · 2 |
| §2.4 Field Modifiers — constraint-desugar prose (1131-1140) | section | ADDED | `2.4-field-modifiers` (mod/constraint-desugar) |
| Field modifier table (1142-1158) | table | ADDED | `2.4-field-modifiers` · 17 (rest) |
| §2.5 Interpolation Reassembly (1160-1169) | section | ADDED | `2.5-interpolation-reassembly` · 4 |
| §2.6 Error Recovery intro (1171-1173) | section | NO-BEHAVIOR | mechanism overview prose |
| Missing-node insertion prose (1175-1177) | section | ADDED | `2.6-error-recovery` (recovery/missing-node) |
| Sync-point resync table + continuation prose (1179-1195) | table | ADDED | `2.6-error-recovery` (recovery/runs-to-end, sync-resync, continuation-not-sync) |
| §2.7 Parser Diagnostics table (1197-1211) | table | ADDED | `2.7-parser-diagnostics` · 11 |
| Section-end horizontal rule (1213) | section | NO-BEHAVIOR | markdown separator |

## Cell-block totals

| cell block | count |
|---|---|
| 2.1-precedence-table | 18 |
| 2.1-non-associative | 11 |
| 2.1-structural-separators | 3 |
| 2.1-null-denotation | 23 |
| 2.1-left-denotation | 16 |
| 2.2-top-level-dispatch | 9 |
| 2.2-field-declaration | 10 |
| 2.2-state-declaration | 11 |
| 2.2-event-declaration | 8 |
| 2.2-rule-declaration | 6 |
| 2.2-preposition-dispatch | 12 |
| 2.2-transition-row | 26 |
| 2.2-state-event-ensure | 7 |
| 2.2-stateless-event-hook | 3 |
| 2.2-state-action | 4 |
| 2.2-quantifier-grammar | 7 |
| 2.2-access-mode-omit | 23 |
| 2.3-typeref-grammar | 44 |
| 2.3-cistring-note | 4 |
| 2.3-inner-value-modifier-compat | 2 |
| 2.3-set-disambiguation | 2 |
| 2.4-field-modifiers | 18 |
| 2.5-interpolation-reassembly | 4 |
| 2.6-error-recovery | 4 |
| 2.7-parser-diagnostics | 11 |
| **total** | **286** |

## Notes on enumeration choices

- **Precedence table (§2.1):** operator precedence/associativity is a structural (parse-shape) property, not directly an accept/reject signal, so those cells carry `expected: accept` and isolate one operator/associativity relationship each; Phase 2 verifies the well-formed fragment parses. The reject signal for precedence lives in the non-associative family (chaining → `NonAssociativeComparison`).
- **Whole-family enumeration:** every ScalarType (21), every CollectionType form (12 incl. `by`/direction/`lookup to`), every ActionStatement variant (18 incl. `remove at`, `enqueue by`, `dequeue into by`, `pop into`, `append by`, `insert at`, `put =`), every Outcome form (3), every StateModifier (6), and every field modifier (16) each get their own cell.
- **Misuse/misorder cases** enumerated per construct: missing `as`/`because`/type/identifier, trailing comma, modifier-after-arrow, `initial`-before-args, `when`-after-because, pre-event guard, unclosed paren.
- **Cross-section duplication is intentional:** several §2.7 diagnostic-table cells (e.g. `NonAssociativeComparison`, `ExpectedOutcome`, `EmptyChoice`, `ChoiceMissingElementType`, `ChoiceElementTypeMismatch`, `OmitDoesNotSupportGuard`) restate behaviors also cited from their originating grammar block. They are kept as separate cells because §2.7 is the diagnostic's normative home (code + message), and a Phase-2 gap could exist in one place but not the other. This is not double-counting a single check — the specCite differs.
- **Access-mode composition rules (§2.2 rules 1-9, D130/D131/D132/D143):** these blend parse-time and type-check/proof-time obligations, but all are behavior specified inside this region, so each gets a cell (`RedundantAccessMode`, `ComputedFieldNotWritable`, `EditableOnEventArg`, `OmittedFieldReadInState`, `OmittedFieldSetInTargetState`, `RequiredFieldUnassignedOnEntry`, `MaterializedFieldSelfReference`). Phase 2 will verdict several as gaps (the backstop audit flags `ChoiceMissingElementType`/`ChoiceElementTypeMismatch` as silently-regressed, and direction-modifier validation as missing).
- **Diagnostic identifiers:** expressed as `reject:<DiagnosticName>` using the names in the §2.7 table (numeric PRE codes not resolved in this pass); ambiguous/recovery cases use `reject:any`.
- No draft cells were dropped (none were in-region to begin with).
