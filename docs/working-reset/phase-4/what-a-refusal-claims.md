# What does it mean when the compiler refuses?

**Prepared for Shane, 2026-07-26. Raised at the phase-1 walk, item 11, where he said it had never been
put to him. Blocks phase 4's contract and phase 5's fragment.**

**Reading time: about five minutes.** Everything below is either a quotation from canon or a compile
result reproduced at HEAD today, marked as such. Compile results appear only to show what the current
compiler does — never as evidence of what it should do.

---

## The question in one sentence

When Precept will not accept a definition, is it saying *"I looked as far as I look and did not find a
proof — here is what would close it"*, or is it saying *"there is no proof"*?

## Why it is not a detail

Both refuse the same definition and both print a message telling the author what to add. The difference
is what the compiler is entitled to claim, and that decides how much of the language can exist.

If a refusal only reports the search, then a definition can be refused today and accepted next year with
no change to the file — because the compiler got better, not because the program did. If a refusal
asserts there is no proof, that cannot happen: a refusal is a fact about the program. The price is that
every construct in the language has to be one where the compiler can actually reach that conclusion.

## What today's compiler does, and why it makes the question concrete

Reproduced at HEAD, 2026-07-26. This definition is refused:

```precept
precept Ledger

field Total as decimal nonnegative max 1000 default 0.0

event Add(Amount1 as decimal nonnegative, Amount2 as decimal nonnegative)

on Add when Add.Amount1 + Add.Amount2 <= 1000
    -> set Total = Add.Amount1 + Add.Amount2
```

The guard says the sum is at most 1000. The field allows up to 1000. The proof is one step and a person
can see it. The compiler reports the value it is about to store as ranging from minus infinity to plus
infinity, and refuses with:

> `PRE0078` — Numeric computation exceeded the representable range on field 'Total'

Two things are wrong with that, and only one of them is this question. The message is inaccurate — nothing
exceeded any representable range, the compiler simply could not use the guard — and that is a defect
already on the list. But underneath it, the compiler has refused a provable program and stated it as
though it were a fact about the program. That is the failure this decision is about.

Rewriting the same intent so the bound sits on each argument is accepted:

```precept
event Add(Amount1 as decimal nonnegative max 500, Amount2 as decimal nonnegative max 500)

on Add
    -> set Total = Add.Amount1 + Add.Amount2
```

Same program, different spelling, opposite answer. Under option one below that is expected and honest.
Under option two it is a bug, because the compiler claimed something false about the first spelling.

---

## Option one — a refusal reports the search

The compiler tries to build a proof using the derivation rules it has, within a stated limit. If it does
not get there, it refuses and names the condition that would close it.

**What the author sees:**

> Cannot prove `Total` stays within `max 1000` after this write.
> Adding `max 500` to `Amount1` and `Amount2` would make it provable.

**What it costs.** Nothing stops compiling that would otherwise compile — this is the more permissive
option. What you give up is the meaning of a refusal. "Precept refused this" no longer means "this
program can go wrong"; it means "this program is outside what the compiler currently reasons about". Two
authors with the same file and different compiler versions can get different answers, and neither is
wrong.

**What canon already says.** `precept-language-spec.md:225` sets out when a proof strategy may be used at
all, and its third criterion allows a hard limit paired with an explicit "could not prove — here is what
would" outcome. So option one is already licensed. Nothing has to change for it.

## Option two — a refusal is a statement about the program

The compiler refuses only when it has established that no proof exists using the contract's derivation
rules. There is no "gave up" outcome.

**What the author sees:**

> `Total` can exceed `max 1000` after this write.
> `Amount1 + Amount2` is not bounded by anything declared. Bound the arguments, or guard the handler.

**What it costs, stated as what stops compiling.** Every construct in the language has to be one the
compiler can settle both ways. Anything where the compiler might search forever without an answer has to
leave the language — not be refused case by case, but removed, because otherwise there are files it can
neither accept nor honestly refuse. Which constructs those are is phase 5's work to determine; the
candidates are the ones that combine multiplication of two unknowns with unbounded values, and the
quantifier and collection forms.

**A subtlety that may make this much cheaper than it sounds.** "No proof exists" here does not mean "no
proof exists in mathematics". It means "no derivation exists using this contract's rules" — and that is a
different, smaller question. A derivation system built so that every rule strictly reduces what is left
to prove has a search that always terminates, and then "no derivation exists" is something the compiler
can genuinely establish, even in a subject area that is undecidable in general. **Whether phase 5 can
build the discharge procedure that way is not known and nobody has tried.** If it can, option two costs
far less than the paragraph above suggests. If it cannot, option two is expensive and option one is the
honest choice.

---

## What is not being asked

Not whether a refusal explains itself. It does either way — want:179 requires the compiler to compute the
condition that would close the proof and show it, and that is settled.

Not whether unprovable means rejected. It does — want:171, "a business rule whose enforcement the
compiler cannot prove complete is rejected". Both options reject; they differ in what the rejection
asserts.

## What happens after you answer

Phase 4 writes the discharge contract stating which claim it makes. Phase 5 draws the fragment under it.
If you pick option two, phase 5 gains an obligation it does not have today: show that the derivation
search terminates, or name the constructs that leave the language.

## One thing worth deciding first

If you want option two but phase 5 cannot make the search terminate, you have to fall back to option one
anyway. It may be cheaper to have phase 5 attempt the terminating design first and bring you the answer
with evidence, rather than rule now and possibly have to revisit. **That would mean phase 4 states the
contract conditionally**, which is a departure from its purpose as the cheap early stop. Say which you
prefer.
