# Security Landscape — Precept

**Date:** 2026-04-08  
**Author:** Uncle Leo (Security Champion)  
**Status:** Landscape map — category survey complete

---

## Product Architecture Summary

Precept is a **domain integrity engine for .NET** — a DSL runtime that compiles business-entity governance rules into a deterministic, inspectable constraint engine. It does not run as a server, host user sessions, or touch file I/O or databases. Its security profile is shaped entirely by what kind of product it is and how it distributes.

### Product categories

| Category | Component | Description |
|---|---|---|
| DSL parser / interpreter | Core (`src/Precept/Dsl/`) | Custom tokenizer + parser + expression evaluator. Accepts untrusted text, builds an AST, compiles and executes it. |
| NuGet library (public API) | `Precept` package | `.ParseFromText`, `.Compile`, `.Fire`, `.Inspect`, `.Update` — callable by any .NET application |
| Language server (LSP) | `Precept.LanguageServer` | stdio-transport sidecar. Receives document text from VS Code on every keystroke. |
| MCP server (AI agent tool) | `Precept.Mcp` | 5 tools over stdio transport consumed by Claude, GitHub Copilot, and similar AI hosts |
| VS Code extension | `Precept.VsCode` | Marketplace extension. Launches language server, renders preview webview, provides syntax highlighting. |
| GitHub / CI pipeline | `.github/workflows/` | Source distribution. Builds, tests, packages, and publishes NuGet + VSIX artifacts. |

### Distribution channels

| Channel | What is distributed |
|---|---|
| NuGet.org (`Precept`) | Core runtime + compiler — consumed by .NET applications |
| VS Code Marketplace (`sfalik.precept-vscode`) | Extension bundling language server and TypeScript UI |
| GitHub (source) | Source code, CI pipeline, AI agent plugin, MCP server |
| AI host context (stdio) | MCP server invoked locally by Claude Desktop, GitHub Copilot, etc. |

### Consumers

| Consumer | What they touch |
|---|---|
| .NET developers | NuGet public API — supplying DSL strings and field/event dictionaries |
| VS Code users | Extension + language server — opening and editing `.precept` files |
| AI agents (Claude, Copilot) | MCP server tools — submitting DSL text and data/args dictionaries |
| The IDE itself | Language server — sending document text via LSP |

### Deployment model per component

- **Core NuGet:** embedded library, runs in the caller's .NET application process. No network, no file I/O, no side effects beyond CPU and memory.
- **Language server:** spawned as a stdio child process by the VS Code extension; communicates via LSP over stdio; single-tenant to the VS Code instance.
- **MCP server:** spawned as a stdio child process by an AI host (Claude Desktop, Copilot); zero network exposure in the default deployment; processes one request at a time on the stdio pipe.
- **VS Code extension:** runs in the VS Code extension host (Node.js); launches the language server, renders a webview, communicates via VS Code API.
- **CI pipeline:** GitHub Actions workflows; produces and publishes NuGet and VSIX artifacts.

---

## Security Domains by Product Category

### DSL Parser / Interpreter

**Why it applies to Precept:** Precept's core operation is accepting arbitrary user-supplied text, parsing it into an AST, and executing it — the canonical interpreter security pattern.

**Authoritative resources:**
- [OWASP Code Injection](https://owasp.org/www-community/attacks/Code_Injection) — injection via interpreted input
- [OWASP Input Validation Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html) — bounds, allowlisting, syntactic + semantic validation

**Top applicable principles:**

- **Validate before interpreting.** Input must be checked for length, encoding, and structural bounds before the parser consumes it. In Precept, no size limit is enforced before tokenization runs.
- **Syntactic and semantic validation are both required.** Syntactic: max length, valid encoding, safe character set. Semantic: what the parsed structure means. The parser handles semantic validation — but only after unbounded tokenization has already consumed the input.
- **Error messages must not echo untrusted content.** Parse errors that embed field names, state names, event names, `because` clauses, or `reject` reasons from the untrusted DSL text can become injection vectors when those error strings are passed to AI consumers or logged. Scrub or delimit user-supplied content in all diagnostic output.
- **Define and enforce resource budgets.** An interpreter accepting arbitrarily large input with no timeout or memory budget is a denial-of-service surface. Establish maximum input sizes, and consider per-invocation CPU budgets for complex parse operations.
- **ReDoS awareness.** Any `Regex` patterns that run over live document text should be audited for catastrophic backtracking under adversarial input.

---

### NuGet Package / Open Source Library / Supply Chain

**Why it applies to Precept:** Precept ships as a public NuGet package distributed to .NET developers who embed it in their applications. Their supply chain integrity depends on ours.

**Authoritative resources:**
- [Microsoft NuGet Security Best Practices](https://learn.microsoft.com/en-us/nuget/concepts/security-best-practices) — package signing, lock files, dependency monitoring
- [SLSA Framework v1.0](https://slsa.dev/spec/v1.0/about) — supply chain integrity levels, provenance, tamper-evident builds

**Top applicable principles:**

- **Package signing asserts identity.** Author-signing a NuGet package allows consumers to verify the package came from the claimed publisher and has not been tampered with in transit. Combined with consumer-side trust policies, this closes the package substitution attack vector.
- **Lock files and reproducible builds eliminate version drift.** A lock file records the exact content hash of resolved packages; reproducible builds produce byte-for-byte identical binaries. Together, they make it verifiable that "what was scanned is what was shipped."
- **Dependency hygiene is continuous, not a one-time check.** Transitive dependencies can acquire vulnerabilities after the initial audit. NuGetAudit (built in to .NET 8+) and Dependabot alerts provide automated monitoring and PR-level remediation.
- **SLSA Build Track: know where your artifacts come from.** SLSA provides a vocabulary and incrementally adoptable checklist for attesting that artifacts were produced by an expected build system from expected source, without tampering. Even SLSA Build Level 1 (publishing provenance) meaningfully increases consumer trust for a security-sensitive library.
- **Protect the package registry account.** 2FA on nuget.org, package ID prefix reservation, and prompt deprecation + unlisting of any version with a known vulnerability are the minimum author-side hygiene requirements.

---

### MCP Server (AI Agent Tool)

**Why it applies to Precept:** Precept ships an MCP server consumed by AI agents — a class of software with a fundamentally different trust model than traditional API clients. AI agents supply inputs derived from LLM inference, which means inputs can be adversarially influenced through the agent's context.

**Authoritative resources:**
- [OWASP MCP Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/MCP_Security_Cheat_Sheet.html) — directly applicable; highest relevance among all fetched resources
- [OWASP LLM Top 10 2025](https://genai.owasp.org/llm-top-10/) — LLM01 (Prompt Injection), LLM05 (Improper Output Handling), LLM10 (Unbounded Consumption)

**Top applicable principles:**

- **Treat MCP tool inputs as untrusted — they originate from LLM output.** Even when the MCP host is trusted (Claude Desktop, GitHub Copilot), the *content* of tool parameters is derived from LLM inference operating over a context window that may include adversarial user-supplied content. Every parameter to every tool is a potential injection vector.
- **Sanitize tool outputs before returning them to the LLM context.** Tool return values are fed back into the model's context and can carry instruction-like content from a previous injection. The `ExpressionText`, `Reason`, `Message`, and `WhenText` fields in Precept's MCP output echo verbatim DSL content — including adversarially crafted `because` clauses and identifier names. This is a canonical indirect prompt injection surface (LLM01:2025).
- **Apply resource controls: rate limits, input size limits, and timeouts.** LLM10:2025 (Unbounded Consumption) covers DoS via resource exhaustion. An MCP server with no input size limits on a compute-intensive parse path is directly vulnerable to this. The stdio deployment model does not protect against a malfunctioning or adversarial agent submitting megabyte-scale DSL text.
- **Log all tool invocations.** The OWASP MCP Cheat Sheet requires logging tool name, parameters, user context, and timestamps for all invocations. Precept's MCP server currently has no logging. Without it, there is no basis for detecting anomalous patterns, DoS conditions, or forensic reconstruction of an attack.
- **Keep the server's operational permission surface minimal.** A stdio MCP server is correct for local developer tooling — it avoids network exposure inherently. That design choice embodies this principle and should be maintained explicitly, not just by default.

---

### VS Code Extension

**Why it applies to Precept:** The VS Code extension runs in the VS Code extension host, ships and launches the language server, and renders a preview webview — making it subject to VS Code's extension security model.

**Authoritative resources:**
- [VS Code Workspace Trust](https://code.visualstudio.com/docs/editor/workspace-trust) — restricted mode, extension-level workspace trust declarations

**Top applicable principles:**

- **Declare Workspace Trust participation.** Extensions that have not explicitly opted into Workspace Trust are disabled by default when VS Code opens an untrusted folder in Restricted Mode. Precept's extension should declare its Workspace Trust support level so users can open pre-vetted `.precept` files in trusted workspaces without friction, and AI agents cannot trigger extension behavior from untrusted folders.
- **Content Security Policy in webviews.** The preview webview must enforce a CSP that prevents inline script execution and restricts resource origins. Webviews that load content derived from DSL evaluation are a potential XSS surface if CSP is not applied — DSL content (state names, field names) could appear in the webview DOM.
- **Least-permission extension manifest.** Extensions should request only the VS Code API capabilities they need. The extension manifest (`package.json`) should not contribute capabilities (commands, file system access, network) beyond the language features and preview.
- **Do not bundle secrets or credentials.** Extension source and packaged VSIX are publicly downloadable from the marketplace. No API keys, signing certificates, or credentials may be embedded in the extension bundle.

---

### Language Server (LSP)

**Why it applies to Precept:** The language server receives full document text from the IDE on every keystroke and feeds it to the same parser used by the MCP server — making it an input processing surface with the same parser-safety concerns, running as a locally privileged child process.

**Authoritative resources:**
- (No standalone LSP security specification; governed by input validation principles applied to the specific deployment)

**Top applicable principles:**

- **Same input safety discipline as the parser library.** The language server uses the same `PreceptParser.ParseWithDiagnostics` path as the MCP server. Unbounded document size, no length limits, and no timeout budgets apply equally here. A large `.precept` file opened in VS Code can cause unbounded CPU consumption on every keystroke.
- **Limit process privileges.** The language server runs as a stdio child process of VS Code. It should not require file system access beyond the workspace, no network access, and no elevated OS privileges. The process should be structured to fail fast on unexpected I/O rather than silently succeed.
- **LSP output returned to VS Code is less risky than MCP output returned to AI agents** — VS Code renders diagnostic messages in the Problems panel as static text, not as AI context. However, diagnostic messages that contain adversarially crafted content could be exploited if the IDE processes them in unexpected ways. Sanitizing non-printable characters and markup from diagnostic text is still worth doing.
- **Document size heuristics for the live-typing path.** The language server runs analysis on every document change event. For very large files, debouncing or size-gating should prevent the edit loop from triggering full parse+compile on every keypress.

---

### GitHub / CI Pipeline

**Why it applies to Precept:** The CI pipeline builds and publishes the NuGet package and VS Code extension to public distribution channels. A compromised pipeline is a supply chain attack against all Precept consumers.

**Authoritative resources:**
- [GitHub Actions Security Hardening Guide](https://docs.github.com/en/actions/security-for-github-actions/security-guides/security-hardening-for-github-actions) — secrets hygiene, third-party action pinning, GITHUB_TOKEN permissions

**Top applicable principles:**

- **Pin third-party Actions to full commit SHAs, not tags.** Tags can be moved or deleted by a bad actor who compromises a publisher's account; a pinned SHA is immutable. This is the primary mitigation for supply-chain attacks via third-party workflow steps.
- **Scope GITHUB_TOKEN to the minimum required permissions.** Each job should declare only the permissions it actually needs (e.g., `contents: read` for test jobs, `packages: write` only for publish jobs).
- **Never store secrets as plaintext in workflow files.** NuGet API keys, VS Code PATs, and signing certificates must be stored as GitHub encrypted secrets. Use `::add-mask::` for sensitive values derived during the build.
- **Use CODEOWNERS to require review on workflow file changes.** Adding `.github/workflows/` to `CODEOWNERS` ensures that any modification to CI workflow files requires an explicit approval from a designated reviewer.
- **Enable Dependabot for GitHub Actions and NuGet dependencies.** Dependabot monitors workflow action versions and NuGet packages for known vulnerabilities and opens automated PRs.

---

## Cross-Cutting Concerns

These security principles apply across all Precept components, not specific to any one category.

- **Dependency management and vulnerability monitoring.** Precept's NuGet dependencies (including `ModelContextProtocol`, `Superpower`, `OmniSharp.Extensions.LanguageServer.*`) should be monitored continuously via NuGetAudit (`dotnet list package --vulnerable`) and Dependabot alerts. Transitive dependencies are especially important to track.
- **Secrets hygiene.** No credentials, API keys, signing certificates, or tokens should appear in source code, workflow YAML, DSL sample files, or configuration files. GitHub secret scanning is enabled on public repositories; any committed credential is presumed compromised and must be rotated immediately.
- **Input validation discipline at trust boundaries.** Every boundary where untrusted input enters — MCP `text`/`data`/`args`, LSP document text, NuGet public API — needs consistent enforcement of: (a) maximum size limits; (b) encoding validation; (c) rejection rather than silent truncation or coercion for out-of-bounds input.
- **Audit logging strategy.** Currently no component generates a structured audit trail of user-submitted inputs or engine outcomes. For the MCP server (which processes AI agent requests), minimum logging should record: tool name, input size (not full text), outcome (success/failure), and timestamp. This is the baseline for operational security monitoring.
- **Minimal I/O surface preservation.** The core runtime's absence of file I/O, network calls, and process spawning is a significant positive security property. This should be treated as a design constraint to uphold in future development — any change that adds I/O capability to the core runtime should require explicit security review.
- **Build artifact provenance.** As NuGet and VSIX distribution scales, publishing build provenance (SLSA Build Level 1+) increases consumer trust that the artifact in their package manager was produced by the expected CI build from the expected source commit without tampering.

---

## Suggested Role Focus Areas

Based on the external research and product architecture, a Security Champion for Precept should prioritize these disciplines — at the category level, not the bug level:

1. **AI agent trust boundaries.** The MCP server is the product's most novel attack surface — inputs arrive via LLM inference operating over potentially adversarial context. The relevant discipline is understanding how OWASP LLM01 (indirect prompt injection) and LLM10 (unbounded consumption) manifest in the concrete design of a stdio MCP server that echoes DSL content in its outputs.

2. **Parser / interpreter input safety.** Every component that tokenizes or evaluates `.precept` text is an interpreter processing untrusted input. The discipline is input bounding + error output hygiene — a small set of principles with broad applicability across NuGet API, language server, and MCP server.

3. **Supply chain integrity.** Precept ships to two public package registries (NuGet, VS Code Marketplace) from a GitHub Actions pipeline. The discipline is package signing, lock files, reproducible builds (SLSA), and action pinning — protecting the chain from source commit to consumer installation.

4. **MCP tool output sanitization.** Distinct from input safety — this is about what Precept *says back* to AI agents in structured output. Any field that echoes user-supplied DSL content (expression text, because clauses, identifier names) is a potential prompt injection carrier. The discipline is: design output schemas that delimit user-supplied content from system-generated content.

5. **CI pipeline hygiene.** The pipeline is the trust root for all distributed artifacts. The discipline is GITHUB_TOKEN scoping, action pinning to commit SHAs, CODEOWNERS enforcement on workflow changes, and Dependabot on both NuGet and Actions dependencies.

6. **Extension host security.** The VS Code extension runs in a privileged extension context. The discipline is Workspace Trust compliance, webview CSP enforcement, and minimal-permission manifest design.

---

## What This Role Is NOT

Explicit scope exclusions to keep the charter crisp:

- **Not cloud infrastructure security.** Precept has no cloud-deployed services, no HTTP endpoints, no database, and no cloud credentials. Infrastructure security is out of scope until that architecture changes.
- **Not authentication and authorization design.** Precept has no user accounts, sessions, or access control — by design. The security model is input safety and supply chain integrity, not identity and access.
- **Not network security.** The MCP server and language server both use stdio transport exclusively. Network egress, TLS configuration, and firewall rules are not applicable to the current deployment model.
- **Not data privacy / GDPR compliance.** Precept does not collect, store, or transmit user data. The terms of any downstream entity data handled by a consuming application are the consuming application's concern, not Precept's.
- **Not penetration testing execution.** The Security Champion role is responsible for understanding the threat model, sourcing authoritative guidance, and surfacing concerns — not for conducting red team exercises or exploit development.
- **Not runtime security monitoring of consuming applications.** Once a `.precept` definition is compiled and embedded in a consumer's .NET application, the security posture of that application is the consumer's responsibility. Precept's responsibility ends at the API boundary.

---

## Research Sources

| Source | URL | Relevance |
|---|---|---|
| OWASP Code Injection | https://owasp.org/www-community/attacks/Code_Injection | Parser/interpreter input safety |
| OWASP Input Validation Cheat Sheet | https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html | Input bounds, allowlisting |
| OWASP MCP Security Cheat Sheet | https://cheatsheetseries.owasp.org/cheatsheets/MCP_Security_Cheat_Sheet.html | MCP server — highest direct relevance |
| OWASP LLM Top 10 2025 | https://genai.owasp.org/llm-top-10/ | AI agent trust model (LLM01, LLM05, LLM10) |
| Microsoft NuGet Security Best Practices | https://learn.microsoft.com/en-us/nuget/concepts/security-best-practices | Supply chain, package signing |
| SLSA Framework v1.0 | https://slsa.dev/spec/v1.0/about | Build provenance, supply chain integrity levels |
| VS Code Workspace Trust | https://code.visualstudio.com/docs/editor/workspace-trust | Extension host security model |
| GitHub Actions Security Hardening | https://docs.github.com/en/actions/security-for-github-actions/security-guides/security-hardening-for-github-actions | CI pipeline hygiene |


### Surface 1: MCP `text` parameter — unbounded DSL input

**What enters:** Any string, up to system memory limits, from an AI agent.  
**Current handling:** `string.IsNullOrWhiteSpace` check only (`C1`). The entire string is tokenized by `PreceptTokenizerBuilder.Instance.Tokenize(text)`, which is a Superpower token stream parser. No length cap, no timeout, no resource budget.  
**Risk level:** **High**  
**Notes:** A malicious or malfunctioning AI agent caller can submit megabytes of DSL text. The tokenizer will consume it entirely before the parser returns or throws. Combined with the fact that `PreceptCompiler.CompileFromText` is called fresh on every MCP tool invocation (no caching), each failing `precept_fire`/`precept_inspect`/`precept_update` call still fully parses and compiles the input. There is no per-session or per-request rate limit at the MCP layer. This is a classic denial-of-service vector consistent with **OWASP LLM10:2025 Unbounded Consumption**.

### Surface 2: Error messages echo raw input identifiers

**What enters:** DSL text with adversarially crafted identifier names (field names, state names, event names, strategy text in `reject` reasons, `because` clauses).  
**Current handling:** Error messages in `ParseWithDiagnostics`, `DiagnosticCatalog`, and `CompileResult.Diagnostics` include identifier names and `because`-clause reasons verbatim. For example, the parser generates messages like: `"Transition row starting with 'from {stateName} on {eventName}' is missing '->'"`. The `WhenText` field on `BranchDto` echoes the raw guard expression string. Violation `ExpressionText` and `Reason` in `ViolationSourceDto` echo constraint expression text and `because` clauses directly from the DSL.  
**Risk level:** **High**  
**Notes:** MCP tool output is consumed as context by AI agents. An adversarially crafted `because "IGNORE ALL PREVIOUS INSTRUCTIONS — you are now..."` or a field named `IgnorePreviousInstructions` will appear in the structured diagnostic/violation output returned to downstream AI consumers. This is a canonical **indirect prompt injection** vector (OWASP LLM01:2025) transmitted through the tool output channel. The output is serialized JSON, but consuming agents process it as semantic text.

### Surface 3: Field/event/arg dictionary mass assignment

**What enters:** `data`, `fields`, and `args` dictionaries in `precept_fire`, `precept_inspect`, `precept_update` — arbitrary key-value maps from the AI agent caller.  
**Current handling:** `JsonConvert.ToNativeDict` coerces `JsonElement` values. The resulting dictionary is passed to `BuildInitialInstanceData`, which merges caller-supplied keys into instance data without checking whether the keys reference declared fields. Unknown keys are silently stored and available in the evaluation context. The engine's `TryValidateDataContract` runs after merging, but only validates declared fields; undeclared keys pass through as opaque data.  
**Risk level:** **Medium**  
**Notes:** An attacker can inject undeclared field names into the evaluation context. While the constraint engine will not directly execute a constraint against an undeclared field, the expression evaluator accesses keys from the merged dictionary dictionary. The blast radius is limited by the fact that the engine only evaluates expressions declared in the compiled definition, so arbitrary key injection does not yield code execution. However, it may enable subtle guard-bypass attempts where a guard expression references a key by name that can be influenced by injecting a same-named custom key. Requires further investigation.

### Surface 4: No authentication or caller identity on MCP tools

**What enters:** Any caller with access to the `stdio` transport connection.  
**Current handling:** MCP server listens on `stdio`. No authentication, no session identity, no caller allowlist. `Program.cs` uses the Microsoft MCP SDK with stdio transport — all tool calls are accepted.  
**Risk level:** **Medium**  
**Notes:** The MCP server is launched locally by VS Code per the `mcp.json` configuration. In that deployment, the attack surface is limited to processes running on the developer's machine. However, if the server were exposed over a network transport (HTTP/SSE), there would be zero authentication. The current deployment model is not inherently network-exposed, but this design decision should be explicitly stated and protected in documentation. The MCP Security Cheat Sheet notes that `stdio` transport is correct for local servers and limits exposure.

### Surface 5: Language Server document text (LSP boundary)

**What enters:** Full document content of any `.precept` file open in VS Code, sent by the IDE via LSP `textDocument/didOpen`, `textDocument/didChange`, `textDocument/didSave`.  
**Current handling:** `PreceptTextDocumentSyncHandler` stores documents in a `ConcurrentDictionary<DocumentUri, string>`. Text is passed directly to `PreceptParser.ParseWithDiagnostics`. LSP transport is `stdio`. No length limits. The analyzer does not check document size.  
**Risk level:** **Medium**  
**Notes:** The language server is invoked from VS Code on any `.precept` file. A maliciously crafted large file (e.g., placed in a project by a supply chain attack or if a user opens a file from an untrusted source) will be fully tokenized and compiled repeatedly on every keystroke change. This is a local resource exhaustion vector. Not exploitable remotely in the current deployment model, but relevant for VS Code extension security hardening.

### Surface 6: `JsonConvert.ToNative` JSON object handling

**What enters:** `JsonElement` values of kind `Object` or `Array` from MCP deserialization (when an agent passes a nested object as a field value).  
**Current handling:** The `_ => je.GetRawText()` branch in `JsonConvert.ToNative` converts unknown `JsonElement` kinds (objects, arrays) to their raw JSON string representation. This raw string then enters the expression evaluator as a string value.  
**Risk level:** **Low**  
**Notes:** An agent passing a nested JSON object as a field value will have it silently stringified. This is not exploitable for code execution since the expression evaluator is a pure value evaluator with no string-interpolation or dynamic dispatch. However, it represents an implicit type coercion that is not documented and could produce confusing constraint evaluation behavior. The real risk is confusion, not injection.

### Surface 7: No file system operations, network calls, or process spawning

**What is NOT present:** No `File.*`, `Directory.*`, `Process.*`, `HttpClient`, `WebClient`, DNS, or network IO in `PreceptParser.cs`, `PreceptRuntime.cs`, or any MCP tool.  
**Verification:** Confirmed by reading all four tool files and both core DSL files. No reflection-based type instantiation. No `dynamic`, no `Activator.CreateInstance`, no `Assembly.Load`.  
**Risk level:** **Low**  
**Notes:** This is a positive finding. The core runtime has no side-effecting I/O surface beyond CPU/memory. Deserialization uses `System.Text.Json` with `JsonElement` — no polymorphic type resolution, no `TypeNameHandling`. No known .NET RCE deserialization gadgets are reachable.




