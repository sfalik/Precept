# Coverage report — Phase 1 (block → cell accounting)

Status: **Phase 1 complete.** Every section / table / example block in each truth doc below is dispositioned as COVERED (≥1 draft cell already exercises it), ADDED (a gap this pass filled with net-new cells), or NO-BEHAVIOR (doc content that states no testable runtime/compile-time behavior — pure prose, rationale, metadata, implementation-internal notes). Nine per-doc-region agents produced the per-unit inventories linked below; this report is the merged rollup.

Merged cell corpus: **`assembled/all-cells.json`** — 4,592 cells, ids globally unique (0 collisions). Per-unit cell files remain in `assembled/<unit>.cells.json`.

## Rollup

| Unit (→ inventory) | Truth doc | Line range | Blocks total | Covered | Added | No-behavior | Cells draft | Cells added | Cells total |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|
| [spec-preamble-lexer](coverage/spec-preamble-lexer.md) | precept-language-spec.md (§0 Preamble + §1 Lexer) | 84–812 | 47 | 5 | 13 | 29 | 1 | 186 | 187 |
| [spec-parser](coverage/spec-parser.md) | precept-language-spec.md (§2 Parser) | 813–1214 | 38 | 0 | 29 | 9 | 0 | 286 | 286 |
| [spec-typecheck](coverage/spec-typecheck.md) | precept-language-spec.md (§3 Name Binding & Type Checking) | 1215–1846 | 75 | 3 | 50 | 22 | 14 | 419 | 433 |
| [spec-semantics-proof](coverage/spec-semantics-proof.md) | precept-language-spec.md (§3A Semantics + §4 Graph + §5 Proof) | 1847–2169 | 52 | 0 | 24 | 28 | 0 | 51 | 51 |
| [primitive-types](coverage/primitive-types.md) | primitive-types.md (whole doc) | 1–734 | 79 | 53 | 9 | 17 | 1,021 | 26 | 1,047 |
| [temporal](coverage/temporal.md) | temporal-type-system.md (whole doc) | 1–end | 192 | 142 † | — † | 50 | 615 | 42 | 657 |
| [business-domain](coverage/business-domain.md) | business-domain-types.md (whole doc) | 1–2091 | 153 | 97 | 14 ‡ | 42 | 829 | 23 | 852 |
| [collection](coverage/collection.md) | collection-types.md (whole doc) | 1–1556 | 140 | 98 | 11 | 31 | 933 | 27 | 960 |
| [proof-engine-prevention](coverage/proof-engine-prevention.md) | proof-engine.md (prevention behaviors) | whole doc | 112 | 44 | 4 | 64 | 0 | 119 | 119 |
| **Total** | 5 language docs + proof-engine | — | **888** | — | — | — | **3,413** | **1,179** | **4,592** |

† Temporal reports blocks as "covered-or-added 142 / no-behavior 50"; its inventory does not split the 142 into separate covered vs. added columns (every block carries an explicit disposition in the per-block table, and net-new ADDED cells are folded into the block's own row). The 142 figure is the combined covered+added block count.

‡ Business-domain's 14 is its "COVERED + ADDED" category — blocks that already had draft coverage but carried a partial gap that this pass filled. Its inventory reports COVERED 97 / COVERED+ADDED 14 / NO-BEHAVIOR 42.

## Completeness

**100% of blocks are accounted in every unit.** For each unit, `covered + added + no-behavior == blocks total`:

| Unit | covered+added+no-behavior | blocks total | accounted |
|---|---:|---:|:--:|
| spec-preamble-lexer | 5+13+29 = 47 | 47 | ✅ |
| spec-parser | 0+29+9 = 38 | 38 | ✅ |
| spec-typecheck | 3+50+22 = 75 | 75 | ✅ |
| spec-semantics-proof | 0+24+28 = 52 | 52 | ✅ |
| primitive-types | 53+9+17 = 79 | 79 | ✅ |
| temporal | 142+50 = 192 | 192 | ✅ |
| business-domain | 97+14+42 = 153 | 153 | ✅ |
| collection | 98+11+31 = 140 | 140 | ✅ |
| proof-engine-prevention | 44+4+64 = 112 | 112 | ✅ |

Cell-count reconciliation is also exact: the per-unit cells-total column sums to 4,592, matching the merged `assembled/all-cells.json` array length, with 3,413 `source:"draft"` + 1,179 `source:"added"`. All 4,592 cell ids are globally unique (no collisions across units).

### Open stop-and-fix items (carried to Phase 2 — owner/author ruling needed)

Block accounting is complete, but three items surfaced during verification are **doc-internal contradictions or under-determined doc text** where a cell was still authored (with the more operationally-specific reading chosen and recorded) but cannot be trustworthily *verdicted* until an owner/author resolves the source doc. These are flagged, not silently resolved:

1. **primitive-types — integer overflow contradiction.** `primitive-types.md:81` (Approximation Stance) says integer is "arbitrary-precision… no overflow"; `:197` (Backing) says "Overflow is a type error"; `:203` (operators table) says "Checked overflow." Three incompatible claims. Cell `primitive-types/integer-add-overflow-type-error` picks the reject reading; needs a canonical-stance ruling before Phase 2 can verdict it.
2. **primitive-types — `pow` negative-exponent lane scope ambiguous.** `primitive-types.md:613` ("`exp` must be non-negative for integer lane") leaves open whether decimal/number bases may accept negative exponents. Draft cell `primitive/number/fn-pow-negative-exp` and added cell `primitive-types/pow-decimal-base-negative-exponent` both reject unconditionally (family-completeness reading); needs a definitive reading of line 613.
   - (Related but *resolved*, not open: 9 primitive-types draft cells that asserted `accept` for unconstrained-divisor `/`/`%` and unconstrained-sign `sqrt` were **corrected** to `reject` per prove-or-reject, each carrying a `fix_note`. These are fixed, not open.)
3. **business-domain — stale Implementation Scope vs. retired D10.** `business-domain-types.md:1989` (Type-checker changes) describes implicit ISO-4217-derived `maxplaces` and automatic half-even rounding, which contradicts the locked D10 retirement (`:1760-1768`, explicit-only `maxplaces`) and the Approximation Stance (`:174`, no implicit rounding). Reads as pre-D10-retirement language never updated. Cells follow the locked D10 text; the Implementation Scope bullet should be corrected at the source. A secondary source-doc fix is also flagged: `business-domain-types.md:1576`'s example `field Score as quantity max 100` should carry a qualifier (`in 'points'`) to satisfy the Bounds-qualification rule (PRE0133) it otherwise violates.

Temporal's inventory explicitly reports **Stop-and-fix: None**; the six other units report none. So the open set is confined to the two units above.

## Honest limit

This is the **strongest available structural check against the docs, not a mathematical proof of exhaustiveness.** The method guarantees that every block a careful reader identified in each truth doc received an explicit disposition, and that every non-NO-BEHAVIOR block maps to ≥1 concrete cell. It does **not** guarantee that every testable behavior was extracted from every block. The named residual: **a behavior stated only deep in prose — a single qualifying clause, a parenthetical, a footnote inside an otherwise-covered section — that the region reader did not decompose into its own cell.** Block-level accounting cannot detect a behavior it never saw. Phase 2 (deterministic per-cell measurement) will surface cells whose specified behavior the compiler does not implement, but it inherits this corpus and cannot recover a behavior that was never enumerated here.

## Truth docs (cell sources)

- `docs/language/precept-language-spec.md`
- `docs/language/primitive-types.md`
- `docs/language/temporal-type-system.md`
- `docs/language/business-domain-types.md`
- `docs/language/collection-types.md`
- `docs/compiler/proof-engine.md` (prevention behaviors)

## Cross-check docs (consistency, not primary cell sources)

- `docs/compiler/diagnostic-system.md`
- `docs/language/literal-system.md`
- `docs/language/precept-grammar.md`
