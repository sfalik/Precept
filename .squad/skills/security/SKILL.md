---
name: "security"
description: "Domain knowledge skill for Precept security. Covers where security research lives, how to capture new findings, how to use them in decisions, and how to keep the threat model current."
domain: "security"
confidence: "high"
source: "earned — generalized from language-design audit; ensures security work is grounded in Precept's specific threat model, not generic OWASP advice"
---

## Context

This skill governs how agents work with the **security research corpus** — reading the threat model before making security-relevant decisions, capturing new findings, and keeping the security survey current as the product evolves.

**Applies to:** Any agent doing security-related work — threat modeling, input validation, supply chain, parser safety, MCP transport, or distribution security. Primary user: Uncle Leo (Security Champion). Also: George (runtime input handling), Newman (MCP transport), Kramer (extension security).

## Research Location

| Category | Path | Contents |
|----------|------|----------|
| Security survey | `research/security/security-survey.md` | Complete product architecture, security domains by category, deployment model, consumers, threat vectors |
| Product philosophy | `docs/philosophy.md` | Core guarantees (prevention, determinism, inspectability) — security claims must align |
| Runtime API design | `docs/RuntimeApiDesign.md` | API attack surface |
| MCP design | `docs/McpServerDesign.md` | MCP transport and tool input handling |

### Security domains mapped in the survey

| Product category | Key security domains |
|---|---|
| DSL parser/interpreter | Code injection, input validation, resource budgets, ReDoS, error message scrubbing |
| NuGet package | Supply chain integrity, package signing, SLSA, dependency monitoring |
| Language server (LSP) | stdio transport isolation, single-tenant model, document text handling |
| MCP server | stdio transport, AI host trust boundary, prompt injection via DSL text |
| VS Code extension | Extension host sandbox, webview security, marketplace distribution |
| CI/CD pipeline | Workflow permissions, secret management, artifact integrity |

## Using Research in Work

### Before any security decision

1. Identify which **product category** is affected (see table above)
2. Read the relevant section in `research/security/security-survey.md`
3. Cite the **specific threat vectors and principles** documented there
4. If the concern isn't covered by the survey, flag it as a gap

### Citation standard

| Acceptable | NOT acceptable |
|---|---|
| "Per security-survey.md §DSL Parser: 'no size limit is enforced before tokenization runs'" | "Input should be validated" |
| "Per security-survey.md §Deployment: Core NuGet runs in-process, no network, no file I/O" | "The library doesn't do network calls" |
| "Per security-survey.md §DSL Parser: error messages embedding field names from untrusted DSL can become injection vectors for AI consumers" | "Error messages could leak data" |

**Rule:** If a claim could be made by any security engineer without reading the survey, it is not a citation. Cite using Precept-specific threat model.

## Capturing New Research

### Where to put it

| Type of finding | Location |
|-----------------|----------|
| New threat vector or domain analysis | Update `research/security/security-survey.md` under the relevant product category |
| New product category or distribution channel | Add a new section to the survey |
| Specific vulnerability finding | `research/security/{finding}.md` + reference from survey |
| External reference (OWASP, SLSA, etc.) | Cite in the survey; don't create separate reference copies |

### Format consistency

New survey sections should follow the existing pattern:
- **"Why it applies to Precept"** — map the domain to Precept's specific architecture
- **"Authoritative resources"** — external references with URLs
- **"Top applicable principles"** — Precept-specific, not generic

## Maintaining Existing Research

- **When a new component ships:** Check whether the survey covers its product category and deployment model
- **When distribution channels change:** Update the channel table and consumer profile
- **When a threat is mitigated:** Note the mitigation in the survey (don't delete the threat vector)
- **When dependencies change:** Re-evaluate supply chain section
