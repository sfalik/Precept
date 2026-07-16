---
title: "Four-Disposition Structural Analysis — Draft C's Two-Bucket Compression and What It Drops"
date: 2026-07-14
author: Frank
status: Working — analysis only, no fixes yet applied
target: hybrid-model-draft-C-fresh-structure-2026-07-14.md
---

I've now read Draft C's actual §1.2, §2.1–§2.4, §3.3, §4, and §5.1. This is the real thing, and I'm going to give you the honest structural answer, not a patch. Shane's instinct is correct that something systematic is wrong — but it is **not** the underlying model, and it is **not** months of circling. Let me show exactly what breaks and why.

## Bottom line up front

**The underlying hybrid model is sound and already handles this example. Draft C's *expression* of it is not — and the reason is a single root cause, not a series of one-off holes.** Draft C's own §1.2 states the correct principle — *"you classify values and obligations, never constructs"* — and then §2.1, §2.3, and §5.1 quietly **regress to classifying constructs into two buckets with a clean two-disposition mapping** (`supplied → governed`, `computed → proven`). That compression is lossy. It drops two dispositions the ratified corpus already worked out, and Shane's example lands precisely in the dropped ones. The fix is a bounded rewrite of Draft C's decision model back to the ratified four-disposition, obligation-granularity form — not a reopening of whether prove-or-reject-plus-governed is the right model.

## Working Shane's example through Draft C's *own* procedure

```
field test  as number nonzero default 12    # immutable: no editable, no <-, no set
field test2 as number nonzero default 10    # immutable
rule test2 > sqrt(test)
```

Apply §2.3 ("do not look at the constraint first; look at each value"):

**Step 1 — name the values.** `test2`, `test`, and the computed `sqrt(test)`.

**Step 2 — trace each to origin.** Here §2.3 **stalls immediately.** Its literal choices are "construction input, event argument, or direct edit (supplied), or does an expression derive it (computed)." `test` and `test2` hold their **default** values — which are *none of the three supplied entry points* and *none of the three computed forms* (a constant literal is not "an expression the definition derives"). §2.1's enumeration cannot classify them. That is the default gap from my last analysis, now reproduced *inside a rule* — confirming it was never a one-off.

**Step 3–4 — even if we grant the patch** (defaults are internal), the procedure now produces a **direct contradiction with §5.1**:
- §2.3 step 4: "If it relates supplied values, it is a governed constraint. If it constrains a computed value, it also becomes a prove-or-reject obligation on that computed value." The rule relates **no supplied value** (both operands internal), and it **does** constrain a computed value (`sqrt(test)`). So §2.3 routes it to **prove-or-reject**, and — both operands being constants — that obligation is decidable: `sqrt(12) < 10` → **proven-true**.
- §5.1: "**A rule is *always* governed.**" Blanket, universal.

For this example these two sections of the same document give **different dispositions.** §2.3 says prove-or-reject-on-a-computed-value; §5.1 says governed. They collide exactly when a rule references no supplied value.

**The decisive variant.** Change one character — `default 10` → `default 1` on `test2`:

```
field test2 as number nonzero default 1     # rule becomes 1 > sqrt(12) ≈ 3.46  →  FALSE, forever
```

Now the sole configuration the entity can *ever* hold violates the rule. Under §5.1 ("always governed"), the rule is enforced at the construction sweep — which means **every `Create` fails at runtime and the entity can never be instantiated, yet the definition compiles.** That is precisely the "compiles but no valid Create exists" anti-pattern. The ratified model rejects this **at compile time** as definition incoherence (a third category, always-Error). Draft C's "always governed" produces the *wrong disposition* here — not an incomplete list, a wrong answer. That is the tell that this is structural, not enumerative.

## The three sub-questions, answered directly

**1. Is the rule meaningfully enforced at runtime?** No. Both fields are immutable and their values are fixed by constant defaults, so the rule's truth is fully determined at compile time. The construction sweep would evaluate it once against values that can never differ from what the compiler already knows. It can never issue a refusal a compile-time proof didn't already settle — and if it *could* "fail," the failure would be addressed to no one (there is no external supplier to resupply a value, no editable field, no event arg). That is the same "no addressee" disqualifier §6 uses to *exclude* governing undecidable computed values. **Shane's own framing is exactly right: the rule's only meaningful role is compile-time — proven-true-forever (a dead/vacuous rule, a flag) or proven-false-forever (definition incoherence, a reject).**

**2. Does §5.1's "always governed + maybe premise" hold here?** No — and this is the core structural finding. **§5.1's universality silently assumes at least one referenced field has a reachable external write** (so the sweep has something real to refuse). When every operand is immutable-internal, "governed" degenerates to a vacuous re-check of a compile-time-known fact. Governance is meaningful only where a value can actually *arrive from outside*; the model never states that precondition, so §5.1 over-claims. The honest statement is: *a rule referencing at least one externally-writable value is governed; a rule over exclusively immutable-internal values has no live governance role — its disposition is compile-time.*

**3. Is `sqrt(test)` a prove-or-reject obligation like `sqrt` in a `set`/`<-`?** **Yes — per the model** (ignore the current compiler, which as we found doesn't stamp it; that's an implementation gap, not the design). `sqrt(test)` is a fault-prone operation over an internal value, so it carries its **own** prove-or-reject obligation — the sqrt domain condition `test >= 0` — **independent of the relational comparison.** If `test`'s default were `-12`, the model should reject on `sqrt` of a provably-negative operand, regardless of what the `> test2` comparison does. So the rule actually spawns **two** distinct prove-or-reject obligations (the `sqrt` domain and the relational containment `sqrt(test) < test2`), plus a governance obligation that is vacuous here. **§5.1's "governed + maybe premise" duality captures none of these** — it treats the rule as an atomic constraint and never wires §5 (rules) to §4 (fault-freedom on computed sub-expressions). A rule that *contains* a computed sub-expression needs §4's machinery applied to that sub-expression, and Draft C doesn't connect them.

## The root cause — one defect, four symptoms

Every hole we've found this session is the same regression:

> Draft C reduced **"disposition is a property of an obligation (four kinds), routed by the provenance of each operand"** down to **"disposition is a property of a value's bucket (two kinds)."**

The ratified corpus established **four** dispositions (a 2×2 on "does a failed check have a graceful runtime home?" × "is the fact decidable from the definition alone?"):

1. **Prove-or-reject** — fault-freedom + containment on computed values → Draft C §4 ✅
2. **Govern** — external values at ingress/sweep → Draft C §3 ✅
3. **Definition incoherence (reject, Error)** — a constraint over values fully determined at compile time that is *proven-violating* (constant defaults violating a rule; unsatisfiable initial config) → Draft C has this **only for computed values inside §4.2, never wired to rules over immutable operands** ❌
4. **Flag / dead-rule (warn)** — a constraint *proven-true-forever* that constrains nothing, or an unreachable rule → **absent from Draft C entirely** ❌

Shane's example lands squarely in the two missing dispositions: `rule test2 > sqrt(test)` over immutable constants is **#4 (dead/vacuous)** when proven-true, and **#3 (incoherence)** when proven-false. Draft C, having neither, force-fits it into #2 (governed), where it degenerates or gives a wrong answer. The default gap (my prior finding) is the *same* defect at the value level: a constant default is the *immutable-internal* value whose violation is disposition #3, and Draft C's two-bucket §2.1 has no slot for it.

## What actually needs to change (reconsideration, not a patch)

Not "add `default` to the list." Four coordinated changes, all restoring the ratified model Draft C compressed away:

1. **Restore the four dispositions in the decision model.** Route each *obligation* to one of: prove-or-reject / govern / incoherence-reject / dead-flag. §2.3's clean "supplied→governed, computed→proven" binary must become a routing over obligations, with the two compile-time-coherence dispositions first-class.

2. **Define governance's precondition.** "Governed" applies to a constraint iff at least one referenced field has a **reachable external write** (editable in some reachable state, or fed by a construction input / event argument). §5.1's "a rule is always governed" must be qualified: a rule over exclusively immutable-internal values is a compile-time obligation, not a governed one.

3. **Fold defaults into internal origin — but recognize the immutable sub-case.** A constant default is internal; when the field is *also* immutable (no editable/`<-`/`set`), the value is fully determined at compile time and routes to coherence (#3/#4), not to per-operation governance or per-operation prove-or-reject.

4. **Make disposition attach at obligation/expression granularity.** A single rule spawns independent obligations — one prove-or-reject per fault-prone sub-expression (`sqrt(test) → test>=0`), one relational containment, one governance — each routed by *its own* operands' provenance. This is what §1.2 already promises and §5.1 fails to deliver.

## Verdict

**The two-bucket *provenance* classification generalizes** — provenance genuinely is binary (a value comes from outside or is produced inside; a default is inside). **But the two-*disposition* mapping Draft C builds on top of it does not generalize** — because provenance does not map one-to-one onto mechanism. Internal values split into *recomputed-per-operation* (prove-or-reject) and *immutable-fully-determined* (compile-time coherence), and "governed" requires a live external write path that Draft C never made a precondition.

So, to Shane's real question: **the model is sound; Draft C's expression of it is insufficient, and insufficient in a single, diagnosable, already-solved way.** The answer is not out in "months of circling" — it is in the ratified corpus's four-disposition obligation-centric framing, which Draft C's own §1.2 gestures at and then abandons. What's required is a rewrite of Draft C's §2.3/§4.2/§5.1 to that framing plus explicit §3/§4 sections for the incoherence and dead-rule dispositions — bounded work against a known target, not a reopening of the boundary. I'd stake the position on that: hand me the go-ahead and I can specify the corrected decision model precisely, but I've made no edits pending your and the Fact-Checker's parallel sweep.
