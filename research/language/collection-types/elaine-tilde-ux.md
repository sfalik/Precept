# Elaine's UX Review: Scalar ~string

**Author:** Elaine (Language UX Designer, Author Experience)
**Branch:** `spike/Precept-V2`
**Date:** 2026-05-28
**In response to:** Frank's composition analysis in `tilde-string-analysis.md` (§ Composition Analysis)

---

## Summary

Scalar `~string` under the enforcement model is author-experience positive for a specific and important reason: domain experts — the audience who most need this feature — are exactly the authors least likely to catch the silent `==`-on-an-email-field bug through testing. The enforcement model removes a whole class of quiet semantic mistakes from a population who rarely writes unit tests to catch them. The syntax is learnable, the symbol is consistent, and the enforcement is protective. The feature should ship — but with meaningfully improved error messages, clearer documentation framing, and an honest acknowledgment that the mixed-collection error demands special attention in tooling and documentation.

---

## What works well for authors

### Symbol consistency lowers the long-term learning load

Frank is right that vocabulary fracture is the worst outcome. A domain expert who has learned `~=` ("this compares without caring about uppercase or lowercase") arrives at `field Email as ~string` with a complete mental bridge. The `~` already means something specific in their head. This is real UX value — not theoretical coherence, but actual transfer of prior knowledge. The one-time cost of learning `~` pays dividends across the whole language surface.

### Enforcement is the right model for this audience

Domain experts don't write automated tests. A compliance officer or operations lead writing a `.precept` file for the first time has no xUnit safety net under them. They rely on the compiler to catch mistakes. `~string` with enforcement gives the compiler the domain knowledge it needs to catch "you used `==` on an email address field" — a mistake this audience is almost certain to make, because `==` is the obvious first instinct for equality. The enforcement model is not the compiler second-guessing intent; it is the compiler *honoring* a declared intent that the field-level annotation provides. That distinction matters.

### The `notempty` + `~string` combination is natural and complete

`field Email as ~string notempty` reads cleanly and covers the common email field requirement in one declaration: non-empty storage and case-insensitive comparisons required. Authors who know `notempty` will reach for this combination naturally, and it cleanly sidesteps the empty-string check case (see frictions below) by making it a structural constraint rather than a guard.

### The enforcement fires at authoring time, not runtime

Every error in this model is a compile-time error. The author sees the mistake in their editor, not in a production bug report. For the target audience, this is the gold standard of feedback timing.

---

## What creates friction

### `~` carries no English meaning on first encounter

This is the feature's biggest discoverability problem. "Field Email as tilde-string" is how a domain expert reads this aloud. "Tilde-string" is not an English concept — it is a programmer's shorthand. `string ignorecase` loses vocabulary coherence, but it wins the readability contest decisively for a first-time reader. The `~` sigil must be **taught before it can be used**, which places higher demands on completions, hover documentation, and onboarding materials than any keyword alternative would.

To be clear: this is not a reason to reject the feature. `~` is learnable and consistent. But the documentation burden is higher than Frank's analysis acknowledges, and the tooling (completion text, hover messages) must do more work to compensate.

### The empty-string check produces a confusing correction

`ensures Email ~= ""` is syntactically correct but semantically jarring. When a domain expert writes `ensures Email == ""` and the compiler tells them to use `~=`, the teaching moment fails for this specific case. The domain expert's reaction is: *"Why would I use a case-insensitive comparison to check whether a field is empty? That doesn't make sense."* And they are right — the `~` in `~=` is irrelevant for the empty-string case. The compiler is technically correct and practically confusing.

Frank's answer (use `notempty` instead) is the right answer, but it requires the error message to teach both the fix *and* the superior alternative. An error message that only says "use `~=`" will leave the author confused. An error message that also says "or declare the field `notempty` to eliminate this check entirely" will leave the author educated. This is fixable in the error message — but it must be explicit, not assumed.

### The mixed-collection error is a distance surprise

`CaseInsensitiveValueInCaseSensitiveContains` is the hardest error for authors to encounter smoothly. Here is why:

The author adds `~string` to `field Email` because they understand that Email comparisons should ignore case. This is local reasoning — they are thinking about Email. The error fires somewhere else in the file, potentially on a `field AdminEmails as set of string` they wrote six months ago or inherited entirely. The connection between the two errors is not obvious without understanding how `contains` semantics are governed by the collection's inner type, not the value being tested.

This is not bad design — the compiler is catching a real, dangerous semantic mismatch. But from the author's experience, it feels like: *"I added `~string` to Email and now there's an error in AdminEmails. What did I break?"* The answer is: nothing was broken, but something was revealed. The error message must make this distinction explicit.

### The "I didn't write that field" scenario has no escape hatch

If an author inherits a precept they did not write, adds `~string` to a new field, and receives a `CaseInsensitiveValueInCaseSensitiveContains` error pointing at a collection they do not own conceptually, they face a real problem. The compiler is telling them to change something they didn't add. The error message must offer both a primary fix (change the collection to `set of ~string`) and a legitimate alternative path (remove `~string` from the field and use an explicit `~=` in a quantifier predicate). Both paths should be in the message, because the right choice depends on ownership context the compiler cannot know.

There is no "I know what I'm doing, suppress this" escape hatch proposed, and I agree with Frank that the error should be `Error` severity, not `Warning`. The mismatch is provably wrong. But the message must give the author two routes out, not one.

### `~string` in a completion list is opaque without hover

A domain expert typing `field Email as ` will see `string` and `~string` as completion candidates. Without hover text that immediately explains `~string` in plain English ("case-insensitive string — comparisons on this field will always ignore uppercase and lowercase"), the author has no basis for choosing. The gap between `string` and `~string` in the completion list is invisible until the author has already learned the feature. This is a tooling problem, not a language design problem, but it is a real author friction that must be closed.

---

## Error message review

### `CaseInsensitiveFieldRequiresTildeEquals`

**Frank's proposed message:**
> "Field `Email` is declared `~string` (case-insensitive). Use `~=` instead of `==` to compare it — `==` is case-sensitive."

**Assessment:** Clear and actionable for authors who understand "case-sensitive." For authors who do not, the message tells them what to do but not why it matters. "Case-sensitive" is technical vocabulary that business analysts hear but may not fully internalize.

**Suggested improvement:**
> "`Email` is declared `~string` — comparisons must ignore letter case. Use `~=` instead of `==`.
> `==` treats `"admin@example.com"` and `"Admin@Example.COM"` as different values. `~=` treats them as equal."

**For the empty-string case specifically:** The language server should detect when the right operand is `""` and append:
> "To require a non-empty value, declare the field with `notempty` instead: `field Email as ~string notempty`."

---

### `CaseInsensitiveFieldRequiresTildeNotEquals`

**Frank's proposed message:**
> "Field `Email` is declared `~string` (case-insensitive). Use `!~` instead of `!=` to compare it — `!=` is case-sensitive."

**Assessment:** Parallel structure to the `~=` message, same strengths and same weaknesses. The correction `!~` is introduced without explanation for authors who have not yet seen it.

**Suggested improvement:**
> "`Email` is declared `~string` — comparisons must ignore letter case. Use `!~` instead of `!=`.
> `!=` treats `"admin@example.com"` and `"Admin@Example.COM"` as different values. `!~` treats them as equal (and reports `true` when they are not equal under case-insensitive comparison)."

The parenthetical on `!~` is worth including — the not-equals direction is slightly less intuitive than equals for CI semantics.

---

### `CaseInsensitiveValueInCaseSensitiveContains`

**Frank's proposed message:**
> "Field `Email` is declared `~string`, but `AdminEmails` uses case-sensitive `string` for its inner type. The `contains` test will use case-sensitive comparison — `"admin@example.com"` and `"Admin@Example.COM"` are treated as different values. Either declare `AdminEmails as set of ~string` or use an explicit `~=` in a quantifier predicate."

**Assessment:** The message is accurate and complete, but it leads with the technical diagnosis and buries the actionable fix at the end. For a domain expert, the first thing they need is: *what is wrong, in domain terms?* The second thing they need is: *what do I do about it?* The current draft gives them the technical explanation first, which they may struggle to parse before reaching the fix.

The "or use an explicit `~=` in a quantifier predicate" option is too abstract for most domain experts — quantifier predicates are not everyday vocabulary.

**Suggested improvement:**
> "`AdminEmails` is declared `set of string`, which uses case-sensitive membership. `Email` is declared `~string`, which requires case-insensitive comparisons.
>
> The `contains` test will treat `"admin@example.com"` and `"Admin@Example.COM"` as different values — this is almost certainly not what you want.
>
> **Fix:** Change `AdminEmails` to `set of ~string` to make membership case-insensitive throughout.
> **Alternative:** If `AdminEmails` must remain case-sensitive, remove `~string` from `Email` and rewrite the comparison as: `any e in AdminEmails (e ~= Email)`."

The `any`-quantifier alternative is more concrete than "a quantifier predicate," but even this may be too advanced for a first-time author. An additional note — "Talk to whoever defined `AdminEmails` to agree on whether membership should be case-insensitive" — may be the most useful guidance in practice, since this error often implies a collaborative design question, not just a syntax fix.

---

## The mental model question

### Does declaration-level CI intent help or hurt use-site clarity?

The `~=` operator was designed to make case-insensitive comparisons visible at every use site. The honest question is: does `field Email as ~string` add a second layer of intent that helps authors or adds overhead?

**It helps — at the field level.** The field declaration is where domain intent lives. "Email is always compared case-insensitively" is a domain truth about Email, not a truth about any particular comparison. Expressing it at the field level is the right semantic location. A business analyst reading the field declarations at the top of a precept file understands what Email *is* — and `~string` contributes to that description, the same way `notempty` or `optional` does.

**It adds overhead — at the use site.** An author reading a guard `Email ~= "admin@example.com"` currently just sees "case-insensitive comparison." With scalar `~string`, the `~=` becomes a double signal — it tells the author the comparison is CI (operator-visible) AND it satisfies the field's declared type obligation (type-visible). The author reading the guard still sees the same thing. The author *writing* the guard now has a reason for `~=` that goes beyond operator preference — they are satisfying a type obligation. This distinction is invisible to a reader but meaningful to an author.

The real tension is subtler: **the enforcement model changes `~=` from a preference to a requirement.** On `~string` fields, `~=` is not optional — it is mandated. This is a philosophical step from "explicit at every use site" to "declared at field level, enforced at use site." These are not contradictory, but they are different stances. The `~=` operator surface remains complete and explicit; what changes is that the field declaration creates a *proof obligation* the operator must satisfy. The author experience shifts from "I chose to use `~=` here" to "I am required to use `~=` here." For domain experts who understand the domain reason, this is clarifying. For authors who encounter the constraint without context, it is initially puzzling.

**Net verdict on the mental model:** The declaration-level intent is the right design for this audience. Domain experts think about fields holistically ("Email is a case-insensitive identifier") rather than operator-by-operator. The enforcement model matches the way they think. The friction is in the learning curve, not in the ongoing use of the feature once learned.

### In a longer precept file: does it help or add noise?

In a file with 15 fields and 30 guards, having `~string` on three or four identifier-like fields (Email, CouponCode, CountryCode, DepartmentCode) reduces noise in the body of the file. Every comparison on those fields uses `~=` — not because the author remembered to, but because the compiler will not accept `==`. The body of the file becomes more consistent and readable, not less, because the author cannot accidentally mix comparison styles.

Without `~string`, a long file with many guards is a hidden maintenance problem: any `==` on an email field is potentially a bug, and the author must audit all of them. With `~string`, the audit is structural — the compiler does it.

---

## Author journey walkthrough

### Step 1: Author writes `field Email as ~string`

First-time encounter. The author has seen `~=` in documentation or samples, so `~string` might trigger the right intuition. If not, completion hover text reading *"case-insensitive string — comparisons will require `~=`"* is essential. Without that hover text, the author is guessing. **Risk: low if tooling is complete; high if completions are bare.**

### Step 2: Author writes `when Email == "admin@example.com"` — error fires

The error fires immediately in the editor. The error message (with improvements above) reads: *"`Email` is declared `~string` — comparisons must ignore letter case. Use `~=` instead of `==`."* The author corrects it. This step is smooth. **Author feels: informed and guided.**

### Step 3: Author writes `when Email ~= "admin@example.com"` — clean

No issues. The guard reads correctly. **Author feels: done.**

**Variation — Step 2b: Author writes `ensures Email == ""`**

The error fires and says "use `~=`." The author's reaction: *"I'm checking if Email is empty. Why do I need the case-insensitive operator for that?"* Without the additional message ("or declare `notempty` to eliminate this check entirely"), the author will use `Email ~= ""` correctly but with confusion. **Author feels: vaguely puzzled but compliant.** With the additional message, the author has an opportunity to write `field Email as ~string notempty` and eliminate the check entirely. **Author feels: taught something useful.** The difference between these outcomes is entirely in the error message.

### Step 4: Author adds `field AdminEmails as set of string` and writes `when AdminEmails contains Email` — error fires

This is the hardest step. The error points at a different field than the one the author just modified. The message (with improvements above) must lead with the domain consequence: *"This `contains` test will treat `"admin@example.com"` and `"Admin@Example.COM"` as different values."* The author must understand that their change to `Email` revealed a latent mismatch in `AdminEmails`.

**Author feels (without improved message): confused, possibly defensive ("I didn't change AdminEmails")**
**Author feels (with improved message): surprised but understanding — "oh, AdminEmails needs to know too"**

This step requires the most from the error message. The current proposed message is not sufficient for this audience. The improved message must be concrete about the domain consequence and explicit about the fix.

### Step 5: Author fixes to `field AdminEmails as set of ~string`

Clean resolution. The author has now understood the relationship between field types and collection membership semantics. **Author feels: helped, has learned something real about their data model.**

### Journey verdict

The journey is smooth at steps 1, 2, and 3. Step 4 is the friction point, and the quality of the author's experience at that step is almost entirely determined by the error message. The journey overall leaves the author with a more correct precept and a deeper understanding of their own domain. That is the right outcome. But it requires the tooling to carry more weight than Frank's current error message designs allow.

---

## Recommendation to owner

**Ship it, with targeted amendments to error messages and tooling.**

The feature is the right design for the right audience. Domain experts who write governance rules for email addresses, coupon codes, and identifier fields will make the `==`-vs-`~=` mistake silently and never know it. The enforcement model is protective, not pedantic — it is precisely the kind of structural prevention that Precept's philosophy promises.

**Required before shipping:**

1. **Improve the three error messages** using the concrete-consequence framing above. "Case-sensitive" and "case-insensitive" as standalone terms are insufficient for this audience. Show them what the mismatch *does* to their data values.

2. **`CaseInsensitiveValueInCaseSensitiveContains` must give two concrete fix paths**, not one, and must acknowledge the "you may not own that field" scenario explicitly.

3. **Completion hover for `~string`** must explain the feature in plain English before using any technical vocabulary. The hover text on the `~string` completion candidate is the primary discoverability path for domain experts who have not read documentation.

4. **The empty-string check error message** must additionally recommend `notempty` as the structural alternative. Authors who end up writing `~= ""` in production have been failed by the tooling.

**The mixed-collection enforcement rule (Frank's Rule 2) must ship in the same slice.** Frank is right: Rule 1 without Rule 2 creates the false impression of safety. A feature that signals protection while leaving the most dangerous composition case unguarded is worse than no feature at all.

The syntax (`~string`) is correct. The enforcement model (Option 3) is correct. The error messages need work. Ship it right.
