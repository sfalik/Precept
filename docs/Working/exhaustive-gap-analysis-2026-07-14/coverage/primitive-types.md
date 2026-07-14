# Coverage inventory — primitive-types

**Region:** `docs/language/primitive-types.md` (whole doc, lines 1-734)

**Rollup:** blocks total 79 · covered 53 · added 9 · no-behavior 17 — cells draft-reused 1021 · added 26 · total 1047 (of which 9 pre-existing draft cells were FIXED and 1 exact-duplicate draft cell was deduped — see Fix Log)

**Draft reuse note:** the draft (`primitive-types.draft.json`, 1022 cells) is dense and maps cleanly onto almost every table/prose block in this doc. All 1022 draft cells matched to a block by their first cited line number (script-verified, zero unmatched). This pass's job was mostly verification + family-completion, per the unit guidance. Two genuine classes of finding came out of verification, both recorded in the Fix Log below:

1. **Nine draft cells asserted `accept` for a proof-obligation the doc explicitly names, on an exercise that does not satisfy the obligation** (unconstrained `sqrt` argument sign for `sqrt`'s non-negativity check, line 614; unconstrained divisor for `/` and `%`'s divisor safety, lines 206/207/239/240/274/275). These are corrected to `reject:any` with a note, and accept-side companion cells were added showing the same construct with the constraint present.
2. **Several real families had zero draft coverage**: integer/decimal overflow, decimal-exact vs. number-approximate arithmetic (the doc's own worked `0.1+0.2` examples), the rounding-function edge-value table (banker's-rounding .5 boundaries), the non-finite-`number`-input table, the "no literal suffix syntax" claim, and one ambiguous `pow` negative-exponent lane question.

## Block inventory

| block (heading / table / example + line) | kind | disposition | cell ids / added count / no-behavior reason |
|---|---|---|---|
| Title + hr (1-4) | section | NO-BEHAVIOR | markdown title/separator |
| Status table (5-12) | table | NO-BEHAVIOR | doc metadata (maturity/impl-state/scope/related links), not a behavior claim |
| Contents / TOC (16-57) | section | NO-BEHAVIOR | navigation only |
| Overview prose — "no implicit coercion, no truthy/falsy, no user-defined types" (61) | section | COVERED | 12 draft cells in block `overview-prose` |
| Six-primitive-types table (63-71) | table | COVERED | 3 draft cells in block `overview-types-table` — summary row per type; full behavior tested in each type's own section below |
| Approximation Stance table + lane-separation prose (74-87) | table | NO-BEHAVIOR | restates per-type exactness stance detailed in each type's own Backing line below. **FLAGGED CONTRADICTION:** this table's `integer` row claims "arbitrary-precision... no overflow" while the integer section's Backing line (197) and operators-table note (203) claim "Overflow is a type error" / "Checked overflow" — see the `integer-backing` row below and stopAndFix |
| `string` declaration example (95-100) | example | COVERED | 1 draft cell in block `string-declaration` |
| `string` operators table + arithmetic/logical/ordering rejection prose (102-112) | table | COVERED | 50 draft cells in block `string-operators` |
| `.length` member table (114-118) | table | COVERED | 5 draft cells in block `string-member-access` |
| `string` Constraints line (120) | section | COVERED | 10 draft cells in block `string-constraints` |
| String interpolation prose (122) | section | COVERED | 39 draft cells in block `string-interpolation` |
| `~string` intro prose + declaration code block (124-134) | example | COVERED | 2 draft cells in block `tilde-string-intro` (1 exact duplicate removed — see Notes) |
| "When to use `~string`" guidance prose (136-140) | section | NO-BEHAVIOR | author-guidance prose, not a compiler behavior claim; the refactor-diagnostic sentence (140) is restated/tested via the enforcement-rules block below |
| Three enforcement rules 1-3 — equality / prefix-suffix / CS-collection-contains (142-158) | section | COVERED | 30 draft cells in block `tilde-enforcement-rules` |
| `~startsWith`/`~endsWith` operator table (160-165) | table | COVERED | 11 draft cells in block `tilde-ci-operators-table` |
| Event arg CI declaration prose (167) | section | COVERED | 3 draft cells in block `tilde-event-arg` |
| Type-unification table + concatenation-is-different callout (169-178) | table | COVERED | 16 draft cells in block `tilde-unification` |
| "String functions unaffected" prose — trim/left/right/mid/toLower/toUpper (179) | section | COVERED | 8 draft cells in block `tilde-string-functions-unaffected` |
| "`choice of ~string` is excluded" prose (181-182) | section | COVERED | 5 draft cells in block `tilde-choice-excluded` |
| Assignment compatibility prose (183-184) | section | COVERED | 6 draft cells in block `tilde-assignment-compat` |
| `integer` declaration example (191-195) | example | COVERED | 1 draft cell in block `integer-declaration` |
| `integer` Backing line — "Overflow is a type error" (197) | section | **ADDED** | `primitive-types/integer-add-overflow-type-error` — flagged as contradicting the Approximation-Stance "no overflow" claim; see stopAndFix |
| `integer` operators table (199-211) | table | COVERED | 17 draft cells in block `integer-operators-table` — **2 draft cells FIXED**: `primitive/op/integer/div`, `primitive/op/integer/mod` (unconstrained-divisor accept → reject; see Fix Log) |
| `integer` Widening / Constraints / "surfaces that produce integer" prose (212-217) | section | COVERED | 3 draft cells in block `integer-widening-constraints-surfaces` |
| `decimal` declaration example (224-228) | example | COVERED | 1 draft cell in block `decimal-declaration` |
| `decimal` Backing line — "0.1 + 0.2 == 0.3 is true" (230) | section | **ADDED** | `primitive-types/decimal-exact-arithmetic-no-drift` |
| `decimal` operators table (232-243) | table | **ADDED** | `primitive-types/decimal-add-overflow-error`, `primitive-types/div-decimal-nonzero-modifier-accept`, `primitive-types/mod-decimal-nonzero-modifier-accept` (plus 16 pre-existing draft cells) — **2 draft cells FIXED**: `primitive/op/decimal/div`, `primitive/op/decimal/mod` (see Fix Log) |
| `decimal` Widening / Constraints / `maxplaces` / business-domain-scalar-role prose (245-251) | section | COVERED | 3 draft cells in block `decimal-widening-constraints` |
| `number` declaration example (259-263) | example | COVERED | 2 draft cells in block `number-declaration` |
| `number` Backing line — "0.1 + 0.2 ≠ 0.3" (265) | section | **ADDED** | `primitive-types/number-ieee754-drift-visible` |
| `number` operators table (267-278) | table | **ADDED** | `primitive-types/div-number-nonzero-modifier-accept`, `primitive-types/mod-number-nonzero-modifier-accept` (plus 17 pre-existing draft cells) — **2 draft cells FIXED**: `primitive/op/number/div`, `primitive/op/number/mod` (see Fix Log) |
| `number` Widening / Constraints / number-lane-only-functions / "not a scalar for business-domain types" prose (280-286) | section | COVERED | 5 draft cells in block `number-widening-constraints-fns` |
| `boolean` declaration example (294-296) | example | COVERED | 2 draft cells in block `boolean-declaration` |
| `boolean` operators table + ordering/arithmetic rejection prose (301-311) | table | COVERED | 39 draft cells in block `boolean-operators-table` |
| `boolean` Constraints + "Role in the language" prose (313-315) | section | COVERED | 12 draft cells in block `boolean-constraints-role` |
| `choice` declaration example (323-326) | example | COVERED | 8 draft cells in block `choice-declaration` |
| `choice` Backing prose + unordered/ordered variants table (329-336) | table | COVERED | 32 draft cells in block `choice-backing-variants-table` |
| `choice` operators table + arithmetic/logical rejection prose (340-346) | table | COVERED | 41 draft cells in block `choice-operators-table` |
| "Ordinal rank is field-local" + cross-field comparison prose (348) | section | COVERED | 11 draft cells in block `choice-ordinal-rank-field-local` |
| "Literal validation" prose (350) | section | COVERED | 38 draft cells in block `choice-literal-validation` |
| `choice` Constraints line (352) | section | COVERED | 12 draft cells in block `choice-constraints` |
| "The three lanes" intro + lanes table (360-368) | table | COVERED | 11 draft cells in block `lane-rules-intro-three-lanes` |
| "Lane rules" numbered list 1-5 (372-376) | section | COVERED | 49 draft cells in block `lane-rules-list` — **1 draft cell FIXED**: `primitive/integer/fn-sqrt-widens-to-number` (unconstrained-widen-then-sqrt accept → reject; see Fix Log) |
| Complete conversion map — implicit + explicit-bridge tables (380-400) | table | COVERED | 10 draft cells in block `lane-conversion-map` |
| "Blocked" conversions table (404-411) | table | COVERED | 19 draft cells in block `lane-blocked-table` |
| Context-by-context matrix (415-421) | table | COVERED | 19 draft cells in block `lane-context-matrix` |
| "Why no comparison exception?" rationale prose (423-425) | section | NO-BEHAVIOR | rationale prose; the behavior itself (`decimal == number` type error) is tested via `lane-blocked-table` |
| "Widening ceiling for business-domain types" prose (429-431) | section | COVERED | 3 draft cells in block `lane-widening-ceiling-bizdomain` |
| Context-Sensitive Literal Resolution — literal-form table (439-443) | table | COVERED | 112 draft cells in block `literal-resolution-table` |
| Context sources numbered list 1-6 (445-452) | section | COVERED | 36 draft cells in block `literal-context-sources` |
| "No literal suffix syntax" prose (454) | section | **ADDED** | `primitive-types/literal-no-suffix-syntax-rejected` |
| "No implicit fallback" + business-domain-co-operand-rule prose (456-458) | section | COVERED | 3 draft cells in block `literal-no-fallback` — the business-domain co-operand rule (458) is a cross-reference; its authoritative test lives in `business-domain-types.md § Scalar Literal Type Resolution`, not duplicated here |
| Type Operator Surface Summary table + `~string`-enforcement callout (464-475) | table | COVERED | 64 draft cells in block `type-operator-summary-table` |
| "No truthy/falsy coercion" + "No mixed-type string concatenation" prose (477-479) | section | COVERED | 3 draft cells in block `type-operator-summary-notes` |
| Case-Insensitive Comparison operator table (487-490) | table | COVERED | 12 draft cells in block `ci-comparison-table` |
| "String-only" + "Ordinal, not culture-aware" + "Same precedence" prose (492-496) | section | NO-BEHAVIOR | the string-only type-error family is covered by `ci-comparison-table`; the Ordinal/`OrdinalIgnoreCase`-vs-culture claim describes .NET comparer selection, not an independently DSL-testable accept/reject behavior beyond the already-covered `~=`/`!~` cells |
| Usage examples code block — guard, keyword-negation-equivalent, conditional expression (502-514) | example | COVERED | 2 draft cells in block `ci-usage-examples` |
| Design rationale numbered list 1-6 (517-524) | section | NO-BEHAVIOR | rationale prose |
| "String Ordering — Out of Scope" intro prose — `<`/`>`/`<=`/`>=` intentional type error (530) | section | COVERED | 8 draft cells in block `string-ordering-oos-prose` |
| Idiomatic substitutes table (534-541) | table | NO-BEHAVIOR | author-guidance table pointing at already-shipped features (`choice ordered`, `startsWith`, etc.); not itself a new testable claim |
| Design rationale numbered list 1-4 (543-548) | section | NO-BEHAVIOR | rationale prose |
| Alternatives considered and rejected (550-554) | section | NO-BEHAVIOR | historical rationale for unshipped alternatives (`between()`, fixed-format types) — nothing exists to test |
| Precedent prose (556-558) | section | NO-BEHAVIOR | external-survey citation, not a behavior claim |
| Tradeoff accepted prose (560-562) | section | NO-BEHAVIOR | rationale prose |
| Status subsection — `TypeKind.String` / `OperationKind.cs` claim (564-566) | section | NO-BEHAVIOR | source-level implementation description restating the already-covered string-ordering type-error behavior |
| Constraint Catalog table (572-585) | table | COVERED | 45 draft cells in block `constraint-catalog-table` |
| Constraint validation rules table (589-597) | table | COVERED | 19 draft cells in block `constraint-validation-rules-table` |
| Numeric functions table — min/max/abs/clamp/pow/sqrt (607-614) | table | **ADDED** | `primitive-types/sqrt-integer-widen-nonnegative-accept`, `primitive-types/sqrt-approximate-bridge-nonnegative-accept`, `primitive-types/pow-decimal-base-negative-exponent` (**AMBIGUOUS**, see stopAndFix) (plus 60 pre-existing draft cells) — **2 draft cells FIXED**: `primitive/fn/sqrt/number`, `primitive/fn/sqrt/approximate-bridge` (unconstrained-sqrt accept → reject; see Fix Log) |
| Rounding functions table — floor/ceil/truncate/round (618-624) | table | COVERED | 36 draft cells in block `builtin-fns-rounding-table` |
| Bridge functions table — approximate/round(places) (628-631) | table | COVERED | 7 draft cells in block `builtin-fns-bridge-table` |
| Edge-value behavior — .5-boundary table + round(places)-precision note + `approximate()`-precision-loss note (637-648) | table | **ADDED** | `primitive-types/floor-half-boundary-values`, `primitive-types/ceil-half-boundary-values`, `primitive-types/truncate-half-boundary-values`, `primitive-types/round-banker-half-boundary-values`, `primitive-types/round-places-exceeds-precision-unchanged`, `primitive-types/approximate-precision-loss-15-17-digits` (plus 3 pre-existing draft cells) |
| Non-finite `number` inputs table — ±∞/NaN into rounding functions + integer-conversion-overflow prose (650-660) | table | **ADDED** | `primitive-types/nonfinite-floor-nan-runtime-fault`, `primitive-types/nonfinite-ceil-nan-runtime-fault`, `primitive-types/nonfinite-truncate-nan-runtime-fault`, `primitive-types/nonfinite-round-noplaces-nan-runtime-fault`, `primitive-types/nonfinite-round-places-nan-runtime-fault`, `primitive-types/nonfinite-floor-positive-infinity-runtime-fault`, `primitive-types/nonfinite-floor-negative-infinity-runtime-fault`, `primitive-types/integer-conversion-overflow-large-number` |
| String functions table — trim/startsWith/endsWith/toLower/toUpper/left/right/mid (664-673) | table | COVERED | 29 draft cells in block `builtin-fns-string-table` |
| "Function lane integrity rule" — closed-vs-not-closed list (677-680) | section | NO-BEHAVIOR | restates facts already tested via the numeric/rounding function tables above (`sqrt` number-lane-only, `decimal` overloads on `abs`/`min`/`max`/`clamp`/`pow`/`round`/`floor`/`ceil`/`truncate`) |
| Open Questions / Implementation Notes — "TBD" placeholder (686) | section | NO-BEHAVIOR | placeholder, no content |
| Teachable Error Messages — Numeric type errors table (696-704) | table | COVERED | 8 draft cells in block `teachable-numeric-errors-table` |
| Teachable Error Messages — String / `~string` errors table (708-713) | table | COVERED | cross-reference restating the `tilde-enforcement-rules` behaviors already tested there; no distinct new claim (0 direct draft cites, all cited from the enforcement-rules block) |
| Teachable Error Messages — Unresolved literal table (719) | table | COVERED | 2 draft cells in block `teachable-unresolved-literal-table` |
| Cross-References table (727-733) | table | NO-BEHAVIOR | navigation only |

## Fix Log — draft cells whose `expected` was corrected

Per the Phase 1 rule ("a draft cell may be wrong; if a draft cell's expected contradicts the doc, fix it and note it"), verification surfaced two families of miscoded proof obligations. In both families the draft cell's *exercise* leaves the safety-relevant field **unconstrained** (no `nonnegative` / `nonzero` modifier, no guard) yet marks `expected: accept`. The doc names these as proof-engine obligations ("Proof engine checks non-negativity" for `sqrt`, line 614; "Divisor safety applies" for `/` and `%`, lines 206/207/239/240/274/275), and the project's prove-or-reject discipline for compile-time impossibilities (division-by-zero is explicitly named in the project's core philosophy) means an unconstrained/unguarded field must be **rejected**, not accepted. Each fix is preserved in the assembled JSON with a `fix_note` field; the pre-fix `expected` value is recorded here too.

| id | draft `expected` | corrected `expected` | why |
|---|---|---|---|
| `primitive/integer/fn-sqrt-widens-to-number` | `accept` | `reject:any` | `sqrt(CountA)` where `CountA` is plain unconstrained `integer` (widened to `number`) — sign unproven |
| `primitive/fn/sqrt/number` | `accept` | `reject:any` | `sqrt(A)` where `A` is plain unconstrained `number` — sign unproven. This is the exact "sqrt of a possibly-negative value must be rejected unless proven non-negative" shape named in the task brief |
| `primitive/fn/sqrt/approximate-bridge` | `accept` | `reject:any` | `sqrt(approximate(Score))` where `Score` is plain unconstrained `decimal` — the bridge doesn't launder the unproven sign |
| `primitive/op/integer/div` | `accept` | `reject:any` | `(A / B) == A` with both `A`, `B` plain unconstrained `integer` — divisor's non-zero-ness unproven |
| `primitive/op/decimal/div` | `accept` | `reject:any` | same shape, `decimal` |
| `primitive/op/number/div` | `accept` | `reject:any` | same shape, `number` |
| `primitive/op/integer/mod` | `accept` | `reject:any` | `(A % B) == 0` with both `A`, `B` plain unconstrained `integer` — divisor's non-zero-ness unproven |
| `primitive/op/decimal/mod` | `accept` | `reject:any` | same shape, `decimal` |
| `primitive/op/number/mod` | `accept` | `reject:any` | same shape, `number` |

For each fixed cell, a companion **accept** cell was added showing the identical construct with the missing constraint present (`nonnegative` for `sqrt`, `nonzero` for `/` and `%`), so the family still has both sides represented: `primitive-types/sqrt-integer-widen-nonnegative-accept`, `primitive-types/sqrt-approximate-bridge-nonnegative-accept`, `primitive-types/div-decimal-nonzero-modifier-accept`, `primitive-types/div-number-nonzero-modifier-accept`, `primitive-types/mod-decimal-nonzero-modifier-accept`, `primitive-types/mod-number-nonzero-modifier-accept`. (The `integer` accept side was already correctly covered pre-fix by `primitive/integer/op-divide` and `primitive/integer/op-modulo`, whose recovered exercises already declare the divisor `nonzero`.)

## Notes

- All 1022 draft cells were matched to exactly one block by their first cited line number, script-verified with zero unmatched cells — this gives high confidence the draft-reuse count above is accurate, not an undercount from citation-parsing gaps. One exact duplicate was found and removed: `string/tilde/declare` (`field Email as ~string`, line 129) appeared twice in the draft with identical exercise/expected/specCite — the assembled JSON keeps a single instance (1021 draft cells survive, not 1022).
- The doc is exceptionally dense in per-family enumeration already (e.g. the numeric-lane literal-resolution family alone carries 112 draft cells across whole/fractional/exponent × decimal/integer/number/`~string`/boolean/choice × 6 context sources). Verification therefore focused on (a) spot-checking a representative sample of each large family for internal consistency against the doc text, and (b) exhaustively checking every block with **zero** draft citations, since those are the blocks most likely to hide a genuine gap. Every zero-citation block above is disposed as either NO-BEHAVIOR (with a stated reason) or ADDED (with new cells) — none were left unaccounted.
- Two doc-internal contradictions were found and are NOT resolved here (Phase 1 curates cells, it does not adjudicate the doc) — see stopAndFix.

## stopAndFix

1. **Integer overflow contradiction.** `docs/language/primitive-types.md:81` (Approximation Stance table) states integer is "Arbitrary-precision whole numbers; no overflow, no rounding." `docs/language/primitive-types.md:197` (integer Backing) states "Overflow is a type error," and the operators table (`docs/language/primitive-types.md:203`) annotates `integer + integer` as "Checked overflow." These are three different claims (unbounded/never-overflows vs. compile-time type error vs. runtime checked-arithmetic exception). The added cell `primitive-types/integer-add-overflow-type-error` picks the "reject" reading (siding with lines 197/203) since that's the more operationally specific claim, but this needs an owner/doc-author ruling on which stance is canonical before Phase 2 can verdict it meaningfully.
2. **`pow` negative-exponent lane scope is ambiguous.** `docs/language/primitive-types.md:613` reads: "`exp` must be non-negative for integer lane" — the explicit "for integer lane" scoping suggests decimal/number bases might legitimately accept negative exponents (reciprocal is closed over decimal/number, unlike integer, where a negative exponent would force a fractional result outside the lane). The pre-existing draft cell `primitive/number/fn-pow-negative-exp` rejects unconditionally, i.e. treats the restriction as lane-independent. The added cell `primitive-types/pow-decimal-base-negative-exponent` mirrors that same (possibly-wrong) reading for family-completeness, but is explicitly flagged: Phase 2 needs a definitive reading of line 613 before verdicting either cell.
3. Two blocks' claims are runtime-value assertions (not compile accept/reject in the strict sense) that don't map cleanly onto the binary `expected` schema: the rounding-function edge-value table (2.5/-2.5/3.5/0.0 → exact outputs) and the decimal-exact vs. number-approximate arithmetic claims (`0.1+0.2`). Cells were added as `accept` (the code compiles) with the exact expected runtime value spelled out in `desc` — Phase 2's runner will need to check the produced value against that `desc`, not just compile-success, for these specific cells.

## Runner note (assembled JSON)

`docs/Working/exhaustive-gap-analysis-2026-07-14/assembled/primitive-types.cells.json` contains all 1048 cells (1022 draft + 26 added). Each cell carries `"source": "draft"` or `"source": "added"` and a `"block"` field matching the block ids used in this table. The 9 fixed cells additionally carry a `"fix_note"` field explaining the correction (their `"expected"` field already reflects the corrected value, not the original draft value — the original value is recorded only in the Fix Log table above).
