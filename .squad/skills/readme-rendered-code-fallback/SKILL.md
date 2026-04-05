---
name: "readme-rendered-code-fallback"
description: "Use a rendered asset plus a lightweight source fallback when GitHub cannot faithfully render a branded code sample."
domain: "documentation"
confidence: "high"
source: "earned"
---

## Context
Use this when a README needs a branded or syntax-colored code sample on GitHub, but GitHub's markdown rendering cannot preserve the intended visual treatment.

## Patterns
- Treat the rendered asset as the public-facing surface for GitHub.
- Keep the source nearby in a collapsed block so the sample remains copyable for developers and AI agents.
- Adjust surrounding copy so it explicitly describes the image as the GitHub-safe presentation, not as a temporary styling hack.
- Reuse the same source file or snippet that generated the asset so the image and plaintext stay synchronized.

## Examples
- `README.md` shows `brand/readme-hero-dsl.png` for the Subscription contract and keeps the exact DSL in a collapsed `Copyable DSL` block.
- `brand/readme-hero-dsl.precept` remains the source-of-truth text for the rendered sample.

## Anti-Patterns
- Leaving a long styled HTML code block inline and hoping GitHub preserves it.
- Showing only the image when the sample is meant to be copied or reused.
- Keeping a plaintext fallback that drifts from the rendered asset.
