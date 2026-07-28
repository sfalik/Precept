# Notes on the philosophy rewrite — full pass

Committed at `20021deb`, with the three approved ninth-round changes applied on top of that commit
(uncommitted at the time of writing). Nine rounds: one full rewrite, then eight of owner-directed
revision, then the ninth described at the end. These notes describe
the whole pass, not the first draft. The file went from 3,309 words at the old HEAD to roughly
2,600, and every round kept the phase 2 substance intact: proof instead of re-checking, the four
kinds of declared premise, values held to their constraints as they enter, and the runtime
verifying a definition's proof before governing under it.

## Cut

- **The four-operation list** (CreateInstance / Inspect / Fire / Update). Replaced by the
  closed-set paragraph: a governed entity's data changes by being created, moved by an event, or
  edited where declared editable — previewable throughout — and the set is closed because an
  operation exists only if its proof does. No method names remain anywhere. The
  explicitly-forbidden-event sentence went with the list; the spec carries it.
- **"A domain integrity engine for .NET" as the opener.** The document now opens: "Precept governs
  what a business entity's data is allowed to become." .NET appears later, as a shipping fact.
- **The doubled stateless argument.** The opening's entity enumeration, the insurance headcount
  ("data entities outnumber workflow entities"), and "this is not a secondary capability" are gone,
  along with the Terraform / SQL / DDD precedent list. States-are-optional is now said once flatly
  in each place: one clause in the opening, one plain line in the principles. The market-size claim
  was unsupportable and unnecessary — if stateless precepts are first-class, that holds whether or
  not they are the majority.
- **"The word itself."** Its one working phrase — "declared once, holding always" — moved up to the
  second paragraph; the rest repeated the hierarchy section. The document no longer ends on its own
  etymology.
- **The softeners**, found one round at a time: "absolute" (the guarantee connective now names the
  two halves and stops), "cannot get stuck" (under-claimed — the headline now carries all three
  process properties), "nobody" twice in that headline (personified a structural claim), "simply",
  "fully", "not a weaker rule; it is a precise one" (an objection nobody raised), and "active
  rule-activator, not a passive label" (redundant with the sentence before it).
- **"There is no fifth thing."** It was false: restore is a fifth thing, named in the want document
  and the specification. The set is now scoped to the ways data *changes*, and restore is named —
  it changes nothing, returning a configuration that was already committed.
- **The deployment enumeration** ("not an unproven definition, not a tampered one, not one left
  over from an older version of the file"). An operational promise about a subsystem that does not
  exist yet, concrete enough for an architect to design a deployment around. The verification
  guarantee survives without it.

## Kept, deliberately

- **All nine positioning comparisons and the three complements**, tightened but not thinned.
- **Three not-X-but-Y constructions**, when the negation-density question was put to me: the
  opening "not caught and rejected — structurally impossible" and the closing "not which bugs get
  caught… which bugs can exist" frame the guarantee section as a deliberate pair, and the FHIR
  "validation pass, not a structural property" is a comparison bullet doing a comparison's job.
  A fourth (the rule-activator sentence) was reflex, and went.
- **"an operation nobody declared has no such proof and no existence"** — the outside reader named
  it the strongest sentence in the document; untouched through every later round.

## Added

- **The closing section, "What Precept refuses to do":** no escape hatch, no trust-me annotation,
  no construct that compiles with obligations unproven — and the cost named honestly: things
  writable in another tool cannot be written here, and the language admits less than its grammar
  suggests. The document now ends on the trade, not the name.
- **Two authoring paragraphs:** the refusal teaches (a rejection names the missing premise, so the
  domain expert learns how their own rules compose), and an AI agent meets the same refusal through
  the MCP server that a person meets in an editor — the guarantee does not weaken when an agent
  writes the rules. No superlatives; the claim rests on the compiler, not on tooling.

## Still open — the owner's decisions, not oversights

- **Overflow** stays listed unqualified among the compile-time impossibilities, in two places. The
  document states the target; the reconciling note is drafted and unruled.
- **The headline guarantee carries no boundary**, while the specification now states one (data
  written around the engine is not re-governed). Explicitly reserved. Do not fix it in passing.

## The ninth round — proposed here, owner-approved, applied

- **"Verifies against what?" now has its answer.** The verification paragraph opens: "The proof
  travels with the definition, and before an engine will govern under it, it verifies that proof
  for itself." One clause naming the artifact's existence — no format, no checker, no load step.
- **The closed-set paragraph is split in three:** the ways data changes (with ingress), then
  preview and restore, then the closing sentence — "an operation exists in the compiled engine
  only because the compiler proved every rule survives it, and an operation nobody declared has no
  such proof and no existence" — standing alone, with the room it earns.
- **The ending stops at three beats.** "…worth more than a larger one in which every rule is a
  hope." The final restatement ("the price of a guarantee that means what it says") is cut;
  "expressiveness surrendered on purpose" and the author paying the cost both stand.

## A tenth round

Nothing. The one thing I weighed and left: the document now ends on the word "hope". The
antithesis puts the rejected thing last, which is the classical shape, and the sentence's claim —
the smaller language is worth more — lands where it should. I considered flipping the clause order
and every flip read worse. I would change nothing.
