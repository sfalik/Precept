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

### Stop-and-fix items — resolved (owner-ruled 2026-07-14)

Block accounting surfaced four **doc-internal contradictions / under-determined doc text** where a cell was authored (most-operationally-specific reading chosen) but could not be trustworthily *verdicted* until the source doc was resolved. The owner walked and ruled all four before Phase 2; each source doc was corrected. Records below.

1. **primitive-types — integer overflow contradiction.** `primitive-types.md:81` (Approximation Stance) said integer is "arbitrary-precision… no overflow"; `:197` (Backing) said "Overflow is a type error"; `:203` (operators) said "Checked overflow" — three incompatible claims.
   - **Owner ruling (2026-07-14):** `integer` is **fixed-width 64-bit today**; the overflow *model* — compile-time proof-or-reject **vs. adopting arbitrary-precision** — is a **deferred post-MVP decision**, arbitrary-precision still a live option. `:81` was rewritten to state fixed-width and disclose the deferral; an Open Questions note was added. `:197`/`:203` (bounded, overflow-is-a-fault) were left as-is — they state the fixed-width target end-state. **This overrides the earlier ledger-review recommendation to delete "arbitrary-precision" from `:81`** (that recommendation assumed fixed-width was the settled end-state; the owner keeps the model open).
   - **Cell disposition:** integer *representational-overflow* cells are a **known build-order gap** (overflow prevention is parked post-MVP) — not a fresh finding and not a pass. Tag them parked; do not re-report as drift. Unary `-long.MinValue` negation overflow sits in the same parked lane and re-activates when the overflow model is ruled.
2. **primitive-types — `pow` negative-exponent lane scope.** `primitive-types.md:613` ("`exp` must be non-negative for integer lane") left open whether decimal/number bases accept negative exponents.
   - **Owner ruling (2026-07-14): resolved by existing locked precedent — no new surface.** `:677` (Function lane integrity) admits `pow`'s decimal overload only because the op is "closed over finite decimals"; `:614` keeps `sqrt` number-lane-only because it is not closed. A negative exponent is not finite-decimal-closed (`pow(3m, -1)` = 0.333…), so the only consistent reading is **non-negative exponent across all lanes**. Cells `primitive/number/fn-pow-negative-exp` and `primitive-types/pow-decimal-base-negative-exponent` (both `reject`) are **correct**. `:613` note corrected to "non-negative — negative exponents break decimal-lane closure."
   - **Phase-2 verification item:** whether the compiler actually enforces a `pow` exponent-sign obligation today (`Functions.cs` encodes proof only on the integer-base overload) — a doc/catalog sync to verify, not new surface. *(This corrects an earlier provisional annotation attributing the resolution to Frank; Frank's ledger review did not cover `pow` — this is an owner ruling.)*
   - (Related, already resolved: 9 primitive-types draft cells that asserted `accept` for unconstrained-divisor `/`/`%` and unconstrained-sign `sqrt` were corrected to `reject` per prove-or-reject, each carrying a `fix_note`.)
3. **business-domain — stale D10 Implementation Scope + bounds-qualifier example.**
   - **Owner ruling (2026-07-14):** `:1989` was stale pre-retirement text (implicit ISO-4217 `maxplaces` + automatic half-even rounding) contradicting the locked D10 retirement (`:1760-1768`, explicit-only) and the Approximation Stance (`:174`, no implicit rounding). Rewritten to explicit-only `maxplaces` + author-invoked `round(...)`, matching the already-correct `:1579`.
   - **Secondary (`:1576`):** the example `field Score as quantity max 100` violated `PRE0133 BoundsRequireQualifier` (a `max` bound on a `quantity` field requires the `in`/`of` qualifier — `precept_compile`-confirmed). The earlier `in 'points'` suggestion **fails** `PRE0075` (`'points'` is not a valid unit); corrected to `field Score as quantity in 'each' max 100` (compile-verified). Both source-doc fixes applied.

Temporal's inventory reports **Stop-and-fix: None**; the six other units report none. **All four items are resolved and their source docs corrected — the corpus is verdict-ready for Phase 2.**

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
