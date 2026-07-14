# Coverage inventory — spec-preamble-lexer

Region: `docs/language/precept-language-spec.md` lines 84–812 (§0 Preamble + §1 Lexer).

**Rollup:** blocks total 47 · covered 5 · added 13 · no-behavior 29 — cells draft 1 · added 186 · total 187.

Behavior surface for this unit = tokenization, literal lanes, keyword/identifier reservation, comment/whitespace handling, and lexer diagnostics. §0 (Preamble) is design principles + design contracts; its concrete testable behaviors (proof obligations, graph-analyzer guarantees, type rules) are homed in the §3/§4/§5 units, so §0 blocks are dispositioned NO-BEHAVIOR here (see notes on §0.6). §1 keyword-vocabulary tables list token vocabulary; the observable reservation behavior (a reserved word cannot be an identifier) is enumerated as one family under §1.2, so those tables are NO-BEHAVIOR with the family homed at §1.2.

Only 1 of the 15 draft cells for this doc falls in-region: gid 1457 (`%` modulo, line 512). The other 14 draft cells cite lines 1580–1672 (temporal/quantity/exchangerate — other units) and are intentionally excluded here.

## §0 Preamble (lines 84–306)

| block | kind | disposition | cells / reason |
|---|---|---|---|
| §0 intro "foundational identity" (84) | section | NO-BEHAVIOR | framing prose |
| §0.1 Design Principles (88) | section | NO-BEHAVIOR | 11 language principles / rationale; concrete behaviors homed in type-checker/proof/graph units |
| §0.2 Language Model (114) | section | NO-BEHAVIOR | conceptual model (entity/fields/rules) — no lexer behavior |
| §0.3 Governance, Not Validation (136) | section | NO-BEHAVIOR | positioning / semantics rationale |
| §0.4 Execution Model Properties (154) | section | NO-BEHAVIOR | design-choice rationale (no loops/branches/etc.) |
| §0.5 Graph Analyzer Design Contract (176) | section | NO-BEHAVIOR | contract; testable graph behaviors homed in §4 graph-analyzer unit |
| §0.6 Proof Engine Design Contract + status table (195–258) | section+table | NO-BEHAVIOR | contract + impl-status table names concrete PRE codes (PRE0155/0159/0154/0082/0153/0136) but these are proof-engine diagnostics fully specified in §5; cell-homed in the proof-engine unit (see notes) |
| §0.7 Compile-Time & Runtime Guarantee Contract (262) | section | NO-BEHAVIOR | guarantee-composition rationale |
| §0.8 Authoring Audience (278) | section | NO-BEHAVIOR | audience constraint / review policy |

## §1 Lexer (lines 308–812)

| block | kind | disposition | cells / reason |
|---|---|---|---|
| §1 intro — TokenStream + 64KB ceiling (308–312) | section | COVERED | `lexer/diag/input-too-large` (homed §1.8) |
| §1.1 intro (314) | section | NO-BEHAVIOR | "every token, organized by category" framing |
| Keywords: Declaration (318) | table | ADDED | 1 — `retired-not-reserved/writable` (line 331); reservation of listed words homed §1.2 |
| Keywords: Prepositions (337) | table | NO-BEHAVIOR | token vocabulary; reservation family homed §1.2 |
| Keywords: Control (351) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Actions (360) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Outcomes (376) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Access Modes (384) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Logical Operators (393) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Membership (401) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Quantifiers/Modifiers (408) | table | NO-BEHAVIOR | vocabulary; `no` lookahead disambiguation is parser-level (parser unit); reservation §1.2 |
| Keywords: State Modifiers (417) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Constraints (428) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Types (447) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Temporal Types (465) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Business-Domain Types (478) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Keywords: Literals true/false (490) | table | NO-BEHAVIOR | vocabulary; reservation §1.2 |
| Operators (497) | table | ADDED | 10 — `operator/*` (backarrow, arrow, ~=, ~= non-string, !~, tilde qualifier, tilde non-string, collection-inner tilde) + draft `%` |
| Scan order for operators (519) | section | COVERED | multi-char-before-single exercised by `operator/backarrow-not-lt-minus`, `operator/ci-equals-*` |
| ~startsWith / ~endsWith note (521) | section | ADDED | 2 — `operator/tilde-startswith`, `operator/tilde-wrong-identifier-rejected` |
| Punctuation (523) | table | NO-BEHAVIOR | vocabulary; exercised implicitly by every fragment using `.`/`,`/`(`/`)`/`[`/`]` |
| Literals token table (534) | table | COVERED | string/typed-constant decomposition homed in §1.3 `lexer/string/*`, `lexer/typed/*` |
| Token.Text contract for quoted literals (548) | section | NO-BEHAVIOR | implementation-internal token-field contract; not observable via accept/reject |
| Token.Offset/Length contract (550) | section | NO-BEHAVIOR | implementation-internal token-field contract |
| Identifiers grammar (554–566) | table+grammar | ADDED | 3 — `identifier/leading-underscore-rejected`, `identifier/internal-digit-and-underscore`, `identifier/leading-digit-rejected` (case-sensitivity cells homed §1.2) |
| Structure: Comment/NewLine/EOF (568) | table | COVERED | homed §1.4 `lexer/comment/*`, `lexer/whitespace/*` |
| §1.2 Reserved Keywords (576–610) | section | ADDED | 111 — 101 `reserved-collision/*` (full canonical set) + 8 `case-sensitive-identifier/*` + 2 `v3-removed-not-reserved/*` + 3 `v1-removed-not-reserved/*` |
| §1.3 intro — two quoted forms (612) | section | NO-BEHAVIOR | describes the two literal roles; concrete behavior in sub-blocks |
| §1.3 Numeric literals (616–629) | section+example | ADDED | 19 — `numeric/valid/*` (10), `numeric/invalid/*` (6), `numeric/exponent/*` (3) |
| §1.3 String literals (631–648) | section+example | ADDED | 6 — `string/plain`, `string/interpolation`, `string/escaped-open/close-brace`, `string/empty-interpolation-rejected`, `string/escape-sequences` |
| §1.3 Typed constants (650–660) | section | ADDED | 6 — `typed/plain-date`, `typed/escape-single-quote`, `typed/newline-escape-rejected`, `typed/tab-escape-rejected`, `typed/content-words-not-keywords/*` |
| §1.3 List literals (662–670) | section+example | ADDED | 2 — `list/numbers`, `list/strings` |
| §1.4 Comments and Whitespace (672–685) | section | ADDED | 4 — `comment/standalone`, `comment/inline`, `whitespace/indentation-cosmetic`, `whitespace/multiline-declaration` |
| §1.5 Operator and Punctuation Scanning (687–706) | section | COVERED | scan-priority ladder exercised by `operator/backarrow-not-lt-minus`, `operator/arrow`, `operator/ci-equals-*` |
| §1.6 Dual-Use Token Disambiguation (708–747) | section+tables | ADDED | 11 — `dualuse/set-*`, `dualuse/is-set-presence`, `dualuse/min|max-*`, `dualuse/in-*`, `dualuse/of-*`, `dualuse/in-and-of-mutually-exclusive`. Counting-unit prose (745–747) is business-type semantics, no lexer behavior |
| Mode stack / interpolation nesting (749–774) | section+tables | ADDED | 2 — `modes/nested-literal-in-interpolation`, `modes/nesting-depth-exceeded` |
| §1.8 Lexer Diagnostics (776–793) | section+table | ADDED | 8 — `diag/input-too-large`, `diag/unterminated-string`, `diag/unterminated-typed-constant`, `diag/unterminated-interpolation`, `diag/invalid-character`, `diag/unrecognized-string-escape`, `diag/unrecognized-typed-escape`, `diag/unescaped-brace` |
| Recovery rules (795–809) | section+table | NO-BEHAVIOR | recovery-boundary internals; diagnostic emission covered by §1.8 cells, recovery scanning not observable via accept/reject |

## Notes

- **§0.6 concrete PRE codes deliberately not cell-homed here.** The §0.6 implementation-status table (lines 239–258) states "Implemented — emitting" for PRE0155/PRE0159 (contradictory/unsatisfiable rule), PRE0154 (vacuous rule), PRE0082 (unsatisfiable guard), PRE0153 (tautological guard), PRE0136 (count-bound). These are proof-engine behaviors fully specified in §5 (implementation) and belong to the proof-engine unit's region. Homing them here would double-count. Flagged so a reviewer of the §5 unit confirms they land there.
- **Reserved-keyword collision family** is enumerated exhaustively (101 cells, the full canonical set from lines 583–600) as `field <kw> as number` → reject. This is the observable form of "the word lexes as a keyword, not an Identifier." Expected `reject:any` (a specific parse diagnostic exists but the exact code depends on the parser, out of lexer scope).
- **One draft cell excluded silently: none.** Only gid 1457 was in-region and it is retained. The 14 out-of-region draft cells (gids 9,11,12,18,147,148,149,150,186,187,238,239,903,1020 — all citing lines 1580–1672) are not part of this unit and are left for their owning units.
- **Escape/unterminated cells** are homed in §1.8 (diagnostic table) rather than duplicated in the §1.3 literal sub-blocks; the §1.3 sub-blocks carry the accept-form and parser-reject (empty interpolation) cells.
