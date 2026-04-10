# Session Log — 2026-04-08T23:50:00Z

**Topic:** Exploratory MCP regression + charter/template bake-in  
**Branch:** `feature/issue-22-data-only-precepts`

---

Soup Nazi executed exploratory MCP regression rounds 1+2 against the data-only precepts implementation. All 7 outcome kinds confirmed. 18 compile probes — 15 pass as authored; 3 test-plan syntax errors found (not engine bugs). All corrected probes pass. Five authoring corrections documented.

Coordinator baked corrections into Soup Nazi's charter (new MCP Regression Testing skill section) and added MCP regression as a required checklist item on the language-proposal issue template.

Result: regression methodology is now self-improving — authoring errors from live execution feed back into the charter before the next round.

**Artifacts produced:**
- `.squad/decisions/inbox/soup-nazi-mcp-regression-exploratory.md` (Soup Nazi)
- `.squad/agents/soup-nazi/charter.md` updated (Coordinator)
- `.github/ISSUE_TEMPLATE/language-proposal.yml` updated (Coordinator)
