# MVP wording corrections — 2026-07-27

Uncommitted. Records a wording correction applied across the canonical documents, plus two
owner-approved edits to `docs/philosophy.md`.

## The error being corrected

The owner's ruling at `docs/working-reset/phase-1/the-walk.md` item 15 defers the arithmetic overflow
model **past the MVP**. An earlier correction pass rendered that as **"past the first release"** in four
places in `docs/language/precept-language-spec.md` and two more in `docs/language/primitive-types.md`.

He does not intend to release the MVP. It is the first working compiler, an internal milestone, not
something a customer receives. The two phrasings are therefore different claims: something absent from a
release is a gap the product ships with, while something not yet in the first working compiler is simply
not built yet. Two independent readers took the "first release" text at face value and concluded overflow
was a deliberate shipped gap, and one argued from that text that the documents were overclaiming.

The accurate status was already in the tree. `docs/compiler/soundness-and-coverage.md` § 3.1 marks
`NumericOverflow` **"in-scope, not-yet-implemented"** and § 6 records the sequencing. Nothing in the
correction pass consulted it. The corrections below adopt that posture everywhere.

The claim itself is **not** weakened. Overflow stays listed among the faults the compiler prevents,
because that is the target, and walk item 23 governs: the specification states the target and the code is
validated against it, not the reverse. What changes is only how the not-yet-built status is described.

## 1. The four specification sites

`docs/language/precept-language-spec.md`

| Line | Before | After |
|---|---|---|
| 104 (Principle 7) | "**Overflow is currently exempted from this principle**, by an explicit ruling deferring the arithmetic overflow model past the first release; see §0.7." | "**Overflow is on that list as the target and is not implemented yet**: the arithmetic overflow model is in scope, and its build is deferred by ordering rather than dropped, so nothing proves the absence of overflow today; see §0.7." |
| 110 (Principle 10, Totality) | same sentence | same replacement |
| 112 (Principle 11, Static completeness) | same sentence | same replacement |
| 272 (§ 0.7) | "…is exempted from prove-or-reject for the first release by an explicit ruling. The compiler does not prove the absence of overflow…" | "…is in scope and not yet implemented. By an explicit ruling its build is sequenced after the MVP, the first working compiler, which is an internal milestone and not something anybody receives, so this is an ordering decision about when the work happens and not a gap the product is meant to carry (`docs/compiler/soundness-and-coverage.md` defines the milestone at its head and records the sequencing in §3.1 and §6). Until it is built the compiler does not prove the absence of overflow…" |

The rest of the § 0.7 paragraph is unchanged — it still says plainly that the compiler does not prove the
absence of overflow, that the runtime carries no guard, and that a clean-compiling definition can
overflow. The disclosure is kept; only its framing is corrected.

The contradiction register's test for this area still passes:

```
grep -rniE 'division by zero, overflow, empty collection access' docs/language docs/compiler \
  docs/runtime docs/tooling docs/philosophy.md --include=*.md | grep -viE 'exempt|deferr|§0\.7'
```

returns nothing. Each replacement keeps both "deferred" and "§0.7" on the line.

## 2. Two further sites carrying the same phrasing

Not among the four named, but the identical error, so corrected in the same pass.

- `docs/language/primitive-types.md:197` — "deferred past the first release" → "in scope and not yet
  implemented, its build sequenced after the MVP".
- `docs/language/primitive-types.md:236` — "deferred past the first release as for `integer`" → "in scope
  and not yet implemented, as for `integer`".

After this pass, `grep -rn "first release"` over `docs/language`, `docs/compiler`, `docs/runtime`,
`docs/tooling`, `docs/philosophy.md` and `docs/README.md` returns nothing.

## 3. The MVP sweep

About twenty uses across eight files were read. **One implied that the MVP is shipped**, plus one
"first release" phrasing inside the same document:

- `docs/compiler/soundness-and-coverage.md:139` — "The format the MVP ships" → "The format the MVP
  produces".
- `docs/compiler/soundness-and-coverage.md:166` — "recorded the checker as deferred out of the first
  release" → "deferred out of scope". A historical note about a withdrawn deferral; the phrasing carried
  the same false implication.

Left alone, because each uses MVP correctly as a scope boundary on the compiler's proof work with no
suggestion that anybody receives it:

- `docs/compiler/soundness-and-coverage.md` — :119 (cross-reference), :130 (point-binding limits), :137
  (§ 5a heading, "MVP prerequisite"), :174 (fail-open holes), :226 (the `NumericOverflow` row, which is
  the posture the rest of this pass adopts), :307 and :309 (GATE-O sequencing).
- `docs/compiler/proof-engine.md` — :8, :2618, :2620. All read "designed, not yet implemented". No change
  made to this file; another agent is working in it.
- `docs/compiler/README.md:30` — "prove-or-reject MVP designed".
- `docs/language/precept-language-spec.md` — :10 and :2200, both "designed, not yet implemented".
- `docs/language/primitive-types.md` — :81 and :697, the fixed-width-versus-arbitrary-precision decision,
  correctly recorded as open and not yet ruled.
- `docs/language/graph-analyzer-roadmap.md` — :10 and :18. Considered: "not in MVP; ship additively" and
  "what the runtime MVP needs". The shipping in both refers to the deferred modifiers landing later, not
  to the MVP being released, so the sentences do not carry the wrong implication. Left as written.
- `docs/language/temporal-type-system.md:626` — "deferred, post-MVP".
- `docs/runtime/evaluator.md:1806` — "the post-MVP job of the restore path".

### Where the milestone is now stated

`docs/compiler/soundness-and-coverage.md`, in the note block at the head of the document, immediately
before Part A. That file is the heaviest user of the term (ten occurrences) and owns the scope boundary
the term marks. The note says the MVP is the first working compiler, an internal build milestone marking
when the prove-or-reject engine runs end to end; that it is not a release and nobody outside the project
receives it; and that "post-MVP" therefore places work later in the build order rather than describing a
gap the product carries. The specification's § 0.7 paragraph points at it.

### One unrelated vocabulary fix

`docs/language/temporal-type-system.md:626` and :628 used "arm" for a case of a decision, which the
project's writing rules forbid. Changed to "case" in both. Same paragraph as the overflow note, so it
travelled with this pass.

## 4. `docs/philosophy.md` — two owner-approved edits

Both were approved before this pass. The file had uncommitted edits already in the tree and was taken as
it stood; nothing else in it was touched.

**Line 94 — the authoring paragraph.** Cut the final clause, "The author learns how their own rules
compose without ever learning to program." An author writes a formal declarative language with types,
guards, constraints and a proof model, which is programming. An outside reader named this the one claim an
experienced reader refuses, and said the refusal damages the neighbouring claims that deserved belief. The
paragraph now ends at "this guard would close the proof", and its real point — that the refusal names the
missing premise and so teaches — is intact without the clause.

**Line 23 — the determinism sentence.** The commitment is kept in full: no randomness, no evaluation
reaching outside the entity's own data, the clock as the one admitted exception, and the fact that a
time-dependent rule is visible in the file rather than hidden. Four specification facts were removed —
the function name `now()`, that it is the sole construct reading the environment, what an author declares
by writing it, and that the clock holds one value within an operation. Those belong to the specification's
authority and are simply not stated here now. Naming the construct and its semantics also answered, by
implication, a question `docs/Working/what-i-want-2026-07-16.md` deliberately leaves open.

The clause now reads: "The one environmental value the language admits is the clock, and it is admitted
honestly rather than hidden: a rule whose correctness depends on when it is evaluated says so in the
definition, where anyone reading the file can see it."

## Not touched

`docs/Working/bugs.md`; anything under `docs/working-reset/phase-5/`; `docs/compiler/proof-engine.md`;
every pre-existing file under `docs/working-reset/phase-2/` (this file is new). Nothing was compiled and
no catalog, sample, or compiler behaviour was used as evidence.
