---
name: "test-strategy"
description: "Domain knowledge skill for Precept testing. Covers where the test corpus and sample files live, how to capture test patterns, how to use corpus data in test design, and how to keep coverage current."
domain: "testing"
confidence: "high"
source: "earned — generalized from language-design audit; ensures test work is grounded in the real corpus, not synthetic examples"
---

## Context

This skill governs how agents work with the **test corpus and sample files** — using real DSL patterns as the source of truth for test design, capturing reusable test strategies, and keeping coverage aligned with what the language actually does.

**Applies to:** Any agent doing test-related work — test case design, coverage analysis, regression risk assessment, acceptance criteria, or validating DSL behavior. Primary user: Soup Nazi (Tester). Also: George (runtime tests), Kramer (language server tests), Newman (MCP tests).

## Research Location

### Test projects

| Project | Path | Covers |
|---------|------|--------|
| Core tests | `test/Precept.Tests/` | Parser, type checker, expression evaluator, runtime engine |
| Language server tests | `test/Precept.LanguageServer.Tests/` | Completions, semantic tokens, diagnostics, preview |
| MCP tests | `test/Precept.Mcp.Tests/` | MCP tool integration tests |

### Sample files (the real-world corpus)

| Path | Contents |
|------|----------|
| `samples/` | 24 canonical `.precept` files — the ground truth for DSL behavior |

These are not test fixtures — they are real business-domain definitions. Use them as the source of realistic DSL patterns when designing test cases.

### Corpus data

| Source | Path | Contents |
|--------|------|----------|
| Verbosity analysis | `research/language/expressiveness/internal-verbosity-analysis.md` | Statement counts per sample, construct breakdowns, verbosity smell rankings, gate metrics |
| Language spec | `docs/PreceptLanguageDesign.md` | Expected behavior — boundary conditions, semantics |
| Runtime API spec | `docs/RuntimeApiDesign.md` | API contracts: input types, output shapes, error conditions |

### Test conventions (from project)

- Framework: **xUnit** with **FluentAssertions**
- Naming: `PascalCase` + `Tests` suffix
- Attributes: `[Fact]` and `[Theory]`

## Using Research in Work

### Before designing test cases

1. **Read the relevant samples** — don't invent synthetic DSL from general knowledge
2. Use the **verbosity analysis** to identify which samples exercise the most of a given construct
3. Read the **language spec** for boundary conditions and expected semantics
4. Check the **existing tests** for patterns already covered — avoid duplication
5. **Cite sample-specific data** when it motivates a test case

### Citation standard

| Acceptable | NOT acceptable |
|---|---|
| "loan-application.precept has 5 invariant statements, 3 enforce nonnegative on numeric fields" | "Some samples have numeric constraints" |
| "Per internal-verbosity-analysis.md: crosswalk-signal has 29 statements, closest to the 6-8 gate" | "Samples are verbose" |
| "PreceptParserTests.cs has 42 guard-expression tests covering binary operators" | "Guard expressions are tested" |
| "Per PreceptLanguageDesign.md §Invariants: invariants run on every transition, not just specific events" | "Invariants are always checked" |

**Rule:** If a claim could be made by any QA engineer without reading the test corpus, it is not a citation.

## Capturing New Research

### Where to put it

| Type of finding | Location |
|-----------------|----------|
| New test case | Appropriate test project (`test/Precept.Tests/`, etc.) |
| Test strategy / coverage analysis | `.squad/skills/test-strategy/` (update this skill) or `docs/` if it's a project-level testing plan |
| New sample file | `samples/{name}.precept` — must follow DSL authoring skill |
| Updated verbosity data | Update `research/language/expressiveness/internal-verbosity-analysis.md` |

### Test design from samples

When a new language feature ships:
1. Identify which sample files would exercise it (use verbosity analysis for targeting)
2. Write tests using patterns from those samples — not synthetic DSL
3. Note the sample file in the test's documentation or naming

## Maintaining Existing Research

- **When samples change:** Re-check verbosity counts; update the analysis if counts shift
- **When the language spec changes:** Audit existing tests for stale boundary conditions
- **When test counts change significantly:** Note the new count for future reference
- **When a feature ships:** Verify acceptance tests exist and are green; update coverage gaps
