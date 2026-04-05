# Expression Tracking Notes

Use the `dsl-expressiveness` tag for language proposals whose main value is helping authors say something they cannot say cleanly today, or cannot say at all, in Precept.

## Belongs under `dsl-expressiveness`

- Missing language vocabulary or reusable expression forms
- Proposals backed by the research in this folder
- Capability gaps where comparable tools express the idea more directly
- Current examples: named guards (#8), ternary expressions in `set` (#9), string `.length` (#10)

## Does not belong under `dsl-expressiveness`

- Pure shorthand that mainly removes repeated headers or boilerplate
- Routing or authoring convenience changes that do not materially expand what authors can express
- Current examples: `absorb` shorthand (#11), inline `else reject` (#12)

## How it differs from `dsl-compactness`

- `dsl-expressiveness` = new or missing expression power
- `dsl-compactness` = less ceremony for semantics the DSL already mostly supports

A proposal may carry both tags if it genuinely does both, but `dsl-expressiveness` should stay reserved for the capability-gap slice of the roadmap.
