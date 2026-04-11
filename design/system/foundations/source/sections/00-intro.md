### A semantic system should feel authored, not merely exported.

Precept needs a document surface that reads with the same governed precision as the product itself. The point is not decoration. The point is that a reader can recognize **Structure**, **State**, **Event**, **Data**, and **Rule** quickly across code, runtime views, and explanation.

This composed pass is no longer just a four-band experiment. It is a fuller rehearsal for what a hybrid source system could look like when the same document carries editorial framing, reference galleries, and runtime proof without collapsing back into one giant handcrafted file.

- Markdown stays responsible for narrative flow, section framing, and short reference notes.
- HTML islands stay responsible for dense layouts where hand-shaped structure is the actual content.
- A tiny local builder keeps the contract explicit and inspectable.

The reading contract still maps to the layout study:

1. **Tier A** narrows and explains.
2. **Tier B** widens and compares.
3. **Tier C** spends width on proof.

```precept
precept FeeSchedule

field BaseRate as number
field IsWaived as boolean default false

rule BaseRate >= 0 because "Rates cannot go negative"
```

### Why the split matters

If every part of the system is forced through markdown, the visual proof becomes awkward. If every part is handwritten HTML, the document becomes expensive to revise. A hybrid source lets each section use the lighter format until the layout itself becomes the argument.

This means the document can stay legible to an AI as authored prose and structure, while still reserving authored HTML for the moments where the visual system itself is the thing being specified.