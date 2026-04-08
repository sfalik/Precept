## Core Context

- Owns architecture, system boundaries, language direction, and final architectural review across runtime, tooling, and documentation surfaces.
- Co-owns docs/research/language/ with George; proposal work must stay aligned with docs/PreceptLanguageDesign.md, the MCP/runtime truth, and Shane sign-off gates.
- Public-facing docs and design assets must describe implemented behavior honestly; samples and README examples are product claims, not decorative filler.
- The sample corpus is roadmap instrumentation. Preserve real domain pressure, classify sample complexity explicitly, and use FUTURE(#N) comments when a believable workflow needs not-yet-shipped syntax.
- Architectural guardrails remain stable: thin MCP wrappers, keyword-anchored flat statements, deterministic inspectability, first-match routing, and no speculative semantics in docs.

## Recent Updates

### 2026-04-07 - PR #35 merge: finalize README cleanup and record Squad decision
- Merged PR #35 (chore: finalize README cleanup and record Squad decision) to `main`.
- Branch `chore/upgrade-squad-latest` carried 2 commits: (1) Scribe post-task recording for PR #34 merge, and (2) README Quick Example refactoring (removed explanatory hedge, copyable DSL block, replaced markdown image syntax with fixed-width HTML img tag).
- Cleaned up merged inbox entries from `.squad/decisions/inbox/` and updated `.squad/agents/j-peterman/history.md` with team update.
- Workflow: Clean working tree before push (separated uncommitted Squad sync artifacts from committed README cleanup work), explicit PR creation with `gh pr create --base main --head sfalik:chore/upgrade-squad-latest`, merged with merge-commit strategy.
- User directive honored: branch retained locally and remotely per user request (NOT deleted post-merge).
- Verified zero scope creep: 81 additions, 3 files changed (README.md, .squad/decisions.md, .squad/agents/j-peterman/history.md), no unrelated code changes.
- Co-authored-by trailer included in original commits.
### 2026-04-08 - Sample ceiling consolidation recorded
- Team update (2026-04-08T01:13:25.793Z): Scribe merged Frank's ceiling analysis and philosophy addendum with Steinbrenner's planning lane and Peterman's benchmark lane into `.squad/decisions.md` — decided by Frank, Steinbrenner, and J. Peterman.
- Shared outcome: 42 is now the hard upper bound for the flat in-repo corpus, while the real acceptance gate is whether a candidate strengthens domain fit, prevention, inspectability, and marginal corpus value.


### 2026-05-18 - Sample ceiling philosophy addendum
- Produced docs/research/sample-realism/frank-sample-ceiling-philosophy-addendum.md.
- Made explicit how Precept philosophy (prevention, inspectability, determinism, one-file completeness, AI-first) shapes corpus size, domain selection, and refusal criteria.
- Established domain-fit tiers: Tier 1 (native) = claims/disputes, financial approvals, compliance; Tier 2 = IAM, healthcare auth, HR lifecycle; Tier 3 = scheduling/logistics, reference data.
- Defined dilution test: 5 questions to evaluate whether a candidate sample earns its slot or inflates count.
- Reframed 42 as an upper bound, not a quota. Realistic philosophy-driven range is 30–42 depending on how many candidates pass the quality floor.
- Decision note filed to .squad/decisions/inbox/frank-sample-ceiling-philosophy.md.

### 2026-05-17 - Sample corpus ceiling analysis
- Produced docs/research/sample-realism/frank-sample-ceiling-analysis.md.
- Recommended optimal operating band of 40-50 samples, soft ceiling at 50, hard ceiling at 55.
- Proposed three-tier model: Core Canon (15-20), Extended Canon (20-25), Experimental (5-10 rotating).
- Confirmed Steinbrenner's 42-sample target as sound near-term anchor at the low end of the optimal band.
- Introduced marginal-value gate: new samples past 42 must demonstrate value on ≥2 of 3 axes (domain, shape, construct).
- Decision note filed to .squad/decisions/inbox/frank-sample-ceiling.md.

### 2026-04-08 - Sample realism research consolidated
- Scribe merged Frank, George, J. Peterman, and Steinbrenner sample-realism findings into .squad/decisions.md and cleared the inbox notes.
- The active decision set now locks the FUTURE(#N) comment protocol, modeling-over-syntax principle, case-centric flagship sample bias, the 42-sample portfolio direction, and Shane's directive to use Opus when sample/design judgment is especially high.

### 2026-05-17 - Sample realism initiative: language & philosophy research
- Produced docs/research/sample-realism/frank-language-and-philosophy.md.
- Established realism criteria, sample complexity labels, and feature-to-sample traceability expectations for the sample expansion pass.
- Ranked proposal impact on sample quality: Tier 1 choice, conditional expressions, field constraints, logical operators; Tier 2 integer/decimal/conditional invariants/computed fields/named rules; Tier 3 data-only, absorb, date, .length, .contains(), and functions.
- Proposed 20 additional sample candidates spanning missing business domains so the corpus can teach category breadth instead of only syntax coverage.

### 2026-05-01 - Expression feature research
- Produced docs/research/dsl-expressiveness/expression-feature-proposals.md and aligned the research README around verified parser/runtime gaps.
- Recommended staged rollout: Wave 1 .length plus conditional expressions, Wave 2 named rules and conditional invariants, later waves for richer string/numeric helpers.
- FEEL/DMN remains the strongest business-rule comparator; Cedar remains the strongest analyzability counterweight.

### 2026-04-07 - PR #34 merge with Squad config and README image fixes
- Merged PR #34 to main, keeping the scope to Squad metadata plus README image-path corrections.
- Confirmed the workflow pattern: commit related uncommitted work before push, create the PR with explicit base/head, and keep the history auditable with the required trailer.

### 2026-04-05 - Named rule proposal converged
- Finalized the proposal framing around rule <Name> when <BoolExpr> with reuse in when, invariant, and state assert, but not on <Event> assert.
- Elevated philosophy fit and non-programmer readability from soft preferences to explicit review gates.

## Learnings

### 2026-05-18 - Philosophy draft terminology correction
- Corrected `design/brand/research/philosophy-draft-v2.md` to use actual DSL construct names in concept lists: fields, states, events, invariants, and assertions. `guard` remains acceptable only as informal prose or as the inline `when` condition concept, not as a top-level declared construct.
- Adjusted the draft's opening construct list and Peterman note so fields and states are framed as peer authoring tools rather than a required-vs-optional hierarchy. This keeps the philosophy aligned with the intended stateless-precept direction from Issue #22 without overstating current parser constraints.

### 2026-04-08 - Philosophy location canonicalized
- Philosophy canonical location: `docs/philosophy.md` (not `design/brand/`)
- `design/brand/philosophy.md` is now a redirect pointer
- The old icon-specific sections were stripped in earlier edits

### 2026-05-18 - README hero sizing PR closeout
- Merged PR #36 (`chore: finalize README hero DSL sizing contract`) from `chore/upgrade-squad-latest` into `main` with a merge commit, preserving the branch per Shane's standing directive to keep it open for follow-on work.
- Final README hero DSL tuning is now anchored to GitHub's 830px repo-view image ceiling; the durable regeneration contract lives in `design/brand/readme-hero-dsl.html` and `design/brand/capture-hero-dsl.mjs`, with the rendered artifact at `design/brand/readme-hero-dsl.png`.
- When a README asset PR is already visually approved, keep the PR/body scoped to the final user-visible change plus the reproducibility contract; do not reopen aesthetic debate once the principal has said the result "looks perfect."

### 2026-04-08 - Already-merged PR verification on retained branches
- When a long-lived branch is supposed to stay open after merge, branch existence proves nothing about PR state. Verify with `gh pr status` and `gh pr view` before attempting any PR creation or merge action.
- Fetch first, then compare `origin/main`, `HEAD`, and `origin/<branch>`. A stale local `main` can make a completed PR look unmerged when GitHub has already landed the merge commit.
- Correct closeout in this situation is factual reporting: PR number, title, merge commit, merged timestamp, and surviving branch state locally/remotely — not a second round of theatrics.

### 2026-04-05 - Beyond-v1 type system roadmap reasoning
- Completed forward-looking type system growth analysis appended to `docs/research/language/references/type-system-survey.md`. Evaluated 9 type candidates against the same 6-system survey base (FEEL, Cedar, Drools, NRules, BPMN, SQL).
- **Phase 2 (post-v1) top 3:** (1) ordinal `choice` comparison with explicit `ordered` keyword — lowest cost, highest convenience, extends v1; (2) named choice sets (`choiceset`) — reduces repetition, low parser cost; (3) `integer` — first genuinely new type, justified when fractional-rejection invariants appear as workaround patterns.
- **Phase 3 (enterprise):** `decimal`/`money` (financial precision), `duration` (scheduling/SLA), `time` (business hours — hardest timezone design problem).
- **Long-term:** `range`/intervals (tier-based decisioning), string pattern constraints, `list<T>` with index access.
- **Never-add list is architectural identity, not technical limitation:** record/structured types, map/dynamic keys, datetime/timestamp, function types, domain-specific string types, `any`/dynamic typing, inheritance. Each would erode Precept's core contract (statically analyzed, deterministically executable, self-contained).
- **Key design principle confirmed:** each phase transition gated by evidence from real usage (workaround cost > complexity cost), not speculation. Requires 50+ real-world precepts before Phase 2 triggers.
- **Hardest open question for Phase 2:** integer division semantics — does `5 / 2` yield `2` or `2.5`? Must be resolved before `integer` ships. Recommendation: truncate, no implicit promotion.
- **`map<K,V>` is the strongest "never" candidate** — not because it's technically impossible but because a precept with dynamic keys is a precept that hasn't been modeled. The right answer is always "model your fields explicitly."

### 2026-05-16 - Focused PR handling for squad metadata
- When Shane asks for a PR on an existing `.squad/team.md` edit, keep the branch and PR surgically scoped to that file even if local bookkeeping updates happen afterward.
- Reliable path set for this workflow: `.squad/team.md` for the roster/source metadata, `.squad/agents/frank/history.md` for post-task learnings, and `main` as the PR base unless explicitly redirected.

### 2026-05-15 - Standard issue workflow normalization
- Standardize GitHub issue management by separating concerns cleanly: routing labels tell Squad who owns the work, taxonomy labels tell humans what kind of work it is, GitHub Project `Status` carries lifecycle, and issue open/closed carries terminal semantics.
- Proposal-specific status labels (`needs-decision`, `decided`, `deferred`) are architectural clutter. A proposal is just an issue type; decision-waiting belongs in board status (`In Review`), and the final outcome belongs in the closing comment plus issue closure.
- Recommended minimal routing surface for this repo: `squad` as the team inbox and exactly one `squad:{member}` label for direct ownership. Keep priority/blocker/security-style labels optional and special; do not let them become shadow workflow states.
- Key references for this recommendation: `.squad/templates/issue-lifecycle.md`, `.squad/routing.md`, `docs/research/language/README.md`, and `.copilot/skills/architectural-proposals/SKILL.md`.

### 2026-05-01 - Expression research methodology
- MCP compile tool is the authoritative way to confirm expression limitations — faster and more reliable than reading parser source. Use `precept_compile` with minimal test precepts to verify each proposed construct before documenting it.
- FEEL (DMN) is the strongest external comparator for Precept's expression surface. It's a business-oriented DSL with ternary (`if-then-else`), string functions (`string length`, `contains`), numeric functions (`abs`, `min`, `max`, `round`), and range membership (`x in [1..10]`). Any expression feature Precept considers should be benchmarked against FEEL's design.

### 2026-05-17 - README DSL contract text sizing analysis
- Evaluated four approaches for keeping DSL contract text size consistent with surrounding README text on GitHub: (a) redesigned PNG, (b) SVG with `<text>`, (c) styled HTML/CSS, (d) fenced code block.
- **Key architectural insight:** Any image-based approach (PNG or SVG) is fundamentally brittle for text sizing. Images render in their own scaling context via `<img>` — text inside scales with the image viewport, not the page's font cascade. No `width` attribute or viewBox tuning can synchronize image-internal text with page text across viewports and zoom levels.
- **SVG is better quality, same sizing problem.** GitHub renders SVGs as `<img>` tags, not inline. Vector quality eliminates pixelation but the viewport-mismatch remains identical to PNG.
- **HTML/CSS is impossible on GitHub.** The sanitizer strips `<style>`, `style=""`, `class`, and all custom CSS. No path to styled code in a README.
- **Fenced code blocks are the only stable approach.** Native `<pre><code>` participates in the page's CSS font cascade. Text scales identically to all other page text at every viewport and zoom level.
- **The DSL's readability without decoration proves the language design thesis.** If the DSL needs color to be readable, the keyword-anchored English-ish design has failed. It hasn't.
- **Medium-term unlock: GitHub Linguist registration.** A Tree-sitter grammar + Linguist inclusion gives real syntax highlighting in fenced code blocks that tracks GitHub themes and scales natively.
- Sizing analysis and the resulting README image-width tradeoff are now preserved in `.squad/decisions.md`.

### 2026-04-07 - PR workflow with uncommitted changes
- When branch carries uncommitted changes related to the stated PR task (e.g., README image fix), create a commit before pushing and creating the PR. Include the Co-authored-by trailer per repo policy.
- Push with `-u` flag to set upstream tracking, then use `gh pr create` with explicit `--base` and `--head` to ensure correct target branch.
- For merge decisions, prefer merge commit strategy when PR spans multiple decision areas (Squad config + image fix). Use auto-merge when available, but be prepared for merge method prompt and branch deletion confirmation.
- Verified successful PR #34: merged to main with both Squad configuration upgrades and README image link fixes (design/ prefix added) in a single clean commit history.
- Cedar (AWS) is the strongest *counter-precedent*. It deliberately omits division and most math functions to maintain formal analyzability. Precept should note which features Cedar excludes and why — not everything FEEL does is automatically right for a constrained DSL.
- The ternary gap is the single highest-impact expression limitation. It causes row duplication across 14+ samples. String `.length` is the most *embarrassing* gap — it's a table-stakes feature that every comparison target provides.

### 2026-04-05 - Language proposal review sequencing
- Reviewed language proposal issues #8-#13 against the DSL expressiveness research and the current language-design constraints.
- Recommended first-wave candidates: `#10` string `.length` and `#8` named guards; second wave: `#9` ternary-in-`set`, then `#11` absorb shorthand; last wave: `#12` inline `else reject` and `#13` field-level constraints.
- Reaffirmed that keyword-anchored flat statements and first-match routing are architectural guardrails; proposals that pressure either surface need explicit containment or they will sprawl.

### 2026-04-05 - Language proposal body expansion (#11, #12, #13)
- Expanded issues #11, #12, #13 from acceptance-criteria stubs into full proposal writeups with real Precept examples drawn from existing sample files and reference-language code (xstate, Polly, Zod, FluentValidation).
- **#11 (`absorb`):** `absorb` must be event-scoped (not bare); explicit `set` takes precedence; language server must warn on zero-match absorb. Last wave.
- **#12 (`else reject`):** Scope locked to `reject` only — never `else transition` or `else set`. Only one `else reject` per event+state pair; multi-guard scenarios must use standalone fallback rows. The multi-else-reject interaction must be resolved in a design doc before any code. Second-to-last wave.
- **#13 (field-level constraints):** Shape A (inline `min`/`max`) violates the keyword-anchor principle — research README already rejected it. Shape B (`constrain` keyword) preserves the principle but creates two constraint pathways. Neither shape is implementation-ready without a Shane sign-off on which to adopt. Last wave.
- Decisions inbox entry written at `.squad/decisions/inbox/frank-expand-language-proposals.md`.

### 2026-05-14 - Guard reuse in invariants and state asserts
- Researched whether named guard declarations (#8) should be referenceable in `invariant` and state `assert` contexts, not just `when` clauses.
- **Recommendation: YES.** Guard bodies are field-scoped; invariants and state asserts are field-scoped. Exact scope match — no widening needed. Allowing guard names in all field-scoped boolean-expression positions is the natural name-resolution rule.
- **Event asserts: NO.** Confirmed via PRECEPT016 that `on <Event> assert` is arg-only scoped — fundamentally incompatible with field-scoped guards. This is correct scoping, not a limitation.
- **`set` RHS: NO (v1).** Even though type-compatible for boolean fields, allowing guards in value positions blurs the boundary with computed fields (Proposal 7). Keep concepts separate.
- Guard reuse should be part of v1 guard implementation, not a follow-up — the implementation cost is near-zero if guards are resolved as named boolean expression symbols.
- Key composition point with Proposal 4 (conditional invariants): `invariant X > 0 when GuardName because "..."` — guards as conditions on conditional invariants.
- Updated `docs/research/dsl-expressiveness/expression-feature-proposals.md` Proposal 3 with full reuse analysis, scope compatibility table, examples, and Proposal 4 interaction.
- Decision filed at `.squad/decisions/inbox/frank-guard-reuse.md`.

### 2026-04-05 - Type system expansion proposal
- Filed comprehensive GitHub issue proposal for expanding the Precept type system. Triggered by Shane asking "do we support dates yet?" — we don't.
- **Corpus evidence is strong:** 4 samples use `number` as day-counter workaround for dates; 1 sample uses `number` + paired invariants as ersatz enum; 3+ samples use `set of string` for named-item collections that are really constrained value sets; 1 sample concatenates strings to simulate structured data.
- **Recommended `choice` type** over `enum`: `choice` reads as configuration ("pick one of these"), `enum` reads as programming. `choice("Low", "Medium", "High")` — the value set is the type. This is the highest-confidence addition because it has no operator design, no temporal semantics, and no philosophy tension.
- **Recommended `date` type** at day-level granularity only: no time-of-day, no timezone. Deterministic day arithmetic (`+` / `-` with `number` of days), comparison operators, `.day` / `.month` / `.year` accessors. This matches corpus usage (all 4 date-workaround samples track relative day offsets, not wall-clock times).
- **Rejected `datetime`/`timestamp`:** Timezone semantics violate deterministic inspectability. If you can't fire the same event with the same inputs and get the same result regardless of where the server is, you've broken Precept's core contract. Time-of-day is an event input, not a DSL type.
- **Rejected structured/record types:** Precept is flat by design. The trafficlight `EmergencyReason = AuthorizedBy + ": " + Reason` concatenation is a deliberate flattening, not a workaround. Adding record types would create nested field paths, complicate the type checker's per-field interval analysis, and pressure the keyword-anchored flat statement model.
- **`choice` interacts with collections:** `set of choice(...)` is the natural replacement for `set of string` when the string values are a known domain. This should be supported from v1 — the collection inner-type constraint just widens from `{string, number, boolean}` to `{string, number, boolean, choice}`.
- Decision filed at `.squad/decisions/inbox/frank-type-system-expansion.md`.

### 2026-05-15 - Named construct renamed from `guard`/`predicate` to `rule`
- Shane challenged the `guard` naming: if the construct is reused in `invariant` and state `assert`, it's no longer a guard. He was right.
- Evaluated 6 options: keep `guard` (rejected — routing connotation), `rule` (recommended), `check` (rejected — imperative), `condition` (rejected — verbose), `predicate` (rejected — academic, violates "English-ish" principle), split constructs (rejected — artificial complexity for identical scope/semantics).
- **Recommendation: `rule`.** Rationale: (1) scope-neutral — serves routing, data truth, and movement truth equally; (2) Precept's README already says "One file, all rules"; (3) C46 already calls these "rule positions"; (4) 4 chars, natural English, declarative syntax; (5) `guard` stays in its proper home — the inline `when` expression in transition rows.
- Updated `expression-feature-proposals.md` Proposal 3, `expression-language-audit.md` L3, and `docs/research/dsl-expressiveness/README.md` — all renamed from `predicate` to `rule`.
- Superseded `frank-guard-reuse.md` (reuse analysis still valid; keyword changes).
- Decision filed at `.squad/decisions/inbox/frank-guard-naming.md`.

### 2026-04-05 - Closeout lanes must follow operational cohesion
- When auditing a dirty worktree for trunk closeout, group files by **one deployable behavior change**, not by author, folder, or “they were touched at the same time.”
- For workflow normalization, the safe trunk lane is the live GitHub automation plus the first-order docs/templates that describe the same lifecycle. Agent histories, research-tree reshuffles, PRDs, mockups, and stray lockfiles are not part of that lane and must not hitchhike.
- A good closeout sequence is: land the enforcement mechanics first, then the operator-facing guidance that matches those mechanics, then bring UX exploration separately after a fresh product/design review.
### 2026-04-05 - Type system survey findings
- Surveyed 6 systems (DMN/FEEL, Cedar, Drools, NRules, BPMN, SQL) for type system evidence. Key findings:
- **`date` is universally present** — all 6 systems have a date type. A business rule DSL without date is incomplete. Day-granularity date-only (no time, no timezone) is the safe v1 choice, validated by Cedar's date-only constructor and SQL's timezone cautionary tale.
- **`enum`/`choice` is table stakes** — Drools has `declare enum`, SQL has `ENUM`, NRules uses C# `enum`. FEEL and Cedar lack it and users work around the absence. Highest-confidence addition.
- **Constructor function pattern is the standard** — FEEL uses `date("2024-03-15")`, Cedar uses `datetime("2024-10-15")`. Precept should follow this pattern if literal syntax is too complex.
- **Duration splits in two** in FEEL and SQL (calendar vs clock), but Cedar uses one (millisecond-based). For Precept's day-granularity model, number-of-days arithmetic is simpler and sufficient — no dedicated duration type needed in v1.
- **Decimal/money appears universally** but is a growth-phase addition: `BigDecimal` (FEEL), `decimal` (Cedar extension), `DECIMAL(p,s)` (SQL). Not a v1 essential — Precept's `number` already handles decimal values.
- **Record/structured types conflict with Precept's flat model** — FEEL has `context`, Cedar has `Record`, but both serve different architectural goals. Precept's rejection of structured types is validated.
- **Proposal #25 is well-calibrated** — `choice` and `date` are the two highest-value additions. The rejections (datetime, structured types, overengineered duration) are validated by the survey.
- Research written to `docs/research/language/references/type-system-survey.md`.
## Language Design Expertise — Deep Study (2026-04-05)

### A. PreceptLanguageDesign.md — Complete Internalization

**5 Goals:**
1. No indentation-based structure — blocks must be explicit and line-oriented. Maps cleanly to Superpower without offside-rule handling.
2. Tooling-friendly — keyword-anchored statements, deterministic parse, predictable IntelliSense. Parser knows statement kind at first token.
3. Keyword-anchored flat statements — every statement begins with a recognizable keyword. No section headers, no indentation.
4. Explicit nullability — `nullable` keyword, not punctuation-based null markers. First-class in type checking.
5. Compile-time-first semantics — catch authoring mistakes early. The compiler proves contradictions; the inspector handles data-dependent impossibility.

**12 Design Principles (with practical implications):**
1. **Deterministic, inspectable model.** Fire/inspect always produces the same result for the same inputs. All validation evaluates against "proposed world" (post-mutation, pre-commit). No hidden state, no side effects.
2. **English-ish but not English.** Keywords like `with`, `because`, `from`, `on` read naturally but don't attempt full sentences. Samples are the tutorial.
3. **Minimal ceremony.** No colons, curly braces, semicolons. `because` is the sentinel. Keyword anchoring replaces punctuation.
4. **Locality of reference.** Rules live near what they describe — invariants near fields, state asserts near states.
5. **Data truth vs movement truth.** `invariant` = static data constraints (always hold). `assert` = movement constraints (checked when something happens).
6. **Collect-all for validation, first-match for routing.** Validation reports every failure. Transition rows evaluate top-to-bottom, first match wins.
7. **Self-contained rows.** Each transition row is independently readable. No shared context with sibling rows.
8. **Sound, compile-time-first static analysis.** Rejects real semantic mistakes early but never guesses. If the checker can't prove a contradiction, it assumes satisfiable.
9. **Tooling drives syntax.** IntelliSense, diagnostics, preview are first-class design constraints. Grammar is friendly to Superpower token/combinator parser.
10. **Consistent prepositions.** `from` = leaving, `to` = entering, `in` = while in, `on` = when an event fires. Same meaning everywhere.
11. **`->` means "do something."** Arrow introduces an action. Separates context from action. Sequential execution, read-your-writes.
12. **AI is a first-class consumer.** Deterministic semantics, keyword-anchored flat statements, structured tool APIs (MCP). The intended workflow: domain expert describes intent, AI authors the precept, toolchain closes the correctness loop.

**12 Deliberate Exclusions:**
1. No indentation-sensitive syntax
2. No punctuation-heavy delimiters (`{...}`, `[...]` for blocks)
3. No lookahead-heavy constructs
4. No section headers (`fields:`, `rules:`)
5. No implicit state machine semantics (auto-advance, timeout)
6. No cross-precept composition (imports, mixins, inheritance)
7. No implicit null handling (`?.`, `??`)
8. No function definitions
9. No ternary expressions (currently — high-impact future feature)
10. No string methods (currently — `.length` is table-stakes gap)
11. No type annotations on literals
12. No computed/virtual fields

Each exclusion removes complexity that Superpower would struggle with (indentation, lookahead) or that requires context-dependent parsing.

### B. Core Pipeline Architecture

**Tokenize → Parse → Type-Check → Assemble → Execute:**
- Phase 1 (Tokenize): `PreceptTokenizerBuilder` → keywords registered via reflection from `[TokenSymbol]` attributes (zero drift). Output: `TokenList<PreceptToken>`.
- Phase 2 (Parse): `PreceptParser` → 8 statement combinators in priority order (EditDecl, EventAssertDecl, StateAssertDecl, TransitionRowParser, StateActionDecl, FieldDecl, InvariantDecl, StateDecl). `AssembleModel` validates structure and calls type checker.
- Phase 3 (Type-Check): `PreceptTypeChecker` → builds symbol tables, traverses expressions, checks types and null-flow. C38–C43.
- Phase 4 (Compile): `PreceptCompiler` → validates graph (reachability, coverage). C48–C53 warnings.
- Phase 5 (Execute): `PreceptEngine.Fire()` → find transition rows, evaluate guards, execute mutations, validate asserts/invariants.

**Phase boundaries:** Each phase owns a specific concern and assumes the prior phase passed. Parser = syntax. Type checker = types/null-flow. Compiler = graph. Runtime = behavior.

**Key constraint codes:** Parse C1–C10, Type-check C38–C43, Compile C25/C46–C53, Runtime C14–C16/C54. Every diagnostic maps to exactly one code.

### C. Superpower Parser — Enables and Constrains

**What it does naturally:**
- Flat token stream parsing — no indentation tracking
- Keyword-led combinators — `Token.EqualTo().Then()` identifies statement kind
- Try-or chains — `.Try().Or()` for disambiguation without committing
- First-match semantics — mirrors transition row evaluation
- Sequential action pipes — LINQ-style `from ... select`
- Expression precedence via mutually recursive combinators

**What it doesn't do:**
- Indentation-aware parsing → no block nesting in Precept
- Unbounded lookahead → grammar stays LL-friendly (LL(2) max)
- Context-sensitive tokens → resolved by try-or branches, not tokenizer feedback
- Semantic actions during parse → validation decoupled into AssembleModel/TypeChecker

**March 2026 redesign:** Replaced regex parser + indentation blocks with flat keyword-anchored Superpower combinators. The language surface was shaped *around* Superpower's strengths.

### D. Keyword-Anchored Principle — Definitive Understanding

**What it means:** Every statement begins with a keyword. Statement kind resolvable by first token or LL(2) lookahead. No indentation tracking. No block balancing.

**What it does NOT mean (per issue #13 reassessment):** Inline keyword suffixes like `min`/`max` on `field` lines are NOT a violation. Shape A (`field Amount as number default 0 min 0 max 10000`) stays on one line, starts with `field`, uses keyword-led suffixes (like existing `as`, `default`, `nullable`), requires no indentation or block balancing.

**The real test:** Does a proposed construct require indentation tracking, block balancing, or context-sensitive disambiguation? If no, it does not violate keyword-anchoring.

### E. Research Library Integration

**Expressiveness studies (6+ libraries):**
- xstate: Closest overlap on `from/on/when`. Precept's flat rows are simpler but lack hierarchical states (intentionally excluded — too costly for domain model).
- Polly: Overlap on `->` action pipeline. Polly's retry/circuit-breaker patterns map to Precept's action chain but with different semantics.
- FluentValidation: Overlap on `invariant`, `in <State> assert`. FluentValidation's `.Must()` chains are more expressive but lose inspectability.
- Zod/Valibot: Overlap on field declarations + `invariant`. Schema-first validation; strongest comparator for field-level constraints (issue #13).
- LINQ: Overlap on mutation/value-selection. Ternary gap is most visible here.
- FluentAssertions: Overlap on `invariant`, `on ... assert`. Chain-style assertions are more readable for complex predicates.
- FEEL/DMN: Strongest external comparator for business-rule DSL expression design. Has ternary, string functions, numeric functions, range membership.
- Cedar (AWS): Strongest counter-precedent. Deliberately omits division and most math for formal analyzability.

**Expression audit highlights:** No ternary (row duplication across 14+ samples), no string `.length` (table-stakes gap), no function calls, no named guards. Current expression surface covers: arithmetic, logical, comparison, contains, collection accessors.

**Verbosity analysis:** Three top smells: event-argument ingestion boilerplate, guard-pair duplication, non-negative constraint boilerplate. Map to proposals #11 (absorb), #12 (else reject), #13 (field constraints).

**Open proposals #8–#18:**
- Wave 1 (highest impact): #10 string `.length`, #8 named rules, #9 ternary
- Wave 2: #11 absorb, #12 else reject, #14 conditional invariants
- Wave 3: #13 field constraints, #15 string `.contains()`, #16 numeric functions
- Future: #17 computed fields
- Rejected: #18 conditional outcome in `->` chain

All proposals are additive and Superpower-compatible. No structural redesign required.
- 2026-05-18: philosophy-driven corpus analysis is more useful than count-driven analysis because it gives the team a refusal test ("does this sample demonstrate prevention, inspectability, or deterministic policy enforcement?") rather than a quota ("we need 7 more to hit 42"). Refusal criteria prevent dilution; quotas invite it.
- 2026-05-17: corpus ceiling is best argued from three converging axes (domain breadth, workflow shape, construct coverage) rather than raw count. All three saturate in the 40-55 range for a focused DSL. Maintenance cost is the hard backstop — past ~55 files, single-feature update passes become unreliable without migration tooling.
- 2026-05-17: sample realism is mostly a semantic-types and policy-density problem, not a sample-count problem. The biggest credibility failures are categorical values, money, and calendar logic forced through string/number workarounds.
- 2026-05-16: keep PRs surgically scoped when Shane asks for targeted metadata changes; local bookkeeping should not hitchhike into those PRs.
- 2026-05-15: GitHub issue hygiene works best when routing labels, taxonomy labels, board status, and close/open state each keep one job.
- 2026-05-01: validate DSL capability claims with MCP tools before writing research, and benchmark new expression ideas against both FEEL and Cedar.
- 2026-04-07: for PRs on dirty branches, commit the relevant work first, push with upstream tracking, and use explicit base/head flags to avoid routing mistakes.
- 2026-04-05: sequence language proposals by containment risk; anything that pressures keyword-anchored flat statements or first-match routing needs extra architectural scrutiny before implementation.
