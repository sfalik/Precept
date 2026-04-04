---
name: "reviewer-correction-chain"
description: "When reviewers correct each other's corrections, trace the chain to the terminal state before implementing — never implement an intermediate correction that was itself superseded"
domain: "documentation, code review, technical writing, proposal implementation"
confidence: "high"
source: "earned — README pass 1 implementation 2026-04-08 (Peterman)"
---

# Skill: Reviewer Correction Chain — Tracing to Terminal State

## The Problem

A proposal goes through multiple reviews. Reviewer A flags a required change (RC-1). Reviewer B, reading Reviewer A's correction, identifies that RC-1 itself contains an error and issues their own correction (G1 supersedes RC-1).

The implementer reads both reviews. If they implement RC-1 without reading G1 first, they introduce the error Reviewer A's correction contained.

**Example from README pass 1:**
- Frank's RC-1 specified the correct C# API chain but included `RestoreInstance` as an alternative method.
- George's G1 corrected Frank: `RestoreInstance` does not exist. The correct pattern uses `CreateInstance(savedState, savedData)` — a second overload of the same method.
- Implementing Frank's RC-1 as-written would have introduced a fabricated API name into the README.

## The Pattern

Before implementing any reviewer correction, scan all subsequent reviews for superseding corrections targeting the same section or claim.

**Triage sequence:**
1. Read all reviews fully before implementing any single correction.
2. For each RC, check: "Did any downstream reviewer explicitly correct or supersede this?"
3. If yes: implement the terminal correction, not the intermediate one. Note the chain in your implementation notes.
4. If no downstream correction exists: implement as specified.

## Signal Phrases That Indicate Superseding Corrections

Look for phrases like:
- "Frank's RC-1 contains an error..."
- "I have to correct RC-1 before it propagates..."
- "This is partially wrong..."
- "[Reviewer]'s fix contains an error..."

These phrases signal that a downstream reviewer has read and corrected an upstream correction. They are not advisory — they are corrections to the correction.

## What NOT to Do

- Do not implement corrections in review-file order without checking for downstream supersession.
- Do not "merge" an intermediate correction with a downstream correction by splitting the difference — apply the terminal correction.
- Do not assume a correction is terminal just because it appeared in the most recent review file; check all files.

## Application

This skill applies any time:
- A proposal goes through sequential review rounds
- Multiple reviewers have access to each other's reviews
- The artifact under review requires technical accuracy (API names, method signatures, counts, constraints)

For purely stylistic or structural corrections (heading names, section order), supersession is less common but still possible. Apply the same trace.
