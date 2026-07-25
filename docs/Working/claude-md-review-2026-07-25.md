# CLAUDE.md review — findings for the reset phase

**Status**: Draft findings — 2026-07-25. Not a design. Deferred to phase 1 of the compiler
completion plan by owner decision, 2026-07-25.

This is the output of an independent review commissioned by Shane: research what a CLAUDE.md
should be, then review this one against that standard. Three research legs (Anthropic guidance,
field practice, what loading the file costs here), three independent reviews, one attack on
those reviews, one proposal.

**Four findings were verified directly in session and are not in doubt:**

1. The writing rule committed today bans three of Precept'''s own domain words. `denominator`
   appears 77 times in `docs/language/business-domain-types.md`; `anchor` is a modifier family
   with 17 hits in `src/Precept/Language/Modifiers.cs`; `witness` is in the spec'''s own
   definition of a proof outcome. The rule cannot be obeyed while documenting the type system
   correctly. Needs a named, closed carve-out for exactly those three.
2. That rule instructs itself to be copied into every sub-agent and workflow prompt. Zero copies
   exist — `grep -rl "plain engineering prose" .claude .github CONTRIBUTING.md` returns nothing.
3. Three documents still claim the catalogs are the language specification, unqualified:
   `docs/agent-onboarding.md:27`, `.github/copilot-instructions.md:22`,
   `docs/language/catalog-system.md:1279`. The first is on CLAUDE.md'''s own read-first list.
4. Two paths in CLAUDE.md died three months ago — `design/brand/research/` and
   `design/system/research/` moved to `research/brand/` and `research/design-system/` in
   commit `441acab5`.

**One correction to an earlier claim made in the main session:** the assertion that the file is
too long is not supported. The one controlled experiment found is reported below and shows no
compliance difference across 25/100/250/500-line files. The proposal is argued from
contradiction, staleness and duplication instead, and says so explicitly.

---

# CLAUDE.md — proposal for change

## The verdict

The file is in decent shape and better written than most of its kind, but it is carrying about 100 lines it does not need and — more seriously — it currently contains a rule that argues against itself. It is 368 lines, 4,447 words, 31,553 bytes (`wc -l -w -c CLAUDE.md`, run today). Every line is paid for on every turn of every session and every sub-agent spawn. It has grown from 237 lines on 24 May to 368 today across ten commits, and not one of those commits ever made it shorter.

The single most important finding is not about length. The correction added today — that the catalogs are not yet the language specification and must not be worked as if they were — exists only in CLAUDE.md. Four other documents still make the unqualified opposite claim, and CLAUDE.md:95 tells agents that when a catalog and a canonical document disagree, the canonical document wins. So an agent that follows CLAUDE.md:36 and reads `docs/agent-onboarding.md:27` ("The catalogs together ARE the language specification; they are not commentary about it") has been instructed by CLAUDE.md itself to prefer that sentence over the correction. Verified today: `docs/language/catalog-system.md:99` and `:1279`, `docs/agent-onboarding.md:27`, `.github/copilot-instructions.md:22`. This needs fixing whether or not a single line is trimmed.

Two smaller correctness problems: three of today's rules instruct themselves to travel into other prompts and none of them has (`grep -rl "plain engineering prose" .claude .github tools docs CONTRIBUTING.md` returns nothing, exit 1); and two file paths in the file are dead — `design/brand/research/` and `design/system/research/` were moved to `research/brand/` and `research/design-system/` in commit `441acab5` three months ago. The `research/` subfolder list at line 77 also omits three folders that exist. Two of the three independent reviewers claimed every path in the file resolves; both were wrong, and both were using that claim to argue the file's pointers are trustworthy.

On the numbers that can be measured: 10 of 18 top-level sections are marked "(Non-Negotiable)". Roughly 96 sentences issue an order. Line 302's "~3600 tests across 4 projects" is wrong twice over — `find test -name "*.csproj"` returns five projects, and the `[Fact]`/`[Theory]` attribute count alone is 4,254 before any `[Theory]` expands into cases. Line 317 lists four VS Code tasks; `.vscode/tasks.json` defines seven, and one of the three missing is `grammar: regenerate`, which is the task that produces the file line 110 forbids you to hand-edit.

## The standard we measured against

The checklist was built from three bodies of research and applied section by section, each item as a yes/no question with a command behind it.

**On size.** Anthropic's own documentation gives a target of under 200 lines and says longer files reduce adherence, but cites no experiment. The one controlled experiment reported (1,650 Claude Code sessions across 25/100/250/500-line files) found *no* difference in compliance — 60.0%, 65.2%, 67.7%, 64.0%, p = 0.16. Work on instruction *counts* (IFScale, ManyIFEval) does find degradation as the number of competing rules rises, which is the question this file actually poses at ~96 directives. So the checklist follows the instruction-count line and treats Anthropic's 200 lines as a free secondary check, not as evidence. **Nothing in this proposal depends on length being harmful.** Every recommendation below is argued from duplication, staleness, contradiction, or content the agent could read out of the repository.

**On what belongs here.** Four questions per section: can an agent break this rule before opening any other file (if yes it must stay); could Claude work it out by reading the repository (if yes, cut — this is exactly what Anthropic's own trim removes: directory layouts, dependency lists, architecture overviews); is it consulted only when a particular skill is already running (move it into the skill); does it apply to only one kind of file (move it to a path-scoped rule file).

**On duplication.** Compare each section against `CONTRIBUTING.md`, `docs/README.md`, `.github/copilot-instructions.md`, the eight skill files and two agent files, and — the check that actually decides the argument — ask whether the two copies have already drifted, because a drifted copy is a wrong fact an agent will read.

**On whether the rule is obeyed.** Take the rule's own words and grep the tree.

**Where the research was silent:** whether 368 lines costs this project any obedience at all (unmeasured, and the one relevant experiment found nothing); how many rules is too many (direction only, no threshold); whether the middle of a file this size is read less (documented only at far longer inputs); whether leaving the reasoning behind a rule in place helps a model obey it (all three sources silent — removing it is justified by cost, not by measured effect). Also unknown: what this file costs in Claude's own tokenizer (`/context` in a live session would tell you), and whether path-scoped rule files load for sub-agents as well as for the main session.

## What to change

Correctness first. These are wrong facts and self-defeating rules, and most of them save no lines at all.

| Section | What is wrong | What to do | Lines | What could go wrong |
|---|---|---|---|---|
| Catalog System 89–100, read against four other docs | The correction added today is contradicted by `catalog-system.md:99` and `:1279`, `agent-onboarding.md:27`, `copilot-instructions.md:22` — and line 95 tells the agent to believe those over CLAUDE.md | Retract the claim in all four. Phrase it as "this is the intended machine-readable form and is not complete yet", reusing CLAUDE.md:89 and :98 verbatim | 0 | Wrong phrasing ("catalogs are wrong") gives an agent licence to stop putting new language elements in catalogs, destroying the discipline line 98 protects |
| Research 77–79 | `design/brand/research/` and `design/system/research/` do not exist (moved in `441acab5`); the subfolder list omits `brand/`, `design-system/`, `references/` | Point at `research/brand/` and `research/design-system/`; complete the list | 0 | None |
| How to write, line 20 | Bans `denominator`, `anchor` and `witness`, all three real Precept vocabulary. `business-domain-types.md` uses denominator 77 times; `Modifiers.cs` defines the anchor modifier family; `precept-language-spec.md:223–229` defines the proof verdict in terms of a concrete witness configuration | Add a named carve-out for exactly those three, closed-ended | 0 | A carve-out is a foothold for arguing every banned word has a domain meaning. Naming all three explicitly and closing the list limits it. The alternative is a rule that cannot be obeyed while documenting the type system correctly — which teaches agents to weigh the file rather than follow it |
| How to write, line 22 | "Copy it verbatim into every sub-agent and workflow prompt" has produced zero copies. Same for the samples rule and the catalog correction | Paste the rule into the two agent files and eight `SKILL.md` files, then rewrite line 22 into a statement of fact. **Same commit** | ~2 | Deleting line 22 before the copies exist loses the intent entirely. Ten copies can drift — but they are short enough to diff |
| Use the MCP Tools First 334–336 | Contradiction created today: 8 of 11 files in `tools/Precept.Mcp/Tools/` call `CatalogFormatters`, the ninth runs the compiler — so line 336 says start with the tools that read the catalog while lines 97 and 120 forbid trusting either as a measure of what the language should be | Add a boundary clause: the tools tell you what is built and catalogued today, which answers "how do I write this" and does not answer "what should the language do" | −2 | Weakening the section pushes agents back to reading source and guessing DSL syntax, which it exists to prevent. This is an addition, not a cut |
| Documentation Sync, line 156 | Names `/audit` as an existing skill. `ls .claude/skills/` returns eight, no audit. `CONTRIBUTING.md:17` says it is deferred | Change to "`/research` through `/review`" | 0 | None |
| Lines 85, 102, heading 173 | The file breaks its own line-20 ban three times, once in a heading | Three word swaps; heading becomes "Which document is authoritative for what" | 0 | None |
| `.github/copilot-instructions.md` | A two-month-old fork of this file (last touched `95d8f3d1`, 16 May). Carries the unqualified catalog claim at :22, "~2000 tests across 3 projects" at :43, "5 MCP tools" at :11, three MCP config files at :66 against CLAUDE.md's four, and tells the reader to use a `get_errors` tool at :107 where CLAUDE.md says `precept_compile` | Decide whether it is live. If live, generate it from CLAUDE.md; if dead, delete it | 0 | It currently teaches a second population of agents the exact claims corrected today. Guessing wrong in either direction makes it worse — this is a question for you |

Then the trims, in rough order of confidence.

| Section | What is wrong | What to do | Lines | What could go wrong |
|---|---|---|---|---|
| Transient vs Canonical references 183–211 | 29 lines that only apply when writing a code comment, paid for on every turn including turns that touch no code. It is also half-obeyed: zero slice labels and zero finding IDs in `src/`, but 16 test files carry slice labels and 23 carry finding IDs | Move to `.claude/rules/code-comments.md` with `paths: ["**/*.cs", "**/*.precept"]`, examples intact | 29 | **Contested.** Two reviewers said move, one refused for lack of evidence about the mechanism. The attack settled it: Anthropic documents path-scoped rules, and they fire when Claude reads a matching file. What is still unknown is whether they load for sub-agents, which is where most code here gets written. Test on something small first. Separately: someone has to decide whether the test files get swept or the rule gets scoped to `src/` and `tools/` — leaving it as-is is the one option that keeps teaching agents the file is decorative |
| Build & Test 293–317 | Test count wrong on both halves; per-project list omits `test/Precept.MatrixTools.Tests/`; task list names four of seven and omits `grammar: regenerate`, which line 110's prohibition depends on | Drop the test count outright rather than fixing it (line 102 already sets that pattern). Keep the artifacts-path language-server build — that flag is not discoverable from the `.csproj` — and the npm scripts. Add `grammar: regenerate` to the task list | ~15 | Small: a fresh agent no longer sees at a glance that the suite is large. Recoverable in one command |
| Doc status conventions 57–73 | Duplicates `docs/README.md:25–45`, where the copy is better (it separates reference from working statuses and gives the inline-annotation example) and has already drifted by a word | Keep lines 59 and 73 — the two sentences that are instructions rather than a lookup list — and drop the taxonomy | ~13 | If agents do not follow pointers, an unfamiliar status costs a file read, not a wrong decision. Note: one reviewer claimed 13 of 41 canonical docs carry no status, arguing line 59 is untrue. That is wrong — a looser check gives 2 of 41 (`philosophy.md` and one contributing checklist); a strict `## Status` table check gives 7, six of them navigation READMEs. Line 59 is substantially true |
| Architecture 3–14 | A directory listing that names 6 of 14 real directories, including omitting `tools/Precept.GrammarGen` — the generator line 110 depends on | Keep line 5 (what Precept is) with the pipeline sequence, and line 14's samples caveat. Drop the folder rows | ~9 | Line 5 is the only place the file says what the product is; it must survive. The pipeline sequence is real orientation `ls` will not give |
| DSL Sample Files 354–362 vs The samples are examples 114–122 | Same topic 240 lines apart. Line 356's entire content is "go read the other section". They also pull against each other: line 120 says never treat compiler output as the specification, line 361 describes `precept_compile` as returning "diagnostics from the full compiler pipeline" | Fold 358–362 into 114–122; delete the heading. Keep "never run `dotnet build` on a `.precept` file" as its own named bullet, and reword the compile step to say what it gives you — whether the file trips the compiler as it stands today | ~8 | A careless merge blurs the mechanical fact into the judgement rule and loses the mechanical one |
| Test Conventions 364–368 | Three facts visible in any of the 281 test files, at the very end of the file. Both naming examples given (`PreceptParserTests.cs`, `PreceptRuntimeTests.cs`) do not exist | Delete. `CONTRIBUTING.md:499–503` already has it | 5 | A fresh agent writing the very first test in a new project could reach for a different framework. One sentence would cover it if that feels risky |
| Entry points 38–55 | Six of ten rows duplicate `docs/README.md`'s own table, three lines after telling the agent to read `docs/README.md` | Cut the six duplicated rows. Keep the four carrying a rule rather than a location — philosophy "read before any design decision", `docs/contributing/` (which `docs/README.md` lacks), `docs/Working/`, `docs/archive/` "never update" | ~6 | **Contested.** One reviewer wanted the whole table gone (16 lines). The attack showed `docs/README.md` does not cover four of the rows, and two of them are prohibitions. Take the smaller cut |
| MCP Tool Sync 338–343 | Applies only to `tools/Precept.Mcp/**` | Move to a path-scoped rule file alongside the code-comment rule | 6 | Same unknown about sub-agents. Small enough that leaving it is also defensible |
| Issue Implementation Workflow 345–352 | Defers to `CONTRIBUTING.md` in its own first line and then restates four rules from it, at the end of the file. `CONTRIBUTING.md:84` says the current spike branch skips the GitHub gates entirely | Keep only "never create a separate implementation-plan markdown file" and move it up next to the other document rules | ~6 | If the project returns to pull-request workflow the section becomes live again. The one rule an agent breaks unprompted is the one being kept |
| Catalog System, line 100 ("What ends this suspension") | A project plan written for you, not an instruction any agent executes | Wrap in `<!-- -->`. Block-level HTML comments are stripped before the file is injected, so it costs nothing per turn and stays in the file | ~1 free | None. The file currently has zero HTML comments — this mechanism is unused |

**Two things the reviewers recommended that should not be done.** Both were overturned.

- **Moving the doc-routing table (160–171) to `CONTRIBUTING.md`.** All three reviewers wanted this. It fails twice. Nothing routes a general session to `CONTRIBUTING.md` — `grep -rn "CONTRIBUTING" .claude/ --include=*.md` returns hits only in the execute skill and the reviewer agent, all about pull requests, and doc-sync happens on ordinary code turns. And five skill references name CLAUDE.md by name for this table: `design/SKILL.md:354, :439, :493` and `plan/SKILL.md:61, :83`. Fix it the other way: add the `Catalog architecture | docs/language/catalog-system.md` row from `CONTRIBUTING.md:67` into the CLAUDE.md table, delete the `CONTRIBUTING.md` copy, and delete its stale line 59 preamble.
- **Moving the catalog evidence at line 91 out.** All three wanted it in `catalog-system.md`; all three then noted the move is only safe once that document is corrected. Keep it visible **until** `catalog-system.md:99` and `:1279` are fixed. It is what stops an agent talking itself out of the rule on reading a canonical document that says the opposite — and three documents currently do. Once they agree, reduce it to one clause.

## What to leave alone

- **The four catalog rules at 108–112.** These are the best-obeyed rules in the file and the evidence is unambiguous: nine of eleven files in `tools/Precept.Mcp/Tools/` are exactly 13 lines and no method there exceeds 40, so the thin-wrapper rule is simply followed; `git log -- '*tmLanguage.json'` returns eight commits, all generator or catalog work, so the hand-edit prohibition has held. Line 111 in particular gives the exact code shape to look for and then draws the line that stops it being over-applied. Do not touch any of it.
- **The samples section at 114–122.** Every bullet describes a mistake an agent makes unprompted, mid-task, before any other file loads — counting sample files to claim coverage, reading absence as evidence, treating what the compiler accepts as what the language allows. Line 120's "This one feels like evidence, which is why it keeps happening" does more work than a longer explanation would, and line 122 concedes what samples *are* good for, which stops the rule reading as blanket dismissal. Nothing to cut here except the duplicate 240 lines down.
- **The consultation gate at 226–270, including "Why this exists".** One reviewer wanted the tier bodies moved into the design and research skills. That does not work: the gate fires *before* a skill is invoked — line 228 names invoking `/research`, `/design` or `/plan` as things that must not happen until the conversation has. `design/SKILL.md:36` only runs once `/design` is loaded, and its own wording says the consultation "must be satisfied" — checked, not conducted. The tier definitions are how an agent decides whether the conversation is needed at all, with nothing loaded but this file. The `units { }` incident at 262–265 is the concrete case that stops Tier 3 reading as procedure. The only saving here is collapsing the three restatements of the encouragement point (lines 230, 243, 254) into one — about eight words.
- **The philosophy principles at 132–139.** One reviewer argued for cutting them and then argued against it in the same finding. Take the second argument. "States are optional — stateless precepts are first-class" is precisely the kind of thing that gets re-derived wrongly by an agent that never opens `philosophy.md`.
- **The four-way MCP configuration block at 327–332.** The most accurate paragraph in the file — I confirmed all four files exist and which schema each uses, including that `.claude/settings.local.json` holds the enable list rather than the server definition. `CONTRIBUTING.md:471` still says three. Do not let a future cleanup resolve that duplication the wrong way.
- **The four-part rationale rule at 280–291.** Twelve lines, no padding, and obeyed on every locked design in the tree. It works because the enforcement lives in the design skill while the short statement lives here — that is the arrangement the rest of the file should copy.
- **The good/bad comment pairs at 206–209**, wherever the surrounding rule ends up. They are the rule in operational form; the abstract version does not work.
- **Line 158, the one-line doc-sync instruction**, and **line 73, the drift rule** — both are things an agent can break before opening anything else, and both stay even when the tables around them go.

## Where things would move

| Leaving | Going to | Exists? | What makes it get read |
|---|---|---|---|
| Doc status taxonomy | `docs/README.md:25–45` | Yes, and already holds a fuller version | CLAUDE.md:33 already names `docs/README.md` as a read-first document. A status is read off a document you already have open, so the lookup is one hop away at the moment it is needed |
| Six entry-point rows | `docs/README.md` Structure table | Yes | Same |
| Code-comment rule | New `.claude/rules/code-comments.md`, `paths: ["**/*.cs", "**/*.precept"]` | **No — must be created.** `ls .claude/` returns agents, skills, settings.local.json, worktrees | The mechanism itself: path-scoped rules fire when Claude reads a matching file. **Unverified for sub-agents**, which is where most code here gets written. Test before moving |
| MCP tool-sync rule | Same mechanism, `paths: ["tools/Precept.Mcp/**"]` | No | Same |
| Pull-request workflow | `CONTRIBUTING.md` § 3 | Yes, holds the full version | Line 347 already sends the reader there, and the moment it is needed is the moment someone opens a PR |
| Build commands and test conventions | `CONTRIBUTING.md:412–503` | Yes, and itself needs the fifth test project added | Weak. Nothing routes a general session to `CONTRIBUTING.md`. But `dotnet build` and `dotnet test` are not knowledge anyone needs routing to |
| Catalog evidence (line 91), *after* the canonical docs are corrected | `docs/language/catalog-system.md` | Yes | That is where someone goes to argue with the catalog claim, and it is the file being edited anyway |
| "What ends this suspension" (line 100) | Stays in CLAUDE.md, wrapped in `<!-- -->` | n/a | Not a move at all — it stays in the file for you and costs nothing per turn |

**Honest note on two of these.** Moving the build commands and test conventions to `CONTRIBUTING.md` is close to deletion, because nothing makes agents open that file outside the execute skill. I am recommending it anyway because the content is recoverable from the `.csproj` files in one command — the pointer is not doing the work, the repository is. The same is not true of the routing table, which is why that one must not move.

## What the file would look like afterwards

Roughly **265 lines**, down from 368 — about a 28% cut. Not the 190 two reviewers projected, because three of their larger cuts do not survive scrutiny (routing table, consultation tiers, philosophy principles). Directive count falls from about 96 to somewhere near 70.

Proposed section list, in order:

1. **Architecture** — one sentence on what Precept is, with the pipeline sequence. No table.
2. **How to write (Non-Negotiable)** — the rule, with the three-word carve-out, and line 22 rewritten as a statement of fact.
3. **Documentation Map** — read-first documents (unchanged); four entry-point rows that carry a rule; two sentences replacing the status taxonomy; Research with the paths corrected; the `.squad/` boundary.
4. **Catalog System (Non-Negotiable)** — the statement, the evidence (until the canonical docs are corrected, then one clause), the four rules. Line 100 as an HTML comment.
5. **The samples are examples, not a specification (Non-Negotiable)** — with the two `.precept` operational rules folded in.
6. **Product Philosophy (Non-Negotiable)** — unchanged.
7. **Documentation Sync (Non-Negotiable)** — the sync instruction, the routing table (now with the catalog row), "Which document is authoritative for what", "When research is involved". The code-comment rules gone to a path-scoped file.
8. **DSL Authoring (Non-Negotiable)** — unchanged.
9. **Pre-Design Owner Consultation (Non-Negotiable)** — unchanged but for collapsing three restatements into one.
10. **Language Surface Design (Non-Negotiable)** — unchanged.
11. **Per-Decision Rationale (Non-Negotiable)** — unchanged.
12. **Build & Test** — the language-server build with its artifacts flag, the npm scripts, the corrected task list. No test count.
13. **Development Workflow** — the four MCP configuration files (unchanged), and the one non-obvious fact from the build loop: changing `src/` needs no reload, changing the extension does.
14. **Use the MCP Tools First** — with the boundary clause.
15. Plus one line, placed with the other document rules: never create a separate implementation-plan markdown file.

Sections gone entirely: MCP Tool Sync (moved), Issue Implementation Workflow (one line kept), DSL Sample Files (merged), Test Conventions (deleted).

## Risks

**The catalog correction gets weaker before it gets stronger.** Right now CLAUDE.md is the only place it exists. If anything is trimmed from lines 89–100 before `catalog-system.md` and `agent-onboarding.md` are fixed, the correction loses its footing entirely. Order matters: fix the canonical documents, verify by grep, and only then touch this file.

**Path-scoped rules might not reach sub-agents.** Anthropic's documentation confirms they fire when Claude reads a matching file; it says nothing about sub-agent loading. Sub-agents write most of the code here. The code-comment rule is currently *half* working — clean in `src/` and `tools/`, broken in about 39 test files — and a bad move turns half-working into not working.

**Pointers may not get followed.** Five of these changes replace content with a pointer. There is a widely-repeated complaint that Claude often does not open documents it is told to open. I have no measurement of that for this repository. If it is true here, the status-taxonomy and entry-point cuts cost more than they save. Both are cheap to reverse.

**Copying the writing rule into ten files creates ten copies that can drift** — the exact failure this whole review is complaining about. A hook or `--append-system-prompt` would be more reliable, but no hooks are configured anywhere in this project, so that is new infrastructure.

**Dropping the "(Non-Negotiable)" mark from a section reads to an agent as a downgrade.** That is the point of reducing it from ten, but it means the choice of which sections keep it is a judgement about consequence, and should be yours.

**None of this is measured to improve obedience.** Every recommendation rests on duplication, wrong facts, contradiction, or content the repository already answers. If length turns out not to matter at all, the correctness fixes still stand and the trims simply save less than they appear to.

## Open questions for Shane

1. **Is `.github/copilot-instructions.md` still in use?** It is a two-month-old fork carrying six wrong facts, including the unqualified catalog claim corrected today. If it is live, the fix is generating it from CLAUDE.md; if it is dead, delete it. Guessing wrong makes it worse either way, and I could not tell from the tree.

2. **The ~39 test files with slice labels, finding IDs and Decision numbers in comments — sweep them, or scope the rule to `src/` and `tools/`?** As it stands the rule is obeyed in production code and broken in test code, which teaches agents that the file is applied selectively. Either answer fixes that; leaving it does not.

3. **Which sections keep "(Non-Negotiable)"?** Ten of eighteen carry it now. My suggestion would be the ones where a violation cannot be undone by the next commit — do not edit `philosophy.md` without approval, do not settle syntax in chat, do not skip the owner conversation — but which four or five matter is a judgement about consequence that belongs to you, not to me.