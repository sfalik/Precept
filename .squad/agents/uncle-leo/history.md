## Core Context

- Owns editorial/code review with emphasis on implementation honesty, clarity, and redundancy removal.
- Reviews should verify claims against code, samples, and tests rather than accept narrative at face value.
- Hero snippets and README examples must be compact, compile-credible, and free of decorative language that obscures the product claim.

## Recent Updates

### 2026-04-08 - Initial security survey: Phase 1 + Phase 2 complete

**Phase 1 — Internal codebase survey findings:**
- Product: AI-facing domain integrity engine for .NET. MCP server (stdio transport, 5 tools) + LSP language server. No HTTP endpoints, no file I/O, no process spawning, no reflection-based type loading.
- Primary trust boundaries: `text` parameter (unbounded DSL string), `data`/`fields`/`args` dictionaries (unvalidated key-value maps), MCP tool output (echoes raw user-supplied content), LSP document text.
- Top risks found: (1) No input size limits on `text` — DoS via unbounded parsing; (2) Error messages and violation DTOs echo raw identifier names and `because` clauses — indirect prompt injection via MCP output; (3) No logging of any kind in MCP server; (4) `data`/`fields` dicts accept unknown keys with no allowlisting.
- Positive: No file I/O, network calls, process spawning, or dangerous deserialization patterns. System.Text.Json with JsonElement is safe.

**Phase 2 — External resources fetched:**
- OWASP Input Validation Cheat Sheet (input bounds, allowlisting)
- OWASP LLM01:2025 Prompt Injection (indirect injection via output)
- OWASP LLM Top 10 Project page (context and scope)
- OWASP MCP Security Cheat Sheet (directly applicable — new resource, highest relevance)
- OWASP Deserialization Cheat Sheet (confirmed safe .NET pattern)
- OWASP Logging Cheat Sheet (no logging present — gap identified)
- Microsoft .NET Security and User Input (range checking guidance)

**Output written:** `docs/research/security/security-survey.md`

### 2026-04-05 - Consolidation safety gate recorded
- Rejected direct merge, force-repoint, blind squash, and docs-only cherry-pick for the unrelated-history return to trunk.
- Confirmed the health checks were green at review time (dotnet build; dotnet test --no-build, 703/703) and approved only a freeze-and-curate cutover from a frozen SHA.

## Learnings

### 2026-04-18 - Currency/Quantity/UOM design security review

**Design-stage review patterns — what to check in a DSL type extension:**
- Verify every new string-backed type has an explicit runtime validator spec: normalization order (trim→length→charset→allowlist), not just "validated against registry."
- Decimal backing (D12 equivalent) is the right mandate for financial types — flag any design that allows `double` anywhere in the arithmetic pipeline.
- Interpolation into structured strings (unit/currency expressions) is an injection surface. The fix is always "validate the leaf value BEFORE it enters the compound expression, not after."
- "No external library dependency" for registries is a positive security property — static tables are easier to audit than dynamic lookups.

**Key threat patterns for the currency/quantity domain:**
- Decimal precision attacks: `decimal.MaxValue` and 28-digit-precision values are the adversarial boundary inputs for financial types.
- Unit string injection: the attacker's lever is a `unitofmeasure` field set to a value containing `/` that then gets interpolated into a compound `in '...'` constraint. Closed at design time by allowlist-only validation.
- Overflow in compound arithmetic (`price * quantity → money`): `decimal` overflow throws `OverflowException` — must be caught at the evaluation boundary, not propagated.
- Issue #115 (`double` intermediate) is not just a precision bug — it is a trust-boundary violation for a financial integrity engine. Frame it as a security prerequisite in design docs.

**Prompt injection extension to financial domain:**
- `because` clauses and rejection messages in `money`-domain rules are high-trust signals to AI consumers ("payment rejected," "currency mismatch"). These are high-value injection surfaces that need output tagging (user-supplied vs system-generated).

**Positive patterns to record and reuse:**
- Closed entity-scoped registries (compile-time fixed set) eliminate open-ended string injection before it reaches the parser.
- Level C (multi-term compound units) permanently excluded = entire class of parsing complexity attacks eliminated by scope boundary.
- Mutual exclusivity (`in` XOR `of`) as a compile error = prevents guard bypass via ambiguous constraint state.
- Cross-currency arithmetic as a compile error (D11) = prevention, not detection, applied correctly.

**Verdict approach for design-stage reviews:**
- APPROVED WITH CONDITIONS is the right call when: no Critical blockers, but gaps exist that would be expensive to discover at code review time. Require the gaps be closed in the design doc before implementation starts.
- Two High findings (Issue #115 framing, unit injection spec gap) were design-doc gaps, not code bugs — both can and should be fixed before a single line of implementation is written.

### 2026-04-18 - Currency review batch consolidation

- Frank's architectural blockers and George's runtime caveats both reinforce the same security posture: design contracts must be explicit before code exists, especially for cancellation semantics and registry-backed structured values.
- Newman narrowed the MCP decision to string-versus-object compound transport. From a security perspective, the simpler typed-string form remains the lower-risk default until object ingestion is deliberately hardened.
- Soup Nazi's blockers line up with the security view: Issue #115 and the duration/days boundary are not edge polish; they are correctness and trust-boundary concerns.
- Net requirement: treat validator normalization order and decimal exactness as pre-implementation design obligations, not follow-up cleanup.
