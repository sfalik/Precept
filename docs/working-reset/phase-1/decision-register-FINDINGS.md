# Decision register — what it is, and what was wrong with it

**Status**: Reference — 2026-07-26
**Subject**: `docs/working-reset/phase-1/decision-register.json` (547 rows)

## Read this first

**The want document is 82% Shane's.** `docs/Working/what-i-want-2026-07-16.md` is 221 lines. 182 of them are the document as he first committed it. 23 were reworded the same evening by an editor pass that changed no claims. **16 were written the next day by an agent amendment that added new content**, and those 16 are the problem.

The 16 added lines are: a second field (:36), a second rule (:40), two new transition rows (:71-72, :80-81), guards grafted onto two rows Shane wrote (:68, :75, :78), a bullet on non-linear obligations (:105), three mutation-table rows (:121-123), a first-person paragraph beginning "On non-linear rules I want the line drawn in the open" (:135), and one reword the amendment made necessary (:154).

Two of them matter a great deal:

- **:81** is a reject message reading `{MonthlyRepayment}` — a field declared `optional` — on a row with no guard. It is the only optional read inside a message anywhere in the document. **In Shane's own version there is no optional read anywhere at all**, in a message or out of one.
- **:72** is a reject message containing a division. **In Shane's own version no message contains any arithmetic.**

The full line-by-line map, with the original wording quoted beside every changed line, is at [`want-document-provenance.md`](want-document-provenance.md).

## What the register is

The register is the output of phase 1 of `compiler-readiness-plan.md`. Phase 1 walked 83 commits and, for every decision those commits claim was made, asked one question: **is there any evidence Shane made it?**

Its rule is that only a document Shane wrote himself can produce evidence. Commit messages do not count, because every commit in this repository is authored under his identity by an agent. Agent-written design docs do not count, and neither do the words "owner ruling" inside them. That leaves the want document as the only source of evidence in the repository, which is why 432 of the 547 rows came back `no trace`.

The three verdicts:

- **traced** — the want document says this, in Shane's own words.
- **contradicted** — the want document says the opposite of this.
- **no trace** — nothing Shane wrote settles it either way. This is not "wrong". It means the decision is unsupported, and if it is load-bearing it needs him.

A `no trace` verdict says nothing about whether a decision is good. Several of the best-reasoned decisions in the repository are `no trace`.

## What was wrong, and what was corrected

Phase 1 applied its rule to the want document **as it stands at HEAD**, without checking which parts of it Shane wrote. So some verdicts rested on text an agent wrote inside the one document everything else is measured against.

The correction pass on 2026-07-26 built the line-level provenance map, then re-checked every `traced` and every `contradicted` row against it.

### Counts

| Verdict | Before | After |
|---|---|---|
| traced | 82 | **78** |
| contradicted | 33 | **32** |
| no trace | 432 | **437** |
| **total** | **547** | **547** |

Every row and every original field was kept. 67 rows carry a new `basisCorrection` field recording what changed and why.

### Seven verdicts fell

Four `traced` rows and three `contradicted` rows lost their basis entirely and became `no trace`.

**Three rows about fault-prone expressions inside messages** — `c67f334e`, `62479cae`, and the row at `presence-tolerant-message-rendering-2026-07-23.md:71`. All three argued that message holes must enroll as obligation sites because Shane's own worked example puts a division inside a reject message. That message is :72, written by the agent amendment. The other citation those rows lean on — :104, "faults (division by zero, overflow, out-of-range) get the identical premise-and-certificate treatment" — is genuinely Shane's, but it establishes that fault obligations exist, not that a hole inside a message is one of the places they arise. Nothing he wrote reaches the message position.

**Four rows about presence in messages** — `b2dc5d0a` (traced), and `ad78f956`, `72312285`, `63e4faeb` (all contradicted). All four turn on `want:80-81`, and all four are gone.

The three contradicted rows are the serious ones. Each argued that the "refuse uniformly" rule refuses an example Shane says is accepted, and the design was locked on the strength of that. **The example does not contain an optional read.** `MonthlyRepayment` is the only optional field in Shane's version and it appears exactly four times — declared at :35, written at :67, mentioned in prose at :98, and named in one mutation-table row at :113. It is never read. No message in his version reads any optional field.

So refuse-uniformly refuses nothing Shane wrote. **The rule was locked against a conflict that was not there.** That does not make the rule wrong — it may well be right — but the argument recorded for it, that it costs the owner his own example, was made against agent-written text. If it is to stay locked it needs a reason that is actually Shane's, or his agreement.

### Three verdicts survived on a substituted basis

`frank-go-forward-posture-replay-2026-07-14.md:57`, `frank-review-what-i-want-2026-07-16-opinion.md:192`, and `fable-analysis-of-frank-response-2026-07-11.md:127` all cited :135, the non-linear paragraph the amendment added, and the last two cited nothing else. Their verdicts stand, but on different text: Shane's unqualified no-deferral statement at :171 and his statement at :199 that the runtime does not re-evaluate rules against the post-mutation configuration. A non-linear rule is a business rule, so a decision that governs one at runtime does contradict him.

Record the narrowing honestly, because it is easy to lose: **Shane's own document says nothing about non-linearity anywhere.** Everything the repository treats as his position on non-linear rules — including the paragraph at :135 that reads in his voice — is agent-written. The disagreement is with his general rule, not with anything he said about that case.

One further row, `69e12c9f`, cited :135 alongside three of Shane's own lines and had already flagged :135 as later text. Its verdict is unaffected.

### 48 verdicts stand with the wording substituted

These cite lines whose HEAD wording comes from the `ed57d7fd` editor pass. That pass reworded 23 lines and moved the Open-questions section; it introduced no claim the original does not make, so no verdict moved. Each affected row now carries the original line number and the wording difference in its quotation.

The one systematic change is vocabulary. Shane's document said "door" — "ingress is exactly two doors", "validation at the door", "the editable door". The editor pass replaced all of it with "ingress". Every register quotation using the word "ingress" in that sense is quoting the editor, not Shane. The claims are unchanged.

Two edits in that pass deserve a look and are marked in the provenance map:

- **:141** — Shane wrote "(post-MVP, this expands to include schema evolution)", where "this" could mean ingress or restore. The pass pinned it to restore. Its commit message calls that an owner ruling; nothing outside the commit message says so. Eight register rows cite this line, including all of those about `Restore` owing no preservation obligation — but they rest on its first sentence, which is verbatim his, not on the parenthesis.
- **:165** — the pass added "a trust it establishes by verification, once, at load (see Certificates)". That claim is Shane's, pulled forward from the certificates section he wrote at `72c2e183:197`. One row (`ed57d7fd`, index 346) is about exactly this edit and had already traced it correctly to the original.

### Two smaller defects

**A malformed commit hash.** Four rows carried `9e719661921ab3b84422199b1de4384bb00ebd7` — 39 characters, resolving to no git object; a `6` had been dropped. The commit is `9e7196661921ab3b84422199b1de4384bb00ebd7`, "docs(matrix): write the transport and type-structural validity arguments", 2026-07-23. The hash is repaired on all four rows.

The premise that `9e719666` therefore had no valid row is wrong, and the correction is smaller than it looked: the commit also has three rows under the short hash `9e719666` (indices 368-370), and two of the four repaired rows restate two of those three. The commit was walked twice by two passes and neither walk was lost. Nothing needed to be produced from scratch.

**Two verdicts for one decision.** The deferral of arithmetic overflow past the MVP, with no compile-time guarantee in the meantime, appeared as `contradicted` in five rows (`90f0792f`, `d32d0135`, `69e12c9f`, and the STATUS:61 row) and `no trace` in two (`dfeb52cc`, `0aa3723e`).

`contradicted` is right. Overflow is named in Shane's own list of faults that get the full premise-and-certificate treatment, at `72c2e183:98`, and that line is untouched by both later commits. A deferral leaving overflow with no compile-time guarantee disagrees with it. Both `no trace` rows are corrected to `contradicted`.

The **temporal** half of the same exclusion is left without a verdict, as one of the 0aa3723e rows already did. The want document leaves time-referencing rules open at :219 and says nothing about temporal arithmetic either way.

## What this does not settle

The correction pass changed which rows have a basis. It did not change what the register is fundamentally short of, which is Shane.

- **`no trace` still means what it meant.** 437 rows have no evidence behind them. That is the register's actual finding and the correction pass did not improve it.
- **Whether the want document itself records a conversation faithfully** cannot be checked from the repository. `72c2e183`'s message says it was "drafted bit-by-bit in conversation", which is a claim in a commit message like any other. Phase 1 treats it as evidence because it has to treat something as evidence; that choice is not verified.
- **Whether Shane dictated any of the amendment's 16 lines** cannot be established either. The commit calls itself an amendment and cites a review, not a conversation. If he did dictate them, seven verdicts come back — but that is his to say, not the repository's.
- **`the-walk.md` is now stale.** It was generated from the register before these corrections, and it still carries the three withdrawn presence contradictions. Regenerating it is a separate step.

## Where to look

| For | Read |
|---|---|
| Which lines of the want document are whose | [`want-document-provenance.md`](want-document-provenance.md) |
| The rows themselves | `decision-register.json` — `correctionPass` holds the counts; changed rows carry `basisCorrection` |
| What phase 1 was trying to do | `compiler-readiness-plan.md`, phase 1 |
| The register's own list of what it could not determine | `decision-register.json` → `couldNotDetermine`, 62 entries. Entries 5, 26 and 46 anticipated this problem; entry 26 named the amendment commit and the exact lines. |
