---
name: "pr-review"
description: "How to produce and post structured PR reviews with inline code comments via squad-reviewer[bot]. Covers the full review conversation lifecycle: initial review, dev fix replies, re-review verification, and thread resolution — mirroring how humans use GitHub."
domain: "code review, design review, PR review"
confidence: "high"
source: "earned — PR #68 review cycle 2026-04-12; established squad-reviewer GitHub App, structured JSON format, and conversational review lifecycle"
---

# Skill: PR Review — Conversational Review Lifecycle

## The Problem

AI agent reviews that create new, disconnected threads for re-review don't work like human reviews. Humans comment, devs reply with fixes, reviewers verify and resolve — all in one conversation thread. Bots should work the same way.

## The Pattern

**Every PR review follows a three-phase conversation lifecycle that mirrors how humans use GitHub:**

1. **Initial review** — Reviewers post `REQUEST_CHANGES` with inline comments on specific files and lines.
2. **Dev fix** — Devs fix the code, push, and **reply** to each review thread explaining the fix.
3. **Re-review** — Reviewers verify by **replying** to the same threads, **resolve** satisfied threads, and submit `APPROVE`.

All interactions happen within the same review thread — no orphaned duplicate threads.

## Phase 1: Initial Review

Reviewers produce a **structured JSON file**. The coordinator posts it via the review script.

```json
{
  "event": "REQUEST_CHANGES",
  "body": "## {emoji} {AgentName} — {Review Type}\n\n**BLOCKED** — {one-line verdict}\n\n### Blockers\n\n**B1:** ...\n\n### Warnings (non-blocking)\n\n**W1:** ...\n\n### What's strong\n\n**G1:** ...",
  "comments": [
    {
      "path": "src/Precept/Dsl/FunctionRegistry.cs",
      "line": 42,
      "body": "**B1:** Explanation of what's wrong and what to fix."
    }
  ]
}
```

### Comment Requirements

| Field | Required | Description |
|-------|----------|-------------|
| `path` | Yes | File path relative to repo root (forward slashes) |
| `line` | Yes | Line number in the PR's HEAD commit — must be in the diff |
| `body` | Yes | Markdown. Prefix: `B{N}:` blockers, `W{N}:` warnings, `G{N}:` positive callouts |

### Rules

1. **Every blocker MUST have an inline comment** on the relevant file and line.
2. **Warnings SHOULD have inline comments** when they reference specific code.
3. **Line numbers must be accurate.** Read the actual file first — don't guess.
4. **Use `REQUEST_CHANGES`** when any blocker exists. `APPROVE` only when all blocking criteria pass.

### How to Post (Initial Review)

```
node tools/scripts/squad-review.js <pr-number> <review-file.json>
```

## Phase 2: Dev Fix Response

After fixing the code and pushing, the coordinator replies to each original review thread explaining what was fixed. This keeps the conversation in one place.

### Coordinator Workflow

1. Run `node tools/scripts/squad-review.js <pr-number> threads --unresolved` to list open threads.
2. Map each thread's `commentId` to the fix that addresses it.
3. Construct a reply JSON and post it.

### Reply JSON Format (Dev Fix)

```json
{
  "replies": [
    {
      "commentId": 123456,
      "body": "Fixed in commit `abc1234`. Added `RequiresNonNegativeProof` case to `FormatArgConstraint`."
    },
    {
      "commentId": 789012,
      "body": "Fixed in commit `abc1234`. Added `TypeChecker_C72_MinSingleArg` and `TypeChecker_C72_MaxSingleArg` tests."
    }
  ]
}
```

No `event` or `body` — just replies. No review is submitted; this is the dev responding.

### How to Post (Dev Fix)

```
node tools/scripts/squad-review.js <pr-number> <fix-reply.json>
```

## Phase 3: Re-Review and Resolve

Reviewers verify fixes by replying to the same threads. Satisfied threads are resolved. An `APPROVE` review is submitted alongside the replies.

### Reply JSON Format (Re-Review)

```json
{
  "event": "APPROVE",
  "body": "## {emoji} {AgentName} — Re-Review\n\n**APPROVED** — All blockers resolved.",
  "replies": [
    {
      "commentId": 123456,
      "body": "✅ **B1 — Verified.** RequiresNonNegativeProof now serialized correctly.",
      "resolve": true
    },
    {
      "commentId": 789012,
      "body": "✅ **B3 — Verified.** Both min/max single-arg C72 tests present and correct.",
      "resolve": true
    }
  ],
  "comments": [
    {
      "path": "src/Precept/Dsl/FunctionRegistry.cs",
      "line": 55,
      "body": "**B4:** New finding during re-review — this overload is unreachable."
    }
  ]
}
```

- `replies`: Respond to existing threads. `resolve: true` resolves the thread.
- `comments`: New inline comments for issues discovered during re-review. Same format as initial review comments.
- `event` + `body`: Submits the review. New comments are attached to this review.
- If new blockers are found, use `REQUEST_CHANGES` with the new comments, resolve the satisfied threads, and leave unsatisfied threads open.

### How to Post (Re-Review)

```
node tools/scripts/squad-review.js <pr-number> <rereview.json>
```

The script posts all replies first, resolves marked threads, then submits the review.

## Listing Threads

The coordinator lists existing review threads to map findings to comment IDs:

```
node tools/scripts/squad-review.js <pr-number> threads [--unresolved]
```

Output (JSON array):
```json
[
  {
    "threadId": "PRRT_...",
    "commentId": 123456,
    "path": "tools/Precept.Mcp/Tools/LanguageTool.cs",
    "line": 134,
    "body": "**B1:** FormatArgConstraint returns null for RequiresNonNegativeProof...",
    "author": "squad-reviewer",
    "createdAt": "2026-04-12T14:59:02Z",
    "isResolved": false
  }
]
```

Use `--unresolved` to filter to open threads only.

## Review Types

This skill applies to ALL review types. The format is the same; the criteria differ by role:

### Code Review (Frank — Lead/Architect)

- Doc accuracy vs. implementation
- Diagnostic message correctness
- Grammar sync (TextMate grammar matches parser)
- Completions/hover sync
- MCP sync (DTOs match core types)
- Dead code scan
- Design philosophy compliance

### Test Review (Soup Nazi — Tester)

- AC-to-test matrix (every behavioral criterion has a test)
- Test quality (FluentAssertions, edge cases, error paths)
- Disabled test scan
- CatalogDriftTests coverage
- Cross-surface drift (tests across all 3 test projects)

### Design Review (Frank — facilitated)

- Philosophy filter
- Impact assessment (runtime, tooling, MCP)
- Rationale completeness
- Precedent grounding
- Inline comments on specific doc sections

## Spawn Prompt Additions

### Initial Review Spawn

```
REVIEW OUTPUT FORMAT: You MUST output a JSON object (not Markdown) with this structure:
{
  "event": "REQUEST_CHANGES" | "APPROVE" | "COMMENT",
  "body": "## {emoji} {YourName} — {Review Type}\n\n**{APPROVED|BLOCKED}** — ...",
  "comments": [
    { "path": "relative/file/path.cs", "line": <number>, "body": "**B1:** ..." }
  ]
}

Every blocker MUST have an inline comment with the exact file path and line number.
Read the actual files to determine correct line numbers — do not guess.
```

### Re-Review Spawn

When spawning a reviewer for re-review, include the existing thread data so they can verify each one:

```
RE-REVIEW: You are verifying fixes for blockers from a previous review.
The following review threads exist on the PR (unresolved):

{thread list from `threads --unresolved` output}

For EACH thread, verify whether the fix is satisfactory. Also do a fresh pass —
if you find NEW issues not in the original threads, add them as new comments.

Output a JSON object:
{
  "event": "APPROVE" | "REQUEST_CHANGES",
  "body": "## {emoji} {YourName} — Re-Review\n\n**{APPROVED|BLOCKED}** — ...",
  "replies": [
    { "commentId": <from thread data>, "body": "✅ B1 — Verified. ...", "resolve": true },
    { "commentId": <from thread data>, "body": "❌ B2 — Still broken. ...", "resolve": false }
  ],
  "comments": [
    { "path": "relative/file.cs", "line": <number>, "body": "**B4:** New finding..." }
  ]
}

Use resolve: true for satisfied threads, resolve: false for unresolved issues.
Include "comments" only if you find NEW issues not covered by existing threads.
Use APPROVE only when ALL threads can be resolved AND no new blockers found.
REQUEST_CHANGES if any thread remains open or any new blocker is added.
```

## Coordinator Workflow (Complete Lifecycle)

### After Initial Review

1. Parse JSON from reviewer agent's response.
2. Write to `temp/{agent-name}-review.json`.
3. Run `node tools/scripts/squad-review.js <pr> temp/{agent-name}-review.json`.
4. Report results to user.

### After Dev Fix

1. Run `node tools/scripts/squad-review.js <pr> threads --unresolved` to list open threads.
2. Map each fix to its thread's `commentId`.
3. Construct reply JSON with fix explanations (no event/body — just replies).
4. Write to `temp/{agent-name}-fix-reply.json`.
5. Run `node tools/scripts/squad-review.js <pr> temp/{agent-name}-fix-reply.json`.

### After Re-Review

1. Run `node tools/scripts/squad-review.js <pr> threads --unresolved` to get current threads.
2. Spawn reviewer with thread data in prompt.
3. Parse reviewer's reply JSON output.
4. Write to `temp/{agent-name}-rereview.json`.
5. Run `node tools/scripts/squad-review.js <pr> temp/{agent-name}-rereview.json`.
6. Confirm threads resolved and APPROVE posted.

## History

- **2026-04-12:** Established during PR #68 review cycle. GitHub blocks `REQUEST_CHANGES` on own PRs, so we created the Squad Reviewer GitHub App (`squad-reviewer[bot]`). Structured JSON format with inline comments adopted as the standard.
- **2026-04-12 (v2):** Upgraded to conversational lifecycle. Reviews now follow a three-phase pattern: initial review → dev fix reply → re-review with resolve. All interactions happen in the same thread, mirroring how humans use GitHub. Script extended with `threads`, `reply`, and `resolve` support.
