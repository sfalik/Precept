## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable language-surface guidance for parser, catalog, and tooling work.
- Historical summary: drove collection-surface research, parser/catalog design review cycles, vision-to-spec migration, and whitespace-insensitivity docs alignment.

## Learnings

- Expression-level grammar (atoms, multi-token left-denotations) is structural — not catalog vocabulary. The `Constructs` catalog covers declarations; expression nodes are parser output. Catalog compliance audits must distinguish vocabulary decisions (which tokens/operators exist) from grammar shape decisions (how the parser assembles tokens into AST nodes).
- **REVISED:** Multi-token operators (`is set`) DO need catalog entries even though the parser handles them structurally. The catalog is the language spec in machine-readable form — MCP vocabulary, LS hover, and AI grounding all iterate `Operators.All`. An operator that exists in the language but not in the catalog is a Completeness Principle violation. The `OperatorMeta` model accommodates `is set` with `Arity.Postfix` and `Token: TokenKind.Is` (lead token) — no `MultiTokenSequence` field needed.
- **Lesson:** "Does the parser need to read from it?" and "Does it need to exist in the catalog?" are different questions. I got this wrong in the original audit and conflated them. Catalogs serve all consumers, not just the parser.
- Implementation plans must specify WHERE in a method each change goes (e.g. "after the Dot handler, before OperatorPrecedence check"), not just what method. George needs insertion-point precision.
- GAP-2 fix does NOT require removing `When` from boundary tokens — the expression parser correctly stops; the ensure-parser then picks up `when` as a structural keyword. Keep boundary logic intact.
- `InterpolationPart` types are delimiter-agnostic — reusable across string and typed-constant interpolation without new node types.
- Named types are right when the choice changes storage or behavioral contract; modifiers are right when they narrow values without redefining the type.
- Collection docs work best per kind with shared cross-cutting sections because that mirrors how authors encounter the surface in `.precept` files.
- When the type system already disambiguates an operation, surface keywords should not restate the distinction.
- Philosophy and spec copy must say what the runtime actually guarantees; if they drift, flag the gap instead of silently rewriting either side.
- Durable research needs rationale, rejected alternatives, and concrete examples, not just a winning syntax.
- Whitespace-insensitivity is a language-identity rule, not a parser convenience; examples should prove that vertical layout is cosmetic.
- **"No consumer currently uses it" is never a valid argument against cataloging language surface.** This was proven the hard way: I initially rejected expression forms from the catalog on "no consumer value" grounds, which inverted the catalog-driven vision. The vision means: describe the language completely, then consumers derive from it. The vision precedes consumers. This principle is now codified in catalog-system.md § Vision precedes consumers with explicit anti-pattern naming.
- **Vocabulary vs. Structure test (SUPERSEDED — see revision below):**~~A language element belongs in a catalog if its identity is *what* — a named vocabulary member whose existence, metadata, and enumeration serve downstream consumers. It stays in the parser if its identity is *how* — a structural assembly rule that produces an AST shape.~~ This test was wrong. The Constructs catalog already catalogs parser output shapes (declaration-level grammar forms like `TransitionRow`, `FieldDeclaration`). The vocabulary/structure boundary I drew was a false dichotomy.
- **Revised test:** A language element belongs in a catalog if it is *language surface* — if it appears in `.precept` files, carries semantics that consumers need, or represents a concept that would appear in a complete description of Precept. This is what catalog-system.md § Decision Framework criterion 1 actually says. Expression forms (list literals, binary operations, function calls, etc.) pass this test and belong in the catalog as a 13th axis.
- Qualifier docs must model real multi-qualifier types rather than simplified one-qualifier prose.
- Inline pending-decision callouts are better than silently adopting unsettled syntax in canonical docs.
- **Parser coverage assertion against ExpressionFormKind is achievable via two-layer enforcement:** (1) compile-time CS8509 exhaustive switch in a `GetLeadTokens(ExpressionFormKind)` method that maps forms to their triggering tokens, (2) test-time xUnit assertion that verifies the parser's dispatch tables actually handle all declared lead tokens. The parser cannot use `ExpressionFormKind` as a runtime routing key because Pratt parsing discovers the form BY parsing tokens — the form is output, not input. But the catalog can assert "I described N forms, the parser handles N forms" which is genuine enforcement, not theater. Recommended as follow-on slice after GAP-6/GAP-7 fixes.

## Recent Updates

### 2026-05-01 — Parser coverage assertion follow-on locked
- Decision merged to canonical squad ledgers: parser coverage against `ExpressionFormKind` is worth doing, but as a follow-on slice rather than an expansion of the current parser-gap slice.
- Durable recommendation: use two-layer enforcement — catalog-side `GetLeadTokens(ExpressionFormKind)` exhaustive switch for CS8509 compile-time pressure plus an xUnit assertion that parser dispatch handles every declared lead token.
- Reasoning locked: Pratt parsing discovers expression form by reading tokens, so `ExpressionFormKind` is a coverage witness and validation axis, not a runtime parser routing key.

### 2026-05-01 — ExpressionFormKind catalog added to parser gap fixes plan

- Shane approved the architectural decision: expression forms get a new separate 13th catalog — `ExpressionFormKind` / `ExpressionFormMeta`.
- Added Slice 4 (`ExpressionFormKind` catalog) to `docs/working/parser-gap-fixes-plan.md` as a prerequisite for GAP-6 (list literals, now Slice 5) and GAP-7 (method calls, now Slice 6). Dependency ordering updated accordingly.
- `ExpressionFormMeta` shape: `Kind`, `Category` (Atom/Composite/Invocation/Collection), `IsLeftDenotation` (true for led forms: BinaryOperation, MemberAccess, MethodCall), `HoverDocs` (string). 10 members total.
- `LanguageTool.cs` MCP change added to scope: `expression_forms` section in `precept_language` output, grouped by `Category`.
- File inventory updated: `src/Precept/Language/ExpressionForms.cs` (Create), `tools/Precept.Mcp/Tools/LanguageTool.cs` (Modify), `test/Precept.Tests/ExpressionFormCatalogTests.cs` (Create) all added.
- §5 MCP Sync updated: plan now has one required MCP tooling change (LanguageTool.cs) rather than zero.

### 2026-05-01 — Expression form catalog placement analysis

- Completed full metadata shape comparison: ConstructMeta vs. projected ExpressionFormMeta.
- Domain-specific fields have ZERO overlap (5 Construct-specific, 2–3 ExpressionForm-specific).
- Recommendation: **Separate 13th catalog** (`ExpressionFormKind` / `ExpressionFormMeta`). Decisive factor: metadata shape incompatibility makes Option B require either nullable fields (anti-pattern per catalog-system.md) or a DU (which reinvents two catalogs with forced shared plumbing). Separate flat records are simpler and right-sized.
- Decision written to `.squad/decisions/inbox/frank-expression-form-catalog-placement.md`.

### 2026-05-01 — Expression forms catalog boundary REVISED

- **Prior position withdrawn:** My ruling that expression forms "do not get a catalog" was wrong. The argument rested on "no consumer value" and a vocabulary/structure distinction that the catalog system itself contradicts — the Constructs catalog already catalogs parser output shapes (declaration-level grammar forms). Expression forms are the same kind of thing at the expression level.
- **Revised position:** Expression forms belong in the catalog as a 13th axis. The Completeness Principle, the decision framework criterion 1, and the Constructs precedent all demand it. Expression forms are unambiguously language surface — they appear in every `.precept` file and would appear in a complete description of the language.
- **Key lesson:** "No consumer currently uses it" is not a valid argument against cataloging language surface. The catalog-driven vision means the catalog IS the machine-readable spec. The vision precedes consumers — consumers derive from it, not the other way around.
- **Open design question:** Separate 13th catalog (`ExpressionForms`) vs. extending `Constructs`. Metadata shapes differ enough to suggest separate.
- Decision written to `.squad/decisions/inbox/frank-expression-forms-revised.md`.

### 2026-05-01 — Parser gap fixes implementation plan authored
- Wrote `docs/working/parser-gap-fixes-plan.md` — 11 vertical slices covering GAP-1/2/3/6/7/8 + test coverage gaps.
- Key design decisions captured in `.squad/decisions/inbox/frank-parser-gap-plan.md`.
- Confirmed: no MCP/tooling/catalog changes needed. All tokens already exist. TypeChecker is blocked anyway.
- Ordering: spec fix → test-only slices → expression atom fixes → left-denotation fixes → ensure guard → sample integration tests last.

### 2026-05-01 — Full parser spec audit completed
- Completed exhaustive parser-vs-spec audit on `spike/Precept-V2`; findings in `.squad/decisions/inbox/frank-full-spec-review.md`.
- **GAP-2 history confirmed:** post-condition `ensure Cond when Guard` form was already in samples before `50a459c` (2026-04-28) updated the spec to match. Parser was never updated. Old stashed-guard form (`when Guard ensure Cond`) still works but is the wrong form.
- **New gaps found:**
  - GAP-6 (Medium): List literal expressions — no `LeftBracket` case in `ParseAtom()`, no `ListLiteralExpression` AST node.
  - GAP-7 (Medium): Method calls on member access (`obj.method(args)`) — no `LeftParen` left-denotation in Pratt loop, no `MethodCallExpression` AST node.
  - GAP-8 (Low/Spec Defect): `because` clause marked optional (`?`) in §2.2 grammar — contradicts design principle 9 and all 28 samples. Parser is correct; spec needs correction. This is mine to fix.
- **Coverage estimate:** ~88% of spec correctly implemented; the remaining 12% concentrated in expression-atom and post-condition-guard gaps.
- **Sample impact:** GAP-3 (`is set`) affects 11/28 samples; GAP-2 affects 2/28 samples; all other sample files parse cleanly.

### 2026-05-01 — GAP-1/2/3 spec analysis recorded
- Inbox analysis on typed constants, guarded `ensure`, and `is set` / `is not set` was merged into `.squad/decisions/decisions.md`.
- Durable recommendation captured: canonical guarded-ensure surface stays post-condition (`ensure Condition when Guard because ...`), while GAP-3 remains blocked on catalog-backed presence-operator design.

### 2026-05-01 — WSI docs sync recorded
- Updated `docs/language/precept-language-spec.md` (§0.1.5 and §1.4), `docs/compiler/parser.md`, `docs/working/parser-implementation-notes.md`, and `docs/language/collection-types.md` to align docs with trivia-free parsing and multi-line qualifier examples.
- Locked wording: line-oriented structure is keyword-anchored, not newline-delimited.
- Preserved multi-qualifier scalar examples and catalog-driven qualifier disambiguation guidance.

### 2026-04-29 — Collection surface research consolidated
- Authored and reconciled the collection-surface research that fed `docs/language/collection-types.md`, including quantifier direction, field-level rule rollout, and candidate-type evaluation.
- Durable rule: decide named-type vs modifier proposals by asking whether the change alters behavioral contract or only admissible values.

### 2026-04-29 — Lookup/queue surface simplification approved
- Approved `containskey` → `contains`, `removekey` → `remove`, and dequeue-capture `priority` → `by`.
- Key principle: when collection type already determines key/value role, surface vocabulary should stay shared and minimal.

### 2026-04-28 — Design/doc revision bar clarified
- Helped shift combined-design and parser-design docs toward decision-led, implementation-ready prose with explicit rationale and cross-surface sync.
