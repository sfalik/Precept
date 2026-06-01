# Deep Dive — Dynamically-Dispatched Diagnostic Emission — 2026-06-01

**Status**: Active — investigation grounding **Phase 8 Slice 2** (code-mediation standardization) and **Slice 3** (ownership architecture). Commissioned to answer, independently of any reviewer suggestion: *what are the dynamic-dispatch emission sites, why were they created, what problem do they solve, is the complexity worth it, should we standardize, and if so how do we design for it.* Read-only; no code changed.

**Method**: enumerated every `Diagnostics.Create(code, …)` site where `code` is not a literal `DiagnosticCode.X` (the real population, not inferred); traced each selector + its inputs; git-archaeology'd each for origin/rationale. Three parallel neutral investigations (front-end, CI, proof), synthesized here.

---

## 1. The population — five families, three rationales

| Family | Site(s) | What selects the code | Candidate set | Origin / why |
|---|---|---|---|---|
| **A. Lexer mode-switch** | `Lexer.cs:187` | `s.Mode` (closed 4-member `LexerMode` enum) | 3 `Unterminated*` codes | Consequence of merging N per-mode scanners into one mode-stack EOF-drain loop (`99297b24`); predecessor `fabf30b5` emitted literals per scanner |
| **B. CI catalog-mediated** | `CI.cs:90,104,182` | `BinaryOperationMeta/FunctionMeta.CIDiagnosticCode` (catalog field) | 5 `CaseInsensitive*` codes | Catalog-derivation sweep (`b1c95512`, 2026-05-09); each `~string` op needs its own code + fix-message |
| **C. Typed-constant content** | `Expressions.cs:268,396,402,406` | validator string `Code` + Format/Semantic kind + catalog `Format/SemanticErrorCode` + concrete-vs-interpolated gate | ~8 codes | `ff58d19d` (Slice 9B — move per-family code into catalog vs always-generic PRE0053); `d68eb6bc` (honor specific code only when qualifiers concrete) |
| **D. Proof obligation→code (subtype)** | `ProofEngine.Diagnostics.cs` `CreateDiagnostic` switch | `ProofRequirement` DU subtype | 10 of 17 codes | Foundational (`d27fae6b`); **Slice 9C `51c7c03c` already moved these to catalog `ProofRequirementMeta.DiagnosticCode`** |
| **E. Proof obligation→code (context)** | `GetNumericRequirementDiagnosticCode:314`, KeyPresence flag, QualifierChain override | Site shape / requirement flag / resolved-qualifier content | 7 of 17 codes | Same; the residue Slice 9C left as `null` in catalog (genuinely 1:many) |

These collapse to **two underlying patterns**, which is the key to the whole question:

- **Pattern 1 — catalog-mediated code** (B, C-catalog-stage, D): the code is *static metadata declared on a catalog entry*; the emission site reads it. This is **not** "dynamic dispatch" in any worrying sense — it is Precept's catalog-driven architecture working as designed. Bounded and statically enumerable by reading the catalog.
- **Pattern 2 — context-determined code** (A, E, C-string-stage): the code is a function of *runtime context* the stamped metadata doesn't capture — the open lexer mode, the discharge-site shape, a requirement flag, or a resolved-qualifier's component count.

## 2. Why they exist / what problem each solves

**The dominant rationale is catalog-derivation, not indirection-for-its-own-sake.** B, C's catalog stage, and 10/17 of D all exist to keep the *code-per-member* association as **catalog data** rather than a hardcoded `*Kind → DiagnosticCode` switch in pipeline logic — which CLAUDE.md's Catalog System rules explicitly forbid ("Never maintain parallel keyword lists … Never switch on `*Kind` enum identity to dispatch per-member behavior"). Concretely:
- **B**: each `~string` operation (`==`/`!=`/`contains`/`startsWith`/`endsWith`) has its own code carrying its own fix-text (`==`→use `~=`; `contains` uses 4 message args). The association lives on the operator/function catalog entry (`Operations.cs:851-860`, `Functions.cs:225-245`) and is *also* consumed by the MCP formatter (`CatalogFormatters.cs:294-296,384-387`) — a literal rewrite would need a parallel `OperatorKind→code` list in two places.
- **C-catalog**: each typed-constant family declares format-vs-semantic codes in the catalog (`Types.cs:63-86`), letting validators stay diagnostic-code-agnostic and avoiding "always generic PRE0053."
- **D-subtype**: Slice 9C (`51c7c03c`) *already proved* these belong in the catalog — it moved them to `ProofRequirementMeta.DiagnosticCode` and made `CreateFaultSiteLink` read `GetMeta(kind).DiagnosticCode` (`ProofEngine.Diagnostics.cs:406`).

**Pattern 2 exists because the code genuinely isn't a property of any single catalog member:**
- **A**: at EOF you can't statically know which/how-many literal modes are open; the map from a popped frame's mode → code is the minimal consequence of the mode-stack architecture. Trivial and bounded.
- **E-Numeric-collection**: the *same* `NumericProofRequirement(count > 0)` surfaces as `IndexBoundsGuard` / `UnguardedCollectionAccess` / `UnguardedCollectionMutation` purely by whether the discharge **Site** is a parameterized member-access, a member-access, or a field-ref (`:318-322`). The requirement record carries no distinguishing field.
- **E-KeyPresence / QualifierChain-override**: code depends on a requirement flag (`RequireAbsence`) or on resolving the period subject's `TemporalUnit` component count at discharge.

## 3. Is the complexity worth it? (neutral verdict)

**Mostly yes — because most of it is the catalog architecture, not gratuitous indirection.** But three pieces are genuine, removable debt, and they are exactly the parts that are *not* clean catalog-mediation:

| Piece | Worth it? | Why |
|---|---|---|
| Pattern 1 (catalog-mediated: B, C-catalog, D-subtype) | **Yes** | It IS the catalog-driven, no-parallel-lists architecture. Removing it (literals at each site) would *violate* a non-negotiable. Bounded + statically enumerable. |
| A (lexer mode-switch) | **Yes** | Trivial, fully bounded, an inevitable consequence of the mode-stack. Not worth touching. |
| E (genuine context-determined: Numeric-by-Site, KeyPresence-flag, QualifierChain-override) | **Yes, but isolate** | The code truly depends on discharge context; can't be a pure catalog field. Legitimate — but should be *explicitly marked* as the residue, not blended with Pattern 1. |
| **C-string round-trip** (validator does `DiagnosticCode.X.ToString()`, checker `Enum.TryParse`s it back, `:349`) | **No — debt** | Stringly-typed, typo-silent (a bad string falls through to generic PRE0053 with no compile error), and the **one open seam that makes C un-boundable** for static analysis ("any string that names a DiagnosticCode"). |
| **D dual-surface re-hardcoding** | **No — debt** | `CreateDiagnostic` re-hardcodes the 10 subtype-fixed codes that `CreateFaultSiteLink` already reads from the catalog (`:406`) — so a code change must touch two places or drift. Slice 9C fixed half the surface and left this half. |
| **B paired-field invariant** (`HasCIVariant` + `CIDiagnosticCode` must agree) | **Minor debt** | Two fields encode one fact; a `true` flag with null code silently no-ops. Could be one nullable field. |

## 4. Should we standardize on one approach? — Yes, and here's the shape

The right answer is **not** "make everything a literal" (violates the catalog rule) nor "leave three ad-hoc shapes." It is: **converge the Pattern-1 cases on a single explicit convention, and isolate the Pattern-2 residue.**

**Standardization (the design):**

1. **One convention for catalog-mediated codes**: a `DiagnosticCode` (or `DiagnosticCode?`) field on the relevant catalog meta — `BinaryOperationMeta`/`FunctionMeta` (B, exists), `ContentValidation` (C, exists as `Format/SemanticErrorCode`), `ProofRequirementMeta` (D, exists as `DiagnosticCode`). The emission site reads `meta.<Field>`. This is already the pattern in three places; standardizing means **finishing it**, not inventing it:
   - **C**: drop the `DiagnosticCode.ToString()` ↔ `Enum.TryParse` round-trip — have validators carry a `DiagnosticCode?` (or just the Format/Semantic kind) directly; the concrete-vs-interpolated gate stays checker-side but keys on a typed value, not a parsed string.
   - **D**: have `CreateDiagnostic` read `meta.DiagnosticCode` for the 10 subtype-fixed kinds (as `CreateFaultSiteLink` already does), so the code lives in exactly one place; keep only message-arg formatting in the per-subtype arms.
   - **B**: optionally collapse `HasCIVariant`+`CIDiagnosticCode` to one nullable field.

2. **Explicitly mark the Pattern-2 residue** — A (lexer), E (Numeric-by-Site, KeyPresence-flag, QualifierChain-override). For these, prefer **pushing the distinguishing bit onto the stamped artifact** where feasible (e.g. stamp the target code or a discriminating requirement variant — turning Pattern 2 into Pattern 1), and where the code is *only* knowable at discharge (Numeric-by-Site), keep the dispatch but **declare its bounded candidate set** so it remains enumerable.

**What standardization buys** (in priority order — note analyzability is a *byproduct*, not the driver):
- Removes the two real debts (string round-trip, dual-surface drift) — a correctness/maintainability win on its own merits.
- Makes "where is code X emitted / what codes can site Y emit" answerable from the catalog, not from grepping selectors.
- *Then*, as a consequence, the emission surface becomes a small fixed set of shapes (catalog-field reads + a marked residue), which is what makes any ownership invariant — analyzer or test — tractable. The Slice 3 ownership analyzer's reach over dynamic sites is a *result* of this standardization, not the reason to do it.

## 5. Relationship to the reviewer's BLOCKER (for the record)

The Slice 3 design review independently flagged that the ownership analyzer can't bind dynamic-dispatch codes to a containing type and proposed a "catalog-side stage invariant." This deep dive reaches a compatible end state **from the opposite direction** — not "make the analyzer cope with dispatch," but "standardize the dispatch into catalog-mediated metadata, of which analyzability is a byproduct." The difference matters for sequencing: the standardization (§4) is worth doing for maintainability **regardless of the analyzer**, and it should drive the design; the analyzer invariant then reads the now-uniform catalog fields. If the owner prefers, the standardization can precede or fold into Slice 3's D1/D2 rather than being bolted on as an analyzer special-case.

## 6. Recommendation (neutral options for the owner)

- **Option 1 — standardize as part of Slice 3**: fold §4's convergence (finish catalog-mediation for C and D, mark the E residue) into Slice 3's scope. The ownership analyzer then enforces a clean catalog invariant over uniform fields. Most coherent; larger Slice 3.
- **Option 2 — standardize as a precursor slice**: do §4 first as its own small slice (it's a maintainability refactor with no behavior change), then Slice 3's analyzer lands on the cleaned surface. Smaller Slice 3; one more slice.
- **Option 3 — accept the residue, scope the analyzer narrowly**: leave the dispatch shapes as-is, analyzer covers literal sites only, the dynamic families covered by tests. Least work; leaves the string round-trip + dual-surface debt.

The investigation's finding is that **§4 standardization is worth doing on its own merits** (Options 1 or 2), independent of the analyzer — the string round-trip and dual-surface re-hardcoding are real debt the catalog architecture would otherwise keep paying.

## 7. Is there a wider opportunity to adopt the pattern? (scan result)

Scanned the pipeline for the inverse — places that *should* be catalog-mediated but aren't (switch-on-catalog-`*Kind` to pick a code or per-member behavior). Findings, in three tiers:

**Tier 1 — the pattern is already established and the egregious smell is largely absent.** Catalog-mediated-`DiagnosticCode` already has ~5 instances: `StaticallyPreventableAttribute(DiagnosticCode)` on `FaultCode` (`FaultCode.cs:4`, 16 members), `CIDiagnosticCode` on `BinaryOperationMeta`/`FunctionMeta`, `Format/SemanticErrorCode` on `ContentValidation` (×2), `ProofRequirementMeta.DiagnosticCode`. A broad scan for raw `OperationKind switch`/`FunctionKind switch`-to-select-behavior in the pipeline returned **nothing** — the "switch on catalog enum identity for per-member behavior" smell is controlled, backed by a rich analyzer family (`Precept0008`–`0017` CrossRef, `0024` anti-mirroring, `0025/0026` DU completeness) and the pending `F-CAT-01` catalog audit (Phase 11). So the *general* discipline is not greenfield.

**Tier 2 — concrete surgical opportunities:**
- **`DiagnosticCode → FaultCode` switch (`ProofEngine.Diagnostics.cs:412`)**: its bijective rows duplicate the `[StaticallyPreventable]` correspondence (which today is *declared but never read by any reflection* — no consumer derives a map from it). It also encodes a many-to-one collapse + a backstop default (genuine extra policy). Opportunity: derive the bijective part from the attribute (kill the parallel rows), keep the collapse/backstop as explicit policy. **Caveat: partial duplication, not a clean smell — the collapse/backstop is real.**
- **Finish the half-done mediations** (§3): C's string round-trip, D's dual-surface re-hardcoding.

**Tier 3 — the high-leverage generalization is the *analyzer*, not more switches.** Slice 3's ownership analyzer (code's `Stage` = emitting stage) is one instance of a general capability: *enforce that every catalog-declared `DiagnosticCode` reference is consistent.* The shared `DiagnosticCoverageScanner` already backs `Precept0015` (cross-ref), `Precept0027` (coverage), and Slice 3's new analyzer. "Adopt more widely" is best read as **keep adding invariants on that one scanner** — stage-ownership (Slice 3), the `[StaticallyPreventable]` ↔ `:412` consistency (would catch drift between the attribute and the switch), emit-or-retire (`0027`) — rather than converting legitimate DU-subtype/structural dispatch (which CLAUDE.md sanctions) into metadata.

**Net**: the opportunity is real but **bounded and surgical** — (a) a small standardization (finish catalog-mediation; reconcile `:412` with the attribute) and (b) treating the ownership analyzer as the first member of a *catalog↔DiagnosticCode consistency* analyzer family. It is **not** a sweeping convert-every-switch effort; the broad smell is already controlled by the existing analyzers + F-CAT-01.

## Evidence index
- **A**: `Lexer.cs:51,176-188,215-217`; git `fabf30b5` (literal predecessor), `99297b24` (mode-stack switch).
- **B**: `CI.cs:79-105,156-188`; `Operation.cs:94-95`, `Function.cs:47-55`; `Operations.cs:88-95,851-860`, `Functions.cs:225-245`; `DiagnosticCode.cs:146,281-284`; git `b1c95512` (fields), `44981dd1` (CI pass).
- **C**: `Expressions.cs:265-270,332-356,358-385,387-407`; `TypedConstantValidation.cs`, `TypedConstantParseResult.cs:24-36`, `Type.cs:196-211`, `Types.cs:63-86`, `QuantityValidator.cs:44,65`; git `ff58d19d` (Slice 9B), `d68eb6bc`.
- **D/E**: `ProofEngine.Diagnostics.cs:11,21-24,66,137-140,210,256,297,314,389-428`; `ProofRequirement.cs:264-266,346`; git `d27fae6b` (birth), `51c7c03c` (Slice 9C — catalog migration), `196ad9f1`, `fd147dc2`.
