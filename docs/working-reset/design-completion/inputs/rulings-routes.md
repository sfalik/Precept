# Rulings routes — lookup by row number

Working file for scripts and agents, not for Shane. Step 0 commits it as `inputs/rulings-routes.md` and
seeds the route of each row of `rulings.jsonl` from it. Row numbers are those of `history-rulings.md`.
The plan (§ 5) states the same routes in words; where the two differ the plan stands. The conflict codes
(C, T, P, N and the cold-read numbers) are already carried by `technical-canon-consistency.md` and
`technical-proof-model.md`, and step 0 copies them into `technical/conflicts.jsonl`.

Outcome codes: 1 = canonical line recorded; 2 = carried by a plan rule, a `CLAUDE.md` line or a
`handoff.jsonl` row; 3 = superseded (needs the later words quoted and a `uses/` file); 4 = no longer
applies (same requirement).

| Rows | Route | Outcome aimed at |
|---|---|---|
| 5, 8, 9, 10, 13, 15, 18-23, 26, 31, 33, 43, 45, 46, 47, 49, 50, 53, 54, 57, 58, 60, 61, 66, 67, 69, 70, 71, 74, 75, 78, 80, 88, 93, 95, 96 | Already in canon; the row records the line | 1 |
| 4 | Step 6 removes the permanent-limit text; plan § 4.3 | 1 |
| 6, 51 | Steps 4 and 11 | 1 |
| 12 | In canon (spec:270); step 6 defines "states the condition directly" and is bound by it | 1 |
| 28 | Step 10 | 1 |
| 42 | Build order only; step 2 designs the model. **Barred as a refusal reason** | 1 |
| 59 | About the first build; steps 8 and 12 state the three verdicts. **Barred as a refusal reason** | 1 |
| 65 | Stays deferred on his words; the one exception under finish conditions 6 and 7; one information line in the first batch | 1 (canon records it open, with his words) |
| 87 | Question sheet 2; step 2 | 1 |
| 3 | Step 14 (pointer form, ownership table, document-by-document pass); step 12 for the frozen list | 2 |
| 11 | Acceptance criteria in step 0; diagnostics in steps 8 and 12 | 1 |
| 14 | Step 4 | 1 |
| 24, 27 | Step 11, spec guarantee section; philosophy not edited | 1 |
| 25 | Step 8; `language-server.md` in step 12 | 1 |
| 35 | Step 12 specifies the catalog entries the checker asks; also an input to the check on question 8 | 1 |
| 38, 56 | Step 4 | 1 |
| 62, 64 | Mechanical edit in step 14; decisions stated in steps 5 and 12 | 1 |
| 63 | Carried by the plan lines in steps 2 and 5: waiver covers period ordering only | 2 |
| 68, 90 | A checker in step 14 confirms each against canon and records the line, or opens a finding | 1 |
| 81 | Record only; fourth part superseded, later words quoted | 2 / 3 |
| 98 | Mechanical edit in step 14 | 1 |
| 7, 55, 37, 39, 40, 52 | Step 4 | 1 |
| 16 | Build order only; mechanical edit in step 14; design owed in step 12. **Barred as a refusal reason** | 1 |
| 17 | Question sheet 1 (three options); step 4; the sentences in step 12 | 1 |
| 29, 30 | Steps 10 and 12 | 1 |
| 32 | Question sheet 7; step 3 | 1 |
| 41 | About the first build; step 6 states the intended rewrites and marks the first build's set. **Barred as a refusal reason** | 1 |
| 91 | Steps 2 and 6 | 1 |
| 92 | Words recorded (memory file `project_boundary_define_what_i_want_2026_07_16.md:175`); steps 3 and 4 | 1 |
| 1, 2, 36, 72, 79, 99 | Plan § 1, § 4.1, § 4.4 | 2 |
| 34 | Plan § 9 | 2 |
| 77 | Plan § 2.3 | 2 |
| 120 | Build-stage rule in `handoff.jsonl`, step 13 | 2 |
| 73 | The checks read all of canon | 2 |
| 101-110, 113, 119 | Plan § 4.1, § 4.7, § 6, § 7 | 2 |
| 111, 118 | Plan § 7.2, § 7.5 | 2 |
| 112 | Plan § 4.1, § 6.2 | 2 |
| 114, 115 | `handoff.jsonl`, step 13 | 2 |
| 116, 117 | Plan § 4.1 | 2 |
| 121 | Plan § 4.7 | 2 |
| 44, 48, 76, 82, 83, 84, 85, 94, 97, 100 | Superseded; each row names and quotes what replaced it | 3 |
| 89 | Checker in step 14 looks for the example. Found with `in 'each'`: line recorded. Found with another unit: mechanical edit. Not found: stays open, on the sign-off list | 1, or open |
| 86 | Checker in step 14 looks for his words. Found: becomes a question (touches the philosophy). Not found: stays open, on the sign-off list; never recorded as dropped | open until he answers |

Rows barred as refusal reasons (plan § 4.3, reason 4): 16, 41, 42, 59. `refusal-check` reads this list.
