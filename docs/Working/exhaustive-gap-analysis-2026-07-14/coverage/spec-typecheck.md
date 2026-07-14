# Coverage — spec-typecheck (`docs/language/precept-language-spec.md` §3, lines 1215–1846)

**Rollup:** blocks total 75 · covered 3 · added 50 · no-behavior 22 — cells draft(in-region) 14 · added 419 · total 433.

Region = §3 Name Binding and Type Checking. Behavior surface = name resolution, type inference/widening, context-sensitive literal/typed-constant resolution, operator × operand-type matrices (scalar / temporal / business-domain / unary / contains / member-access), the built-in function catalog, and the §3.8 semantic-check tables (type compatibility, ~string enforcement, quantifier, modifier applicability + value, actions, access modes, computed fields, choice, list literals, transitions, stateless/stateful). Heavy ADD as predicted.

Draft cells: 14 draft cells cite lines in this region (gids 9, 11, 12, 18, 147, 148, 149, 150, 186, 187, 238, 239, 903, 1020) — all kept. One draft cell (gid 1457, `percentage/operator-percent-is-modulo/binary-percent-token`) cites line 512, **outside** this region — omitted here; it belongs to the lexer/parse unit that owns line 512.

Diagnostic-name convention: where the spec names a `DiagnosticName`, the expected uses `reject:<Name>`; where it says only "type error", `reject:TypeMismatch`; where a rejection is specified but the exact code is under-determined by the doc (e.g. mismatched-currency `money + money`), `reject:any`. Draft cells retain their original `PRE####` form (PRE0033 = InvalidModifierForType, PRE0018 = TypeMismatch); Phase 2 maps names↔codes.

## Block inventory

| block (heading / table / example + line) | kind | disposition | cell ids / added count / reason |
|---|---|---|---|
| §3 intro prose (1215) | section | no-behavior | pipeline/architecture narrative, no compile behavior |
| pipeline diagram code fence (1219) | example | no-behavior | impl-internal stage flow |
| `Bind`/`Check` C# signatures (1226) | example | no-behavior | impl-internal public API |
| §3.1 Processing Model — two passes (1231) | section | no-behavior | impl-internal (declaration/reference/checking passes) |
| §3.1 declaration-order behavior (1242) | prose | added | 2 → `scope/forward-field-ref-rule`, `scope/transition-ref-later-state` (block 3.5-scope) |
| §3.2 heading + widening code fence + bullets (1244) | section | added | part of 3.2-widen (int→dec, int→num implicit; decimal↛number; no narrowing) |
| §3.2 widening-contexts table (1260) | table | added | 12 (block 3.2-widen): assign/binary/funcarg/default/comparison × widen-ok + decimal→number-error + narrow-errors |
| §3.3 heading + uniform no-context rule (1270) | section | added | no-context → diagnostic+ErrorType cells in 3.3-numeric-literal / 3.3-typed-constant |
| §3.3 numeric literals table (1280) | table | added | 10 (block 3.3-numeric-literal): whole/fractional/exponent × integer/decimal/number/no-context |
| §3.3 typed constants prose (1288) | prose | added | `typedconst/no-context-error` (block 3.3-typed-constant) |
| §3.3 content-validation table (1298) | table | added | 31 (block 3.3-typed-constant): 15 types × {valid content accept, invalid content reject} + no-context |
| §3.4 name resolution table (1320) | table | added | 10 (block 3.4-name-resolution): Duplicate{Field,State,Event,Arg}, Undeclared{Field,State,Event}, MultipleInitialStates, NoInitialState, UnqualifiedEventArgReference |
| §3.5 heading + global scope prose (1332) | section | no-behavior | restates §3.1 order-independence (covered there) |
| §3.5 expression scope table (1342) | table | added | 3.5-scope: default forward/self-ref errors, computed any-field, constraint-modifier forward/mutual ref |
| §3.5 scope rationale prose (1354) | prose | no-behavior | evaluation-model motivation |
| §3.5 omit-field-read prose (1356) | prose | added | `scope/omit-field-read` → OmittedFieldReadInState |
| §3.5 quantifier binding-var table (1362) | table | added | `scope/binding-shadows-field`, `binding-reserved-keyword`, `binding-ci-inheritance` |
| §3.5 event arg access prose (1370) | prose | added | `scope/event-arg-dotted-access`, `event-arg-out-of-scope`, + `name/unqualified-event-arg` |
| §3.6 heading (1380) | section | no-behavior | intro |
| §3.6 core scalar operators table (1386) | table | added | 44 across blocks 3.6-binop-scalar(14)/equality(10)/ci-compare(9)/ordering(7)/logical(4) |
| §3.6 common numeric prose (1402) | prose | added | 3 (block 3.6-common-numeric): int-op-dec→dec, int-op-num→num, decimal-op-number error |
| §3.6 temporal operators table (1406) | table | added | 19 (block 3.6-temporal-ops): every left×op×right row + UnqualifiedPeriodArithmetic + duration×decimal error |
| §3.6 temporal comparison prose (1425) | prose | added | 8 (block 3.6-temporal-compare): date/time/instant/duration/datetime ordering, period/timezone/zdt ordering error, cross-type error |
| §3.6 business-domain operators table (1429) | table | added | 18 (block 3.6-business-ops): money/quantity/price/exchangerate rows + same/diff-qualifier + number-scalar errors |
| §3.6 business comparison prose (1446) | prose | added | 7 (block 3.6-business-compare): money/quantity/price ordering, exchangerate/currency ordering error, cross-unit count → PRE0137 |
| §3.6 affine-units prose (1448) | prose | no-behavior | normalization scope rationale; behavior lives in business-domain-types normalization |
| §3.6 unary operators table (1452) | table | added | 12 (block 3.6-unary): not/negate × boolean/numeric/duration/period/money/quantity/price + string/boolean negate errors |
| §3.6 contains table (1464) | table | added | 11 (block 3.6-contains): each collection kind + log-by key/value + non-collection error |
| §3.6 contains CI rules prose (1478) | prose | added | `contains/cs-collection-ci-value`, `contains/ci-collection-string-value` |
| §3.6 is set / is not set table (1482) | table | added | 2 (block 3.6-is-set): optional ok, non-optional error |
| §3.6 conditional prose (1487) | prose | added | `cond/compatible-same`, `cond/widening-branch`, `cond/incompatible-error` |
| §3.6 ~string unification table (1493) | table | added | `cond/ci-plus-ci`, `cond/ci-plus-string`, `cond/string-plus-string` |
| §3.6 concatenation-is-different note (1499) | prose | no-behavior | clarification; concat behavior covered in 3.6-binop-scalar |
| §3.6 member-access collection table (1505) | table | added | 16 (block 3.6-member-access): count/min/max/peek/peekby/countof/first/last/at/length + invalid-member + for |
| §3.6 lookup `for` note (1535) | prose | added | `member/lookup-for` |
| §3.6 temporal accessors prose (1537) | prose | added | 11 (block 3.6-member-temporal): date/time/instant/duration/period/zdt/datetime accessors + instant.year error |
| §3.6 business accessors prose (1539) | prose | added | 8 (block 3.6-member-business): money/quantity/price/exchangerate/unitofmeasure accessors |
| §3.6 other-member row (1541) | table | added | `member/invalid-member` → InvalidMemberAccess |
| §3.6 function calls pointer (1543) | prose | no-behavior | cross-ref to §3.7 |
| §3.6 parenthesized (1547) | prose | no-behavior | transparent pass-through, no error surface |
| §3.6 string interpolation (1551) | prose | added | `interp/scalar-in-string`, `interp/collection-in-string-error` |
| §3.6 typed-constant interpolation (1555) | prose | added | `interp/typed-constant-interp` |
| §3.7 heading + closed-catalog prose (1559) | section | no-behavior | no-user-defined-fns design fact (enforced via UndeclaredFunction, covered in fn-validation) |
| §3.7 function catalog table (1563) | table | added | 43 (block 3.7-functions): min/max/abs/clamp/floor/ceil/truncate/round/approximate/pow/sqrt/trim/startsWith/endsWith/~startsWith/~endsWith/toLower/toUpper/left/right/mid/now + overload & error cases |
| §3.7 CI-functions catalog note (1599) | prose | no-behavior | impl FunctionKind/ExpressionForms detail |
| §3.7 ~string arg-compat note (1601) | prose | covered | `fn/trim-ci-string` |
| §3.7 primitive-numeric shorthand note (1603) | prose | no-behavior | terminology |
| §3.7 lane bridge functions prose (1605) | prose | covered | `fn/approximate-decimal`, `fn/round-places-decimal`, `fn/floor-decimal`, `fn/ceil-decimal`, `fn/truncate-decimal` |
| §3.7 function validation checks table (1609) | table | added | 5 (block 3.7-fn-validation): UndeclaredFunction, arity (few/many), TypeMismatch, FunctionArgConstraintViolation |
| §3.8 heading (1616) | section | no-behavior | intro |
| §3.8 type compatibility table (1620) | table | added | 11 (block 3.8-type-compat): assignment/guard/rule/ensure/message/binary/unordered-choice/conditional/default/collection-element/numeric-literal |
| §3.8 operator-result-typing prose (1634) | prose | no-behavior | impl catalog-derivation description |
| §3.8 ~string enforcement table (1640) | table | added | 5 core (block 3.8-cistring-enforcement): ==, !=, contains, startsWith, endsWith |
| §3.8 ~string enforcement prose (1648–1650) | prose | added | `cienf/literal-lhs-position`, `cienf/assignment-not-fired` |
| §3.8 quantifier predicate table (1654) | table | added | 4 (block 3.8-quantifier-predicate): not-boolean, non-collection target, reserved keyword, CI-in-predicate |
| §3.8 modifier applicability table (1665) | table | added | 38 (block 3.8-modifier-applicability): each modifier × applicable-accept + wrong-type-reject (incl. draft gids 9/11/12/18/147/148/149/150/1020) |
| §3.8 per-element consultation prose (1679) | prose | added | `mod/per-element-inner-type` |
| §3.8 mincount-1 / notempty-inner prose (1681) | prose | added | `mod/per-element-notempty-inner` (mincount static-discharge of `.min/.max/.peek` access obligations is a proof-stage behavior — belongs to the proof unit; noted) |
| §3.8 notempty+optional prose (1683) | prose | added | `mod/notempty-optional-exclusive` → ConflictingModifiers C120 |
| §3.8 modifier value validation table (1687) | table | added | 10 (block 3.8-modifier-value): min>max, minlength>maxlength, mincount>maxcount, negative len/count/places, maxplaces-not-int, duplicate, redundant(warning), conflicting |
| §3.8 action statement table (1700) | table | added | 23 (block 3.8-action-validation): set/add/remove/remove-at/enqueue/enqueue-by/dequeue/push/pop/clear/append/append-by/insert/put + wrong-kind + unguarded-mutation |
| §3.8 action type-errors prose (1717) | prose | covered | `act/add-non-set-error`, `act/push-non-stack-error` |
| §3.8 access mode table (1721) | table | added | 8 (block 3.8-access-mode): undeclared field/state, computed-editable×2, editable-on-arg, conflicting, redundant unguarded/guarded |
| §3.8 computed field table (1734) | table | added | 5 (block 3.8-computed-field): self-ref, transitive cycle, type mismatch, with-default, write-target |
| §3.8 choice type table (1744) | table | added | 9 (block 3.8-choice-validation): missing-element, empty, duplicate, wrong-literal, non-choice-assigned, literal-not-in-set, arg-outside-set, element-mismatch, rank-conflict |
| §3.8 choice v1 notes (1756) | prose | added | `choice/negative-literals-ok`, `choice/nested-in-collection-ok` |
| §3.8 list literal table (1760) | table | added | 3 (block 3.8-list-literal): element mismatch, outside-default, empty-default ok |
| §3.8 transition outcome table (1768) | table | added | 2 (block 3.8-transition-outcome): undeclared target state, reject-message-not-string |
| §3.8 stateless/stateful prose (1773) | prose | added | 3 (block 3.8-stateless-stateful): handler+states → PRE0092, construction-row exempt, stateless hooks ok |
| §3.9 ErrorType propagation prose + rules (1783) | section | no-behavior | impl-internal error recovery (resilient contract) |
| §3.9 IsMissing handling table (1797) | table | no-behavior | impl-internal parser-error recovery |
| §3.9 one-diagnostic-per-root-cause prose (1805) | prose | no-behavior | impl-internal cascade suppression (diagnostic UX, not a prevention guarantee) |
| §3.10 canonical sources prose (1809) | section | no-behavior | pointer to DiagnosticCode.cs / Diagnostics.cs |
| §3.10 diagnostic groups table (1821) | table | no-behavior | ordinal/stage catalog reference |
| §3.10 design notes (1840) | prose | no-behavior | impl notes (choice parse-stage codes, code-66 reassignment) |

## Omitted draft cells

| gid | id | reason |
|---|---|---|
| 1457 | `percentage/operator-percent-is-modulo/binary-percent-token` | specCite `…:512` is outside region 1215–1846 (percentage/`%` token; owned by a lexer/parse unit) |

## Notes on draft-cell fidelity

All 14 in-region draft cells' `expected` values agree with the doc after re-check:
- gids 11/12/18 (date positive/nonzero/notempty → PRE0033) match §3.8 modifier applicability (1669–1671): those modifiers don't apply to `date`.
- gids 147/148/149/150 (instant nonnegative/nonzero/min/max → PRE0033) match "non-magnitude temporals excluded" (1668) and "temporals excluded from min/max" (1672).
- gid 9 (date editable → accept) matches editable applying to "any non-computed field type" (1667).
- gid 1020 (exchangerate nonnegative → accept) matches nonnegative applicability list (1668, includes exchangerate).
- gids 186/187 (instant + string / + boolean → PRE0018) match binary-operator-type-error (1627) / no temporal+string|boolean row.
- gids 238/239 (`.inZone` string/integer arg → PRE0018) match arg-type-mismatch (1613); `.inZone(tz)` expects a timezone.
- gid 903 (quantity `round` with no places → reject:any) matches: only `round(value)` on decimal|number and `round(value, places)` overloads exist; no `round(quantity)` no-places overload (1580–1583).

None required correction.
