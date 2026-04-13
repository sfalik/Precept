# Steinbrenner Sample Ceiling Plan

Date: 2026-04-08  
Owner: Steinbrenner (PM)  
Scope: sample-portfolio planning

## Executive recommendation

- The repo should operate in a **30-36 total sample** range, not on an automatic "double it" rule.
- Treat **42 total samples** as the **hard upper bound**, not the default target.
- Keep a **core maintained canon of 12-14 flagship samples** for README/docs/proposals, plus **2 tiny teaching controls** kept in-repo for smoke/onboarding.
- Use the remaining **extended set** to broaden domain coverage without forcing every sample to carry equal documentation and review weight.

The PM call: we need enough samples to show breadth, but not so many that the library turns into an uncurated shelf. Past about the mid-30s, discoverability and maintenance cost rise faster than user value.

## Why the ceiling should be lower than "just double"

The existing research supports expansion, but it also points to a more disciplined upper end:

- George's audit shows the current **21-sample** corpus is still too skewed toward cleaner intake/review flows and needs denser policy, date, money, exception, and data-model pressure.
- Frank's philosophy research says samples are **not tutorials, not stress tests, and not aspirational fiction**. That raises the maintenance bar for every file we keep.
- Peterman's benchmark work says the strongest Precept story comes from **serious, evidence-bearing, exception-rich case files**, not from a very large shelf of lightweight examples.
- `docs\@ToDo.md` already anticipates **sample integration tests** later. That means every added sample eventually becomes part of the quality surface, not just a loose doc artifact.

So yes: the corpus should grow. But the realistic upper end is set by **curation economics** as much as by language pressure.

## Recommended operating model

### 1. Core maintained canon

Keep **12-14 flagship samples** as the canon that must stay continuously polished, easy to find, and suitable for issue/proposal citation.

This canon should:

- represent the major business lanes Precept wants to win
- cover starter, standard, and advanced difficulty
- include the strongest roadmap-pressure anchors
- be the set that README/docs point new users toward first
- get the deepest review whenever language behavior or sample guidance changes

Recommended canon composition:

| Slice | Count | Notes |
|---|---:|---|
| Flagship business samples | 8-10 | finance, claims/casework, scheduling/logistics, workplace/identity, public-sector/regulatory |
| Secondary onboarding-friendly business samples | 2-4 | simpler but still realistic files for first-read value |
| **Core canon total** | **12-14** | Main maintained showcase |

### 2. Teaching controls

Keep **2 tiny teaching/control samples** (`trafficlight`, `crosswalk-signal`) in-repo, but **do not treat them as canon** and **do not let them drive roadmap arguments**.

They still earn their keep as:

- syntax smoke samples
- tiny onboarding examples
- quick sanity checks for preview/demo moments

### 3. Extended set

Use the extended set for broader coverage once the canon is healthy.

This set should include:

- alternate domains that improve breadth
- niche but credible workflows
- data-only/reference samples once that lane is ready
- second-tier examples useful for research, but not required to carry README-level prominence

Recommended extended-set size:

| Portfolio layer | Count |
|---|---:|
| Core canon | 12-14 |
| Teaching controls | 2 |
| Extended set | 16-20 |
| **Operating total** | **30-36** |

## Tradeoffs as sample count grows

### Maintenance cost

Realistic samples are expensive to keep honest. Each file needs:

- domain-credible naming
- comments that explain current compromises without becoming fantasy design docs
- updates when shipped language features remove current workarounds
- periodic review against the product story

At **30-36**, that remains manageable with explicit canon/extended-set discipline.  
At **37-42**, the team starts paying for samples it cannot actively curate.  
Beyond **42**, the repo risks becoming a sample graveyard unless dedicated ownership and tooling appear.

### Review cost

Review burden does not scale linearly because the best samples are policy-dense.

- A 6-sample increase is not just 6 more filenames.
- It is more proposal-citation surface, more "is this still believable?" review, and more churn whenever features like `choice`, `date`, `decimal`, computed fields, or field constraints ship.
- Reviewers can keep a **known-good canon** in their heads; they cannot hold 40 equally important samples there.

### Test burden

Today the samples are mostly documentation/research assets. The roadmap says they should gain stronger integration-test value later.

As the count grows:

- compile-smoke burden rises first
- scenario-test burden rises next
- update burden rises again when a language feature should refresh multiple samples in one pass

That argues for:

- **all samples** compiling clean
- **canon samples** getting the deepest scenario/test attention first
- extended samples staying lighter unless they become important roadmap anchors

### Discoverability and onboarding

This is the strongest reason not to treat 42 as the default target.

- New users do not want a wall of similarly weighted files.
- Docs and README can only highlight a small, curated set before choice overload kicks in.
- A good sample library should answer "where do I start?" in seconds.

That is realistic at **12-14 canon samples** and still workable with a **30-36 total library**. It gets much harder when the repo approaches the 40s without strong indexing and taxonomy.

## Growth bands

| Total sample count | PM read |
|---|---|
| 21-24 | Still underpowered for roadmap pressure; breadth gaps remain obvious |
| 25-30 | Healthy next step; enough room to deepen anchors and add missing lanes |
| 31-36 | Best operating range; broad without becoming noisy |
| 37-42 | Only justified if indexing, metadata, and canon discipline are already in place |
| 43+ | Not recommended under current staffing/maintenance assumptions |

## Concrete recommendation

### Near-term target

**30 total samples**

Why:

- large enough to fix the current breadth problem
- small enough to keep the next pass focused on anchor rewrites plus the highest-value missing lanes
- consistent with the need to improve docs/onboarding quality, not just count

### Medium-term cap

**36 total samples**

Why:

- enough space for a serious extended set
- still browseable if the canon is clearly marked
- still plausible to review when language changes land

### Hard upper bound

**42 total samples**

Why:

- it preserves the earlier instinct that the corpus can grow substantially
- it should be treated as the red line where the repo stops gaining much user-facing clarity and starts accumulating curation debt
- crossing it should require explicit justification, better sample indexing, and evidence that the existing set is already well maintained

## What this means for the current plan

The earlier **42-sample** plan was directionally useful because it forced broader domain thinking. But the better PM framing is:

- **30** is the next planned target
- **36** is the medium-term cap
- **42** is the maximum credible ceiling under current maintenance assumptions

In other words: **42 should move from "the plan" to "the limit."**

## Practical sequencing

1. Keep the rewrite-first rule for the existing roadmap anchors.
2. Grow to **30** by filling the highest-value missing lanes, not by trying to hit a symmetric domain spreadsheet.
3. Establish the **canon vs extended** split explicitly in docs/indexing.
4. Only push into the **31-36** range once the canon is stable and obviously useful for onboarding and proposal work.
5. Treat movement toward **42** as exceptional, not routine.

## PM conclusion

Precept needs a stronger sample portfolio, but not an endlessly larger one. The right upper-end plan is a **curated library with a clear canon**, not a brute-force doubling exercise.
