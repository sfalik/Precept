# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. Category creation play — "domain integrity engine" positioning.
- **Stack:** C# / .NET 10.0, TypeScript, DSL
- **My domain:** `README.md`, `brand/`, `docs/`, NuGet/VS Code/Claude marketplace copy
- **Brand decisions (locked):** Deep indigo `#6366F1`, Cascadia Cove font, small-cap wordmark, category-creator narrative. All locked in `brand/brand-decisions.md`.
- **Voice:** Authoritative with warmth. No hedging, no hype. Matter-of-fact with clarity.
- **Positioning:** "domain integrity engine" — category creator, like Temporal/Docker/Terraform
- **Secondary positioning:** "the contract AI can reason about" — AI-native
- **Key files:** `brand/brand-decisions.md` (locked), `brand/philosophy.md`, `brand/brand-spec.html`
- **Created:** 2026-04-04

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-04 — Hero example criteria + TimeMachine rework

**What makes a great hero example:**
- The `because` messages are the brand voice moment — they must be memorable. "The flux capacitor cannot run on vibes" lands harder than "1.21 gigawatts required."
- Three states > two states. The intermediate state (Accelerating) makes the state machine legible as a machine, not just an on/off switch.
- The `when` guard on event args (`when FloorIt.TargetMph >= 88 && FloorIt.Gigawatts >= 1.21`) is more readable than guarding on field values, because the reader sees the contract stated at the moment of the transition.
- A `reject` with a brand-voice message is the highest-impact line in the whole snippet. It's where Precept's promise ("invalid states are structurally impossible") becomes vivid and human.
- Event name matters. `FloorIt` is funnier and more instantly legible than `Accelerate`.

**Candidate I — TimeMachine rework:**
- Original: 2 states, no invariant, no when guard, no reject, flat because messages. Missing half the DSL surface.
- Improved: 3 states (Parked, Accelerating, TimeTraveling), invariant on Speed, dual event asserts with personality, when guard on event args enforcing the 88/1.21 conditions, reject with BTTF subversion line.
- Compiles clean (zero diagnostics).

**DSL conventions confirmed:**
- `when` guard can reference `EventName.ArgName` — evaluates against incoming event args before mutations.
- Unguarded `reject` row after a guarded transition row is the correct pattern for "try condition, else reject" flows.
- Bare arg names (no dotted prefix) are valid in `on Event assert` expressions.
- `precept_compile` returns `valid: true` + full typed structure — use it to validate every snippet before publishing.

### 2026-04-04 — Voice & tone directive received; hero domain conflict

- **User voice directive:** Brand voice updated to permit occasional jokes. Hero example may use fun/pop-culture domain (Back to the Future explicitly approved). Jokes in `because` reason messages are appropriate for hero snippet. **Supersedes:** Prior "Serious. No jokes." guidance.
- **Copilot directive:** Model upgrade policy — always use `claude-opus-4.6` or `claude-sonnet-4.6` for Precept agents. No haiku.
- **Hero domain conflict recorded:** User prefers TimeMachine (reworked by J. Peterman to 18 lines, full feature coverage). Steinbrenner spec verdict favors Subscription (higher score, no fantasy domain). Both candidates on shortlist pending team decision.
- **Brand research observations filed:** (1) reference files need STATUS headers to prevent re-litigation; (2) AI-native frame is undersold; (3) hero snippet is the most consequential brand asset; (4) wordmark rationale should surface in public docs.

### 2026-04-05 — Philosophy coverage added

`brand/philosophy.md` is now synthesized into `brand-brief.md` under `## Philosophy`. Covers: the feeling of use (prevention/inspectability as product experience), the four differentiators framed for brand copy, the name "Precept" and why it is exact rather than evocative, and the icon brief — specifically the open/closed-path geometry and containment as the visual expression of the engine's structural guarantee. Color guidance from philosophy (dark background, monochrome-strong, semantic color optional) is captured. Metaphors to avoid (generic SaaS badge, vague concepts) are documented.
