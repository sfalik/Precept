---
title: Should Precept force arg→field constraint propagation? — a design opinion
status: Design opinion (advisory consult) — 2026-06-23 — not a decision, not a plan change
author: Frank (Lead/Architect & Language Designer)
requested-by: Shane (project owner)
grounding: docs/philosophy.md (§ governance-on-ingress vs fault-prevention-at-compile-time); docs/language/precept-language-spec.md §0.7 L266; samples/event-registration.precept
---

# Should Precept force arg→field constraint propagation?

## Verdict

**Oppose — as posed.** Forcing every author to re-declare a field's constraint on the
event arg that feeds it (so an under-constrained `set Field = Arg` becomes a compile-time
rejection) is not stronger prevention — it is *earlier rejection of the unprovable*. The
out-of-band state is already unreachable. The genuinely good idea hiding inside the request
is the **inverse**: in the direct-assignment case, let the compiler **infer** the field's
band as the arg's contract, **reject only the provably out-of-band**, and let runtime
governance refuse the merely-unprovable. Infer-band / reject-provable / govern-the-rest.

## Why

- **It restates the single source of truth.** The field already declares the band. Forcing
  the same numbers onto every feeding arg duplicates that truth, creates a drift surface
  (change the field, now chase every arg), and makes the definition read less like
  configuration and more like a proof obligation the author hand-carries.

- **It rejects the *unprovable*, not the *invalid*.** I probed this with `precept_compile`:
  the band lane already obligates the assignment. Given a field `min 0 max 100` and
  `set Field = Arg`, an **unconstrained** arg widens to `[−∞,+∞]` → `Unresolved` → **PRE0078
  reject** — a rejection fired on a value that might be perfectly fine. The invalid *state*
  is already foreclosed by the field band + governance-on-ingress + the compile-time catch of
  the *provably* bad. Forcing propagation just moves the over-rejection earlier.

- **Event args are the system boundary.** An arg's validity is a runtime fact about a value
  that does not exist at compile time. You cannot pre-prove what the world will hand you; you
  can only constrain it on entry. Demanding a compile-time proof of ingress data asks the
  compiler to prove something that is, by construction, not a compile-time fact.

- **The philosophy line is being misread.** Spec §0.7 L266 — "no result outside a declared
  bound… there is no deferral" — is the *fault-prevention* mechanism, and it is correct **for
  derived/computed results**: what the system itself computes, it must prove. Stretching that
  into a mandate over *what the world hands us* conflates the two mechanisms the spec keeps
  deliberately distinct. Ingress → runtime governance (enforced the moment a value enters).
  Derivations → compile-time proof. The amendment imports the derivation guarantee onto the
  ingress boundary, where it doesn't belong.

## The inverse — infer-band / reject-provable / govern-rest

- **Rationale.** In `set Field = Arg`, the field's band IS the arg's contract — so infer it
  rather than make the author retype it. Reject only what is *provably* out of band; admit the
  rest and let governance refuse it on entry. Same safety, no duplication, reads like config.
- **Alternatives rejected.** *Force propagation* (the posed form) — duplicates the source of
  truth and over-rejects the merely-unprovable. *Do nothing* — leaves the PRE0078 over-rejection
  on the band lane standing as a real authoring papercut.
- **Precedent.** `samples/event-registration.precept` already hand-carries the contract today:
  `event UpdateSeats(Seats as integer positive max 100)` feeding `field SeatsReserved … max 100`
  via `set SeatsReserved = UpdateSeats.Seats`. Inference would let the arg drop `max 100` and
  still be safe — the field already says it. The `precept_compile` band-lane probe (PRE0078 on
  an unconstrained arg) is the first-party evidence the over-rejection is live, not theoretical.
- **Tradeoff accepted.** Inference is scoped to the *direct* `set Field = Arg` shape; the moment
  an arg feeds an expression (`set X = Arg * 2`) or multiple fields, there is no single band to
  infer and the author still carries the constraint. We accept that narrowness in exchange for
  killing the duplication in the common case.

## Open philosophy question — surfaced, not resolved

Where the prevention boundary sits for ingress arg→field assignment — compile-time structural
impossibility vs runtime-governance refusal — is **Shane's call**, not mine. `philosophy.md`'s
compile-time-impossibility list (division-by-zero, overflow, empty-collection access) does **not**
list bands. Whether bands-on-ingress-assignment belong there is a philosophy-level ruling; I
surface it, I don't resolve it.

— Advisory consult. Not a ratified decision, not a plan amendment.
