# Parser Rebuild Reassessment: AI Velocity Correction

**Author:** Frank (Lead/Architect)  
**Date:** 2026-05-02T19:07:59-04:00  
**Triggered by:** Shane's correction — the entire parser was built from specs in 2 days using AI  
**Status:** Correction to frank-architecture-decision-parser-rebuild.md

---

## Where I Was Wrong

My memo's primary argument for Path C over Path B was time cost: "1-2 weeks of careful work" for a rebuild vs. 2-3 days for targeted improvements. That estimate was implicitly calibrated to human development velocity. Shane is right to flag this — when the original parser was built from spec in 2 days with AI, a rebuild estimate of 1-2 weeks is indefensible. I anchored on the wrong variable. Time cost was never the real differentiator; I gave it too much weight because I wasn't thinking about how this team actually builds.

## Does This Change the Recommendation?

**It weakens Path C's advantage significantly, but Path C still wins — on different grounds.**

With AI velocity, the time gap between Path B and Path C collapses. A rebuild is ~2 days. Targeted improvements are ~2 days. Time is a wash. But time was only one of three risk axes, and honestly, it was the least important one. The remaining two haven't changed:

- **Design risk is unchanged.** George's three unsolved gaps — stashed-guard pattern, split-modifier problem, inline variant-action detection — are design problems, not coding problems. AI writes code fast. AI does not solve open design questions faster. A rebuild that hits any of these gaps mid-flight stalls on design, not velocity. Targeted improvements (Path C) sidestep all three because they don't require solving them.

- **Regression risk is reduced but not eliminated.** 2000+ tests still need to pass. AI can run them fast and iterate fast, but a rebuild that introduces a subtle grammar regression in an edge case the tests don't cover is just as dangerous whether it took 2 days or 2 weeks to write. The current parser embeds implicit grammar knowledge that isn't in the spec — it's in the code. A clean-room rebuild with AI risks rediscovering edge cases, and AI is particularly bad at knowing what it doesn't know.

## Revised Recommendation

**Path C still wins, but the argument is now purely risk-based, not time-based.** The case is simpler and more honest:

- Path B and Path C take the same time (~2 days).
- Path C solves the Slice 5 blocker without touching the three unsolved design gaps.
- Path B requires solving all three design gaps simultaneously, under rebuild pressure, with no incremental fallback if one of them stalls.
- Path C leaves the parser green and testable at every step. Path B is atomic — it's broken until it's done.

The question isn't "can we rebuild fast?" — yes, obviously. The question is "should we take on three unsolved design problems as prerequisites for work that doesn't require solving them?" The answer is still no.

## The One Thing to Nail If You Rebuild Anyway

If Shane decides Path B despite this, the single most important design decision to lock before writing a line of code is **the stashed-guard pattern** — how the parser handles tokens consumed out-of-order before disambiguation. This is the gap that breaks the slot-sequential model most fundamentally. The split-modifier and variant-action problems have plausible workarounds; stashed-guard does not, and it affects the core parsing loop architecture. Solve that on paper first. If the solution is clean, the rebuild will be clean. If it's not, you'll know Path B was wrong before you've wasted 2 days proving it.

---

*My original memo overweighted time cost because I wasn't calibrated to AI-assisted development velocity. Shane's correction is fair. The recommendation survives, but the honest reason is risk avoidance, not schedule savings. I should have said that the first time.*
