# The trace's short list — the owner's rulings

**What this is.** The trace ([`trace/`](trace/)) sorted every change made to canonical documentation in
this phase into four categories. The last category — changes that trace to nothing the owner wrote or
ruled — was the only part put to him. This records what he decided, on 2026-08-26, so the rulings are
not left in a conversation.

Twelve items were filed. All twelve are now dispositioned.

---

## Closed by his approval of the philosophy rewrite

He was asked whether he approved the rewrite of `docs/philosophy.md` — the whole-document rewrite, 3,309
words to 2,775, rather than the eight corrections that had been prepared. **He confirmed he did.**

That closes three items with no further work:

- **The status block at `philosophy.md:3`** claiming "Locked 2026-07-28 — approved by the owner". The
  claim is true. It stays.
- **The rewrite's editorial decisions** — dropping "a domain integrity engine for .NET" as the opener,
  deleting the four-operation list, the "The word itself" section, the Terraform / SQL / DDD precedent
  list, and rewriting the comparator bullets. All approved with the rewrite.
- **The paragraph at `philosophy.md:101`** on the compiler speaking to editors through a language server
  and to agents through an MCP server. Part of the approved rewrite.

**One repair inside this, which needs no ruling.** The market-size paragraph was deleted with the reason
recorded as "the claim was unsupportable". The research file `philosophy.md` itself links to —
`research/philosophy/entity-first-positioning-evidence.md` § 1.2 — is headed with that same sentence and
carries a five-domain table supporting it. The deletion stands on his approval; **the stated reason is
wrong and is corrected**, so the record does not misdescribe the evidence.

## Raised and withdrawn

- **The non-linear paragraph at `precept-language-spec.md:270`.** The trace filed it as unwarranted
  because it rests on want-document lines an agent typed. He resolved this separately — he reviewed and
  approved the want document, the content carries his approval, and **nothing comes out of canon**. Full
  record: [`want-amendment-marking.md`](want-amendment-marking.md).

## Ruled 2026-08-26

- **The "and nothing wider" narrowing of what an ingress check evaluates.** **Comes out of canon** —
  every site. The specification stays quiet on what a direct field edit evaluates until phase 5 settles
  it; nothing is added that answers the question in either direction. His reasoning, when the alternative
  of keeping it with its disputed markings was put to him: he did not want to settle the underlying
  question now, and a sentence that decides it in passing is not a tidy-up. Verified site list and the
  surgery: the pass that executed it. Ten qualifier-phrase sentences, two open-question index entries
  deleted, three places stating the same thing operationally narrowed to the event path only.

- **The open-question indexes across the runtime documents** — the 21-row table in `runtime/README.md`,
  the eight-bullet index in `precept-builder.md`, the eleven-entry index in `evaluator.md`, and the
  renamed section in `descriptor-types.md`. Nobody asked for them; an editor added them because the open
  questions were scattered and unfindable. **They stay.** *"agree"*.

  **One entry comes out**: the `FieldDescriptor.AccessModes` entry in `descriptor-types.md`, which indexes
  a question not written in that document at all — the printed type does not carry the property and the
  question lives in `precept-builder.md` and `evaluator.md`. The pass that wrote it flagged it as possible
  over-reach at the time. An index that points at questions living elsewhere becomes a second place to
  maintain.

- **The paragraph in `proof-engine.md` saying the reference-identity rule on `ProofObligation.Site` may
  buy nothing.** **Comes out entirely.** *"lets go clean"*. It was an editor's engineering judgement
  written in canon's own voice — that the rule "costs nothing and buys nothing" — which nobody had
  checked. Phase 5 specifies the certificate checker and will meet the question on its own; the paragraph
  itself says so. The invariant and its stated reason are untouched.

- **"What the chain rules out" in `diagnostic-system.md`.** **No action.** It is an inference the owner
  never wrote, but it is drawn from rules the document already states, and it marks the unresolved part
  as unresolved and points at the parked question. That is a specification doing its job.

- **The definition of "the MVP" in `soundness-and-coverage.md`** — that it is an internal build milestone
  and nobody outside the project receives it. The word is his; the definition was sourced only to a
  conversation held in an assistant's memory file, which nobody can check. **Put to him directly and
  confirmed**: *"yes"*. It stays, now recorded as confirmed by him rather than as an unsourced gloss.

  This one was worth asking because it does real work: it is why "post-MVP" throughout the documents reads
  as build ordering rather than a gap the product ships with, and the arithmetic-overflow deferral rests on
  it.
