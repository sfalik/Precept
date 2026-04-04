# External Research: Hero Code Snippet Length

## Analysis Summary

This research examined the primary hero code examples from 10 comparable DSL, rule engine, state machine, and validation libraries, fetched directly from their official GitHub README or home page.

## Findings Table

| Project | Category | Hero Snippet Lines | Meaningful Statements | Notes |
|---------|----------|-------------------|----------------------|-------|
| **xstate** | State machine (JS) | 16 | 8 | Super-quick-start example; createMachine config + actor usage |
| **Mermaid** | Diagram DSL (JS) | 5 | 5 | Flowchart example; minimal keyword-driven syntax |
| **Fluent Validation** | Rule DSL (C#) | 16 | 11 | Class-based validator with 4 RuleFor declarations |
| **Stateless** | State machine (C#) | 10 | 5 | Configure phone call states with 3 Permit transitions |
| **NRules** | Rule engine (C#) | 0 | 0 | No code example in README; only docs links |
| **AutoMapper** | Convention DSL (C#) | 18 | 8 | MapperConfiguration + two CreateMap + one Map call |
| **Zod** | Schema validation (TS) | 9 | 4 | z.object with 2 fields, parse call |
| **Pydantic** | Data validation (Python) | 10 | 5 | BaseModel subclass with 4 fields, instantiation |
| **Joi** | Schema validation (JS) | 0 | 0 | README truncated; only links provided |
| **Polly** | Resilience DSL (C#) | 7 | 3 | ResiliencePipelineBuilder chain with AddRetry + AddTimeout |

## Statistical Analysis

| Metric | Min | Max | Median | Mean |
|--------|-----|-----|--------|------|
| **Lines** | 5 | 18 | 10.5 | 11 |
| **Meaningful statements** | 3 | 11 | 5.5 | 6 |

**Note:** NRules and Joi are excluded from statistics due to missing code examples in their README hero sections.

## Key Observations

1. **Range:** Hero snippets across comparable projects span 5–18 lines of code, with a median of ~10–11 lines.

2. **Statement density:** Meaningful statements (rules, declarations, assignments) range from 3–11, with a median of ~5–6 statements.

3. **Whitespace agnostic:** Most projects do not count blank lines, braces, or comments as "statements." They count only executable, domain-relevant declarations.

4. **C# examples tend longer:** FluentValidation (16L, 11S) and AutoMapper (18L, 8S) showcase more context and setup than simpler validation DSLs.

5. **Compact DSLs shine short:** Mermaid (5L, 5S), Polly (7L, 3S), and Zod (9L, 4S) demonstrate that keyword-heavy or fluent APIs can express substantial intent in minimal code.

6. **Declarative DSLs cluster mid-range:** Most declarative or fluent examples land in the 9–16 line range, suggesting a "sweet spot" for readability and impact.

## Recommendation for Precept

Based on **external benchmark data alone**, Precept's hero code snippet should target:

- **Line ceiling: 12–15 lines** (just above the 10.5-line median)
- **Statement ceiling: 6–8 meaningful statements** (just above the 5.5-statement median)

This positions Precept's hero example as professional, complete, and visually substantial—without sacrificing clarity or forcing readers to scroll.

## Justification

Precept ignores whitespace and emphasizes statements as the unit of expressiveness. Given that:

1. The external benchmark median is **10.5 lines** and **5.5 statements**
2. Precept's hero must demonstrate **state definition, initial state, state transitions, and events**—a richer contract than a single rule or validation schema
3. A complete, compelling example of Precept (vs. a toy one-liner) requires showing the interplay of declarations, not just grammar

A **12–15 line, 6–8 statement** ceiling is defensible: it remains within the external benchmark range (not an outlier), places Precept at the higher end of expressiveness (reflecting its richer domain model), and still fits in a typical markdown viewport without scrolling.

**The right gate is: *any hero example that exceeds 15 lines or 8 statements should justify why the additional complexity is necessary to illustrate Precept's core value proposition.*

