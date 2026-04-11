# Semantic Visual System Source Prototype

This folder is a first-pass source architecture for composing a visual-system document from small authored parts instead of one large handcrafted HTML file.

Source types:

- `sections/` holds markdown-led narrative bands.
- `islands/` holds focused HTML fragments for layouts that are awkward to express in markdown.
- `data/` holds navigation and page metadata.
- `shell/` holds the outer document frame.
- `assets/` holds shared presentation behavior used by the generated file.

Build:

```text
node tools/scripts/build-semantic-visual-system.mjs
```

Generated output:

- `design/system/foundations/semantic-visual-system-composed.html`

This is a prototype architecture only. The canonical artifact remains `design/system/foundations/semantic-visual-system.html`.