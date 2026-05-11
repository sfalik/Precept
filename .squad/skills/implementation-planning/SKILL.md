# Skill: Implementation Planning

**Confidence:** high
**Domain:** implementation planning, vertical slice design, PR body authoring

## What this skill covers

How to build a detailed, actionable implementation plan for a Precept feature PR — from issue research through codebase exploration to vertical slice decomposition. This skill covers the **planning process**; for PR body structure and checklist formatting, see `.squad/skills/pr-implementation-plan/SKILL.md`.

## Relationship to CONTRIBUTING.md

This skill operationalizes the [Implementation Plan Quality Bar](../../CONTRIBUTING.md#implementation-plan-quality-bar) section of CONTRIBUTING.md. That section defines the required elements; this skill defines the workflow for producing them.

## When to use

- When Frank (or any agent) is building an implementation plan for a new feature PR
- When a plan has been rejected for insufficient detail
- When onboarding a new squad member to the planning process

## Quality bar exemplar

PR #108 (`feat: compile-time divisor safety via unified narrowing`) is the reference implementation for this skill. It demonstrates all required elements at the expected depth.

## Planning workflow

### Phase 1: Issue research (no assumptions)

1. **Read the full issue body.** Extract: summary, design decisions, acceptance criteria, test plan, impact assessment, resolved questions.
2. **Read ALL comments.** Issue comments contain implementation notes, scope additions, additional tests, ordering constraints, and reviewer feedback that amend the body. Treat the full comment thread as part of the spec.
3. **Build a scope inventory** from both sources. Track each item's origin (body vs. comment N) so nothing is lost.

Common comment types that add scope:
- "Implementation note — ..." (code-level guidance: case ordering, filtering patterns, caching)
- "Additional scope — ..." (new items added after initial review)
- "Additional test — ..." (specific test cases from reviewers)
- "Regression anchor exact method names" (test names that must pass unchanged)

### Phase 2: Codebase exploration

Before writing any plan, locate the exact code that will change:

1. **Find every file involved.** Use search tools to locate the methods, classes, and line numbers referenced in the issue.
2. **Read the relevant code.** Understand the current structure — method signatures, call sites, data flow. A plan built on assumptions about code structure will be wrong.
3. **Identify integration points.** Where does new code wire into existing code? What line numbers? What methods call what?
4. **Identify regression risk.** Which existing tests exercise the code being changed? These become regression anchors.

Key things to locate:
- Methods to modify (with line numbers and surrounding structure)
- Methods to create (where they fit in the file, what they parallel)
- Test files and existing test method names for regression anchors
- Diagnostic catalog (next available code, registration pattern)
- Any infrastructure the new code will reuse (helpers, patterns, conventions)

### Phase 3: Vertical slice decomposition

Organize the work into slices. Each slice is a coherent unit that can be implemented, tested, and verified independently.

**Slice design principles:**
- **Each slice is testable.** It includes its own tests — not "implement in slice 3, test in slice 8."
- **Each slice has a clear boundary.** "Create method X, modify method Y, add tests A/B/C" — not "work on narrowing."
- **Dependencies are explicit.** If Slice 2 needs Slice 1's infrastructure, say so and say why.
- **Regression anchors live with the slice that creates risk.** If Slice 3 replaces existing behavior, Slice 3 lists the tests that must pass unchanged.

**Slice anatomy (required per slice):**

```markdown
### Slice N — {descriptive title}

{1-2 sentence summary of what this slice delivers and why it matters.}

**Create:**
- `MethodName(params)` — {purpose} (~N lines) in `path/to/File.cs`. {Brief description of logic.}

**Modify:**
- `ExistingMethod()` (~LN–LN) — {what changes and where in the method.}

**Tests (in `path/to/TestFile.cs`):**
- {Test description} — `[Fact]` or `[Theory]` with N rows
- {Test description} — what it verifies

**Regression anchors:**
- `ExactTestMethodName` — {why it's at risk}

**Files:** `file1.cs`, `file2.cs`

- [ ] Checkbox 1
- [ ] Checkbox 2
```

### Phase 4: Plan-level elements

After all slices are defined, add these plan-level sections:

1. **Ordering constraints** — A narrative section explaining the dependency graph. "Slice 1 must be first because...", "Slices 3 and 4 can be parallelized because..."
2. **File inventory table** — Every file mapped to the slices that touch it.
3. **Tooling/MCP sync assessment** — Per-category statement: syntax highlighting, completions, semantic tokens, MCP. Either "changes needed" with specifics or "no changes needed" with reasoning.

## Common mistakes

| Mistake | Fix |
|---------|-----|
| Plan says "modify type checker" without naming methods | Name the method, the file, and the line number or structural landmark |
| Tests grouped as a separate slice at the end | Tests belong in the slice that creates the behavior they verify |
| Missing regression anchors for refactored code | Any slice that replaces existing behavior must list exact test method names that must pass unchanged |
| Plan built without reading issue comments | Comments contain scope additions, implementation notes, and test requirements. Read all of them. |
| Slice dependencies implied but not stated | Write an explicit ordering constraints section |
| "No tooling changes needed" without justification | Explain WHY no changes are needed (e.g., "no new keywords, operators, or syntax forms") |

## Relationship to other skills

- **`pr-implementation-plan`** — covers PR body structure and checklist formatting. This skill covers the planning *process* that produces those checklists.
- **`proposal-review`** — the design review ceremony that produces the issue content this skill consumes.
- **`pr-review`** — reviewers use the plan to scope their review. A good plan makes review faster.
