## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.
- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings.
- Historical summary: established the sample-file promotion gate, large parser coverage matrices, analyzer-harness expectations, and the rule that spec/catalog changes are not complete until regression anchors exist.

## Learnings

- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.
- Hardcoded enum/count assertions are acceptable regression anchors only when they are intentionally updated alongside catalog growth.
- Expression-level diagnostics often need a full parser host (rule/transition row) instead of the bare expression helper.
- DSL keywords are common identifier traps in tests; use non-keyword names for fields/events unless the test is explicitly about keyword handling.
- Multi-source analyzer tests only need a small harness extension; the rest of the analyzer test shape can stay minimal.
- For operation analyzers, use `ctx.Operation.SemanticModel` (nullable) rather than APIs available only on `SyntaxNodeAnalysisContext`.

## Recent Updates

### 2026-05-01T20:36:28Z — Full review gaps closed and branch green
- The full coverage review is now durably recorded end-to-end: the initial review found 6 missing tests, and Soup-Nazi-4 landed all 6 (M1-M6) plus the RS1030 follow-on fix.
- Validation closed green at 2687 passing tests across the branch; no skipped tests were introduced.
- Treat the coverage review as resolved, not merely advisory: the missing test matrix is now implemented.

### 2026-05-01 — 6 coverage gaps from full review implemented (M1-M6)
- Wrote the six missing tests for postfix arity, multi-token operator token sequences, postfix round-trip coverage, PRECEPT0020 MultiTokenOp skip behavior, chained `is set` behavior, and postfix precedence.
- Fixed the PRECEPT0013 follow-on issue by switching to `ctx.Operation.SemanticModel` with a null-guard.
- Branch validation finished green with no new failures.

### 2026-05-01 — Full-surface review summary compressed
- Historical branch review work already established the sample integration gate, parser remediation anchors, whitespace-insensitivity regression layer, and the broad gap matrix that drove the spike's later test additions.
- Earlier detailed notes were summarized here to keep the active history compact while preserving durable testing patterns and the branch's executed outcomes.
