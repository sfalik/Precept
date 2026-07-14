# Coverage — Temporal Type System (`docs/language/temporal-type-system.md`)

**Rollup:** blocks total **192** / covered-or-added **142** / no-behavior **50** (every block below carries an explicit disposition — zero unaccounted). Cells: draft **615** / added **42** / total **657**. Every draft cell survived (none removed or contradicted the doc on inspection); 42 net-new cells fill enumerated gaps, folded into the COVERED count above wherever a block's own row is marked "ADDED" or "COVERED + ADDED". Blocks are headings/tables/examples in reading order; "disposition" is per the Phase-1 taxonomy (COVERED / ADDED / NO-BEHAVIOR). Where a block's behavior is exercised by cells that live under a different, more specific block id in the JSON (e.g., a Locked-Decision rationale whose actual test cells sit under the type's own section), the table says so explicitly — no cell is double-counted in the total.

Full per-cell data (including `block` tags) is in `docs/Working/exhaustive-gap-analysis-2026-07-14/assembled/temporal.cells.json`.

## Front matter / Status / Contents

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Title, Status table (lines 1–15) | section | NO-BEHAVIOR | Metadata (doc maturity, implementation state, grounding links) — no testable claim. |
| Contents / TOC (17–116) | section | NO-BEHAVIOR | Navigation only. |

## Summary (117–246)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Opening paragraph — 8 types, NodaTime, `.inZone(tz)`, typed constants (119) | section | NO-BEHAVIOR | Prose framing; constituent claims covered per-type below. |
| "Temporal quantity construction" bullets — static/interpolated/combined quantities (121–124) | section | COVERED | `period-construction`, `duration-construction` (static/combined/interpolated literal cells for both types). |
| Closed type grammar for interpolated shapes; "unit holes not supported in compound forms" (126) | section | COVERED | `temporal/duration/literal-unit-hole-in-compound-rejected`, `temporal/period/literal/unit-hole-in-compound-reject`. |
| Integer-magnitude requirement paragraph (128) | section | COVERED | `temporal/duration/literal-fractional-magnitude-rejected`, `temporal/period/literal/fractional-days-reject`, `.../fractional-months-reject`, `temporal/date/op/plus-fractional-days-rejected`, `temporal/instant/op/plus-quantity-fractional`. |
| "Formatted temporal constants" bullet — 6 formatted-type literal examples (130–131) | section | COVERED | `date-literal`, `time-literal`, `instant-literal`, `datetime-literal`, `zoneddatetime-literal`, `timezone-literal`. |
| **Canonical temporal parser** — subsystem prose (133–136) | section | NO-BEHAVIOR | Implementation-internal (`src/Precept/Language/Time/`), no new testable claim. |
| F-LANG-TEMP-01/02 classification table (137–143) | table | COVERED* | Rows for `duration`/`period` expected-type covered throughout per-type sections. *Flag:* the `(none)` row's "Period path" / "Duration path" cells for unqualified calendar/time units appear to conflict with the later, more specific "Type resolution rules — context-dependent" section (1165–1220), which states plain-context `'3 days'` is a compile error with no silent Period/Duration default. Existing draft cells (`temporal/date/literal/no-context-rejected`, `temporal/duration/literal-ambiguous-no-context`) follow the later, more detailed section. Flagging for Phase 2 rather than resolving unilaterally — this is a doc-internal tension, not a draft-cell error. |
| F-LANG-TEMP-05 mixed-unit paragraph (145) | section | COVERED | `temporal/period/literal/mixed-units-no-target-type-reject` (TEMP005 no-target-type case); mixed-unit-with-target-type cases covered via `duration/literal-mixed-units-accepted`, `period/literal/combined-mixed-date-time`. |
| F-LANG-TEMP-06/07 timezone error recovery paragraph (147) | section | COVERED | `timezone-literal` block (unrecognized/legacy-abbrev/windows-name cells). |
| Surface-form/backing-type/parse-shape/serialization table (149–158) | table | COVERED | One literal-form cell per type across `date-literal`/`time-literal`/`datetime-literal`/`instant-literal`/`zoneddatetime-literal`/`timezone-literal`/`duration-construction`/`period-construction`. |
| Timezone mediation bullet list — `.inZone(tz)` chain examples (160–165) | list | COVERED | `inzone-mediation` block. |
| Type hierarchy diagram (167–183) | example | COVERED | Strict-hierarchy skip-level rejections in `instant-not-supported` (`accessor/*-direct`), `inzone-mediation` (`skip-level-instant-rejected`, `skip-level-year`), and no-timezone-on-date/time/datetime via `date/accessor/inZone-rejected`. |
| "What changed in v5/v4/v3/v2" changelog bullets (185–226) | section | NO-BEHAVIOR | Historical changelog prose; each superseded/introduced behavior is tested at its live location (typed-constant door, `.inZone` dot accessor, `period`/`duration` split, etc.) — not a distinct block-level claim. |
| NodaTime-alignment-directive prose + Precept/NodaTime mapping table (207–219) | section | NO-BEHAVIOR | Philosophy framing. |
| Design-principles bullets (#1/#2/#8/#12) + governing question (221–228) | section | NO-BEHAVIOR | Philosophy grounding; restates behaviors tested elsewhere. |
| "Temporal types as the gateway beyond primitives" (230–232) | section | NO-BEHAVIOR | Forward-framing prose. |
| "The NodaTime alignment directive" numbered list (234–245) | section | NO-BEHAVIOR | Directive restated; no new testable claim beyond per-type sections. |

## Approximation Stance (249–267)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Stance table — Exact for 7 types, "admits calendar-relative ambiguity" for `period` (253–262) | table | COVERED | Exactness is the default (no special cell needed — it's the absence of approximation-diagnostics, tested implicitly by every `accept` cell). `period`'s calendar ambiguity / rejected period-as-duration coercion is exercised by `temporal/period/op/duration-plus-period-reject`, `temporal/duration/op-plus-period-rejected`, `.../op-minus-period-rejected` (cites "PRECEPT0007" by name in the doc; existing cells use generic `reject:any` — Phase 2 should confirm the emitted code matches PRE0007/its current successor). |
| "Mixing periods and durations is a category error" paragraph (264) | section | COVERED | Same cells as above. |
| "Timezone mediation is explicit" paragraph (266) | section | COVERED | `inzone-mediation` (no ambient/implicit zone; `.inZone(tz)` always explicit). |

## Motivation (270–381)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| "The temporal gap" corpus-count table (274–286) | table | NO-BEHAVIOR | Corpus statistics, not a compiler behavior claim. |
| "Before and After" worked examples ×4 (288–373) | example | NO-BEHAVIOR (illustrative) | Each constituent claim (date+period arithmetic, instant/duration SLA, multi-timezone `.inZone` chain, `date`/`period` loan term) is already covered by the relevant type-section cells (`date-operators-table`, `instant-operators-table`, `inzone-mediation`, `period-operators-table`); the worked examples themselves add no behavior beyond those. |
| "What happens if we don't build this" bullets (375–381) | section | NO-BEHAVIOR | Consequence narrative. |

## NodaTime as Backing Library (385–412)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| "Why NodaTime" rationale numbered list + Decision format (391–411) | section | NO-BEHAVIOR | Pure rationale/precedent/tradeoff — no testable claim distinct from the per-type sections. |

## Proposed Types (415–1034)

### `date` (417–479)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Declaration examples (423–429) | example | COVERED | `date-literal`, `date-constraints`. |
| Single-quoted literal paragraph (431) | section | COVERED | `date-literal`. |
| Operators table (435–446) | table | COVERED | `date-operators-table`. |
| "Not supported" table (448–453) | table | COVERED | `date-operators-table` (rejection cells: `plus-date-rejected`, `plus-integer-rejected`, `plus-decimal-rejected`, `plus-duration-rejected`). |
| Accessors table (455–463) | table | COVERED | `date-accessors-table`. |
| Constraints paragraph (464) | section | COVERED | `date-constraints`. |
| Serialization sentence (466) | section | NO-BEHAVIOR | Wire-format description, not a compile/runtime pass/fail claim. |
| Teachable error messages table (468–477) | table | COVERED | Same underlying type errors tested in `date-operators-table`/`date-literal` — message wording is a presentation concern layered on already-tested rejections. |
| Collection inner types (`set`/`queue`/`stack` of `date`) | *(cross-referenced from § Collection inner types, 1726–1745)* | ADDED | `temporal/date/collection-set-of-date`, `collection-queue-of-date`, `collection-stack-of-date`, `collection-set-of-date-min-max` — gap: draft had zero collection cells for `date` despite the doc's table listing it. |

### `time` (481–541)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Declaration examples (487–492) | example | COVERED | `time-literal`, `time-constraints`. |
| Single-quoted literal paragraph, seconds-optional (494) | section | COVERED | `time-literal` (`literal-omit-seconds`). |
| Operators table (498–508) | table | COVERED | `time-operators-table`. |
| Note on `time ± period` provable time-only requirement (510) | section | COVERED | `time-operators-table` (`op-plus-period-of-time-field`, `op-plus-period-unconstrained-reject`). |
| "Not supported" table (512–518) | table | COVERED | `time-operators-table` (`op-plus-time-reject`, `op-plus-integer-reject`, `op-plus-days-literal-reject`, `op-plus-months/years-literal-reject`, `op-plus-period-unconstrained-reject`). |
| Accessors table (520–526) | table | COVERED | `time-accessors-table`. |
| Constraints / Serialization (528–530) | section | COVERED / NO-BEHAVIOR | Constraints → `time-constraints`; serialization sentence is wire-format, no-behavior. |
| Teachable error messages table (532–539) | table | COVERED | Same underlying rejections as above; message-wording layer. |
| Collection inner types (`set`/`queue`/`stack` of `time`) | *(cross-ref, 1726–1745)* | COVERED | `collection-inner-types` (`temporal/time/collection-*`). |

### `instant` (543–653)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Declaration examples (549–554) | example | COVERED | `instant-literal`. |
| Single-quoted literal paragraph, trailing-Z requirement (556) | section | COVERED | `instant-literal` (`missing-z-suffix`). |
| `now()` subsection — signature, dot-vs-function rationale, UTC-always, determinism note, usage patterns, type-rules table, locked decision (558–599) | section+table | COVERED | `instant-now` (assign/now-with-arg-rejected/minus-instant/plus-duration/inzone-date/skip-level-year). Determinism-note prose itself is NO-BEHAVIOR (non-diagnosable runtime relativity), folded into this row since it carries no separate testable claim. |
| Operators table (601–613) | table | COVERED | `instant-operators-table`. |
| "Not supported" table (615–620) | table | COVERED | `instant-not-supported` (accessor rejections) + `instant-operators-table` (`plus-bare-integer`, `plus-period-fieldref` rejected). |
| "Note on days/weeks in instant context" paragraph (622) | section | COVERED | `instant-operators-table` (`plus-quantity-days/weeks`), `temporal-quantity-type-resolution-tables` (`duration/context-instant-plus-days`). |
| Overflow note paragraph (624) | section | COVERED | `temporal/duration/cross-instant-plus-duration-overflow-accepted` (compiler accepts; overflow is an accepted runtime edge case per the doc, not statically caught). |
| ".inZone(tz) sole mediation path" prose + completion-teaching note (626–628) | section | NO-BEHAVIOR | IDE/tooling behavior (completion-list content), not compiler pass/fail. |
| `.inZone(tz)` navigation table (630–636) | table | COVERED | `inzone-mediation`. |
| Accessors/Constraints/Serialization (638–642) | section | COVERED / NO-BEHAVIOR | Accessors ("`.inZone(tz)` only") → `instant-not-supported`; constraints → **gap, see ADDED below**; serialization → no-behavior. |
| Teachable error messages table (644–652) | table | COVERED | Same underlying rejections tested above. |
| Constraint-rejection parity (`min`/`max`/`nonnegative`/`nonzero` on `instant`) | *(implied by 640, by family with `date`'s explicit table at 464)* | ADDED | `instant-section`: `temporal/instant/constraint-min-rejected`, `max-rejected`, `nonnegative-rejected`, `nonzero-rejected` — draft only covered `maxplaces`/`minlength`/`maxlength`. |
| `.inZone(tz)` wrong-argument-type (passing a non-`timezone` value) | *(implied by 160–165, 626–638; arity already covered)* | ADDED | `inzone-mediation`: `temporal/instant/zone/inzone-wrong-arg-type-rejected`. |
| Collection inner types (`set`/`queue`/`stack` of `instant`) | *(cross-ref, 1726–1745)* | ADDED | `temporal/instant/collection-set-of-instant`, `collection-queue-of-instant`, `collection-stack-of-instant`, `collection-set-of-instant-min-max` — same gap pattern as `date`. |

### `duration` (656–743)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| "What v2 changed" prose (662) | section | NO-BEHAVIOR | Historical note. |
| Construction via postfix units + combined durations examples (664–679) | example | COVERED | `duration-construction`. |
| "No constructor literal form" paragraph + ISO-notation table (681–687) | table | COVERED | `duration-construction` (`literal-constructor-function-rejected`, `literal-iso8601-notation-rejected`, `literal-bare-postfix-rejected`); sub-second `PT0.5S` non-expressibility → `literal-unknown-unit-milliseconds-rejected`. |
| Paren-postfix form (`(SlaHours) hours`) eliminated | *(implied by Decision #17's "what was eliminated (v5)" list, 1506–1508)* | ADDED | `duration-section`: `temporal/duration/literal-paren-postfix-rejected` — draft covered bare-postfix and constructor-function elimination but not paren-postfix. |
| Operators table (689–708, incl. division-by-zero prevention sub-section 700–707) | table | COVERED | `duration-operators-table` + `duration-division-by-zero` (17 cells covering literal-zero, unproven-numeric/-duration divisor, all proof sources, compound-expression conservatism). |
| "Not supported" table (710–716) | table | COVERED | `duration-operators-table` (`op-mult-duration-rejected`, `op-mult-decimal-rejected`, `op-div-decimal-rejected`, `op-minus-period-rejected`, `op-plus-integer-rejected`). |
| Accessors table (718–725) | table | COVERED | `duration-accessors-table`. |
| Field-type example + corpus count (727–734) | example | COVERED | `duration-constraints` (`constraint-default-quantity`). |
| Constraints paragraph (736) | section | COVERED | `duration-constraints`. |
| "`nonzero` on duration" paragraph (738) | section | COVERED | `duration-constraints` (`constraint-nonzero`), `duration-division-by-zero` (`op-div-duration-field-proof-nonzero`). |
| "Implementation note" — type-aware desugar to `Duration.Zero` (740) | section | NO-BEHAVIOR | Implementation-internal (desugar mechanics); outward behavior already covered by `constraint-nonnegative`/`constraint-nonzero`. |
| Serialization sentence (742) | section | NO-BEHAVIOR | Wire-format. |
| Collection inner types (`set`/`queue`/`stack` of `duration`) | *(cross-ref, 1726–1745)* | COVERED | `collection-inner-types` (`temporal/duration/collection-*`). |

### `period` (746–867)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| "What it makes explicit" / "Why this type exists (v2→v2.1)" prose (748–756) | section | NO-BEHAVIOR | Design-history rationale. |
| Construction via postfix units + combined periods examples (758–775) | example | COVERED | `period-construction`. |
| "No constructor literal form" + ISO-notation table (777–786) | table | ADDED | Draft had zero coverage of period's own constructor-elimination / bare-postfix / composite-juxtaposition family (only `duration`'s analogous cells existed). `period-section`: `literal-constructor-call-form-rejected`, `literal-bare-postfix-rejected`, `literal-paren-postfix-rejected`, `literal-iso8601-notation-rejected`, `literal-composite-juxtaposition-no-plus-rejected`. |
| "Decision: full NodaTime Period" paragraph (787) | section | COVERED | `period-construction` (`combined-mixed-date-time`), `period-accessors-introspection` (`.hours`/`.minutes`/`.seconds`). |
| Operators table (789–797, incl. degenerate-comparison warning) | table | COVERED | `period-operators-table` (`eq-degenerate-literal-warning`, `neq-degenerate-literal-warning`, `unary-negate`, `eq-period`, `neq-period`). |
| "Not supported" table (799–803) | table | COVERED | `period-operators-table` (`lt/gt/lte/gte-reject`, `mul-integer-reject`, `plus/minus-duration-reject`). |
| Accessors table (805–817) | table | COVERED | `period-accessors-introspection`. |
| Introspection table — `.hasDateComponent`/`.hasTimeComponent` (819–826) | table | COVERED | `period-accessors-introspection` (`hasDateComponent`, `hasTimeComponent`); `duration-accessors-table` carries the rejected counterpart on `duration`. |
| Field example + corpus count (828–838) | example | COVERED | `period-constraints` (`default-single-unit`, `default-combined`). |
| Constraints paragraph (840) | section | COVERED | `period-constraints` (`optional`, `nonnegative`, `nonzero`, `min-reject`, `max-reject`). |
| `in`/`of` qualification system — table + prose (842–854) | table | COVERED + ADDED | `period-qualifier-in-of` (`of-date`, `of-time`, `unconstrained`, `in-days`, `in-months`, `both-in-and-of-reject`, closure/mismatch cells). **Gap filled:** the `in 'days'`/`in 'months'` basis-pinning was only declaration-tested, not enforcement-tested — added `in-days-matching-unit-accept`, `in-days-mismatched-unit-rejected`, `in-months-mismatched-unit-rejected`. |
| Serialization sentence (856) | section | NO-BEHAVIOR | Wire-format. |
| Teachable error messages table (858–866) | table | COVERED | Same underlying rejections as operator/comparison tables above. |
| Collection inner types (`set`/`queue`/`stack` of `period`, incl. `.min`/`.max` type-error) | *(cross-ref, 1726–1745)* | COVERED | `collection-inner-types` (`temporal/period/collection/*`). |

### `timezone` (869–905)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Declaration examples (875–880) | example | COVERED | `timezone-constraints`. |
| Single-quoted literal paragraph (882) | section | COVERED | `timezone-literal` (`literal-valid-iana`). |
| "Operators: `==`,`!=` only. No ordering, no arithmetic." (884) | section | COVERED | `timezone-operators-eq-neq-only` (54 cells: eq/neq accepted; lt/gt/lte/gte/+/-/*// all rejected against same-type literal, same-type fieldref, quantity-literal, `duration`, `period`). |
| "Accessors: None." (886) | section | COVERED | `timezone-accessors-none` (`.id`, `.name`, `.offset`, `.year` all rejected; `.inZone` as receiver rejected). |
| Validation paragraph — compile-time IANA DB check, runtime event-arg validation (888–890) | section | COVERED | `timezone-literal` + `timezone-constraints` (`literal-event-arg-typed`). |
| Constraints paragraph (892) | section | COVERED + ADDED | `timezone-constraints` (`optional`, `default`, `min-rejected`, `max-rejected` already present). **Gap filled:** `nonnegative`/`nonzero`/`maxplaces`/`minlength`/`maxlength` rejection had no cells — added all 5 (`timezone-section`). |
| Double-quoted timezone literal (wrong door) | *(implied by 1541 two-door table; parity with the other 7 types, all of which had this cell)* | ADDED | `timezone-section`: `temporal/timezone/literal-double-quoted-rejected`. |
| Serialization sentence (894) | section | NO-BEHAVIOR | Wire-format. |
| Teachable error messages table — legacy abbrev, Windows name, unrecognized (896–904) | table | COVERED | `timezone-literal` (`literal-legacy-abbrev-est`, `literal-legacy-abbrev-group`, `literal-windows-name`, `literal-unrecognized`). |
| Collection inner types (`set`/`queue`/`stack` of `timezone`) | *(cross-ref, 1726–1745)* | COVERED | `collection-inner-types` (`temporal/timezone/collection-*`). |

### `zoneddatetime` (907–977)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Declaration examples (913–918) | example | COVERED | `zoneddatetime-constraints` (`field-decl-*`). |
| Single-quoted literal paragraph (920) | section | COVERED | `zoneddatetime-literal` (`literal-bracket-form-valid`). |
| Construction via `.inZone(tz)` examples + "no standalone construction function" (922–932) | example | COVERED | `zoneddatetime-construction` (`construct-from-instant`, `construct-from-datetime`, `construct-from-now`, `constructor-call-form` rejected). |
| Operators table (934–947) | table | COVERED | `zoneddatetime-operators-table`. |
| "Not supported" table (949–953) | table | COVERED | `zoneddatetime-operators-table` (`lessthan/greaterthan/lessequal/greaterequal-reject`, `plus-period-field-reject`, `plus-integer-literal-reject`). |
| Accessors table (955–964) | table | COVERED | `zoneddatetime-accessors-table`. |
| Constraints / Serialization (966–968) | section | COVERED + ADDED / NO-BEHAVIOR | Constraints → `zoneddatetime-constraints` (`field-constraint-min/max` already present). **Gap filled:** `nonnegative`/`nonzero`/`maxplaces`/`minlength`/`maxlength` rejection had no cells — added all 5 (`zoneddatetime-section`). Serialization sentence is wire-format, no-behavior. |
| Teachable error messages table (970–975) | table | COVERED | Same underlying rejections as operator table. |
| Collection inner types (`set`/`queue`/`stack` of `zoneddatetime`, incl. `.min`/`.max` type-error) | *(cross-ref, 1726–1745)* | COVERED | `collection-inner-types` (`temporal/zoneddatetime/*-of-zoneddatetime-*`). |

### `datetime` (979–1034)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Declaration examples (985–990) | example | COVERED | `datetime-constraints`. |
| Single-quoted literal paragraph (992) | section | COVERED | `datetime-literal`. |
| Operators table (994–1002) | table | COVERED | `datetime-operators-table`. |
| "Note: `datetime ± period` accepts all component categories" (1004) | section | COVERED | `datetime-operators-table` (`plus-period-unconstrained-fieldref`, `plus-quoted-hours-resolves-period`). |
| "Not supported" table (1006–1009) | table | COVERED | `datetime-operators-table` (`plus-bare-integer-rejected`, `plus-duration-fieldref-rejected`). |
| `.inZone(tz)` reverse-mediation table (1011–1016) | table | COVERED | `inzone-mediation` (`nav/inzone-produces-zoneddatetime`, `nav/inzone-instant-reverse`, `nav/skip-level-instant-rejected`). |
| Accessors table (1018–1025) | table | COVERED | `datetime-accessors-table`. |
| Constraints paragraph (1027) | section | COVERED + ADDED | `datetime-constraints` (`min/max/nonnegative/maxplaces/minlength/maxlength-rejected`, `of-date-qualifier-rejected`, `optional/default/optional-plus-default-accept` all already present). **Gap filled:** `nonzero-rejected` was missing — added (`datetime-section`). |
| Serialization sentence (1029) | section | NO-BEHAVIOR | Wire-format. |
| "Decision: datetime included" subsection (1031–1033) | section | NO-BEHAVIOR | Scope-inclusion rationale, not a new testable claim. |
| Collection inner types (`set`/`queue`/`stack` of `datetime`) | *(cross-ref, 1726–1745)* | ADDED | `temporal/datetime/collection-set-of-datetime`, `collection-queue-of-datetime`, `collection-stack-of-datetime`, `collection-set-of-datetime-min-max` — same gap pattern as `date`/`instant`. |

## Timezone Mediation — `.inZone(tz)` Dot Accessor (1037–1123)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Single mediation operation table (1043–1047) | table | COVERED | `inzone-mediation`. |
| Dot-chain navigation table (1048–1056) | table | COVERED | `inzone-mediation` (`instant/zone/inzone-then-*`). |
| "Why one operation, not three functions" — D8/D11/D13 rationale (1058–1066) | section | NO-BEHAVIOR | Design-history rationale; behavior already tested. |
| "Strict hierarchy — no skip-level accessors" prose + diagram + valid/invalid table (1068–1089) | section+table | COVERED | `instant-not-supported` (`accessor/*-direct` rejections), `inzone-mediation` (`skip-level-instant-rejected`), `zoneddatetime-accessors-table` (`.date` direct one-step exception). |
| "DST ambiguity resolution (locked)" — gap/overlap `LenientResolver` behavior (1091–1096) | section | NO-BEHAVIOR | Runtime numeric-output behavior (which wall-clock instant a DST-ambiguous local time resolves to), not a compile diagnostic — outside this cell schema's `accept`/`reject:<code>` shape. Flagging rather than fabricating a diagnostic-shaped cell for a non-diagnostic runtime policy. |
| "Composability with existing operations" bullets + multi-timezone example (1098–1123) | section+example | COVERED | `inzone-mediation` + `instant-operators-table`/`date-operators-table` (the chained example composes already-tested primitive operations). |

## Temporal Quantity Construction — Typed Constants (1127–1222)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| "Three forms" intro + static/interpolated/combined examples (1129–1157) | example | COVERED | `period-construction`, `duration-construction`. |
| Unit-names-not-keywords paragraph (1159) | section | NO-BEHAVIOR | Lexer/grammar-internal (keyword-set design), not itself a pass/fail claim distinct from the literal cells. |
| Integer-requirement paragraph (1161) | section | COVERED | (duplicate of block at line 128 — same cells.) |
| "Left-associative `+` subtlety" paragraph (1163) | section | COVERED | `date-operators-table` (`plus-inline-months` chained twice would exercise this; the mechanism is exercised by `plus-compound-mixed-date-units` for the parenthesized-inside-quote form). No dedicated cell for the *sequential-vs-compound* truncation distinction exists in the draft — judged a runtime-numeric-outcome distinction (both forms compile) rather than a compile-time accept/reject fact, so left NO-BEHAVIOR for this cell schema beyond the two forms' independent `accept` status, which are already covered. |
| Type-resolution rules — days/weeks table (1169–1180) | table | COVERED | `temporal-quantity-type-resolution-tables` (`duration/context-*`), `date-operators-table`, `instant-operators-table`. |
| Type-resolution rules — months/years always-period table (1182–1194) | table | COVERED | `temporal-quantity-type-resolution-tables` (`context-instant-plus-months-rejected`), `instant-operators-table` (`plus-quantity-months/years` rejected). |
| Type-resolution rules — hours/minutes/seconds table (1195–1206) | table | COVERED | `temporal-quantity-type-resolution-tables` (`context-time-plus-hours-bridges`, `context-datetime-plus-hours-resolves-to-period-not-duration`), `date-operators-table` (`plus-hours-cross-domain-rejected`). |
| "Design rationale" quote (1208) | section | NO-BEHAVIOR | Rationale. |
| Teachable error messages table (1210–1221) | table | COVERED | Same underlying rejections tested throughout this section and per-type sections. |

## Literal Mechanism Architecture (1224–1274)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Two-door table + "Zero constructors exist" paragraph (1228–1235) | table | COVERED + ADDED | Door 1 (string) is the default type-checker behavior, not temporal-specific. Door 2 → all six formatted-literal blocks above. "Zero constructors" → `locked-decision-4` + `literal-mechanism-zero-constructors` + `period-section` (constructor-rejected cells for all types that lacked them, added above). |
| "Context-born resolution" prose + inhabitants table (1237–1252) | table | COVERED | Same literal cells across all 6 formatted types + quantity ambiguous-context cells. |
| "Why two doors are sufficient" bullets (1254–1262) | section | NO-BEHAVIOR | Design-completeness argument, forward-looking, not itself testable. |
| "Temporal types prove the framework" bullets (1264–1273) | section | NO-BEHAVIOR | Retrospective framing claim. |

## Semantic Rules (1277–1378)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Type-interaction matrix — calendar domain table (1281–1290) | table | COVERED | `semantic-rules-type-interaction-matrix` + per-type operator tables. |
| Type-interaction matrix — timeline domain table (1292–1305) | table | COVERED | `duration-operators-table`, `time-operators-table`, `zoneddatetime-operators-table`. |
| Type-interaction matrix — composition table + note on Decision #27 reversal (1307–1314) | table | COVERED | `datetime-construction` (`date-plus-time`, `time-plus-date-commutative`); reversal → `datetime-operators-table` (`plus-duration-fieldref-rejected`). |
| Comparison rules table (1316–1329) | table | COVERED | Per-type `cmp`/comparison blocks (`date-comparison`, `time-comparison`, `instant-operators-table`, `duration-operators-table`, `period-operators-table` lt/gt-reject, `timezone-operators-eq-neq-only`, `datetime-operators-table`, `zoneddatetime-operators-table`). |
| "Cross-type arithmetic: what's NOT allowed" table (1331–1350) | table | COVERED | Every row maps to an existing rejection cell in the relevant type's operators table (verified row-by-row: `date±duration`, `instant±period`, `duration±period`, `zoneddatetime±period`, `date±integer`, `instant±integer`, `period<period`, `zoneddatetime<zoneddatetime`, `period*integer`, all five same-type `+`-of-two-points rejections, `instant.year`/`instant.date`, `duration*duration`, `duration*decimal`/`duration/decimal`, `date-time`). |
| "Field-type-aware diagnostics" table (1352–1360) | table | COVERED | Message-quality layer on already-tested rejections (`date-operators-table`/`instant-operators-table`/`datetime-operators-table`); no new pass/fail claim. |
| Optional-and-default-behavior table (1362–1377) | table | COVERED | `date-constraints`, `time-constraints`, `instant-constraints`, `duration-constraints`, `period-constraints`, `timezone-constraints`, `zoneddatetime-constraints`, `datetime-constraints` (`optional`/`default`/`optional-plus-default` cells for each type). |

## Locked Design Decisions 1–28 (1381–1639)

Each decision is a WHY/alternatives/precedent/tradeoff rationale block. None introduces a testable claim that isn't already exercised at its concrete location in the Proposed Types / Semantic Rules sections, **except** the three noted, which fed net-new cells.

| Decision | Disposition | Notes |
|---|---|---|
| #1 NodaTime as backing library (1383–1389) | NO-BEHAVIOR | Rationale only. |
| #2 Day granularity for `date` (1390–1395) | NO-BEHAVIOR | Behavior tested via `date-literal`/`date-operators-table`. |
| #3 ISO 8601 sole format (1397–1402) | COVERED | `date-literal`, `instant-literal`, `datetime-literal`, `time-literal`, `zoneddatetime-literal` (all reject non-ISO forms). |
| #4 Single-quoted typed constant delimiter, incl. `char`-exclusion (1404–1412) | COVERED + ADDED | Delimiter behavior → all `*-literal` blocks. `char`-type-permanent-exclusion sub-claim → NO-BEHAVIOR (absence of a keyword; nothing to positively exercise; would reduce to a generic unknown-type-identifier check, not temporal-specific). Constructor-elimination → `locked-decision-4` (added cells). |
| #5 No timezone on `date`/`time`/`datetime` (1413–1418) | COVERED | `date/accessor/inZone-rejected` and the type sections' own no-timezone framing. |
| #6 `instant` restricted semantics (1420–1425) | COVERED | `instant-not-supported`. |
| #7 `timezone` first-class type (1427–1432) | COVERED | `timezone-literal`. |
| #8 `.inZone(tz)` dot accessor (1434–1439) | COVERED | `inzone-mediation`. |
| #9 Determinism relative to runtime environment (1441–1446) | NO-BEHAVIOR | Philosophical/non-diagnosable (same as `now()`'s determinism note above). |
| #10 `date + integer` type error (1448–1453) | COVERED | `date-operators-table` (`plus-integer-rejected`). |
| #11 `date - date` returns `period` (1455–1460) | COVERED | `semantic-rules-type-interaction-matrix` (`date-minus-date-produces-period`). |
| #12 `period` is a surface type (1462–1467) | COVERED | `period-construction`, `period-constraints`. |
| #13 `period` full NodaTime Period (1469–1474) | COVERED | `period-accessors-introspection`, `period-construction` (mixed date+time). |
| #14 No ordering on `period` (1476–1481) | COVERED | `period-operators-table` (`lt/gt/lte/gte-reject`). |
| #15 Structural equality on `period` (1483–1488) | COVERED | `period-operators-table` (`eq-degenerate-literal-warning`). |
| #16 Sub-day duration bridging (1490–1495) | COVERED | `time-operators-table` (`op-plus-duration-field`), `temporal-quantity-type-resolution-tables` (`context-time-plus-hours-bridges`). |
| #17 Typed constant quantities sole mechanism (1497–1521) | COVERED + ADDED | `period-construction`/`duration-construction` plus `period-section`/`duration-section` (bare-postfix, paren-postfix, constructor-function, ISO-notation, composite-juxtaposition all now rejected on both types — filled the period-side gap). |
| #18 Single-quoted typed constants (1523–1566) | COVERED | All `*-literal` blocks; tooling-implications sub-bullets (IntelliSense/hover) → NO-BEHAVIOR (IDE feature, not compiler pass/fail). |
| #19 Bare English naming (1567–1572) | NO-BEHAVIOR | Naming-convention rationale. |
| #20 Dot-vs-function rule (1574–1579) | NO-BEHAVIOR | Design principle; instantiated by `inzone-mediation` and accessor blocks. |
| #21 `inZone` sole mediation operation (1581–1586) | COVERED | `inzone-mediation`. |
| #22 Strict type hierarchy (1588–1593) | COVERED | `instant-not-supported`, `inzone-mediation` (`skip-level-*`). |
| #23 `pin` eliminated (1595–1600) | COVERED | `inzone-mediation` (`nav/inzone-instant-reverse`). |
| #24 Instant restricted semantics (duplicate of #6) (1602–1607) | COVERED | Same cells as #6. |
| #25 `date + time → datetime` sole construction path (1609–1614) | COVERED | `datetime-construction`. |
| #26 Period component constraints `of`/`in` (1616–1624) | COVERED + ADDED | `period-qualifier-in-of` + the two `in`-basis-mismatch cells added above. |
| #27 No `datetime + duration` (1626–1631) | COVERED | `datetime-operators-table` (`plus-duration-fieldref-rejected`, `minus-duration-fieldref-rejected`). |
| #28 Integer-only temporal quantities (1633–1639) | COVERED | `duration-construction`/`period-construction` (`literal-fractional-magnitude-rejected`, `fractional-days-reject`, `fractional-months-reject`). |

## Double-Quote Alternative Analysis (1643–1674)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| "The compiler CAN resolve it in ~99% of cases" (1647–1654) | section | NO-BEHAVIOR | Feasibility argument for a rejected alternative — not the shipped behavior. |
| "Why it was rejected" — 3 arguments (1656–1664) | section | NO-BEHAVIOR | Rationale. The resulting shipped behavior (single-quote required, double-quote rejected) is tested via every type's `literal-double-quoted-rejected`/`double-quoted-string`/`rejects-double-quote-delimiter` cell — already counted under each type's `*-literal` block above. |
| "The choice-type precedent" (1666–1670) | section | NO-BEHAVIOR | Comparative-design argument. |
| "Conclusion" (1672–1674) | section | NO-BEHAVIOR | Summary of the above. |

## Design Challenges — Resolution (1678–1694)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Challenge 1 — `date + number` (1680–1682) | COVERED | `date-operators-table` (`plus-number-field-rejected`, `plus-decimal-rejected`). |
| Challenge 2 — `date + 2.5` fractional (1684–1686) | COVERED | `date-operators-table` (`plus-fractional-days-rejected`); `period-construction` (`fractional-days-reject`). |
| Challenge 3 — `optional + default` (1688–1690) | COVERED | `date-constraints`/etc. (`optional-plus-default` cells across types). |
| Challenge 4 — `date - date` (1692–1694) | COVERED | `semantic-rules-type-interaction-matrix` (`date-minus-date-produces-period`). |

## Dependencies and Related Issues (1698–1711)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Issue-relationship table (1700–1710) | table | NO-BEHAVIOR | Project/issue-tracker cross-references — not compiler behavior. |

## Explicit Exclusions / Out of Scope (1714–1745)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Exclusions table — `OffsetDateTime`, `AnnualDate`, `YearMonth`, `DateInterval`/`daterange`, fiscal calendars, leap seconds, parameterized types (1716–1725) | table | NO-BEHAVIOR | Absence claims about types/features that don't exist in the grammar — there is no positive DSL fragment to exercise; a `field X as offsetdatetime` failure would just be a generic unknown-type-identifier error, not a temporal-specific behavior. |
| Collection inner types — `queue`/`stack` prose (1728–1730) | section | COVERED | `collection-inner-types` (all 8 types' queue/stack cells). |
| `set of <T>` table + `.min`/`.max` rule (1732–1745) | table | COVERED + ADDED | 5 of 8 types already had full `set of` coverage (`time`, `duration`, `period`, `timezone`, `zoneddatetime`); **gap filled** for `date`, `instant`, `datetime` (12 added cells: `set`/`queue`/`stack`-of plus `.min`/`.max`-accept for each, since these three carry natural ordering). |

## Implementation Scope (1749–1828)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Parser/Tokenizer bullets (1751–1760) | section | NO-BEHAVIOR | Implementation-internal (token types, keyword lists); the outward-visible parse results are tested via every `*-literal` block. |
| Type Checker bullets, incl. `#118` file-placement note (1762–1776) | section | COVERED (outward) / NO-BEHAVIOR (internal) | File-placement/dispatch-mechanism prose → no-behavior. The listed rules (period/duration split, postfix resolution, cross-domain unit rejection, ordering/scaling rejection, sub-day bridging) → already covered by the cells cited throughout this document; no new claim. |
| Expression Evaluator bullets (1778–1793) | section | NO-BEHAVIOR | Implementation-internal (which NodaTime API each operator desugars to). |
| Runtime Engine bullets (1795–1799) | section | NO-BEHAVIOR | Implementation-internal. |
| TextMate Grammar bullets (1801–1807) | section | NO-BEHAVIOR | Editor syntax-highlighting, not compiler behavior. |
| Language Server bullets (1809–1815) | section | NO-BEHAVIOR | IDE feature surface, not compiler pass/fail. |
| MCP Tools bullets (1817–1821) | section | NO-BEHAVIOR | Tooling surface, out of this doc's compiler-behavior scope. |
| Samples, Documentation, Tests bullets (1823–1827) | section | NO-BEHAVIOR | Project housekeeping. |

## Forward Design: Temporal Types as the Gateway Beyond Primitives (1831–1920)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Entire section — "Why this matters now" through "Scope boundary" (1837–1920), incl. all sub-tables (UOM context, natural extension domains, unit-resolution scopes, three design levels, standards landscape) | section | NO-BEHAVIOR | Explicitly scoped as "a forward design note, not a commitment" (1920) about *future, unbuilt* domains (currency, UOM, entity-scoped units) — no temporal-type-system behavior to test; nothing here describes shipped or intended-and-locked temporal surface. |

## Open Questions / Implementation Notes (1924–1926)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| "_TBD_" placeholder (1926) | section | NO-BEHAVIOR | Empty — no content to derive cells from. |

## Cross-References (1930–1938)

| Block | Kind | Disposition | Notes |
|---|---|---|---|
| Cross-reference table | table | NO-BEHAVIOR | Navigation links to other docs. |

---

## Draft-cell disposition

All 615 draft cells were spot-checked against the doc section they claim to test (concentrated sampling across every type namespace, plus a full read of every `timezone`, `period`, `datetime`, `instant` cell's `expected` value). None were found to contradict the doc — none removed, none `expected`-corrected. Every draft cell is retained with `"source":"draft"` and a `block` tag in the assembled JSON.

## Added-cell summary (42 cells)

| Family | Count | Reason |
|---|---|---|
| `set`/`queue`/`stack` of `date`/`instant`/`datetime` (+ `.min`/`.max` accept) | 12 | Doc's collection-inner-types table (1732–1745) lists all 8 temporal types as valid inner types with 5 of them (incl. these 3) carrying natural ordering; draft had zero collection cells for these 3 types despite full coverage for the other 5. |
| Zero-constructor-form rejected: `date`/`time`/`datetime`/`timezone`/`period` | 5 | Decision #4/#18 ("zero constructors exist... no `date(2026-01-15)`") was only cell-tested for `instant`, `zoneddatetime`, and `duration`. |
| Period quantity-elimination family: constructor-call, bare-postfix, paren-postfix, ISO-notation, composite-juxtaposition | 4 | Decision #17's "what was eliminated" list applies identically to `period` and `duration`; draft only tested it for `duration`. |
| Duration paren-postfix-rejected | 1 | Same Decision #17 family gap on the `duration` side (bare-postfix/constructor/ISO were covered; paren-postfix was not). |
| Timezone double-quoted-literal-rejected | 1 | All 7 other temporal types had this family-parity cell; `timezone` did not. |
| Constraint-rejection family parity: `instant` (min/max/nonnegative/nonzero), `datetime` (nonzero), `zoneddatetime` (nonnegative/nonzero/maxplaces/minlength/maxlength), `timezone` (nonnegative/nonzero/maxplaces/minlength/maxlength) | 15 | `date`, `time`, `duration`, `period` all had a fully enumerated non-applicable-constraint rejection family; these 4 types had partial coverage only. |
| `period in 'days'`/`in 'months'` basis-enforcement (matching-unit accept + mismatched-unit reject ×2) | 3 | The `in` qualifier's declaration was tested but its enforcement (rejecting a value/comparison outside the pinned unit basis) was not. |
| `.inZone(tz)` wrong-argument-type rejected | 1 | Arity misuse (`missing-arg`/`extra-arg`) was tested; wrong-*type* argument (e.g., passing a `date` where a `timezone` is expected) was not. |

## Stop-and-fix

None. Every block in the doc received an explicit disposition; the one substantive open item (the `(none)`-row apparent tension in the F-LANG-TEMP-01/02 table vs. the later context-dependent-resolution section, noted under Summary above) is flagged for Phase 2 attention rather than left unaccounted.
