# Response: "Built From Spec" Challenge

**Author:** Frank (Lead/Architect)  
**Date:** 2026-05-02T19:11:32-04:00  
**Replying to:** Shane's challenge on the implicit-knowledge regression argument

---

Shane is right, and I should have caught this myself. If the parser was built from the spec using AI, then "implicit grammar knowledge that isn't in the spec — it's in the code" is either wrong or it's an indictment of the spec, not a defense of the code. Those are the only two options. Either the spec is complete enough that a rebuild reproduces the behavior, in which case my regression argument is empty — or the spec has gaps that the original build silently papered over, in which case those gaps are bugs we're choosing to keep. Neither option supports preserving the existing code on regression grounds. The implicit-knowledge argument is dead. I withdraw it.

If behavior did diverge from the spec during the original build — and in a 2-day AI sprint, it almost certainly did in small ways — then those divergences are *undocumented spec gaps*, not earned knowledge. They're liabilities, not assets. The correct response is to close the gaps in the spec so that any rebuild, by any agent, at any time, produces the same parser. Using "the code knows things the spec doesn't" as a reason not to rebuild is exactly backward — it's a reason the code *should* be rebuilt, after the spec is fixed, so the implicit knowledge becomes explicit and reproducible. Shane's logic here is airtight.

This does change my position, but not as much as it might seem. The regression argument was the weaker leg. Path C's surviving case rests entirely on the three open design gaps: stashed-guard, split-modifier, and variant-action detection. Those are real, they're unsolved, and they don't go away because the spec argument collapsed. A rebuild still requires solving all three simultaneously under rebuild pressure. Path C still avoids that. But I'll be direct: if Shane solves those three on paper before starting — or demonstrates they're already covered by the spec and catalogs — then Path C has nothing left, and I'll say so. The design gaps are the last load-bearing wall. If they fall, so does the recommendation.
