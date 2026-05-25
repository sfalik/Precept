---
reviewer: Frank (Lead/Architect)
date: 2026-05-25
subject: docs/Working/field-never-set-diagnostic.md
verdict: APPROVED with notes
---

# Architectural Review: `FieldNeverSet` + `writable` vs `editable` Naming

## Part 1: `writable` vs `editable` — Findings

### What they are

They are **two distinct concepts at two distinct layers**. No naming inconsistency exists.

| Surface | Keyword | Catalog Entry | Role |
|---------|---------|---------------|------|
| Field declaration modifier | `writable` | `ModifierKind.Writable` (Value Modifier) | Sets the **field-level baseline** — "this field is directly editable by default across all states unless a state-scoped override says otherwise." |
| State-scoped access mode adjective | `editable` | `ModifierKind.Write` (Access Modifier) | Per-state override — "in this state, this field is writable by the caller." |

### The two-layer composition model (from the spec)

1. **Layer 1 — `writable` on the field declaration.** Sets the field's **default** access to editable everywhere. Without it, the field defaults to read-only.
2. **Layer 2 — `in State modify Field editable|readonly`.** State-scoped override. Always wins over the baseline for that (field, state) pair.

So:
- `writable` is a **declaration-time baseline modifier** — it IS a default, exactly as Shane said ("it's not permanent, it can be modified, it's just a default").
- `editable` is a **state-scoped access-mode adjective** in the `modify` construct.

They operate on different grammatical positions, different catalog categories (Value Modifier vs Access Modifier), and different lifecycle scopes. The names are **deliberately different** because they describe different things.

### Why not name them the same?

If both were `editable`, you'd get grammar ambiguity:
```
field Amount as decimal editable        ← field declaration? modify construct?
in Draft modify Amount editable         ← fine
```

More importantly, `writable` carries the semantics "this field's **baseline** is writable" — a permanent declaration property that can be overridden per-state. `editable` carries the semantics "in this state, this field is writable" — a localized access-mode fact. Different scopes warrant different words. This is correct design.

### Is the design doc using `editable` correctly?

**Yes.** The doc says `modify Field editable` is a write site. This refers to the state-scoped `in State modify Field editable` construct — the access mode adjective. This is semantically correct: declaring a field `editable` in some state grants the caller write access via the runtime API, which constitutes a write capability.

The doc does NOT confuse `writable` with `editable`. It never claims the `writable` declaration modifier is a write site (nor should it — `writable` only sets the baseline, it doesn't write the field). The write site comes from the `editable` access-mode declaration opening a mutation window.

### Does `writable` itself count as a write site?

**No, and correctly so.** `writable` without a state-scoped `modify` declaration means the field is editable *by default* in all states — but this only matters if the runtime caller *actually calls Update on it*. The analyzer should treat `modify Field editable` as the write site, not `writable`, because:
- `writable` is universal (it's the default for the field everywhere)
- A `writable` field with no `modify` declarations, no `set` actions, and no event args SHOULD still trip `FieldNeverSet` — because the field-level baseline alone doesn't guarantee any caller will actually write it. The fact that it CAN be written isn't a write site; the fact that a state explicitly declares it editable IS one.

**Wait.** This introduces a subtlety: if a field is `writable` (baseline editable everywhere) but has NO explicit `modify` declarations and no internal write sites, should `FieldNeverSet` fire?

The answer should be **yes** — and the current design doc is correct in not listing `writable` as a write site. A `writable` baseline without any explicit `modify editable` declaration still allows caller writes in every state, but the same is true of a field without `writable` if a `modify Field editable` appears. The analyzer's job is to detect "no write site exists" — and `writable` alone doesn't constitute a write site because it's a baseline declaration, not an operational access grant.

**However** — this creates a false positive scenario the design doc does not address: a `writable` field with no `modify` declarations and no internal writes is still callable-writable in every state via the runtime API. The caller CAN write it. The `FieldNeverSet` would fire, but the field IS reachable by external writes.

**This is the one issue I'm flagging.** See B1 below.

---

## Part 2: Review of `docs/Working/field-never-set-diagnostic.md`

### Verdict: APPROVED with one blocker (B1) that requires a decision addition

---

### B1: `writable` baseline as implicit write site — BLOCKER

The design doc correctly lists `modify Field editable` as a write site (Decision 4). But it does **not** address the `writable` modifier on the field declaration itself.

A field declared `field Amount as decimal writable` — with no `modify` declarations, no `set` actions — is callable-writable in EVERY state via the runtime API. The `writable` baseline grants universal write access without any state-scoped `modify` declaration.

The design must make an **explicit decision** about whether `writable` is a write site:
- **If yes:** Add it to the write-site list. No false positive on `writable` fields.
- **If no (recommended):** `writable` alone is not a write site because it represents *capability* without *intent*. A field that's universally writable but never internally governed, never set, never exposed by a specific `modify editable` declaration is likely an authoring oversight. The diagnostic fires. Add a note explaining why `writable` is NOT a write site (same reasoning as: "a lock that's never locked isn't a security mechanism just because it exists").

My recommendation: **`writable` IS a write site.** Rationale: the two-layer model treats `writable` as "this field is editable everywhere" — which is semantically equivalent to `in every_state modify Field editable`. If the explicit per-state form counts as a write site, the field-level baseline that means the same thing globally should too. The author has declared intent: "callers can write this field." That intent suppresses the diagnostic.

Add Decision 5 to address this.

---

### G1: Decision 1–4 are well-structured

All four decisions carry proper four-leg rationale. The `FieldNeverSet` / `FieldNeverRead` split is the right call — different confidence surfaces, different suppression stories, different design conversations.

### G2: Write-site shapes are complete (pending B1)

The seven shapes listed in the analyzer description cover the full internal write surface. With B1 resolved, the external write surface is also covered.

### G3: Acceptance criteria are test-shaped and verifiable

Each criterion maps to a specific test or check. The parameterized negative test covering all write-site shapes is the correct pattern.

### G4: Doc-update enumeration is thorough

The routing table correctly identifies which docs need updates and which don't. The MCP note ("verify formatter handles it automatically") is the right level of specificity for a catalog-driven system.

### G5: Corpus sweep obligation is clear

The "fix every sample before merge" requirement prevents the diagnostic from shipping with known noise. Good.

### G6: Diagnostic message template is appropriate

`"Field '{0}' has no write site — it can only hold its declared default or remain unset"` — clear, actionable, no jargon. Consistent with the catalog's existing message style.

### G7: Stage placement is correct

Graph stage, after reachability/completeness, is exactly right. The analyzer needs whole-program visibility and type-checked references. No earlier stage has both.

---

## Resolution Required

**B1 must be resolved** before implementation begins. Add Decision 5: "The `writable` modifier on a field declaration counts as a write site (or does not), because [rationale]."

Once B1 is resolved with a decision addition, this design is clear to implement.
