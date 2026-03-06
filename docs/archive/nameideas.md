# Library Name Ideas

> **ARCHIVED** — The name "Precept" has been selected and locked. This document is retained for historical reference only.

## Core concept to convey

The library enforces that **state and data change together, only through transitions**. Neither can be mutated independently. The `Inspect`-before-`Fire` pattern lets you evaluate a transition before committing it.

---

## Available on NuGet ✓

| Name | Concept fit | Rationale |
|---|---|---|
| **Lockstep** | State and data move in lockstep | Strongest conceptual fit — immediately graspable |
| **Vigil** | Watch before acting (Inspect → Fire) | Short, memorable, ties directly to the inspect-before-fire pattern |
| **Liminal** | "At the threshold" (Latin: *limen*) | Transitions *are* liminal — precise and distinctive; niche but memorable |
| **Precept** | A rule that governs behavior | Precise — the builder defines precepts, the machine enforces them |
| **Bastion** | A defended position; guards protect state | Solid, but slightly generic |
| **Sanctum** | A protected space — only valid transitions may enter | Evocative, but less obviously a library name |
| **Invariant** | The machine guarantees state+data stay consistent | Very precise, but reads as an adjective rather than a product name |
| **Meridian** | A defining line — the transition is the meridian between states | Evocative but loose connection |

## Taken on NuGet ✗

| Name | What occupies it |
|---|---|
| Tether | Registered (unlisted, abandoned — ObservableCollection sync, 2014) |
| Forge | Data mapping/import library |
| Sentry | Sentry error monitoring SDK |
| Gatekeeper | RBAC library |
| Nexum | CQRS/MediatR successor |
| Axiom | 3D rendering engine |
| Traverse | Tree/graph traversal |
| Conduit | Abandoned package |
| Veil | Template renderer/view engine |
| Ratchet | Mobile HTML/CSS/JS components |
| Covenant | SBOM generation tool |

---

## Earlier brainstorming (not yet NuGet-checked)

| Name | Concept fit |
|---|---|
| Pivot | Controlled directional change — clean, action-oriented |
| Phase | Each state is a phase; transitions are phase shifts |
| Helm | Steer through defined paths, can't veer off-course |
| BoundState | Explicitly communicates "data is bound to state" |
| Blueprint | Builder produces a blueprint; instances built from it |
| Mold | One mold, many casts (CreateInstance) |

---

## Top recommendations

1. **`Lockstep`** — strongest conceptual fit; "state and data move in lockstep" maps directly to the library's guarantee; familiar phrase, unique in the ecosystem
2. **`Vigil`** — best feel; short, distinctive, ties to the inspect-before-fire design; clean NuGet namespace
3. **`Liminal`** — most distinctive; developers who know the word love it; those who don't look it up once and remember it forever

---

## The "Precept" Decision (March 2026)

As the project evolved to include data bound to state, atomic mutations, `edit` fields, and most importantly, top-level and field-level `rule` invariants, the original name began to undersell the scope. It is no longer just a state machine—it is a **deterministic entity lifecycle engine**.

We have decided to center the next phase of the project around the name **Precept**.

### What is a Precept?
> *A general rule intended to regulate behavior or thought; a command or principle intended as a general rule of action.*

### The Philosophical Alignment
* **The Engine is a Guardian:** By wrapping state, data, and transitions together into a single cohesive unit, the engine strictly enforces the *precepts* of an entity. 
* **Rules are Precepts:** The introduction of the `rule` keyword (`rule Balance >= 0`) literally defines precepts that cannot be violated, whether moving through a transition or executing a direct data `edit`.
* **Domain Integrity over Plumbing:** The old branding implied low-level infrastructure (nodes, edges, switches). "Precept" elevates the conversation to business domain integrity. The DSL file acts as the comprehensive constitution—the absolute precepts—for an entity's lifecycle.
* **Compliance & Predictability:** The engine's core guarantees—atomic transitions, determinism, and inspect-before-fire—all exist to uphold the defined precepts without side effects.

### Proposed Nomenclature Shift (For Future Migration)
* **Project Name:** `Precept`
* **Core Metaphor:** Developers write the "lifecycle precepts" for an entity (e.g., an Order, a Support Ticket).
* **Namespaces:** `Precept.*` (e.g., `Precept.Runtime`, `Precept.LanguageServer`)
* **Core Types:** `DslMachine` → `PreceptDefinition` or `PreceptMachine`, `DslWorkflowInstance` → `PreceptInstance`.
* **File Extension:** Evaluate if `.precept` should remain, or if a shorter extension like `.prc` is preferable.
